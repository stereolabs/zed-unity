//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace sl
{
public static class NativeWrapper
{
    private const string LibName = sl.ZEDCamera.nameDll;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CheckZEDPluginDelegate(int Major, int Minor);

    private static CheckZEDPluginDelegate _checkZEDPlugin;

    private static bool _initialized = false;

    public static bool IsWrapperLoaded => _initialized;

    // Import the function directly using DllImport
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sl_check_plugin(int major, int minor);

    /// <summary>
    /// Call this once to initialize the wrapper
    /// </summary>
    public static bool Init()
    {
        try
        {
            // Assign delegate for convenience
            _checkZEDPlugin = sl_check_plugin;
            _initialized = true;
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError($"[NativeWrapper] Native library {LibName} not found: {e.Message}");
        }
        catch (EntryPointNotFoundException e)
        {
            Debug.LogError($"[NativeWrapper] Function sl_check_plugin not found: {e.Message}");
        }
        
        return _initialized;
    }

    /// <summary>
    /// Call the plugin check function
    /// </summary>
    public static int CheckPlugin()
    {
        if (!_initialized || _checkZEDPlugin == null)
            throw new InvalidOperationException("NativeWrapper is not initialized or function missing.");

        return _checkZEDPlugin(ZEDCamera.PluginVersion.Major, ZEDCamera.PluginVersion.Minor);
    }
}

    /// <summary>
    /// Main interface between Unity and the ZED SDK. Primarily consists of extern calls to the ZED SDK wrapper .dll and
    /// low-level logic to process data sent to/received from it.
    /// </summary>
    /// <remarks>The ZEDManager component creates a ZEDCamera instance in Awake() and handles all initialization.
    /// Most ZED functionality can be handled simply in Unity via ZEDManager or other high-level manager components
    /// (ZEDSpatialMappingManager, ZEDPlaneDetectionManager, etc.)
    /// Textures created by ZEDCamera by CreateTextureImageType() and CreateTextureMeasureType()
    /// are updated automatically by the wrapper whenever a new frame is available. They represent a specific kind of
    /// output, like left RGB image, or depth relative to the right sensor. As such, you never need more than one texture
    /// of each kind, since it can simply be reused by all scripts that need it. Therefore, textures created in ZEDCamera
    /// are indexed by their type (Image or Measure) and then by the options of each type. If a script needs a certain kind
    /// of output, ZEDCamera will make a new one if it doesn't exist, but refer to the existing one otherwise.</remarks>
    ///
    public class ZEDCamera
    {
        /// <summary>
        /// Type of textures requested.
        /// </summary>
        public enum TYPE_VIEW
        {
            /// <summary>
            /// Image-type texture. Human-viewable but loses measurement accuracy.
            /// </summary>
            RETRIEVE_IMAGE,
            /// <summary>
            /// Measure-type texture. Preserves measurement accuracy but isn't human-viewable.
            /// </summary>
            RETRIEVE_MEASURE
        }

        /// <summary>
        /// Information for a requested texture. Stored in the texturesRequested list so DestroyTexture()
        /// can be called with the correct arguments in DestroyAllTexture().
        /// </summary>
        private struct TextureRequested
        {
            /// <summary>
            /// Texture type - 'image' or 'measure.' Assigned using ZEDCamera.TYPE_VIEW.
            /// </summary>
            public int type;
            /// <summary>
            /// View mode (left/right eye, depth, etc.) Assigned using ZEDCommon.VIEW.
            /// </summary>
            public int option;
        };

        /********* Camera members ********/

        /// <summary>
        /// DLL name, used for extern calls to the wrapper.
        /// </summary>
        public const string nameDll = sl.ZEDCommon.NameDLL;

        /// <summary>
        /// List of all created textures, representing SDK output. Indexed by ints corresponding to its ZEDCamera.TYPE_VIEW
        /// and its ZEDCommon.VIEW as you can't have more than one texture for each combination (nor would it be useful to).
        /// </summary>
        private Dictionary<int, Dictionary<int, Texture2D>> textures;

        /// <summary>
        /// List of all requested textures. Used for destroying all textures when the camera closes.
        /// </summary>
        private List<TextureRequested> texturesRequested;

        /// <summary>
        /// Width of the textures in pixels. Corresponds to the ZED's current resolution setting.
        /// </summary>
        private int imageWidth;
        /// <summary>
        /// Width of the images returned by the ZED in pixels. Corresponds to the ZED's current resolution setting.
        /// </summary>
        public int ImageWidth
        {
            get
            {
                return imageWidth;
            }
        }
        /// <summary>
        /// Height of the textures in pixels. Corresponds to the ZED's current resolution setting.
        /// </summary>
        private int imageHeight;
        /// <summary>
        /// Height of the images returned by the ZED in pixels. Corresponds to the ZED's current resolution setting.
        /// </summary>
        public int ImageHeight
        {
            get
            {
                return imageHeight;
            }
        }
        /// <summary>
        /// Projection matrix corresponding to the ZED's camera traits. Useful for lining up virtual cameras with the ZED image.
        /// </summary>
        private Matrix4x4 projection = new Matrix4x4();
        /// <summary>
        /// Projection matrix corresponding to the ZED's camera traits. Useful for lining up virtual cameras with the ZED image.
        /// </summary>
        public Matrix4x4 Projection
        {
            get
            {
                return projection;
            }
        }


        /// <summary>
        /// True if the ZED SDK is installed.
        /// </summary>
        private static bool pluginIsReady = true;

        /// <summary>
        /// Mutex for the image acquisition thread.
        /// </summary>
        public object grabLock = new object();

        /// <summary>
        /// Current ZED resolution setting. Set at initialization.
        /// </summary>
        private RESOLUTION currentResolution;

        /// <summary>
        /// Callback for c++ debugging. Should not be used in C#.
        /// </summary>
        private delegate void DebugCallback(string message);

        /// <summary>
        /// Desired FPS from the ZED camera. This is the maximum FPS for the ZED's current
        /// resolution unless a lower setting was specified in Init().
        /// Maximum values are bound by the ZED's output, not system performance.
        /// </summary>
        private uint fpsMax = 60; //Defaults to HD720 resolution's output.
        /// <summary>
        /// Desired FPS from the ZED camera. This is the maximum FPS for the ZED's current
        /// resolution unless a lower setting was specified in Init().
        /// Maximum values are bound by the ZED's output, not system performance.
        /// </summary>
        public float GetRequestedCameraFPS()
        {
            return fpsMax;
        }
        /// <summary>
        /// Holds camera settings like brightness/contrast, gain/exposure, etc.
        /// </summary>
        private ZEDCameraSettings cameraSettingsManager = new ZEDCameraSettings();

        /// <summary>
        /// Camera's stereo baseline (distance between the cameras). Extracted from calibration files.
        /// </summary>
        private float baseline = 0.0f;
        /// <summary>
        /// Camera's stereo baseline (distance between the cameras). Extracted from calibration files.
        /// </summary>
        public float Baseline
        {
            get { return baseline; }
        }
        /// <summary>
        /// ZED's current horizontal field of view in degrees.
        /// </summary>
        private float fov_H = 0.0f;
        /// <summary>
        /// ZED's current vertical field of view in degrees.
        /// </summary>
        private float fov_V = 0.0f;
        /// <summary>
        /// ZED's current horizontal field of view in degrees.
        /// </summary>
        public float HorizontalFieldOfView
        {
            get { return fov_H; }
        }
        /// <summary>
        /// ZED's current vertical field of view in degrees.
        /// </summary>
        public float VerticalFieldOfView
        {
            get { return fov_V; }
        }
        /// <summary>
        /// Structure containing information about all the sensors available in the current device
        /// </summary>
        private SensorsConfiguration sensorsConfiguration;
        /// <summary>
        /// Stereo parameters for current ZED camera prior to rectification (distorted).
        /// </summary>
        private CalibrationParameters calibrationParametersRaw;
        /// <summary>
        /// Stereo parameters for current ZED camera after rectification (undistorted).
        /// </summary>
        private CalibrationParameters calibrationParametersRectified;
        /// <summary>
        /// Camera model - ZED or ZED Mini.
        /// </summary>
        private sl.MODEL cameraModel;

        /// <summary>
        /// Whether the camera has been successfully initialized.
        /// </summary>
        private bool cameraReady = false;

        /// <summary>
        /// Structure containing information about all the sensors available in the current device
        /// </summary>
        public SensorsConfiguration SensorsConfiguration
        {
            get { return sensorsConfiguration; }
        }
        /// <summary>
        /// Stereo parameters for current ZED camera prior to rectification (distorted).
        /// </summary>
        public CalibrationParameters CalibrationParametersRaw
        {
            get { return calibrationParametersRaw; }
        }
        /// <summary>
        /// Stereo parameters for current ZED camera after rectification (undistorted).
        /// </summary>
        public CalibrationParameters CalibrationParametersRectified
        {
            get { return calibrationParametersRectified; }
        }
        /// <summary>
        /// Camera model
        /// </summary>
        public sl.MODEL CameraModel
        {
            get { return cameraModel; }
        }
        /// <summary>
        /// Whether the camera has been successfully initialized.
        /// </summary>
        public bool IsCameraReady
        {
            get { return cameraReady; }
        }

        /// <summary>
        /// Whether the current device (ZED or ZED Mini) should be used for pass-through AR.
        /// True if using ZED Mini, false if using ZED. </summary><remarks>Note: the plugin will allow using the original ZED
        /// for pass-through but it will feel quite uncomfortable due to the baseline.</remarks>
        public bool IsHmdCompatible
        {
            get { return cameraModel == sl.MODEL.ZED_M; }
        }

        /// <summary>
        /// Camera ID (for multiple cameras)
        /// </summary>
        public int CameraID = 0;

        /// <summary>
        /// Layer that the ZED can't see, but overlay cameras created by ZEDMeshRenderer and ZEDPlaneRenderer can.
        /// </summary>
        //int tagInvisibleToZED = 16;
        /// <summary>
        /// Layer that the ZED can't see, but overlay cameras created by ZEDMeshRenderer and ZEDPlaneRenderer can.
        /// </summary>
        public int TagInvisibleToZED
        {
            get { return ZEDLayers.tagInvisibleToZED; }
        }
        public const int brightnessDefault = 4;
        public const int contrastDefault = 4;
        public const int hueDefault = 0;
        public const int saturationDefault = 4;
        public const int sharpnessDefault = 3;
        public const int gammaDefault = 5;
        public const int whitebalanceDefault = 2600;


        #region DLL Calls
        /// <summary>
        /// Current Plugin Version.
        /// </summary>
        public static readonly System.Version PluginVersion = new System.Version(5, 1, 0);

        /******** DLL members ***********/
        [DllImport(nameDll, EntryPoint = "GetRenderEventFunc")]
        private static extern IntPtr GetRenderEventFunc();

        [DllImport(nameDll, EntryPoint = "sl_register_callback_debuger")]
        private static extern void dllz_register_callback_debuger(DebugCallback callback);


        /*
          * Utils function.
          */
        [DllImport(nameDll, EntryPoint = "sl_free")]
        public static extern void dllz_free(IntPtr ptr);

        [DllImport(nameDll, EntryPoint = "sl_unload_all_instances")]
        private static extern void dllz_unload_all_instances();

        [DllImport(nameDll, EntryPoint = "sl_unload_instance")]
        private static extern void dllz_unload_instance(int id);

        [DllImport(nameDll, EntryPoint = "sl_find_usb_device")]
        private static extern bool dllz_find_usb_device(USB_DEVICE dev);

        [DllImport(nameDll, EntryPoint = "sl_generate_unique_id")]
        private static extern int dllz_generate_unique_id([In, Out] byte[] id);

        /*
          * Create functions
          */
        [DllImport(nameDll, EntryPoint = "sl_create_camera")]
        private static extern bool dllz_create_camera(int cameraID);


        /*
        * Opening function (Opens camera and creates textures).
        * Some initparameters are passed as arguments to facilitate Marshalling.
        */
        [DllImport(nameDll, EntryPoint = "sl_open_camera")]
        private static extern int dllz_open(int cameraID, ref dll_initParameters parameters, uint serialNumber, System.Text.StringBuilder svoPath, System.Text.StringBuilder ipStream, int portStream, System.Text.StringBuilder output, System.Text.StringBuilder opt_settings_path, System.Text.StringBuilder opencv_calib_path);

        /*
         * Close function.
         */
        [DllImport(nameDll, EntryPoint = "sl_close_camera")]
        private static extern void dllz_close(int cameraID);


        /*
         * Grab function.
         */
        [DllImport(nameDll, EntryPoint = "sl_grab")]
        private static extern int dllz_grab(int cameraID, ref sl.RuntimeParameters runtimeParameters);


        /*
         * GetDeviceList function
         */
        [DllImport(nameDll, EntryPoint = "sl_get_device_list")]
        private static extern void dllz_get_device_list(sl.DeviceProperties[] deviceList, out int nbDevices);

        /*
         * GetStreamingDeviceList function
         */
        [DllImport(nameDll, EntryPoint = "sl_get_streaming_device_list")]
        private static extern void dllz_get_streaming_device_list(sl.StreamingProperties[] streamingDeviceList, out int nbDevices);

        /*
        * Reboot function.
        */
        [DllImport(nameDll, EntryPoint = "sl_reboot")]
        private static extern int dllz_reboot(int serialNumber, bool fullReboot);

        /*
        * Recording functions.
        */
        [DllImport(nameDll, EntryPoint = "sl_enable_recording")]
        private static extern int dllz_enable_recording(int cameraID, System.Text.StringBuilder video_filename, int compresssionMode, int bitrate, int target_fps, bool transcode);

        [DllImport(nameDll, EntryPoint = "sl_get_recording_status")]
        private static extern IntPtr dllz_get_recording_status(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_recording_parameters")]
        private static extern IntPtr dllz_get_recording_parameters(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_disable_recording")]
        private static extern bool dllz_disable_recording(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_pause_recording")]
        private static extern void dllz_pause_recording(int cameraID, bool status);

        // Recording Gen 2 functions

        [DllImport(nameDll, EntryPoint = "sl_ingest_data_into_svo")]
        private static extern ERROR_CODE dllz_ingest_data_into_svo(int cameraID, ref SVOData data);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_data_size")]
        private static extern int dllz_get_svo_data_size(int cameraID, string key, ulong ts_begin, ulong ts_end);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_svo_data")]
        private static extern ERROR_CODE dllz_retrieve_svo_data(int cameraID, string key, int nb_data, [Out] SVOData[] data, ulong ts_begin, ulong ts_end);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_data_size")]
        private static extern int dllz_get_svo_data_keys_size(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_data_keys")]
        private static extern void dllz_get_svo_data_keys(int cameraID, int nb_keys, [Out] string[] keys);

        /*
        * Texturing functions.
        */
        [DllImport(nameDll, EntryPoint = "sl_retrieve_textures")]
        private static extern void dllz_retrieve_textures(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_updated_textures_timestamp")]
        private static extern ulong dllz_get_updated_textures_timestamp(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_swap_textures")]
        private static extern void dllz_swap_textures(int cameraID);



        [DllImport(nameDll, EntryPoint = "sl_register_texture_image_type")]
        private static extern int dllz_register_texture_image_type(int cameraID, int option, IntPtr id, int width, int height);

        [DllImport(nameDll, EntryPoint = "sl_register_texture_measure_type")]
        private static extern int dllz_register_texture_measure_type(int cameraID, int option, IntPtr id, int width, int height);

        [DllImport(nameDll, EntryPoint = "sl_unregister_texture_measure_type")]
        private static extern int dllz_unregister_texture_measure_type(int cameraID, int option);

        [DllImport(nameDll, EntryPoint = "sl_unregister_texture_image_type")]
        private static extern int dllz_unregister_texture_image_type(int cameraID, int option);

        [DllImport(nameDll, EntryPoint = "sl_get_copy_mat_texture_image_type")]
        private static extern IntPtr dllz_get_copy_mat_texture_image_type(int cameraID, int option);

        [DllImport(nameDll, EntryPoint = "sl_get_copy_mat_texture_measure_type")]
        private static extern IntPtr dllz_get_copy_mat_texture_measure_type(int cameraID, int option);


        /*
         * Camera control functions.
         */

        [DllImport(nameDll, EntryPoint = "sl_is_camera_setting_supported")]
        private static extern bool dllz_is_video_setting_supported(int id, int setting);

        [DllImport(nameDll, EntryPoint = "sl_set_video_settings")]
        private static extern void dllz_set_video_settings(int id, int mode, int value);

        [DllImport(nameDll, EntryPoint = "sl_get_video_settings")]
        private static extern int dllz_get_video_settings(int id, int mode, ref int value);

        [DllImport(nameDll, EntryPoint = "sl_set_roi_for_aec_agc")]
        private static extern int dllz_set_roi_for_aec_agc(int id, int side, sl.Rect roi, bool reset);

        [DllImport(nameDll, EntryPoint = "sl_get_roi_for_aec_agc")]
        private static extern int dllz_get_roi_for_aec_agc(int id, int side, ref sl.Rect roi);


        [DllImport(nameDll, EntryPoint = "sl_get_input_type")]
        private static extern int dllz_get_input_type(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_camera_fps")]
        private static extern float dllz_get_camera_fps(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_is_opened")]
        private static extern bool dllz_is_opened(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_width")]
        private static extern int dllz_get_width(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_height")]
        private static extern int dllz_get_height(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_update_self_calibration")]
        private static extern void dllz_update_self_calibration(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_calibration_parameters")]
        private static extern IntPtr dllz_get_calibration_parameters(int cameraID, bool raw);

        [DllImport(nameDll, EntryPoint = "sl_get_sensors_configuration")]
        private static extern IntPtr dllz_get_sensors_configuration(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_camera_model")]
        private static extern int dllz_get_camera_model(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_camera_firmware")]
        private static extern int dllz_get_camera_firmware(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_sensors_firmware")]
        private static extern int dllz_get_sensors_firmware(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_zed_serial")]
        private static extern int dllz_get_zed_serial(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_camera_imu_transform")]
        private static extern void dllz_get_camera_imu_transform(int cameraID, out Vector3 translation, out Quaternion rotation);

        [DllImport(nameDll, EntryPoint = "sl_get_camera_timestamp")]
        private static extern ulong dllz_get_image_timestamp(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_current_Timestamp")]
        private static extern ulong dllz_get_current_timestamp(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_frame_dropped_count")]
        private static extern uint dllz_get_frame_dropped_count(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_frame_dropped_percent")]
        private static extern float dllz_get_frame_dropped_percent(int cameraID);
        /*
                [DllImport(nameDll, EntryPoint = "sl_get_init_parameters")]
                private static extern IntPtr dllz_get_init_parameters(int cameraID);

                [DllImport(nameDll, EntryPoint = "sl_get_runtime_parameters")]
                private static extern IntPtr dllz_get_runtime_parameters(int cameraID);

                [DllImport(nameDll, EntryPoint = "sl_get_positional_tracking_parameters")]
                private static extern IntPtr dllz_get_positional_tracking_parameters(int cameraID);*/

        /*
         * SVO control functions.
         */

        [DllImport(nameDll, EntryPoint = "sl_set_svo_position")]
        private static extern void dllz_set_svo_position(int cameraID, int frame);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_number_of_frames")]
        private static extern int dllz_get_svo_number_of_frames(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_position")]
        private static extern int dllz_get_svo_position(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_svo_position_at_timestamp")]
        private static extern int dllz_get_svo_position_at_timestamp(int cameraID, ulong timestamp);

        /*
         * Depth Sensing utils functions.
         */
        /* Removed as of ZED SDK v3.0.
       [DllImport(nameDll, EntryPoint = "set_confidence_threshold")]
       private static extern void dllz_set_confidence_threshold(int cameraID, int threshold);
       [DllImport(nameDll, EntryPoint = "set_depth_max_range_value")]
       private static extern void dllz_set_depth_max_range_value(int cameraID, float distanceMax);
       */

        [DllImport(nameDll, EntryPoint = "sl_get_confidence_threshold")]
        private static extern int dllz_get_confidence_threshold(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_depth_max_range_value")]
        private static extern float dllz_get_depth_max_range_value(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_depth_value")]
        private static extern float dllz_get_depth_value(int cameraID, uint x, uint y);

        [DllImport(nameDll, EntryPoint = "sl_get_distance_value")]
        private static extern float dllz_get_distance_value(int cameraID, uint x, uint y);

        [DllImport(nameDll, EntryPoint = "sl_get_normal_value")]
        private static extern bool dllz_get_normal_value(int cameraID, uint x, uint y, out Vector4 value);

        [DllImport(nameDll, EntryPoint = "sl_get_xyz_value")]
        private static extern bool dllz_get_xyz_value(int cameraID, uint x, uint y, out Vector4 value);

        [DllImport(nameDll, EntryPoint = "sl_get_depth_min_range_value")]
        private static extern float dllz_get_depth_min_range_value(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_current_min_max_depth")]
        private static extern float dllz_get_current_min_max_depth(int cameraID, ref float min, ref float max);


        /*
         * Motion Tracking functions.
         */
        [DllImport(nameDll, EntryPoint = "sl_enable_positional_tracking_unity")]
        private static extern int dllz_enable_tracking(int cameraID, ref Quaternion quat, ref Vector3 vec, bool enableSpatialMemory = false, bool enablePoseSmoothing = false, bool enableFloorAlignment = false,
            bool trackingIsStatic = false, bool enableIMUFusion = true, float depthMinRange = -1.0f, bool setGravityAsOrigin = true, sl.POSITIONAL_TRACKING_MODE mode = sl.POSITIONAL_TRACKING_MODE.GEN_1,
            bool enableLocalizationOnly = false, bool enable2DGroundMode = false, System.Text.StringBuilder aeraFilePath = null);

        [DllImport(nameDll, EntryPoint = "sl_disable_positional_tracking")]
        private static extern void dllz_disable_tracking(int cameraID, System.Text.StringBuilder path);

        [DllImport(nameDll, EntryPoint = "sl_is_positional_tracking_enabled")]
        private static extern bool dllz_is_positional_tracking_enabled(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_save_area_map")]
        private static extern int dllz_save_area_map(int cameraID, System.Text.StringBuilder path);

        [DllImport(nameDll, EntryPoint = "sl_get_position_data")]
        private static extern int dllz_get_position_data(int cameraID, ref Pose pose, int reference_frame);

        [DllImport(nameDll, EntryPoint = "sl_get_positional_tracking_status")]
        private static extern IntPtr dllz_get_positional_tracking_status(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_position")]
        private static extern int dllz_get_position(int cameraID, ref Quaternion quat, ref Vector3 vec, int reference_frame);

        [DllImport(nameDll, EntryPoint = "sl_get_position_at_target_frame")]
        private static extern int dllz_get_position_at_target_frame(int cameraID, ref Quaternion quaternion, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation, int reference_frame);

        [DllImport(nameDll, EntryPoint = "sl_transform_pose")]
        private static extern void dllz_transform_pose(ref Quaternion quaternion, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation);

        [DllImport(nameDll, EntryPoint = "sl_reset_positional_tracking")]
        private static extern int dllz_reset_tracking(int cameraID, Quaternion rotation, Vector3 translation);

        [DllImport(nameDll, EntryPoint = "sl_reset_tracking_with_offset")]
        private static extern int dllz_reset_tracking_with_offset(int cameraID, Quaternion rotation, Vector3 translation, Quaternion offsetQuaternion, Vector3 offsetTranslation);

        [DllImport(nameDll, EntryPoint = "sl_estimate_initial_position")]
        private static extern int dllz_estimate_initial_position(int cameraID, ref Quaternion quaternion, ref Vector3 translation, int countSuccess, int countTimeout);

        [DllImport(nameDll, EntryPoint = "sl_set_imu_prior_orientation")]
        private static extern int dllz_set_imu_prior_orientation(int cameraID, Quaternion rotation);

        [DllImport(nameDll, EntryPoint = "sl_get_internal_imu_orientation")]
        private static extern int dllz_get_internal_imu_orientation(int cameraID, ref Quaternion rotation, int reference_time);

        [DllImport(nameDll, EntryPoint = "sl_get_internal_sensors_data")]
        private static extern int dllz_get_internal_sensors_data(int cameraID, ref SensorsData imuData, int reference_time);

        [DllImport(nameDll, EntryPoint = "sl_get_area_export_state")]
        private static extern int dllz_get_area_export_state(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_set_region_of_interest")]
        private static extern int dllz_set_region_of_interest(int cameraID, IntPtr roiMask, bool[] module);

        [DllImport(nameDll, EntryPoint = "sl_get_region_of_interest")]
        private static extern int dllz_get_region_of_interest(int cameraID, IntPtr roiMask, int width, int height, MODULE module);

        [DllImport(nameDll, EntryPoint = "sl_start_region_of_interest_auto_detection")]
        private static extern int dllz_start_region_of_interest_auto_detection(int cameraID, ref RegionOfInterestParameters roiParams);

        [DllImport(nameDll, EntryPoint = "sl_get_region_of_interest_auto_detection_status")]
        private static extern int dllz_get_region_of_interest_auto_detection_status(int cameraID);

        /*
        * Spatial Mapping functions.
        */
        [DllImport(nameDll, EntryPoint = "sl_enable_spatial_mapping")]
        private static extern int dllz_enable_spatial_mapping(int cameraID, ref SpatialMappingParameters mappingParams);

        [DllImport(nameDll, EntryPoint = "sl_disable_spatial_mapping")]
        private static extern void dllz_disable_spatial_mapping(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_spatial_mapping_parameters")]
        private static extern IntPtr dllz_get_spatial_mapping_parameters(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_pause_spatial_mapping")]
        private static extern void dllz_pause_spatial_mapping(int cameraID, bool status);

        [DllImport(nameDll, EntryPoint = "sl_request_mesh_async")]
        private static extern void dllz_request_mesh_async(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_mesh_request_status_async")]
        private static extern int dllz_get_mesh_request_status_async(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_update_mesh")]
        private static extern int dllz_update_mesh(int cameraID, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_mesh")]
        private static extern int dllz_retrieve_mesh(int cameraID, Vector3[] vertices, int[] triangles, int nbSubmesh, Vector2[] uvs, IntPtr textures);

        [DllImport(nameDll, EntryPoint = "sl_update_fused_point_cloud")]
        private static extern int dllz_update_fused_point_cloud(int cameraID, ref int pbPoints);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_fused_point_cloud")]
        private static extern int dllz_retrieve_fused_point_cloud(int cameraID, Vector4[] points);

        [DllImport(nameDll, EntryPoint = "sl_save_mesh")]
        private static extern bool dllz_save_mesh(int cameraID, string filename, MESH_FILE_FORMAT format);

        [DllImport(nameDll, EntryPoint = "sl_save_point_cloud")]
        private static extern bool dllz_save_point_cloud(int cameraID, string filename, MESH_FILE_FORMAT format);

        [DllImport(nameDll, EntryPoint = "sl_load_mesh")]
        private static extern bool dllz_load_mesh(int cameraID, string filename, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbMaxSubmesh, int[] textureSize = null);

        [DllImport(nameDll, EntryPoint = "sl_apply_texture")]
        private static extern bool dllz_apply_texture(int cameraID, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int[] textureSize, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "sl_filter_mesh")]
        private static extern bool dllz_filter_mesh(int cameraID, FILTER meshFilter, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "sl_get_spatial_mapping_state")]
        private static extern int dllz_get_spatial_mapping_state(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_spatial_mapping_merge_chunks")]
        private static extern void dllz_spatial_mapping_merge_chunks(int cameraID, int numberFaces, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "sl_spatial_mapping_get_gravity_estimation")]
        private static extern void dllz_spatial_mapping_get_gravity_estimation(int cameraID, ref Vector3 v);

        /*
         * Plane Detection functions (starting v2.4)
         */
        [DllImport(nameDll, EntryPoint = "sl_find_floor_plane")]
        private static extern IntPtr dllz_find_floor_plane(int cameraID, out Quaternion rotation, out Vector3 translation, Quaternion priorQuaternion, Vector3 priorTranslation);

        [DllImport(nameDll, EntryPoint = "sl_find_plane_at_hit")]
        private static extern IntPtr dllz_find_plane_at_hit(int cameraID, Vector2 HitPixel, ref sl_PlaneDetectionParameters plane_params, bool refine);

        [DllImport(nameDll, EntryPoint = "sl_convert_floorplane_to_mesh")]
        private static extern int dllz_convert_floorplane_to_mesh(int cameraID, Vector3[] vertices, int[] triangles, out int numVertices, out int numTriangles);

        [DllImport(nameDll, EntryPoint = "sl_convert_hitplane_to_mesh")]
        private static extern int dllz_convert_hitplane_to_mesh(int cameraID, Vector3[] vertices, int[] triangles, out int numVertices, out int numTriangles);


        /*
         * Streaming Module functions (starting v2.8)
         */
        [DllImport(nameDll, EntryPoint = "sl_enable_streaming")]
        private static extern int dllz_enable_streaming(int cameraID, sl.STREAMING_CODEC codec, uint bitrate, ushort port, int gopSize, int adaptativeBitrate, int chunk_size, int target_fps);

        [DllImport(nameDll, EntryPoint = "sl_is_streaming_enabled")]
        private static extern int dllz_is_streaming_enabled(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_disable_streaming")]
        private static extern void dllz_disable_streaming(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_get_streaming_parameters")]
        private static extern IntPtr dllz_get_streaming_parameters(int cameraID);

        /*
        * Objects Detection functions (starting v3.0)
        */

        [DllImport(nameDll, EntryPoint = "sl_check_AI_model_status")]
        private static extern IntPtr dllz_check_AI_model_status(AI_MODELS model, int gpu_id);

        [DllImport(nameDll, EntryPoint = "sl_optimize_AI_model")]
        private static extern int dllz_optimize_AI_model(AI_MODELS model, int gpu_id);


        [DllImport(nameDll, EntryPoint = "sl_enable_object_detection")]
        private static extern int dllz_enable_object_detection(int cameraID, ref ObjectDetectionParameters od_params);

        [DllImport(nameDll, EntryPoint = "sl_get_object_detection_parameters")]
        private static extern IntPtr dllz_get_object_detection_parameters(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_disable_object_detection")]
        private static extern void dllz_disable_object_detection(int cameraID, uint instanceID, bool force_disable_all_instances);

        [DllImport(nameDll, EntryPoint = "sl_ingest_custom_box_objects")]
        private static extern int dllz_ingest_custom_box_objects(int cameraID, int nb_objects, CustomBoxObjectData[] objects_in, uint instanceID);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_objects")]
        private static extern int dllz_retrieve_objects_data(int cameraID, ref ObjectDetectionRuntimeParameters od_params, ref Objects objs, uint instanceID);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_custom_objects")]
        private static extern int dllz_retrieve_custom_objects(int cameraID, ref dll_customObjectDetectionRuntimeParameters od_params, ref Objects objs, uint instanceID);

        [DllImport(nameDll, EntryPoint = "sl_enable_body_tracking")]
        private static extern int dllz_enable_body_tracking(int cameraID, ref BodyTrackingParameters bt_params);

        [DllImport(nameDll, EntryPoint = "sl_get_object_detection_parameters")]
        private static extern IntPtr dllz_get_body_tracking_parameters(int cameraID);

        [DllImport(nameDll, EntryPoint = "sl_disable_body_tracking")]
        private static extern void dllz_disable_body_tracking(int cameraID, uint instanceID, bool force_disable_all_instances);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_bodies")]
        private static extern int dllz_retrieve_bodies_data(int cameraID, ref BodyTrackingRuntimeParameters bt_params, ref Bodies bodies, uint instanceID);

        [DllImport(nameDll, EntryPoint = "sl_update_objects_batch")]
        private static extern int dllz_update_objects_batch(int cameraID, out int nbBatches);

        [DllImport(nameDll, EntryPoint = "sl_get_objects_batch")]
        private static extern int dllz_get_objects_batch_data(int cameraID, int batch_index, ref int numData, ref int id, ref OBJECT_CLASS label, ref OBJECT_SUBCLASS sublabel, ref POSITIONAL_TRACKING_STATE trackingState,
            [In, Out] Vector3[] position, [In, Out] float[,] positionCovariances, [In, Out] Vector3[] velocities, [In, Out] ulong[] timestamps, [In, Out] Vector2[,] boundingBoxes2D, [In, Out] Vector3[,] boundingBoxes,
            [In, Out] float[] confidences, [In, Out] OBJECT_ACTION_STATE[] actionStates, [In, Out] Vector2[,] headBoundingBoxes2D, [In, Out] Vector3[,] headBoundingBoxes, [In, Out] Vector3[] headPositions);

        /*
        * Save utils function
        */
        [DllImport(nameDll, EntryPoint = "sl_save_current_image")]
        private static extern int dllz_save_current_image(int cameraID, VIEW view, string filename);

        [DllImport(nameDll, EntryPoint = "sl_save_current_depth")]
        private static extern int dllz_save_current_depth(int cameraID, int side, string filename);

        [DllImport(nameDll, EntryPoint = "sl_save_current_point_cloud")]
        private static extern int dllz_save_current_point_cloud(int cameraID, int side, string filename);

        /*
         * Specific plugin functions
         */

        /*        [DllImport(nameDll, EntryPoint = "sl_check_plugin")]
                private static extern int dllz_check_plugin(int major, int minor);*/

        [DllImport(nameDll, EntryPoint = "sl_get_sdk_version")]
        private static extern IntPtr dllz_get_sdk_version();

        /*
         * Change the coordinate system of a transform matrix.
        */
        [DllImport(nameDll, EntryPoint = "sl_convert_coordinate_system")]
        private static extern int dllz_convert_coordinate_system(ref Quaternion rotation, ref Vector3 translation, sl.COORDINATE_SYSTEM coordSystemSrc, sl.COORDINATE_SYSTEM coordSystemDest);

        [DllImport(nameDll, EntryPoint = "sl_compute_offset")]
        private static extern void dllz_compute_offset(float[] A, float[] B, int nbVectors, float[] C);

        [DllImport(nameDll, EntryPoint = "sl_compute_optical_center_offsets")]
        private static extern System.IntPtr dllz_compute_optical_center_offsets(ref Vector4 calibLeft, ref Vector4 calibRight, int width, int height, float planeDistance);


        /*
         * Retreieves used by mat
         */
        [DllImport(nameDll, EntryPoint = "sl_retrieve_measure")]
        private static extern int dllz_retrieve_measure(int cameraID, System.IntPtr ptr, int type, int mem, int width, int height);

        [DllImport(nameDll, EntryPoint = "sl_retrieve_image")]
        private static extern int dllz_retrieve_image(int cameraID, System.IntPtr ptr, int type, int mem, int width, int height);

        #endregion

        public static void UnloadPlugin()
        {
            dllz_unload_all_instances();
        }

        public static void UnloadInstance(int id)
        {
            dllz_unload_instance(id);
        }


        public static void ComputeOffset(float[] A, float[] B, int nbVectors, ref Quaternion rotation, ref Vector3 translation)
        {
            float[] C = new float[16];
            if (A.Length != 4 * nbVectors || B.Length != 4 * nbVectors || C.Length != 16) return;
            dllz_compute_offset(A, B, nbVectors, C);

            Matrix4x4 m = Matrix4x4.identity;
            Float2Matrix(ref m, C);

            rotation = Matrix4ToQuaternion(m);
            Vector4 t = m.GetColumn(3);
            translation.x = t.x;
            translation.y = t.y;
            translation.z = t.z;

        }

        /// <summary>
        /// Return a string from a pointer to a char. Used in GetSDKVersion().
        /// </summary>
        /// <param name="ptr">Pointer to a char.</param>
        /// <returns>The char as a string.</returns>
        private static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;
            if (len == 0)
            {
                return "";
            }
            byte[] array = new byte[len];
            Marshal.Copy(ptr, array, 0, len);
            return System.Text.Encoding.ASCII.GetString(array);
        }

        /// <summary>
        /// Displays a console message. Used to display C++ SDK messages in Unity's console.
        /// </summary>
        /// <param name="message"></param>
        private static void DebugMethod(string message)
        {
            Debug.Log("[ZED plugin]: " + message);
        }

        /// <summary>
        /// Convert a pointer to a char into an array of bytes. Used to send file names to SDK for SVO recording.
        /// </summary>
        /// <param name="ptr">Pointer to a char.</param>
        /// <returns>The array.</returns>
        private static byte[] StringUtf8ToByte(string str)
        {
            byte[] array = System.Text.Encoding.ASCII.GetBytes(str);
            return array;
        }

        /// <summary>
        /// Gets the max FPS for each resolution setting. Higher FPS will cause lower GPU performance.
        /// </summary>
        /// <param name="reso"></param>
        /// <returns>The resolution</returns>
        static private uint GetFpsForResolution(RESOLUTION reso)
        {
            if (reso == RESOLUTION.HD1080) return 30;
            else if (reso == RESOLUTION.HD2K) return 15;
            else if (reso == RESOLUTION.HD720) return 60;
            else if (reso == RESOLUTION.VGA) return 100;
            return 30;
        }

        /// <summary>
        /// Get a quaternion from a matrix with a minimum size of 3x3.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>A quaternion which contains the matrix's rotation.</returns>
        public static Quaternion Matrix4ToQuaternion(Matrix4x4 m)
        {
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }

        /// <summary>
        /// Performs a rigid transform.
        /// </summary>
        /// <param name="quaternion"></param>
        /// <param name="translation"></param>
        /// <param name="targetQuaternion"></param>
        /// <param name="targetTranslation"></param>
        public static void TransformPose(ref Quaternion quaternion, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation)
        {
            dllz_transform_pose(ref quaternion, ref translation, ref targetQuaternion, ref targetTranslation);
        }

        public static string GenerateUniqueID()
        {
            byte[] array = new byte[37];
            int size = dllz_generate_unique_id(array);

            return new string(System.Text.Encoding.ASCII.GetChars(array));
        }

        /// <summary>
        /// Checks that the ZED plugin's dependencies are installed.
        /// </summary>
        public static bool CheckPlugin()
        {
            if (NativeWrapper.Init())
            {
                int res = NativeWrapper.CheckPlugin();
                if (res == 0)
                {
                    pluginIsReady = true;
                    return true;
                }
            }

            //0 = installed SDK is compatible with plugin. 1 otherwise.
            Debug.LogError(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_DEPENDENCIES_ISSUE));
            return false;
        }

        /// <summary>
        /// Checks if the USB device of a 'brand' type is connected. Used to check if a VR headset are connected
        /// for display in ZEDManager's Inspector.
        /// </summary>
        /// <returns><c>True</c>, if USB device connected was found, <c>false</c> otherwise.</returns>
        /// <param name="Device brand.">Type.</param>
        public static bool CheckUSBDeviceConnected(USB_DEVICE Type)
        {
            return dllz_find_usb_device(Type);
        }



        /// <summary>
        /// Private constructor. Initializes lists to hold references to textures and texture requests.
        /// </summary>
        public ZEDCamera()
        {
            //Create the textures
            textures = new Dictionary<int, Dictionary<int, Texture2D>>();
            texturesRequested = new List<TextureRequested>();
        }

        /// <summary>
        /// Create a camera in Live mode (input comes from a connected ZED device, not SVO playback).
        /// </summary>
        /// <param name="verbose">True to create detailed log file of SDK calls at the cost of performance.</param>
        public bool CreateCamera(int cameraID, bool verbose)
        {
            string infoSystem = SystemInfo.graphicsDeviceType.ToString().ToUpper();
            if (!infoSystem.Equals("DIRECT3D11") && !infoSystem.Equals("OPENGLCORE") && !infoSystem.Equals("VULKAN"))
            {
                throw new Exception("The graphic library [" + infoSystem + "] is not supported");
            }
            CameraID = cameraID;
            //tagOneObject += cameraID;
            return dllz_create_camera(cameraID);
        }

        /// <summary>
        /// Closes the camera and deletes all textures.
        /// Once destroyed, you need to recreate a camera instance to restart again.
        /// </summary>
        public void Destroy()
        {
            cameraReady = false;
            dllz_close(CameraID);
            DestroyAllTexture();
        }
        public void Close()
        {
            dllz_close(CameraID);
            dllz_unload_instance(CameraID);
        }

        /// <summary>
        /// DLL-friendly version of InitParameters (found in ZEDCommon.cs).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct dll_initParameters
        {
            public sl.INPUT_TYPE inputType;
            /// <summary>
            /// Resolution the ZED will be set to.
            /// </summary>
            public sl.RESOLUTION resolution;
            /// <summary>
            /// Desired camera FPS. Max is set by resolution.
            /// </summary>
            public int cameraFps;
            /// <summary>
            /// ID for identifying which of multiple connected ZEDs to use.
            /// </summary>
            public int cameraDeviceID;
            /// <summary>
            /// True to flip images horizontally.
            /// </summary>
            public int cameraImageFlip;
            /// <summary>
            /// True to disable self-calibration, using unoptimized optional calibration parameters.
            /// False is recommended for optimized calibration.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool cameraDisableSelfCalib;
            /// <summary>
            /// True if depth relative to the right sensor should be computed.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool enableRightSideMeasure;
            /// <summary>
            /// True to skip dropped frames during SVO playback.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool svoRealTimeMode;
            /// <summary>
            /// Quality level of depth calculations. Higher settings improve accuracy but cost performance.
            /// </summary>
            public sl.DEPTH_MODE depthMode;
            /// <summary>
            /// This sets the depth stabilizer temporal smoothing strength.
	        /// the depth stabilize smooth range is [0, 100]
            /// 0 means a low temporal smmoothing behavior(for highly dynamic scene),
            /// 100 means a high temporal smoothing behavior(for static scene)
            /// </summary>
            public int depthStabilization;
            /// <summary>
            /// Minimum distance from the camera from which depth will be computed, in the defined coordinateUnit.
            /// </summary>
            public float depthMinimumDistance;
            /// <summary>
            /// Maximum distance that can be computed.
            /// </summary>
            public float depthMaximumDistance;
            /// <summary>
            /// Coordinate unit for all measurements (depth, tracking, etc.). Meters are recommended for Unity.
            /// </summary>
            public UNIT coordinateUnit;
            /// <summary>
            /// Defines order and direction of coordinate system axes. Unity uses left-handed, Y up, so this setting is recommended.
            /// </summary>
            public COORDINATE_SYSTEM coordinateSystem;
            /// <summary>
            /// ID of the graphics card on which the ZED's computations will be performed.
            /// </summary>
            public int sdkGPUId;
            /// <summary>
            /// True for the SDK to provide text feedback.
            /// </summary>
            public int sdkVerbose;
            /// <summary>
            /// True if sensors are required, false will not trigger an error if sensors are missing.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool sensorsRequired;
            /// <summary>
            /// Whether to enable improved color/gamma curves added in ZED SDK 3.0.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool enableImageEnhancement;
            /// <summary>
            /// Define a timeout in seconds after which an error is reported if the \ref open() command fails.
            /// Set to '-1' to try to open the camera endlessly without returning error in case of failure.
            /// Set to '0' to return error in case of failure at the first attempt.
            /// This parameter only impacts the LIVE mode.
            /// </summary>
            public float openTimeoutSec;
            /// <summary>
            /// 	Define the behavior of the automatic camera recovery during grab() function call. When async is enabled and there's an issue with the communication with the camera
            /// the grab() will exit after a short period and return the ERROR_CODE::CAMERA_REBOOTING warning.The recovery will run in the background until the correct communication is restored.
            /// When async_grab_camera_recovery is false, the grab() function is blocking and will return only once the camera communication is restored or the timeout is reached.
            /// The default behavior is synchronous, like previous ZED SDK versions
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool asyncGrabRecovery;
            /// </summary>
            /// Define a computation upper limit to the grab frequency. 0 means that the setting is ignored.
            /// This can be useful to get a known constant fixed rate or limit the computation load while keeping a short exposure time by setting a high camera capture framerate.
            /// The value should be inferior to the InitParameters::camera_fps and strictly positive. It has no effect when reading an SVO file.
            /// This is an upper limit and won't make a difference if the computation is slower than the desired compute capping fps.
            /// Internally the grab function always tries to get the latest available image while respecting the desired fps as much as possible.
            /// Default value is 0.
            /// </summary>
            public float grabComputeCappingFPS;

            /// <summary>
            ///  Enable or disable the image validity verification.
            ///  This will perform additional verification on the image to identify corrupted data. This verification is done in the grab function and requires some computations.
            ///  If an issue is found, the grab function will output a warning as sl::ERROR_CODE::CORRUPTED_FRAME.
            ///  This version doesn't detect frame tearing currently.
            ///  \n default: disabled
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool enableImageValidityCheck;

            public Resolution maximumWorkingResolution;

            /// <summary>
            /// Copy constructor. Takes values from Unity-suited InitParameters class.
            /// </summary>
            /// <param name="init"></param>
            public dll_initParameters(InitParameters init)
            {
                inputType = init.inputType;
                resolution = init.resolution;
                cameraFps = init.cameraFPS;
                svoRealTimeMode = init.svoRealTimeMode;
                coordinateUnit = init.coordinateUnit;
                depthMode = init.depthMode;
                depthMinimumDistance = init.depthMinimumDistance;
                depthMaximumDistance = init.depthMaximumDistance;
                cameraImageFlip = init.cameraImageFlip;
                enableRightSideMeasure = init.enableRightSideMeasure;
                cameraDisableSelfCalib = init.cameraDisableSelfCalib;
                sdkVerbose = init.sdkVerbose;
                sdkGPUId = init.sdkGPUId;
                cameraDeviceID = init.cameraDeviceID;
                coordinateSystem = init.coordinateSystem;
                depthStabilization = init.depthStabilization;
                sensorsRequired = init.sensorsRequired;
                enableImageEnhancement = init.enableImageEnhancement;
                openTimeoutSec = init.openTimeoutSec;
                asyncGrabRecovery = init.asyncGrabCameraRecovery;
                grabComputeCappingFPS = init.grabComputeCappingFPS;
                enableImageValidityCheck = init.enableImageValidityCheck;
                maximumWorkingResolution = init.maximumWorkingResolution;
            }
        }

        /// <summary>
        /// Checks if the ZED camera is plugged in, opens it, and initializes the projection matix and command buffers for updating textures.
        /// </summary>
        /// <param name="initParameters">Class with all initialization settings.
        /// A newly-instantiated InitParameters will have recommended default values.</param>
        /// <returns>ERROR_CODE: The error code gives information about the internal connection process.
        /// If SUCCESS is returned, the camera is ready to use. Every other code indicates an error.</returns>
        public ERROR_CODE Open(ref InitParameters initParameters)
        {
            //Update values with what we're about to pass to the camera.
            currentResolution = initParameters.resolution;
            fpsMax = GetFpsForResolution(currentResolution);
            if (initParameters.cameraFPS == 0)
            {
                initParameters.cameraFPS = (int)fpsMax;
            }
            dll_initParameters initP = new dll_initParameters(initParameters); //DLL-friendly version of InitParameters.
            initP.coordinateSystem = COORDINATE_SYSTEM.LEFT_HANDED_Y_UP; //Left-hand, Y-up is Unity's coordinate system, so we match that

            int v = dllz_open(CameraID, ref initP, initParameters.serialNumber,
                new System.Text.StringBuilder(initParameters.pathSVO, initParameters.pathSVO.Length),
                new System.Text.StringBuilder(initParameters.ipStream, initParameters.ipStream.Length),
                initParameters.portStream,
                new System.Text.StringBuilder(initParameters.sdkVerboseLogFile, initParameters.sdkVerboseLogFile.Length),
                new System.Text.StringBuilder(initParameters.optionalSettingsPath, initParameters.optionalSettingsPath.Length),
                new System.Text.StringBuilder(initParameters.optionalOpencvCalibrationFile, initParameters.optionalOpencvCalibrationFile.Length));

            if ((ERROR_CODE)v != ERROR_CODE.SUCCESS)
            {
                cameraReady = false;
                return (ERROR_CODE)v;
            }

            //Set more values if the initialization was successful.
            imageWidth = dllz_get_width(CameraID);
            imageHeight = dllz_get_height(CameraID);

            if (imageWidth > 0 && imageHeight > 0)
            {
                GetCalibrationParameters(false);
                FillProjectionMatrix();
                baseline = calibrationParametersRectified.Trans[0];
                fov_H = calibrationParametersRectified.leftCam.hFOV * Mathf.Deg2Rad;
                fov_V = calibrationParametersRectified.leftCam.vFOV * Mathf.Deg2Rad;
                cameraModel = GetCameraModel();
                cameraReady = true;
                return (ERROR_CODE)v;
            }
            else
                return sl.ERROR_CODE.CAMERA_NOT_INITIALIZED;

        }



        /// <summary>
        /// Fills the projection matrix with the parameters of the ZED. Needs to be called only once.
        /// This projection matrix is off-center.
        /// </summary>
        /// <param name="zFar"></param>
        /// <param name="zNear"></param>
        public void FillProjectionMatrix(float zFar = 500, float zNear = 0.1f)
        {
            CalibrationParameters parameters = GetCalibrationParameters(false);
            float fovx = parameters.leftCam.hFOV * Mathf.Deg2Rad;
            float fovy = parameters.leftCam.vFOV * Mathf.Deg2Rad;

            float f_imageWidth = (float)ImageWidth;
            float f_imageHeight = (float)ImageHeight;

            //Manually construct the matrix based on initialization/calibration values.
            projection[0, 0] = 1.0f / Mathf.Tan(fovx * 0.5f); //Horizontal FoV.
            projection[0, 1] = 0;
            projection[0, 2] = 2.0f * ((f_imageWidth - 1.0f * parameters.leftCam.cx) / f_imageWidth) - 1.0f; //Horizontal offset.
            projection[0, 3] = 0;

            projection[1, 0] = 0;
            projection[1, 1] = 1.0f / Mathf.Tan(fovy * 0.5f); //Vertical FoV.
            projection[1, 2] = -(2.0f * ((f_imageHeight - 1.0f * parameters.leftCam.cy) / f_imageHeight) - 1.0f); //Vertical offset.
            projection[1, 3] = 0;

            projection[2, 0] = 0;
            projection[2, 1] = 0;
            projection[2, 2] = -(zFar + zNear) / (zFar - zNear); //Near and far planes.
            projection[2, 3] = -(2.0f * zFar * zNear) / (zFar - zNear); //Near and far planes.

            projection[3, 0] = 0;
            projection[3, 1] = 0;
            projection[3, 2] = -1;
            projection[3, 3] = 0.0f;

        }

        /// <summary>
        /// Grabs a new image, rectifies it, and computes the disparity map and (optionally) the depth map.
        /// The grabbing function is typically called in the main loop in a separate thread.
        /// </summary><remarks>For more info, read about the SDK function it calls:
        /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/classsl_1_1Camera.html#afa3678a18dd574e162977e97d7cbf67b </remarks>
        /// <param name="runtimeParameters">Struct holding all grab parameters. </param>
        /// <returns>the function returns false if no problem was encountered,
        /// true otherwise.</returns>
        public sl.ERROR_CODE Grab(ref sl.RuntimeParameters runtimeParameters)
        {
            return (sl.ERROR_CODE)dllz_grab(CameraID, ref runtimeParameters);
        }

        /// <summary>
        /// Return the INPUT_TYPE currently used
        /// </summary>
        /// <returns></returns>
        public sl.INPUT_TYPE GetInputType()
        {
            return (sl.INPUT_TYPE)dllz_get_input_type(CameraID);
        }

        /// <summary>
        /// Creates a file for recording the ZED's output into a .SVO or .AVI video.
        /// </summary><remarks>An SVO is Stereolabs' own format designed for the ZED. It holds the video feed with timestamps
        /// as well as info about the camera used to record it.</remarks>
        /// <param name="videoFileName">Filename. Whether it ends with .svo or .avi defines its file type.</param>
        /// <param name="compressionMode">How much compression to use</param>
        /// <returns>An ERROR_CODE that defines if the file was successfully created and can be filled with images.</returns>
        public ERROR_CODE EnableRecording(string videoFileName, SVO_COMPRESSION_MODE compressionMode = SVO_COMPRESSION_MODE.H264_BASED, int bitrate = 0, int target_fps = 0, bool transcode = false)
        {
            return (ERROR_CODE)dllz_enable_recording(CameraID, new System.Text.StringBuilder(videoFileName, videoFileName.Length), (int)compressionMode, bitrate, target_fps, transcode);
        }

        /// <summary>
        ///  Get the recording information
        /// </summary>
        /// <returns></returns>
        public sl.RecordingStatus GetRecordingStatus()
        {
            IntPtr p = dllz_get_recording_status(CameraID);

            if (p == IntPtr.Zero)
            {
                return new RecordingStatus();
            }
            RecordingStatus parameters = (RecordingStatus)Marshal.PtrToStructure(p, typeof(RecordingStatus));

            return parameters;
        }

        /// <summary>
        /// Pauses or resumes the recording.
        /// </summary>
        /// <param name="status"> if true, the recording is paused. If false, the recording is resumed. </param>
        /// <returns></returns>
        public void PauseRecording(bool status)
        {
            dllz_pause_recording(CameraID, status);
        }

        /// <summary>
        /// Stops recording to an SVO/AVI, if applicable, and closes the file.
        /// </summary>
        public bool DisableRecording()
        {
            return dllz_disable_recording(CameraID);
        }

        /// <summary>
        /// Ingest SVOData in a SVO file.
        /// </summary>
        /// <param name="data">Data to ingest in the SVO file..</param>
        /// Note: The method works only if the camera is recording.
        /// <returns></returns>
        public ERROR_CODE IngestDataIntoSVO(ref SVOData data)
        {
            ERROR_CODE err = dllz_ingest_data_into_svo(CameraID, ref data);
            return err;
        }

        /// <summary>
        /// Retrieves SVO data from the SVO file at the given channel key and in the given timestamp range.
        /// </summary>
        /// <param name="key"> The key of the SVOData that is going to be retrieved.</param>
        /// <param name="data"> The map to be filled with SVOData objects, with timestamps as keys.</param>
        /// <param name="tsBegin"> The beginning of the range.</param>
        /// <param name="tsEnd">The end of the range.</param>
        /// <returns>sl.ERROR_CODE.SUCCESS in case of success, sl.ERROR_CODE.FAILURE otherwise.</returns>
        public ERROR_CODE RetrieveSVOData(string key, ref List<SVOData> data, ulong tsBegin, ulong tsEnd)
        {
            ERROR_CODE err = ERROR_CODE.FAILURE;

            int nb_data = dllz_get_svo_data_size(CameraID, key, tsBegin, tsEnd);

            if (nb_data > 0)
            {
                SVOData[] data_array = new SVOData[nb_data];

                err = dllz_retrieve_svo_data(CameraID, key, nb_data, data_array, tsBegin, tsEnd);
                data = new List<SVOData>(data_array);
            }

            return err;
        }

        /// <summary>
        ///  Gets the external channels that can be retrieved from the SVO file.
        /// </summary>
        /// <returns>List of available keys.</returns>
        public List<string> GetSVODataKeys()
        {
            int nb_keys = dllz_get_svo_data_keys_size(CameraID);

            if (nb_keys > 0)
            {
                string[] keys_array = new string[nb_keys];

                dllz_get_svo_data_keys(CameraID, nb_keys, keys_array);

                List<string> keys = new List<string>(keys_array);

                return keys;
            }

            return new List<string>();
        }

        /// <summary>
        /// Sets the position of the SVO file currently being read to a desired frame.
        /// </summary>
        /// <param name="frame">Index of the desired frame to be decoded.</param>
        public void SetSVOPosition(int frame)
        {
            dllz_set_svo_position(CameraID, frame);
        }

        /// <summary>
        /// Gets the current confidence threshold value for the disparity map (and by extension the depth map).
        /// Values below the given threshold are removed from the depth map.
        /// </summary>
        /// <returns>Filtering value between 0 and 100.</returns>
        public int GetConfidenceThreshold()
        {
            return dllz_get_confidence_threshold(CameraID);
        }

        /// <summary>
        /// Gets the timestamp at the time the latest grabbed frame was extracted from the USB stream.
        /// This is the closest timestamp you can get from when the image was taken. Must be called after calling grab().
        /// </summary>
        /// <returns>Current timestamp in nanoseconds. -1 means it's is not available, such as with an .SVO file without compression.</returns>
        public ulong GetCameraTimeStamp()
        {
            return dllz_get_image_timestamp(CameraID);
        }

        /// <summary>
        /// Gets the current timestamp at the time the function is called. Can be compared to the camera timestamp
        /// for synchronization, since they have the same reference (the computer's start time).
        /// </summary>
        /// <returns>The timestamp in nanoseconds.</returns>
        public ulong GetCurrentTimeStamp()
        {
            return dllz_get_current_timestamp(CameraID);
        }

        /// <summary>
        /// Get the current position of the SVO being recorded to.
        /// </summary>
        /// <returns>Index of the frame being recorded to.</returns>
        public int GetSVOPosition()
        {
            return dllz_get_svo_position(CameraID);
        }

        /// <summary>
        /// Retrieves the frame index within the SVO file corresponding to the provided timestamp.
        /// </summary>
        /// <param name="timestamp">The target timestamp for which the frame index is to be determined.</param>
        /// <returns>The frame index within the SVO file that aligns with the given timestamp. Returns -1 if the timestamp falls outside the bounds of the SVO file.</returns>
        public int GetSVOPositionAtTimestamp(ulong timestamp)
        {
            return dllz_get_svo_position_at_timestamp(CameraID, timestamp);
        }

        /// <summary>
        /// Gets the total number of frames in the loaded SVO file.
        /// </summary>
        /// <returns>Total frames in the SVO file. Returns -1 if the SDK is not reading an SVO.</returns>
        public int GetSVONumberOfFrames()
        {
            return dllz_get_svo_number_of_frames(CameraID);
        }

        /// <summary>
        /// Gets the closest measurable distance by the camera, according to the camera type and depth map parameters.
        /// </summary>
        /// <returns>The nearest possible depth value.</returns>
        public float GetDepthMinRangeValue()
        {
            return dllz_get_depth_min_range_value(CameraID);
        }

        /// <summary>
        /// Returns the current maximum distance of depth/disparity estimation.
        /// </summary>
        /// <returns>The closest depth</returns>
        public float GetDepthMaxRangeValue()
        {
            return dllz_get_depth_max_range_value(CameraID);
        }

        public static AI_MODELS ToAIModel(sl.DEPTH_MODE depth_mode)
        {
            switch (depth_mode)
            {
                case DEPTH_MODE.NEURAL_PLUS:
                    return AI_MODELS.NEURAL_PLUS_DEPTH;
                case DEPTH_MODE.NEURAL:
                    return AI_MODELS.NEURAL_DEPTH;
                case DEPTH_MODE.NEURAL_LIGHT:
                    return AI_MODELS.NEURAL_LIGHT_DEPTH;
                default:
                    return AI_MODELS.LAST;
            }
        }

        /// <summary>
        /// Initialize and Start the tracking functions
        /// </summary>
        /// <param name="quat"> rotation used as initial world transform. By default it should be identity.</param>
        /// <param name="vec"> translation used as initial world transform. By default it should be identity.</param>
        /// <param name="enableSpatialMemory">  (optional) define if spatial memory is enable or not.</param>
        /// <param name="areaFilePath"> (optional) file of spatial memory file that has to be loaded to relocate in the scene.</param>
        /// <returns></returns>
        public sl.ERROR_CODE EnableTracking(ref Quaternion quat, ref Vector3 vec, bool enableSpatialMemory = true, bool enablePoseSmoothing = false, bool enableFloorAlignment = false, bool trackingIsStatic = false,
            bool enableIMUFusion = true, float depthMinRange = -1.0f, bool setGravityAsOrigin = true, sl.POSITIONAL_TRACKING_MODE mode = POSITIONAL_TRACKING_MODE.GEN_1,
            bool enableLocalizationOnly = false, bool enable2DGroundMode = false, string areaFilePath = "")
        {
            sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            trackingStatus = (sl.ERROR_CODE)dllz_enable_tracking(CameraID, ref quat, ref vec, enableSpatialMemory, enablePoseSmoothing, enableFloorAlignment,
                trackingIsStatic, enableIMUFusion, depthMinRange, setGravityAsOrigin, mode, enableLocalizationOnly, enable2DGroundMode, new System.Text.StringBuilder(areaFilePath, areaFilePath.Length));
            return trackingStatus;
        }

        /// <summary>
        /// Reset tracking
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public sl.ERROR_CODE ResetTracking(Quaternion rotation, Vector3 translation)
        {
            sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            trackingStatus = (sl.ERROR_CODE)dllz_reset_tracking(CameraID, rotation, translation);
            return trackingStatus;
        }

        public sl.ERROR_CODE ResetTrackingWithOffset(Quaternion rotation, Vector3 translation, Quaternion rotationOffset, Vector3 translationOffset)
        {
            sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            trackingStatus = (sl.ERROR_CODE)dllz_reset_tracking_with_offset(CameraID, rotation, translation, rotationOffset, translationOffset);
            return trackingStatus;
        }


        public sl.ERROR_CODE EstimateInitialPosition(ref Quaternion rotation, ref Vector3 translation)
        {
            sl.ERROR_CODE status = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            status = (sl.ERROR_CODE)dllz_estimate_initial_position(CameraID, ref rotation, ref translation, 2, 100);
            return status;
        }


        /// <summary>
        ///  Stop the motion tracking, if you want to restart, call enableTracking().
        /// </summary>
        /// <param name="path">The path to save the area file</param>
        public void DisableTracking(string path = "")
        {
            dllz_disable_tracking(CameraID, new System.Text.StringBuilder(path, path.Length));
        }

        /// <summary>
        /// Returns the current state of the area learning saving
        /// </summary>
        /// <returns></returns>
        public sl.AREA_EXPORT_STATE GetAreaExportState()
        {
            return (sl.AREA_EXPORT_STATE)dllz_get_area_export_state(CameraID);
        }

        /// <summary>
        /// Register a texture to the base
        /// </summary>
        private void RegisterTexture(Texture2D m_Texture, int type, int mode)
        {
            TextureRequested t = new TextureRequested();

            t.type = type;
            t.option = mode;
            texturesRequested.Add(t);
            textures[type].Add(mode, m_Texture);
        }

        /// <summary>
        /// Creates or retrieves a texture of type Image. Will be updated each frame automatically.
        /// <para>Image type textures are human-viewable, but have less accuracy than measure types.</para>
        /// </summary>
        /// <remarks>
        /// Note that the new texture will exist on the GPU, so accessing from the CPU will result in an empty image. To get images
        /// with the CPU, use RetrieveImage() instead and specify CPU memory in the arguments.
        /// </remarks>
        /// <param name="mode">What the image shows (left RGB image, right depth image, normal map, etc.)</param>
        /// /// <param name="resolution">Resolution of the image. Should correspond to ZED's current resolution.</param>
        /// <returns>Texture2D that will update each frame with the ZED SDK's output.</returns>
        public Texture2D CreateTextureImageType(VIEW mode, Resolution resolution = new Resolution())
        {
            if (HasTexture((int)TYPE_VIEW.RETRIEVE_IMAGE, (int)mode))
            {
                return textures[(int)TYPE_VIEW.RETRIEVE_IMAGE][(int)mode];
            }
            if (!cameraReady)
                return null;

            int width = ImageWidth;
            int height = imageHeight;
            if (!((uint)resolution.width == 0 && (uint)resolution.height == 0)) //Handles if Resolution arg was empty, as in the default.
            {
                width = (int)resolution.width;
                height = (int)resolution.height;
            }

            Texture2D m_Texture;
            if (mode == VIEW.LEFT_GREY || mode == VIEW.RIGHT_GREY || mode == VIEW.LEFT_UNRECTIFIED_GREY || mode == VIEW.RIGHT_UNRECTIFIED_GREY)
            {
                m_Texture = new Texture2D(width, height, TextureFormat.Alpha8, false);
            }
            else if (mode == VIEW.SIDE_BY_SIDE)
            {
                m_Texture = new Texture2D(width * 2, height, TextureFormat.RGBA32, false); //Needs to be twice as wide for SBS because there are two images.
            }
            else
            {

                m_Texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }
            m_Texture.filterMode = FilterMode.Point;
            m_Texture.wrapMode = TextureWrapMode.Clamp;

            m_Texture.Apply();

            IntPtr idTexture = m_Texture.GetNativeTexturePtr();
            int error = dllz_register_texture_image_type(CameraID, (int)mode, idTexture, (int)resolution.width, (int)resolution.height);
            if (error != 0)
            {
                throw new Exception("CUDA error:" + error + " if the problem appears again, please contact Stereolabs support.");
            }
            if (!textures.ContainsKey((int)TYPE_VIEW.RETRIEVE_IMAGE))
            {
                textures.Add((int)TYPE_VIEW.RETRIEVE_IMAGE, new Dictionary<int, Texture2D>());
            }
            RegisterTexture(m_Texture, (int)TYPE_VIEW.RETRIEVE_IMAGE, (int)mode); //Save so you don't make a duplicate if another script needs the texture.

            return m_Texture;
        }

        /// <summary>
        /// Creates or retrievse a texture of type Measure. Will be updated each frame automatically.
        /// Measure types are not human-viewable, but don't lose any accuracy.
        /// </summary>
        /// <remarks>
        /// Note that the new texture will exist on the GPU, so accessing from the CPU will result in an empty image. To get images
        /// with the CPU, use RetrieveMeasure() instead and specify CPU memory in the arguments.
        /// </remarks>
        /// <param name="mode">What the image shows (disparity, depth, confidence, etc.)</param>
        /// <param name="resolution">Resolution of the image. Should correspond to ZED's current resolution.</param>
        /// <returns>Texture2D that will update each frame with the ZED SDK's output.</returns>
        public Texture2D CreateTextureMeasureType(MEASURE mode, Resolution resolution = new Resolution())
        {
            if (HasTexture((int)TYPE_VIEW.RETRIEVE_MEASURE, (int)mode))
            {
                return textures[(int)TYPE_VIEW.RETRIEVE_MEASURE][(int)mode];
            }
            if (!cameraReady)
                return null;

            Texture2D m_Texture;
            int width = ImageWidth;
            int height = imageHeight;
            if (!((uint)resolution.width == 0 && (uint)resolution.height == 0))
            {
                width = (int)resolution.width;
                height = (int)resolution.height;
            }
            //Handle the mode options.
            if (mode == MEASURE.XYZ || mode == MEASURE.XYZABGR || mode == MEASURE.XYZARGB || mode == MEASURE.XYZBGRA || mode == MEASURE.XYZRGBA || mode == MEASURE.NORMALS
                || mode == MEASURE.XYZ_RIGHT || mode == MEASURE.XYZABGR_RIGHT || mode == MEASURE.XYZARGB_RIGHT || mode == MEASURE.XYZBGRA_RIGHT || mode == MEASURE.XYZRGBA_RIGHT || mode == MEASURE.NORMALS_RIGHT)
            {
                m_Texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            }
            else if (mode == MEASURE.DEPTH || mode == MEASURE.CONFIDENCE || mode == MEASURE.DISPARITY || mode == MEASURE.DEPTH_RIGHT || mode == MEASURE.DISPARITY_RIGHT)
            {
                m_Texture = new Texture2D(width, height, TextureFormat.RFloat, false, true);
            }
            else
            {
                m_Texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            }
            if (!((uint)resolution.width == 0 && (uint)resolution.height == 0))
            {
                m_Texture.filterMode = FilterMode.Bilinear;
            }
            else
            {
                m_Texture.filterMode = FilterMode.Point;
            }
            m_Texture.wrapMode = TextureWrapMode.Clamp;
            m_Texture.Apply();

            IntPtr idTexture = m_Texture.GetNativeTexturePtr();

            int error = dllz_register_texture_measure_type(CameraID, (int)mode, idTexture, (int)resolution.width, (int)resolution.height);

            if (error != 0)
            {
                throw new Exception("CUDA error:" + error + " if the problem appears again, please contact Stereolabs support.");
            }
            if (!textures.ContainsKey((int)TYPE_VIEW.RETRIEVE_MEASURE))
            {
                textures.Add((int)TYPE_VIEW.RETRIEVE_MEASURE, new Dictionary<int, Texture2D>());
            }

            RegisterTexture(m_Texture, (int)TYPE_VIEW.RETRIEVE_MEASURE, (int)mode); //Save to avoid duplicates if texture type is needed elsewhere.

            return m_Texture;
        }

        /// <summary>
        /// Unregisters a texture of type Image. The texture will be destroyed and will no longer be updated each frame.
        /// </summary>
        /// <param name="view">What the image was showing (left RGB image, right depth image, normal map, etc.)</param>
        public bool UnregisterTextureImageType(sl.VIEW view)
        {
            DestroyTextureImageType((int)view);
            return dllz_unregister_texture_image_type(CameraID, (int)view) != 0;
        }

        /// <summary>
        /// Unregisters a texture of type Measure, The texture will be destroyed and will no longer be updated each frame.
        /// </summary>
        /// <param name="measure">What the measure was showing (disparity, depth, confidence, etc.)</param>
        public bool UnregisterTextureMeasureType(sl.MEASURE measure)
        {
            DestroyTextureMeasureType((int)measure);
            return dllz_unregister_texture_measure_type(CameraID, (int)measure) != 0;
        }

        /// <summary>
        /// Copies a Texture of type Image into a ZEDMat. This function should be called after a Grab() and an UpdateTextures().
        /// </summary>
        /// <param name="view">View type (left rgb, right depth, etc.)</param>
        /// <returns>New ZEDMat for an image texture of the selected view type.</returns>
        public ZEDMat RequestCopyMatFromTextureImageType(sl.VIEW view)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_image_type(CameraID, (int)view));
        }

        /// <summary>
        /// Copies a texture of type Measure into a ZEDMat. This function should be called after a Grab() and an UpdateTextures().
        /// </summary>
        /// <param name="measure">Measure type (depth, disparity, confidence, etc.)</param>
        /// <returns>New ZEDMat for a measure texture of the selected measure type.</returns>
        public ZEDMat RequestCopyMatFromTextureMeasureType(sl.MEASURE measure)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_measure_type(CameraID, (int)measure));
        }

        /// <summary>
        /// Destroys a texture and removes its reference in the textures list.
        /// </summary>
        /// <param name="type">Type of texture as an int (0 for Image, 1 for Measure).</param>
        /// <param name="option">Corresponding options enum (sl.VIEW if Image type, sl.MEASURE if Measure type) as an integer.</param>
        private void DestroyTexture(int type, int option)
        {
            if (textures.ContainsKey(type) && textures[type].ContainsKey(option))
            {
                textures[type][option] = null;
                textures[type].Remove(option);
                if (textures[type].Count == 0)
                {
                    textures.Remove(type);
                }
            }
        }

        /// <summary>
        /// Destroy all textures that were ever requested.
        /// </summary>
        private void DestroyAllTexture()
        {
            if (cameraReady)
            {
                foreach (TextureRequested t in texturesRequested)
                {
                    DestroyTexture(t.type, t.option);
                }
                texturesRequested.Clear();
            }
        }


        /// <summary>
        /// Destroy a texture created with CreateTextureImageType().
        /// </summary>
        /// <param name="type">View type (left RGB, right depth image, etc.) as an integer.</param>
        private void DestroyTextureImageType(int option)
        {
            DestroyTexture((int)TYPE_VIEW.RETRIEVE_IMAGE, option);
        }

        /// <summary>
        /// Destroy a texture created with CreateTextureMeasureType().
        /// </summary>
        /// <param name="type">Measure type (depth, confidence, etc.) as an integer.</param>
        private void DestroyTextureMeasureType(int option)
        {
            DestroyTexture((int)TYPE_VIEW.RETRIEVE_MEASURE, option);
        }

        /// <summary>
        /// Retrieves a texture that was already created.
        /// </summary>
        /// <param name="type">Type of texture as an integer (0 for Image, 1 for Measure).</param>
        /// <param name="mode">Corresponding options enum (sl.VIEW if Image type, sl.MEASURE if Measure type) as an integer.</param>
        /// <returns>Existing texture of the given type/mode.</returns>
        public Texture2D GetTexture(TYPE_VIEW type, int mode)
        {
            if (HasTexture((int)type, mode))
            {
                return textures[(int)type][mode];
            }
            return null;
        }

        /// <summary>
        /// Checks if a texture of a given type has already been created.
        /// </summary>
        /// <param name="type">Type of texture as an integer (0 for Image, 1 for Measure).</param>
        /// <param name="mode">Corresponding options enum (sl.VIEW if Image type, sl.MEASURE if Measure type) as an integer.</param>
        /// <returns>True if the texture is available.</returns>
        private bool HasTexture(int type, int mode)
        {
            if (cameraReady) //Texture can't exist if the ZED hasn't been initialized yet.
            {
                return textures.ContainsKey((int)type) && textures[type].ContainsKey((int)mode);
            }
            return false;
        }

        /// <summary>
        /// Returns the current camera FPS. This is limited primarily by resolution but can also be lower due to
        /// setting a lower desired resolution in Init() or from USB connection/bandwidth issues.
        /// </summary>
        /// <returns>The current fps</returns>
        public float GetCameraFPS()
        {
            return dllz_get_camera_fps(CameraID);
        }

        /// <summary>
        /// Reports if the camera has been successfully opened.
        /// </summary>
        /// <returns> Returns true if the ZED is already setup, otherwise false.</returns>
        public bool IsOpened()
        {
            return dllz_is_opened(CameraID);
        }

        public CalibrationParameters GetCalibrationParameters(bool raw = false)
        {

            IntPtr p = dllz_get_calibration_parameters(CameraID, raw);

            if (p == IntPtr.Zero)
            {
                return new CalibrationParameters();
            }
            CalibrationParameters parameters = (CalibrationParameters)Marshal.PtrToStructure(p, typeof(CalibrationParameters));

            if (raw)
                calibrationParametersRaw = parameters;
            else
                calibrationParametersRectified = parameters;


            return parameters;

        }

        public SensorsConfiguration GetInternalSensorsConfiguration()
        {
            IntPtr p = dllz_get_sensors_configuration(CameraID);

            if (p == IntPtr.Zero)
            {
                return new SensorsConfiguration();
            }
            SensorsConfiguration configuration = (SensorsConfiguration)Marshal.PtrToStructure(p, typeof(SensorsConfiguration));

            return configuration;
        }

        /// <summary>
        /// Gets the ZED camera model (ZED or ZED Mini).
        /// </summary>
        /// <returns>Model of the ZED as sl.MODEL.</returns>
        public sl.MODEL GetCameraModel()
        {
            return (sl.MODEL)dllz_get_camera_model(CameraID);
        }

        /// <summary>
        /// Gets the ZED's camera firmware version.
        /// </summary>
        /// <returns>Firmware version.</returns>
        public int GetCameraFirmwareVersion()
        {
            return dllz_get_camera_firmware(CameraID);
        }

        /// <summary>
        /// Gets the ZED's sensors firmware version.
        /// </summary>
        /// <returns>Firmware version.</returns>
        public int GetSensorsFirmwareVersion()
        {
            return dllz_get_sensors_firmware(CameraID);
        }

        /// <summary>
        /// Gets the ZED's serial number.
        /// </summary>
        /// <returns>Serial number</returns>
        public int GetZEDSerialNumber()
        {
            return dllz_get_zed_serial(CameraID);
        }

        /// <summary>
        /// Returns the ZED's vertical field of view in radians.
        /// </summary>
        /// <returns>Vertical field of view.</returns>
        public float GetFOV()
        {
            return GetCalibrationParameters(false).leftCam.vFOV * Mathf.Deg2Rad;
        }


        /// <summary>
        /// Computes textures from the ZED. The new textures will not be displayed until an event is sent to the render thread.
        /// This event is called from UpdateTextures().
        /// </summary>
        public void RetrieveTextures()
        {
            dllz_retrieve_textures(CameraID);
        }

        /// <summary>
        /// Swaps textures safely between the acquisition and rendering threads.
        /// </summary>
        public void SwapTextures()
        {
            dllz_swap_textures(CameraID);
        }

        /// <summary>
        /// Timestamp of the images used the last time the ZED wrapper updated textures.
        /// </summary>
        /// <returns></returns>
        public ulong GetImagesTimeStamp()
        {
            return dllz_get_updated_textures_timestamp(CameraID);
        }

        /// <summary>
        /// Perform a new self calibration process.
        /// In some cases, due to temperature changes or strong vibrations, the stereo calibration becomes less accurate.
        /// Use this function to update the self-calibration data and get more reliable depth values.
        /// <remarks>The self calibration will occur at the next \ref grab() call.</remarks>
        /// New values will then be available in \ref getCameraInformation(), be sure to get them to still have consistent 2D <-> 3D conversion.
        /// </summary>
        /// <param name="cameraID"></param>
        /// <returns></returns>
        public void UpdateSelfCalibration()
        {
            dllz_update_self_calibration(CameraID);
        }

        /// <summary>
        /// Gets the number of frames dropped since Grab() was called for the first time.
        /// Based on camera timestamps and an FPS comparison.
        /// </summary><remarks>Similar to the Frame Drop display in the ZED Explorer app.</remarks>
        /// <returns>Frames dropped since first Grab() call.</returns>
        public uint GetFrameDroppedCount()
        {
            return dllz_get_frame_dropped_count(CameraID);
        }

        /// <summary>
        /// Gets the percentage of frames dropped since Grab() was called for the first time.
        /// </summary>
        /// <returns>Percentage of frames dropped.</returns>
        public float GetFrameDroppedPercent()
        {
            return dllz_get_frame_dropped_percent(CameraID);
        }

        /// <summary>
        /// Returns the current status of positional tracking module.
        /// </summary>
        /// <returns> The current status of positional tracking module. </returns>
        public PositionalTrackingStatus GetPositionalTrackingStatus()
        {
            IntPtr p = dllz_get_positional_tracking_status(CameraID);
            if (p == IntPtr.Zero)
            {
                return new PositionalTrackingStatus();
            }

            PositionalTrackingStatus positionalTrackingStatus = (PositionalTrackingStatus)Marshal.PtrToStructure(p, typeof(PositionalTrackingStatus));
            return positionalTrackingStatus;
        }

        /// <summary>
        /// Gets the position of the camera and the current state of the ZED Tracking.
        /// </summary>
        /// <param name="rotation">Quaternion filled with the current rotation of the camera depending on its reference frame.</param>
        /// <param name="position">Vector filled with the current position of the camera depending on its reference frame.</param>
        /// <param name="referenceType">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public POSITIONAL_TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 position, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (POSITIONAL_TRACKING_STATE)dllz_get_position(CameraID, ref rotation, ref position, (int)referenceType);
        }

        /// <summary>
        /// Gets the current position of the camera and state of the tracking, with an optional offset to the tracking frame.
        /// </summary>
        /// <param name="rotation">Quaternion filled with the current rotation of the camera depending on its reference frame.</param>
        /// <param name="position">Vector filled with the current position of the camera depending on its reference frame.</param>
        /// <param name="targetQuaternion">Rotational offset applied to the tracking frame.</param>
        /// <param name="targetTranslation">Positional offset applied to the tracking frame.</param>
        /// <param name="referenceFrame">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public POSITIONAL_TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation, REFERENCE_FRAME referenceFrame = REFERENCE_FRAME.WORLD)
        {
            return (POSITIONAL_TRACKING_STATE)dllz_get_position_at_target_frame(CameraID, ref rotation, ref translation, ref targetQuaternion, ref targetTranslation, (int)referenceFrame);
        }


        /// <summary>
        /// Gets the current position of the camera and state of the tracking, with a defined tracking frame.
        /// A tracking frame defines what part of the ZED is its center for tracking purposes. See ZEDCommon.TRACKING_FRAME.
        /// </summary>
        /// <param name="rotation">Quaternion filled with the current rotation of the camera depending on its reference frame.</param>
        /// <param name="position">Vector filled with the current position of the camera depending on its reference frame.</param>
        /// <param name="trackingFrame">Center of the ZED for tracking purposes (left eye, center, right eye).</param>
        /// <param name="referenceFrame">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public POSITIONAL_TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 translation, TRACKING_FRAME trackingFrame, REFERENCE_FRAME referenceFrame = REFERENCE_FRAME.WORLD)
        {
            Quaternion rotationOffset = Quaternion.identity;
            Vector3 positionOffset = Vector3.zero;
            switch (trackingFrame) //Add offsets to account for different tracking frames.
            {
                case sl.TRACKING_FRAME.LEFT_EYE:
                    positionOffset = new Vector3(0, 0, 0);
                    break;
                case sl.TRACKING_FRAME.RIGHT_EYE:
                    positionOffset = new Vector3(Baseline, 0, 0);
                    break;
                case sl.TRACKING_FRAME.CENTER_EYE:
                    positionOffset = new Vector3(Baseline / 2.0f, 0, 0);
                    break;
            }

            return (POSITIONAL_TRACKING_STATE)dllz_get_position_at_target_frame(CameraID, ref rotation, ref translation, ref rotationOffset, ref positionOffset, (int)referenceFrame);
        }

        /// <summary>
        /// Gets the current position of the camera and state of the tracking, filling a Pose struct useful for AR pass-through.
        /// </summary>
        /// <param name="pose">Current pose.</param>
        /// <param name="referenceType">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public POSITIONAL_TRACKING_STATE GetPosition(ref Pose pose, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (POSITIONAL_TRACKING_STATE)dllz_get_position_data(CameraID, ref pose, (int)referenceType);
        }


        /// <summary>
        /// Sets a prior to the IMU orientation (only for ZED-M).
        /// Prior must come from a external IMU, such as the HMD orientation and should be in a time frame
        /// that's as close as possible to the camera.
        /// </summary>
        /// <returns>Error code status.</returns>
        /// <param name="rotation">Prior rotation.</param>
        public ERROR_CODE SetIMUOrientationPrior(ref Quaternion rotation)
        {
            sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            trackingStatus = (sl.ERROR_CODE)dllz_set_imu_prior_orientation(CameraID, rotation);
            return trackingStatus;
        }
        /// <summary>
        /// Gets the rotation given by the ZED-M/ZED2 IMU. Return an error if using ZED (v1) which does not contains internal sensors
        /// </summary>
        /// <param name="rotation">Rotation from the IMU.</param>
        /// <param name="referenceTime">time reference.</param>
        /// <returns>Error code status.</returns>
        public ERROR_CODE GetInternalIMUOrientation(ref Quaternion rotation, TIME_REFERENCE referenceTime = TIME_REFERENCE.IMAGE)
        {
            sl.ERROR_CODE err = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            err = (sl.ERROR_CODE)dllz_get_internal_imu_orientation(CameraID, ref rotation, (int)referenceTime);
            return err;
        }

        /// <summary>
        /// Gets the full Sensor data from the ZED-M or ZED2 . Return an error if using ZED (v1) which does not contains internal sensors
        /// </summary>
        /// <param name="data">Sensor Data.</param>
        /// <param name="referenceTime">Time reference.</param>
        /// <returns>Error code status.</returns>
        public ERROR_CODE GetInternalSensorsData(ref SensorsData data, TIME_REFERENCE referenceTime = TIME_REFERENCE.IMAGE)
        {
            sl.ERROR_CODE err = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
            err = (sl.ERROR_CODE)dllz_get_internal_sensors_data(CameraID, ref data, (int)referenceTime);
            return err;
        }

        /// <summary>
        /// Defines a region of interest to focus on for all the SDK, discarding other parts.
        /// </summary>
        /// <param name="roiMask"> The Mat defining the requested region of interest, pixels lower than 127 will be discarded from all modules: depth, positional tracking, etc.
        /// If empty, set all pixels as valid. The mask can be either at lower or higher resolution than the current images.</param>
        /// <param name="module"> Apply the ROI to a list of SDK module, all by default. Must of size sl.MODULE.LAST. 
        /// The Mat defining the requested region of interest, pixels lower than 127 will be discarded from all modules: depth, positional tracking, etc.
        /// If empty, set all pixels as valid. The mask can be either at lower or higher resolution than the current images.
        /// </param>
        /// <returns>An sl.ERROR_CODE if something went wrong.</returns>
        public ERROR_CODE SetRegionOfInterest(sl.ZEDMat roiMask, bool[] module)
        {
            if (module.Length != (int)MODULE.LAST) return sl.ERROR_CODE.FAILURE;

            return (sl.ERROR_CODE)dllz_set_region_of_interest(CameraID, roiMask.GetPtr(), module);
        }

        /// <summary>
        /// Get the previously set or computed region of interest.
        /// </summary>
        /// <param name="roiMask">The \ref Mat returned</param>
        /// <param name="resolution">The optional size of the returned mask</param>
        /// <param name="module"> Specifies the module from which the ROI is to be obtained. </param>
        /// <returns>An sl.ERROR_CODE if something went wrong.</returns>
        public ERROR_CODE GetRegionOfInterest(sl.ZEDMat roiMask, sl.Resolution resolution = new sl.Resolution(), MODULE module = MODULE.ALL)
        {
            return (sl.ERROR_CODE)dllz_get_region_of_interest(CameraID, roiMask.MatPtr, (int)resolution.width, (int)resolution.height, module);
        }

        /// <summary>
        /// Start the auto detection of a region of interest to focus on for all the SDK, discarding other parts.
        /// This detection is based on the general motion of the camera combined with the motion in the scene.
        /// The camera must move for this process, an internal motion detector is used, based on the Positional Tracking module.
        /// It requires a few hundreds frames of motion to compute the mask.
        ///  \note This module is expecting a static portion, typically a fairly close vehicle hood at the bottom of the image.
        /// This module may not work correctly or detect incorrect background area, especially with slow motion, if there's no static element.
        /// This module work asynchronously, the status can be obtained using \ref GetRegionOfInterestAutoDetectionStatus(), the result is either auto applied,
        /// or can be retrieve using \ref GetRegionOfInterest function.
        /// </summary>
        /// <param name="roiParams"></param>
        /// <returns>An sl.ERROR_CODE if something went wrong.</returns>
        public ERROR_CODE StartRegionOfInterestAutoDetection(RegionOfInterestParameters roiParams)
        {
            return (sl.ERROR_CODE)dllz_start_region_of_interest_auto_detection(CameraID, ref roiParams);
        }

        /// <summary>
        ///  Return the status of the automatic Region of Interest Detection.
        ///  The automatic Region of Interest Detection is enabled by using \ref StartRegionOfInterestAutoDetection
        /// </summary>
        /// <returns>An sl.ERROR_CODE if something went wrong.</returns>
        public REGION_OF_INTEREST_AUTO_DETECTION_STATE GetRegionOfInterestAutoDetectionStatus()
        {
            return (REGION_OF_INTEREST_AUTO_DETECTION_STATE)dllz_get_region_of_interest_auto_detection_status(CameraID);
        }

        /// <summary>
        /// Converts a float array to a matrix.
        /// </summary>
        /// <param name="m">Matrix to be filled.</param>
        /// <param name="f">Float array to be turned into a matrix.</param>
        static public void Float2Matrix(ref Matrix4x4 m, float[] f)
        {
            if (f == null) return;
            if (f.Length != 16) return;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    m[i, j] = f[i * 4 + j];
                }
            }
        }

        /// <summary>
        /// Test if the video setting is supported by the camera
        /// setting : The video setting to test
        /// true if the \ref SL_VIDEO_SETTINGS is supported by the camera, false otherwise
        /// </summary>
        public bool IsCameraSettingSupported(CAMERA_SETTINGS setting)
        {
            return dllz_is_video_setting_supported(CameraID, (int)setting);
        }

        /// <summary>
        /// Sets a value in the ZED's camera settings.
        /// </summary>
        /// <param name="settings">Setting to be changed (brightness, contrast, gain, exposure, etc.)</param>
        /// <param name="value">New value.</param>
        /// <param name="usedefault">True to set the settings to their default values.</param>
        public void SetCameraSettings(CAMERA_SETTINGS settings, int value)
        {
            AssertCameraIsReady();
            //cameraSettingsManager.SetCameraSettings(CameraID, settings, value);
            dllz_set_video_settings(CameraID, (int)settings, value);
        }

        /// <summary>
        /// Gets the value of a given setting from the ZED camera.
        /// </summary>
        /// <param name="settings">Setting to be retrieved (brightness, contrast, gain, exposure, etc.)</param>
        public sl.ERROR_CODE GetCameraSettings(CAMERA_SETTINGS settings, ref int value)
        {
            AssertCameraIsReady();
            return (sl.ERROR_CODE)dllz_get_video_settings(CameraID, (int)settings, ref value);
            //return cameraSettingsManager.GetCameraSettings(CameraID, settings);
        }

        /// <summary>
        /// Overloaded function for CAMERA_SETTINGS.AEC_AGC_ROI (requires iRect as input)
        /// </summary>
        /// <param name="settings"> Must be set to CAMERA_SETTINGS.AEC_AGC_ROI. Otherwise will return -1.</param>
        /// <param name="side"> defines left=0 or right=1 or both=2 sensor target</param>
        /// <param name="roi">the roi defined as a sl.Rect</param>
        /// <param name="reset">Defines if the target must be reset to full sensor</param>
        /// <returns></returns>
        public int SetCameraSettings(CAMERA_SETTINGS settings, int side, sl.Rect roi, bool reset)
        {
            AssertCameraIsReady();
            if (settings == CAMERA_SETTINGS.AEC_AGC_ROI)
                return dllz_set_roi_for_aec_agc(CameraID, side, roi, reset);
            else
                return -1;
        }

        /// <summary>
        /// Overloaded function for CAMERA_SETTINGS.AEC_AGC_ROI (requires iRect as input)
        /// </summary>
        /// <param name="settings"> Must be set to CAMERA_SETTINGS.AEC_AGC_ROI. Otherwise will return -1.</param>
        /// <param name="side"> defines left=0 or right=1 or both=2 sensor target.</param>
        /// <param name="roi"> Roi that will be filled.</param>
        /// <returns></returns>
        public int GetCameraSettings(CAMERA_SETTINGS settings, int side, ref sl.Rect roi)
        {
            AssertCameraIsReady();
            if (settings == CAMERA_SETTINGS.AEC_AGC_ROI)
                return dllz_get_roi_for_aec_agc(CameraID, side, ref roi);
            else
                return -1;
        }

        /// <summary>
        /// Reset camera settings to default
        /// </summary>
        public void ResetCameraSettings()
        {
            AssertCameraIsReady();
            //cameraSettingsManager.ResetCameraSettings(this);

            foreach (sl.CAMERA_SETTINGS setting_ in Enum.GetValues(typeof(sl.CAMERA_SETTINGS)))
                SetCameraSettings(setting_, -1);
        }

        /// <summary>
        /// Loads camera settings (brightness, contrast, hue, saturation, gain, exposure) from a file in the
        /// project's root directory.
        /// </summary>
        /// <param name="path">Filename.</param>
        public void LoadCameraSettings(string path)
        {
            cameraSettingsManager.LoadCameraSettings(this, path);
        }

        /// <summary>
        /// Save the camera settings (brightness, contrast, hue, saturation, gain, exposure) to a file
        /// relative to the project's root directory.
        /// </summary>
        /// <param name="path">Filename.</param>
        public void SaveCameraSettings(string path)
        {
            cameraSettingsManager.SaveCameraSettings(path);
        }

        /// <summary>
        /// Retrieves camera settings from the ZED camera and loads them into a CameraSettings instance
        /// handled by ZEDCameraSettingsManager.
        /// </summary>
        public void RetrieveCameraSettings()
        {
            cameraSettingsManager.RetrieveSettingsCamera(this);
        }

        /// <summary>
        /// Returns if the camera's exposure mode is set to automatic.
        /// </summary>
        /// <returns><c>True</c> if automatic, <c>false</c> if manual.</returns>
        public bool GetExposureUpdateType()
        {
            return cameraSettingsManager.auto;
        }

        /// <summary>
        /// Returns if the camera's white balance  is set to automatic.
        /// </summary>
        /// <returns><c>True</c> if automatic, <c>false</c> if manual.</returns>
        public bool GetWhiteBalanceUpdateType()
        {
            return cameraSettingsManager.whiteBalanceAuto;
        }

        /// <summary>
        /// Applies all the settings registered in the ZEDCameraSettingsManager instance to the actual ZED camera.
        /// </summary>
        public void SetCameraSettings()
        {
            cameraSettingsManager.SetSettings(this);
        }

        /// <summary>
        /// Gets the version of the currently installed ZED SDK.
        /// </summary>
        /// <returns>ZED SDK version as a string in the format MAJOR.MINOR.PATCH.</returns>
        public static string GetSDKVersion()
        {
            return PtrToStringUtf8(dllz_get_sdk_version());
        }


        /// <summary>
        /// Gets the version of the currently installed ZED SDK.
        /// </summary>
        /// <returns>ZED SDK version as a string in the format MAJOR.MINOR.PATCH.</returns>
        public static void GetSDKVersion(ref int major, ref int minor, ref int patch)
        {
            string sdkVersion = PtrToStringUtf8(dllz_get_sdk_version());

            string[] version = sdkVersion.Split('.');

            if (version.Length == 3)
            {
                int.TryParse(version[0], out major);
                int.TryParse(version[1], out minor);
                int.TryParse(version[2], out patch);
            }
        }

        /// <summary>
        /// Change the coordinate system of a transform matrix.
        /// </summary>
        /// <param name="rotation">[In, Out] : rotation to transform</param>
        /// <param name="translation"> [In, Out] : translation to transform</param>
        /// <param name="coordinateSystemSrc"> The current coordinate system of the translation/rotation</param>
        /// <param name="coordinateSystemDest"> The destination coordinate system for the translation/rotation.</param>
        /// <returns> SUCCESS if everything went well, FAILURE otherwise.</returns>
        public static sl.ERROR_CODE ConvertCoordinateSystem(ref Quaternion rotation, ref Vector3 translation, sl.COORDINATE_SYSTEM coordinateSystemSrc, sl.COORDINATE_SYSTEM coordinateSystemDest)
        {
            return (sl.ERROR_CODE)dllz_convert_coordinate_system(ref rotation, ref translation, coordinateSystemSrc, coordinateSystemDest);
        }

        /// <summary>
        /// List all the connected devices with their associated information.
        /// This function lists all the cameras available and provides their serial number, models and other information.
        /// </summary>
        /// <returns>The device properties for each connected camera</returns>
        public static sl.DeviceProperties[] GetDeviceList(out int nbDevices)
        {
            sl.DeviceProperties[] deviceList = new sl.DeviceProperties[(int)Constant.MAX_CAMERA_PLUGIN];
            dllz_get_device_list(deviceList, out nbDevices);

            return deviceList;
        }

        /// <summary>
        /// List all the connected devices with their associated information.
        /// This function lists all the cameras available and provides their serial number, models and other information.
        /// </summary>
        /// <returns>The device properties for each connected camera</returns>
        public static sl.StreamingProperties[] GetStreamingDeviceList(out int nbDevices)
        {
            sl.StreamingProperties[] streamingDeviceList = new sl.StreamingProperties[(int)Constant.MAX_CAMERA_PLUGIN];
            dllz_get_streaming_device_list(streamingDeviceList, out nbDevices);

            return streamingDeviceList;
        }

        /// <summary>
        /// Performs an hardware reset of the ZED 2/ZED 2i.
        /// This method only works for ZED 2, ZED 2i, and newer camera models.
        /// This method will invalidate any sl.Camera object, since the device is rebooting.
        /// Under Windows it is not possible to get exclusive access to HID devices, hence calling this method while the camera is opened by another process will cause it to freeze for a few seconds while the device is rebooting.
        /// </summary>
        /// <param name="serialNumber">Serial number of the camera to reset, or 0 to reset the first camera detected.</param>
        /// <param name="fullReboot">Perform a full reboot (sensors and video modules) if true, otherwise only the video module will be rebooted.</param>
        /// <returns>sl.ERROR_CODE.SUCCESS if everything went fine. sl.ERROR_CODE.CAMERA_NOT_DETECTED if no camera was detected. sl.ERROR_CODE.FAILURE otherwise.</returns>
        public static sl.ERROR_CODE Reboot(int serialNumber, bool fullReboot = true)
        {
            return (sl.ERROR_CODE)dllz_reboot(serialNumber, fullReboot);
        }

        /// <summary>
        /// Checks if the camera has been initialized and the plugin has been loaded. Throws exceptions otherwise.
        /// </summary>
        private void AssertCameraIsReady()
        {
            if (!cameraReady)
                throw new Exception("ZED camera is not connected or Init() was not called.");

            if (!pluginIsReady)
                throw new Exception("Could not resolve ZED plugin dependencies.");

        }

        /// <summary>
        /// Deploys an event that causes the textures to be updated with images received from the ZED.
        /// Should be called after RetrieveTextures() so there are new images available.
        /// </summary>
        public void UpdateTextures()
        {
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }


        ///////////////////////////// SINGLE PIXEL UTILITY FUNCTIONS ////////////////////////////////

        /// <summary>
        /// Gets the current depth value of a pixel in the UNITS specified when the camera was started with Init().
        /// May result in errors if the ZED image does not fill the whole screen.
        /// <param name="position">The pixel's screen space coordinates as a Vector3.
        /// The Z component is unused - designed to take input from Input.mousePosition.</param>
        /// <returns>Depth value as a float.</returns>
        /// </summary>
        public float GetDepthValue(Vector3 pixel)
        {
            if (!cameraReady)
            {
                return -1;
            }

            float posX = (float)ImageWidth * (float)((float)pixel.x / (float)Screen.width);
            float posY = ImageHeight * (1 - (float)pixel.y / (float)Screen.height);

            posX = Mathf.Clamp(posX, 0, ImageWidth);
            posY = Mathf.Clamp(posY, 0, ImageHeight);
            float d = dllz_get_depth_value(CameraID, (uint)posX, (uint)posY);
            return d;
        }

        /// <summary>
        /// Gets the current depth value of a pixel in the UNITS specified when the camera was started with Init().
        /// <param name="position">The pixel's coordinates of the ZED Image as a Vector2.
        /// <returns>Depth value as a float.</returns>
        /// </summary>
        public float GetDepthValue(Vector2 pixel)
        {
            if (!cameraReady)
            {
                return -1;
            }

            float d = dllz_get_depth_value(CameraID, (uint)pixel.x, (uint)pixel.y);
            return d;
        }

        /// <summary>
        /// Gets the current Euclidean distance (sqrt(x²+y²+z²)) of the targeted pixel of the screen to the camera.
        /// May result in errors if the ZED image does not fill the whole screen.
        /// <param name="pixel">The pixel's screen space coordinates as a Vector3.
        /// The Z component is unused - designed to take input from Input.mousePosition.</param>
        /// <returns>Distance as a float.</returns>
        /// </summary>
        public float GetDistanceValue(Vector3 pixel)
        {
            if (!cameraReady) //Do nothing if the ZED isn't initialized.
            {
                return -1;
            }
            float posX = ImageWidth * (float)pixel.x / (float)Screen.width;
            float posY = ImageHeight * (1 - (float)pixel.y / (float)Screen.height);
            posX = Mathf.Clamp(posX, 0, ImageWidth);
            posY = Mathf.Clamp(posY, 0, ImageHeight);

            return dllz_get_distance_value(CameraID, (uint)posX, (uint)posY);
        }

        /// <summary>
        /// Gets the position of a camera-space pixel relative to the camera frame.
        /// <param name="pixel">The pixel's screen space coordinates as a Vector3.
        /// The Z component is unused - designed to take input from Input.mousePosition.</param>
        /// <param name="xyz">Position relative to the camera.</param>
        /// <returns>True if successful.</returns>
        /// </summary>
        public bool GetXYZValue(Vector3 pixel, out Vector4 xyz)
        {
            if (!cameraReady) //Do nothing if the ZED isn't initialized.
            {
                xyz = Vector3.zero;
                return false;
            }

            float posX = (float)ImageWidth * (float)((float)pixel.x / (float)Screen.width);
            float posY = ImageHeight * (1 - (float)pixel.y / (float)Screen.height);
            posX = Mathf.Clamp(posX, 0, ImageWidth);
            posY = Mathf.Clamp(posY, 0, ImageHeight);
            bool r = dllz_get_xyz_value(CameraID, (uint)posX, (uint)posY, out xyz);
            return r;
        }


        /// <summary>
        /// Gets the normal of a camera-space pixel. The normal is relative to the camera.
        /// Use cam.worldToCameraMatrix.inverse to transform it to world space.
        /// Note that ZEDSupportFunctions contains high-level versions of this function that are easier to use.
        /// <param name="pixel">The pixel's screen space coordinates as a Vector3.
        /// The Z component is unused - designed to take input from Input.mousePosition.</param>
        /// <param name="normal">Normal value of the pixel as a Vector4.</param>
        /// <returns>True if successful.</returns>
        /// </summary>
        public bool GetNormalValue(Vector3 pixel, out Vector4 normal)
        {
            if (!cameraReady) //Do nothing if the ZED isn't initialized.
            {
                normal = Vector3.zero;
                return false;
            }

            float posX = (float)ImageWidth * (float)((float)pixel.x / (float)Screen.width);
            float posY = ImageHeight * (1 - (float)pixel.y / (float)Screen.height);

            posX = Mathf.Clamp(posX, 0, ImageWidth);
            posY = Mathf.Clamp(posY, 0, ImageHeight);

            bool r = dllz_get_normal_value(CameraID, (uint)posX, (uint)posY, out normal);
            return r;
        }


        /// <summary>
        /// Gets the current range of perceived depth.
        /// </summary>
        /// <param name="min">Minimum depth detected (in selected sl.UNIT)</param>
        /// <param name="max">Maximum depth detected (in selected sl.UNIT)</param>
        /// <returns>SUCCESS if values have been extracted. Other ERROR_CODE otherwise.</returns>
        public sl.ERROR_CODE GetCurrentMixMaxDepth(ref float min, ref float max)
        {
            return (sl.ERROR_CODE)dllz_get_current_min_max_depth(CameraID, ref min, ref max);
        }

        /// <summary>
        /// Initializes and begins the spatial mapping processes.
        /// </summary>
        /// <param name="resolution_meter">Spatial mapping resolution in meters.</param>
        /// <param name="max_range_meter">Maximum scanning range in meters.</param>
        /// <param name="saveTexture">True to scan surface textures in addition to geometry.</param>
        /// <returns></returns>
        public sl.ERROR_CODE EnableSpatialMapping(ref SpatialMappingParameters spatialMappingParameters)
        {
            sl.ERROR_CODE spatialMappingStatus = ERROR_CODE.FAILURE;
            //lock (grabLock)
            {
                spatialMappingStatus = (sl.ERROR_CODE)dllz_enable_spatial_mapping(CameraID, ref spatialMappingParameters);
            }
            return spatialMappingStatus;
        }

        /// <summary>
        /// Disables the Spatial Mapping process.
        /// </summary>
        public void DisableSpatialMapping()
        {
            lock (grabLock)
            {
                dllz_disable_spatial_mapping(CameraID);
            }
        }

        /// <summary>
        /// Gets the current position of the camera and state of the tracking, with an optional offset to the tracking frame.
        /// </summary>
        /// <returns> true if the tracking module is enabled</returns>
        public bool IsPositionalTrackingEnabled()
        {
            return dllz_is_positional_tracking_enabled(CameraID);
        }

        /// <summary>
        /// Updates the internal version of the mesh and returns the sizes of the meshes.
        /// </summary>
        /// <param name="nbVerticesInSubmeshes">Array of the number of vertices in each submesh.</param>
        /// <param name="nbTrianglesInSubmeshes">Array of the number of triangles in each submesh.</param>
        /// <param name="nbSubmeshes">Number of submeshes.</param>
        /// <param name="updatedIndices">List of all submeshes updated since the last update.</param>
        /// <param name="nbVertices">Total number of updated vertices in all submeshes.</param>
        /// <param name="nbTriangles">Total number of updated triangles in all submeshes.</param>
        /// <param name="nbSubmeshMax">Maximum number of submeshes that can be handled.</param>
        /// <returns>Error code indicating if the update was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE UpdateMesh(int[] nbVerticesInSubmeshes, int[] nbTrianglesInSubmeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmeshMax)
        {
            sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
            err = (sl.ERROR_CODE)dllz_update_mesh(CameraID, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);

            return err;
        }

        /// <summary>
        /// Retrieves all chunks of the generated mesh. Call UpdateMesh() before calling this.
        /// Vertex and triangle arrays must be at least of the sizes returned by UpdateMesh (nbVertices and nbTriangles).
        /// </summary>
        /// <param name="vertices">Vertices of the mesh.</param>
        /// <param name="triangles">Triangles, formatted as the index of each triangle's three vertices in the vertices array.</param>
        /// <param name="nbSubmeshMax">Maximum number of submeshes that can be handled.</param>
        /// <returns>Error code indicating if the retrieval was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE RetrieveMesh(Vector3[] vertices, int[] triangles, int nbSubmeshMax, Vector2[] uvs, IntPtr textures)
        {
            return (sl.ERROR_CODE)dllz_retrieve_mesh(CameraID, vertices, triangles, nbSubmeshMax, uvs, textures);
        }

        /// <summary>
        /// Saves the current area learning file. The file will contain spatial memory data generated by the tracking.
        /// </summary>
        /// <param name="path"></param>
        /// <param name=""></param>
        public ERROR_CODE SaveAreaMap(string areaFilePath)
        {
            return (ERROR_CODE)dllz_save_area_map(CameraID, new System.Text.StringBuilder(areaFilePath, areaFilePath.Length));
        }

        /// <summary>
        /// Updates the fused point cloud (if spatial map type was FUSED_POINT_CLOUD)
        /// </summary>
        /// <returns>Error code indicating if the update was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE UpdateFusedPointCloud(ref int nbVertices)
        {
            sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
            err = (sl.ERROR_CODE)dllz_update_fused_point_cloud(CameraID, ref nbVertices);
            return err;
        }

        /// <summary>
        /// Retrieves all points of the fused point cloud. Call UpdateFusedPointCloud() before calling this.
        /// Vertex arrays must be at least of the sizes returned by UpdateFusedPointCloud
        /// </summary>
        /// <param name="vertices">Points of the fused point cloud.</param>
        /// <returns>Error code indicating if the retrieval was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE RetrieveFusedPointCloud(Vector4[] vertices)
        {
            return (sl.ERROR_CODE)dllz_retrieve_fused_point_cloud(CameraID, vertices);
        }

        /// <summary>
        /// Starts the mesh generation process in a thread that doesn't block the spatial mapping process.
        /// ZEDSpatialMappingHelper calls this each time it has finished applying the last mesh update.
        /// </summary>
        public void RequestMesh()
        {
            dllz_request_mesh_async(CameraID);
        }

        /// <summary>
        /// Sets the pause state of the data integration mechanism for the ZED's spatial mapping.
        /// </summary>
        /// <param name="status">If true, the integration is paused. If false, the spatial mapping is resumed.</param>
        public void PauseSpatialMapping(bool status)
        {
            dllz_pause_spatial_mapping(CameraID, status);
        }

        /// <summary>
        /// Returns the mesh generation status. Useful for knowing when to update and retrieve the mesh.
        /// </summary>
        public sl.ERROR_CODE GetMeshRequestStatus()
        {
            return (sl.ERROR_CODE)dllz_get_mesh_request_status_async(CameraID);
        }

        /// <summary>
        /// Saves the scanned mesh in a specific file format.
        /// </summary>
        /// <param name="filename">Path and filename of the mesh.</param>
        /// <param name="format">File format (extension). Can be .obj, .ply or .bin.</param>
        public bool SaveMesh(string filename, MESH_FILE_FORMAT format)
        {
            return dllz_save_mesh(CameraID, filename, format);
        }

        /// <summary>
        /// Saves the scanned point cloud in a specific file format.
        /// </summary>
        /// <param name="filename">Path and filename of the point cloud.</param>
        /// <param name="format">File format (extension). Can be .obj, .ply or .bin.</param>
        public bool SavePointCloud(string filename, MESH_FILE_FORMAT format)
        {
            return dllz_save_point_cloud(CameraID, filename, format);
        }

        /// <summary>
        /// Loads a saved mesh file. ZEDSpatialMapping then configures itself as if the loaded mesh was just scanned.
        /// </summary>
        /// <param name="filename">Path and filename of the mesh. Should include the extension (.obj, .ply or .bin).</param>
        /// <param name="nbVerticesInSubmeshes">Array of the number of vertices in each submesh.</param>
        /// <param name="nbTrianglesInSubmeshes">Array of the number of triangles in each submesh.</param>
        /// <param name="nbSubmeshes">Number of submeshes.</param>
        /// <param name="updatedIndices">List of all submeshes updated since the last update.</param>
        /// <param name="nbVertices">Total number of updated vertices in all submeshes.</param>
        /// <param name="nbTriangles">Total number of updated triangles in all submeshes.</param>
        /// <param name="nbSubmeshMax">Maximum number of submeshes that can be handled.</param>
        /// <param name="textureSize">Array containing the sizes of all the textures (width, height) if applicable.</param>
        public bool LoadMesh(string filename, int[] nbVerticesInSubmeshes, int[] nbTrianglesInSubmeshes, ref int nbSubmeshes, int[] updatedIndices,
            ref int nbVertices, ref int nbTriangles, int nbSubmeshMax, int[] textureSize = null)
        {
            return dllz_load_mesh(CameraID, filename, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices,
                ref nbTriangles, nbSubmeshMax, textureSize);
        }

        /// <summary>
        /// Filters a mesh to remove triangles while still preserving its overall shape (though less accurate).
        /// </summary>
        /// <param name="filterParameters">Filter level. Higher settings remove more triangles.</param>
        /// <param name="nbVerticesInSubmeshes">Array of the number of vertices in each submesh.</param>
        /// <param name="nbTrianglesInSubmeshes">Array of the number of triangles in each submesh.</param>
        /// <param name="nbSubmeshes">Number of submeshes.</param>
        /// <param name="updatedIndices">List of all submeshes updated since the last update.</param>
        /// <param name="nbVertices">Total number of updated vertices in all submeshes.</param>
        /// <param name="nbTriangles">Total number of updated triangles in all submeshes.</param>
        /// <param name="nbSubmeshMax">Maximum number of submeshes that can be handled.</param>
        public bool FilterMesh(FILTER filterParameters, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmeshMax)
        {
            return dllz_filter_mesh(CameraID, filterParameters, nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);
        }

        /// <summary>
        /// Applies the scanned texture onto the internal scanned mesh.
        /// You will need to call RetrieveMesh() with uvs and textures to get the result into Unity.
        /// </summary>
        /// <param name="nbVerticesInSubmeshes">Array of the number of vertices in each submesh.</param>
        /// <param name="nbTrianglesInSubmeshes">Array of the number of triangles in each submesh.</param>
        /// <param name="nbSubmeshes">Number of submeshes.</param>
        /// <param name="updatedIndices">List of all submeshes updated since the last update.</param>
        /// <param name="nbVertices">Total number of updated vertices in all submeshes.</param>
        /// <param name="nbTriangles">Total number of updated triangles in all submeshes.</param>
        /// <param name="textureSize"> Vector containing the size of all the texture (width, height). </param>
        /// <param name="nbSubmeshMax">Maximum number of submeshes that can be handled.</param>
        /// <returns></returns>
        public bool ApplyTexture(int[] nbVerticesInSubmeshes, int[] nbTrianglesInSubmeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int[] textureSize, int nbSubmeshMax)
        {
            return dllz_apply_texture(CameraID, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, textureSize, nbSubmeshMax);
        }

        /// <summary>
        /// Gets the current state of spatial mapping.
        /// </summary>
        /// <returns></returns>
        public SPATIAL_MAPPING_STATE GetSpatialMappingState()
        {
            return (sl.SPATIAL_MAPPING_STATE)dllz_get_spatial_mapping_state(CameraID);
        }

        /// <summary>
        /// Gets a vector pointing toward the direction of gravity. This is estimated from a 3D scan of the environment,
        /// and as such, a scan must be started/finished for this value to be calculated.
        /// If using the ZED Mini / ZED2, this isn't required thanks to its IMU.
        /// </summary>
        /// <returns>Vector3 pointing downward.</returns>
        public Vector3 GetGravityEstimate()
        {
            Vector3 v = Vector3.zero;
            dllz_spatial_mapping_get_gravity_estimation(CameraID, ref v);
            return v;
        }

        /// <summary>
        /// Consolidates the chunks from a scan. This is used to turn lots of small meshes (which are efficient for
        /// the scanning process) into several large meshes (which are more convenient to work with).
        /// </summary>
        /// <param name="numberFaces"></param>
        /// <param name="nbVerticesInSubmeshes">Array of the number of vertices in each submesh.</param>
        /// <param name="nbTrianglesInSubmeshes">Array of the number of triangles in each submesh.</param>
        /// <param name="nbSubmeshes">Number of submeshes.</param>
        /// <param name="updatedIndices">List of all submeshes updated since the last update.</param>
        /// <param name="nbVertices">Total number of updated vertices in all submeshes.</param>
        /// <param name="nbTriangles">Total number of updated triangles in all submeshes.</param>
        public void MergeChunks(int numberFaces, int[] nbVerticesInSubmeshes, int[] nbTrianglesInSubmeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh)
        {
            dllz_spatial_mapping_merge_chunks(CameraID, numberFaces, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmesh);
        }

        /// <summary>
        /// Retrieves a measure texture from the ZED SDK and loads it into a ZEDMat. Use this to get an individual
        /// texture from the last grabbed frame with measurements in every pixel - such as a depth map, confidence map, etc.
        /// Measure textures are not human-viewable but don't lose accuracy, unlike image textures.
        /// </summary><remarks>
        /// If you want to access the texture via script, you'll usually want to specify CPU memory. Then you can use
        /// Marshal.Copy to move them into a new byte array, which you can load into a Texture2D.
        /// RetrieveMeasure() calls Camera::retrieveMeasure() in the C++ SDK. For more info, read:
        /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/classsl_1_1Camera.html#af799d12342a7b884242fffdef5588a7f
        /// </remarks>
        /// <param name="mat">ZEDMat to fill with the new texture.</param>
        /// <param name="measure">Measure type (depth, confidence, xyz, etc.)</param>
        /// <param name="mem">Whether the image should be on CPU or GPU memory.</param>
        /// <param name="resolution">Resolution of the texture.</param>
        /// <returns>Error code indicating if the retrieval was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE RetrieveMeasure(sl.ZEDMat mat, sl.MEASURE measure, sl.ZEDMat.MEM mem = sl.ZEDMat.MEM.MEM_CPU, sl.Resolution resolution = new sl.Resolution())
        {
            return (sl.ERROR_CODE)(dllz_retrieve_measure(CameraID, mat.MatPtr, (int)measure, (int)mem, (int)resolution.width, (int)resolution.height));
        }

        /// <summary>
        /// Retrieves an image texture from the ZED SDK and loads it into a ZEDMat. Use this to get an individual
        /// texture from the last grabbed frame in a human-viewable format. Image textures work for when you want the result to be visible,
        /// such as the direct RGB image from the camera, or a greyscale image of the depth. However it will lose accuracy if used
        /// to show measurements like depth or confidence, unlike measure textures.
        /// </summary><remarks>
        /// If you want to access the texture via script, you'll usually want to specify CPU memory. Then you can use
        /// Marshal.Copy to move them into a new byte array, which you can load into a Texture2D. Note that you may need to
        /// change the color space and/or flip the image.
        /// RetrieveMeasure() calls Camera::retrieveMeasure() in the C++ SDK. For more info, read:
        /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/classsl_1_1Camera.html#ac40f337ccc76cacd3412b93f7f4638e2
        /// </remarks>
        /// <param name="mat">ZEDMat to fill with the new texture.</param>
        /// <param name="view">Image type (left RGB, right depth map, etc.)</param>
        /// <param name="mem">Whether the image should be on CPU or GPU memory.</param>
        /// <param name="resolution">Resolution of the texture.</param>
        /// <returns>Error code indicating if the retrieval was successful, and why it wasn't otherwise.</returns>
        public sl.ERROR_CODE RetrieveImage(sl.ZEDMat mat, sl.VIEW view, sl.ZEDMat.MEM mem = sl.ZEDMat.MEM.MEM_CPU, sl.Resolution resolution = new sl.Resolution())
        {
            return (sl.ERROR_CODE)(dllz_retrieve_image(CameraID, mat.MatPtr, (int)view, (int)mem, (int)resolution.width, (int)resolution.height));
        }


        /// <summary>
        /// Computes offsets of the optical centers used to line up the ZED's images properly with Unity cameras.
        /// Called in ZEDRenderingPlane after the ZED finished initializing.
        /// </summary>
        /// <param name="planeDistance">Distance from a camera in the ZED rig to the quad/Canvas object holding the ZED image.</param>
        /// <returns></returns>
        public Vector4 ComputeOpticalCenterOffsets(float planeDistance)
        {
            IntPtr p = IntPtr.Zero;
            sl.CalibrationParameters calib = GetCalibrationParameters(false);

            Vector4 calibLeft = new Vector4(calib.leftCam.fx, calib.leftCam.fy, calib.leftCam.cx, calib.leftCam.cy);
            Vector4 calibRight = new Vector4(calib.rightCam.fx, calib.rightCam.fy, calib.rightCam.cx, calib.rightCam.cy);

            p = dllz_compute_optical_center_offsets(ref calibLeft, ref calibRight, this.ImageWidth, this.ImageHeight, planeDistance);
            if (p == IntPtr.Zero)
            {
                return new Vector4();
            }
            Vector4 parameters = (Vector4)Marshal.PtrToStructure(p, typeof(Vector4));
            return parameters;
        }

        ////////////////////////
        /// Plane Detection  ///
        ////////////////////////


        /// <summary>
        /// Looks for a plane in the visible area that is likely to represent the floor.
        /// Use ZEDPlaneDetectionManager.DetectFloorPlane for a higher-level version that turns planes into GameObjects.
        /// </summary>
        /// <param name="plane">Data on the detected plane.</param>
        /// <param name="playerHeight">Height of the camera from the newly-detected floor.</param>
        /// <param name="priorQuat">Prior rotation.</param>
        /// <param name="priorTrans">Prior position.</param>
        /// <returns></returns>
        public sl.ERROR_CODE findFloorPlane(ref ZEDPlaneGameObject.PlaneData plane, out float playerHeight, Quaternion priorQuat, Vector3 priorTrans)
        {
            IntPtr p = IntPtr.Zero;
            Quaternion out_quat = Quaternion.identity;
            Vector3 out_trans = Vector3.zero;
            p = dllz_find_floor_plane(CameraID, out out_quat, out out_trans, priorQuat, priorTrans);
            plane.Bounds = new Vector3[256];
            playerHeight = 0;

            if (p != IntPtr.Zero)
            {
                plane = (ZEDPlaneGameObject.PlaneData)Marshal.PtrToStructure(p, typeof(ZEDPlaneGameObject.PlaneData));
                playerHeight = out_trans.y;
                return (sl.ERROR_CODE)plane.ErrorCode;
            }
            else
                return sl.ERROR_CODE.FAILURE;
        }
        /// <summary>
        /// Using data from a detected floor plane, updates supplied vertex and triangle arrays with
        /// data needed to make a mesh that represents it. These arrays are updated directly from the wrapper.
        /// </summary>
        /// <param name="vertices">Array to be filled with mesh vertices.</param>
        /// <param name="triangles">Array to be filled with mesh triangles, stored as indexes of each triangle's points.</param>
        /// <param name="numVertices">Total vertices in the mesh.</param>
        /// <param name="numTriangles">Total triangle indexes (3x number of triangles).</param>
        /// <returns></returns>
        public int convertFloorPlaneToMesh(Vector3[] vertices, int[] triangles, out int numVertices, out int numTriangles)
        {
            return dllz_convert_floorplane_to_mesh(CameraID, vertices, triangles, out numVertices, out numTriangles);
        }

        /// <summary>
        /// DLL-friendly version of PlaneDetectionParameters (found in ZEDCommon.cs).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct sl_PlaneDetectionParameters
        {
            public float maxDistanceThreshold;
            public float normalSimilarityThreshold;
        };

        /// <summary>
        /// Checks for a plane in the real world at given screen-space coordinates.
        /// Use ZEDPlaneDetectionManager.DetectPlaneAtHit() for a higher-level version that turns planes into GameObjects.
        /// </summary>
        /// <param name="plane">Data on the detected plane.</param>
        /// <param name="screenPos">Point on the ZED image to check for a plane.</param>
        /// <returns></returns>
        public sl.ERROR_CODE findPlaneAtHit(ref ZEDPlaneGameObject.PlaneData plane, Vector2 screenPos, ref PlaneDetectionParameters planeDetectionParameters)
        {
            IntPtr p = IntPtr.Zero;
            Quaternion out_quat = Quaternion.identity;
            Vector3 out_trans = Vector3.zero;

            float posX = (float)ImageWidth * (float)((float)screenPos.x / (float)Screen.width);
            float posY = ImageHeight * (1 - (float)screenPos.y / (float)Screen.height);
            posX = Mathf.Clamp(posX, 0, ImageWidth);
            posY = Mathf.Clamp(posY, 0, ImageHeight);

            sl_PlaneDetectionParameters plane_params = new sl_PlaneDetectionParameters();
            plane_params.maxDistanceThreshold = planeDetectionParameters.maxDistanceThreshold;
            plane_params.normalSimilarityThreshold = planeDetectionParameters.normalSimilarityThreshold;

            p = dllz_find_plane_at_hit(CameraID, new Vector2(posX, posY), ref plane_params, false);
            plane.Bounds = new Vector3[256];

            if (p != IntPtr.Zero)
            {
                plane = (ZEDPlaneGameObject.PlaneData)Marshal.PtrToStructure(p, typeof(ZEDPlaneGameObject.PlaneData));
                return (sl.ERROR_CODE)plane.ErrorCode;
            }
            else
                return sl.ERROR_CODE.FAILURE;
        }

        /// <summary>
        /// Using data from a detected hit plane, updates supplied vertex and triangle arrays with
        /// data needed to make a mesh that represents it. These arrays are updated directly from the wrapper.
        /// </summary>
        /// <param name="vertices">Array to be filled with mesh vertices.</param>
        /// <param name="triangles">Array to be filled with mesh triangles, stored as indexes of each triangle's points.</param>
        /// <param name="numVertices">Total vertices in the mesh.</param>
        /// <param name="numTriangles">Total triangle indexes (3x number of triangles).</param>
        /// <returns></returns>
        public int convertHitPlaneToMesh(Vector3[] vertices, int[] triangles, out int numVertices, out int numTriangles)
        {
            return dllz_convert_hitplane_to_mesh(CameraID, vertices, triangles, out numVertices, out numTriangles);
        }


        ////////////////////////
        /// Streaming Module ///
        ////////////////////////

        /// <summary>
        /// Creates an streaming pipeline.
        /// </summary>
        /// <params>
        /// Streaming parameters: See sl::StreamingParameters of ZED SDK. See ZED SDK API doc for more informations
        /// </params>
        /// <returns>An ERROR_CODE that defines if the streaming pipe was successfully created</returns>
        public ERROR_CODE EnableStreaming(STREAMING_CODEC codec = STREAMING_CODEC.AVCHD_BASED, uint bitrate = 8000, ushort port = 30000, int gopSize = -1, bool adaptativeBitrate = false, int chunk_size = 8096, int target_fps = 0)
        {
            int doAdaptBitrate = adaptativeBitrate ? 1 : 0;
            return (ERROR_CODE)dllz_enable_streaming(CameraID, codec, bitrate, port, gopSize, doAdaptBitrate, chunk_size, target_fps);
        }

        /// <summary>
        /// Tells if streaming is running or not.
        /// </summary>
        /// <returns> false if streaming is not enabled, true if streaming is on</returns>
        public bool IsStreamingEnabled()
        {
            int res = dllz_is_streaming_enabled(CameraID);
            return res == 1;
        }

        /// <summary>
        /// Stops the streaming pipeline.
        /// </summary>
        public void DisableStreaming()
        {
            dllz_disable_streaming(CameraID);
        }


        ////////////////////////
        /// Save utils fct   ///
        ////////////////////////

        /// <summary>
        /// Save current image (specified by view) in a file defined by filename
        /// Supported formats are jpeg and png. Filename must end with either .jpg or .png
        /// </summary>
        public sl.ERROR_CODE SaveCurrentImageInFile(sl.VIEW view, String filename)
        {
            sl.ERROR_CODE err = (sl.ERROR_CODE)dllz_save_current_image(CameraID, view, filename);
            return err;
        }

        /// <summary>
        /// Save the current depth in a file defined by filename.
        /// Supported formats are PNG,PFM and PGM
        /// </summary>
        /// <param name="side"> defines left (0) or right (1) depth</param>
        /// <param name="filename"> filename must end with .png, .pfm or .pgm</param>
        /// <returns></returns>
        public sl.ERROR_CODE SaveCurrentDepthInFile(int side, String filename)
        {
            sl.ERROR_CODE err = (sl.ERROR_CODE)dllz_save_current_depth(CameraID, side, filename);
            return err;
        }

        /// <summary>
        /// Save the current point cloud in a file defined by filename.
        /// Supported formats are PLY,VTK,XYZ and PCD
        /// </summary>
        /// <param name="side">defines left (0) or right (1) point cloud</param>
        /// <param name="filename"> filename must end with .ply, .xyz , .vtk or .pcd </param>
        /// <returns></returns>
        public sl.ERROR_CODE SaveCurrentPointCloudInFile(int side, String filename)
        {
            sl.ERROR_CODE err = (sl.ERROR_CODE)dllz_save_current_point_cloud(CameraID, side, filename);
            return err;
        }

        ////////////////////////
        /// Object detection ///
        ////////////////////////
        #region Object Detection
        public static sl.AI_MODELS cvtDetection(sl.OBJECT_DETECTION_MODEL m_in)
        {
            sl.AI_MODELS m_out = sl.AI_MODELS.LAST;
            switch (m_in)
            {
                case sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE: m_out = sl.AI_MODELS.MULTI_CLASS_ACCURATE_DETECTION; break;
                case sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM: m_out = sl.AI_MODELS.MULTI_CLASS_MEDIUM_DETECTION; break;
                case sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_FAST: m_out = sl.AI_MODELS.MULTI_CLASS_FAST_DETECTION; break;
                case sl.OBJECT_DETECTION_MODEL.PERSON_HEAD_BOX_FAST: m_out = sl.AI_MODELS.PERSON_HEAD_FAST_DETECTION; break;
                case sl.OBJECT_DETECTION_MODEL.PERSON_HEAD_BOX_ACCURATE: m_out = sl.AI_MODELS.PERSON_HEAD_ACCURATE_DETECTION; break;
            }
            return m_out;
        }

        public static int cvtDetection(sl.AI_MODELS m_in)
        {
            int m_out = -1;
            switch (m_in)
            {
                case sl.AI_MODELS.HUMAN_BODY_ACCURATE_DETECTION: m_out = (int)sl.BODY_TRACKING_MODEL.HUMAN_BODY_ACCURATE; break;
                case sl.AI_MODELS.HUMAN_BODY_MEDIUM_DETECTION: m_out = (int)sl.BODY_TRACKING_MODEL.HUMAN_BODY_MEDIUM; break;
                case sl.AI_MODELS.HUMAN_BODY_FAST_DETECTION: m_out = (int)sl.BODY_TRACKING_MODEL.HUMAN_BODY_FAST; break;
                case sl.AI_MODELS.MULTI_CLASS_ACCURATE_DETECTION: m_out = (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE; break;
                case sl.AI_MODELS.MULTI_CLASS_MEDIUM_DETECTION: m_out = (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM; break;
                case sl.AI_MODELS.MULTI_CLASS_FAST_DETECTION: m_out = (int)sl.OBJECT_DETECTION_MODEL.MULTI_CLASS_BOX_FAST; break;
                case sl.AI_MODELS.PERSON_HEAD_FAST_DETECTION: m_out = (int)sl.OBJECT_DETECTION_MODEL.PERSON_HEAD_BOX_FAST; break;
                case sl.AI_MODELS.PERSON_HEAD_ACCURATE_DETECTION: m_out = (int)sl.OBJECT_DETECTION_MODEL.PERSON_HEAD_BOX_ACCURATE; break;
            }
            return m_out;
        }

        /// <summary>
        /// Check if a corresponding optimized engine is found for the requested Model based on your rig configuration.
        /// </summary>
        /// <param name="model"> AI model to check.</param>
        /// <param name="gpu_id">ID of the gpu.</param>
        /// <returns></returns>
        public static AI_Model_status CheckAIModelStatus(AI_MODELS model, int gpu_id = 0)
        {
            IntPtr p = dllz_check_AI_model_status(model, gpu_id);
            if (p == IntPtr.Zero)
            {
                return new AI_Model_status();
            }
            AI_Model_status status = (AI_Model_status)Marshal.PtrToStructure(p, typeof(AI_Model_status));

            return status;
        }

        /// <summary>
        /// Optimize the requested model, possible download if the model is not present on the host.
        /// </summary>
        /// <param name="model">AI model to optimize.</param>
        /// <param name="gpu_id">ID of the gpu to optimize on.</param>
        /// <returns></returns>
        public static sl.ERROR_CODE OptimizeAIModel(AI_MODELS model, int gpu_id = 0)
        {
            return (sl.ERROR_CODE)dllz_optimize_AI_model(model, gpu_id);
        }

        /// <summary>
        /// Enable object detection module
        /// </summary>
        public sl.ERROR_CODE EnableObjectDetection(ref ObjectDetectionParameters od_params)
        {
            sl.ERROR_CODE objDetectStatus = ERROR_CODE.FAILURE;
            lock (grabLock)
            {
                objDetectStatus = (sl.ERROR_CODE)dllz_enable_object_detection(CameraID, ref od_params);
            }

            return objDetectStatus;
        }

        /// <summary>
        /// Disable object detection module and release the resources.
        /// </summary>
        public void DisableObjectDetection(uint objectDetectionInstanceID = 0)
        {
            lock (grabLock)
            {
                dllz_disable_object_detection(CameraID, objectDetectionInstanceID, false);
            }
        }

        public sl.ERROR_CODE IngestCustomBoxObjects(List<CustomBoxObjectData> objects_in, uint instanceID)
        {
            return (sl.ERROR_CODE)dllz_ingest_custom_box_objects(CameraID, objects_in.Count, objects_in.ToArray(), instanceID);
        }


        /// <summary>
        /// Retrieve object detection data
        /// </summary>
        /// <param name="od_params"> Object detection runtime parameters</param>
        /// <param name="objFrame"> Objects that contains all the detection data</param>
        /// <param name="instanceID"> Id of the object detection instance. Used when multiple instances of the object detection module are enabled at the same time. </param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveObjects(ref ObjectDetectionRuntimeParameters od_params, ref Objects objFrame, uint instanceID = 0)
        {
            return (sl.ERROR_CODE)dllz_retrieve_objects_data(CameraID, ref od_params, ref objFrame, instanceID);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct dll_customObjectDetectionProperties
        {
            /// <summary>
            /// Index of the class represented by this set of properties.
            /// </summary>
            public int classID;

            /// <summary>
            /// Whether the object object is kept or not.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool enabled;
            /// <summary>
            ///  Confidence threshold.
            ///  From 1 to 100, with 1 meaning a low threshold, more uncertain objects and 99 very few but very precise objects.
            ///  If the scene contains a lot of objects, increasing the confidence can slightly speed up the process, since every object instance is tracked.
            ///  Default: 20.f
            /// </summary>
            public float detectionConfidenceThreshold;

            /// <summary>
            ///	Provide hypothesis about the object movements(degrees of freedom or DoF) to improve the object tracking.
            /// - true: 2 DoF projected alongside the floor plane. Case for object standing on the ground such as person, vehicle, etc.
            /// The projection implies that the objects cannot be superposed on multiple horizontal levels.
            /// - false: 6 DoF (full 3D movements are allowed).
            /// This parameter cannot be changed for a given object tracking id.
            /// It is advised to set it by labels to avoid issues.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool isGrounded;

            /// <summary>
            /// Provide hypothesis about the object staticity to improve the object tracking.
            /// - true: the object will be assumed to never move nor being moved.
            /// - false: the object will be assumed to be able to move or being moved.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool isStatic;

            /// <summary>
            /// Maximum tracking time threshold (in seconds) before dropping the tracked object when unseen for this amount of time.
            /// By default, let the tracker decide internally based on the internal sub class of the tracked object.
            /// Only valid for static object.
            /// </summary>
            public float trackingTimeout;

            /// <summary>
            /// Maximum tracking distance threshold (in meters) before dropping the tracked object when unseen for this amount of meters.
            /// By default, do not discard tracked object based on distance.
            /// Only valid for static object.
            /// </summary>
            public float trackingMaxDist;

            /// <summary>
            /// Maximum allowed width normalized to the image size.
            /// Any prediction bigger than that will be filtered out.
            /// Default: -1 (no filtering)
            /// </summary>
            public float maxBoxWidthNormalized;

            /// <summary>
            /// Minimum allowed width normalized to the image size.
            /// Any prediction smaller than that will be filtered out.
            /// Default: -1 (no filtering)
            /// </summary>
            public float minBoxWidthNormalized;

            /// <summary>
            /// Maximum allowed height normalized to the image size.
            /// Any prediction bigger than that will be filtered out.
            /// Default: -1 (no filtering)
            /// </summary>
            public float maxBoxHeightNormalized;

            /// <summary>
            /// Minimum allowed Height normalized to the image size.
            /// Any prediction smaller than that will be filtered out.
            /// Default: -1 (no filtering)
            /// </summary>
            public float minBoxHeightNormalized;

            public dll_customObjectDetectionProperties(CustomObjectDetectionProperties customObjectDetectionProperties)
            {
                classID = customObjectDetectionProperties.classID;
                enabled = customObjectDetectionProperties.enabled;
                detectionConfidenceThreshold = customObjectDetectionProperties.detectionConfidenceThreshold;
                isGrounded = customObjectDetectionProperties.isGrounded;
                isStatic = customObjectDetectionProperties.isStatic;
                trackingTimeout = customObjectDetectionProperties.trackingTimeout;
                trackingMaxDist = customObjectDetectionProperties.trackingMaxDist;
                maxBoxWidthNormalized = customObjectDetectionProperties.maxBoxWidthNormalized;
                minBoxWidthNormalized = customObjectDetectionProperties.minBoxWidthNormalized;
                maxBoxHeightNormalized = customObjectDetectionProperties.maxBoxHeightNormalized;
                minBoxHeightNormalized = customObjectDetectionProperties.minBoxHeightNormalized;
            }
        };

        /// <summary>
        ///  DLL-friendly version of CustomObjectDetectionRuntimeParameters (found in ZEDCommon.cs).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct dll_customObjectDetectionRuntimeParameters
        {
            /// <summary>
            /// Global object detection properties.
            /// objectDetectionProperties is used as a fallback when CustomObjectDetectionRuntimeParameters.objectClassDetectionProperties is partially set.
            /// </summary>
            public dll_customObjectDetectionProperties objectDetectionProperties;

            /// <summary>
            /// Per class object detection properties.
            /// </summary>
            public IntPtr objectClassDetectionProperties;

            /// <summary>
            /// Size of the \ref objectClassDetectionProperties array.
            /// </summary>
            public uint numberCustomDetectionProperties;

            public dll_customObjectDetectionRuntimeParameters(CustomObjectDetectionRuntimeParameters customObjectDetectionRuntimeParameters)
            {
                objectDetectionProperties = new dll_customObjectDetectionProperties(customObjectDetectionRuntimeParameters.objectDetectionProperties);
                numberCustomDetectionProperties = (uint)customObjectDetectionRuntimeParameters.objectClassDetectionProperties.Count;
                objectClassDetectionProperties = Marshal.AllocHGlobal(customObjectDetectionRuntimeParameters.objectClassDetectionProperties.Count * Marshal.SizeOf(typeof(dll_customObjectDetectionProperties)));
            }
        };

        /// <summary>
        /// Retrieve object detection data from custom object detection
        /// </summary>
        /// <param name="objectDetectionRuntimeParameters"> Custim object detection runtime parameters</param>
        /// <param name="objFrame">Objects that contains all the detection data</param>
        /// <param name="instanceID"> Id of the object detection instance. Used when multiple instances of the object detection module are enabled at the same time. </param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveObjects(ref CustomObjectDetectionRuntimeParameters objectDetectionRuntimeParameters, ref Objects objFrame, uint instanceID = 0)
        {
            dll_customObjectDetectionRuntimeParameters dll_CustomObjectDetectionRuntime = new dll_customObjectDetectionRuntimeParameters(objectDetectionRuntimeParameters);
            var e = (sl.ERROR_CODE)dllz_retrieve_custom_objects(CameraID, ref dll_CustomObjectDetectionRuntime, ref objFrame, instanceID);
            Marshal.FreeHGlobal(dll_CustomObjectDetectionRuntime.objectClassDetectionProperties);
            return e;
        }

        /// <summary>
        /// Update the batch trajectories and retrieve the number of batches.
        /// </summary>
        /// <param name="nbBatches"> numbers of batches
        /// <returns> returns an ERROR_CODE that indicates the type of error </returns>
        public sl.ERROR_CODE UpdateObjectsBatch(out int nbBatches)
        {
            return (sl.ERROR_CODE)dllz_update_objects_batch(CameraID, out nbBatches);
        }

        /// <summary>
        /// Retrieve a batch of objects.
        /// This function need to be called after RetrieveObjects, otherwise trajectories will be empty.
        /// If also needs to be called after UpdateOBjectsBatch in order to retrieve the number of batch trajectories.
        /// </summary>
        /// <remarks> To retrieve all the objectsbatches, you need to iterate from 0 to nbBatches (retrieved from UpdateObjectBatches) </remarks>
        /// <param name="batch_index"> index of the batch retrieved.
        /// <param name="objectsBatch"> trajectory that will be filled by the batching queue process</param>
        /// <returns> returns an ERROR_CODE that indicates the type of error </returns>
        public sl.ERROR_CODE GetObjectsBatch(int batch_index, ref ObjectsBatch objectsBatch)
        {
            return (sl.ERROR_CODE)dllz_get_objects_batch_data(CameraID, batch_index, ref objectsBatch.numData, ref objectsBatch.id, ref objectsBatch.label, ref objectsBatch.sublabel,
                ref objectsBatch.trackingState, objectsBatch.positions, objectsBatch.positionCovariances, objectsBatch.velocities, objectsBatch.timestamps, objectsBatch.boundingBoxes2D,
                objectsBatch.boundingBoxes, objectsBatch.confidences, objectsBatch.actionStates, objectsBatch.headBoundingBoxes2D,
                objectsBatch.headBoundingBoxes, objectsBatch.headPositions);
        }
        #endregion


        ////////////////////////
        /// Body Tracking  /////
        ////////////////////////
        #region Body Tracking
        public static sl.AI_MODELS cvtDetection(sl.BODY_TRACKING_MODEL m_in, sl.BODY_FORMAT bodyFormat)
        {
            sl.AI_MODELS m_out = sl.AI_MODELS.LAST;
            if (bodyFormat == sl.BODY_FORMAT.BODY_18 || bodyFormat == sl.BODY_FORMAT.BODY_34)
            {
                switch (m_in)
                {
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_FAST: m_out = sl.AI_MODELS.HUMAN_BODY_FAST_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_MEDIUM: m_out = sl.AI_MODELS.HUMAN_BODY_MEDIUM_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_ACCURATE: m_out = sl.AI_MODELS.HUMAN_BODY_ACCURATE_DETECTION; break;
                }
            }
            else if (bodyFormat == sl.BODY_FORMAT.BODY_38)
            {
                switch (m_in)
                {
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_FAST: m_out = sl.AI_MODELS.HUMAN_BODY_38_FAST_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_MEDIUM: m_out = sl.AI_MODELS.HUMAN_BODY_38_MEDIUM_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_ACCURATE: m_out = sl.AI_MODELS.HUMAN_BODY_38_ACCURATE_DETECTION; break;
                }

            }
            else
            {
                switch (m_in)
                {
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_FAST: m_out = sl.AI_MODELS.HUMAN_BODY_FAST_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_MEDIUM: m_out = sl.AI_MODELS.HUMAN_BODY_MEDIUM_DETECTION; break;
                    case sl.BODY_TRACKING_MODEL.HUMAN_BODY_ACCURATE: m_out = sl.AI_MODELS.HUMAN_BODY_ACCURATE_DETECTION; break;
                }
            }

            return m_out;
        }

        /// <summary>
        /// Enable body tracking module
        /// </summary>
        public sl.ERROR_CODE EnableBodyTracking(ref BodyTrackingParameters bt_params)
        {
            sl.ERROR_CODE bodyTrackingStatus = ERROR_CODE.FAILURE;
            lock (grabLock)
            {
                bodyTrackingStatus = (sl.ERROR_CODE)dllz_enable_body_tracking(CameraID, ref bt_params);
            }

            return bodyTrackingStatus;
        }

        /// <summary>
        /// Disable body tracking module and release the resources.
        /// </summary>
        public void DisableBodyTracking(uint bodyTrackingInstanceID = 1)
        {
            lock (grabLock)
            {
                dllz_disable_body_tracking(CameraID, bodyTrackingInstanceID, false);
            }
        }

        /// <summary>
        /// Retrieve body tracking data 
        /// </summary>
        /// <param name="body_params"> Body Tracking runtime parameters</param>
        /// <param name="bodies"> Bodies that contains all the detection data</param>
        /// <param name="instanceID"> Id of the object detection instance. Used when multiple instances of the object detection module are enabled at the same time. </param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveBodies(ref BodyTrackingRuntimeParameters bt_params, ref Bodies bodies, uint instanceID = 0)
        {
            return (sl.ERROR_CODE)dllz_retrieve_bodies_data(CameraID, ref bt_params, ref bodies, instanceID);
        }
        #endregion


    }//Zed Camera class
} // namespace sl
