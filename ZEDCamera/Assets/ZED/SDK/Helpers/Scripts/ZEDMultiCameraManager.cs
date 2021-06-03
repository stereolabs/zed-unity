using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ZEDMultiCameraManager : MonoBehaviour
{

    sl.ZEDMultiCameraHandler multiCameraHandler;
    sl.RuntimeMultiCameraParameters rt_params;
    sl.ObjectsFrameSDK objs;

    sl.CameraIdentifier uuid_2;
    sl.CameraIdentifier uuid_1;
    sl.CameraIdentifier uuid_3;

    // Start is called before the first frame update
    void Start()
    {
        multiCameraHandler = new sl.ZEDMultiCameraHandler();

        sl.InitMultiCameraParameters init_params = new sl.InitMultiCameraParameters
        {
            maxInputFps = 15
        };

        Debug.Log("Init : " + multiCameraHandler.Init(ref init_params));



        sl.ObjectDetectionFusionParameters od_params = new sl.ObjectDetectionFusionParameters();
        od_params.detectionModel = sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE;

        Debug.Log("enable OD " + multiCameraHandler.EnableObjectDetectionFusion(ref od_params));

        /* 
        Quaternion quat = Quaternion.identity;
         Vector3 vec = Vector3.zero;
         sl.CameraIdentifier uuid = new sl.CameraIdentifier();

         sl.InputType inputType = new sl.InputType
         {
             input = sl.INPUT_TYPE.INPUT_TYPE_USB,
             serialNumber = 26261972
         };

         Debug.Log("Add camera " + multiCameraHandler.AddCamera(inputType, uuid, ref quat, ref vec));*/

        string svo_path = "D:/SVO/Multicam/demo_room_aquis/";

         uuid_2 = new sl.CameraIdentifier();

        Quaternion quat2 = new Quaternion(-0.788159f, 0.019431f, -0.023061f, 1);
        Vector3 vec2 = new Vector3(0.000393f, -3608.203897f, 0.002617f);

        sl.InputType inputType_2 = new sl.InputType
        {
            input = sl.INPUT_TYPE.INPUT_TYPE_SVO,
            svoFilename = svo_path + "SVO_SN27355959.svo"
        };

        Debug.Log("Add camera " + multiCameraHandler.AddCamera(ref inputType_2, ref uuid_2, ref quat2, ref vec2));

        uuid_1 = new sl.CameraIdentifier();

        Quaternion quat1 = new Quaternion(-0.627618f, 1.511193f, 0.589684f, 1);
        Vector3 vec1 = new Vector3(-2205.152163f, -3558.421850f, 3804.892312f);

        sl.InputType inputType_1 = new sl.InputType
        {
            input = sl.INPUT_TYPE.INPUT_TYPE_SVO,
            svoFilename = svo_path + "SVO_SN27000102.svo"
        };

        Debug.Log("Add camera " + multiCameraHandler.AddCamera(ref inputType_1, ref uuid_1, ref quat1, ref vec1));

        uuid_3 = new sl.CameraIdentifier();

        Quaternion quat3 = new Quaternion(0.001680f, 2.933510f, 1.112900f, 1);
        Vector3 vec3 = new Vector3(357.992638f, -3601.408081f, 6378.509892f);

        sl.InputType inputType_3 = new sl.InputType
        {
            input = sl.INPUT_TYPE.INPUT_TYPE_SVO,
            svoFilename = svo_path + "SVO_SN28625780.svo"
        };

        Debug.Log("Add camera " + multiCameraHandler.AddCamera(ref inputType_3, ref uuid_3, ref quat3, ref vec3));

        Debug.Log("uuid 1 : " + uuid_1.sn + " / uuid2 :" + uuid_2.sn + " / uuid3 : " + uuid_3.sn);

        //Debug.Log("Set Positions : " + multiCameraHandler.SetCamerasPosition(svo_path + "camera.rloc"));
        objs = new sl.ObjectsFrameSDK();

        rt_params = new sl.RuntimeMultiCameraParameters();
        rt_params.forceGrabCall = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (multiCameraHandler.GrabAll(ref rt_params) == sl.ERROR_CODE.SUCCESS)
        {        
            multiCameraHandler.RetrieveFusedObjects(ref objs);
        }
    }

    private void OnApplicationQuit()
    {
        multiCameraHandler.DisableObjectDetectionFusion();

        multiCameraHandler.RemoveCamera(ref uuid_2);
        multiCameraHandler.RemoveCamera(ref uuid_1);
        multiCameraHandler.RemoveCamera(ref uuid_3);

        multiCameraHandler.Close();
    }
}
