//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector :Add a button to the ZEDCameraSettingsEditor at the end of the panel ZEDManager
/// </summary>
[CustomEditor(typeof(ZEDManager)), CanEditMultipleObjects]
public class ZEDCameraEditor : Editor
{
    ZEDManager manager; 

    //Store copies of ZEDManager's fields to detect changes
    sl.RESOLUTION resolution;
    sl.DEPTH_MODE depthmode;
    bool usespatialmemory;
    bool usedepthocclusion;
    bool usepostprocessing;

    bool pendingchange = false;

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
        DrawDefaultInspector();

        if(GUI.changed)
        {
            pendingchange = CheckChange();
        }

        if (Application.isPlaying && manager.IsZEDReady && pendingchange)
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
                manager.Reset(); //Reset the ZED

                //Reset the fields now that they're synced
                resolution = manager.resolution;
                depthmode = manager.depthMode;
                usespatialmemory = manager.enableSpatialMemory;
                usedepthocclusion = manager.depthOcclusion;
                usepostprocessing = manager.postProcessing;

                pendingchange = false;
            }


        }

        GUILayout.Space(10);

        //Status window
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("SDK Version:", manager.versionZED);
        EditorGUILayout.TextField("Engine FPS:", manager.engineFPS);
        EditorGUILayout.TextField("Camera FPS:", manager.cameraFPS);
        EditorGUILayout.TextField("HMD Device:", manager.HMDDevice);
        EditorGUILayout.TextField("Tracking State:", manager.trackingState);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);
        if (GUILayout.Button("Open Camera Control"))
        {
            EditorWindow.GetWindow(typeof(ZEDCameraSettingsEditor), false, "ZED Camera").Show();
        }

        

    }

    private bool CheckChange()
    {
        if (resolution != manager.resolution ||
            depthmode != manager.depthMode ||
            usespatialmemory != manager.enableSpatialMemory ||
            usedepthocclusion != manager.depthOcclusion ||
            usepostprocessing != manager.postProcessing)
        {
            return true;
        }
        else return false;
    }
}
