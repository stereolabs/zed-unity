//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//       ##DEPRECATED


using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if ZED_STEAM_VR
    using Valve.VR;
#endif

/// <summary>
/// Saves and loads the pose of this object relative to its parent. 
/// Used primarily when mounting the ZED to a tracked object (like a VR controller) for 3rd person mixed reality. 
/// This way, you can calibrate its position/rotation to line up the real/virtual worlds, and hit Save. 
/// It will automatically load a calibration in the future if it's found.
/// Note that if you used our beta tool for SteamVR calibration, this script will load that calibration automatically. 
/// </summary>
public class ZEDOffsetController : MonoBehaviour
{
    /// <summary>
    /// ZED offset file name.
    /// </summary>
    [SerializeField]
    public static string ZEDOffsetFile = "ZED_Position_Offset.conf";

    /// <summary>
    /// Where to save the ZED offset file. 
    /// </summary>
    private string path = @"Stereolabs\steamvr";

    /// <summary>
    /// The ZEDControllerTracker object in the scene from which we're offset. 
    /// This script checks this object, its parents and its children (in that order) for such a component.
    /// </summary>
	public ZEDControllerTracker controllerTracker;

    /// <summary>
    /// If the object is instantiated and ready to save/load an offset file. 
    /// Used by the custom Inspector editor to know if the Save/Load buttons should be pressable. 
    /// </summary>
	public bool isReady = false;

    /// <summary>
    /// Save the local position/rotation of the ZED into an offset file. 
    /// </summary>
    public void SaveZEDPos()
    {
		using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
        {
            string tx = "x=" + transform.localPosition.x.ToString() + " //Translation x";
            string ty = "y=" + transform.localPosition.y.ToString() + " //Translation y";
            string tz = "z=" + transform.localPosition.z.ToString() + " //Translation z";
            string rx = "rx=" + transform.localRotation.eulerAngles.x.ToString() + " //Rotation x";
            string ry = "ry=" + transform.localRotation.eulerAngles.y.ToString() + " //Rotation y";
            string rz = "rz=" + transform.localRotation.eulerAngles.z.ToString() + " //Rotation z";

            //Write those values into the file. 
            file.WriteLine(tx);
            file.WriteLine(ty);
            file.WriteLine(tz);
            file.WriteLine(rx);
            file.WriteLine(ry);
            file.WriteLine(rz);
			if (sl.ZEDCamera.GetInstance().IsCameraReady)
            {
                string fov = "fov=" + (sl.ZEDCamera.GetInstance().GetFOV() * Mathf.Rad2Deg).ToString();
                file.WriteLine(fov);

            }


#if ZED_STEAM_VR
            if (TrackerComponentExist()) 
            {
                //If using SteamVR, get the serial number of the tracked device, or write "NONE" to indicate we checked but couldn't find it. 
                //This is used by ZEDControllerManager later to know specifically which device the loaded offset has been calibrated to, in the event of multiple controllers/trackers. 
                string i = "indexController=" + (controllerTracker.index > 0 ? SteamVR.instance.GetStringProperty(Valve.VR.ETrackedDeviceProperty.Prop_SerialNumber_String, (uint)controllerTracker.index) : "NONE") + " //SN of the pad attached to the camera (NONE to set no pad on it)";
                file.WriteLine(i);
            }
#endif

            file.Close(); //Finalize the new file. 
        }
    }

    /// <summary>
    /// Whether there is a referenced ZEDControllerTracker object in this object, a parent, or a child. 
    /// </summary>
    /// <returns>True if such a component exists and is used to handle the offset.</returns>
    public bool TrackerComponentExist()
    {
        if (controllerTracker != null)
            return true;
        else
            return false;
    }

    private void OnEnable()
    {
        LoadTrackerComponent();
    }

    /// <summary>
    /// Searched for a ZEDControllerTracker component in this object, its parents, and its children.
    /// Sets the controllerTracker value to the first one it finds. 
    /// </summary>
    private void LoadTrackerComponent()
    {
        ZEDControllerTracker zct = GetComponent<ZEDControllerTracker>();
        if (zct == null)
            zct = GetComponentInParent<ZEDControllerTracker>();
        if (zct == null)
            zct = GetComponentInChildren<ZEDControllerTracker>();
        if (zct != null)
            controllerTracker = zct;
    }
	
    /// <summary>
    /// Tries to find the relevant ZEDControllerTracker object, and loads the existing
    /// offset file if there is one. 
    /// </summary>
    void Awake()
    {
        LoadTrackerComponent();

        string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string specificFolder = Path.Combine(folder, @"Stereolabs\steamvr");
        path = Path.Combine(specificFolder, ZEDOffsetFile);

        // Check if folder exists and if not, create it
        if (!Directory.Exists(specificFolder))
			Directory.CreateDirectory(specificFolder);


        LoadZEDPos();
		CreateFileWatcher(specificFolder);

		isReady = true;
    }

    private void Update()
    {
        if (ZEDManager.Instance.IsZEDReady)
            LoadZEDPos();
    }

    /// <summary>
    /// Loads the offset file and sets the local position/rotation to the loaded values.
    /// </summary>
    public void LoadZEDPos()
    {
		if (!System.IO.File.Exists(path)) return;

        string[] lines = null;
        try
        {
			lines = System.IO.File.ReadAllLines(path);
        }
        catch (System.Exception)
        {
            controllerTracker.SNHolder = "NONE";
        }
        if (lines == null)
        {
            controllerTracker.SNHolder = "NONE";
            return;
        }
        if (lines == null) return;
        Vector3 position = new Vector3(0, 0, 0);
        Vector3 eulerRotation = new Vector3(0, 0, 0);
        foreach (string line in lines)
        {
            string[] splittedLine = line.Split('=');
            if (splittedLine != null && splittedLine.Length >= 2)
            {
                string key = splittedLine[0];
                string field = splittedLine[1].Split(' ')[0];

                if (key == "x")
                {
                    position.x = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "y")
                {
                    position.y = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "z")
                {
                    position.z = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "rx")
                {
                    eulerRotation.x = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "ry")
                {
                    eulerRotation.y = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "rz")
                {
                    eulerRotation.z = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if(key == "indexController")
                {
                    LoadTrackerComponent();

                    if (TrackerComponentExist())
                    {
                        controllerTracker.SNHolder = field;
                    }
                }
            }
        }
        transform.localPosition = position;
        transform.localRotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z);
    }

    /// <summary>
    /// Creates a FileSystemWatcher that keeps track of the offset file, in case it
    /// changes or moves. 
    /// </summary>
    /// <param name="path"></param>
    public void CreateFileWatcher(string path)
    {
        // Create a new FileSystemWatcher and set its properties.
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = path;
        /* Watch for changes in LastAccess and LastWrite times, and 
           the renaming of files or directories. */
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        // Only watch text files.
        watcher.Filter = ZEDOffsetFile;

        // Add event handlers.
        watcher.Changed += new FileSystemEventHandler(OnChanged);

        // Begin watching.
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Event handler for when the offset file changes or moves. 
    /// Called by the FileSystemWatcher created in CreateFileWatcher(). 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        if (TrackerComponentExist())
        {
            LoadZEDPos();
        }
    }
}

#if UNITY_EDITOR


/// <summary>
/// Custom editor for ZEDOffsetController, to define its Inspector layout.
/// Specifically, it doesn't draw public fields like normal but instead places Save/Load buttons
/// for the offset file that are only pressable during runtime. 
/// </summary>
[CustomEditor(typeof(ZEDOffsetController))]
public class ZEDPositionEditor : Editor
{
    private ZEDOffsetController positionManager;

    public void OnEnable()
    {
        positionManager = (ZEDOffsetController)target;

    }

    public override void OnInspectorGUI() //Called when the Inspector GUI becomes visible, or changes at all. 
    {
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

		GUI.enabled = positionManager.isReady;
        GUIContent savecontent = new GUIContent("Save Offset", "Saves the object's local position/rotation to a text file to be loaded anytime in the future.");
        if (GUILayout.Button(savecontent))
        {
            positionManager.SaveZEDPos();
        }
        GUIContent loadcontent = new GUIContent("Load Offset", "Loads local position/rotation from an offset file previously saved, or created by the beta ZED calibration tool.");
        if (GUILayout.Button(loadcontent))
        {
            positionManager.LoadZEDPos();
        }
        EditorGUILayout.EndHorizontal();
    }
}

#endif
