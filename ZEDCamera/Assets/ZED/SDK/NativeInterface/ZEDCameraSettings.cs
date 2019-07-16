//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Runtime.InteropServices;

/// <summary>
/// Stores the camera settings (brightness/contrast, gain/exposure, etc.) and interfaces with the ZED
/// when they need to be loaded or changed. 
/// Created by ZEDCamera and referenced by ZEDCameraSettingsEditor.
/// </summary><remarks>
/// The actual settings themselves are stored in an instance of CameraSettings, for easier manipulation. 
/// But this class provides accessors for each value within it.
/// </remarks>
public class ZEDCameraSettings
{
    #region DLL Calls
    const string nameDll = "sl_unitywrapper";
    [DllImport(nameDll, EntryPoint = "dllz_set_camera_settings")]
    private static extern void dllz_set_camera_settings(int id, int mode, int value, int usedefault);

    [DllImport(nameDll, EntryPoint = "dllz_get_camera_settings")]
    private static extern int dllz_get_camera_settings(int id, int mode);

    #endregion

    /// <summary>
    /// Container for ZED camera settings, with constructors for easily creating default or specific values
    /// or making duplicate instances. 
    /// </summary>
    public class CameraSettings
    {
        /// <summary>
        /// Holds an int for each setting, with indexes corresponding to sl.CAMERA_SETTINGS.
        /// </summary>
        public int[] settings = new int[System.Enum.GetNames(typeof(sl.CAMERA_SETTINGS)).Length];

        /// <summary>
        /// Constructor. Call without arguments to set all values to default. 
        /// </summary>
        /// <param name="brightness">Camera's brightness setting.</param>
        /// <param name="contrast">Camera's contrast setting.</param>
        /// <param name="hue">Camera's hue setting.</param>
        /// <param name="saturation">Camera's saturation setting.</param>
        /// <param name="whiteBalance">Camera's white balance setting. -1 means automatic.</param>
        /// <param name="gain">Camera's gain setting. -1 means automatic.</param>
        /// <param name="exposure">Camera's exposure setting. -1 means automatic.</param>
        public CameraSettings(int brightness = 4, int contrast = 4, int hue = 0, int saturation = 4, int whiteBalance = -1, int gain = -1, int exposure = -1,int ledStatus = 1)
        {
            settings = new int[System.Enum.GetNames(typeof(sl.CAMERA_SETTINGS)).Length];
            settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS] = brightness;
            settings[(int)sl.CAMERA_SETTINGS.CONTRAST] = contrast;
            settings[(int)sl.CAMERA_SETTINGS.SATURATION] = saturation;
            settings[(int)sl.CAMERA_SETTINGS.HUE] = hue;
            settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE] = whiteBalance;
            settings[(int)sl.CAMERA_SETTINGS.GAIN] = gain;
            settings[(int)sl.CAMERA_SETTINGS.EXPOSURE] = exposure;
            settings[(int)sl.CAMERA_SETTINGS.LED_STATUS] = ledStatus;
        }

        /// <summary>
        /// Constructor. Sets settings to match another CameraSettings passed in the argument. 
        /// </summary>
        /// <param name="other"></param>
        public CameraSettings(CameraSettings other)
        {
            settings = new int[System.Enum.GetNames(typeof(sl.CAMERA_SETTINGS)).Length];
            settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS] = other.settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS];
            settings[(int)sl.CAMERA_SETTINGS.CONTRAST] = other.settings[(int)sl.CAMERA_SETTINGS.CONTRAST];
            settings[(int)sl.CAMERA_SETTINGS.SATURATION] = other.settings[(int)sl.CAMERA_SETTINGS.SATURATION];
            settings[(int)sl.CAMERA_SETTINGS.HUE] = other.settings[(int)sl.CAMERA_SETTINGS.HUE];
            settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE] = other.settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE];
            settings[(int)sl.CAMERA_SETTINGS.GAIN] = other.settings[(int)sl.CAMERA_SETTINGS.GAIN];
            settings[(int)sl.CAMERA_SETTINGS.EXPOSURE] = other.settings[(int)sl.CAMERA_SETTINGS.EXPOSURE];
            settings[(int)sl.CAMERA_SETTINGS.LED_STATUS] = other.settings[(int)sl.CAMERA_SETTINGS.LED_STATUS];
        }

        /// <summary>
        /// Returns a new instance of CameraSettings with the same settings as the instance this function was called with.
        /// </summary>
        /// <returns>New instance of CameraSettings.</returns>
        public CameraSettings Clone()
        {
            return new CameraSettings(this);
        }

        /// <summary>
        /// ZED camera's brightness setting. 
        /// </summary>
        public int Brightness
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS] = value;
            }
        }

        /// <summary>
        /// ZED camera's saturation setting. 
        /// </summary>
        public int Saturation
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.SATURATION];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.SATURATION] = value;
            }
        }

        /// <summary>
        /// ZED camera's hue setting. 
        /// </summary>
        public int Hue
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.HUE];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.HUE] = value;
            }
        }

        /// <summary>
        /// ZED camera's contrast setting. 
        /// </summary>
        public int Contrast
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.CONTRAST];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.CONTRAST] = value;
            }
        }

        /// <summary>
        /// ZED camera's gain setting. -1 means automatic.
        /// </summary>
        public int Gain
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.GAIN];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.GAIN] = value;
            }
        }

        /// <summary>
        /// ZED camera's exposure setting. -1 means automatic.
        /// </summary>
        public int Exposure
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.EXPOSURE];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.EXPOSURE] = value;
            }
        }

        /// <summary>
        /// ZED camera's white balance setting. -1 means automatic.
        /// </summary>
        public int WhiteBalance
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE] = value;
            }
        }

        /// <summary>
        /// ZED camera's LED status
        /// </summary>
        public int LEDStatus
        {
            get
            {
                return settings[(int)sl.CAMERA_SETTINGS.LED_STATUS];
            }

            set
            {
                settings[(int)sl.CAMERA_SETTINGS.LED_STATUS] = value;
            }
        }

    }
    /// <summary>
    /// Reference to the settings container object. 
    /// </summary>
    private CameraSettings settings_;
    /// <summary>
    /// Reference to the settings container object. 
    /// </summary>
    public CameraSettings Settings
    {
        get { return settings_.Clone(); }
    }

    /// <summary>
    /// Whether exposure is set to automatic. 
    /// </summary>
    public bool auto = true;

    /// <summary>
    /// Whether white balance is set to automatic.
    /// </summary>
    public bool whiteBalanceAuto = true;

    /// <summary>
    /// Constructor. Creates a new instance of CameraSettings to contain all settings values. 
    /// </summary>
    public ZEDCameraSettings()
    {
        settings_ = new CameraSettings();
    }

    /// <summary>
    /// Applies all settings from the container to the actual ZED camera.
    /// </summary>
    /// <param name="zedCamera">Current instance of the ZEDCamera wrapper.</param>
    public void SetSettings(sl.ZEDCamera zedCamera)
    {
        if (zedCamera != null)
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, settings_.Brightness, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, settings_.Contrast, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, settings_.Hue, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, settings_.Saturation, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, settings_.Gain, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, settings_.Exposure, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, settings_.LEDStatus, false);
            if (settings_.WhiteBalance != -1)
            {
                zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, settings_.WhiteBalance, false);
            }
        } 
    }


	/// <summary>
	/// Applies all settings from the container to the actual ZED camera.
	/// </summary>
	/// <param name="zedCamera">Current instance of the ZEDCamera wrapper.</param>
	public void ResetCameraSettings(sl.ZEDCamera zedCamera)
	{
		if (zedCamera != null)
		{
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, 0, true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, 0 , true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, 0 , true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, 0 , true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, 0, true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, 0, true);
			zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, 0, true);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, 1, true);
        } 
	}
    /// <summary>
    /// Loads camera settings from a file, and sets them to the container and camera.
    /// File is loaded from the root project folder (one above Assets). 
    /// </summary>
    /// <param name="zedCamera"></param>
    /// <param name="path"></param>
    public void LoadCameraSettings(sl.ZEDCamera zedCamera, string path = "ZED_Settings.conf")
    {

        string[] lines = null;
        try
        {
            lines = System.IO.File.ReadAllLines(path);
        }
        catch (System.Exception)
        {

        }
        if (lines == null) return;

        foreach (string line in lines)
        {
            string[] splittedLine = line.Split('=');
            if (splittedLine.Length == 2)
            {
                string key = splittedLine[0];
                string field = splittedLine[1];

                if (key == "brightness")
                {
                    settings_.Brightness = int.Parse(field);
                }
                else if (key == "contrast")
                {
                    settings_.Contrast = int.Parse(field);
                }
                else if (key == "hue")
                {
                    settings_.Hue = int.Parse(field);
                }
                else if (key == "saturation")
                {
                    settings_.Saturation = int.Parse(field);
                }
                else if (key == "whiteBalance")
                {
                    settings_.WhiteBalance = int.Parse(field);
                }
                else if (key == "gain")
                {
                    settings_.Gain = int.Parse(field);
                }
                else if (key == "exposure")
                {
                    settings_.Exposure = int.Parse(field);
                }
                else if (key == "LED")
                {
                    settings_.LEDStatus = int.Parse(field);
                }
            }
        }
        SetSettings(zedCamera);
        auto = (settings_.Exposure == -1);
        whiteBalanceAuto = (settings_.WhiteBalance == -1);
    }


    /// <summary>
    /// Retrieves current settings from the ZED camera.
    /// </summary>
    /// <param name="zedCamera"></param>
    public void RetrieveSettingsCamera(sl.ZEDCamera zedCamera)
    {
        if (zedCamera != null)
        {
            settings_.Brightness = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS);
            settings_.Contrast = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST);
            settings_.Hue = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.HUE);
            settings_.Saturation = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SATURATION);
            settings_.Gain = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAIN);
            settings_.Exposure = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE);
            settings_.WhiteBalance = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE);
            settings_.LEDStatus = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS);
        } 
    }

    /// <summary>
    /// Applies an individual setting to the ZED camera. 
    /// </summary>
    /// <param name="settings">Setting to be changed (brightness, contrast, gain, exposure, etc.)</param>
    /// <param name="value">New value for the setting.</param>
    /// <param name="usedefault">If true, ignores the value and applies the default setting.</param>
    public void SetCameraSettings(int cid, sl.CAMERA_SETTINGS settings, int value, bool usedefault = false)
    {
        settings_.settings[(int)settings] = !usedefault && value != -1 ? value : -1;
		dllz_set_camera_settings(cid, (int)settings, value, System.Convert.ToInt32(usedefault));
    }

    /// <summary>
    /// Gets the value from an individual ZED camera setting (brightness, contrast, gain, exposure, etc.)
    /// </summary>
    /// <param name="settings">Setting to be retrieved.</param>
    /// <returns>Current value.</returns>
    public int GetCameraSettings(int cid, sl.CAMERA_SETTINGS settings)
    {
		settings_.settings[(int)settings] = dllz_get_camera_settings(cid, (int)settings);
        return settings_.settings[(int)settings];
    }

    /// <summary>
    /// Saves all camera settings into a file into the specified path/name. 
    /// </summary>
    /// <param name="path">Path and filename to save the file (ex. /Assets/ZED_Settings.conf)</param>
    public void SaveCameraSettings(string path)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
        {
            file.WriteLine("brightness=" + settings_.Brightness.ToString());
            file.WriteLine("contrast=" + settings_.Contrast.ToString());
            file.WriteLine("hue=" + settings_.Hue.ToString());
            file.WriteLine("saturation=" + settings_.Saturation.ToString());
            file.WriteLine("whiteBalance=" + settings_.WhiteBalance.ToString());
            file.WriteLine("gain=" + settings_.Gain.ToString());
            file.WriteLine("exposure=" + settings_.Exposure.ToString());
            file.WriteLine("LED=" + settings_.LEDStatus.ToString());
            file.Close();
        }
    }
}
