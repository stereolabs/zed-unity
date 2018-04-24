//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if ZED_STEAM_VR
using Valve.VR;
#elif ZED_OVR

#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controls the SteamVR controllers and delays their tracking
/// </summary>
public class ZEDSteamVRControllerManager : MonoBehaviour, ZEDControllerManager
{
	enum HMD
	{
		OCULUS,
		STEAMVR,
		NONE
	}

	/**** Structures used to delay the pads*****/
	public struct TimedPoseData
	{
		public float timestamp;
		public Quaternion rotation;
		public Vector3 position;
	}
	#if !ZED_STEAM_VR
	[HideInInspector]
	[Tooltip("Controller ID holding the ZED")]
	public int controllerIndexZEDHolder = -1;
	public int ControllerIndexZEDHolder { get { return controllerIndexZEDHolder; } }
	public string SNHolder = "";
	public void LoadIndex(string path)
	{
	if (!System.IO.File.Exists(path))
	{
	SNHolder = "NONE";
	return;
	}

	string[] lines = null;
	try
	{
	lines = System.IO.File.ReadAllLines(path);
	}
	catch (System.Exception)
	{
	SNHolder = "NONE";
	}
	if (lines == null)
	{
	SNHolder = "NONE";
	return;
	}

	foreach (string line in lines)
	{
	string[] splittedLine = line.Split('=');
	if (splittedLine.Length >= 2)
	{
	string key = splittedLine[0];
	string field = splittedLine[1].Split(' ')[0];

	if (key == "indexController")
	{
	SNHolder = field;
	}
	}
	}
	}
	#endif
	#if ZED_STEAM_VR
	private int previousStateIndex = -1;
	#endif
    /// <summary>
    /// Callback when the Controller ID is set
    /// </summary>
	public delegate void ZEDOnPadIndexSetAction();
	public static event ZEDOnPadIndexSetAction ZEDOnPadIndexSet;

    /// <summary>
    /// Flag if the controllers are init
    /// </summary>
	private bool padsAreInit = false;

    /// <summary>
    /// Checks if the controllers are init, if returned false, a controller is missing
    /// </summary>
	public bool PadsAreInit
	{
		get { return padsAreInit; }
	}
    /// <summary>
    /// Structure to store the position and rotation of a tracked controller
    /// </summary>
	public struct Pose
	{
		public Vector3 pose;
		public Quaternion rot;
	}
	#if ZED_STEAM_VR
    /// <summary>
    /// Structure to store a reference to the delayed controller
    /// </summary>
	public struct DelayedPad
	{
		public GameObject o;
		public SteamVR_Controller.Device d;
	}
    /// <summary>
    /// List of the delayed controllers, stored by their IDS
    /// </summary>
	[HideInInspector]
	public Dictionary<int, DelayedPad> delayedPads = new Dictionary<int, DelayedPad>();

	/// <summary>
    /// The controller manager of SteamVR
    /// </summary>
	private SteamVR_ControllerManager controllerManager;

    /// <summary>
    /// List of all the tracked controller of steamVR
    /// </summary>
	[HideInInspector]
	public List<SteamVR_TrackedController> controllers = new List<SteamVR_TrackedController>();

    /// <summary>
    /// List of the gameObjects controlled by SteamVR
    /// </summary>
	[HideInInspector]
	public List<GameObject> controllersGameObject = new List<GameObject>();

    /// <summary>
    /// List of the devices controlled by SteamVR
    /// </summary>
	[HideInInspector]
	public List<SteamVR_Controller.Device> devices = new List<SteamVR_Controller.Device>();

    /// <summary>
    /// Reference to the controller of SteamVR
    /// </summary>
	[HideInInspector]
	public GameObject controllerObject;

    /// <summary>
    /// Per each ID, contains the list of the position of each controller, used to delayed their tracking
    /// </summary>
	private Dictionary<int, List<TimedPoseData>> poseData = new Dictionary<int, List<TimedPoseData>>();

    /// <summary>
    /// Current controller ID which is holding the ZED
    /// </summary>
	[HideInInspector]
	[Tooltip("Controller ID holding the ZED")]
	public int controllerIndexZEDHolder = -1;

    /// <summary>
    /// The Serial number of the controller which is holding the ZED. If no controllers are holding the ZED, set it to NONE
    /// </summary>
	public string SNHolder = "";

	/// <summary>
    /// Latency compensation
    /// </summary>
	[Range(0, 200)]
	public int latencyCompensation = 78;

    /// <summary>
    /// Timers between each checks, to registers new pads
    /// </summary>
	private float timerVive = 0.0f;
	private float timerMaxVive = 1.0f;

    /// <summary>
    /// Returns the current ID of the controller holding the ZED
    /// </summary>
	public int ControllerIndexZEDHolder { get { return controllerIndexZEDHolder; } }

    /// <summary>
    /// Event received from SteamVR when new poses are available
    /// </summary>
	private SteamVR_Events.Action newPoses;

    /// <summary>
    /// Flag checking if the controllers are ready
    /// </summary>
	[HideInInspector]
	public bool padsSet = false;

    /// <summary>
    /// Reference to the cameraRig of SteamVR
    /// </summary>
	[HideInInspector]
	public GameObject cameraRig = null;

    /// <summary>
    /// Model to replace the standard shapes of the controllers
    /// </summary>
	public GameObject modelToReplaceVivePads = null;

    /// <summary>
    /// Checks if the holder of the ZED is ready
    /// </summary>
	private bool ZEDControllerSet = false;

    /// <summary>
    /// Current HMD model, either Oculus or SteamVR
    /// </summary>
	private HMD model = HMD.NONE;
	private void Awake()
	{
		cameraRig = GameObject.Find("[CameraRig]");
		if (cameraRig == null)
		{
			Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.VR_CAMERA_RIG_NOT_FOUND));
			controllerManager = FindObjectOfType(typeof(SteamVR_ControllerManager)) as SteamVR_ControllerManager;
			if (controllerManager == null)
			{
				Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.VR_CAMERA_RIG_CONTROLLER_NOT_FOUND));
			}

		}

	}

    /// <summary>
    /// Initialized all the components
    /// </summary>
	void Start()
	{
		newPoses = SteamVR_Events.NewPosesAction(OnNewPoses);
		newPoses.enabled = true;

		RegisterExistingPads();
		HideGameObjectFromZED();

		if (UnityEngine.VR.VRDevice.model.Contains("Oculus"))
		{
			model = HMD.OCULUS;
		}
		else
		{
			model = HMD.STEAMVR;
		}
	}


    /// <summary>
    /// Load the Serail number or set it to NONE if no file is found
    /// </summary>
    /// <param name="path"></param>
	public void LoadIndex(string path)
	{
		if (SNHolder.Equals("NONE")) return;

		if (!System.IO.File.Exists(path))
		{
			SNHolder = "NONE";
			return;
		}

		string[] lines = null;
		try
		{
			lines = System.IO.File.ReadAllLines(path);
		}
		catch (System.Exception)
		{
			SNHolder = "NONE";
		}
		if (lines == null)
		{
			SNHolder = "NONE";
			return;
		}

		foreach (string line in lines)
		{
			string[] splittedLine = line.Split('=');
			if (splittedLine.Length >= 2)
			{
				string key = splittedLine[0];
				string field = splittedLine[1].Split(' ')[0];

				if (key == "indexController")
				{
					SNHolder = field;
				}
			}
		}
	}


	/**********************************/

	//To each new poses from the VIVE, the pos is registered and a delay is applied
	private void OnNewPoses(TrackedDevicePose_t[] poses)
	{
		foreach (KeyValuePair<int, DelayedPad> pad in delayedPads)
		{
			var i = (int)pad.Key;

			if (i == -1)
				return;

			if (poses.Length <= i)
				return;

			if (!poses[i].bDeviceIsConnected)
				return;

			if (!poses[i].bPoseIsValid)
				return;

			var pose = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);

			RegisterPosition(i, pose.pos, pose.rot);
			Pose p = GetValuePosition(i, (float)(latencyCompensation / 1000.0f));
			DelayedPad delayedPad;
			if (delayedPads.TryGetValue(i, out delayedPad))
			{
				delayedPads[i].o.transform.position = cameraRig.transform.TransformPoint(p.pose);
				delayedPads[i].o.transform.rotation = cameraRig.transform.rotation * p.rot;
			}

			if (i == controllerIndexZEDHolder)
			{
				transform.parent.position = delayedPads[i].o.transform.position;
				transform.parent.rotation = delayedPads[i].o.transform.rotation;

			}
			if (controllerIndexZEDHolder == -2)
			{
				transform.parent.position = cameraRig.transform.position;
				transform.parent.rotation = cameraRig.transform.rotation;
			}
		}
	}

	//Register the existing pads, the pads registered will be checked if their IDs are correct
	void RegisterExistingPads()
	{
		if (cameraRig != null)
		{
			controllerManager = cameraRig.transform.GetComponent<SteamVR_ControllerManager>();
		}
		if (controllerManager == null) return;
		controllerManager.Refresh();
		controllersGameObject.Clear();
		devices.Clear();
		controllersGameObject.Add(controllerManager.left);
		controllersGameObject.Add(controllerManager.right);
		foreach (GameObject o in controllerManager.objects)
		{
			if (!controllersGameObject.Contains(o))
			{
				controllersGameObject.Add(o);
			}
		}


	}

	void OnApplicationQuit()
	{
		if (newPoses != null)
		{
			newPoses.enabled = false;
		}
	}

	public bool GetPadIndex()
	{
		foreach (GameObject c in controllersGameObject)
		{
			if (CheckIndexExist(c))
			{
				devices.Add(SteamVR_Controller.Input((int)c.GetComponent<SteamVR_TrackedObject>().index));
			}
		}
		return devices.Count >= 2;
	}

	//The new pads are registered and created. The preview is also created
	public void CreateNewPads()
	{
		poseData.Clear();
		foreach (DelayedPad o in delayedPads.Values)
		{
			Destroy(o.o);
		}
		delayedPads.Clear();

		foreach (SteamVR_Controller.Device d in devices)
		{
			if (poseData.ContainsKey((int)d.index))
				continue;
			DelayedPad p = new DelayedPad();
			if (modelToReplaceVivePads == null)
			{
				p.o = new GameObject();
				p.o.name = "VirtualDelayedPad_" + (int)d.index;
				p.o.layer = sl.ZEDCamera.Tag;
				SteamVR_RenderModel m = p.o.AddComponent<SteamVR_RenderModel>();
				m.modelOverride = "vr_controller_vive_1_5";

				m.SetDeviceIndex((int)d.index);
			}
			else
			{
				p.o = Instantiate(modelToReplaceVivePads);
			}
			p.d = d;

			poseData.Add((int)d.index, new List<TimedPoseData>());
			delayedPads.Add((int)d.index, p);
		}
	}

	// If the index is tracked, not an hmd and not already registered => true
	private bool CheckIndexExist(GameObject c)
	{
		SteamVR_TrackedObject tracked = c.GetComponent<SteamVR_TrackedObject>();
		return tracked != null && (int)tracked.index != -1 && (int)tracked.index != 0 && !devices.Contains(SteamVR_Controller.Input((int)tracked.index));
	}

	public bool SetPads(bool noCameraTracking)
	{

		bool controllerFound = false;
		uint leftIndex = OpenVR.k_unTrackedDeviceIndexInvalid, rightIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
		var system = OpenVR.System;

		if (system != null)
		{
			leftIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			rightIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
		}

		int index = 0;
		if (devices.Count == 0) return false;
		if (!noCameraTracking)
		{
			foreach (SteamVR_Controller.Device d in devices)
			{
				if ((d.index != OpenVR.k_unTrackedDeviceIndexInvalid && d.index != leftIndex && d.index != rightIndex && leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
					|| d.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
				{
					controllerIndexZEDHolder = (int)d.index;
					ZEDControllerSet = true;
					controllerFound = true;
					break;
				}
			}
		}
		else if (noCameraTracking)
		{
			ZEDControllerSet = true;
			controllerFound = true;
			controllerIndexZEDHolder = -2;
		}


		foreach (SteamVR_Controller.Device d in devices)
		{

			if (ZEDControllerSet && controllerIndexZEDHolder != (int)d.index && d.valid)
			{
				while (index < controllersGameObject.Count && !controllersGameObject[index].activeInHierarchy)
				{
					index++;
				}
				SteamVR_TrackedController c = controllersGameObject[index].GetComponent<SteamVR_TrackedController>();
				if (c == null)
				{
					c = controllersGameObject[index].AddComponent<SteamVR_TrackedController>();
				}
				if (controllerObject == null)
				{
					controllerObject = controllersGameObject[index];

				}
				c.SetDeviceIndex((int)d.index);
				controllers.Add(c);


				padsSet = true;

			}
			index++;
		}

		return controllerFound;
	}
	private const int layerToHide = 10;

	//Hides the Camera rig (mostly pads) from the ZED
	public void HideGameObjectFromZED()
	{
		Camera zedCamera = transform.GetChild(0).GetComponent<Camera>();

		if (zedCamera != null && cameraRig != null)
		{
			zedCamera.cullingMask &= ~(1 << layerToHide);
			SetLayerRecursively(cameraRig, layerToHide);
		}
	}

    /// <summary>
    /// Set the layer recursively
    /// </summary>
    /// <param name="go"></param>
    /// <param name="layerNumber"></param>
	public static void SetLayerRecursively(GameObject go, int layerNumber)
	{
		if (go == null) return;
		foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
		{
			trans.gameObject.layer = layerNumber;
		}
	}



	//Compute the delayed position and rotation from history
	private Pose GetValuePosition(int index, float timeDelay)
	{
		Pose p = new Pose();
		p.pose = poseData[index][poseData[index].Count - 1].position;
		p.rot = poseData[index][poseData[index].Count - 1].rotation;

		float idealTS = (Time.time - timeDelay);

		for (int i = 0; i < poseData[index].Count; ++i)
		{
			if (poseData[index][i].timestamp > idealTS)
			{
				int currentIndex = i;
				if (currentIndex > 0)
				{
					float timeBetween = poseData[index][currentIndex].timestamp - poseData[index][currentIndex - 1].timestamp;
					float alpha = ((Time.time - poseData[index][currentIndex - 1].timestamp) - timeDelay) / timeBetween;

					Vector3 pos = Vector3.Lerp(poseData[index][currentIndex - 1].position, poseData[index][currentIndex].position, alpha);
					Quaternion rot = Quaternion.Lerp(poseData[index][currentIndex - 1].rotation, poseData[index][currentIndex].rotation, alpha);

					p = new Pose();
					p.pose = pos;
					p.rot = rot;
					poseData[index].RemoveRange(0, currentIndex - 1);
				}
				return p;
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
	private void RegisterPosition(int index, Vector3 position, Quaternion rot)
	{
		TimedPoseData currentPoseData = new TimedPoseData();
		currentPoseData.timestamp = Time.time;
		currentPoseData.rotation = rot;
		currentPoseData.position = position;

		poseData[index].Add(currentPoseData);
	}

	private void Update()
	{
		timerVive += Time.deltaTime;
		if (timerVive > timerMaxVive)
		{
			timerVive = 0;
			if (!padsSet)
			{
				GetPadIndex();
			}

			if (devices.Count != delayedPads.Count)
			{
				CreateNewPads();
			}

			if (devices.Count >= 2) 
				padsAreInit = true;

            //if (devices.Count < 2)
            //{
            //    Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.NOT_ENOUGH_PAD_DETECTED) + devices.Count);
            //}


			if (controllerIndexZEDHolder == -1)
			{
				if (SNHolder.Equals("NONE"))
				{
					Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.PAD_CAMERA_CALIBRATION_NOT_FOUND));
					controllerIndexZEDHolder = -2;
					padsAreInit = true;
				}
				else
				{
					foreach (SteamVR_Controller.Device d in devices)
					{
						if (SteamVR.instance.GetStringProperty(Valve.VR.ETrackedDeviceProperty.Prop_SerialNumber_String, (uint)d.index).Equals(SNHolder))
						{
							controllerIndexZEDHolder = (int)d.index;
							padsAreInit = true;
						}
					}

					if (controllerIndexZEDHolder == -1 && devices.Count >= 2)
					{		 
						Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.PAD_CAMERA_CALIBRATION_MISMATCH)+SNHolder);
					}
				}
			}





			if (previousStateIndex != controllerIndexZEDHolder && controllerIndexZEDHolder != -1)
			{
				previousStateIndex = controllerIndexZEDHolder;
				if (ZEDOnPadIndexSet != null)
				{
					ZEDOnPadIndexSet();
				}
			}
		}
	}

    /// <summary>
    /// Converts a button to a SteamVR enum
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
	private EVRButtonId ConvertToButton(sl.CONTROLS_BUTTON button)
	{
		switch (button)
		{
		case sl.CONTROLS_BUTTON.ONE: return model == HMD.OCULUS ? EVRButtonId.k_EButton_A : EVRButtonId.k_EButton_ApplicationMenu;
		case sl.CONTROLS_BUTTON.THREE: return EVRButtonId.k_EButton_ApplicationMenu;
		case sl.CONTROLS_BUTTON.PRIMARY_THUBMSTICK: return EVRButtonId.k_EButton_SteamVR_Touchpad;
		case sl.CONTROLS_BUTTON.SECONDARY_THUMBSTICK: return EVRButtonId.k_EButton_SteamVR_Touchpad;

		}
		return EVRButtonId.k_EButton_ApplicationMenu;
	}

    /// <summary>
    /// Converts an axis to a SteamVR enum
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
	private EVRButtonId ConvertToAxis(sl.CONTROLS_AXIS1D button)
	{
		switch (button)
		{
		case sl.CONTROLS_AXIS1D.PRIMARY_INDEX_TRIGGER: return EVRButtonId.k_EButton_SteamVR_Trigger;
		case sl.CONTROLS_AXIS1D.SECONDARY_INDEX_TRIGGER: return EVRButtonId.k_EButton_SteamVR_Trigger;
		case sl.CONTROLS_AXIS1D.PRIMARY_HAND_TRIGGER: return EVRButtonId.k_EButton_Grip;
		case sl.CONTROLS_AXIS1D.SECONDARY_HAND_TRIGGER: return EVRButtonId.k_EButton_Grip;

		}
		return EVRButtonId.k_EButton_Grip;
	}
	#endif
    /// <summary>
    /// Checks if button is down
    /// </summary>
    /// <param name="idButton"></param>
    /// <param name="idPad"></param>
    /// <returns></returns>
	public bool GetDown(sl.CONTROLS_BUTTON idButton, int idPad)
	{
		#if ZED_STEAM_VR
		if (!padsAreInit || idPad == -1) return false;
		return SteamVR_Controller.Input(idPad).GetPressDown(ConvertToButton(idButton));
		#else
		return false;
		#endif
	}
    /// <summary>
    /// Get the current position of a controller
    /// </summary>
    /// <param name="idPad"></param>
    /// <returns></returns>
	public Vector3 GetPosition(int idPad)
	{
		#if ZED_STEAM_VR
		if (!padsAreInit || idPad == -1) return Vector3.zero;
		return delayedPads[idPad].o.transform.position;
		#else
		return Vector3.zero;
		#endif
	}

    /// <summary>
    /// Returns the state of triggers
    /// </summary>
    /// <param name="idTrigger"></param>
    /// <param name="idPad"></param>
    /// <returns></returns>
	public float GetHairTrigger(sl.CONTROLS_AXIS1D idTrigger, int idPad)
	{
		#if ZED_STEAM_VR
		if (!padsAreInit || idPad == -1) return 0;

		return (float)SteamVR_Controller.Input(idPad).GetAxis((EVRButtonId)idTrigger).magnitude;
		#else
		return 0;
		#endif
	}

    /// <summary>
    /// Returns the ID of the right index
    /// </summary>
    /// <returns></returns>
	public int GetRightIndex()
	{
		#if ZED_STEAM_VR
		if (PadsAreInit)
		{
			var system = OpenVR.System;

			if (system != null)
			{
				return (int)system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
			}
		}
		return -1;
		#else
		return -1;
		#endif
	}

    /// <summary>
    /// Returns the ID of the left index
    /// </summary>
    /// <returns></returns>
	public int GetLeftIndex()
	{
		#if ZED_STEAM_VR
		if (PadsAreInit)
		{
			var system = OpenVR.System;

			if (system != null)
			{
				return (int)system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			}
		}
		return -1;
		#else
		return -1;
		#endif
	}


}

#if UNITY_EDITOR
[CustomEditor(typeof(ZEDSteamVRControllerManager)), CanEditMultipleObjects]
public class ZEDSteamVRDependencies : Editor
{
    [SerializeField]
    const string defineName = "ZED_STEAM_VR";
    const string packageName = "SteamVR";



    public override void OnInspectorGUI()
    {
        if (EditorPrefs.GetBool(packageName))
        {
            DrawDefaultInspector();
        }
        else
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Load SteamVR data"))
            {
                if(CheckPackageExists(packageName))
                {
                    ActivateDefine();
                }
            }
            EditorGUILayout.HelpBox(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.STEAMVR_NOT_INSTALLED), MessageType.Warning);
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

    public class AssetPostProcessSteamVR : AssetPostprocessor
    {

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            if (ZEDSteamVRDependencies.CheckPackageExists("SteamVR"))
            {
                ZEDSteamVRDependencies.ActivateDefine();
            }
            else
            {
                ZEDSteamVRDependencies.DesactivateDefine();
            }
        }
    }
}

#endif
