//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the ZED SDK's plane detection feature. 
/// This allows you to check a point on the screen to see if it's part of a real-life plane, such as 
/// a floor, wall, tabletop, etc. If it is, this component will create a GameObject representing that 
/// plane with its proper world position and boundaries. 
/// If this component exists in your scene, you can also click the screen to detect planes there. 
/// By default it adds a MeshCollider, so that physics objects can interact with it properly, and a 
/// MeshRenderer, so that it's visible. 
/// Planes are rendered with ZEDPlaneRenderer, so they aren't occluded, thereby avoiding Z-fighting with the
/// surfaces they represent.
/// </summary>
[DisallowMultipleComponent]
public class ZEDPlaneDetectionManager : MonoBehaviour
{
    /// <summary>
    /// GameObject all planes are parented to. Created at runtime, called '[ZED Planes]' in Hierarchy.
    /// </summary>
    private GameObject holder;

    /// <summary>
    /// Whether a floor plane has been detected during runtime. 
    /// This won't happen unless DetectFloorPlane() is called. 
    /// </summary>
	private bool hasDetectedFloor = false;
    /// <summary>
    /// Public accessor for hasDetectedFloor, which is whether a floor plane has been detected during runtime. 
    /// </summary>
	public bool HasDetectedFloor
    {
        get { return hasDetectedFloor; }
    }

    /// <summary>
    /// GameObject holding floorPlane, which representing the floor plane, if one has been detected. 
    /// </summary>
    private GameObject floorPlaneGO;

    /// <summary>
    /// ZEDPlaneGameObject representing the floor plane, if one has been detected. 
    /// This reference is used to clear the existing floor plane if a new one is detected. 
    /// </summary>
    private ZEDPlaneGameObject floorPlane = null;

    /// <summary>
    /// Returns the ZEDPlaneGameObject representing the floor, if the floor has been detected. 
    /// </summary>
	public ZEDPlaneGameObject getFloorPlane
    {
        get { return floorPlane; }
    }

    /// <summary>
    /// How many hit planes have been detected. Used to assign the index of hitPlaneList
    /// to the ZEDPlaneGameObject's themselves, and to name their GameObjects. 
    /// </summary>
	private int planeHitCount = 0;

    /// <summary>
    /// All the hit planes that have been detected using DetectPlaneAtHit().
    /// </summary>
	public List<ZEDPlaneGameObject> hitPlaneList = null;

    /// <summary>
    /// Returns a ZEDPlaneGameObject with the provided index in hitPlaneList. 
    /// </summary>
    /// <param name="i">Index within hitPlaneList.</param>
    /// <returns></returns>
	public ZEDPlaneGameObject getHitPlane(int i)
    {
        if (i < hitPlaneList.Count)
            return hitPlaneList[i];
        else
            return null;
    }

    /// <summary>
    /// Buffer for holding a new plane's vertex data from the SDK.
    /// </summary>
	private Vector3[] planeMeshVertices;
    /// <summary>
    /// Buffer for holding a new plane's triangle data from the SDK.
    /// </summary>
    private int[] planeMeshTriangles;

    /// <summary>
    /// Whether we're displaying the planes in the Scene view using ZEDPlaneRenderer. Usually the same as isVisibleInSceneOption.
    /// </summary>
    public static bool isSceneDisplay = true;

    /// <summary>
    /// Whether we're displaying the planes in the Game view using ZEDPlaneRenderer. Usually the same as isVisibleInGameOption.
    /// Used by ZEDRenderingPlane to know if it should draw the meshes in its OnRenderImage() method. 
    /// </summary>
    public static bool isGameDisplay = false;


    /// <summary>
    /// Whether the planes can collide with virtual objects (with colliders). Changing this enables/disables
    /// mesh colliders on all new and existing planes. 
    /// </summary>
    public bool planesHavePhysics
    {
        get
        {
            return addPhysicsOption;
        }
        set
        {
            if (addPhysicsOption != value)
            {
                willUpdatePhysics = true;
                addPhysicsOption = value;
            }
        }
    }
    /// <summary>
    /// Whether the planes can collide with virtual objects (with colliders). 
    /// To update publicly or at runtime, use planesHavePhysics instead. 
    /// </summary>
    [SerializeField]
    private bool addPhysicsOption = true;
    /// <summary>
    /// If true, existing planes will have their colliders updated
    /// based on addPhhysicsOption in the next Update() step.
    /// </summary>
    private bool willUpdatePhysics = false;

    /// <summary>
    /// Whether the planes are drawn. Changing this setting enables/disables the plane's MeshRenderers.
    /// Note that if disabled, planes will not be drawn in the Game view regardless of the planesVisibleInGame setting. 
    /// </summary>
    public bool planesVisibleInScene
    {
        get
        {
            return isVisibleInSceneOption;
        }
        set
        {
            if (value != isVisibleInSceneOption)
            {

                isVisibleInSceneOption = value;
                willUpdateSceneVisibility = true;
            }
        }
    }
    /// <summary>
    /// Whether the planes are drawn.
    /// To update publicly or at runtime, use planesVisibleInScene instead. 
    /// </summary>
    [SerializeField]
    private bool isVisibleInSceneOption = true;
    /// <summary>
    /// If true, existing planes will have their visibility updated 
    /// based on isVisibleInSceneOption in the next Update() step.
    /// </summary>
    private bool willUpdateSceneVisibility = true;

    /// <summary>
    /// Whether the planes are drawn in the ZED's final output, viewable in Unity's Game window or a build. 
    /// </summary>
    public bool planesVisibleInGame
    {
        get
        {
            return isVisibleInGameOption;
        }
        set
        {
            if (value != isVisibleInGameOption)
            {
                isVisibleInGameOption = value;
                willUpdateGameVisibility = true;
            }
        }
    }
    /// <summary>
    /// Whether the planes are drawn in the ZED's final output, viewable in Unity's Game window or a build. 
    /// To update publicly or at runtime, use planesVisibleInGame instead. 
    /// </summary>
    [SerializeField]
    private bool isVisibleInGameOption = true;
    /// <summary>
    /// If true, existing planes will have their in-game visibility updated 
    /// based on isVisibleInGameOption in the next Update() step.
    /// </summary>
    private bool willUpdateGameVisibility = true;

    /// <summary>
    /// Overrides the default wireframe material used to draw planes in the Scene and Game view. 
    /// Leave this setting to null to draw the default wireframes. 
    /// </summary>
    public Material overrideMaterial = null; //If null, shows wireframe. Otherwise, displays your custom material. 

    /// <summary>
    /// How high the player's head is from the floor. Filled in when DetectFloorPlane() is called. 
    /// </summary>
    private float estimatedPlayerHeight = 0.0f;
    /// <summary>
    /// Public accessor for estimatedPlayerHeight, which is how high the player's head was when DetectFloorPlane() was last called. 
    /// </summary>
    public float GetEstimatedPlayerHeight
    {
        get { return estimatedPlayerHeight; }
    }

    /// <summary>
    /// Assign references, create the holder gameobject, and other misc. initialization. 
    /// </summary>
    private void Start()
    {
        //Create a holder for all the planes
        holder = new GameObject();
        holder.name = "[ZED Planes]";
        holder.transform.parent = this.transform;
        holder.transform.position = Vector3.zero;
        holder.transform.rotation = Quaternion.identity;
        StaticBatchingUtility.Combine(holder);

        //initialize Vertices/Triangles with enough length
        planeMeshVertices = new Vector3[65000];
        planeMeshTriangles = new int[65000];

        //floorPlaneGO = holder.AddComponent<ZEDPlaneGameObject> ();
        hitPlaneList = new List<ZEDPlaneGameObject>();
    }

    public void OnDisable()
    {
        foreach (Transform child in holder.transform)
        {
            Destroy(child.gameObject);

            Destroy(holder);
        }
    }

    /// <summary>
    /// Transforms the plane mesh from Camera frame to local frame, where each vertex is relative to the plane's center. 
    /// Used because plane data from the ZED SDK is relative to the camera, not the world. 
    /// </summary>
    /// <param name="camera">Camera transform.</param>
    /// <param name="srcVertices">Source vertices (in camera space).</param>
    /// <param name="srcTriangles">Source triangles (in camera space).</param>
    /// <param name="dstVertices">Dst vertices (in world space).</param>
    /// <param name="dstTriangles">Dst triangles (in world space).</param>
    /// <param name="numVertices">Number of vertices.</param>
    /// <param name="numTriangles">Number of triangles.</param>
    private void TransformCameraToLocalMesh(Transform camera, Vector3[] srcVertices, int[] srcTriangles, Vector3[] dstVertices, int[] dstTriangles, int numVertices, int numTriangles, Vector3 centerpos)
    {
        if (numVertices == 0 || numTriangles == 0)
            return; //Plane is empty. 

        System.Array.Copy(srcVertices, dstVertices, numVertices);
        System.Buffer.BlockCopy(srcTriangles, 0, dstTriangles, 0, numTriangles * sizeof(int));

        for (int i = 0; i < numVertices; i++)
        {
            dstVertices[i] -= centerpos;
            dstVertices[i] = camera.transform.rotation * dstVertices[i];
        }

    }

    /// <summary>
    /// Detects the floor plane. Replaces the current floor plane, if there is one, unlike DetectPlaneAtHit(). 
    /// If a floor is detected, also assigns the user's height from the floor to estimatedPlayerHeight.
    /// <para>Uses the first available ZEDManager in the scene.</para>
    /// </summary>
    /// <returns><c>true</c>, if floor plane was detected, <c>false</c> otherwise.</returns>
    public bool DetectFloorPlane(bool auto)
    {
        //Find the first available ZEDManager. 
        List<ZEDManager> managers = ZEDManager.GetInstances();
        if (managers.Count == 0) return false; //No ZEDManager to use. 
        else return DetectFloorPlane(managers[0], auto);
    }

    /// <summary>
    /// Detects the floor plane. Replaces the current floor plane, if there is one, unlike DetectPlaneAtHit(). 
    /// If a floor is detected, also assigns the user's height from the floor to estimatedPlayerHeight.
    /// </summary>
    /// <returns><c>true</c>, if floor plane was detected, <c>false</c> otherwise.</returns>
    public bool DetectFloorPlane(ZEDManager manager, bool auto)
    {
        if (!manager.IsZEDReady)
            return false; //Do nothing if the ZED isn't finished initializing. 

        sl.ZEDCamera zedcam = manager.zedCamera;
        Camera leftcamera = manager.GetLeftCamera();

        ZEDPlaneGameObject.PlaneData plane = new ZEDPlaneGameObject.PlaneData();
        if (zedcam.findFloorPlane(ref plane, out estimatedPlayerHeight, Quaternion.identity, Vector3.zero) == sl.ERROR_CODE.SUCCESS) //We found a plane. 
        {
            int numVertices, numTriangles = 0;
            zedcam.convertFloorPlaneToMesh(planeMeshVertices, planeMeshTriangles, out numVertices, out numTriangles);
            if (numVertices > 0 && numTriangles > 0)
            {
                Vector3[] worldPlaneVertices = new Vector3[numVertices];
                int[] worldPlaneTriangles = new int[numTriangles];
                TransformCameraToLocalMesh(leftcamera.transform, planeMeshVertices, planeMeshTriangles, worldPlaneVertices, worldPlaneTriangles, numVertices, numTriangles, plane.PlaneCenter);

                hasDetectedFloor = true;

                if (!floorPlaneGO)
                { //Make the GameObject.
                    floorPlaneGO = new GameObject("Floor Plane");
                    floorPlaneGO.transform.SetParent(holder.transform);
                }

                if (!floorPlaneGO) //Make the GameObject.
                {
                    floorPlaneGO = new GameObject("Floor Plane");
                    floorPlaneGO.transform.SetParent(holder.transform);
                }

                //Move the GameObject to the center of the plane. Note that the plane data's center is relative to the camera. 
                floorPlaneGO.transform.position = leftcamera.transform.position; //Add the camera's world position 
                floorPlaneGO.transform.position += leftcamera.transform.rotation * plane.PlaneCenter; //Add the center of the plane

                if (!floorPlane) //Add a new ZEDPlaneGameObject to the floor plane if it doesn't already exist. 
                {
                    floorPlane = floorPlaneGO.AddComponent<ZEDPlaneGameObject>();
                }


                if (!floorPlane.IsCreated) //Call ZEDPlaneGameObject.Create() on the floor ZEDPlaneGameObject if it hasn't yet been run. 
                {
                    if (overrideMaterial != null) floorPlane.Create(leftcamera, plane, worldPlaneVertices, worldPlaneTriangles, 0, overrideMaterial);
                    else floorPlane.Create(leftcamera, plane, worldPlaneVertices, worldPlaneTriangles, 0);
                    floorPlane.SetPhysics(addPhysicsOption);
                }
                else //Update the ZEDPlaneGameObject with the new plane's data. 
                {
                    floorPlane.UpdateFloorPlane(!auto, plane, worldPlaneVertices, worldPlaneTriangles, overrideMaterial);
                    floorPlane.SetPhysics(addPhysicsOption);

                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects the plane around screen-space coordinates specified. 
    /// <para>Uses the first available ZEDManager in the scene.</para>
    /// </summary>
    /// <returns><c>true</c>, if plane at hit was detected, <c>false</c> otherwise.</returns>
    /// <param name="screenPos">Position of the pixel in screen space (2D).</param>
    public bool DetectPlaneAtHit(Vector2 screenPos)
    {
        //Find the first available ZEDManager. 
        List<ZEDManager> managers = ZEDManager.GetInstances();
        if (managers.Count == 0) return false; //No ZEDManager to use. 
        else if (managers.Count > 1) //They're using multiple cameras but using the first available manager. Not ideal. 
        {
            Debug.LogWarning("Using DetectPlaneAtHit without specifying a manager while multiple ZEDManagers are present. " +
                "This can cause planes to be calculated by the wrong camera. It's recommended to use the (ZEDManager, Vector2) overload.");
        }
        return DetectPlaneAtHit(managers[0], screenPos);
    }

    /// <summary>
    /// Detects the plane around screen-space coordinates specified. 
    /// </summary>
    /// <returns><c>true</c>, if plane at hit was detected, <c>false</c> otherwise.</returns>
    /// <param name="screenPos">Position of the pixel in screen space (2D).</param>
    public bool DetectPlaneAtHit(ZEDManager manager, Vector2 screenPos)
    {
        if (!manager.IsZEDReady)
            return false; //Do nothing if the ZED isn't finished initializing. 

        sl.ZEDCamera zedcam = manager.zedCamera;
        Camera leftcamera = manager.GetLeftCamera();

        ZEDPlaneGameObject.PlaneData plane = new ZEDPlaneGameObject.PlaneData();
        if (zedcam.findPlaneAtHit(ref plane, screenPos) == sl.ERROR_CODE.SUCCESS) //We found a plane. 
        {
            int numVertices, numTriangles = 0;
            zedcam.convertHitPlaneToMesh(planeMeshVertices, planeMeshTriangles, out numVertices, out numTriangles);
            if (numVertices > 0 && numTriangles > 0)
            {
                GameObject newhitGO = new GameObject(); //Make a new GameObject to hold the new plane. 
                newhitGO.transform.SetParent(holder.transform);

                Vector3[] worldPlaneVertices = new Vector3[numVertices];
                int[] worldPlaneTriangles = new int[numTriangles];
                TransformCameraToLocalMesh(leftcamera.transform, planeMeshVertices, planeMeshTriangles, worldPlaneVertices, worldPlaneTriangles, numVertices, numTriangles, plane.PlaneCenter);

                //Move the GameObject to the center of the plane. Note that the plane data's center is relative to the camera. 
                newhitGO.transform.position = leftcamera.transform.position; //Add the camera's world position 
                newhitGO.transform.position += leftcamera.transform.rotation * plane.PlaneCenter; //Add the center of the plane

                ZEDPlaneGameObject hitPlane = newhitGO.AddComponent<ZEDPlaneGameObject>();

                if (overrideMaterial != null) hitPlane.Create(leftcamera, plane, worldPlaneVertices, worldPlaneTriangles, planeHitCount + 1, overrideMaterial);
                else hitPlane.Create(leftcamera, plane, worldPlaneVertices, worldPlaneTriangles, planeHitCount + 1);

                hitPlane.SetPhysics(addPhysicsOption);
                //hitPlane.SetVisible(isVisibleInSceneOption);
                hitPlaneList.Add(hitPlane);
                planeHitCount++;
                return true;
            }
        }

        return false;

    }


    /// <summary>
    /// Check if the screen was clicked. If so, check for a plane where the click happened using DetectPlaneAtHit().
    /// </summary>
    void Update()
    {
        //Detect hits when you click on the screen. 
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 ScreenPosition = Input.mousePosition;

            List<ZEDManager> managers = ZEDManager.GetInstances();
            if (managers.Count == 1) //There's only one ZEDManager, so use that one. 
                DetectPlaneAtHit(managers[0], ScreenPosition);
            else if (managers.Count > 1)
            {
                //There are at least two ZEDManagers rendering. Find the one that's likely the last to render, so it's the one the user clicked on. 
                float highestdepth = Mathf.NegativeInfinity;
                ZEDManager highestmanager = managers[0];
                foreach (ZEDManager manager in managers)
                {
                    float depth = manager.GetLeftCamera().depth;
                    if (depth >= highestdepth)
                    {
                        highestdepth = depth;
                        highestmanager = manager;
                    }
                }

                DetectPlaneAtHit(highestmanager, ScreenPosition);
            }
        }

        //Update plane physics if needed. 
        if (willUpdatePhysics)
        {
            if (floorPlane != null && floorPlane.IsCreated)
            {
                floorPlane.SetPhysics(addPhysicsOption);
            }
            if (hitPlaneList != null)
            {
                foreach (ZEDPlaneGameObject c in hitPlaneList)
                {
                    if (c.IsCreated)
                        c.SetPhysics(addPhysicsOption);
                }
            }
            willUpdatePhysics = false;
        }

        //Update visibility if needed. 
        if (willUpdateSceneVisibility)
        {
            /*if (floorPlane != null && floorPlane.IsCreated)
            {
                floorPlane.SetVisible(isVisibleInSceneOption);
            }
            if (hitPlaneList != null)
            {
                foreach (ZEDPlaneGameObject c in hitPlaneList)
                {
                    c.SetVisible(isVisibleInSceneOption);
                }
            }*/
            SwitchSceneDisplay();
            willUpdateSceneVisibility = false;
        }

        //Update in-game visibility if needed.
        if (willUpdateGameVisibility)
        {
            SwitchGameDisplay();
            willUpdateGameVisibility = false;
        }

    }

    /// <summary>
    /// Switches the isGameDisplay setting, used to know if planes should be rendered to the Scene window. 
    /// </summary>
    public void SwitchSceneDisplay()
    {
        isSceneDisplay = isVisibleInSceneOption;
    }

    /// <summary>
    /// Switches the isGameDisplay setting, used to know if planes should be rendered to the Game window. 
    /// </summary>
    public void SwitchGameDisplay()
    {
        isGameDisplay = isVisibleInGameOption;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called when the Inspector is visible or changes. Updates plane physics and visibility settings. 
    /// </summary>
    void OnValidate()
    {
        if (floorPlane != null && floorPlane.IsCreated)
        {
            floorPlane.SetPhysics(addPhysicsOption);

            //floorPlane.SetVisible(isVisibleInSceneOption);
        }

        if (hitPlaneList != null)
            foreach (ZEDPlaneGameObject c in hitPlaneList)
            {
                if (c.IsCreated)
                    c.SetPhysics(addPhysicsOption);

                //c.SetVisible(isVisibleInSceneOption);
            }

        SwitchSceneDisplay();
        SwitchGameDisplay();
    }


#endif

}



#if UNITY_EDITOR
/// <summary>
/// Custom Inspector editor for ZEDPlaneDetectionManager. 
/// Adds a button to detect the floor, and causes planes to get updated instantly when their visibility settings change. 
/// </summary>
[CustomEditor(typeof(ZEDPlaneDetectionManager))]
public class ZEDPlaneDetectionEditor : Editor
{
    /// <summary>
    /// The ZEDPlaneDetectionManager component that this editor is displaying. 
    /// </summary>
    private ZEDPlaneDetectionManager planeDetector;

    // private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

    /// <summary>
    /// Serializable version of ZEDPlaneDetectionManager's addPhysicsOption property. 
    /// </summary>
    private SerializedProperty addPhysicsOption;
    /// <summary>
    /// Serializable version of ZEDPlaneDetectionManager's isVisibleInSceneOption property. 
    /// </summary>
	private SerializedProperty isVisibleInSceneOption;
    /// <summary>
    /// Serializable version of ZEDPlaneDetectionManager's isVisibleInGameOption property. 
    /// </summary>
	private SerializedProperty isVisibleInGameOption;
    /// <summary>
    /// Serializable version of ZEDPlaneDetectionManager's overrideMaterialOption property. 
    /// </summary>
    private SerializedProperty overrideMaterialOption;


    private ZEDPlaneDetectionManager Target
    {
        get { return (ZEDPlaneDetectionManager)target; }
    }

    public void OnEnable()
    {
        //Assign the serialized properties to their appropriate properties. 
        planeDetector = (ZEDPlaneDetectionManager)target;
        addPhysicsOption = serializedObject.FindProperty("addPhysicsOption");
        isVisibleInSceneOption = serializedObject.FindProperty("isVisibleInSceneOption");
        isVisibleInGameOption = serializedObject.FindProperty("isVisibleInGameOption");
        overrideMaterialOption = serializedObject.FindProperty("overrideMaterial");
    }

    public override void OnInspectorGUI()
    {
        //bool cameraIsReady = planeDetector.Manager != null ? planeDetector.Manager.zedCamera.IsCameraReady : false;

        serializedObject.Update();

        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Detection Parameters", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        //GUI.enabled = cameraIsReady;
        EditorGUILayout.BeginHorizontal();
        GUIContent floordetectionlabel = new GUIContent("Single-shot Floor Detection", "Attempt to detect a floor plane in the current view.");
        GUILayout.Label(floordetectionlabel); GUILayout.Space(20);

        GUIContent floordetectbuttonlabel = new GUIContent("Detect", "Attempt to detect a floor plane in the current view.");
        if (GUILayout.Button(floordetectbuttonlabel))
        {
            planeDetector.DetectFloorPlane(false);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Visualization", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIContent visiblescenelabel = new GUIContent("Visible in Scene", "Whether the planes are drawn in Unity's Scene view.");
        GUIContent visiblegamelabel = new GUIContent("Visible in Game", "Whether the planes are drawn in Unity's Game view.");
        isVisibleInSceneOption.boolValue = EditorGUILayout.Toggle(visiblescenelabel, isVisibleInSceneOption.boolValue);

#if !UNITY_2018_1_OR_NEWER
        if (!isVisibleInSceneOption.boolValue)
        {
            //Older Unity versions have a bug that will spam errors if Visible In Scene is disabled. Warn the user. 
            GUIStyle warningmessagestyle = new GUIStyle(EditorStyles.label);
            warningmessagestyle.normal.textColor = Color.gray;
            warningmessagestyle.wordWrap = true;
            warningmessagestyle.fontSize = 9;

            string warningtext = "Warning: Disabling Visible in Scene causes Unity versions 2017 and lower to spam error messages. This is due to a Unity bug and does not effect the scene.";
            Rect labelrect = GUILayoutUtility.GetRect(new GUIContent(warningtext, ""), warningmessagestyle);
            EditorGUI.LabelField(labelrect, warningtext, warningmessagestyle);
        }
#endif

        isVisibleInGameOption.boolValue = EditorGUILayout.Toggle(visiblegamelabel, isVisibleInGameOption.boolValue);

        GUIContent overridematlabel = new GUIContent("Override Material: ", "Material applied to all planes if visible. If left empty, default materials will be applied depending on the plane type.");
        planeDetector.overrideMaterial = (Material)EditorGUILayout.ObjectField(overridematlabel, planeDetector.overrideMaterial, typeof(Material), false);


        planeDetector.SwitchGameDisplay();
        GUILayout.Space(20);
        GUI.enabled = true;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Physics", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIContent physicslabel = new GUIContent("Collisions", "Whether the planes can be collided with using physics.");
        addPhysicsOption.boolValue = EditorGUILayout.Toggle(physicslabel, addPhysicsOption.boolValue);


        serializedObject.ApplyModifiedProperties(); //Applies all changes to serializedproperties to the actual properties they're connected to. 

        //if (!cameraIsReady) Repaint();
        Repaint();

    }
}


#endif
