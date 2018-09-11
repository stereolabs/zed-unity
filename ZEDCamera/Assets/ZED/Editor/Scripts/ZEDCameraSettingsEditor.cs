//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom window editor that handles what you see after clicking Window -> ZED Camera in the editor, or 
/// the Open Camera Control button in the ZEDManager Inspector. 
/// </summary>
public class ZEDCameraSettingsEditor : EditorWindow
{
    /// <summary>
    /// Brightness default value.
    /// </summary>
    private const int cbrightness = 4;

    /// <summary>
    /// Contrast default value.
    /// </summary>
    private const int ccontrast = 4;

    /// <summary>
    /// Hue default value.
    /// </summary>
    private const int chue = 0;

    /// <summary>
    /// Saturation default value.
    /// </summary>
    private const int csaturation = 4;

    /// <summary>
    /// White balance default value.
    /// </summary>
    private const int cwhiteBalance = 2600;

    /// <summary>
    /// Path to save/load configurations.
    /// </summary>
    private const string ZEDSettingsPath = "ZED_Settings.conf";

    /// <summary>
    /// Current brightness value.
    /// </summary>
    static private int brightness = 4;

    /// <summary>
    /// Current contrast value.
    /// </summary>
    static private int contrast = 4;

    /// <summary>
    /// Current hue value.
    /// </summary>
    static private int hue = 0;

    /// <summary>
    /// Current saturation value.
    /// </summary>
    static private int saturation = 4;

    /// <summary>
    /// Current gain value.
    /// </summary>
    [SerializeField]
    public int gain;

    /// <summary>
    /// Current exposure value.
    /// </summary>
    [SerializeField]
    public int exposure;
    static private int whiteBalance = cwhiteBalance;

    /// <summary>
    /// Is the exposure and gain in auto mode?
    /// </summary>
    [SerializeField]
    public static bool groupAuto = true;

    /// <summary>
    /// Is the white balance in auto mode?
    /// </summary>
    [SerializeField]
    public static bool whiteBalanceAuto = true;

    /// <summary>
    /// Has data been loaded from the file?
    /// </summary>
    [SerializeField]
    public static bool loaded = false;

    /// <summary>
    /// Has the user or a script requested a reset? 
    /// </summary>
    [SerializeField]
    public bool resetWanted = false;

    /// <summary>
    /// Refresh rate of the values of gain and exposure when auto mode is activated.
    /// </summary>
    private const int refreshRate = 60;
    /// <summary>
    /// Timer used to know when to refresh. 
    /// </summary>
    static private int refreshCount = 0;

    /// <summary>
    /// Whether we've set a manual value to gain and exposure or if they're in auto mode. 
    /// </summary>
    static bool setManualValue = true;
    /// <summary>
    /// Whether we've set a manual value to white balance or if it's in auto mode. 
    /// </summary>
    static bool setManualWhiteBalance = true;

    /// <summary>
    /// Default GUI color.
    /// </summary>
    static Color defaultColor;

    static GUIStyle style = new GUIStyle();
    static private GUILayoutOption[] optionsButton = { GUILayout.MaxWidth(100) };

    static private sl.ZEDCamera zedCamera;

    static int tab = 0;

    static bool isInit = false;

    static sl.CalibrationParameters parameters = new sl.CalibrationParameters();
    static int zed_serial_number = 0;
    static int zed_fw_version = 0;
    static sl.MODEL zed_model = sl.MODEL.ZED;

    private bool launched = false;

    /// <summary>
    /// Empty Constructor. 
    /// </summary>
    public ZEDCameraSettingsEditor()
    {

    }

    /// <summary>
    /// Updates values from the camera and redraws the elements. 
    /// Called whenever the application play state changes. 
    /// </summary>
    void Draw()
    {
        if (zedCamera != null && Application.isPlaying)
        {
            parameters = zedCamera.GetCalibrationParameters(false);
            zed_serial_number = zedCamera.GetZEDSerialNumber();
            zed_fw_version = zedCamera.GetZEDFirmwareVersion();
            zed_model = zedCamera.GetCameraModel();

        }
        this.Repaint();
    }

    [MenuItem("Window/ZED Camera")]
    static void Init()
    {
        //Gets existing open window or, if none exists, makes a new one.
        ZEDCameraSettingsEditor window = (ZEDCameraSettingsEditor)EditorWindow.GetWindow(typeof(ZEDCameraSettingsEditor), false, "ZED Camera");
        window.position = new Rect(window.position.x, window.position.y, 400, 400);
        style.normal.textColor = Color.red;
        style.fontSize = 15;
        style.margin.left = 5;

        parameters = new sl.CalibrationParameters();
        window.Show();

        Debug.Log("Camera S/N : " + zed_serial_number);
        Debug.Log("Camera Model : " + zed_model);
        Debug.Log("Camera FW : " + zed_fw_version);


    }

    /// <summary>
    /// Refreshes data. Called by Unity when this window gets focus, such as when 
    /// it's clicked on or alt-tabbed to. 
    /// </summary>
    void OnFocus()
    {
        if (zedCamera != null && zedCamera.IsCameraReady)
        {
            parameters = zedCamera.GetCalibrationParameters(false);
            zed_serial_number = zedCamera.GetZEDSerialNumber();
            zed_fw_version = zedCamera.GetZEDFirmwareVersion();
            zed_model = zedCamera.GetCameraModel();

            if (!loaded)
            {
                zedCamera.RetrieveCameraSettings();
                UpdateValuesCameraSettings();
            }
        }
    }

    /// <summary>
    /// Initializes all the starting values, and gets current values from the ZED.
    /// </summary>
    void FirstInit()
    {
        if (!isInit)
        {
            zedCamera = sl.ZEDCamera.GetInstance();
            EditorApplication.playmodeStateChanged += Draw;

            if (zedCamera != null && zedCamera.IsCameraReady)
            {
                isInit = true;

                if (!loaded)
                {
                    if (resetWanted)
                    {
                        ResetValues(groupAuto);
                        resetWanted = false;
                    }

                    zedCamera.RetrieveCameraSettings();
                    ZEDCameraSettingsManager.CameraSettings settings = zedCamera.GetCameraSettings();
                    groupAuto = zedCamera.GetExposureUpdateType();
                    whiteBalanceAuto = zedCamera.GetWhiteBalanceUpdateType();

                    hue = settings.Hue;
                    brightness = settings.Brightness;
                    contrast = settings.Contrast;
                    saturation = settings.Saturation;

                    exposure = settings.Exposure;
                    gain = settings.Gain;

                    zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, groupAuto);
                    zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposure, groupAuto);
                    zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whiteBalance, whiteBalanceAuto);

                }
                else
                {
                    LoadCameraSettings();
                }

                parameters = zedCamera.GetCalibrationParameters(false);
                zed_serial_number = zedCamera.GetZEDSerialNumber();
                zed_fw_version = zedCamera.GetZEDFirmwareVersion();
                zed_model = zedCamera.GetCameraModel();

            }
        }
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
    /// <summary>
    /// View for the camera settings
    /// </summary>
    void CameraSettingsView()
    {
        GUILayout.Label("Video Mode", EditorStyles.boldLabel);

        if (zedCamera != null && zedCamera.IsCameraReady)
        {
            EditorGUILayout.LabelField("Resolution ", zedCamera.ImageWidth + " x " + zedCamera.ImageHeight);
            EditorGUILayout.LabelField("FPS ", zedCamera.GetCameraFPS().ToString());
            launched = true;
        }
        else
        {
            EditorGUILayout.LabelField("Resolution ", 0 + " x " + 0);
            EditorGUILayout.LabelField("FPS ", "0");
        }
        EditorGUI.indentLevel = 0;
        GUILayout.Space(20);
        GUILayout.Label("Settings", EditorStyles.boldLabel);


        EditorGUI.BeginChangeCheck();
        brightness = EditorGUILayout.IntSlider("Brightness", brightness, 0, 8);
        if (EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, brightness, false);
        }

        EditorGUI.BeginChangeCheck();
        contrast = EditorGUILayout.IntSlider("Contrast", contrast, 0, 8);
        if (EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, contrast, false);
        }

        EditorGUI.BeginChangeCheck();
        hue = EditorGUILayout.IntSlider("Hue", hue, 0, 11);
        if (EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, hue, false);
        }

        EditorGUI.BeginChangeCheck();
        saturation = EditorGUILayout.IntSlider("Saturation", saturation, 0, 8);
        if (EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, saturation, false);
        }
        EditorGUI.BeginChangeCheck();
        var origFontStyle = EditorStyles.label.fontStyle;
        EditorStyles.label.fontStyle = FontStyle.Bold;
        GUILayout.Space(20);

        whiteBalanceAuto = EditorGUILayout.Toggle("Automatic ", whiteBalanceAuto, EditorStyles.toggle);
        if (!whiteBalanceAuto && setManualWhiteBalance && EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whiteBalance / 100, false);
            setManualWhiteBalance = false;
        }
        if (whiteBalanceAuto && EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whiteBalance / 100, true);
            setManualWhiteBalance = true;
        }

        EditorGUI.BeginChangeCheck();
        GUI.enabled = !whiteBalanceAuto;
        whiteBalance = 100 * EditorGUILayout.IntSlider("White balance", whiteBalance / 100, 26, 65);
        if (!whiteBalanceAuto && EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, whiteBalance, false);
        }

        GUI.enabled = true;
        EditorGUI.BeginChangeCheck();
        groupAuto = EditorGUILayout.Toggle("Automatic", groupAuto, EditorStyles.toggle);
        if (!groupAuto && setManualValue && EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, false);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposure, false);
            setManualValue = false;
        }
        if (groupAuto && zedCamera.IsCameraReady && EditorGUI.EndChangeCheck())
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, true);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposure, true);
            setManualValue = true;
        }
        EditorStyles.label.fontStyle = origFontStyle;

        GUI.enabled = !groupAuto;
        EditorGUI.BeginChangeCheck();
        gain = EditorGUILayout.IntSlider("Gain", gain, 0, 100);

        if (EditorGUI.EndChangeCheck())
        {
            if (!groupAuto)
            {
                zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, false);
            }
        }
        EditorGUI.BeginChangeCheck();
        exposure = EditorGUILayout.IntSlider("Exposure", exposure, 0, 100);
        if (EditorGUI.EndChangeCheck())
        {
            if (!groupAuto)
            {
                zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposure, false);
            }
        }

        refreshCount++;
        if (refreshCount >= refreshRate)
        {
            if (zedCamera != null && zedCamera.IsCameraReady)
            {
                exposure = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE);
                gain = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAIN);
                refreshCount = 0;
            }
        }

        GUI.enabled = true;
        EditorGUI.indentLevel = 0;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset", optionsButton))
        {
            brightness = cbrightness;
            contrast = ccontrast;
            hue = chue;
            saturation = csaturation;

            groupAuto = true;

            whiteBalanceAuto = true;

            ResetValues(groupAuto);
            zedCamera.RetrieveCameraSettings();
            loaded = false;
            if (zedCamera != null)
            {
                resetWanted = true;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save"))
        {
            SaveCameraSettings();
        }


        if (GUILayout.Button("Load"))
        {
            LoadCameraSettings();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

    }

    /// <summary>
    /// Resets all the values to defaults. Used when pressing the Reset button. 
    /// </summary>
    /// <param name="auto"></param>
    private void ResetValues(bool auto)
    {
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, cbrightness, false);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, ccontrast, false);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, 0, false);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, csaturation, false);

        if (auto)
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, cwhiteBalance, true);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, gain, true);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, exposure, true);
        }

    }

    /// <summary>
    /// Saves the camera settings into a file.
    /// </summary>
    void SaveCameraSettings()
    {
        zedCamera.SaveCameraSettings(ZEDSettingsPath);
    }

    /// <summary>
    /// Gets the registered values and updates the interface.
    /// </summary>
    private void UpdateValuesCameraSettings()
    {
        ZEDCameraSettingsManager.CameraSettings settings = zedCamera.GetCameraSettings();
        hue = settings.Hue;

        brightness = settings.Brightness;
        contrast = settings.Contrast;
        exposure = settings.Exposure;
        saturation = settings.Saturation;
        gain = settings.Gain;
        whiteBalance = settings.WhiteBalance;
    }

    /// <summary>
    /// Loads the data from the file and updates the current settings.
    /// </summary>
    void LoadCameraSettings()
    {
        zedCamera.LoadCameraSettings(ZEDSettingsPath);
        UpdateValuesCameraSettings();
        groupAuto = zedCamera.GetExposureUpdateType();
        whiteBalanceAuto = zedCamera.GetWhiteBalanceUpdateType();
        setManualWhiteBalance = true;
        loaded = true;
    }

    /// <summary>
    /// Draws a horizontal label with a label and a box. 
    /// </summary>
    /// <param name="name">Text of the label.</param>
    /// <param name="value">Value to be displayed. Will be converted to a string.</param>
    void LabelHorizontal(string name, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name);
        GUILayout.Box(value.ToString());
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Displays the calibration settings view.
    /// </summary>
    void CalibrationSettingsView()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.BeginVertical();
        GUILayout.Label("Left camera", EditorStyles.boldLabel);
        GUILayout.EndVertical();
        GUILayout.BeginVertical(EditorStyles.helpBox);

        LabelHorizontal("fx", parameters.leftCam.fx);
        LabelHorizontal("fy", parameters.leftCam.fy);
        LabelHorizontal("cx", parameters.leftCam.cx);
        LabelHorizontal("cy", parameters.leftCam.cy);

        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        if (parameters.leftCam.disto != null)
        {
            LabelHorizontal("k1", (float)parameters.leftCam.disto[0]);
            LabelHorizontal("k2", (float)parameters.leftCam.disto[1]);
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        LabelHorizontal("vFOV", parameters.leftCam.vFOV);
        LabelHorizontal("hFOV", parameters.leftCam.hFOV);

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.BeginVertical();
        GUILayout.Label("Right camera", EditorStyles.boldLabel);
        GUILayout.EndVertical();
        GUILayout.BeginVertical(EditorStyles.helpBox);

        LabelHorizontal("fx", parameters.rightCam.fx);
        LabelHorizontal("fy", parameters.rightCam.fy);
        LabelHorizontal("cx", parameters.rightCam.cx);
        LabelHorizontal("cy", parameters.rightCam.cy);

        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        if (parameters.rightCam.disto != null)
        {
            LabelHorizontal("k1", (float)parameters.rightCam.disto[0]);
            LabelHorizontal("k2", (float)parameters.rightCam.disto[1]);
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        LabelHorizontal("vFOV", parameters.rightCam.vFOV);
        LabelHorizontal("hFOV", parameters.rightCam.hFOV);

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        GUILayout.Label("Stereo", EditorStyles.boldLabel);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        LabelHorizontal("Baseline", parameters.Trans[0]);
        LabelHorizontal("Convergence", parameters.Rot[1]);
        GUILayout.EndVertical();

        GUILayout.Label("Optional", EditorStyles.boldLabel);
        GUILayout.BeginVertical(EditorStyles.helpBox);
        LabelHorizontal("Rx", parameters.Rot[0]);
        LabelHorizontal("Rz", parameters.Rot[2]);
        GUILayout.EndVertical();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    void OnGUI()
    {
        FirstInit();
        defaultColor = GUI.color;
        if (zedCamera != null && zedCamera.IsCameraReady)
            GUI.color = Color.green;
        else GUI.color = Color.red;
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.FlexibleSpace();
        if (zedCamera != null && zedCamera.IsCameraReady)
        {
            style.normal.textColor = Color.black;
            GUILayout.Label("Online", style);
        }
        else
        {
            style.normal.textColor = Color.black;
            if (!launched)
            {
                GUILayout.Label("To access information, please launch your scene once", style);
            }
            else
            {
                GUILayout.Label("Offline", style);
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUI.color = defaultColor;
        EditorGUI.BeginChangeCheck();
        tab = GUILayout.Toolbar(tab, new string[] { "Camera Control", "Calibration" });
        if (EditorGUI.EndChangeCheck())
        {
            if (zedCamera != null && zedCamera.IsCameraReady)
            {
                parameters = zedCamera.GetCalibrationParameters(false);
            }
        }
        switch (tab)
        {
            case 0:
                CameraSettingsView();
                break;

            case 1:
                CalibrationSettingsView();
                break;

            default:
                CameraSettingsView();
                break;
        }
    }
}
