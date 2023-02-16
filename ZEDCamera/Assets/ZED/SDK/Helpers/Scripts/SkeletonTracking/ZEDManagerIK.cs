using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HeightOffsetter))]
public class ZEDManagerIK : MonoBehaviour
{
    protected Animator animator;
    private HeightOffsetter heightOffsetter;

    #region inspector vars

    [Header("IK SETTINGS")]
    [Tooltip("Enable foot IK (feet on ground when near it)")]
    public bool enableFootIK = true;
    [Tooltip("EXPERIMENTAL: Filter feet movements caused by root offset when the feet should not be moving (on floor).")]
    public bool filterMovementsOnGround = false;
    [Tooltip("Distance (between ankle and environment under it) under which a foot is considered on the floor.")]
    public float thresholdEnterGroundedState = .14f;
    [Tooltip("Distance (between ankle and environment under it) under which a foot is considered on the floor. Used to check if the foot is still on the floor.")]
    public float thresholdLeaveGroundedState = .18f;
    [Tooltip("Radius of movements filtered when the filterMovementsOnGround parameter is enabled.")]
    public float groundedFreeDistance = .05f;
    [Tooltip("Layers detected as floor for the IK")]
    public LayerMask raycastDetectionLayers;
    [Range(0f, 1f)]
    public float ikApplicationRatio = 1f;

    [Header("RIG SETTINGS")]
    public Transform LeftFootTransform = null;
    public Transform RightFootTransform = null;
    public Transform _rootJoint;
    public Vector3 ankleHeightOffset = new Vector3(0, 0.102f, 0);
    public ZEDSkeletonTrackingViewer bodyTrackingManager;

    [Header("SMOOTHING SETTINGS")]
    [Tooltip("Frequency of reception of new OD data, in FPS")]
    public float objectDetectionFrequency = 30f;
    [Tooltip("Latency of interpolation. 1=no latency; 0=instant movement, no lerp;")]
    public float lerpLatency = 3f;

    #endregion

    #region vars

    private SkeletonHandler skhandler = null;
    public SkeletonHandler Skhandler { get => skhandler; set => skhandler = value; }

    private bool groundedL = false;
    private bool groundedR = false;

    private Vector3 currentPosFootL;
    private Vector3 currentPosFootR;

    private Vector3 normalL = Vector3.up;
    private Vector3 normalR = Vector3.up;

    private Vector3 currentGroundedPosL = Vector3.zero;
    private Vector3 currentGroundedPosR = Vector3.zero;

    private Vector3 rootHeightOffset = Vector3.zero;
    public Vector3 RootHeightOffset { get => rootHeightOffset; set => rootHeightOffset = value; }
    public bool HeightOffsetStabilized { get => heightOffsetStabilized; set => heightOffsetStabilized = value; }

    /**
     * LERP DATA FOR IK TARGETS
     */
    private Vector3 startLerpPosL;
    private Vector3 targetLerpPosL;
    // necessary because Move() will reset it
    private Vector3 curEffectorPosL;
    private float curTValL;

    private Vector3 startLerpPosR;
    private Vector3 targetLerpPosR;
    // necessary because Move() will reset it
    private Vector3 curEffectorPosR;
    private float curTValR;

    private float totalLerpTime;
    private float curLerpTimeL;
    private float curLerpTimeR;

    private Vector3 ankleRPosBeforMove = Vector3.zero;
    private Vector3 ankleLPosBeforMove = Vector3.zero;

    #endregion


    [SerializeField]
    [Tooltip("Number of frames when both feet are visible used to stabilize the automatic offset")]
    private int nbCalibrationFrames = 150;
    private int curCalibrationFrames = -1;
    [SerializeField]
    private bool heightOffsetStabilized = false;
    [SerializeField]
    private bool footIKLockedForCalib = false;

    [Header("Debug")]
    public float targetLerpPosMultiplier = 1f;
    public Color colorAnkleRBeforeMove = Color.white;
    public Color colorAnkleRPlusHeightOffset = Color.gray;
    public Color colorAnkleRAfterMove = Color.black;

    private void Awake()
    {
        bodyTrackingManager = (ZEDSkeletonTrackingViewer)FindObjectOfType(typeof(ZEDSkeletonTrackingViewer));
        if (bodyTrackingManager == null)
        {
            Debug.LogError("ZEDManagerIK: No body tracking manager loaded!");
        }
        heightOffsetter = GetComponent<HeightOffsetter>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        currentGroundedPosL = LeftFootTransform.position;
        currentGroundedPosR = RightFootTransform.position;
        footIKLockedForCalib = true;
    }

    /**
     * a callback for calculating IK
     * 1) Apply bones rotations 2) Apply root position and rotations. 3) Do Foot IK.
    */
    void OnAnimatorIK()
    {
        if (skhandler)
        {
            // 1) Update target positions and rotations, and apply rotations.
            // This needs to be called each frame, else there will be a jitter between the resting and correct poses of the skeleton.
            skhandler.Move();

            // 2) Move actor position, effectively moving root position. (root relative position is (0,0,0) in the prefab)
            // This way, we have root motion and can easily apply effects following the main gameobject's transform.
            transform.position = skhandler.TargetBodyPositionWithHipOffset;
            transform.rotation = skhandler.TargetBodyOrientation;

            //// height offset management
            heightOffsetter.ComputeRootHeightOffsetXFrames(
            skhandler.confidences[SkeletonHandler.JointType_AnkleLeft],
            skhandler.confidences[SkeletonHandler.JointType_AnkleRight],
            ankleLPosBeforMove,
            ankleRPosBeforMove,
            ankleHeightOffset.y);
            transform.position += rootHeightOffset + ankleHeightOffset;

            // 3) Manage Foot IK
            if (animator)
            {
                //if the IK is active, set the position and rotation directly to the goal.
                if (enableFootIK && !footIKLockedForCalib)
                {
                    // Set the right foot target position and rotation, if one has been assigned
                    if (RightFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, ikApplicationRatio);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, ikApplicationRatio);

                        // init/prepare values
                        currentPosFootR = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                        Vector3 effectorTargetPos;

                        // move foot target
                        effectorTargetPos = AccumulateLerpVec3(startLerpPosR, targetLerpPosR, ref curTValR, ref curLerpTimeR);

                        Ray ray = new Ray(effectorTargetPos + (Vector3.up * (groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 2 * (groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = CustomInterp(startLerpPosR, hit.point + ankleHeightOffset, ref curLerpTimeR);
                            normalR = hit.normal;
                            groundedR = true;
                            animator.SetIKRotation(
                              AvatarIKGoal.RightFoot,
                              Quaternion.FromToRotation(
                                  animator.GetBoneTransform(HumanBodyBones.RightFoot).up, normalL) * animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation);
                        }
                        else
                        {
                            groundedR = false;
                            normalR = animator.GetBoneTransform(HumanBodyBones.RightFoot).up;
                            animator.SetIKRotation(
                                 AvatarIKGoal.RightFoot,
                                 animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation);
                        }

                        // update current effector position because next Move() will reset it to the skeleton pose
                        curEffectorPosR = effectorTargetPos;

                        // set IK position and rotation
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, effectorTargetPos);
                    }
                    // Set the right foot target position and rotation, if one has been assigned
                    if (LeftFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, ikApplicationRatio);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, ikApplicationRatio);

                        // init/prepare values
                        currentPosFootL = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                        Vector3 effectorTargetPos;

                        // move foot target
                        effectorTargetPos = AccumulateLerpVec3(startLerpPosL, targetLerpPosL, ref curTValL, ref curLerpTimeL);

                        Ray ray = new Ray(effectorTargetPos + (Vector3.up * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 2 * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = CustomInterp(startLerpPosL, hit.point + ankleHeightOffset, ref curLerpTimeL);
                            normalL = hit.normal;
                            groundedL = true;
                            animator.SetIKRotation(
                                 AvatarIKGoal.LeftFoot,
                                 Quaternion.FromToRotation(
                                     animator.GetBoneTransform(HumanBodyBones.LeftFoot).up, normalR) * animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation);
                        }
                        else
                        {
                            groundedL = false;
                            normalL = animator.GetBoneTransform(HumanBodyBones.LeftFoot).up; animator.SetIKRotation(
                              AvatarIKGoal.LeftFoot,
                              animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation);
                        }
                        // update current effector position because next Move() will reset it to the skeleton pose
                        curEffectorPosL = effectorTargetPos;

                        // set IK position and rotation
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, effectorTargetPos);

                    }
                }

                //if the IK is not active, set the position and rotation of the hand and head back to the original position
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
                }
            }
        }
    }

    // computes distance between the projections of vec1 and vec2 on an horizontal plane
    private float HorizontalDist(Vector3 vec1, Vector3 vec2)
    {
        return Vector2.Distance(new Vector2(vec1.x, vec1.z), new Vector2(vec2.x, vec2.z));
    }

    // Should be called each time the skeleton data is changed in the handler
    // prepares new lerp data.
    // Checks distance to filter feet parasite movements on floor.
    public void PoseWasUpdated()
    {
        startLerpPosL = curEffectorPosL;

        try
        {
            targetLerpPosL = bodyTrackingManager.mirrorMode ?
                skhandler.joints[SkeletonHandler.JointType_AnkleLeft] + rootHeightOffset :
                skhandler.joints[SkeletonHandler.JointType_AnkleLeft] + rootHeightOffset;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        if (filterMovementsOnGround)
        {
            targetLerpPosL = (groundedL && HorizontalDist(startLerpPosL, targetLerpPosL) < groundedFreeDistance)
                ? startLerpPosL
                : targetLerpPosL;
        }

        startLerpPosR = curEffectorPosR;

        try
        {
            targetLerpPosR = bodyTrackingManager.mirrorMode ?
                skhandler.joints[SkeletonHandler.JointType_AnkleRight] + rootHeightOffset :
                skhandler.joints[SkeletonHandler.JointType_AnkleRight] + rootHeightOffset;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        if (filterMovementsOnGround)
        {
            targetLerpPosR = (groundedR && HorizontalDist(startLerpPosR, targetLerpPosR) < groundedFreeDistance)
                ? startLerpPosR
                : targetLerpPosR;
        }

        if (bodyTrackingManager && bodyTrackingManager.mirrorMode)
        {
            Vector3 vL = targetLerpPosL;
            Vector3 vR = targetLerpPosR;

            targetLerpPosL = vR.mirror_x();
            targetLerpPosR = vL.mirror_x();
        }

        // define totallerptime = 1/ODFrequency
        // reset curlerptime
        curLerpTimeL = 0;
        curLerpTimeR = 0;
        curTValL = 0;
        curTValR = 0;
        totalLerpTime = 1 / objectDetectionFrequency;
        totalLerpTime *= lerpLatency;

        if (!HeightOffsetStabilized && heightOffsetter.automaticOffset
            && skhandler.confidences[SkeletonHandler.JointType_AnkleLeft] > 0
            && skhandler.confidences[SkeletonHandler.JointType_AnkleRight] > 0)
        {
            curCalibrationFrames++;
            heightOffsetStabilized = curCalibrationFrames > nbCalibrationFrames;
            footIKLockedForCalib = !(HeightOffsetStabilized && enableFootIK);
        }
    }

    private Vector3 AccumulateLerpVec3(Vector3 startPos, Vector3 targetPos, ref float curTVal, ref float currentLerpTime)
    {
        if (Vector3.Distance(startPos, targetPos) > .00001f)
        {
            currentLerpTime += Time.deltaTime;
            float t = currentLerpTime / totalLerpTime;
            t = Mathf.Clamp(t, 0, 1);
            t = (Mathf.Sin(t * Mathf.PI * 0.5f));
            curTVal = t;
            return Vector3.Lerp(startPos, targetPos, t);
        }
        else
        {
            return startPos;
        }
    }

    private Vector3 CustomInterp(Vector3 startPos, Vector3 targetPos, ref float currentLerpTime)
    {
        if (Vector3.Distance(startPos, targetPos) > .00001f)
        {
            float t = currentLerpTime / totalLerpTime;
            t = Mathf.Clamp(t, 0, 1);
            t = (Mathf.Sin(t * Mathf.PI * 0.5f));
            return Vector3.Lerp(startPos, targetPos, t);
        }
        else
        {
            return startPos;
        }
    }

    private void LateUpdate()
    {
        ankleRPosBeforMove = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        ankleLPosBeforMove = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            enableFootIK = !enableFootIK;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            filterMovementsOnGround = !filterMovementsOnGround;
        }

        // reset automatic height offset calibration
        if (Input.GetKeyDown(KeyCode.R))
        {
            curCalibrationFrames = 0;
            heightOffsetStabilized = false;
            footIKLockedForCalib = true;
        }
    }

}