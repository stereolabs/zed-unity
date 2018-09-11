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
    sl.RESOLUTION resolution;
    sl.DEPTH_MODE depthmode;
    bool usespatialmemory;
	bool usedepthocclusion = true;
	bool usepostprocessing = true;

    bool restartneeded = false;
 

    private void OnEnable()
    {
        manager = (ZEDManager)target;

        resolution = manager.resolution;
        depthmode = manager.depthMode;
        usespatialmemory = manager.enableSpatialMemory;
        usedepthocclusion = manager.depthOcclusion;
        usepostprocessing = manager.postProcessing;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); //Draws what you'd normally see in the inspector in absense of a custom inspector. 

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

		GUIStyle standardStyle = new GUIStyle(EditorStyles.textField);
		GUIStyle errorStyle = new GUIStyle(EditorStyles.textField);
		errorStyle.normal.textColor = Color.red;

        GUILayout.Space(10);

        //Status window.
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
