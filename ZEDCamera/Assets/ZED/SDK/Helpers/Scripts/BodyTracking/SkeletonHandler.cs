//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public class SkeletonHandler : ScriptableObject
{
    #region const_variables
    // For Skeleton Display
    public const int
        // --------- Common
        JointType_PELVIS = 0,
        JointType_SPINE_1 = 1,
        JointType_SPINE_2 = 2,
        JointType_SPINE_3 = 3,
        JointType_NECK = 4,
        JointType_NOSE = 5,
        JointType_LEFT_EYE = 6,
        JointType_RIGHT_EYE = 7,
        JointType_LEFT_EAR = 8,
        JointType_RIGHT_EAR = 9,
        JointType_LEFT_CLAVICLE = 10,
        JointType_RIGHT_CLAVICLE = 11,
        JointType_LEFT_SHOULDER = 12,
        JointType_RIGHT_SHOULDER = 13,
        JointType_LEFT_ELBOW = 14,
        JointType_RIGHT_ELBOW = 15,
        JointType_LEFT_WRIST = 16,
        JointType_RIGHT_WRIST = 17,
        JointType_LEFT_HIP = 18,
        JointType_RIGHT_HIP = 19,
        JointType_LEFT_KNEE = 20,
        JointType_RIGHT_KNEE = 21,
        JointType_LEFT_ANKLE = 22,
        JointType_RIGHT_ANKLE = 23,
        JointType_LEFT_BIG_TOE = 24,
        JointType_RIGHT_BIG_TOE = 25,
        JointType_LEFT_SMALL_TOE = 26,
        JointType_RIGHT_SMALL_TOE = 27,
        JointType_LEFT_HEEL = 28,
        JointType_RIGHT_HEEL = 29,
        // --------- Body 38 specific
        JointType_38_LEFT_HAND_THUMB_4 = 30, // tip
        JointType_38_RIGHT_HAND_THUMB_4 = 31,
        JointType_38_LEFT_HAND_INDEX_1 = 32, // knuckle
        JointType_38_RIGHT_HAND_INDEX_1 = 33,
        JointType_38_LEFT_HAND_MIDDLE_4 = 34, // tip
        JointType_38_RIGHT_HAND_MIDDLE_4 = 35,
        JointType_38_LEFT_HAND_PINKY_1 = 36, // knuckle
        JointType_38_RIGHT_HAND_PINKY_1 = 37,
        JointType_38_COUNT = 38,
        // --------- Body34
        JointType_34_Head = 26,
        JointType_34_Neck = 3,
        JointType_34_ClavicleRight = 11,
        JointType_34_ShoulderRight = 12,
        JointType_34_ElbowRight = 13,
        JointType_34_WristRight = 14,
        JointType_34_ClavicleLeft = 4,
        JointType_34_ShoulderLeft = 5,
        JointType_34_ElbowLeft = 6,
        JointType_34_WristLeft = 7,
        JointType_34_HipRight = 22,
        JointType_34_KneeRight = 23,
        JointType_34_AnkleRight = 24,
        JointType_34_FootRight = 25,
        JointType_34_HeelRight = 33,
        JointType_34_HipLeft = 18,
        JointType_34_KneeLeft = 19,
        JointType_34_AnkleLeft = 20,
        JointType_34_FootLeft = 21,
        JointType_34_HeelLeft = 32,
        JointType_34_EyesRight = 30,
        JointType_34_EyesLeft = 28,
        JointType_34_EarRight = 31,
        JointType_34_EarLeft = 29,
        JointType_34_SpineBase = 0,
        JointType_34_SpineNaval = 1,
        JointType_34_SpineChest = 2,
        JointType_34_Nose = 27,
        jointType_34_COUNT = 34;


    // List of bones (pair of joints) for BODY_38. Used for Skeleton mode.
    private static readonly int[] bonesList38 = new int[] {
    // Torso
    JointType_PELVIS, JointType_SPINE_1,
    JointType_SPINE_1, JointType_SPINE_2,
    JointType_SPINE_2, JointType_SPINE_3,
    JointType_SPINE_3, JointType_NECK,
    JointType_PELVIS, JointType_LEFT_HIP,
    JointType_PELVIS, JointType_RIGHT_HIP,
    JointType_NECK, JointType_NOSE,
    JointType_NECK, JointType_LEFT_CLAVICLE,
    JointType_LEFT_CLAVICLE, JointType_LEFT_SHOULDER,
    JointType_NECK, JointType_RIGHT_CLAVICLE,
    JointType_RIGHT_CLAVICLE, JointType_RIGHT_SHOULDER,
    JointType_NOSE, JointType_LEFT_EYE,
    JointType_LEFT_EYE, JointType_LEFT_EAR,
    JointType_NOSE, JointType_RIGHT_EYE,
    JointType_RIGHT_EYE, JointType_RIGHT_EAR,
    // Left arm
    JointType_LEFT_SHOULDER, JointType_LEFT_ELBOW,
    JointType_LEFT_ELBOW, JointType_LEFT_WRIST,
    JointType_LEFT_WRIST, JointType_38_LEFT_HAND_THUMB_4, // -
    JointType_LEFT_WRIST, JointType_38_LEFT_HAND_INDEX_1,
    JointType_LEFT_WRIST, JointType_38_LEFT_HAND_MIDDLE_4,
    JointType_LEFT_WRIST, JointType_38_LEFT_HAND_PINKY_1, // -
    // right arm
    JointType_RIGHT_SHOULDER, JointType_RIGHT_ELBOW,
    JointType_RIGHT_ELBOW, JointType_RIGHT_WRIST,
    JointType_RIGHT_WRIST, JointType_38_RIGHT_HAND_THUMB_4, // -
    JointType_RIGHT_WRIST, JointType_38_RIGHT_HAND_INDEX_1,
    JointType_RIGHT_WRIST, JointType_38_RIGHT_HAND_MIDDLE_4,
    JointType_RIGHT_WRIST, JointType_38_RIGHT_HAND_PINKY_1, // -
    // legs
    JointType_LEFT_HIP, JointType_LEFT_KNEE,
    JointType_LEFT_KNEE, JointType_LEFT_ANKLE,
    JointType_LEFT_ANKLE, JointType_LEFT_HEEL,
    JointType_LEFT_ANKLE, JointType_LEFT_BIG_TOE,
    JointType_LEFT_ANKLE, JointType_LEFT_SMALL_TOE,
    JointType_RIGHT_HIP, JointType_RIGHT_KNEE,
    JointType_RIGHT_KNEE, JointType_RIGHT_ANKLE,
    JointType_RIGHT_ANKLE, JointType_RIGHT_HEEL,
    JointType_RIGHT_ANKLE, JointType_RIGHT_BIG_TOE,
    JointType_RIGHT_ANKLE, JointType_RIGHT_SMALL_TOE
    };

    // List of bones (pair of joints) for BODY_34. Used for Skeleton mode.
    private static readonly int[] bonesList34 = new int[] {
    // Torso
        JointType_34_SpineBase, JointType_34_HipRight,
        JointType_34_HipLeft, JointType_34_SpineBase,
        JointType_34_SpineBase, JointType_34_SpineNaval,
        JointType_34_SpineNaval, JointType_34_SpineChest,
        JointType_34_SpineChest, JointType_34_Neck,
        JointType_34_EarRight, JointType_34_EyesRight,
        JointType_34_EarLeft, JointType_34_EyesLeft,
        JointType_34_EyesRight, JointType_34_Nose,
        JointType_34_EyesLeft, JointType_34_Nose,
        JointType_34_Nose, JointType_34_Neck,
    // left
        JointType_34_SpineChest, JointType_34_ClavicleLeft,
        JointType_34_ClavicleLeft, JointType_34_ShoulderLeft,
        JointType_34_ShoulderLeft, JointType_34_ElbowLeft,         // LeftUpperArm
        JointType_34_ElbowLeft, JointType_34_WristLeft,            // LeftLowerArm
        JointType_34_HipLeft, JointType_34_KneeLeft,               // LeftUpperLeg
        JointType_34_KneeLeft, JointType_34_AnkleLeft,             // LeftLowerLeg6
        JointType_34_AnkleLeft, JointType_34_FootLeft,
        JointType_34_AnkleLeft, JointType_34_HeelLeft,
        JointType_34_FootLeft, JointType_34_HeelLeft,
    // right
        JointType_34_SpineChest, JointType_34_ClavicleRight,
        JointType_34_ClavicleRight, JointType_34_ShoulderRight,
        JointType_34_ShoulderRight, JointType_34_ElbowRight,       // RightUpperArm
        JointType_34_ElbowRight, JointType_34_WristRight,          // RightLowerArm
        JointType_34_HipRight, JointType_34_KneeRight,             // RightUpperLeg
        JointType_34_KneeRight, JointType_34_AnkleRight,           // RightLowerLeg
        JointType_34_AnkleRight, JointType_34_FootRight,
        JointType_34_AnkleRight, JointType_34_HeelRight,
        JointType_34_FootRight, JointType_34_HeelRight
    };

    // List of joint that will be rendered as a sphere in the Skeleton mode.
    // These are the joints with a rotation information (not the hands in order to not clutter the display)
    private static readonly int[] sphereList38 = new int[] {
        JointType_PELVIS,
        JointType_SPINE_1,
        JointType_SPINE_2,
        JointType_SPINE_3,
        JointType_NECK,
        JointType_NOSE,
        JointType_LEFT_EYE,
        JointType_RIGHT_EYE,
        JointType_LEFT_EAR,
        JointType_RIGHT_EAR,
        JointType_LEFT_CLAVICLE,
        JointType_RIGHT_CLAVICLE,
        JointType_LEFT_SHOULDER,
        JointType_RIGHT_SHOULDER,
        JointType_LEFT_ELBOW,
        JointType_RIGHT_ELBOW,
        JointType_LEFT_WRIST,
        JointType_RIGHT_WRIST,
        JointType_LEFT_HIP,
        JointType_RIGHT_HIP,
        JointType_LEFT_KNEE,
        JointType_RIGHT_KNEE,
        JointType_LEFT_ANKLE,
        JointType_RIGHT_ANKLE,
        JointType_LEFT_BIG_TOE,
        JointType_RIGHT_BIG_TOE,
        JointType_LEFT_SMALL_TOE,
        JointType_RIGHT_SMALL_TOE,
        JointType_LEFT_HEEL,
        JointType_RIGHT_HEEL,
        // --------- Body 38 specific
        JointType_38_LEFT_HAND_THUMB_4, // tip
        JointType_38_RIGHT_HAND_THUMB_4,
        JointType_38_LEFT_HAND_INDEX_1, // knuckle
        JointType_38_RIGHT_HAND_INDEX_1,
        JointType_38_LEFT_HAND_MIDDLE_4, // tip
        JointType_38_RIGHT_HAND_MIDDLE_4,
        JointType_38_LEFT_HAND_PINKY_1, // knuckle
        JointType_38_RIGHT_HAND_PINKY_1
    };

    // List of joint that will be rendered as a sphere in the Skeleton mode.
    // These are the joints with a rotation information (not the hands in order to not clutter the display)
    private static readonly int[] sphereList34 = new int[] {
        JointType_34_SpineBase,
        JointType_34_SpineNaval,
        JointType_34_SpineChest,
        JointType_34_Neck,
        JointType_34_HipLeft,
        JointType_34_HipRight,
        JointType_34_ClavicleLeft,
        JointType_34_ShoulderLeft,
        JointType_34_ElbowLeft,
        JointType_34_WristLeft,
        JointType_34_KneeLeft,
        JointType_34_AnkleLeft,
        JointType_34_FootLeft,
        JointType_34_HeelLeft,
        JointType_34_ClavicleRight,
        JointType_34_ShoulderRight,
        JointType_34_ElbowRight,
        JointType_34_WristRight,
        JointType_34_KneeRight,
        JointType_34_AnkleRight,
        JointType_34_FootRight,
        JointType_34_HeelRight,
        JointType_34_EyesLeft,
        JointType_34_EyesRight,
        JointType_34_EarRight,
        JointType_34_EarLeft,
        JointType_34_Nose
    };

    // List of available colors for Skeletons
    private Color[] colors = new Color[]{
    new Color( 232.0f / 255.0f, 176.0f / 255.0f,59.0f / 255.0f),
    new Color(175.0f / 255.0f, 208.0f / 255.0f,25.0f / 255.0f),
    new Color(102.0f / 255.0f / 255.0f, 205.0f / 255.0f,105.0f / 255.0f),
    new Color(185.0f / 255.0f, 0.0f / 255.0f,255.0f / 255.0f),
    new Color(99.0f / 255.0f, 107.0f / 255.0f,252.0f / 255.0f),
    new Color(252.0f / 255.0f, 225.0f / 255.0f, 8.0f / 255.0f),
    new Color(167.0f / 255.0f, 130.0f / 255.0f, 141.0f / 255.0f),
    new Color(194.0f / 255.0f, 72.0f / 255.0f, 113.0f / 255.0f)
    };

    // Indexes of bones' parents for BODY_38 
    private static readonly int[] parentsIdx_38 = new int[]
    {
        -1,
        0,
        1,
        2,
        3,
        4,
        4,
        4,
        4,
        4,
        3,
        3,
        10,
        11,
        12,
        13,
        14,
        15,
        0,
        0,
        18,
        19,
        20,
        21,
        22,
        23,
        22,
        23,
        22,
        23,
        16,
        17,
        16,
        17,
        16,
        17,
        16,
        17
    };

    // Indexes of bones' parents for BODY_34 
    private static readonly int[] parentsIdx_34 = new int[]
    {
        -1,
        0,
        1,
        2,
        2,
        4,
        5,
        6,
        7,
        8,
        7,
        2,
        11,
        12,
        13,
        14,
        15,
        14,
        0,
        18,
        19,
        20,
        0,
        22,
        23,
        24,
        3,
        26,
        26,
        26,
        26,
        26
    };

    // Bones output by the ZED SDK (in this order)
    private static HumanBodyBones[] humanBones38 = new HumanBodyBones[] {
    HumanBodyBones.Hips,
    HumanBodyBones.Spine,
    HumanBodyBones.Chest,
    HumanBodyBones.UpperChest,
    HumanBodyBones.Neck,
    HumanBodyBones.LastBone, // Nose
    HumanBodyBones.LastBone, // Left Eye
    HumanBodyBones.LastBone, // Right Eye
    HumanBodyBones.LastBone, // Left Ear
    HumanBodyBones.LastBone, // Right Ear
    HumanBodyBones.LeftShoulder,
    HumanBodyBones.RightShoulder,
    HumanBodyBones.LeftUpperArm,
    HumanBodyBones.RightUpperArm,
    HumanBodyBones.LeftLowerArm,
    HumanBodyBones.RightLowerArm,
    HumanBodyBones.LeftHand, // Left Wrist
    HumanBodyBones.RightHand, // Left Wrist
    HumanBodyBones.LeftUpperLeg, // Left Hip
    HumanBodyBones.RightUpperLeg, // Right Hip
    HumanBodyBones.LeftLowerLeg,
    HumanBodyBones.RightLowerLeg,
    HumanBodyBones.LeftFoot,
    HumanBodyBones.RightFoot,
    HumanBodyBones.LastBone, // Left Big Toe
    HumanBodyBones.LastBone, // Right Big Toe
    HumanBodyBones.LastBone, // Left Small Toe
    HumanBodyBones.LastBone, // Right Small Toe
    HumanBodyBones.LastBone, // Left Heel
    HumanBodyBones.LastBone, // Right Heel
    // Hands
    HumanBodyBones.LastBone, // Left Hand Thumb Tip
    HumanBodyBones.LastBone, // Right Hand Thumb Tip
    HumanBodyBones.LastBone, // Left Hand Index Knuckle
    HumanBodyBones.LastBone, // Right Hand Index Knuckle
    HumanBodyBones.LastBone, // Left Hand Middle Tip
    HumanBodyBones.LastBone, // Right Hand Middle Tip
    HumanBodyBones.LastBone, // Left Hand Pinky Knuckle
    HumanBodyBones.LastBone, // Right Hand Pinky Knuckle
    HumanBodyBones.LastBone // Last
    };

    // Bones output by the ZED SDK (in this order)
    private static HumanBodyBones[] humanBones34 = new HumanBodyBones[] {
    HumanBodyBones.Hips,
    HumanBodyBones.Spine,
    HumanBodyBones.UpperChest,
    HumanBodyBones.Neck,
    HumanBodyBones.LeftShoulder,
    HumanBodyBones.LeftUpperArm,
    HumanBodyBones.LeftLowerArm,
    HumanBodyBones.LeftHand, // Left Wrist
    HumanBodyBones.LastBone, // Left Hand
    HumanBodyBones.LastBone, // Left HandTip
    HumanBodyBones.LastBone,
    HumanBodyBones.RightShoulder,
    HumanBodyBones.RightUpperArm,
    HumanBodyBones.RightLowerArm,
    HumanBodyBones.RightHand, // Right Wrist
    HumanBodyBones.LastBone, // Right Hand
    HumanBodyBones.LastBone, // Right HandTip
    HumanBodyBones.LastBone,
    HumanBodyBones.LeftUpperLeg,
    HumanBodyBones.LeftLowerLeg,
    HumanBodyBones.LeftFoot,
    HumanBodyBones.LeftToes,
    HumanBodyBones.RightUpperLeg,
    HumanBodyBones.RightLowerLeg,
    HumanBodyBones.RightFoot,
    HumanBodyBones.RightToes,
    HumanBodyBones.Head,
    HumanBodyBones.LastBone, // Nose
    HumanBodyBones.LastBone, // Left Eye
    HumanBodyBones.LastBone, // Left Ear
    HumanBodyBones.LastBone, // Right Eye
    HumanBodyBones.LastBone, // Right Ear
   // HumanBodyBones.LastBone, // Left Heel
   // HumanBodyBones.LastBone, // Right Heel
    };
    #endregion

    #region vars

    public Vector3[] joints34 = new Vector3[jointType_34_COUNT];
    public Vector3[] joints38 = new Vector3[JointType_38_COUNT];
    public Vector3[] currentJoints;
    public float[] confidences34 = new float[jointType_34_COUNT];
    public float[] confidences38 = new float[JointType_38_COUNT];
    public float[] currentConfidences;
    public int[] currentSpheresList;
    public int[] currentBonesList;
    public int currentKeypointsCount = -1;
    public int currentLeftAnkleIndex = -1;
    public int currentRightAnkleIndex = -1;
    public HumanBodyBones[] currentHumanBodyBones;
    public int[] currentParentIds;
    GameObject skeleton;
    public GameObject[] bones;
    public GameObject[] spheres;

    private GameObject humanoid;
    public ZEDSkeletonAnimator zedSkeletonAnimator = null;
    private Animator animator;
    private Dictionary<HumanBodyBones, RigBone> rigBone = null;
    private Dictionary<HumanBodyBones, Quaternion> rigBoneTarget = null;
    private Dictionary<HumanBodyBones, Quaternion> rigBoneRotationLastFrame = null;

    private Dictionary<HumanBodyBones, Quaternion> default_rotations = null;
    private Dictionary<HumanBodyBones, Quaternion> defaultRotationsWorld = null;

    private Vector3 targetBodyPosition = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 targetBodyPositionWithHipOffset = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 targetBodyPositionLastFrame = Vector3.zero; // smoothing var
    private Quaternion targetBodyOrientation = Quaternion.identity;
    private Quaternion targetBodyOrientationSmoothed = Quaternion.identity;
    private Quaternion targetBodyOrientationLastFrame = Quaternion.identity; // smoothing var

    private bool usingAvatar = true;

    [SerializeField] private float _feetOffset = 0.0f;
    public float FeetOffset
    {
        get { return _feetOffset; }
        set { _feetOffset = value; }
    }

    public Dictionary<HumanBodyBones, Quaternion> RigBoneTarget { get => rigBoneTarget; set => rigBoneTarget = value; }
    public Dictionary<HumanBodyBones, Quaternion> DefaultRotations { get => default_rotations; }
    public Dictionary<HumanBodyBones, Quaternion> DefaultRotationsWorld { get => defaultRotationsWorld; }
    public Quaternion TargetBodyOrientationSmoothed { get => targetBodyOrientationSmoothed; set => targetBodyOrientationSmoothed = value; }
    public Vector3 TargetBodyPositionWithHipOffset { get => targetBodyPositionWithHipOffset; set => targetBodyPositionWithHipOffset = value; }

    private sl.BODY_FORMAT currentBodyFormat = sl.BODY_FORMAT.BODY_38;
    public sl.BODY_FORMAT BodyFormat { get { return currentBodyFormat; } set { currentBodyFormat = value; UpdateCurrentValues(currentBodyFormat); } }

    public Dictionary<HumanBodyBones, RigBone> RigBone { get => rigBone; set => rigBone = value; }
    public Dictionary<HumanBodyBones, Quaternion> RigBoneRotationLastFrame { get => rigBoneRotationLastFrame; set => rigBoneRotationLastFrame = value; }

    #endregion

    /// <summary>
    /// Update the "currentXXX" values depending on the active BODY_FORMAT
    /// </summary>
    /// <param name="pBodyFormat"></param>
    private void UpdateCurrentValues(sl.BODY_FORMAT pBodyFormat)
    {
        switch (pBodyFormat)
        {
            case sl.BODY_FORMAT.BODY_34:
                currentConfidences = confidences34;
                currentJoints = joints34;
                currentHumanBodyBones = humanBones34;
                currentSpheresList = sphereList34;
                currentBonesList = bonesList34;
                currentParentIds = parentsIdx_34;
                currentLeftAnkleIndex = JointType_34_AnkleLeft;
                currentRightAnkleIndex = JointType_34_AnkleRight;
                currentKeypointsCount = jointType_34_COUNT;
                break;
            case sl.BODY_FORMAT.BODY_38:
                currentConfidences = confidences38;
                currentJoints = joints38;
                currentHumanBodyBones = humanBones38;
                currentSpheresList = sphereList38;
                currentBonesList = bonesList38;
                currentParentIds = parentsIdx_38;
                currentLeftAnkleIndex = JointType_LEFT_ANKLE;
                currentRightAnkleIndex = JointType_RIGHT_ANKLE;
                currentKeypointsCount = JointType_38_COUNT;
                break;
            default:
                Debug.LogError("Error: Invalid BODY_MODEL! Please use either BODY_34 or BODY_38.");
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
                break;
        }
    }

    /// <summary>
    /// Get Animator;
    /// </summary>
    /// <returns>Humanoid</returns>
    public Animator GetAnimator()
    {
        return animator;
    }

    /// <summary>
    /// Create the avatar control
    /// </summary>
    /// <param name="h">The humanoid GameObject prefab.</param>
    /// <param name="body_format">The Body model to apply (34 or 38 bones).</param>
    public void Create(GameObject h, sl.BODY_FORMAT body_format)
    {
        humanoid = (GameObject)Instantiate(h, Vector3.zero, Quaternion.identity);
        animator = humanoid.GetComponent<Animator>();

        BodyFormat = body_format;

        zedSkeletonAnimator = humanoid.GetComponent<ZEDSkeletonAnimator>();
        zedSkeletonAnimator.Skhandler = this;

        // Init list of bones that will be updated by the data retrieved from the ZED SDK
        rigBone = new Dictionary<HumanBodyBones, RigBone>();
        rigBoneTarget = new Dictionary<HumanBodyBones, Quaternion>();
        rigBoneRotationLastFrame = new Dictionary<HumanBodyBones, Quaternion>();

        default_rotations = new Dictionary<HumanBodyBones, Quaternion>();
        defaultRotationsWorld = new Dictionary<HumanBodyBones, Quaternion>();

        foreach (HumanBodyBones bone in currentHumanBodyBones)
        {
            if (bone != HumanBodyBones.LastBone)
            {
                rigBone[bone] = new RigBone(humanoid, bone);

                if (animator != null)
                {
                    // Store rest pose rotations
                    default_rotations[bone] = animator.GetBoneTransform(bone).localRotation;
                    defaultRotationsWorld[bone] = animator.GetBoneTransform(bone).rotation;
                }

            }
            rigBoneTarget[bone] = Quaternion.identity;
            rigBoneRotationLastFrame[bone] = Quaternion.identity;
        }
    }

    public void Destroy()
    {
        GameObject.Destroy(humanoid);
        GameObject.Destroy(skeleton);
        rigBone.Clear();
        rigBoneTarget.Clear();
        rigBoneRotationLastFrame.Clear();
        default_rotations.Clear();
        Array.Clear(bones, 0, bones.Length);
        Array.Clear(spheres, 0, spheres.Length);
    }

    /// <summary>
    /// Returns the symmetric/mirror bone of <paramref name="humanBodyBone"/> in the human rig of the animator.
    /// </summary>
    /// <returns></returns>
    private HumanBodyBones MirrorBone(HumanBodyBones humanBodyBone)
    {
        switch (humanBodyBone)
        {
            case HumanBodyBones.Hips: return HumanBodyBones.Hips;
            case HumanBodyBones.LeftUpperLeg: return HumanBodyBones.RightUpperLeg;
            case HumanBodyBones.RightUpperLeg: return HumanBodyBones.LeftUpperLeg;
            case HumanBodyBones.LeftLowerLeg: return HumanBodyBones.RightLowerLeg;
            case HumanBodyBones.RightLowerLeg: return HumanBodyBones.LeftLowerLeg;
            case HumanBodyBones.LeftFoot: return HumanBodyBones.RightFoot;
            case HumanBodyBones.RightFoot: return HumanBodyBones.LeftFoot;
            case HumanBodyBones.Spine: return HumanBodyBones.Spine;
            case HumanBodyBones.Chest: return HumanBodyBones.Chest;
            case HumanBodyBones.UpperChest: return HumanBodyBones.UpperChest;
            case HumanBodyBones.Neck: return HumanBodyBones.Neck;
            case HumanBodyBones.Head: return HumanBodyBones.Head;
            case HumanBodyBones.LeftShoulder: return HumanBodyBones.RightShoulder;
            case HumanBodyBones.RightShoulder: return HumanBodyBones.LeftShoulder;
            case HumanBodyBones.LeftUpperArm: return HumanBodyBones.RightUpperArm;
            case HumanBodyBones.RightUpperArm: return HumanBodyBones.LeftUpperArm;
            case HumanBodyBones.LeftLowerArm: return HumanBodyBones.RightLowerArm;
            case HumanBodyBones.RightLowerArm: return HumanBodyBones.LeftLowerArm;
            case HumanBodyBones.LeftHand: return HumanBodyBones.RightHand;
            case HumanBodyBones.RightHand: return HumanBodyBones.LeftHand;
            case HumanBodyBones.LeftToes: return HumanBodyBones.RightToes;
            case HumanBodyBones.RightToes: return HumanBodyBones.LeftToes;
            case HumanBodyBones.LeftEye: return HumanBodyBones.RightEye;
            case HumanBodyBones.RightEye: return HumanBodyBones.LeftEye;
            case HumanBodyBones.Jaw: return HumanBodyBones.Jaw;
            case HumanBodyBones.LeftThumbProximal: return HumanBodyBones.RightThumbProximal;
            case HumanBodyBones.LeftThumbIntermediate: return HumanBodyBones.RightThumbIntermediate;
            case HumanBodyBones.LeftThumbDistal: return HumanBodyBones.RightThumbDistal;
            case HumanBodyBones.LeftIndexProximal: return HumanBodyBones.RightIndexProximal;
            case HumanBodyBones.LeftIndexIntermediate: return HumanBodyBones.RightIndexIntermediate;
            case HumanBodyBones.LeftIndexDistal: return HumanBodyBones.RightIndexDistal;
            case HumanBodyBones.LeftMiddleProximal: return HumanBodyBones.RightMiddleProximal;
            case HumanBodyBones.LeftMiddleIntermediate: return HumanBodyBones.RightMiddleIntermediate;
            case HumanBodyBones.LeftMiddleDistal: return HumanBodyBones.RightMiddleDistal;
            case HumanBodyBones.LeftRingProximal: return HumanBodyBones.RightRingProximal;
            case HumanBodyBones.LeftRingIntermediate: return HumanBodyBones.RightRingIntermediate;
            case HumanBodyBones.LeftRingDistal: return HumanBodyBones.RightRingDistal;
            case HumanBodyBones.LeftLittleProximal: return HumanBodyBones.RightLittleProximal;
            case HumanBodyBones.LeftLittleIntermediate: return HumanBodyBones.RightLittleIntermediate;
            case HumanBodyBones.LeftLittleDistal: return HumanBodyBones.RightLittleDistal;
            case HumanBodyBones.RightThumbProximal: return HumanBodyBones.LeftThumbProximal;
            case HumanBodyBones.RightThumbIntermediate: return HumanBodyBones.LeftThumbIntermediate;
            case HumanBodyBones.RightThumbDistal: return HumanBodyBones.LeftThumbDistal;
            case HumanBodyBones.RightIndexProximal: return HumanBodyBones.LeftIndexProximal;
            case HumanBodyBones.RightIndexIntermediate: return HumanBodyBones.LeftIndexIntermediate;
            case HumanBodyBones.RightIndexDistal: return HumanBodyBones.LeftIndexDistal;
            case HumanBodyBones.RightMiddleProximal: return HumanBodyBones.LeftMiddleProximal;
            case HumanBodyBones.RightMiddleIntermediate: return HumanBodyBones.LeftMiddleIntermediate;
            case HumanBodyBones.RightMiddleDistal: return HumanBodyBones.LeftMiddleDistal;
            case HumanBodyBones.RightRingProximal: return HumanBodyBones.LeftRingProximal;
            case HumanBodyBones.RightRingIntermediate: return HumanBodyBones.LeftRingIntermediate;
            case HumanBodyBones.RightRingDistal: return HumanBodyBones.LeftRingDistal;
            case HumanBodyBones.RightLittleProximal: return HumanBodyBones.LeftLittleProximal;
            case HumanBodyBones.RightLittleIntermediate: return HumanBodyBones.LeftLittleIntermediate;
            case HumanBodyBones.RightLittleDistal: return HumanBodyBones.LeftLittleDistal;
            case HumanBodyBones.LastBone:
            default: return HumanBodyBones.LastBone;
        }
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement.
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones34.</param>
    /// <param name="mirror">Should do the rotations/translations for mirror mode(true) or not(false).</param>
    private void SetHumanPoseControl(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation, bool mirror)
    {
        foreach(var rb in currentHumanBodyBones)
        {
            // Store any joint local rotation (if the bone exists)
            if (rb != HumanBodyBones.LastBone && rigBone[rb].transform)
            {
                rigBoneTarget[rb] = mirror 
                    ? jointsRotation[Array.IndexOf(currentHumanBodyBones, MirrorBone(rb))].mirror_x()
                    : jointsRotation[Array.IndexOf(currentHumanBodyBones, rb)];
            }
        }

        if (mirror)
        {
            rootPosition = rootPosition.mirror_x();
            rootRotation = rootRotation.mirror_x();
        }

        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Initialize SDK skeleton display (based on keypoints positions).
    /// </summary>
    /// <param name="person_id">Skeleton ID</param>
    /// <param name="skBaseMat">Material for the skeleton.</param>
    public void InitSkeleton(int person_id, Material skBaseMat)
    {
        bones = new GameObject[currentBonesList.Length / 2];
        spheres = new GameObject[currentSpheresList.Length];

        skeleton = new GameObject { name = "Skeleton_ID_" + person_id };
        float width = 0.0125f;

        Color color = colors[person_id % colors.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            //cylinder.layer = LayerMask.NameToLayer("tagInvisibleToZED");
            cylinder.GetComponent<Renderer>().material = skBaseMat;
            skBaseMat.color = color;
            cylinder.transform.parent = skeleton.transform;
            bones[i] = cylinder;
        }
        for (int j = 0; j < spheres.Length; j++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.layer = LayerMask.NameToLayer("tagInvisibleToZED");
            sphere.GetComponent<Renderer>().material = skBaseMat;
            skBaseMat.color = color;
            sphere.transform.localScale = new Vector3(width * 2, width * 2, width * 2);
            sphere.transform.parent = skeleton.transform;
            sphere.name = currentSpheresList[j].ToString();
            spheres[j] = sphere;
        }

        skeleton.layer = LayerMask.NameToLayer("tagInvisibleToZED");
    }

    /// <summary>
    /// Updates SDK skeleton display.
    /// </summary>
    /// <param name="offsetSDK">In case the "displaySDKSkeleton" option is enabled in the ZEDSkeletonTrackingManager, the skeleton will be displayed with this offset.</param>
    void UpdateSkeleton(Vector3 offsetSDK, bool mirrorMode = false)
    {
        float width = 0.0125f;
        for (int j = 0; j < spheres.Length; j++)
        {
            if (sl.ZEDCommon.IsVector3NaN(currentJoints[currentSpheresList[j]]))
            {
                spheres[j].transform.position = Vector3.zero + offsetSDK;
                spheres[j].SetActive(false);
            }
            else
            {
                spheres[j].transform.position = mirrorMode ? (currentJoints[currentSpheresList[j]] + offsetSDK).mirror_x() : currentJoints[currentSpheresList[j]] + offsetSDK;
                spheres[j].SetActive(true);
            }
        }

        for (int i = 0; i < bones.Length; i++)
        {
            Vector3 start = spheres[Array.IndexOf(currentSpheresList, currentBonesList[2 * i])].transform.position;
            Vector3 end = spheres[Array.IndexOf(currentSpheresList, currentBonesList[2 * i + 1])].transform.position;

            if (start == Vector3.zero || end == Vector3.zero)
            {
                bones[i].SetActive(false);
                continue;
            }

            bones[i].SetActive(true);
            Vector3 offset = end - start;
            Vector3 scale = new Vector3(width, offset.magnitude / 2.0f, width);
            Vector3 position = start + (offset / 2.0f);

            bones[i].transform.position = position;
            bones[i].transform.up = offset;
            bones[i].transform.localScale = scale;

        }
    }

    /// <summary>
    /// Sets the avatar control with joint position.
    /// Called on camera's OnObjectDetection event.
    /// Updates the target rotations so that the 3D avatar can be correctly animated.
    /// </summary>
    /// <param name="jointsPosition">The keypoints position from the ZED SDK.</param>
    /// <param name="jointsRotation">The bones local orientations from the ZED SDK.</param>
    /// <param name="rootRotation">The global root orientation from the ZED SDK.</param>
    /// <param name="useAvatar">If the 3D avatar should be displayed (and if the corresponding data should be updated).</param>
    /// <param name="_mirrorOnYAxis">Mirror the 3D avatars or not.</param>
    public void SetControlWithJointPosition(Vector3[] jointsPosition, Quaternion[] jointsRotation, Quaternion rootRotation, bool useAvatar, bool _mirrorOnYAxis)
    {
        currentJoints = jointsPosition;

        humanoid.SetActive(useAvatar);
        skeleton.SetActive(!useAvatar || ZEDBodyTrackingManager.DisplaySDKSkeleton);
        usingAvatar = useAvatar;

        if (useAvatar)
        {
            SetHumanPoseControl(jointsPosition[0], rootRotation, jointsRotation, _mirrorOnYAxis);

            if (ZEDBodyTrackingManager.DisplaySDKSkeleton)
            {
                UpdateSkeleton(ZEDBodyTrackingManager.OffsetSDKSkeleton + (ZEDBodyTrackingManager.ApplyHeighOffsetToSDKSkeleton?zedSkeletonAnimator.RootHeightOffset:Vector3.zero), _mirrorOnYAxis);
            }
        }
        else
        {
            UpdateSkeleton(Vector3.zero, _mirrorOnYAxis);
        }
    }

    /// <summary>
    /// Utility function to apply the rest pose to the bones.
    /// </summary>
    void PropagateRestPoseRotations(int parentIdx, Dictionary<HumanBodyBones, RigBone> outPose, Quaternion restPosRot, bool inverse)
    {
        for (int i = 0; i < currentHumanBodyBones.Length; i++)
        {
            if (currentHumanBodyBones[i] != HumanBodyBones.LastBone && outPose[currentHumanBodyBones[i]].transform)
            {
                Transform outPoseTransform = outPose[currentHumanBodyBones[i]].transform;

                if (currentParentIds[i] == parentIdx)
                {
                    Quaternion restPoseRotation = default_rotations[currentHumanBodyBones[i]];
                    Quaternion restPoseRotChild = new Quaternion();

                    if (currentParentIds[i] != -1)
                    {
                        Quaternion jointRotation = restPosRot * outPoseTransform.localRotation;
                        outPoseTransform.localRotation = jointRotation;

                        if (!inverse)
                        {
                            restPoseRotChild = restPosRot * restPoseRotation;
                        }
                        else
                        {
                            restPoseRotChild = Quaternion.Inverse(restPoseRotation) * restPosRot;
                        }
                    }
                    else
                    {
                        restPoseRotChild = restPosRot;
                    }

                    PropagateRestPoseRotations(i, outPose, restPoseRotChild, inverse);
                }
            }
        }
    }


    /// <summary>
    /// Sets 3D avatar position, and the bones rotations. Called in Update().
    /// This method does not use the animator, and instead directly sets the rotations of the bones transforms.
    /// </summary>
    private void MoveAvatar()
    {
        // Put in Ref Pose
        foreach (HumanBodyBones bone in currentHumanBodyBones)
        {
            if (bone != HumanBodyBones.LastBone)
            {
                if (rigBone[bone].transform)
                {
                    rigBone[bone].transform.localRotation = default_rotations[bone];
                }
            }
        }

        PropagateRestPoseRotations(0, rigBone, default_rotations[0], false);

        for (int i = 0; i < currentHumanBodyBones.Length; i++)
        {
            if (currentHumanBodyBones[i] != HumanBodyBones.LastBone && rigBone[currentHumanBodyBones[i]].transform)
            {
                if (currentParentIds[i] != -1)
                {
                    Quaternion newRotation = rigBoneTarget[currentHumanBodyBones[i]] * rigBone[currentHumanBodyBones[i]].transform.localRotation;
                    rigBone[currentHumanBodyBones[i]].transform.localRotation = newRotation;
                }
            }
        }
        PropagateRestPoseRotations(0, rigBone, Quaternion.Inverse(default_rotations[0]), true);

        // Reposition root depending on hips position.
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            TargetBodyPositionWithHipOffset = targetBodyPosition;
            rigBone[HumanBodyBones.Hips].transform.SetPositionAndRotation(TargetBodyPositionWithHipOffset, targetBodyOrientation);
        }
    }

    /// <summary>
    /// Utility function to fill the confidence arrays.
    /// </summary>
    /// <param name="confidences">Confidences from the ZED SDK.</param>
    public void SetConfidences(float[] confidences)
    {
        currentConfidences = confidences;
    }

    /// <summary>
    /// Update the 3D avatar display.
    /// Used to animate a rig without AnimatorController component.
    /// </summary>
    public void Move()
    {
        if (usingAvatar)
        {
            MoveAvatar();
        }
    }

    /// <summary>
    /// Ignore the smoothing on the first frame to not have the lerp from 0-pose as first animation.
    /// </summary>
    private bool firstFrame = true;

    /// <summary>
    /// Propagate rotations and set them to the animator.
    /// </summary>   
    public void MoveAnimator(bool smoothingEnabled, float smoothValue)
    {
        // Put in Ref Pose
        foreach (HumanBodyBones bone in currentHumanBodyBones)
        {
            if (bone != HumanBodyBones.LastBone)
            {
                if (rigBone[bone].transform)
                {
                    rigBone[bone].transform.localRotation = default_rotations[bone];
                }
            }
        }

        PropagateRestPoseRotations(0, rigBone, default_rotations[0], false);

        for (int i = 0; i < currentHumanBodyBones.Length; i++)
        {
            if (currentHumanBodyBones[i] != HumanBodyBones.LastBone && rigBone[currentHumanBodyBones[i]].transform)
            {
                if (currentParentIds[i] != -1)
                {
                    Quaternion newRotation = rigBoneTarget[currentHumanBodyBones[i]] * rigBone[currentHumanBodyBones[i]].transform.localRotation;
                    rigBone[currentHumanBodyBones[i]].transform.localRotation = newRotation;
                }
            }
        }
        PropagateRestPoseRotations(0, rigBone, Quaternion.Inverse(default_rotations[0]), true);

        //Add offset to hips for body34.
        if (BodyFormat == sl.BODY_FORMAT.BODY_34)
        {
            TargetBodyPositionWithHipOffset = targetBodyPosition + (0.1f * rigBone[HumanBodyBones.Hips].transform.up);
        }
        else
        {
            TargetBodyPositionWithHipOffset = targetBodyPosition;
        }
        targetBodyOrientationSmoothed = targetBodyOrientation;

        // animatorization
        if (smoothingEnabled && !firstFrame)
        {
            targetBodyPositionWithHipOffset = Vector3.Lerp(targetBodyPositionLastFrame, targetBodyPositionWithHipOffset, smoothValue);
            targetBodyPositionLastFrame = targetBodyPositionWithHipOffset;

            if (float.IsNaN(targetBodyOrientationLastFrame.w)) { Debug.LogWarning("NaN value detected. This can happen if \"SHOW_OFF\" is enabled in BodyTrackingManager."); targetBodyOrientationLastFrame = targetBodyOrientation; }

            targetBodyOrientationSmoothed = Quaternion.Slerp(
                targetBodyOrientationLastFrame,
                targetBodyOrientationSmoothed,
                smoothValue);
            targetBodyOrientationLastFrame = targetBodyOrientationSmoothed;

            foreach (HumanBodyBones bone in currentHumanBodyBones)
            {
                if (bone != HumanBodyBones.LastBone && bone != HumanBodyBones.Hips)
                {
                    if (rigBone[bone].transform)
                    {
                        Quaternion squat = Quaternion.Slerp(
                                RigBoneRotationLastFrame[bone],
                                rigBone[bone].transform.localRotation,
                                smoothValue);
                        animator.SetBoneLocalRotation(bone, squat);
                        RigBoneRotationLastFrame[bone] = squat;
                    }
                }
            }
        }
        else // smoothing disabled
        {
            targetBodyOrientationLastFrame = targetBodyOrientationSmoothed;
            targetBodyPositionLastFrame = targetBodyPositionWithHipOffset;
            foreach (HumanBodyBones bone in currentHumanBodyBones)
            {
                if (bone != HumanBodyBones.LastBone && bone != HumanBodyBones.Hips)
                {
                    if (rigBone[bone].transform)
                    {
                        animator.SetBoneLocalRotation(bone, rigBone[bone].transform.localRotation);
                        RigBoneRotationLastFrame[bone] = rigBone[bone].transform.localRotation;
                    }
                }
            }
            firstFrame = false;
        }

    }

    /// <summary>
    /// Pass the correct joints position, depending on index based on body format, to the CheckFootLock method of zedSkeletonAnimator.
    /// Should be called only if enableFootLock is true in the ZEDBodyTrackingManager.
    /// </summary>
    public void CheckFootLockAnimator()
    {
        if(BodyFormat == sl.BODY_FORMAT.BODY_34)
        {
            zedSkeletonAnimator.CheckFootLock(currentJoints[JointType_34_AnkleLeft], currentJoints[JointType_34_AnkleRight]);
        } else if (BodyFormat != sl.BODY_FORMAT.BODY_18)
        {
            zedSkeletonAnimator.CheckFootLock(currentJoints[JointType_LEFT_ANKLE], currentJoints[JointType_RIGHT_ANKLE]);
        }
    }
}

public static class TransformExtensions
{
    public static Vector3 mirror_x(this Vector3 input)
    {
        input.x *= -1f;
        return input;
    }

    public static Quaternion mirror_x(this Quaternion input)
    {
        input.x *= -1f;
        input.w *= -1f;
        return input;
    }
}
