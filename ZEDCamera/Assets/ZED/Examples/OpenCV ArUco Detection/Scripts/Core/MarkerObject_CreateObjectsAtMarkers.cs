#if ZED_OPENCV_FOR_UNITY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Creates a pre-specified object at each detected marker of markerID and causes that object to follow the marker. 
/// Supports multiple instances of the same marker, unlike MarkerObject_MoveToMarker, and will manage their positions as they move and disappear. 
/// </summary><remarks>
/// It is generally recommended not to show multiple copies of the same marker in the image, instead relying on a unique
/// marker for every instantiation of the object (see MarkerObject_MoveToMarker). This script was intended for a situation like an AR
/// trading card game, where you may want one marker for each type of a creature/object with the capacity to have multiple copies
/// out at the same time. It attempts to match each marker with an existing object by checking the distances between them. 
/// </remarks>
public class MarkerObject_CreateObjectsAtMarkers : MarkerObject
{
    /// <summary>
    /// Object to be instantiated over each detected marker of markerID. 
    /// </summary>
    [Tooltip("Object to be instantiated over each detected marker of markerID. ")]
    public GameObject prefab;

    /// <summary>
    /// How many objects to instantiate into the 'pool' at start, so they only need to be switched on when a marker is detected. 
    /// Increasing makes load time slightly longer, but avoids small performance cost when a new marker is added in excess of this number. 
    /// </summary>
    [Tooltip("How many objects to instantiate into the 'pool' at start, so they only need to be switched on when a marker is detected.\r\n" +
        "Increasing makes load time slightly longer, but avoids small performance cost when a new marker is added in excess of this number. ")]
    public int numberPreInstantiatedObjects = 3;

    /// <summary>
    /// Instances of the prefab that have been instantiated but aren't currently used. 
    /// </summary>
    private Queue<GameObject> inactiveObjectPool = new Queue<GameObject>();
    /// <summary>
    /// Instances of the prefab that are enabled and being moved over markers. 
    /// </summary>
    private List<ActiveAnchored> activeObjects = new List<ActiveAnchored>();

    /// <summary>
    /// How many frames to use to smooth marker position/rotation updates.
    /// Larger numbers reduce jitter from detection inaccuracy, but add latency to the marker's movements.
    /// </summary>
    [Tooltip("How many frames to use to smooth marker position/rotation updates. " +
        "Larger numbers reduce jitter from detection inaccuracy, but add latency to the marker's movements.")]
    public int smoothedFrames = 4;


    /// <summary>
    /// If true, the object will be disabled when not out of view. 
    /// This only applies if the marker has been detected at least once - otherwise it'll still be disabled. 
    /// Set to false for unmoving marker, and true for objects that may move when the camera can't see the marker. 
    /// </summary>
    [Tooltip("If true, the object will be disabled when not out of view. " +
        "This only applies if the marker has been detected at least once - otherwise it'll still be disabled.\r\n" +
        "Set to false for unmoving marker, and true for objects that may move when the camera can't see the marker. ")]
    public bool disableWhenLeftView = false;

    /// <summary>
    /// How many consecutive frames of detection does this object not have to be seen before it's disabled.
    /// Used to avoid flickering when noise causes frames not to be detected for a short time
    /// when they're actually in view of the camera.
    /// </summary>
    [Tooltip("How many consecutive frames of detection does this object not have to be seen before it's disabled.\r\n" +
        "Used to avoid flickering when noise causes frames not to be detected for a short time " +
        "when they're actually in view of the camera.")]
    public int missedFramesUntilDisabled = 2;


    protected override void Start()
    {
        base.Start();

        //gameObject.SetActive(false);

        for (int i = 0; i < numberPreInstantiatedObjects; i++)
        {
            GameObject pre = Instantiate(prefab);
            pre.SetActive(false);
            pre.transform.SetParent(transform);
            inactiveObjectPool.Enqueue(pre);
        }
    }

    public override void MarkerNotDetected()
    {
        //This means that no markers were called at all this frame. Register a hidden frame with all active objects. 
        //If any report that it has exceeded the number of consecutive hidden frames allowed, hide it and return to the inactivePool.
        for(int i = 0; i < activeObjects.Count; i++)
        {
            if (activeObjects[i].CountHiddenFrame() == true)
            {
                GameObject gotoremove = activeObjects[i].gameObject;
                activeObjects.RemoveAt(i);
                ReturnObjectToQueue(gotoremove);
            }
        }
    }

    /// <summary>
    /// Given the positions of all detected markers of markerID this frame, matches each one to an instance of the prefab. 
    /// Will activate or disable the prefab instances to make sure there is one for each marker. 
    /// <remarks>The correct marker for each existing object is determined each frame by checking the nearest marker to each.</remarks>
    /// </summary>
    /// <param name="worldposes">List of all world poses (translation/position and rotation) where each one was a detected marker of markerID.</param>
    public override void MarkerDetectedAll(List<sl.Pose> worldposes)
    {
        List<ActiveAnchored> active = new List<ActiveAnchored>(activeObjects); //Make copy so we can remove them. 
        //List<Vector3> worldposlist = worldposes.Select(a => a.translation).ToList();

        //List<Vector3> worldpostodiscard = new List<Vector3>();
        //First, we handle active objects that still have a valid tracker. So we count up to the lowest between the number
        //of active objects and the number of detected markers, and match one-to-one. 
        int lowestlistcount = (activeObjects.Count < worldposes.Count) ? activeObjects.Count : worldposes.Count;

        for (int i = 0; i < lowestlistcount; i++)
        // (int i = 0; i < activeObjects.Count; i++)
        {
            //We make a new list at each iteration because we want to find the closest _pair_ of object and pose, 
            //Not just match each object to a pose, or each pose to an object. 
            //Otherwise, objects that don't correspond to a marker because their marker is hidden (but not enough time 
            //has passed for it to be destroyed) can warp to a different one. 
            List<Vector3> worldposlist = worldposes.Select(a => a.translation).ToList();

            active.Sort((x, y) => x.GetNearestMarkerDist(worldposlist).CompareTo(y.GetNearestMarkerDist(worldposlist)));
            Vector3 nearest;
            float neardist = active[0].GetNearestMarkerDist(worldposlist, out nearest);

            try
            {
                //sl.Pose nearestpose = worldposes.First(x => x.translation == nearest); //Simpler, but harder to handle errors. 
                //More complicated version of the above, but more transparent. 
                sl.Pose nearestpose;
                bool foundone = false;
                for(int p = 0; p < worldposes.Count; p++)
                {
                    if(worldposes[p].translation == nearest)
                    {
                        foundone = true;
                        nearestpose = worldposes[p];

                        active[0].SetSmoothedPose(nearestpose.translation, nearestpose.rotation);

                        active.RemoveAt(0);
                        worldposes.Remove(nearestpose);
                        break;
                    }
                }

                if(!foundone)
                {
                    Debug.LogError("No close pose for marker, but there should be one available.");
                }
            }
            catch (System.Exception e)
            {
                print(e);
            }
        }


        //If we don't have enough active objects, spawn enough objects to fill the remaining world poses.
        for (int i = 0; i < worldposes.Count; i++) //Won't do anything unless we don't have enough active objects. 
        {
            GameObject newgo = GetInstantiatedObject();
            newgo.transform.position = worldposes[i].translation;
            //newgo.transform.position = worldposlist[i];
            newgo.transform.rotation = worldposes[i].rotation;
            ActiveAnchored newaa = new ActiveAnchored(this, newgo);
            activeObjects.Add(newaa);
        }

        //If we have too many active objects, register a hidden frame with each of them, and disable/return to the inactivePool if hidden for long enough. 
        for(int i = 0; i < active.Count; i++) //Won't do anything unless we have leftover active gameobjects. 
        {
            if (active[i].CountHiddenFrame() == true)
            {
                GameObject gotoremove = active[i].gameObject;
                //activeObjects.Remove(activeObjects.First(x => x.gameObject == gotoremove));
                activeObjects.Remove(active[i]);
                ReturnObjectToQueue(gotoremove);
            }
        }

    }

    /// <summary>
    /// Returns a pre-instantiated instance of the prefab from the inactivePool. If there are none available, it will instantiate one. 
    /// </summary>
    private GameObject GetInstantiatedObject()
    {
        //Make sure one is available. 
        if (inactiveObjectPool.Count == 0)
        {
            GameObject pre = Instantiate(prefab);
            pre.transform.SetParent(transform);
            return pre;
        }

        GameObject fromqueue = inactiveObjectPool.Dequeue();
        fromqueue.SetActive(true);
        return fromqueue;
    }

    /// <summary>
    /// Disables an object and returns it to the inactivePool. 
    /// </summary>
    /// <param name="go">Object to be disabled.</param>
    private void ReturnObjectToQueue(GameObject go)
    {
        go.SetActive(false);
        inactiveObjectPool.Enqueue(go);
    }

    /// <summary>
    /// Handles a GameObject instantiated from the prefab that is moved to follow a marker. 
    /// When used to move/hide the GameObjects, automatically provides smoothing, counting hidden frames to allow
    /// for some before disabling, and helper functions. 
    /// <para>Kept in the activeObjects list. Created each time an object is activated - not kept in a pool, unlike the GameObjects themselves.</para>
    /// </summary>
    private class ActiveAnchored
    {
        /// <summary>
        /// Object this ActiveAnchored instance represents. 
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// How many frames in a row the corresponding marker was not seen. 
        /// </summary>
        public int hiddenFramesCount = 0;

        /// <summary>
        /// Instance of MarkerObject_CreateObjectsAtMarkers that spawned this instance. Used to check settings, like smooth frame counts. 
        /// </summary>
        private MarkerObject_CreateObjectsAtMarkers parentManager;

        /// <summary>
        /// List of last X positions updated, where X is the max number of smoothed frames (unless that many haven't happened yet).
        /// Used for smoothing positions. 
        /// </summary>
        private CappedStack<Vector3> positionStack;
        /// <summary>
        /// List of last X rotations updated, where X is the max number of smoothed frames (unless that many haven't happened yet).
        /// Used for smoothing rotations. 
        /// </summary>
        private CappedStack<Quaternion> rotationStack;

        public ActiveAnchored(MarkerObject_CreateObjectsAtMarkers parent, GameObject targetobject)
        {
            parentManager = parent;
            gameObject = targetobject;

            positionStack = new CappedStack<Vector3>(parentManager.smoothedFrames);
            rotationStack = new CappedStack<Quaternion>(parentManager.smoothedFrames);
        }

        /// <summary>
        /// Given the position of a marker from this frame, averages it with the last few registered poses
        /// to calculate a smoothed pose, and sets the gameObject's position/rotation to that pose. 
        /// </summary>
        /// <param name="pos">World position of marker as detected this frame.</param>
        /// <param name="rot">World rotation of marker as detected this frame.</param>
        public void SetSmoothedPose(Vector3 pos, Quaternion rot)
        {
            hiddenFramesCount = 0;

            positionStack.Push(pos);
            gameObject.transform.position = GetAveragePosition();

            rotationStack.Push(rot);
            gameObject.transform.rotation = GetAverageRotation();

        }

        /// <summary>
        /// Registers that we didn't see the object this frame. 
        /// If it's been hidden enough consecutive frames to be hidden, returns true, 
        /// which the MarkerObject_MoveMarkerAdvanced will use to disable the object. 
        /// </summary>
        /// <returns>True if we hid it this frame.</returns>
        public bool CountHiddenFrame()
        {
            hiddenFramesCount++;
            //positionStack.Clear();
            //rotationStack.Clear();
            if (hiddenFramesCount >= parentManager.missedFramesUntilDisabled)
            {
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Shorthand for calling Vector3.Distance between the given point and this ActiveAnchor's gameObject's position.  
        /// </summary>
        public float GetDist(Vector3 worldpos) 
        {
            return Vector3.Distance(gameObject.transform.position, worldpos);
        }

        /// <summary>
        /// Given a list of potential poses, returns the distance to the nearest one to this ActiveAnchor.
        /// </summary>
        public float GetNearestMarkerDist(List<Vector3> potentialposes)
        {
            Vector3 nearestpos; //Not used but needed to compile. 
            return GetNearestMarkerDist(potentialposes, out nearestpos);
        }

        /// <summary>
        /// Given a list of potential poses, returns the distance to the nearest one to this ActiveAnchor.
        /// Overload that also provides the nearest position it found. 
        /// </summary>
        public float GetNearestMarkerDist(List<Vector3> potentialpos, out Vector3 nearestpos)
        {
            float nearestdist = Mathf.Infinity;
            nearestpos = Vector3.zero;
            foreach (Vector3 vec in potentialpos)
            {
                float dist = GetDist(vec);
                if (dist < nearestdist)
                {
                    nearestdist = dist;
                    nearestpos = vec;
                }
            }

            return nearestdist;
        }

        /// <summary>
        /// Calculates average position of all positions in positionStack.
        /// </summary>
        /// <returns>Smoothed position.</returns>
        private Vector3 GetAveragePosition()
        {
            Vector3 sumvector = Vector3.zero;
            foreach(Vector3 vec in positionStack)
            {
                sumvector += vec;
            }
            return sumvector / positionStack.Count;
        }

        /// <summary>
        /// Calculates average rotation of all rotations in rotationStack.
        /// </summary>
        /// <returns>Smoothed rotation.</returns>
        private Quaternion GetAverageRotation()
        {
            Vector4 sumquatvalues = Vector4.zero;
            foreach(Quaternion quat in rotationStack)
            {
                sumquatvalues.x += quat.x;
                sumquatvalues.y += quat.y;
                sumquatvalues.z += quat.z;
                sumquatvalues.w += quat.w;
            }

            Quaternion returnquat = new Quaternion();
            sumquatvalues /= rotationStack.Count;
            returnquat.x = sumquatvalues.x;
            returnquat.y = sumquatvalues.y;
            returnquat.z = sumquatvalues.z;
            returnquat.w = sumquatvalues.w;

            return returnquat;
        }
    }
}

#endif