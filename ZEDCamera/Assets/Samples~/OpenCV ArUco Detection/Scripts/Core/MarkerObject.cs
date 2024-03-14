#if ZED_OPENCV_FOR_UNITY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ArucoModule;

/// <summary>
/// Base class for objects that should do something when a specific marker is detected by ZEDArUcoDetectionManager.
/// Inherit from this class to make custom behaviors when the marker is detected and not detected. 
/// </summary>
public abstract class MarkerObject : MonoBehaviour
{
    /// <summary>
    /// ID of the marker that will trigger the MarkerDetected function to be called. 
    /// ID corresponds to an ID in the OpenCV dictionary. 
    /// </summary>
    [Tooltip("ID of the marker that will trigger the MarkerDetected function to be called.\r\n" +
        "ID corresponds to an ID in the OpenCV dictionary. ")]
    public int markerID = 0;

    // Use this for initialization
    protected virtual void Start ()
    {
        ZEDArUcoDetectionManager.RegisterMarker(this); //Tells ZEDArUcoDetectionManager to tell us when our marker is detected. 
    }

    /// <summary>
    /// Called when the object's marker was detected this frame, once per detection. 
    /// Use to reposition the object or perform similar logic. 
    /// <para>To support detecting multiple copies of the same marker, it's usually best to use MarkerDetectedAll
    /// to avoid repeat behavior.</para>
    /// </summary>
    /// <param name="worldposition">World space position of the marker.</param>
    /// <param name="worldrotation">World space rotation of the marker.</param>
    public virtual void MarkerDetectedSingle(Vector3 worldposition, Quaternion worldrotation) { }

    /// <summary>
    /// Called when the object's marker did not appear after detecting markers. 
    /// Use to hide/disable the object or perform similar logic. 
    /// </summary>
    public virtual void MarkerNotDetected() { }

    /// <summary>
    /// Called when the object's marker was detected this frame, with the world poses of each marker that was detected. 
    /// Use when you want to support multiple 
    /// </summary>
    /// <param name="worldposes">One Pose (with a world space translation/position and rotation) for each marker of markerID.</param>
    public virtual void MarkerDetectedAll(List<sl.Pose> worldposes) { }

    protected virtual void OnDestroy()
    {
        ZEDArUcoDetectionManager.DeregisterMarker(this);
    }
}

#endif