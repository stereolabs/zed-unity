//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// This file holds classes built to be exchanged between the ZED wrapper DLL (sl_unitywrapper.dll)
/// and C# scripts within Unity. Most have parity with a structure within the ZED C++ SDK.
/// Find more info at https://www.stereolabs.com/developers/documentation/API/latest/. 
/// </summary>

namespace sl
{

	public class ZEDCommon
	{
		public const string NameDLL = "sl_unitywrapper";
	}

	public enum ZED_CAMERA_ID
	{
		CAMERA_ID_01,
		CAMERA_ID_02,
		CAMERA_ID_03,
		CAMERA_ID_04,
	};


    public enum INPUT_TYPE
    {
        INPUT_TYPE_USB,
        INPUT_TYPE_SVO,
        INPUT_TYPE_STREAM
    };

    /// <summary>
    /// Constant for plugin. Should not be changed
    /// </summary>
    public enum Constant
	{
		MAX_CAMERA_PLUGIN = 4,
		PLANE_DISTANCE = 10
	};

    /// <summary>
    /// Holds a 3x3 matrix that can be marshaled between the ZED
    /// Unity wrapper and C# scripts. 
    /// </summary>
	public struct Matrix3x3
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
		public float[] m; //3x3 matrix.
	};

    /// <summary>
    /// Holds a camera resolution as two pointers (for height and width) for easy
    /// passing back and forth to the ZED Unity wrapper. 
    /// </summary>
    public struct Resolution
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Resolution(uint width, uint height)
        {
            this.width = (System.UIntPtr)width;
            this.height = (System.UIntPtr)height;
        }

        public System.UIntPtr width;
        public System.UIntPtr height;
    };


	/// <summary>
	/// Pose structure with data on timing and validity in addition to 
    /// position and rotation. 
	/// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Pose
    {
        public bool valid;
        public ulong timestap;
        public Quaternion rotation;
        public Vector3 translation;
        public int pose_confidence;
	};
	 
	/// <summary>
	/// Full IMU data structure.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct IMUData
	{
        /// <summary>
        /// Gyroscope raw data in degrees/second.
        /// </summary>
		public Vector3 angularVelocity; 
        /// <summary>
        /// Accelerometer raw data in m/s².
        /// </summary>
		public Vector3 linearAcceleration; 
        /// <summary>
        /// Orientation from gyro/accelerator fusion.
        /// </summary>
		public Quaternion fusedOrientation; 
        /// <summary>
        /// Covariance matrix of the quaternion.
        /// </summary>
		public Matrix3x3 orientationCovariance; 
        /// <summary>
        /// Gyroscope raw data covariance matrix.
        /// </summary>
		public Matrix3x3 angularVelocityCovariance; 
        /// <summary>
        /// Accelerometer raw data covariance matrix.
        /// </summary>
		public Matrix3x3 linearAccelerationCovariance; 
        /// <summary>
        /// RFU.
        /// </summary>
		public int imu_image_sync_val; 
	};




    /// <summary>
    /// Calibration information for an individual sensor on the ZED (left or right). </summary>
    /// <remarks>For more information, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/structsl_1_1CameraParameters.html </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraParameters
    {
        /// <summary>
        /// Focal X.
        /// </summary>
        public float fx;
        /// <summary>
        /// Focal Y.
        /// </summary>
        public float fy;
        /// <summary>
        /// Optical center X.
        /// </summary>
        public float cx;
        /// <summary>
        /// Optical center Y.
        /// </summary>
        public float cy;

        /// <summary>
        /// Distortion coefficients. 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 5)]
        public double[] disto;

        /// <summary>
        /// Vertical field of view after stereo rectification.
        /// </summary>
        public float vFOV;
        /// <summary>
        /// Horizontal field of view after stereo rectification.
        /// </summary>
        public float hFOV;
        /// <summary>
        /// Diagonal field of view after stereo rectification.
        /// </summary>
        public float dFOV;
        /// <summary>
        /// Camera's current resolution.
        /// </summary>
        public Resolution resolution;
    };


    /// <summary>
    /// Holds calibration information about the current ZED's hardware, including per-sensor
    /// calibration and offsets between the two sensors. 
    /// </summary> <remarks>For more info, see:
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/structsl_1_1CalibrationParameters.html </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct CalibrationParameters
    {
        /// <summary>
        /// Rotation (using Rodrigues' transformation) between the two sensors. Defined as 'tilt', 'convergence' and 'roll'.
        /// </summary>
        public Vector3 Rot;
        /// <summary>
        /// Translation between the two sensors. T[0] is the distance between the two cameras in meters.
        /// </summary>
        public Vector3 Trans;
        /// <summary>
        /// Parameters of the left sensor.
        /// </summary>
        public CameraParameters leftCam;
        /// <summary>
        /// Parameters of the right sensor.
        /// </summary>
        public CameraParameters rightCam;
    };

    /// <summary>
    /// Container for information about the current SVO recording process.  
    /// </summary><remarks>
    /// Mirrors sl.RecordingState in the ZED C++ SDK. For more info, visit: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/structsl_1_1RecordingState.html
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct Recording_state
    {
        /// <summary>
        /// Status of the current frame. True if recording was successful, false if frame could not be written.
        /// </summary>
        public bool status;
        /// <summary>
        /// Compression time for the current frame in milliseconds.
        /// </summary>
        public double current_compression_time;
        /// <summary>
        /// Compression ratio (% of raw size) for the current frame.
        /// </summary>
        public double current_compression_ratio;
        /// <summary>
        /// Average compression time in millisecond since beginning of recording.
        /// </summary>
        public double average_compression_time;
        /// <summary>
        /// Compression ratio (% of raw size) since recording was started. 
        /// </summary>
        public double average_compression_ratio;
    }

    /// <summary>
    /// Status of the ZED's self-calibration. Since v0.9.3, self-calibration is done in the background and 
    /// starts in the sl.ZEDCamera.Init or Reset functions.
    /// </summary><remarks>
    /// Mirrors SELF_CALIBRATION_STATE in the ZED C++ SDK. For more info, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Video__group.html#gacce19db438a07075b7e5e22ee5845c95
    /// </remarks>
    public enum ZED_SELF_CALIBRATION_STATE
    {
        /// <summary>
        /// Self-calibration has not yet been called (no Init() called).
        /// </summary>
        SELF_CALIBRATION_NOT_CALLED,
        /// <summary>
        /// Self-calibration is currently running.
        /// </summary>
        SELF_CALIBRATION_RUNNING,
        /// <summary>
        /// Self-calibration has finished running but did not manage to get coherent values. Old Parameters are used instead.
        /// </summary>
        SELF_CALIBRATION_FAILED,
        /// <summary>
        /// Self Calibration has finished running and successfully produces coherent values. 
        /// </summary>
        SELF_CALIBRATION_SUCCESS
    };

    /// <summary>
    /// Lists available depth computation modes. Each mode offers better accuracy than the 
    /// mode before it, but at a performance cost. 
    /// </summary><remarks>
    /// Mirrors DEPTH_MODE in the ZED C++ SDK. For more info, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Depth__group.html#ga8d542017c9b012a19a15d46be9b7fa43
    /// </remarks>
    public enum DEPTH_MODE
    {
		/// <summary>
		/// Does not compute any depth map. Only rectified stereo images will be available.
		/// </summary>
		NONE,
		/// <summary>
		/// Fastest mode for depth computation.
		/// </summary>
		PERFORMANCE,
		/// <summary>
		/// Balanced quality mode. Depth map is robust in most environment and requires medium compute power.
		/// </summary>
		MEDIUM,
		/// <summary>
		/// Favors accuracy over performance. Requires more compute power.
		/// </summary>
		QUALITY,
		/// <summary>
		/// Native depth. Very accurate, but at a large performance cost.
		/// </summary>
		ULTRA
    };

    /// <summary>
    /// Types of Image view modes, for creating human-viewable textures. 
    /// Used only in ZEDRenderingPlane as a simplified version of sl.VIEW, which has more detailed options. 
    /// </summary>
	public enum VIEW_MODE
	{
		/// <summary>
		/// Dsplays regular color images.
		/// </summary>
		VIEW_IMAGE,
		/// <summary>
		/// Displays a greyscale depth map.
		/// </summary>
		VIEW_DEPTH,
		/// <summary>
		/// Displays a normal map.
		/// </summary>
		VIEW_NORMALS
	};


    /// <summary>
    /// List of error codes in the ZED SDK.
    /// </summary><remarks>
    /// Mirrors ERROR_CODE in the ZED C++ SDK. For more info, read: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Camera__group.html#ga4db9ee29f2ff83c71567c12f6bfbf28c
    /// </remarks>
    public enum ERROR_CODE
    {
		/// <summary>
		/// Operation was successful. 
		/// </summary>
		SUCCESS,
		/// <summary>
		/// Standard, generic code for unsuccessful behavior when no other code is more appropriate. 
		/// </summary>
		FAILURE,
		/// <summary>
		/// No GPU found, or CUDA capability of the device is not supported.
		/// </summary>
		NO_GPU_COMPATIBLE,
		/// <summary>
		/// Not enough GPU memory for this depth mode. Try a different mode (such as PERFORMANCE).
		/// </summary>
		NOT_ENOUGH_GPUMEM,
		/// <summary>
		/// The ZED camera is not plugged in or detected.
		/// </summary>
		CAMERA_NOT_DETECTED,
		/// <summary>
		/// a ZED Mini is detected but the inertial sensor cannot be opened. (Never called for original ZED)
		/// </summary>
		SENSOR_NOT_DETECTED,
		/// <summary>
		/// For Nvidia Jetson X1 only - resolution not yet supported (USB3.0 bandwidth).
		/// </summary>
		INVALID_RESOLUTION,
		/// <summary>
		/// USB communication issues. Occurs when the camera FPS cannot be reached, due to a lot of corrupted frames.
        /// Try changing the USB port.
		/// </summary>
		LOW_USB_BANDWIDTH,
		/// <summary>
		/// ZED calibration file is not found on the host machine. Use ZED Explorer or ZED Calibration to get one.
		/// </summary>
		CALIBRATION_FILE_NOT_AVAILABLE, 
		/// <summary>
		/// ZED calibration file is not valid. Try downloading the factory one or recalibrating using the ZED Calibration tool.
		/// </summary>
		INVALID_CALIBRATION_FILE, 
		/// <summary>
		/// The provided SVO file is not valid.
		/// </summary>
		INVALID_SVO_FILE,
		/// <summary>
		/// An SVO recorder-related error occurred (such as not enough free storage or an invalid file path).
		/// </summary>
		SVO_RECORDING_ERROR,
		/// <summary>
		/// An SVO related error when NVIDIA based compression cannot be loaded
		/// </summary>
		SVO_UNSUPPORTED_COMPRESSION,
		/// <summary>
		/// The requested coordinate system is not available.
		/// </summary>
		INVALID_COORDINATE_SYSTEM, 
		/// <summary>
		/// The firmware of the ZED is out of date. Update to the latest version.
		/// </summary>
		INVALID_FIRMWARE,
		/// <summary>
		///  An invalid parameter has been set for the function.
		/// </summary>
		INVALID_FUNCTION_PARAMETERS,
		/// <summary>
		/// In grab() only, the current call return the same frame as last call. Not a new frame.
		/// </summary>
		NOT_A_NEW_FRAME,
		/// <summary>
		/// In grab() only, a CUDA error has been detected in the process. Activate wrapperVerbose in ZEDManager.cs for more info.
		/// </summary>
		CUDA_ERROR,
		/// <summary>
		/// In grab() only, ZED SDK is not initialized. Probably a missing call to sl::Camera::open.
		/// </summary>
		CAMERA_NOT_INITIALIZED, 
		/// <summary>
		/// Your NVIDIA driver is too old and not compatible with your current CUDA version.
		/// </summary>
		NVIDIA_DRIVER_OUT_OF_DATE, 
		/// <summary>
		/// The function call is not valid in the current context. Could be a missing a call to sl::Camera::open.
		/// </summary>
		INVALID_FUNCTION_CALL, 
		/// <summary>
		///  The SDK wasn't able to load its dependencies, the installer should be launched.
		/// </summary>
		CORRUPTED_SDK_INSTALLATION, 
		/// <summary>
		/// The installed SDK is not the SDK used to compile the program.
		/// </summary>
		INCOMPATIBLE_SDK_VERSION,
		/// <summary>
		/// The given area file does not exist. Check the file path.
		/// </summary>
		INVALID_AREA_FILE, 
		/// <summary>
		/// The area file does not contain enough data to be used ,or the sl::DEPTH_MODE used during the creation of the 
        /// area file is different from the one currently set.
		/// </summary>
		INCOMPATIBLE_AREA_FILE,
		/// <summary>
		/// Camera failed to set up.
		/// </summary>
		CAMERA_FAILED_TO_SETUP,
		/// <summary>
		/// Your ZED cannot be opened. Try replugging it to another USB port or flipping the USB-C connector (if using ZED Mini). 
		/// </summary>
		CAMERA_DETECTION_ISSUE,
		/// <summary>
		/// The Camera is already in use by another process.
		/// </summary>
		CAMERA_ALREADY_IN_USE, 
		/// <summary>
		/// No GPU found or CUDA is unable to list it. Can be a driver/reboot issue.
		/// </summary>
		NO_GPU_DETECTED,
		/// <summary>
		/// Plane not found. Either no plane is detected in the scene, at the location or corresponding to the floor, 
        /// or the floor plane doesn't match the prior given.
		/// </summary>
		PLANE_NOT_FOUND, 
		/// <summary>
		/// Missing or corrupted AI module ressources. 
		/// Please reinstall the ZED SDK with the AI (object detection) module to fix this issue
		/// </summary>
		AI_MODULE_NOT_AVAILABLE,
		/// <summary>
		/// The cuDNN library cannot be loaded, or is not compatible with this version of the ZED SDK
		/// </summary>
		INCOMPATIBLE_CUDNN_VERSION,
		/// <summary>
		/// End of error code. Used before init has been called. 
		/// </summary>
		ERROR_CODE_LAST
    };


    /// <summary>
    /// Represents the available resolution options. 
    /// </summary>
    public enum RESOLUTION
    {
        /// <summary>
        /// 2208*1242. Supported frame rate: 15 FPS.
        /// </summary>
        HD2K,
        /// <summary>
        /// 1920*1080. Supported frame rates: 15, 30 FPS.
        /// </summary>
        HD1080,
        /// <summary>
        /// 1280*720. Supported frame rates: 15, 30, 60 FPS.
        /// </summary>
        HD720,
        /// <summary>
        /// 672*376. Supported frame rates: 15, 30, 60, 100 FPS.
        /// </summary>
        VGA
    };


	/// <summary>
	/// Types of compatible ZED cameras. 
	/// </summary>
	public enum MODEL
	{
        /// <summary>
        /// Original ZED camera. 
        /// </summary>
		ZED, 
        /// <summary>
        /// ZED Mini. 
        /// </summary>
		ZED_M 
	};

    /// <summary>
    /// Lists available sensing modes - whether to produce the original depth map (STANDARD) or one with
    /// smoothing and other effects added to fill gaps and roughness (FILL). 
    /// </summary>
    public enum SENSING_MODE
    {
        /// <summary>
        /// This mode outputs the standard ZED depth map that preserves edges and depth accuracy.
        /// However, there will be missing data where a depth measurement couldn't be taken, such as from
        /// a surface being occluded from one sensor but not the other. 
        /// Better for: Obstacle detection, autonomous navigation, people detection, 3D reconstruction.
        /// </summary>
        STANDARD,
        /// <summary>
        /// This mode outputs a smooth and fully dense depth map. It doesn't have gaps in the data
        /// like STANDARD where depth can't be calculated directly, but the values it fills them with
        /// is less accurate than a real measurement. 
        /// Better for: AR/VR, mixed-reality capture, image post-processing.
        /// </summary>
        FILL
    };

    /// <summary>
    /// Lists available view types retrieved from the camera, used for creating human-viewable (Image-type) textures.
    /// </summary><remarks>
    /// Based on the VIEW enum in the ZED C++ SDK. For more info, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Video__group.html#ga77fc7bfc159040a1e2ffb074a8ad248c
    /// </remarks>
    public enum VIEW
    {
        /// <summary>
        /// Left RGBA image. As a ZEDMat, MAT_TYPE is set to MAT_TYPE_8U_C4.
        /// </summary>
        LEFT,
        /// <summary>
        /// Right RGBA image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C4.
        /// </summary>
        RIGHT,
        /// <summary>
        /// Left GRAY image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C1.
        /// </summary>
        LEFT_GREY,
        /// <summary>
        /// Right GRAY image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C1.
        /// </summary>
        RIGHT_GREY,
        /// <summary>
        /// Left RGBA unrectified image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C4.
        /// </summary>
        LEFT_UNRECTIFIED,
        /// <summary>
        /// Right RGBA unrectified image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C4.
        /// </summary>
        RIGHT_UNRECTIFIED,
        /// <summary>
        /// Left GRAY unrectified image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C1.
        /// </summary>
        LEFT_UNRECTIFIED_GREY,
        /// <summary>
        /// Right GRAY unrectified image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C1.
        /// </summary>
        RIGHT_UNRECTIFIED_GREY,
        /// <summary>
        ///  Left and right image. Will be double the width to hold both. As a ZEDMat, MAT_TYPE is set to MAT_8U_C4.
        /// </summary>
        SIDE_BY_SIDE,
        /// <summary>
        /// Normalized depth image. As a ZEDMat, MAT_TYPE is set to sl::MAT_TYPE_8U_C4.
        /// <para>Use an Image texture for viewing only. For measurements, use a Measure type instead 
        /// (ZEDCamera.RetrieveMeasure()) to preserve accuracy. </para>
        /// </summary>
        DEPTH,
        /// <summary>
        /// Normalized confidence image. As a ZEDMat, MAT_TYPE is set to MAT_8U_C4.
        /// <para>Use an Image texture for viewing only. For measurements, use a Measure type instead 
        /// (ZEDCamera.RetrieveMeasure()) to preserve accuracy. </para>
        /// </summary>
        CONFIDENCE,
        /// <summary>
        /// Color rendering of the normals. As a ZEDMat, MAT_TYPE is set to MAT_8U_C4.
        /// <para>Use an Image texture for viewing only. For measurements, use a Measure type instead 
        /// (ZEDCamera.RetrieveMeasure()) to preserve accuracy. </para>
        /// </summary>
        NORMALS,
        /// <summary>
        /// Color rendering of the right depth mapped on right sensor. As a ZEDMat, MAT_TYPE is set to MAT_8U_C4.
        /// <para>Use an Image texture for viewing only. For measurements, use a Measure type instead 
        /// (ZEDCamera.RetrieveMeasure()) to preserve accuracy. </para>
        /// </summary>
        DEPTH_RIGHT,
        /// <summary>
        /// Color rendering of the normals mapped on right sensor. As a ZEDMat, MAT_TYPE is set to MAT_8U_C4.
        /// <para>Use an Image texture for viewing only. For measurements, use a Measure type instead 
        /// (ZEDCamera.RetrieveMeasure()) to preserve accuracy. </para>
        /// </summary>
        NORMALS_RIGHT
    };

    /// <summary>
    ///  Lists available camera settings for the ZED camera (contrast, hue, saturation, gain, etc.)
    /// </summary>
    public enum CAMERA_SETTINGS
    {
        /// <summary>
        /// Brightness control. Value should be between 0 and 8.
        /// </summary>
        BRIGHTNESS,
        /// <summary>
        /// Contrast control. Value should be between 0 and 8.
        /// </summary>
        CONTRAST,
        /// <summary>
        /// Hue control. Value should be between 0 and 11.
        /// </summary>
        HUE,
        /// <summary>
        /// Saturation control. Value should be between 0 and 8
        /// </summary>
        SATURATION,
        /// <summary>
        /// Gain control. Value should be between 0 and 100 for manual control. 
        /// If ZED_EXPOSURE is set to -1 (automatic mode), then gain will be automatic as well.
        /// </summary>
        GAIN,
        /// <summary>
        /// Exposure control. Value can be between 0 and 100. 
        /// Setting to -1 enables auto exposure and auto gain.
        /// Setting to 0 disables auto exposure but doesn't change the last applied automatic values. 
        /// Setting to 1-100 disables auto mode and sets exposure to the chosen value.
        /// </summary>
        EXPOSURE,
        /// <summary>
        /// Color temperature control. Value should be between 2800 and 6500 with a step of 100. 
        /// </summary>
        WHITEBALANCE,
		/// <summary>
		/// Defines if the white balance is in automatic mode or not.
		/// </summary>
		AUTO_WHITEBALANCE,
        /// <summary>
        /// front LED status (1==enable, 0 == disable)
        /// </summary>
        LED_STATUS
    };

    /// <summary>
    /// Lists available measure types retrieved from the camera, used for creating precise measurement maps
    /// (Measure-type textures). 
    /// Based on the MEASURE enum in the ZED C++ SDK. For more info, see: 
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Depth__group.html#ga798a8eed10c573d759ef7e5a5bcd545d
    /// </summary>
    public enum MEASURE
    {
        /// <summary>
        /// Disparity map. As a ZEDMat, MAT_TYPE is set to MAT_32F_C1.
        /// </summary>
        DISPARITY,
        /// <summary>
        /// Depth map. As a ZEDMat, MAT_TYPE is set to MAT_32F_C1.
        /// </summary>
        DEPTH,
        /// <summary>
        /// Certainty/confidence of the disparity map. As a ZEDMat, MAT_TYPE is set to MAT_32F_C1.
        /// </summary>
        CONFIDENCE,
        /// <summary>
        /// 3D coordinates of the image points. Used for point clouds in ZEDPointCloudManager. 
        /// As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. The 4th channel may contain the colors.
        /// </summary>
        XYZ,
        /// <summary>
        /// 3D coordinates and color of the image. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4.
        /// The 4th channel encodes 4 UCHARs for colors in R-G-B-A order.
        /// </summary>
        XYZRGBA,
        /// <summary>
        /// 3D coordinates and color of the image. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4.
        /// The 4th channel encode 4 UCHARs for colors in B-G-R-A order.
        /// </summary>
        XYZBGRA,
        /// <summary>
        /// 3D coordinates and color of the image. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4.
        /// The 4th channel encodes 4 UCHARs for color in A-R-G-B order.
        /// </summary>
        XYZARGB,
        /// <summary>
        /// 3D coordinates and color of the image. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4.
        /// Channel 4 contains color in A-B-G-R order.
        /// </summary>
        XYZABGR,
        /// <summary>
        /// 3D coordinates and color of the image. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. 
        /// The 4th channel encode 4 UCHARs for color in A-B-G-R order.
        /// </summary>
        NORMALS,
        /// <summary>
        /// Disparity map for the right sensor. As a ZEDMat, MAT_TYPE is set to  MAT_32F_C1.
        /// </summary>
        DISPARITY_RIGHT,
        /// <summary>
        /// Depth map for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C1.
        /// </summary>
        DEPTH_RIGHT,
        /// <summary>
        /// Point cloud for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. Channel 4 is empty.
        /// </summary>
        XYZ_RIGHT,
        /// <summary>
        /// Colored point cloud for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. 
        /// Channel 4 contains colors in R-G-B-A order.
        /// </summary>
        XYZRGBA_RIGHT,
        /// <summary>
        /// Colored point cloud for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. 
        /// Channel 4 contains colors in B-G-R-A order.
        /// </summary>
        XYZBGRA_RIGHT,
        /// <summary>
        ///  Colored point cloud for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4.
        ///  Channel 4 contains colors in A-R-G-B order.
        /// </summary>
        XYZARGB_RIGHT,
        /// <summary>
        /// Colored point cloud for right sensor. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. 
        /// Channel 4 contains colors in A-B-G-R order.
        /// </summary>
        XYZABGR_RIGHT,
        /// <summary>
        ///  Normals vector for right view. As a ZEDMat, MAT_TYPE is set to MAT_32F_C4. 
        ///  Channel 4 is empty (set to 0).
        /// </summary>
        NORMALS_RIGHT

    };


	/// <summary>
	/// Categories indicating when a timestamp is captured. 
	/// </summary>
	public enum TIME_REFERENCE
	{
		/// <summary>
		/// Timestamp from when the image was received over USB from the camera, defined
        /// by when the entire image was available in memory. 
		/// </summary>
		IMAGE,
		/// <summary>
		/// Timestamp from when the relevant function was called. 
		/// </summary>
		CURRENT
	};

    /// <summary>
	/// Reference frame (world or camera) for tracking and depth sensing.
    /// </summary>
    public enum REFERENCE_FRAME
    {
        /// <summary>
        /// Matrix contains the total displacement from the world origin/the first tracked point. 
        /// </summary>
        WORLD,
        /// <summary>
        /// Matrix contains the displacement from the previous camera position to the current one. 
        /// </summary>
        CAMERA
    };

    /// <summary>
    /// Possible states of the ZED's Tracking system.
    /// </summary>
    public enum TRACKING_STATE
    {
        /// <summary>
        /// Tracking is searching for a match from the database to relocate to a previously known position.
        /// </summary>
        TRACKING_SEARCH,
        /// <summary>
        /// Tracking is operating normally; tracking data should be correct.
        /// </summary>
        TRACKING_OK,
        /// <summary>
        /// Tracking is not enabled. 
        /// </summary>
        TRACKING_OFF
    }

    /// <summary>
    /// SVO compression modes.
    /// </summary>
    public enum SVO_COMPRESSION_MODE
    {
        /// <summary>
        /// RAW images; no compression. This option can lead to extremely large file sizes. 
        /// </summary>
        RAW_BASED,
        /// <summary>
        /// Lossless compression based on png/zstd. Average size = 42% of RAW.
        /// </summary>
        LOSSLESS_BASED,
        /// <summary>
        /// Lossy compression based on jpeg. Average size = 22% of RAW.
        /// </summary>
        LOSSY_BASED,
		/// <summary>
		/// AVCHD Based compression (H264). Available since ZED SDK 2.7
		/// </summary>
		AVCHD_BASED,
		/// <summary>
		/// HEVC Based compression (H265). Available since ZED SDK 2.7
		/// </summary>
		HEVC_BASED,
    }


    /// <summary>
    /// Streaming codecs
    /// </summary>
    public enum STREAMING_CODEC
    {
        /// <summary>
        /// AVCHD Based compression (H264)
        /// </summary>
        AVCHD_BASED,
        /// <summary>
        /// HEVC Based compression (H265)
        /// </summary>
        HEVC_BASED
    }

    /// <summary>
    /// Mesh formats that can be saved/loaded with spatial mapping. 
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
        /// Contains vertices, normals, faces, and texture information (if possible).
        /// </summary>
        OBJ
    }

    /// <summary>
    /// Presets for filtering meshes scannedw ith spatial mapping. Higher values reduce total face count by more. 
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
        /// Drastically reduce the number of faces.
        /// </summary>
        HIGH,
    }

    /// <summary>
    /// Possible states of the ZED's Spatial Mapping system.
    /// </summary>
    public enum SPATIAL_MAPPING_STATE
    {
        /// <summary>
        /// Spatial mapping is initializing.
        /// </summary>
        SPATIAL_MAPPING_STATE_INITIALIZING,
        /// <summary>
        /// Depth and tracking data were correctly integrated into the fusion algorithm.
        /// </summary>
        SPATIAL_MAPPING_STATE_OK,
        /// <summary>
        /// Maximum memory dedicated to scanning has been reached; the mesh will no longer be updated.
        /// </summary>
        SPATIAL_MAPPING_STATE_NOT_ENOUGH_MEMORY,
        /// <summary>
        /// EnableSpatialMapping() wasn't called (or the scanning was stopped and not relaunched).
        /// </summary>
        SPATIAL_MAPPING_STATE_NOT_ENABLED,
        /// <summary>
        /// Effective FPS is too low to give proper results for spatial mapping. 
        /// Consider using performance-friendly parameters (DEPTH_MODE_PERFORMANCE, VGA or HD720 camera resolution, 
        /// and LOW spatial mapping resolution).
        /// </summary>
        SPATIAL_MAPPING_STATE_FPS_TOO_LOW
    }

    /// <summary>
    /// Units used by the SDK for measurements and tracking. METER is best to stay consistent with Unity.
    /// </summary>
    public enum UNIT
    {
        /// <summary>
        /// International System, 1/1000 meters.
        /// </summary>
        MILLIMETER,
        /// <summary>
        /// International System, 1/100 meters.
        /// </summary>
        CENTIMETER,
        /// <summary>
        /// International System, 1/1 meters.
        /// </summary>
        METER,
        /// <summary>
        ///  Imperial Unit, 1/12 feet.
        /// </summary>
        INCH,
        /// <summary>
        ///  Imperial Unit, 1/1 feet.
        /// </summary>
        FOOT
    }

    /// <summary>
    /// Struct containing all parameters passed to the SDK when initializing the ZED. 
    /// These parameters will be fixed for the whole execution life time of the camera.
    /// </summary><remarks>For more details, see the InitParameters class in the SDK API documentation:
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/structsl_1_1InitParameters.html </remarks>
    /// </summary>
    public class InitParameters
    {
        public sl.INPUT_TYPE inputType;
        /// <summary>
        /// Resolution the ZED will be set to.  
        /// </summary>
        public sl.RESOLUTION resolution;
        /// <summary>
        /// Requested FPS for this resolution. Setting it to 0 will choose the default FPS for this resolution. 
        /// </summary>
        public int cameraFPS;
        /// <summary>
        /// ID for identifying which of multiple connected ZEDs to use. 
        /// </summary>
        public int cameraID;
        /// <summary>
        /// Path to a recorded SVO file to play, including filename.
        /// </summary>
        public string pathSVO = "";
        /// <summary>
        /// In SVO playback, this mode simulates a live camera and consequently skipped frames if the computation framerate is too slow.
        /// </summary>
        public bool svoRealTimeMode;
        /// <summary>
        ///  Define a unit for all metric values (depth, point clouds, tracking, meshes, etc.) Meters are recommended for Unity. 
        /// </summary>
        public UNIT coordinateUnit;
        /// <summary>
        /// This defines the order and the direction of the axis of the coordinate system. 
        /// LEFT_HANDED_Y_UP is recommended to match Unity's coordinates. 
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
        ///  Defines if images are horizontally flipped.
        /// </summary>
        public bool cameraImageFlip;
        /// <summary>
        /// Defines if measures relative to the right sensor should be computed (needed for MEASURE_<XXX>_RIGHT).
        /// </summary>
        public bool enableRightSideMeasure;
        /// <summary>
        /// True to disable self-calibration and use the optional calibration parameters without optimizing them.
        /// False is recommended, so that calibration parameters can be optimized.
        /// </summary>
        public bool cameraDisableSelfCalib;
        /// <summary>
        /// Set the number of buffers for the internal buffer process. LINUX ONLY - NOT CURRENTLY USED IN UNITY PLUGIN. 
        /// </summary>
        public int cameraBufferCountLinux;
        /// <summary>
        /// True for the SDK to provide text feedback. 
        /// </summary>
        public bool sdkVerbose;
        /// <summary>
        /// ID of the graphics card on which the ZED's computations will be performed. 
        /// </summary>
        public int sdkGPUId;
        /// <summary>
        /// If set to verbose, the filename of the log file into which the SDK will store its text output.
        /// </summary>
        public string sdkVerboseLogFile = "";
        /// <summary>
        /// True to stabilize the depth map. Recommended. 
        /// </summary>
        public bool depthStabilization;
		/// <summary>
		/// Optional path for searching configuration (calibration) file SNxxxx.conf. (introduced in ZED SDK 2.6)
		/// </summary>
		public string optionalSettingsPath = "";
		/// <summary>
		/// True to stabilize the depth map. Recommended. 
		/// </summary>
		public bool cameraDisableIMU;
        /// <summary>
        /// Path to a recorded SVO file to play, including filename.
        /// </summary>
        public string ipStream = "";
        /// <summary>
        /// Path to a recorded SVO file to play, including filename.
        /// </summary>
        public ushort portStream = 30000;

        /// <summary>
        /// Constructor. Sets default initialization parameters recommended for Unity. 
        /// </summary>
        public InitParameters()
        {
            this.inputType = sl.INPUT_TYPE.INPUT_TYPE_USB;
            this.resolution = RESOLUTION.HD720;
            this.cameraFPS = 60;
            this.cameraID = 0;
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
			this.optionalSettingsPath = "";
			this.cameraDisableIMU = false;
            this.ipStream = "";
            this.portStream = 30000;
        }

    }
    /// <summary>
    /// List of available coordinate systems. Left-Handed, Y Up is recommended to stay consistent with Unity. 
    /// consistent with Unity. 
    /// </summary>
    public enum COORDINATE_SYSTEM
    {
        /// <summary>
        /// Standard coordinates system used in computer vision. 
        /// Used in OpenCV. See: http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html 
        /// </summary>
        IMAGE,
        /// <summary>
        /// Left-Handed with Y up and Z forward. Recommended. Used in Unity with DirectX.
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
    ///  Possible states of the ZED's spatial memory area export, for saving 3D features used 
    ///  by the tracking system to relocalize the camera. This is used when saving a mesh generated
    ///  by spatial mapping when Save Mesh is enabled - a .area file is saved as well. 
    /// </summary>
    public enum AREA_EXPORT_STATE
    {
        /// <summary>
        /// Spatial memory file has been successfully created.
        /// </summary>
        AREA_EXPORT_STATE_SUCCESS,
        /// <summary>
        /// Spatial memory file is currently being written to.
        /// </summary>
        AREA_EXPORT_STATE_RUNNING,
        /// <summary>
        /// Spatial memory file export has not been called.
        /// </summary>
        AREA_EXPORT_STATE_NOT_STARTED,
        /// <summary>
        /// Spatial memory contains no data; the file is empty.
        /// </summary>
        AREA_EXPORT_STATE_FILE_EMPTY,
        /// <summary>
        /// Spatial memory file has not been written to because of a bad file name.
        /// </summary>
        AREA_EXPORT_STATE_FILE_ERROR,
        /// <summary>
        /// Spatial memory has been disabled, so no file can be created.
        /// </summary>
        AREA_EXPORT_STATE_SPATIAL_MEMORY_DISABLED
    };


    /// <summary>
    /// Runtime parameters used by the ZEDCamera.Grab() function, and its Camera::grab() counterpart in the SDK. 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RuntimeParameters {
        /// <summary>
        /// Defines the algorithm used for depth map computation, more info : \ref SENSING_MODE definition.
        /// </summary>
        [FieldOffset(12)] //In 2.2, the runtime parameters need 3 int of offset.
        public sl.SENSING_MODE sensingMode;
        /// <summary>
        /// Provides 3D measures (point cloud and normals) in the desired reference frame (default is REFERENCE_FRAME_CAMERA).
        /// </summary>
        [FieldOffset(16)]
		public sl.REFERENCE_FRAME measure3DReferenceFrame;
        /// <summary>
        /// Defines whether the depth map should be computed.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(20)]
        public bool enableDepth;
        /// <summary>
        ///  Defines whether the point cloud should be computed (including XYZRGBA).
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(21)]
        public bool enablePointCloud;
    }

	/// <summary>
	/// Part of the ZED (left/right sensor, center) that's considered its center for tracking purposes.
	/// </summary>
	public enum TRACKING_FRAME
	{
        /// <summary>
        /// Camera's center is at the left sensor.
        /// </summary>
		LEFT_EYE,
        /// <summary>
        /// Camera's center is in the camera's physical center, between the sensors.
        /// </summary>
		CENTER_EYE,
        /// <summary>
        /// Camera's center is at the right sensor.
        /// </summary>
		RIGHT_EYE
	};


	/// <summary>
	/// Types of USB device brands.
	/// </summary>
	public enum USB_DEVICE
    {
        /// <summary>
        /// Oculus device, eg. Oculus Rift VR Headset. 
        /// </summary>
		USB_DEVICE_OCULUS,
        /// <summary>
        /// HTC device, eg. HTC Vive. 
        /// </summary>
		USB_DEVICE_HTC,
        /// <summary>
        /// Stereolabs device, eg. ZED/ZED Mini. 
        /// </summary>
		USB_DEVICE_STEREOLABS
	};





}// end namespace sl
