﻿//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
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
    private SerializedProperty usbSNProperty;
    private SerializedProperty svoFileNameProperty;
    private SerializedProperty svoLoopProperty;
    private SerializedProperty svoRealTimeModeProperty;
    private SerializedProperty pauseSVOProperty;
    private SerializedProperty currentFrameSVOProperty;
    private SerializedProperty maxFrameSVOProperty;
    private SerializedProperty streamIPProperty;
    private SerializedProperty streamPortProperty;

#if ZED_HDRP
    //SRP Lighting Prop
    private SerializedProperty srpShaderTypeProperty;
    private SerializedProperty selfIlluminationProperty;
    private SerializedProperty applyZEDNormalsProperty;
#endif

    //Tracking Prop
    private SerializedProperty enableTrackingProperty;
    private SerializedProperty enableSMProperty;
    private SerializedProperty pathSMProperty;
    private SerializedProperty floorAsOriginProperty;
    private SerializedProperty gravityAsOriginProperty;
    private SerializedProperty trackingIsStaticProperty;
    private SerializedProperty positionalTrackingModeProperty;

    //Rendering Prop
    private SerializedProperty depthOcclusionProperty;
    private SerializedProperty arpostProcessingPropery;
    private SerializedProperty camBrightnessProperty;

    //Recording Prop
    private SerializedProperty svoOutputFileNameProperty;
    private SerializedProperty svoOutputCompressionModeProperty;
    private SerializedProperty svoOutputBitrateProperty;
    private SerializedProperty svoOutputTargetFPSProperty;
    private SerializedProperty svoOutputTranscodeProperty;


    //Streaming Prop
    private SerializedProperty streamingOutProperty;
    private SerializedProperty streamingOutCodecProperty;
    private SerializedProperty streamingOutPortProperty;
    private SerializedProperty streamingOutBitrateProperty;
    private SerializedProperty streamingOutGopSizeProperty;
    private SerializedProperty streamingOutAdaptBitrateProperty;
    private SerializedProperty streamingOutChunkSizeProperty;
    private SerializedProperty streamingOutTargetFPSProperty;


    //Spatial mapping prop
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
    /// Layout option used to draw the '...' button for opening a File Explorer window to find a mesh file.
    /// </summary>
    private SerializedProperty meshPath;

    //Object Detection Prop
    private SerializedProperty OD_ObjectTracking;
    private SerializedProperty OD_2DMask;
    private SerializedProperty OD_DetectionModel;
    private SerializedProperty OD_MaxRange;
    private SerializedProperty OD_FilteringMode;
    private SerializedProperty OD_PredictionTimeout;
    private SerializedProperty OD_AllowReducedPrecisionInference;
    //Object Detection Runtime Prop
    private SerializedProperty OD_VehicleDetectionConfidence;
    private SerializedProperty OD_PersonDetectionConfidence;
    private SerializedProperty OD_BagDetectionConfidence;
    private SerializedProperty OD_AnimalDetectionConfidence;
    private SerializedProperty OD_ElectronicsDetectionConfidence;
    private SerializedProperty OD_FruitVegetableDetectionConfidence;
    private SerializedProperty OD_SportDetectionConfidence;
    private SerializedProperty OD_PersonFilter;
    private SerializedProperty OD_VehicleFilter;
    private SerializedProperty OD_BagFilter;
    private SerializedProperty OD_AnimalFilter;
    private SerializedProperty OD_ElectronicsFilter;
    private SerializedProperty OD_FruitVegetableFilter;
    private SerializedProperty OD_SportFilter;
    //Body Tracking
    private SerializedProperty BT_ObjectTracking;
    private SerializedProperty BT_2DMask;
    private SerializedProperty BT_DetectionModel;
    private SerializedProperty BT_BodyFitting;
    private SerializedProperty BT_BodyFormat;
    private SerializedProperty BT_BodySelection;
    private SerializedProperty BT_MaxRange;
    private SerializedProperty BT_PredictionTimeout;
    private SerializedProperty BT_AllowReducedPrecisionInference;

    // runtime params
    private SerializedProperty BT_Confidence;
    private SerializedProperty BT_MinimumKPThresh;
    private SerializedProperty BT_SkSmoothing;

    /// <summary>
    /// Layout option used to draw the '...' button for opening a File Explorer window to find a mesh file.
    /// </summary>
    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };
    private GUILayoutOption[] optionsButtonStandard = { /*GUILayout.(EditorGUIUtility.labelWidth)*/};

    SerializedProperty rightDepthProperty;
    SerializedProperty maxDepthProperty;
    SerializedProperty confidenceThresholdProperty;
    SerializedProperty textureConfidenceThresholdProperty;
    SerializedProperty enableSelfCalibrationProperty;
    SerializedProperty enableIMUFusionProperty;
    SerializedProperty opencvCalibFilePath;
    SerializedProperty openTimeoutSecProperty;
    SerializedProperty asyncGrabCameraRecoveryProperty;
    SerializedProperty grabComputeCappingFPSProperty;
    SerializedProperty enableImageValidityCheckProperty;

    // Rendering Prop
    private int arlayer;
    private SerializedProperty showarrig;
    private SerializedProperty fadeinonstart;
    private SerializedProperty dontdestroyonload;
    private SerializedProperty enableImageEnhancementProperty;
    private SerializedProperty enableFillModeProperty;
    SerializedProperty setIMUPrior;
    SerializedProperty allowPassThroughProperty;
    SerializedProperty greyskybox;

    SerializedProperty showadvanced; //Show advanced settings or not.
    SerializedProperty showSpatialMapping;  //Show spatial mapping or not.
    SerializedProperty showObjectDetection; //show object detection settings or not
    SerializedProperty showBodyTracking; //show bodyTracking settings or not
    SerializedProperty showRecording;  //Show recording settings or not. 
    SerializedProperty showStreamingOut;  //Show streaming out settings or not 
    SerializedProperty showcamcontrol; //Show cam control settings or not. 

    // Current value for camera controls
    SerializedProperty videoSettingsInitModeProperty;
    SerializedProperty brightnessProperty;
    SerializedProperty contrastProperty;
    SerializedProperty hueProperty;
    SerializedProperty saturationProperty;
    SerializedProperty autoGainExposureProperty;
    SerializedProperty exposureProperty;
    SerializedProperty gainProperty;
    SerializedProperty autoWhiteBalanceProperty;
    SerializedProperty whitebalanceProperty;
    SerializedProperty sharpnessProperty;
    SerializedProperty gammaProperty;
    SerializedProperty ledStatus;

    //private bool hasLoadedSettings = false;
    /// <summary>
    /// Whether we've set a manual value to gain and exposure or if they're in auto mode.
    /// </summary>
    //private bool setManualValue = true;
    /// <summary>
    /// Whether  we've set a manual value to white balance or if it's in auto mode.
    /// </summary>
    //private bool setManualWhiteBalance = true;

    private string[] toolbarStrings = new string[] { "USB", "SVO", "Stream" };
    private string pauseText = "Pause";
    private string pauseTooltip = " SVO playback or recording."; //Appended to the pause Text to make tooltip text.
    private string[] filters = { "Svo files", "svo,svo2" }; //Filters used for browsing for an SVO.

    private void OnEnable()
    {
        manager = (ZEDManager)target;

        ////////////////////////////////////////////////// FoldOut
        showadvanced = serializedObject.FindProperty("advancedPanelOpen");
        showSpatialMapping = serializedObject.FindProperty("spatialMappingFoldoutOpen");
        showcamcontrol = serializedObject.FindProperty("camControlFoldoutOpen");
        showRecording = serializedObject.FindProperty("recordingFoldoutOpen");
        showStreamingOut = serializedObject.FindProperty("streamingOutFoldoutOpen");
        showObjectDetection = serializedObject.FindProperty("objectDetectionFoldoutOpen");
        showBodyTracking = serializedObject.FindProperty("bodyTrackingFoldoutOpen");



        resolution = manager.resolution;
        depthmode = manager.depthMode;
        usespatialmemory = manager.enableSpatialMemory;

        //Input Serialized Properties
        cameraIDProperty = serializedObject.FindProperty("cameraID");
        depthModeProperty = serializedObject.FindProperty("depthMode");
        inputTypeProperty = serializedObject.FindProperty("inputType");
        usbResolutionProperty = serializedObject.FindProperty("resolution");
        usbFPSProperty = serializedObject.FindProperty("FPS");
        usbSNProperty = serializedObject.FindProperty("serialNumber");
        svoFileNameProperty = serializedObject.FindProperty("svoInputFileName");
        svoLoopProperty = serializedObject.FindProperty("svoLoopBack");
        svoRealTimeModeProperty = serializedObject.FindProperty("svoRealTimeMode");
        streamIPProperty = serializedObject.FindProperty("streamInputIP");
        streamPortProperty = serializedObject.FindProperty("streamInputPort");
        pauseSVOProperty = serializedObject.FindProperty("pauseSVOReading");
        currentFrameSVOProperty = serializedObject.FindProperty("currentFrame");
        maxFrameSVOProperty = serializedObject.FindProperty("numberFrameMax");

#if ZED_HDRP
        //SRP Lighting Serialized Property
        srpShaderTypeProperty = serializedObject.FindProperty("srpShaderType");
        selfIlluminationProperty = serializedObject.FindProperty("selfIllumination");
        applyZEDNormalsProperty = serializedObject.FindProperty("applyZEDNormals");
#endif

        //Tracking Serialized Properties
        enableTrackingProperty = serializedObject.FindProperty("enableTracking");
        enableSMProperty = serializedObject.FindProperty("enableSpatialMemory");
        pathSMProperty = serializedObject.FindProperty("pathSpatialMemory");
        floorAsOriginProperty = serializedObject.FindProperty("setFloorAsOrigin");
        gravityAsOriginProperty = serializedObject.FindProperty("setGravityAsOrigin");
        trackingIsStaticProperty = serializedObject.FindProperty("trackingIsStatic");
        positionalTrackingModeProperty = serializedObject.FindProperty("positionalTrackingMode");


        ///Rendering Serialized Properties
        depthOcclusionProperty = serializedObject.FindProperty("depthOcclusion");
        arpostProcessingPropery = serializedObject.FindProperty("postProcessing");
        camBrightnessProperty = serializedObject.FindProperty("m_cameraBrightness");

        ///Spatial Mapping Serialized Properties
        range = serializedObject.FindProperty("mappingRangePreset");
        mappingResolution = serializedObject.FindProperty("mappingResolutionPreset");
        isFilteringEnable = serializedObject.FindProperty("isMappingFilteringEnable");
        filterParameters = serializedObject.FindProperty("meshFilterParameters");
        isTextured = serializedObject.FindProperty("isMappingTextured");
        saveWhenOver = serializedObject.FindProperty("saveMeshWhenOver");
        meshPath = serializedObject.FindProperty("meshPath");

        ///Object Detection Serialized Properties
        OD_ObjectTracking = serializedObject.FindProperty("objectDetectionTracking");

        OD_2DMask = serializedObject.FindProperty("objectDetection2DMask");
        OD_DetectionModel = serializedObject.FindProperty("objectDetectionModel");
        OD_MaxRange = serializedObject.FindProperty("objectDetectionMaxRange");
        OD_FilteringMode = serializedObject.FindProperty("objectDetectionFilteringMode");

        OD_PredictionTimeout = serializedObject.FindProperty("objectDetectionPredictionTimeout");
        OD_AllowReducedPrecisionInference = serializedObject.FindProperty("objectDetectionAllowReducedPrecisionInference");

        OD_PersonDetectionConfidence = serializedObject.FindProperty("OD_personDetectionConfidenceThreshold");
        OD_VehicleDetectionConfidence = serializedObject.FindProperty("OD_vehicleDetectionConfidenceThreshold");
        OD_BagDetectionConfidence = serializedObject.FindProperty("OD_bagDetectionConfidenceThreshold");
        OD_AnimalDetectionConfidence = serializedObject.FindProperty("OD_animalDetectionConfidenceThreshold");
        OD_ElectronicsDetectionConfidence = serializedObject.FindProperty("OD_electronicsDetectionConfidenceThreshold");
        OD_FruitVegetableDetectionConfidence = serializedObject.FindProperty("OD_fruitVegetableDetectionConfidenceThreshold");
        OD_SportDetectionConfidence = serializedObject.FindProperty("OD_sportDetectionConfidenceThreshold");
        OD_PersonFilter = serializedObject.FindProperty("objectClassPersonFilter");
        OD_VehicleFilter = serializedObject.FindProperty("objectClassVehicleFilter");
        OD_BagFilter = serializedObject.FindProperty("objectClassBagFilter");
        OD_AnimalFilter = serializedObject.FindProperty("objectClassAnimalFilter");
        OD_ElectronicsFilter = serializedObject.FindProperty("objectClassElectronicsFilter");
        OD_FruitVegetableFilter = serializedObject.FindProperty("objectClassFruitVegetableFilter");
        OD_SportFilter = serializedObject.FindProperty("objectClassSportFilter");

        // Body tracking serialied properties

        BT_ObjectTracking = serializedObject.FindProperty("bodyTrackingTracking");
        BT_2DMask = serializedObject.FindProperty("bodyTracking2DMask");
        BT_DetectionModel = serializedObject.FindProperty("bodyTrackingModel");
        BT_MaxRange = serializedObject.FindProperty("bodyTrackingMaxRange");
        BT_PredictionTimeout = serializedObject.FindProperty("bodyTrackingPredictionTimeout");
        BT_AllowReducedPrecisionInference = serializedObject.FindProperty("bodyTrackingAllowReducedPrecisionInference");
        BT_MinimumKPThresh = serializedObject.FindProperty("bodyTrackingMinimumKPThreshold");
        BT_BodyFitting = serializedObject.FindProperty("bodyFitting");
        BT_BodyFormat = serializedObject.FindProperty("bodyFormat");
        BT_BodySelection = serializedObject.FindProperty("bodySelection");

        BT_Confidence = serializedObject.FindProperty("bodyTrackingConfidenceThreshold");
        BT_SkSmoothing = serializedObject.FindProperty("bodyTrackingSkeletonSmoothing");
        //Recording Serialized Properties
        svoOutputFileNameProperty = serializedObject.FindProperty("svoOutputFileName");
        svoOutputCompressionModeProperty = serializedObject.FindProperty("svoOutputCompressionMode");
        svoOutputBitrateProperty = serializedObject.FindProperty("svoOutputBitrate");
        svoOutputTargetFPSProperty = serializedObject.FindProperty("svoOutputTargetFPS");
        svoOutputTranscodeProperty = serializedObject.FindProperty("svoOutputTranscodeStreaming");


        streamingOutProperty = serializedObject.FindProperty("enableStreaming");
        streamingOutCodecProperty = serializedObject.FindProperty("streamingCodec");
        streamingOutPortProperty = serializedObject.FindProperty("streamingPort");
        streamingOutBitrateProperty = serializedObject.FindProperty("bitrate");
        streamingOutGopSizeProperty = serializedObject.FindProperty("gopSize");
        streamingOutAdaptBitrateProperty = serializedObject.FindProperty("adaptativeBitrate");
        streamingOutChunkSizeProperty = serializedObject.FindProperty("chunkSize");
        streamingOutTargetFPSProperty = serializedObject.FindProperty("streamingTargetFramerate");



        ///Advanced Settings Serialized Properties
        arlayer = ZEDLayers.arlayer;
        showarrig = serializedObject.FindProperty("showarrig");
        fadeinonstart = serializedObject.FindProperty("fadeInOnStart");
        greyskybox = serializedObject.FindProperty("greySkybox");
        dontdestroyonload = serializedObject.FindProperty("dontDestroyOnLoad");
        showarrig = serializedObject.FindProperty("showarrig");
        rightDepthProperty = serializedObject.FindProperty("enableRightDepthMeasure");
        maxDepthProperty = serializedObject.FindProperty("m_maxDepthRange");
        confidenceThresholdProperty = serializedObject.FindProperty("m_confidenceThreshold");
        textureConfidenceThresholdProperty = serializedObject.FindProperty("m_textureConfidenceThreshold");
        enableSelfCalibrationProperty = serializedObject.FindProperty("enableSelfCalibration");
        enableIMUFusionProperty = serializedObject.FindProperty("enableIMUFusion");
        allowPassThroughProperty = serializedObject.FindProperty("allowARPassThrough");
        setIMUPrior = serializedObject.FindProperty("setIMUPriorInAR");
        enableImageEnhancementProperty = serializedObject.FindProperty("enableImageEnhancement");
        enableFillModeProperty = serializedObject.FindProperty("enableFillMode");
        opencvCalibFilePath = serializedObject.FindProperty("opencvCalibFile");
        openTimeoutSecProperty = serializedObject.FindProperty("openTimeoutSec");
        asyncGrabCameraRecoveryProperty = serializedObject.FindProperty("asyncGrabCameraRecovery");
        grabComputeCappingFPSProperty = serializedObject.FindProperty("grabComputeCappingFPS");
        enableImageValidityCheckProperty = serializedObject.FindProperty("enableImageValidityCheck");

        //Video Settings Serialized Properties
        videoSettingsInitModeProperty = serializedObject.FindProperty("videoSettingsInitMode");

        brightnessProperty = serializedObject.FindProperty("videoBrightness"); ;
        contrastProperty = serializedObject.FindProperty("videoContrast"); ;
        hueProperty = serializedObject.FindProperty("videoHue"); ;
        saturationProperty = serializedObject.FindProperty("videoSaturation"); ;
        autoGainExposureProperty = serializedObject.FindProperty("videoAutoGainExposure"); ;
        gainProperty = serializedObject.FindProperty("videoGain"); ;
        exposureProperty = serializedObject.FindProperty("videoExposure"); ;
        autoWhiteBalanceProperty = serializedObject.FindProperty("videoAutoWhiteBalance"); ;
        whitebalanceProperty = serializedObject.FindProperty("videoWhiteBalance"); ;
        sharpnessProperty = serializedObject.FindProperty("videoSharpness");
        gammaProperty = serializedObject.FindProperty("videoGamma");
        ledStatus = serializedObject.FindProperty("videoLEDStatus"); ;
    }

    public override void OnInspectorGUI()
    {
        GUIStyle boldfoldout = new GUIStyle(EditorStyles.foldout);
        boldfoldout.fontStyle = FontStyle.Bold;
        //DrawDefaultInspector(); //Draws what you'd normally see in the inspector in absence of a custom inspector.

        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth * 0.4f;
        ///////////////////////////////////////////////////////////////
        ///  Inputlayout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;
        GUIContent cameraIDLabel = new GUIContent("Camera ID", "Which ZED camera to connect to. Used when multiple ZED cameras are connected to this device.");
        cameraIDProperty.enumValueIndex = (int)(sl.ZED_CAMERA_ID)EditorGUILayout.EnumPopup(cameraIDLabel, (sl.ZED_CAMERA_ID)cameraIDProperty.enumValueIndex);

        GUIContent cameraDepthModeLabel = new GUIContent("Depth Mode", "Camera depth mode. Higher values increase quality at the cost of performance.");
        depthModeProperty.enumValueIndex = (int)(sl.DEPTH_MODE)EditorGUILayout.EnumPopup(cameraDepthModeLabel, (sl.DEPTH_MODE)depthModeProperty.enumValueIndex);
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        GUIContent inputTypeLabel = new GUIContent("Input Type", "Where the ZED video feed comes from.\r\n\n" +
            "- USB: A live ZED camera connected to this device.\r\n\n- SVO: A video file recorded from a ZED previously.\r\n\n" +
            "- Stream: A live ZED camera connected to a device elsewhere on the network.");
        EditorGUILayout.LabelField(inputTypeLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
        GUI.enabled = !Application.isPlaying;
        inputTypeProperty.intValue = GUILayout.Toolbar(inputTypeProperty.intValue, toolbarStrings, GUILayout.ExpandWidth(true));
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        switch (inputTypeProperty.intValue)
        {
            case 0:
                GUIContent cameraResolutionLabel = new GUIContent("Resolution", "Camera resolution.");
                //GUI.enabled = !Application.isPlaying;
                usbResolutionProperty.enumValueIndex = (int)(sl.USB_RESOLUTION)EditorGUILayout.EnumPopup(cameraResolutionLabel, (sl.USB_RESOLUTION)usbResolutionProperty.enumValueIndex);
                //GUI.enabled = true;
                GUIContent cameraFPSLabel = new GUIContent("FPS", "Desired camera FPS. Maximum FPS depends on your resolution setting:\r\n\n" +
                    "- HD2k: 15FPS\r\n\n- HD1080: 30FPS\r\n\n- HD720p: 60FPS\r\n\n- VGA: 100FPS");
                GUI.enabled = !Application.isPlaying;
                usbFPSProperty.intValue = EditorGUILayout.IntField(cameraFPSLabel, usbFPSProperty.intValue);

                GUIContent cameraSerialNumberLabel = new GUIContent("Serial Number", "Serial number of the camera to open. Leave the SN to 0 to open the camera by ID.");
                GUI.enabled = !Application.isPlaying;
                usbSNProperty.intValue = EditorGUILayout.IntField(cameraSerialNumberLabel, usbSNProperty.intValue);
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();

                //Check if we need to restart the camera, and create a button for the user to do so.
                if (Application.isPlaying && manager.IsZEDReady && CheckChange())
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
                }

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
                GUIContent svoLoopLabel = new GUIContent("Loop SVO", "Loop SVO when it reaches the end.");
                svoLoopProperty.boolValue = EditorGUILayout.Toggle(svoLoopLabel, svoLoopProperty.boolValue);
                GUIContent svoRealTimeModelabel = new GUIContent("Real-Time Mode", "When enabled, the time between frames comes from the actual timestamps of each frame. Otherwise, " +
                    "each frame is read based on the maximum FPS of the recorded resolution (ex. 30FPS for HD1080). Real-Time mode makes playback speed more true, but dropped frames result in pauses.");
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
                    manager.zedCamera.SetSVOPosition(manager.CurrentFrame);
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();
                break;

            case 2:
                GUIContent streamIPLabel = new GUIContent("IP", "IP of the host device with the ZED attached.");
                GUI.enabled = !Application.isPlaying;
                streamIPProperty.stringValue = EditorGUILayout.TextField(streamIPLabel, streamIPProperty.stringValue);
                GUI.enabled = true;
                GUIContent streamPortLabel = new GUIContent("Port", "Port where the ZED stream is sent to.");
                GUI.enabled = !Application.isPlaying;
                streamPortProperty.intValue = EditorGUILayout.IntField(streamPortLabel, streamPortProperty.intValue);
                GUI.enabled = true;
                serializedObject.ApplyModifiedProperties();
                break;
        }

        EditorGUI.indentLevel--;

#if ZED_HDRP
        ///////////////////////////////////////////////////////////////
        ///  HDRP Lighting layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("SRP Lighting", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;

        GUIContent shaderTypeLabel = new GUIContent("Lighting Type", "Defines the type of shader (lit or unlit) that's applied to the canvas object(s) used to display the ZED image. " +
            "Lit takes into account scene lighting - it is the most realistic but harder to configure. Unlit makes the ZED image evenly lit, but lacks lighting/shadow effects.");
        string[] shaderoptions = new string[5] { "Lit", "Unlit", "Lit Greenscreen", "Unlit Greenscreen", "Don't Change"};

        GUI.enabled = !Application.isPlaying;
        srpShaderTypeProperty.enumValueIndex = EditorGUILayout.Popup(shaderTypeLabel, srpShaderTypeProperty.enumValueIndex, shaderoptions);
        GUI.enabled = true;

        if (srpShaderTypeProperty.enumValueIndex == 2 || srpShaderTypeProperty.enumValueIndex == 3)
        {
            GUIStyle greenscreennotestyle = new GUIStyle();
            greenscreennotestyle.normal.textColor = new Color(.7f, .7f, .7f);
            greenscreennotestyle.wordWrap = true;
            greenscreennotestyle.fontSize = 10;
            greenscreennotestyle.fixedWidth = 0;
            greenscreennotestyle.stretchWidth = false;
            greenscreennotestyle.alignment = TextAnchor.MiddleLeft;
            greenscreennotestyle.fontStyle = FontStyle.Italic;

            GUILayout.Space(2);
            EditorGUI.indentLevel++;

            string greenscreennote = "Requires GreenScreenManager component on the ZED rig's Camera objects.";
            Rect gsrect = GUILayoutUtility.GetRect(new GUIContent(greenscreennote, ""), greenscreennotestyle);
            EditorGUI.LabelField(gsrect, greenscreennote, greenscreennotestyle);

            GUILayout.Space(8);
            EditorGUI.indentLevel--;
        }

        if (srpShaderTypeProperty.enumValueIndex == 0 || srpShaderTypeProperty.enumValueIndex == 2)
        {
            GUIContent selfIlluminationLabel = new GUIContent("Self-Illumination", "How much the ZED image should light itself via emission. " +
                "Setting to zero is most realistic, but requires you to emulate the real-world lighting conditions within Unity. Higher settings cause the image " +
                "to be uniformly lit, but light and shadow effects are less visible.");
            selfIlluminationProperty.floatValue = EditorGUILayout.Slider(selfIlluminationLabel, selfIlluminationProperty.floatValue, 0, 1);

            GUIContent applyZEDNormalsLabel = new GUIContent("ZED Normals", "Apply normals map from the ZED SDK. Causes lighting to be calculated based "
                + "on the real-world angle of the geometry, instead of treating the ZED image like a plane. However, the normals map is imperfect and can lead to noise.");
            applyZEDNormalsProperty.boolValue = EditorGUILayout.Toggle(applyZEDNormalsLabel, applyZEDNormalsProperty.boolValue);

        }

        EditorGUI.indentLevel--;
#endif

        ///////////////////////////////////////////////////////////////
        ///  Motion Tracking layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Positional Tracking", EditorStyles.boldLabel);
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

        GUIContent floorAsOriginPropertyLabel = new GUIContent("Set Floor As Origin", "Estimate initial position by detecting the floor. Leave it false if using VR Headset");
        floorAsOriginProperty.boolValue = EditorGUILayout.Toggle(floorAsOriginPropertyLabel, floorAsOriginProperty.boolValue);

        GUIContent gravityAsOriginPropertyLabel = new GUIContent("Set Gravity As Origin", "Whether to override 2 of the 3 rotations from \ref initial_world_transform using the IMU gravity.");
        gravityAsOriginProperty.boolValue = EditorGUILayout.Toggle(gravityAsOriginPropertyLabel, gravityAsOriginProperty.boolValue);

        GUIContent trackingIsStaticPropertyLabel = new GUIContent("Tracking Is Static", "If true, tracking is enabled but doesn't move after initializing. " +
            "Can be useful for stationary cameras where you still need tracking enabled, such as in Object Detection.");
        trackingIsStaticProperty.boolValue = EditorGUILayout.Toggle(trackingIsStaticPropertyLabel, trackingIsStaticProperty.boolValue);

        GUIContent positionalTrackingModePropertyLabel = new GUIContent("Positional Tracking Mode", "Lists the mode of positional tracking that can be used.\r\n " +
                        "- GEN_1 : Default mode, best compromise in performance and accuracy.\r\n" +
                        "- GEN_2 : Next generation of positional tracking, allows better accuracy.");
        positionalTrackingModeProperty.enumValueIndex = (int)(sl.POSITIONAL_TRACKING_MODE)EditorGUILayout.EnumPopup(positionalTrackingModePropertyLabel, (sl.POSITIONAL_TRACKING_MODE)positionalTrackingModeProperty.enumValueIndex);

        EditorGUI.indentLevel--;

        ///////////////////////////////////////////////////////////////
        ///  Spatial Mapping layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        showSpatialMapping.boolValue = EditorGUILayout.Foldout(showSpatialMapping.boolValue, "Spatial Mapping", boldfoldout);
        if (showSpatialMapping.boolValue)
        {
            EditorGUI.indentLevel++;
            bool cameraIsReady = false;

            if (manager)
                cameraIsReady = manager.zedCamera != null ? manager.zedCamera.IsCameraReady : false;

            displayText = manager.IsSpatialMappingDisplay ? "Hide Mesh" : "Display Mesh";

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.EndHorizontal();


            GUIContent resolutionlabel = new GUIContent("Resolution", "Resolution setting for the scan. " +
                                         "A higher resolution creates more submeshes and uses more memory, but is more accurate.");
            ZEDSpatialMapping.RESOLUTION newResolution = (ZEDSpatialMapping.RESOLUTION)EditorGUILayout.EnumPopup(resolutionlabel, manager.mappingResolutionPreset);
            if (newResolution != manager.mappingResolutionPreset)
            {
                mappingResolution.enumValueIndex = (int)newResolution;
                serializedObject.ApplyModifiedProperties();
            }

            GUIContent rangelabel = new GUIContent("Range", "Maximum distance geometry can be from the camera to be scanned. " +
                                    "Geometry scanned from farther away will be less accurate.");
            ZEDSpatialMapping.RANGE newRange = (ZEDSpatialMapping.RANGE)EditorGUILayout.EnumPopup(rangelabel, manager.mappingRangePreset);
            if (newRange != manager.mappingRangePreset)
            {
                range.enumValueIndex = (int)newRange;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.BeginHorizontal();
            GUIContent filteringlabel = new GUIContent("Mesh Filtering", "Whether mesh filtering is needed.");
            filterParameters.enumValueIndex = (int)(sl.FILTER)EditorGUILayout.EnumPopup(filteringlabel, (sl.FILTER)filterParameters.enumValueIndex);
            isFilteringEnable.boolValue = true;


            EditorGUILayout.EndHorizontal();

            GUI.enabled = !manager.IsMappingRunning; //Don't allow changing the texturing setting while the scan is running.

            GUIContent texturedlabel = new GUIContent("Texturing", "Whether surface textures will be scanned and applied. " +
                                       "Note that texturing will add further delay to the post-scan finalizing period.");
            isTextured.boolValue = EditorGUILayout.Toggle(texturedlabel, isTextured.boolValue);

            GUI.enabled = cameraIsReady; //Gray out below elements if the ZED hasn't been initialized as you can't yet start a scan.

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (!manager.IsMappingRunning)
            {
                GUIContent startmappinglabel = new GUIContent("Start Spatial Mapping", "Begin the spatial mapping process.");
                if (GUILayout.Button(startmappinglabel))
                {
                    if (!manager.IsSpatialMappingDisplay)
                    {
                        manager.SwitchDisplayMeshState(true);
                    }
                    manager.StartSpatialMapping();
                }
            }
            else
            {
                if (manager.IsMappingRunning && !manager.IsMappingUpdateThreadRunning || manager.IsMappingRunning && manager.IsMappingTexturingRunning)
                {
                    GUILayout.FlexibleSpace();
                    GUIContent finishinglabel = new GUIContent("Spatial mapping is finishing", "Please wait - the mesh is being processed.");
                    GUILayout.Label(finishinglabel);
                    Repaint();
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUIContent stopmappinglabel = new GUIContent("Stop Spatial Mapping", "Ends spatial mapping and begins processing the final mesh.");
                    if (GUILayout.Button(stopmappinglabel))
                    {
                        manager.StopSpatialMapping();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = cameraIsReady;
            string displaytooltip = manager.IsSpatialMappingDisplay ? "Hide the mesh from view." : "Display the hidden mesh.";
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUIContent displaylabel = new GUIContent(displayText, displaytooltip);
            if (GUILayout.Button(displayText))
            {
                manager.SwitchDisplayMeshState(!manager.IsSpatialMappingDisplay);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUIContent clearMesheslabel = new GUIContent("Clear All Meshes", "Clear all meshes created with the ZED");
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button(clearMesheslabel))
            {
                manager.ClearAllMeshes();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Storage", EditorStyles.boldLabel);

            GUIContent savelabel = new GUIContent("Save Mesh (when finished)", "Whether to save the mesh and .area file when finished scanning.");
            saveWhenOver.boolValue = EditorGUILayout.Toggle(savelabel, saveWhenOver.boolValue);


            EditorGUILayout.BeginHorizontal();

            GUIContent pathlabel = new GUIContent("Mesh Path", "Path where the mesh is saved/loaded from. Valid file types are .obj, .ply and .bin.");
            meshPath.stringValue = EditorGUILayout.TextField(pathlabel, meshPath.stringValue);

            GUIContent findfilelabel = new GUIContent("...", "Browse for an existing .obj, .ply or .bin file.");
            if (GUILayout.Button(findfilelabel, optionsButtonBrowse))
            {
                meshPath.stringValue = EditorUtility.OpenFilePanel("Mesh file", "", "ply,obj,bin");
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.enabled = System.IO.File.Exists(meshPath.stringValue) && cameraIsReady;
            GUIContent loadlabel = new GUIContent("Load", "Load an existing mesh and .area file into the scene.");
            if (GUILayout.Button(loadlabel))
            {
                manager.LoadMesh(meshPath.stringValue);
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            EditorGUI.indentLevel--;

        }
        serializedObject.ApplyModifiedProperties();


        ///////////////////////////////////////////////////////////////
        ///  Object Detection layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);

        showObjectDetection.boolValue = EditorGUILayout.Foldout(showObjectDetection.boolValue, "Object Detection", boldfoldout);
        if (showObjectDetection.boolValue)
        {
            bool cameraIsReady = false;
            if (manager)
                cameraIsReady = manager.zedCamera != null ? manager.zedCamera.IsCameraReady : false;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Detection Parameters", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUI.indentLevel++;

            GUI.enabled = !cameraIsReady || !manager.IsObjectDetectionRunning;

            GUIContent ObjectDetectionModelLabel = new GUIContent("Object Detection Model", "Select one object detection model");
            OD_DetectionModel.enumValueIndex = (int)(sl.OBJECT_DETECTION_MODEL)EditorGUILayout.EnumPopup(ObjectDetectionModelLabel, (sl.OBJECT_DETECTION_MODEL)OD_DetectionModel.enumValueIndex);


            //if (OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_FAST || OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_MEDIUM || OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_ACCURATE
            //    || OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.LAST)
            //{   
            //    GUILayout.Space(10);
            //    GUIStyle orangetext = new GUIStyle(EditorStyles.label);
            //    orangetext.normal.textColor = Color.red;
            //    orangetext.wordWrap = true;
            //    string labeltext = "This detection model is not compatible with the Object detection module. Please use a non-human model";
            //    Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(labeltext, ""), orangetext);
            //    EditorGUI.LabelField(labelrect, labeltext, orangetext);
            //}

            /*            EditorGUI.indentLevel--;

                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Init Parameters", EditorStyles.boldLabel);
                        GUILayout.Space(5);

                        EditorGUI.indentLevel++;*/


            /*            GUIContent OD_ImageSyncModeLabel = new GUIContent("Image Sync", "If enabled, object detection will be computed for each image before the next frame is available, " +
                            "locking the main thread if necessary.\r\n\nRecommended setting is false for real-time applications.");
                        OD_ImageSyncMode.boolValue = EditorGUILayout.Toggle(OD_ImageSyncModeLabel, OD_ImageSyncMode.boolValue);*/

            GUIContent OD_Object2DMaskLabel = new GUIContent("Enable Segmentation", "Whether to calculate 2D masks for each object, showing exactly which pixels within the 2D bounding box are the object.\r\n\n" +
            "Must be on when Object Detection starts. Requires more performance, so do not enable unless needed.");
            OD_2DMask.boolValue = EditorGUILayout.Toggle(OD_Object2DMaskLabel, OD_2DMask.boolValue);

            GUIContent OD_MaxRangeLabel = new GUIContent("Max Detection Range", "Defines a upper depth range for detections.");
            OD_MaxRange.floatValue = EditorGUILayout.Slider(OD_MaxRangeLabel, OD_MaxRange.floatValue, 0, 40.0f);

            GUIContent OD_AllowReducedPrecisionInferenceLabel = new GUIContent("Allow Reduced Precision Inference", "Allow inference to run at a lower precision to improve runtime and memory usage.");
            OD_AllowReducedPrecisionInference.boolValue = EditorGUILayout.Toggle(OD_AllowReducedPrecisionInferenceLabel, OD_AllowReducedPrecisionInference.boolValue);

            GUI.enabled = true;

            EditorGUI.indentLevel--;
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Tracking Parameters", EditorStyles.boldLabel);

            GUILayout.Space(5);

            EditorGUI.indentLevel++;

            GUIContent OD_ObjectTrackingLabel = new GUIContent("Enable Tracking", "Whether to track objects across multiple frames using the ZED's position relative to the floor.\r\n\n" +
            "Requires tracking to be on. It's also recommended to enable Estimate Initial Position to find the floor.");
            OD_ObjectTracking.boolValue = EditorGUILayout.Toggle(OD_ObjectTrackingLabel, OD_ObjectTracking.boolValue);

            GUIContent OD_FilteringModeLabel = new GUIContent("Filtering Mode", "Defines the bounding box preprocessor used.");
            OD_FilteringMode.enumValueIndex = (int)(sl.OBJECT_FILTERING_MODE)EditorGUILayout.EnumPopup(OD_FilteringModeLabel, (sl.OBJECT_FILTERING_MODE)OD_FilteringMode.enumValueIndex);

            GUIContent OD_PredictionTimeoutLabel = new GUIContent("Prediction Timeout", "When an object is not detected anymore, the SDK will predict its positions during a short period of time before its state switched to SEARCHING.");
            OD_PredictionTimeout.floatValue = EditorGUILayout.Slider(OD_PredictionTimeoutLabel, OD_PredictionTimeout.floatValue, 0, 1.0f);

            if (OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_FAST || OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM || OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE)
            {
                GUILayout.Space(10);

                GUIContent OD_personDetectionConfidenceThresholdLabel = new GUIContent("Person Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_PersonDetectionConfidence.intValue = EditorGUILayout.IntSlider(OD_personDetectionConfidenceThresholdLabel, OD_PersonDetectionConfidence.intValue, 1, 99);

                GUIContent vehicleDetectionConfidenceThresholdLabel = new GUIContent("Vehicle Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_VehicleDetectionConfidence.intValue = EditorGUILayout.IntSlider(vehicleDetectionConfidenceThresholdLabel, OD_VehicleDetectionConfidence.intValue, 1, 99);

                GUIContent bagDetectionConfidenceThresholdLabel = new GUIContent("Bag Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_BagDetectionConfidence.intValue = EditorGUILayout.IntSlider(bagDetectionConfidenceThresholdLabel, OD_BagDetectionConfidence.intValue, 1, 99);

                GUIContent animalDetectionConfidenceThresholdLabel = new GUIContent("Animal Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_AnimalDetectionConfidence.intValue = EditorGUILayout.IntSlider(animalDetectionConfidenceThresholdLabel, OD_AnimalDetectionConfidence.intValue, 1, 99);

                GUIContent electronicsDetectionConfidenceThresholdLabel = new GUIContent("Electronics Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_ElectronicsDetectionConfidence.intValue = EditorGUILayout.IntSlider(electronicsDetectionConfidenceThresholdLabel, OD_ElectronicsDetectionConfidence.intValue, 1, 99);

                GUIContent fruitVegetableDetectionConfidenceThresholdLabel = new GUIContent("Fruit and Vegetable Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_FruitVegetableDetectionConfidence.intValue = EditorGUILayout.IntSlider(fruitVegetableDetectionConfidenceThresholdLabel, OD_FruitVegetableDetectionConfidence.intValue, 1, 99);

                GUIContent sportDetectionConfidenceThresholdLabel = new GUIContent("Sport Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_SportDetectionConfidence.intValue = EditorGUILayout.IntSlider(sportDetectionConfidenceThresholdLabel, OD_SportDetectionConfidence.intValue, 1, 99);

                GUILayout.Space(5);

                GUIContent PersonFilterLabel = new GUIContent("Person Filter", "Whether to detect people during object detection.");
                OD_PersonFilter.boolValue = EditorGUILayout.Toggle(PersonFilterLabel, OD_PersonFilter.boolValue);

                GUIContent VehicleFilterLabel = new GUIContent("Vehicle Filter", "Whether to detect vehicles during object detection.");
                OD_VehicleFilter.boolValue = EditorGUILayout.Toggle(VehicleFilterLabel, OD_VehicleFilter.boolValue);

                GUIContent BagFilterLabel = new GUIContent("Bag Filter", "Whether to detect bags during object detection.");
                OD_BagFilter.boolValue = EditorGUILayout.Toggle(BagFilterLabel, OD_BagFilter.boolValue);

                GUIContent AnimalFilterLabel = new GUIContent("Animal Filter", "Whether to detect animals during object detection.");
                OD_AnimalFilter.boolValue = EditorGUILayout.Toggle(AnimalFilterLabel, OD_AnimalFilter.boolValue);

                GUIContent ElectronicsFilterLabel = new GUIContent("Electronics Filter", "Whether to detect electronics devices during object detection.");
                OD_ElectronicsFilter.boolValue = EditorGUILayout.Toggle(ElectronicsFilterLabel, OD_ElectronicsFilter.boolValue);

                GUIContent FruitVegetableFilterLabel = new GUIContent("Fruit and Vegetable Filter", "Whether to detect fruits and vegetablesduring object detection.");
                OD_FruitVegetableFilter.boolValue = EditorGUILayout.Toggle(FruitVegetableFilterLabel, OD_FruitVegetableFilter.boolValue);

                GUIContent SportFilterLabel = new GUIContent("Sport Filter", "Whether to detect sport related objects during object detection.");
                OD_SportFilter.boolValue = EditorGUILayout.Toggle(SportFilterLabel, OD_SportFilter.boolValue);

                EditorGUI.indentLevel--;
            }

            else if (OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.PERSON_HEAD_BOX_FAST)
            {

                EditorGUILayout.LabelField("Runtime Parameters", EditorStyles.boldLabel);
                GUILayout.Space(5);
                EditorGUI.indentLevel++;

                GUIContent OD_personDetectionConfidenceThresholdLabel = new GUIContent("Person head Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +

                "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
                OD_PersonDetectionConfidence.intValue = EditorGUILayout.IntSlider(OD_personDetectionConfidenceThresholdLabel, OD_PersonDetectionConfidence.intValue, 1, 99);
            }
            else if (OD_DetectionModel.enumValueIndex == (int)sl.OBJECT_DETECTION_MODEL.CUSTOM_BOX_OBJECTS)
            {
            }
            else //SKELETON
            {
            }

            GUI.enabled = cameraIsReady;

            GUILayout.Space(10);
            if (!manager.IsObjectDetectionRunning)
            {
                GUIContent startODlabel = new GUIContent("Start Object Detection", "Begin the OD process.");
                if (GUILayout.Button(startODlabel))
                {
                    manager.StartObjectDetection();
                }
            }
            else
            {
                GUIContent stopODlabel = new GUIContent("Stop Object Detection", "Stop the OD process.");
                if (GUILayout.Button(stopODlabel))
                {
                    manager.StopObjectDetection();
                }
            }

            GUI.enabled = true;
        }


        ///////////////////////////////////////////////////////////////
        ///  Body Tracking layout  /////////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);

        showBodyTracking.boolValue = EditorGUILayout.Foldout(showBodyTracking.boolValue, "Body Tracking", boldfoldout);
        if (showBodyTracking.boolValue)
        {
            bool cameraIsReady = false;
            if (manager)
                cameraIsReady = manager.zedCamera != null ? manager.zedCamera.IsCameraReady : false;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Detection Parameters", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUI.indentLevel++;

            GUI.enabled = !cameraIsReady || !manager.IsBodyTrackingRunning;

            GUIContent BodyTrackingModelLabel = new GUIContent("Body Tracking Model", "Select one Body Tracking model (HUMAN_BODY_XXX)");
            BT_DetectionModel.enumValueIndex = (int)(sl.BODY_TRACKING_MODEL)EditorGUILayout.EnumPopup(BodyTrackingModelLabel, (sl.BODY_TRACKING_MODEL)BT_DetectionModel.enumValueIndex);

            //if (BT_DetectionModel.enumValueIndex != (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_FAST && BT_DetectionModel.enumValueIndex != (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_MEDIUM && BT_DetectionModel.enumValueIndex != (int)sl.OBJECT_DETECTION_MODEL.HUMAN_BODY_ACCURATE)
            //{
            //    GUILayout.Space(10);
            //    GUIStyle orangetext = new GUIStyle(EditorStyles.label);
            //    orangetext.normal.textColor = Color.red;
            //    orangetext.wordWrap = true;
            //    string labeltext = "This detection model is not compatible with the Body Tracking module. Please use a human_body_xxx model";
            //    Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(labeltext, ""), orangetext);
            //    EditorGUI.LabelField(labelrect, labeltext, orangetext);
            //}

            GUIContent BT_BodyFormatLabel = new GUIContent("Body Format", "");
            BT_BodyFormat.enumValueIndex = (int)(sl.BODY_FORMAT)EditorGUILayout.EnumPopup(BT_BodyFormatLabel, (sl.BODY_FORMAT)BT_BodyFormat.enumValueIndex);

            GUIContent BT_BodySelectionLabel = new GUIContent("Body Selection", "");
            BT_BodySelection.enumValueIndex = (int)(sl.BODY_KEYPOINTS_SELECTION)EditorGUILayout.EnumPopup(BT_BodySelectionLabel, (sl.BODY_KEYPOINTS_SELECTION)BT_BodySelection.enumValueIndex);

            /*            EditorGUI.indentLevel--;

                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Init Parameters", EditorStyles.boldLabel);
                        GUILayout.Space(5);

                        EditorGUI.indentLevel++;*/

            /*            GUIContent BT_ImageSyncModeLabel = new GUIContent("Image Sync", "If enabled, object detection will be computed for each image before the next frame is available, " +
                            "locking the main thread if necessary.\r\n\nRecommended setting is false for real-time applications.");
                        BT_ImageSyncMode.boolValue = EditorGUILayout.Toggle(BT_ImageSyncModeLabel, BT_ImageSyncMode.boolValue);*/

            /*            GUIContent BT_Object2DMaskLabel = new GUIContent("Enable Segmentation", "Whether to calculate 2D masks for each object, showing exactly which pixels within the 2D bounding box are the object.\r\n\n" +
                        "Must be on when Object Detection starts. Requires more performance, so do not enable unless needed.");
                        BT_2DMask.boolValue = EditorGUILayout.Toggle(BT_Object2DMaskLabel, BT_2DMask.boolValue);*/

            GUIContent BT_MaxRangeLabel = new GUIContent("Max Detection Range", "Defines a upper depth range for detections.");
            BT_MaxRange.floatValue = EditorGUILayout.Slider(BT_MaxRangeLabel, BT_MaxRange.floatValue, 0, 40.0f);

            GUIContent BT_Object2DMaskLabel = new GUIContent("Enable Segmentation", "Whether to calculate 2D masks for each object, showing exactly which pixels within the 2D bounding box are the object.\r\n\n" +
                "Must be on when Object Detection starts. Requires more performance, so do not enable unless needed.");
            BT_2DMask.boolValue = EditorGUILayout.Toggle(BT_Object2DMaskLabel, BT_2DMask.boolValue);

            GUIContent BT_AllowReducedPrecisionInferenceLabel = new GUIContent("Allow Reduced Precision Inference", "Allow inference to run at a lower precision to improve runtime and memory usage.");
            BT_AllowReducedPrecisionInference.boolValue = EditorGUILayout.Toggle(BT_AllowReducedPrecisionInferenceLabel, BT_AllowReducedPrecisionInference.boolValue);

            GUI.enabled = true;

            EditorGUI.indentLevel--;
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Tracking Parameters", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUI.indentLevel++;

            GUIContent BT_ObjectTrackingLabel = new GUIContent("Enable Tracking", "Whether to track objects across multiple frames using the ZED's position relative to the floor.\r\n\n" +
            "Requires tracking to be on. It's also recommended to enable Estimate Initial Position to find the floor.");
            BT_ObjectTracking.boolValue = EditorGUILayout.Toggle(BT_ObjectTrackingLabel, BT_ObjectTracking.boolValue);

            GUIContent BT_PredictionTimeoutLabel = new GUIContent("Prediction Timeout", "When an object is not detected anymore, the SDK will predict its positions during a short period of time before its state switched to SEARCHING.");
            BT_PredictionTimeout.floatValue = EditorGUILayout.Slider(BT_PredictionTimeoutLabel, BT_PredictionTimeout.floatValue, 0, 1.0f);

            GUIContent BodyTrackingMinKeypointsLabel = new GUIContent("Minimum Visible Keypoints", "Minimum number of keypoints tracked by the SDK on a body to report it" +
            "as a body.\r\n\nTweak this value depending on your application. Keep in mind that some parts of the body have a lot of keypoints (e.g. hands and face).");
            BT_MinimumKPThresh.intValue = EditorGUILayout.IntField(BodyTrackingMinKeypointsLabel, BT_MinimumKPThresh.intValue);

            GUIContent BodyTrackingConfidenceLabel = new GUIContent("Confidence Threshold", "Detection sensitivity.Represents how sure the SDK must be that " +
            "an object exists to report it.\r\n\nEx: If the threshold is 80, then only objects where the SDK is 80% sure or greater will appear in the list of detected objects.");
            BT_Confidence.intValue = EditorGUILayout.IntSlider(BodyTrackingConfidenceLabel, BT_Confidence.intValue, 1, 99);

            GUIContent BT_SkSmoothingLabel = new GUIContent("Skeleton Smoothing", "From 0 (no smoothing) to 1 (max smoothing), amount of smoothing applied to the skeleton data during the fitting. Higher values will have more latency, but less jitter.");
            BT_SkSmoothing.floatValue = EditorGUILayout.Slider(BT_SkSmoothingLabel, BT_SkSmoothing.floatValue, 0, 1.0f);

            GUI.enabled = cameraIsReady;

            GUILayout.Space(10);
            if (!manager.IsBodyTrackingRunning)
            {
                GUIContent startBTlabel = new GUIContent("Start Body Tracking", "Begin the BT process.");
                if (GUILayout.Button(startBTlabel))
                {
                    manager.StartBodyTracking();
                }
            }
            else
            {
                GUIContent stopBTlabel = new GUIContent("Stop Body Tracking", "Stop the BT process.");
                if (GUILayout.Button(stopBTlabel))
                {
                    manager.StopBodyTracking();
                }
            }

            GUI.enabled = true;
        }

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

            GUIContent svoOutBitrateLabel = new GUIContent("Bitrate", "Bitrate for H264/5 recording. default : 0 means default values (depends on the resolution)");
            svoOutputBitrateProperty.intValue = EditorGUILayout.IntField(svoOutBitrateLabel, svoOutputBitrateProperty.intValue);

            GUIContent svoOutTargetFPSLabel = new GUIContent("Target FPS", "Target FPS for SVO recording");
            svoOutputTargetFPSProperty.intValue = EditorGUILayout.IntField(svoOutTargetFPSLabel, svoOutputTargetFPSProperty.intValue);

            GUIContent svoOutputTranscodeLabel = new GUIContent("Transcode", "If streaming input, set to false to avoid transcoding (decoding+ re-encoding for SVO). Recommended to leave at false.");
            svoOutputTranscodeProperty.boolValue = EditorGUILayout.Toggle(svoOutputTranscodeLabel, svoOutputTranscodeProperty.boolValue);

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

                    if (manager.zedCamera.EnableRecording(svoOutputFileNameProperty.stringValue, (sl.SVO_COMPRESSION_MODE)svoOutputCompressionModeProperty.enumValueIndex, (int)svoOutputBitrateProperty.intValue, (int)svoOutputTargetFPSProperty.intValue, svoOutputTranscodeProperty.boolValue) == sl.ERROR_CODE.SUCCESS)
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

            GUIContent streamingOutChunkSizePropertyLabel = new GUIContent("Payload", "Chunk size for packet streaming");
            streamingOutChunkSizeProperty.intValue = EditorGUILayout.IntField(streamingOutChunkSizePropertyLabel, streamingOutChunkSizeProperty.intValue);

            GUIContent streamingOutTargetFPSPropertyLabel = new GUIContent("Target FPS", "Target FPS for streaming output");
            streamingOutTargetFPSProperty.intValue = EditorGUILayout.IntField(streamingOutTargetFPSPropertyLabel, streamingOutTargetFPSProperty.intValue);

            EditorGUI.indentLevel--;
        }


        ///////////////////////////////////////////////////////////////
        ///  Advanced Settings layout  ///////////////////////////////
        /////////////////////////////////////////////////////////////
        GUILayout.Space(10);
        showadvanced.boolValue = EditorGUILayout.Foldout(showadvanced.boolValue, "Advanced Settings", boldfoldout);
        if (showadvanced.boolValue)
        {
            EditorGUI.indentLevel++;

            GUILayout.Space(5);

            GUIContent maxDepthPropertyLabel = new GUIContent("Max Depth Range", "Maximum depth at which the camera will display the real world, in meters. " +
                "Pixels further than this value will be invisible.");
            maxDepthProperty.floatValue = EditorGUILayout.Slider(maxDepthPropertyLabel, maxDepthProperty.floatValue, 0f, 40f);



            GUIContent confidenceThresholdPropertyLabel = new GUIContent("Confidence Threshold", "How tolerant the ZED SDK is to low confidence values. Lower values filter more pixels based on stereo matching score.");
            if (Application.isPlaying)
            {
                manager.confidenceThreshold = EditorGUILayout.IntSlider(confidenceThresholdPropertyLabel, manager.confidenceThreshold, 0, 100);
            }
            else
            {
                confidenceThresholdProperty.intValue = EditorGUILayout.IntSlider(confidenceThresholdPropertyLabel, confidenceThresholdProperty.intValue, 0, 100);
            }

            GUIContent textureConfidenceThresholdPropertyLabel = new GUIContent("Texture Confidence Threshold", "How tolerant the ZED SDK is to homogenous block. Lower values filter more pixels based on textureness.");
            if (Application.isPlaying)
            {
                manager.textureConfidenceThreshold = EditorGUILayout.IntSlider(textureConfidenceThresholdPropertyLabel, manager.textureConfidenceThreshold, 0, 100);
            }
            else
            {
                textureConfidenceThresholdProperty.intValue = EditorGUILayout.IntSlider(textureConfidenceThresholdPropertyLabel, textureConfidenceThresholdProperty.intValue, 0, 100);
            }



            GUILayout.Space(12);


            GUIContent enableFillModeLabel = new GUIContent("Enable Fill Mode", "Defines if the depth map should be completed or not, similar to the removed SENSING_MODE::FILL.");
            enableFillModeProperty.boolValue = EditorGUILayout.Toggle(enableFillModeLabel, enableFillModeProperty.boolValue);

            //Enable image enhancement toggle
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            GUIContent imageenhancementlabel = new GUIContent("Image Enhancement", "Whether to enable the new color/gamma curve added to the ZED SDK in v3.0.\r\n" +
                "Exposes more detail in darker regions and removes a slight red bias.");
            enableImageEnhancementProperty.boolValue = EditorGUILayout.Toggle(imageenhancementlabel, manager.enableImageEnhancement);
            EditorGUI.EndDisabledGroup();

            GUIContent enalbeIMUFusionLabel = new GUIContent("Visual-Inertial Tracking", "If true, and you are using a ZED2 or ZED Mini, IMU fusion uses data from the camera's IMU to improve tracking results. ");
            enableIMUFusionProperty.boolValue = EditorGUILayout.Toggle(enalbeIMUFusionLabel, enableIMUFusionProperty.boolValue);

            //Whether to enable the ZED SDK's self-calibration feature. 

            GUIContent enableselfcaliblabel = new GUIContent("Self-Calibration", "If true, the ZED SDK will subtly adjust the ZED's calibration " +
                "during runtime to account for heat and other factors. Reasons to disable this are rare. ");
            enableSelfCalibrationProperty.boolValue = EditorGUILayout.Toggle(enableselfcaliblabel, enableSelfCalibrationProperty.boolValue);

            //Grey Skybox toggle.
            GUIContent greyskyboxlabel = new GUIContent("Grey Out Skybox on Start", "True to set the background to a neutral gray when the scene starts.\n\r" +
                "Recommended for AR so that lighting on virtual objects better matches the real world.");
            greyskybox.boolValue = EditorGUILayout.Toggle(greyskyboxlabel, manager.greySkybox);

            //Don't Destroy On Load toggle.
            GUIContent dontdestroylabel = new GUIContent("Don't Destroy on Load", "When enabled, applies DontDestroyOnLoad() on the ZED rig in Awake(), " +
                "preserving it between scene transitions.");
            dontdestroyonload.boolValue = EditorGUILayout.Toggle(dontdestroylabel, manager.dontDestroyOnLoad);

            GUIContent openCalibPathlabel = new GUIContent("Opencv Calibration File ", "Optional, Set an optional file path where the SDK can find a file containing the calibration information of the camera computed by OpenCV. ");
            opencvCalibFilePath.stringValue = EditorGUILayout.TextField(openCalibPathlabel, opencvCalibFilePath.stringValue);

            GUIContent openTimeoutSecLabel = new GUIContent("Open() Timeout Duration", "Define a timeout in seconds after which an error is reported if the open() command fails.\n" +
                "Set to '-1' to try to open the camera endlessly without returning error in case of failure.\n" +
                "Set to '0' to return error in case of failure at the first attempt.\n" +
                "This parameter only impacts the LIVE mode.");
            openTimeoutSecProperty.floatValue = EditorGUILayout.FloatField(openTimeoutSecLabel, openTimeoutSecProperty.floatValue);

            GUIContent asyncGrabCameraRecoveryLabel = new GUIContent("Enable Async Grab Camera Recovery", "If enabled, if there's an issue with the communication with the camera, the grab() will exit after a short period and return the ERROR_CODE::CAMERA_REBOOTING warning.\n" +
                "The recovery will run in the background until the correct communication is restored." +
                "When disabled, the grab() function is blocking and will return only once the camera communication is restored or the timeout is reached." +
                "Default is disabled.");
            asyncGrabCameraRecoveryProperty.boolValue = EditorGUILayout.Toggle(asyncGrabCameraRecoveryLabel, asyncGrabCameraRecoveryProperty.boolValue);

            GUIContent grabComputeCappingFPSLabel = new GUIContent("Grab Frequency Upper Limit", "This can be useful to get a known constant fixed rate or limit the computation load while keeping a short exposure time by setting a high camera capture framerate.\n" +
                "The value should be inferior to the InitParameters.CameraFPS and strictly positive. It has no effect when reading an SVO file.\n" +
                "This is an upper limit and won't make a difference if the computation is slower than the desired compute capping fps." +
                "Default is 0, which means that the setting is not used.");
            grabComputeCappingFPSProperty.floatValue = EditorGUILayout.FloatField(grabComputeCappingFPSLabel, grabComputeCappingFPSProperty.floatValue);

            GUIContent enableImageValidityCheckLabel = new GUIContent("Enable Image Validity Check", "Enable or disable the image validity verification." +
                "\nThis will perform additional verification on the image to identify corrupted data. This verification is done in the grab function and requires some computations." +
                "\nIf an issue is found, the grab function will output a warning as sl::ERROR_CODE::CORRUPTED_FRAME." +
                "This version currently doesn't detect frame tearing." +
                "\nDefault: disabled");
            enableImageValidityCheckProperty.boolValue = EditorGUILayout.Toggle(enableImageValidityCheckLabel, enableImageValidityCheckProperty.boolValue);

            GUILayout.Space(12);

            EditorGUI.indentLevel--;


            EditorGUILayout.LabelField("AR Passthrough Settings", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUI.indentLevel++;

            GUIContent depthOcclusionPropertyLabel = new GUIContent("Depth Occlusion", "When enabled, the real world can occlude (cover up) virtual objects that are behind it. " +
            "Otherwise, virtual objects will appear in front.");
            depthOcclusionProperty.boolValue = EditorGUILayout.Toggle(depthOcclusionPropertyLabel, depthOcclusionProperty.boolValue);

            GUIContent arpostProcessingProperyLabel = new GUIContent("AR Post-Processing", "Enables post-processing effects on virtual objects that blends them in with the real world.");
            arpostProcessingPropery.boolValue = EditorGUILayout.Toggle(arpostProcessingProperyLabel, arpostProcessingPropery.boolValue);

            GUIContent camBrightnessPropertyLabel = new GUIContent("Camera Brightness", "Brightness of the final real-world image. Default is 100. Lower to darken the environment in a realistic-looking way. " +
            "This is a rendering setting that doesn't affect the raw input from the camera.");
            camBrightnessProperty.intValue = EditorGUILayout.IntSlider(camBrightnessPropertyLabel, camBrightnessProperty.intValue, 0, 100);

            //Style for the AR layer box. 
            GUIStyle layerboxstyle = new GUIStyle(EditorStyles.numberField);
            layerboxstyle.fixedWidth = 30;
            layerboxstyle.stretchWidth = false;
            layerboxstyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle layerboxstylewarning = new GUIStyle(layerboxstyle);
            layerboxstylewarning.normal.textColor = new Color(.9f, .9f, 0); //Red color if layer number is invalid.

            GUIStyle layerboxstyleerror = new GUIStyle(layerboxstyle);
            layerboxstyleerror.normal.textColor = new Color(.8f, 0, 0); //Red color if layer number is invalid.

            GUIContent arlayerlabel = new GUIContent("AR Layer", "Layer that a final, normally-hidden AR rig sees. Used to confine it from the rest of the scene.\r\n " +
                "You can assign this to any empty layer, and multiple ZEDs can share the same layer.");
            arlayer = EditorGUILayout.IntField(arlayerlabel, ZEDLayers.arlayer, arlayer < 32 ? layerboxstyle : layerboxstyleerror);

            //Show an error message if the set layer is invalid.
            GUIStyle errormessagestyle = new GUIStyle(EditorStyles.label);
            errormessagestyle.normal.textColor = layerboxstyleerror.normal.textColor;
            errormessagestyle.wordWrap = true;
            errormessagestyle.fontSize = 10;

            //Show small error message if user set layer to below zero.
            if (arlayer < 0)
            {
                string errortext = "Unity layers must be above zero to be visible.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(errortext, ""), errormessagestyle);
                EditorGUI.LabelField(labelrect, errortext, errormessagestyle);
            }

            //Show small error message if user set layer higher than 31, which is invalid because Unity layers only go up to 31.
            if (arlayer > 31)
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
            if (arlayer == 31)
            {
                string warningext = "Warning: Unity reserves layer 31 for previews in the editor. Assigning to layer 31 can cause conflicts.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(warningext, ""), warningmessagestyle);
                EditorGUI.LabelField(labelrect, warningext, warningmessagestyle);
            }

            //Show small warning message if user set layer to 0
            if (arlayer == 0)
            {
                string warningext = "Warning: Setting the AR rig to see the Default layer means other objects will be drawn in the background, " +
                    "and in unexpected positions as the AR rig position is not synced with the ZED_Rig_Stereo object.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(warningext, ""), warningmessagestyle);
                EditorGUI.LabelField(labelrect, warningext, warningmessagestyle);
            }
            ZEDLayersManager.ClearLayer(ZEDLayers.ID_arlayer);
            ZEDLayersManager.CreateLayer(ZEDLayers.ID_arlayer, arlayer);

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
                    manager.zedRigDisplayer.hideFlags = showarrig.boolValue ? HideFlags.None : HideFlags.HideInHierarchy;
                }
            }

            //GUILayout.Space(12);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            GUIContent rightDepthLabel = new GUIContent("Enable Right Depth", "Whether to enable depth measurements from the right camera. Required for depth effects in AR pass-through, " +
                "but requires performance even if not used.\r\n\n'AUTO' enables it only if a ZEDRenderingPlane component set to the right eye is detected as a child of ZEDManager's " +
                "GameObject (as in the ZED rig prefabs.)");
            rightDepthProperty.enumValueIndex = (int)(ZEDManager.RightDepthEnabledMode)EditorGUILayout.EnumPopup(rightDepthLabel, (ZEDManager.RightDepthEnabledMode)rightDepthProperty.enumValueIndex);

            GUIContent allowPassThroughLabel = new GUIContent("Allow AR Pass-Through", "If true, the ZED rig will enter 'pass-through' mode if it detects a stereo rig - at least " +
                "two cameras as children with ZEDRenderingPlane components, each with a different eye) - and a VR headset is connected. If false, it will never enter pass-through mode.");
            allowPassThroughProperty.boolValue = EditorGUILayout.Toggle(allowPassThroughLabel, allowPassThroughProperty.boolValue);

            //Whether to set the IMU prior in AR passthrough mode.
            GUIContent setimupriorlabel = new GUIContent("Set IMU Prior in AR", "In AR pass-through mode, whether to compare the " +
                "ZED's IMU data against the reported position of the VR headset. This helps compensate for drift and should " +
                "usually be left on. However, in some setups, like when using a custom mount, this can cause tracking errors.");
            setIMUPrior.boolValue = EditorGUILayout.Toggle(setimupriorlabel, manager.setIMUPriorInAR);


            //Fade In At Start toggle. 
            GUIContent fadeinlabel = new GUIContent("Fade In at Start", "When enabled, makes the ZED image fade in from black when the application starts.");
            fadeinonstart.boolValue = EditorGUILayout.Toggle(fadeinlabel, manager.fadeInOnStart);

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();



        ///////////////////////////////////////////////////////////////
        ///  Camera control layout ///////////////////////////////////
        /////////////////////////////////////////////////////////////

        /*//TEST: Try loading starting settings.
        if (Application.isPlaying && manager.zedCamera.IsCameraReady)
        {
            if (!hasLoadedSettings)
            {
                Debug.Log("Loaded settings.");

                LoadCurrentVideoSettings();

                hasLoadedSettings = true;
            }
        }
        else hasLoadedSettings = false;*/


        GUILayout.Space(10);
        showcamcontrol.boolValue = EditorGUILayout.Foldout(showcamcontrol.boolValue, "Camera Controls", boldfoldout);
        if (showcamcontrol.boolValue)
        {
            GUILayout.Space(5);
            EditorGUI.indentLevel++;

            //usbResolutionProperty.enumValueIndex = (int)(sl.RESOLUTION)EditorGUILayout.EnumPopup(cameraResolutionLabel, (sl.RESOLUTION)usbResolutionProperty.enumValueIndex);
            //(ZEDManager.VideoSettingsInitMode)

            GUIContent videoInitModeLabel = new GUIContent("Load From: ", "Where the ZED's settings come from when you start the scene.\r\n\n" +
                "- Custom: Applies settings as set below before runtime.\r\n\n- Load From SDK: Camera will load settings last applied to the ZED. " +
                "May have been from a source outside Unity.\r\n\n- Default: Camera will load default video settings.");
            videoSettingsInitModeProperty.enumValueIndex = (int)(ZEDManager.VideoSettingsInitMode)EditorGUILayout.EnumPopup(videoInitModeLabel,
                (ZEDManager.VideoSettingsInitMode)videoSettingsInitModeProperty.enumValueIndex);

            GUI.enabled = manager.zedCamera != null || videoSettingsInitModeProperty.enumValueIndex == (int)ZEDManager.VideoSettingsInitMode.Custom;


            EditorGUI.BeginChangeCheck();
            brightnessProperty.intValue = EditorGUILayout.IntSlider("Brightness", brightnessProperty.intValue, 0, 8);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, brightnessProperty.intValue);
            }

            EditorGUI.BeginChangeCheck();
            contrastProperty.intValue = EditorGUILayout.IntSlider("Contrast", contrastProperty.intValue, 0, 8);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, contrastProperty.intValue);
            }

            EditorGUI.BeginChangeCheck();
            hueProperty.intValue = EditorGUILayout.IntSlider("Hue", hueProperty.intValue, 0, 11);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, hueProperty.intValue);
            }

            EditorGUI.BeginChangeCheck();
            saturationProperty.intValue = EditorGUILayout.IntSlider("Saturation", saturationProperty.intValue, 0, 8);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, saturationProperty.intValue);
            }

            EditorGUI.BeginChangeCheck();
            sharpnessProperty.intValue = EditorGUILayout.IntSlider("Sharpness", sharpnessProperty.intValue, 0, 8);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SHARPNESS, sharpnessProperty.intValue);
            }

            EditorGUI.BeginChangeCheck();
            gammaProperty.intValue = EditorGUILayout.IntSlider("Gamma", gammaProperty.intValue, 1, 9);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAMMA, gammaProperty.intValue);
            }


            EditorGUI.BeginChangeCheck();
            ledStatus.boolValue = EditorGUILayout.Toggle("LED Status", ledStatus.boolValue, EditorStyles.toggle);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                {
                    int lst = ledStatus.boolValue ? 1 : 0;
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, lst);
                }
            }

            EditorGUI.BeginChangeCheck();
            autoGainExposureProperty.boolValue = EditorGUILayout.Toggle("AEC / AGC ", autoGainExposureProperty.boolValue, EditorStyles.toggle);
            if (Application.isPlaying && manager.zedCamera != null && manager.zedCamera.IsCameraReady)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    if (autoGainExposureProperty.boolValue)
                    {
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AEC_AGC, 1);
                    }
                    else
                    {
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AEC_AGC, 0);

                        int value = 0;
                        sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
                        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAIN, ref value);
                        if (err == sl.ERROR_CODE.SUCCESS) { gainProperty.intValue = value; }
                        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, ref value);
                        if (err == sl.ERROR_CODE.SUCCESS) { exposureProperty.intValue = value; }


                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gainProperty.intValue); //Apply last settings immediately.
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposureProperty.intValue);

                    }
                }

            }

            GUI.enabled = !autoGainExposureProperty.boolValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            gainProperty.intValue = EditorGUILayout.IntSlider("Gain", gainProperty.intValue, 0, 100);

            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && !autoGainExposureProperty.boolValue)
                {
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gainProperty.intValue);
                }
            }
            EditorGUI.BeginChangeCheck();
            exposureProperty.intValue = EditorGUILayout.IntSlider("Exposure", exposureProperty.intValue, 0, 100);
            if (EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady && !autoGainExposureProperty.boolValue)
                {
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposureProperty.intValue);
                }

            }
            GUI.enabled = manager.zedCamera != null || videoSettingsInitModeProperty.enumValueIndex == (int)ZEDManager.VideoSettingsInitMode.Custom;

            EditorGUI.indentLevel--;

            EditorGUI.BeginChangeCheck();
            autoWhiteBalanceProperty.boolValue = EditorGUILayout.Toggle(" AWB ", autoWhiteBalanceProperty.boolValue, EditorStyles.toggle);
            if (Application.isPlaying && manager && manager.zedCamera != null && manager.zedCamera.IsCameraReady)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    if (autoWhiteBalanceProperty.boolValue)
                    {
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AUTO_WHITEBALANCE, 1);
                    }
                    else
                    {
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AUTO_WHITEBALANCE, 0);
                        int value = 0;
                        sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
                        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, ref value);
                        if (err == sl.ERROR_CODE.SUCCESS) { whitebalanceProperty.intValue = value * 100; }
                        manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whitebalanceProperty.intValue / 100);

                    }
                }

            }
            EditorGUI.indentLevel++;
            GUI.enabled = !autoWhiteBalanceProperty.boolValue;
            EditorGUI.BeginChangeCheck();
            whitebalanceProperty.intValue = 100 * EditorGUILayout.IntSlider("White balance", whitebalanceProperty.intValue / 100, 26, 65);
            if (!autoWhiteBalanceProperty.boolValue && EditorGUI.EndChangeCheck())
            {
                if (manager.zedCamera != null && manager.zedCamera.IsCameraReady)
                    manager.zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whitebalanceProperty.intValue);
            }



            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;


            GUILayout.Space(7);
            GUI.enabled = manager.zedCamera != null || videoSettingsInitModeProperty.enumValueIndex == (int)ZEDManager.VideoSettingsInitMode.Custom;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUIContent camcontrolbuttonreset = new GUIContent("Reset to Default", "Reset camera controls to default.");
            if (GUILayout.Button(camcontrolbuttonreset))
            {
                if (Application.isPlaying && manager.zedCamera.IsCameraReady)
                {
                    manager.zedCamera.ResetCameraSettings();
                    LoadCurrentVideoSettings();
                }
                else
                {
                    brightnessProperty.intValue = sl.ZEDCamera.brightnessDefault;
                    contrastProperty.intValue = sl.ZEDCamera.contrastDefault;
                    hueProperty.intValue = sl.ZEDCamera.hueDefault;
                    saturationProperty.intValue = sl.ZEDCamera.saturationDefault;

                    autoGainExposureProperty.boolValue = true;
                    autoWhiteBalanceProperty.boolValue = true;

                    sharpnessProperty.intValue = sl.ZEDCamera.sharpnessDefault;
                    gammaProperty.intValue = sl.ZEDCamera.gammaDefault;
                    ledStatus.boolValue = true;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Space(12);

            GUIContent camRebootButton = new GUIContent("Reboot Camera", "Reboot the camera.");
            if (GUILayout.Button(camRebootButton))
            {
                if (Application.isPlaying && manager.zedCamera.IsCameraReady)
                {
                    manager.Reboot();
                }
            }
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
            EditorGUILayout.TextField(trackingstatelabel, manager.trackingState, standardStyle);
        else
            EditorGUILayout.TextField(trackingstatelabel, manager.trackingState, errorStyle);

        GUIContent odfpslabel = new GUIContent("Obj Detection FPS:", "How many images per second are used for OD");
        EditorGUILayout.TextField(odfpslabel, manager.objectDetectionFPS);

        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// Check if something has changed that requires restarting the camera.
    /// Used to know if the Restart Camera button and a prompt to press it should be visible.
    /// </summary>
    /// <returns>True if a setting was changed that won't go into effect until a restart. </returns>
    private bool CheckChange()
    {
        return resolution != manager.resolution ||
            depthmode != manager.depthMode;
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

    /// <summary>
    /// Loads all current camera video settings from the ZED SDK into the buffer values (brightness, contrast, etc.)
    /// </summary>
    private void LoadCurrentVideoSettings()
    {
        sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
        int value = 0;
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { brightnessProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { contrastProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.HUE, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { hueProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { saturationProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SHARPNESS, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { sharpnessProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAMMA, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { gammaProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAIN, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { gainProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { exposureProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { whitebalanceProperty.intValue = value; }

        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.AEC_AGC, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { autoGainExposureProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.AUTO_WHITEBALANCE, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { autoWhiteBalanceProperty.intValue = value; }
        err = manager.zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, ref value);
        if (err == sl.ERROR_CODE.SUCCESS) { ledStatus.intValue = value; }

    }

}
