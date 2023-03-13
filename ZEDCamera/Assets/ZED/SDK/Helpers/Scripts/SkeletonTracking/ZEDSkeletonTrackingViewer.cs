//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;

#if ZED_URP
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Contols the ZEDSkeletonTracking . Links the SDK to Unity
/// </summary>
[DisallowMultipleComponent]
public class ZEDSkeletonTrackingViewer : MonoBehaviour
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
    public sl.BODY_FORMAT bodyModel = sl.BODY_FORMAT.BODY_38;

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

    [Header("MOVEMENT SMOOTHING SETTINGS")]
    [Tooltip("Expected frequency of reception of new Body Tracking data, in FPS")]
    [SerializeField]
    private float bodyTrackingFrequency = 30f;
    public static float BodyTrackingFrequency = 30f;
    [Tooltip("Factor for the interpolation duration. " +
        "\n0=>instant movement, no lerp; 1=>Rotation of the SDK should be done between two frames. More=>Interpolation will be longer, latency grow but movements will be smoother.")]
    [SerializeField]
    private float smoothingFactor = 3f;
    public static float SmoothingFactor = 3f;

    private Dictionary<int,SkeletonHandler> avatarControlList;
    public Dictionary<int, SkeletonHandler> AvatarControlList { get => avatarControlList;}

    //private float alpha = 0.1f;

    #endregion

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

#if ZED_URP
            UniversalAdditionalCameraData urpCamData = zedManager.GetLeftCamera().GetComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;
            urpCamData.renderShadows = false;
#endif

            zedManager.OnZEDReady += OnZEDReady;
            zedManager.OnBodyTracking += UpdateSkeletonData;
		}

        if (zedManager.bodyTrackingModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX || zedManager.bodyTrackingModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE || zedManager.bodyTrackingModel == sl.DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM )
        {
            Debug.LogWarning("MULTI_CLASS_BOX model can't be used for skeleton tracking, please use either HUMAN_BODY_FAST or HUMAN_BODY_ACCURATE");
        }

    }

    private void OnZEDReady()
    {
        if (startBodyTrackingAutomatically && !zedManager.IsBodyTrackingRunning)
        {
            zedManager.StartBodyTracking();
        }
    }
	private void OnDestroy()
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
				SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
                Vector3 spawnPosition = zedManager.GetZedRootTansform().TransformPoint(dbody.rawBodyData.position);
                handler.Create(avatar, bodyModel);
                handler.InitSkeleton(person_id, new Material(skeletonBaseMaterial));
                avatarControlList.Add(person_id, handler);
                UpdateAvatarControl(handler, dbody.rawBodyData);
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
        BodyTrackingFrequency = bodyTrackingFrequency;    
        SmoothingFactor = smoothingFactor;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            useAvatar = !useAvatar;
            if (useAvatar)
            {
                if (zedManager.enableBodyFitting)
                    Debug.Log("<b><color=green> Switch to Avatar mode</color></b>");

            }
            else
                Debug.Log("<b><color=green> Switch to Skeleton mode</color></b>");
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

        UpdateViewCameraPosition();
    }
     
	/// <summary>
	/// Function to update avatar control with data from ZED SDK.
	/// </summary>
	/// <param name="handler">Handler.</param>
	/// <param name="p">P.</param>
	private void UpdateAvatarControl(SkeletonHandler handler, sl.BodyData data)
	{
        Vector3[] worldJointsPos; 
        Quaternion[] worldJointsRot;
        switch (bodyModel)
        {
            case sl.BODY_FORMAT.BODY_34:
                worldJointsPos = new Vector3[34];
                worldJointsRot = new Quaternion[34];
                break;
            case sl.BODY_FORMAT.BODY_38:
                worldJointsPos = new Vector3[38];
                worldJointsRot = new Quaternion[38];
                break;
            case sl.BODY_FORMAT.BODY_70:
                worldJointsPos = new Vector3[70];
                worldJointsRot = new Quaternion[70];
                break;
            default:
                Debug.LogError("Invalid body model, select at least BODY_34 to use a 3D avatar, defaulting to 38");
                worldJointsPos = new Vector3[38];
                worldJointsRot = new Quaternion[38];
                break;
        }


        for (int i = 0; i < worldJointsPos.Length; i++)
        {
            worldJointsPos[i] = zedManager.GetZedRootTansform().TransformPoint(data.keypoint[i]);
            worldJointsRot[i] = data.localOrientationPerJoint[i].normalized;
        }

        if (data.localOrientationPerJoint.Length > 0 && data.keypoint.Length > 0 && data.keypointConfidence.Length > 0)
        {
            handler.SetConfidences(data.keypointConfidence);
            handler.SetControlWithJointPosition(
                handler.SkBodyModel, worldJointsPos,
                worldJointsRot, data.globalRootOrientation,
                useAvatar, mirrorMode);
        }

    }

    void UpdateViewCameraPosition()
    {
        viewCamera.transform.position = zedManager.transform.localPosition;
        viewCamera.transform.rotation = zedManager.transform.localRotation;
    }
}
