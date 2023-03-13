//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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
        // --------- Body 70 specific
        // Left hand
        JointType_70_LEFT_HAND_THUMB_1 = 30,
        JointType_70_LEFT_HAND_THUMB_2 = 31,
        JointType_70_LEFT_HAND_THUMB_3 = 32,
        JointType_70_LEFT_HAND_THUMB_4 = 33, // tip
        JointType_70_LEFT_HAND_INDEX_1 = 34, // knuckle
        JointType_70_LEFT_HAND_INDEX_2 = 35,
        JointType_70_LEFT_HAND_INDEX_3 = 36,
        JointType_70_LEFT_HAND_INDEX_4 = 37, // tip
        JointType_70_LEFT_HAND_MIDDLE_1 = 38,
        JointType_70_LEFT_HAND_MIDDLE_2 = 39,
        JointType_70_LEFT_HAND_MIDDLE_3 = 40,
        JointType_70_LEFT_HAND_MIDDLE_4 = 41,
        JointType_70_LEFT_HAND_RING_1 = 42,
        JointType_70_LEFT_HAND_RING_2 = 43,
        JointType_70_LEFT_HAND_RING_3 = 44,
        JointType_70_LEFT_HAND_RING_4 = 45,
        JointType_70_LEFT_HAND_PINKY_1 = 46,
        JointType_70_LEFT_HAND_PINKY_2 = 47,
        JointType_70_LEFT_HAND_PINKY_3 = 48,
        JointType_70_LEFT_HAND_PINKY_4 = 49,
        // Right hand
        JointType_70_RIGHT_HAND_THUMB_1 = 50,
        JointType_70_RIGHT_HAND_THUMB_2 = 51,
        JointType_70_RIGHT_HAND_THUMB_3 = 52,
        JointType_70_RIGHT_HAND_THUMB_4 = 53,
        JointType_70_RIGHT_HAND_INDEX_1 = 54,
        JointType_70_RIGHT_HAND_INDEX_2 = 55,
        JointType_70_RIGHT_HAND_INDEX_3 = 56,
        JointType_70_RIGHT_HAND_INDEX_4 = 57,
        JointType_70_RIGHT_HAND_MIDDLE_1 = 58,
        JointType_70_RIGHT_HAND_MIDDLE_2 = 59,
        JointType_70_RIGHT_HAND_MIDDLE_3 = 60,
        JointType_70_RIGHT_HAND_MIDDLE_4 = 61,
        JointType_70_RIGHT_HAND_RING_1 = 62,
        JointType_70_RIGHT_HAND_RING_2 = 63,
        JointType_70_RIGHT_HAND_RING_3 = 64,
        JointType_70_RIGHT_HAND_RING_4 = 65,
        JointType_70_RIGHT_HAND_PINKY_1 = 66,
        JointType_70_RIGHT_HAND_PINKY_2 = 67,
        JointType_70_RIGHT_HAND_PINKY_3 = 68,
        JointType_70_RIGHT_HAND_PINKY_4 = 69,
        JointType_70_COUNT = 70,
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
        jointType_34_Count = 34;


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

    // List of bones (pair of joints) for BODY_70. Used for Skeleton mode.
    private static readonly int[] bonesList70 = new int[] {
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
    JointType_LEFT_WRIST, JointType_70_LEFT_HAND_THUMB_1,
    JointType_70_LEFT_HAND_THUMB_1, JointType_70_LEFT_HAND_THUMB_2,
    JointType_70_LEFT_HAND_THUMB_2, JointType_70_LEFT_HAND_THUMB_3,
    JointType_70_LEFT_HAND_THUMB_3, JointType_70_LEFT_HAND_THUMB_4,
    JointType_LEFT_WRIST, JointType_70_LEFT_HAND_INDEX_1,
    JointType_70_LEFT_HAND_INDEX_1, JointType_70_LEFT_HAND_INDEX_2,
    JointType_70_LEFT_HAND_INDEX_2, JointType_70_LEFT_HAND_INDEX_3,
    JointType_70_LEFT_HAND_INDEX_3, JointType_70_LEFT_HAND_INDEX_4,
    JointType_LEFT_WRIST, JointType_70_LEFT_HAND_MIDDLE_1,
    JointType_70_LEFT_HAND_MIDDLE_1, JointType_70_LEFT_HAND_MIDDLE_2,
    JointType_70_LEFT_HAND_MIDDLE_2, JointType_70_LEFT_HAND_MIDDLE_3,
    JointType_70_LEFT_HAND_MIDDLE_3, JointType_70_LEFT_HAND_MIDDLE_4,
    JointType_LEFT_WRIST, JointType_70_LEFT_HAND_RING_1,
    JointType_70_LEFT_HAND_RING_1, JointType_70_LEFT_HAND_RING_2,
    JointType_70_LEFT_HAND_RING_2, JointType_70_LEFT_HAND_RING_3,
    JointType_70_LEFT_HAND_RING_3, JointType_70_LEFT_HAND_RING_4,
    JointType_LEFT_WRIST, JointType_70_LEFT_HAND_PINKY_1,
    JointType_70_LEFT_HAND_PINKY_1, JointType_70_LEFT_HAND_PINKY_2,
    JointType_70_LEFT_HAND_PINKY_2, JointType_70_LEFT_HAND_PINKY_3,
    JointType_70_LEFT_HAND_PINKY_3, JointType_70_LEFT_HAND_PINKY_4,
    // right arm
    JointType_RIGHT_SHOULDER, JointType_RIGHT_ELBOW,
    JointType_RIGHT_ELBOW, JointType_RIGHT_WRIST,
    JointType_RIGHT_WRIST, JointType_70_RIGHT_HAND_THUMB_1,
    JointType_70_RIGHT_HAND_THUMB_1, JointType_70_RIGHT_HAND_THUMB_2,
    JointType_70_RIGHT_HAND_THUMB_2, JointType_70_RIGHT_HAND_THUMB_3,
    JointType_70_RIGHT_HAND_THUMB_3, JointType_70_RIGHT_HAND_THUMB_4,
    JointType_RIGHT_WRIST, JointType_70_RIGHT_HAND_INDEX_1,
    JointType_70_RIGHT_HAND_INDEX_1, JointType_70_RIGHT_HAND_INDEX_2,
    JointType_70_RIGHT_HAND_INDEX_2, JointType_70_RIGHT_HAND_INDEX_3,
    JointType_70_RIGHT_HAND_INDEX_3, JointType_70_RIGHT_HAND_INDEX_4,
    JointType_RIGHT_WRIST, JointType_70_RIGHT_HAND_MIDDLE_1,
    JointType_70_RIGHT_HAND_MIDDLE_1, JointType_70_RIGHT_HAND_MIDDLE_2,
    JointType_70_RIGHT_HAND_MIDDLE_2, JointType_70_RIGHT_HAND_MIDDLE_3,
    JointType_70_RIGHT_HAND_MIDDLE_3, JointType_70_RIGHT_HAND_MIDDLE_4,
    JointType_RIGHT_WRIST, JointType_70_RIGHT_HAND_RING_1,
    JointType_70_RIGHT_HAND_RING_1, JointType_70_RIGHT_HAND_RING_2,
    JointType_70_RIGHT_HAND_RING_2, JointType_70_RIGHT_HAND_RING_3,
    JointType_70_RIGHT_HAND_RING_3, JointType_70_RIGHT_HAND_RING_4,
    JointType_RIGHT_WRIST, JointType_70_RIGHT_HAND_PINKY_1,
    JointType_70_RIGHT_HAND_PINKY_1, JointType_70_RIGHT_HAND_PINKY_2,
    JointType_70_RIGHT_HAND_PINKY_2, JointType_70_RIGHT_HAND_PINKY_3,
    JointType_70_RIGHT_HAND_PINKY_3, JointType_70_RIGHT_HAND_PINKY_4,
    // legs
    JointType_LEFT_HIP, JointType_LEFT_KNEE,
    JointType_LEFT_ANKLE, JointType_LEFT_HEEL,
    JointType_LEFT_ANKLE, JointType_LEFT_BIG_TOE,
    JointType_LEFT_ANKLE, JointType_LEFT_SMALL_TOE,
    JointType_RIGHT_HIP, JointType_RIGHT_KNEE,
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
    private static readonly int[] sphereList70 = new int[] {
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
        // --------- Body 70 specific
        // Left hand
        JointType_70_LEFT_HAND_THUMB_1,
        JointType_70_LEFT_HAND_THUMB_2,
        JointType_70_LEFT_HAND_THUMB_3,
        JointType_70_LEFT_HAND_THUMB_4, // tip
        JointType_70_LEFT_HAND_INDEX_1, // knuckle
        JointType_70_LEFT_HAND_INDEX_2,
        JointType_70_LEFT_HAND_INDEX_3,
        JointType_70_LEFT_HAND_INDEX_4, // tip
        JointType_70_LEFT_HAND_MIDDLE_1,
        JointType_70_LEFT_HAND_MIDDLE_2,
        JointType_70_LEFT_HAND_MIDDLE_3,
        JointType_70_LEFT_HAND_MIDDLE_4,
        JointType_70_LEFT_HAND_RING_1,
        JointType_70_LEFT_HAND_RING_2,
        JointType_70_LEFT_HAND_RING_3,
        JointType_70_LEFT_HAND_RING_4,
        JointType_70_LEFT_HAND_PINKY_1,
        JointType_70_LEFT_HAND_PINKY_2,
        JointType_70_LEFT_HAND_PINKY_3,
        JointType_70_LEFT_HAND_PINKY_4,
        // Right hand
        JointType_70_RIGHT_HAND_THUMB_1,
        JointType_70_RIGHT_HAND_THUMB_2,
        JointType_70_RIGHT_HAND_THUMB_3,
        JointType_70_RIGHT_HAND_THUMB_4,
        JointType_70_RIGHT_HAND_INDEX_1,
        JointType_70_RIGHT_HAND_INDEX_2,
        JointType_70_RIGHT_HAND_INDEX_3,
        JointType_70_RIGHT_HAND_INDEX_4,
        JointType_70_RIGHT_HAND_MIDDLE_1,
        JointType_70_RIGHT_HAND_MIDDLE_2,
        JointType_70_RIGHT_HAND_MIDDLE_3,
        JointType_70_RIGHT_HAND_MIDDLE_4,
        JointType_70_RIGHT_HAND_RING_1,
        JointType_70_RIGHT_HAND_RING_2,
        JointType_70_RIGHT_HAND_RING_3,
        JointType_70_RIGHT_HAND_RING_4,
        JointType_70_RIGHT_HAND_PINKY_1,
        JointType_70_RIGHT_HAND_PINKY_2,
        JointType_70_RIGHT_HAND_PINKY_3,
        JointType_70_RIGHT_HAND_PINKY_4
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

    // Indexes of bones' parents for BODY_70
    private static readonly int[] parentsIdx_70 = new int[]
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
        30,
        31,
        32,
        16,
        30,
        31,
        32,
        16,
        30,
        31,
        32,
        16,
        30,
        31,
        32,
        16,
        30,
        31,
        32,
        17,
        50,
        51,
        52,
        17,
        50,
        51,
        52,
        17,
        50,
        51,
        52,
        17,
        50,
        51,
        52,
        17,
        50,
        51,
        52
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
    private static HumanBodyBones[] humanBones70 = new HumanBodyBones[] {
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
    // Left Hand
    HumanBodyBones.LeftThumbProximal,
    HumanBodyBones.LeftThumbIntermediate,
    HumanBodyBones.LeftThumbDistal,
    HumanBodyBones.LastBone, // Left Hand Thumb Tip
    HumanBodyBones.LeftIndexProximal,
    HumanBodyBones.LeftIndexIntermediate,
    HumanBodyBones.LeftIndexDistal,
    HumanBodyBones.LastBone, // Left Hand Index Tip
    HumanBodyBones.LeftMiddleProximal,
    HumanBodyBones.LeftMiddleIntermediate,
    HumanBodyBones.LeftMiddleDistal,
    HumanBodyBones.LastBone, // Left Hand Middle Tip
    HumanBodyBones.LeftRingProximal,
    HumanBodyBones.LeftRingIntermediate,
    HumanBodyBones.LeftRingDistal,
    HumanBodyBones.LastBone, // Left Hand Ring Tip
    HumanBodyBones.LeftLittleProximal,
    HumanBodyBones.LeftLittleIntermediate,
    HumanBodyBones.LeftLittleDistal,
    HumanBodyBones.LastBone, // Left Hand Pinky Tip
    // Right Hand
    HumanBodyBones.RightThumbProximal,
    HumanBodyBones.RightThumbIntermediate,
    HumanBodyBones.RightThumbDistal,
    HumanBodyBones.LastBone, // Right Hand Thumb Tip
    HumanBodyBones.RightIndexProximal,
    HumanBodyBones.RightIndexIntermediate,
    HumanBodyBones.RightIndexDistal,
    HumanBodyBones.LastBone, // Right Hand Index Tip
    HumanBodyBones.RightMiddleProximal,
    HumanBodyBones.RightMiddleIntermediate,
    HumanBodyBones.RightMiddleDistal,
    HumanBodyBones.LastBone, // Right Hand Middle Tip
    HumanBodyBones.RightRingProximal,
    HumanBodyBones.RightRingIntermediate,
    HumanBodyBones.RightRingDistal,
    HumanBodyBones.LastBone, // Right Hand Ring Tip
    HumanBodyBones.RightLittleProximal,
    HumanBodyBones.RightLittleIntermediate,
    HumanBodyBones.RightLittleDistal,
    HumanBodyBones.LastBone, // Right Hand Pinky Tip
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

    private sl.BODY_FORMAT skBodyModel = sl.BODY_FORMAT.BODY_38;

    public Vector3[] joints34 = new Vector3[jointType_34_Count];
    public Vector3[] joints38 = new Vector3[JointType_38_COUNT];
    public Vector3[] joints70 = new Vector3[JointType_70_COUNT];
    public float[] confidences34 = new float[jointType_34_Count];
    public float[] confidences38 = new float[JointType_38_COUNT];
    public float[] confidences70 = new float[JointType_70_COUNT];
    GameObject skeleton;
    public GameObject[] bones;
    public GameObject[] spheres;

    private GameObject humanoid;
    private ZEDSkeletonAnimator zedSkeletonAnimator = null;
    private Animator animator;
    private Dictionary<HumanBodyBones, RigBone> rigBone = null;
    private Dictionary<HumanBodyBones, Quaternion> rigBoneTarget = null;

    private Dictionary<HumanBodyBones, Quaternion> default_rotations = null;

    private List<GameObject> sphere = new List<GameObject>();

    private Vector3 targetBodyPosition = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 targetBodyPositionWithHipOffset = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion targetBodyOrientation = Quaternion.identity;

    private bool usingAvatar = true;

    [SerializeField] private float _feetOffset = 0.0f;
    public float FeetOffset {
        get { return _feetOffset; }
        set { _feetOffset = value; }
    }

    public Dictionary<HumanBodyBones, Quaternion> RigBoneTarget { get => rigBoneTarget; set => rigBoneTarget = value; }
    public Quaternion TargetBodyOrientation { get => targetBodyOrientation; set => targetBodyOrientation = value; }
    public Vector3 TargetBodyPositionWithHipOffset { get => targetBodyPositionWithHipOffset; set => targetBodyPositionWithHipOffset = value; }
    public sl.BODY_FORMAT SkBodyModel { get => skBodyModel; set => skBodyModel = value; }

    #endregion

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
    /// <param name="body_model">The Body model to apply (38 or 70 bones).</param>
    public void Create(GameObject h, sl.BODY_FORMAT body_model)
    {
        humanoid = (GameObject)Instantiate(h, Vector3.zero, Quaternion.identity);
        animator = humanoid.GetComponent<Animator>();

        skBodyModel = body_model;

        zedSkeletonAnimator = humanoid.GetComponent<ZEDSkeletonAnimator>();
        zedSkeletonAnimator.Skhandler = this;

        // Init list of bones that will be updated by the data retrieved from the ZED SDK
        rigBone = new Dictionary<HumanBodyBones, RigBone>();
        rigBoneTarget = new Dictionary<HumanBodyBones, Quaternion>();

        default_rotations = new Dictionary<HumanBodyBones, Quaternion>();

        switch (body_model)
        {
            case sl.BODY_FORMAT.BODY_34:
                foreach (HumanBodyBones bone in humanBones34)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        rigBone[bone] = new RigBone(humanoid, bone);

                        if (h.GetComponent<Animator>())
                        {
                            // Store rest pose rotations
                            default_rotations[bone] = animator.GetBoneTransform(bone).localRotation;
                        }

                    }
                    rigBoneTarget[bone] = Quaternion.identity;
                }
                break;
            case sl.BODY_FORMAT.BODY_38:
                foreach (HumanBodyBones bone in humanBones38)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        rigBone[bone] = new RigBone(humanoid, bone);

                        if (h.GetComponent<Animator>())
                        {
                            // Store rest pose rotations
                            default_rotations[bone] = animator.GetBoneTransform(bone).localRotation;
                        }

                    }
                    rigBoneTarget[bone] = Quaternion.identity;
                }
                break;
            case sl.BODY_FORMAT.BODY_70:
                foreach (HumanBodyBones bone in humanBones70)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        rigBone[bone] = new RigBone(humanoid, bone);

                        if (h.GetComponent<Animator>())
                        {
                            // Store rest pose rotations
                            default_rotations[bone] = animator.GetBoneTransform(bone).localRotation;
                        }

                    }
                    rigBoneTarget[bone] = Quaternion.identity;
                }
                break;
            default:
                Debug.LogError("Error: Invalid BODY_MODEL!");
                break;
        }
    }

    public void Destroy()
    {
        GameObject.Destroy(humanoid);
        GameObject.Destroy(skeleton);
        rigBone.Clear();
        rigBoneTarget.Clear();
        default_rotations.Clear();
        Array.Clear(bones, 0, bones.Length);
        Array.Clear(spheres, 0, spheres.Length);
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement.
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones34.</param>
    private void SetHumanPoseControlBody34(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Hips)];
        }

        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Spine)];
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.UpperChest)];
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightShoulder)];
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightUpperArm)];
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightLowerArm)];
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightHand)];
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftShoulder)];
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftUpperArm)];
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftLowerArm)];
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftHand)];
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Neck)];
        }

        if (rigBone[HumanBodyBones.Head].transform)
        {
            //rigBoneTarget[HumanBodyBones.Head] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Head)];
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightUpperLeg)];
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightLowerLeg)];
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightFoot)];
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftUpperLeg)];
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftLowerLeg)];
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftFoot)];
        }

        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement.
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones38.</param>
    private void SetHumanPoseControlBody38(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Hips)];
        }

        if (rigBone[HumanBodyBones.Spine].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Spine)];
        }

        if (rigBone[HumanBodyBones.Chest].transform)
        {
            rigBoneTarget[HumanBodyBones.Chest] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Chest)];
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.UpperChest)];
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Neck)];
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftShoulder)];
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightShoulder)];
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftUpperArm)];
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightUpperArm)];
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftLowerArm)];
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightLowerArm)];
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftHand)];
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightHand)];
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftUpperLeg)];
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightUpperLeg)];
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftLowerLeg)];
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightLowerLeg)];
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftFoot)];
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightFoot)];
        }

        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement.
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones70.</param>
    private void SetHumanPoseControlBody70(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Hips)];
        }

        if (rigBone[HumanBodyBones.Spine].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Spine)];
        }

        if (rigBone[HumanBodyBones.Chest].transform)
        {
            rigBoneTarget[HumanBodyBones.Chest] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Chest)];
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.UpperChest)];
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Neck)];
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftShoulder)];
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightShoulder)];
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftUpperArm)];
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightUpperArm)];
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLowerArm)];
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLowerArm)];
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftHand)];
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightHand)];
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftUpperLeg)];
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightUpperLeg)];
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLowerLeg)];
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLowerLeg)];
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftFoot)];
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightFoot)];
        }

        // Left Hand

        if (rigBone[HumanBodyBones.LeftThumbProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbProximal)];
        }

        if (rigBone[HumanBodyBones.LeftThumbIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbIntermediate)];
        }

        if (rigBone[HumanBodyBones.LeftThumbDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbDistal)];
        }

        if (rigBone[HumanBodyBones.LeftIndexProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexProximal)];
        }

        if (rigBone[HumanBodyBones.LeftIndexIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexIntermediate)];
        }

        if (rigBone[HumanBodyBones.LeftIndexDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexDistal)];
        }

        if (rigBone[HumanBodyBones.LeftMiddleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleProximal)];
        }

        if (rigBone[HumanBodyBones.LeftMiddleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleIntermediate)];
        }

        if (rigBone[HumanBodyBones.LeftMiddleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleDistal)];
        }

        if (rigBone[HumanBodyBones.LeftRingProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingProximal)];
        }

        if (rigBone[HumanBodyBones.LeftRingIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingIntermediate)];
        }

        if (rigBone[HumanBodyBones.LeftRingDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingDistal)];
        }

        if (rigBone[HumanBodyBones.LeftLittleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleProximal)];
        }

        if (rigBone[HumanBodyBones.LeftLittleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleIntermediate)];
        }

        if (rigBone[HumanBodyBones.LeftLittleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleDistal)];
        }

        // Right Hand

        if (rigBone[HumanBodyBones.RightThumbProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbProximal)];
        }

        if (rigBone[HumanBodyBones.RightThumbIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbIntermediate)];
        }

        if (rigBone[HumanBodyBones.RightThumbDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbDistal)];
        }

        if (rigBone[HumanBodyBones.RightIndexProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexProximal)];
        }

        if (rigBone[HumanBodyBones.RightIndexIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexIntermediate)];
        }

        if (rigBone[HumanBodyBones.RightIndexDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexDistal)];
        }

        if (rigBone[HumanBodyBones.RightMiddleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleProximal)];
        }

        if (rigBone[HumanBodyBones.RightMiddleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleIntermediate)];
        }

        if (rigBone[HumanBodyBones.RightMiddleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleDistal)];
        }

        if (rigBone[HumanBodyBones.RightRingProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingProximal)];
        }

        if (rigBone[HumanBodyBones.RightRingIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingIntermediate)];
        }

        if (rigBone[HumanBodyBones.RightRingDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingDistal)];
        }

        if (rigBone[HumanBodyBones.RightLittleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleProximal)];
        }

        if (rigBone[HumanBodyBones.RightLittleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleIntermediate)];
        }

        if (rigBone[HumanBodyBones.RightLittleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleDistal)];
        }

        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// This function mirrors the rotations.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones34.</param>
    private void SetHumanPoseControlMirrored34(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        rootPosition = rootPosition.mirror_x();
        rootRotation = rootRotation.mirror_x();

        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Hips)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Spine)].mirror_x();
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.UpperChest)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Neck)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Head].transform)
        {
            rigBoneTarget[HumanBodyBones.Head] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.Head)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.LeftFoot)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones34, HumanBodyBones.RightFoot)].mirror_x();
        }

        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// This function mirrors the rotations.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones38.</param>
    private void SetHumanPoseControlMirrored38(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        rootPosition = rootPosition.mirror_x();
        rootRotation = rootRotation.mirror_x();

        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Hips)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Spine].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Spine)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Chest].transform)
        {
            rigBoneTarget[HumanBodyBones.Chest] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Chest)].mirror_x();
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.UpperChest)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.Neck)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.RightFoot)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones38, HumanBodyBones.LeftFoot)].mirror_x();
        }


        // Store global transform (to be applied to the Hips joint).
        targetBodyOrientation = rootRotation;
        targetBodyPosition = rootPosition;
    }

    /// <summary>
    /// Function that handles the humanoid position, rotation and bones movement
    /// Fills the rigBoneTarget map with rotations from the SDK. They can then be applied to the corresponding bones.
    /// This function mirrors the rotations.
    /// </summary>
    /// <param name="rootPosition">Position to apply to the root of the 3D avatar.</param>
    /// <param name="rootRotation">Global rotation of the detected body.</param>
    /// <param name="jointsRotation">Array of rotations ordered following humanBones70.</param>
    private void SetHumanPoseControlMirrored70(Vector3 rootPosition, Quaternion rootRotation, Quaternion[] jointsRotation)
    {
        rootPosition = rootPosition.mirror_x();
        rootRotation = rootRotation.mirror_x();

        // Store any joint local rotation (if the bone exists)
        if (rigBone[HumanBodyBones.Hips].transform)
        {
            rigBoneTarget[HumanBodyBones.Hips] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Hips)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Spine].transform)
        {
            rigBoneTarget[HumanBodyBones.Spine] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Spine)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Chest].transform)
        {
            rigBoneTarget[HumanBodyBones.Chest] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Chest)].mirror_x();
        }

        if (rigBone[HumanBodyBones.UpperChest].transform)
        {
            rigBoneTarget[HumanBodyBones.UpperChest] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.UpperChest)].mirror_x();
        }

        if (rigBone[HumanBodyBones.Neck].transform)
        {
            rigBoneTarget[HumanBodyBones.Neck] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.Neck)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftShoulder] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightShoulder].transform)
        {
            rigBoneTarget[HumanBodyBones.RightShoulder] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftShoulder)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftUpperArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerArm].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerArm] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLowerArm)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftHand].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftHand] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightHand].transform)
        {
            rigBoneTarget[HumanBodyBones.RightHand] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftHand)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftUpperLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightUpperLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightUpperLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftUpperLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLowerLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLowerLeg].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLowerLeg] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLowerLeg)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftFoot] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightFoot)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightFoot].transform)
        {
            rigBoneTarget[HumanBodyBones.RightFoot] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftFoot)].mirror_x();
        }

        // Left Hand

        if (rigBone[HumanBodyBones.LeftThumbProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftThumbIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftThumbDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftThumbDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightThumbDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftIndexProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftIndexIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftIndexDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftIndexDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightIndexDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftMiddleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftMiddleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftMiddleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftMiddleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightMiddleDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftRingProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftRingIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftRingDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftRingDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightRingDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLittleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLittleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.LeftLittleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.LeftLittleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.RightLittleDistal)].mirror_x();
        }

        // Right Hand

        if (rigBone[HumanBodyBones.RightThumbProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightThumbIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightThumbDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightThumbDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftThumbDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightIndexProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightIndexIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightIndexDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightIndexDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftIndexDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightMiddleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightMiddleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightMiddleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightMiddleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftMiddleDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightRingProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightRingIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightRingDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightRingDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftRingDistal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLittleProximal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleProximal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleProximal)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLittleIntermediate].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleIntermediate] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleIntermediate)].mirror_x();
        }

        if (rigBone[HumanBodyBones.RightLittleDistal].transform)
        {
            rigBoneTarget[HumanBodyBones.RightLittleDistal] = jointsRotation[Array.IndexOf(humanBones70, HumanBodyBones.LeftLittleDistal)].mirror_x();
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
        int[] curSphereList;
        switch (skBodyModel)
        {
            case sl.BODY_FORMAT.BODY_34:
                bones = new GameObject[bonesList34.Length / 2];
                spheres = new GameObject[sphereList34.Length];
                curSphereList = sphereList34;
                break;
            case sl.BODY_FORMAT.BODY_38:
                bones = new GameObject[bonesList38.Length / 2];
                spheres = new GameObject[sphereList38.Length];
                curSphereList = sphereList38;
                break;
            case sl.BODY_FORMAT.BODY_70:
                bones = new GameObject[bonesList70.Length / 2];
                spheres = new GameObject[sphereList70.Length];
                curSphereList = sphereList70;
                break;
            default:
                Debug.LogError("Error! InitSkeleton: Invalid body model, select at least BODY_34 to use a 3D avatar. Assuming Body38.");
                bones = new GameObject[bonesList38.Length / 2];
                spheres = new GameObject[sphereList38.Length];
                curSphereList = sphereList38;
                break;
        }

        skeleton = new GameObject();
        skeleton.name = "Skeleton_ID_" + person_id;
        float width = 0.025f;

        Color color = colors[person_id % colors.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.GetComponent<Renderer>().material = skBaseMat;
            skBaseMat.color = color;
            cylinder.transform.parent = skeleton.transform;
            bones[i] = cylinder;
        }
        for (int j = 0; j < spheres.Length; j++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material = skBaseMat;
            skBaseMat.color = color;
            sphere.transform.localScale = new Vector3(width * 2, width * 2, width * 2);
            sphere.transform.parent = skeleton.transform;
            sphere.name = curSphereList[j].ToString();
            spheres[j] = sphere;
        }
    }
    
    /// <summary>
    /// Updates SDK skeleton display.
    /// </summary>
    /// <param name="body_model">38 or 70 keypoints.</param>
    /// <param name="offsetDebug">In case the "displayDebugSkeleton" option is enabled in the ZEDSkeletonTrackingManager, the skeleton will be displayed with this offset.</param>
    void UpdateSkeleton(sl.BODY_FORMAT body_model, Vector3 offsetDebug)
    {
        float width = 0.025f;

        switch (body_model)
        {
            case sl.BODY_FORMAT.BODY_34:
                for (int j = 0; j < spheres.Length; j++)
                {
                    if (sl.ZEDCommon.IsVector3NaN(joints34[sphereList34[j]]))
                    {
                        spheres[j].transform.position = Vector3.zero + offsetDebug;
                        spheres[j].SetActive(false);
                    }
                    else
                    {
                        spheres[j].transform.position = joints34[sphereList34[j]] + offsetDebug;
                        spheres[j].SetActive(true);
                    }
                }

                for (int i = 0; i < bones.Length; i++)
                {
                    Vector3 start = spheres[Array.IndexOf(sphereList34, bonesList34[2 * i])].transform.position;
                    Vector3 end = spheres[Array.IndexOf(sphereList34, bonesList34[2 * i + 1])].transform.position;

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
                break;
            case    sl.BODY_FORMAT.BODY_38:
                for (int j = 0; j < spheres.Length; j++)
                {
                    if (sl.ZEDCommon.IsVector3NaN(joints38[sphereList38[j]]))
                    {
                        spheres[j].transform.position = Vector3.zero + offsetDebug;
                        spheres[j].SetActive(false);
                    }
                    else
                    {
                        spheres[j].transform.position = joints38[sphereList38[j]] + offsetDebug;
                        spheres[j].SetActive(true);
                    }
                }

                for (int i = 0; i < bones.Length; i++)
                {
                    Vector3 start = spheres[Array.IndexOf(sphereList38, bonesList38[2 * i])].transform.position;
                    Vector3 end = spheres[Array.IndexOf(sphereList38, bonesList38[2 * i + 1])].transform.position;

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
                break;
            case sl.BODY_FORMAT.BODY_70:
                for (int j = 0; j < spheres.Length; j++)
                {
                    if (sl.ZEDCommon.IsVector3NaN(joints70[sphereList70[j]]))
                    {
                        spheres[j].transform.position = Vector3.zero + offsetDebug;
                        spheres[j].SetActive(false);
                    }
                    else
                    {
                        spheres[j].transform.position = joints70[sphereList70[j]] + offsetDebug;
                        spheres[j].SetActive(true);
                    }
                }

                for (int i = 0; i < bones.Length; i++)
                {
                    Vector3 start = spheres[Array.IndexOf(sphereList70, bonesList70[2 * i])].transform.position;
                    Vector3 end = spheres[Array.IndexOf(sphereList70, bonesList70[2 * i + 1])].transform.position;

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
                break;
            default:
                Debug.LogError("Error: updateSkeleton: Invalid Body Model. No update.");
                break;
        }
    }

    /// <summary>
    /// Sets the avatar control with joint position.
    /// Called on camera's OnObjectDetection event.
    /// Updates the target rotations so that the 3D avatar can be correctly animated.
    /// </summary>
    /// <param name="body_model">Body model in use.</param>
    /// <param name="jointsPosition">The keypoints position from the ZED SDK.</param>
    /// <param name="jointsRotation">The bones local orientations from the ZED SDK.</param>
    /// <param name="rootRotation">The global root orientation from the ZED SDK.</param>
    /// <param name="useAvatar">If the 3D avatar should be displayed (and if the corresponding data should be updated).</param>
    /// <param name="_mirrorOnYAxis">Mirror the 3D avatars or not.</param>
    public void SetControlWithJointPosition(sl.BODY_FORMAT body_model, Vector3[] jointsPosition, Quaternion[] jointsRotation, Quaternion rootRotation, bool useAvatar, bool _mirrorOnYAxis)
    {
        switch (body_model)
        {
            case sl.BODY_FORMAT.BODY_34:
                joints34 = jointsPosition;

                humanoid.SetActive(useAvatar);
                skeleton.SetActive(!useAvatar || ZEDSkeletonTrackingViewer.DisplayDebugSkeleton);
                usingAvatar = useAvatar;

                if (useAvatar)
                {
                    if (_mirrorOnYAxis)
                        SetHumanPoseControlMirrored34(jointsPosition[0], rootRotation, jointsRotation);
                    else
                        SetHumanPoseControlBody34(jointsPosition[0], rootRotation, jointsRotation);

                    if (ZEDSkeletonTrackingViewer.DisplayDebugSkeleton)
                    {
                        UpdateSkeleton(body_model, ZEDSkeletonTrackingViewer.OffsetDebugSkeleton);
                    }
                }
                else
                {
                    UpdateSkeleton(body_model, Vector3.zero);
                }

                zedSkeletonAnimator.PoseWasUpdatedIK();
                break;

            case sl.BODY_FORMAT.BODY_38:
                joints38 = jointsPosition;

                humanoid.SetActive(useAvatar);
                skeleton.SetActive(!useAvatar || ZEDSkeletonTrackingViewer.DisplayDebugSkeleton);
                usingAvatar = useAvatar;

                if (useAvatar)
                {
                    if (_mirrorOnYAxis)
                        SetHumanPoseControlMirrored38(jointsPosition[0], rootRotation, jointsRotation);
                    else
                        SetHumanPoseControlBody38(jointsPosition[0], rootRotation, jointsRotation);

                    if (ZEDSkeletonTrackingViewer.DisplayDebugSkeleton)
                    {
                        UpdateSkeleton(body_model, ZEDSkeletonTrackingViewer.OffsetDebugSkeleton);
                    }
                }
                else
                {
                    UpdateSkeleton(body_model, Vector3.zero);
                }

                zedSkeletonAnimator.PoseWasUpdatedIK();
                break;

            case sl.BODY_FORMAT.BODY_70:
                joints70 = jointsPosition;

                humanoid.SetActive(useAvatar);
                skeleton.SetActive(!useAvatar || ZEDSkeletonTrackingViewer.DisplayDebugSkeleton);
                usingAvatar = useAvatar;

                if (useAvatar)
                {
                    if (_mirrorOnYAxis)
                        SetHumanPoseControlMirrored70(jointsPosition[0], rootRotation, jointsRotation);
                    else
                        SetHumanPoseControlBody70(jointsPosition[0], rootRotation, jointsRotation);

                    if (ZEDSkeletonTrackingViewer.DisplayDebugSkeleton)
                    {
                        UpdateSkeleton(body_model, ZEDSkeletonTrackingViewer.OffsetDebugSkeleton);
                    }
                }
                else
                {
                    UpdateSkeleton(body_model, Vector3.zero);
                }
                zedSkeletonAnimator.PoseWasUpdatedIK();
                break;

            default:
                Debug.LogError("Error: setControlWithJointPosition: Invalid Body Model. No update.");
                break;
        }
    }

    /// <summary>
    /// Utility function to apply the rest pose to the bones.
    /// </summary>
    void PropagateRestPoseRotations(sl.BODY_FORMAT body_model, int parentIdx, Dictionary<HumanBodyBones, RigBone> outPose, Quaternion restPosRot, bool inverse)
    {
        if (body_model == sl.BODY_FORMAT.BODY_34)
        {
            for (int i = 0; i < humanBones34.Length; i++)
            {
                if (humanBones34[i] != HumanBodyBones.LastBone && outPose[humanBones34[i]].transform)
                {
                    Transform outPoseTransform = outPose[humanBones34[i]].transform;

                    if (parentsIdx_34[i] == parentIdx)
                    {
                        Quaternion restPoseRotation = default_rotations[humanBones34[i]];
                        Quaternion restPoseRotChild = new Quaternion();

                        if (parentsIdx_34[i] != -1)
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

                        PropagateRestPoseRotations(body_model, i, outPose, restPoseRotChild, inverse);
                    }
                }
            }
        }
        else if (body_model == sl.BODY_FORMAT.BODY_38)
        {
            for (int i = 0; i < humanBones38.Length; i++)
            {
                if (humanBones38[i] != HumanBodyBones.LastBone && outPose[humanBones38[i]].transform)
                {
                    Transform outPoseTransform = outPose[humanBones38[i]].transform;

                    if (parentsIdx_38[i] == parentIdx)
                    {
                        Quaternion restPoseRotation = default_rotations[humanBones38[i]];
                        Quaternion restPoseRotChild = new Quaternion();

                        if (parentsIdx_38[i] != -1)
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

                        PropagateRestPoseRotations(body_model, i, outPose, restPoseRotChild, inverse);
                    }
                }
            }
        }
        else if (body_model == sl.BODY_FORMAT.BODY_70)
        {
            for (int i = 0; i < humanBones70.Length; i++)
            {
                if (humanBones70[i] != HumanBodyBones.LastBone && outPose[humanBones70[i]].transform)
                {
                    Transform outPoseTransform = outPose[humanBones70[i]].transform;

                    if (parentsIdx_70[i] == parentIdx)
                    {
                        Quaternion restPoseRotation = default_rotations[humanBones70[i]];
                        Quaternion restPoseRotChild = new Quaternion();

                        if (parentsIdx_70[i] != -1)
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

                        PropagateRestPoseRotations(body_model, i, outPose, restPoseRotChild, inverse);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Error:PropagateRestPoseRotations: Invalid Body Model.");
        }
    }


    /// <summary>
    /// Sets 3D avatar position, and the bones rotations. Called in Update().
    /// This method does not use the animator, and instead directly sets the rotations of the bones transforms.
    /// </summary>
    /// <param name="body_model">Body model in use.</param>
    private void MoveAvatar(sl.BODY_FORMAT body_model)
    {
        switch (body_model)
        {
            case sl.BODY_FORMAT.BODY_34:
                // Put in Ref Pose
                foreach (HumanBodyBones bone in humanBones34)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        if (rigBone[bone].transform)
                        {
                            rigBone[bone].transform.localRotation = default_rotations[bone];
                        }
                    }
                }

                PropagateRestPoseRotations(body_model, 0, rigBone, default_rotations[0], false);

                for (int i = 0; i < humanBones34.Length; i++)
                {
                    if (humanBones34[i] != HumanBodyBones.LastBone && rigBone[humanBones34[i]].transform)
                    {
                        if (parentsIdx_38[i] != -1)
                        {
                            Quaternion newRotation = rigBoneTarget[humanBones34[i]] * rigBone[humanBones34[i]].transform.localRotation;
                            rigBone[humanBones34[i]].transform.localRotation = newRotation;
                        }
                    }
                }
                PropagateRestPoseRotations(body_model, 0, rigBone, Quaternion.Inverse(default_rotations[0]), true);
                break;

            case sl.BODY_FORMAT.BODY_38:
                // Put in Ref Pose
                foreach (HumanBodyBones bone in humanBones38)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        if (rigBone[bone].transform)
                        {
                            rigBone[bone].transform.localRotation = default_rotations[bone];
                        }
                    }
                }

                PropagateRestPoseRotations(body_model, 0, rigBone, default_rotations[0], false);

                for (int i = 0; i < humanBones38.Length; i++)
                {
                    if (humanBones38[i] != HumanBodyBones.LastBone && rigBone[humanBones38[i]].transform)
                    {
                        if (parentsIdx_38[i] != -1)
                        {
                            Quaternion newRotation = rigBoneTarget[humanBones38[i]] * rigBone[humanBones38[i]].transform.localRotation;
                            rigBone[humanBones38[i]].transform.localRotation = newRotation;
                        }
                    }
                }
                PropagateRestPoseRotations(body_model, 0, rigBone, Quaternion.Inverse(default_rotations[0]), true);
                break;

            case sl.BODY_FORMAT.BODY_70:
                // Put in Ref Pose
                foreach (HumanBodyBones bone in humanBones70)
                {
                    if (bone != HumanBodyBones.LastBone)
                    {
                        if (rigBone[bone].transform)
                        {
                            rigBone[bone].transform.localRotation = default_rotations[bone];
                        }
                    }
                }

                PropagateRestPoseRotations(body_model, 0, rigBone, default_rotations[0], false);

                for (int i = 0; i < humanBones70.Length; i++)
                {
                    if (humanBones70[i] != HumanBodyBones.LastBone && rigBone[humanBones70[i]].transform)
                    {
                        if (parentsIdx_70[i] != -1)
                        {
                            Quaternion newRotation = rigBoneTarget[humanBones70[i]] * rigBone[humanBones70[i]].transform.localRotation;

                            // update animator bones.
                            animator.SetBoneLocalRotation(humanBones70[i], newRotation);
                        }

                    }
                }
                PropagateRestPoseRotations(body_model, 0, rigBone, Quaternion.Inverse(default_rotations[0]), true);
                break;

            default:
                Debug.LogError("Error: Invalid Body Model.");
                break;
        }

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
        switch (skBodyModel)
        {
            case sl.BODY_FORMAT.BODY_34:
                confidences34 = confidences;
                break;
            case sl.BODY_FORMAT.BODY_38:
                confidences38 = confidences;
                break;
            case sl.BODY_FORMAT.BODY_70:
                confidences70 = confidences;
                break;
            default:
                Debug.LogError("Error: SetConfidences: Invalid body model, select at least BODY_34 to use a 3D avatar.");
                break;
        }
    }

    /// <summary>
    /// Update the 3D avatar display.
    /// </summary>
    public void Move()
    {
        if (usingAvatar)
        {
            MoveAvatar(SkBodyModel);
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
