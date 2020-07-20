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
public class ZED3DSkeletonVisualizer : MonoBehaviour
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


    public int indexFakeTest = 9;
	public Dictionary<int,SkeletonHandler> avatarControlList;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
	/// <summary>
	/// Start this instance.
	/// </summary>
    private void Start()
    {
		avatarControlList = new Dictionary<int,SkeletonHandler> ();
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

		if (zedManager)
        {
        zedManager.OnObjectDetection += updateSkeletonData;
        zedManager.OnZEDReady += OnZEDReady;
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
		List<DetectedObject> newobjects = dframe.GetFilteredObjectList(true, true, true);

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
				UpdateAvatarControl(handler,dobj.rawObjectData);

				// remove keys from list 
				remainingKeyList.Remove(person_id);
			}
			else
			{
				SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
                Vector3 spawnPosition = zedManager.GetZedRootTansform().TransformPoint(dobj.rawObjectData.rootWorldPosition);
                spawnPosition.y = 0;
                handler.Create(Avatar, spawnPosition);
				avatarControlList.Add(person_id,handler);
				UpdateAvatarControl(handler,dobj.rawObjectData);
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

        //UpdateViewCameraPosition();
        
#if FAKEMODE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            indexFakeTest++;
            if (indexFakeTest == 10) indexFakeTest = 1;
            Debug.Log(indexFakeTest);
        }
#endif
    }
 
 
	/// <summary>
	/// Function to update avatar control with data from ZED SDK.
	/// </summary>
	/// <param name="handler">Handler.</param>
	/// <param name="p">P.</param>
	private void UpdateAvatarControl(SkeletonHandler handler, sl.ObjectDataSDK data)
	{

		Vector3 bodyCenter = data.rootWorldPosition;

        if (bodyCenter == Vector3.zero)  return; // Object not detected

		Vector3[] world_joints_pos = new Vector3[19];
		for (int i=0;i<18;i++)
		{
			world_joints_pos[i] = zedManager.GetZedRootTansform().TransformPoint(data.skeletonJointPosition[i]);
		}

        //Create Joint with middle position : 
        world_joints_pos[0] = (world_joints_pos[16] + world_joints_pos[17]) / 2;
        world_joints_pos[18] = (world_joints_pos[8] + world_joints_pos[11])/2; 

		/*
		for (int i=0;i<19;i++)
		Debug.Log(" jt "+i+" : "+world_joints_pos[i]);
		*/


		Vector3 worldbodyRootPosition = zedManager.GetZedRootTansform().TransformPoint(bodyCenter);
        worldbodyRootPosition.y = 0;
		handler.setControlWithJointPosition (world_joints_pos, worldbodyRootPosition);
       

        handler.SetSmoothFactor (smoothFactor);
       
       // handler.setFakeTest((indexFakeTest));
    }

    void UpdateViewCameraPosition()
    {
        viewCamera.transform.position = zedManager.transform.localPosition;
    }
}
 