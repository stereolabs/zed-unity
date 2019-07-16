using UnityEngine;
using System;
using System.Threading;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// The central script of the ZED Unity plugin, and the primary way a developer can interact with the camera.
/// It sets up and closes connection to the ZED, adjusts parameters based on user settings, enables/disables/handles
/// features like tracking, and holds numerous useful properties, methods, and callbacks.
/// </summary>
/// <remarks>
/// ZEDManager is attached to the root objects in the ZED_Rig_Mono and ZED_Rig_Stereo prefabs. 
/// If using ZED_Rig_Stereo, it will set isStereoRig to true, which triggers several behaviors unique to stereo pass-through AR. 
/// </remarks>
public class ZEDManager : MonoBehaviour
{

    /// <summary>
    /// Static function to get instance of the ZEDManager with a given camera_ID. See sl.ZED_CAMERA_ID for the available choices.
    /// </summary>
    public static object grabLock;
    static ZEDManager[] ZEDManagerInstance = null;
    public static ZEDManager GetInstance(sl.ZED_CAMERA_ID _id)
    {
        if (ZEDManagerInstance == null)
            return null;
        else
            return ZEDManagerInstance[(int)_id];
    }

    /// <summary>
    /// Static function to get all ZEDManagers that have been properly instantiated. 
    /// <para>Cameras may not necessarily be connected, if they haven't finished connecting, have disconnected,
    /// or if no camera is available.</para>
    /// </summary>
    /// <returns></returns>
    public static List<ZEDManager> GetInstances()
    {
        List<ZEDManager> instances = new List<ZEDManager>();
        for (int i = 0; i < (int)sl.Constant.MAX_CAMERA_PLUGIN; i++)
        {
            ZEDManager instance = GetInstance((sl.ZED_CAMERA_ID)i);
            if (instance != null)
                instances.Add(instance);
        }
        return instances;
    }


    /// <summary>
    /// For advanced debugging. Default false. Set true for the Unity wrapper to log all SDK calls to a new file
    /// at C:/ProgramData/stereolabs/SL_Unity_wrapper.txt. This helps find issues that may occur within
    /// the protected .dll, but can decrease performance. 
    /// </summary>
    private bool wrapperVerbose = true;

    /// <summary>
    /// Current instance of the ZED Camera, which handles calls to the Unity wrapper .dll. 
    /// </summary>
    public sl.ZEDCamera zedCamera = null;

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Camera Settings ///////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Resolution setting for all images retrieved from the camera. Higher resolution means lower framerate.
    /// HD720 is strongly recommended for pass-through AR.
    /// </summary>

    /// <summary>
    /// Camera ID
    /// </summary>
    [HideInInspector]
    public sl.ZED_CAMERA_ID cameraID = sl.ZED_CAMERA_ID.CAMERA_ID_01;

    /// <summary>
    /// The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance. 
    /// Note there's a significant jump in performance cost between QUALITY and ULTRA modes.
    /// </summary>
    /*[Tooltip("The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance.")]*/
    [HideInInspector]
    public sl.DEPTH_MODE depthMode = sl.DEPTH_MODE.PERFORMANCE;


    /// <summary>
    /// Input Type in SDK (USB, SVO or Stream)
    /// </summary>
    [HideInInspector]
    public sl.INPUT_TYPE inputType = sl.INPUT_TYPE.INPUT_TYPE_USB;
    /// <summary>
    /// Camera Resolution
    /// </summary>
    [HideInInspector]
    public sl.RESOLUTION resolution = sl.RESOLUTION.HD720;
    /// <summary>
    /// Targeted FPS, based on the resolution. VGA = 100, HD720 = 60, HD1080 = 30, HD2K = 15. 
    /// </summary>
	[HideInInspector]
    public int FPS = 60;

    /// <summary>
    /// SVO Input FileName
    /// </summary>
    [HideInInspector]
    public string svoInputFileName = "";

    /// <summary>
    /// SVO loop back option
    /// </summary>
    [HideInInspector]
    public bool svoLoopBack = true;

    /// <summary>
    /// SVO loop back option
    /// </summary>
    [HideInInspector]
    public bool svoRealTimeMode = false;

    /// <summary>
    /// Current frame being read from the SVO. Doesn't apply when recording. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int currentFrame = 0;
    /// <summary>
    /// Current frame being read from the SVO. Doesn't apply when recording. 
    /// </summary>
    public int CurrentFrame
    {
        get
        {
            return currentFrame;
        }
        set
        {
            currentFrame = value;
        }
    }

    /// <summary>
    /// Total number of frames in a loaded SVO. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private int numberFrameMax = 0;
    /// <summary>
    /// Total number of frames in a loaded SVO. 
    /// </summary>
    public int NumberFrameMax
    {
        set
        {
            numberFrameMax = value;
        }
        get
        {
            return numberFrameMax;
        }
    }
    [HideInInspector]
    [SerializeField]
    public bool pauseSVOReading = false;
    [HideInInspector]
    public bool pauseLiveReading = false;

    /// <summary>
    /// Ask a new frame is in pause (SVO only)
    /// </summary>
    [HideInInspector]
    public bool NeedNewFrameGrab = false;

    /// <summary>
    /// Streaming Input IP (v2.8)
    /// </summary>
    [HideInInspector]
    public string streamInputIP = "127.0.0.1";

    /// <summary>
    /// Streaming Input Port (v2.8)
    /// </summary>
    [HideInInspector]
    public int streamInputPort = 30000;




    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Motion Tracking ///////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// If enabled, the ZED will move/rotate itself using its own inside-out tracking.
    /// If false, the camera tracking will move with the VR HMD if connected and available.
    /// <para>Normally, ZEDManager's GameObject will move according to the tracking. But if in AR pass-through mode, 
    /// then the Camera_eyes object in ZED_Rig_Stereo will move while this object stays still. </para>
    /// </summary>
    [HideInInspector]
    public bool enableTracking = true;


    /// <summary>
    /// Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment, 
    /// but may cause visible jumps when it happens.
    /// </summary>
    [HideInInspector]
    public bool enableSpatialMemory = true;
    /// <summary>
    /// If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded. 
    /// .area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan. 
    /// </summary>
    [HideInInspector]
    public string pathSpatialMemory;

    /// <summary>
    /// Estimate initial position by detecting the floor.
    /// </summary>
    [HideInInspector]
    public bool estimateInitialPosition = true;



    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Spatial Mapping ///////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Resolution setting for the scan. A higher resolution creates more submeshes and uses more memory, but is more accurate.
    /// </summary>
    [HideInInspector]
    public ZEDSpatialMapping.RESOLUTION mappingResolutionPreset = ZEDSpatialMapping.RESOLUTION.MEDIUM;

    /// <summary>
    /// Maximum distance geometry can be from the camera to be scanned. Geometry scanned from farther away will be less accurate. 
    /// </summary>
    [HideInInspector]
    public ZEDSpatialMapping.RANGE mappingRangePreset = ZEDSpatialMapping.RANGE.MEDIUM;

    /// <summary>
    /// Whether mesh filtering is needed.
    /// </summary>
    [HideInInspector]
    public bool isMappingFilteringEnable = false;

    /// <summary>
    /// Whether surface textures will be scanned and applied. Note that texturing will add further delay to the post-scan finalizing period. 
    /// </summary>
    [HideInInspector]
    public bool isMappingTextured = false;

    /// <summary>
    /// Whether to save the mesh .obj and .area files once the scan is finished. 
    /// </summary>
    [HideInInspector]
    public bool saveMeshWhenOver = false;

    /// <summary>
    /// Path to save the .obj and .area files. 
    /// </summary>
    [HideInInspector]
    public string meshPath = "Assets/ZEDMesh.obj";

    /// <summary>
    /// Filtering setting. More filtering results in fewer faces in the mesh, reducing both file size and accuracy. 
    /// </summary>
    [HideInInspector]
    public sl.FILTER meshFilterParameters;

    /// <summary>
    /// Instance of the ZEDSpatialMapping class that handles the actual spatial mapping implementation within Unity. 
    /// </summary>
    [HideInInspector]
    private ZEDSpatialMapping spatialMapping = null;
    public ZEDSpatialMapping GetSpatialMapping { get { return spatialMapping; } }

    /// <summary>
    /// Whether the spatial mapping is currently scanning. 
    /// </summary>
    public bool IsMappingRunning { get { return spatialMapping != null ? spatialMapping.IsRunning() : false; } }

    /// <summary>
    /// List of the processed submeshes. This list isn't filled until StopSpatialMapping() is called. 
    /// </summary>
    public List<ZEDSpatialMapping.Chunk> MappingChunkList { get { return spatialMapping != null ? spatialMapping.ChunkList : null; } }

    /// <summary>
    /// Whether the mesh update thread is running. 
    /// </summary>
    public bool IsMappingUpdateThreadRunning { get { return spatialMapping != null ? spatialMapping.IsUpdateThreadRunning : false; } }

    /// <summary>
    /// Whether the spatial mapping was running but has been paused (not stopped) by the user. 
    /// </summary>
    public bool IsMappingPaused { get { return spatialMapping != null ? spatialMapping.IsPaused : false; } }

    /// <summary>
    /// Whether the mesh is in the texturing stage of finalization. 
    /// </summary>
    public bool IsMappingTexturingRunning { get { return spatialMapping != null ? spatialMapping.IsTexturingRunning : false; } }

    /// <summary>
    /// Gets a value indicating whether the spatial mapping display is enabled.
    /// </summary>
    public bool IsSpatialMappingDisplay { get { return spatialMapping != null ? spatialMapping.display : false; } }
 

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////////// Rendering ///////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Rendering paths available to the ZED with the corresponding Unity rendering path. 
    /// </summary>
    public enum ZEDRenderingMode
    {
        FORWARD = RenderingPath.Forward,
        DEFERRED = RenderingPath.DeferredShading
    };

    /// <summary>
    /// When enabled, the real world can occlude (cover up) virtual objects that are behind it. 
    /// Otherwise, virtual objects will appear in front.  
    /// </summary>
    [HideInInspector]
    public bool depthOcclusion = true;

    /// <summary>
    /// Enables post-processing effects on virtual objects that blends them in with the real world.
    /// </summary>
    [HideInInspector]
    public bool postProcessing = true;

    /// <summary>
    /// Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way. 
    /// This is a rendering setting that doesn't affect the raw input from the camera.
    /// </summary>
    [HideInInspector]
    public int m_cameraBrightness = 100;
    /// <summary>
    /// Public accessor for m_cameraBrightness, which is the post-capture brightness setting of the real-world image. 
    /// </summary>
	public int CameraBrightness
    {
        get { return m_cameraBrightness; }
        set
        {
            if (m_cameraBrightness == value) return;
            m_cameraBrightness = value;
            if (OnCamBrightnessChange != null)
                OnCamBrightnessChange(m_cameraBrightness);
        }
    }

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Recording Module //////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// SVO Output file name
    /// </summary>
    [HideInInspector]
    public string svoOutputFileName = "Assets/Recording.svo";

    /// <summary>
    /// SVO Compression mode used for recording
    /// </summary>
    [HideInInspector]
    public sl.SVO_COMPRESSION_MODE svoOutputCompressionMode = sl.SVO_COMPRESSION_MODE.AVCHD_BASED;
    
    /// <summary>
    /// Indicates if frame must be recorded
    /// </summary>
    [HideInInspector]
    public bool needRecordFrame = false;

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Streaming Module //////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Enable/Disable Streaming module
    /// </summary>
    [HideInInspector]
    public bool enableStreaming = false;
    /// <summary>
    /// Status of streaming request
    /// </summary>
    private bool isStreamingEnable = false;

    /// <summary>
    /// Codec used for Streaming
    /// </summary>
    [HideInInspector]
    public sl.STREAMING_CODEC streamingCodec = sl.STREAMING_CODEC.AVCHD_BASED;

    /// <summary>
    /// port used for Streaming
    /// </summary>
    [HideInInspector]
    public int streamingPort = 30000;
    
    /// <summary>
    /// bitrate used for Streaming
    /// </summary>
    [HideInInspector]
    public int bitrate = 8000;

    /// <summary>
    /// gop size used for Streaming
    /// </summary>
    [HideInInspector]
    public int gopSize = -1;
    
    /// <summary>
    /// Enable/Disable adaptative bitrate
    /// </summary>
    [HideInInspector]
    public bool adaptativeBitrate = false;

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Advanced  control /////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    /// 
    /// <summary>
    /// True to make the ZED image fade from black when the application starts. 
    /// </summary>
    [HideInInspector]
    public bool fadeInOnStart = true;
    /// <summary>
    /// True to apply DontDestroyOnLoad() on the ZED rig in Awake(), preserving it between scenes. 
    /// </summary>
    [HideInInspector]
    public bool dontDestroyOnLoad = false;

    /// <summary>
    /// Grey Out Skybox on Start", "True to set the background to a neutral gray when the scene starts.
    /// Recommended for AR so that lighting on virtual objects better matches the real world.
    /// </summary>
    [HideInInspector]
    public bool greySkybox = true;

    /// <summary>
    /// Delegate for OnCamBrightnessChange, which is used to update shader properties when the brightness setting changes. 
    /// </summary>
    /// <param name="newVal"></param>
	public delegate void onCamBrightnessChangeDelegate(int newVal);
    /// <summary>
    /// Event fired when the camera brightness setting is changed. Used to update shader properties. 
    /// </summary>
	public event onCamBrightnessChangeDelegate OnCamBrightnessChange;

    /// <summary>
    /// Whether to show the hidden camera rig used in stereo AR mode to prepare images for HMD output. 
    /// </summary>
    [SerializeField]
    [HideInInspector]
    private bool showarrig = false;
    /// <summary>
    /// Whether to show the hidden camera rig used in stereo AR mode to prepare images for HMD output. 
    /// <para>This is rarely needed, but can be useful for understanding how the ZED output works.</para>
    /// </summary>
    public bool showARRig
    {
        get
        {
            return showarrig;
        }
        set
        {
            if (Application.isPlaying && showarrig != value && zedRigDisplayer != null)
            {
                zedRigDisplayer.hideFlags = value ? HideFlags.None : HideFlags.HideInHierarchy;
            }

            showarrig = value;
        }
    }


    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Status Report /////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    //Strings used for the Status display in the Inspector. 
    [Header("Status")]
    /// <summary>
    /// The camera model (ZED or ZED-M).
    /// </summary>
    [ReadOnly("Camera S/N")] [HideInInspector] public string cameraModel = "-";
    /// <summary>
    /// The camera serial number.
    /// </summary>
    [ReadOnly("Camera S/N")] [HideInInspector] public string cameraSerialNumber = "-";
    /// <summary>
    /// The camera firmware version
    /// </summary>
    [ReadOnly("Camera Firmware")] [HideInInspector] public string cameraFirmware = "-";
    /// <summary>
    /// Version of the installed ZED SDK, for display in the Inspector.
    /// </summary>
    [ReadOnly("Version")] [HideInInspector] public string versionZED = "-";
    /// <summary>
    /// How many frames per second the engine is rendering, for display in the Inspector. 
    /// </summary>
    [ReadOnly("Engine FPS")] [HideInInspector] public string engineFPS = "-";
    /// <summary>
    /// How many images per second are received from the ZED, for display in the Inspector. 
    /// </summary>
    [ReadOnly("Camera FPS")] [HideInInspector] public string cameraFPS = "-";
    /// <summary>
    /// The connected VR headset, if any, for display in the Inspector. 
    /// </summary>
    [ReadOnly("HMD Device")] [HideInInspector] public string HMDDevice = "-";
    /// <summary>
    /// Whether the ZED's tracking is on, off, or searching (lost position, trying to recover) for display in the Inspector.
    /// </summary>
    [ReadOnly("Tracking State")] [HideInInspector] public string trackingState = "-";



    ////////////////////////////
    //////// Private ///////////
    ////////////////////////////
    /// <summary>
    /// Initialization parameters used to start the ZED. Holds settings that can't be changed at runtime
    /// (resolution, depth mode, .SVO path, etc.).
    /// </summary>
    private sl.InitParameters initParameters;
    /// <summary>
    /// Runtime parameters used to grab a new image. Settings can change each frame, but are lower level
    /// (sensing mode, point cloud, if depth is enabled, etc.).
    /// </summary>
    private sl.RuntimeParameters runtimeParameters;
    /// <summary>
    /// Enables the ZED SDK's depth stabilizer, which improves depth accuracy and stability. There's rarely a reason to disable this. 
    /// </summary>
    private bool depthStabilizer = true;
    /// <summary>
    /// Disable the IMU of the ZED-M
    /// </summary>
    private bool cameraDisableIMU = false;
    /// <summary>
    /// Set the camera in Flip mode
    /// </summary>
    private bool cameraFlipMode = false;
    /// <summary>
    /// Whether the camera is currently being tracked using the ZED's inside-out tracking. 
    /// </summary>
    private bool isZEDTracked = false;
    /// <summary>
    /// Whether the ZED's inside-out tracking has been activated.
    /// </summary>
    private bool isTrackingEnable = false;
    /// <summary>
    /// Whether the camera is tracked in any way (ZED's tracking or a VR headset's tracking). 
    /// </summary>
    private bool isCameraTracked = false;
    /// <summary>
    /// Public accessor for whether the camera is tracked in any way (ZED's tracking or a VR headset's tracking). 
    /// </summary>
	public bool IsCameraTracked
    {
        get { return isCameraTracked; }
    }

    /// <summary>
    /// Whether the camera has a new frame available.
    /// </summary>
    private bool isNewFrameGrabbed = false;
    /// <summary>
    /// Public accessor for whether the camera has a new frame available.
    /// </summary>
    public bool IsNewFrameGrabbed
    {
        get { return isNewFrameGrabbed; }
    }

    /// <summary>
    /// Orientation last returned by the ZED's tracking.
    /// </summary>
    private Quaternion zedOrientation = Quaternion.identity;
    /// <summary>
    /// Position last returned by the ZED's tracking.
    /// </summary>
	private Vector3 zedPosition = Vector3.zero;
    /// <summary>
    /// Instance of the manager that handles reading/recording SVO files, which are video files
    /// with metadata that you can treat like regular ZED input. 
    /// </summary>
    //private ZEDSVOManager zedSVOManager;

    /// <summary>
    /// Position of the camera (zedRigRoot) when the scene starts. Not used in Stereo AR. 
    /// </summary>
    private Vector3 initialPosition = new Vector3();
    /// <summary>
	/// Orientation of the camera (zedRigRoot) when the scene starts. Not used in Stereo AR. 
    /// </summary>
	private Quaternion initialRotation = Quaternion.identity;
    /// <summary>
    /// Sensing mode: STANDARD or FILL. FILL corrects for missing depth values. 
    /// Almost always better to use FILL, since we need depth without holes for proper occlusion.
    /// </summary>
    private sl.SENSING_MODE sensingMode = sl.SENSING_MODE.FILL;
    /// <summary>
    /// Rotation offset used to retrieve the tracking with a rotational offset.
    /// </summary>
    private Quaternion rotationOffset;
    /// <summary>
    /// Position offset used to retrieve the tracking with a positional offset. 
    /// </summary>
    private Vector3 positionOffset;
    /// <summary>
    /// Enables pose smoothing during drift correction. Leave it to true.
    /// </summary>
    private bool enablePoseSmoothing = true;
    /// <summary>
    /// The engine FPS, updated every frame. 
    /// </summary>
    private float fps_engine = 90.0f;

    /// <summary>
    /// Recording state
    /// </summary>
    private bool isRecording = false;

    ///////////////////////////////////////
    /////////// Static States /////////////
    ///////////////////////////////////////

    /// <summary>
    /// Whether AR mode is activated. 
    /// </summary>
    private bool isStereoRig = false;
    /// <summary>
    /// Whether AR mode is activated. Assigned by ZEDManager.CheckStereoMode() in Awake().
    /// Will be true if the ZED_Rig_Stereo prefab (or a similarly-structured prefab) is used.
    /// </summary>
    public bool IsStereoRig
    {
        get { return isStereoRig; }
    }

    /// <summary>
    /// Checks if the ZED has finished initializing. 
    /// </summary>
    private bool zedReady = false;
    /// <summary>
    /// Checks if the ZED has finished initializing. 
    /// </summary>
    public bool IsZEDReady
    {
        get { return zedReady; }
    }

    /// <summary>
    /// Flag set to true if the camera was connected and the wasn't anymore. 
    /// Causes ZEDDisconnected() to be called each frame, which attemps to restart it. 
    /// </summary>
    private bool isDisconnected = false;

    /// <summary>
    /// Current state of tracking: On, Off, or Searching (lost tracking, trying to recover). Used by anti-drift.
    /// </summary>
    private sl.TRACKING_STATE zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;
    /// <summary>
    /// Current state of tracking: On, Off, or Searching (lost tracking, trying to recover). Used by anti-drift.
    /// </summary>
    public sl.TRACKING_STATE ZEDTrackingState
    {
        get { return zedtrackingState; }
    }
    /// <summary>
    /// First position registered after the tracking has started (whether via ZED or a VR HMD).
    /// </summary>
    public Vector3 OriginPosition { get; private set; }
    /// <summary>
    /// First rotation/orientation registered after the tracking has started (whether via ZED or a VR HMD).
    /// </summary>
    public Quaternion OriginRotation { get; private set; }



    ///////////////////////////////////////////////////
    [HideInInspector] public Quaternion gravityRotation = Quaternion.identity;
    [HideInInspector] public Vector3 ZEDSyncPosition;
    [HideInInspector] public Vector3 HMDSyncPosition;
    [HideInInspector] public Quaternion ZEDSyncRotation;
    [HideInInspector] public Quaternion HMDSyncRotation;


    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread threadGrab = null;
    /// <summary>
    /// State of the image acquisition thread. 
    /// </summary>
    private bool running = false;

    /// <summary>
    /// Initialization thread. 
    /// </summary>
    private Thread threadOpening = null;
    /// <summary>
    /// Result of the latest attempt to initialize the ZED. 
    /// </summary>
    private sl.ERROR_CODE lastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
    public sl.ERROR_CODE LastInitStatus { get { return lastInitStatus; } }
    /// <summary>
    /// State of the ZED initialization thread. 
    /// </summary>
    private bool openingLaunched;

    /// <summary>
    /// Wait Handle used to safely tell the init thread to shut down. 
    /// </summary>
    EventWaitHandle initQuittingHandle;
    /// <summary>
    /// When true, the init thread will close early instead of completing all its connection attempts. 
    /// Set to true when the application is closed before a camera finishes its initialization. 
    /// </summary>
    private bool forceCloseInit = false;

    /// <summary>
    /// Tracking initialization thread. Used as the tracking takes some time to start.
    /// </summary>
    private Thread trackerThread = null;


    ///////////////////////////////////////////
    //////  Camera and Player Transforms //////
    ///////////////////////////////////////////
    /// <summary>
    /// Transform of the left camera in the ZED rig. 
    /// </summary>
    private Transform camLeftTransform = null;

    /// <summary>
    /// Transform of the right camera in the ZED rig. Only exists in a stereo rig (like ZED_Rig_Stereo). 
    /// </summary>
	private Transform camRightTransform = null;

    /// <summary>
	/// Contains the position of the player's head, which is different from the ZED's position in AR mode.
	/// But its position relative to the ZED does not change during use (it's a rigid transform).
    /// In ZED_Rig_Mono, this will be the root ZED_Rig_Mono object. In ZED_Rig_Stereo, this is Camera_eyes. 
    /// </summary>
	private Transform zedRigRoot = null;

    /// <summary>
    /// Left camera in the ZED rig. Also the "main" camera if in ZED_Rig_Mono.
    /// </summary>
    private Camera cameraLeft;

    /// <summary>
    /// Right camera of the ZED rig. Only exists in a stereo rig (like ZED_Rig_Stereo).
    /// </summary>
    private Camera cameraRight;


    /// <summary>
    /// Gets the center transform, which is the transform moved by the tracker in AR mode. 
    /// This is the root object in ZED_Rig_Mono, and Camera_eyes in ZED_Rig_Stereo.
    /// </summary>
    public Transform GetZedRootTansform()
    {
        return zedRigRoot;
    }

    /// <summary>
    /// Gets the left camera transform in the ZED rig. It's best to use this one as it's available in all configurations.
    /// </summary>
    public Transform GetLeftCameraTransform()
    {
        return camLeftTransform;
    }

    /// <summary>
    /// Get the right camera transform in the ZED rig. Only available in the stereo rig (ZED_Rig_Stereo).
    /// </summary>
    public Transform GetRightCameraTransform()
    {
        return camRightTransform;
    }

    /// <summary>
    /// Gets the left camera in the ZED rig. It's best to use this one as it's available in all configurations.
    /// </summary>
    public Camera GetLeftCamera()
    {
        if (cameraLeft == null && camLeftTransform != null)
            cameraLeft = camLeftTransform.GetComponent<Camera>();
        return cameraLeft;
    }

    /// <summary>
    /// Get the right camera in the ZED rig. Only available in the stereo rig (ZED_Rig_Stereo).
    /// </summary>
    public Camera GetRightCamera()
    {
        if (cameraRight == null && camRightTransform != null)
            cameraRight = camRightTransform.GetComponent<Camera>();
        return cameraRight;
    }


    /// <summary>
    /// Save the foldout options as it was used last time
    /// </summary>
    [SerializeField]
    [HideInInspector]
    private bool advancedPanelOpen = false;
    [SerializeField]
    [HideInInspector]
    private bool spatialMappingFoldoutOpen = false;
    [SerializeField]
    [HideInInspector]
    private bool recordingFoldoutOpen = false;
    [SerializeField]
    [HideInInspector]
    private bool streamingOutFoldoutOpen = false;
    [SerializeField]
    [HideInInspector]
    private bool camControlFoldoutOpen = false;

    /////////////////////////////////////
    //////  Timestamps             //////
    /////////////////////////////////////

    /// <summary>
    /// Timestamp of the last ZED image grabbed. Textures from this grab may not have updated yet.
    /// </summary>
    private ulong cameraTimeStamp = 0;
    /// <summary>
    /// Timestamp of the last ZED image grabbed. Textures from this grab may not have updated yet.
    /// </summary>
	public ulong CameraTimeStamp
    {
        get { return cameraTimeStamp; }
    }

    /// <summary>
    /// Timestamp of the images used to create the current textures. 
    /// </summary>
	private ulong imageTimeStamp = 0;
    /// <summary>
    /// Timestamp of the images used to create the current textures. 
    /// </summary>
	public ulong ImageTimeStamp
    {
        get { return imageTimeStamp; }
    }

    /// <summary>
    /// Whether the grabbing thread should grab a new frame from the ZED SDK. 
    /// True unless the last grabbed frame hasn't been applied yet, or the ZED isn't initialized. 
    /// </summary>
	private bool requestNewFrame = false;
    /// <summary>
    /// Whether a new frame has been grabbed from the ZED SDK that needs to be updated. 
    /// </summary>
	private bool newFrameAvailable = false;



    /////////////////////////////////////
    //////  Layers for ZED         //////
    /////////////////////////////////////

    /// <summary>
    /// Layer assigned to the cameras and objects of a (normally hidden) AR camera rig created to handle
    /// pass-through AR. This allows the cameras to see nothing but two canvas objects with the final MR images. 
    /// </summary>
    [HideInInspector]
    public int arLayer
    {
        get
        {
            return arlayer;
        }
    }
    [SerializeField]
    [HideInInspector]
    private int arlayer = 30;

    /////////////////////////////////////
    //////  ZED specific events    //////
    /////////////////////////////////////

    /// <summary>
    /// Delegate for OnZEDReady. 
    /// </summary>
    public delegate void OnZEDManagerReady();
    /// <summary>
    /// Called when the ZED has finished initializing successfully. 
    /// Used by many scripts to run startup logic that requires that the ZED is active. 
    /// </summary>
    public event OnZEDManagerReady OnZEDReady;

    /// <summary>
    /// Delegate for OnZEDDisconnected. 
    /// </summary>
    public delegate void OnZEDManagerDisconnected();
    /// <summary>
    /// Event called when ZED was running but became disconnected. 
    /// </summary>
    public event OnZEDManagerDisconnected OnZEDDisconnected;

    /// <summary>
    /// Delegate for new Frame grabbed for external module update
    /// </summary>
    public delegate void OnGrabAction();
    /// <summary>
    /// Event called when ZED has grabbed a new frame.
    /// </summary>
    public event OnGrabAction OnGrab;


    #region CHECK_AR
    /// <summary>
    /// Checks if this GameObject is a stereo rig. Requires a child object called 'Camera_eyes' and 
    /// two cameras as children of that object, one with stereoTargetEye set to Left, the other two Right.
    /// Regardless, sets references to leftCamera and (if relevant) rightCamera.
    /// </summary>
    private void CheckStereoMode()
    {
        zedRigRoot = gameObject.transform; //The object moved by tracking. By default it's this Transform. May get changed. 

        bool devicePresent = UnityEngine.VR.VRDevice.isPresent; //May not need. 

        //Set first left eye
        Component[] cams = gameObject.GetComponentsInChildren<Camera>();
        Camera firstmonocam = null;
        foreach(Camera cam in cams)
        {
            switch(cam.stereoTargetEye)
            {
                case StereoTargetEyeMask.Left:
                    if(!cameraLeft)
                    {
                        cameraLeft = cam;
                        camLeftTransform = cam.transform;
                    }
                    break;
                case StereoTargetEyeMask.Right:
                    if(!cameraRight)
                    {
                        cameraRight = cam;
                        camRightTransform = cam.transform;
                    }
                    break;
                case StereoTargetEyeMask.None:
                    if (firstmonocam == null) firstmonocam = cam;
                    break;
                case StereoTargetEyeMask.Both:
                default:
                    break;
            }
        }
        if(cameraLeft == null && firstmonocam != null)
        {
            cameraLeft = firstmonocam;
            camLeftTransform = firstmonocam.transform;
        }

        if (camLeftTransform && camRightTransform && cameraLeft.stereoTargetEye == StereoTargetEyeMask.Left) //We found both a left- and right-eye camera. 
        {
            isStereoRig = UnityEngine.VR.VRDevice.isPresent;
            if (camLeftTransform.transform.parent != null)
            {
                zedRigRoot = camLeftTransform.parent; //Make the camera's parent object (Camera_eyes in the ZED_Rig_Stereo prefab) the new zedRigRoot to be tracked. 
            }
            
            if(UnityEngine.VR.VRDevice.isPresent)
            {
                isStereoRig = true;
            }
            else
            {
                isStereoRig = false;
                //If there's no VR headset, then cameras set to Left and Right won't display in Unity. Set them both to None. 
                if (cameraLeft) cameraLeft.stereoTargetEye = StereoTargetEyeMask.None;
                if (cameraRight) cameraRight.stereoTargetEye = StereoTargetEyeMask.None;
            }

        }
        else //Not all conditions for a stereo rig were met. 
        {
            isStereoRig = false;

            if (camLeftTransform)
            {
                Camera caml = camLeftTransform.gameObject.GetComponent<Camera>();
                cameraLeft = caml;

                if (camLeftTransform.transform.parent != null)
                    zedRigRoot = camLeftTransform.parent;
            }
            else
            {
                zedRigRoot = transform;
            }
        }
    }
    #endregion


    /// <summary>
    /// Sets the target GameObject and all its children to the specified layer.
    /// </summary>
    /// <param name="go">Target GameObject.</param>
    /// <param name="layerNumber">Layer that the GameObject and all children will be set to.</param>
    public static void SetLayerRecursively(GameObject go, int layerNumber)
    {
        if (go == null) return;
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }


    /// <summary>
    /// Stops the initialization and grabbing threads. 
    /// </summary>
    public void Destroy()
    {
        running = false;

        //In case the opening thread is still running.
        if (threadOpening != null)
        {
            initQuittingHandle.Reset();
            forceCloseInit = true;
            initQuittingHandle.Set();

            threadOpening.Join();
            threadOpening = null;
        }

        //Shut down the image grabbing thread.
        if (threadGrab != null)
        {
            threadGrab.Join();
            threadGrab = null;
        }

        if (IsMappingRunning)
            StopSpatialMapping();


        Thread.Sleep(10);
    }

    /// <summary>
    /// Called by Unity when the application is closed. 
    /// Also called by Reset() to properly start from a 'clean slate.'
    /// </summary>
    void OnApplicationQuit()
    {

        CloseManager();
        //sl.ZEDCamera.UnloadPlugin();

        //If this was the last camera to close, make sure all instances are closed. 
        bool notlast = false;
        foreach(ZEDManager manager in ZEDManagerInstance)
        {
            if(manager != null && manager.IsZEDReady == true)
            {
                notlast = true;
                break;
            }
        }
        if (notlast == false)
        {
            sl.ZEDCamera.UnloadPlugin();
        }

    }

    private void CloseManager()
    {
        if (spatialMapping != null)
            spatialMapping.Dispose();

        ClearRendering();

        zedReady = false;
        OnCamBrightnessChange -= SetCameraBrightness;
        Destroy(); //Close the grab and initialization threads. 

        if (zedCamera != null)
        {
            if (isRecording)
            {
                zedCamera.DisableRecording();
            }       
            zedCamera.Destroy();
            zedCamera = null;
        }

#if UNITY_EDITOR //Prevents building the app otherwise. 
        //Restore the AR layers that were hidden, if necessary. 

        if (!showarrig)
        {
            LayerMask layerNumberBinary = (1 << arLayer); //Convert layer index into binary number. 
            UnityEditor.Tools.visibleLayers |= (layerNumberBinary);

        }
#endif
        sl.ZEDCamera.UnloadInstance((int)cameraID);
    }

    private void ClearRendering()
    {
        if (camLeftTransform != null)
        {
            ZEDRenderingPlane leftRenderingPlane = camLeftTransform.GetComponent<ZEDRenderingPlane>();
            if (leftRenderingPlane)
            {
                leftRenderingPlane.Clear();
            }
        }

        if (IsStereoRig)
        {
            ZEDRenderingPlane rightRenderingPlane = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
            rightRenderingPlane.Clear();
        }


    }


    /// <summary>
    /// Sets up starting properties and starts the ZED initialization co-routine. 
    /// </summary>
    void Awake()
    {

        // If never initialized, init the array of instances linked to each ZEDManager that could be created.
        if (ZEDManagerInstance == null)
        {
            ZEDManagerInstance = new ZEDManager[(int)sl.Constant.MAX_CAMERA_PLUGIN];
            for (int i = 0; i < (int)sl.Constant.MAX_CAMERA_PLUGIN; i++)
                ZEDManagerInstance[i] = null;
        }


        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        zedReady = false;
        ZEDManagerInstance[(int)cameraID] = this;
        zedCamera = new sl.ZEDCamera();
        if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root); //If you want the ZED rig not to be destroyed when loading a scene. 

        //Set first few parameters for initialization. This will get passed to the ZED SDK when initialized. 
        initParameters = new sl.InitParameters();
        initParameters.resolution = resolution;
        initParameters.cameraFPS = FPS;
        initParameters.cameraID = (int)cameraID;
        initParameters.depthMode = depthMode;
        initParameters.depthStabilization = depthStabilizer;
        initParameters.cameraDisableIMU = cameraDisableIMU;
        initParameters.cameraImageFlip = cameraFlipMode;

        //Check if this rig is a stereo rig. Will set isStereoRig accordingly.
        CheckStereoMode();

        //Set initialization parameters that may change depending on what was done in CheckStereoMode(). 
        isZEDTracked = enableTracking;
        zedPosition = initialPosition;
        zedOrientation = initialRotation;

        lastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;

        bool res = zedCamera.CreateCamera((int)cameraID, wrapperVerbose);
        if (!res)
        {
            Debug.LogError("ZEDManager on " + gameObject.name + " couldn't connect to camera: " + cameraID +
               ". Check if another ZEDManager is already connected.");
            //this.gameObject.SetActive (false);
            //return;
        }
        initParameters.inputType = inputType;
        if (inputType == sl.INPUT_TYPE.INPUT_TYPE_USB)
        {
        }
        else if (inputType == sl.INPUT_TYPE.INPUT_TYPE_SVO)
        {
            initParameters.pathSVO = svoInputFileName;
            initParameters.svoRealTimeMode = svoRealTimeMode;
        }
        else if (inputType == sl.INPUT_TYPE.INPUT_TYPE_STREAM)
        {
            initParameters.ipStream = streamInputIP;
            initParameters.portStream = (ushort)streamInputPort;
        }

        versionZED = "[SDK]: " + sl.ZEDCamera.GetSDKVersion().ToString() + " [Plugin]: " + sl.ZEDCamera.PluginVersion.ToString();


        //Behavior specific to AR pass-through mode. 
        if (isStereoRig)
        {
            //Creates a hidden camera rig that handles final output to the headset. 
            GameObject o = CreateZEDRigDisplayer();
            if (!showarrig) o.hideFlags = HideFlags.HideInHierarchy;
            o.transform.parent = transform;

            //Force some initParameters that are required for a good AR experience.
            initParameters.enableRightSideMeasure = isStereoRig; //Creates a depth map for both eyes, not just one. 
            initParameters.depthMinimumDistance = 0.1f; //Allow depth calculation to very close objects. 

            //For the Game/output window, mirror the headset view using a custom script that avoids stretching. 
            CreateMirror();
        }

        //Starts a coroutine that initializes the ZED without freezing the game. 
        lastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
        openingLaunched = false;
        StartCoroutine(InitZED());


        OnCamBrightnessChange += SetCameraBrightness; //Subscribe event for adjusting brightness setting. 


        //Create Module Object
        //Create the spatial mapping module object (even if not used necessarly)
        spatialMapping = new ZEDSpatialMapping(transform, this);

    }


    void Start()
    {
        //adjust layers for multiple camera
        //setLayersForMultiCamera ();
    }

    #region INITIALIZATION
    //const int MAX_OPENING_TRIES = 10;
    private uint numberTriesOpening = 0;/// Counter of tries to open the ZED
    /// <summary>
    /// ZED opening function. Should be called in the initialization thread (threadOpening).
    /// </summary>
    private void OpenZEDInBackground()
    {
        openingLaunched = true;
        int timeout = 0;

        do
        {
            initQuittingHandle.WaitOne(0); //Makes sure we haven't been turned off early, which only happens in Destroy() from OnApplicationQuit(). 
            if (forceCloseInit) break;

            lastInitStatus = zedCamera.Init(ref initParameters);
            timeout++;
            numberTriesOpening++;
        } while (lastInitStatus != sl.ERROR_CODE.SUCCESS);
    }

    /// <summary>
    /// Initialization coroutine.
    /// </summary>

    private System.Collections.IEnumerator InitZED()
    {
        zedReady = false;
        if (!openingLaunched)
        {
            initQuittingHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
            threadOpening = new Thread(new ThreadStart(OpenZEDInBackground)); //Assign thread. 
            threadOpening.Start();
        }

        while (lastInitStatus != sl.ERROR_CODE.SUCCESS)
        {
            yield return new WaitForSeconds(0.3f);
        }



        //ZED has initialized successfully. 
        if (lastInitStatus == sl.ERROR_CODE.SUCCESS)
        {

            threadOpening.Join();
            //Initialize the tracking thread, AR initial transforms and SVO read/write as needed.
            ZEDReady();

            //If using tracking, wait until the tracking thread has been initialized. 
            while (enableTracking && !isTrackingEnable)
            {
                yield return new WaitForSeconds(0.5f);
            }

            //Tells all the listeners that the ZED is ready! :)
            if (OnZEDReady != null)
            {
                OnZEDReady();
            }

            //Make sure the screen is at 16:9 aspect ratio or close. Warn the user otherwise. 
            float ratio = (float)Screen.width / (float)Screen.height;
            float target = 16.0f / 9.0f;
            if (Mathf.Abs(ratio - target) > 0.01)
            {
                Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SCREEN_RESOLUTION));
            }

            //get informations from camera (S/N, firmware, model...)
            cameraModel = zedCamera.GetCameraModel().ToString();
            cameraFirmware = zedCamera.GetZEDFirmwareVersion().ToString();
            cameraSerialNumber = zedCamera.GetZEDSerialNumber().ToString();

            if (inputType == sl.INPUT_TYPE.INPUT_TYPE_SVO)
            {
                numberFrameMax = zedCamera.GetSVONumberOfFrames();
            }

            // If streaming has been switched on before play
            if (enableStreaming && !isStreamingEnable)
            {
                lock (zedCamera.grabLock)
                {
                    sl.ERROR_CODE err = zedCamera.EnableStreaming(streamingCodec, (uint)bitrate, (ushort)streamingPort, gopSize, adaptativeBitrate);
                    if (err == sl.ERROR_CODE.SUCCESS)
                    {
                        isStreamingEnable = true;
                    }
                    else
                    {
                        enableStreaming = false;
                        isStreamingEnable = false;
                    }
                }
            }

            //If not already launched, launch the image grabbing thread.
            if (!running)
            {

                running = true;
                requestNewFrame = true;

                threadGrab = new Thread(new ThreadStart(ThreadedZEDGrab));
                threadGrab.Start();

            }

            zedReady = true;
            isDisconnected = false; //In case we just regained connection.

            setRenderingSettings(); //Find the ZEDRenderingPlanes in the rig and configure them. 
            AdjustZEDRigCameraPosition(); //If in AR mode, move cameras to proper offset relative to zedRigRoot.
        }

    }


    /// <summary>
	/// Adjust camera(s) relative to zedRigRoot transform, which is what is moved each frame. Called at start of tracking. 
    /// <para>In AR mode, offset is each camera's position relative to center of the user's head. Otherwise, cameras are just spaced
    /// by the camera's baseline/IPD, or no offset is applied if there's just one camera. </para>
    /// </summary>
    void AdjustZEDRigCameraPosition()
    {
        Vector3 rightCameraOffset = new Vector3(zedCamera.Baseline, 0.0f, 0.0f);
        if (isStereoRig && VRDevice.isPresent) //Using AR pass-through mode. 
        {
            //zedRigRoot transform (origin of the global camera) is placed on the HMD headset. Therefore, we move the 
            //camera in front of it by offsetHmdZEDPosition to compensate for the ZED's position on the headset. 
            //If values are wrong, tweak calibration file created in ZEDMixedRealityPlugin. 
            camLeftTransform.localPosition = arRig.HmdToZEDCalibration.translation;
            camLeftTransform.localRotation = arRig.HmdToZEDCalibration.rotation;
            if (camRightTransform) camRightTransform.localPosition = camLeftTransform.localPosition + rightCameraOffset; //Space the eyes apart. 
            if (camRightTransform) camRightTransform.localRotation = camLeftTransform.localRotation;
        }
        else if (isStereoRig && !VRDevice.isPresent) //Using stereo rig, but no VR headset. 
        {
            //When no VR HMD is available, simply put the origin at the left camera.
            camLeftTransform.localPosition = Vector3.zero;
            camLeftTransform.localRotation = Quaternion.identity;
            if (camRightTransform) camRightTransform.localPosition = rightCameraOffset; //Space the eyes apart. 
            if (camRightTransform) camRightTransform.localRotation = Quaternion.identity;
        }
        else //Using mono rig (ZED_Rig_Mono). No offset needed. 
        {
            if (camLeftTransform)
            {
                camLeftTransform.localPosition = Vector3.zero;
                camLeftTransform.localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// Find the ZEDRenderingPlane components in the ZED rig and set their rendering settings 
    /// (rendering path, shader values, etc.) for left and right cameras. Also activate/deactivate depth occlusions.
    /// </summary>
    void setRenderingSettings()
    {
        ZEDRenderingPlane leftRenderingPlane = null;
        if (GetLeftCameraTransform() != null)
        {
            leftRenderingPlane = GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
            leftRenderingPlane.SetPostProcess(postProcessing);
            GetLeftCameraTransform().GetComponent<Camera>().renderingPath = RenderingPath.UsePlayerSettings;
            SetCameraBrightness(m_cameraBrightness);
        }

        ZEDRenderingPlane rightRenderingPlane = null;
        if (IsStereoRig)
        {
            rightRenderingPlane = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
            rightRenderingPlane.SetPostProcess(postProcessing);
        }

        if (camLeftTransform != null)
        {
            ZEDRenderingMode renderingPath = (ZEDRenderingMode)camLeftTransform.GetComponent<Camera>().actualRenderingPath;

            //Make sure we're in either forward or deferred rendering. Default to forward otherwise. 
            if (renderingPath != ZEDRenderingMode.FORWARD && renderingPath != ZEDRenderingMode.DEFERRED)
            {
                Debug.LogError("[ZED Plugin] Only Forward and Deferred Shading rendering path are supported");
                GetLeftCameraTransform().GetComponent<Camera>().renderingPath = RenderingPath.Forward;
                if (IsStereoRig)
                    GetRightCameraTransform().GetComponent<Camera>().renderingPath = RenderingPath.Forward;
            }

            //Set depth occlusion. 
            if (renderingPath == ZEDRenderingMode.FORWARD)
            {
                if (leftRenderingPlane) leftRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");
                if (rightRenderingPlane)
                    rightRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");

            }
            else if (renderingPath == ZEDRenderingMode.DEFERRED)
            {
                if (leftRenderingPlane) leftRenderingPlane.ManageKeywordDeferredMat(!depthOcclusion, "NO_DEPTH_OCC");
                if (rightRenderingPlane)
                    rightRenderingPlane.ManageKeywordDeferredMat(!depthOcclusion, "NO_DEPTH_OCC");
            }
        }

    }
    #endregion

    #region IMAGE_ACQUIZ
    /// <summary>
    /// Continuously grabs images from the ZED. Runs on its own thread. 
    /// </summary>
    private void ThreadedZEDGrab()
    {
        runtimeParameters = new sl.RuntimeParameters();
        runtimeParameters.sensingMode = sensingMode;
        runtimeParameters.enableDepth = true;
        //Don't change this reference frame. If we need normals in the world frame, better to do the conversion ourselves.
        runtimeParameters.measure3DReferenceFrame = sl.REFERENCE_FRAME.CAMERA;

        while (running)
        {
            if (zedCamera == null)
                return;

            AcquireImages();
        }
    }


    /// <summary>
    /// Grabs images from the ZED SDK and updates tracking, FPS and timestamp values.
    /// Called from ThreadedZEDGrab() in a separate thread. 
    /// </summary>
    private void AcquireImages()
    {
        if (requestNewFrame && zedReady)
        {
            sl.ERROR_CODE e = sl.ERROR_CODE.FAILURE;
            if (inputType == sl.INPUT_TYPE.INPUT_TYPE_SVO)
            {
                //handle pause
                if (NeedNewFrameGrab && pauseSVOReading)
                {
                    e = zedCamera.Grab(ref runtimeParameters);
                    NeedNewFrameGrab = false;
                }
                else if (!pauseSVOReading)
                    e = zedCamera.Grab(ref runtimeParameters);

                currentFrame = zedCamera.GetSVOPosition();
            }
            else if (!pauseLiveReading)
            {
                e = zedCamera.Grab(ref runtimeParameters);
            }


            lock (zedCamera.grabLock)
            {
                if (e == sl.ERROR_CODE.CAMERA_NOT_DETECTED)
                {
                    Debug.Log("Camera not detected or disconnected.");
                    isDisconnected = true;
                    Thread.Sleep(10);
                    requestNewFrame = false;
                }
                else if (e == sl.ERROR_CODE.SUCCESS)
                {
                    #if UNITY_EDITOR
                    float camera_fps = zedCamera.GetCameraFPS();
                    cameraFPS = camera_fps.ToString() + " FPS";
                    #endif
                    //Get position of camera
                    if (isTrackingEnable)
                    {
                        zedtrackingState = zedCamera.GetPosition(ref zedOrientation, ref zedPosition, sl.TRACKING_FRAME.LEFT_EYE);
                    }
                    else
                    {
                        zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;
                    }

                    if (needRecordFrame)
                        zedCamera.Record();

                    // Indicate that a new frame is available and pause the thread until a new request is called
                    newFrameAvailable = true;
                    requestNewFrame = false;
                }
                else
                    Thread.Sleep(1);
            }
        }
        else
        {
            //To avoid "overheating."
            Thread.Sleep(1);
        }
    }
    #endregion

    /// <summary>
    /// Initialize the SVO, and launch the thread to initialize tracking. Called once the ZED
    /// is initialized successfully. 
    /// </summary>
    private void ZEDReady()
    {
        FPS = (int)zedCamera.GetRequestedCameraFPS();
        if (enableTracking)
        {
            trackerThread = new Thread(EnableTrackingThreaded);
            trackerThread.Start();
        }
        else if (estimateInitialPosition)
        {
            sl.ERROR_CODE err = zedCamera.EstimateInitialPosition(ref initialRotation, ref initialPosition);
            if (zedCamera.GetCameraModel() == sl.MODEL.ZED_M)
                zedCamera.GetInternalIMUOrientation(ref initialRotation, sl.TIME_REFERENCE.IMAGE);

            if (err != sl.ERROR_CODE.SUCCESS)
                Debug.LogWarning("Failed to estimate initial camera position");
        }

        if (enableTracking)
            trackerThread.Join();


        if (isStereoRig && VRDevice.isPresent)
        {
            ZEDMixedRealityPlugin.Pose pose = arRig.InitTrackingAR();
            OriginPosition = pose.translation;
            OriginRotation = pose.rotation;

            if (!zedCamera.IsHmdCompatible && zedCamera.IsCameraReady)
                Debug.LogWarning("WARNING: AR Passtrough with a ZED is not recommended. Consider using ZED Mini, designed for this purpose.");
        }
        else
        {
            OriginPosition = initialPosition;
            OriginRotation = initialRotation;
        }

        //Set the original transform for the Rig
        zedRigRoot.localPosition = OriginPosition;
        zedRigRoot.localRotation = OriginRotation;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif
    }

    /// <summary>
    /// Initializes the ZED's inside-out tracking. Started as a separate thread in OnZEDReady. 
    /// </summary>
    void EnableTrackingThreaded()
    {
        lock (zedCamera.grabLock)
        {
            //If using spatial memory and given a path to a .area file, make sure that path is valid.
            if (enableSpatialMemory && pathSpatialMemory != "" && !System.IO.File.Exists(pathSpatialMemory))
            {
                Debug.Log("Specified path to .area file '" + pathSpatialMemory + "' does not exist. Ignoring.");
                pathSpatialMemory = "";
            }


            //Now enable the tracking with the proper parameters.
            if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory,
                enablePoseSmoothing, estimateInitialPosition, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
            {
                throw new Exception(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
            }
            else
            {
                isTrackingEnable = true;
            }
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// Handler for playmodeStateChanged. 
    /// </summary>
    void HandleOnPlayModeChanged()
    {

        if (zedCamera == null) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif
    }
#endif



    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////// ENGINE UPDATE REGION   /////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region ENGINE_UPDATE
    /// <summary>
    /// If a new frame is available, this function retrieves the images and updates the Unity textures. Called in Update().
    /// </summary>
    public void UpdateImages()
    {
        if (zedCamera == null)
            return;

        if (newFrameAvailable) //ThreadedZEDGrab()/AcquireImages() grabbed images we haven't updated yet. 
        {
            lock (zedCamera.grabLock)
            {
                zedCamera.RetrieveTextures(); //Tell the wrapper to compute the textures. 
                zedCamera.UpdateTextures(); //Tell the wrapper to update the textures. 
                imageTimeStamp = zedCamera.GetImagesTimeStamp();

            }

            //For external module ... Trigger the capture done event.
            if (OnGrab != null)
                OnGrab();

            //SVO and loop back ? --> reset position if needed
            if (zedCamera.GetInputType() == sl.INPUT_TYPE.INPUT_TYPE_SVO && svoLoopBack)
            {
                int maxSVOFrame = zedCamera.GetSVONumberOfFrames();
                if (zedCamera.GetSVOPosition() >= maxSVOFrame - (svoRealTimeMode ? 2 : 1))
                {
                    zedCamera.SetSVOPosition(0);
                    if (enableTracking)
                    {
                        if (!(enableTracking = (zedCamera.ResetTracking(initialRotation, initialPosition) == sl.ERROR_CODE.SUCCESS)))
                        {

                            Debug.LogError("ZED Tracking disabled: Not available during SVO playback when Loop is enabled.");
                        }
                    }
                    zedRigRoot.localPosition = initialPosition;
                    zedRigRoot.localRotation = initialRotation;
                }
            }
            requestNewFrame = true; //Lets ThreadedZEDGrab/AcquireImages() start grabbing again. 
            newFrameAvailable = false;
        }
    }


    /// <summary>
    /// Gets the tracking position from the ZED and updates zedRigRoot's position. Also updates the AR tracking if enabled. 
	/// Only called in Live (not SVO playback) mode. Called in Update().
    /// </summary>
    private void UpdateTracking()
    {
        if (!zedReady)
            return;

        if (isZEDTracked) //ZED inside-out tracking is enabled and initialized. 
        {
            Quaternion r;
            Vector3 v;

            isCameraTracked = true;

            if (VRDevice.isPresent && isStereoRig) //AR pass-through mode. 
            {
                if (calibrationHasChanged) //If the HMD offset calibration file changed during runtime. 
                {
                    AdjustZEDRigCameraPosition(); //Re-apply the ZED's offset from the VR headset. 
                    calibrationHasChanged = false;
                }

                arRig.ExtractLatencyPose(imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation. 
                arRig.AdjustTrackingAR(zedPosition, zedOrientation, out r, out v);
                zedRigRoot.localRotation = r;
                zedRigRoot.localPosition = v;

                ZEDSyncPosition = v;
                ZEDSyncRotation = r;
                HMDSyncPosition = arRig.LatencyPose().translation;
                HMDSyncRotation = arRig.LatencyPose().rotation;
            }
            else //Not AR pass-through mode. 
            {
                zedRigRoot.localRotation = zedOrientation;
                if (!ZEDSupportFunctions.IsVector3NaN(zedPosition))
                    zedRigRoot.localPosition = zedPosition;
            }
        }
        else if (VRDevice.isPresent && isStereoRig) //ZED tracking is off but HMD tracking is on. Fall back to that. 
        {
            isCameraTracked = true;
            arRig.ExtractLatencyPose(imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation. 
            zedRigRoot.localRotation = arRig.LatencyPose().rotation;
            zedRigRoot.localPosition = arRig.LatencyPose().translation;
        }
        else //The ZED is not tracked by itself or an HMD. 
            isCameraTracked = false;
    }

    /// <summary>
    /// Stores the HMD's current pose. Used in AR mode for latency compensation. 
    /// Pose will be applied to final canvases when a new image's timestamp matches 
    /// the time when this is called. 
    /// </summary>
    void UpdateHmdPose()
    {
        if (IsStereoRig && VRDevice.isPresent)
            arRig.CollectPose(); //Save headset pose with current timestamp. 
    }

    /// <summary>
    /// Updates images, collects HMD poses for latency correction, and applies tracking. 
    /// Called by Unity each frame. 
    /// </summary>
	void Update()
    {

        //Check if ZED is disconnected; invoke event and call function if so. 
        if (isDisconnected)
        {
            if (OnZEDDisconnected != null)
                OnZEDDisconnected(); //Invoke event. Used for GUI message and pausing ZEDRenderingPlanes. 

            ZEDDisconnected(); //Tries to reset the camera. 
            return;
        }


        // Then update all modules
        UpdateImages(); //Image is updated first so we have its timestamp for latency compensation. 
        UpdateHmdPose(); //Store the HMD's pose at the current timestamp. 
        UpdateTracking(); //Apply position/rotation changes to zedRigRoot. 
        UpdateMapping(); //Update mapping if activated


        /// If in Unity Editor, update the ZEDManager status list
#if UNITY_EDITOR
        //Update strings used for 	di	splaying stats in the Inspector. 
        if (zedCamera != null)
        {
            float frame_drop_count = zedCamera.GetFrameDroppedPercent();
            float CurrentTickFPS = 1.0f / Time.deltaTime;
            fps_engine = (fps_engine + CurrentTickFPS) / 2.0f;
            engineFPS = fps_engine.ToString("F0") + " FPS";
            if (frame_drop_count > 30 && fps_engine < 45)
                engineFPS += "WARNING: Low engine framerate detected";

            if (isZEDTracked)
                trackingState = ZEDTrackingState.ToString();
            else if (VRDevice.isPresent && isStereoRig)
                trackingState = "HMD Tracking";
            else
                trackingState = "Camera Not Tracked";
        }
#endif

    }

    public void LateUpdate()
    {
        if (IsStereoRig)
        {
            arRig.LateUpdateHmdRendering(); //Update textures on final AR rig for output to the headset. 
        }
    }
    #endregion

    /// <summary>
    /// Event called when camera is disconnected
    /// </summary>
    void ZEDDisconnected()
    {
        cameraFPS = "Disconnected";
        isDisconnected = true;

        if (zedReady)
        {
            Reset(); //Cache tracking, turn it off and turn it back on again. 
        }
    }

    private void OnDestroy()
    {
        //OnApplicationQuit();
        CloseManager();
    }



    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////// SPATIAL MAPPING REGION   /////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region MAPPING_MODULE
    /// <summary>
    /// Tells ZEDSpatialMapping to begin a new scan. This clears the previous scan from the scene if there is one. 
    /// </summary>
    public void StartSpatialMapping()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        spatialMapping.StartStatialMapping(mappingResolutionPreset, mappingRangePreset, isMappingTextured);
    }

    /// <summary>
    /// Ends the current spatial mapping. Once called, the current mesh will be filtered, textured (if enabled) and saved (if enabled), 
    /// and a mesh collider will be added. 
    /// </summary>
    public void StopSpatialMapping()
    {
        if (spatialMapping != null)
        {
            spatialMapping.StopStatialMapping();
            if (saveMeshWhenOver)
                SaveMesh(meshPath);
        }
    }

    /// <summary>
    /// Updates the filtering parameters and call the ZEDSpatialMapping instance's Update() function. 
    /// </summary>
    private void UpdateMapping()
    {
        if (spatialMapping != null)
        {
            if (IsMappingUpdateThreadRunning)
            {
                spatialMapping.filterParameters = meshFilterParameters;
                spatialMapping.Update();
            }
        }
    }

    /// <summary>
    /// Toggles whether to display the mesh or not. 
    /// </summary>
    /// <param name="state"><c>True</c> to make the mesh visible, <c>false</c> to make it invisible. </param>
    public void SwitchDisplayMeshState(bool state)
    {
        if (spatialMapping != null)
            spatialMapping.SwitchDisplayMeshState(state);
    }

    public void ClearAllMeshes()
    {
        if (spatialMapping != null)
            spatialMapping.ClearAllMeshes();
    }

    /// <summary>
    /// Pauses the current scan. 
    /// </summary>
    /// <param name="state"><c>True</c> to pause the scanning, <c>false</c> to unpause it.</param>
    public void SwitchPauseState(bool state)
    {
        if (spatialMapping != null)
            spatialMapping.SwitchPauseState(state);
    }

    /// <summary>
    /// Saves the mesh into a 3D model (.obj, .ply or .bin) file. Also saves an .area file for spatial memory for better tracking. 
    /// Calling this will end the spatial mapping if it's running. Note it can take a significant amount of time to finish. 
    /// </summary>
    /// <param name="meshPath">Path where the mesh and .area files will be saved.</param>
    public bool SaveMesh(string meshPath = "ZEDMeshObj.obj")
    {
        return spatialMapping.SaveMesh(meshPath);
    }

    /// <summary>
    /// Loads a mesh and spatial memory data from a file.
    /// If scanning is running, it will be stopped. Existing scans in the scene will be cleared. 
    /// </summary>
    /// <param name="meshPath">Path to the 3D mesh file (.obj, .ply or .bin) to load.</param>
    /// <returns><c>True</c> if successfully loaded, <c>false</c> otherwise.</returns>
    public bool LoadMesh(string meshPath = "ZEDMeshObj.obj")
    {
        //Cache the save setting and set to false, to avoid overwriting the mesh file during the load. 
        bool oldSaveWhenOver = saveMeshWhenOver;
        saveMeshWhenOver = false;

        gravityRotation = Quaternion.identity;

        spatialMapping.SetMeshRenderer();
        bool loadresult = spatialMapping.LoadMesh(meshPath);

        saveMeshWhenOver = oldSaveWhenOver; //Restoring old setting.
        return loadresult;
    }
    #endregion



    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////// AR REGION //////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region AR_CAMERAS
    /// <summary>
    /// Stereo rig that adjusts images from ZED_Rig_Stereo to look correct in the HMD. 
    /// <para>Hidden by default as it rarely needs to be changed.</para>
    /// </summary>
    [HideInInspector]
    public GameObject zedRigDisplayer;
    private ZEDMixedRealityPlugin arRig;
    /// <summary>
	/// Create a GameObject to display the ZED in an headset (ZED-M Only).
    /// </summary>
    /// <returns></returns>
    private GameObject CreateZEDRigDisplayer()
    {
        //Make sure we don't already have one, such as if the camera disconnected and reconnected. 
        if (zedRigDisplayer != null) return zedRigDisplayer;

        zedRigDisplayer = new GameObject("ZEDRigDisplayer");
        arRig = zedRigDisplayer.AddComponent<ZEDMixedRealityPlugin>();

        /*Screens left and right */
        GameObject leftScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftScreen.name = "Quad - Left";
        MeshRenderer meshLeftScreen = leftScreen.GetComponent<MeshRenderer>();
        meshLeftScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshLeftScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshLeftScreen.receiveShadows = false;
        meshLeftScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshLeftScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshLeftScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        leftScreen.layer = arLayer;
        GameObject.Destroy(leftScreen.GetComponent<MeshCollider>());

        GameObject rightScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightScreen.name = "Quad - Right";
        MeshRenderer meshRightScreen = rightScreen.GetComponent<MeshRenderer>();
        meshRightScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRightScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRightScreen.receiveShadows = false;
        meshRightScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRightScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObject.Destroy(rightScreen.GetComponent<MeshCollider>());
        meshRightScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        rightScreen.layer = arLayer;

        /*Camera left and right*/
        GameObject camLeft = new GameObject("cameraLeft");
        camLeft.transform.SetParent(zedRigDisplayer.transform);
        Camera camL = camLeft.AddComponent<Camera>();
        camL.stereoTargetEye = StereoTargetEyeMask.Both; //Temporary setting to fix loading screen issue.
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 1 << arLayer;
        camL.allowHDR = false;
        camL.allowMSAA = false;
        camL.depth = camLeftTransform.GetComponent<Camera>().depth;

        GameObject camRight = new GameObject("cameraRight");
        camRight.transform.SetParent(zedRigDisplayer.transform);
        Camera camR = camRight.AddComponent<Camera>();
        camR.renderingPath = RenderingPath.Forward;//Minimal overhead
        camR.clearFlags = CameraClearFlags.Color;
        camR.backgroundColor = Color.black;
        camR.stereoTargetEye = StereoTargetEyeMask.Both; //Temporary setting to fix loading screen issue.
        camR.cullingMask = 1 << arLayer;
        camR.allowHDR = false;
        camR.allowMSAA = false;
        camR.depth = camRightTransform.GetComponent<Camera>().depth;

        HideFromWrongCameras.RegisterZEDCam(camL);
        HideFromWrongCameras lhider = leftScreen.AddComponent<HideFromWrongCameras>();
        lhider.SetRenderCamera(camL);
        lhider.showInNonZEDCameras = false;

        HideFromWrongCameras.RegisterZEDCam(camR);
        HideFromWrongCameras rhider = rightScreen.AddComponent<HideFromWrongCameras>();
        rhider.SetRenderCamera(camR);
        rhider.showInNonZEDCameras = false;

        SetLayerRecursively(camRight, arLayer);
        SetLayerRecursively(camLeft, arLayer);

        //Hide camera in editor.
#if UNITY_EDITOR
        if (!showarrig)
        {
            LayerMask layerNumberBinary = (1 << arLayer); //Convert layer index into binary number. 
            LayerMask flippedVisibleLayers = ~UnityEditor.Tools.visibleLayers;
            UnityEditor.Tools.visibleLayers = ~(flippedVisibleLayers | layerNumberBinary);
        }
#endif
        leftScreen.transform.SetParent(zedRigDisplayer.transform);
        rightScreen.transform.SetParent(zedRigDisplayer.transform);


        arRig.finalCameraLeft = camLeft;
        arRig.finalCameraRight = camRight;
        arRig.ZEDEyeLeft = camLeftTransform.gameObject;
        arRig.ZEDEyeRight = camRightTransform.gameObject;
        arRig.quadLeft = leftScreen.transform;
        arRig.quadRight = rightScreen.transform;


        ZEDMixedRealityPlugin.OnHmdCalibChanged += CalibrationHasChanged;
        if (UnityEngine.VR.VRDevice.isPresent)
            HMDDevice = UnityEngine.VR.VRDevice.model;


        return zedRigDisplayer;
    }

    #endregion

    #region MIRROR
    private ZEDMirror mirror = null;
    private GameObject mirrorContainer = null;
    void CreateMirror()
    {
        GameObject camLeft;
        Camera camL;
        if (mirrorContainer == null)
        {
            mirrorContainer = new GameObject("Mirror");
            mirrorContainer.hideFlags = HideFlags.HideInHierarchy;

            camLeft = new GameObject("MirrorCamera");
            camLeft.hideFlags = HideFlags.HideInHierarchy;
            mirror = camLeft.AddComponent<ZEDMirror>();
            mirror.manager = this;
            camL = camLeft.AddComponent<Camera>();
        }
        else
        {
            camLeft = mirror.gameObject;
            camL = camLeft.GetComponent<Camera>();
        }

        camLeft.transform.parent = mirrorContainer.transform;
        camL.stereoTargetEye = StereoTargetEyeMask.None;
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 0; //It should see nothing. It gets its final image entirely from a Graphics.Blit call in ZEDMirror. 
        camL.allowHDR = false;
        camL.allowMSAA = false;
        camL.useOcclusionCulling = false;

        camL.depth = cameraLeft.GetComponent<Camera>().depth; //Make sure it renders after the left cam so we can copy texture from latest frame. 
    }
    #endregion

    /// <summary>
    /// Closes out the current stream, then starts it up again while maintaining tracking data. 
    /// Used when the zed becomes unplugged, or you want to change a setting at runtime that 
    /// requires re-initializing the camera. 
    /// </summary>
    public void Reset()
    {
        //Save tracking
        if (enableTracking && isTrackingEnable)
        {
            zedCamera.GetPosition(ref zedOrientation, ref zedPosition);
        }

        CloseManager();

        openingLaunched = false;
        running = false;
        numberTriesOpening = 0;
        forceCloseInit = false;

        Awake();

    }


    #region EventHandler
    /// <summary>
    /// Changes the real-world brightness by setting the brightness value in the shaders.
    /// </summary>
    /// <param name="newVal">New value to be applied.</param>
    private void SetCameraBrightness(int newVal)
    {
      
        ZEDRenderingPlane leftRenderingPlane = GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
        if (leftRenderingPlane)
        {
            Material rendmat;
            if (leftRenderingPlane.ActualRenderingPath == RenderingPath.Forward) rendmat = leftRenderingPlane.canvas.GetComponent<Renderer>().material;
            else if (leftRenderingPlane.ActualRenderingPath == RenderingPath.DeferredShading) rendmat = leftRenderingPlane.deferredMat;
            else
            {
                Debug.LogError("Can't set camera brightness for Rendering Path " + leftRenderingPlane.ActualRenderingPath +
                    ": only Forward and DeferredShading are supported.");
                return;
            }
            rendmat.SetFloat("_ZEDFactorAffectReal", newVal / 100.0f); 
        }

        if (IsStereoRig)
        {
            ZEDRenderingPlane rightRenderingPlane = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
            Material rendmat;
            if (rightRenderingPlane.ActualRenderingPath == RenderingPath.Forward) rendmat = rightRenderingPlane.canvas.GetComponent<Renderer>().material;
            else if (rightRenderingPlane.ActualRenderingPath == RenderingPath.DeferredShading) rendmat = rightRenderingPlane.deferredMat;
            else
            {
                return; //Don't log the error twice as the left and right rendering paths are almost certainly the same, and the left cam already logged it. 
            }
            rendmat.SetFloat("_ZEDFactorAffectReal", newVal / 100.0f);
        }
    }


    /// <summary>
    /// Flag set to true when the HMD-to-ZED calibration file has changed during runtime. 
    /// Causes values from the new file to be applied during Update(). 
    /// </summary>
    private bool calibrationHasChanged = false;

    /// <summary>
    /// Sets the calibrationHasChanged flag to true, which causes the next Update() to 
    /// re-apply the HMD-to-ZED offsets. 
    /// </summary>
	private void CalibrationHasChanged()
    {
        calibrationHasChanged = true;
    }
    #endregion




#if UNITY_EDITOR
    /// <summary>
    /// Handles changes to tracking or graphics settings changed from the Inspector. 
    /// </summary>
    void OnValidate()
    {
        if (zedCamera != null)
        {
            // If tracking has been switched on
            if (!isTrackingEnable && enableTracking) 
            {
                //Enables tracking and initializes the first position of the camera.
                if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory, enablePoseSmoothing, estimateInitialPosition, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
                {
                    isZEDTracked = false;
                    throw new Exception(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
                }
                else
                {
                    isZEDTracked = true;
                    isTrackingEnable = true;
                }
            }

            // If tracking has been switched off
            if (isTrackingEnable && !enableTracking)  
            {
                isZEDTracked = false;
                lock (zedCamera.grabLock)
                {
                    zedCamera.DisableTracking();
                }
                isTrackingEnable = false;
            }

            // If streaming has been switched on
            if (enableStreaming && !isStreamingEnable)
            {
                lock (zedCamera.grabLock)
                {
                    sl.ERROR_CODE err = zedCamera.EnableStreaming(streamingCodec, (uint)bitrate, (ushort)streamingPort, gopSize, adaptativeBitrate);
                    if (err == sl.ERROR_CODE.SUCCESS)
                    {
                        isStreamingEnable = true;
                    }
                    else
                    {
                        enableStreaming = false;
                        isStreamingEnable = false;
                    }
                }
            }

            // If streaming has been switched off
            if (!enableStreaming && isStreamingEnable)
            {
                lock (zedCamera.grabLock)
                {
                    zedCamera.DisableStreaming();
                    isStreamingEnable = false;
                }
            }

            //Reapplies graphics settings based on current values. 
            setRenderingSettings(); 
        }

    }
#endif


}

