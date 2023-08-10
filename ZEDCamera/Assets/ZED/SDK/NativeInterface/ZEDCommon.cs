//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
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
        public static bool IsVector3NaN(Vector3 input)
        {
            if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsNaN(input.z))
                return true;
            else
                return false;
        }
    }

	public enum ZED_CAMERA_ID
	{
		CAMERA_ID_01,
		CAMERA_ID_02,
		CAMERA_ID_03,
		CAMERA_ID_04,
        CAMERA_ID_05,
        CAMERA_ID_06,
        CAMERA_ID_07,
        CAMERA_ID_08
    };


    public enum INPUT_TYPE
    {
        INPUT_TYPE_USB,
        INPUT_TYPE_SVO,
        INPUT_TYPE_STREAM,
        INPUT_TYPE_GMSL
    };

    /// <summary>
    /// Constant for plugin. Should not be changed
    /// </summary>
    public enum Constant
	{
		MAX_CAMERA_PLUGIN = 8,
		PLANE_DISTANCE = 10,
        MAX_OBJECTS = 75,
        MAX_BATCH_SIZE = 200
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
        /// <summary>
        /// boolean that indicates if tracking is activated or not. You should check that first if something wrong.
        /// </summary>
        public bool valid;
        /// <summary>
        /// Timestamp of the pose. This timestamp should be compared with the camera timestamp for synchronization.
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// orientation from the pose.
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// translation from the pose.
        /// </summary>
        public Vector3 translation;
        /// <summary>
        /// Confidence/Quality of the pose estimation for the target frame.
        /// A confidence metric of the tracking[0 - 100], 0 means that the tracking is lost, 100 means that the tracking can be fully trusted.
        /// </summary>
        public int poseConfidence;
        /// <summary>
        /// 6x6 Pose covariance of translation (the first 3 values) and rotation in so3 (the last 3 values)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public float[] poseCovariance;
        /// <summary>
        /// Twist of the camera available in reference camera, this expresses velocity in free space, broken into its linear and angular parts.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] twist;
        /// <summary>
        /// Row-major representation of the 6x6 twist covariance matrix of the camera, this expresses the uncertainty of the twist.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public float[] twistCovariance;
    };

    /// <summary>
    /// Rect structure to define a rectangle or a ROI in pixels
    /// Use to set ROI target for AEC/AGC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int x;
        public int y;
        public int width;
        public int height;

        Rect(int x_ = 0, int y_ = 0, int width_= 0, int height_= 0)
        {
            this.x = x_;
            this.y = y_;
            this.width = width_;
            this.height = height_;
        }
    };

    public enum CAMERA_STATE
    {
        /// <summary>
        /// Defines if the camera can be openned by the sdk
        /// </summary>
        AVAILABLE,
        /// <summary>
        /// Defines if the camera is already opened and unavailable
        /// </summary>
        NOT_AVAILABLE
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceProperties
    {
        /// <summary>
        /// The camera state
        /// </summary>
        public sl.CAMERA_STATE cameraState;
        /// <summary>
        /// The camera id (Notice that only the camera with id '0' can be used on Windows)
        /// </summary>
        public int id;
        /// <summary>
        /// The camera model
        /// </summary>
        public sl.MODEL cameraModel;
        /// <summary>
        /// The camera serial number
        /// </summary>
        public int sn;
    };

    /// <summary>
    /// Streaming device properties
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StreamingProperties
    {
        /// <summary>
        /// The streaming IP of the device
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ip;
        /// <summary>
        /// The streaming port
        /// </summary>
        public ushort port;
        /// <summary>
        /// The current bitrate of encoding of the streaming device
        /// </summary>
        public int currentBitrate;
        /// <summary>
        /// The current codec used for compression in streaming device
        /// </summary>
        public sl.STREAMING_CODEC codec;
    };

    /// <summary>
    /// Container for information about the current SVO recording process.
    /// </summary><remarks>
    /// Mirrors RecordingStatus in the ZED C++ SDK. For more info, visit:
    /// https://www.stereolabs.com/docs/api/structsl_1_1RecordingStatus.html
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct RecordingStatus
    {
        /// <summary>
        /// Recorder status, true if enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool isRecording;
        /// <summary>
        /// Recorder status, true if the pause is enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool isPaused;
        /// <summary>
        /// Status of the current frame. True if recording was successful, false if frame could not be written.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool status;
        /// <summary>
        /// Compression time for the current frame in milliseconds.
        /// </summary>
        public double currentCompressionTime;
        /// <summary>
        /// Compression ratio (% of raw size) for the current frame.
        /// </summary>
        public double currentCompressionRatio;
        /// <summary>
        /// Average compression time in millisecond since beginning of recording.
        /// </summary>
        public double averageCompressionTime;
        /// <summary>
        /// Compression ratio (% of raw size) since recording was started.
        /// </summary>
        public double averageCompressionRatio;
    }

    /// <summary>
    /// Full IMU data structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
	public struct ImuData
	{
        /// <summary>
        /// Indicates if imu data is available
        /// </summary>
        public bool available;
        /// <summary>
        /// IMU Data timestamp in ns
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// Gyroscope calibrated data in degrees/second.
        /// </summary>
		public Vector3 angularVelocity;
        /// <summary>
        /// Accelerometer calibrated data in m/s².
        /// </summary>
		public Vector3 linearAcceleration;
        /// <summary>
        /// Gyroscope raw/uncalibrated data in degrees/second.
        /// </summary>
        public Vector3 angularVelocityUncalibrated;
        /// <summary>
        /// Accelerometer raw/uncalibrated data in m/s².
        /// </summary>
		public Vector3 linearAccelerationUncalibrated;
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
	};


    [StructLayout(LayoutKind.Sequential)]
    public struct BarometerData
    {
        /// <summary>
        /// Indicates if mag data is available
        /// </summary>
        public bool available;
        /// <summary>
        /// mag Data timestamp in ns
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// Barometer ambient air pressure in hPa
        /// </summary>
        public float pressure;
        /// <summary>
        /// Relative altitude from first camera position
        /// </summary>
        public float relativeAltitude;
    };

    public enum HEADING_STATE
    {
        /// <summary>
        /// The heading is reliable and not affected by iron interferences.
        /// </summary>
        GOOD,
        /// <summary>
        /// The heading is reliable, but affected by slight iron interferences.
        /// </summary>
        OK,
        /// <summary>
        /// The heading is not reliable because affected by strong iron interferences.
        /// </summary>
        NOT_GOOD,
        /// <summary>
        /// The magnetometer has not been calibrated.
        /// </summary>
        NOT_CALIBRATED,
        /// <summary>
        /// The magnetomer sensor is not available.
        /// </summary>
        MAG_NOT_AVAILABLE
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MagnetometerData
    {
        /// <summary>
        /// Indicates if mag data is available
        /// </summary>
        public bool available;
        /// <summary>
        /// mag Data timestamp in ns
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// Magnetic field calibrated values in uT
        /// </summary>
        /// </summary>
        public Vector3 magneticField;
        /// <summary>
        /// Magnetic field raw values in uT
        /// </summary>
        public Vector3 magneticFieldUncalibrated;
        /// <summary>
        /// The camera heading in degrees relative to the magnetic North Pole.
        /// note: The magnetic North Pole has an offset with respect to the geographic North Pole, depending on the
        /// geographic position of the camera.
        /// To get a correct magnetic heading the magnetometer sensor must be calibrated using the ZED Sensor Viewer tool
        /// </summary>
        public float magneticHeading;
        /// <summary>
        /// The state of the /ref magnetic_heading value
        /// </summary>
        public HEADING_STATE magneticHeadingState;
        /// <summary>
        /// The accuracy of the magnetic heading measure in the range [0.0,1.0].
        /// A negative value means that the magnetometer must be calibrated using the ZED Sensor Viewer tool
        /// </summary>
        public float magneticHeadingAccuracy;

    };


    [StructLayout(LayoutKind.Sequential)]
    public struct TemperatureData
    {
        /// <summary>
        /// Temperature from IMU device ( -100 if not available)
        /// </summary>
        public float imuTemp;
        /// <summary>
        /// Temperature from Barometer device ( -100 if not available)
        /// </summary>
        public float barometerTemp;
        /// <summary>
        /// Temperature from Onboard left analog temperature sensor ( -100 if not available)
        /// </summary>
        public float onboardLeftTemp;
        /// <summary>
        /// Temperature from Onboard right analog temperature sensor ( -100 if not available)
        /// </summary>
        public float onboardRightTemp;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SensorsData
    {
        /// <summary>
        /// Contains Imu Data
        /// </summary>
        public ImuData imu;
        /// <summary>
        /// Contains Barometer Data
        /// </summary>
        public BarometerData barometer;
        /// <summary>
        /// Contains Mag Data
        /// </summary>
        public MagnetometerData magnetometer;
        /// <summary>
        /// Contains Temperature Data
        /// </summary>
        public TemperatureData temperatureSensor;
        /// <summary>
        /// Indicated if camera is :
        /// -> Static : 0
        /// -> Moving : 1
        /// -> Falling : 2
        /// </summary>
        public int cameraMovingState;
        /// <summary>
        /// Indicates if the current sensors data is sync to the current image (>=1). Otherwise, value will be 0.
        /// </summary>
        public int imageSyncTrigger;
    };

    /*******************************************************************************************************************************
     *******************************************************************************************************************************/

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
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 12)]
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
        /// <summary>
        /// Real focal length in millimeters
        /// </summary>
        public float focalLengthMetric;

    };

    /// <summary>
    /// List of the available onboard sensors
    /// </summary>
    public enum SENSOR_TYPE
    {
        /// <summary>
        /// Three axis Accelerometer sensor to measure the inertial accelerations.
        /// </summary>
        ACCELEROMETER,
        /// <summary>
        /// Three axis Gyroscope sensor to measure the angular velocitiers.
        /// </summary>
        GYROSCOPE,
        /// <summary>
        /// Three axis Magnetometer sensor to measure the orientation of the device respect to the earth magnetic field.
        /// </summary>
        MAGNETOMETER,
        /// <summary>
        /// Barometer sensor to measure the atmospheric pressure.
        /// </summary>
        BAROMETER,

        LAST
    };

    /// <summary>
    /// List of the available onboard sensors measurement units
    /// </summary>
    public enum SENSORS_UNIT
    {
        /// <summary>
        /// Acceleration [m/s²].
        /// </summary>
        M_SEC_2,
        /// <summary>
        /// Angular velocity [deg/s].
        /// </summary>
        DEG_SEC,
        /// <summary>
        /// Magnetic Fiels [uT].
        /// </summary>
        U_T,
        /// <summary>
        /// Atmospheric pressure [hPa].
        /// </summary>
        HPA,
        /// <summary>
        /// Temperature [°C].
        /// </summary>
        CELSIUS,
        /// <summary>
        /// Frequency [Hz].
        /// </summary>
        HERTZ,
        /// <summary>
        ///
        /// </summary>
        LAST
    };

    /// <summary>
    /// Structure containing information about a single sensor available in the current device
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorParameters
    {
        /// <summary>
        /// The type of the sensor as \ref DEVICE_SENSORS.
        /// </summary>
        public SENSOR_TYPE type;
        /// <summary>
        /// The resolution of the sensor.
        /// </summary>
        public float resolution;
        /// <summary>
        /// The sampling rate (or ODR) of the sensor.
        /// </summary>
        public float samplingRate;
        /// <summary>
        /// The range values of the sensor. MIN: `range.x`, MAX: `range.y`
        /// </summary>
        public float2 range;
        /// <summary>
        /// also known as white noise, given as continous (frequency independant). Units will be expressed in sensor_unit/√(Hz). `NAN` if the information is not available.
        /// </summary>
        public float noiseDensity;
        /// <summary>
        /// derived from the Allan Variance, given as continous (frequency independant). Units will be expressed in sensor_unit/s/√(Hz).`NAN` if the information is not available.
        /// </summary>
        public float randomWalk;
        /// <summary>
        /// The string relative to the measurement unit of the sensor.
        /// </summary>
        public SENSORS_UNIT sensorUnit;
        /// <summary>
        ///
        /// </summary>
        public bool isAvailable;
    };

    /// <summary>
    /// Structure containing information about all the sensors available in the current device
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorsConfiguration
    {
        /// <summary>
        /// The firmware version of the sensor module, 0 if no sensors are available (ZED camera model).
        /// </summary>
        public uint firmwareVersion;
        /// <summary>
        /// contains rotation between IMU frame and camera frame.
        /// </summary>
        public float4 cameraImuRotation;
        /// <summary>
        /// contains translation between IMU frame and camera frame.
        /// </summary>
        public float3 cameraImuTranslation;
        /// <summary>
        /// Magnetometer to IMU rotation. contains rotation between IMU frame and magnetometer frame.
        /// </summary>
        public float4 imuMagnometerRotation;
        /// <summary>
        /// Magnetometer to IMU translation. contains translation between IMU frame and magnetometer frame.
        /// </summary>
        public float3 imuMagnometerTranslation;
        /// <summary>
        /// Magnetometer to IMU rotation. contains rotation between IMU frame and magnetometer frame.
        /// </summary>
        public float4 imu_magnometer_rotation;
        /// <summary>
        /// Magnetometer to IMU translation. contains translation between IMU frame and magnetometer frame.
        /// </summary>
        public float3 imu_magnometer_translation;
        /// <summary>
        /// Configuration of the accelerometer device.
        /// </summary>
        public SensorParameters accelerometerParameters;
        /// <summary>
        /// Configuration of the gyroscope device.
        /// </summary>
        public SensorParameters gyroscopeParameters;
        /// <summary>
        /// Configuration of the magnetometer device.
        /// </summary>
        public SensorParameters magnetometerParameters;
        /// <summary>
        /// Configuration of the barometer device
        /// </summary>
        public SensorParameters barometerParameters;
        /// <summary>
        /// if a sensor type is available on the device
        /// </summary>
        /// <param name="sensor_type"></param>
        /// <returns></returns>
        public bool isSensorAvailable(SENSOR_TYPE sensor_type)
        {
            switch (sensor_type)
            {
                case SENSOR_TYPE.ACCELEROMETER:
                    return accelerometerParameters.isAvailable;
                case SENSOR_TYPE.GYROSCOPE:
                    return gyroscopeParameters.isAvailable;
                case SENSOR_TYPE.MAGNETOMETER:
                    return magnetometerParameters.isAvailable;
                case SENSOR_TYPE.BAROMETER:
                    return barometerParameters.isAvailable;
                default:
                    break;
            }
            return false;
        }
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
        /// Parameters of the left sensor.
        /// </summary>
        public CameraParameters leftCam;
        /// <summary>
        /// Parameters of the right sensor.
        /// </summary>
        public CameraParameters rightCam;
        /// <summary>
        /// Rotation (using Rodrigues' transformation) between the two sensors. Defined as 'tilt', 'convergence' and 'roll'.
        /// </summary>
        public Quaternion Rot;
        /// <summary>
        /// Translation between the two sensors. T[0] is the distance between the two cameras in meters.
        /// </summary>
        public Vector3 Trans;
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
        public double currentCompressionTime;
        /// <summary>
        /// Compression ratio (% of raw size) for the current frame.
        /// </summary>
        public double currentCompressionRatio;
        /// <summary>
        /// Average compression time in millisecond since beginning of recording.
        /// </summary>
        public double averageCompressionTime;
        /// <summary>
        /// Compression ratio (% of raw size) since recording was started.
        /// </summary>
        public double averageCompressionRatio;
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
        /// Computation mode designed for challenging areas with untextured surfaces.
        /// </summary>
        QUALITY,
		/// <summary>
		/// Native depth. Very accurate, but at a large performance cost.
		/// </summary>
		ULTRA,
        /// <summary>
        ///  End to End Neural disparity estimation, requires AI module
        /// </summary>
        NEURAL
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
		VIEW_NORMALS,
	};


    /// <summary>
    /// List of error codes in the ZED SDK.
    /// </summary><remarks>
    /// Mirrors ERROR_CODE in the ZED C++ SDK. For more info, read:
    /// https://www.stereolabs.com/developers/documentation/API/v2.5.1/group__Camera__group.html#ga4db9ee29f2ff83c71567c12f6bfbf28c
    /// </remarks>
    public enum ERROR_CODE
    {
        CAMERA_REBOOTING = -1,
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
        /// The MCU that controls the sensors module has an invalid Serial Number. You can try to recover it launching the 'ZED Diagnostic' tool from the command line with the option '-r'.
        /// </summary>
        SENSORS_NOT_INITIALIZED,
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
        /// SVO end of file has been reached, and no frame will be available until the SVO position is reset.
        /// </summary>
        END_OF_SVOFILE_REACHED,
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
        /// No GPU found or CUDA is unable to list it. Can be a driver/reboot issue.
        /// </summary>
        NO_GPU_DETECTED,
        /// <summary>
        /// Plane not found. Either no plane is detected in the scene, at the location or corresponding to the floor,
        /// or the floor plane doesn't match the prior given.
        /// </summary>
        PLANE_NOT_FOUND,
        /// <summary>
        /// The Object detection module is only compatible with the ZED 2
        /// </summary>
        MODULE_NOT_COMPATIBLE_WITH_CAMERA,
        /// <summary>
        /// The module needs the sensors to be enabled (see InitParameters::sensors_required)
        /// </summary>
        MOTION_SENSORS_REQUIRED,
        /// <summary>
        /// The module needs a newer version of CUDA
        /// </summary>
        MODULE_NOT_COMPATIBLE_WITH_CUDA_VERSION,
        /// <summary>
        /// End of ERROR_CODE
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
        /// 1920*1200 (x2), available framerates: 30,60 fps. (ZED-X(M) only)
        /// </summary>
        HD1200,
        /// <summary>
        /// 1280*720. Supported frame rates: 15, 30, 60 FPS.
        /// </summary>
        HD720,
        /// <summary>
        /// 960*600 (x2), available framerates: 60, 120 fps. (ZED-X(M) only)
        /// </summary>
        SVGA,
        /// <summary>
        /// 672*376. Supported frame rates: 15, 30, 60, 100 FPS.
        /// </summary>
        VGA,
        /// <summary>
        /// Select the resolution compatible with camera, on ZEDX HD1200, HD720 otherwise
        /// </summary>
        AUTO
    };

    /// <summary>
    /// Represents the available resolution options.
    /// </summary>
    public enum USB_RESOLUTION
    {
        /// <summary>
        /// 2208*1242. Supported frame rate: 15 FPS.
        /// </summary>
        HD2K = 0,
        /// <summary>
        /// 1920*1080. Supported frame rates: 15, 30 FPS.
        /// </summary>
        HD1080 = 1,
        /// <summary>
        /// 1280*720. Supported frame rates: 15, 30, 60 FPS.
        /// </summary>
        HD720 = 3,
        /// <summary>
        /// 672*376. Supported frame rates: 15, 30, 60, 100 FPS.
        /// </summary>
        VGA = 5,
        /// <summary>
        /// Select the resolution compatible with camera, on ZEDX HD1200, HD720 otherwise
        /// </summary>
        AUTO = 6
    };


    /// <summary>
    /// Types of compatible ZED cameras.
    /// </summary>
    public enum MODEL
	{
        /// <summary>
        /// ZED(1)
        /// </summary>
		ZED,
        /// <summary>
        /// ZED Mini.
        /// </summary>
		ZED_M,
        /// <summary>
        /// ZED2.
        /// </summary>
        ZED2,
        /// <summary>
        /// ZED2i
        /// </summary>
        ZED2i,
        /// <summary>
        /// ZED X
        /// </summary>
        ZED_X,
        /// <summary>
        /// ZED X Mini
        /// </summary>
        ZED_XM
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
        /// Saturation control. Value should be between 0 and 8.
        /// </summary>
        SATURATION,
        /// <summary>
        /// Sharpness control. Value should be between 0 and 8.
        /// </summary>
        SHARPNESS,
        /// <summary>
        /// Gamma control. Value should be between 1 and 9
        /// </summary>
        GAMMA,
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
        /// Auto-exposure and auto gain. Setting this to true switches on both. Assigning a specifc value to GAIN or EXPOSURE will set this to 0.
        /// </summary>
        AEC_AGC,
        /// <summary>
        /// ROI for auto exposure/gain. ROI defines the target where the AEC/AGC will be calculated
        /// Use overloaded function for this enum
        /// </summary>
        AEC_AGC_ROI,
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
        NORMALS_RIGHT,
        /// <summary>
        /// Depth map in millimeter. Each pixel  contains 1 unsigned short. As a ZEDMat, MAT_TYPE is set to MAT_U16_C1.
        /// </summary>
        DEPTH_U16_MM,
        /// <summary>
        /// Depth map in millimeter for right sensor. Each pixel  contains 1 unsigned short. As a ZEDMat, MAT_TYPE is set to MAT_U16_C1.
        /// </summary>
        DEPTH_U16_MM_RIGHT
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
        /// The camera is searching for a previously known position to locate itself.
        /// </summary>
        TRACKING_SEARCH,
        /// <summary>
        /// Tracking is operating normally; tracking data should be correct.
        /// </summary>
        TRACKING_OK,
        /// <summary>
        /// Tracking is not enabled.
        /// </summary>
        TRACKING_OFF,
        /// <summary>
        /// Effective FPS is too low to give proper results for motion tracking. Consider using PERFORMANCES parameters (DEPTH_MODE_PERFORMANCE, low camera resolution (VGA,HD720))
        /// </summary>
        TRACKING_FPS_TOO_LOW,
        /// <summary>
        /// The camera is searching for the floor plane to locate itself related to it, the REFERENCE_FRAME::WORLD will be set afterward.
        /// </summary>
        TRACKING_SEARCHING_FLOOR_PLANE
    }

    /// <summary>
    /// Lists the mode of positional tracking that can be used.
    /// </summary>
    public enum POSTIONAL_TRACKING_MODE
    {
        /// <summary>
        ///  Default mode, best compromise in performance and accuracy
        /// </summary>
        STANDARD,
        /// <summary>
        /// Improve accuracy in more challening scenes such as outdoor repetitive patterns like extensive field. Curently works best with ULTRA depth mode, requires more compute power 
        /// </summary>
        QUALITY
    }

    /// <summary>
    /// SVO compression modes.
    /// </summary>
    public enum SVO_COMPRESSION_MODE
    {
        /// <summary>
        /// Lossless compression based on png/zstd. Average size = 42% of RAW.
        /// </summary>
        LOSSLESS_BASED,
        /// <summary>
        /// H264(AVCHD) GPU based compression : avg size = 1% (of RAW). Requires a NVIDIA GPU
        /// </summary>
        H264_BASED,
        /// <summary>
        /// H265(HEVC) GPU based compression: avg size = 1% (of RAW). Requires a NVIDIA GPU, Pascal architecture or newer
        /// </summary>
        H265_BASED,
        /// <summary>
        /// H264 Lossless GPU/Hardware based compression: avg size = 25% (of RAW). Provides a SSIM/PSNR result (vs RAW) >= 99.9%. Requires a NVIDIA GPU
        /// </summary>
        H264_LOSSLESS_BASED,
        /// <summary>
        /// H265 Lossless GPU/Hardware based compression: avg size = 25% (of RAW). Provides a SSIM/PSNR result (vs RAW) >= 99.9%. Requires a NVIDIA GPU
        /// </summary>
        H265_LOSSLESS_BASED,
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
    /// Spatial Mapping type (default is mesh)
    /// </summary>
    public enum SPATIAL_MAP_TYPE
    {
        /// <summary>
        /// Represent a surface with faces, 3D points are linked by edges, no color information
        /// </summary>
        MESH,
        /// <summary>
        ///  Geometry is represented by a set of 3D colored points.
        /// </summary>
        FUSED_POINT_CLOUD
    };



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
        public int cameraDeviceID;
        /// <summary>
        /// Serial number of the camera to open
        /// </summary>
        public uint serialNumber;
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
        ///   When estimating the depth, the SDK uses this upper limit to turn higher values into \ref TOO_FAR ones.
        ///   The current maximum distance that can be computed in the defined \ref UNIT.
        ///   Changing this value has no impact on performance and doesn't affect the positional tracking nor the spatial mapping. (Only the depth, point cloud, normals)
        /// </summary>
        public float depthMaximumDistance;
        /// <summary>
        ///  Defines if images are horizontally flipped.
        /// </summary>
        public int cameraImageFlip;
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
        /// True for the SDK to provide text feedback.
        /// </summary>
        public int sdkVerbose;
        /// <summary>
        /// ID of the graphics card on which the ZED's computations will be performed.
        /// </summary>
        public int sdkGPUId;
        /// <summary>
        /// If set to verbose, the filename of the log file into which the SDK will store its text output.
        /// </summary>
        public string sdkVerboseLogFile = "";
        /// <summary>
        /// This sets the depth stabilizer temporal smoothing strength.
        /// the depth stabilize smooth range is [0, 100]
        /// 0 means a low temporal smmoothing behavior(for highly dynamic scene),
        /// 100 means a high temporal smoothing behavior(for static scene)
        /// </summary>
        public int depthStabilization;
		/// <summary>
		/// Optional path for searching configuration (calibration) file SNxxxx.conf. (introduced in ZED SDK 2.6)
		/// </summary>
		public string optionalSettingsPath = "";
		/// <summary>
		/// True to stabilize the depth map. Recommended.
		/// </summary>
		public bool sensorsRequired;
        /// <summary>
        /// Path to a recorded SVO file to play, including filename.
        /// </summary>
        public string ipStream = "";
        /// <summary>
        /// Path to a recorded SVO file to play, including filename.
        /// </summary>
        public ushort portStream = 30000;
        /// <summary>
        /// Whether to enable improved color/gamma curves added in ZED SDK 3.0.
        /// </summary>
        public bool enableImageEnhancement = true;
        /// <summary>
        /// Set an optional file path where the SDK can find a file containing the calibration information of the camera computed by OpenCV.
        /// <remarks> Using this will disable the factory calibration of the camera. </remarks>
        /// <warning> Erroneous calibration values can lead to poor SDK modules accuracy. </warning>
        /// </summary>
        public string optionalOpencvCalibrationFile = "";
        /// <summary>
        /// Define a timeout in seconds after which an error is reported if the \ref open() command fails.
        /// Set to '-1' to try to open the camera endlessly without returning error in case of failure.
        /// Set to '0' to return error in case of failure at the first attempt.
        /// This parameter only impacts the LIVE mode.
        /// </summary>
        public float openTimeoutSec = 5f;

        /// <summary>
        /// Define the behavior of the automatic camera recovery during grab() function call. When async is enabled and there's an issue with the communication with the camera
        /// the grab() will exit after a short period and return the ERROR_CODE::CAMERA_REBOOTING warning.The recovery will run in the background until the correct communication is restored.
        /// When async_grab_camera_recovery is false, the grab() function is blocking and will return only once the camera communication is restored or the timeout is reached.
        /// The default behavior is synchronous, like previous ZED SDK versions
        /// </summary>
        public bool asyncGrabCameraRecovery = false;


        /// </summary>
        /// Define a computation upper limit to the grab frequency. 0 means that the setting is ignored.
        /// This can be useful to get a known constant fixed rate or limit the computation load while keeping a short exposure time by setting a high camera capture framerate.
        /// The value should be inferior to the InitParameters::camera_fps and strictly positive. It has no effect when reading an SVO file.
        /// This is an upper limit and won't make a difference if the computation is slower than the desired compute capping fps.
        /// Internally the grab function always tries to get the latest available image while respecting the desired fps as much as possible.
        /// Default value is 0.
        /// </summary>
        public float grabComputeCappingFPS = 0f;

        /// <summary>
        /// Constructor. Sets default initialization parameters recommended for Unity.
        /// </summary>
        public InitParameters()
        {
            this.inputType = sl.INPUT_TYPE.INPUT_TYPE_USB;
            this.resolution = RESOLUTION.HD720;
            this.cameraFPS = 60;
            this.cameraDeviceID = 0;
            this.serialNumber = 0;
            this.pathSVO = "";
            this.svoRealTimeMode = false;
            this.coordinateUnit = UNIT.METER;
            this.coordinateSystem = COORDINATE_SYSTEM.LEFT_HANDED_Y_UP;
            this.depthMode = DEPTH_MODE.PERFORMANCE;
            this.depthMinimumDistance = -1;
            this.depthMaximumDistance = -1;
            this.cameraImageFlip = 2;
            this.cameraDisableSelfCalib = false;
            this.sdkVerbose = 0;
            this.sdkGPUId = -1;
            this.sdkVerboseLogFile = "";
            this.enableRightSideMeasure = false;
            this.depthStabilization = -1;
			this.optionalSettingsPath = "";
			this.sensorsRequired = false;
            this.ipStream = "";
            this.portStream = 30000;
            this.enableImageEnhancement = true;
            this.optionalOpencvCalibrationFile = "";
            this.openTimeoutSec = 5.0f;
            this.asyncGrabCameraRecovery = false;
            this.grabComputeCappingFPS = 0f;
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
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeParameters {
        /// <summary>
        /// Provides 3D measures (point cloud and normals) in the desired reference frame (default is REFERENCE_FRAME_CAMERA).
        /// </summary>
        public sl.REFERENCE_FRAME measure3DReferenceFrame;
        /// <summary>
        /// Defines whether the depth map should be computed.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableDepth;
        /// <summary>
        /// Defines if the depth map should be completed or not, similar to the removed SENSING_MODE::FILL.
        /// Warning: Enabling this will override the confidence values confidenceThreshold and textureConfidenceThreshold as well as removeSaturatedAreas
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableFillMode;
        /// <summary>
        ///  Defines the confidence threshold for the depth. Based on stereo matching score.
        /// </summary>
        public int confidenceThreshold;
        /// <summary>
        /// Defines texture confidence threshold for the depth. Based on textureness confidence.
        /// </summary>
        public int textureConfidenceThreshold;
        /// <summary>
        /// Defines if the saturated area (Luminance>=255) must be removed from depth map estimation
        /// </summary>
        public bool removeSaturatedAreas;

    }

    /// <summary>
    /// Sets the plane detection parameters.
    /// </summary>
    public class PlaneDetectionParameters
    {
        /// <summary>
        /// Controls the spread of plane by checking the position difference.
        /// Default is 0.15 meters.
        /// </summary>
        public float maxDistanceThreshold = 0.15f;

        /// <summary>
        /// Controls the spread of plane by checking the angle difference.
        /// Default is 15 degrees.
        /// </summary>
        public float normalSimilarityThreshold = 15.0f;

        public PlaneDetectionParameters()
        {
            this.maxDistanceThreshold = 0.15f;
            this.normalSimilarityThreshold = 15.0f;
        }
        public PlaneDetectionParameters(float maxDistanceThreshold, float normalSimilarityThreshold)
        {
            this.maxDistanceThreshold = maxDistanceThreshold;
            this.normalSimilarityThreshold = normalSimilarityThreshold;
        }
    }


    /// <summary>
    ///brief Lists available compression modes for SVO recording.
    /// </summary>
    public enum FLIP_MODE
    {
        OFF = 0,  ///  default behavior.
        ON = 1,   /// Images and camera sensors data are flipped, useful when your camera is mounted upside down.
        AUTO = 2, /// in live mode: use the camera orientation (if an IMU is available) to set the flip mode, in SVO mode, read the state of this enum when recorded
    };

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

    ////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////  Object Detection /////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// sets batch trajectory parameters
    /// The default constructor sets all parameters to their default settings.
    /// Parameters can be user adjusted.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchParameters
    {
        /// <summary>
        /// Defines if the Batch option in the object detection module is enabled. Batch queueing system provides:
        ///  - Deep-Learning based re-identification
        /// - Trajectory smoothing and filtering
        /// </summary>
        /// <remarks>
        /// To activate this option, enable must be set to true.
        /// </remarks>
        [MarshalAs(UnmanagedType.U1)]
        public bool enable;
        /// <summary>
        /// Max retention time in seconds of a detected object. After this time, the same object will mostly have a different ID.
        /// </summary>
        public float idRetentionTime;
        /// <summary>
        /// Trajectories will be output in batch with the desired latency in seconds.
        /// During this waiting time, re-identification of objects is done in the background.
        /// Specifying a short latency will limit the search (falling in timeout) for previously seen object IDs but will be closer to real time output.
        /// Specifying a long latency will reduce the change of timeout in Re-ID but increase difference with live output.
        /// </summary>
        public float latency;
    }

    /// <summary>
    /// Contains AI model status.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AI_Model_status
    {
        /// <summary>
        /// the model file is currently present on the host.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool downloaded;
        /// <summary>
        /// an engine file with the expected architecure is found.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool optimized;
    };

    /// <summary>
    /// Sets the object detection parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectDetectionParameters
    {
        /// <summary>
        /// Defines a module instance id. This is used to identify which object detection model instance is used.
        /// </summary>
        public uint instanceModuleID;
        /// <summary>
        /// Defines if the object detection is synchronized to the image or runs in a separate thread.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool imageSync;
        /// <summary>
        /// Defines if the object detection will track objects across multiple images, instead of an image-by-image basis.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableTracking;
        /// <summary>
        /// Defines if the SDK will calculate 2D masks for each object. Requires more performance, so don't enable if you don't need these masks.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableSegmentation;
        /// <summary>
        /// Body Format. BODY_FORMAT.POSE_34 automatically enables body fitting.
        /// </summary>
        public sl.OBJECT_DETECTION_MODEL detectionModel;
        /// <summary>
        /// Defines a upper depth range for detections.
        /// Defined in  UNIT set at  sl.Camera.Open.
        /// Default value is set to sl.Initparameters.depthMaximumDistance (can not be higher).
        /// </summary>
        public float maxRange;
        /// <summary>
        /// Batching system parameters.
        /// Batching system(introduced in 3.5) performs short-term re-identification with deep learning and trajectories filtering.
        /// BatchParameters.enable need to be true to use this feature (by default disabled)
        /// </summary>
        public BatchParameters batchParameters;
        /// <summary>
        /// Defines the filtering mode that should be applied to raw detections.
        /// </summary>
        public OBJECT_FILTERING_MODE filteringMode;
        /// <summary>
        /// When an object is not detected anymore, the SDK will predict its positions during a short period of time before switching its state to SEARCHING.
        /// It prevents the jittering of the object state when there is a short misdetection.The user can define its own prediction time duration.
        /// During this time, the object will have OK state even if it is not detected.
        /// The duration is expressed in seconds.
        /// The prediction_timeout_s will be clamped to 1 second as the prediction is getting worst with time.
        /// Set this parameter to 0 to disable SDK predictions.
        /// </summary>
        public float predictionTimeout_s;
        /// <summary>
        /// Allow inference to run at a lower precision to improve runtime and memory usage,
	    /// it might increase the initial optimization time and could include downloading calibration data or calibration cache and slightly reduce the accuracy
        /// \note The fp16 is automatically enabled if the GPU is compatible and provides a speed up of almost x2 and reduce memory usage by almost half, no precision loss.
	    /// \note This setting allow int8 precision which can speed up by another x2 factor (compared to fp16, or x4 compared to fp32) and half the fp16 memory usage, however some accuracy can be lost.
        /// 
        /// The accuracy loss should not exceed 1-2% on the compatible models.
	    /// The current compatible models are all HUMAN_BODY_XXXX
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool allowReducedPrecisionInference;
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectDetectionRuntimeParameters
    {
        /// <summary>
        /// The detection confidence threshold between 1 and 99.
        /// A confidence of 1 means a low threshold, more uncertain objects and 99 very few but very precise objects.
        /// Ex: If set to 80, then the SDK must be at least 80% sure that a given object exists before reporting it in the list of detected objects.
        /// If the scene contains a lot of objects, increasing the confidence can slightly speed up the process, since every object instance is tracked.
        /// </summary>
        public float detectionConfidenceThreshold;
        /// <summary>
        ///
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)sl.OBJECT_CLASS.LAST)]
        public int[] objectClassFilter;

        /// <summary>
        ///
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)sl.OBJECT_CLASS.LAST)]
        public int[] objectConfidenceThreshold;
    };

    /// <summary>
    /// Lists of supported skeleton body model
    /// </summary>
    public enum BODY_FORMAT
    {
        BODY_18,
        BODY_34=1,
        BODY_38=2
    };

    /// <summary>
    /// 
    /// </summary>
    public enum BODY_KEYPOINTS_SELECTION
    {
        /// <summary>
        /// Full keypoint model
        /// </summary>
        FULL,
        /// <summary>
        /// Only the upper body will be output (from hip)
        /// </summary>
        UPPER_BODY,
        /// <summary>
        /// Hands only
        /// </summary>
       // HAND
    };

    /// <summary>
    /// Object data structure directly from the SDK. Represents a single object detection.
    /// See DetectedObject for an abstracted version with helper functions that make this data easier to use in Unity.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectData
    {
        /// <summary>
        /// Object identification number, used as a reference when tracking the object through the frames.
        /// </summary>
        public int id; 
        /// <summary>
        ///Unique ID to help identify and track AI detections. Can be either generated externally, or using \ref ZEDCamera.generateUniqueId() or left empty
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 37)]
        public string uniqueObjectId;
        /// <summary>
        ///  Object label, forwarded from \ref CustomBoxObjects when using DETECTION_MODEL.CUSTOM_BOX_OBJECTS
        /// </summary>
        public int rawLabel;
        /// <summary>
        /// Object category. Identify the object type.
        /// </summary>
        public sl.OBJECT_CLASS label;
        public sl.OBJECT_SUBCLASS subLabel;
        public sl.OBJECT_TRACKING_STATE trackingState;
        public sl.OBJECT_ACTION_STATE actionState;
        /// <summary>
        /// 3D space data (Camera Frame since this is what we used in Unity)
        /// </summary>
        public Vector3 position; //object root position
        /// <summary>
        /// Defines the detection confidence value of the object.
        /// From 0 to 100, a low value means the object might not be localized perfectly or the label(OBJECT_CLASS) is uncertain.
        /// </summary>
        public float confidence;
        /// <summary>
        /// Mask
        /// </summary>
        public System.IntPtr mask;
        
        /// <summary>
        /// Image data.
        /// Note that Y in these values is relative from the top of the image, whereas the opposite is true
        /// in most related Unity functions. If using this raw value, subtract Y from the
        /// image height to get the height relative to the bottom.
        /// </summary>
        ///  0 ------- 1
        ///  |   obj   |
        ///  3-------- 2
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Vector2[] boundingBox2D;
        
        /// <summary>
        /// 3D head centroid. Defined in \ref sl:InitParameters.UNIT, expressed in \ref RuntimeParameters.measure3D_reference_frame.
        /// </summary>
        public Vector3 headPosition;
        /// <summary>
        /// Defines the object 3D velocity
        /// </summary>
        public Vector3 velocity; //object root velocity
        
        /// <summary>
        /// 3D object dimensions: width, height, length. Defined in InitParameters.UNIT, expressed in RuntimeParameters.measure3DReferenceFrame.
        /// </summary>
        public Vector3 dimensions;
        
        /// <summary>
        /// The 3D space bounding box. given as array of vertices
        /// </summary>
        ///   1 ---------2
        ///  /|         /|
        /// 0 |--------3 |
        /// | |        | |
        /// | 5--------|-6
        /// |/         |/
        /// 4 ---------7
        ///
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Vector3[] boundingBox;

        /// <summary>
        /// bounds the head with eight 3D points.
        ///  \note Not available with DETECTION_MODEL::MULTI_CLASS_BOX.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Vector3[] headBoundingBox;
        /// <summary>
        /// bounds the head with four 2D points.
        /// Expressed in pixels on the original image resolution.
        ///  \note Not available with DETECTION_MODEL::MULTI_CLASS_BOX.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Vector2[] headBoundingBox2D;

        // Full covariance matrix for position (3x3). Only 6 values are necessary
        // [p0, p1, p2]
        // [p1, p3, p4]
        // [p2, p4, p5]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] positionCovariance;// covariance matrix of the 3d position, represented by its upper triangular matrix value
    };

    /// <summary>
    /// Container to store the externally detected objects. The objects can be ingested using IngestCustomBoxObjects() function to extract 3D information and tracking over time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CustomBoxObjectData
    {
        /// <summary>
        ///Unique ID to help identify and track AI detections. Can be either generated externally, or using \ref ZEDCamera.generateUniqueId() or left empty
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 37)]
        public string uniqueObjectID;
        /// <summary>
        /// 2D bounding box represented as four 2D points starting at the top left corner and rotation clockwise.
        /// </summary>
        ///  0 ------- 1
        ///  |   obj   |
        ///  3-------- 2
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Vector2[] boundingBox2D;
        /// <summary>
        /// Object label, this information is passed-through and can be used to improve object tracking
        /// </summary>
        public int label;
        /// <summary>
        /// Detection confidence. Should be [0-1]. It can be used to improve the object tracking
        /// </summary>
        public float probability;
        /// <summary>
        /// Provide hypothesis about the object movements(degrees of freedom) to improve the object tracking
        /// true: means 2 DoF projected alongside the floor plane, the default for object standing on the ground such as person, vehicle, etc
        /// false : 6 DoF full 3D movements are allowed
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool isGrounded;
    }

    /// <summary>
    /// Object Scene data directly from the ZED SDK. Represents all detections given during a single image frame.
    /// See DetectionFrame for an abstracted version with helper functions that make this data easier to use in Unity.
    /// Contains the number of object in the scene and the objectData structure for each object.
    /// Since the data is transmitted from C++ to C#, the size of the structure must be constant. Therefore, there is a limitation of 200 (MAX_OBJECT constant) objects in the image.
    /// <c> This number cannot be changed.<c>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Objects
    {
        /// <summary>
        /// How many objects were detected this frame. Use this to iterate through the top of objectData; objects with indexes greater than numObject are empty.
        /// </summary>
        public int nbObjects;
        /// <summary>
        /// Timestamp of the image where these objects were detected.
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// Defines if the object frame is new (new timestamp)
        /// </summary>
        public int isNew;
        /// <summary>
        /// Defines if the object is tracked
        /// </summary>
        public int isTracked;
        /// <summary>
        /// Current detection model used.
        /// </summary>
        public sl.OBJECT_DETECTION_MODEL detectionModel;
        /// <summary>
        /// Array of objects
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)(Constant.MAX_OBJECTS))]
        public ObjectData[] objectList;
    };

    /// <summary>
    /// Sets the body tracking parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyTrackingParameters
    {
        /// <summary>
        /// Defines a module instance id. This is used to identify which object detection model instance is used.
        /// </summary>
        public uint instanceModuleID;
        /// <summary>
        /// Defines if the object detection is synchronized to the image or runs in a separate thread.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool imageSync;
        /// <summary>
        /// Defines if the object detection will track objects across multiple images, instead of an image-by-image basis.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableTracking;
        /// <summary>
        /// Defines if the SDK will calculate 2D masks for each object. Requires more performance, so don't enable if you don't need these masks. 
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool enableSegmentation;
        /// <summary>
        /// Defines the AI model used for detection 
        /// </summary>
        public sl.BODY_TRACKING_MODEL detectionModel;
        /// <summary>
        /// Defines if the body fitting will be applied
        /// </summary>
        public bool enableBodyFitting;
        /// <summary>
        /// Defines the body format outputed by the sdk when \ref retrieveBodies is called.
        /// </summary>
        public sl.BODY_FORMAT bodyFormat;
        /// <summary>
        /// Defines the body selection is output by the sdk when \ref retrieveBodies is called.
        /// </summary>
        public sl.BODY_KEYPOINTS_SELECTION bodySelection;
        /// <summary>
        /// Defines a upper depth range for detections.
        /// Defined in  UNIT set at  sl.Camera.Open.
        /// Default value is set to sl.Initparameters.depthMaximumDistance (can not be higher).
        /// </summary>
        public float maxRange;
        /// <summary>
        /// When an object is not detected anymore, the SDK will predict its positions during a short period of time before switching its state to SEARCHING.
        /// It prevents the jittering of the object state when there is a short misdetection.The user can define its own prediction time duration.
        /// During this time, the object will have OK state even if it is not detected.
        /// The duration is expressed in seconds.
        /// The prediction_timeout_s will be clamped to 1 second as the prediction is getting worst with time.
        /// Set this parameter to 0 to disable SDK predictions.
        /// </summary>
        public float predictionTimeout_s;
        /// <summary>
        /// Allow inference to run at a lower precision to improve runtime and memory usage,
	    /// it might increase the initial optimization time and could include downloading calibration data or calibration cache and slightly reduce the accuracy
        /// \note The fp16 is automatically enabled if the GPU is compatible and provides a speed up of almost x2 and reduce memory usage by almost half, no precision loss.
	    /// \note This setting allow int8 precision which can speed up by another x2 factor (compared to fp16, or x4 compared to fp32) and half the fp16 memory usage, however some accuracy can be lost.
        /// 
        /// The accuracy loss should not exceed 1-2% on the compatible models.
	    /// The current compatible models are all HUMAN_BODY_XXXX
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool allowReducedPrecisionInference;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct BodyTrackingRuntimeParameters
    {
        /// <summary>
        /// The detection confidence threshold between 1 and 99. 
        /// A confidence of 1 means a low threshold, more uncertain objects and 99 very few but very precise objects.
        /// Ex: If set to 80, then the SDK must be at least 80% sure that a given object exists before reporting it in the list of detected objects. 
        /// If the scene contains a lot of objects, increasing the confidence can slightly speed up the process, since every object instance is tracked.
        /// </summary>
        public float detectionConfidenceThreshold;
        /// <summary>
        /// The SDK will outputs skeleton with more keypoints than this threshold.
        /// It is useful for example to remove unstable fitting results when a skeleton is partially occluded. 
        /// </summary>
        public int minimumKeypointsThreshold;
        /// <summary>
        /// This value controls the smoothing of the fitted fused skeleton.
        /// It is ranged from 0 (low smoothing) and 1 (high smoothing)
        /// </summary>
        public float skeletonSmoothing;
    };

    /// <summary>
    /// Body data structure directly from the SDK. Represents a single body detection. 
    /// See DetectedObject for an abstracted version with helper functions that make this data easier to use in Unity. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyData
    {
        /// <summary>
        /// Object identification number, used as a reference when tracking the object through the frames.
        /// </summary>
        public int id;
        /// <summary>
        ///Unique ID to help identify and track AI detections. Can be either generated externally, or using \ref ZEDCamera.generateUniqueId() or left empty
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 37)]
        public string uniqueObjectId;
        public sl.OBJECT_TRACKING_STATE trackingState;
        public sl.OBJECT_ACTION_STATE actionState;
        /// <summary>
        /// 3D space data (Camera Frame since this is what we used in Unity)
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Defines the object 3D velocity
        /// </summary>
        public Vector3 velocity;
        // Full covariance matrix for position (3x3). Only 6 values are necessary
        // [p0, p1, p2]
        // [p1, p3, p4]
        // [p2, p4, p5]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] positionCovariance;// covariance matrix of the 3d position, represented by its upper triangular matrix value
        /// <summary>
        /// Defines the detection confidence value of the object.
        /// From 0 to 100, a low value means the object might not be localized perfectly or the label(OBJECT_CLASS) is uncertain.
        /// </summary>
        public float confidence;
        /// <summary>
        /// Mask
        /// </summary>
        public System.IntPtr mask;

        /// <summary>
        /// Image data.
        /// Note that Y in these values is relative from the top of the image, whereas the opposite is true 
        /// in most related Unity functions. If using this raw value, subtract Y from the 
        /// image height to get the height relative to the bottom. 
        /// </summary>
        ///  0 ------- 1
        ///  |   obj   |
        ///  3-------- 2
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Vector2[] boundingBox2D;

        /// <summary>
        /// 3D head centroid. Defined in \ref sl:InitParameters.UNIT, expressed in \ref RuntimeParameters.measure3D_reference_frame.
        /// </summary>
        public Vector3 headPosition;


        /// <summary>
        /// 3D object dimensions: width, height, length. Defined in InitParameters.UNIT, expressed in RuntimeParameters.measure3DReferenceFrame.
        /// </summary>
        public Vector3 dimensions;

        /// <summary>
        /// The 3D space bounding box. given as array of vertices
        /// </summary>
        ///   1 ---------2  
        ///  /|         /|
        /// 0 |--------3 |
        /// | |        | |
        /// | 5--------|-6
        /// |/         |/
        /// 4 ---------7
        /// 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Vector3[] boundingBox;

        /// <summary>
        /// bounds the head with eight 3D points.
        ///  \note Not available with DETECTION_MODEL::MULTI_CLASS_BOX.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Vector3[] headBoundingBox;
        /// <summary>
        /// bounds the head with four 2D points.
        /// Expressed in pixels on the original image resolution.
        ///  \note Not available with DETECTION_MODEL::MULTI_CLASS_BOX.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Vector2[] headBoundingBox2D;
        /// <summary>
        /// \brief A set of useful points representing the human body, expressed in 2D, respect to the original image resolution.
        /// We use a classic 18 points representation, the points semantic and order is given by BODY_PARTS.
        /// Expressed in pixels on the original image resolution, [0,0] is the top left corner.
        /// \warning in some cases, eg. body partially out of the image, some keypoint can not be detected, they will have negatives coordinates.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public Vector2[] keypoint2D;
        /// <summary>
        /// \brief A set of useful points representing the human body, expressed in 3D.
	    /// We use a classic 18 points representation, the points semantic and order is given by BODY_PARTS.
        /// Defined in \ref sl:InitParameters::UNIT, expressed in \ref RuntimeParameters::measure3D_reference_frame.
	    /// \warning in some cases, eg. body partially out of the image or missing depth data, some keypoint can not be detected, they will have non finite values.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public Vector3[] keypoint;
        /// <summary>
        /// Per keypoint detection confidence, can not be lower than the \ref ObjectDetectionRuntimeParameters::detection_confidence_threshold.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public float[] keypointConfidence;
        /// <summary>
        /// Per keypoint local position (the position of the child keypoint with respect to its parent expressed in its parent coordinate frame)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public Vector3[] localPositionPerJoint;
        /// <summary>
        /// Per keypoint local orientation
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public Quaternion[] localOrientationPerJoint;
        /// <summary>
        ///  global root orientation of the skeleton. The orientation is also represented by a quaternion with the same format as \ref local_orientation_per_joint
        /// </summary>
        public Quaternion globalRootOrientation;
    };

    /// <summary>
    /// Body Scene data directly from the ZED SDK. Represents all detections given during a single image frame. 
    /// See DetectionFrame for an abstracted version with helper functions that make this data easier to use in Unity. 
    /// Contains the number of object in the scene and the objectData structure for each object.
    /// Since the data is transmitted from C++ to C#, the size of the structure must be constant. Therefore, there is a limitation of 200 (MAX_OBJECT constant) objects in the image.
    /// <c> This number cannot be changed.<c>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Bodies
    {
        /// <summary>
        /// How many bodies were detected this frame. Use this to iterate through the top of bodyData; objects with indexes greater than numObject are empty. 
        /// </summary>
        public int nbBodies;
        /// <summary>
        /// Timestamp of the image where these objects were detected.
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// Defines if the object frame is new (new timestamp)
        /// </summary>
        public int isNew;
        /// <summary>
        /// Defines if the object is tracked
        /// </summary>
        public int isTracked;
        /// <summary>
        /// Current detection model used.
        /// </summary>
        public sl.OBJECT_DETECTION_MODEL detectionModel;
        /// <summary>
        /// Array of objects 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)(Constant.MAX_OBJECTS))]
        public BodyData[] bodyList;
    };

    /// <summary>
    /// Lists available object class
    /// </summary>
    public enum OBJECT_CLASS
    {
        PERSON = 0,
        VEHICLE = 1,
        BAG = 2,
        ANIMAL = 3,
        ELECTRONICS = 4,
        FRUIT_VEGETABLE = 5,
        SPORT = 6,
        LAST = 7
    };

    /// <summary>
    /// Lists available object subclass.
    /// </summary>
    public enum OBJECT_SUBCLASS
    {
        PERSON = 0,
        // VEHICLES
        BICYCLE = 1,
        CAR = 2,
        MOTORBIKE = 3,
        BUS = 4,
        TRUCK = 5,
        BOAT = 6,
        // BAGS
        BACKPACK = 7,
        HANDBAG = 8,
        SUITCASE = 9,
        // ANIMALS
        BIRD = 10,
        CAT = 11,
        DOG = 12,
        HORSE = 13,
        SHEEP = 14,
        COW = 15,
        // ELECTRONICS
        CELLPHONE = 16,
        LAPTOP = 17,
        // FRUITS/VEGETABLES
        BANANA = 18,
        APPLE = 19,
        ORANGE = 20,
        CARROT = 21,
        PERSON_HEAD = 22,
        SPORTSBALL = 23,
        LAST = 24
    };

    /// <summary>
    /// Tracking state of an individual object.
    /// </summary>
    public enum OBJECT_TRACKING_STATE
    {
        OFF, /**< The tracking is not yet initialized, the object ID is not usable */
        OK, /**< The object is tracked */
        SEARCHING,/**< The object couldn't be detected in the image and is potentially occluded, the trajectory is estimated */
        TERMINATE/**< This is the last searching state of the track, the track will be deleted in the next retreiveObject */
    };

    public enum OBJECT_ACTION_STATE
    {
        IDLE = 0, /**< The object is staying static. */
        MOVING = 1, /**< The object is moving. */
        LAST = 2
    };

    /// <summary>
    /// List available models for detection
    /// </summary>
    public enum OBJECT_DETECTION_MODEL {
        /// <summary>
        /// Any objects, bounding box based.
        /// </summary>
		MULTI_CLASS_BOX_FAST,
        /// <summary>
        /// Any objects, bounding box based.
        /// </summary>
        MULTI_CLASS_BOX_MEDIUM,
        /// <summary>
        /// Any objects, bounding box based.
        /// </summary>
        MULTI_CLASS_BOX_ACCURATE,
        /// <summary>
        ///  Bounding Box detector specialized in person heads, particulary well suited for crowded environement, the person localization is also improved
        /// </summary>
        PERSON_HEAD_BOX_FAST,
        /// <summary>
        ///  Bounding Box detector specialized in person heads, particulary well suited for crowded environement, the person localization is also improved
        /// </summary>
        PERSON_HEAD_BOX_ACCURATE,
        /// <summary>
        /// For external inference, using your own custom model and/or frameworks. This mode disable the internal inference engine, the 2D bounding box detection must be provided
        /// </summary>
        CUSTOM_BOX_OBJECTS,
        LAST
    };

    /// <summary>
    /// List available models for detection
    /// </summary>
    public enum BODY_TRACKING_MODEL
    {
        /// <summary>
        /// Keypoints based, specific to human skeleton, real time performance even on Jetson or low end GPU cards.
        /// </summary>
        HUMAN_BODY_FAST,
        /// <summary>
        /// Keypoints based, specific to human skeleton, real time performance even on Jetson or low end GPU cards.
        /// </summary>
        HUMAN_BODY_MEDIUM,
        /// <summary>
        ///  Keypoints based, specific to human skeleton, state of the art accuracy, requires powerful GPU.
        /// </summary>
		HUMAN_BODY_ACCURATE
    };

    /// <summary>
    /// Lists of supported bounding box preprocessing
    /// </summary>
    public enum OBJECT_FILTERING_MODE
    {
        /// <summary>
        /// SDK will not apply any preprocessing to the detected objects 
        /// </summary>
        NONE,
        /// <summary>
        /// SDK will remove objects that are in the same 3D position as an already tracked object (independant of class ID). Default value
        /// </summary>
        NMS3D,
        /// <summary>
        /// SDK will remove objects that are in the same 3D position as an already tracked object of the same class ID
        /// </summary>
        NMS3D_PER_CLASS
    };

    public enum AI_MODELS
    {
        /// <summary>
        /// related to sl.DETECTION_MODEL.MULTI_CLASS_BOX
        /// </summary>
        MULTI_CLASS_FAST_DETECTION=0,
        /// <summary>
        /// related to sl.DETECTION_MODEL.MULTI_CLASS_BOX_MEDIUM
        /// </summary>
        MULTI_CLASS_MEDIUM_DETECTION=1,
        /// <summary>
        /// related to sl.DETECTION_MODEL.MULTI_CLASS_BOX_ACCURATE
        /// </summary>
        MULTI_CLASS_ACCURATE_DETECTION=2,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_FAST
        /// </summary>
        HUMAN_BODY_FAST_DETECTION=3,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_MEDIUM
        /// </summary>
        HUMAN_BODY_MEDIUM_DETECTION=4,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE
        /// </summary>
        HUMAN_BODY_ACCURATE_DETECTION=5,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_FAST
        /// </summary>
        HUMAN_BODY_38_FAST_DETECTION=6,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_MEDIUM
        /// </summary>
        HUMAN_BODY_38_MEDIUM_DETECTION=7,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE
        /// </summary>
        HUMAN_BODY_38_ACCURATE_DETECTION=8,
        /// <summary>
        /// related to sl.DETECTION_MODEL.HUMAN_BODY_FAST
        /// </summary>
        PERSON_HEAD_FAST_DETECTION=9,
        /// <summary>
        /// related to sl.DETECTION_MODEL.PERSON_HEAD
        /// </summary>
        PERSON_HEAD_ACCURATE_DETECTION=10,
        /// <summary>
        /// related to sl.BatchParameters.enable
        /// </summary>
        REID_ASSOCIATION=11,
        /// <summary>
        /// related to sl.DETECTION_MODEL.NEURAL
        /// </summary>
        NEURAL_DEPTH=12,

        LAST=13
    };

    /// <summary>
    /// semantic and order of human body keypoints.
    /// </summary>
    public enum BODY_18_PARTS
    {
        NOSE = 0,
        NECK = 1,
        RIGHT_SHOULDER = 2,
        RIGHT_ELBOW = 3,
        RIGHT_WRIST = 4,
        LEFT_SHOULDER = 5,
        LEFT_ELBOW = 6,
        LEFT_WRIST = 7,
        RIGHT_HIP = 8,
        RIGHT_KNEE = 9,
        RIGHT_ANKLE = 10,
        LEFT_HIP = 11,
        LEFT_KNEE = 12,
        LEFT_ANKLE = 13,
        RIGHT_EYE = 14,
        LEFT_EYE = 15,
        RIGHT_EAR = 16,
        LEFT_EAR = 17,
        LAST = 18
    };

    /// <summary>
    /// ssemantic of human body parts and order keypoints for BODY_FORMAT.BODY_34.
    /// </summary>
    public enum BODY_34_PARTS
    {
        PELVIS = 0,
        NAVAL_SPINE = 1,
        CHEST_SPINE = 2,
        NECK = 3,
        LEFT_CLAVICLE = 4,
        LEFT_SHOULDER = 5,
        LEFT_ELBOW = 6,
        LEFT_WRIST = 7,
        LEFT_HAND = 8,
        LEFT_HANDTIP = 9,
        LEFT_THUMB = 10,
        RIGHT_CLAVICLE = 11,
        RIGHT_SHOULDER = 12,
        RIGHT_ELBOW = 13,
        RIGHT_WRIST = 14,
        RIGHT_HAND = 15,
        RIGHT_HANDTIP = 16,
        RIGHT_THUMB = 17,
        LEFT_HIP = 18,
        LEFT_KNEE = 19,
        LEFT_ANKLE = 20,
        LEFT_FOOT = 21,
        RIGHT_HIP = 22,
        RIGHT_KNEE = 23,
        RIGHT_ANKLE = 24,
        RIGHT_FOOT = 25,
        HEAD = 26,
        NOSE = 27,
        LEFT_EYE = 28,
        LEFT_EAR = 29,
        RIGHT_EYE = 30,
        RIGHT_EAR = 31,
        LEFT_HEEL = 32,
        RIGHT_HEEL = 33,
        LAST = 34
    };

    /// <summary>
    /// semantic of human body parts and order keypoints for BODY_FORMAT.BODY_38.
    /// </summary>
    public enum BODY_38_PARTS
    {
        PELVIS = 0,
        SPINE_1 = 1,
        SPINE_2 = 2,
        SPINE_3 = 3,
        NECK = 4,
        NOSE = 5,
        LEFT_EYE = 6,
        RIGHT_EYE = 7,
        LEFT_EAR = 8,
        RIGHT_EAR = 9,
        LEFT_CLAVICLE = 10,
        RIGHT_CLAVICLE = 11,
        LEFT_SHOULDER = 12,
        RIGHT_SHOULDER = 13,
        LEFT_ELBOW = 14,
        RIGHT_ELBOW = 15,
        LEFT_WRIST = 16,
        RIGHT_WRIST = 17,
        LEFT_HIP = 18,
        RIGHT_HIP = 19,
        LEFT_KNEE = 20,
        RIGHT_KNEE = 21,
        LEFT_ANKLE = 22,
        RIGHT_ANKLE = 23,
        LEFT_BIG_TOE = 24,
        RIGHT_BIG_TOE = 25,
        LEFT_SMALL_TOE = 26,
        RIGHT_SMALL_TOE = 27,
        LEFT_HEEL = 28,
        RIGHT_HEEL = 29,
        // Hands
        LEFT_HAND_THUMB_4 = 30, 
        RIGHT_HAND_THUMB_4 = 31,
        LEFT_HAND_INDEX_1 = 32,
        RIGHT_HAND_INDEX_1 = 33,
        LEFT_HAND_MIDDLE_4 = 34,
        RIGHT_HAND_MIDDLE_4 = 35,
        LEFT_HAND_PINKY_1 = 36, 
        RIGHT_HAND_PINKY_1 = 37,
        LAST = 38
    };

    /// <summary>
    /// Contains batched data of a detected object
    /// </summary>
    /// <summary>
    /// Contains batched data of a detected object
    /// </summary>
    public class ObjectsBatch
    {
        /// <summary>
        /// How many data were stored. Use this to iterate through the top of position/velocity/bounding_box/...; objects with indexes greater than numData are empty.
        /// </summary>
        public int numData = 0;
        /// <summary>
        /// The trajectory id
        /// </summary>
        public int id = 0;
        /// <summary>
        /// Object Category. Identity the object type
        /// </summary>
        public OBJECT_CLASS label = OBJECT_CLASS.LAST;
        /// <summary>
        /// Object subclass
        /// </summary>
        public OBJECT_SUBCLASS sublabel = OBJECT_SUBCLASS.LAST;
        /// <summary>
        ///  Defines the object tracking state
        /// </summary>
        public TRACKING_STATE trackingState = TRACKING_STATE.TRACKING_OFF;
        /// <summary>
        /// A sample of 3d position
        /// </summary>
        public Vector3[] positions = new Vector3[(int)Constant.MAX_BATCH_SIZE];
        /// <summary>
        /// a sample of the associated position covariance
        /// </summary>
        public float[,] positionCovariances = new float[(int)Constant.MAX_BATCH_SIZE, 6];
        /// <summary>
        /// A sample of 3d velocity
        /// </summary>
        public Vector3[] velocities = new Vector3[(int)Constant.MAX_BATCH_SIZE];
        /// <summary>
        /// The associated position timestamp
        /// </summary>
        public ulong[] timestamps = new ulong[(int)Constant.MAX_BATCH_SIZE];
        /// <summary>
        /// A sample of 3d bounding boxes
        /// </summary>
        public Vector3[,] boundingBoxes = new Vector3[(int)Constant.MAX_BATCH_SIZE, 8];
        /// <summary>
        /// 2D bounding box of the person represented as four 2D points starting at the top left corner and rotation clockwise.
        /// Expressed in pixels on the original image resolution, [0, 0] is the top left corner.
        ///      A ------ B
        ///      | Object |
        ///      D ------ C
        /// </summary>
        public Vector2[,] boundingBoxes2D = new Vector2[(int)Constant.MAX_BATCH_SIZE, 4];
        /// <summary>
        /// a sample of object detection confidence
        /// </summary>
        public float[] confidences = new float[(int)Constant.MAX_BATCH_SIZE];
        /// <summary>
        /// a sample of the object action state
        /// </summary>
        public OBJECT_ACTION_STATE[] actionStates = new OBJECT_ACTION_STATE[(int)Constant.MAX_BATCH_SIZE];
        /// <summary>
        /// bounds the head with four 2D points.
        /// Expressed in pixels on the original image resolution.
        /// Not available with DETECTION_MODEL.MULTI_CLASS_BOX.
        /// </summary>
        public Vector2[,] headBoundingBoxes2D = new Vector2[(int)Constant.MAX_BATCH_SIZE, 8];
        /// <summary>
        /// bounds the head with eight 3D points.
		/// Defined in sl.InitParameters.UNIT, expressed in RuntimeParameters.measure3DReferenceFrame.
		/// Not available with DETECTION_MODEL.MULTI_CLASS_BOX.
        /// </summary>
        public Vector3[,] headBoundingBoxes = new Vector3[(int)Constant.MAX_BATCH_SIZE, 8];
        /// <summary>
        /// 3D head centroid.
		/// Defined in sl.InitParameters.UNIT, expressed in RuntimeParameters.measure3DReferenceFrame.
		/// Not available with DETECTION_MODEL.MULTI_CLASS_BOX.
        /// </summary>
        public Vector3[] headPositions = new Vector3[(int)Constant.MAX_BATCH_SIZE];
    }
}// end namespace sl
