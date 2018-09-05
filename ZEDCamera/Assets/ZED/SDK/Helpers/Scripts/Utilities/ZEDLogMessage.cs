//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============


/// <summary>
/// Holds the ERROR enum returned by various high- and mid-level camera functions, and the Error2Str() method for 
/// converting the errors to human-readible versions for displaying for the user. 
/// </summary>
public class ZEDLogMessage
{
 
	/// <summary>
	/// Current version of the required SDK plugin as a string. Used to display errors
	/// relating to a missing or mismatched SDK version. 
	/// </summary>
    static private string zed_sdk_version
    {
        get
        {
            int major = sl.ZEDCamera.PluginVersion.Major;
            int minor = sl.ZEDCamera.PluginVersion.Minor;
            return "v" + major + "." + minor;
        }
    }
 
    /// <summary>
    /// Error categories returned by various camera functions, most often in GUIMessage.
    /// See ZEDCommon.ERROR_CODE for errors straignt from the SDK.  
    /// </summary>
	public enum ERROR
    {
        /// <summary>
        /// The screen resolution is not 16:9.
        /// </summary>
		SCREEN_RESOLUTION,
        /// <summary>
        /// The ZED tracking could not be initialized.
        /// </summary>
		TRACKING_NOT_INITIALIZED,
        /// <summary>
        /// The camera failed to initialize.
        /// </summary>
		CAMERA_NOT_INITIALIZED,
		/// <summary>
		/// The camera has not been initialized yet. 
		/// </summary>
		CAMERA_LOADING,
        /// <summary>
        /// Could not open the camera.
        /// </summary>
		UNABLE_TO_OPEN_CAMERA,
		/// <summary>
		/// Camera detection issue.
		/// </summary>
		CAMERA_DETECTION_ISSUE,
		/// <summary>
		/// Motion sensor not detected (ZED Mini only).
		/// </summary>
		SENSOR_NOT_DETECTED,
		/// <summary>
		/// Low USB bandwidth.
		/// </summary>
		LOW_USB_BANDWIDTH,
        /// <summary>
        /// SteamVR plugin Camera Rig prefab not found.
        /// </summary>
		VR_CAMERA_RIG_NOT_FOUND,
        /// <summary>
        /// SteamVR plugin Camera Rig controller not found.
        /// </summary>
		VR_CAMERA_RIG_CONTROLLER_NOT_FOUND,
        /// <summary>
        ///  A calibration file has been found but no controller/Tracker exists of the file's listed serial number. 
        /// </summary>
		PAD_CAMERA_CALIBRATION_MISMATCH,
        /// <summary>
        /// The serial number of the calibration tool does not match any of the current controllers.
        /// </summary>
		PAD_CAMERA_CALIBRATION_NOT_FOUND,
        /// <summary>
        ///  At least one VR controller must be detected.
        /// </summary>
		NOT_ENOUGH_PAD_DETECTED,
        /// <summary>
        /// SteamVR Unity plugin hasn't been imported.
        /// </summary>
        STEAMVR_NOT_INSTALLED,
		/// <summary>
		/// Oculus Integration Unity plugin hasn't been imported. 
		/// </summary>
		OVR_NOT_INSTALLED,
		/// <summary>
        /// The ZED has been disconnected. (It was connected previously)
        /// </summary>
        ZED_IS_DISCONNECETD,
        /// <summary>
        /// The ZED SDK is not installed or a dependency is missing.
        /// </summary>
        SDK_NOT_INSTALLED,
		/// <summary>
		/// The ZED SDK is installed but it's not the version the Unity plugin requires.
		/// </summary>
		INCORRECT_ZED_SDK_VERSION,
        /// <summary>
        /// The SDK has a missing dependency.
        /// </summary>
        SDK_DEPENDENCIES_ISSUE,
        /// <summary>
        /// Scanned mesh is too small to create a Nav Mesh.
        /// </summary>
        NAVMESH_NOT_GENERATED,
        /// <summary>
        /// The tracking system could not load the spatial memory area file.
        /// </summary>
        TRACKING_BASE_AREA_NOT_FOUND,
    }


    /// <summary>
    /// Converts an ERROR enum to a string for displaying to the user. Called by various editor windows. 
    /// </summary>
    /// <param name="error">Error type to be converted to a string.</param>
    /// <returns></returns>
    static public string Error2Str(ERROR error)
    {
        switch (error)
        {
            case ERROR.SCREEN_RESOLUTION:
                return "Warning: Screen size should be set to 16:9 aspect ratio";

            case ERROR.TRACKING_NOT_INITIALIZED:
                return "Error: Unable to initialize Tracking module";

            case ERROR.CAMERA_NOT_INITIALIZED:
                return "Unable to open camera";

            case ERROR.UNABLE_TO_OPEN_CAMERA:
                return "Camera not detected";

			case ERROR.CAMERA_DETECTION_ISSUE:
				return "Unable to open camera";

		    case ERROR.SENSOR_NOT_DETECTED:
			    return "Camera motion sensor not detected";
		
		    case ERROR.LOW_USB_BANDWIDTH : 
				return "Low USB bandwidth";

			case ERROR.CAMERA_LOADING:
				return "Loading...";

            case ERROR.VR_CAMERA_RIG_NOT_FOUND:
                return "Warning: No SteamVR [Camera Rig] object found. Make sure you attach the CameraRig SteamVR Prefab in the project to be able to use a VR controller.\n " + 
                    "Otherwise, make sure the tracking is activated in the ZED Manager interface";

            case ERROR.VR_CAMERA_RIG_CONTROLLER_NOT_FOUND:
                return "Warning: At least one controller is recommended for the external camera";

            case ERROR.PAD_CAMERA_CALIBRATION_MISMATCH:
                return "Warning: VR Controller and ZED Camera must be calibrated before use with Stereolabs' GreenScreen Calibration tool). " +
                    "\n The controller/Tracker in the calibration file is not present.";

            case ERROR.PAD_CAMERA_CALIBRATION_NOT_FOUND:
			    return "Warning: VR Controller and ZED Camera must be calibrated before use with Stereolabs' GreenScreen Calibration tool). " + 
                    "\n No calibration file has been detected.";

            case ERROR.NOT_ENOUGH_PAD_DETECTED:
                return "Warning: At least one controller must be detected. Number of devices detected: ";

            case ERROR.STEAMVR_NOT_INSTALLED:
			    return "Warning: SteamVR is not installed.";

		    case ERROR.OVR_NOT_INSTALLED: 
			    return "Warning: OVR Plugin is not installed.";

            case ERROR.ZED_IS_DISCONNECETD:
                return "Camera disconnected";

            case ERROR.SDK_NOT_INSTALLED:
                return "ZED SDK not installed";

            case ERROR.SDK_DEPENDENCIES_ISSUE:
			    return "The ZED plugin cannot be loaded. \n Please check that you have ZED SDK "+ zed_sdk_version +" installed" +
                                    "\n\n If the problem persists, please contact our support team at support@stereolabs.com\n";
            case ERROR.NAVMESH_NOT_GENERATED:
                return "The NavMesh cannot be generated. Please change the settings of the Navigation Agent, or scan a wider zone.";
            case ERROR.TRACKING_BASE_AREA_NOT_FOUND:
                return "The tracking could not load the spatial memory area file.";


            default:
                return "Unknown error";
        }

    }

}
