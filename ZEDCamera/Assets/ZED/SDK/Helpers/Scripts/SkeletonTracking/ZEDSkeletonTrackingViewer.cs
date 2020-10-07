//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//#define FAKEMODE

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// Contols the ZEDSkeletonTracking . Links the SDK to Unity
/// </summary>
[DisallowMultipleComponent]
public class ZEDSkeletonTrackingViewer : MonoBehaviour
{
	/// <summary>
    /// The scene's ZEDManager.
    /// If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DSkeletonVisualizer commponents in the scene.
    /// </summary>
    [Tooltip("The scene's ZEDManager.\r\n" +
        "If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DSkeletonVisualizer commponents in the scene. ")]
    public ZEDManager zedManager;

    [Tooltip("The camera view to display ZED images")]
    public Camera viewCamera;


	/// <summary>
	/// Activate skeleton tracking when play mode is on and ZED ready
	/// </summary>
	[Header("Game Control")]

    public bool startObjectDetectionAutomatically = true;

    /// <summary>
    /// Vizualisation mode. Use a 3D model or only display the skeleton
    /// </summary>
    [Header("Vizualisation Mode")]
    /// <summary>
    /// Display 3D avatar. If set to false, only display bones and joint
    /// </summary>
    [Tooltip("Display 3D avatar. If set to false, only display bones and joint")]
    public bool useAvatar = true;

    [Space(5)]
    [Header("State Filters")]
    [Tooltip("Display objects that are actively being tracked by object tracking, where valid positions are known. ")]
    public bool showON = true;
    /// <summary>
    /// Display objects that were actively being tracked by object tracking, but that were lost very recently.
    /// </summary>
    [Tooltip("Display objects that were actively being tracked by object tracking, but that were lost very recently.")]
    public bool showSEARCHING = false;
    /// <summary>
    /// Display objects that are visible but not actively being tracked by object tracking (usually because object tracking is disabled in ZEDManager).
    /// </summary>
    [Tooltip("Display objects that are visible but not actively being tracked by object tracking (usually because object tracking is disabled in ZEDManager).")]
    public bool showOFF = false;

    [Header("Avatar Control")]
	/// <summary>
	/// Avatar game object
	/// </summary>
    public GameObject Avatar;

	/// <summary>
	/// Smoothing factor for humanoid movments.
	/// </summary>
	[Range(0, 1)]
    [Tooltip("Smooth factor used for avatar movements and joint rotations.")]
    public float smoothFactor = 0.5f;


    int indexFakeTest = 9;
	public Dictionary<int,SkeletonHandler> avatarControlList;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float SpineHeight = 0.85f;
	/// <summary>
	/// Start this instance.
	/// </summary>
    private void Start()
    {
        Application.targetFrameRate = 60; // Set Engine frame rate to 60fps

		avatarControlList = new Dictionary<int,SkeletonHandler> ();
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

		if (zedManager)
        {
        zedManager.OnZEDReady += OnZEDReady;
        zedManager.OnObjectDetection += updateSkeletonData;
		}

        if (zedManager.objectDetectionModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX || zedManager.objectDetectionModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE)
        {
            Debug.LogWarning("MULTI_CLASS_BOX model can't be used for skeleton tracking, please use either HUMAN_BODY_FAST or HUMAN_BODY_ACCURATE");
        }
    }

    private void OnZEDReady()
    {
        if (startObjectDetectionAutomatically && !zedManager.IsObjectDetectionRunning)
        {
            zedManager.StartObjectDetection();
        }
    }

	private void OnDestroy()
    {
        if (zedManager)
        {
            zedManager.OnObjectDetection -= updateSkeletonData;
            zedManager.OnZEDReady -= OnZEDReady;
        }
    }



	/// <summary>
	/// Updates the skeleton data from ZEDCamera call and send it to Skeleton Handler script.
	/// </summary>
    private void updateSkeletonData(DetectionFrame dframe)
    {

        #if FAKEMODE

        if (avatarControlList.ContainsKey(0))
        {
            SkeletonHandler handler = avatarControlList[0];
            handler.setFakeTest(indexFakeTest);
        }
        else
        {
            SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
            handler.Create(Avatar, Vector3.zero);
            avatarControlList.Add(0, handler);
        }


        #else
		List<int> remainingKeyList = new List<int>(avatarControlList.Keys);
		List<DetectedObject> newobjects = dframe.GetFilteredObjectList(showON, showSEARCHING, showOFF);

		/*if (dframe.rawObjectsFrame.detectionModel!= sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE &&
			dframe.rawObjectsFrame.detectionModel!= sl.DETECTION_MODEL.HUMAN_BODY_FAST)
		{
			Debug.Log("Wrong model selected : " + dframe.rawObjectsFrame.detectionModel);
		return;
		}*/

 		foreach (DetectedObject dobj in newobjects)
        {
			int person_id = dobj.rawObjectData.id;

			//Avatar controller already exist --> update position
			if (avatarControlList.ContainsKey(person_id))
			{
				SkeletonHandler handler = avatarControlList[person_id];
				UpdateAvatarControl(handler,dobj.rawObjectData, useAvatar);

				// remove keys from list
				remainingKeyList.Remove(person_id);
			}
			else
			{
				SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
                Vector3 spawnPosition = zedManager.GetZedRootTansform().TransformPoint(dobj.rawObjectData.rootWorldPosition);
                handler.Create(Avatar, spawnPosition);
                handler.initSkeleton(person_id);
                avatarControlList.Add(person_id, handler);
                UpdateAvatarControl(handler, dobj.rawObjectData, useAvatar);
			}
		}

        foreach (int index in remainingKeyList)
		{
			SkeletonHandler handler = avatarControlList[index];
			handler.Destroy();
			avatarControlList.Remove(index);
		}

		#endif
    }

	public void Update()
	{
		foreach (var skelet in avatarControlList) {
            skelet.Value.Move ();
		}

        UpdateViewCameraPosition();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            useAvatar = !useAvatar;
        }

#if FAKEMODE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            indexFakeTest++;
            if (indexFakeTest == 15) indexFakeTest = 1;
            Debug.Log(indexFakeTest);
        }
#endif
    }


	/// <summary>
	/// Function to update avatar control with data from ZED SDK.
	/// </summary>
	/// <param name="handler">Handler.</param>
	/// <param name="p">P.</param>
	private void UpdateAvatarControl(SkeletonHandler handler, sl.ObjectDataSDK data, bool useAvatar)
	{

		Vector3 bodyCenter = data.rootWorldPosition;

        if (bodyCenter == Vector3.zero)  return; // Object not detected

		Vector3[] world_joints_pos = new Vector3[20];
		for (int i=0;i<18;i++)
		{
			world_joints_pos[i] = zedManager.GetZedRootTansform().TransformPoint(data.skeletonJointPosition[i]);
		}

        //Create Joint with middle position :
        world_joints_pos[0] = (world_joints_pos[16] + world_joints_pos[17]) / 2;
        world_joints_pos[18] = (world_joints_pos[8] + world_joints_pos[11]) / 2;
        world_joints_pos[19] = zedManager.GetZedRootTansform().TransformPoint(data.skeletonJointPosition[0]); // Add Nose Joint for skeleton vizualisation

        /*
		for (int i=0;i<19;i++)
		Debug.Log(" jt "+i+" : "+world_joints_pos[i]);*/

        Vector3 worldbodyRootPosition = zedManager.GetZedRootTansform().TransformPoint(bodyCenter);
        if (float.IsNaN(world_joints_pos[18].y)) worldbodyRootPosition.y = 0;
        else worldbodyRootPosition.y = world_joints_pos[18].y - SpineHeight;

        handler.setControlWithJointPosition (world_joints_pos, worldbodyRootPosition, useAvatar) ;
        //handler.setJointSpherePoint(world_joints_pos);

        handler.SetSmoothFactor (smoothFactor);

    }

    void UpdateViewCameraPosition()
    {
        viewCamera.transform.position = zedManager.transform.localPosition;
        viewCamera.transform.rotation = zedManager.transform.localRotation;
    }
}
