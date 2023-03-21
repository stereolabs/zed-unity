using System;
using UnityEngine;
using System.Collections.Generic;

public class Utility
{
    public static float[] Vector3ToFloat3(Vector3 vector)
    {
        return new float[3] { vector.x, vector.y, vector.z };
    }

    public static Vector3 ToImage(Vector3 vector)
    {
        return new Vector3(vector.x, -vector.y, vector.z); 
    }

    public static float[] WorldToScreen(Vector3 point, float fx, float fy, float cx, float cy)
    {
        float[] screenPos = new float[2];

        screenPos[0] = (point.x / point.z) * fx + cx;
        screenPos[1] = (point.y / point.z) * fy + cy;
        return screenPos;
    }
}

public class SKDataExporter : MonoBehaviour
{
    // Bones output by the ZED SDK (in this order) - bodsy 34
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
    HumanBodyBones.LeftMiddleDistal, // Left HandTip
    HumanBodyBones.LeftThumbDistal, // Left Thumb
    HumanBodyBones.RightShoulder,
    HumanBodyBones.RightUpperArm,
    HumanBodyBones.RightLowerArm,
    HumanBodyBones.RightHand, // Right Wrist
    HumanBodyBones.LastBone, // Right Hand
    HumanBodyBones.RightMiddleDistal, // Right HandTip
    HumanBodyBones.RightThumbDistal, // Right Thumb
    HumanBodyBones.LeftUpperLeg,
    HumanBodyBones.LeftLowerLeg,
    HumanBodyBones.LeftFoot,
    HumanBodyBones.LeftToes,
    HumanBodyBones.RightUpperLeg,
    HumanBodyBones.RightLowerLeg,
    HumanBodyBones.RightFoot,
    HumanBodyBones.RightToes,
    HumanBodyBones.Jaw,
    HumanBodyBones.LastBone, // Nose
    HumanBodyBones.LeftEye, // Left Eye
    HumanBodyBones.LastBone, // Left Ear
    HumanBodyBones.RightEye, // Right Eye
    HumanBodyBones.LastBone, // Right Ear
    HumanBodyBones.LastBone, // Left Heel
    HumanBodyBones.LastBone, // Right Heel
    };

    // Bones output by the ZED SDK (in this order) body 18
    private static HumanBodyBones[] humanBones18 = new HumanBodyBones[] {
    HumanBodyBones.LastBone,
    HumanBodyBones.Neck,
    HumanBodyBones.RightUpperArm,
    HumanBodyBones.RightLowerArm,
    HumanBodyBones.RightHand,
    HumanBodyBones.LeftUpperArm,
    HumanBodyBones.LeftLowerArm,
    HumanBodyBones.LeftHand, // Left Wrist
    HumanBodyBones.RightUpperLeg,
    HumanBodyBones.RightLowerLeg,
    HumanBodyBones.RightFoot,
    HumanBodyBones.LeftUpperLeg,
    HumanBodyBones.LeftLowerLeg,
    HumanBodyBones.LeftFoot,
    HumanBodyBones.RightEye, // Right Eye
    HumanBodyBones.LeftEye, // Left Eye
    HumanBodyBones.LastBone, // Right Ear
    HumanBodyBones.LastBone, // Left Eye
    };

    public string jsonFilename = "D:/SVO/test.json";

    ZEDManager zedManager;
    ZEDSkeletonTrackingViewer skeletonTrackingViewer;
    Dataset data;

    bool isInit = false;

    public static float InvalidValue = -999.0f;
    public static bool  IsMicroSeconds = false;

    ulong previousTS = 0;
    int FrameCount = -1;

    bool requestSavingJson = false;

    public void Awake()
    {
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }
        if (!skeletonTrackingViewer)
        {
            skeletonTrackingViewer = FindObjectOfType<ZEDSkeletonTrackingViewer>();
        }

        if (zedManager)
        {
            zedManager.OnZEDReady += Init;
        }
    }

    public void Init()
    {
        data = new Dataset();
        MetaData metaData = new MetaData();

        sl.ZEDCamera zedCamera = zedManager.zedCamera;

        metaData.Bbox3dMinimumVolume = 0.0f;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        long unixTime = now.ToUnixTimeMilliseconds();
        metaData.SequenceID = unixTime.ToString();
        metaData.IsRealZED = false;
        metaData.IsRectified = true;
        metaData.TargetFPS = (int)zedCamera.GetRequestedCameraFPS();
        metaData.InvalidValue = InvalidValue;
        metaData.IsMicroseconds = IsMicroSeconds;
        metaData.ImageHeight = zedManager.zedCamera.ImageHeight;
        metaData.ImageWidth = zedManager.zedCamera.ImageWidth;

        data.SetMetaData(metaData);

        isInit = true;
    }

    public void Update()
    {
        if (isInit && FrameCount == zedManager.zedCamera.GetSVONumberOfFrames() - 2)
        {
            Save(jsonFilename);
        }
    }

    public void LateUpdate()
    {
        if (isInit)
        {
            var avatarList = skeletonTrackingViewer.AvatarControlList;
            if (zedManager.SVOPosition > FrameCount)
            {
                FrameCount = zedManager.SVOPosition;
                ulong ts = zedManager.ImageTimeStamp;
                if (ts > previousTS)
                {
                    if (IsMicroSeconds) ts /= 1000;
                    previousTS = ts;

                    FrameData frameData = new FrameData();

                    frameData.EpochTimeStamp = ts;
                    frameData.FrameIndex = FrameCount;

                    frameData.ImageFileName = "_" + ts.ToString() + "_" + data.GetFramesCount().ToString("D5") + ".png";

                    FramePoseData trackedPose = new FramePoseData();

                    Transform camTransform = zedManager.GetLeftCameraTransform();
                    Matrix4x4 camPose = Matrix4x4.TRS(camTransform.position, camTransform.rotation, camTransform.localScale);

                    trackedPose.WorldPose.fromMatrix(camPose);
                    frameData.TrackedPose = trackedPose;

                    var CalibrationParameters = zedManager.zedCamera.GetCalibrationParameters();
                    float cx = CalibrationParameters.leftCam.cx;
                    float cy = CalibrationParameters.leftCam.cy;
                    float fx = CalibrationParameters.leftCam.fx;
                    float fy = CalibrationParameters.leftCam.fy;

                    int height = zedManager.zedCamera.ImageHeight;

                    FrameDetections frameDetections = new FrameDetections();

                    foreach (var avatar in avatarList)
                    {
                        SingleDetection singleDetection = new SingleDetection();

                        Animator animator = avatar.Value.GetAnimator();

                        ////////////////////////////////////////////////
                        //////////////////// Body 18 ///////////////////
                        ////////////////////////////////////////////////

                        Keypoints3DData keypoints3D = new Keypoints3DData();
                        Keypoints2DData keypoints2D = new Keypoints2DData();

                    /*  keypoints3D.NOSE = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D.NECK = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.NECK]).position);
                        keypoints3D.RIGHT_SHOULDER = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_SHOULDER]).position));
                        keypoints3D.RIGHT_ELBOW = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ELBOW]).position));
                        keypoints3D.RIGHT_WRIST = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_WRIST]).position));
                        keypoints3D.LEFT_SHOULDER = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_SHOULDER]).position));
                        keypoints3D.LEFT_ELBOW = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ELBOW]).position));
                        keypoints3D.LEFT_WRIST = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_WRIST]).position));
                        keypoints3D.RIGHT_HIP = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_HIP]).position));
                        keypoints3D.RIGHT_KNEE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_KNEE]).position));
                        keypoints3D.RIGHT_ANKLE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ANKLE]).position));
                        keypoints3D.LEFT_HIP = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_HIP]).position));
                        keypoints3D.LEFT_KNEE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_KNEE]).position));
                        keypoints3D.LEFT_ANKLE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ANKLE]).position));
                        keypoints3D.RIGHT_EYE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_EYE]).position));
                        keypoints3D.LEFT_EYE = Utility.Vector3ToFloat3(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_EYE]).position));
                        keypoints3D.RIGHT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D.LEFT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };*/

                        keypoints3D.NOSE = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D.NECK = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.NECK]).position);
                        keypoints3D.RIGHT_SHOULDER = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_SHOULDER]).position);
                        keypoints3D.RIGHT_ELBOW = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ELBOW]).position);
                        keypoints3D.RIGHT_WRIST = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_WRIST]).position);
                        keypoints3D.LEFT_SHOULDER = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_SHOULDER]).position);
                        keypoints3D.LEFT_ELBOW = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ELBOW]).position);
                        keypoints3D.LEFT_WRIST = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_WRIST]).position);
                        keypoints3D.RIGHT_HIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_HIP]).position);
                        keypoints3D.RIGHT_KNEE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_KNEE]).position);
                        keypoints3D.RIGHT_ANKLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ANKLE]).position);
                        keypoints3D.LEFT_HIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_HIP]).position);
                        keypoints3D.LEFT_KNEE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_KNEE]).position);
                        keypoints3D.LEFT_ANKLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ANKLE]).position);
                        keypoints3D.RIGHT_EYE = new float[3] { InvalidValue, InvalidValue, InvalidValue }; //Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_EYE]).position);
                        keypoints3D.LEFT_EYE = new float[3] { InvalidValue, InvalidValue, InvalidValue };  //Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_EYE]).position);
                        keypoints3D.RIGHT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D.LEFT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };

                        keypoints2D.NOSE = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D.NECK = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.NECK]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_SHOULDER = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_SHOULDER]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_ELBOW = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ELBOW]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_WRIST = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_WRIST]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_SHOULDER = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_SHOULDER]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_ELBOW = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ELBOW]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_WRIST = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_WRIST]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_HIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_HIP]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_KNEE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_KNEE]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_ANKLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_ANKLE]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_HIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_HIP]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_KNEE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_KNEE]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_ANKLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_ANKLE]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_EYE = new float[2] { InvalidValue, InvalidValue }; // Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.RIGHT_EYE]).position)), fx, fy, cx, cy);
                        keypoints2D.LEFT_EYE = new float[2] { InvalidValue, InvalidValue };  //Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones18[(int)sl.BODY_18_PARTS.LEFT_EYE]).position)), fx, fy, cx, cy);
                        keypoints2D.RIGHT_EAR = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D.LEFT_EAR = new float[2] { InvalidValue, InvalidValue };

                        singleDetection.Keypoints3D = keypoints3D;
                        singleDetection.Keypoints2D = keypoints2D;

                        ////////////////////////////////////////////////
                        //////////////////// Body 34 ///////////////////
                        ////////////////////////////////////////////////

                        Keypoints2DData_34 keypoints2D_34 = new Keypoints2DData_34();
                        Keypoints3DData_34 keypoints3D_34 = new Keypoints3DData_34();

                        keypoints3D_34.PELVIS = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.PELVIS]).position);
                        keypoints3D_34.NAVAL_SPINE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.NAVAL_SPINE]).position);
                        keypoints3D_34.CHEST_SPINE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.CHEST_SPINE]).position);
                        keypoints3D_34.NECK = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.NECK]).position);
                        keypoints3D_34.LEFT_CLAVICLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_CLAVICLE]).position);
                        keypoints3D_34.LEFT_SHOULDER = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_SHOULDER]).position);
                        keypoints3D_34.LEFT_ELBOW = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_ELBOW]).position);
                        keypoints3D_34.LEFT_WRIST = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_WRIST]).position);
                        keypoints3D_34.LEFT_HAND = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.LEFT_HANDTIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_HANDTIP]).position);
                        keypoints3D_34.LEFT_THUMB = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_THUMB]).position);
                        keypoints3D_34.RIGHT_CLAVICLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_CLAVICLE]).position);
                        keypoints3D_34.RIGHT_SHOULDER = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_SHOULDER]).position);
                        keypoints3D_34.RIGHT_ELBOW = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_ELBOW]).position);
                        keypoints3D_34.RIGHT_WRIST = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_WRIST]).position);
                        keypoints3D_34.RIGHT_HAND = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.RIGHT_HANDTIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_HANDTIP]).position);
                        keypoints3D_34.RIGHT_THUMB = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_THUMB]).position);
                        keypoints3D_34.LEFT_HIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_HIP]).position);
                        keypoints3D_34.LEFT_KNEE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_KNEE]).position);
                        keypoints3D_34.LEFT_ANKLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_ANKLE]).position);
                        keypoints3D_34.LEFT_FOOT = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_FOOT]).position);
                        keypoints3D_34.RIGHT_HIP = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_HIP]).position);
                        keypoints3D_34.RIGHT_KNEE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_KNEE]).position);
                        keypoints3D_34.RIGHT_ANKLE = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_ANKLE]).position);
                        keypoints3D_34.RIGHT_FOOT = Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_FOOT]).position);
                        keypoints3D_34.HEAD = new float[3] { InvalidValue, InvalidValue, InvalidValue }; // Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.HEAD]).position);
                        keypoints3D_34.NOSE = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.LEFT_EYE = new float[3] { InvalidValue, InvalidValue, InvalidValue };  // Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_EYE]).position);
                        keypoints3D_34.LEFT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.RIGHT_EYE = new float[3] { InvalidValue, InvalidValue, InvalidValue };  //Utility.Vector3ToFloat3(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_EYE]).position);
                        keypoints3D_34.RIGHT_EAR = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.LEFT_HEEL = new float[3] { InvalidValue, InvalidValue, InvalidValue };
                        keypoints3D_34.RIGHT_HEEL = new float[3] { InvalidValue, InvalidValue, InvalidValue };

                        keypoints2D_34.PELVIS = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.PELVIS]).position)), fx, fy, cx, cy);
                        keypoints2D_34.NAVAL_SPINE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.NAVAL_SPINE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.CHEST_SPINE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.CHEST_SPINE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.NECK = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.NECK]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_CLAVICLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_CLAVICLE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_SHOULDER = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_SHOULDER]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_ELBOW = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_ELBOW]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_WRIST = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_WRIST]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_HAND = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.LEFT_HANDTIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_HANDTIP]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_THUMB = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_THUMB]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_CLAVICLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_CLAVICLE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_SHOULDER = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_SHOULDER]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_ELBOW = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_ELBOW]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_WRIST = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_WRIST]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_HAND = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.RIGHT_HANDTIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_HANDTIP]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_THUMB = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_THUMB]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_HIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_HIP]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_KNEE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_KNEE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_ANKLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_ANKLE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_FOOT = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_FOOT]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_HIP = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_HIP]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_KNEE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_KNEE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_ANKLE = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_ANKLE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_FOOT = Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_FOOT]).position)), fx, fy, cx, cy);
                        keypoints2D_34.HEAD = new float[2] { InvalidValue, InvalidValue };  //Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.HEAD]).position)), fx, fy, cx, cy);
                        keypoints2D_34.NOSE = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.LEFT_EYE = new float[2] { InvalidValue, InvalidValue };  // Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.LEFT_EYE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.LEFT_EAR = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.RIGHT_EYE = new float[2] { InvalidValue, InvalidValue };  // Utility.WorldToScreen(Utility.ToImage(camTransform.InverseTransformPoint(animator.GetBoneTransform(humanBones34[(int)sl.BODY_34_PARTS.RIGHT_EYE]).position)), fx, fy, cx, cy);
                        keypoints2D_34.RIGHT_EAR = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.LEFT_HEEL = new float[2] { InvalidValue, InvalidValue };
                        keypoints2D_34.RIGHT_HEEL = new float[2] { InvalidValue, InvalidValue };

                        singleDetection.Keypoints3D_34 = keypoints3D_34;
                        singleDetection.Keypoints2D_34 = keypoints2D_34;

                        singleDetection.ObjectType = 0;
                        singleDetection.ObjectID = 1; // avatar.Key;

                        frameDetections.ObjectDetections.Add(singleDetection);
                    }

                    frameData.Detections = frameDetections;
                    data.PushNewFrame(frameData);
                }
            }
 
        }

        if (requestSavingJson)
        {
            isInit = false;
            requestSavingJson = false;
            Save(jsonFilename);
        }
    }

    public void Save(string filename)
    {
        data.SaveToJson(filename);
        Debug.Log("Done");
    }

    public void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 100), "Save"))
        {
            requestSavingJson = true;
        }
    }

    public void OnDrawGizmo()
    {
        //Gizmos.DrawSphere(, .1f);
    }
}
