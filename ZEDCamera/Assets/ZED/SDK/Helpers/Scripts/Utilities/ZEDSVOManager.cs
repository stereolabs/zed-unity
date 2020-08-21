//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//       ##DEPRECATED

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// [Obsolete] Lets you play back a recorded SVO file, which works in place of the input from a real ZED, 
/// or record an SVO file from a live ZED. 
/// <para>To use, attach to the same GameObject as ZED Manager (ZED_Rig_Mono or ZED_Rig_Stereo).</para
/// </summary><remarks>
/// [Obsolete] When playing an SVO, the SDK behaves the same as if a real ZED were attached, so all regular features
/// are available. The one exception is pass-through AR, as the ZED's timestamps and transform will both be 
/// significantly out of sync with the VR headset. 
/// </remarks>

[System.Obsolete("SVO Management has been moved to ZEDManager directly. Not used anymore", true)]
public class ZEDSVOManager : MonoBehaviour
{
    /// <summary>
    /// Set to true to record an SVO. Recording begins when the ZED initialized and ends when the
    /// application finishes. 
    /// </summary>
    [SerializeField]
    [Tooltip("Set to true to record an SVO. Recording begins when the ZED initialized and ends when " + 
        "the application finishes.")]
    public bool record = false;

    /// <summary>
    /// Set to true to read an SVO, using it as input instead of a real ZED. An SVO cannot be read and recorded
    /// at the same time. 
    /// </summary>
    [SerializeField]
    [Tooltip("Set to true to read an SVO, using it as input instead of a real ZED. " +
        "An SVO cannot be read and recorded at the same time.")]
    public bool read = false;

    /// <summary>
    /// Path to the SVO to be read, or where a new SVO will be recorded. 
    /// <para>Note: If building the application, put the file in the root directory or specify a non-relative path
    /// to preserve this reference.</para>
    /// </summary>
    [SerializeField]
    [HideInInspector]
    public string videoFile = "Assets/ZEDRecording.svo";

    /// <summary>
    /// If reading an SVO, set this to true if the SVO should repeat once it finishes. 
    /// <para>Some features won't handle this gracefully, such as tracking.</para>
    /// </summary>
    [SerializeField]
    [Tooltip("If reading an SVO, set this to true if the SVO should repeat once it finishes. " +
        "Some features won't handle this gracefully, such as tracking.")]
    public bool loop = false;

    /// <summary>
    /// If reading an SVO, set true to use frame timestamps to set playback speed. Dropped
    /// frames will cause a 'pause' in playback instead of a 'skip.' 
    /// </summary>
    [Tooltip("If reading an SVO, set true to use frame timestamps to set playback speed. " +
        "Dropped frames will cause a 'pause' in playback instead of a 'skip.'")]
    [SerializeField]
    [HideInInspector]
    public bool realtimePlayback = false;

    /// <summary>
    /// Current frame being read from the SVO. Doesn't apply when recording. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int currentFrame = 0;
    /// <summary>
    /// Current frame being read from the SVO. Doesn't apply when recording. 
    /// </summary>
    public int CurrentFrame
    {
        get
        {
            return currentFrame;
        }
        set
        {
            currentFrame = value;
        }
    }

    /// <summary>
    /// Total number of frames in a loaded SVO. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int numberFrameMax = 0;
    /// <summary>
    /// Total number of frames in a loaded SVO. 
    /// </summary>
    public int NumberFrameMax
    {
        set
        {
            numberFrameMax = value;
        }
        get
        {
            return numberFrameMax;
        }
    }

    /// <summary>
    /// Set true to pause the SVO reading or recording. Will not pause the Unity scene itself. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool pause = false;

    /// <summary>
    /// Compression mode used when recording an SVO. 
    /// Uncompressed SVOs are extremely large (multiple gigabytes per minute). 
    /// </summary>
    [Tooltip("Compression mode used when recording an SVO. " +
        "Uncompressed SVOs are extremely large (multiple gigabytes per minute).")]
    [SerializeField]
	public sl.SVO_COMPRESSION_MODE compressionMode = sl.SVO_COMPRESSION_MODE.H264_BASED;

    /// <summary>
    /// Flag set to true when we need to force ZEDManager to grab a new frame, even though
    /// SVO playback is paused. Used when the SVO is paused but the playback slider has moved. 
    /// </summary>
    public bool NeedNewFrameGrab { get; set; }

	/// <summary>
	/// ZED Camera 
	/// </summary>
	public sl.ZEDCamera zedCam;

    /// <summary>
    /// Changes the value of record if recording fails, and gets the length of a read SVO file.
    /// </summary>
    /// <param name="zedCamera">Reference to the Scene's ZEDCamera instance.</param>
    public void InitSVO(sl.ZEDCamera zedCamera)
    {
		zedCam = zedCamera;
        if (record)
        {
            sl.ERROR_CODE svoerror = zedCamera.EnableRecording(videoFile, compressionMode,0,0);
            if (svoerror != sl.ERROR_CODE.SUCCESS)
            {
                record = false;
            }
            else if(svoerror == sl.ERROR_CODE.SVO_RECORDING_ERROR)
            {
                Debug.LogError("SVO recording failed. Check that there is enough space on the drive and that the "
                    + "path provided is valid.");
            }
        }

        if (read)
        {
            NumberFrameMax = zedCamera.GetSVONumberOfFrames();
        }
    }




}

#if UNITY_EDITOR

/// <summary>
/// Custom editor for ZEDSVOManager to change how it's drawn in the Inspector.
/// Adds a playback slider and pause button, and makes Record and Read mutually-exclusive. 
/// </summary>
[System.Obsolete("SVO Management has been moved to ZEDManager directly. Not used anymore", true)]
[CustomEditor(typeof(ZEDSVOManager)), CanEditMultipleObjects]
public class SVOManagerInspector : Editor
{
    //Caches for record and read values, to make them mutually exclusive. 
    private bool current_recordValue = false;
    private bool current_readValue = false;

    //Serializable versions of ZEDSVOManager's values so changes can be saved/serialized. 
    private SerializedProperty pause;
    private SerializedProperty record;
    private SerializedProperty read;
    private SerializedProperty loop;
    private SerializedProperty videoFile;
    private SerializedProperty currentFrame;
    private SerializedProperty numberFrameMax;

    Rect drop_area; //Bounds for dragging and dropping SVO files. 

    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) }; //Adds padding for the SVO browse button. 
    string pauseText = "Pause";
    string pauseTooltip = " SVO playback or recording."; //Appended to the pause Text to make tooltip text.

    string[] filters = { "Svo files", "svo" }; //Filters used for browsing for an SVO. 
    private ZEDSVOManager obj;

    /// <summary>
    /// Called by Unity each time the editor is viewed. 
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        obj = (ZEDSVOManager)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            string tooltip = "If reading an SVO, set true to use frame timestamps to set playback speed." +
                "Dropped frames will cause a 'pause' in playback instead of a 'skip.'";
            obj.realtimePlayback = EditorGUILayout.Toggle(new GUIContent("Realtime Playback", tooltip), obj.realtimePlayback);
        }

        EditorGUILayout.BeginHorizontal();
        GUIContent pathlabel = new GUIContent("SVO Path", "Path to the SVO to be read, or where a new SVO will be recorded. " +
            "Note: If building the application, put the file in the root directory or specify a non-relative path " +
            "to preserve this reference.");
        videoFile.stringValue = EditorGUILayout.TextField(pathlabel, videoFile.stringValue);

        GUIContent loadlabel = new GUIContent("...", "Browse for existing SVO file.");
        if (GUILayout.Button(loadlabel, optionsButtonBrowse))
        {
            obj.videoFile = EditorUtility.OpenFilePanelWithFilters("Load SVO", "", filters);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();
        if (drop_area.width != EditorGUIUtility.currentViewWidth || drop_area.height != Screen.height)
        {
            drop_area = new Rect(0, 0, EditorGUIUtility.currentViewWidth, Screen.height);
        }
        if (EditorGUI.EndChangeCheck())
        {
            CheckChange();
        }
        EditorGUI.BeginChangeCheck();

        GUI.enabled = (obj.NumberFrameMax > 0);
        GUIContent sliderlabel = new GUIContent("Frame ", "SVO playback position");
        currentFrame.intValue = EditorGUILayout.IntSlider(sliderlabel, currentFrame.intValue, 0, numberFrameMax.intValue);
        if (EditorGUI.EndChangeCheck())
        {
			if (obj.zedCam != null)
            {
                //If the slider of frame from the SVO has moved, manually grab the frame and update the textures.
				obj.zedCam.SetSVOPosition(currentFrame.intValue);
                if (pause.boolValue)
                {
					obj.NeedNewFrameGrab = true;
                }
            }
        }
      
		GUI.enabled = false;

		if (obj.zedCam != null)
			GUI.enabled = obj.zedCam.IsCameraReady;
	
        pauseText = pause.boolValue ? "Resume" : "Pause";
        GUIContent pauselabel = new GUIContent(pauseText, pauseText + pauseTooltip);
        if (GUILayout.Button(pauselabel))
        {
            pause.boolValue = !pause.boolValue;
        }
        GUI.enabled = true;
        DropAreaGUI();

        serializedObject.ApplyModifiedProperties(); //Applies changes to serialized properties to the values they represent. 

    }

    /// <summary>
    /// Binds the serialized properties to their respective values in ZEDSVOManager. 
    /// </summary>
    private void OnEnable()
    {
        pause = serializedObject.FindProperty("pause");
        record = serializedObject.FindProperty("record");
        read = serializedObject.FindProperty("read");
        loop = serializedObject.FindProperty("loop");

        videoFile = serializedObject.FindProperty("videoFile");
        currentFrame = serializedObject.FindProperty("currentFrame");
        numberFrameMax = serializedObject.FindProperty("numberFrameMax");
    }

    /// <summary>
    /// Allows looping when reading an SVO file (only) and prevents
    /// reading and recording from both being true at the same time.
    /// </summary>
    private void CheckChange()
    {
        if (loop.boolValue && record.boolValue)
        {
            loop.boolValue = false;
        }
        if (read.boolValue && (current_readValue != read.boolValue))
        {
            record.boolValue = false;
            current_recordValue = false;
            current_readValue = read.boolValue;
        }
        if (!read.boolValue && (current_readValue != read.boolValue))
        {
            loop.boolValue = false;
        }
        if (record.boolValue && (current_recordValue != record.boolValue))
        {
            read.boolValue = false;
            current_readValue = false;
            loop.boolValue = false;
            current_recordValue = record.boolValue;
        }

    }

    /// <summary>
    /// Helper to get the name of a drag and dropped file.
    /// </summary>
    public void DropAreaGUI()
    {
        Event evt = Event.current;
        ZEDSVOManager obj = (ZEDSVOManager)target;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!drop_area.Contains(evt.mousePosition))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (string dragged_object in DragAndDrop.paths)
                    {
                        videoFile.stringValue = dragged_object;
                    }
                }
                break;
        }
    }
}
#endif
