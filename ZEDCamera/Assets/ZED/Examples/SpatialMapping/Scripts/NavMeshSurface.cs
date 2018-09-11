//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Takes a mesh supplied by ZEDSpatialMappingManager after a scan and converts it into a 
/// NavMesh at runtime that can be used for AI pathfinding. 
/// If this script is present, the process will happen automatically when a scan is completed. 
/// See the ZED spatial mapping tutorial for more info: https://docs.stereolabs.com/mixed-reality/unity/spatial-mapping-unity/
/// </summary>
[RequireComponent(typeof(ZEDSpatialMappingManager))]
public class NavMeshSurface: MonoBehaviour
{
#if UNITY_5_6_OR_NEWER

    /// <summary>
    /// The ID of the agent type the NavMesh will be built for. 
    /// See available agent types (or create a new one) in Unity's Navigation window. 
    /// </summary>
    [SerializeField]
    public int agentTypeID = 0;

    /// <summary>
    /// List of all NavMeshBuildSource objects the script creates to make the final mesh. 
    /// One is created for each 'chunk' of the scan. 
    /// </summary>
    private List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

    /// <summary>
    /// The final NavMesh. 
    /// </summary>
    private NavMeshData navMesh;

    /// <summary>
    /// Outside bounds of the mesh. Computed after all the chunks have been collected. 
    /// </summary>
    private Bounds bounds;

    /// <summary>
    /// Material used to display the final NavMesh in the editor. 
    /// Normally Mat_ZED_Transparent_NavMesh.
    /// </summary>
    private Material materialTransparent;
#endif
    /// <summary>
    /// Arguments passed by NavMeshSurface's OnNavMeshReady event, so a class that places NPCs
    /// (like EnemyManager) can place said NPC properly. 
    /// </summary>
    public class PositionEventArgs : System.EventArgs
    {
        /// <summary>
        /// The world space position of the center of the new NavMesh.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Whether the NavMesh created is valid for placement (and drawing) purposes. 
        /// </summary>
        public bool valid = false;

        /// <summary>
        /// The ID of the NavMesh's agent type.
        /// </summary>
        public int agentTypeID = 0;
    }
    /// <summary>
    /// Event that gets called when the NavMesh is finished being created. 
    /// </summary>
    public static event System.EventHandler<PositionEventArgs> OnNavMeshReady;

    /// <summary>
    /// Reference to this GameObject's ZEDSpatialMappingManager component. 
    /// </summary>
    private ZEDSpatialMappingManager zedSpatialMapping;

    /// <summary>
    /// Whether to display the NavMesh in the editor. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool hideFlag = false;

    /// <summary>
    /// Whether the NavMesh construction is finished or not. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool isOver = false;

    /// <summary>
    /// The GameObject that represents the NavMesh visually. 
    /// </summary>
    private GameObject navMeshObject;

    /// <summary>
    /// World space position of the final NavMesh. 
    /// </summary>
    private Vector3 navMeshPosition;
    /// <summary>
    /// Public accessor for the world space position of the final NavMesh. 
    /// </summary>
    public Vector3 NavMeshPosition { get { return navMeshObject != null ? navMeshPosition : Vector3.zero; } }

    // Use this for initialization
    void Start()
    {
        zedSpatialMapping = GetComponent<ZEDSpatialMappingManager>();

#if UNITY_5_6_OR_NEWER
        navMesh = new NavMeshData();
        NavMesh.AddNavMeshData(navMesh);

        //Initialize a position for the bounds. 
        bounds = new Bounds(transform.position, new Vector3(30, 30, 30));
        materialTransparent = Resources.Load("Materials/Mat_ZED_Transparent_NavMesh") as Material; //The material applied to the display object. 
#endif
    }

    void OnEnable()
    {
        //Listen for when the spatial mapping starts scanning, and when it's finished. 
        ZEDSpatialMapping.OnMeshReady += MeshIsOver;
        ZEDSpatialMapping.OnMeshStarted += NewNavMesh;
    }

    /// <summary>
    /// Clears the existing display object, if one exists. 
    /// </summary>
    void NewNavMesh()
    {
        if(navMeshObject != null)
        {
            Destroy(navMeshObject); 
        }        
    }

    void OnDisable()
    {
        //Unsubscribe from events, as they can otherwise still fire when this component is disabled. 
        ZEDSpatialMapping.OnMeshReady -= MeshIsOver;
        ZEDSpatialMapping.OnMeshStarted -= NewNavMesh;
    }

    /// <summary>
    /// Called when a new NavMesh has finished being processed. 
    /// Updates several values and calls the OnMeshReady event with the proper arguments. 
    /// </summary>
    void MeshIsOver()
    {
#if UNITY_5_6_OR_NEWER
        UpdateNavMesh();

		//Nav mesh has been built. Clear the sources as we don't need them
		sources.Clear();

		//Draw the nav mesh is possible (triangulation from navmesh has return an area) 
        bool isDrawn = Draw();

#endif
        if (isDrawn)
        {
            PositionEventArgs args = new PositionEventArgs();
            args.position = NavMeshPosition;
            args.valid = isDrawn;
            args.agentTypeID = agentTypeID;
            System.EventHandler<PositionEventArgs> handler = OnNavMeshReady;
            if (handler != null)
            {
                handler(this, args);
            }
        }else
        {
            Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.NAVMESH_NOT_GENERATED));
        }

        isOver = true;
        
    }

    /// <summary>
    /// Creates a mesh with the same shape as the NavMesh for the display object. 
    /// </summary>
    /// <returns>Whether the mesh is valid.</returns>
    bool Draw()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.areas.Length == 0) 
			return false;
        
        //Create a child object of this one that displays the NavMesh. 
		if (navMeshObject == null)
        {
            navMeshObject = new GameObject("NavMesh");
        }
        navMeshObject.transform.parent = transform;
        
        
        MeshFilter meshFilter = navMeshObject.GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            meshFilter = navMeshObject.AddComponent<MeshFilter>();
        }
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = triangulation.vertices;

        meshFilter.mesh.triangles = triangulation.indices;
        meshFilter.mesh.RecalculateNormals();
        MeshRenderer r = navMeshObject.GetComponent<MeshRenderer>();
        if (!r)
        {
            r = navMeshObject.AddComponent<MeshRenderer>();
        }
        r.sharedMaterial = materialTransparent;
        navMeshPosition = r.bounds.center;
        navMeshObject.SetActive(hideFlag); //Hidden by default. 
        return true;
    }

	/// <summary>
	/// Collect all the submeshes, or 'chunks', converts them into NavMeshBuildSource objects,
    /// then fills the sources list with them. 
	/// </summary>
	void CollectSources()
    {
#if UNITY_5_6_OR_NEWER
        sources.Clear();

        foreach (var o in zedSpatialMapping.ChunkList)
        {
            MeshFilter m = o.o.GetComponent<MeshFilter>();
            if (m != null)
            {
                NavMeshBuildSource s = new NavMeshBuildSource();
                s.shape = NavMeshBuildSourceShape.Mesh;
                s.sourceObject = m.mesh;
                s.transform = o.o.transform.localToWorldMatrix;
                s.area = agentTypeID;
                sources.Add(s);
            }
        }

#endif
    }

    /// <summary>
    /// Calculates the NavMesh's bounds and inflates them slightly.
    /// Called after all NavmeshBuildSource objects have been created. 
    /// </summary>
    /// <param name="sources">Sources.</param>
    void CalculateBounds(List<NavMeshBuildSource> sources)
	{
		//Use the unscaled matrix for the NavMeshSurface.
		if (sources.Count != 0) {
			bounds.center = transform.position;
	
			//For each source, grows the bounds.
			foreach (var src in sources) {
				Mesh m = src.sourceObject as Mesh;
				bounds.Encapsulate (m.bounds);
			}
		}
		//Inflate the bounds a bit to avoid clipping co-planar sources.
		bounds.Expand(0.1f);
	}

    /// <summary>
    /// Collects the meshes and constructs a single NavMesh from them. 
    /// </summary>
    void UpdateNavMesh()
    {
        isOver = false;
        
		//First collect all the sources (submeshes).
		CollectSources();

#if UNITY_5_6_OR_NEWER
        if (sources.Count != 0)
        {
			//Adjust bounds.
			CalculateBounds(sources);

			//Update the NavMesh with sources and bounds.
	        var defaultBuildSettings = NavMesh.GetSettingsByID(agentTypeID);
            NavMeshBuilder.UpdateNavMeshData(navMesh, defaultBuildSettings, sources, bounds);
        }
#endif
    }

#if UNITY_5_6_OR_NEWER
    /// <summary>
    /// Hide or display the NavMesh using the display object (navMeshObject). 
    /// </summary>
    public void SwitchStateDisplayNavMesh()
    {
        hideFlag = !hideFlag;
        if (navMeshObject != null)
        {
            navMeshObject.SetActive(hideFlag);
        }
    }
#endif

    private void OnApplicationQuit()
    {
        Destroy(navMeshObject);
    }

}
#if UNITY_5_6_OR_NEWER
#if UNITY_EDITOR
/// <summary>
/// Custom editor for NavMeshSurface to extend how it's drawn in the Inspector. 
/// It adds a custom drop-down for agent types, and the Display button that toggles the NavMesh's visibility. 
/// </summary>
[CustomEditor(typeof(NavMeshSurface))]
class ZEDNavMeshEditor : Editor
{
    //Represent the relevant properties as SerializedProperties. 
    //This lets us manipulate and also save (serialize) the data in the scene. 
    /// <summary>
    /// Bound to agentTypeID, the agent type of the NavMesh. 
    /// </summary>
    SerializedProperty agentID;
    /// <summary>
    /// Bound to hideFlag, whether the NavMesh is visible or not. 
    /// </summary>
    SerializedProperty hideFlag;
    /// <summary>
    /// Bound to isOver, whether the NavMesh has been calculated or not. 
    /// </summary>
    SerializedProperty isOver;

    private void OnEnable()
    {
        //Bind the serialized properties to the relevant properties in NavMeshSurface. 
        agentID = serializedObject.FindProperty("agentTypeID");
        hideFlag = serializedObject.FindProperty("hideFlag");
        isOver = serializedObject.FindProperty("isOver");
    }

    /// <summary>
    /// Creates a custom drop-down for the Agent Type enum, which includes a button to 
    /// open the Navigation window for creating/looking up agent types. 
    /// </summary>
    /// <param name="labelName">Text of the label beside the drop-down. Usually "Agent Type".</param>
    /// <param name="agentTypeID">SerializedProperty that the drop-down reads/writes.</param>
    public static void AgentTypePopup(string labelName, SerializedProperty agentTypeID)
    {
        var index = -1;
        var count = NavMesh.GetSettingsCount();
        //var agentTypeNames = new string[count + 2];
        GUIContent[] agentTypeNames = new GUIContent[count + 2];
        for (var i = 0; i < count; i++)
        {
            var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            var name = NavMesh.GetSettingsNameFromID(id);
            agentTypeNames[i] = new GUIContent(name);
            if (id == agentTypeID.intValue)
                index = i;
        }
        agentTypeNames[count] = new GUIContent("");
        agentTypeNames[count + 1] = new GUIContent("Open Agent Settings...");

        bool validAgentType = index != -1;
        if (!validAgentType)
        {
            EditorGUILayout.HelpBox("Agent Type invalid.", MessageType.Warning);
        }

        var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        EditorGUI.BeginProperty(rect, GUIContent.none, agentTypeID);

        EditorGUI.BeginChangeCheck();
        GUIContent text = new GUIContent(labelName, "The ID of the agent type the NavMesh will be built for.");
        index = EditorGUI.Popup(rect, text, index, agentTypeNames);
        if (EditorGUI.EndChangeCheck())
        {
            if (index >= 0 && index < count)
            {
                var id = NavMesh.GetSettingsByIndex(index).agentTypeID;
                agentTypeID.intValue = id;
            }
            else if (index == count + 1)
            {
                UnityEditor.AI.NavMeshEditorHelpers.OpenAgentSettings(-1);
            }
        }

        EditorGUI.EndProperty();
    }

    public override void OnInspectorGUI()
    {
		NavMeshSurface obj = (NavMeshSurface)target;
        serializedObject.Update();
        AgentTypePopup("Agent Type", agentID);

        GUI.enabled = isOver.boolValue; //Only let the user click the button if the NavMesh is finished.
        GUIContent text = new GUIContent(hideFlag.boolValue ? "Hide" : "Display", "Toggle the visibility of the NavMesh.");
        if (GUILayout.Button(text))
        {
            obj.SwitchStateDisplayNavMesh(); //Switch the setting to whatever it wasn't before. 
        }
        serializedObject.ApplyModifiedProperties(); //Applies everything we just changed. 
    }
}
#endif
#endif