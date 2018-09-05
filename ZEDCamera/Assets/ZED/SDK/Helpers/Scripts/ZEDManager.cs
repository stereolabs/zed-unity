using UnityEngine;
using System;
using System.Threading;
using UnityEngine.VR;

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
    /// For advanced debugging. Default false. Set true for the Unity wrapper to log all SDK calls to a new file
    /// at C:/ProgramData/stereolabs/SL_Unity_wrapper.txt. This helps find issues that may occur within
    /// the protected .dll, but can decrease performance. 
    /// </summary>
    private bool wrapperVerbose = false;

    ////////////////////////////
    //////// Public ///////////
    ////////////////////////////
    /// <summary>
    /// Current instance of the ZED Camera, which handles calls to the Unity wrapper .dll. 
    /// </summary>
    public sl.ZEDCamera zedCamera;

    /// <summary>
    /// Resolution setting for all images retrieved from the camera. Higher resolution means lower framerate.
    /// HD720 is strongly recommended for pass-through AR.
    /// </summary>
    [Header("Camera")]
    [Tooltip("Resolution setting for all images retrieved from the camera. Higher resolution means lower framerate. " + 
        "HD720 is strongly recommended for pass-through AR.")]
    public sl.RESOLUTION resolution = sl.RESOLUTION.HD720;
    /// <summary>
    /// Targeted FPS, based on the resolution. VGA = 100, HD720 = 60, HD1080 = 30, HD2K = 15. 
    /// </summary>
    private int FPS = 60;
    /// <summary>
    /// The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance. 
    /// Note there's a significant jump in performance cost between QUALITY and ULTRA modes.
    /// </summary>
    [Tooltip("The accuracy of depth calculations. Higher settings mean more accurate occlusion and lighting but costs performance.")]
    public sl.DEPTH_MODE depthMode = sl.DEPTH_MODE.PERFORMANCE;

    /// <summary>
    /// If enabled, the ZED will move/rotate itself using its own inside-out tracking.
    /// If false, the camera tracking will move with the VR HMD if connected and available.
    /// <para>Normally, ZEDManager's GameObject will move according to the tracking. But if in AR pass-through mode, 
    /// then the Camera_eyes object in ZED_Rig_Stereo will move while this object stays still. </para>
    /// </summary>
    [Header("Motion Tracking")]
    [Tooltip("If enabled, the ZED will move/rotate itself using its own inside-out tracking. " +
        "If false, the camera tracking will move with the VR HMD if connected and available.")]
    public bool enableTracking = true;
    /// <summary>
    /// Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment, 
	/// but may cause visible jumps when it happens.
    /// </summary>
	[Tooltip("Enables the spatial memory. Will detect and correct tracking drift by remembering features and anchors in the environment, " 
        + "but may cause visible jumps when it happens")]
    public bool enableSpatialMemory = true;
    /// <summary>
    /// If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded. 
    /// .area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan. 
    /// </summary>
    [Tooltip("If using Spatial Memory, you can specify a path to an existing .area file to start with some memory already loaded. " +
        ".area files are created by scanning a scene with ZEDSpatialMappingManager and saving the scan.")]
    public string pathSpatialMemory = "ZED_spatial_memory";

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
    [Header("Rendering")]
    [Tooltip("When enabled, the real world can occlude (cover up) virtual objects that are behind it. " +
        "Otherwise, virtual objects will appear in front.")]
    public bool depthOcclusion = true;

    /// <summary>
    /// Enables post-processing effects on virtual objects that blends them in with the real world.
    /// </summary>
    [LabelOverride("AR Post-Processing")]
    [Tooltip("Enables post-processing effects on virtual objects that blends them in with the real world.")]
    public bool postProcessing = true;

    /// <summary>
    /// Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way. 
    /// This is a rendering setting that doesn't affect the raw input from the camera.
    /// </summary>
    [Range(0, 1)]
    [Tooltip("Brightness of the final real-world image. Default is 1. Lower to darken the environment in a realistic-looking way. "  +
        "This is a rendering setting that doesn't affect the raw input from the camera.")]
    public float m_cameraBrightness = 1.0f;
    /// <summary>
    /// Public accessor for m_cameraBrightness, which is the post-capture brightness setting of the real-world image. 
    /// </summary>
	public float CameraBrightness
    {
		get {return m_cameraBrightness;}
        set {
			if (m_cameraBrightness == value) return;
			m_cameraBrightness = value;
			if (OnCamBrightnessChange != null)
				OnCamBrightnessChange(m_cameraBrightness);
        }
    }
    /// <summary>
    /// Delegate for OnCamBrightnessChange, which is used to update shader properties when the brightness setting changes. 
    /// </summary>
    /// <param name="newVal"></param>
	public delegate void onCamBrightnessChangeDelegate(float newVal);
    /// <summary>
    /// Event fired when the camera brightness setting is changed. Used to update shader properties. 
    /// </summary>
	public event onCamBrightnessChangeDelegate OnCamBrightnessChange;



    //Strings used for the Status display in the Inspector. 
    /// <summary>
    /// Version of the installed ZED SDK, for display in the Inspector.
    /// </summary>
    [Header("Status")]
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
    /// Orientation last returned by the ZED's tracking.
    /// </summary>
	private Quaternion zedOrientation = Quaternion.identity;
    /// <summary>
    /// Position last returned by the ZED's tracking.
    /// </summary>
	private Vector3 zedPosition = new Vector3();
    /// <summary>
    /// Instance of the manager that handles reading/recording SVO files, which are video files
    /// with metadata that you can treat like regular ZED input. 
    /// </summary>
    private ZEDSVOManager zedSVOManager;
    
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
    /// Enables pose smoothing during drift correction. For AR, this is especially important when 
    /// spatial memory is activated. 
    /// </summary>
    private bool enablePoseSmoothing = false;
    /// <summary>
    /// The engine FPS, updated every frame. 
    /// </summary>
    private float fps_engine = 90.0f;


    ///////////////////////////////////////
    /////////// Static States /////////////
    ///////////////////////////////////////

    /// <summary>
    /// Whether AR mode is activated. 
    /// </summary>
    private static bool isStereoRig = false;
    /// <summary>
    /// Whether AR mode is activated. Assigned by ZEDManager.CheckStereoMode() in Awake().
    /// Will be true if the ZED_Rig_Stereo prefab (or a similarly-structured prefab) is used.
    /// </summary>
    public static bool IsStereoRig
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
    /// Mutex for the image acquisition thread. 
    /// </summary>
    public object grabLock = new object(); 
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
    public static sl.ERROR_CODE LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST; 
    /// <summary>
    /// Result of last frame's attempt to initialize the ZED. 
    /// </summary>
    public static sl.ERROR_CODE PreviousInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
    /// <summary>
    /// State of the ZED initialization thread. 
    /// </summary>
    private bool openingLaunched;
    
    /// <summary>
    /// Tracking initialization thread. Used as the tracking takes some time to start.
    /// </summary>
    private Thread trackerThread;

    ///////////////////////////////////////////
    //////  Camera and Player Transforms //////
    ///////////////////////////////////////////
    /// <summary>
    /// Transform of the left camera in the ZED rig. 
    /// </summary>
    private Transform cameraLeft = null;

    /// <summary>
    /// Transform of the right camera in the ZED rig. Only exists in a stereo rig (like ZED_Rig_Stereo). 
    /// </summary>
	private Transform cameraRight = null;

    /// <summary>
	/// Contains the position of the player's head, which is different from the ZED's position in AR mode.
	/// But its position relative to the ZED does not change during use (it's a rigid transform).
    /// In ZED_Rig_Mono, this will be the root ZED_Rig_Mono object. In ZED_Rig_Stereo, this is Camera_eyes. 
    /// </summary>
	private Transform zedRigRoot = null;

    /// <summary>
    /// Gets the center transform, which is the transform moved by the tracker in AR mode. 
    /// This is the root object in ZED_Rig_Mono, and Camera_eyes in ZED_Rig_Stereo.
    /// </summary>
    public Transform GetZedRootTansform()
    {
        return zedRigRoot;
    }

    /// <summary>
    /// Gets the left camera in the ZED rig. It's best to use this one as it's available in all configurations.
    /// </summary>
    public Transform GetLeftCameraTransform()
    {
        return cameraLeft;
    }

    /// <summary>
    /// Get the right camera in the ZED rig. Only available in the stereo rig (ZED_Rig_Stereo).
    /// </summary>
    /// <returns></returns>
    public Transform GetRightCameraTransform()
    {
        return cameraRight;
    }



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
    /// Layer that the left canvas GameObject (showing the image from the left eye) is set to.
    /// The right camera in ZED_Rig_Stereo can't see this layer. 
    /// </summary>
    private int layerLeftScreen = 8;
    /// <summary>
    /// Layer that the right canvas GameObject (showing the image from the right eye) is set to.
    /// The left camera in ZED_Rig_Stereo can't see this layer. 
    /// </summary>
    private int layerRightScreen = 10;
    /// <summary>
    /// Layer that the final left image canvas in the hidden AR rig is set to. (See CreateZEDRigDisplayer())
    /// Hidden from all ZED cameras except the final left camera. 
    /// </summary>
    private int layerLeftFinalScreen = 9;
    /// <summary>
    /// Layer that the final right image canvas in the hidden AR rig is set to. (See CreateZEDRigDisplayer())
    /// Hidden from all ZED cameras except the final right camera. 
    /// </summary>
    private int layerRightFinalScreen = 11;


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
    public static event OnZEDManagerReady OnZEDReady;

    /// <summary>
    /// Delegate for OnZEDDisconnected. 
    /// </summary>
    public delegate void OnZEDManagerDisconnected();
    /// <summary>
    /// Event called when ZED was running but became disconnected. 
    /// </summary>
    public static event OnZEDManagerDisconnected OnZEDDisconnected;


    /// <summary>
    /// ZEDManager instance for singleton implementation.
    /// </summary>
    // Static singleton instance
    private static ZEDManager instance;

    /// <summary>
    /// Singleton implementation: Gets the scene's instance of ZEDManager, and creates one in if nonexistant.
    /// </summary>
    public static ZEDManager Instance
    {
        get { return instance ?? (instance = new GameObject("ZEDManager").AddComponent<ZEDManager>()); }
    }



    #region CHECK_AR
    /// <summary>
    /// Checks if this GameObject is a stereo rig. Requires a child object called 'Camera_eyes' and 
    /// two cameras as children of that object, one with stereoTargetEye set to Left, the other two Right.
    /// Regardless, sets references to leftCamera and (if relevant) rightCamera and sets their culling masks.
    /// </summary>
    private void CheckStereoMode()
    {

        zedRigRoot = gameObject.transform; //The object moved by tracking. By default it's this Transform. May get changed. 

        bool devicePresent = UnityEngine.VR.VRDevice.isPresent;
        if (gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).gameObject.name.Contains("Camera_eyes"))
        {
            //Camera_eyes object exists. Now check all cameras in its children for left- and right-eye cameras.  
            Component[] cams = gameObject.transform.GetChild(0).GetComponentsInChildren(typeof(Camera));
            foreach (Camera cam in cams)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    cameraLeft = cam.transform;
                    SetLayerRecursively(cameraLeft.gameObject, layerLeftScreen);

                    cam.cullingMask &= ~(1 << layerRightScreen);
                    cam.cullingMask &= ~(1 << layerRightFinalScreen);
                    cam.cullingMask &= ~(1 << layerLeftFinalScreen);
                    cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);
                }
                else if (cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    cameraRight = cam.transform;
                    SetLayerRecursively(cameraRight.gameObject, layerRightScreen);
                    cam.cullingMask &= ~(1 << layerLeftScreen);
                    cam.cullingMask &= ~(1 << layerLeftFinalScreen);
                    cam.cullingMask &= ~(1 << layerRightFinalScreen);
                    cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);

                }
            }
        }
        else //No Camera_eyes object exists. It's a mono rig. Set child cameras to a non-VR eye. 
        {
            Component[] cams = gameObject.transform.GetComponentsInChildren(typeof(Camera));
            foreach (Camera cam in cams)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.None)
                {
                    cameraLeft = cam.transform;
                    cam.cullingMask = -1;
                    cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);
                }
            }
        }



        if (cameraLeft && cameraRight) //We found a Camera_eyes object and both a left- and right-eye camera. 
        {
            isStereoRig = true;
            if (cameraLeft.transform.parent != null)
                zedRigRoot = cameraLeft.transform.parent; //Make Camera_eyes the new zedRigRoot to be tracked. 
        }
        else //Not all conditions for a stereo rig were met. Set culling masks accordingly. 
        {
            isStereoRig = false;
            Camera temp = cameraLeft.gameObject.GetComponent<Camera>();

            if (cameraLeft.transform.parent != null)
                zedRigRoot = cameraLeft.transform.parent;

            foreach (Camera c in Camera.allCameras) //Child cameras to leftCamera get matching cullingMasks.
            {
                if (c != temp) 
                {
					c.cullingMask &= ~(1 << layerLeftScreen);
                    c.cullingMask &= ~(1 << sl.ZEDCamera.Tag);
                }
            }
            if (cameraLeft.gameObject.transform.childCount > 0)
            {
				cameraLeft.transform.GetChild(0).gameObject.layer = layerLeftScreen;
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
            threadOpening.Join();
            threadOpening = null;
        }

        //Shut down the image grabbing thread.
        if (threadGrab != null)
        {
            threadGrab.Join();
            threadGrab = null;
        }

        Thread.Sleep(10);
    }

    /// <summary>
    /// Called by Unity when the application is closed. 
    /// Also called by Reset() to properly start from a 'clean slate.'
    /// </summary>
    void OnApplicationQuit()
    {
        zedReady = false;
		OnCamBrightnessChange -= CameraBrightnessChangeHandler;
        Destroy(); //Close the grab and initialization threads. 

        if (zedCamera != null)
        {
            if (zedSVOManager != null)
            {
                if (zedSVOManager.record)
                {
                    zedCamera.DisableRecording();
                }
            }
            zedCamera.Destroy(); 
            zedCamera = null;
        }
    }

    /// <summary>
    /// Sets up starting properties and starts the ZED initialization co-routine. 
    /// </summary>
    void Awake()
    {
        instance = this;
        zedReady = false;
        
        DontDestroyOnLoad(transform.root); //If you want the ZED rig not to be destroyed when loading a scene. 

        //Set first few parameters for initialization. This will get passed to the ZED SDK when initialized. 
        initParameters = new sl.InitParameters();
        initParameters.resolution = resolution;
        initParameters.depthMode = depthMode;
        initParameters.depthStabilization = depthStabilizer;

        //Check if this rig is a stereo rig. Will set isStereoRig accordingly.
        CheckStereoMode();

        //Set initialization parameters that may change depending on what was done in CheckStereoMode(). 
        isZEDTracked = enableTracking;
        initialPosition = zedRigRoot.transform.localPosition; 
        zedPosition = initialPosition;
        zedOrientation = initialRotation;

        //Create a ZEDCamera instance and return an error message if the ZED SDK's dependencies are not detected.
        zedCamera = sl.ZEDCamera.GetInstance();
        LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;

        zedSVOManager = GetComponent<ZEDSVOManager>(); 
        zedCamera.CreateCamera(wrapperVerbose);

        if (zedSVOManager != null) //If we have a ZEDSVOManager, change settings to read to/write from it. 
        {
            if ((zedSVOManager.read || zedSVOManager.record) && zedSVOManager.videoFile.Length == 0) //Path to SVO is empty. 
            {
                zedSVOManager.record = false;
                zedSVOManager.read = false;
            }
            if (zedSVOManager.read) //Playing back an SVO. We'll use that for input instead of actual ZED. 
            {
                zedSVOManager.record = false;
                initParameters.pathSVO = zedSVOManager.videoFile;
                initParameters.svoRealTimeMode = zedSVOManager.realtimePlayback;
                initParameters.depthStabilization = depthStabilizer;
            }
        }

        versionZED = "[SDK]: " + sl.ZEDCamera.GetSDKVersion().ToString() + " [Plugin]: " + sl.ZEDCamera.PluginVersion.ToString();


        //Behavior specific to AR pass-through mode. 
        if (isStereoRig)
        {
            //Creates a hidden camera rig that handles final output to the headset. 
            GameObject o = CreateZEDRigDisplayer();
            o.hideFlags = HideFlags.HideAndDontSave; 
            o.transform.parent = transform;

            //Force some initParameters that are required for a good AR experience.
            initParameters.enableRightSideMeasure = isStereoRig; //Creates a depth map for both eyes, not just one. 
            initParameters.depthMinimumDistance = 0.1f; //Allow depth calculation to very close objects. 
            initParameters.depthStabilization = depthStabilizer; //Improve depth stability and accuracy. 

            //For the Game/output window, mirror the headset view using a custom script that avoids stretching. 
            CreateMirror();
        }

        //Starts a coroutine that initializes the ZED without freezing the game. 
        LastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
        openingLaunched = false;
        StartCoroutine("InitZED");

		OnCamBrightnessChange += CameraBrightnessChangeHandler; //Subscribe event for adjusting brightness setting. 


    }


    #region INITIALIZATION

	/// <summary>
	/// ZED opening function. Should be called in the initialization thread (threadOpening).
	/// </summary>
    void OpenZEDInBackground()
    {
        openingLaunched = true;
        LastInitStatus = zedCamera.Init(ref initParameters);
        openingLaunched = false;
    }


	/// <summary>
	/// Initialization coroutine.
	/// </summary>
	private uint numberTriesOpening = 0;/// Counter of tries to open the ZED
	const int MAX_OPENING_TRIES = 50;
    System.Collections.IEnumerator InitZED()
    {
        zedReady = false;
        while (LastInitStatus != sl.ERROR_CODE.SUCCESS)
        {
            //Initialize the camera
            if (!openingLaunched) //Don't try initializing again if the last attempt is still going. 
            {
                threadOpening = new Thread(new ThreadStart(OpenZEDInBackground)); //Assign thread. 

                if (LastInitStatus != sl.ERROR_CODE.SUCCESS) //If it failed, report it and log one failure. 
                {
#if UNITY_EDITOR
                    numberTriesOpening++;
                    if (numberTriesOpening % 2 == 0 && LastInitStatus == PreviousInitStatus)
                    {
                        Debug.LogWarning("[ZEDPlugin]: " + LastInitStatus);
                    }

                    if (numberTriesOpening > MAX_OPENING_TRIES) //Failed too many times. Give up. 
                    {
                        Debug.Log("[ZEDPlugin]: Stopping initialization.");
                        yield break;
                    }
#endif


                    PreviousInitStatus = LastInitStatus;
                }


                threadOpening.Start(); 
            }

            yield return new WaitForSeconds(0.3f);
        }


        //ZED has initialized successfully. 
        if (LastInitStatus == sl.ERROR_CODE.SUCCESS)
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
            cameraLeft.localPosition = ar.HmdToZEDCalibration.translation;
            cameraLeft.localRotation = ar.HmdToZEDCalibration.rotation;
            if (cameraRight) cameraRight.localPosition = cameraLeft.localPosition + rightCameraOffset; //Space the eyes apart. 
            if (cameraRight) cameraRight.localRotation = cameraLeft.localRotation;
        }
        else if (isStereoRig && !VRDevice.isPresent) //Using stereo rig, but no VR headset. 
        {
            //When no VR HMD is available, simply put the origin at the left camera.
            cameraLeft.localPosition = Vector3.zero;
            cameraLeft.localRotation = Quaternion.identity;
            if (cameraRight) cameraRight.localPosition = rightCameraOffset; //Space the eyes apart. 
            if (cameraRight) cameraRight.localRotation = Quaternion.identity;
        }
        else //Using mono rig (ZED_Rig_Mono). No offset needed. 
        {
            cameraLeft.localPosition = Vector3.zero;
            cameraLeft.localRotation = Quaternion.identity;
        }
    }

	/// <summary>
	/// Find the ZEDRenderingPlane components in the ZED rig and set their rendering settings 
    /// (rendering path, shader values, etc.) for left and right cameras. Also activate/deactivate depth occlusions.
	/// </summary>
    void setRenderingSettings()
    {
        ZEDRenderingPlane leftRenderingPlane = GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
        leftRenderingPlane.SetPostProcess(postProcessing);
		GetLeftCameraTransform().GetComponent<Camera>().renderingPath = RenderingPath.UsePlayerSettings;
        Shader.SetGlobalFloat("_ZEDFactorAffectReal", m_cameraBrightness);

		ZEDRenderingPlane rightRenderingPlane = null; 

		if (IsStereoRig)
        {
            rightRenderingPlane = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
            rightRenderingPlane.SetPostProcess(postProcessing);
       }
        
        ZEDRenderingMode renderingPath = (ZEDRenderingMode)GetLeftCameraTransform().GetComponent<Camera>().actualRenderingPath;
        
		//Make sure we're in either forward or deferred rendering. Default to forward otherwise. 
		if (renderingPath != ZEDRenderingMode.FORWARD && renderingPath != ZEDRenderingMode.DEFERRED)
        {
			Debug.LogError ("[ZED Plugin] Only Forward and Deferred Shading rendering path are supported");
			GetLeftCameraTransform ().GetComponent<Camera> ().renderingPath = RenderingPath.Forward;
			if (IsStereoRig)
				GetRightCameraTransform ().GetComponent<Camera> ().renderingPath = RenderingPath.Forward;
		} 

		//Set depth occlusion. 
		if (renderingPath == ZEDRenderingMode.FORWARD)
		{
			leftRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");
			if (rightRenderingPlane) 
				rightRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");
		 
		}else if (renderingPath == ZEDRenderingMode.DEFERRED) {
			leftRenderingPlane.ManageKeywordDeferredMat(!depthOcclusion, "NO_DEPTH_OCC");
			if (rightRenderingPlane) 
				rightRenderingPlane.ManageKeywordDeferredMat(!depthOcclusion, "NO_DEPTH_OCC");
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

			sl.ERROR_CODE e = sl.ERROR_CODE.NOT_A_NEW_FRAME;

			// Live or SVO? If SVO is paused, we don't need to call Grab() again as the image will not change.
			if (zedSVOManager == null)
				e = zedCamera.Grab (ref runtimeParameters);
			else {
				if (!zedSVOManager.pause)
					e = zedCamera.Grab (ref runtimeParameters);
				else {
					if (zedSVOManager.NeedNewFrameGrab) {
						e = zedCamera.Grab (ref runtimeParameters);
						zedSVOManager.NeedNewFrameGrab = false;
					}
					else
						e = sl.ERROR_CODE.SUCCESS;
				}
			}


            lock (grabLock)
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

                    //Save the timestamp
                    cameraTimeStamp = zedCamera.GetCameraTimeStamp();

#if UNITY_EDITOR
                    float camera_fps = zedCamera.GetCameraFPS();
                    cameraFPS = camera_fps.ToString() + "Fps";
                    
                    if (camera_fps <= FPS * 0.8)
                        cameraFPS += " WARNING: Low USB bandwidth detected";
#endif

                    //Get position of camera
                    if (isTrackingEnable)
                    {
						zedtrackingState = zedCamera.GetPosition(ref zedOrientation, ref zedPosition, sl.TRACKING_FRAME.LEFT_EYE);
                    }
                    else
                        zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;


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

        if (zedSVOManager != null)
        {
            if (zedSVOManager.record)
            {
                sl.ERROR_CODE svorec = zedCamera.EnableRecording(zedSVOManager.videoFile, zedSVOManager.compressionMode);
                if (svorec != sl.ERROR_CODE.SUCCESS)
                {
                    zedSVOManager.record = false;
                    Debug.LogError("SVO recording failed. Check that there is enough space on the drive and that the "
                    + "path provided is valid.");
                }
            }

            if (zedSVOManager.read)
            {
                zedSVOManager.NumberFrameMax = zedCamera.GetSVONumberOfFrames();
            }
        }


        if (enableTracking)
            trackerThread.Join();

        if (isStereoRig && VRDevice.isPresent)
        {
            ZEDMixedRealityPlugin.Pose pose = ar.InitTrackingAR();
            OriginPosition = pose.translation;
            OriginRotation = pose.rotation;
            zedRigRoot.localPosition = OriginPosition;
            zedRigRoot.localRotation = OriginRotation;

			if (!zedCamera.IsHmdCompatible && zedCamera.IsCameraReady)
				Debug.LogWarning("WARNING: AR Passtrough with a ZED is not recommended. Consider using ZED Mini, designed for this purpose.");
        }
        else
        {
            OriginPosition = initialPosition;
            OriginRotation = initialRotation;
        }
			
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif
    }

    /// <summary>
    /// Initializes the ZED's inside-out tracking. Started as a separate thread in OnZEDReady. 
    /// </summary>
    void EnableTrackingThreaded()
    {
        enablePoseSmoothing = enableSpatialMemory;
        lock (grabLock)
        {
            //Make sure we have grabbed a frame first. 
            sl.ERROR_CODE e = zedCamera.Grab(ref runtimeParameters);
            int timeOut_grab = 0;
            while (e != sl.ERROR_CODE.SUCCESS && timeOut_grab < 100)
            {
                e = zedCamera.Grab(ref runtimeParameters);
                Thread.Sleep(10);
                timeOut_grab++;
            }

            //If using spatial memory and given a path to a .area file, make sure that path is valid.
            if (enableSpatialMemory && pathSpatialMemory != "" && !System.IO.File.Exists(pathSpatialMemory))
            {
                Debug.Log("Specified path to .area file '" + pathSpatialMemory + "' does not exist. Ignoring.");
                pathSpatialMemory = "";
            }

            //Now enable the tracking with the proper parameters.
            if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory, 
                enablePoseSmoothing, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
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
            lock (grabLock)
            {
                zedCamera.RetrieveTextures(); //Tell the wrapper to compute the textures. 
                zedCamera.UpdateTextures(); //Tell the wrapper to update the textures. 
                imageTimeStamp = zedCamera.GetImagesTimeStamp();
            }

            requestNewFrame = true; //Lets ThreadedZEDGrab/AcquireImages() start grabbing again. 
            newFrameAvailable = false;
        }

        #region SVO Manager
        if (zedSVOManager != null) //If using an SVO, call SVO-specific functions. 
        {
            if (!zedSVOManager.pause)
            {
                if (zedSVOManager.record)
                {
                    zedCamera.Record();
                }
                else if (zedSVOManager.read)
                {
                    zedSVOManager.CurrentFrame = zedCamera.GetSVOPosition();
                    if (zedSVOManager.loop && zedSVOManager.CurrentFrame >= zedCamera.GetSVONumberOfFrames() - 1)
                    {
                        zedCamera.SetSVOPosition(0);

                        if (enableTracking)
                        {
                            if (!(enableTracking = (zedCamera.ResetTracking(initialRotation, initialPosition) == sl.ERROR_CODE.SUCCESS)))
                            {
                                throw new Exception("Error: Tracking not available after SVO playback has looped.");
                            }

                            zedRigRoot.localPosition = initialPosition;
                            zedRigRoot.localRotation = initialRotation;
                        }
                    }
                }
            }
            else if (zedSVOManager.read)
            {
                zedCamera.SetSVOPosition(zedSVOManager.CurrentFrame); //As this wasn't updated, it's effectively the last frame. 
            }
        }
        #endregion

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

				ar.ExtractLatencyPose (imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation. 
                ar.AdjustTrackingAR (zedPosition, zedOrientation, out r, out v);
				zedRigRoot.localRotation = r;
				zedRigRoot.localPosition = v;

				ZEDSyncPosition = v;
				ZEDSyncRotation = r;
				HMDSyncPosition = ar.LatencyPose ().translation;
				HMDSyncRotation = ar.LatencyPose ().rotation;
			}
            else //Not AR pass-through mode. 
            {
				zedRigRoot.localRotation = zedOrientation;
				zedRigRoot.localPosition = zedPosition;
			}
		} else if (VRDevice.isPresent && isStereoRig) //ZED tracking is off but HMD tracking is on. Fall back to that. 
        {
			isCameraTracked = true;
			ar.ExtractLatencyPose (imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation. 
            zedRigRoot.localRotation = ar.LatencyPose ().rotation;
			zedRigRoot.localPosition = ar.LatencyPose ().translation;
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
            ar.CollectPose(); //Save headset pose with current timestamp. 
    }

    /// <summary>
    /// Updates images, collects HMD poses for latency correction, and applies tracking. 
    /// Called by Unity each frame. 
    /// </summary>
	void Update()
    {
        // Then update the tracking
        UpdateImages(); //Image is updated first so we have its timestamp for latency compensation. 
        UpdateHmdPose(); //Store the HMD's pose at the current timestamp. 
        UpdateTracking(); //Apply position/rotation changes to zedRigRoot. 

        //Check if ZED is disconnected; invoke event and call function if so. 
        if (isDisconnected)
        {
            if (OnZEDDisconnected != null)
                OnZEDDisconnected(); //Invoke event. Used for GUI message and pausing ZEDRenderingPlanes. 

            ZEDDisconnected(); //Tries to reset the camera. 
        }

		#if UNITY_EDITOR
        //Update strings used for displaying stats in the Inspector. 
        if (zedCamera != null)
        {
            float frame_drop_count = zedCamera.GetFrameDroppedPercent();
            float CurrentTickFPS = 1.0f / Time.deltaTime;
            fps_engine = (fps_engine + CurrentTickFPS) / 2.0f;
            engineFPS = fps_engine.ToString("F1") + " FPS";
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
        if (IsStereoRig && VRDevice.isPresent)
        {
            ar.LateUpdateHmdRendering(); //Update textures on final AR rig for output to the headset. 
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
        OnApplicationQuit();
    }


#region AR_CAMERAS
    private GameObject zedRigDisplayer;
    private ZEDMixedRealityPlugin ar;
    /// <summary>
	/// Create a GameObject to display the ZED in an headset (ZED-M Only)
    /// </summary>
    /// <returns></returns>
    private GameObject CreateZEDRigDisplayer()
    {
        //Make sure we don't already have one, such as if the camera disconnected and reconnected. 
        if (zedRigDisplayer != null) return zedRigDisplayer;

        zedRigDisplayer = new GameObject("ZEDRigDisplayer");
        ar = zedRigDisplayer.AddComponent<ZEDMixedRealityPlugin>();


        /*Screens : Left and right */
        GameObject leftScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer meshLeftScreen = leftScreen.GetComponent<MeshRenderer>();
        meshLeftScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshLeftScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshLeftScreen.receiveShadows = false;
        meshLeftScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshLeftScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshLeftScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        leftScreen.layer = layerLeftFinalScreen;
        GameObject.Destroy(leftScreen.GetComponent<MeshCollider>());

        GameObject rightScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer meshRightScreen = rightScreen.GetComponent<MeshRenderer>();
        meshRightScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRightScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRightScreen.receiveShadows = false;
        meshRightScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRightScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObject.Destroy(rightScreen.GetComponent<MeshCollider>());
        meshRightScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        rightScreen.layer = layerRightFinalScreen;

        /*Camera left and right*/
        GameObject camLeft = new GameObject("cameraLeft");
        camLeft.transform.SetParent(zedRigDisplayer.transform);
        Camera camL = camLeft.AddComponent<Camera>();
        camL.stereoTargetEye = StereoTargetEyeMask.Both; //Temporary setting to fix loading screen issue.
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 1 << layerLeftFinalScreen;
        camL.allowHDR = false;
        camL.allowMSAA = false;

        GameObject camRight = new GameObject("cameraRight");
        camRight.transform.SetParent(zedRigDisplayer.transform);
        Camera camR = camRight.AddComponent<Camera>();
        camR.renderingPath = RenderingPath.Forward;//Minimal overhead
        camR.clearFlags = CameraClearFlags.Color;
        camR.backgroundColor = Color.black;
		camR.stereoTargetEye = StereoTargetEyeMask.Both; //Temporary setting to fix loading screen issue.
        camR.cullingMask = 1 << layerRightFinalScreen;
        camR.allowHDR = false;
        camR.allowMSAA = false;
 
		SetLayerRecursively (camRight, layerRightFinalScreen);
		SetLayerRecursively (camLeft, layerLeftFinalScreen);
 
        //Hide camera in editor
#if UNITY_EDITOR
        LayerMask layerNumberBinary = (1 << layerRightFinalScreen); //Convert layer index into binary number. 
        layerNumberBinary |= (1 << layerLeftFinalScreen);
        LayerMask flippedVisibleLayers = ~UnityEditor.Tools.visibleLayers;
        UnityEditor.Tools.visibleLayers = ~(flippedVisibleLayers | layerNumberBinary);
#endif
        leftScreen.transform.SetParent(zedRigDisplayer.transform);
        rightScreen.transform.SetParent(zedRigDisplayer.transform);


        ar.finalCameraLeft = camLeft;
        ar.finalCameraRight = camRight;
        ar.ZEDEyeLeft = cameraLeft.gameObject;
        ar.ZEDEyeRight = cameraRight.gameObject;
        ar.quadLeft = leftScreen.transform;
        ar.quadRight = rightScreen.transform;


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
            mirrorContainer.hideFlags = HideFlags.HideAndDontSave;

            camLeft = new GameObject("MirrorCamera");
            camLeft.hideFlags = HideFlags.HideAndDontSave;
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
        camL.gameObject.layer = 8;
        camL.stereoTargetEye = StereoTargetEyeMask.None;
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 0;
        camL.allowHDR = false;
        camL.allowMSAA = false;
        camL.useOcclusionCulling = false;
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

        OnApplicationQuit();

        openingLaunched = false;
        running = false;

        Awake();

    }



#region EventHandler
	/// <summary>
	/// Changes the real-world brightness by setting the brightness value in the shaders.
	/// </summary>
	/// <param name="newVal">New value to be applied.</param>
	private void CameraBrightnessChangeHandler(float newVal)
	{
		Shader.SetGlobalFloat ("_ZEDFactorAffectReal", m_cameraBrightness);
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
			if (!isTrackingEnable && enableTracking) //If the user switched on tracking. 
			{
				//Enables tracking and initializes the first position of the camera.
				enablePoseSmoothing = enableSpatialMemory;
				if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory, 
                    enablePoseSmoothing, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
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


			if (isTrackingEnable && !enableTracking) //If the user switched off tracking. 
			{
				isZEDTracked = false;
				lock (grabLock)
				{
					zedCamera.DisableTracking();
				}
				isTrackingEnable = false;
			}


			setRenderingSettings(); //Reapplies graphics settings based on current values. 
		}

	}
#endif


}

