using UnityEngine;
using System.Collections.Generic;
#if ZED_STEAM_VR
using Valve.VR;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ZEDControllerTracker : MonoBehaviour
{
	private string loadedDevice = "";

#if ZED_STEAM_VR
    public enum EIndex
    {
        None = -1,
        Hmd = (int)OpenVR.k_unTrackedDeviceIndex_Hmd,
        Device1,
        Device2,
        Device3,
        Device4,
        Device5,
        Device6,
        Device7,
        Device8,
        Device9,
        Device10,
        Device11,
        Device12,
        Device13,
        Device14,
        Device15
    }
    [HideInInspector]
    public EIndex index = EIndex.None;

    /// <summary>
    /// Timers between each checks, to registers new pads
    /// </summary>
    private float timerVive = 0.0f;
    private float timerMaxVive = 1.0f;
    private devices oldDevice;
#endif

    /// <summary>
    /// Per each ID, contains the list of the position of each controller, used to delayed their tracking
    /// </summary>
    public Dictionary<int, List<TimedPoseData>> poseData = new Dictionary<int, List<TimedPoseData>>();
    
    public enum devices
    {
        RightController,
        LeftController,

#if ZED_STEAM_VR
        ViveTracker,
#endif
        Hmd,
    };
    [Tooltip("List of trackable devices that can be selected.")]
    public devices deviceToTrack;

    [Tooltip("Latency to be applied on the movement of this tracked object, to match the camera frequency.")]
    [Range(0, 200)]
    public int _latencyCompensation = 78;

    /// <summary>
    /// The Serial number of the controller which is holding the ZED.
    /// If Null, then there's no calibration to be applied to this script.
    /// If NONE, the ZEDControllerOffset failed to find any calibration file.
    /// If S/N is present, then this script will calibrate itself to track the correct device, if that's not the case already.
    /// </summary>
    public string SNHolder = "";

    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    void Awake()
    {
        //Reseting the dictionary.
        poseData.Clear();
        //Creating our element with its key and value.
        poseData.Add(1, new List<TimedPoseData>());
        //Looking for the loaded device
        loadedDevice = UnityEngine.VR.VRSettings.loadedDeviceName;
    }

    /// <summary>
    /// Update is called every frame.
    /// For SteamVR plugin this is where the device Index is set up.
    /// For Oculus plugin this is where the tracking is done.
    /// </summary>
    void Update()
    {

#if ZED_OCULUS
        //Check if the VR headset is connected.
        if (OVRManager.isHmdPresent && loadedDevice == "Oculus")
        {
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
            {
                //Depending on which tracked device we are looking for, start tracking it.
                if (deviceToTrack == devices.LeftController) //Track the Left Oculus Controller.
                    RegisterPosition(1, OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch));
                if (deviceToTrack == devices.RightController) //Track the Right Oculus Controller.
                    RegisterPosition(1, OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));

                if (deviceToTrack == devices.Hmd) //Track the Oculus Hmd.
                    RegisterPosition(1, UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.CenterEye), UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.CenterEye));

                //Taking our saved positions, and applying a delay before assigning it to our Transform.
                if (poseData.Count > 0)
                {
                    sl.Pose p;
                    //Delaying the saved values inside GetValuePosition() by a factor of latencyCompensation to the millisecond.
                    p = GetValuePosition(1, (float)(_latencyCompensation / 1000.0f));
                    transform.position = p.translation; //Assign new delayed Position
                    transform.rotation = p.rotation; //Assign new delayed Rotation.
                }
            }
        }
        //Enabling the updates of the internal state of OVRInput.
        OVRInput.Update();
#endif
#if ZED_STEAM_VR

        //Timer for checking on devices
        timerVive += Time.deltaTime;

        if (timerVive <= timerMaxVive)
            return;

        timerVive = 0f;

        //Checks if a device has been assigned
        if (index == EIndex.None && loadedDevice == "OpenVR")
        {
            if (BIsManufacturerController("HTC") || BIsManufacturerController("Oculus"))
            {
                //We look for any device that has "tracker" in its 3D model mesh name.
                //We're doing this since the device ID changes based on how many devices are connected to Steam VR.
                //This way if there's no controllers or just one, it's going to get the right ID for the Tracker.
                if (deviceToTrack == devices.ViveTracker)
                {
                    var error = ETrackedPropertyError.TrackedProp_Success;
                    for (uint i = 0; i < 16; i++)
                    {
                        var result = new System.Text.StringBuilder((int)64);
                        OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);
                        if (result.ToString().Contains("tracker"))
                        {
                            index = (EIndex)i;
                            break; //We break out of the loop, but we can use this to set up multiple Vive Trackers if we want to.
                        }
                    }
                }

                //Looks for a device with the role of a Right Hand.
                if (deviceToTrack == devices.RightController)
                {
                    index = (EIndex)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
                }
                //Looks for a device with the role of a Left Hand.
                if (deviceToTrack == devices.LeftController)
                {
                    index = (EIndex)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
                }

                //Assigns the Hmd
                if (deviceToTrack == devices.Hmd)
                {
                    index = EIndex.Hmd;
                }
            }

            //Display Warning if there was supposed to be a calibration file, and none was found.
            if (SNHolder.Equals("NONE"))
            {
                Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.PAD_CAMERA_CALIBRATION_NOT_FOUND));
            }
            else if (SNHolder != null && index != EIndex.None)
            {
                //If the Serial number of the Calibrated device isn't the same as the current tracked device by this script...
                if (!SteamVR.instance.GetStringProperty(Valve.VR.ETrackedDeviceProperty.Prop_SerialNumber_String, (uint)index).Contains(SNHolder))
                {
                    Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.PAD_CAMERA_CALIBRATION_MISMATCH) + SNHolder);
                    //... then look for that device through all the connected devices.
                    for (int i = 0; i < 16; i++)
                    {
                        //If a device with the same Serial Number is found, then change the device to track of this script.
                        if(SteamVR.instance.GetStringProperty(Valve.VR.ETrackedDeviceProperty.Prop_SerialNumber_String, (uint)i).Contains(SNHolder))
                        {
                            index = (EIndex)i;
                            string deviceRole = OpenVR.System.GetControllerRoleForTrackedDeviceIndex((uint)index).ToString();
                            if (deviceRole.Equals("RightHand"))
                                deviceToTrack = devices.RightController;
                            else if (deviceRole.Equals("LeftHand"))
                                deviceToTrack = devices.LeftController;
                            else if (deviceRole.Equals("Invalid"))
                            {
                                var error = ETrackedPropertyError.TrackedProp_Success;
                                var result = new System.Text.StringBuilder((int)64);
                                OpenVR.System.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);
                                if (result.ToString().Contains("tracker"))
                                    deviceToTrack = devices.ViveTracker;
                            }
                            Debug.Log("A connected device with the correct Serial Number was found, and assigned to " + this + " the correct device to track.");
                            break;
                        }
                    }
                }
            }
            oldDevice = deviceToTrack;
        }

        if (deviceToTrack != oldDevice)
            index = EIndex.None;

#endif
    }

#if ZED_STEAM_VR
    public bool isValid { get; private set; }
    /// <summary>
    /// Tracking the devices for SteamVR and applying a delay.
    /// <summary>
    private void OnNewPoses(TrackedDevicePose_t[] poses)
    {
        if (index == EIndex.None)
            return;

        var i = (int)index;

        isValid = false;

        if (poses.Length <= i)
            return;

        if (!poses[i].bDeviceIsConnected)
            return;

        if (!poses[i].bPoseIsValid)
            return;

        isValid = true;

        //Get the position and rotation of our tracked device.
        var pose = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);
        //Saving those values.
        RegisterPosition(1, pose.pos, pose.rot);
        //Delaying the saved values inside GetValuePosition() by a factor of latencyCompensation to the millisecond.
        sl.Pose p = GetValuePosition(1, (float)(_latencyCompensation / 1000.0f));
        transform.localPosition = p.translation;
        transform.localRotation = p.rotation;
        
    }

    SteamVR_Events.Action newPosesAction;

    ZEDControllerTracker()
    {
        newPosesAction = SteamVR_Events.NewPosesAction(OnNewPoses);
    }

    void OnEnable()
    {
        var render = SteamVR_Render.instance;
        if (render == null)
        {
            enabled = false;
            return;
        }

        newPosesAction.enabled = true;
    }

    void OnDisable()
    {
        newPosesAction.enabled = false;
        isValid = false;
    }

    public bool BIsManufacturerController(string name)
    {
        System.Text.StringBuilder sbType = new System.Text.StringBuilder(1000);
        Valve.VR.ETrackedPropertyError err = Valve.VR.ETrackedPropertyError.TrackedProp_Success;
        SteamVR.instance.hmd.GetStringTrackedDeviceProperty((uint)0, Valve.VR.ETrackedDeviceProperty.Prop_ManufacturerName_String, sbType, 1000, ref err);
        return (err == Valve.VR.ETrackedPropertyError.TrackedProp_Success && sbType.ToString().StartsWith(name));
    }


#endif
    /// <summary>
    /// Compute the delayed position and rotation from history
    /// </summary>
    /// <param name="indx"></param>
    /// <param name="timeDelay"></param>
    /// <returns></returns>
    private sl.Pose GetValuePosition(int indx, float timeDelay)
    {
        sl.Pose p = new sl.Pose();
        if (poseData.ContainsKey(indx))
        {
            //Get the saved position & rotation.
            p.translation = poseData[indx][poseData[indx].Count - 1].position;
            p.rotation = poseData[indx][poseData[indx].Count - 1].rotation;

            float idealTS = (Time.time - timeDelay);

            for (int i = 0; i < poseData[indx].Count; ++i)
            {
                if (poseData[indx][i].timestamp > idealTS)
                {
                    int currentIndex = i;
                    if (currentIndex > 0)
                    {
                        //Calculate the time between the pose and the delayed pose.
                        float timeBetween = poseData[indx][currentIndex].timestamp - poseData[indx][currentIndex - 1].timestamp;
                        float alpha = ((Time.time - poseData[indx][currentIndex - 1].timestamp) - timeDelay) / timeBetween;
                        //Lerping to the next position based on the time determied above.
                        Vector3 pos = Vector3.Lerp(poseData[indx][currentIndex - 1].position, poseData[indx][currentIndex].position, alpha);
                        Quaternion rot = Quaternion.Lerp(poseData[indx][currentIndex - 1].rotation, poseData[indx][currentIndex].rotation, alpha);
                        //Applies new values
                        p = new sl.Pose();
                        p.translation = pos;
                        p.rotation = rot;
                        //Removes used elements from dictionary.
                        poseData[indx].RemoveRange(0, currentIndex - 1);
                    }
                    return p;
                }
            }
        }
        return p;
    }

    /// <summary>
    /// Set the current tracking to a container
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    /// <param name="rot"></param>
    private void RegisterPosition(int indx, Vector3 position, Quaternion rot)
    {
        TimedPoseData currentPoseData = new TimedPoseData();
        currentPoseData.timestamp = Time.time;
        currentPoseData.rotation = rot;
        currentPoseData.position = position;
        poseData[indx].Add(currentPoseData);
    }

    /// <summary>
    /// Structures used to delay the controllers
    /// </summary>
    public struct TimedPoseData
    {
        public float timestamp;
        public Quaternion rotation;
        public Vector3 position;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ZEDControllerTracker)), CanEditMultipleObjects]
public class ZEDVRDependencies : Editor
{
    [SerializeField]
    static string defineName;
    static string packageName;

    public override void OnInspectorGUI()
    {
        if (CheckPackageExists("SteamVR"))
        {
            defineName = "ZED_STEAM_VR";
            packageName = "SteamVR";
        }
        else if (CheckPackageExists("Oculus") || CheckPackageExists("OVR"))
        {
            defineName = "ZED_OCULUS";
            packageName = "Oculus";
        }

        if (EditorPrefs.GetBool(packageName))
        {
            DrawDefaultInspector();
        }
        else
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Load " + packageName + " data"))
            {
                if (CheckPackageExists(packageName))
                {
                    ActivateDefine();
                }
            }
            if (packageName == "SteamVR")
                EditorGUILayout.HelpBox(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.STEAMVR_NOT_INSTALLED), MessageType.Warning);
            else if (packageName == "Oculus")
                EditorGUILayout.HelpBox(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.OVR_NOT_INSTALLED), MessageType.Warning);
        }
    }

    public static bool CheckPackageExists(string name)
    {
        string[] packages = AssetDatabase.FindAssets(name);
        return packages.Length != 0 && AssetDatabase.IsValidFolder("Assets/" + name);
    }


    public static void ActivateDefine()
    {
        EditorPrefs.SetBool(packageName, true);

        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (defines.Length != 0)
        {
            if (!defines.Contains(defineName))
            {
                defines += ";" + defineName;
            }
        }
        else
        {
            if (!defines.Contains(defineName))
            {
                defines += defineName;
            }
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
    }


    public static void DesactivateDefine()
    {
        EditorPrefs.SetBool(packageName, false);

        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (defines.Length != 0)
        {
            if (defines.Contains(defineName))
            {
                defines = defines.Remove(defines.IndexOf(defineName), defineName.Length);

                if (defines.LastIndexOf(";") == defines.Length - 1 && defines.Length != 0)
                {
                    defines.Remove(defines.LastIndexOf(";"), 1);
                }
            }
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
    }

    public class AssetPostProcessZEDVR : AssetPostprocessor
    {

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            if (ZEDVRDependencies.CheckPackageExists("OVR") || ZEDVRDependencies.CheckPackageExists("Oculus"))
            {
                defineName = "ZED_OCULUS";
                packageName = "Oculus";
                ActivateDefine();
            }
            if (ZEDVRDependencies.CheckPackageExists("SteamVR"))
            {
                defineName = "ZED_STEAM_VR";
                packageName = "SteamVR";
                ActivateDefine();
            }
            else
            {
                DesactivateDefine();
            }
        }
    }
}

#endif