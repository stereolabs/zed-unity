//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// High level interface for the ZED's Spatial Mapping features. Allows you to scan your environment into a 3D mesh.
/// The scan will appear as a set of "chunk" meshes under a GameObject called "[ZED Mesh Holder] created at runtime. 
/// The mesh can be used once scanning is finished after a brief finalizing/filtering/texturing period, and saved into 
/// an .obj, .ply or .bin if desired. It will also get a MeshCollider cadded to it, so that virtual objects can collide with it. 
/// Saving a scan made with this class also saves an .area file in the same location that can be used by the ZED's Spatial Memory
/// feature for better tracking localization. 
/// Most of the spatial mapping implementation is handled in ZEDSpatialMapping.cs, but this class simplifies its use. 
/// For more information and a tutorial, see our documentation: https://docs.stereolabs.com/mixed-reality/unity/spatial-mapping-unity/
/// </summary>
[DisallowMultipleComponent]
public class ZEDSpatialMappingManager : MonoBehaviour
{
    /// <summary>
    /// Resolution setting for the scan. A higher resolution creates more submeshes and uses more memory, but is more accurate.
    /// </summary>
	public ZEDSpatialMapping.RESOLUTION resolution_preset = ZEDSpatialMapping.RESOLUTION.MEDIUM;

    /// <summary>
    /// Maximum distance geometry can be from the camera to be scanned. Geometry scanned from farther away will be less accurate. 
    /// </summary>
	public ZEDSpatialMapping.RANGE range_preset = ZEDSpatialMapping.RANGE.MEDIUM;

    /// <summary>
    /// Whether mesh filtering is needed.
    /// </summary>
    public bool isFilteringEnable = false;

    /// <summary>
    /// Whether surface textures will be scanned and applied. Note that texturing will add further delay to the post-scan finalizing period. 
    /// </summary>
    public bool isTextured = false;

    /// <summary>
    /// Whether to save the mesh .obj and .area files once the scan is finished. 
    /// </summary>
    public bool saveWhenOver = false;

    /// <summary>
    /// Path to save the .obj and .area files. 
    /// </summary>
    public string meshPath = "Assets/ZEDMesh.obj";

    /// <summary>
    /// Filtering setting. More filtering results in fewer faces in the mesh, reducing both file size and accuracy. 
    /// </summary>
    public sl.FILTER filterParameters;

    /// <summary>
    /// Instance of the ZEDSpatialMapping class that handles the actual spatial mapping implementation within Unity. 
    /// </summary>
    private ZEDSpatialMapping spatialMapping;

    /// <summary>
    /// The scene's ZEDManager instance. Usually attached to the ZED rig root object (ZED_Rig_Mono or ZED_Rig_Stereo). 
    /// </summary>
    private ZEDManager manager;

    /// <summary>
    /// Fills the manager reference and instantiates ZEDSpatialMapping.
    /// </summary>
    private void Start()
    {
        manager = ZEDManager.Instance;
        spatialMapping = new ZEDSpatialMapping(transform, sl.ZEDCamera.GetInstance(), manager);
       
    }

    /// <summary>
    /// Whether the spatial mapping is currently scanning. 
    /// </summary>
    public bool IsRunning {get { return spatialMapping!= null ? spatialMapping.IsRunning(): false; }}

    /// <summary>
    /// List of the processed submeshes. This list isn't filled until StopSpatialMapping() is called. 
    /// </summary>
    public List<ZEDSpatialMapping.Chunk> ChunkList { get { return spatialMapping != null ? spatialMapping.ChunkList : null; } }

    /// <summary>
    /// Whether the mesh update thread is running. 
    /// </summary>
    public bool IsUpdateThreadRunning { get { return spatialMapping != null ? spatialMapping.IsUpdateThreadRunning: false; } }

    /// <summary>
    /// Whether the spatial mapping was running but has been paused (not stopped) by the user. 
    /// </summary>
    public bool IsPaused { get { return spatialMapping != null ? spatialMapping.IsPaused :false; } }

    /// <summary>
    /// Whether the mesh is in the texturing stage of finalization. 
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

    /// <summary>
    /// Saves the mesh once it's finished, if saveWhenOver is set to true. 
    /// </summary>
    void SpatialMappingHasStopped()
    {
        if (saveWhenOver)
            SaveMesh(meshPath);
    }

    /// <summary>
    /// Tells ZEDSpatialMapping to begin a new scan. This clears the previous scan from the scene if there is one. 
    /// </summary>
    public void StartSpatialMapping()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        spatialMapping.StartStatialMapping(resolution_preset, range_preset, isTextured);        
    }

    /// <summary>
    /// Ends the current spatial mapping. Once called, the current mesh will be filtered, textured (if enabled) and saved (if enabled), 
    /// and a mesh collider will be added. 
    /// </summary>
    public void StopSpatialMapping()
    {
        spatialMapping.StopStatialMapping();
    }

    /// <summary>
    /// Updates the filtering parameters and call the ZEDSpatialMapping instance's Update() function. 
    /// </summary>
    private void Update()
    {
        if (spatialMapping != null)
        {
            spatialMapping.filterParameters = filterParameters;
            spatialMapping.Update(); //As ZEDSpatialMapping doesn't inherit from Monobehaviour, this doesn't happen automatically. 
        }
    }

    /// <summary>
    /// Properly clears existing scan data when the application is closed. 
    /// </summary>
    private void OnApplicationQuit()
    {
        spatialMapping.Dispose();
    }

    /// <summary>
    /// Toggles whether to display the mesh or not. 
    /// </summary>
    /// <param name="state"><c>True</c> to make the mesh visible, <c>false</c> to make it invisible. </param>
    public void SwitchDisplayMeshState(bool state)
    {
        spatialMapping.SwitchDisplayMeshState(state);
    }

    /// <summary>
    /// Pauses the current scan. 
    /// </summary>
    /// <param name="state"><c>True</c> to pause the scanning, <c>false</c> to unpause it.</param>
    public void SwitchPauseState(bool state)
    {
        spatialMapping.SwitchPauseState(state);
    }

    /// <summary>
    /// Saves the mesh into a 3D model (.obj, .ply or .bin) file. Also saves an .area file for spatial memory for better tracking. 
    /// Calling this will end the spatial mapping if it's running. Note it can take a significant amount of time to finish. 
    /// </summary>
    /// <param name="meshPath">Path where the mesh and .area files will be saved.</param>
    public bool SaveMesh(string meshPath = "ZEDMeshObj.obj")
    {
        return spatialMapping.SaveMesh(meshPath);
    }

    /// <summary>
    /// Loads a mesh and spatial memory data from a file.
    /// If scanning is running, it will be stopped. Existing scans in the scene will be cleared. 
    /// </summary>
    /// <param name="meshPath">Path to the 3D mesh file (.obj, .ply or .bin) to load.</param>
    /// <returns><c>True</c> if successfully loaded, <c>false</c> otherwise.</returns>
    public bool LoadMesh(string meshPath = "ZEDMeshObj.obj")
    {
        //Cache the save setting and set to false, to avoid overwriting the mesh file during the load. 
        bool oldSaveWhenOver = saveWhenOver;
        saveWhenOver = false; 

        manager.gravityRotation = Quaternion.identity;

        spatialMapping.SetMeshRenderer();
        bool loadresult = spatialMapping.LoadMesh(meshPath);

        saveWhenOver = oldSaveWhenOver; //Restoring old setting.
        return loadresult;
    }

}
#if UNITY_EDITOR

/// <summary>
/// Custom Inspector screen for ZEDSpatialMappingManager. 
/// Displays values in an organized manner, and adds buttons for starting/stopping spatial
/// mapping and hiding/displaying the resulting mesh. 
/// </summary>
[CustomEditor(typeof(ZEDSpatialMappingManager))]
public class ZEDSpatialMappingEditor : Editor
{
    /// <summary>
    /// The ZEDSpatialMappingManager component this editor instance is editing. 
    /// </summary>
    private ZEDSpatialMappingManager spatialMapping;

    /// <summary>
    /// Layout option used to draw the '...' button for opening a File Explorer window to find a mesh file. 
    /// </summary>
    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

    /// <summary>
    /// Text on the mesh visibility button. Switches between 'Hide Mesh' and 'Display Mesh'.
    /// </summary>
    private string displayText = "Hide Mesh";

    ///Serialized properties used to apply and save changes. 

    /// <summary>
    /// Serialized version of ZEDSpatialMappingManager's range_preset property. 
    /// </summary>
    private SerializedProperty range;
    /// <summary>
    /// Serialized version of ZEDSpatialMappingManager's resolution_preset property. 
    /// </summary>
    private SerializedProperty resolution;
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

    /// <summary>
    /// Public accessor to the ZEDSpatialMappingManager component this editor instance is editing. 
    /// </summary>
    private ZEDSpatialMappingManager Target
    {
        get { return (ZEDSpatialMappingManager)target; }
    }

    public void OnEnable()
    {
        //Bind the serialized properties to their respective properties in ZEDSpatialMappingManager.
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

        GUIContent resolutionlabel = new GUIContent("Resolution", "Resolution setting for the scan. " +
            "A higher resolution creates more submeshes and uses more memory, but is more accurate.");
        ZEDSpatialMapping.RESOLUTION newResolution = (ZEDSpatialMapping.RESOLUTION)EditorGUILayout.EnumPopup(resolutionlabel, spatialMapping.resolution_preset);
        if (newResolution != spatialMapping.resolution_preset)
        {
            resolution.enumValueIndex = (int)newResolution;
            serializedObject.ApplyModifiedProperties();
        }

        GUIContent rangelabel = new GUIContent("Range", "Maximum distance geometry can be from the camera to be scanned. " + 
            "Geometry scanned from farther away will be less accurate.");
        ZEDSpatialMapping.RANGE newRange = (ZEDSpatialMapping.RANGE)EditorGUILayout.EnumPopup(rangelabel, spatialMapping.range_preset);
        if (newRange != spatialMapping.range_preset)
        {
            range.enumValueIndex = (int)newRange;
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.BeginHorizontal();
        GUIContent filteringlabel = new GUIContent("Mesh Filtering", "Whether mesh filtering is needed.");
        filterParameters.enumValueIndex = (int)(sl.FILTER)EditorGUILayout.EnumPopup(filteringlabel, (sl.FILTER)filterParameters.enumValueIndex);
		isFilteringEnable.boolValue = true;


        EditorGUILayout.EndHorizontal();

        GUI.enabled = !spatialMapping.IsRunning; //Don't allow changing the texturing setting while the scan is running. 

        GUIContent texturedlabel = new GUIContent("Texturing", "Whether surface textures will be scanned and applied. " + 
            "Note that texturing will add further delay to the post-scan finalizing period.");
        isTextured.boolValue = EditorGUILayout.Toggle(texturedlabel, isTextured.boolValue);

        GUI.enabled = cameraIsReady; //Gray out below elements if the ZED hasn't been initialized as you can't yet start a scan. 
        
        EditorGUILayout.BeginHorizontal();
        if (!spatialMapping.IsRunning)
        {
            GUIContent startmappinglabel = new GUIContent("Start Spatial Mapping", "Begin the spatial mapping process.");
            if (GUILayout.Button(startmappinglabel))
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
                    spatialMapping.StopSpatialMapping();
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = cameraIsReady;
        string displaytooltip = ZEDSpatialMapping.display ? "Hide the mesh from view." : "Display the hidden mesh.";
        GUIContent displaylabel = new GUIContent(displayText, displaytooltip);
        if (GUILayout.Button(displayText))
        {
            spatialMapping.SwitchDisplayMeshState(!ZEDSpatialMapping.display);
        }
        GUI.enabled = true;
        GUILayout.Label("Mesh Storage", EditorStyles.boldLabel);
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
            spatialMapping.LoadMesh(meshPath.stringValue);
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        if (!cameraIsReady) Repaint();
    }


}

#endif
