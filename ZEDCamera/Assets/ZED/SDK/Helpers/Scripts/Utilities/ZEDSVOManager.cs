//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Manages the SVO, only used as an interface for Unity
/// </summary>
public class ZEDSVOManager : MonoBehaviour
{
    /// <summary>
    /// Set to true to record a SVO
    /// </summary>
    [SerializeField]
    public bool record = false;

    /// <summary>
    /// Set to true to read a SVO, a SVO cannot be read and written at once
    /// </summary>
    [SerializeField]
    public bool read = false;

    /// <summary>
    /// Set the video file to record or read
    /// </summary>
    [SerializeField]
    [HideInInspector]
    public string videoFile = "Assets/ZEDRecording.svo";

    /// <summary>
    /// Loop the svo, if read is set to true
    /// </summary>
    [SerializeField]
    [Tooltip("Loop the SVO")]
    public bool loop = false;

    [SerializeField]
    [HideInInspector]
    public bool realtimePlayback = true;

    /// <summary>
    /// Current frame of the SVO (read)
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int currentFrame = 0;

    /// <summary>
    /// Number max of frames in the SVO
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int numberFrameMax = 0;

    /// <summary>
    /// Pause the SVO, Unity will continue to run
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool pause = false;

    /// <summary>
    /// Compression mode to register a SVO
    /// </summary>
    [SerializeField]
	public sl.SVO_COMPRESSION_MODE compressionMode = sl.SVO_COMPRESSION_MODE.LOSSY_BASED;

    /// <summary>
    /// Number frame max of the SVO
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
    /// Current frame of the SVO (number)
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
    /// Init the SVOManager
    /// </summary>
    /// <param name="zedCamera"></param>
    public void InitSVO(sl.ZEDCamera zedCamera)
    {
        if (record)
        {
			if (zedCamera.EnableRecording(videoFile,compressionMode) != sl.ERROR_CODE.SUCCESS)
            {
                record = false;
            }
        }

        if (read)
        {
            NumberFrameMax = zedCamera.GetSVONumberOfFrames();
        }
    }
}

#if UNITY_EDITOR


[CustomEditor(typeof(ZEDSVOManager)), CanEditMultipleObjects]
public class SVOManagerInspector : Editor
{
    private bool current_recordValue = false;
    private bool current_readValue = false;

    private SerializedProperty pause;
    private SerializedProperty record;
    private SerializedProperty read;
    private SerializedProperty loop;
    private SerializedProperty videoFile;
    private SerializedProperty currentFrame;
    private SerializedProperty numberFrameMax;

    Rect drop_area;

    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };
    string pauseText = "Pause";

    string[] filters = { "Svo files", "svo" };
    private ZEDSVOManager obj;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        obj = (ZEDSVOManager)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            string tooltip = "Uses timecodes of each frame for playback. Causes a pause for dropped frames or when pause was used during recording.";
            obj.realtimePlayback = EditorGUILayout.Toggle(new GUIContent("Realtime Playback", tooltip), obj.realtimePlayback);
        }

            EditorGUILayout.BeginHorizontal();
        videoFile.stringValue = EditorGUILayout.TextField("SVO Path", videoFile.stringValue);
        if (GUILayout.Button("...", optionsButtonBrowse))
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
        currentFrame.intValue = EditorGUILayout.IntSlider("Frame ", currentFrame.intValue, 0, numberFrameMax.intValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (sl.ZEDCamera.GetInstance() != null)
            {
                //If the slider of frame from the SVO has moved, grab mannually the frame and update the textures
                sl.ZEDCamera.GetInstance().SetSVOPosition(currentFrame.intValue);
                if (pause.boolValue)
                {
                    sl.ZEDCamera.GetInstance().UpdateTextures();
                }
            }
        }
        GUI.enabled = true;

		GUI.enabled = sl.ZEDCamera.GetInstance() != null && sl.ZEDCamera.GetInstance().IsCameraReady;
        pauseText = pause.boolValue ? "Resume" : "Pause";
        if (GUILayout.Button(pauseText))
        {
            pause.boolValue = !pause.boolValue;
        }
        GUI.enabled = true;
        DropAreaGUI();

        serializedObject.ApplyModifiedProperties();

    }

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
    /// To allow looping when reading is on
    /// To not allow to read and record at the same time
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
    /// Helper to get the name of a drag and dropped file
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
