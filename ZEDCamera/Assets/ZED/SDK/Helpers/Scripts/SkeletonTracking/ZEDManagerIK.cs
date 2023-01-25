using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ZEDManagerIK : MonoBehaviour
{
    protected Animator animator;

    public bool ikActive = false;
    public Transform LeftFootTransform = null;
    public Transform RightFootTransform = null;

    public Transform sphereRepereR = null;
    public Transform sphereRepereL = null;

    public Transform _rootJoint; 

    private SkeletonHandler skhandler = null;

    public SkeletonHandler Skhandler { get => skhandler; set => skhandler = value; }
    public Vector3 ankleHeightOffset = new Vector3(0, 0.102f, 0);

    [Tooltip("Distance (between ankle and environment under it) under which a foot is considered on the floor.")]
    public float thresholdEnterGroundedState = .2f;
    public float thresholdLeaveGroundedState = .15f;

    public LayerMask raycastDetectionLayers;

    private bool groundedL = false;
    private bool groundedR = false;

    private Vector3 currentPosFootL;
    private Vector3 currentPosFootR;
    public float lerpIntensity = 5;

    private Vector3 velocityTargetL = Vector3.zero;
    private Vector3 velocityTargetR = Vector3.zero;

    private Vector3 normalL = Vector3.up;
    private Vector3 normalR = Vector3.up;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
    }

    private void OnAnimatorMove()
    {
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
            skhandler.Move();

            // 2) Move actor position, effectively moving root position. (root relative position is (0,0,0) in the prefab)
            // This way, we have root motion and can easily apply effects following the main gameobject's transform.
            transform.position = skhandler.TargetBodyPositionWithHipOffset;
            transform.rotation = skhandler.TargetBodyOrientation;

            // 3) Manage Foot IK
            if (animator)
            {
                //if the IK is active, set the position and rotation directly to the goal.
                if (ikActive)
                {
                    // Set the right foot target position and rotation, if one has been assigned
                    if (RightFootTransform != null)
                    {
                        currentPosFootR = RightFootTransform.position;

                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

                        Vector3 effectorTargetPos;

                        Ray ray = new Ray(skhandler.joints[SkeletonHandler.JointType_AnkleRight] + (Vector3.up * (groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit info, 2 * ( groundedR ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = info.point + ankleHeightOffset;
                            normalR = info.normal;
                            groundedR = true;
                        }
                        else
                        {
                            groundedR = false;
                            normalR = animator.GetBoneTransform(HumanBodyBones.RightFoot).up;
                            effectorTargetPos = skhandler.joints[SkeletonHandler.JointType_AnkleRight];
                        }

                        // set IK position and rotation
                        sphereRepereR.position = effectorTargetPos;
                        //effectorTargetPos = Vector3.Lerp(currentPosFootR,effectorTargetPos, .7f);
                        //effectorTargetPos = Vector3.SmoothDamp(currentPosFootR,effectorTargetPos, ref velocityTargetR, lerpIntensity);
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, effectorTargetPos);
                        animator.SetIKRotation(
                             AvatarIKGoal.RightFoot,
                             Quaternion.FromToRotation(
                                 animator.GetBoneTransform(HumanBodyBones.RightFoot).up, normalL) * animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation);
                    }
                    // Set the left foot target position and rotation, if one has been assigned
                    if (LeftFootTransform != null)
                    {
                        currentPosFootL = LeftFootTransform.position;

                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

                        Vector3 effectorTargetPos;

                        Ray ray = new Ray(skhandler.joints[SkeletonHandler.JointType_AnkleLeft] + (Vector3.up * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState)), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit info, 2 * (groundedL ? thresholdLeaveGroundedState : thresholdEnterGroundedState), raycastDetectionLayers))
                        {
                            effectorTargetPos = info.point + ankleHeightOffset;
                            normalL = info.normal;
                            groundedL = true;
                        }
                        else
                        {
                            groundedL = false;
                            normalL = animator.GetBoneTransform(HumanBodyBones.LeftFoot).up;
                            effectorTargetPos = skhandler.joints[SkeletonHandler.JointType_AnkleLeft];
                        }

                        // set IK position and rotation
                        sphereRepereL.position = effectorTargetPos;
                        //effectorTargetPos = Vector3.Lerp(currentPosFootL, effectorTargetPos, .7f);
                        //effectorTargetPos = Vector3.SmoothDamp(currentPosFootL, effectorTargetPos, ref velocityTargetL, lerpIntensity);
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, effectorTargetPos);
                        animator.SetIKRotation(
                            AvatarIKGoal.LeftFoot, 
                            Quaternion.FromToRotation(
                                animator.GetBoneTransform(HumanBodyBones.LeftFoot).up, normalL) * animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation);
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

    /**
     * Manage position of root
     */
    private void LateUpdate()
    {
    }

    private void OnDrawGizmos()
    {
        if (groundedR) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(RightFootTransform.position + (Vector3.up * thresholdEnterGroundedState), RightFootTransform.position - (Vector3.up * thresholdEnterGroundedState));

        if (groundedL) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(LeftFootTransform.position + (Vector3.up * thresholdEnterGroundedState), LeftFootTransform.position - (Vector3.up * thresholdEnterGroundedState));
    }
}
