//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Collections;

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

    [Tooltip("Canvas on which the ZED's video feed is displayed. Used to mirror the video feed in mirror mode.")]
    public GameObject cameraFrame;

	/// <summary>
	/// Activate skeleton tracking when play mode is on and ZED ready
	/// </summary>
	[Header("------ Game Control ------")]

    public bool startBodyTrackingAutomatically = true;

    /// <summary>
    /// Vizualisation mode. Use a 3D model or only display the skeleton
    /// </summary>
    [Header("------ Vizualisation Mode ------")]
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
    [Header("------ State Filters ------")]
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

    [Tooltip("Delay before spawning an avatar. At the end, the tracking state is checked, and if the filters pass then the avatar is spawned.\n" +
        "Use this to reduce popups of avatars in case of partial occlusion/tracked people at the edges of the ZED's frustum.")]
    public float delayBeforeSpawn= 1f;


    [Header("------ Avatar Control ------")]
    /// <summary>
    /// Avatar game object
    /// </summary>
    [Tooltip("3D Rigged model.")]
    public GameObject avatar;
    public Material skeletonBaseMaterial;
    [Tooltip("Display bones and joints along 3D avatar")]
    [SerializeField]
    private bool displaySDKSkeleton = false;
    public static bool DisplaySDKSkeleton = false;
    [SerializeField]
    private bool applyHeighOffsetToSDKSkeleton = false;
    public static bool ApplyHeighOffsetToSDKSkeleton = false;
    [SerializeField]
    private Vector3 offsetSDKSkeleton = new Vector3(0f, 0f, 0f);
    public static Vector3 OffsetSDKSkeleton = new Vector3(0f, 0f, 0f);
    [Tooltip("Mirror the animation.")]
    public bool mirrorMode;

    private Dictionary<int,SkeletonHandler> avatarControlList;
    public Dictionary<int, SkeletonHandler> AvatarControlList { get => avatarControlList;}
    public bool EnableSmoothing { get => enableSmoothing; set => enableSmoothing = value; }
    public bool EnableFootIK { get => enableFootIK; set => enableFootIK = value; }
    public bool EnableFootLocking { get => enableFootLocking; set => enableFootLocking = value; }

    private sl.BODY_FORMAT bodyFormat = sl.BODY_FORMAT.BODY_38;
    [Space(10)]
    [Header("------ Heigh Offset ------")]
    [Tooltip("Height offset applied to transform each frame.")]
    public Vector3 manualOffset = Vector3.zero;
    [Tooltip("Automatic offset adjustment: Finds an automatic offset that sets both feet above ground, and at least one foot on the ground.")]
    public bool automaticOffset = false;
    [Tooltip("Step in manual increase/decrease of offset.")]
    public float offsetStep = 0.1f;

    [Space(5)]
    [Header("------ Animation Smoothing ------")]
    [Tooltip("Animation smoothing setting. 0 = No latency, no smoothing. 1 = Maximum latency.\n Tweak this value depending on your framerate, and the fps of the camera."), Range(0f,1f)]
    public float smoothingValue = .2f;
    [SerializeField]
    [Tooltip("Enable animation smoothing or not (induces latency).")]
    private bool enableSmoothing = true;

    [Space(5)]
    [Header("------ Experimental - IK Settings ------")]
    [Tooltip("Enable foot IK (feet on ground when near it)")]
    [SerializeField]
    private bool enableFootIK = false;
    [SerializeField]
    [Tooltip("Enable animation smoothing or not (induces latency).")]
    private bool enableFootLocking = true;
    [Tooltip("Foot locking smoothing setting. 0 = No latency, no smoothing. 1 = \"Full latency\" so no movement.\n Tweak this value depending on your framerate, and the fps of the camera.\nValues closer to 1 induce more latency, but improve fluidity."), Range(0f, 1f)]
    public float footLockingSmoothingValue = .8f;

    [Space(5)]
    [Header("------ Keyboard mapping ------")]
    public KeyCode toggleFootIK = KeyCode.I;
    public KeyCode toggleFootLock = KeyCode.F;
    public KeyCode toggleMirrorMode = KeyCode.M;
    public KeyCode toggleAutomaticHeightOffset = KeyCode.O;
    public KeyCode increaseOffsetKey = KeyCode.UpArrow;
    public KeyCode decreaseOffsetKey = KeyCode.DownArrow;

    //private float alpha = 0.1f;

    #endregion

    private void Awake()
    {
        if(cameraFrame == null)
        {
            Debug.LogError("ZEDBodyTrackingManager: Set Camera Frame. It is located under ZED_Rig_X -> Camera_X -> Frame.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
        }
    }

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

        if(avatar.GetComponent<Animator>().runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animator has no animator controller. Animation from ZED plugin will not work.");
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
        StartCoroutine(TimerToMirrorCanvas());
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
    /// Coroutine to spawn an avatar only after it's been detected for a set duration.
    /// </summary>
    /// <returns></returns>
    IEnumerator InstanciateAvatarWithDelay(float delay, DetectedBody dbody)
    {
        SkeletonHandler handler = ScriptableObject.CreateInstance<SkeletonHandler>();
        handler.Create(avatar, bodyFormat);
        handler.InitSkeleton(dbody.rawBodyData.id, new Material(skeletonBaseMaterial));

        avatarControlList.Add(dbody.rawBodyData.id, handler);
        UpdateAvatarControl(handler, dbody.rawBodyData);
        handler.GetAnimator().gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);

        if (avatarControlList.ContainsKey(dbody.rawBodyData.id)) handler.zedSkeletonAnimator.canSpawn=true;
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
                    StartCoroutine(InstanciateAvatarWithDelay(delayBeforeSpawn, dbody));
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
        DisplaySDKSkeleton = displaySDKSkeleton;
        OffsetSDKSkeleton = offsetSDKSkeleton;  
        ApplyHeighOffsetToSDKSkeleton = applyHeighOffsetToSDKSkeleton;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            useAvatar = !useAvatar;
        }

        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            displaySDKSkeleton = !displaySDKSkeleton;
        }

        if (Input.GetKeyDown(toggleFootIK))
        {
            enableFootIK = !enableFootIK;
        }

        if (Input.GetKeyDown(toggleFootLock))
        {
            enableFootLocking = !enableFootLocking;
        }

        if (Input.GetKeyDown(toggleMirrorMode))
        {
            mirrorMode = !mirrorMode;
            MirrorCanvas(mirrorMode);
        }

        if (Input.GetKeyDown(increaseOffsetKey))
        {
            manualOffset.y += offsetStep;
        }
        else if (Input.GetKeyDown(decreaseOffsetKey))
        {
            manualOffset.y -= offsetStep;
        }
        if (Input.GetKeyDown(toggleAutomaticHeightOffset))
        {
            automaticOffset = !automaticOffset;
        }

        // Display avatars or not depending on useAvatar setting.
        foreach (var skelet in avatarControlList)
        {
            skelet.Value.zedSkeletonAnimator.TryShowAvatar(useAvatar);
        }

    }

    IEnumerator TimerToMirrorCanvas()
    {
        yield return new WaitForSeconds(1.0f);
        MirrorCanvas(mirrorMode);
    }

    public void MirrorCanvas(bool mirror)
    {
        float newX = Mathf.Abs(cameraFrame.transform.localScale.x);
        newX *= mirror? -1.0f : 1.0f;
        cameraFrame.transform.localScale = new Vector3(
            newX,
            cameraFrame.transform.localScale.y,
            cameraFrame.transform.localScale.z);   
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
            if (enableFootLocking)
            {
                handler.CheckFootLockAnimator();
            }
        }
    }
}
