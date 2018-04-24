//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;


namespace sl
{
    /*
     * Unity representation of a sl::Camera
     */
	public class ZEDCamera
    {
        /// <summary>
        /// Type of textures requested
        /// </summary>
        public enum TYPE_VIEW
        {
            RETRIEVE_IMAGE,
            RETRIEVE_MEASURE
        }

        /// <summary>
        /// Informations of requested textures
        /// </summary>
        private struct TextureRequested
        {
            public int type;
            public int option;
        };

        /********* Camera members ********/

        //DLL name
        const string nameDll = "sl_unitywrapper";

        //List of all textures using the type as unique key
        private Dictionary<int, Dictionary<int, Texture2D>> textures;

        //List of all requested textures
        private List<TextureRequested> texturesRequested;

        //Width of the textures
        private int imageWidth;

        //Height of the textures
        private int imageHeight;

        //ZED projection matrix
        private Matrix4x4 projection = new Matrix4x4();

        //Singleton of ZEDCamera
        private static ZEDCamera instance = null;

        //True if the SDK is installed
        private static bool pluginIsReady = true;

        //Mutex for threaded rendering
        private static object _lock = new object();

        //The current resolution
        private RESOLUTION currentResolution;

        //Callback for c++ debug, should not be used in C#
        private delegate void DebugCallback(string message);

        //HD720 default FPS
        private uint fpsMax = 60;
        private ZEDCameraSettingsManager cameraSettingsManager = new ZEDCameraSettingsManager();


        // Baseline of the camera
        private float baseline = 0.0f;
        public float Baseline
        {
            get { return baseline; }
        }
        /// <summary>
        /// Current field of view
        /// </summary>
        private float fov_H = 0.0f;
		private float fov_V = 0.0f;

        /// <summary>
        /// Current field of view
        /// </summary>
        public float HorizontalFieldOfView
        {
			get { return fov_H; }
        }

		public float VerticalFieldOfView
		{
			get { return fov_V; }
		}
        /// <summary>
        /// Information in cache
        /// </summary>
		private CalibrationParameters calibrationParametersRaw;
		private CalibrationParameters calibrationParametersRectified;

		/// <summary>
		/// Camera Model
		/// </summary>
		private sl.MODEL cameraModel;

		/// <summary>
		/// Camera is opened or not
		/// </summary>
		private bool cameraReady = false;

        /// <summary>
        /// Information in cache, call GetInformation to update
        /// </summary>
		public CalibrationParameters CalibrationParametersRaw
        {
			get { return calibrationParametersRaw; }
        }
		/// <summary>
		/// Information in cache, call GetInformation to update
		/// </summary>
		public CalibrationParameters CalibrationParametersRectified
		{
			get { return calibrationParametersRectified; }
		}
		/// <summary>
		/// Information in cache, call GetInformation to update
		/// </summary>
		public sl.MODEL CameraModel
		{
			get { return cameraModel; }
		}

		public bool IsCameraReady
		{
			get { return cameraReady; }
		}


		public bool IsHmdCompatible
		{
			get { return cameraModel == sl.MODEL.ZED_M; }
		}
        /// <summary>
        /// DLL needed to run the exe
        /// </summary>
        static private string[] dependenciesNeeded =
        {
            "sl_zed64.dll",
            "sl_core64.dll",
            "sl_input64.dll"
		};


        const int tagZEDCamera = 20;
        /// <summary>
        /// Layer only the ZED is able to see
        /// </summary>
        public static int Tag
        {
            get { return tagZEDCamera; }
        }
        /// <summary>
        /// Layer only the other cameras apart the ZED are able to see
        /// </summary>
        const int tagOneObject = 12;
        public static int TagOneObject
        {
            get { return tagOneObject; }
        }

		/// <summary>
		/// Cuurent Plugin Version
		/// </summary>
		public static readonly System.Version PluginVersion = new System.Version(2, 4, 0);

        /******** DLL members ***********/

        [DllImport(nameDll, EntryPoint = "GetRenderEventFunc")]
        private static extern IntPtr GetRenderEventFunc();

        [DllImport(nameDll, EntryPoint = "dllz_register_callback_debuger")]
        private static extern void dllz_register_callback_debuger(DebugCallback callback);


        /*
          * Create functions
          */
        [DllImport(nameDll, EntryPoint = "dllz_create_camera")]
		private static extern System.IntPtr dllz_create_camera(bool verbose);


        /*
        * Opening function (Open camera and create textures)
        */
        [DllImport(nameDll, EntryPoint = "dllz_open")]
        private static extern int dllz_open(ref dll_initParameters parameters, System.Text.StringBuilder svoPath, System.Text.StringBuilder output);




        /*
         * Close function
         */
        [DllImport(nameDll, EntryPoint = "dllz_close")]
        private static extern void dllz_close();


        /*
               * Grab function
               */
        [DllImport(nameDll, EntryPoint = "dllz_grab")]
        private static extern int dllz_grab(ref sl.RuntimeParameters runtimeParameters);



        /*
        * Recording functions
        */
        [DllImport(nameDll, EntryPoint = "dllz_enable_recording")]
        private static extern int dllz_enable_recording(byte[] video_filename, int compresssionMode);

        [DllImport(nameDll, EntryPoint = "dllz_record")]
        private static extern void dllz_record(ref Recording_state state);

        [DllImport(nameDll, EntryPoint = "dllz_disable_recording")]
        private static extern bool dllz_disable_recording();


        /*
        * Texturing functions
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
        * Self-calibration function
        */
        [DllImport(nameDll, EntryPoint = "dllz_reset_self_calibration")]
        private static extern void dllz_reset_self_calibration();

        [DllImport(nameDll, EntryPoint = "dllz_get_self_calibration_state")]
        private static extern int dllz_get_self_calibration_state();


        /*
         * Camera control functions
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
         * SVO control functions
         */

        [DllImport(nameDll, EntryPoint = "dllz_set_svo_position")]
        private static extern void dllz_set_svo_position(int frame);

        [DllImport(nameDll, EntryPoint = "dllz_get_svo_number_of_frames")]
        private static extern int dllz_get_svo_number_of_frames();

        [DllImport(nameDll, EntryPoint = "dllz_get_svo_position")]
        private static extern int dllz_get_svo_position();


        /*
         * Depth Sensing utils functions
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
         * Motion Tracking functions
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


        [DllImport(nameDll, EntryPoint = "dllz_get_area_export_state")]
        private static extern int dllz_get_area_export_state();
        /*
        * Spatial Mapping functions
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
        /// Return a string from a pointer to char
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns>the string</returns>
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
        /// Display a console message from c++
        /// </summary>
        /// <param name="message"></param>
        private static void DebugMethod(string message)
        {
            Debug.Log("[ZED plugin]: " + message);
        }

        /// <summary>
        /// Convert a pointer to char to an array of bytes
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns>The array</returns>
        private static byte[] StringUtf8ToByte(string str)
        {
            byte[] array = System.Text.Encoding.ASCII.GetBytes(str);
            return array;
        }

        /// <summary>
        /// Get the max fps for each resolution, higher fps will cause lower GPU performance
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
        /// Get a quaternion from a matrix with a minimum size of 3x3
        /// </summary>
        /// <param name="m">The matrix </param>
        /// <returns>A quaternion which contains the rotation</returns>
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
        /// Rigid transform
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
        /// Check if the plugin is available
        /// </summary>
        public static bool CheckPlugin()
        {
			pluginIsReady = false;
			string env = Environment.GetEnvironmentVariable("ZED_SDK_ROOT_DIR");
			if (env != null)
			{
				bool error = CheckDependencies(System.IO.Directory.GetFiles(env + "\\bin"));
				if (error) {
					Debug.LogError (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.SDK_NOT_INSTALLED));
					return false;
				} else {

					try {
						if (dllz_check_plugin (PluginVersion.Major, PluginVersion.Minor) != 0) {
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
			else
			{
				Debug.LogError(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_NOT_INSTALLED));
				return false;
			}


			return true;
         }

        /// <summary>
        /// Checks if all the dlls ara available and try to call a dummy function from the DLL
        /// </summary>
        /// <param name="filesFound"></param>
        /// <returns></returns>
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
        /// Gets an instance of the ZEDCamera
        /// </summary>
        /// <returns>The instance</returns>
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
        /// Private constructor
        /// </summary>
        private ZEDCamera()
        {
            //Create the textures
            textures = new Dictionary<int, Dictionary<int, Texture2D>>();
            texturesRequested = new List<TextureRequested>();
        }

        /// <summary>
        /// Create a camera in live mode
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="fps"></param>
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
        /// Close the camera and delete all textures
        /// Once destroyed, you need to recreate a camera to restart again
        /// </summary>
        public void Destroy()
        {

			if (instance != null) {
				cameraReady = false;
				dllz_close ();
				DestroyAllTexture ();
				instance = null;
			}
        }



        [StructLayout(LayoutKind.Sequential)]
        public struct dll_initParameters
        {
            public sl.RESOLUTION resolution;
            public int cameraFps;
            public int cameraLinuxID;
            [MarshalAs(UnmanagedType.U1)]
            public bool svoRealTimeMode;
            public UNIT coordinateUnit;
            public COORDINATE_SYSTEM coordinateSystem;
            public sl.DEPTH_MODE depthMode;
            public float depthMinimumDistance;
            [MarshalAs(UnmanagedType.U1)]
            public bool cameraImageFlip;
            [MarshalAs(UnmanagedType.U1)]
            public bool enableRightSideMeasure;
            [MarshalAs(UnmanagedType.U1)]
            public bool cameraDisableSelfCalib;
            public int cameraBufferCountLinux;
            [MarshalAs(UnmanagedType.U1)]
            public bool sdkVerbose;
            public int sdkGPUId;
            [MarshalAs(UnmanagedType.U1)]
            public bool depthStabilization;

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
        /// The Init function must be called after the instantiation. The function checks if the ZED camera is plugged and opens it, initialize the projection matix and command buffers to update textures
        /// </summary>
        /// <param name="mode_">defines the quality of the depth map, affects the level of details and also the computation time.</param>
        /// <param name="minDist_">specify the minimum depth information that will be computed, in the unit you previously define.</param>
        /// <param name="self_calib">if set to true, it will disable self-calibration and take the initial calibration parameters without optimizing them</param>
        /// <returns>ERROR_CODE : The error code gives information about the
        /// internal process, if SUCCESS is returned, the camera is ready to use.
        /// Every other code indicates an error and the program should be stopped.
        ///
        /// For more details see sl::zed::ERRCODE.</returns>
        public ERROR_CODE Init(ref InitParameters initParameters)
        {

			currentResolution = initParameters.resolution;
			fpsMax = GetFpsForResolution(currentResolution);
            if (initParameters.cameraFPS == 0)
            {
                initParameters.cameraFPS = (int)fpsMax;
            }

            dll_initParameters initP = new dll_initParameters(initParameters);
            initP.coordinateSystem = COORDINATE_SYSTEM.LEFT_HANDED_Y_UP;

			initParameters.sdkVerboseLogFile = "D:/tmp/sdl_log.txt";
            int v = dllz_open(ref initP, new System.Text.StringBuilder(initParameters.pathSVO, initParameters.pathSVO.Length),
               new System.Text.StringBuilder(initParameters.sdkVerboseLogFile, initParameters.sdkVerboseLogFile.Length));

			if ((ERROR_CODE)v != ERROR_CODE.SUCCESS)
            {
				cameraReady = false;
				return (ERROR_CODE)v;
            }

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
        /// Fill the projection matrix with the parameters of the ZED, needs to be called only once. This projection matrix is off center.
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


            //Debug.Log(parameters.leftCam.cx);
            projection[0, 0] = 1.0f / Mathf.Tan(fovx * 0.5f);
            projection[0, 1] = 0;
			projection[0, 2] = 2.0f * ((f_imageWidth - 1.0f * parameters.leftCam.cx) / f_imageWidth) - 1.0f;
            projection[0, 3] = 0;

            projection[1, 0] = 0;
            projection[1, 1] = 1.0f / Mathf.Tan(fovy * 0.5f);
			projection[1, 2] = -(2.0f * ((f_imageHeight - 1.0f * parameters.leftCam.cy) / f_imageHeight) - 1.0f);
            projection[1, 3] = 0;

            projection[2, 0] = 0;
            projection[2, 1] = 0;
            projection[2, 2] = -(zFar + zNear) / (zFar - zNear);
            projection[2, 3] = -(2.0f * zFar * zNear) / (zFar - zNear);

            projection[3, 0] = 0;
            projection[3, 1] = 0;
            projection[3, 2] = -1;
            projection[3, 3] = 0.0f;

        }

        /// <summary>
        /// Grab a new image, rectifies it and computes the
        /// disparity map and optionally the depth map.
        /// The grabbing function is typically called in the main loop.
        /// </summary>
        /// <param name="sensingMode">defines the type of disparity map, more info : SENSING_MODE definition</param>
        /// <returns>the function returns false if no problem was encountered,
        /// true otherwise.</returns>
        public sl.ERROR_CODE Grab(ref sl.RuntimeParameters runtimeParameters)
        {
            return (sl.ERROR_CODE)dllz_grab(ref runtimeParameters);
        }


        /// <summary>
        ///  The reset function can be called at any time AFTER the Init function has been called.
        ///  It will reset and calculate again correction for misalignment, convergence and color mismatch.
        ///  It can be called after changing camera parameters without needing to restart your executable.
        /// </summary>
        ///
        /// <returns>ERRCODE : error boolean value : the function returns false if no problem was encountered,
        /// true otherwise.
        /// if no problem was encountered, the camera will use new parameters. Otherwise, it will be the old ones
        ///</returns>
        public void ResetSelfCalibration()
        {
            dllz_reset_self_calibration();
        }

        /// <summary>
        /// Creates a file for recording the current frames.
        /// </summary>
        /// <param name="videoFileName">can be a *.svo file or a *.avi file (detected by the suffix name provided)</param>
        /// <param name="compressionMode">can be one of the sl.SVO_COMPRESSION_MODE enum</param>
        /// <returns>an sl.ERRCODE that defines if file was successfully created and can be filled with images</returns>
        public ERROR_CODE EnableRecording(string videoFileName, SVO_COMPRESSION_MODE compressionMode = SVO_COMPRESSION_MODE.LOSSLESS_BASED)
        {
            return (ERROR_CODE)dllz_enable_recording(StringUtf8ToByte(videoFileName), (int)compressionMode);
        }

        /// <summary>
        /// Record the images, EnableRecording needs to be called before.
        /// </summary>
        public Recording_state Record()
        {
            Recording_state state = new Recording_state();
            dllz_record(ref state);
            return state;
        }

        /// <summary>
        /// Stops the recording and closes the file.
        /// </summary>
        /// <returns></returns>
        public bool DisableRecording()
        {
            return dllz_disable_recording();
        }

        /// <summary>
        /// Set a new frame rate for the camera, or the closest available frame rate.
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public void SetCameraFPS(int fps)
        {
            if (GetFpsForResolution(currentResolution) >= fps)
            {
                fpsMax = (uint)fps;
            }

            dllz_set_camera_fps(fps);
        }

        /// <summary>
        /// Sets the position of the SVO file to a desired frame.
        /// </summary>
        /// <param name="frame"> the number of the desired frame to be decoded.</param>
        /// <returns></returns>
        public void SetSVOPosition(int frame)
        {
            dllz_set_svo_position(frame);
        }

        /// <summary>
        /// Gets the current confidence threshold value for the disparity map (and by extension the depth map).
        /// </summary>
        /// <returns>current filtering value between 0 and 100.</returns>
        public int GetConfidenceThreshold()
        {
            return dllz_get_confidence_threshold();
        }

        /// <summary>
        /// Get the time stamp at the time the frame has been extracted from USB stream. (should be called after a grab())
        /// </summary>
        /// <returns>Current time stamp in ns. -1 is not available(SVO file without compression).
        /// Note that new SVO file from SDK 1.0.0 (with compression) contains the camera time stamp for each frame.</returns>
        public ulong GetCameraTimeStamp()
        {
            return dllz_get_camera_timestamp();
        }

        /// <summary>
        /// Get the current time stamp at the time the function is called. Can be compared to the camera time stamp for synchronization.
        /// Use this function to compare the current time stamp and the camera time stamp, since they have the same reference (Computer start time).
        /// </summary>
        /// <returns>The timestamp</returns>
        public ulong GetCurrentTimeStamp()
        {
            return dllz_get_current_timestamp();
        }

        /// <summary>
        /// Last time stamp from image update, (based on the computer time stamp)
        /// </summary>
        /// <returns>The timestamp</returns>
        public ulong GetImageUpdaterTimeStamp()
        {
            return dllz_get_image_updater_time_stamp();
        }

        /// <summary>
        /// Get the current position of the SVO in the record
        /// </summary>
        /// <returns>The position</returns>
        public int GetSVOPosition()
        {
            return dllz_get_svo_position();
        }

        /// <summary>
        /// Get the number of frames in the SVO file.
        /// </summary>
        /// <returns>SVO Style Only : the total number of frames in the SVO file(-1 if the SDK is not reading a SVO)</returns>
        public int GetSVONumberOfFrames()
        {
            return dllz_get_svo_number_of_frames();
        }

        /// <summary>
        /// Get the closest measurable distance by the camera, according to the camera and the depth map parameters.
        /// </summary>
        /// <returns>The closest depth</returns>
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
        /// <param name="quat"> rotation used as initial world transform.By default it should be identity.</param>
        /// <param name="vec"> translation used as initial world transform. By default it should be identity.</param>
        /// <param name="enableSpatialMemory">  (optional) define if spatial memory is enable or not.</param>
        /// <param name="areaFilePath"> (optional) file of spatial memory file that has to be loaded to relocate in the scene.</param>
        /// <returns></returns>
		public sl.ERROR_CODE EnableTracking(ref Quaternion quat, ref Vector3 vec, bool enableSpatialMemory = true,bool enablePoseSmoothing = false, string areaFilePath = "")
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
        /// Create or retrieve a texture of type Image (from Camera::retrieveImage()). Create the texture if needed.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
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
            if (!((uint)resolution.width == 0 && (uint)resolution.height == 0))
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
                m_Texture = new Texture2D(width * 2, height, TextureFormat.RGBA32, false);
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
                throw new Exception("Error Cuda " + error + " if the problem appears again, please contact the support");
            }
            if (!textures.ContainsKey((int)TYPE_VIEW.RETRIEVE_IMAGE))
            {
                textures.Add((int)TYPE_VIEW.RETRIEVE_IMAGE, new Dictionary<int, Texture2D>());
            }
            RegisterTexture(m_Texture, (int)TYPE_VIEW.RETRIEVE_IMAGE, (int)mode);

            return m_Texture;
        }

        /// <summary>
        /// Create or retrieve a texture of type Measure (from Camera::retrieveMeasure()). Create the texture if needed.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
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
                throw new Exception("Error Cuda " + error + " if the problem appears again, please contact the support");
            }
            if (!textures.ContainsKey((int)TYPE_VIEW.RETRIEVE_MEASURE))
            {
                textures.Add((int)TYPE_VIEW.RETRIEVE_MEASURE, new Dictionary<int, Texture2D>());
            }

            RegisterTexture(m_Texture, (int)TYPE_VIEW.RETRIEVE_MEASURE, (int)mode);

            return m_Texture;
        }

        /// <summary>
        /// Unregister a texture of type image, this texture will be destroyed and not be updated anymore
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool UnregisterTextureImageType(sl.VIEW view)
        {
            DestroyTextureImageType((int)view);
            return dllz_unregister_texture_image_type((int)view) != 0;
        }

        /// <summary>
        /// Unregister a texture of type measure, this texture will be destroyed and not be updated anymore
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool UnregisterTextureMeasureType(sl.MEASURE measure)
        {
            DestroyTextureMeasureType((int)measure);
            return dllz_unregister_texture_measure_type((int)measure) != 0;
        }

        /// <summary>
        /// Copy a Texture of type image to a Mat, this function should be called after a grab and a updateTexture
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public ZEDMat RequestCopyMatFromTextureImageType(sl.VIEW view)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_image_type((int)view));
        }

        /// <summary>
        /// Copy a Texture of type measure to a Mat, this function should be called after a grab and a updateTexture
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public ZEDMat RequestCopyMatFromTextureMeasureType(sl.MEASURE measure)
        {
            return new ZEDMat(dllz_get_copy_mat_texture_measure_type((int)measure));
        }

        /// <summary>
        /// Destroy a texture and free the texture.
        /// </summary>
        /// <param name="type">The type of texture</param>
        /// <param name="option">The options</param>
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
        /// Destroy all textures
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
        /// Destroy a texture created with CreateTextureImageType
        /// </summary>
        /// <param name="type">The option of the texture</param>
        private void DestroyTextureImageType(int option)
        {
            DestroyTexture((int)TYPE_VIEW.RETRIEVE_IMAGE, option);
        }

        /// <summary>
        /// Destroy a texture created with CreateTextureMeasureType
        /// </summary>
        /// <param name="type">The option of the texture</param>
        private void DestroyTextureMeasureType(int option)
        {
            DestroyTexture((int)TYPE_VIEW.RETRIEVE_MEASURE, option);
        }

        /// <summary>
        /// Retrieves a texture previously created
        /// </summary>
        /// <param name="type">The type of texture</param>
        /// <param name="mode">The mode</param>
        /// <returns>The texture</returns>
        public Texture2D GetTexture(TYPE_VIEW type, int mode)
        {
            if (HasTexture((int)type, mode))
            {
                return textures[(int)type][mode];
            }
            return null;
        }

        /// <summary>
        /// Check if a texture is available
        /// </summary>
        /// <param name="type">The type of texture</param>
        /// <param name="mode">The mode</param>
        /// <returns>True if the texture is available</returns>
        private bool HasTexture(int type, int mode)
        {
			if (cameraReady)
            {
                return textures.ContainsKey((int)type) && textures[type].ContainsKey((int)mode);
            }
            return false;
        }
        /// <summary>
        /// Width of the images returned by the ZED
        /// </summary>
        public int ImageWidth
        {
            get
            {
                return imageWidth;
            }
        }

        /// <summary>
        /// Returns the height of the image
        /// </summary>
        public int ImageHeight
        {
            get
            {
                return imageHeight;
            }
        }

        /// <summary>
        /// The projection which corresponds to the traits of the ZED
        /// </summary>
        public Matrix4x4 Projection
        {
            get
            {
                return projection;
            }
        }

        /// <summary>
        /// Sets a filtering value for the disparity map (and by extension the depth map). The function should be called before the grab to be taken into account.
        /// </summary>
        /// <param name="threshold"> a value in [1,100]. A lower value means more confidence and precision (but less density), an upper value reduces the filtering (more density, less certainty). Other value means no filtering.
        ///</param>
        public void SetConfidenceThreshold(int threshold)
        {
            dllz_set_confidence_threshold(threshold);
        }

        /// <summary>
        /// Set the maximum distance of depth/disparity estimation (all values after this limit will be reported as TOO_FAR value)
        /// </summary>
        /// <param name="distanceMax"> maximum distance in the defined UNIT</param>
        public void SetDepthMaxRangeValue(float distanceMax)
        {
            dllz_set_depth_max_range_value(distanceMax);
        }

        /// <summary>
        /// Returns the current fps
        /// </summary>
        /// <returns>The current fps</returns>
        public float GetCameraFPS()
        {
            return dllz_get_camera_fps();
        }

		public float GetRequestedCameraFPS()
		{
			return fpsMax;
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
		/// Gets the zed camera model
		/// </summary>
		/// <returns>The camera model (sl.MODEL)</returns>
		public sl.MODEL GetCameraModel()
		{
			return (sl.MODEL)dllz_get_camera_model ();
		}

        /// <summary>
        /// Gets the zed firmware
        /// </summary>
        /// <returns>The firmware</returns>
        public int GetZEDFirmwareVersion()
        {
            return dllz_get_zed_firmware();
        }

		/// <summary>
		/// Gets the zed firmware
		/// </summary>
		/// <returns>The serial number</returns>
		public int GetZEDSerialNumber()
		{
			return dllz_get_zed_serial();
		}


        /// <summary>
        /// Returns the vertical field of view in radians
        /// </summary>
        /// <returns>The field of view</returns>
        public float GetFOV()
        {
			return GetCalibrationParameters(false).leftCam.vFOV * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Gets the calibration status
        /// </summary>
        /// <returns>The calibration status</returns>
        public ZED_SELF_CALIBRATION_STATE GetSelfCalibrationStatus()
        {
            return (ZED_SELF_CALIBRATION_STATE)dllz_get_self_calibration_state();
        }


        /// <summary>
        /// Compute textures from the ZED, the new textures will not be displayed until an event is sent to the Render Thread.
        /// </summary>
        public void RetrieveTextures()
        {
            dllz_retrieve_textures();
        }


		/// <summary>
		/// swap textures between acquisition and rendering thread
		/// </summary>
		public void SwapTextures()
		{
			dllz_swap_textures();
		}


		public ulong GetImagesTimeStamp()
		{
			return dllz_get_updated_textures_timestamp();
		}

        /// <summary>
        /// Get the number of frame dropped since grab() has been called for the first time
        /// Based on camera time stamp and fps comparison.
        /// </summary>
        /// <returns>number	of frame dropped since first grab() call.</returns>
        public uint GetFrameDroppedCount()
        {
            return dllz_get_frame_dropped_count();
        }

		/// <summary>
		/// Get the percentage  of frame dropped since grab() has been called for the first time
		/// </summary>
		/// <returns>number (percentage) of frame dropped.</returns>
		public float GetFrameDroppedPercent()
		{
			return dllz_get_frame_dropped_percent ();
		}

        /// <summary>
        ///  Gets the position of the camera and the current state of the Tracker
        /// </summary>
        /// <param name="rotation">the quaternion will be filled with the current rotation of the camera depending on the reference</param>
        /// <param name="position">the vector will be filled with the current position of the camera depending on the reference</param>
        /// <param name="referenceType">The reference type will fill the quaternion and vector with either the diffrences between the last pose(CAMERA) or the cumul of poses (WORLD)</param>
        /// <returns>A tracking frame state</returns>
        public TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 position, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position(ref rotation, ref position, (int)referenceType);
        }

        /// <summary>
        /// Get the current position of the camera with an optionnal transformation of the camera frame/motion tracking frame.
        ///
        /// </summary>
        /// <param name="rotation">the quaternion will be filled with the current rotation of the camera depending on the reference</param>
        /// <param name="position">the vector will be filled with the current position of the camera depending on the reference</param>
        /// <param name="targetQuaternion"></param>
        /// <param name="targetTranslation"></param>
        /// <param name="mat_type"></param>
        /// <returns></returns>
        public TRACKING_STATE GetPosition(ref Quaternion rotation, ref Vector3 translation, ref Quaternion targetQuaternion, ref Vector3 targetTranslation, REFERENCE_FRAME mat_type = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position_at_target_frame(ref rotation, ref translation, ref targetQuaternion, ref targetTranslation, (int)mat_type);
        }

		/// <summary>
        /// Get position and fill a pose
        /// </summary>
        /// <param name="pose"> current pose</param>
        /// <param name="referenceType">reference frame for the tracking</param>
        /// <returns></returns>
        public TRACKING_STATE GetPosition(ref Pose pose, REFERENCE_FRAME referenceType = REFERENCE_FRAME.WORLD)
        {
            return (TRACKING_STATE)dllz_get_position_data(ref pose, (int)referenceType);
        }


		/// <summary>
		/// Set a prior to the IMU orientation (only for ZED-M). Prior must come from a external IMU such as the Hmd orientation and must be as much as possible in the same timeframe than the camera
		/// </summary>
		/// <returns>Error code status</returns>
		/// <param name="rotation">Rotation.</param>
		public ERROR_CODE SetIMUOrientationPrior(ref Quaternion rotation)
		{
			sl.ERROR_CODE trackingStatus = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			trackingStatus = (sl.ERROR_CODE)dllz_set_imu_prior_orientation(rotation);
			return trackingStatus;
		}


		/// <summary>
		/// Get the quaternion given by the IMU of the ZED-M (only available for ZED-M)
		/// </summary>
		/// <returns>Error code status</returns>
		/// <param name="rotation">Rotation.</param>
		public ERROR_CODE GetInternalIMUOrientation(ref Quaternion rotation, TIME_REFERENCE referenceTime = TIME_REFERENCE.IMAGE)
		{
			sl.ERROR_CODE err = sl.ERROR_CODE.CAMERA_NOT_DETECTED;
			err = (sl.ERROR_CODE)dllz_get_internal_imu_orientation(ref rotation,(int)referenceTime);
			return err;
		}


        /// <summary>
        /// Converts a float array to a matrix
        /// </summary>
        /// <param name="m"></param>
        /// <param name="f"></param>
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
        /// Set settings of the camera
        /// </summary>
        /// <param name="settings">The setting which will be changed</param>
        /// <param name="value">The value</param>
        /// <param name="usedefault">will set default (or automatic) value if set to true (value (int) will not be taken into account)</param>
        public void SetCameraSettings(CAMERA_SETTINGS settings, int value, bool usedefault = false)
        {
            cameraSettingsManager.SetCameraSettings(settings, value, usedefault);
        }

        /// <summary>
        /// Get the value from a setting of the camera
        /// </summary>
        /// <param name="settings"></param>
        public int GetCameraSettings(CAMERA_SETTINGS settings)
        {
            AssertCameraIsReady();
            return cameraSettingsManager.GetCameraSettings(settings);
        }

        /// <summary>
        /// Load the camera settings (brightness, contrast, hue, saturation, gain, exposure)
        /// </summary>
        /// <param name="path"></param>
        public void LoadCameraSettings(string path)
        {
            cameraSettingsManager.LoadCameraSettings(instance, path);
        }

        /// <summary>
        /// Save the camera settings (brightness, contrast, hue, saturation, gain, exposure)
        /// </summary>
        /// <param name="path"></param>
        public void SaveCameraSettings(string path)
        {
            cameraSettingsManager.SaveCameraSettings(path);
        }

        /// <summary>
        /// Retrirves camera settings from the camera
        /// </summary>
        public void RetrieveCameraSettings()
        {
            cameraSettingsManager.RetrieveSettingsCamera(instance);
        }

        /// <summary>
        /// Returns a copy of the camera settings, cannot be modified
        /// </summary>
        /// <returns></returns>
        public ZEDCameraSettingsManager.CameraSettings GetCameraSettings()
        {
            return cameraSettingsManager.Settings;
        }

        /// <summary>
        /// Return the state of the exposure (true = automatic, false = manual)
        /// </summary>
        /// <returns></returns>
        public bool GetExposureUpdateType()
        {
            return cameraSettingsManager.auto;
        }

        /// <summary>
        /// Return the state of the white balance (true = automatic, false = manual)
        /// </summary>
        /// <returns></returns>
        public bool GetWhiteBalanceUpdateType()
        {
            return cameraSettingsManager.whiteBalanceAuto;
        }

        /// <summary>
        /// Set all the settings registered, to the camera
        /// </summary>
        public void SetCameraSettings()
        {
            cameraSettingsManager.SetSettings(instance);
        }

        /// <summary>
        /// The function checks if ZED cameras are connected, can be called before instantiating a Camera object
        /// </summary>
        /// <remarks> On Windows, only one ZED is accessible so this function will return 1 even if multiple ZED are connected.</remarks>
        /// <returns>the number of connected ZED</returns>
        public static bool IsZedConnected()
        {
            return Convert.ToBoolean(dllz_is_zed_connected());
        }

        /// <summary>
        /// The function return the version of the currently installed ZED SDK
        /// </summary>
        /// <returns>ZED SDK version as a string with the following format : MAJOR.MINOR.PATCH</returns>
        public static string GetSDKVersion()
        {
            return PtrToStringUtf8(dllz_get_sdk_version());
        }

        /// <summary>
        /// Check if camera has been initialized, or if the plugin has been loaded
        /// </summary>
        private void AssertCameraIsReady()
        {
            if (!cameraReady || !pluginIsReady)
                throw new Exception("Camera is not connected, init was not called or a dependency problem occurred");
        }

        /// <summary>
        /// retrieve the images from the ZED and send an event to update the textures
        /// </summary>
        public void UpdateTextures()
        {
            //Retrieves the images from the ZED
            //RetrieveTextures();
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }


		///////////////////////////////////////////// SINGLE PIXEL UTILS FUNCTIONS ////////////////////////////////////////////////

        /// <summary>
        /// Get the current depth value in the UNIT
		/// <param name="position">The pixel as a vector3 where the component Z is unused, this position has to be in the screen dimensions "GetDepthValue(Input.mousePosition)" works only if the zed image fills the whole screen</param>
        /// <returns>the depth value as a float</returns>
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
		/// Get the current euclidean distance( sqrt(x²+y²+z²)) value of the targeted pixelof the screen to the camera.
		/// <param name="pixel">The pixel as a vector3 where the component Z is unused, this position has to be in the screen dimensions "GetDistancehValue(Input.mousePosition)" works only if the zed image fills the whole screen</param>
		/// <returns>the distance in float</returns>
		/// </summary>
		public float GetDistanceValue(Vector3 pixel)
		{
			if (!cameraReady)
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
		/// Get the (x,y,z) position of the pixel, relative to the camera frame
		/// <param name="pixel">The pixel as a vector3 where the component Z is unused, this position has to be in the screen dimensions "GetNormalValue(Input.mousePosition)" works only if the zed image fills the whole screen</param>
		/// <returns> true if success. xyz will be then filled with the appropriate values</returns>
		/// </summary>
		public bool GETXYZValue(Vector3 pixel, out Vector4 xyz)
		{

			if (!cameraReady)
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
        /// Get the current normal value in the UNIT
		/// The normal is in the camera frame. Use cam.worldToCameraMatrix.inverse to transform to world normals. Or use the ZEDSupportFunctions
		/// <param name="pixel">The pixel as a vector3, the component Z is unused, this position has to be in the screen dimensions "GetNormalValue(Input.mousePosition)" works only if the zed image fills the whole screen</param>
		/// <returns> true if success. normal will be then filled with the appropriate values</returns>
		/// </summary>
        public bool GetNormalValue(Vector3 pixel, out Vector4 normal)
        {

			if (!cameraReady)
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
        /// Initializes and starts the spatial mapping processes.
        /// </summary>
        /// <param name="resolution_meter">Spatial mapping resolution in meters</param>
        /// <param name="min_range_meter">Minimum depth integration range in meters</param>
        /// <param name="max_range_meter">Maximum depth integration range in meters</param>
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
        /// <returns></returns>
        public void DisableSpatialMapping()
        {
			lock (ZEDManager.Instance.grabLock)
            {
                dllz_disable_spatial_mapping();
            }
        }

        /// <summary>
        /// Update the internal version of the mesh and return the sizes of the meshes.
        /// </summary>
        /// <param name="nbVerticesInSubemeshes"> Vector containing the number of vertices in each submeshes. </param>
        /// <param name="nbTrianglesInSubemeshes"> Vector containing the number of triangles in each submeshes. </param>
        /// <param name="nbSubmeshes"> Number of submeshes. </param>
        /// <param name="updatedIndices"> List of the submeshes updated since the last update. </param>
        /// <param name="nbVertices"> Total number of updated vertices for all submeshes. </param>
        /// <param name="nbTriangles"> Total number of updated triangles for all submeshes. </param>
        /// <param name="nbSubmeshMax"> Maximal number of submeshes handle in the script. </param>
        /// <returns></returns>
        public sl.ERROR_CODE UpdateMesh(int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmeshMax)
        {
            sl.ERROR_CODE err = sl.ERROR_CODE.FAILURE;
            err = (sl.ERROR_CODE)dllz_update_mesh(nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);
            return err;
        }

        /// <summary>
        /// Retrieves all the chunks of the generated mesh. You need to call UpdateMesh first. vertices and triangles must be at least of the size returned by UpdateMesh (nbVertices and nbTriangles).
        /// </summary>
        /// <param name="vertices">Vertices are defined by a 3D point</param>
        /// <param name="triangles">Triangles (or faces) contains the index of its three vertices</param>
        /// <param name="nbSubmeshMax">Maximal number of submeshes handle in the script.</param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveMesh(Vector3[] vertices, int[] triangles, int nbSubmeshMax, Vector2[] uvs, IntPtr textures)
        {
            return (sl.ERROR_CODE)dllz_retrieve_mesh(vertices, triangles, nbSubmeshMax, uvs, textures);
        }

        /// <summary>
        /// Starts the mesh generation process in a non blocking thread from the spatial mapping process.
        /// </summary>
        public void RequestMesh()
        {
            dllz_request_mesh_async();
        }

        /// <summary>
        /// Switches the pause status of the data integration mechanism for the spatial mapping.
        /// </summary>
        /// <param name="status"> if true, the integration is paused. If false, the spatial mapping is resumed.</param>
        public void PauseSpatialMapping(bool status)
        {
            dllz_pause_spatial_mapping(status);
        }

        /// <summary>
        /// Returns the mesh generation status, useful to after calling requestMeshAsync.
        /// </summary>
        /// <returns> If true, the integration is paused. If false, the spatial mapping is resumed.</returns>
        public sl.ERROR_CODE GetMeshRequestStatus()
        {
            return (sl.ERROR_CODE)dllz_get_mesh_request_status_async();
        }

        /// <summary>
        /// Save the mesh in a specific file format
        /// </summary>
        /// <param name="filename">the path and filename of the mesh.</param>
        /// <param name="format">defines the file format (extension).</param>
        /// <returns></returns>
        public bool SaveMesh(string filename, MESH_FILE_FORMAT format)
        {
            return dllz_save_mesh(filename, format);
        }

        /// <summary>
        /// Loads a mesh
        /// </summary>
        /// <param name="filename"> The path and filename of the mesh (do not forget the extension). </param>
        /// <param name="nbVerticesInSubemeshes"> Vector containing the number of vertices in each submeshes. </param>
        /// <param name="nbTrianglesInSubemeshes"> Vector containing the number of triangles in each submeshes. </param>
        /// <param name="nbSubmeshes"> Number of submeshes. </param>
        /// <param name="updatedIndices"> List of the submeshes updated since the last update. </param>
        /// <param name="nbVertices"> Total number of updated vertices for all submeshes. </param>
        /// <param name="nbTriangles"> Total number of updated triangles for all submeshes. </param>
        /// <param name="nbSubmeshMax"> Maximal number of submeshes handle in the script. </param>
        /// <param name="textureSize"> Vector containing the size of all the texture (width, height). </param>
        /// <returns></returns>
        public bool LoadMesh(string filename, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmeshMax, int[] textureSize = null)
        {
            return dllz_load_mesh(filename, nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax, textureSize);
        }

        /// <summary>
        /// Filters a mesh; less triangles
        /// </summary>
        /// <param name="filterParameters"> Parameters for the optional filtering step of a mesh </param>
        /// <param name="nbVerticesInSubemeshes"> Vector containing the number of vertices in each submeshes after filtering. </param>
        /// <param name="nbTrianglesInSubemeshes"> Vector containing the number of triangles in each submeshes after filtering. </param>
        /// <param name="nbSubmeshes"> Number of submeshes after filtering. </param>
        /// <param name="updatedIndices"> List of the submeshes updated by the filter. </param>
        /// <param name="nbVertices"> Total number of updated vertices for all submeshes after filtering. </param>
        /// <param name="nbTriangles"> Total number of updated triangles for all submeshes after filtering. </param>
        /// <param name="nbSubmeshMax"> Maximal number of submeshes handle in the script. </param>
        /// <returns></returns>
        public bool FilterMesh(FILTER filterParameters, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmeshMax)
        {
            return dllz_filter_mesh(filterParameters, nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmeshMax);
        }

        /// Apply the texture on the internal mesh, you will need to call RetrieveMesh with uvs and textures to get the mesh
        /// </summary>
        /// <param name="nbVerticesInSubemeshes"> Vector containing the number of vertices in each textured submeshes. </param>
        /// <param name="nbTrianglesInSubemeshes"> Vector containing the number of triangles in each textured submeshes after filtering. </param>
        /// <param name="nbSubmeshes"> Number of submeshes after filtering. </param>
        /// <param name="updatedIndices"> List of the submeshes updated by the texture mapping. </param>
        /// <param name="nbVertices"> Total number of updated vertices for all submeshes. </param>
        /// <param name="nbTriangles"> Total number of updated triangles for all submeshes. </param>
        /// <param name="textureSize"> Vector containing the size of all the texture (width, height). </param>
        /// <param name="nbSubmeshMax"> Maximal number of submeshes handle in the script. </param>
        /// <returns></returns>
        public bool ApplyTexture(int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int[] textureSize, int nbSubmeshMax)
        {
            return dllz_apply_texture(nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, textureSize, nbSubmeshMax);
        }

        /// <summary>
        /// Get the current state of spatial mapping
        /// </summary>
        /// <returns></returns>
        public SPATIAL_MAPPING_STATE GetSpatialMappingState()
        {
            return (sl.SPATIAL_MAPPING_STATE)dllz_get_spatial_mapping_state();
        }

        public Vector3 GetGravityEstimate()
        {
            Vector3 v = Vector3.zero;
            dllz_spatial_mapping_get_gravity_estimation(ref v);
            return v;
        }

        /// <summary>
        /// Reshape the chunks, to get more mesh per chunks and less chunks
        /// </summary>
        /// <param name="numberFaces"></param>
        /// <param name="nbVerticesInSubemeshes"></param>
        /// <param name="nbTrianglesInSubemeshes"></param>
        /// <param name="nbSubmeshes"></param>
        /// <param name="updatedIndices"></param>
        /// <param name="nbVertices"></param>
        /// <param name="nbTriangles"></param>
        /// <param name="nbSubmesh"></param>
        public void MergeChunks(int numberFaces, int[] nbVerticesInSubemeshes, int[] nbTrianglesInSubemeshes, ref int nbSubmeshes, int[] updatedIndices, ref int nbVertices, ref int nbTriangles, int nbSubmesh)
        {
            dllz_spatial_mapping_merge_chunks(numberFaces, nbVerticesInSubemeshes, nbTrianglesInSubemeshes, ref nbSubmeshes, updatedIndices, ref nbVertices, ref nbTriangles, nbSubmesh);
        }

        /// <summary>
        /// Retrieves images for a specific mat
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="measure"></param>
        /// <param name="mem"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveMeasure(sl.ZEDMat mat, sl.MEASURE measure, sl.ZEDMat.MEM mem = sl.ZEDMat.MEM.MEM_CPU, sl.Resolution resolution = new sl.Resolution())
        {
            return (sl.ERROR_CODE)(dllz_retrieve_measure(mat.MatPtr, (int)measure, (int)mem, resolution));
        }

        /// <summary>
        /// Retrieves measures for a specific mat
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="view"></param>
        /// <param name="mem"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public sl.ERROR_CODE RetrieveImage(sl.ZEDMat mat, sl.VIEW view, sl.ZEDMat.MEM mem = sl.ZEDMat.MEM.MEM_CPU, sl.Resolution resolution = new sl.Resolution())
        {
            return (sl.ERROR_CODE)(dllz_retrieve_image(mat.MatPtr, (int)view, (int)mem, resolution));
        }

        /// <summary>
        /// Compute offset optical center
        /// </summary>
        /// <param name="planeDistance"></param>
        /// <returns></returns>
        public Vector4 ComputeOpticalCenterOffsets(float planeDistance)
        {
            IntPtr p = IntPtr.Zero;
			sl.CalibrationParameters calib = GetCalibrationParameters (false);//GetCalibrationinstance.CalibrationParametersRectified;
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


		////*
		/// Plane Detection
		/// 
		/// 
		public sl.ERROR_CODE findFloorPlane(ref ZEDPlaneGameObject.PlaneData plane,out float playerHeight, Quaternion priorQuat, Vector3 priorTrans)
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

		public int convertFloorPlaneToMesh(Vector3[] vertices, int[] triangles,out int numVertices, out int numTriangles)
		{
			return dllz_convert_floorplane_to_mesh (vertices, triangles,out numVertices,out numTriangles);
		}

		public sl.ERROR_CODE findPlaneAtHit(ref ZEDPlaneGameObject.PlaneData plane,Vector2 screenPos)
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

		public int convertHitPlaneToMesh(Vector3[] vertices, int[] triangles,out int numVertices, out int numTriangles)
		{
			return dllz_convert_hitplane_to_mesh (vertices, triangles,out numVertices,out numTriangles);
		}

    }
} // namespace sl
