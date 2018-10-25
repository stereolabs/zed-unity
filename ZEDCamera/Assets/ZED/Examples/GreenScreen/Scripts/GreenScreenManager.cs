//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using UnityEngine;
using System.IO;

/// <summary>
/// When attached to an object that also has a ZEDRenderingPlane, removes all real-world pixels
/// of a specified color. Useful for third-person mixed reality setups. 
/// Also requires the Frame object of the ZEDRenderingPlane component to have its MeshRenderer material set to Mat_ZED_GreenScreen.
/// The simplest way to use the plugin's GreenScreen feature is to use the ZED_GreenScreen prefab in the GreenScreen sample folder. 
/// For detailed information on using the ZED for greenscreen effects, see https://docs.stereolabs.com/mixed-reality/unity/green-screen-vr/.
/// </summary>
[RequireComponent(typeof(ZEDRenderingPlane))]
public class GreenScreenManager : MonoBehaviour
{
    /// <summary>
    /// The plane used for rendering. Equal to the canvas value of ZEDRenderingPlane. 
    /// </summary>
    private GameObject screen = null;

    /// <summary>
    /// The screen manager script. Automatically assigned in OnEnable(). 
    /// </summary>
	public ZEDRenderingPlane screenManager = null;

    /// <summary>
    /// Set to true when there is data available from a configuration file that needs to be loaded.
    /// It will then be loaded the next call to Update(). 
    /// </summary>
    private bool toUpdateConfig = false;

    /// <summary>
    /// Holds chroma key settings together in a single serializable object. 
    /// </summary>
    [System.Serializable]
    public struct ChromaKey
    {
        public Color color;
        public float smoothness;
        public float range;
    }
    /// <summary>
    /// Holds chroma key data together in a single serializable object. 
    /// </summary>
    [System.Serializable]
    public struct ChromaKeyData
    {
        public ChromaKey chromaKeys;
        public int erosion;
        public int numberBlurIterations;
        public float blurPower;
        public float whiteClip;
        public float blackClip;
        public float spill;
    }

    /// <summary>
    /// Holds greenscreen and garbage matte configuration data together in a single serializable object. 
    /// </summary>
    [System.Serializable]
    public struct GreenScreenData
    {
        public ChromaKeyData chromaKeyData;
        public GarbageMatte.GarbageMatteData garbageMatteData;
    }

    /// <summary>
    /// Array of available chroma key settings
    /// </summary>
    public ChromaKey keys;
    /// <summary>
    /// Array of available key colors
    /// </summary>
    [SerializeField]
    public Color keyColors = new Color(0.0f, 1.0f, 0.0f, 1);
    /// <summary>
    /// Causes pixels on the edge of the range to the color to fade out, instead of all pixels being 100% or 0% visible.
    /// </summary>
    [SerializeField]
    public float smoothness;
    /// <summary>
    /// Governs how similar a pixel must be to the chosen color to get removed.
    /// </summary>
    [SerializeField]
    public float range;
    /// <summary>
    /// subtracts the color value from the foreground image, making it appear "less green" for instance. 
    /// Useful because bright lighting can cause the color of a greenscreen to spill onto your actual subject.
    /// </summary>
    [SerializeField]
    public float spill = 0.2f;
    /// <summary>
    /// Default green color for the chroma key. 
    /// </summary>
    private Color defaultColor = new Color(0.0f, 1.0f, 0.0f, 1);
    /// <summary>
    /// Default Smoothness value. 
    /// </summary>
    private const float defaultSmoothness = 0.08f;
    /// <summary>
    /// Default Range value. 
    /// </summary>
    private const float defaultRange = 0.42f;
    /// <summary>
    /// Default Spill value. 
    /// </summary>
    private const float defaultSpill = 0.1f;
    /// <summary>
    /// Default Erosion value. 
    /// </summary>
    private const int defaultErosion = 0;
    /// <summary>
    /// Default White Clip value. 
    /// </summary>
    private const float defaultWhiteClip = 1.0f;
    /// <summary>
    /// Default Black Clip value. 
    /// </summary>
    private const float defaultBlackClip = 0.0f;
    /// <summary>
    /// Default sigma.
    /// </summary>
    private const float defaultSigma = 0.1f;
    /// <summary>
    /// Final rendering material, eg. the material on the ZEDRenderingPlane's canvas object. 
    /// </summary>
    public Material finalMat;
    /// <summary>
    /// Green screen effect material.
    /// </summary>
    private Material greenScreenMat;
    /// <summary>
    /// Material used to apply preprocessing effects in OnPreRender.
    /// </summary>
    private Material preprocessMat;
    /// <summary>
    /// Alpha texture for blending.
    /// </summary>
    private RenderTexture finalTexture;
    /// <summary>
    /// Public accessor for the alpha texture used for blending. 
    /// </summary>
    public RenderTexture FinalTexture
    {
        get { return finalTexture; }
    }
    /// <summary>
    /// Available canals (views) for displaying the chroma key effect.
    /// </summary>
    public enum CANAL
    {
        FOREGROUND,
        BACKGROUND,
        ALPHA,
        KEY,
        FINAL
    };
    /// <summary>
    /// The current view. Change to see different steps of the rendering stage, which can be helpful for tweaking other settings.
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public CANAL canal = CANAL.FINAL;

    /// <summary>
    /// Carves off pixels from the edges between the foreground and background.
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public int erosion = 0;

    /// <summary>
    /// Causes pixels with alpha values above its setting to be set to 100% alpha, useful for reducing noise resulting from the smoothness setting.
    /// </summary>
    [SerializeField]
    public float whiteClip = 1.0f;

    /// <summary>
    /// Causes pixels with alpha values below its setting to be set to 0% alpha, useful for reducing noise resulting from the smoothness setting.
    /// </summary>
    [SerializeField]
    public float blackClip = 0.0f;

    /// <summary>
    /// The path to the .config file, where configurations would be loaded or saved when you press the relevant button. 
    /// </summary>
    [SerializeField]
    public string pathFileConfig = "Assets/Config_greenscreen.json";

    /// <summary>
    /// Material used to apply blur effect in OnPreRender(). 
    /// </summary>
    private Material blurMaterial;
    /// <summary>
    /// Blur iteration number. A larger value increases the blur effect.
    /// </summary>
    public int numberBlurIterations = 5;
    /// <summary>
    /// Sigma value. A larger value increases the blur effect.
    /// </summary>
    public float sigma_ = 0.1f;
    /// <summary>
    /// Current sigma value.
    /// </summary>
    private float currentSigma = -1;
    /// <summary>
    /// Weights for blur effect. 
    /// </summary>
    private float[] weights_ = new float[5];
    /// <summary>
    /// Offsets for blur effect. 
    /// </summary>
    private float[] offsets_ = { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f };
    /// <summary>
    /// Material used to convert an RGBA texture into YUV, which makes it simpler to add certain effects. 
    /// </summary>
    public Material matYUV;
    /// <summary>
    /// Reference to the currently active GarbageMatte. 
    /// We make an empty one regardless of the enableGarbageMatte setting so a reference to one is always available. 
    /// </summary>
    [SerializeField]
    [HideInInspector]
    public GarbageMatte garbageMatte = new GarbageMatte();
    /// <summary>
    /// Whether we're using the garbage matte effect or not. 
    /// </summary>
    public bool enableGarbageMatte = false;
    /// <summary>
    /// All the data for the garbage matte. 
    /// </summary>
    private GarbageMatte.GarbageMatteData garbageMatteData;

    /// <summary>
    /// Sets all settings to the default values. Called on a new component, and when Reset is clicked. 
    /// </summary>
    public void SetDefaultValues()
    {
        keyColors = defaultColor;
        smoothness = defaultSmoothness;
        range = defaultRange;
        spill = defaultSpill;
        erosion = defaultErosion;
        whiteClip = defaultWhiteClip;
        blackClip = defaultBlackClip;
        sigma_ = defaultSigma;
    }

    /// <summary>
    /// Sets up the greenscreen with saved data.
    /// Used exclusively by ZEDSteamVRControllerManager to make sure this happens after controllers are set up. 
    /// Otherwise, included logic gets set elsewhere in different places. 
    /// </summary>
    public void PadReady()
    {
        if (garbageMatteData.numberMeshes != 0 && garbageMatte != null)
        {
            if (enableGarbageMatte)
            {
                garbageMatte = new GarbageMatte(GetComponent<Camera>(), finalMat, transform, garbageMatte);
            }
            garbageMatte.LoadData(garbageMatteData);
        }
    }

    private void OnEnable()
    {
        ZEDManager.OnZEDReady += ZEDReady;

        Shader.SetGlobalInt("ZEDGreenScreenActivated", 1);
		screenManager = GetComponent<ZEDRenderingPlane>();

    }

    private void OnDisable()
    {
        ZEDManager.OnZEDReady -= ZEDReady;
    }

    private void Awake()
    {
        Shader.SetGlobalInt("_ZEDStencilComp", 0);

        if (screen == null)
        {
            screen = gameObject.transform.GetChild(0).gameObject;
            finalMat = screen.GetComponent<Renderer>().material;
        }
        if (enableGarbageMatte)
        {
            garbageMatte = new GarbageMatte(GetComponent<Camera>(), finalMat, transform, garbageMatte);
        }

#if !UNITY_EDITOR
        Debug.Log("Load Chroma keys");
        LoadGreenScreenData();
        UpdateShader();
        CreateFileWatcher("");
#endif
    }

    /// <summary>
    /// Holds whether we need to apply the DEPTH_ALPHA keyword to the ZED imagefirst pass material (Mat_ZED_Forward)
    /// which causes it to process the greenscreen mask. 
    /// </summary>
    private bool textureOverlayInit = false;

    private void Update()
    {
        if(screenManager != null && !textureOverlayInit) //Need to tell the shader to apply the mask. 
        {
            if(screenManager.ManageKeywordForwardMat(true, "DEPTH_ALPHA"))
            {
                textureOverlayInit = true;
            }
        }

        if (toUpdateConfig) //Need to load available greenscreen configuration data into the current, active data. 
        {
            toUpdateConfig = false;
            LoadGreenScreenData();
        }

        if (enableGarbageMatte) //Set up the garbage matte if needed. 
        {
            if (garbageMatte != null && garbageMatte.IsInit)
            {
                garbageMatte.Update();
            }
            else
            {
                garbageMatte = new GarbageMatte(GetComponent<Camera>(), finalMat, transform, garbageMatte);
            }
        }
    }

    /// <summary>
    /// Initialization logic that must be done after the ZED camera has finished initializing. 
    /// Added to the ZEDManager.OnZEDReady() callback in OnEnable(). 
    /// </summary>
    private void ZEDReady()
    {
        //Set up textures and materials used for the final output. 
        finalTexture = new RenderTexture(sl.ZEDCamera.GetInstance().ImageWidth, sl.ZEDCamera.GetInstance().ImageHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        finalTexture.SetGlobalShaderProperty("ZEDMaskTexGreenScreen");
        finalMat.SetTexture("_MaskTex", finalTexture);
        greenScreenMat = Resources.Load("Materials/Mat_ZED_Compute_GreenScreen") as Material;
        blurMaterial = Resources.Load("Materials/PostProcessing/Mat_ZED_Blur") as Material;
        matYUV = Resources.Load("Materials/Mat_ZED_YUV") as Material;
        matYUV.SetInt("_isLinear", System.Convert.ToInt32(QualitySettings.activeColorSpace));

        preprocessMat = Resources.Load("Materials/Mat_ZED_Preprocess") as Material;
        preprocessMat.SetTexture("_CameraTex", screenManager.TextureEye);
        ZEDPostProcessingTools.ComputeWeights(1, out weights_, out offsets_);

        //Send the values to the current shader.
        blurMaterial.SetFloatArray("weights2", weights_);
        blurMaterial.SetFloatArray("offset2", offsets_);
        greenScreenMat.SetTexture("_CameraTex", screenManager.TextureEye);

        UpdateShader();
        UpdateCanal();
        if (System.IO.File.Exists("ZED_Settings.conf"))
        {
            sl.ZEDCamera.GetInstance().LoadCameraSettings("ZED_Settings.conf");
            sl.ZEDCamera.GetInstance().SetCameraSettings();
        }
    }

    /// <summary>
    /// Update the current canal (VIEW) to the setting in the editor. 
    /// The greenscreen shader uses #ifdef keywords to render differently depending on the active canal.
    /// This class makes sure that exactly one such keyword is defined at one time. 
    /// </summary>
    public void UpdateCanal()
    {
        foreach (CANAL c in System.Enum.GetValues(typeof(CANAL))) //Clear all keywords
        {
            manageKeyWord(false, c.ToString());
        }

        if (screenManager != null)
        {
            //Set NO_DEPTH keyword as well if set to BACKGROUND. 
            if (canal == CANAL.BACKGROUND) screenManager.ManageKeywordForwardMat(true, "NO_DEPTH");
            else screenManager.ManageKeywordForwardMat(false, "NO_DEPTH");

            manageKeyWord(true, canal.ToString()); //Activate the keyword corresponding to the current Canal. 
        }
    }

    /// <summary>
    /// Enables or disables a shader keyword in the finalMat material.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="name"></param>
    void manageKeyWord(bool value, string name)
    {
        if (finalMat != null)
        {
            if (value)
            { 
                finalMat.EnableKeyword(name);
            }
            else
            {
                finalMat.DisableKeyword(name);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate() //When the Inspector is visible and it changes somehow. 
    {
        UpdateShader();
    }
#endif

    private void OnApplicationQuit() 
    {
        if (finalTexture != null && finalTexture.IsCreated()) finalTexture.Release();
    }

    public void Reset() //When the Unity editor Reset button is used.
    {

        ZEDManager zedManager = null;
        zedManager = transform.parent.gameObject.GetComponent<ZEDManager>();
        if (zedManager != null)
        {
            zedManager.resolution = sl.RESOLUTION.HD1080;
            Debug.Log("Resolution set to HD1080 for better result");
        }
        SetDefaultValues();
    }

    /// <summary>
    /// Helper function to convert colors in RGB color space to YUV format. 
    /// </summary>
    /// <param name="rgb">Color to be converted.</param>
    /// <param name="clamped">Whether to keep the values within the maximum.</param>
    /// <returns></returns>
    Vector3 RGBtoYUV(Color rgb, bool clamped = true)
    {
        double Y = 0.182586f * rgb.r + 0.614231f * rgb.g + 0.062007f * rgb.b + 0.062745f; // Luma
        double U = -0.100644f * rgb.r - 0.338572f * rgb.g + 0.439216f * rgb.b + 0.501961f; // Delta Blue
        double V = 0.439216f * rgb.r - 0.398942f * rgb.g - 0.040274f * rgb.b + 0.501961f; // Delta Red
        if (!clamped)
        {
            U = -0.100644 * rgb.r - 0.338572 * rgb.g + 0.439216 * rgb.b; // Delta Blue
            V = 0.439216 * rgb.r - 0.398942 * rgb.g - 0.040274 * rgb.b; // Delta Red
        }
        return new Vector3((float)Y, (float)U, (float)V);
    }


    /// <summary>
    /// Update all the data to the greenscreen shader.
    /// The weights and offsets will be set when sigma changes.
    /// </summary>
    public void UpdateShader()
    {
        if (greenScreenMat != null)
        {
            greenScreenMat.SetVector("_keyColor", RGBtoYUV(keyColors));
            greenScreenMat.SetFloat("_range", range);

            preprocessMat.SetFloat("_erosion", erosion);

            preprocessMat.SetFloat("_smoothness", smoothness);
            preprocessMat.SetFloat("_whiteClip", whiteClip);
            preprocessMat.SetFloat("_blackClip", blackClip);
            preprocessMat.SetFloat("_spill", spill);
            preprocessMat.SetColor("_keyColor", keyColors);
        }
    }

    /// <summary>
    /// Load the data from a file and fills a structure.
    /// </summary>
    /// <returns>Whether there's a valid file where pathFileConfig says there is.</returns>
    private bool LoadData(out GreenScreenData gsData)
    {
        gsData = new GreenScreenData();
        if (File.Exists(pathFileConfig))
        {
            string dataAsJson = File.ReadAllText(pathFileConfig);
            gsData = JsonUtility.FromJson<GreenScreenData>(dataAsJson);
            return true;
        }
      
        return false;
    }

    /// <summary>
    /// Creates a new serializable ChromaKeyData file from the current settings. 
    /// </summary>
    public ChromaKeyData RegisterDataChromaKeys()
    {
        ChromaKeyData chromaKeyData = new ChromaKeyData();
        chromaKeyData.chromaKeys = new ChromaKey();
        chromaKeyData.erosion = erosion;
        chromaKeyData.blurPower = sigma_;
        chromaKeyData.numberBlurIterations = numberBlurIterations;
        chromaKeyData.whiteClip = whiteClip;
        chromaKeyData.spill = spill;
        chromaKeyData.blackClip = blackClip;


        chromaKeyData.chromaKeys.color = keyColors;
        chromaKeyData.chromaKeys.smoothness = smoothness;
        chromaKeyData.chromaKeys.range = range;

        return chromaKeyData;
    }

    /// <summary>
    /// Saves the chroma keys used in a file (JSON format).
    /// </summary>
    public void SaveData(ChromaKeyData chromaKeyData, GarbageMatte.GarbageMatteData garbageMatteData)
    {
        GreenScreenData gsData = new GreenScreenData();
        gsData.chromaKeyData = chromaKeyData;
        gsData.garbageMatteData = garbageMatteData;
        string dataAsJson = JsonUtility.ToJson(gsData);

        File.WriteAllText(pathFileConfig, dataAsJson);
    }

    /// <summary>
    /// Fills the current chroma keys with the data from a file.
    /// </summary>
    public void LoadGreenScreenData(bool forcegarbagemate = false)
    {
        GreenScreenData gsData;
        if (LoadData(out gsData))
        {
            ChromaKeyData chromaKeyData = gsData.chromaKeyData;
            garbageMatteData = gsData.garbageMatteData;

            erosion = chromaKeyData.erosion;
            sigma_ = chromaKeyData.blurPower;
            numberBlurIterations = chromaKeyData.numberBlurIterations;
            whiteClip = chromaKeyData.whiteClip;
            blackClip = chromaKeyData.blackClip;
            spill = chromaKeyData.spill;

            keyColors = chromaKeyData.chromaKeys.color;
            smoothness = chromaKeyData.chromaKeys.smoothness;
            range = chromaKeyData.chromaKeys.range;
        }

        UpdateShader();

        if (forcegarbagemate && garbageMatte != null)
        {
            if (!garbageMatte.IsInit)
            {
                garbageMatte = new GarbageMatte(GetComponent<Camera>(), finalMat, transform, garbageMatte);
            }
            enableGarbageMatte = true;
            garbageMatte.LoadData(gsData.garbageMatteData);
            garbageMatte.ApplyGarbageMatte();
        }
    }


    /// <summary>
    /// Where various image processing effects are applied, including the green screen effect itself. 
    /// </summary>
    private void OnPreRender()
    {
        if (screenManager.TextureEye == null || screenManager.TextureEye.width == 0) return;
        if (canal.Equals(CANAL.FOREGROUND)) return;


        RenderTexture tempAlpha = RenderTexture.GetTemporary(finalTexture.width, finalTexture.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        RenderTexture tempFinalAlpha = RenderTexture.GetTemporary(finalTexture.width, finalTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        Graphics.Blit(screenManager.TextureEye, tempAlpha, greenScreenMat);
        preprocessMat.SetTexture("_MaskTex", tempAlpha);

        Graphics.Blit(screenManager.TextureEye, tempFinalAlpha, preprocessMat);

        //If the sigma has changed, recompute the weights and offsets used by the blur.
        if (sigma_ == 0)
        {
            if (sigma_ != currentSigma)
            {
                currentSigma = sigma_;

                ZEDPostProcessingTools.ComputeWeights(currentSigma, out weights_, out offsets_);

                //Send the values to the current shader
                blurMaterial.SetFloatArray("weights", weights_);
                blurMaterial.SetFloatArray("offset", offsets_);
            }
            ZEDPostProcessingTools.Blur(tempFinalAlpha, finalTexture, blurMaterial, 0, 1, 1);
        }
        else
        {
            Graphics.Blit(tempFinalAlpha, finalTexture);
        }

        //Destroy all the temporary buffers
        RenderTexture.ReleaseTemporary(tempAlpha);
        RenderTexture.ReleaseTemporary(tempFinalAlpha);
    }

    /// <summary>
    /// Gets a value within a gaussian spread defined by sigma. 
    /// </summary>
    float Gaussian(float x, float sigma)
    {
        return (1.0f / (2.0f * Mathf.PI * sigma)) * Mathf.Exp(-((x * x) / (2.0f * sigma)));
    }

    /// <summary>
    /// Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories.
    /// Used to make sure the path to the config file remains valid. 
    /// </summary>
    /// <param name="path"></param>
    public void CreateFileWatcher(string path)
    {
        if (!File.Exists(pathFileConfig)) return;

        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = path;

        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        watcher.Filter = Path.GetFileName(pathFileConfig);

        watcher.Changed += new FileSystemEventHandler(OnChanged);
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Event handler for when the path to the config file changes while within the editor. 
    /// </summary>
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        toUpdateConfig = true;
    }
}
