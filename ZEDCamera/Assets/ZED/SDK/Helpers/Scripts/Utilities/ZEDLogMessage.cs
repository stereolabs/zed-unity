//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============


/// <summary>
/// Registers all the messages displayed in the Unity plugin inside an enum
/// </summary>
public class ZEDLogMessage
{

	static private string zed_sdk_version = "v2.4";

    /// <summary>
    /// List of the errors displayed  in Unity
    /// </summary>
	public enum ERROR
    {
        /// <summary>
        /// The screen resolution is not 16:9
        /// </summary>
		SCREEN_RESOLUTION,
        /// <summary>
        /// The tracking could not be initialized
        /// </summary>
		TRACKING_NOT_INITIALIZED,
        /// <summary>
        /// The camera has not been initialized
        /// </summary>
		CAMERA_NOT_INITIALIZED,
		/// <summary>
		/// The camera has not been initialized
		/// </summary>
		CAMERA_LOADING,
        /// <summary>
        /// Could not open the camera
        /// </summary>
		UNABLE_TO_OPEN_CAMERA,
		/// <summary>
		/// Camera Detection issue
		/// </summary>
		CAMERA_DETECTION_ISSUE,
		/// <summary>
		/// motion sensor not detected (ZED-M)
		/// </summary>
		SENSOR_NOT_DETECTED,
		/// <summary>
		/// Low USB Bandwidth
		/// </summary>
		LOW_USB_BANDWIDTH,
        /// <summary>
        /// CameraRig not found for the SteamVR plugin
        /// </summary>
		VR_CAMERA_RIG_NOT_FOUND,
        /// <summary>
        /// CameraRig controller not found 
        /// </summary>
		VR_CAMERA_RIG_CONTROLLER_NOT_FOUND,
        /// <summary>
        ///  A calibration file has been found but not the SN given
        /// </summary>
		PAD_CAMERA_CALIBRATION_MISMATCH,
        /// <summary>
        /// The serial number of the calibration tool does not match any of the current controllers
        /// </summary>
		PAD_CAMERA_CALIBRATION_NOT_FOUND,
        /// <summary>
        ///  At least one controller must be detected
        /// </summary>
		NOT_ENOUGH_PAD_DETECTED,
        /// <summary>
        /// SteamVR is not installed.
        /// </summary>
        STEAMVR_NOT_INSTALLED,
		/// <summary>
		/// SteamVR is not installed.
		/// </summary>
		OVR_NOT_INSTALLED,
		/// <summary>
        /// The ZED is disconnected
        /// </summary>
        ZED_IS_DISCONNECETD,
        /// <summary>
        /// The SDK is not installed or a dependency is missing
        /// </summary>
        SDK_NOT_INSTALLED,
		/// <summary>
		/// The SDK is not installed or a dependency is missing
		/// </summary>
		INCORRECT_ZED_SDK_VERSION,
        /// <summary>
        /// The SDK has a missing dependency
        /// </summary>
        SDK_DEPENDENCIES_ISSUE,
        /// <summary>
        /// The Mesh is too small to accept a naVMesh
        /// </summary>
        NAVMESH_NOT_GENERATED,
        /// <summary>
        /// The tracking could not load the spatial memory area
        /// </summary>
        TRACKING_BASE_AREA_NOT_FOUND,
        
    }


    /// <summary>
    /// Converts an enum to a string
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    static public string Error2Str(ERROR error)
    {
        switch (error)
        {
            case ERROR.SCREEN_RESOLUTION:
                return "Warning: screen size must respect 16:9 aspect ratio";

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
                return "Warning: No VR CameraRig object found. Make sure you attach the CameraRig SteamVR Prefab in the project to be able to use a VR controller.\n Otherwise, make sure the tracking is activated in the ZED Manager interface";

            case ERROR.VR_CAMERA_RIG_CONTROLLER_NOT_FOUND:
                return "Warning: One controller is recommended for the external camera";

            case ERROR.PAD_CAMERA_CALIBRATION_MISMATCH:
                return "Warning: VR Controller and ZED Camera must be calibrated before use (by using the GreenScreen Calibration tool). \n It seems that the current controller has not been calibrated.";

            case ERROR.PAD_CAMERA_CALIBRATION_NOT_FOUND:
			    return "Warning: VR Controller and ZED Camera must be calibrated before use (by using the GreenScreen Calibration tool). \n It seems that no calibration has been made";

            case ERROR.NOT_ENOUGH_PAD_DETECTED:
                return "Warning : At least one controller must be detected. Number of devices detected: ";

            case ERROR.STEAMVR_NOT_INSTALLED:
			    return "Warning : SteamVR is not installed.";

		    case ERROR.OVR_NOT_INSTALLED: 
			    return "Warning : OVR Plugin is not installed.";

            case ERROR.ZED_IS_DISCONNECETD:
                return "Camera is not detected";

            case ERROR.SDK_NOT_INSTALLED:
                return "The SDK is not installed";

            case ERROR.SDK_DEPENDENCIES_ISSUE:
			    return "The ZED plugin cannot be loaded. \n Please check that you have the latest ZED SDK "+ zed_sdk_version +" installed" +
                                    "\n\n If the problem persists, please contact our support team at support@stereolabs.com\n";
            case ERROR.NAVMESH_NOT_GENERATED:
                return "The NavMesh cannot be generated, please change the settings of the agent, or scan a wider zone.";
            case ERROR.TRACKING_BASE_AREA_NOT_FOUND:
                return "The tracking could not load the spatial memory area";


            default:
                return "Unknown error";
        }

    }

}
