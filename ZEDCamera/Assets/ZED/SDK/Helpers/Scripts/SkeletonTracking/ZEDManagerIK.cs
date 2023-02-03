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

    [Header("RIG SETTINGS")]
    public Transform LeftFootTransform = null;
    public Transform RightFootTransform = null;
    public Transform _rootJoint;
    public Vector3 ankleHeightOffset = new Vector3(0, 0.102f, 0);
    
    [Header("SMOOTHING SETTINGS")]
    [Tooltip("Frequency of reception of new OD data, in FPS")]
    public float objectDetectionFrequency = 30f;
    [Tooltip("Latency of interpolation. 1=no latency; 0=instant movement, no lerp;")]
    public float lerpLatency = 3f;

    public float gizmoSize = .15f;

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

    #endregion

    private Vector3 bluecubepos = Vector3.zero;
    private Vector3 grincubepos = Vector3.zero;
    private Vector3 blakcubepos = Vector3.zero;
    private Vector3 gizAnkleRBeforeMove = Vector3.zero;
    private Vector3 gizAnkleRPlusHeightOffset = Vector3.zero;
    private Vector3 gizAnkleRAfterMove = Vector3.zero;

    [Header("Debug")]
    public float targetLerpPosMultiplier = 1f;
    public Color colorAnkleRBeforeMove = Color.white;
    public Color colorAnkleRPlusHeightOffset = Color.gray;
    public Color colorAnkleRAfterMove = Color.black;

    void Start()
    {
        animator = GetComponent<Animator>();
        heightOffsetter = GetComponent<HeightOffsetter>();
        currentGroundedPosL = LeftFootTransform.position;
        currentGroundedPosR = RightFootTransform.position;
    }

    /**
     * a callback for calculating IK
     * 1) Apply bones rotations 2) Apply root position and rotations. 3) Do Foot IK.
    */
    void OnAnimatorIK()
    {
        if (skhandler)
        {
            bluecubepos = transform.position;

            // 1) Update target positions and rotations, and apply rotations.
            // This needs to be called each frame, else there will be a jitter between the resting and correct poses of the skeleton.
            skhandler.Move();

            // 2) Move actor position, effectively moving root position. (root relative position is (0,0,0) in the prefab)
            // This way, we have root motion and can easily apply effects following the main gameobject's transform.
            transform.position = skhandler.TargetBodyPositionWithHipOffset/* + rootHeightOffset*/;
            transform.rotation = skhandler.TargetBodyOrientation;

            //gizAnkleRBeforeMove = animator.GetBoneTransform(HumanBodyBones.Hips).position;
            gizAnkleRBeforeMove = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

            //// height offset management
            heightOffsetter.ComputeRootHeightOffset(
            skhandler.confidences[SkeletonHandler.JointType_AnkleLeft],
            skhandler.confidences[SkeletonHandler.JointType_AnkleRight],
            animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,
            animator.GetBoneTransform(HumanBodyBones.RightFoot).position, 
            //grincubepos/* + ankleHeightOffset*/,
            //blakcubepos/* + ankleHeightOffset*/,
            //LeftFootTransform.position,
            //RightFootTransform.position, 
            ankleHeightOffset.y);

            /** 
             * DEBUG: Apply twice
             */
            transform.position += rootHeightOffset + ankleHeightOffset;

            //gizAnkleRAfterMove = animator.GetBoneTransform(HumanBodyBones.Hips).position;
            gizAnkleRAfterMove = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            gizAnkleRPlusHeightOffset = gizAnkleRAfterMove + rootHeightOffset;
            //gizAnkleRPlusHeightOffset = gizAnkleRAfterMove + rootHeightOffset;

            grincubepos = LeftFootTransform.position;
            blakcubepos = RightFootTransform.position;

            // 3) Manage Foot IK
            if (animator)
            {
                //if the IK is active, set the position and rotation directly to the goal.
                if (enableFootIK)
                {
                    // Set the right foot target position and rotation, if one has been assigned
                    if (RightFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

                        // init/prepare values
                        currentPosFootR = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                        Vector3 effectorTargetPos;

                        // move foot target
                        effectorTargetPos = AccumulateLerpVec3(startLerpPosR, targetLerpPosR, ref curTValR, ref curLerpTimeR);

                        Ray ray = new Ray(effectorTargetPos + (Vector3.up * (groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 2 * (groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = hit.point + ankleHeightOffset;
                            normalR = hit.normal;
                            groundedR = true;
                        }
                        else
                        {
                            groundedR = false;
                            normalR = animator.GetBoneTransform(HumanBodyBones.RightFoot).up;
                        }
                        //Debug.Log("groundedR: " + groundedR);

                        // update current effector position because next Move() will reset it to the skeleton pose
                        curEffectorPosR = effectorTargetPos;
                        blakcubepos = curEffectorPosR;

                        // set IK position and rotation
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, effectorTargetPos);
                        animator.SetIKRotation(
                             AvatarIKGoal.RightFoot,
                             Quaternion.FromToRotation(
                                 animator.GetBoneTransform(HumanBodyBones.RightFoot).up, normalL) * animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation);
                    }
                    // Set the right foot target position and rotation, if one has been assigned
                    if (LeftFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

                        // init/prepare values
                        currentPosFootL = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                        Vector3 effectorTargetPos;

                        // move foot target
                        effectorTargetPos = AccumulateLerpVec3(startLerpPosL, targetLerpPosL, ref curTValL, ref curLerpTimeL);

                        Ray ray = new Ray(effectorTargetPos + (Vector3.up * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 2 * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = hit.point + ankleHeightOffset;
                            normalL = hit.normal;
                            groundedL = true;
                        }
                        else
                        {
                            groundedL = false;
                            normalL = animator.GetBoneTransform(HumanBodyBones.LeftFoot).up;
                        }
                        // update current effector position because next Move() will reset it to the skeleton pose
                        curEffectorPosL = effectorTargetPos;
                        grincubepos = curEffectorPosL;

                        // set IK position and rotation
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, effectorTargetPos);
                        animator.SetIKRotation(
                             AvatarIKGoal.LeftFoot,
                             Quaternion.FromToRotation(
                                 animator.GetBoneTransform(HumanBodyBones.LeftFoot).up, normalR) * animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation);
                    }
                }

                //if the IK is not active, set the position and rotation of the hand and head back to the original position
                else
                {
                    animator.SetIKPosition(AvatarIKGoal.RightFoot, targetLerpPosR);
                    animator.SetIKRotation(AvatarIKGoal.RightFoot, RightFootTransform.rotation);
                    animator.SetIKPosition(AvatarIKGoal.LeftFoot, targetLerpPosL);
                    animator.SetIKRotation(AvatarIKGoal.LeftFoot, LeftFootTransform.rotation);

                    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  0);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,  0);
                }
            }
        }
    }

    // computes distance between the projections of vec1 and vec2 on an horizontal plane
    private float HorizontalDist(Vector3 vec1, Vector3 vec2)
    {
        float d = Vector2.Distance(new Vector2(vec1.x, vec1.z), new Vector2(vec2.x, vec2.z));
        if(d <= groundedFreeDistance)
        {
            Debug.Log("DISTH = " + d + " / "+ vec1 + " / "+ vec2);
        }
        return d;
    }

    // Should be called each time the skeleton data is changed in the handler
    // prepares new lerp data.
    // Checks distance to filter feet parasite movements on floor.
    public void PoseWasUpdated()
    {
        Debug.LogWarning("POSE WAS UPDATED");
        startLerpPosL = curEffectorPosL;

        targetLerpPosL = skhandler.joints[SkeletonHandler.JointType_AnkleLeft] + targetLerpPosMultiplier * rootHeightOffset;
        if (filterMovementsOnGround)
        {
            targetLerpPosL = (groundedL && HorizontalDist(startLerpPosL, targetLerpPosL) < groundedFreeDistance)
                ? startLerpPosL
                : targetLerpPosL;
        }
        // targetLerpPosL += targetLerpPosMultiplier * rootHeightOffset;
        
        startLerpPosR = curEffectorPosR;
        //gizAnkleRAfterMove = startLerpPosR;

        targetLerpPosR = skhandler.joints[SkeletonHandler.JointType_AnkleRight] + targetLerpPosMultiplier * rootHeightOffset;
        if (filterMovementsOnGround)
        {
            targetLerpPosR = (groundedR && HorizontalDist(startLerpPosR, targetLerpPosR) < groundedFreeDistance)
                ? startLerpPosR
                : targetLerpPosR;
        }
        // targetLerpPosR += targetLerpPosMultiplier*rootHeightOffset;

        // gizCurTarBeforeRay = targetLerpPosR;

        // define totallerptime = 1/ODFrequency
        // reset curlerptime
        curLerpTimeL = 0;
        curLerpTimeR = 0;
        curTValL = 0;
        curTValR = 0;
        totalLerpTime = 1 / objectDetectionFrequency;
        totalLerpTime *= lerpLatency;
    }

    private Vector3 AccumulateLerpVec3(Vector3 startPos, Vector3 targetPos, ref float curTVal, ref float currentLerpTime)
    {
        if(Vector3.Distance(startPos,targetPos) > .00001f)
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

    private void OnDrawGizmos()
    {
        if (groundedR) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(RightFootTransform.position + (Vector3.up * thresholdEnterGroundedState), RightFootTransform.position - (Vector3.up * thresholdEnterGroundedState));

        if (groundedL) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(LeftFootTransform.position + (Vector3.up * thresholdEnterGroundedState), LeftFootTransform.position - (Vector3.up * thresholdEnterGroundedState));

        //Gizmos.color = Color.blue;
        //Gizmos.DrawCube(curEffectorPosL, new Vector3(gizmoSize, gizmoSize, gizmoSize));
        //Gizmos.color = Color.magenta;
        //Gizmos.DrawCube(curEffectorPosR, new Vector3(gizmoSize, gizmoSize, gizmoSize));

        //Gizmos.color = Color.green;
        //Gizmos.DrawCube(grincubepos, new Vector3(gizmoSize+.05f, gizmoSize + .05f, gizmoSize + .05f));

        //Gizmos.color = Color.blue;
        //Gizmos.DrawCube(bluecubepos, new Vector3(gizmoSize, gizmoSize, gizmoSize));

        //Gizmos.color = Color.black;
        //Gizmos.DrawCube(blakcubepos, new Vector3(gizmoSize, gizmoSize, gizmoSize));
        Gizmos.color = colorAnkleRBeforeMove;
        Gizmos.DrawCube(gizAnkleRBeforeMove, new Vector3(gizmoSize, .1f, gizmoSize));
        Gizmos.color = colorAnkleRAfterMove;
        Gizmos.DrawCube(gizAnkleRAfterMove, new Vector3(gizmoSize, .08f, gizmoSize));
        Gizmos.color = colorAnkleRPlusHeightOffset;
        Gizmos.DrawCube(gizAnkleRPlusHeightOffset, new Vector3(gizmoSize, .06f, gizmoSize));

        Gizmos.color = new Color(1,0.4f,1);
        Gizmos.DrawLine(gizAnkleRBeforeMove, gizAnkleRAfterMove);
    }
}
