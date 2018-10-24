//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;


namespace sl
{
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
        const string nameDll = "sl_unitywrapper";

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
        /// Current ZED instance. Used for Singleton implementation. 
        /// </summary>
        private static ZEDCamera instance = null;

        /// <summary>
        /// True if the ZED SDK is installed.
        /// </summary>
        private static bool pluginIsReady = true;

        /// <summary>
        /// Mutex for threaded rendering.
        /// </summary>
        private static object _lock = new object();

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
        private ZEDCameraSettingsManager cameraSettingsManager = new ZEDCameraSettingsManager();

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
        /// Camera model - ZED or ZED Mini. 
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
        /// List of DLLs needed to run the plugin. 
        /// </summary>
        static private string[] dependenciesNeeded =
        {
            "sl_zed64.dll",
            "sl_core64.dll",
            "sl_input64.dll"
		};

        /// <summary>
        /// Layer that only the ZED is able to see. Used in ZEDSteamVRControllerManager for 'clone' controllers. 
        /// </summary>
        const int tagZEDCamera = 20;
        /// <summary>
        /// Layer that only the ZED is able to see. Used in ZEDSteamVRControllerManager for 'clone' controllers. 
        /// </summary>
        public static int Tag
        {
            get { return tagZEDCamera; }
        }
        /// <summary>
        /// Layer that the ZED can't see, but overlay cameras created by ZEDMeshRenderer and ZEDPlaneRenderer can.
        /// </summary>
        const int tagOneObject = 12;
        /// <summary>
        /// Layer that the ZED can't see, but overlay cameras created by ZEDMeshRenderer and ZEDPlaneRenderer can.
        /// </summary>
        public static int TagOneObject
        {
            get { return tagOneObject; }
        }

        #region DLL Calls
        /// <summary>
        /// Cuurent Plugin Version.
        /// </summary>
        public static readonly System.Version PluginVersion = new System.Version(2, 7, 0);

        /******** DLL members ***********/

        [DllImport(nameDll, EntryPoint = "GetRenderEventFunc")]
        private static extern IntPtr GetRenderEventFunc();

        [DllImport(nameDll, EntryPoint = "dllz_register_callback_debuger")]
        private static extern void dllz_register_callback_debuger(DebugCallback callback);


		/*
          * Utils function.
          */
		[DllImport(nameDll, EntryPoint = "dllz_find_usb_device")]
		private static extern bool dllz_find_usb_device(USB_DEVICE dev);

        /*
          * Create functions
          */
        [DllImport(nameDll, EntryPoint = "dllz_create_camera")]
		private static extern System.IntPtr dllz_create_camera(bool verbose);


        /*
        * Opening function (Opens camera and creates textures).
        */
        [DllImport(nameDll, EntryPoint = "dllz_open")]
		private static extern int dllz_open(ref dll_initParameters parameters, System.Text.StringBuilder svoPath, System.Text.StringBuilder output,System.Text.StringBuilder opt_settings_path);




        /*
         * Close function.
         */
        [DllImport(nameDll, EntryPoint = "dllz_close")]
        private static extern void dllz_close();


        /*
         * Grab function.
         */
        [DllImport(nameDll, EntryPoint = "dllz_grab")]
        private static extern int dllz_grab(ref sl.RuntimeParameters runtimeParameters);



        /*
        * Recording functions.
        */
        [DllImport(nameDll, EntryPoint = "dllz_enable_recording")]
        private static extern int dllz_enable_recording(byte[] video_filename, int compresssionMode);

        [DllImport(nameDll, EntryPoint = "dllz_record")]
        private static extern void dllz_record(ref Recording_state state);

        [DllImport(nameDll, EntryPoint = "dllz_disable_recording")]
        private static extern bool dllz_disable_recording();


        /*
        * Texturing functions.
        */
		[DllImport(nameDll, EntryPoint = "dllz_retrieve_textures")]
		private static extern void dllz_retrieve_textures();

		[DllImport(nameDll, EntryPoint = "dllz_get_updated_textures_timestamp")]
		private static extern ulong dllz_get_updated_textures_timestamp();

		[DllImport(nameDll, EntryPoint = "dllz_swap_textures")]
		private static extern ulong dllz_swap_textures();



        [DllImport(nameDll, EntryPoint = "dllz_register_texture_image_type")]
        private static extern int dllz_register_texture_image_type(int option, IntPtr id, Resolution resolution);

        [DllImport(nameDll, EntryPoint = "dllz_register_texture_measure_type")]
        private static extern int dllz_register_texture_measure_type(int option, IntPtr id, Resolution resolution);

        [DllImport(nameDll, EntryPoint = "dllz_unregister_texture_measure_type")]
        private static extern int dllz_unregister_texture_measure_type(int option);

        [DllImport(nameDll, EntryPoint = "dllz_unregister_texture_image_type")]
        private static extern int dllz_unregister_texture_image_type(int option);

        [DllImport(nameDll, EntryPoint = "dllz_get_copy_mat_texture_image_type")]
        private static extern IntPtr dllz_get_copy_mat_texture_image_type(int option);

        [DllImport(nameDll, EntryPoint = "dllz_get_copy_mat_texture_measure_type")]
        private static extern IntPtr dllz_get_copy_mat_texture_measure_type(int option);

        /*
        * Self-calibration functions.
        */
        [DllImport(nameDll, EntryPoint = "dllz_reset_self_calibration")]
        private static extern void dllz_reset_self_calibration();

        [DllImport(nameDll, EntryPoint = "dllz_get_self_calibration_state")]
        private static extern int dllz_get_self_calibration_state();


        /*
         * Camera control functions.
         */


        [DllImport(nameDll, EntryPoint = "dllz_set_camera_fps")]
        private static extern void dllz_set_camera_fps(int fps);

        [DllImport(nameDll, EntryPoint = "dllz_get_camera_fps")]
        private static extern float dllz_get_camera_fps();

        [DllImport(nameDll, EntryPoint = "dllz_get_width")]
        private static extern int dllz_get_width();

        [DllImport(nameDll, EntryPoint = "dllz_get_height")]
        private static extern int dllz_get_height();

		[DllImport(nameDll, EntryPoint = "dllz_get_calibration_parameters")]
		private static extern IntPtr dllz_get_calibration_parameters(bool raw);

		[DllImport(nameDll, EntryPoint = "dllz_get_camera_model")]
		private static extern int dllz_get_camera_model();

		[DllImport(nameDll, EntryPoint = "dllz_get_zed_firmware")]
		private static extern int dllz_get_zed_firmware();

		[DllImport(nameDll, EntryPoint = "dllz_get_zed_serial")]
		private static extern int dllz_get_zed_serial();

		[DllImport(nameDll, EntryPoint = "dllz_get_camera_imu_transform")]
		private static extern void dllz_get_camera_imu_transform(ulong timeStamp, bool useLatency,out Vector3 translation, out Quaternion rotation);

        [DllImport(nameDll, EntryPoint = "dllz_is_zed_connected")]
        private static extern int dllz_is_zed_connected();

        [DllImport(nameDll, EntryPoint = "dllz_get_camera_timestamp")]
        private static extern ulong dllz_get_camera_timestamp();

        [DllImport(nameDll, EntryPoint = "dllz_get_current_timestamp")]
        private static extern ulong dllz_get_current_timestamp();

        [DllImport(nameDll, EntryPoint = "dllz_get_image_updater_time_stamp")]
        private static extern ulong dllz_get_image_updater_time_stamp();

        [DllImport(nameDll, EntryPoint = "dllz_get_frame_dropped_count")]
        private static extern uint dllz_get_frame_dropped_count();

		[DllImport(nameDll, EntryPoint = "dllz_get_frame_dropped_percent")]
		private static extern float dllz_get_frame_dropped_percent();

        /*
         * SVO control functions.
         */

        [DllImport(nameDll, EntryPoint = "dllz_set_svo_position")]
        private static extern void dllz_set_svo_position(int frame);

        [DllImport(nameDll, EntryPoint = "dllz_get_svo_number_of_frames")]
        private static extern int dllz_get_svo_number_of_frames();

        [DllImport(nameDll, EntryPoint = "dllz_get_svo_position")]
        private static extern int dllz_get_svo_position();


        /*
         * Depth Sensing utils functions.
         */
        [DllImport(nameDll, EntryPoint = "dllz_set_confidence_threshold")]
        private static extern void dllz_set_confidence_threshold(int threshold);

        [DllImport(nameDll, EntryPoint = "dllz_get_confidence_threshold")]
        private static extern int dllz_get_confidence_threshold();

        [DllImport(nameDll, EntryPoint = "dllz_set_depth_max_range_value")]
        private static extern void dllz_set_depth_max_range_value(float distanceMax);

        [DllImport(nameDll, EntryPoint = "dllz_get_depth_max_range_value")]
        private static extern float dllz_get_depth_max_range_value();

        [DllImport(nameDll, EntryPoint = "dllz_get_depth_value")]
        private static extern float dllz_get_depth_value(uint x, uint y);

		[DllImport(nameDll, EntryPoint = "dllz_get_distance_value")]
		private static extern float dllz_get_distance_value(uint x, uint y);

        [DllImport(nameDll, EntryPoint = "dllz_get_normal_value")]
        private static extern int dllz_get_normal_value(uint x, uint y, out Vector4 value);

		[DllImport(nameDll, EntryPoint = "dllz_get_xyz_value")]
		private static extern int dllz_get_xyz_value(uint x, uint y, out Vector4 value);

		[DllImport(nameDll, EntryPoint = "dllz_get_depth_min_range_value")]
        private static extern float dllz_get_depth_min_range_value();


        /*
         * Motion Tracking functions.
         */
        [DllImport(nameDll, EntryPoint = "dllz_enable_tracking")]
		private static extern int dllz_enable_tracking(ref Quaternion quat, ref Vector3 vec, bool enableSpatialMemory = false,bool enablePoseSmoothing = false, System.Text.StringBuilder aeraFilePath = null);

        [DllImport(nameDll, EntryPoint = "dllz_disable_tracking")]
        private static extern void dllz_disable_tracking(System.Text.StringBuilder path);

        [DllImport(nameDll, EntryPoint = "dllz_save_current_area")]
        private static extern int dllz_save_current_area(System.Text.StringBuilder path);

        [DllImport(nameDll, EntryPoint = "dllz_get_position_data")]
        private static extern int dllz_get_position_data(ref Pose pose, int reference_frame);

        [DllImport(nameDll, EntryPoint = "dllz_get_position")]
        private static extern int dllz_get_position(ref Quaternion quat, ref Vector3 vec, int reference_frame);

        [DllImport(nameDll, EntryPoint = "dllz_get_position_at_target_frame")]
        private static extern int dllz_get_position_at_target_frame(ref Quaternion quaternion, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation, int reference_frame);

        [DllImport(nameDll, EntryPoint = "dllz_transform_pose")]
        private static extern void dllz_transform_pose(ref Quaternion quaternion, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation);

        [DllImport(nameDll, EntryPoint = "dllz_reset_tracking")]
        private static extern int dllz_reset_tracking(Quaternion rotation, Vector3 translation);

		[DllImport(nameDll, EntryPoint = "dllz_reset_tracking_with_offset")]
		private static extern int dllz_reset_tracking_with_offset(Quaternion rotation, Vector3 translation, Quaternion offsetQuaternion, Vector3 offsetTranslation);

		[DllImport(nameDll, EntryPoint = "dllz_set_imu_prior_orientation")]
		private static extern int dllz_set_imu_prior_orientation(Quaternion rotation);

		[DllImport(nameDll, EntryPoint = "dllz_get_internal_imu_orientation")]
		private static extern int dllz_get_internal_imu_orientation(ref Quaternion rotation, int reference_time);

		[DllImport(nameDll, EntryPoint = "dllz_get_internal_imu_data")]
		private static extern int dllz_get_internal_imu_data(ref IMUData imuData, int reference_time);

        [DllImport(nameDll, EntryPoint = "dllz_get_area_export_state")]
        private static extern int dllz_get_area_export_state();

        /*
        * Spatial Mapping functions.
        */
        [DllImport(nameDll, EntryPoint = "dllz_enable_spatial_mapping")]
        private static extern int dllz_enable_spatial_mapping(float resolution_meter, float max_range_meter, int saveTexture);

        [DllImport(nameDll, EntryPoint = "dllz_disable_spatial_mapping")]
        private static extern void dllz_disable_spatial_mapping();

        [DllImport(nameDll, EntryPoint = "dllz_pause_spatial_mapping")]
        private static extern void dllz_pause_spatial_mapping(bool status);

        [DllImport(nameDll, EntryPoint = "dllz_request_mesh_async")]
        private static extern void dllz_request_mesh_async();

        [DllImport(nameDll, EntryPoint = "dllz_get_mesh_request_status_async")]
        private static extern int dllz_get_mesh_request_status_async();

        [DllImport(nameDll, EntryPoint = "dllz_update_mesh")]
        private static extern int dllz_update_mesh(int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "dllz_retrieve_mesh")]
        private static extern int dllz_retrieve_mesh(Vector3[] vertices, int[] triangles, int nbSubmesh, Vector2[] uvs, IntPtr textures);

        [DllImport(nameDll, EntryPoint = "dllz_save_mesh")]
        private static extern bool dllz_save_mesh(string filename, MESH_FILE_FORMAT format);

        [DllImport(nameDll, EntryPoint = "dllz_load_mesh")]
        private static extern bool dllz_load_mesh(string filename, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbMaxSubmesh, int[] textureSize = null);

        [DllImport(nameDll, EntryPoint = "dllz_apply_texture")]
        private static extern bool dllz_apply_texture(int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int[] textureSize, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "dllz_filter_mesh")]
        private static extern bool dllz_filter_mesh(FILTER meshFilter, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "dllz_get_spatial_mapping_state")]
        private static extern int dllz_get_spatial_mapping_state();

        [DllImport(nameDll, EntryPoint = "dllz_spatial_mapping_merge_chunks")]
        private static extern void dllz_spatial_mapping_merge_chunks(int numberFaces, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh);

        [DllImport(nameDll, EntryPoint = "dllz_spatial_mapping_get_gravity_estimation")]
        private static extern void dllz_spatial_mapping_get_gravity_estimation(ref Vector3 v);

		/*
		 * Plane Detection functions (starting v2.4)
		 */
		[DllImport(nameDll, EntryPoint = "dllz_find_floor_plane")]
		private static extern IntPtr dllz_find_floor_plane(out Quaternion rotation,out Vector3 translation, Vector3 priorQuaternion, Vector3 priorTranslation);

		[DllImport(nameDll, EntryPoint = "dllz_find_plane_at_hit")]
		private static extern IntPtr dllz_find_plane_at_hit(Vector2 HitPixel,bool refine);

		[DllImport(nameDll, EntryPoint = "dllz_convert_floorplane_to_mesh")]
		private static extern int dllz_convert_floorplane_to_mesh(Vector3[] vertices, int[] triangles,out int numVertices,out int numTriangles);

		[DllImport(nameDll, EntryPoint = "dllz_convert_hitplane_to_mesh")]
		private static extern int dllz_convert_hitplane_to_mesh (Vector3[] vertices, int[] triangles, out int numVertices, out int numTriangles);


        /*
         * Specific plugin functions
         */
        [DllImport(nameDll, EntryPoint = "dllz_check_plugin")]
		private static extern int dllz_check_plugin(int major, int minor);

        [DllImport(nameDll, EntryPoint = "dllz_set_is_threaded")]
        private static extern void dllz_set_is_threaded();

        [DllImport(nameDll, EntryPoint = "dllz_get_sdk_version")]
        private static extern IntPtr dllz_get_sdk_version();

        [DllImport(nameDll, EntryPoint = "dllz_compute_offset")]
        private static extern void dllz_compute_offset(float[] A, float[] B, int nbVectors, float[] C);

        [DllImport(nameDll, EntryPoint = "dllz_compute_optical_center_offsets")]
        private static extern System.IntPtr dllz_compute_optical_center_offsets(ref Vector4 calibLeft, ref Vector4 calibRight, sl.Resolution imageResolution, float planeDistance);


        /*
         * Retreieves used by mat
         */
        [DllImport(nameDll, EntryPoint = "dllz_retrieve_measure")]
        private static extern int dllz_retrieve_measure(System.IntPtr ptr, int type, int mem, sl.Resolution resolution);

        [DllImport(nameDll, EntryPoint = "dllz_retrieve_image")]
        private static extern int dllz_retrieve_image(System.IntPtr ptr, int type, int mem, sl.Resolution resolution);

        #endregion

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

        /// <summary>
        /// Checks that the ZED plugin's dependencies are installed. 
        /// </summary>
        public static bool CheckPlugin()
        {
			pluginIsReady = false;
			string env = Environment.GetEnvironmentVariable("ZED_SDK_ROOT_DIR"); 
			if (env != null) //Found path to the ZED SDK in PC's environmental variables. 
			{
				bool error = CheckDependencies(System.IO.Directory.GetFiles(env + "\\bin")); //Make sure ZED dlls exist and work. 
				if (error) {
					Debug.LogError (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.SDK_NOT_INSTALLED)); //Print that SDK isn't installed. 
					return false;
				} else { //Found DLLs in ZED install directory. 

					try {
						if (dllz_check_plugin (PluginVersion.Major, PluginVersion.Minor) != 0) { //0 = installed SDK is compatible with plugin. 1 otherwise.
							Debug.LogError (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.SDK_DEPENDENCIES_ISSUE));
							return false;
						}
					}
					catch(DllNotFoundException) {

						Debug.LogError (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.SDK_DEPENDENCIES_ISSUE));
						return false;
					}
				}
			}
			else //Couldn't find the ZED SDK in the computer's environmental variables. 
			{
				Debug.LogError(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_NOT_INSTALLED));
				return false;
			}

			pluginIsReady = true;
			return true;
         }

		/// <summary>
		/// Checks if the USB device of a 'brand' type is connected. Used to check if a VR headset are connected
        /// for display in ZEDManager's Inspector. 
		/// </summary>
		/// <returns><c>True</c>, if USB device connected was found, <c>false</c> otherwise.</returns>
		/// <param name="Device brand.">Type.</param>
		public static bool CheckUSBDeviceConnected(USB_DEVICE Type)
		{
			if (dllz_find_usb_device (Type))
				return true;
			else
				return false;
		}

        /// <summary>
        /// Checks if all the required DLLs are available and tries calling a dummy function from each one. 
        /// </summary>
        /// <param name="filesFound">All files in a specified directory.</param>
        /// <returns>True if the dependencies were not found. False if they were all found.</returns>
        static private bool CheckDependencies(string[] filesFound)
        {
            bool isASDKPb = false;
			if (filesFound == null) {
				return true;
			}
            foreach (string dependency in dependenciesNeeded)
            {
                bool found = false;
                foreach (string file in filesFound)
                {
                    if (System.IO.Path.GetFileName(file).Equals(dependency))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    isASDKPb = true;
                    Debug.LogError("[ZED Plugin ] : " + dependency + " is not found");
                }
            }

			return isASDKPb;
        }

        /// <summary>
        /// Singleton implementation. Gets the instance of the ZEDCamera, creating a new one if one doesn't exist. 
        /// </summary>
        /// <returns>The ZEDCamera instance.</returns>
        public static ZEDCamera GetInstance()
        {
            lock (_lock)
            {
                if (instance == null)
                {
                    instance = new ZEDCamera();
					if (CheckPlugin())
                    dllz_register_callback_debuger(new DebugCallback(DebugMethod));
                }
                return instance;
            }
        }

        /// <summary>
        /// Private constructor. Initializes lists to hold references to textures and texture requests. 
        /// </summary>
        private ZEDCamera()
        {
            //Create the textures
            textures = new Dictionary<int, Dictionary<int, Texture2D>>();
            texturesRequested = new List<TextureRequested>();
        }

        /// <summary>
        /// Create a camera in Live mode (input comes from a connected ZED device, not SVO playback). 
        /// </summary>
        /// <param name="verbose">True to create detailed log file of SDK calls at the cost of performance.</param>
		public void CreateCamera(bool verbose)
        {
            string infoSystem = SystemInfo.graphicsDeviceType.ToString().ToUpper();
            if (!infoSystem.Equals("DIRECT3D11") && !infoSystem.Equals("OPENGLCORE"))
            {
                throw new Exception("The graphic library [" + infoSystem + "] is not supported");
            }
			dllz_create_camera(verbose);
        }

        /// <summary>
        /// Closes the camera and deletes all textures.
        /// Once destroyed, you need to recreate a camera instance to restart again.
        /// </summary>
        public void Destroy()
        {
			if (instance != null) {
				cameraReady = false;
				dllz_close();
				DestroyAllTexture();
				instance = null;
			}
        }

        /// <summary>
        /// DLL-friendly version of InitParameters (found in ZEDCommon.cs). 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct dll_initParameters
        {
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
            /// NOT CURRENTLY SUPPORTED IN UNITY. 
            /// </summary>
            public int cameraLinuxID;
            /// <summary>
            /// True to skip dropped frames during SVO playback. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool svoRealTimeMode;
            /// <summary>
            /// Coordinate unit for all measurements (depth, tracking, etc.). Meters are recommended for Unity. 
            /// </summary>
            public UNIT coordinateUnit;
            /// <summary>
            /// Defines order and direction of coordinate system axes. Unity uses left-handed, Y up, so this setting is recommended. 
            /// </summary>
            public COORDINATE_SYSTEM coordinateSystem;
            /// <summary>
            /// Quality level of depth calculations. Higher settings improve accuracy but cost performance. 
            /// </summary>
            public sl.DEPTH_MODE depthMode;
            /// <summary>
            /// Minimum distance from the camera from which depth will be computed, in the defined coordinateUnit. 
            /// </summary>
            public float depthMinimumDistance;
            /// <summary>
            /// True to flip images horizontally. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool cameraImageFlip;
            /// <summary>
            /// True if depth relative to the right sensor should be computed. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool enableRightSideMeasure;
            /// <summary>
            /// True to disable self-calibration, using unoptimized optional calibration parameters. 
            /// False is recommended for optimized calibration. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool cameraDisableSelfCalib;
            /// <summary>
            /// Set the number of buffers for the internal buffer process. LINUX ONLY - NOT CURRENTLY USED IN UNITY PLUGIN. 
            /// </summary>
            public int cameraBufferCountLinux;
            /// <summary>
            /// True for the SDK to provide text feedback. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool sdkVerbose;
            /// <summary>
            /// ID of the graphics card on which the ZED's computations will be performed. 
            /// </summary>
            public int sdkGPUId;
            /// <summary>
            /// True to stabilize the depth map. Recommended. 
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool depthStabilization;

            /// <summary>
            /// Copy constructor. Takes values from Unity-suited InitParameters class. 
            /// </summary>
            /// <param name="init"></param>
            public dll_initParameters(InitParameters init)
            {
                resolution = init.resolution;
                cameraFps = init.cameraFPS;
                svoRealTimeMode = init.svoRealTimeMode;
                coordinateUnit = init.coordinateUnit;
                depthMode = init.depthMode;
                depthMinimumDistance = init.depthMinimumDistance;
                cameraImageFlip = init.cameraImageFlip;
                enableRightSideMeasure = init.enableRightSideMeasure;
                cameraDisableSelfCalib = init.cameraDisableSelfCalib;
                cameraBufferCountLinux = init.cameraBufferCountLinux;
                sdkVerbose = init.sdkVerbose;
                sdkGPUId = init.sdkGPUId;
                cameraLinuxID = init.cameraLinuxID;
                coordinateSystem = init.coordinateSystem;
                depthStabilization = init.depthStabilization;
            }
        }


        /// <summary>
        /// Checks if the ZED camera is plugged in, opens it, and initializes the projection matix and command buffers for updating textures.
        /// </summary>
        /// <param name="initParameters">Class with all initialization settings. 
        /// A newly-instantiated InitParameters will have recommended default values.</param>
        /// <returns>ERROR_CODE: The error code gives information about the internal connection process. 
        /// If SUCCESS is returned, the camera is ready to use. Every other code indicates an error.</returns>
        public ERROR_CODE Init(ref InitParameters initParameters)
        {
            //Update values with what we're about to pass to the camera. 
			currentResolution = initParameters.resolution;
			fpsMax = GetFpsForResolution(currentResolution);
            if (initParameters.cameraFPS == 0)
            {
                initParameters.cameraFPS = (int)fpsMax;
            }

            dll_initParameters initP = new dll_initParameters(initParameters); //DLL-friendly version of InitParameters.  
            initP.coordinateSystem = COORDINATE_SYSTEM.LEFT_HANDED_Y_UP; //Left-hand, Y-up is Unity's coordinate system, so we match that. 
            int v = dllz_open(ref initP, 
				new System.Text.StringBuilder(initParameters.pathSVO, initParameters.pathSVO.Length),
				new System.Text.StringBuilder(initParameters.sdkVerboseLogFile, initParameters.sdkVerboseLogFile.Length),
				new System.Text.StringBuilder(initParameters.optionalSettingsPath, initParameters.optionalSettingsPath.Length));

			if ((ERROR_CODE)v != ERROR_CODE.SUCCESS)
            {
				cameraReady = false;
				return (ERROR_CODE)v;
            }

            //Set more values if the initialization was successful. 
            imageWidth = dllz_get_width();
            imageHeight = dllz_get_height();

			GetCalibrationParameters(false);
			FillProjectionMatrix();
			baseline = calibrationParametersRectified.Trans[0];
			fov_H = calibrationParametersRectified.leftCam.hFOV * Mathf.Deg2Rad;
			fov_V = calibrationParametersRectified.leftCam.vFOV * Mathf.Deg2Rad;
			cameraModel = GetCameraModel ();
			cameraReady = true;
			return (ERROR_CODE)v;
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
            return (sl.ERROR_CODE)dllz_grab(ref runtimeParameters);
        }

        /// <summary>
        /// The reset function can be called at any time AFTER the Init function has been called.
        /// It will reset and recalculate to correct for misalignment, convergence and color mismatch.
        /// It can be called after changing camera parameters without needing to restart your executable.
        /// </summary>
        public void ResetSelfCalibration()
        {
            dllz_reset_self_calibration();
        }

        /// <summary>
        /// Creates a file for recording the ZED's output into a .SVO or .AVI video.
        /// </summary><remarks>An SVO is Stereolabs' own format designed for the ZED. It holds the video feed with timestamps
        /// as well as info about the camera used to record it.</remarks>
        /// <param name="videoFileName">Filename. Whether it ends with .svo or .avi defines its file type.</param>
        /// <param name="compressionMode">How much compression to use</param>
        /// <returns>An ERROR_CODE that defines if the file was successfully created and can be filled with images.</returns>
		public ERROR_CODE EnableRecording(string videoFileName, SVO_COMPRESSION_MODE compressionMode = SVO_COMPRESSION_MODE.AVCHD_BASED)
        {
            return (ERROR_CODE)dllz_enable_recording(StringUtf8ToByte(videoFileName), (int)compressionMode);
        }

        /// <summary>
        /// Begins record the images to an SVO or AVI. EnableRecording() needs to be called first.
        /// </summary>
        public Recording_state Record()
        {
            Recording_state state = new Recording_state();
            dllz_record(ref state);
            return state;
        }

        /// <summary>
        /// Stops recording to an SVO/AVI, if applicable, and closes the file.
        /// </summary>
        public bool DisableRecording()
        {
            return dllz_disable_recording();
        }

        /// <summary>
        /// Sets a new target frame rate for the camera. If it's not possible with the current resolution,
        /// the SDK will target the closest possible frame rate instead.
        /// </summary>
        /// <param name="fps">New target FPS.</param>
        public void SetCameraFPS(int fps)
        {
            if (GetFpsForResolution(currentResolution) >= fps)
            {
                fpsMax = (uint)fps;
            }

            dllz_set_camera_fps(fps);
        }

        /// <summary>
        /// Sets the position of the SVO file currently being read to a desired frame.
        /// </summary>
        /// <param name="frame">Index of the desired frame to be decoded.</param>
        public void SetSVOPosition(int frame)
        {
            dllz_set_svo_position(frame);
        }

        /// <summary>
        /// Gets the current confidence threshold value for the disparity map (and by extension the depth map).
        /// Values below the given threshold are removed from the depth map. 
        /// </summary>
        /// <returns>Filtering value between 0 and 100.</returns>
        public int GetConfidenceThreshold()
        {
            return dllz_get_confidence_threshold();
        }

        /// <summary>
        /// Gets the timestamp at the time the latest grabbed frame was extracted from the USB stream. 
        /// This is the closest timestamp you can get from when the image was taken. Must be called after calling grab().
        /// </summary>
        /// <returns>Current timestamp in nanoseconds. -1 means it's is not available, such as with an .SVO file without compression.</returns>
        public ulong GetCameraTimeStamp()
        {
            return dllz_get_camera_timestamp();
        }

        /// <summary>
        /// Gets the current timestamp at the time the function is called. Can be compared to the camera timestamp 
        /// for synchronization, since they have the same reference (the computer's start time).
        /// </summary>
        /// <returns>The timestamp in nanoseconds.</returns>
        public ulong GetCurrentTimeStamp()
        {
            return dllz_get_current_timestamp();
        }

        /// <summary>
        /// Timestamp from the most recent image update. Based on the computer's start time. 
        /// </summary>
        /// <returns>The timestamp in nanoseconds.</returns>
        public ulong GetImageUpdaterTimeStamp()
        {
            return dllz_get_image_updater_time_stamp();
        }

        /// <summary>
        /// Get the current position of the SVO being recorded to. 
        /// </summary>
        /// <returns>Index of the frame being recorded to.</returns>
        public int GetSVOPosition()
        {
            return dllz_get_svo_position();
        }

        /// <summary>
        /// Gets the total number of frames in the loaded SVO file.
        /// </summary>
        /// <returns>Total frames in the SVO file. Returns -1 if the SDK is not reading an SVO.</returns>
        public int GetSVONumberOfFrames()
        {
            return dllz_get_svo_number_of_frames();
        }

        /// <summary>
        /// Gets the closest measurable distance by the camera, according to the camera type and depth map parameters.
        /// </summary>
        /// <returns>The nearest possible depth value.</returns>
        public float GetDepthMinRangeValue()
        {
            return dllz_get_depth_min_range_value();
        }

        /// <summary>
        /// Returns the current maximum distance of depth/disparity estimation.
        /// </summary>
        /// <returns>The closest depth</returns>
        public float GetDepthMaxRangeValue()
        {
            return dllz_get_depth_max_range_value();
        }

        /// <summary>
        /// Initialize and Start the tracking functions
        /// </summary>
        /// <param name="quat"> rotation used as initial world transform. By default it should be identity.</param>
        /// <param name="vec"> translation used as initial world transform. By default it should be identity.</param>
        /// <param name="enableSpatialMemory">  (optional) define if spatial memory is enable or not.</param>
        /// <param name="areaFilePath"> (optional) file of spatial memory file that has to be loaded to relocate in the scene.</param>
        /// <returns></returns>
		public sl.ERROR_CODE EnableTracking(ref Quaternion quat, ref Vector3 vec, bool enableSpatialMemory = true, bool enablePoseSmoothing = false, string areaFilePath = "")
        {
			sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			trackingStatus = (sl.ERROR_CODE)dllz_enable_tracking (ref quat, ref vec, enableSpatialMemory, enablePoseSmoothing, new System.Text.StringBuilder (areaFilePath, areaFilePath.Length));
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
            trackingStatus = (sl.ERROR_CODE)dllz_reset_tracking(rotation, translation);
			return trackingStatus;
        }

		public sl.ERROR_CODE ResetTrackingWithOffset(Quaternion rotation, Vector3 translation,Quaternion rotationOffset, Vector3 translationOffset)
		{
			sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			trackingStatus = (sl.ERROR_CODE)dllz_reset_tracking_with_offset(rotation, translation,rotationOffset,translationOffset);
			return trackingStatus;
		}


        /// <summary>
        ///  Stop the motion tracking, if you want to restart, call enableTracking().
        /// </summary>
        /// <param name="path">The path to save the area file</param>
        public void DisableTracking(string path = "")
        {
			dllz_disable_tracking (new System.Text.StringBuilder (path, path.Length));
        }

        public sl.ERROR_CODE SaveCurrentArea(string path)
        {
            return (sl.ERROR_CODE)dllz_save_current_area(new System.Text.StringBuilder(path, path.Length));
        }

        /// <summary>
        /// Returns the current state of the area learning saving
        /// </summary>
        /// <returns></returns>
        public sl.AREA_EXPORT_STATE GetAreaExportState()
        {
            return (sl.AREA_EXPORT_STATE)dllz_get_area_export_state();
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
            int error = dllz_register_texture_image_type((int)mode, idTexture, resolution);
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

            int error = dllz_register_texture_measure_type((int)mode, idTexture, resolution);

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
            return dllz_unregister_texture_image_type((int)view) != 0;
        }

        /// <summary>
        /// Unregisters a texture of type Measure, The texture will be destroyed and will no longer be updated each frame. 
        /// </summary>
        /// <param name="measure">What the measure was showing (disparity, depth, confidence, etc.)</param>
        public bool UnregisterTextureMeasureType(sl.MEASURE measure)
        {
            DestroyTextureMeasureType((int)measure);
            return dllz_unregister_texture_measure_type((int)measure) != 0;
        }

        /// <summary>
        /// Copies a Texture of type Image into a ZEDMat. This function should be called after a Grab() and an UpdateTextures().
        /// </summary>
        /// <param name="view">View type (left rgb, right depth, etc.)</param>
        /// <returns>New ZEDMat for an image texture of the selected view type.</returns>
        public ZEDMat RequestCopyMatFromTextureImageType(sl.VIEW view)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_image_type((int)view));
        }

        /// <summary>
        /// Copies a texture of type Measure into a ZEDMat. This function should be called after a Grab() and an UpdateTextures().
        /// </summary>
        /// <param name="measure">Measure type (depth, disparity, confidence, etc.)</param>
        /// <returns>New ZEDMat for a measure texture of the selected measure type.</returns>
        public ZEDMat RequestCopyMatFromTextureMeasureType(sl.MEASURE measure)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_measure_type((int)measure));
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
        /// Sets a filtering value for the disparity map (and by extension the depth map). The filter removes depth values if their confidence 
        /// value is below the filtering value. Should be called before any Grab() that should take the threshold into account. 
        /// </summary>
        /// <param name="threshold">Value in [1,100]. Lower values mean more confidence and precision, but less density.
        /// Upper values result in more density, but less confidence overall. Values outside the range result in no filtering. 
        ///</param>
        public void SetConfidenceThreshold(int threshold)
        {
            dllz_set_confidence_threshold(threshold);
        }

        /// <summary>
        /// Sets the maximum distance of depth/disparity estimation. All values beyond will be reported as TOO_FAR and not used. 
        /// </summary>
        /// <param name="distanceMax">Maximum distance in the units set in the InitParameters used in Init().</param>
        public void SetDepthMaxRangeValue(float distanceMax)
        {
            dllz_set_depth_max_range_value(distanceMax);
        }

        /// <summary>
        /// Returns the current camera FPS. This is limited primarily by resolution but can also be lower due to 
        /// setting a lower desired resolution in Init() or from USB connection/bandwidth issues. 
        /// </summary>
        /// <returns>The current fps</returns>
        public float GetCameraFPS()
        {
            return dllz_get_camera_fps();
        }



		public CalibrationParameters GetCalibrationParameters(bool raw=false)
		{

			IntPtr p = dllz_get_calibration_parameters(raw);

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

		/// <summary>
		/// Gets the ZED camera model (ZED or ZED Mini).
		/// </summary>
		/// <returns>Model of the ZED as sl.MODEL.</returns>
		public sl.MODEL GetCameraModel()
		{
			return (sl.MODEL)dllz_get_camera_model ();
		}

        /// <summary>
        /// Gets the ZED's firmware version. 
        /// </summary>
        /// <returns>Firmware version.</returns>
        public int GetZEDFirmwareVersion()
        {
            return dllz_get_zed_firmware();
        }

		/// <summary>
		/// Gets the ZED's serial number. 
		/// </summary>
		/// <returns>Serial number</returns>
		public int GetZEDSerialNumber()
		{
			return dllz_get_zed_serial();
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
        /// Gets the status of the ZED's self-calibration process (not called, running, failed or success). 
        /// </summary>
        /// <returns>Self-calibration status.</returns>
        public ZED_SELF_CALIBRATION_STATE GetSelfCalibrationStatus()
        {
            return (ZED_SELF_CALIBRATION_STATE)dllz_get_self_calibration_state();
        }

        /// <summary>
        /// Computes textures from the ZED. The new textures will not be displayed until an event is sent to the render thread.
        /// This event is called from UpdateTextures().
        /// </summary>
        public void RetrieveTextures()
        {
            dllz_retrieve_textures();
        }

		/// <summary>
		/// Swaps textures safely between the acquisition and rendering threads.
		/// </summary>
		public void SwapTextures()
		{
			dllz_swap_textures();
		}

        /// <summary>
        /// Timestamp of the images used the last time the ZED wrapper updated textures. 
        /// </summary>
        /// <returns></returns>
		public ulong GetImagesTimeStamp()
		{
			return dllz_get_updated_textures_timestamp();
		}

        /// <summary>
        /// Gets the number of frames dropped since Grab() was called for the first time.
        /// Based on camera timestamps and an FPS comparison. 
        /// </summary><remarks>Similar to the Frame Drop display in the ZED Explorer app.</remarks>
        /// <returns>Frames dropped since first Grab() call.</returns>
        public uint GetFrameDroppedCount()
        {
            return dllz_get_frame_dropped_count();
        }

		/// <summary>
		/// Gets the percentage of frames dropped since Grab() was called for the first time.
		/// </summary>
		/// <returns>Percentage of frames dropped.</returns>
		public float GetFrameDroppedPercent()
		{
			return dllz_get_frame_dropped_percent ();
		}

        /// <summary>
        /// Gets the position of the camera and the current state of the ZED Tracking.
        /// </summary>
        /// <param name="rotation">Quaternion filled with the current rotation of the camera depending on its reference frame.</param>
        /// <param name="position">Vector filled with the current position of the camera depending on its reference frame.</param>
        /// <param name="referenceType">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 position, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position(ref rotation, ref position, (int)referenceType);
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
        public TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation, REFERENCE_FRAME referenceFrame = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position_at_target_frame(ref rotation, ref translation, ref targetQuaternion, ref targetTranslation, (int)referenceFrame);
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
        public TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 translation, TRACKING_FRAME trackingFrame, REFERENCE_FRAME referenceFrame = REFERENCE_FRAME.WORLD)
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

			return (TRACKING_STATE)dllz_get_position_at_target_frame(ref rotation, ref translation, ref rotationOffset, ref positionOffset, (int)referenceFrame);
		}

        /// <summary>
        /// Gets the current position of the camera and state of the tracking, filling a Pose struct useful for AR pass-through. 
        /// </summary>
        /// <param name="pose">Current pose.</param>
        /// <param name="referenceType">Reference frame for setting the rotation/position. CAMERA gives movement relative to the last pose.
        /// WORLD gives cumulative movements since tracking started.</param>
        /// <returns>State of ZED's Tracking system (off, searching, ok).</returns>
        public TRACKING_STATE GetPosition(ref Pose pose, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position_data(ref pose, (int)referenceType);
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
			trackingStatus = (sl.ERROR_CODE)dllz_set_imu_prior_orientation(rotation);
			return trackingStatus;
		}

		/// <summary>
		/// Gets the rotation given by the ZED Mini's IMU. Doesn't work if using the original ZED. 
		/// </summary>
		/// <returns>Error code status.</returns>
		/// <param name="rotation">Rotation from the IMU.</param>
		public ERROR_CODE GetInternalIMUOrientation(ref Quaternion rotation, TIME_REFERENCE referenceTime = TIME_REFERENCE.IMAGE)
		{
			sl.ERROR_CODE err = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			err = (sl.ERROR_CODE)dllz_get_internal_imu_orientation(ref rotation,(int)referenceTime);
			return err;
		}

		/// <summary>
		/// Gets the full IMU data (raw value and fused values) from the ZED Mini. Doesn't work if using the original ZED. 
		/// </summary>
		/// <returns>Error code status.</returns>
		/// <param name="rotation">Rotation from the IMU.</param>
		public ERROR_CODE GetInternalIMUData(ref IMUData data, TIME_REFERENCE referenceTime = TIME_REFERENCE.IMAGE)
		{
			sl.ERROR_CODE err = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			err = (sl.ERROR_CODE)dllz_get_internal_imu_data(ref data,(int)referenceTime);
			return err;
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
        /// Sets a value in the ZED's camera settings.
        /// </summary>
        /// <param name="settings">Setting to be changed (brightness, contrast, gain, exposure, etc.)</param>
        /// <param name="value">New value.</param>
        /// <param name="usedefault">If true, ignores the value and applies the default setting.</param>
        public void SetCameraSettings(CAMERA_SETTINGS settings, int value, bool usedefault = false)
        {
            cameraSettingsManager.SetCameraSettings(settings, value, usedefault);
        }

        /// <summary>
        /// Gets the value of a given setting from the ZED camera. 
        /// </summary>
        /// <param name="settings">Setting to be retrieved (brightness, contrast, gain, exposure, etc.)</param>
        public int GetCameraSettings(CAMERA_SETTINGS settings)
        {
            AssertCameraIsReady();
            return cameraSettingsManager.GetCameraSettings(settings);
        }

        /// <summary>
        /// Loads camera settings (brightness, contrast, hue, saturation, gain, exposure) from a file in the
        /// project's root directory.
        /// </summary>
        /// <param name="path">Filename.</param>
        public void LoadCameraSettings(string path)
        {
            cameraSettingsManager.LoadCameraSettings(instance, path);
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
            cameraSettingsManager.RetrieveSettingsCamera(instance);
        }

        /// <summary>
        /// Returns a copy of the camera settings from ZEDCameraSettingsManager. Modifying this copy
        /// has no effect on the camera or ZEDCameraSettingsManager.
        /// </summary>
        /// <returns></returns>
        public ZEDCameraSettingsManager.CameraSettings GetCameraSettings()
        {
            return cameraSettingsManager.Settings;
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
            cameraSettingsManager.SetSettings(instance);
        }

        /// <summary>
        /// Checks if the ZED camera is connected. 
        /// </summary>
        /// <remarks>The C++ SDK version of this call returns the number of connected ZEDs. But multiple ZEDs aren't supported in the Unity plugin.</remarks>
        /// <returns>True if ZED is connected.</returns>
        public static bool IsZedConnected()
        {
            return Convert.ToBoolean(dllz_is_zed_connected());
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
			float d = dllz_get_depth_value((uint)posX, (uint)posY);
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

			return dllz_get_distance_value((uint)posX, (uint)posY);
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
			bool r = dllz_get_xyz_value((uint)posX, (uint)posY, out xyz) != 0;
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

			bool r = dllz_get_normal_value((uint)posX, (uint)posY, out normal) != 0;
			return r;
        }

        /// <summary>
        /// Initializes and begins the spatial mapping processes.
        /// </summary>
        /// <param name="resolution_meter">Spatial mapping resolution in meters.</param>
        /// <param name="max_range_meter">Maximum scanning range in meters.</param>
        /// <param name="saveTexture">True to scan surface textures in addition to geometry.</param>
        /// <returns></returns>
        public sl.ERROR_CODE EnableSpatialMapping(float resolution_meter, float max_range_meter, bool saveTexture = false)
        {
            sl.ERROR_CODE spatialMappingStatus = ERROR_CODE.FAILURE;
			lock (ZEDManager.Instance.grabLock)
            {
                spatialMappingStatus = (sl.ERROR_CODE)dllz_enable_spatial_mapping(resolution_meter, max_range_meter, System.Convert.ToInt32(saveTexture));
            }
            return spatialMappingStatus;
        }

        /// <summary>
        /// Disables the Spatial Mapping process.
        /// </summary>
        public void DisableSpatialMapping()
        {
			lock (ZEDManager.Instance.grabLock)
            {
                dllz_disable_spatial_mapping();
            }
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
            err = (sl.ERROR_CODE)dllz_update_mesh(nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);
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
            return (sl.ERROR_CODE)dllz_retrieve_mesh(vertices, triangles, nbSubmeshMax, uvs, textures);
        }

        /// <summary>
        /// Starts the mesh generation process in a thread that doesn't block the spatial mapping process.
        /// ZEDSpatialMappingHelper calls this each time it has finished applying the last mesh update. 
        /// </summary>
        public void RequestMesh()
        {
            dllz_request_mesh_async();
        }

        /// <summary>
        /// Sets the pause state of the data integration mechanism for the ZED's spatial mapping.
        /// </summary>
        /// <param name="status">If true, the integration is paused. If false, the spatial mapping is resumed.</param>
        public void PauseSpatialMapping(bool status)
        {
            dllz_pause_spatial_mapping(status);
        }

        /// <summary>
        /// Returns the mesh generation status. Useful for knowing when to update and retrieve the mesh.
        /// </summary>
        public sl.ERROR_CODE GetMeshRequestStatus()
        {
            return (sl.ERROR_CODE)dllz_get_mesh_request_status_async();
        }

        /// <summary>
        /// Saves the scanned mesh in a specific file format.
        /// </summary>
        /// <param name="filename">Path and filename of the mesh.</param>
        /// <param name="format">File format (extension). Can be .obj, .ply or .bin.</param>
        public bool SaveMesh(string filename, MESH_FILE_FORMAT format)
        {
            return dllz_save_mesh(filename, format);
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
            return dllz_load_mesh(filename, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, 
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
            return dllz_filter_mesh(filterParameters, nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);
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
            return dllz_apply_texture(nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, textureSize, nbSubmeshMax);
        }

        /// <summary>
        /// Gets the current state of spatial mapping.
        /// </summary>
        /// <returns></returns>
        public SPATIAL_MAPPING_STATE GetSpatialMappingState()
        {
            return (sl.SPATIAL_MAPPING_STATE)dllz_get_spatial_mapping_state();
        }

        /// <summary>
        /// Gets a vector pointing toward the direction of gravity. This is estimated from a 3D scan of the environment,
        /// and as such, a scan must be started/finished for this value to be calculated. 
        /// If using the ZED Mini, this isn't required thanks to its IMU. 
        /// </summary>
        /// <returns>Vector3 pointing downward.</returns>
        public Vector3 GetGravityEstimate()
        {
            Vector3 v = Vector3.zero;
            dllz_spatial_mapping_get_gravity_estimation(ref v);
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
            dllz_spatial_mapping_merge_chunks(numberFaces, nbVerticesInSubmeshes, nbTrianglesInSubmeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmesh);
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
            return (sl.ERROR_CODE)(dllz_retrieve_measure(mat.MatPtr, (int)measure, (int)mem, resolution));
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
            return (sl.ERROR_CODE)(dllz_retrieve_image(mat.MatPtr, (int)view, (int)mem, resolution));
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
            sl.Resolution imageResolution = new sl.Resolution((uint)instance.ImageWidth, (uint)instance.ImageHeight);


            Vector4 calibLeft = new Vector4(calib.leftCam.fx, calib.leftCam.fy, calib.leftCam.cx, calib.leftCam.cy);
            Vector4 calibRight = new Vector4(calib.rightCam.fx, calib.rightCam.fy, calib.rightCam.cx, calib.rightCam.cy);

            p = dllz_compute_optical_center_offsets(ref calibLeft, ref calibRight, imageResolution, planeDistance);

            if (p == IntPtr.Zero)
            {
                return new Vector4();
            }
            Vector4 parameters = (Vector4)Marshal.PtrToStructure(p, typeof(Vector4));
            return parameters;

        }

        ///
        /// Plane Detection
        /// 


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
			IntPtr p =  IntPtr.Zero;
			Quaternion out_quat = Quaternion.identity;
			Vector3 out_trans= Vector3.zero;
			p = dllz_find_floor_plane (out out_quat, out out_trans, priorTrans, priorTrans);
			plane.Bounds = new Vector3[256];
			playerHeight = 0;

			if (p != IntPtr.Zero) {
				plane = (ZEDPlaneGameObject.PlaneData)Marshal.PtrToStructure (p, typeof(ZEDPlaneGameObject.PlaneData));
				playerHeight = out_trans.y;
				return (sl.ERROR_CODE)plane.ErrorCode;
			} else
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
			return dllz_convert_floorplane_to_mesh (vertices, triangles,out numVertices,out numTriangles);
		}

        /// <summary>
        /// Checks for a plane in the real world at given screen-space coordinates.
        /// Use ZEDPlaneDetectionManager.DetectPlaneAtHit() for a higher-level version that turns planes into GameObjects.
        /// </summary>
        /// <param name="plane">Data on the detected plane.</param>
        /// <param name="screenPos">Point on the ZED image to check for a plane.</param>
        /// <returns></returns>
		public sl.ERROR_CODE findPlaneAtHit(ref ZEDPlaneGameObject.PlaneData plane, Vector2 screenPos)
		{
			IntPtr p =  IntPtr.Zero;
			Quaternion out_quat = Quaternion.identity;
			Vector3 out_trans= Vector3.zero;

			float posX = (float)ImageWidth * (float)((float)screenPos.x / (float)Screen.width);
			float posY = ImageHeight * (1 - (float)screenPos.y / (float)Screen.height);
			posX = Mathf.Clamp(posX, 0, ImageWidth);
			posY = Mathf.Clamp(posY, 0, ImageHeight);

			p = dllz_find_plane_at_hit (new Vector2(posX,posY),true);
			plane.Bounds = new Vector3[256];

			if (p != IntPtr.Zero) {
				plane = (ZEDPlaneGameObject.PlaneData)Marshal.PtrToStructure (p, typeof(ZEDPlaneGameObject.PlaneData));
				return (sl.ERROR_CODE)plane.ErrorCode;
			} else
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
		public int convertHitPlaneToMesh(Vector3[] vertices, int[] triangles,out int numVertices, out int numTriangles)
		{
			return dllz_convert_hitplane_to_mesh (vertices, triangles,out numVertices,out numTriangles);
		}

    }
} // namespace sl
