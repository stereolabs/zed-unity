//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Contols the ZEDSpatialMapping and hides its implementation
/// </summary>
[DisallowMultipleComponent]
public class ZEDSpatialMappingManager : MonoBehaviour
{



    /// <summary>
    /// Current resolution, a higher resolution create more meshes
    /// </summary>
	public ZEDSpatialMapping.RESOLUTION resolution_preset = ZEDSpatialMapping.RESOLUTION.MEDIUM;

    /// <summary>
    /// The range of the spatial mapping, how far the depth is taken into account
    /// </summary>
	public ZEDSpatialMapping.RANGE range_preset = ZEDSpatialMapping.RANGE.MEDIUM;

    /// <summary>
    /// Flag if filtering is needed
    /// </summary>
    public bool isFilteringEnable = false;

    /// <summary>
    /// Falg is the textures will be created and applied
    /// </summary>
    public bool isTextured = false;

    /// <summary>
    /// Flag to save when spatial mapping is over
    /// </summary>
    public bool saveWhenOver = false;

    /// <summary>
    /// Path to save the .obj and the .area
    /// </summary>
    public string meshPath = "Assets/ZEDMesh.obj";

    /// <summary>
    /// The parameters of filtering
    /// </summary>
    public sl.FILTER filterParameters;

    /// <summary>
    /// The core of spatial mapping
    /// </summary>
    private ZEDSpatialMapping spatialMapping;
    private ZEDManager manager;

    private void Start()
    {
        manager = GameObject.FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        spatialMapping = new ZEDSpatialMapping(transform, sl.ZEDCamera.GetInstance(), manager);
       
    }

    /// <summary>
    /// Is the spatial mapping running
    /// </summary>
    public bool IsRunning {get { return spatialMapping!= null ? spatialMapping.IsRunning(): false; }}

    /// <summary>
    /// List of the submeshes processed, it is filled only when "StopSpatialMapping" is called
    /// </summary>
    public List<ZEDSpatialMapping.Chunk> ChunkList { get { return spatialMapping != null ? spatialMapping.ChunkList : null; } }

    /// <summary>
    /// Is the update thread running, the thread is stopped before the post process
    /// </summary>
    public bool IsUpdateThreadRunning { get { return spatialMapping != null ? spatialMapping.IsUpdateThreadRunning: false; } }

    /// <summary>
    /// Is the spatial mapping process is stopped
    /// </summary>
    public bool IsPaused { get { return spatialMapping != null ? spatialMapping.IsPaused :false; } }

    /// <summary>
    /// Is the texturing is running
    /// </summary>
    public bool IsTexturingRunning { get { return spatialMapping != null ? spatialMapping.IsTexturingRunning : false; } }


    private void OnEnable()
    {
        ZEDSpatialMapping.OnMeshReady += SpatialMappingHasStopped;
    }

    private void OnDisable()
    {
        ZEDSpatialMapping.OnMeshReady -= SpatialMappingHasStopped;
    }

    void SpatialMappingHasStopped()
    {
        if (saveWhenOver)
            SaveMesh(meshPath);
    }

    public void StartSpatialMapping()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        spatialMapping.StartStatialMapping(resolution_preset, range_preset, isTextured);        
    }

    /// <summary>
    /// Stop the current spatial mapping, the current mapping will be processed to add a mesh collider, and events will be called
    /// </summary>
    public void StopSpatialMapping()
    {
        spatialMapping.StopStatialMapping();
    }

    private void Update()
    {
        if (spatialMapping != null)
        {
            spatialMapping.filterParameters = filterParameters;
            spatialMapping.Update();
        }
    }

    private void OnApplicationQuit()
    {
        spatialMapping.Dispose();
    }

    /// <summary>
    /// Display the mesh
    /// </summary>
    /// <param name="state"></param>
    public void SwitchDisplayMeshState(bool state)
    {
        spatialMapping.SwitchDisplayMeshState(state);
    }

    /// <summary>
    /// Pause the computation of the mesh
    /// </summary>
    /// <param name="state"></param>
    public void SwitchPauseState(bool state)
    {
        spatialMapping.SwitchPauseState(state);
    }

    /// <summary>
    /// Save the mesh in a file. The saving will disable the spatial mapping and register and area memory.
    /// May take some time
    /// </summary>
    /// <param name="meshPath"></param>
    /// <returns></returns>
    public bool SaveMesh(string meshPath = "ZEDMeshObj.obj")
    {
        return spatialMapping.SaveMesh(meshPath);
    }

    /// <summary>
    /// Load the mesh from a file
    /// </summary>
    /// <param name="meshPath"></param>
    /// <returns></returns>
    public bool LoadMesh(string meshPath = "ZEDMeshObj.obj")
    {
        manager.gravityRotation = Quaternion.identity;

        spatialMapping.SetMeshRenderer();
        return spatialMapping.LoadMesh(meshPath);
    }

}
#if UNITY_EDITOR


[CustomEditor(typeof(ZEDSpatialMappingManager))]
public class ZEDSpatialMappingEditor : Editor
{
    private ZEDSpatialMappingManager spatialMapping;

    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

    private string displayText = "Hide Mesh";
    private SerializedProperty range;
    private SerializedProperty resolution;
    private SerializedProperty isFilteringEnable;
    private SerializedProperty filterParameters;
    private SerializedProperty saveWhenOver;
    private SerializedProperty isTextured;
    private SerializedProperty meshPath;

    private ZEDSpatialMappingManager Target
    {
        get { return (ZEDSpatialMappingManager)target; }
    }

    public void OnEnable()
    {
        spatialMapping = (ZEDSpatialMappingManager)target;
        range = serializedObject.FindProperty("range_preset");
        resolution = serializedObject.FindProperty("resolution_preset");
        isFilteringEnable = serializedObject.FindProperty("isFilteringEnable");
        filterParameters = serializedObject.FindProperty("filterParameters");
        isTextured = serializedObject.FindProperty("isTextured");
        saveWhenOver = serializedObject.FindProperty("saveWhenOver");
        meshPath = serializedObject.FindProperty("meshPath");
    }



    public override void OnInspectorGUI()
    {

		bool cameraIsReady = sl.ZEDCamera.GetInstance().IsCameraReady;
        displayText = ZEDSpatialMapping.display ? "Hide Mesh" : "Display Mesh";
        serializedObject.Update();
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Mesh Parameters", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        ZEDSpatialMapping.RESOLUTION newResolution = (ZEDSpatialMapping.RESOLUTION)EditorGUILayout.EnumPopup("Resolution", spatialMapping.resolution_preset);
        if (newResolution != spatialMapping.resolution_preset)
        {
            resolution.enumValueIndex = (int)newResolution;
            serializedObject.ApplyModifiedProperties();
        }

        ZEDSpatialMapping.RANGE newRange = (ZEDSpatialMapping.RANGE)EditorGUILayout.EnumPopup("Range", spatialMapping.range_preset);
        if (newRange != spatialMapping.range_preset)
        {
            range.enumValueIndex = (int)newRange;
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.BeginHorizontal();
        filterParameters.enumValueIndex = (int)(sl.FILTER)EditorGUILayout.EnumPopup("Mesh Filtering", (sl.FILTER)filterParameters.enumValueIndex);
		isFilteringEnable.boolValue = true;


        EditorGUILayout.EndHorizontal();

        GUI.enabled = !spatialMapping.IsRunning;

        isTextured.boolValue = EditorGUILayout.Toggle("Texturing", isTextured.boolValue);

        GUI.enabled = cameraIsReady;
        
        EditorGUILayout.BeginHorizontal();
        if (!spatialMapping.IsRunning)
        {
            if (GUILayout.Button("Start Spatial Mapping"))
            {
                if (!ZEDSpatialMapping.display)
                {
                    spatialMapping.SwitchDisplayMeshState(true);
                }
                spatialMapping.StartSpatialMapping();
            }
        }
        else
        {
            if (spatialMapping.IsRunning && !spatialMapping.IsUpdateThreadRunning || spatialMapping.IsRunning && spatialMapping.IsTexturingRunning)

            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Spatial mapping is finishing");
                Repaint();
                GUILayout.FlexibleSpace();
            }
            else
            {
                if (GUILayout.Button("Stop Spatial Mapping"))
                {
                    spatialMapping.StopSpatialMapping();
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = cameraIsReady;
        if (GUILayout.Button(displayText))
        {
            spatialMapping.SwitchDisplayMeshState(!ZEDSpatialMapping.display);
        }
        GUI.enabled = true;
        GUILayout.Label("Mesh Storage", EditorStyles.boldLabel);
		saveWhenOver.boolValue = EditorGUILayout.Toggle("Save Mesh (when stop)", saveWhenOver.boolValue);


        EditorGUILayout.BeginHorizontal();

        meshPath.stringValue = EditorGUILayout.TextField("Mesh Path", meshPath.stringValue);

        if (GUILayout.Button("...", optionsButtonBrowse))
        {
            meshPath.stringValue = EditorUtility.OpenFilePanel("Mesh file", "", "ply,obj,bin");
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        //GUI.enabled = cameraIsReady;
        GUILayout.FlexibleSpace();

        GUI.enabled = System.IO.File.Exists(meshPath.stringValue) && cameraIsReady;
        if (GUILayout.Button("Load"))
        {
            spatialMapping.LoadMesh(meshPath.stringValue);
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        if (!cameraIsReady) Repaint();
    }


}

#endif
