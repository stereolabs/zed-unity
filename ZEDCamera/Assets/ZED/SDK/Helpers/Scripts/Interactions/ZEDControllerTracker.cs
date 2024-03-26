using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// Causes the GameObject it's attached to to position itself where a tracked VR object is, such as
/// a Touch controller or Vive Tracker, but compensates for the ZED's latency. This way, virtual
/// controllers don't move ahead of its real-world image.
/// This is done by logging position data from the VR SDK in use (Oculus or OpenVR/SteamVR) each frame, but only
/// applying that position data to this transform after the delay in the latencyCompensation field.
/// Used in the ZED GreenScreen, Drone Shooter, Movie Screen, Planetarium and VR Plane Detection example scenes.
/// </summary>
public class ZEDControllerTracker : MonoBehaviour
{
    /// <summary>
    /// Type of VR SDK loaded.
    /// </summary>
	public string loadedDevice = "";

    /// <summary>
    /// Per each tracked object ID, contains a list of their recent positions.
    /// Used to look up where OpenVR says a tracked object was in the past, for latency compensation.
    /// </summary>
    public Dictionary<int, List<TimedPoseData>> poseData = new Dictionary<int, List<TimedPoseData>>();

    /// <summary>
    /// Types of tracked devices.
    /// </summary>
    public enum Devices
    {
        RightController,
        LeftController,
        ViveTracker,
        Hmd,
    };


    /// <summary>
    /// Type of trackable device that should be tracked.
    /// </summary>
    [Tooltip("Type of trackable device that should be tracked.")]
    public Devices deviceToTrack;

    /// <summary>
    /// Latency in milliseconds to be applied on the movement of this tracked object, so that virtual controllers don't
    /// move ahead of their real-world image.
    /// </summary>
    [Tooltip("Latency in milliseconds to be applied on the movement of this tracked object, so that virtual controllers don't" +
        " move ahead of their real-world image.")]
    [Range(0, 200)]
    public int latencyCompensation = 78;

    /// <summary>
    /// If true, and this is a controller, will offset controller's position by the difference between
    /// the VR headset and the ZED's tracked position. This keeps controller's position relative to the ZED.
    /// </summary>
    [Tooltip("If true, and this is a controller, will offset controller's position by the difference between " +
        "the VR headset and the ZED's tracked position. This keeps controller's position relative to the ZED. ")]
    [LabelOverride("Enable Controller Drift Fix")]
    public bool correctControllerDrift = true;

    /// <summary>
    /// The Serial number of the controller/tracker to be tracked.
    /// If specified, it will override the device returned using the 'Device to Track' selection.
    /// Useful for forcing a specific device to be tracked, instead of the first left/right/Tracker object found.
    /// If Null, then there's no calibration to be applied to this script.
    /// If NONE, the ZEDControllerOffset failed to find any calibration file.
    /// If S/N is present, then this script will calibrate itself to track the correct device, if that's not the case already.
    /// Note that ZEDOffsetController will load this number from a GreenScreen calibration file, if present.
    /// </summary>
    [Tooltip("The Serial number of the controller/tracker to be tracked." +
        " If specified, overrides the 'Device to Track' selection.")]
    public string SNHolder = "";

    /// <summary>
    /// Cached transform that represents the ZED's head, retrieved from ZEDManager.GetZedRootTransform().
    /// Used to find the offset between the HMD and tracked transform to compensate for drift.
    /// </summary>
    protected Transform zedRigRoot;

    /// <summary>
    /// Reference to the scene's ZEDManager component. Used for compensating for headset drift when this is on a controller.
    /// </summary>
    [Tooltip("Reference to the scene's ZEDManager component. Used for compensating for headset drift when this is on a controller. " +
        "If left blank, will be set to the first available ZEDManager.")]
    public ZEDManager zedManager = null;

    /// <summary>
    /// Sets up the timed pose dictionary and identifies the VR SDK being used.
    /// </summary>
    protected virtual void Awake()
    {
        poseData.Clear(); //Reset the dictionary.
        poseData.Add(1, new List<TimedPoseData>()); //Create the list within the dictionary with its key and value.
        //Looking for the loaded device
        loadedDevice = XRSettings.loadedDeviceName;
        
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
            //If there are multiple cameras in a scene, this arbitrary assignment could be bad. Warn the user.
            if (ZEDManager.GetInstances().Count > 1)
            {
                //Using Log instead of LogWarning because most users don't enable warnings but this is actually important.
                Debug.Log("Warning: ZEDController automatically set itself to first available ZED (" + zedManager.cameraID + ") because zedManager " +
                    "value wasn't set, but there are multiple ZEDManagers in the scene. Assign a reference directly to ensure no unexpected behavior.");
            }
        }
        if (zedManager) zedRigRoot = zedManager.GetZedRootTansform();
    }

    /// <summary>
    /// Update is called every frame.
    /// For SteamVR plugin this is where the device Index is set up.
    /// For Oculus plugin this is where the tracking is done.
    /// </summary>
    protected virtual void Update()
    {
        //Check if the VR headset is connected.
        if (ZEDSupportFunctions.hasXRDevice())
        {
                //Depending on which tracked device we are looking for, start tracking it.
                if (deviceToTrack == Devices.LeftController)//Track the Left Controller.
                {
                    InputDeviceCharacteristics leftTrackedControllerFilter = InputDeviceCharacteristics.Left;
                    List<InputDevice> foundLeftControllers = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(leftTrackedControllerFilter, foundLeftControllers);
                    if (foundLeftControllers.Count > 0)
                    {
                        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                        leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftHandPosition);
                        leftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftHandRotation);
                        RegisterPosition(1, leftHandPosition, leftHandRotation);

                }
                    else
                    {
                        //Debug.LogError("Left Controller is not found.");
                        return;
                    }
                }
                if (deviceToTrack == Devices.RightController)//Track the Right Controller.
                {


                    InputDeviceCharacteristics rightTrackedControllerFilter = InputDeviceCharacteristics.Right;
                    List<InputDevice> foundRightControllers = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(rightTrackedControllerFilter, foundRightControllers);

                    if (foundRightControllers.Count > 0)
                    {
                        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                        rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightHandPosition);
                        rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightHandRotation);
                        RegisterPosition(1, rightHandPosition, rightHandRotation);
                    }
                    else
                    {
                        //Debug.LogError("Right Controller is not found.");
                        return;
                    }
                }
                if (deviceToTrack == Devices.Hmd) //Track the Hmd.
                {
                    List<InputDevice> foundHeadControllers = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, foundHeadControllers);

                    if (foundHeadControllers.Count > 0)
                    {
                        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                        head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
                        head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);
                        RegisterPosition(1, headPosition, headRotation);
                    }
                    else
                    {
                        //Debug.LogError("HMD is not found.");
                        return;
                    }
                }
                //Use our saved positions to apply a delay before assigning it to this object's Transform.
                if (poseData.Count > 0)
                {
                    sl.Pose p;

                    //Delay the saved values inside GetValuePosition() by a factor of latencyCompensation in milliseconds.
                    p = GetValuePosition(1, (float)(latencyCompensation / 1000.0f));
                    transform.position = p.translation; //Assign new delayed Position
                    transform.rotation = p.rotation; //Assign new delayed Rotation.
                }
        }
    }

    /// <summary>
    /// Compute the delayed position and rotation from the history stored in the poseData dictionary.
    /// </summary>
    /// <param name="keyindex"></param>
    /// <param name="timeDelay"></param>
    /// <returns></returns>
    private sl.Pose GetValuePosition(int keyindex, float timeDelay)
    {
        sl.Pose p = new sl.Pose();
        if (poseData.ContainsKey(keyindex))
        {
            //Get the saved position & rotation.
            p.translation = poseData[keyindex][poseData[keyindex].Count - 1].position;
            p.rotation = poseData[keyindex][poseData[keyindex].Count - 1].rotation;

            float idealTS = (Time.time - timeDelay);

            for (int i = 0; i < poseData[keyindex].Count; ++i)
            {
                if (poseData[keyindex][i].timestamp > idealTS)
                {
                    int currentIndex = i;
                    if (currentIndex > 0)
                    {
                        //Calculate the time between the pose and the delayed pose.
                        float timeBetween = poseData[keyindex][currentIndex].timestamp - poseData[keyindex][currentIndex - 1].timestamp;
                        float alpha = ((Time.time - poseData[keyindex][currentIndex - 1].timestamp) - timeDelay) / timeBetween;

                        //Lerp to the next position based on the time determined above.
                        Vector3 pos = Vector3.Lerp(poseData[keyindex][currentIndex - 1].position, poseData[keyindex][currentIndex].position, alpha);
                        Quaternion rot = Quaternion.Lerp(poseData[keyindex][currentIndex - 1].rotation, poseData[keyindex][currentIndex].rotation, alpha);

                        //Apply new values.
                        p = new sl.Pose
                        {
                            translation = pos,
                            rotation = rot
                        };

                        //Add drift correction, but only if the user hasn't disabled it, it's on an actual controller, and the zedRigRoot position won't be affected.
                        if (correctControllerDrift == true &&
                            (deviceToTrack == Devices.LeftController || deviceToTrack == Devices.RightController || deviceToTrack == Devices.ViveTracker) &&
                            (zedManager != null && zedManager.IsStereoRig == true && !zedManager.transform.IsChildOf(transform)))
                        {
                            //Compensate for positional drift by measuring the distance between HMD and ZED rig root (the head's center). 
                            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                            head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
                            head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);
                            Vector3 zedhmdposoffset = zedRigRoot.position - headPosition;
                            Quaternion zedhmdrotoffset = zedRigRoot.rotation * Quaternion.Inverse(headRotation);

                            p.translation += zedhmdposoffset;
                            p.rotation *= zedhmdrotoffset;
                            //Debug.Log(zedRigRoot.rotation.eulerAngles);
                            //Debug.Log(headRotation.eulerAngles);

                        }
                        //Removes used elements from the dictionary.
                        poseData[keyindex].RemoveRange(0, currentIndex - 1);
                    }
                    return p;
                }
            }
        }
        return p;
    }

    /// <summary>
    /// Set the current tracking to a container (TimedPoseData) to be stored in poseData and retrieved/applied after the latency period.
    /// </summary>
    /// <param name="index">Key value in the dictionary.</param>
    /// <param name="position">Tracked object's position from the VR SDK.</param>
    /// <param name="rot">Tracked object's rotation from the VR SDK.</param>
    private void RegisterPosition(int keyindex, Vector3 position, Quaternion rot)
    {
        TimedPoseData currentPoseData = new TimedPoseData();
        currentPoseData.timestamp = Time.time;
        currentPoseData.rotation = rot;
        currentPoseData.position = position;

        poseData[keyindex].Add(currentPoseData);

    }

    /// <summary>
    /// Structure used to hold the pose of a controller at a given timestamp.
    /// This is stored in poseData with RegisterPosition() each time the VR SDK makes poses available.
    /// It's retrieved with GetValuePosition() in Update() each frame.
    /// </summary>
    public struct TimedPoseData
    {
        /// <summary>
        /// Value from Time.time when the pose was collected.
        /// </summary>
        public float timestamp;

        /// <summary>
        /// Rotation of the tracked object as provided by the VR SDK.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// Position of the tracked object as provided by the VR SDK.
        /// </summary>
        public Vector3 position;
    }
}
