//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class ZEDOculusControllerManager : MonoBehaviour, ZEDControllerManager
{

    private bool padsAreInit = false;
    public bool PadsAreInit
    {
        get { return padsAreInit; }
    }
    private int controllerIndexZEDHolder = -1;
    public int ControllerIndexZEDHolder { get { return controllerIndexZEDHolder; } }

#if ZED_OCULUS
    enum CONTROLLER
    {
        LEFT,
        RIGHT
    }
    /// <summary>
    /// Container of the tracking at a certain time
    /// </summary>
    public struct TimedPoseData
    {
        public float timestamp;
        public Quaternion rotation;
        public Vector3 position;
    }

    /// <summary>
    /// Struct for the delayed controller
    /// </summary>
    public struct DelayedPad
    {
        public GameObject controller;
        public List<TimedPoseData> delays;
    }

    /// <summary>
    /// Model of the left controller
    /// </summary>
    public GameObject modelLeftController;

    /// <summary>
    /// Model of the right controller
    /// </summary>
    public GameObject modelRightController;

    /// <summary>
    /// Left controller
    /// </summary>
    private GameObject leftController = null;

    /// <summary>
    /// Right controller
    /// </summary>
    private GameObject rightController = null;


    /// <summary>
    /// List of delayed controllers
    /// </summary>
    private DelayedPad[] controllers = new DelayedPad[(int)CONTROLLER.RIGHT + 1];
    [Range(0, 200)]
    public uint delay = 0;

    /// <summary>
    /// Struct to store a position
    /// </summary>
    public struct Pose
    {
        public Vector3 pose;
        public Quaternion rot;
    }

    /// <summary>
    /// Copy the mesh to set it to the left and right delayed controller
    /// </summary>
    private void CopyMeshController()
    {

        //Copy original controllers and destroy useless components
        if (leftController == null)
        {
            if (modelLeftController != null)
            {
                leftController = Instantiate(modelLeftController);

                leftController.name = "DelayedPad_" + CONTROLLER.LEFT.ToString();

                controllers[(int)CONTROLLER.LEFT].controller = leftController;
                controllers[(int)CONTROLLER.LEFT].delays = new List<TimedPoseData>();
            }
        }

        if (rightController == null)
        {
            if (modelRightController != null)
            {
                rightController = Instantiate(modelRightController);
                rightController.name = "DelayedPad_" + CONTROLLER.RIGHT.ToString();

                controllers[(int)CONTROLLER.RIGHT].controller = rightController;
                controllers[(int)CONTROLLER.RIGHT].delays = new List<TimedPoseData>();

            }

        }
    }

    /// <summary>
    /// Registers the position at a defined time
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

        controllers[index].delays.Add(currentPoseData);
    }

    //Compute the delayed position and rotation from history
    private Pose GetValuePosition(int index, float timeDelay)
    {
        Pose p = new Pose();
        p.pose = controllers[index].delays[controllers[index].delays.Count - 1].position;
        p.rot = controllers[index].delays[controllers[index].delays.Count - 1].rotation;

        float idealTS = (Time.time - timeDelay);

        for (int i = 0; i < controllers[index].delays.Count; ++i)
        {
            if (controllers[index].delays[i].timestamp > idealTS)
            {
                int currentIndex = i;
                if (currentIndex > 0)
                {
                    float timeBetween = controllers[index].delays[currentIndex].timestamp - controllers[index].delays[currentIndex - 1].timestamp;
                    float alpha = ((Time.time - controllers[index].delays[currentIndex - 1].timestamp) - timeDelay) / timeBetween;

                    Vector3 pos = Vector3.Lerp(controllers[index].delays[currentIndex - 1].position, controllers[index].delays[currentIndex].position, alpha);
                    Quaternion rot = Quaternion.Lerp(controllers[index].delays[currentIndex - 1].rotation, controllers[index].delays[currentIndex].rotation, alpha);

                    p = new Pose();
                    p.pose = pos;
                    p.rot = rot;
                    controllers[index].delays.RemoveRange(0, currentIndex - 1);
                }
                return p;
            }
        }
        controllers[index].delays.Clear();
        return p;
    }

    void Update()
    {

        if (leftController == null || rightController == null)
        {
            CopyMeshController();
        }
        else
        {
            padsAreInit = true;
        }

        if (padsAreInit)
        {
            RegisterPosition((int)CONTROLLER.LEFT, OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch));
            RegisterPosition((int)CONTROLLER.RIGHT, OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));

            Pose p;
            p = GetValuePosition((int)CONTROLLER.LEFT, (float)(delay / 1000.0f));
            controllers[(int)CONTROLLER.LEFT].controller.transform.position = p.pose;
            controllers[(int)CONTROLLER.LEFT].controller.transform.rotation = p.rot;

            p = GetValuePosition((int)CONTROLLER.RIGHT, (float)(delay / 1000.0f));
            controllers[(int)CONTROLLER.RIGHT].controller.transform.position = p.pose;
            controllers[(int)CONTROLLER.RIGHT].controller.transform.rotation = p.rot;

        }

        if(controllerIndexZEDHolder != -1)
        {
			if(controllers[(int)controllerIndexZEDHolder].controller != null) {
            transform.parent.localPosition = controllers[(int)controllerIndexZEDHolder].controller.transform.position;
            transform.parent.localRotation = controllers[(int)controllerIndexZEDHolder].controller.transform.rotation;
			}

        }

        OVRInput.Update();

    }

    /// <summary>
    /// Converts the standard ID button to the ID of Oculus
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
    private OVRInput.Button ConvertToButton(sl.CONTROLS_BUTTON button)
    {
        switch (button)
        {
            case sl.CONTROLS_BUTTON.ONE: return OVRInput.Button.One;
            case sl.CONTROLS_BUTTON.THREE: return OVRInput.Button.Three;
            case sl.CONTROLS_BUTTON.PRIMARY_THUBMSTICK: return OVRInput.Button.PrimaryThumbstick;
            case sl.CONTROLS_BUTTON.SECONDARY_THUMBSTICK: return OVRInput.Button.SecondaryThumbstick;

        }
        return OVRInput.Button.None;
    }

    /// <summary>
    /// Converts the standard ID axis to the ID of Oculus
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
    private OVRInput.Axis1D ConvertToAxis(sl.CONTROLS_AXIS1D button)
    {
        switch (button)
        {
            case sl.CONTROLS_AXIS1D.PRIMARY_INDEX_TRIGGER: return OVRInput.Axis1D.PrimaryIndexTrigger;
            case sl.CONTROLS_AXIS1D.SECONDARY_INDEX_TRIGGER: return OVRInput.Axis1D.SecondaryIndexTrigger;
            case sl.CONTROLS_AXIS1D.PRIMARY_HAND_TRIGGER: return OVRInput.Axis1D.PrimaryHandTrigger;
            case sl.CONTROLS_AXIS1D.SECONDARY_HAND_TRIGGER: return OVRInput.Axis1D.SecondaryIndexTrigger;

        }
        return OVRInput.Axis1D.None;
    }

    /// <summary>
    /// Second step to update Oculus touch
    /// </summary>
    private void FixedUpdate()
    {
        OVRInput.FixedUpdate();
    }
#endif
    /// <summary>
    /// Checks if a button is down
    /// </summary>
    /// <param name="button"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool GetDown(sl.CONTROLS_BUTTON button, int id = -1)
    {
#if ZED_OCULUS
        return OVRInput.GetDown(ConvertToButton(button));
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets the local position of a controller
    /// </summary>
    /// <param name="IDPad"></param>
    /// <returns></returns>
    public Vector3 GetPosition(int IDPad)
    {
#if ZED_OCULUS
        return OVRInput.GetLocalControllerPosition((OVRInput.Controller)IDPad);
#else
        return Vector3.zero;
#endif
    }

    /// <summary>
    /// Gets the status of a trigger
    /// </summary>
    /// <param name="idTrigger"></param>
    /// <param name="idPad"></param>
    /// <returns></returns>
    public float GetHairTrigger(sl.CONTROLS_AXIS1D idTrigger, int idPad)
    {
#if ZED_OCULUS
        return OVRInput.Get(ConvertToAxis(idTrigger), (OVRInput.Controller)idPad);
#else
        return 0;
#endif
    }
    /// <summary>
    /// Get the ID of the right index
    /// </summary>
    /// <returns></returns>
    public int GetRightIndex()
    {
#if ZED_OCULUS
        return (int)OVRInput.Controller.RTouch;
#else
        return -1;
#endif
    }

    /// <summary>
    /// Gets the ID of the left index
    /// </summary>
    /// <returns></returns>
    public int GetLeftIndex()
    {
#if ZED_OCULUS
        return (int)OVRInput.Controller.LTouch;
#else
        return -1;
#endif
    }

    /// <summary>
    /// Loads the index controlling the ZED 
    /// </summary>
    /// <param name="path"></param>
    public void LoadIndex(string path)
    {
        #if ZED_OCULUS

        if (!System.IO.File.Exists(path))
        {
            return;
        }

        string[] lines = null;
        try
        {
            lines = System.IO.File.ReadAllLines(path);
        }
        catch (System.Exception)
        {
        }
        if (lines == null)
        {
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
                    if(field.Contains("Controller_Left"))
                    {

                        controllerIndexZEDHolder = (int)CONTROLLER.LEFT;
                    }else if(field.Contains("Controller_Right"))
                    {
                        controllerIndexZEDHolder = (int)CONTROLLER.RIGHT;
                    }
                }
            }
        }
#endif
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ZEDOculusControllerManager)), CanEditMultipleObjects]
public class ZEDOculusDependencies : Editor
{
	[SerializeField]
	const string defineName = "ZED_OCULUS";
	const string packageName = "Oculus"; //Switch to OVR for old plugin.



	public override void OnInspectorGUI()
	{
		if (EditorPrefs.GetBool(packageName) && CheckPackageExists (packageName) && isDefineActivated())
		{
			DrawDefaultInspector();
		}
		else
		{
			// Should not be needed but in just in case...
			GUILayout.Space(20);
			if (GUILayout.Button("Activate Oculus"))
			{
				if(CheckPackageExists(packageName))
				{
					ActivateDefine();
				}
			}

			//if OVR package does not exist, undef ZED_OCULUS to avoid build errors
			if (!CheckPackageExists (packageName)) {
				EditorGUILayout.HelpBox (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.OVR_NOT_INSTALLED), MessageType.Warning);
				DesactivateDefine ();
			}

		}
	}


	//Enable Or Disable : Define ZED_OCULUS is package exist. Undef it if not, to avoid build errors.
	public void OnEnable()
	{
		if(CheckPackageExists(packageName))
			ActivateDefine();
		else
			DesactivateDefine ();
	}


	public void OnDisable()
	{
		if(CheckPackageExists(packageName))
			ActivateDefine();
		else
			DesactivateDefine ();
	}


	public static bool CheckPackageExists(string name)
	{
		string[] packages = AssetDatabase.FindAssets(name);
		return packages.Length != 0 && AssetDatabase.IsValidFolder("Assets/" + name);
	}

	public static bool isDefineActivated()
	{
		string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
		if (defines.Length != 0)
		{
			return defines.Contains (defineName);
		}

		return false;

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

	public class AssetPostProcessOculus : AssetPostprocessor
	{

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{

			if (ZEDOculusDependencies.CheckPackageExists("OVR") || ZEDOculusDependencies.CheckPackageExists("Oculus"))
			{
				ZEDOculusDependencies.ActivateDefine();
			}
			else
			{
				ZEDOculusDependencies.DesactivateDefine();
			}
		}
	}
}

#endif

