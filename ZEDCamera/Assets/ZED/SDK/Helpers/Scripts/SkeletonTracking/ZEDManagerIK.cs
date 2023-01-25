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
    public float thresholdDistanceToGround = .3f;

    public LayerMask raycastDetectionLayers;

    private bool floorFoundL = false;
    private bool floorFoundR = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        //if (skhandler)
        //{
        //    Debug.Log("Updating;");
        //    Skhandler.Move();
        //}

        //_rootJoint.position = new Vector3(.6f, 7f, 9f);
    }

    private void OnAnimatorMove()
    {
        //if (skhandler)
        //{
        //    Skhandler.Move();
        //}
        // _rootJoint.localPosition = new Vector3(0, 10f, 0);
    }

    /**
     * a callback for calculating IK
     * 0) Apply bones rotations 1) Apply root position and rotations. 2) Do Foot IK. ( 4) Do Hand IK )
    */
    void OnAnimatorIK()
    {
        if (skhandler)
        {
            // 0) Update target positions and rotations, and apply rotations.
            skhandler.Move();

            // 1) Move actor position, effectively moving root position. (root relative position is (0,0,0) in the prefab)
            // This way, we have root motion and can easily apply effects following the main gameobject's transform.
            transform.position = skhandler.TargetBodyPositionWithHipOffset;
            transform.rotation = skhandler.TargetBodyOrientation;

            // 2) Manage Foot IK
            if (animator)
            {
                //if the IK is active, set the position and rotation directly to the goal.
                if (ikActive)
                {
                    // Set the right foot target position and rotation, if one has been assigned
                    if (RightFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

                        Vector3 effectorTargetPos;

                        Ray ray = new Ray(skhandler.joints[SkeletonHandler.JointType_AnkleRight] + (Vector3.up * thresholdDistanceToGround), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit info, 2 * thresholdDistanceToGround, raycastDetectionLayers))
                        {
                            effectorTargetPos = info.point + ankleHeightOffset;
                            floorFoundR = true;
                        }
                        else
                        {
                            floorFoundR = false;
                            effectorTargetPos = skhandler.joints[SkeletonHandler.JointType_AnkleRight];
                        }

                        // target position needs to be offset
                        // v0 is stick on height 0
                        //effectorTargetPos = new Vector3(
                        //        skhandler.joints[SkeletonHandler.JointType_AnkleRight].x,
                        //        0,
                        //        skhandler.joints[SkeletonHandler.JointType_AnkleRight].z);

                        // set IK position and rotation
                        sphereRepereR.position = effectorTargetPos;
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, effectorTargetPos);
                        animator.SetIKRotation(AvatarIKGoal.RightFoot, RightFootTransform.rotation);
                    }
                    // Set the left foot target position and rotation, if one has been assigned
                    if (LeftFootTransform != null)
                    {
                        // blend weight
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

                        Vector3 effectorTargetPos;

                        Ray ray = new Ray(skhandler.joints[SkeletonHandler.JointType_AnkleLeft] + (Vector3.up * thresholdDistanceToGround), Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit info, 2 * thresholdDistanceToGround, raycastDetectionLayers))
                        {
                            effectorTargetPos = info.point + ankleHeightOffset;
                            floorFoundL = true;
                        }
                        else
                        {
                            floorFoundL = false;
                            effectorTargetPos = skhandler.joints[SkeletonHandler.JointType_AnkleLeft];
                        }

                        // target position needs to be offset
                        // v0 is stick on height 0
                        //Vector3 effectorTargetPos = new Vector3(
                        //        skhandler.joints[SkeletonHandler.JointType_AnkleLeft].x,
                        //        0,
                        //        skhandler.joints[SkeletonHandler.JointType_AnkleLeft].z);

                        // set IK position and rotation
                        sphereRepereL.position = effectorTargetPos;
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, effectorTargetPos);
                        animator.SetIKRotation(AvatarIKGoal.LeftFoot, LeftFootTransform.rotation);
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
        //Debug.Log("LU begin: rootjoint: " + _rootJoint.position);
        //animator.GetBoneTransform(HumanBodyBones.Spine).position = new Vector3(.6f, 7f, 9f);
        //_rootJoint.position = skhandler.TargetBodyPosition;
        // transform.position = skhandler.TargetBodyPosition;
    }

    private void OnDrawGizmos()
    {
        if (floorFoundR) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(RightFootTransform.position + (Vector3.up * thresholdDistanceToGround), RightFootTransform.position - (Vector3.up * thresholdDistanceToGround));

        if (floorFoundL) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(LeftFootTransform.position + (Vector3.up * thresholdDistanceToGround), LeftFootTransform.position - (Vector3.up * thresholdDistanceToGround));
    }
}
