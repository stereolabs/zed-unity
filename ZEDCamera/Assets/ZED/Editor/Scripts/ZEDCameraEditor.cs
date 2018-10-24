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
    bool usedepthocclusion;
    bool usepostprocessing;

    bool restartneeded = false;

    SerializedProperty showadvanced; //Show advanced settings or not. 

    SerializedProperty lefteyelayer;
    SerializedProperty righteyelayer;
    SerializedProperty lefteyelayerfinal;
    SerializedProperty righteyelayerfinal;

    SerializedProperty showarrig;
    SerializedProperty fadeinonstart;
    SerializedProperty dontdestroyonload;

    private void OnEnable()
    {
        manager = (ZEDManager)target;

        resolution = manager.resolution;
        depthmode = manager.depthMode;
        usespatialmemory = manager.enableSpatialMemory;
        usedepthocclusion = manager.depthOcclusion;
        usepostprocessing = manager.postProcessing;

        showadvanced = serializedObject.FindProperty("advancedPanelOpen");

        showarrig = serializedObject.FindProperty("showarrig");

        lefteyelayer = serializedObject.FindProperty("lefteyelayer");
        righteyelayer = serializedObject.FindProperty("righteyelayer");
        lefteyelayerfinal = serializedObject.FindProperty("lefteyelayerfinal");
        righteyelayerfinal = serializedObject.FindProperty("righteyelayerfinal");

        fadeinonstart = serializedObject.FindProperty("fadeInOnStart");
        dontdestroyonload = serializedObject.FindProperty("dontDestroyOnLoad");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); //Draws what you'd normally see in the inspector in absence of a custom inspector. 

        if(GUI.changed)
        {
            restartneeded = CheckChange(); 
        }

        if (Application.isPlaying && manager.IsZEDReady && restartneeded) //Checks if we need to restart the camera.
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
                usespatialmemory = manager.enableSpatialMemory;
                usepostprocessing = manager.postProcessing;

                restartneeded = false;
            }
        }


        //Advanced Settings.
        GUILayout.Space(10);
        GUIStyle boldfoldout = new GUIStyle(EditorStyles.foldout);
        boldfoldout.fontStyle = FontStyle.Bold;
        showadvanced.boolValue = EditorGUILayout.Foldout(showadvanced.boolValue, "Advanced Settings", boldfoldout);
        if(showadvanced.boolValue)
        {
            EditorGUI.indentLevel++;

            GUILayout.Space(5);
            EditorGUILayout.LabelField("ZED Plugin Layers", EditorStyles.boldLabel);

            //Style for the number boxes. 
            GUIStyle layerboxstyle = new GUIStyle(EditorStyles.numberField);
            layerboxstyle.fixedWidth = 0;
            layerboxstyle.stretchWidth = false;
            layerboxstyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle layerboxstyleerror = new GUIStyle(layerboxstyle);
            layerboxstyleerror.normal.textColor = new Color(.8f, 0, 0); //Red color if number is invalid. 


            GUIContent lefteyelayerlabel = new GUIContent("Left Eye Layer", "Layer that the left canvas GameObject " +
                "(showing the image from the left eye) is set to. The right camera in ZED_Rig_Stereo can't see this layer.");
            lefteyelayer.intValue = EditorGUILayout.IntField(lefteyelayerlabel, manager.leftEyeLayer, 
                lefteyelayer.intValue < 32 ? layerboxstyle : layerboxstyleerror);

            GUIContent righteyelayerlabel = new GUIContent("Right Eye Layer", "Layer that the right canvas GameObject " +
                "(showing the image from the right eye) is set to. The left camera in ZED_Rig_Stereo can't see this layer.");
            righteyelayer.intValue = EditorGUILayout.IntField(righteyelayerlabel, manager.rightEyeLayer,
                righteyelayer.intValue < 32 ? layerboxstyle : layerboxstyleerror);

            //Cache current final layers in case we need to unhide their old layers. 
            int oldleftfinal = manager.leftEyeLayerFinal;
            int oldrightfinal = manager.rightEyeLayerFinal;

            GUIContent lefteyefinallayerlabel = new GUIContent("Final Left Eye Layer", "Layer that the final left image canvas "
                + "in the hidden AR rig is set to. Hidden from all ZED cameras except the final left camera.");
            lefteyelayerfinal.intValue = EditorGUILayout.IntField(lefteyefinallayerlabel, manager.leftEyeLayerFinal,
                lefteyelayerfinal.intValue < 32 ? layerboxstyle : layerboxstyleerror);

            GUIContent righteyefinallayerlabel = new GUIContent("Final Right Eye Layer", "Layer that the final right image canvas "
                + "in the hidden AR rig is set to. Hidden from all ZED cameras except the final right camera.");
            righteyelayerfinal.intValue = EditorGUILayout.IntField(righteyefinallayerlabel, manager.rightEyeLayerFinal,
                righteyelayerfinal.intValue < 32 ? layerboxstyle : layerboxstyleerror);

            //If either final eye layer changed, make sure the old layer is made visible. 
            if (oldleftfinal != lefteyelayerfinal.intValue)
            {
                Tools.visibleLayers |= (1 << oldleftfinal);
                if (manager.showARRig) Tools.visibleLayers |= (1 << lefteyelayerfinal.intValue);
                else Tools.visibleLayers &= ~(1 << lefteyelayerfinal.intValue);
            }
            if (oldrightfinal != righteyelayerfinal.intValue)
            {
                Tools.visibleLayers |= (1 << oldrightfinal);
                if (manager.showARRig) Tools.visibleLayers |= (1 << righteyelayerfinal.intValue);
                else Tools.visibleLayers &= ~(1 << righteyelayerfinal.intValue);
            }

            //Show small error message if any of the above values are too big. 
            if(lefteyelayer.intValue > 31 || righteyelayer.intValue > 31 || lefteyelayerfinal.intValue > 31 || righteyelayerfinal.intValue > 31)
            {
                GUIStyle errormessagestyle = new GUIStyle(EditorStyles.label);
                errormessagestyle.normal.textColor = layerboxstyleerror.normal.textColor;
                errormessagestyle.wordWrap = true;
                errormessagestyle.fontSize = 10;

                string errortext = "Unity doesn't support layers above 31.";
                Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(errortext, ""), errormessagestyle);
                EditorGUI.LabelField(labelrect, errortext, errormessagestyle);
            }


            GUILayout.Space(7);

            EditorGUILayout.LabelField("Miscellaneous", EditorStyles.boldLabel);

            //Show AR Rig toggle. 
            GUIContent showarlabel = new GUIContent("Show Final AR Rig", "Whether to show the hidden camera rig used in stereo AR mode to " +
                "prepare images for HMD output. You normally shouldn't tamper with this rig, but seeing it can be useful for " + 
                "understanding how the ZED output works.");
            bool lastshowar = manager.showARRig;
            showarrig.boolValue = EditorGUILayout.Toggle(showarlabel, manager.showARRig);

            if (showarrig.boolValue != lastshowar)
            {
                LayerMask arlayers = (1 << manager.leftEyeLayerFinal);
                arlayers |= (1 << manager.rightEyeLayerFinal);

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

            //Fade In At Start toggle. 
            GUIContent fadeinlabel = new GUIContent("Fade In At Start", "When enabled, makes the ZED image fade in from black when the application starts.");
            fadeinonstart.boolValue = EditorGUILayout.Toggle(fadeinlabel, manager.fadeInOnStart);

            //Don't Destroy On Load toggle. 
            GUIContent dontdestroylable = new GUIContent("Don't Destroy on Load", "When enabled, applies DontDestroyOnLoad() on the ZED rig in Awake(), " + 
                "preserving it between scene transitions.");
            dontdestroyonload.boolValue = EditorGUILayout.Toggle(dontdestroylable, manager.dontDestroyOnLoad);

            EditorGUI.indentLevel--;
        }
        serializedObject.ApplyModifiedProperties();

        //Status window.
        GUIStyle standardStyle = new GUIStyle(EditorStyles.textField);
        GUIStyle errorStyle = new GUIStyle(EditorStyles.textField);
        errorStyle.normal.textColor = Color.red;


        GUILayout.Space(10);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);

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
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);
        GUIContent camcontrolbuttonlabel = new GUIContent("Open Camera Control", "Opens a window for adjusting camera settings like brightness, gain/exposure, etc.");
        if (GUILayout.Button(camcontrolbuttonlabel))
        {
            EditorWindow.GetWindow(typeof(ZEDCameraSettingsEditor), false, "ZED Camera").Show();
        }
    }

    /// <summary>
    /// Check if something has changed that requires restarting the camera.
    /// Used to know if the Restart Camera button and a prompt to press it should be visible. 
    /// </summary>
    /// <returns>True if a setting was changed that won't go into effect until a restart. </returns>
    private bool CheckChange()
    {
        if (resolution != manager.resolution ||
            depthmode != manager.depthMode ||
            usespatialmemory != manager.enableSpatialMemory)
        {
            return true;
        }
        else return false;
    }
}
