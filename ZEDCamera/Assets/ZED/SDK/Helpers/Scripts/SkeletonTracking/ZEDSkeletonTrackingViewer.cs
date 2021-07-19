﻿//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

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

	public Dictionary<int,SkeletonHandler> avatarControlList;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    /// <summary>
    /// Start this instance.
    /// </summary>
    private void Start()
    {
        QualitySettings.vSyncCount = 1; // Activate vsync

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

        if (zedManager.objectDetectionModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX || zedManager.objectDetectionModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE || zedManager.objectDetectionModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM )
        {
            Debug.LogWarning("MULTI_CLASS_BOX model can't be used for skeleton tracking, please use either HUMAN_BODY_FAST or HUMAN_BODY_ACCURATE");
        }

        if (zedManager.bodyFormat == sl.BODY_FORMAT.POSE_18)
        {
            Debug.LogWarning(" BODY_FORMAT must be set to POSE_32 to animate 3D Avatars !");
            return;
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
		List<int> remainingKeyList = new List<int>(avatarControlList.Keys);
		List<DetectedObject> newobjects = dframe.GetFilteredObjectList(showON, showSEARCHING, showOFF);

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
                handler.Create(Avatar);
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
    }

	public void Update()
	{
        if (Input.GetKeyDown(KeyCode.Space))
        {
            useAvatar = !useAvatar;
            if (useAvatar)
            {
                if (zedManager.bodyFitting)
                    Debug.Log("<b><color=green> Switch to Avatar mode</color></b>");

            }
            else
                Debug.Log("<b><color=green> Switch to Skeleton mode</color></b>");
        }

        if (useAvatar)
        {
            foreach (var skelet in avatarControlList)
            {
                skelet.Value.Move();
            }
        }

        UpdateViewCameraPosition();
    }
     

	/// <summary>
	/// Function to update avatar control with data from ZED SDK.
	/// </summary>
	/// <param name="handler">Handler.</param>
	/// <param name="p">P.</param>
	private void UpdateAvatarControl(SkeletonHandler handler, sl.ObjectDataSDK data, bool useAvatar)
	{
        Vector3[] worldJointsPos = new Vector3[32];
        Quaternion[] worldJointsRot = new Quaternion[32];
   
        for (int i = 0; i < 32; i++)
        {
            worldJointsPos[i] = zedManager.GetZedRootTansform().TransformPoint(data.skeletonJointPosition[i]);
            worldJointsRot[i] = data.localOrientationPerJoint[i];
        }
        
        handler.setControlWithJointPosition(worldJointsPos, worldJointsRot, data.globalRootOrientation, useAvatar);

        handler.SetSmoothFactor(smoothFactor);
    }

    void UpdateViewCameraPosition()
    {
        viewCamera.transform.position = zedManager.transform.localPosition;
        viewCamera.transform.rotation = zedManager.transform.localRotation;
    }
}
