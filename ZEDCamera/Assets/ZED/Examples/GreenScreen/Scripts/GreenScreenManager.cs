//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.IO;


/// <summary>
/// Creates a mask from a chroma key, and is used as an interface for the garbage matte
/// </summary>
[RequireComponent(typeof(ZEDRenderingPlane))]
public class GreenScreenManager : MonoBehaviour
{
    /// <summary>
    /// The plane used for rendering
    /// </summary>
    private GameObject screen = null;
    /// <summary>
    /// The screen manager script
    /// </summary>
	public ZEDRenderingPlane screenManager = null;

    private bool toUpdateConfig = false;
    /// <summary>
    /// Chroma key settings
    /// </summary>
    [System.Serializable]
    public struct ChromaKey
    {
        public Color color;
        public float smoothness;
        public float range;
    }
    /// <summary>
    /// Chroma key data
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
    /// Array of available similarity
    /// </summary>
    [SerializeField]
    public float smoothness;
    /// <summary>
    /// Array of available blend
    /// </summary>

    [SerializeField]
    public float range;
    /// <summary>
    /// Array of available blend
    /// </summary>
    /// 
    [SerializeField]
    public float spill = 0.2f;
    /// <summary>
    /// Default color for chroma key
    /// </summary>
    private Color defaultColor = new Color(0.0f, 1.0f, 0.0f, 1);
    /// <summary>
    /// DEfault similarity
    /// </summary>
    private const float defaultSmoothness = 0.08f;
    /// <summary>
    /// Default blend
    /// </summary>
    private const float defaultRange = 0.42f;
    /// <summary>
    /// Default blend
    /// </summary>
    private const float defaultSpill = 0.1f;
    /// <summary>
    /// Default erosion
    /// </summary>
    private const int defaultErosion = 0;
    /// <summary>
    /// Default white clip
    /// </summary>
    private const float defaultWhiteClip = 1.0f;
    /// <summary>
    /// Default black clip
    /// </summary>
    private const float defaultBlackClip = 0.0f;
    /// <summary>
    /// Default sigma
    /// </summary>
    private const float defaultSigma = 0.1f;

    /// <summary>
    /// Final rendering material
    /// </summary>
    public Material finalMat;
    /// <summary>
    /// Green screen effect material
    /// </summary>
    private Material greenScreenMat;

    private Material preprocessMat;

    /// <summary>
    /// Alpha texture for blending
    /// </summary>
    private RenderTexture finalTexture;
    public RenderTexture FinalTexture
    {
        get { return finalTexture; }
    }
    /// <summary>
    /// Available canals for display chroma key effect
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
    /// Current canal used
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public CANAL canal = CANAL.FINAL;

    /// <summary>
    /// Erosion value
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public int erosion = 0;

    /// <summary>
    /// CutOff value
    /// </summary>
    [SerializeField]
    public float whiteClip = 1.0f;

    [SerializeField]
    public float blackClip = 0.0f;

    /// <summary>
    /// Green screen shader name
    /// </summary>
    [SerializeField]
    public string pathFileConfig = "Assets/Config_greenscreen.json";


    /// <summary>
    /// Blur material
    /// </summary>
    private Material blurMaterial;
    /// <summary>
    /// Blur iteration number. A larger value increase blur effect.
    /// </summary>
    public int numberBlurIterations = 5;
    /// <summary>
    /// Sigma value. A larger value increase blur effect.
    /// </summary>
    public float sigma_ = 0.1f;
    /// <summary>
    /// Current sigma value
    /// </summary>
    private float currentSigma = -1;
    /// <summary>
    /// Weights for blur
    /// </summary>
    private float[] weights_ = new float[5];
    /// <summary>
    /// Offsets for blur
    /// </summary>
    private float[] offsets_ = { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f };


    public Material matYUV;
    [SerializeField]
    [HideInInspector]
    public GarbageMatte garbageMatte = new GarbageMatte();
    public bool enableGarbageMatte = false;
    private GarbageMatte.GarbageMatteData garbageMatteData;
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
        ZEDSteamVRControllerManager.ZEDOnPadIndexSet += PadReady;


        Shader.SetGlobalInt("ZEDGreenScreenActivated", 1);
		screenManager = GetComponent<ZEDRenderingPlane>();

    }

    private void OnDisable()
    {
        ZEDManager.OnZEDReady -= ZEDReady;
        ZEDSteamVRControllerManager.ZEDOnPadIndexSet -= PadReady;
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

    private bool textureOverlayInit = false;

    private void Update()
    {
        if(screenManager != null && !textureOverlayInit)
        {
            if(screenManager.ManageKeyWordForwardMat(true, "DEPTH_ALPHA"))
            {
                textureOverlayInit = true;
            }
        }

        if (toUpdateConfig)
        {
            toUpdateConfig = false;
            LoadGreenScreenData();
        }
        if (enableGarbageMatte)
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


    private void ZEDReady()
    {
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

        //Send the values to the current shader
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
    /// Update the current canal (VIEW)
    /// </summary>
    public void UpdateCanal()
    {
        foreach (CANAL c in System.Enum.GetValues(typeof(CANAL)))
        {
            manageKeyWord(false, c.ToString());
        }

        if (screenManager != null)
        {
            if (canal == CANAL.BACKGROUND) screenManager.ManageKeyWordForwardMat(true, "NO_DEPTH");
            else screenManager.ManageKeyWordForwardMat(false, "NO_DEPTH");
            manageKeyWord(true, canal.ToString());
        }
    }

    /// <summary>
    /// Enable or disable a keyword
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
    private void OnValidate()
    {
        UpdateShader();
    }
#endif

    private void OnApplicationQuit()
    {
        if (finalTexture != null && finalTexture.IsCreated()) finalTexture.Release();
    }

    public void Reset()
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
    /// Update all the data to the shader
    /// The weights and offsets will be set when sigma change
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
    /// Load the data from a file and fill a structure
    /// </summary>
    /// <returns></returns>
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
    /// Save the chroma keys used in a file (JSON format)
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
    /// Return a string from a pointer to char
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
    /// Fill the current chroma keys with the data from a file
    /// </summary>
    public void LoadGreenScreenData(bool forceGargabeMatte = false)
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

        if (forceGargabeMatte && garbageMatte != null)
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
    /// Return a string from a pointer to char
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

        //If the sigma has changed recompute the weights and offsets used by the blur
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

    float Gaussian(float x, float sigma)
    {
        return (1.0f / (2.0f * Mathf.PI * sigma)) * Mathf.Exp(-((x * x) / (2.0f * sigma)));
    }

    public void CreateFileWatcher(string path)
    {
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = path;
        /* Watch for changes in LastAccess and LastWrite times, and 
           the renaming of files or directories. */
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        watcher.Filter = pathFileConfig;

        watcher.Changed += new FileSystemEventHandler(OnChanged);
        watcher.EnableRaisingEvents = true;
    }

    // Define the event handlers.
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        toUpdateConfig = true;

    }
}
