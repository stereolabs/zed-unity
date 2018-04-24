//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Runtime.InteropServices;

/// <summary>
/// Stores the camera settings and is used as an interface with the ZED
/// </summary>
public class ZEDCameraSettingsManager {


    const string nameDll = "sl_unitywrapper";
    [DllImport(nameDll, EntryPoint = "dllz_set_camera_settings")]
    private static extern void dllz_set_camera_settings(int mode, int value, int usedefault);

    [DllImport(nameDll, EntryPoint = "dllz_get_camera_settings")]
    private static extern int dllz_get_camera_settings(int mode);

    /// <summary>
    /// Container of the camera settings
    /// </summary>
    public class CameraSettings
    {
        public int[] settings = new int[System.Enum.GetNames(typeof(sl.CAMERA_SETTINGS)).Length];

        public CameraSettings(int brightness = 4, int contrast = 4, int hue = 0, int saturation = 4, int whiteBalance = -1, int gain = -1, int exposure = -1)
        {
            settings = new int[System.Enum.GetNames(typeof(sl.CAMERA_SETTINGS)).Length];
            settings[(int)sl.CAMERA_SETTINGS.BRIGHTNESS] = brightness;
            settings[(int)sl.CAMERA_SETTINGS.CONTRAST] = contrast;
            settings[(int)sl.CAMERA_SETTINGS.SATURATION] = saturation;
            settings[(int)sl.CAMERA_SETTINGS.HUE] = hue;
            settings[(int)sl.CAMERA_SETTINGS.WHITEBALANCE] = whiteBalance;
            settings[(int)sl.CAMERA_SETTINGS.GAIN] = gain;
            settings[(int)sl.CAMERA_SETTINGS.EXPOSURE] = exposure;
        }

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
        }

        public CameraSettings Clone()
        {
            return new CameraSettings(this);
        }


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

    }
    /// <summary>
    /// Reference to the container
    /// </summary>
    private CameraSettings settings_;
    public CameraSettings Settings
    {
        get { return settings_.Clone(); }
    }

    /// <summary>
    /// Is in auto mode
    /// </summary>
    public bool auto = true;
    public bool whiteBalanceAuto = true;


    public ZEDCameraSettingsManager()
    {
        settings_ = new CameraSettings();
    }

    /// <summary>
    /// Set settings from the container to the camera
    /// </summary>
    /// <param name="zedCamera"></param>
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
            if (settings_.WhiteBalance != -1)
            {
                zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, settings_.WhiteBalance, false);
            }
        } 
    }

    /// <summary>
    /// Load camera settings from a file, and set them to the container and camera
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
            }
        }
        SetSettings(zedCamera);
        auto = (settings_.Exposure == -1);
        whiteBalanceAuto = (settings_.WhiteBalance == -1);
    }


    /// <summary>
    /// Retrieves settings from the camera
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
        } 
    }

    /// <summary>
    /// Set settings of the camera
    /// </summary>
    /// <param name="settings">The setting which will be changed</param>
    /// <param name="value">The value</param>
    /// <param name="usedefault">will set default (or automatic) value if set to true (value (int) will not be taken into account)</param>
    public void SetCameraSettings(sl.CAMERA_SETTINGS settings, int value, bool usedefault = false)
    {
        settings_.settings[(int)settings] = !usedefault && value != -1 ? value : -1;
        dllz_set_camera_settings((int)settings, value, System.Convert.ToInt32(usedefault));
    }

    /// <summary>
    /// Get the value from a setting of the camera
    /// </summary>
    /// <param name="settings"></param>
    public int GetCameraSettings(sl.CAMERA_SETTINGS settings)
    {
        settings_.settings[(int)settings] = dllz_get_camera_settings((int)settings);
        return settings_.settings[(int)settings];
    }

    /// <summary>
    /// Save the camera settings into a file
    /// </summary>
    /// <param name="path"></param>
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
            file.Close();
        }
    }
}
