using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace sl
{
    public class ZEDMultiCameraHandler
    {
        /// <summary>
        /// Mutex for the image acquisition thread.
        /// </summary>
        public object grabLock = new object();

        #region Dll Calls 

        /*
         * Multi Cam functions (starting 3.6)
        */

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_init_multi_cam")]
        private static extern int dllz_init_multi_cam(System.Text.StringBuilder area_file, float maxFps);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_close_multi_cam")]
        private static extern void dllz_close_multi_cam();

        //NEED TO ADD INPUT TYPE CLASS
        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_add_camera_multi_cam")]
        private static extern int dllz_add_camera_multi_cam(ref InputType inputType, System.Text.StringBuilder svoFilename, System.Text.StringBuilder ipAdress, ref CameraIdentifier uuid, ref Quaternion worldCameraRotation, ref Vector3 worldCameraPosition);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_remove_camera_multi_cam")]
        private static extern int dllz_remove_camera_multi_cam(ref CameraIdentifier uuid);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_set_camera_position_multi_cam")]
        private static extern int dllz_set_camera_position_multi_cam(ref CameraIdentifier uuid, Pose pose);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_set_cameras_position_multi_cam")]
        private static extern int dllz_set_cameras_position_multi_cam(System.Text.StringBuilder filename);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_set_world_position_multi_cam")]
        private static extern int dllz_set_world_position_multi_cam(Pose worldPosition);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_grab_multi_cam")]
        private static extern int dllz_grab_multi_cam(ref CameraIdentifier uuid);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_sync_cameras_multi_cam")]
        private static extern int dllz_sync_cameras_multi_cam(ref RuntimeMultiCameraParameters rt_params);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_grab_all_multi_cam")]
        private static extern int dllz_grab_all_multi_cam(ref RuntimeMultiCameraParameters rt_params);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_retrieve_measure_multi_cam")]
        private static extern int dllz_retrieve_measure_multi_cam(ref CameraIdentifier uuid, System.IntPtr ptr, int type, int mem, sl.Resolution resolution);

        [DllImport(sl.ZEDCamera.nameDll, EntryPoint = "dllz_update_fused_point_cloud_multi_cam")]
        private static extern int dllz_update_fused_point_cloud_multi_cam(ref int nbVerticesTot, uint filteringPercent);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_retrieve_fused_point_cloud_multi_cam")]
        private static extern int dllz_retrieve_fused_point_cloud_multi_cam([In, Out] float[] vertices);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_enable_object_detection_fusion_multi_cam")]
        private static extern int dllz_enable_object_detection_fusion_multi_cam(ref ObjectDetectionFusionParameters od_params, System.Text.StringBuilder reidDatabaseFile);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_retrieve_fused_objects_multi_cam")]
        private static extern int dllz_retrieve_fused_objects_multi_cam(ref ObjectsFrameSDK objs);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "dllz_disable_object_detection_fusion_multi_cam")]
        private static extern void dllz_disable_object_detection_fusion_multi_cam();

        #endregion

        /// <summary>
        /// MultiCameraHandler initialisation
        /// Initialize memory/generic datas
        /// </summary>
        /// <param name="init_param"></param>
        /// <returns>Error code of sl_zed</returns>
        public ERROR_CODE Init(ref InitMultiCameraParameters init_param)
        {
            if (init_param.areaFilePath == null) init_param.areaFilePath = ""; 

            int e = dllz_init_multi_cam(new System.Text.StringBuilder(init_param.areaFilePath, init_param.areaFilePath.Length), init_param.maxInputFps);

            return (ERROR_CODE)e;  
        }

        /// <summary>
        /// MultiCameraHandler close.
        /// </summary>
        public void Close()
        {
            dllz_close_multi_cam();
        }

        public ERROR_CODE AddCamera(ref InputType inputType, ref CameraIdentifier uuid, ref Quaternion worldCameraRotation, ref Vector3 worldCameraPosition)
        {
            if (inputType.svoFilename == null) inputType.svoFilename = "";
            if (inputType.streamInputIp == null) inputType.streamInputIp = "";

            return (ERROR_CODE)dllz_add_camera_multi_cam(ref inputType, new System.Text.StringBuilder(inputType.svoFilename, inputType.svoFilename.Length), new System.Text.StringBuilder(inputType.streamInputIp, inputType.streamInputIp.Length), ref uuid, ref worldCameraRotation, ref worldCameraPosition);
        }

        /// <summary>
        /// detachCamera : remove the camera from the multi camera list.
        /// </summary>
        /// <param name="uuid">uuid attached to sl::Camera object.</param>
        /// <returns>Error code of sl_zed</returns>
        public ERROR_CODE RemoveCamera(ref CameraIdentifier uuid)
        {
            return (ERROR_CODE)dllz_remove_camera_multi_cam(ref uuid);
        }

        /// <summary>
        /// set the camera position for a sl.ZEDCamera.
        /// </summary>
        /// <param name="uuid"> uuid attached to sl.ZEDCamera object.</param>
        /// <param name="pose"> sl.Pose that contains the position of the camera.</param>
        /// <returns>Error code of sl_zed</returns>
        public ERROR_CODE SetCameraPosition(ref CameraIdentifier uuid, Pose pose)
        {
            return (ERROR_CODE)dllz_set_camera_position_multi_cam(ref uuid, pose);
        }

        /// <summary>
        /// set the camera positions for all attachedcamera
        /// </summary>
        /// <param name="filename">.rloc filename that contains all camera positions. This file must be computed by the MultiCameraCalibration tool.</param>
        /// <returns> Error code for the function</returns>
        public ERROR_CODE SetCamerasPosition(string filename)
        {
            return (ERROR_CODE)dllz_set_cameras_position_multi_cam(new System.Text.StringBuilder(filename, filename.Length));
        }

        /// <summary>
        /// set a common world position for all cameras attached.
        /// Use this function to apply a global offset position to all cameras (to move them to a map for example).
        /// </summary>
        /// <param name="pose">world_position defined as a sl.Pose</param>
        /// <returns> Error code</returns>
        public ERROR_CODE SetWorldPosition(Pose pose)
        {
            return (ERROR_CODE)dllz_set_world_position_multi_cam(pose);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public ERROR_CODE Grab(ref CameraIdentifier uuid)
        {
            return (ERROR_CODE)dllz_grab_multi_cam(ref uuid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt_params"></param>
        /// <returns></returns>
        public ERROR_CODE SyncCameras(ref RuntimeMultiCameraParameters rt_params)
        {
            return (ERROR_CODE)dllz_sync_cameras_multi_cam(ref rt_params);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rt_params"></param>
        /// <returns></returns>
        public ERROR_CODE GrabAll(ref RuntimeMultiCameraParameters rt_params)
        {
            return (ERROR_CODE)dllz_grab_all_multi_cam(ref rt_params);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbVertices"></param>
        /// <param name="filteringPercent"></param>
        /// <returns></returns>
        public ERROR_CODE UpdateFusedPointCloud(ref int nbVertices, uint filteringPercent)
        {
            return (ERROR_CODE)dllz_update_fused_point_cloud_multi_cam(ref nbVertices, filteringPercent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="mat"></param>
        /// <param name="measure"></param>
        /// <param name="mem"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public ERROR_CODE RetrieveFusedMeasure(ref CameraIdentifier uuid, sl.ZEDMat mat, sl.MEASURE measure, sl.ZEDMat.MEM mem = sl.ZEDMat.MEM.MEM_CPU, sl.Resolution resolution = new sl.Resolution())
        {
            return (ERROR_CODE)dllz_retrieve_measure_multi_cam(ref uuid, mat.MatPtr, (int)measure, (int)mem, resolution);
        }

        /// <summary>
        /// retrieve a fused point cloud generated by all listed camera.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public ERROR_CODE RetrieveFusedPointCloud(float[] vertices)
        {
            return (ERROR_CODE)dllz_retrieve_fused_point_cloud_multi_cam(vertices);
        }

        /// <summary>
        /// enable Object detection fusion module
        /// </summary>
        /// <param name="od_params"></param>
        /// <returns></returns>
        public ERROR_CODE EnableObjectDetectionFusion(ref ObjectDetectionFusionParameters od_params)
        {
            if (od_params.reidDatabaseFile == null) od_params.reidDatabaseFile = "";

            lock (grabLock)
            {
                return (ERROR_CODE)dllz_enable_object_detection_fusion_multi_cam(ref od_params, new System.Text.StringBuilder(od_params.reidDatabaseFile, od_params.reidDatabaseFile.Length));
            }
        }

        /// <summary>
        /// retrieve a list of objects (in sl.ObjectsFrameSDK class type) seen by all cameras and merged as if it was seen by a single super-camera.
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public ERROR_CODE RetrieveFusedObjects(ref ObjectsFrameSDK objs)
        {
            return (ERROR_CODE)dllz_retrieve_fused_objects_multi_cam(ref objs);
        }

        /// <summary>
        /// disable object detection fusion module
        /// </summary>
        public void DisableObjectDetectionFusion()
        {
            lock (grabLock)
            {
                dllz_disable_object_detection_fusion_multi_cam();
            }
        }
    }
}

