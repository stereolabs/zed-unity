//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor used by ZEDManager to extend the default panel shown in the Inspector.
/// Adds the camera status boxes, the button on the bottom to open camera settings, and a button to restart the camera when 
/// a settings has changed that requires it. 
/// </summary>
[CustomEditor(typeof(ZEDManager)), CanEditMultipleObjects]
public class ZEDCameraEditor : Editor
{
    /// <summary>
    /// Reference to the ZEDManager instance we're editing. 
    /// </summary>
    ZEDManager manager;

    //Store copies of ZEDManager's fields to detect changes later with CheckChange().
    //These do not need to be SerializedProperties because they're only used for checking recent changes. 
    sl.RESOLUTION resolution;
    sl.DEPTH_MODE depthmode;
    bool usespatialmemory;


    //Input Prop
    private SerializedProperty cameraIDProperty;
    private SerializedProperty inputTypeProperty;
    private SerializedProperty depthModeProperty;
    private SerializedProperty usbResolutionProperty;
    private SerializedProperty usbFPSProperty;
    private SerializedProperty svoFileNameProperty;
    private SerializedProperty svoLoopProperty;
    private SerializedProperty svoRealTimeModeProperty;
    private SerializedProperty pauseSVOProperty;
    private SerializedProperty currentFrameSVOProperty;
    private SerializedProperty maxFrameSVOProperty;

    private SerializedProperty streamIPProperty;
    private SerializedProperty streamPortProperty;
    //Tracking Prop
    private SerializedProperty enableTrackingProperty;
    private SerializedProperty enableSMProperty;
    private SerializedProperty pathSMProperty;
    private SerializedProperty estimateIPProperty;

    //Rendering Prop
    private SerializedProperty depthOcclusionProperty;
    private SerializedProperty arpostProcessingPropery;
    private SerializedProperty camBrightnessProperty ;


    //Recording Prop
    private SerializedProperty svoOutputFileNameProperty;
    private SerializedProperty svoOutputCompressionModeProperty;

    //Streaming Prop
    private SerializedProperty streamingOutProperty;
    private SerializedProperty streamingOutCodecProperty;
    private SerializedProperty streamingOutPortProperty;
    private SerializedProperty streamingOutBitrateProperty;
    private SerializedProperty streamingOutGopSizeProperty;
    private SerializedProperty streamingOutAdaptBitrateProperty;
     

    /// <summary>
    /// Layout option used to draw the '...' button for opening a File Explorer window to find a mesh file. 
    /// </summary>
    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };
    private GUILayoutOption[] optionsButtonStandard = { /*GUILayout.(EditorGUIUtility.labelWidth)*/};


    /// <summary>
    /// Text on the mesh visibility button. Switches between 'Hide Mesh' and 'Display Mesh'.
    /// </summary>
    private string displayText = "Hide Mesh";
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's range_preset property. 
	/// </summary>
	private SerializedProperty range;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's resolution_preset property. 
	/// </summary>
	private SerializedProperty mappingResolution;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's isFilteringEnable property. 
	/// </summary>
	private SerializedProperty isFilteringEnable;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's filterParameters property. 
	/// </summary>
	private SerializedProperty filterParameters;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's isTextured property. 
	/// </summary>
	private SerializedProperty saveWhenOver;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's saveWhenOver property. 
	/// </summary>
	private SerializedProperty isTextured;
	/// <summary>
	/// Serialized version of ZEDSpatialMappingManager's meshPath property. 
	/// </summary>
	private SerializedProperty meshPath;

	 
    SerializedProperty arlayer;

    SerializedProperty showarrig;
    SerializedProperty fadeinonstart;
    SerializedProperty dontdestroyonload;
 
	SerializedProperty showadvanced; //Show advanced settings or not. 
	SerializedProperty showSpatialMapping ;  //Show spatial mapping or not. 
    SerializedProperty showRecording;  //Show recording or not. 
    SerializedProperty showStreamingOut;  //Show streaming out or not 
    SerializedProperty showcamcontrol; //Show cam control settings or not. 

   
    /// <summary>
    /// Default value for camera controls
    /// </summary>
    private const int cbrightness = 4;
	private const int ccontrast = 4;
	private const int chue = 0;
	private const int csaturation = 4;
	private const int cwhiteBalance = 2600;

	/// <summary>
	/// Current value for camera controls
	/// </summary>
	private int brightness = 4;
	private int contrast = 4;
	private int hue = 0;
	private int saturation = 4;

	private bool aex_agc_control = true;
	private int exposure;
	private int gain;

	private bool awb_control = true;
	private int whitebalance;
    private bool ledStatus = true;

	/// <summary>
	/// Whether we've set a manual value to gain and exposure or if they're in auto mode. 
	/// </summary>
	private bool setManualValue = true;
	/// <summary>
	/// Whether we've set a manual value to white balance or if it's in auto mode. 
	/// </summary>
	private bool setManualWhiteBalance = true;
    SerializedProperty greyskybox;

    private const string LEFT_EYE_LAYER_NAME = "ZED Rig - Left";
    private const string RIGHT_EYE_LAYER_NAME = "ZED Rig - Right";
    private const string LEFT_EYE_FINAL_LAYER_NAME = "ZED HMD Out - Left";
    private const string RIGHT_EYE_FINAL_LAYER_NAME = "ZED HMD Out - Right";

    private string[] toolbarStrings = new string[] { "USB", "SVO", "Stream" };
    private string pauseText = "Pause";
    private string pauseTooltip = " SVO playback or recording."; //Appended to the pause Text to make tooltip text.
    private string[] filters = { "Svo files", "svo" }; //Filters used for browsing for an SVO. 

    private void OnEnable()
    {
        manager = (ZEDManager)target;

     
        resolution = manager.resolution;
        depthmode = manager.depthMode;
        usespatialmemory = manager.enableSpatialMemory;

        //Input Serialized Property
        cameraIDProperty = serializedObject.FindProperty("cameraID");
        depthModeProperty = serializedObject.FindProperty("depthMode");
        inputTypeProperty = serializedObject.FindProperty("inputType");
        usbResolutionProperty = serializedObject.FindProperty("resolution");
        usbFPSProperty = serializedObject.FindProperty("FPS");
        svoFileNameProperty = serializedObject.FindProperty("svoInputFileName");
        svoLoopProperty = serializedObject.FindProperty("svoLoopBack");
        svoRealTimeModeProperty = serializedObject.FindProperty("svoRealTimeMode");
        streamIPProperty = serializedObject.FindProperty("streamInputIP");
        streamPortProperty = serializedObject.FindProperty("streamInputPort");
        pauseSVOProperty = serializedObject.FindProperty("pauseSVOReading");
        currentFrameSVOProperty = serializedObject.FindProperty("currentFrame");
        maxFrameSVOProperty = serializedObject.FindProperty("numberFrameMax");

        //Tracking Serialized Property
        enableTrackingProperty = serializedObject.FindProperty("enableTracking");
        enableSMProperty = serializedObject.FindProperty("enableSpatialMemory");
        pathSMProperty = serializedObject.FindProperty("pathSpatialMemory");
        estimateIPProperty = serializedObject.FindProperty("estimateInitialPosition");


        ///Rendering Serialized Property
        depthOcclusionProperty = serializedObject.FindProperty("depthOcclusion");
        arpostProcessingPropery = serializedObject.FindProperty("postProcessing");
        camBrightnessProperty = serializedObject.FindProperty("m_cameraBrightness");

        ////////////////////////////////////////////////// FoldOut
        showadvanced = serializedObject.FindProperty("advancedPanelOpen");
        showSpatialMapping = serializedObject.FindProperty("spatialMappingFoldoutOpen");
        showcamcontrol = serializedObject.FindProperty("camControlFoldoutOpen");
        showRecording = serializedObject.FindProperty("recordingFoldoutOpen");
        showStreamingOut = serializedObject.FindProperty("streamingOutFoldoutOpen");


        ///Spatial Mapping Serialized Property
        range = serializedObject.FindProperty("mappingRangePreset");
		mappingResolution = serializedObject.FindProperty("mappingResolutionPreset");
		isFilteringEnable = serializedObject.FindProperty("isMappingFilteringEnable");
		filterParameters = serializedObject.FindProperty("meshFilterParameters");
		isTextured = serializedObject.FindProperty("isMappingTextured");
		saveWhenOver = serializedObject.FindProperty("saveMeshWhenOver");
		meshPath = serializedObject.FindProperty("meshPath");

        ///Recording Serialized Property
        svoOutputFileNameProperty = serializedObject.FindProperty("svoOutputFileName");
        svoOutputCompressionModeProperty = serializedObject.FindProperty("svoOutputCompressionMode");

        streamingOutProperty = serializedObject.FindProperty("enableStreaming");
        streamingOutCodecProperty = serializedObject.FindProperty("streamingCodec");
        streamingOutPortProperty = serializedObject.FindProperty("streamingPort");
        streamingOutBitrateProperty = serializedObject.FindProperty("bitrate");
        streamingOutGopSizeProperty = serializedObject.FindProperty("gopSize");
        streamingOutAdaptBitrateProperty = serializedObject.FindProperty("adaptativeBitrate");


        ///Advanced Settings Serialized Property
        arlayer = serializedObject.FindProperty("arlayer");
		showarrig = serializedObject.FindProperty("showarrig");
        fadeinonstart = serializedObject.FindProperty("fadeInOnStart");
        greyskybox = serializedObject.FindProperty("greySkybox");
        dontdestroyonload = serializedObject.FindProperty("dontDestroyOnLoad");
        showarrig = serializedObject.FindProperty("showarrig");

    }

    public override void OnInspectorGUI()
    {
        GUIStyle boldfoldout = new GUIStyle(EditorStyles.foldout);
        boldfoldout.fontStyle = FontStyle.Bold;
        //DrawDefaultInspector(); //Draws what you'd normally see in the inspector in absence of a custom inspector. 

        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth * 0.4f ;
        ///////////////////////////////////////////////////////////////
        ///  Inputlayout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        GUIContent cameraIDLabel = new GUIContent("Camera ID", "Camera ID in Plugin. Used in Multicam configuration");
        cameraIDProperty.enumValueIndex = (int)(sl.ZED_CAMERA_ID)EditorGUILayout.EnumPopup(cameraIDLabel, (sl.ZED_CAMERA_ID)cameraIDProperty.enumValueIndex);

        GUIContent cameraDepthModeLabel = new GUIContent("Depth Mode", "Camera depth mode");
        depthModeProperty.enumValueIndex = (int)(sl.DEPTH_MODE)EditorGUILayout.EnumPopup(cameraDepthModeLabel, (sl.DEPTH_MODE)depthModeProperty.enumValueIndex);
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Input Type", GUILayout.Width(EditorGUIUtility.labelWidth));
        GUI.enabled = !Application.isPlaying;
        inputTypeProperty.intValue = GUILayout.Toolbar(inputTypeProperty.intValue, toolbarStrings,GUILayout.ExpandWidth(true));
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        switch (inputTypeProperty.intValue)
        {
            case 0:
                GUIContent cameraResolutionLabel = new GUIContent("Resolution", "Camera resolution");
                GUI.enabled = !Application.isPlaying;
                usbResolutionProperty.enumValueIndex = (int)(sl.RESOLUTION)EditorGUILayout.EnumPopup(cameraResolutionLabel, (sl.RESOLUTION)usbResolutionProperty.enumValueIndex);
                GUI.enabled = true;
                GUIContent cameraFPSLabel = new GUIContent("FPS", "Camera FPS");
                GUI.enabled = !Application.isPlaying;
                usbFPSProperty.intValue = EditorGUILayout.IntField(cameraFPSLabel,usbFPSProperty.intValue);
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();

                /* Multicam features have caused the reset button to not work. Will re-implement in future version. 
                if (Application.isPlaying && manager.IsZEDReady && CheckChange()) //Checks if we need to restart the camera.
                {
                    GUILayout.Space(10);
                    GUIStyle orangetext = new GUIStyle(EditorStyles.label);
                    orangetext.normal.textColor = Color.red;
                    orangetext.wordWrap = true;
                    string labeltext = "Settings have changed that require restarting the camera to apply.";
                    Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(labeltext, ""), orangetext);
                    EditorGUI.LabelField(labelrect, labeltext, orangetext);

                    if (GUILayout.Button("Restart Camera"))
                    {
                        manager.Reset(); //Reset the ZED.

                        //Reset the fields now that they're synced.
                        resolution = manager.resolution;
                        depthmode = manager.depthMode;

                    }
                }*/

                break;

            case 1:
                EditorGUILayout.BeginHorizontal();
                GUIContent svoFileNameLabel = new GUIContent("SVO File", "SVO file name");
                GUI.enabled = !Application.isPlaying;
                svoFileNameProperty.stringValue = EditorGUILayout.TextField(svoFileNameLabel, svoFileNameProperty.stringValue);
                GUIContent loadSVOlabel = new GUIContent("...", "Browse for existing SVO file.");
                if (GUILayout.Button(loadSVOlabel, optionsButtonBrowse))
                {
                    svoFileNameProperty.stringValue = EditorUtility.OpenFilePanelWithFilters("Load SVO", "", filters);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                GUIContent svoLoopLabel = new GUIContent("Loop SVO", "Loop SVO when it reaches the end");
                svoLoopProperty.boolValue = EditorGUILayout.Toggle(svoLoopLabel, svoLoopProperty.boolValue);
                GUIContent svoRealTimeModelabel = new GUIContent("Real-Time mode", "Read SVO in real time mode");
                svoRealTimeModeProperty.boolValue = EditorGUILayout.Toggle(svoRealTimeModelabel, svoRealTimeModeProperty.boolValue);
                EditorGUI.BeginChangeCheck();

                GUI.enabled = (manager.NumberFrameMax > 0);
                GUIContent sliderlabel = new GUIContent("Frame ", "SVO playback position");
                currentFrameSVOProperty.intValue = EditorGUILayout.IntSlider(sliderlabel, currentFrameSVOProperty.intValue, 0, maxFrameSVOProperty.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (manager.zedCamera != null)
                    {
                        //If the slider of frame from the SVO has moved, manually grab the frame and update the textures.
                        manager.zedCamera.SetSVOPosition(currentFrameSVOProperty.intValue);
                        if (pauseSVOProperty.boolValue)
                        {
                            manager.NeedNewFrameGrab = true;
                        }
                    }
                }
                GUI.enabled = false;

                if (manager.zedCamera != null)
                    GUI.enabled = manager.zedCamera.IsCameraReady;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                pauseText = pauseSVOProperty.boolValue ? "Resume" : "Pause";
                GUIContent pauselabel = new GUIContent(pauseText, pauseText + pauseTooltip);
                if (GUILayout.Button(pauselabel))
                {
                    pauseSVOProperty.boolValue = !pauseSVOProperty.boolValue;
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();
                break;

            case 2:
                GUIContent streamIPLabel = new GUIContent("IP", "IP of streaming device");
                GUI.enabled = !Application.isPlaying;
                streamIPProperty.stringValue = EditorGUILayout.TextField(streamIPLabel, streamIPProperty.stringValue);
                GUI.enabled = true;
                GUIContent streamPortLabel = new GUIContent("Port", "Port where stream is sent to ");
                GUI.enabled = !Application.isPlaying;
                streamPortProperty.intValue = EditorGUILayout.IntField(streamPortLabel, streamPortProperty.intValue);
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();
                break;    
        }
        
        EditorGUI.indentLevel--;
        ///////////////////////////////////////////////////////////////
        ///  Motion Tracking layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Motion Tracking", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        GUIContent enableTrackingLabel = new GUIContent("Enable Tracking", "If enabled, the ZED will move/rotate itself using its own inside-out tracking. " +
        "If false, the camera tracking will move with the VR HMD if connected and available.");
        enableTrackingProperty.boolValue = EditorGUILayout.Toggle(enableTrackingLabel, enableTrackingProperty.boolValue);

        GUIContent enableSMPropertyLabel = new GUIContent("Enable Spatial Memory", "Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment, "
        + "but may cause visible jumps when it happens");
        enableSMProperty.boolValue = EditorGUILayout.Toggle(enableSMPropertyLabel, enableSMProperty.boolValue);

        GUIContent pathSMlabel = new GUIContent("Path Spatial Memory", "If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded. " +
        ".area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan.");
        pathSMProperty.stringValue = EditorGUILayout.TextField(pathSMlabel, pathSMProperty.stringValue);

        GUIContent estimateIPPropertyLabel = new GUIContent("Estimate Initial Position", "Estimate initial position by detecting the floor. Leave it false if using VR Headset");
        estimateIPProperty.boolValue = EditorGUILayout.Toggle(estimateIPPropertyLabel, estimateIPProperty.boolValue);
        EditorGUI.indentLevel--;

        ///////////////////////////////////////////////////////////////
        ///  Rendering layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        GUIContent depthOcclusionPropertyLabel = new GUIContent("Depth Occlusion", "When enabled, the real world can occlude (cover up) virtual objects that are behind it. " +
        "Otherwise, virtual objects will appear in front.");
        depthOcclusionProperty.boolValue = EditorGUILayout.Toggle(depthOcclusionPropertyLabel, depthOcclusionProperty.boolValue);

        GUIContent arpostProcessingProperyLabel = new GUIContent("AR Post-Processing", "Enables post-processing effects on virtual objects that blends them in with the real world.");
        arpostProcessingPropery.boolValue = EditorGUILayout.Toggle(arpostProcessingProperyLabel, arpostProcessingPropery.boolValue);

        GUIContent camBrightnessPropertyLabel = new GUIContent("Camera Brightness", "Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way. " +
        "This is a rendering setting that doesn't affect the raw input from the camera.");
        camBrightnessProperty.intValue = EditorGUILayout.IntSlider("Camera Brightness", camBrightnessProperty.intValue, 0, 100);
        EditorGUI.indentLevel--;

        ///////////////////////////////////////////////////////////////
        ///  Spatial Mapping layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
		showSpatialMapping.boolValue = EditorGUILayout.Foldout(showSpatialMapping.boolValue, "Spatial Mapping", boldfoldout);
		if (showSpatialMapping.boolValue) {
			EditorGUI.indentLevel++;
			bool cameraIsReady = false;

			if (manager)
				cameraIsReady = manager.zedCamera != null ? manager.zedCamera.IsCameraReady : false;

			displayText = manager.IsSpatialMappingDisplay ? "Hide Mesh" : "Display Mesh";

			EditorGUILayout.BeginHorizontal ();
 
			GUILayout.Space(5);
		 
			EditorGUILayout.EndHorizontal ();

			GUIContent resolutionlabel = new GUIContent ("Resolution", "Resolution setting for the scan. " +
			                             "A higher resolution creates more submeshes and uses more memory, but is more accurate.");
			ZEDSpatialMapping.RESOLUTION newResolution = (ZEDSpatialMapping.RESOLUTION)EditorGUILayout.EnumPopup (resolutionlabel, manager.mappingResolutionPreset);
			if (newResolution != manager.mappingResolutionPreset) {
				mappingResolution.enumValueIndex = (int)newResolution;
				serializedObject.ApplyModifiedProperties ();
			}

			GUIContent rangelabel = new GUIContent ("Range", "Maximum distance geometry can be from the camera to be scanned. " +
			                        "Geometry scanned from farther away will be less accurate.");
			ZEDSpatialMapping.RANGE newRange = (ZEDSpatialMapping.RANGE)EditorGUILayout.EnumPopup (rangelabel, manager.mappingRangePreset);
			if (newRange != manager.mappingRangePreset) {
				range.enumValueIndex = (int)newRange;
				serializedObject.ApplyModifiedProperties ();
			}

			EditorGUILayout.BeginHorizontal ();
			GUIContent filteringlabel = new GUIContent ("Mesh Filtering", "Whether mesh filtering is needed.");
			filterParameters.enumValueIndex = (int)(sl.FILTER)EditorGUILayout.EnumPopup (filteringlabel, (sl.FILTER)filterParameters.enumValueIndex);
			isFilteringEnable.boolValue = true;


			EditorGUILayout.EndHorizontal ();

			GUI.enabled = !manager.IsMappingRunning; //Don't allow changing the texturing setting while the scan is running. 

			GUIContent texturedlabel = new GUIContent ("Texturing", "Whether surface textures will be scanned and applied. " +
			                           "Note that texturing will add further delay to the post-scan finalizing period.");
			isTextured.boolValue = EditorGUILayout.Toggle (texturedlabel, isTextured.boolValue);

			GUI.enabled = cameraIsReady; //Gray out below elements if the ZED hasn't been initialized as you can't yet start a scan. 

			EditorGUILayout.BeginHorizontal ();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (!manager.IsMappingRunning) {
				GUIContent startmappinglabel = new GUIContent ("Start Spatial Mapping", "Begin the spatial mapping process.");
				if (GUILayout.Button (startmappinglabel)) {
					if (!manager.IsSpatialMappingDisplay) {
						manager.SwitchDisplayMeshState (true);
					}
					manager.StartSpatialMapping ();
				}
			} else {
				if (manager.IsMappingRunning && !manager.IsMappingUpdateThreadRunning || manager.IsMappingRunning && manager.IsMappingTexturingRunning) {
					GUILayout.FlexibleSpace ();
					GUIContent finishinglabel = new GUIContent ("Spatial mapping is finishing", "Please wait - the mesh is being processed.");
					GUILayout.Label (finishinglabel);
					Repaint ();
					GUILayout.FlexibleSpace ();
				} else {
					GUIContent stopmappinglabel = new GUIContent ("Stop Spatial Mapping", "Ends spatial mapping and begins processing the final mesh.");
					if (GUILayout.Button (stopmappinglabel)) {
						manager.StopSpatialMapping ();
					}
				}
			}

			EditorGUILayout.EndHorizontal ();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = cameraIsReady;
			string displaytooltip = manager.IsSpatialMappingDisplay ? "Hide the mesh from view." : "Display the hidden mesh.";
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUIContent displaylabel = new GUIContent (displayText, displaytooltip);
			if (GUILayout.Button (displayText)) {
				manager.SwitchDisplayMeshState (!manager.IsSpatialMappingDisplay);
			}
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUIContent clearMesheslabel = new GUIContent ("Clear All Meshes", "Clear all meshes created with the ZED");
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button (clearMesheslabel)) {
				manager.ClearAllMeshes ();
			}
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

			GUILayout.Space(5);
			EditorGUILayout.LabelField("Storage", EditorStyles.boldLabel);
 
			GUIContent savelabel = new GUIContent ("Save Mesh (when finished)", "Whether to save the mesh and .area file when finished scanning.");
			saveWhenOver.boolValue = EditorGUILayout.Toggle (savelabel, saveWhenOver.boolValue);


			EditorGUILayout.BeginHorizontal ();

			GUIContent pathlabel = new GUIContent ("Mesh Path", "Path where the mesh is saved/loaded from. Valid file types are .obj, .ply and .bin.");
			meshPath.stringValue = EditorGUILayout.TextField (pathlabel, meshPath.stringValue);

			GUIContent findfilelabel = new GUIContent ("...", "Browse for an existing .obj, .ply or .bin file.");
			if (GUILayout.Button (findfilelabel, optionsButtonBrowse)) {
				meshPath.stringValue = EditorUtility.OpenFilePanel ("Mesh file", "", "ply,obj,bin");
				serializedObject.ApplyModifiedProperties ();
			}

			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();

			GUILayout.FlexibleSpace ();

			GUI.enabled = System.IO.File.Exists (meshPath.stringValue) && cameraIsReady;
			GUIContent loadlabel = new GUIContent ("Load", "Load an existing mesh and .area file into the scene.");
			if (GUILayout.Button (loadlabel)) {
				manager.LoadMesh (meshPath.stringValue);
			}

			EditorGUILayout.EndHorizontal ();
			GUI.enabled = true;
			EditorGUI.indentLevel--;
	 
		}
		serializedObject.ApplyModifiedProperties ();

        ///////////////////////////////////////////////////////////////
        ///     Recording layout     /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        showRecording.boolValue = EditorGUILayout.Foldout(showRecording.boolValue, "Recording", boldfoldout);
        if (showRecording.boolValue)
        {
            
            EditorGUI.indentLevel++;
            GUILayout.Space(5);
            bool cameraIsReady = false;
            if (manager)
                cameraIsReady = manager.zedCamera != null ? manager.zedCamera.IsCameraReady : false;

            GUIContent svoOutFileNameLabel = new GUIContent("SVO File", "SVO file name");
            svoOutputFileNameProperty.stringValue = EditorGUILayout.TextField(svoOutFileNameLabel, svoOutputFileNameProperty.stringValue, GUILayout.ExpandWidth(true));

            GUIContent svoCompressionModeLabel = new GUIContent("SVO Compression", "SVO Compression mode for the recorded SVO file");
            svoOutputCompressionModeProperty.enumValueIndex = (int)(sl.SVO_COMPRESSION_MODE)EditorGUILayout.EnumPopup(svoCompressionModeLabel, (sl.SVO_COMPRESSION_MODE)svoOutputCompressionModeProperty.enumValueIndex, GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = cameraIsReady;
            string recordLabel = manager.needRecordFrame ? "Stop Recording" : "Start Recording";
            string recordtooltip = manager.needRecordFrame ? "Stop Recording" : "Start Recording";
            GUIContent displaylabel = new GUIContent(recordLabel, recordtooltip);
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button(recordLabel))
            {
                if (manager.needRecordFrame)
                {
                    manager.zedCamera.DisableRecording();
                    manager.needRecordFrame = false;
                }
                else
                {
                   
                    if (manager.zedCamera.EnableRecording(svoOutputFileNameProperty.stringValue, (sl.SVO_COMPRESSION_MODE)svoOutputCompressionModeProperty.enumValueIndex) == sl.ERROR_CODE.SUCCESS)
                       manager.needRecordFrame = true;
                    else
                    {
                        Debug.LogError("Failed to start SVO Recording");
                        manager.needRecordFrame = false;
                    }
                }
            }
            EditorGUI.indentLevel--;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }


        ///////////////////////////////////////////////////////////////
        ///     Streaming Out layout     /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        showStreamingOut.boolValue = EditorGUILayout.Foldout(showStreamingOut.boolValue, "Streaming", boldfoldout);
        if (showStreamingOut.boolValue)
        {
            EditorGUI.indentLevel++;
            GUILayout.Space(5);
            GUIContent streamingOutPropertyLabel = new GUIContent("Enable Streaming Output", "Enable Streaming Output with below settings");
            streamingOutProperty.boolValue = EditorGUILayout.Toggle(streamingOutPropertyLabel, streamingOutProperty.boolValue);

            GUIContent streamingOutCodecPropertyLabel = new GUIContent("Codec", "Codec used for images compression");
            streamingOutCodecProperty.enumValueIndex = (int)(sl.STREAMING_CODEC)EditorGUILayout.EnumPopup(streamingOutCodecPropertyLabel, (sl.STREAMING_CODEC)streamingOutCodecProperty.enumValueIndex);

            GUIContent streamingOutPortPropertyLabel = new GUIContent("Port", "Port where stream is sent to ");
            streamingOutPortProperty.intValue = EditorGUILayout.IntField(streamingOutPortPropertyLabel, streamingOutPortProperty.intValue);

            GUIContent streamingOutBitratePropertyLabel = new GUIContent("Bitrate", "Target Bitrate for the codec");
            streamingOutBitrateProperty.intValue = EditorGUILayout.IntField(streamingOutBitratePropertyLabel, streamingOutBitrateProperty.intValue);

            GUIContent streamingOutGopSizePropertyLabel = new GUIContent("Gop", "Maximum Gop size for the codec");
            streamingOutGopSizeProperty.intValue = EditorGUILayout.IntField(streamingOutGopSizePropertyLabel, streamingOutGopSizeProperty.intValue);

            GUIContent streamingOutAdaptBitratePropertyLabel = new GUIContent("Adaptative Bitrate", "Adaptative bitrate for the codec");
            streamingOutAdaptBitrateProperty.boolValue = EditorGUILayout.Toggle(streamingOutAdaptBitratePropertyLabel, streamingOutAdaptBitrateProperty.boolValue);
            EditorGUI.indentLevel--;
        }


        ///////////////////////////////////////////////////////////////
        ///  Advanced Settings layout  ///////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        showadvanced.boolValue = EditorGUILayout.Foldout(showadvanced.boolValue, "Advanced Settings", boldfoldout);
        if(showadvanced.boolValue)
        {
            EditorGUI.indentLevel++;

            GUILayout.Space(5);

            //Fade In At Start toggle. 
            GUIContent fadeinlabel = new GUIContent("Fade In at Start", "When enabled, makes the ZED image fade in from black when the application starts.");
            fadeinonstart.boolValue = EditorGUILayout.Toggle(fadeinlabel, manager.fadeInOnStart);

            //Grey Skybox toggle.
            GUIContent greyskyboxlabel = new GUIContent("Grey Out Skybox on Start", "True to set the background to a neutral gray when the scene starts.\n\r" +
                "Recommended for AR so that lighting on virtual objects better matches the real world.");
            greyskybox.boolValue = EditorGUILayout.Toggle(greyskyboxlabel, manager.greySkybox);

            //Don't Destroy On Load toggle. 
            GUIContent dontdestroylabel = new GUIContent("Don't Destroy on Load", "When enabled, applies DontDestroyOnLoad() on the ZED rig in Awake(), " + 
                "preserving it between scene transitions.");
            dontdestroyonload.boolValue = EditorGUILayout.Toggle(dontdestroylabel, manager.dontDestroyOnLoad);

            //Show AR Rig toggle. 
            GUIContent showarlabel = new GUIContent("Show Final AR Rig", "Whether to show the hidden camera rig used in stereo AR mode to " +
                "prepare images for HMD output. You normally shouldn't tamper with this rig, but seeing it can be useful for " +
                "understanding how the ZED output works.");
            bool lastshowar = manager.showARRig;
            showarrig.boolValue = EditorGUILayout.Toggle(showarlabel, manager.showARRig);

            if (showarrig.boolValue != lastshowar)
            {
                LayerMask arlayers = (1 << manager.arLayer);

                if (showarrig.boolValue == true)
                {
                    Tools.visibleLayers |= arlayers;
                }
                else
                {
                    Tools.visibleLayers &= ~(arlayers);
                }

                if (manager.zedRigDisplayer != null && Application.isPlaying)
                {
                    manager.zedRigDisplayer.hideFlags = showarrig.boolValue ? HideFlags.None : HideFlags.HideAndDontSave;
                }
            }

            GUILayout.Space(5);

            //Style for the AR layer box. 
            GUIStyle layerboxstyle = new GUIStyle(EditorStyles.numberField);
            layerboxstyle.fixedWidth = 0;
            layerboxstyle.stretchWidth = false;
            layerboxstyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle layerboxstylewarning = new GUIStyle(layerboxstyle);
            layerboxstylewarning.normal.textColor = new Color(.9f, .9f, 0); //Red color if layer number is invalid. 

            GUIStyle layerboxstyleerror = new GUIStyle(layerboxstyle);
            layerboxstyleerror.normal.textColor = new Color(.8f, 0, 0); //Red color if layer number is invalid. 

            GUIContent arlayerlabel = new GUIContent("AR Layer", "Layer that a final, normally-hidden AR rig sees. Used to confine it from the rest of the scene.\r\n " +
                "You can assign this to any empty layer, and multiple ZEDs can share the same layer.");
            arlayer.intValue = EditorGUILayout.IntField(arlayerlabel, manager.arLayer, arlayer.intValue < 32 ? layerboxstyle : layerboxstyleerror);

            //Show an error message if the set layer is invalid.
            GUIStyle errormessagestyle = new GUIStyle(EditorStyles.label);
            errormessagestyle.normal.textColor = layerboxstyleerror.normal.textColor;
            errormessagestyle.wordWrap = true;
            errormessagestyle.fontSize = 10;

            //Show small error message if user set layer to below zero. 
            if (arlayer.intValue < 0)
            {
                string errortext = "Unity layers must be above zero to be visible.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(errortext, ""), errormessagestyle);
                EditorGUI.LabelField(labelrect, errortext, errormessagestyle);
            }

            //Show small error message if user set layer higher than 31, which is invalid because Unity layers only go up to 31. 
            if (arlayer.intValue > 31)
            {
                string errortext = "Unity doesn't support layers above 31.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(errortext, ""), errormessagestyle);
                EditorGUI.LabelField(labelrect, errortext, errormessagestyle);
            }

            //Show warnings if the layer is valid but not recommended. 
            GUIStyle warningmessagestyle = new GUIStyle(EditorStyles.label);
            warningmessagestyle.normal.textColor = layerboxstylewarning.normal.textColor;
            warningmessagestyle.wordWrap = true;
            warningmessagestyle.fontSize = 10;

            //Show small warning message if user set layer to 31, which is technically valid but Unity reserves it for other uses. 
            if (arlayer.intValue == 31)
            {
                string warningext = "Warning: Unity reserves layer 31 for previews in the editor. Assigning to layer 31 can cause conflicts.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(warningext, ""), warningmessagestyle);
                EditorGUI.LabelField(labelrect, warningext, warningmessagestyle);
            }

            //Show small warning message if user set layer to 0
            if (arlayer.intValue == 0)
            {
                string warningext = "Warning: Setting the AR rig to see the Default layer means other objects will be drawn in the background, " +
                    "and in unexpected positions as the AR rig position is not synced with the ZED_Rig_Stereo object.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(warningext, ""), warningmessagestyle);
                EditorGUI.LabelField(labelrect, warningext, warningmessagestyle);
            }

            EditorGUI.indentLevel--;
        }
        
		serializedObject.ApplyModifiedProperties();

	

		///////////////////////////////////////////////////////////////
		///  Camera control layout ///////////////////////////////////
		/////////////////////////////////////////////////////////////
		GUILayout.Space(10);
		showcamcontrol.boolValue = EditorGUILayout.Foldout(showcamcontrol.boolValue, "Camera controls", boldfoldout);
		if (showcamcontrol.boolValue) {
			GUILayout.Space(5);
			EditorGUI.indentLevel++;
			if (manager.zedCamera == null)
				GUI.enabled = false;
			else
				GUI.enabled = true;


			EditorGUI.BeginChangeCheck ();
			brightness = EditorGUILayout.IntSlider ("Brightness", brightness, 0, 8);
			if (EditorGUI.EndChangeCheck ()) {
				if (manager.zedCamera.IsCameraReady)
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.BRIGHTNESS, brightness, false);
			}

			EditorGUI.BeginChangeCheck ();
			contrast = EditorGUILayout.IntSlider ("Contrast", contrast, 0, 8);
			if (EditorGUI.EndChangeCheck ()) {
				if (manager.zedCamera.IsCameraReady)
				manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.CONTRAST, contrast, false);
			}

			EditorGUI.BeginChangeCheck ();
			hue = EditorGUILayout.IntSlider ("Hue", hue, 0, 11);
			if (EditorGUI.EndChangeCheck ()) {
				if (manager.zedCamera.IsCameraReady)
				manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.HUE, hue, false);
			}

			EditorGUI.BeginChangeCheck ();
			saturation = EditorGUILayout.IntSlider ("Saturation", saturation, 0, 8);
			if (EditorGUI.EndChangeCheck ()) {
				if (manager.zedCamera.IsCameraReady)
				manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.SATURATION, saturation, false);
			}

            EditorGUI.BeginChangeCheck();
            ledStatus = EditorGUILayout.Toggle("LED Status", ledStatus, EditorStyles.toggle);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera.IsCameraReady)
                {
                    int lst = ledStatus ? 1 : 0;
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, lst, false);
                }
            }

            EditorGUI.BeginChangeCheck();
			aex_agc_control = EditorGUILayout.Toggle("AEC / AGC ", aex_agc_control, EditorStyles.toggle);
			if (!aex_agc_control && setManualValue && EditorGUI.EndChangeCheck())
			{
				if (manager.zedCamera.IsCameraReady) {
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.GAIN, gain, false);
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.EXPOSURE, exposure, false);
					setManualValue = false;
				}
			}

			if (aex_agc_control && EditorGUI.EndChangeCheck())
			{
				if (manager.zedCamera.IsCameraReady) {
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.GAIN, gain, true);
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.EXPOSURE, exposure, true);
					setManualValue = true;
				}
			}
		
			GUI.enabled = !aex_agc_control;
			EditorGUI.BeginChangeCheck();
			EditorGUI.indentLevel++;
			gain = EditorGUILayout.IntSlider("Gain", gain, 0, 100);

			if (EditorGUI.EndChangeCheck())
			{
				if (!aex_agc_control)
				{
					manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, false);
				}
			}
			EditorGUI.BeginChangeCheck();
			exposure = EditorGUILayout.IntSlider("Exposure", exposure, 0, 100);
			if (EditorGUI.EndChangeCheck ()) {
				if (!aex_agc_control) {
					manager.zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.EXPOSURE, exposure, false);
				}

			}
			if (manager.zedCamera == null)
				GUI.enabled = false;
			else
				GUI.enabled = true;
			
			EditorGUI.indentLevel--;

			EditorGUI.BeginChangeCheck();
			awb_control = EditorGUILayout.Toggle(" AWB ", awb_control, EditorStyles.toggle);
			if (!awb_control && setManualWhiteBalance && EditorGUI.EndChangeCheck())
			{
				if (manager.zedCamera.IsCameraReady)
					manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whitebalance / 100, false);
				setManualWhiteBalance = false;
			}

			if (awb_control && EditorGUI.EndChangeCheck())
			{
				if (manager.zedCamera.IsCameraReady)
					manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whitebalance / 100, true);
				setManualWhiteBalance = true;
			}

			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			GUI.enabled = !awb_control;
			whitebalance = 100 * EditorGUILayout.IntSlider("White balance", whitebalance / 100, 26, 65);
			if (!awb_control && EditorGUI.EndChangeCheck())
			{
				if (manager.zedCamera.IsCameraReady)
					manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whitebalance, false);
			}



			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;


			GUILayout.Space(7);
			if (manager.zedCamera == null)
				GUI.enabled = false;
			else
				GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUIContent camcontrolbuttonreset = new GUIContent("Reset", "Reset camera controls to default");
			if (GUILayout.Button(camcontrolbuttonreset))
			{
				manager.zedCamera.ResetCameraSettings();
				manager.zedCamera.RetrieveCameraSettings ();

				brightness = manager.zedCamera.GetCameraSettings ().Brightness;
				contrast = manager.zedCamera.GetCameraSettings ().Contrast;
				hue = manager.zedCamera.GetCameraSettings ().Hue;
				saturation = manager.zedCamera.GetCameraSettings ().Saturation;

				awb_control = true;
				aex_agc_control = true;
			}

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
		}
		serializedObject.ApplyModifiedProperties();



	    ///////////////////////////////////////////////////////////////
		///  Status layout //////////////////////////////////////////
		/////////////////////////////////////////////////////////////

        serializedObject.ApplyModifiedProperties();

        GUIStyle standardStyle = new GUIStyle(EditorStyles.textField);
        GUIStyle errorStyle = new GUIStyle(EditorStyles.textField);
        errorStyle.normal.textColor = Color.red;


        GUILayout.Space(10);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
		EditorGUI.indentLevel++;
		GUILayout.Space(5);
		GUIContent cameraModellabel = new GUIContent("Camera Model:", "Model of the targeted camera.");
		EditorGUILayout.TextField(cameraModellabel, manager.cameraModel);

		GUIContent cameraSerialNumberlabel = new GUIContent("Camera S/N:", "Serial number of the targeted camera.");
		EditorGUILayout.TextField(cameraSerialNumberlabel, manager.cameraSerialNumber);

		GUIContent cameraFWlabel = new GUIContent("Camera Firmware:", "Firmware of the targeted camera.");
		EditorGUILayout.TextField(cameraFWlabel, manager.cameraFirmware);

        GUIContent sdkversionlabel = new GUIContent("SDK Version:", "Version of the installed ZED SDK.");
        EditorGUILayout.TextField(sdkversionlabel, manager.versionZED);

        GUIContent enginefpslabel = new GUIContent("Engine FPS:", "How many frames per second the engine is rendering.");
        EditorGUILayout.TextField(enginefpslabel, manager.engineFPS);

        GUIContent camerafpslabel = new GUIContent("Camera FPS:", "How many images per second are received from the ZED.");
        EditorGUILayout.TextField(camerafpslabel, manager.cameraFPS);

        GUIContent trackingstatelabel = new GUIContent("Tracking State:", "Whether the ZED's tracking is on, off, or searching (lost position, trying to recover).");
        if (manager.IsCameraTracked || !manager.IsZEDReady) 
			EditorGUILayout.TextField (trackingstatelabel, manager.trackingState, standardStyle);	
		else 
			EditorGUILayout.TextField (trackingstatelabel, manager.trackingState,errorStyle);

        GUIContent hmdlabel = new GUIContent("HMD Device:", "The connected VR headset, if any.");
		if (Application.isPlaying)
			EditorGUILayout.TextField (hmdlabel, manager.HMDDevice);
		else {
			//Detect devices through USB.
			if (sl.ZEDCamera.CheckUSBDeviceConnected(sl.USB_DEVICE.USB_DEVICE_OCULUS))
				EditorGUILayout.TextField (hmdlabel, "Oculus USB Detected");
			else if (sl.ZEDCamera.CheckUSBDeviceConnected(sl.USB_DEVICE.USB_DEVICE_HTC))
				EditorGUILayout.TextField (hmdlabel, "HTC USB Detected");
			else
				EditorGUILayout.TextField (hmdlabel, "-");
		}
		EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();

		//TO REMOVE
        /*GUILayout.Space(20);
        GUIContent camcontrolbuttonlabel = new GUIContent("Open Camera Control", "Opens a window for adjusting camera settings like brightness, gain/exposure, etc.");
        if (GUILayout.Button(camcontrolbuttonlabel))
        {
            EditorWindow.GetWindow(typeof(ZEDCameraSettingsEditor), false, "ZED Camera").Show();
        }*/
    }

    /// <summary>
    /// Check if something has changed that requires restarting the camera.
    /// Used to know if the Restart Camera button and a prompt to press it should be visible. 
    /// </summary>
    /// <returns>True if a setting was changed that won't go into effect until a restart. </returns>
    private bool CheckChange()
    {
        if (resolution != manager.resolution ||
            depthmode != manager.depthMode)
        {
            return true;
        }
        else return false;
    }

    /// <summary>
    /// If the given layer name is equal to the provided string, it clears it. 
    /// Used when a ZED layer is moved to a different layer. 
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="constname"></param>
    private void ClearLayerNameIfNeeded(int layer, string constname)
    {
        if (layer < 0 || layer > 31) return; //Invalid ID. 
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layerNames = tagManager.FindProperty("layers");
        if (layerNames.GetArrayElementAtIndex(layer).stringValue == constname)
        {
            layerNames.GetArrayElementAtIndex(layer).stringValue = "";
            tagManager.ApplyModifiedProperties();
        }


    }

}


