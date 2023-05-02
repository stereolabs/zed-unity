//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

#if ZED_URP
using UnityEngine.Rendering.Universal;
#endif

[DisallowMultipleComponent]
public class ZEDBodyTrackingManager : MonoBehaviour
{
    #region vars

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

    public bool startBodyTrackingAutomatically = true;

    /// <summary>
    /// Vizualisation mode. Use a 3D model or only display the skeleton
    /// </summary>
    [Header("Vizualisation Mode")]
    /// <summary>
    /// Display 3D avatar. If set to false, only display bones and joint
    /// </summary>
    [Tooltip("Display 3D avatar. If set to false, only display bones and joint")]
    public bool useAvatar = true;

    /// <summary>
    /// Maximum number of detection displayed in the scene.
    /// </summary>
    [Tooltip("Maximum number of detections spawnable in the scene")]
    public int maximumNumberOfDetections = (int)sl.Constant.MAX_OBJECTS;

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
    [Tooltip("3D Rigged model.")]
    public GameObject avatar;
    public Material skeletonBaseMaterial;

    [Space(20)]
    [Tooltip("Display bones and joints along 3D avatar")]
    [SerializeField]
    private bool displayDebugSkeleton = false;
    public static bool DisplayDebugSkeleton = false;
    [SerializeField]
    private Vector3 offsetDebugSkeleton = new Vector3(1f, 0f, 0f);
    public static Vector3 OffsetDebugSkeleton = new Vector3(1f, 0f, 0f);
    [SerializeField]
    private bool logFusionMetrics = false;
    public static bool LogFusionMetrics = false;

    [Space(5)]
    [Tooltip("Mirror the animation.")]
    public bool mirrorMode;

    private Dictionary<int,SkeletonHandler> avatarControlList;
    public Dictionary<int, SkeletonHandler> AvatarControlList { get => avatarControlList;}

    private sl.BODY_FORMAT bodyFormat = sl.BODY_FORMAT.BODY_38;

    //private float alpha = 0.1f;

    #endregion

    /// <summary>
    /// Start this instance.
    /// </summary>
    void Start()
    {
        QualitySettings.vSyncCount = 1; // Activate vsync

        avatarControlList = new Dictionary<int,SkeletonHandler> ();
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

		if (zedManager)
        {

#if ZED_URP
            UniversalAdditionalCameraData urpCamData = zedManager.GetLeftCamera().GetComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;
            urpCamData.renderShadows = false;
#endif

            zedManager.OnZEDReady += OnZEDReady;
            zedManager.OnBodyTracking += UpdateSkeletonData;
		}
        bodyFormat = zedManager.bodyFormat;
    }

    private void OnZEDReady()
    {
        if (startBodyTrackingAutomatically && !zedManager.IsBodyTrackingRunning)
        {
            zedManager.StartBodyTracking();
        }
    }
	void OnDestroy()
    {
        if (zedManager)
        {
            zedManager.OnBodyTracking -= UpdateSkeletonData;
            zedManager.OnZEDReady -= OnZEDReady;
        }
    }

	/// <summary>
	/// Updates the skeleton data from ZEDCamera call and send it to Skeleton Handler script.
	/// </summary>
    private void UpdateSkeletonData(BodyTrackingFrame dframe)
    {
		List<int> remainingKeyList = new List<int>(avatarControlList.Keys);
		List<DetectedBody> newbodies = dframe.GetFilteredObjectList(showON, showSEARCHING, showOFF);

 		foreach (DetectedBody dbody in newbodies)
        {
			int person_id = dbody.rawBodyData.id;

			//Avatar controller already exist --> update position
			if (avatarControlList.ContainsKey(person_id))
			{
				SkeletonHandler handler = avatarControlList[person_id];
				UpdateAvatarControl(handler, dbody.rawBodyData);

				// remove keys from list
				remainingKeyList.Remove(person_id);
			}
			else
			{
                if (avatarControlList.Count < maximumNumberOfDetections)
                {
                    SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
                    Vector3 spawnPosition = zedManager.GetZedRootTansform().TransformPoint(dbody.rawBodyData.position);
                    handler.Create(avatar, bodyFormat);
                    handler.InitSkeleton(person_id, new Material(skeletonBaseMaterial));
                    avatarControlList.Add(person_id, handler);
                    UpdateAvatarControl(handler, dbody.rawBodyData);
                }
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
        DisplayDebugSkeleton = displayDebugSkeleton;
        OffsetDebugSkeleton = offsetDebugSkeleton;
        LogFusionMetrics = logFusionMetrics;    

        if (Input.GetKeyDown(KeyCode.Space))
        {
            useAvatar = !useAvatar;
        }

        // Adjust the 3D avatar to the bones rotations from the SDK each frame.
        // These rotations are stored, and updated each time data is received from Fusion.
        if (useAvatar)
        {
            foreach (var skelet in avatarControlList)
            {
                skelet.Value.Move();
            }
        }

    }

    /// <summary>
    /// Function to update avatar control with data from ZED SDK.
    /// </summary>
    /// <param name="handler">Handler.</param>
    /// <param name="data">BodyData to get values from.</param>
    private void UpdateAvatarControl(SkeletonHandler handler, sl.BodyData data)
	{
        Vector3[] worldJointsPos = new Vector3[handler.currentKeypointsCount]; 
        Quaternion[] normalizedLocalJointsRot = new Quaternion[handler.currentKeypointsCount];

        for (int i = 0; i < worldJointsPos.Length; i++)
        {
            worldJointsPos[i] = zedManager.GetZedRootTansform().TransformPoint(data.keypoint[i]);
            normalizedLocalJointsRot[i] = data.localOrientationPerJoint[i].normalized;
        }
        Quaternion worldGlobalRotation = zedManager.GetZedRootTansform().rotation * data.globalRootOrientation;

        if (data.localOrientationPerJoint.Length > 0 && data.keypoint.Length > 0 && data.keypointConfidence.Length > 0)
        {
            handler.SetConfidences(data.keypointConfidence);
            handler.SetControlWithJointPosition(
                worldJointsPos,
                normalizedLocalJointsRot, worldGlobalRotation,
                useAvatar, mirrorMode);
        }
    }
}
