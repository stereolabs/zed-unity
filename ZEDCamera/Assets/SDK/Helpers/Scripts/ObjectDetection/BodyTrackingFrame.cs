using System.Collections.Generic;
using UnityEngine;
using sl;

/// <summary>
/// Holds all bodies detected by the ZED Object Detection module from a single ZED camera during a single frame. 
/// Holds metadata about the frame and camera, and provides helper functions for filtering out the detected objects. 
/// <para>This is provided when subscribed to the ZEDManager.OnBodyTracking event.</para>
/// </summary><remarks>
/// This is a higher level version of sl.Bodies, which comes directly from the ZED SDK and doesn't follow Unity conventions. 
/// </remarks>
public class BodyTrackingFrame
{
    private Bodies bodies;
    /// <summary>
    /// The raw Bodies object that this body is an abstraction of - Bodies comes 
    /// directly from the SDk and doesn't follow Unity conventions. 
    /// </summary>
    public Bodies rawbodies
    {
        get
        {
            return bodies;
        }
    }

    /// <summary>
    /// Timestamp of when the body tracking module finished detecting the frame.
    /// </summary>
    public ulong timestamp
    {
        get
        {
            return bodies.timestamp;
        }
    }

    private int frameDetected = -1;
    /// <summary>
    /// Value of Time.frameCount when this object was created. Assumes the constructor was called in the same frame
    /// that the object was detected by the SDK. 
    /// </summary>
    public int frameCountAtDetection
    {
        get
        {
            return frameDetected;
        }
    }
    /// <summary>
    /// How many objects were detected in total. 
    /// <para>Note this does not include any filtering, so it includes objects in the SEARCHING and OFF tracking states.</para>
    /// </summary>
    public int bodyCount
    {
        get
        {
            return bodies.nbBodies;
        }
    }


    /// <summary>
    /// The manager class responsible for the ZED camera that detected the bodies in this frame. 
    /// </summary>
    public ZEDManager detectingZEDManager;

    private List<DetectedBody> detBodies = new List<DetectedBody>();
    /// <summary>
    /// All bodies detected within this frame. Use GetFilteredObjectList to filter them by category or confidence. 
    /// </summary>
    public List<DetectedBody> detectedBodies
    {
        get
        {
            return detBodies;
        }
    }

    /// <summary>
    /// Constructor that sets up this frame and spawns DetectedBody objects for each raw BodyData in the frame. 
    /// </summary>
    /// <param name="bodies">Raw sl.ObjectsFrame object from the SDK, that this object is an abstraction of.</param>
    /// <param name="detectingmanager">ZEDManager that represents the camera that detected this frame.</param>
    public BodyTrackingFrame(Bodies bframe, ZEDManager detectingmanager)
    {
        bodies = bframe;
        detectingZEDManager = detectingmanager;
        frameDetected = Time.frameCount;

        Vector3 campos = detectingmanager.GetLeftCameraTransform().position;
        Quaternion camrot = detectingmanager.GetLeftCameraTransform().rotation;

        for (int i = 0; i < bodies.nbBodies; i++)
        {
            DetectedBody dobj = new DetectedBody(bframe.bodyList[i], detectingmanager, campos, camrot);
            detBodies.Add(dobj);
        }
    }

    /// <summary>
    /// Returns the list of detected objects from this frame, but with only objects of the desired tracking state and 
    /// minimum confidence included.
    /// <para>Note: The object detection module itself already filters confidence, adjustable with ZEDManager.objectDetectionConfidenceThreshold.
    /// It's simpler to set this value instead (via the Inspector) unless you want multiple filters.</para>
    /// </summary>
    /// <param name="tracking_ok">True to include objects where tracking data is known.</param>
    /// <param name="tracking_searching">True to include objects where the SDK is currently searching for its position.</param>
    /// <param name="tracking_off">True to include objects that have no tracking data at all.</param>
    /// <param name="confidencemin">Minimum confidence threshold.</param>
    public List<DetectedBody> GetFilteredObjectList(bool tracking_ok, bool tracking_searching, bool tracking_off, float confidencemin = 0)
    {
        List<DetectedBody> filteredobjects = new List<DetectedBody>();

        foreach(DetectedBody dobj in detBodies)
        {
            if (dobj.confidence < confidencemin && dobj.confidence != -1) continue;

            switch (dobj.trackingState)
            {
                case OBJECT_TRACKING_STATE.OK:
                    if (tracking_ok) filteredobjects.Add(dobj);
                    break;
                case OBJECT_TRACKING_STATE.SEARCHING:
                    if (tracking_searching) filteredobjects.Add(dobj);
                    break;
                case OBJECT_TRACKING_STATE.OFF:
                    if (tracking_off) filteredobjects.Add(dobj);
                    break;
            }
        }
        return filteredobjects;
    }

    /// <summary>
    /// Releases all textures and ZEDMats in all detected objects. 
    /// Call when the frame won't be used anymore to avoid memory leaks. 
    /// </summary>
    public void CleanUpAllBodies()
    {
        foreach(DetectedBody dobj in detBodies)
        {
            dobj.CleanUpTextures();
        }
    }
}
