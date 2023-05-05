using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HeightOffsetter))]
public class ZEDSkeletonAnimator : MonoBehaviour
{
    protected Animator animator;
    private HeightOffsetter heightOffsetter;

    #region inspector vars

    [Header("IK SETTINGS")]
    [Tooltip("Enable foot IK (feet on ground when near it)")]
    public bool enableFootIK = false;
    [Tooltip("EXPERIMENTAL: Filter feet movements caused by root offset when the feet should not be moving (on floor).")]
    public bool filterSlidingMovementsOnGround = false;
    [Tooltip("Distance (between sole and environment under it) under which a foot is considered on the floor.")]
    public float thresholdEnterGroundedState = .14f;
    [Tooltip("Distance (between sole and environment under it) under which a foot is considered on the floor. Used to check if the foot is still on the floor.")]
    public float thresholdLeaveGroundedState = .18f;
    [Tooltip("Radius of movements filtered when the filterMovementsOnGround parameter is enabled.")]
    public float groundedFreeDistance = .05f;
    [Tooltip("Layers detected as floor for the IK")]
    public LayerMask raycastDetectionLayers;
    [Tooltip("Coefficient of application of foot IK, position-wise.")]
    [Range(0f, 1f)]
    public float ikPositionApplicationRatio = 1f;
    [Tooltip("Coefficient of application of foot IK, rotation-wise.")]
    [Range(0f, 1f)]
    public float ikRotationApplicationRatio = 1f;

    [Header("RIG SETTINGS")]
    public Transform LeftFootTransform = null;
    public Transform RightFootTransform = null;
    public float ankleHeightOffset = 0.102f;
    public ZEDBodyTrackingManager bodyTrackingManager;

    [Header("Keyboard controls")]
    public KeyCode toggleFootIK = KeyCode.I;
    public KeyCode toggleFilterSlidingMovementsOnGround = KeyCode.F;
    public KeyCode resetAutomaticOffset = KeyCode.R;

    // Expected frequency of reception of new Body Tracking data, in FPS
    private float bodyTrackingFrequency = 30f;

    // Factor for the interpolation duration.
    // 0=>instant movement, no lerp; 1=>Rotation of the SDK should be done between two frames. 
    // More=>Interpolation will be longer, latency grow but movements will be smoother.
    private float smoothingFactor = 3f;

    #endregion

    #region vars

    private SkeletonHandler skhandler = null;
    public SkeletonHandler Skhandler { get => skhandler; set => skhandler = value; }

    private bool groundedL = false;
    private bool groundedR = false;

    private Vector3 rootHeightOffset = Vector3.zero;
    public Vector3 RootHeightOffset { get => rootHeightOffset; set => rootHeightOffset = value; }


    // if set to true, all animation will be handled by the MoveAnimator method of skeletonHandler in the OnAnimatorIk here.
    // If false, the Update function will handle the animation via the Move methode of skeletonHandler. Set
    // Set to true at the beginning of the OnAnimatorIK function.
    private bool ikPassIsEnabled = false;
    private bool raisePoseWasUpdatedIKFlag = false;

    /**
     * LERP DATA FOR IK TARGETS
     */
    private Vector3 startLerpPosL;
    private Vector3 targetLerpPosL;
    // necessary because Move() will reset it
    private Vector3 curEffectorPosL;

    private Vector3 startLerpPosR;
    private Vector3 targetLerpPosR;
    // necessary because Move() will reset it
    private Vector3 curEffectorPosR;

    private float totalLerpTime;

    private Vector3 ankleRPosBeforMove = Vector3.zero;
    private Vector3 ankleLPosBeforMove = Vector3.zero;

    #endregion

    #region debug vars

    [Header("Debug")]
    public float targetLerpPosMultiplier = 1f;
    public Color colorPosStartRay = Color.white;
    public Color colorAnkleRPlusHeightOffset = Color.gray;
    public Color colorPosHitRay = Color.black;

    private Vector3 posStartRay = Vector3.zero;

    #endregion

    private void Awake()
    {
        bodyTrackingManager = FindObjectOfType<ZEDBodyTrackingManager>();
        if (bodyTrackingManager == null)
        {
            Debug.LogError("ZEDManagerIK: No body tracking manager loaded!");
        }
        heightOffsetter = GetComponent<HeightOffsetter>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Applies the current local rotations of the rig to the animator.
    /// </summary>
    void ApplyAllRigRotationsOnAnimator()
    {
        skhandler.MoveAnimator();
    }

    /// <summary> 
    /// Computes Foot IK.
    /// 1) Apply bones rotations to animator 2) Apply root position and rotations. 3) Do Foot IK.
    /// </summary>
    void OnAnimatorIK()
    {
        ikPassIsEnabled = true;

        if (skhandler)
        {
            // 1) Update target positions and rotations.
            ApplyAllRigRotationsOnAnimator();

            // 2) Move gameobject position, effectively moving root position. (root relative position is (0,0,0) in the prefab)
            // This way, we have root motion and can easily apply effects following the main gameobject's transform.
            transform.position = skhandler.TargetBodyPositionWithHipOffset;
            transform.rotation = skhandler.TargetBodyOrientation;

            // 3) Manage Foot IK
            if (animator)
            {
                if (enableFootIK)
                {
                    Vector3 ankleHeightVector = new Vector3 (0, ankleHeightOffset, 0);

                    // If the retrieve bodies was called, the IK effectors should be updated
                    if(raisePoseWasUpdatedIKFlag)
                    {
                        PoseWasUpdatedIK();
                        raisePoseWasUpdatedIKFlag = false;
                    }

                    // Apply height offset to avatar.
                    ManageHeightOffset();

                    // Set the right foot target position and rotation, if one has been assigned
                    if (RightFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, ikPositionApplicationRatio);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, ikRotationApplicationRatio);
                        animator.SetIKHintPosition(AvatarIKHint.RightKnee, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position + animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).forward * 0.15f);
                        animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 1);

                        // move foot target
                        Vector3 effectorTargetPos;
                        effectorTargetPos = targetLerpPosR;

                        // start of the raycast: high above the foot.
                        posStartRay = effectorTargetPos + (Vector3.up * 5f);

                        // Fire raycast on 10m to find if there is any ground, to help to have a smooth transition between floored/not floored
                        Ray ray = new Ray(posStartRay, Vector3.down);
                        bool successHit = Physics.Raycast(ray, out RaycastHit hit, 2 * 5f, raycastDetectionLayers);

                        // distance between sole and floor
                        float dist = successHit ? Vector3.Distance(hit.point, effectorTargetPos - ankleHeightVector) : thresholdLeaveGroundedState * 8;

                        // move effector to floor and fully apply ik
                        if (successHit && dist < (thresholdEnterGroundedState))
                        {
                            effectorTargetPos = hit.point + ankleHeightVector;
                            groundedR = true;
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightToes).position - animator.GetBoneTransform(HumanBodyBones.RightFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(forward, hit.normal));
                        }
                        // move effector to floor and apply ik depending on distance to floor.
                        // If closer than thresholdLeaveGroundedState, fully apply. If further than 2*thresholdLeaveGroundedState, no apply.
                        // Maybe a test on groundedL is needed, depending on visual results.
                        else if (successHit)
                        {
                            effectorTargetPos = hit.point + ankleHeightVector;
                            groundedR = dist < (thresholdLeaveGroundedState);
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightToes).position - animator.GetBoneTransform(HumanBodyBones.RightFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(forward, hit.normal));
                            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, GetLinearIKRatio(dist, thresholdLeaveGroundedState * 2, thresholdLeaveGroundedState, ikRotationApplicationRatio));
                            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, GetLinearIKRatio(dist, thresholdLeaveGroundedState * 2, thresholdLeaveGroundedState, ikPositionApplicationRatio));

                        }
                        else
                        {
                            groundedR = false;
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightToes).position - animator.GetBoneTransform(HumanBodyBones.RightFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(forward, Vector3.up));
                            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
                            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                        }
                        // update current effector position because next Move() will reset it to the skeleton pose
                        curEffectorPosR = effectorTargetPos;

                        // set IK position and rotation
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, effectorTargetPos);
                    }

                    // Set the left foot target position and rotation, if one has been assigned
                    if (LeftFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, ikPositionApplicationRatio);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, ikRotationApplicationRatio);
                        animator.SetIKHintPosition(AvatarIKHint.LeftKnee, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position + animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).forward * 0.15f);
                        animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 1);

                        // move foot target
                        Vector3 effectorTargetPos;
                        effectorTargetPos = targetLerpPosL;

                        // start of the raycast: high above the foot.
                        posStartRay = effectorTargetPos + (Vector3.up * 5f);

                        // Fire raycast on 10m to find if there is any ground, to help to have a smooth transition between floored/not floored
                        Ray ray = new Ray(posStartRay, Vector3.down);
                        bool successHit = Physics.Raycast(ray, out RaycastHit hit, 2 * 5f, raycastDetectionLayers);

                        // distance between sole and floor
                        float dist = successHit ? Vector3.Distance(hit.point, effectorTargetPos - ankleHeightVector) : thresholdLeaveGroundedState * 8;

                        // move effector to floor and fully apply ik
                        if (successHit && dist < (thresholdEnterGroundedState))
                        {
                            effectorTargetPos = hit.point + ankleHeightVector;
                            groundedL = true;
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.LeftToes).position - animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(forward, hit.normal));
                        }
                        // move effector to floor and apply ik depending on distance to floor.
                        // If closer than thresholdLeaveGroundedState, fully apply. If further than 2*thresholdLeaveGroundedState, no apply.
                        // Maybe a test on groundedL is needed, depending on visual results.
                        else if (successHit)
                        {
                            effectorTargetPos = hit.point + ankleHeightVector;
                            groundedL = dist < (thresholdLeaveGroundedState);
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.LeftToes).position - animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(forward, hit.normal));
                            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, GetLinearIKRatio(dist, thresholdLeaveGroundedState * 2, thresholdLeaveGroundedState, ikRotationApplicationRatio));
                            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, GetLinearIKRatio(dist, thresholdLeaveGroundedState * 2, thresholdLeaveGroundedState, ikPositionApplicationRatio));

                        }
                        else
                        {
                            groundedL = false;
                            Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.LeftToes).position - animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.up);
                            animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(forward, Vector3.up));
                            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
                            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
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
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0);
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0);
                }
            }
        }
    }

    /// <summary>
    /// Computes and returns distance between the projections of <paramref name="vec1"/> and <paramref name="vec2"/> on an horizontal plane
    /// </summary>
    private float HorizontalDist(Vector3 vec1, Vector3 vec2)
    {
        return Vector2.Distance(new Vector2(vec1.x, vec1.z), new Vector2(vec2.x, vec2.z));
    }

    /// <summary>
    /// Sets raisePoseWasUpdatedIKFlag to true, so that the PoseWasUpdatedIK method will be called after the animation process in the IK pass.
    /// </summary>
    public void RaisePoseWasUpdatedIKFlag()
    {
        raisePoseWasUpdatedIKFlag = true;
    }

    /// <summary>
    /// Should be called each time the skeleton data is changed in the handler
    /// prepares new lerp data.
    /// Checks distance to filter feet parasite movements on floor.
    /// </summary>
    private void PoseWasUpdatedIK()
    {
        float confAnkleLeft = skhandler.currentConfidences[Skhandler.currentLeftAnkleIndex];
        float confAnkleRight = skhandler.currentConfidences[Skhandler.currentRightAnkleIndex];

        startLerpPosL = curEffectorPosL;

        try
        {
            targetLerpPosL = Skhandler.currentJoints[Skhandler.currentLeftAnkleIndex];
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        startLerpPosR = curEffectorPosR;

        try
        {
            targetLerpPosR = Skhandler.currentJoints[skhandler.currentRightAnkleIndex];
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        if (bodyTrackingManager && bodyTrackingManager.mirrorMode)
        {
            Vector3 vL = targetLerpPosL;
            Vector3 vR = targetLerpPosR;

            targetLerpPosL = vR.mirror_x();
            targetLerpPosR = vL.mirror_x();
        }

        if (filterSlidingMovementsOnGround)
        {
            targetLerpPosL = (groundedL && HorizontalDist(startLerpPosL, targetLerpPosL) < groundedFreeDistance)
                ? new Vector3(startLerpPosL.x, targetLerpPosL.y, startLerpPosL.z)
                : targetLerpPosL;
        }

        if (filterSlidingMovementsOnGround)
        {
            targetLerpPosR = (groundedR && HorizontalDist(startLerpPosR, targetLerpPosR) < groundedFreeDistance)
                ? new Vector3(startLerpPosR.x, targetLerpPosR.y, startLerpPosR.z)
                : targetLerpPosR;
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

    /// <summary>
    /// Gets a linear interpolation of the application ratio for the IK depending on the distance to the floor.
    /// Used for smoothing the transition between grounded and not.
    /// </summary>
    /// <param name="d">Distance between sole and floor (not ankle and floor).</param>
    /// <param name="tMin">Threshold min: Distance above this value implies no application of IK.</param>
    /// <param name="tMax">Threshold max: Distance under this value implies full (depending on setting) application of IK.</param>
    /// <param name="rMax">Max ratio (default 1).</param>
    /// <returns>Ratio of IK application between 0 and rmax, depending on d, tmin and tmax./returns>
    private float GetLinearIKRatio(float d, float tMin, float tMax, float rMax = 1)
    {
        return Mathf.Min(1,Mathf.Max(0, rMax * (tMin - d) / (tMin - tMax) ));
    }

    private void LateUpdate()
    {
        ankleRPosBeforMove = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        ankleLPosBeforMove = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

        if (!enableFootIK)
        {
            ManageHeightOffset();
        }
    }

    /// <summary>
    /// Utility function to compute and apply the height offset from the height offset manager to the body.
    /// </summary>
    private void ManageHeightOffset()
    {
        float confAnkleLeft = skhandler.currentConfidences[Skhandler.currentLeftAnkleIndex];
        float confAnkleRight = skhandler.currentConfidences[Skhandler.currentRightAnkleIndex];

        //// height offset management
        rootHeightOffset = heightOffsetter.ComputeRootHeightOffsetXFrames(
        confAnkleLeft,
        confAnkleRight,
        ankleLPosBeforMove,
        ankleRPosBeforMove,
        ankleHeightOffset);
        transform.position = Skhandler.TargetBodyPositionWithHipOffset + rootHeightOffset / 2;
        transform.rotation = Skhandler.TargetBodyOrientation;
    }

    private void Update()
    {
        /// KEY INPUTS
        if (Input.GetKeyDown(toggleFootIK))
        {
            enableFootIK = !enableFootIK;
        }

        if (Input.GetKeyDown(toggleFilterSlidingMovementsOnGround))
        {
            filterSlidingMovementsOnGround = !filterSlidingMovementsOnGround;
        }

        // reset automatic height offset calibration
        if (Input.GetKeyDown(resetAutomaticOffset))
        {
            heightOffsetter.CurrentheightOffset = 0;
        }

        // manage animation if no ik pass or animator
        if(!ikPassIsEnabled)
        {
            Skhandler.Move();
        }
    }
}