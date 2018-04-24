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
public class ZEDPlaneDetectionManager : MonoBehaviour
{ 
	private GameObject holder;
	private ZEDManager manager = null;
	private Camera LeftCamera = null;
	private sl.ZEDCamera zedCam;
	private bool isReady;
	public bool IsReady{ 
		get { return isReady; } 
	}

	private bool hasDetectedFloor = false;
	public bool HasDetectedFloor {
		get { return hasDetectedFloor; }
	}

	private ZEDPlaneGameObject floorPlaneGO = null;
	public ZEDPlaneGameObject getFloorPlane{ 
		get { return floorPlaneGO; } 
	}

	private int planeHitCount = 0;
	public List<ZEDPlaneGameObject> hitPlaneGOList = null;
	public ZEDPlaneGameObject getHitPlaneGO(int i)
	{
		if (i < hitPlaneGOList.Count)
			return hitPlaneGOList [i];
		else
			return null;
	}

	private Vector3[] planeMeshVertices;
	private int[] planeMeshTriangles;

	public static bool isDisplay = false;


	public bool addPhysicsOption = true;
	public bool isVisibleInSceneOption = true;
	public bool isVisibleInGameOption = true;

    public Material overrideMaterial = null;

	private ZEDPlaneRenderer[] meshRenderer = new ZEDPlaneRenderer[2]; 

 
	public float GetEstimatedPlayerHeight {
		get { return estimatedPlayerHeight; }
	}
	private float estimatedPlayerHeight = 0.0f;
	/// <summary>
	/// Start this instance.
	/// </summary>
	private void Start()
	{
		manager = GameObject.FindObjectOfType(typeof(ZEDManager)) as ZEDManager;

		if (manager && manager.GetLeftCameraTransform())
		  LeftCamera = manager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();

		zedCam = sl.ZEDCamera.GetInstance ();
		isReady = false;

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

		floorPlaneGO = new ZEDPlaneGameObject ();
		hitPlaneGOList = new List<ZEDPlaneGameObject> ();

		SetPlaneRenderer();
	}


	/// <summary>
	/// Event When ZED is ready
	/// </summary>
	void ZEDReady()
	{
		if (LeftCamera) {
			isReady = true;
			isDisplay = isVisibleInGameOption;
			SetPlaneRenderer ();
		}

	}


	/// <summary>
	/// Raises the enable event.
	/// </summary>
	public void OnEnable()
	{
		ZEDManager.OnZEDReady += ZEDReady;
	}



	/// <summary>
	/// Raises the disable event.
	/// </summary>
	public void OnDisable()
	{
		if (isReady) {
			foreach (Transform child in holder.transform) {
				GameObject.Destroy (child.gameObject);
			}
			GameObject.Destroy (holder);
		}

		ZEDManager.OnZEDReady -= ZEDReady;
	}


	/// <summary>
	/// Set the plane renderer to the cameras. Is necessary to see the planes
	/// </summary>
	public void SetPlaneRenderer()
	{
 
		if (manager != null)
		{
		    Transform left = manager.GetLeftCameraTransform();
		    if (left != null)
			{
			    meshRenderer[0] = left.gameObject.GetComponent<ZEDPlaneRenderer>();
				if (!meshRenderer[0])
				{
				meshRenderer[0] = left.gameObject.AddComponent<ZEDPlaneRenderer>();

				}
			}
		    Transform right = manager.GetRightCameraTransform();
			if (right != null)
			{
			meshRenderer[1] = right.gameObject.GetComponent<ZEDPlaneRenderer>();
				if (!meshRenderer[1])
				{
				meshRenderer[1] = right.gameObject.AddComponent<ZEDPlaneRenderer>();
				}
			}
		}
	}

	/// <summary>
	/// Transforms the plane mesh from Camera frame to world frame
	/// </summary>
	/// <param name="camera">Camera transform.</param>
	/// <param name="srcVertices">Source vertices (in camera space).</param>
	/// <param name="srcTriangles">Source triangles (in camera space).</param>
	/// <param name="dstVertices">Dst vertices (in world space).</param>
	/// <param name="dstTriangles">Dst triangles (in world space).</param>
	/// <param name="numVertices">Number of vertices.</param>
	/// <param name="numTriangles">Number of triangles.</param>
	private void TransformLocalToWorldMesh(Transform camera,Vector3[] srcVertices, int[] srcTriangles, Vector3[] dstVertices, int[] dstTriangles,int numVertices, int numTriangles)
	{
 		//Since we are in Camera
		if (numVertices == 0 || numTriangles == 0)
			return;
		
		System.Array.Copy(srcVertices, dstVertices, numVertices );
		System.Buffer.BlockCopy(srcTriangles, 0, dstTriangles, 0, numTriangles * sizeof(int));
 
		for (int i = 0; i < numVertices; i++) {
			dstVertices [i] = camera.TransformPoint (dstVertices [i]);
		}

	}

	/// <summary>
	/// Detects the floor plane.
	/// </summary>
	/// <returns><c>true</c>, if floor plane was detected, <c>false</c> otherwise.</returns>
	public bool DetectFloorPlane(bool auto)
	{
		if (!isReady)
			return false;
		
		ZEDPlaneGameObject.PlaneData plane =  new ZEDPlaneGameObject.PlaneData();
		if (zedCam.findFloorPlane (ref plane, out estimatedPlayerHeight, Quaternion.identity, Vector3.zero) == sl.ERROR_CODE.SUCCESS) {
			int numVertices, numTriangles = 0;
			zedCam.convertFloorPlaneToMesh (planeMeshVertices, planeMeshTriangles, out numVertices, out numTriangles);
			if (numVertices > 0 && numTriangles > 0) {
				Vector3[] worldPlaneVertices = new Vector3[numVertices];
				int[] worldPlaneTriangles = new int[numTriangles];
				TransformLocalToWorldMesh (LeftCamera.transform, planeMeshVertices, planeMeshTriangles, worldPlaneVertices, worldPlaneTriangles, numVertices, numTriangles);
				hasDetectedFloor = true;

				if (!floorPlaneGO.IsCreated) {
					if(overrideMaterial != null) floorPlaneGO.Create(holder.transform, plane, worldPlaneVertices, worldPlaneTriangles, 0, overrideMaterial);
                    else floorPlaneGO.Create (holder.transform, plane, worldPlaneVertices, worldPlaneTriangles, 0);
					floorPlaneGO.SetPhysics (addPhysicsOption);
				} else {
					floorPlaneGO.UpdateFloorPlane (!auto,plane, worldPlaneVertices, worldPlaneTriangles, overrideMaterial);
					floorPlaneGO.SetPhysics (addPhysicsOption);

				}
				return true;
			}
		} 

		return false;
	}


	/// <summary>
	/// Detects the plane around pixel hit
	/// </summary>
	/// <returns><c>true</c>, if plane at hit was detected, <c>false</c> otherwise.</returns>
	/// <param name="imagePixel">Image pixel.</param>
	public bool DetectPlaneAtHit(Vector2 imagePixel)
	{
		if (!isReady)
			return false;

		ZEDPlaneGameObject.PlaneData plane =  new ZEDPlaneGameObject.PlaneData();
		if (zedCam.findPlaneAtHit(ref plane,imagePixel) == sl.ERROR_CODE.SUCCESS) {
			int numVertices, numTriangles = 0;
			zedCam.convertHitPlaneToMesh (planeMeshVertices, planeMeshTriangles, out numVertices, out numTriangles);
			if (numVertices > 0 && numTriangles > 0) {
				Vector3[] worldPlaneVertices = new Vector3[numVertices];
				int[] worldPlaneTriangles = new int[numTriangles];
				TransformLocalToWorldMesh (LeftCamera.transform, planeMeshVertices, planeMeshTriangles, worldPlaneVertices, worldPlaneTriangles, numVertices, numTriangles);
				ZEDPlaneGameObject hitPlane = new ZEDPlaneGameObject ();

                if(overrideMaterial != null) hitPlane.Create (holder.transform, plane, worldPlaneVertices, worldPlaneTriangles, planeHitCount + 1, overrideMaterial);
                else hitPlane.Create(holder.transform, plane, worldPlaneVertices, worldPlaneTriangles, planeHitCount + 1);

                hitPlane.SetPhysics (addPhysicsOption);
				hitPlane.SetVisible (isVisibleInSceneOption);
				hitPlaneGOList.Add (hitPlane);
				planeHitCount++;
				return true;
			}
		} 

		return false;

	}


	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update()
	{
		if (Input.GetButtonDown ("Fire1")) {
			Vector2 ScreenPosition = Input.mousePosition;
			DetectPlaneAtHit (ScreenPosition);
		}

	
	}

	/// <summary>
	/// Switchs the display. Set the static variable for rendering
	/// </summary>
	public void SwitchDisplay()
	{
		if (isReady)
		isDisplay = isVisibleInGameOption;
	}

	#if UNITY_EDITOR
	void OnValidate()
	{
		if (floorPlaneGO != null && floorPlaneGO.IsCreated) {
			floorPlaneGO.SetPhysics (addPhysicsOption);
			floorPlaneGO.SetVisible (isVisibleInSceneOption);
		}

		if (hitPlaneGOList != null)
			foreach (ZEDPlaneGameObject c in hitPlaneGOList) {
				if (c.IsCreated)
					c.SetPhysics (addPhysicsOption);

				c.SetVisible (isVisibleInSceneOption);
			}


 		
	}
	#endif

}



#if UNITY_EDITOR
[CustomEditor(typeof(ZEDPlaneDetectionManager ))]
public class ZEDPlaneDetectionEditor : Editor
{
    private ZEDPlaneDetectionManager planeDetector;

   // private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

	private SerializedProperty addPhysicsOption;
	private SerializedProperty isVisibleInSceneOption;
	private SerializedProperty isVisibleInGameOption;
    private SerializedProperty overrideMaterialOption;


    private ZEDPlaneDetectionManager Target
    {
        get { return (ZEDPlaneDetectionManager)target; }
    }

    public void OnEnable()
    {
        planeDetector = (ZEDPlaneDetectionManager)target;
		addPhysicsOption = serializedObject.FindProperty("addPhysicsOption");
		isVisibleInSceneOption = serializedObject.FindProperty("isVisibleInSceneOption");
		isVisibleInGameOption = serializedObject.FindProperty("isVisibleInGameOption");
        overrideMaterialOption = serializedObject.FindProperty("overrideMaterial");
    }



    public override void OnInspectorGUI()
    {
		bool cameraIsReady = sl.ZEDCamera.GetInstance().IsCameraReady;


		serializedObject.Update();

		GUILayout.Space(20);
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Detection Parameters", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		GUI.enabled = cameraIsReady;
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Single-shot Floor Detection");	GUILayout.Space(20);
		if (GUILayout.Button("Detect"))
		{
			if (planeDetector.IsReady)
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
		isVisibleInSceneOption.boolValue = EditorGUILayout.Toggle("Visible in Scene", isVisibleInSceneOption.boolValue);
		isVisibleInGameOption.boolValue = EditorGUILayout.Toggle("Visible in Game", isVisibleInGameOption.boolValue &&  isVisibleInSceneOption.boolValue);

        GUIContent overridematlabel = new GUIContent("Override Material: ", "Material applied to all planes if visible. If left empty, default materials will be applied depending on the plane type.");
        planeDetector.overrideMaterial = (Material)EditorGUILayout.ObjectField(overridematlabel, planeDetector.overrideMaterial, typeof(Material), false);


        planeDetector.SwitchDisplay();
		GUILayout.Space(20);
		GUI.enabled = true;
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Physics", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		addPhysicsOption.boolValue = EditorGUILayout.Toggle("Add Collider", addPhysicsOption.boolValue);



		serializedObject.ApplyModifiedProperties();

		if (!cameraIsReady) Repaint();

    }
}


#endif
