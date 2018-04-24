//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Runtime.InteropServices;
using UnityEngine;


namespace sl
{
    /// <summary>
    /// Resolution of the current camera
    /// </summary>
    public struct Resolution
    {
        public Resolution(uint width, uint height)
        {
            this.width = (System.UIntPtr)width;
            this.height = (System.UIntPtr)height;
        }

        public System.UIntPtr width;
        public System.UIntPtr height;
    };



    [StructLayout(LayoutKind.Sequential)]
    public struct Pose
    {
        public bool valid;
        public ulong timestap;
        public Quaternion rotation;
        public Vector3 translation;
        public int pose_confidence;
    }
	 

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraParameters
    {
        /// <summary>
        /// Focal x
        /// </summary>
        public float fx;
        /// <summary>
        /// Focal y
        /// </summary>
        public float fy;
        /// <summary>
        /// Optical center x
        /// </summary>
        public float cx;
        /// <summary>
        /// Optical center y
        /// </summary>
        public float cy;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 5)]
        public double[] disto;
        /// <summary>
        /// Vertical field of view after stereo rectification
        /// </summary>
        public float vFOV;
        /// <summary>
        /// Horizontal field of view after stereo rectification
        /// </summary>
        public float hFOV;
        /// <summary>
        /// Diagonal field of view after stereo rectification
        /// </summary>
        public float dFOV;
        public Resolution resolution;
    };



    [StructLayout(LayoutKind.Sequential)]
    public struct CalibrationParameters
    {
        /// <summary>
        /// Rotation (using Rodrigues' transformation) between the two sensors. Defined as 'tilt', 'convergence' and 'roll'
        /// </summary>
        public Vector3 Rot;
        /// <summary>
        /// Translation between the two sensors. T[0] is the distance between the two cameras in meters.
        /// </summary>
        public Vector3 Trans;
        /// <summary>
        /// Parameters of the left camera
        /// </summary>
        public CameraParameters leftCam;
        /// <summary>
        /// Parameters of the right camera
        /// </summary>
        public CameraParameters rightCam;
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct Recording_state
    {
        /// <summary>
        /// status of current frame. May be true for success or false if frame could not be written in the SVO file
        /// </summary>
        public bool status;
        /// <summary>
        /// compression time for the current frame in ms
        /// </summary>
        public double current_compression_time;
        /// <summary>
        /// compression ratio (% of raw size) for the current frame
        /// </summary>
        public double current_compression_ratio;
        /// <summary>
        /// average compression time in ms since beginning of recording
        /// </summary>
        public double average_compression_time;
        /// <summary>
        /// compression ratio (% of raw size) since beginning of recording
        /// </summary>
        public double average_compression_ratio;
    }

    /// <summary>
    /// Status for self calibration. Since v0.9.3, self-calibration is done in background and start in the sl.ZEDCamera.Init or Reset function.
    /// </summary>
    public enum ZED_SELF_CALIBRATION_STATE
    {
        /// <summary>
        /// Self Calibration has not yet been called (no init called)
        /// </summary>
        SELF_CALIBRATION_NOT_CALLED,
        /// <summary>
        /// Self Calibration is currently running.
        /// </summary>
        SELF_CALIBRATION_RUNNING,
        /// <summary>
        /// Self Calibration has finished running but did not manage to get coherent values. Old Parameters are taken instead.
        /// </summary>
        SELF_CALIBRATION_FAILED,
        /// <summary>
        /// Self Calibration has finished running and did manage to get coherent values. New Parameters are taken.
        /// </summary>
        SELF_CALIBRATION_SUCCESS
    };

    /// <summary>
    /// List available depth computation modes.
    /// </summary>
    public enum DEPTH_MODE
    {
		/// <summary>
		/// This mode does not compute any depth map. Only rectified stereo images will be available.
		/// </summary>
		NONE,
		/// <summary>
		///  Fastest mode for depth computation.
		/// </summary>
		PERFORMANCE,
		/// <summary>
		/// Balanced quality mode. Depth map is robust in any environment and requires medium resources for computation
		/// </summary>
		MEDIUM,
		/// <summary>
		/// Best quality mode. Requires more compute power.
		/// </summary>
		QUALITY,
		/// <summary>
		/// native depth. Requires more compute power.
		/// </summary>
		ULTRA,
    };


	public enum VIEW_MODE
	{

		/// <summary>
		/// Eyes will display images (left and right) (default)
		/// </summary>
		VIEW_IMAGE,
		/// <summary>
		/// Eyes will display depth (left and right)
		/// </summary>
		VIEW_DEPTH,
		/// <summary>
		/// Eyes will display normals (left and right)
		/// </summary>
		VIEW_NORMALS

	};


    /// <summary>
    /// List error codes in the ZED SDK.
    /// </summary>
    public enum ERROR_CODE
    {
		/// <summary>
		/// Operation success
		/// </summary>
		SUCCESS ,
		/// <summary>
		/// Standard code for unsuccessful behavior
		/// </summary>
		FAILURE,
		/// <summary>
		/// No GPU found or CUDA capability of the device is not supported
		/// </summary>
		NO_GPU_COMPATIBLE,
		/// <summary>
		/// Not enough GPU memory for this depth mode, try a different mode (such as PERFORMANCE).
		/// </summary>
		NOT_ENOUGH_GPUMEM,
		/// <summary>
		/// The ZED camera is not plugged or detected.
		/// </summary>
		CAMERA_NOT_DETECTED,
		/// <summary>
		/// a ZED-M camera is detected but the inertial sensor cannot be opened. Only for ZED-M device
		/// </summary>
		SENSOR_NOT_DETECTED,
		/// <summary>
		/// For Nvidia Jetson X1 only, resolution not yet supported (USB3.0 bandwidth)
		/// </summary>
		INVALID_RESOLUTION,
		/// <summary>
		/// This issue can occurs when the camera FPS cannot be reached, due to a lot of corrupted frames. Try to change USB port.
		/// </summary>
		LOW_USB_BANDWIDTH,
		/// <summary>
		/// ZED calibration file is not found on the host machine. Use ZED Explorer or ZED Calibration to get one
		/// </summary>
		CALIBRATION_FILE_NOT_AVAILABLE, 
		/// <summary>
		/// ZED calibration file is not valid, try to download the factory one or recalibrate your camera using 'ZED Calibration
		/// </summary>
		INVALID_CALIBRATION_FILE, 
		/// <summary>
		/// The provided SVO file is not valid.
		/// </summary>
		INVALID_SVO_FILE,
		/// <summary>
		/// An recorder related error occurred (not enough free storage, invalid file)
		/// </summary>
		SVO_RECORDING_ERROR,
		/// <summary>
		/// The requested coordinate system is not available
		/// </summary>
		INVALID_COORDINATE_SYSTEM, 
		/// <summary>
		/// The firmware of the ZED is out of date. Update to the latest version.
		/// </summary>
		INVALID_FIRMWARE,
		/// <summary>
		///  An invalid parameter has been set for the function
		/// </summary>
		INVALID_FUNCTION_PARAMETERS,
		/// <summary>
		/// In grab() only, the current call return the same frame as last call. Not a new frame
		/// </summary>
		NOT_A_NEW_FRAME,
		/// <summary>
		/// In grab() only, a CUDA error has been detected in the process. Activate verbose in sl::Camera::open for more info
		/// </summary>
		CUDA_ERROR,
		/// <summary>
		/// In grab() only, ZED SDK is not initialized. Probably a missing call to sl::Camera::open
		/// </summary>
		CAMERA_NOT_INITIALIZED, 
		/// <summary>
		/// Your NVIDIA driver is too old and not compatible with your current CUDA version.
		/// </summary>
		NVIDIA_DRIVER_OUT_OF_DATE, 
		/// <summary>
		/// The call of the function is not valid in the current context. Could be a missing call of sl::Camera::open.
		/// </summary>
		INVALID_FUNCTION_CALL, 
		/// <summary>
		///  The SDK wasn't able to load its dependencies, the installer should be launched.
		/// </summary>
		CORRUPTED_SDK_INSTALLATION, 
		/// <summary>
		/// The installed SDK is incompatible SDK used to compile the program
		/// </summary>
		INCOMPATIBLE_SDK_VERSION,
		/// <summary>
		/// The given area file does not exist, check the path
		/// </summary>
		INVALID_AREA_FILE, 
		/// <summary>
		/// The area file does not contain enought data to be used or the sl::DEPTH_MODE used during the creation of the area file is different from the one currently set.
		/// </summary>
		INCOMPATIBLE_AREA_FILE,
		/// <summary>
		/// Camera failed to setup
		/// </summary>
		CAMERA_FAILED_TO_SETUP,
		/// <summary>
		/// Your ZED can not be opened, try replugging it to another USB port or flipping the USB-C connector
		/// </summary>
		CAMERA_DETECTION_ISSUE,
		/// <summary>
		/// The Camera is already used by another process
		/// </summary>
		CAMERA_ALREADY_IN_USE, 
		/// <summary>
		/// No GPU found, CUDA is unable to list it. Can be a driver/reboot issue
		/// </summary>
		NO_GPU_DETECTED,
		/// <summary>
		/// Plane not found, either no plane is detected in the scene, at the location or corresponding to the floor, or the floor plane doesn't match the prior given
		/// </summary>
		ERROR_CODE_PLANE_NOT_FOUND, 
		/// <summary>
		///  end of error code (used before init)
		/// </summary>
		ERROR_CODE_LAST
    };


    /// <summary>
    /// Represents the available resolution
    /// </summary>
    public enum RESOLUTION
    {
        /// <summary>
        /// 2208*1242, supported frame rate : 15 fps
        /// </summary>
        HD2K,
        /// <summary>
        /// 1920*1080, supported frame rates : 15, 30 fps
        /// </summary>
        HD1080,
        /// <summary>
        /// 1280*720, supported frame rates : 15, 30, 60 fps
        /// </summary>
        HD720,
        /// <summary>
        /// 672*376, supported frame rates : 15, 30, 60, 100 fps
        /// </summary>
        VGA
    };


	/// <summary>
	/// List available depth sensing modes.
	/// </summary>
	public enum MODEL
	{
		ZED, /**< ZED camera.*/
		ZED_M /**< ZED M device.*/
	};

    /// <summary>
    /// List available depth sensing modes.
    /// </summary>
    public enum SENSING_MODE
    {
        /// <summary>
        /// This mode outputs ZED standard depth map that preserves edges and depth accuracy.
        /// Applications example: Obstacle detection, Automated navigation, People detection, 3D reconstruction
        /// </summary>
        STANDARD,
        /// <summary>
        /// This mode outputs a smooth and fully dense depth map.
        /// Applications example: AR/VR, Mixed-reality capture, Image post-processing
        /// </summary>
        FILL
    };

    /// <summary>
    /// List available views.
    /// </summary>
    public enum VIEW
    {
        /// <summary>
        /// Left RGBA image, sl::MAT_TYPE_8U_C4.
        /// </summary>
        LEFT,
        /// <summary>
        /// Right RGBA image, sl::MAT_TYPE_8U_C4.
        /// </summary>
        RIGHT,
        /// <summary>
        /// Left GRAY image, sl::MAT_TYPE_8U_C1.
        /// </summary>
        LEFT_GREY,
        /// <summary>
        /// Right GRAY image, sl::MAT_TYPE_8U_C1.
        /// </summary>
        RIGHT_GREY,
        /// <summary>
        /// Left RGBA unrectified image, sl::MAT_TYPE_8U_C4.
        /// </summary>
        LEFT_UNRECTIFIED,
        /// <summary>
        /// Right RGBA unrectified image, sl::MAT_TYPE_8U_C4.
        /// </summary>
        RIGHT_UNRECTIFIED,
        /// <summary>
        /// Left GRAY unrectified image, sl::MAT_TYPE_8U_C1.
        /// </summary>
        LEFT_UNRECTIFIED_GREY,
        /// <summary>
        /// Right GRAY unrectified image, sl::MAT_TYPE_8U_C1.
        /// </summary>
        RIGHT_UNRECTIFIED_GREY,
        /// <summary>
        ///  Left and right image (the image width is therefore doubled) RGBA image, MAT_8U_C4.
        /// </summary>
        SIDE_BY_SIDE,
        /// <summary>
        /// Normalized depth image
        /// </summary>
        DEPTH,
        /// <summary>
        ///  Normalized confidence image, MAT_8U_C4.
        /// </summary>
        CONFIDENCE,
        /// <summary>
        /// Color rendering of the normals, MAT_8U_C4.
        /// </summary>
        NORMALS,
        /// <summary>
        /// Color rendering of the right depth mapped on right sensor, MAT_8U_C4.
        /// </summary>
        DEPTH_RIGHT,
        /// <summary>
        /// Color rendering of the normals mapped on right sensor, MAT_8U_C4.
        /// </summary>
        NORMALS_RIGHT
    };

    /// <summary>
    ///  List available camera settings for the ZED camera (contrast, hue, saturation, gain...).
    /// </summary>
    public enum CAMERA_SETTINGS
    {
        /// <summary>
        /// Defines the brightness control. Affected value should be between 0 and 8
        /// </summary>
        BRIGHTNESS,
        /// <summary>
        /// Defines the contrast control. Affected value should be between 0 and 8
        /// </summary>
        CONTRAST,
        /// <summary>
        /// Defines the hue control. Affected value should be between 0 and 11
        /// </summary>
        HUE,
        /// <summary>
        /// Defines the saturation control. Affected value should be between 0 and 8
        /// </summary>
        SATURATION,
        /// <summary>
        /// Defines the gain control. Affected value should be between 0 and 100 for manual control. If ZED_EXPOSURE is set to -1, the gain is in auto mode too.
        /// </summary>
        GAIN,
        /// <summary>
        /// Defines the exposure control. A -1 value enable the AutoExposure/AutoGain control. Affected value should be between 0 and 100 for manual control. A 0 value only disable auto mode without modifing the last auto values, while a 1 to 100 value disable auto mode and set exposure to chosen value
        /// </summary>
        EXPOSURE,
        /// <summary>
        /// Defines the color temperature control. Affected value should be between 2800 and 6500 with a step of 100. A value of -1 set the AWB ( auto white balance), as the boolean parameter (default) does.
        /// </summary>
        WHITEBALANCE
    };

    /// <summary>
    /// List retrievable measures.
    /// </summary>
    public enum MEASURE
    {
        /// <summary>
        /// Disparity map, 1 channel, FLOAT
        /// </summary>
        DISPARITY,
        /// <summary>
        /// Depth map, 1 channel, FLOAT
        /// </summary>
        DEPTH,
        /// <summary>
        /// Certainty/confidence of the disparity map, 1 channel, FLOAT
        /// </summary>
        CONFIDENCE,
        /// <summary>
        /// 3D coordinates of the image points, 4 channels, FLOAT (the 4th channel may contains the colors)
        /// </summary>
        XYZ,
        /// <summary>
        /// 3D coordinates and Color of the image , 4 channels, FLOAT (the 4th channel encode 4 UCHAR for color in R-G-B-A order)
        /// </summary>
        XYZRGBA,
        /// <summary>
        /// 3D coordinates and Color of the image , 4 channels, FLOAT (the 4th channel encode 4 UCHAR for color in B-G-R-A order)
        /// </summary>
        XYZBGRA,
        /// <summary>
        /// 3D coordinates and Color of the image , 4 channels, FLOAT (the 4th channel encode 4 UCHAR for color in A-R-G-B order)
        /// </summary>
        XYZARGB,
        /// <summary>
        /// 3D coordinates and Color of the image, 4 channels, FLOAT, channel 4 contains color in A-B-G-R order.
        /// </summary>
        XYZABGR,
        /// <summary>
        /// 3D coordinates and Color of the image , 4 channels, FLOAT (the 4th channel encode 4 UCHAR for color in A-B-G-R order)
        /// </summary>
        NORMALS,
        /// <summary>
        /// Disparity map for right sensor,  1 channel, FLOAT.
        /// </summary>
        DISPARITY_RIGHT,
        /// <summary>
        /// Depth map for right sensor,  1 channel, FLOAT.
        /// </summary>
        DEPTH_RIGHT,
        /// <summary>
        /// Point cloud for right sensor, 4 channels, FLOAT, channel 4 is empty.
        /// </summary>
        XYZ_RIGHT,
        /// <summary>
        /// Colored point cloud for right sensor, 4 channels, FLOAT, channel 4 contains color in R-G-B-A order.
        /// </summary>
        XYZRGBA_RIGHT,
        /// <summary>
        ///  Colored point cloud for right sensor, 4 channels, FLOAT, channel 4 contains color in B-G-R-A order.
        /// </summary>
        XYZBGRA_RIGHT,
        /// <summary>
        ///  Colored point cloud for right sensor, 4 channels, FLOAT, channel 4 contains color in A-R-G-B order.
        /// </summary>
        XYZARGB_RIGHT,
        /// <summary>
        /// Colored point cloud for right sensor, 4 channels, FLOAT, channel 4 contains color in A-B-G-R order.
        /// </summary>
        XYZABGR_RIGHT,
        /// <summary>
        ///  Normals vector for right view, 4 channels, FLOAT, channel 4 is empty (set to 0)
        /// </summary>
        NORMALS_RIGHT

    };


	/// <summary>
	/// Only few functions of tracking use this system, the path is the default value
	/// </summary>
	public enum TIME_REFERENCE
	{
		/// <summary>
		/// time when the image was captured on the USB
		/// </summary>
		IMAGE,
		/// <summary>
		/// current time (time of the function call)
		/// </summary>
		CURRENT
	};

    /// <summary>
	/// Reference frame (world or camera) for the tracking and depth sensing
    /// </summary>
    public enum REFERENCE_FRAME
    {
        /// <summary>
        /// The matrix contains the displacement from the first camera to the current one
        /// </summary>
        WORLD,
        /// <summary>
        /// The matrix contains the displacement from the previous camera position to the current one
        /// </summary>
        CAMERA
    };

    /// <summary>
    ///  List the different states of positional tracking.
    /// </summary>
    public enum TRACKING_STATE
    {
        /// <summary>
        /// The tracking is searching a match from the database to relocate at a previously known position
        /// </summary>
        TRACKING_SEARCH,
        /// <summary>
        /// The tracking operates normally, the path should be correct
        /// </summary>
        TRACKING_OK,
        /// <summary>
        /// The tracking is not enabled
        /// </summary>
        TRACKING_OFF
    }

    /// <summary>
    /// List of svo compressions mode available
    /// </summary>
    public enum SVO_COMPRESSION_MODE
    {
        /// <summary>
        /// RAW images, no compression
        /// </summary>
        RAW_BASED,
        /// <summary>
        /// new Lossless, with png/zstd based compression : avg size = 42% of RAW
        /// </summary>
        LOSSLESS_BASED,
        /// <summary>
        /// new Lossy, with jpeg based compression : avg size = 22% of RAW
        /// </summary>
        LOSSY_BASED
    }

    /// <summary>
    /// List of mesh format available
    /// </summary>
    public enum MESH_FILE_FORMAT
    {
        /// <summary>
        /// Contains only vertices and faces.
        /// </summary>
        PLY,
        /// <summary>
        /// Contains only vertices and faces, encoded in binary.
        /// </summary>
        BIN,
        /// <summary>
        /// Contains vertices, normals, faces and textures informations if possible.
        /// </summary>
        OBJ
    }

    /// <summary>
    /// List of filters avvailable for the spatial mapping
    /// </summary>
    public enum FILTER
    {
        /// <summary>
        /// Soft decimation and smoothing.
        /// </summary>
        LOW,
        /// <summary>
        /// Decimate the number of faces and apply a soft smooth.
        /// </summary>
        MEDIUM,
        /// <summary>
        /// Drasticly reduce the number of faces.
        /// </summary>
        HIGH,
    }

    /// <summary>
    /// List of spatial mapping state
    /// </summary>
    public enum SPATIAL_MAPPING_STATE
    {
        /// <summary>
        /// The spatial mapping is initializing.
        /// </summary>
        SPATIAL_MAPPING_STATE_INITIALIZING,
        /// <summary>
        /// The depth and tracking data were correctly integrated in the fusion algorithm.
        /// </summary>
        SPATIAL_MAPPING_STATE_OK,
        /// <summary>
        /// The maximum memory dedicated to the scanning has been reach, the mesh will no longer be updated.
        /// </summary>
        SPATIAL_MAPPING_STATE_NOT_ENOUGH_MEMORY,
        /// <summary>
        /// EnableSpatialMapping() wasn't called (or the scanning was stopped and not relaunched).
        /// </summary>
        SPATIAL_MAPPING_STATE_NOT_ENABLED,
        /// <summary>
        /// Effective FPS is too low to give proper results for spatial mapping. Consider using PERFORMANCES parameters (DEPTH_MODE_PERFORMANCE, low camera resolution (VGA,HD720), spatial mapping low resolution)
        /// </summary>
        SPATIAL_MAPPING_STATE_FPS_TOO_LOW
    }

    /// <summary>
    /// Unit used by the SDK. Prefer using METER with Unity
    /// </summary>
    public enum UNIT
    {
        /// <summary>
        /// International System, 1/1000 METER.
        /// </summary>
        MILLIMETER,
        /// <summary>
        /// International System, 1/100 METER.
        /// </summary>
        CENTIMETER,
        /// <summary>
        /// International System, 1METER.
        /// </summary>
        METER,
        /// <summary>
        ///  Imperial Unit, 1/12 FOOT 
        /// </summary>
        INCH,
        /// <summary>
        ///  Imperial Unit, 1 FOOT
        /// </summary>
        FOOT
    }

    /// <summary>
    /// Parameters that will be fixed for the whole execution life time of the camera.
    /// </summary>
    public class InitParameters
    {
        /// <summary>
        /// Define the chosen ZED resolution 
        /// </summary>
        public sl.RESOLUTION resolution;
        /// <summary>
        /// Requested FPS for this resolution. set as 0 will choose the default FPS for this resolution (see User guide). 
        /// </summary>
        public int cameraFPS;
        /// <summary>
        ///  ONLY for LINUX : if multiple ZEDs are connected. Is not used in Unity
        /// </summary>
        public int cameraLinuxID;
        /// <summary>
        /// Path with filename to the recorded SVO file.
        /// </summary>
        public string pathSVO = "";
        /// <summary>
        /// This mode simulates the live camera and consequently skipped frames if the computation framerate is too slow.
        /// </summary>
        public bool svoRealTimeMode;
        /// <summary>
        ///  Define the unit for all the metric values ( depth, point cloud, tracking, mesh).
        /// </summary>
        public UNIT coordinateUnit;
        /// <summary>
        /// This defines the order and the direction of the axis of the coordinate system. see COORDINATE_SYSTEM for more information.
        /// </summary>
        public COORDINATE_SYSTEM coordinateSystem;
        /// <summary>
        /// Defines the quality of the depth map, affects the level of details and also the computation time.
        /// </summary>
        public sl.DEPTH_MODE depthMode;
        /// <summary>
        ///  Specify the minimum depth value that will be computed, in the UNIT you define.
        /// </summary>
        public float depthMinimumDistance;
        /// <summary>
        ///  Defines if the image are horizontally flipped.
        /// </summary>
        public bool cameraImageFlip;
        /// <summary>
        /// Defines if right MEASURE should be computed (needed for MEASURE_<XXX>_RIGHT)
        /// </summary>
        public bool enableRightSideMeasure;
        /// <summary>
        /// If set to true, it will disable self-calibration and take the optional calibration parameters without optimizing them.
        /// It is advised to leave it as false, so that calibration parameters can be optimized.
        /// </summary>
        public bool cameraDisableSelfCalib;
        /// <summary>
        /// ONLY for LINUX : Set the number of buffers in the internal grabbing process. DO NOT WORK ON UNITY
        /// </summary>
        public int cameraBufferCountLinux;
        /// <summary>
        /// Defines if you want the SDK provides text feedback.
        /// </summary>
        public bool sdkVerbose;
        /// <summary>
        ///  Defines the graphics card on which the computation will be done.
        /// </summary>
        public int sdkGPUId;
        /// <summary>
        /// Store the program outputs into the log file defined by its filename.
        /// </summary>
        public string sdkVerboseLogFile = "";
        /// <summary>
        /// Defines if the depth map should be stabilize.
        /// </summary>
        public bool depthStabilization;

        public InitParameters()
        {
            this.resolution = RESOLUTION.HD720;
            this.cameraFPS = 60;
            this.cameraLinuxID = 0;
            this.pathSVO = "";
            this.svoRealTimeMode = false;
            this.coordinateUnit = UNIT.METER;
            this.coordinateSystem = COORDINATE_SYSTEM.IMAGE;
            this.depthMode = DEPTH_MODE.PERFORMANCE;
            this.depthMinimumDistance = -1;
            this.cameraImageFlip = false;
            this.cameraDisableSelfCalib = false;
            this.cameraBufferCountLinux = 4;
            this.sdkVerbose = false;
            this.sdkGPUId = -1;
            this.sdkVerboseLogFile = "";
            this.enableRightSideMeasure = false;
            this.depthStabilization = true;
        }

    }
    /// <summary>
    /// List of available system of coordinates
    /// </summary>
    public enum COORDINATE_SYSTEM
    {
        /// <summary>
        /// Standard coordinates system in computer vision. Used in OpenCV : see here : http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html 
        /// </summary>
        IMAGE,
        /// <summary>
        /// Left-Handed with Y up and Z forward. Used in Unity with DirectX
        /// </summary>
        LEFT_HANDED_Y_UP,
        /// <summary>
        ///  Right-Handed with Y pointing up and Z backward. Used in OpenGL.
        /// </summary>
        RIGHT_HANDED_Y_UP,
        /// <summary>
        /// Right-Handed with Z pointing up and Y forward. Used in 3DSMax.
        /// </summary>
        RIGHT_HANDED_Z_UP,
        /// <summary>
        /// Left-Handed with Z axis pointing up and X forward. Used in Unreal Engine.
        /// </summary>
        LEFT_HANDED_Z_UP
    }

    /// <summary>
    ///  List the different states of spatial memory area export.
    /// </summary>
    public enum AREA_EXPORT_STATE
    {
        /// <summary>
        ///  The spatial memory file has been successfully created.
        /// </summary>
        AREA_EXPORT_STATE_SUCCESS,
        /// <summary>
        /// The spatial memory is currently written.
        /// </summary>
        AREA_EXPORT_STATE_RUNNING,
        /// <summary>
        /// The spatial memory file exportation has not been called.
        /// </summary>
        AREA_EXPORT_STATE_NOT_STARTED,
        /// <summary>
        /// The spatial memory contains no data, the file is empty.
        /// </summary>
        AREA_EXPORT_STATE_FILE_EMPTY,
        /// <summary>
        ///  The spatial memory file has not been written because of a wrong file name.
        /// </summary>
        AREA_EXPORT_STATE_FILE_ERROR,
        /// <summary>
        /// The spatial memory learning is disable, no file can be created.
        /// </summary>
        AREA_EXPORT_STATE_SPATIAL_MEMORY_DISABLED
    };


    /// <summary>
    /// Runtime parameters used by the grab
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RuntimeParameters {
        /// <summary>
        ///  Defines the algorithm used for depth map computation, more info : \ref SENSING_MODE definition.
        /// </summary>
        [FieldOffset(12)]//In 2.2 the runtime parameters needs 3 int of offset
        public sl.SENSING_MODE sensingMode;
        /// <summary>
        /// Provides 3D measures (point cloud and normals) in the desired reference frame (default is REFERENCE_FRAME_CAMERA)
        /// </summary>
        [FieldOffset(16)]
		public sl.REFERENCE_FRAME measure3DReferenceFrame;
        /// <summary>
        /// Defines if the depth map should be computed.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(20)]
        public bool enableDepth;
        /// <summary>
        ///  Defines if the point cloud should be computed (including XYZRGBA).
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(21)]
        public bool enablePointCloud;

    }

	/// <summary>
	/// Tracking Frame choices : Left (left sensor), Center (Center of the camera), Right (right sensor)
	/// </summary>
	public enum TRACKING_FRAME
	{
		LEFT_EYE,
		CENTER_EYE,
		RIGHT_EYE
	};


	/// <summary>
	/// Constant Rendering Plane distance (Don't change this)
	/// </summary>
	public enum Constant {
		PLANE_DISTANCE = 10
	};








}// end namespace sl
