using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace sl
{
    public class ZEDFusionHandler
    {
        /// <summary>
        /// Mutex for the image acquisition thread.
        /// </summary>
        public object grabLock = new object();

        #region Dll Calls 

        /*
         * Fusion API
        */

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_init")]
        private static extern int slmc_init(ref InitFusionParameters init_params);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_close_multi_camera")]
        private static extern void slmc_close_multi_camera();

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_process")]
        private static extern int slmc_process();

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_subscribe")]
        private static extern int slmc_subscribe(ref CameraIdentifier uuid, System.Text.StringBuilder json_config_filename);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_enable_object_detection_fusion")]
        private static extern int slmc_enable_object_detection_fusion(ref ObjectDetectionFusionParameters od_params);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_retrieve_fused_objects")]
        private static extern int slmc_retrieve_fused_objects(ref ObjectsFrameSDK objs, ref ObjectDetectionFusionRuntimeParameters rt_params);

        [DllImport(sl.ZEDCommon.NameDLL, EntryPoint = "slmc_disable_object_detection_fusion")]
        private static extern void slmc_disable_object_detection_fusion();

        #endregion

        /// <summary>
        /// MultiCameraHandler initialisation
        /// Initialize memory/generic datas
        /// </summary>
        /// <param name="init_param"></param>
        /// <returns>Error code of sl_zed</returns>
        public ERROR_CODE Init(ref InitFusionParameters init_params)
        {
            int e = slmc_init(ref init_params);

            return (ERROR_CODE)e;  
        }

        /// <summary>
        /// MultiCameraHandler close.
        /// </summary>
        public void Close()
        {
            slmc_close_multi_camera();
        }

        /// <summary>
        /// enable Object detection fusion module
        /// </summary>
        /// <param name="od_params"></param>
        /// <returns></returns>
        public ERROR_CODE EnableObjectDetectionFusion(ref ObjectDetectionFusionParameters od_params)
        {
            lock (grabLock)
            {
                return (ERROR_CODE)slmc_enable_object_detection_fusion(ref od_params);
            }
        }

        /// <summary>
        /// retrieve a list of objects (in sl.ObjectsFrameSDK class type) seen by all cameras and merged as if it was seen by a single super-camera.
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public ERROR_CODE RetrieveFusedObjects(ref ObjectsFrameSDK objs, ref ObjectDetectionFusionRuntimeParameters rt_params)
        {
            return (ERROR_CODE)slmc_retrieve_fused_objects(ref objs, ref rt_params);
        }

        /// <summary>
        /// disable object detection fusion module
        /// </summary>
        public void DisableObjectDetectionFusion()
        {
            lock (grabLock)
            {
                slmc_disable_object_detection_fusion();
            }
        }
    }
}