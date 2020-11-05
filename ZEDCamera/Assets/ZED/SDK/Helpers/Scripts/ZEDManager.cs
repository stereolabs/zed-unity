using UnityEngine;
using System;
using System.Threading;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
    private bool wrapperVerbose = false;

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

#if ZED_HDRP
    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// SRP Lighting //////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public enum shaderType
    {
        Lit,
        Unlit,
        Greenscreen_Lit,
        Greenscreen_Unlit,
        DontChange
    }
    /// <summary>
    /// 
    /// </summary>
    [HideInInspector]
    public shaderType srpShaderType = shaderType.Lit;
    

    /// <summary>
    /// How much the ZED image should light itself via emission.
    /// Setting to zero is most realistic, but requires you to emulate the real-world lighting conditions within Unity. Higher settings cause the image\
    /// to be uniformly lit, but light and shadow effects are less visible.
    /// </summary>
    [HideInInspector]
    public float selfIllumination = 0.5f;
    /// <summary>
    /// 
    /// </summary>
    [HideInInspector]
    public bool applyZEDNormals = false;
#endif

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

    /// <summary>
    /// If true, tracking is enabled but doesn't move after initializing. 
    /// </summary>
    [HideInInspector]
    public bool trackingIsStatic = false;

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
    /// Gets a boolean value indicating whether the spatial mapping display is enabled.
    /// </summary>
    public bool IsSpatialMappingDisplay { get { return spatialMapping != null ? spatialMapping.display : false; } }

    /// <summary>
    /// Gets a boolean value indicating whether the spatial mapping has chunks
    /// </summary>
    public bool SpatialMappingHasChunks{ get { return spatialMapping != null ? spatialMapping.Chunks.Count>0 : false; } }

    /////////////////////////////////////////////////////////////////////////
    ////////////////////////  Object Detection //////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Sync the Object on the image on the image.
    /// </summary>
    private bool objectDetectionImageSyncMode = true;

    /// <summary>
    /// Whether to track objects across multiple frames using the ZED's position relative to the floor. 
    /// Requires tracking to be on. It's also recommended to enable Estimate Initial Position to find the floor.
    /// </summary>
    [HideInInspector]
    public bool objectDetectionTracking = true;

    /// <summary>
    /// Whether to calculate 2D masks for each object, showing exactly which pixels within the 2D bounding box are the object.
    /// Requires more performance, so do not enable unless needed.
    /// </summary>
    [HideInInspector]
    public bool objectDetection2DMask = false;

    /// <summary>
    /// Choose what detection model to use in the Object detection module
    /// </summary>
    [HideInInspector]
    public sl.DETECTION_MODEL objectDetectionModel = sl.DETECTION_MODEL.MULTI_CLASS_BOX;

    /// <summary>
    /// Defines if the body fitting will be applied
    /// </summary>
    [HideInInspector]
    public bool bodyFitting = false;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int SK_personDetectionConfidenceThreshold = 50;


    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int OD_personDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int vehicleDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int bagDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int animalDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int electronicsDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Detection sensitivity. Represents how sure the SDK must be that an object exists to report it. Ex: If the threshold is 80, then only objects
    /// where the SDK is 80% sure or greater will appear in the list of detected objects. 
    /// </summary>
    [HideInInspector]
    public int fruitVegetableDetectionConfidenceThreshold = 35;

    /// <summary>
    /// Whether to detect people during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassPersonFilter = true;

    /// <summary>
    /// Whether to detect vehicles during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassVehicleFilter = true;

    /// <summary>
    /// Whether to detect bags during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassBagFilter = true;

    /// <summary>
    /// Whether to detect animals during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassAnimalFilter = true;

    /// <summary>
    /// Whether to detect electronics during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassElectronicsFilter = true;

    /// <summary>
    /// Whether to detect fruits and vegetables during object detection. 
    /// </summary>
    [HideInInspector]
    public bool objectClassFruitVegetableFilter = true;

    /// <summary>
    /// Whether the object detection module has been activated successfully. 
    /// </summary>
    private bool objectDetectionRunning = false;
    /// <summary>
    /// Whether the object detection module has been activated successfully. 
    /// </summary>
    public bool IsObjectDetectionRunning { get { return objectDetectionRunning; } }

    /// <summary>
    /// Set to true when there is not a fresh frame of detected objects waiting for processing, meaning we can retrieve the next one. 
    /// </summary>
    private bool requestobjectsframe = true;
    /// <summary>
    /// Set to true when a new frame of detected objects has been retrieved in the image acquisition thread, ready for the main thread to process. 
    /// </summary>
    private bool newobjectsframeready = false;

    /// <summary>
    /// Last object detection frame detected by the SDK. This data comes straight from the C++ SDK; see detectionFrame for an abstracted version
    /// with many helper functions for use inside Unity. 
    /// </summary>
    private sl.ObjectsFrameSDK objectsFrameSDK = new sl.ObjectsFrameSDK();
    /// <summary>
    /// Last object detection frame detected by the SDK. This data comes straight from the C++ SDK; see GetDetectionFrame for an abstracted version
    /// with many helper functions for use inside Unity. 
    /// </summary>
    public sl.ObjectsFrameSDK GetSDKObjectsFrame { get { return objectsFrameSDK; } }
    /// <summary>
    /// Timestamp of the most recent object frame fully processed. This is used to calculate the FPS of the object detection module. 
    /// </summary>
    private ulong lastObjectFrameTimeStamp = 0;
    /// <summary>
    /// Frame rate at which the object detection module is running. Only reports performance; changing this value has no effect on detection. 
    /// </summary>
    private float objDetectionModuleFPS = 15.0f;

    /// <summary>
    /// Last object detection frame detected by the SDK, in the form of a DetectionFrame instance which has many helper functions for use in Unity. 
    /// </summary>
    private DetectionFrame detectionFrame;
    /// <summary>
    /// Last object detection frame detected by the SDK, in the form of a DetectionFrame instance which has many helper functions for use in Unity. 
    /// </summary>
    public DetectionFrame GetDetectionFrame { get { return detectionFrame; } }
    /// <summary>
    /// Delegate for events that take an object detection frame straight from the SDK (not abstracted). 
    /// </summary>
    public delegate void onNewDetectionTriggerSDKDelegate(sl.ObjectsFrameSDK objFrame);
    /// <summary>
    /// Event that's called whenever the Object Detection module detects a new frame. 
    /// Includes data straight from the C++ SDK. See OnObjectDetection/DetectionFrame for an abstracted version that has many helper functions
    /// that makes it easier to use in Unity. 
    /// </summary>
    public event onNewDetectionTriggerSDKDelegate OnObjectDetection_SDKData;

    /// <summary>
    /// Delegate for events that take an object detection frame, in the form of a DetectionFrame object which has helper functions. 
    /// </summary>
    public delegate void onNewDetectionTriggerDelegate(DetectionFrame objFrame);
    /// <summary>
    /// Event that's called whenever the Object Detection module detects a new frame. 
    /// Supplies data in the form of a DetectionFrame instance, which has many helper functions for use in Unity. 
    /// </summary>
    public event onNewDetectionTriggerDelegate OnObjectDetection;

    private sl.dll_ObjectDetectionRuntimeParameters od_runtime_params = new sl.dll_ObjectDetectionRuntimeParameters();

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
    /// Field version of CameraBrightness property. 
    /// </summary>
    [SerializeField]
    [HideInInspector]
    private int m_cameraBrightness = 100;
    /// Brightness of the final real-world image. Default is 100. Lower to darken the environment in a realistic-looking way. 
    /// This is a rendering setting that doesn't affect the raw input from the camera.
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

    /// <summary>
    /// Whether to enable the new color/gamma curve added to the ZED SDK in v3.0. Exposes more detail in darker regions 
    /// and removes a slight red bias. 
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool enableImageEnhancement = true;

    /// Field version of MaxDepthRange property. 
    /// </summary>
    [SerializeField]
    private float m_maxDepthRange = 40f;
    /// <summary>
    /// Maximum depth at which the camera will display the real world, in meters. Pixels further than this value will be invisible. 
    /// </summary>
    [HideInInspector]
    public float MaxDepthRange
    {
        get { return m_maxDepthRange; }
        set
        {
            if (m_maxDepthRange == value) return;
            m_maxDepthRange = value;
            if (OnMaxDepthChange != null)
                OnMaxDepthChange(m_maxDepthRange);
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
    public sl.SVO_COMPRESSION_MODE svoOutputCompressionMode = sl.SVO_COMPRESSION_MODE.H264_BASED;

    /// <summary>
    /// SVO specific bitrate in KBits/s
    /// Default : 0 = internal bitrate
    /// </summary>
    [HideInInspector]
    public int svoOutputBitrate = 0;
    /// <summary>
    /// SVO specific FPS
    /// Default : 0 = Camera FPS
    /// </summary>
    [HideInInspector]
    public int svoOutputTargetFPS = 0;

    /// <summary>
    /// If input is streaming, then set to direct-dump into SVO file (false) or decoding/re-encoding (true).
    /// Recommended to leave at false to save an encoding session.
    /// </summary>
    public bool svoOutputTranscodeStreaming = false;

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

    /// <summary>
    /// Enable/Disable adaptative bitrate
    /// </summary>
    [HideInInspector]
    public int chunkSize = 8096;

    /// <summary>
    /// Set a specific target for the streaming framerate
    /// </summary>
    [HideInInspector]
    public int streamingTargetFramerate = 0;


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
    /// Field version of confidenceThreshold property. 
    /// </summary>
    [SerializeField]
    [HideInInspector]
    private int m_confidenceThreshold = 100;
    /// <summary>
    /// How tolerant the ZED SDK is to low confidence values. Lower values filter more pixels.
    /// </summary>
    public int confidenceThreshold
    {
        get
        {
            return m_confidenceThreshold;
        }
        set
        {
            if (value == m_confidenceThreshold) return;

            m_confidenceThreshold = Mathf.RoundToInt(Mathf.Clamp(value, 0, 100));
            if (Application.isPlaying && zedReady)
            {
                runtimeParameters.confidenceThreshold = m_confidenceThreshold;
            }

        }
    }

    [SerializeField]
    [HideInInspector]
    private int m_textureConfidenceThreshold = 100;
    /// <summary>
    /// How tolerant the ZED SDK is to low confidence values. Lower values filter more pixels.
    /// </summary>
    public int textureConfidenceThreshold
    {
        get
        {
            return m_textureConfidenceThreshold;
        }
        set
        {
            if (value == m_textureConfidenceThreshold) return;

            m_textureConfidenceThreshold = Mathf.RoundToInt(Mathf.Clamp(value, 0, 100));
            if (Application.isPlaying && zedReady)
            {
                runtimeParameters.textureConfidenceThreshold = m_textureConfidenceThreshold;
            }

        }
    }

    /// <summary>
    /// Options for enabling the depth measurement map for the right camera. Costs performance if on, even if not used. 
    /// </summary>
    public enum RightDepthEnabledMode
    {
        /// <summary>
        /// Right depth measure will be enabled if a ZEDRenderingPlane component set to the right eye is detected as a child of 
        /// ZEDManager's GameObject, as in the ZED rig prefabs.  
        /// </summary>
        AUTO, 
        /// <summary>
        /// Right depth measure is disabled. 
        /// </summary>
        OFF,
        /// <summary>
        /// Right depth measure is enabled. 
        /// </summary>
        ON
    }

    /// <summary>
    /// Whether to enable depth measurements from the right camera. Required for depth effects in AR pass-through, but requires performance even if not used.
    /// Auto enables it only if a ZEDRenderingPlane component set to the right eye is detected as a child of ZEDManager's GameObject (as in the ZED rig prefabs.) 
    /// </summary>
    [HideInInspector]
    public RightDepthEnabledMode enableRightDepthMeasure = RightDepthEnabledMode.AUTO;

    /// <summary>
    /// Delegate for OnCamBrightnessChange, which is used to update shader properties when the brightness setting changes. 
    /// </summary>
	public delegate void onCamBrightnessChangeDelegate(int newVal);
    /// <summary>
    /// Event fired when the camera brightness setting is changed. Used to update shader properties. 
    /// </summary>
	public event onCamBrightnessChangeDelegate OnCamBrightnessChange;
    /// <summary>
    /// Delegate for OnCamBrightnessChange, which is used to update shader properties when the max depth setting changes. 
    /// </summary>
    public delegate void onMaxDepthChangeDelegate(float newVal);
    /// <summary>
    /// Event fired when the max depth setting is changed. Used to update shader properties. 
    /// </summary>
    public event onMaxDepthChangeDelegate OnMaxDepthChange;

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

    private float maxdepthrange = 40f;
    public float maxDepthRange
    {
        get
        {
            return maxdepthrange;
        }
        set
        {
            maxdepthrange = Mathf.Clamp(value, 0, 40);
            if (Application.isPlaying)
            {
                setRenderingSettings();
            }
        }
    }

    /// <summary>
    /// If true, and you are using a ZED2 or ZED Mini, IMU fusion uses data from the camera's IMU to improve tracking results. 
    /// </summary>
    [HideInInspector]
    public bool enableIMUFusion = true;

    /// <summary>
    /// If true, the ZED SDK will subtly adjust the ZED's calibration during runtime to account for heat and other factors. 
    /// Reasons to disable this are rare. 
    /// </summary>
    [HideInInspector]
    public bool enableSelfCalibration = true;

    /////////////////////////////////////////////////////////////////////////
    ///////////////////////// Video Settings ////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    //Controls for the ZED's video settings (brightness, saturation, exposure, etc.)

    /// <summary>
    /// Behavior options for how the ZED's video settings (brightness, saturation, etc.) are applied when the ZED first connects. 
    /// </summary>
    public enum VideoSettingsInitMode
    {
        /// <summary>
        /// Camera will be assigned video settings set in ZEDManager's Inspector before running the scene.
        /// </summary>
        Custom,
        /// <summary>
        /// Camera will load settings last applied to the ZED. May have been from a source outside Unity. 
        /// This is the default behavior in the ZED SDK and most ZED apps. 
        /// </summary>
        LoadFromSDK,
        /// <summary>
        /// Camera will load default video settings. 
        /// </summary>
        Default
    }

    /// <summary>
    /// How the ZED's video settings (brightness, saturation, etc.) are applied when the ZED first connects. 
    /// </summary>
    public VideoSettingsInitMode videoSettingsInitMode = VideoSettingsInitMode.Custom;

    /// <summary>
    /// Brightness setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoBrightness = 4;
    /// <summary>
    /// Contrast setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoContrast = 4;
    /// <summary>
    /// Hue setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoHue = 0;
    /// <summary>
    /// Saturation setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoSaturation = 4;
    /// <summary>
    /// Auto gain/exposure setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private bool videoAutoGainExposure = true;
    /// <summary>
    /// Gain setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom and videoAutoGainExposure is false.  
    /// </summary>
    [SerializeField]
    private int videoGain = 10;
    /// <summary>
    /// Exposure setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom and videoAutoGainExposure is false. 
    /// </summary>
    [SerializeField]
    public int videoExposure = 100;
    /// <summary>
    /// Auto White Balance setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private bool videoAutoWhiteBalance = true;
    /// <summary>
    /// White Balance temperature setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom and videoAutoWhiteBalance is false. 
    /// </summary>
    [SerializeField]
    private int videoWhiteBalance = 3200;
    /// <summary>
    /// Sharpness setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoSharpness = 3;
    /// <summary>
    /// Sharpness setting for the ZED camera itself.
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private int videoGamma = 5;
    /// <summary>
    /// Whether the LED on the ZED camera is on. 
    /// Serialized value is applied to the camera on start when videoSettingsInitMode is set to Custom. 
    /// </summary>
    [SerializeField]
    private bool videoLEDStatus = true;


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
    /// <summary>
    /// Object detection framerate 
    /// </summary>
    [ReadOnly("Object Detection FPS")] [HideInInspector] public string objectDetectionFPS = "-";




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
    /// Indicates if Sensors( IMU,...) is needed/required. For most applications, it is required.
    /// Sensors are transmitted through USB2.0 lines. If USB2 is not available (USB3.0 only extension for example), set it to false.
    /// </summary>
    private bool sensorsRequired = false;
    /// <summary>
    /// Set the camera in Flip mode
    /// </summary>
    private sl.FLIP_MODE cameraFlipMode = sl.FLIP_MODE.AUTO;
    /// <summary>
    /// Whether the camera is currently being tracked using the ZED's inside-out tracking. 
    /// </summary>ccvv
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
    /// If Estimate Initial Position is true and we're in SVO mode with Loop enabled, we'll want to cache our first pose to initialPosition and initialRotation. 
    /// This flag lets us know if we've done that yet so we can only assign them on the first tracked frame. 
    /// </summary>
    private bool initialPoseCached = false;
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
    [SerializeField]
    [HideInInspector]
    public sl.SENSING_MODE sensingMode = sl.SENSING_MODE.FILL;
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

    /// <summary>
    /// In AR pass-through mode, whether to compare the ZED's IMU data against the reported position of
    /// the VR headset. This helps compensate for drift and should usually be left on.
    /// However, in some setups, like when using a custom mount, this can cause tracking errors.
    /// </summary><remarks>
    /// Read more about the potential errors here: https://support.stereolabs.com/hc/en-us/articles/360026482413
    /// </remarks>
    public bool setIMUPriorInAR = true;

    /// <summary>
    /// If true, the ZED rig will enter 'pass-through' mode if it detects a stereo rig - at least two cameras as children with ZEDRenderingPlane
    /// components, each with a different eye) - and a VR headset is connected. If false, it will never enter pass-through mode. 
    /// </summary>
    public bool allowARPassThrough = true;

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
    /// Returns the left ZED camera transform. If there is no left camera but there is a right camera, 
    /// returns the right camera transform instead. 
    /// </summary>
    /// <returns></returns>
    public Transform GetMainCameraTransform()
    {
        if (camLeftTransform) return camLeftTransform;
        else if (camRightTransform) return camRightTransform;
        else return null;
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
    /// Returns the left ZED camera. If there is no left camera but there is a right camera, 
    /// returns the right camera instead. 
    /// </summary>
    /// <returns></returns>
    public Camera GetMainCamera()
    {
        if (cameraLeft) return cameraLeft;
        else if (cameraRight) return cameraRight;
        else return null;
    }

    /// <summary>
    /// Gets the left camera in the ZED rig. Both ZED_Rig_Mono and ZED_Rig_Stereo have a left camera by default. 
    /// </summary>
    public Camera GetLeftCamera()
    {
        if (cameraLeft == null && camLeftTransform != null)
            cameraLeft = camLeftTransform.GetComponent<Camera>();
        return cameraLeft;
    }

    /// <summary>
    /// Get the right camera in the ZED rig. Only available in the stereo rig (ZED_Rig_Stereo) unless configured otherwise. 
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
    private bool objectDetectionFoldoutOpen = false;
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
            return ZEDLayers.arlayer;
        }
    }
    [SerializeField]
    [HideInInspector]
   //private int arlayer = 30;

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

    private bool hasXRDevice()
    {
        return XRDevice.isPresent;
    }


    /// <summary>
    /// Checks if this GameObject is a stereo rig. Requires a child object called 'Camera_eyes' and 
    /// two cameras as children of that object, one with stereoTargetEye set to Left, the other two Right.
    /// Regardless, sets references to leftCamera and (if relevant) rightCamera.
    /// </summary>
    private void CheckStereoMode()
    {
        zedRigRoot = gameObject.transform; //The object moved by tracking. By default it's this Transform. May get changed. 

        bool devicePresent = hasXRDevice(); //May not need. 

        //Set first left eye
        Component[] cams = gameObject.GetComponentsInChildren<Camera>();
        //Camera firstmonocam = null;
        List<Camera> monocams = new List<Camera>();
        foreach (Camera cam in cams)
        {
            switch (cam.stereoTargetEye)
            {
                case StereoTargetEyeMask.Left:
                    if (!cameraLeft)
                    {
                        cameraLeft = cam;
                        camLeftTransform = cam.transform;
                    }
                    break;
                case StereoTargetEyeMask.Right:
                    if (!cameraRight)
                    {
                        cameraRight = cam;
                        camRightTransform = cam.transform;
                    }
                    break;
                case StereoTargetEyeMask.None:
                    monocams.Add(cam);
                    break;
                case StereoTargetEyeMask.Both:
                default:
                    break;
            }
        }

        //If the left camera or right camera haven't been assigned via stereo target eyes, search the monocams
        //based on their ZEDRenderingPlane assignments. 
        //This won't affect whether the rig is in stereo mode, but allows the cameras to be accessed via GetLeftCamera() and GetRightCamera(). 
        if (cameraLeft == null || cameraRight == null)
        {
            foreach (Camera cam in monocams)
            {
                ZEDRenderingPlane rendplane = cam.gameObject.GetComponent<ZEDRenderingPlane>();
                if (!rendplane) continue;

                if (!cameraLeft && (rendplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.LEFT || rendplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.LEFT_FORCE))
                {
                    cameraLeft = cam;
                    camLeftTransform = cam.transform;
                }
                else if (!cameraRight && (rendplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.RIGHT || rendplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.RIGHT_FORCE))
                {
                    cameraRight = cam;
                    camRightTransform = cam.transform;
                }
            }
        }

        if (camLeftTransform && camRightTransform && cameraLeft.stereoTargetEye == StereoTargetEyeMask.Left) //We found both a left- and right-eye camera. 
        {
            if (camLeftTransform.transform.parent != null)
            {
                zedRigRoot = camLeftTransform.parent; //Make the camera's parent object (Camera_eyes in the ZED_Rig_Stereo prefab) the new zedRigRoot to be tracked. 
            }

            if (UnityEngine.XR.XRDevice.isPresent && allowARPassThrough)
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
    private void OnApplicationQuit()
    {
        CloseManager();
        //sl.ZEDCamera.UnloadPlugin();

        //If this was the last camera to close, make sure all instances are closed. 
        bool notlast = false;
        foreach (ZEDManager manager in ZEDManagerInstance)
        {
            if (manager != null && manager.IsZEDReady == true)
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

#if !ZED_LWRP && !ZED_HDRP
        ClearRendering();
#endif

        zedReady = false;
        OnCamBrightnessChange -= SetCameraBrightness;
        OnMaxDepthChange -= SetMaxDepthRange;
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

#if !ZED_LWRP && !ZED_HDRP
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
#endif


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
        initParameters.cameraDeviceID = (int)cameraID;
        initParameters.depthMode = depthMode;
        initParameters.depthStabilization = depthStabilizer;
        initParameters.sensorsRequired = sensorsRequired;
        initParameters.depthMaximumDistance = 40.0f; // 40 meters should be enough for all applications
        initParameters.cameraImageFlip = (int)cameraFlipMode;
        initParameters.enableImageEnhancement = enableImageEnhancement;
        initParameters.cameraDisableSelfCalib = !enableSelfCalibration;

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
            this.gameObject.SetActive(false);
            return;
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

            initParameters.depthMinimumDistance = 0.1f; //Allow depth calculation to very close objects. 

            //For the Game/output window, mirror the headset view using a custom script that avoids stretching. 
            CreateMirror();
        }

        //Determine if we should enable the right depth measurement, which costs performance but is needed for pass-through AR. 
        switch(enableRightDepthMeasure)
        {
            case RightDepthEnabledMode.AUTO:
            default:
                if (isStereoRig) //If so, we've already determined we have both a left and right ZEDRenderingPlane, so skip the lookups. 
                {
                    initParameters.enableRightSideMeasure = true; 
                }
                else
                {
                    foreach (ZEDRenderingPlane renderplane in GetComponentsInChildren<ZEDRenderingPlane>())
                    { 
                        //If we have any ZEDRenderingPlanes that are looking through the right side, enable the measurements. 
                        if (renderplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.RIGHT ||
                            renderplane.viewSide == ZEDRenderingPlane.ZED_CAMERA_SIDE.RIGHT_FORCE)
                        {
                            initParameters.enableRightSideMeasure = true;
                            break;
                        }
                    }
                }
                break;
            case RightDepthEnabledMode.OFF:
                initParameters.enableRightSideMeasure = false;
                break;
            case RightDepthEnabledMode.ON:
                initParameters.enableRightSideMeasure = true;
                break;
        }

        //Starts a coroutine that initializes the ZED without freezing the game. 
        lastInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
        openingLaunched = false;
        StartCoroutine(InitZED());


        OnCamBrightnessChange += SetCameraBrightness; //Subscribe event for adjusting brightness setting. 
        OnMaxDepthChange += SetMaxDepthRange;

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
            cameraFirmware = zedCamera.GetCameraFirmwareVersion().ToString() + "-" + zedCamera.GetSensorsFirmwareVersion().ToString();
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
                    sl.ERROR_CODE err = zedCamera.EnableStreaming(streamingCodec, (uint)bitrate, (ushort)streamingPort, gopSize, adaptativeBitrate, chunkSize,streamingTargetFramerate);
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
        //Vector3 rightCameraOffset = new Vector3(zedCamera.Baseline, 0.0f, 0.0f);
        if (isStereoRig && hasXRDevice()) //Using AR pass-through mode. 
        {
            //zedRigRoot transform (origin of the global camera) is placed on the HMD headset. Therefore, we move the 
            //camera in front of it by offsetHmdZEDPosition to compensate for the ZED's position on the headset. 
            //If values are wrong, tweak calibration file created in ZEDMixedRealityPlugin. 
            camLeftTransform.localPosition = arRig.HmdToZEDCalibration.translation;
            camLeftTransform.localRotation = arRig.HmdToZEDCalibration.rotation;
            if (camRightTransform) camRightTransform.localPosition = camLeftTransform.localPosition + new Vector3(zedCamera.Baseline, 0.0f, 0.0f); //Space the eyes apart. 
            if (camRightTransform) camRightTransform.localRotation = camLeftTransform.localRotation;
        }
        else if (camLeftTransform && camRightTransform) //Using stereo rig, but no VR headset. 
        {
            //When no VR HMD is available, simply put the origin at the left camera.
            camLeftTransform.localPosition = Vector3.zero;
            camLeftTransform.localRotation = Quaternion.identity;
            camRightTransform.localPosition = new Vector3(zedCamera.Baseline, 0.0f, 0.0f); //Space the eyes apart. 
            camRightTransform.localRotation = Quaternion.identity;
        }
        else //Using mono rig (ZED_Rig_Mono). No offset needed. 
        {
            if (GetMainCameraTransform())
            {
                GetMainCameraTransform().localPosition = Vector3.zero;
                GetMainCameraTransform().localRotation = Quaternion.identity;
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
            if (leftRenderingPlane)
            {
                leftRenderingPlane.SetPostProcess(postProcessing);
                GetLeftCameraTransform().GetComponent<Camera>().renderingPath = RenderingPath.UsePlayerSettings;
                SetCameraBrightness(m_cameraBrightness);
                cameraLeft.cullingMask &= ~(1 << zedCamera.TagInvisibleToZED);
            }
        }

        ZEDRenderingPlane rightRenderingPlane = null;
        if (GetRightCameraTransform() != null)
        {
            rightRenderingPlane = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
            if (rightRenderingPlane)
            {
                rightRenderingPlane.SetPostProcess(postProcessing);
                cameraRight.renderingPath = RenderingPath.UsePlayerSettings;
                cameraRight.cullingMask &= ~(1 << zedCamera.TagInvisibleToZED);
            }
        }

        SetCameraBrightness(m_cameraBrightness);
        SetMaxDepthRange(m_maxDepthRange);


#if ZED_HDRP
        SetSelfIllumination(selfIllumination);
        SetBoolValueOnPlaneMaterials("_ApplyZEDNormals", applyZEDNormals);
#endif

        Camera maincam = GetMainCamera();
        if (maincam != null)
        {
            ZEDRenderingMode renderingPath = (ZEDRenderingMode)maincam.actualRenderingPath;

            //Make sure we're in either forward or deferred rendering. Default to forward otherwise. 
            if (renderingPath != ZEDRenderingMode.FORWARD && renderingPath != ZEDRenderingMode.DEFERRED)
            {
                Debug.LogError("[ZED Plugin] Only Forward and Deferred Shading rendering path are supported");
                if (cameraLeft) cameraLeft.renderingPath = RenderingPath.Forward;
                if (cameraRight) cameraRight.renderingPath = RenderingPath.Forward;
            }

            //Set depth occlusion. 
            if (renderingPath == ZEDRenderingMode.FORWARD)
            {
                if (leftRenderingPlane)
                    leftRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");
                if (rightRenderingPlane)
                    rightRenderingPlane.ManageKeywordPipe(!depthOcclusion, "NO_DEPTH_OCC");

            }
            else if (renderingPath == ZEDRenderingMode.DEFERRED)
            {
                if (leftRenderingPlane)
                    leftRenderingPlane.ManageKeywordDeferredMat(!depthOcclusion, "NO_DEPTH_OCC");
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
        runtimeParameters.confidenceThreshold = confidenceThreshold;
        runtimeParameters.textureConfidenceThreshold = textureConfidenceThreshold;
        //Don't change this reference frame. If we need normals in the world frame, better to do the conversion ourselves.
        runtimeParameters.measure3DReferenceFrame = sl.REFERENCE_FRAME.CAMERA;

        while (running)
        {
            if (zedCamera == null)
                return;

            if (runtimeParameters.sensingMode != sensingMode) runtimeParameters.sensingMode = sensingMode;

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

                    //Update object detection here if using object sync. 
                    if (objectDetectionRunning && objectDetectionImageSyncMode == true && requestobjectsframe)
                    {
                        RetrieveObjectDetectionFrame();
                    }

                    //Get position of camera
                    if (isTrackingEnable)
                    {
                        zedtrackingState = zedCamera.GetPosition(ref zedOrientation, ref zedPosition, sl.TRACKING_FRAME.LEFT_EYE);
                        //zedtrackingState = sl.TRACKING_STATE.TRACKING_OK;
                        if (inputType == sl.INPUT_TYPE.INPUT_TYPE_SVO && svoLoopBack == true && initialPoseCached == false)
                        {
                            initialPosition = zedPosition;
                            initialRotation = zedOrientation;
                            initialPoseCached = true;
                        }
                    }
                    else
                    {
                        zedtrackingState = sl.TRACKING_STATE.TRACKING_OFF;
                    }

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
        //Apply camera settings based on user preference. 
        InitVideoSettings(videoSettingsInitMode);

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


        if (isStereoRig && hasXRDevice())
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
        EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
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
                enablePoseSmoothing, estimateInitialPosition, trackingIsStatic, enableIMUFusion, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
            {
                throw new Exception(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
            }
            else
            {
                isTrackingEnable = true;
            }

        }
    }

#if ZED_HDRP
    public bool GetChosenSRPMaterial(out Material srpMat)
    {
        switch(srpShaderType)
        {
            case shaderType.Lit:
                srpMat = Resources.Load<Material>("Materials/Lighting/Mat_ZED_HDRP_Lit");
                if (srpMat == null)
                {
                    Debug.LogError("Couldn't find material in Resources. Path: " + "Materials/Lighting/Mat_ZED_HDRP_Lit");
                    return false;
                }
                else return true;
            case shaderType.Unlit:
                srpMat = Resources.Load<Material>("Materials/Unlit/Mat_ZED_Unlit_RawInput");
                if (srpMat == null)
                {
                    Debug.LogError("Couldn't find material in Resources. Path: " + "Materials/Unlit/Mat_ZED_Unlit_RawInput");
                    return false;
                }
                else return true;
            case shaderType.Greenscreen_Lit:
                srpMat = Resources.Load<Material>("Materials/Lighting/Mat_ZED_Greenscreen_HDRP_Lit");
                if (srpMat == null)
                {
                    Debug.LogError("Couldn't find material in Resources. Path: " + "Materials/Lighting/Mat_ZED_Greenscreen_HDRP_Lit");
                    return false;
                }
                else return true;
            case shaderType.Greenscreen_Unlit:
                srpMat = Resources.Load<Material>("Materials/Unlit/Mat_ZED_Greenscreen_Unlit");
                if (srpMat == null)
                {
                    Debug.LogError("Couldn't find material in Resources. Path: " + "Materials/Unlit/Mat_ZED_Greenscreen_Unlit");
                    return false;
                }
                else return true;
            case shaderType.DontChange:
            default:
                srpMat = null;
                return false;
        }
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// Handler for playmodeStateChanged. 
    /// </summary>
    void HandleOnPlayModeChanged()
    {

        if (zedCamera == null) return;

#if UNITY_EDITOR
        EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
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

            if (hasXRDevice() && isStereoRig) //AR pass-through mode. 
            {
                if (calibrationHasChanged) //If the HMD offset calibration file changed during runtime. 
                {
                    AdjustZEDRigCameraPosition(); //Re-apply the ZED's offset from the VR headset. 
                    calibrationHasChanged = false;
                }

                arRig.ExtractLatencyPose(imageTimeStamp); //Find what HMD's pose was at ZED image's timestamp for latency compensation. 
                arRig.AdjustTrackingAR(zedPosition, zedOrientation, out r, out v, setIMUPriorInAR);
                zedRigRoot.localRotation = r;
                zedRigRoot.localPosition = v;
                //Debug.DrawLine(new Vector3(0, 0.05f, 0), (r * Vector3.one * 5) + new Vector3(0, 0.05f, 0), Color.red);
                //Debug.DrawLine(Vector3.zero, zedOrientation * Vector3.one * 5, Color.green);

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
        else if (hasXRDevice() && isStereoRig) //ZED tracking is off but HMD tracking is on. Fall back to that. 
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
        if (IsStereoRig && hasXRDevice())
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
        UpdateObjectsDetection(); //Update od if activated
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
            else if (hasXRDevice() && isStereoRig)
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
        spatialMapping.StartStatialMapping(sl.SPATIAL_MAP_TYPE.MESH, mappingResolutionPreset, mappingRangePreset, isMappingTextured);
    }

    /// <summary>
    /// Ends the current spatial mapping. Once called, the current mesh will be filtered, textured (if enabled) and saved (if enabled), 
    /// and a mesh collider will be added. 
    /// </summary>
    public void StopSpatialMapping()
    {
        if (spatialMapping != null)
        {
            if (saveMeshWhenOver)
                SaveMesh(meshPath);
            spatialMapping.StopStatialMapping();

        }
    }

    /// <summary>
    /// Updates the filtering parameters and call the ZEDSpatialMapping instance's Update() function. 
    /// </summary>
    private void UpdateMapping()
    {
        if (spatialMapping != null)
        {
            //if (IsMappingUpdateThreadRunning)
            if (spatialMapping.IsRunning())
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
    public void SaveMesh(string meshPath = "ZEDMeshObj.obj")
    {
        spatialMapping.RequestSaveMesh(meshPath);
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
    //////////////////////////////////////////////////////// OBJECT DETECTION REGION //////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region OBJECT_DETECTION
    
    /// <summary>
    /// True when the object detection coroutine is in the process of starting. 
    /// Used to prevent object detection from being launched multiple times at once, which causes instability. 
    /// </summary>
    private bool odIsStarting = false;
    /// <summary>
    /// Starts the ZED object detection.
    /// <para>Note: This will lock the main thread for a moment, which may appear to be a freeze.</para>
    /// </summary>
    public void StartObjectDetection()
    {
        //We start a coroutine so we can delay actually starting the detection. 
        //This is because the main thread is locked for awhile when you call this, appearing like a freeze. 
        //This time lets us deliver a log message to the user indicating that this is expected. 
        StartCoroutine(startObjectDetection());
    }


    /// <summary>
    /// Starts the object detection module after a two-frame delay, allowing us to deliver a log message
    /// to the user indicating that what appears to be a freeze is actually expected and will pass. 
    /// </summary>
    /// <returns></returns>
    private IEnumerator startObjectDetection()
    {
        if (odIsStarting == true)
        {
            Debug.LogError("Tried to start Object Detection while it was already starting. Do you have two scripts trying to start it?");
            yield break;
        }
        if(objectDetectionRunning)
        {
            Debug.LogWarning("Tried to start Object Detection while it was already running.");
        }

        if (zedCamera != null)
        {
            odIsStarting = true;
            Debug.LogWarning("Starting Object Detection. This may take a moment.");

            bool oldpausestate = pauseSVOReading; //The two frame delay will cause you to miss some SVO frames if playing back from an SVO, unless we pause. 
            pauseSVOReading = true;

            yield return null;

            pauseSVOReading = oldpausestate;

            sl.dll_ObjectDetectionParameters od_param = new sl.dll_ObjectDetectionParameters();
            od_param.imageSync = objectDetectionImageSyncMode;
            od_param.enableObjectTracking = objectDetectionTracking;
           // od_param.enable2DMask = objectDetection2DMask;
            od_param.detectionModel = objectDetectionModel;
            od_param.enableBodyFitting = bodyFitting;
            od_runtime_params.object_confidence_threshold = new int[(int)sl.OBJECT_CLASS.LAST];
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.PERSON] = (objectDetectionModel == sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE || objectDetectionModel == sl.DETECTION_MODEL.HUMAN_BODY_FAST) ? SK_personDetectionConfidenceThreshold : OD_personDetectionConfidenceThreshold;
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.VEHICLE] = vehicleDetectionConfidenceThreshold;
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.BAG] = bagDetectionConfidenceThreshold;
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.ANIMAL] = animalDetectionConfidenceThreshold;
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.ELECTRONICS] = electronicsDetectionConfidenceThreshold;
            od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.FRUIT_VEGETABLE] = fruitVegetableDetectionConfidenceThreshold;
            od_runtime_params.objectClassFilter = new int[(int)sl.OBJECT_CLASS.LAST];

            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.PERSON] = Convert.ToInt32(objectClassPersonFilter);
            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.VEHICLE] = Convert.ToInt32(objectClassVehicleFilter);
            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.BAG] = Convert.ToInt32(objectClassBagFilter);
            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.ANIMAL] = Convert.ToInt32(objectClassAnimalFilter);
            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.ELECTRONICS] = Convert.ToInt32(objectClassElectronicsFilter);
            od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.FRUIT_VEGETABLE] = Convert.ToInt32(objectClassFruitVegetableFilter);

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch(); //Time how long the loading takes so we can tell the user. 
            watch.Start();

            sl.ERROR_CODE err = zedCamera.EnableObjectsDetection(ref od_param);
            if (err == sl.ERROR_CODE.SUCCESS)
            {
                Debug.Log("Object Detection module started in " + watch.Elapsed.Seconds + " seconds.");
                objectDetectionRunning = true;
            }
            else
            {
                Debug.Log("Object Detection failed to start. (Error: " + err + " )");
                objectDetectionRunning = false;
            }

            watch.Stop();

            odIsStarting = false;
        }
    }

    /// <summary>
    /// Stops the object detection.
    /// </summary>
    public void StopObjectDetection()
    {
        if (zedCamera != null && running)
        {
            zedCamera.DisableObjectsDetection();
            objectDetectionRunning = false;
        }
    }

    /// <summary>
    /// Updates the objects detection by triggering the detection event
    /// </summary>
    public void UpdateObjectsDetection()
    {
        if (!objectDetectionRunning) return;

        //Update the runtime parameters in case the user made changes. 
        //od_runtime_params.detectionConfidenceThreshold = objectDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold = new int[(int)sl.OBJECT_CLASS.LAST];
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.PERSON] = (objectDetectionModel == sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE || objectDetectionModel == sl.DETECTION_MODEL.HUMAN_BODY_FAST) ? SK_personDetectionConfidenceThreshold : OD_personDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.VEHICLE] = vehicleDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.BAG] = bagDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.ANIMAL] = animalDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.ELECTRONICS] = electronicsDetectionConfidenceThreshold;
        od_runtime_params.object_confidence_threshold[(int)sl.OBJECT_CLASS.FRUIT_VEGETABLE] = fruitVegetableDetectionConfidenceThreshold;
        od_runtime_params.objectClassFilter = new int[(int)sl.OBJECT_CLASS.LAST];
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.PERSON] = Convert.ToInt32(objectClassPersonFilter);
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.VEHICLE] = Convert.ToInt32(objectClassVehicleFilter);
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.BAG] = Convert.ToInt32(objectClassBagFilter);
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.ANIMAL] = Convert.ToInt32(objectClassAnimalFilter);
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.ELECTRONICS] = Convert.ToInt32(objectClassElectronicsFilter);
        od_runtime_params.objectClassFilter[(int)sl.OBJECT_CLASS.FRUIT_VEGETABLE] = Convert.ToInt32(objectClassFruitVegetableFilter);

        if (objectDetectionImageSyncMode == false) RetrieveObjectDetectionFrame(); //If true, this is called in the AcquireImages function in the image acquisition thread. 

        if (newobjectsframeready)
        {
            lock (zedCamera.grabLock)
            {
                float objdetect_fps = 1000000000.0f / (objectsFrameSDK.timestamp - lastObjectFrameTimeStamp);
                objDetectionModuleFPS = (objDetectionModuleFPS + objdetect_fps) / 2.0f;
                objectDetectionFPS = objDetectionModuleFPS.ToString("F1") + " FPS";
                lastObjectFrameTimeStamp = objectsFrameSDK.timestamp;
                ///Trigger the event that holds the raw data, and pass the whole objects frame.
                if (OnObjectDetection_SDKData != null)
                {
                    OnObjectDetection_SDKData(objectsFrameSDK);
                }

                //If there are any subscribers to the non-raw data, create that data and publish the event. 
                if (OnObjectDetection != null)
                {
                    DetectionFrame oldoframe = detectionFrame; //Cache so we can clean it up once we're done setting up the new one. 

                    //DetectionFrame oframe = new DetectionFrame(objectsFrame, this);
                    detectionFrame = new DetectionFrame(objectsFrameSDK, this);
                    OnObjectDetection(detectionFrame);
                    if (oldoframe != null) oldoframe.CleanUpAllObjects();
                }

                //Now that all events have been sent out, it's safe to let the image acquisition thread detect more objects. 
                requestobjectsframe = true;
                newobjectsframeready = false;
            }
        }
    }

    /// <summary>
    /// Requests the latest object detection frame information. If it's new, it'll fill the objectsFrame object
    /// with the new frame info, set requestobjectsframe to false, and set newobjectsframeready to true. 
    /// </summary>
    private void RetrieveObjectDetectionFrame()
    {
        sl.ObjectsFrameSDK oframebuffer = new sl.ObjectsFrameSDK();
        sl.ERROR_CODE res = zedCamera.RetrieveObjectsDetectionData(ref od_runtime_params, ref oframebuffer);
        if (res == sl.ERROR_CODE.SUCCESS && oframebuffer.isNew != 0)
        {
            //Release memory from masks. 
            /*for (int i = 0; i < objectsFrameSDK.numObject; i++)
            {
                sl.ZEDMat oldmat = new sl.ZEDMat(objectsFrameSDK.objectData[i].mask);
                oldmat.Free();
            }*/

            objectsFrameSDK = oframebuffer;

            requestobjectsframe = false;
            newobjectsframeready = true;
        }
    }

    /// <summary>
    /// Switchs the state of the object detection pause.
    /// </summary>
    /// <param name="state">If set to <c>true</c> state, the object detection will pause. It will resume otherwise</param>
    public void SwitchObjectDetectionPauseState(bool state)
    {
        if (zedCamera != null)
        {
            if (objectDetectionRunning)
                zedCamera.PauseObjectsDetection(state);
        }
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
        if (zedRigDisplayer != null) Destroy(zedRigDisplayer);

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
        if (hasXRDevice())
            HMDDevice = XRDevice.model;


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


    public void InitVideoSettings(VideoSettingsInitMode mode)
    {
        if(!zedCamera.IsCameraReady)
        {
            Debug.LogError("Tried to apply camera settings before ZED camera was ready.");
            return;
        }
        switch(mode)
        {
            case VideoSettingsInitMode.Custom:
                ApplyLocalVideoSettingsToZED();
                return;
            case VideoSettingsInitMode.LoadFromSDK:
            default:
                //This is the SDK's default behavior, so we don't need to specify anything. Just apply the ZED's values locally. 
                GetCurrentVideoSettings();
                return;
            case VideoSettingsInitMode.Default:
                zedCamera.ResetCameraSettings();
                GetCurrentVideoSettings();
                return;
        }
    }


    private void GetCurrentVideoSettings()
    {
        //Sets all the video setting values to the ones currently applied to the ZED. 
        videoBrightness = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS);
        videoContrast = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SATURATION);
        videoHue = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.HUE);
        videoSaturation = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SATURATION);
        videoSharpness = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.SHARPNESS);
        videoGamma = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAMMA);
        videoAutoGainExposure = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.AEC_AGC) == 1 ? true : false;
        if (!videoAutoGainExposure)
        {
            videoGain = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.GAIN);
            videoExposure = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE);
        }

        videoAutoWhiteBalance = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.AUTO_WHITEBALANCE) == 1 ? true : false;
        if (!videoAutoWhiteBalance)
        {
            videoWhiteBalance = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE);
        }

        videoLEDStatus = zedCamera.GetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS) == 1 ? true : false;
    }

    private void ApplyLocalVideoSettingsToZED()
    {
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.BRIGHTNESS, videoBrightness);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, videoContrast);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.HUE, videoHue);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, videoSaturation);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SHARPNESS, videoSharpness);
        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAMMA, videoGamma);

        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AEC_AGC, videoAutoGainExposure ? 1 : 0);
        if (!videoAutoGainExposure)
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.GAIN, videoGain);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.EXPOSURE, videoExposure);
        }

        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.AUTO_WHITEBALANCE, videoAutoWhiteBalance ? 1 : 0);
        if (!videoAutoWhiteBalance)
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.WHITEBALANCE, videoWhiteBalance);
        }

        zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.LED_STATUS, 1);
    }
#region EventHandler
    /// <summary>
    /// Changes the real-world brightness by setting the brightness value in the shaders.
    /// </summary>
    /// <param name="newVal">New brightness value to be applied. Should be between 0 and 100.</param>
    public void SetCameraBrightness(int newVal)
    {
        SetFloatValueOnPlaneMaterials("_ZEDFactorAffectReal", newVal / 100f);
    }

#if ZED_HDRP
    public void SetSelfIllumination(float newVal)
    {
        SetFloatValueOnPlaneMaterials("_SelfIllumination", newVal);
    }
#endif

    /// <summary>
    /// Sets the maximum depth range of real-world objects. Pixels further than this range are discarded. 
    /// </summary>
    /// <param name="newVal">Furthest distance, in meters, that the camera will display pixels for. Should be between 0 and 20.</param>
    public void SetMaxDepthRange(float newVal)
    {
        if (newVal < 0 || newVal > 40)
        {
            Debug.LogWarning("Tried to set max depth range to " + newVal + "m. Must be within 0m and 40m.");
            newVal = Mathf.Clamp(newVal, 0, 40);
        }
        SetFloatValueOnPlaneMaterials("_MaxDepth", newVal);
    }

    /// <summary>
    /// Sets a value of a float property on the material(s) rendering the ZED image. 
    /// Used to set things like brightness and maximum depth. 
    /// </summary>
    /// <param name="propertyname">Name of value/property within Shader. </param>
    /// <param name="newvalue">New value for the specified property.</param>
    private void SetFloatValueOnPlaneMaterials(string propertyname, float newvalue)
    {
        foreach (ZEDRenderingPlane renderPlane in GetComponentsInChildren<ZEDRenderingPlane>())
        {
            Material rendmat;
            if (renderPlane.ActualRenderingPath == RenderingPath.Forward) rendmat = renderPlane.canvas.GetComponent<Renderer>().material;
            else if (renderPlane.ActualRenderingPath == RenderingPath.DeferredShading) rendmat = renderPlane.deferredMat;
            else
            {
                Debug.LogError("Can't set " + propertyname + " value  for Rendering Path " + renderPlane.ActualRenderingPath +
                    ": only Forward and DeferredShading are supported.");
                return;
            }
            rendmat.SetFloat(propertyname, newvalue);
        }
    }
    
    private void SetBoolValueOnPlaneMaterials(string propertyname, bool newvalue)
    {
        foreach (ZEDRenderingPlane renderPlane in GetComponentsInChildren<ZEDRenderingPlane>())
        {
            Material rendmat;

            MeshRenderer rend = renderPlane.canvas.GetComponent<MeshRenderer>();
            if (!rend) continue;

            rendmat = rend.material;

            rendmat.SetInt(propertyname, newvalue ? 1 : 0);
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
            if (zedCamera.IsCameraReady && !isTrackingEnable && enableTracking)
            {
                //Enables tracking and initializes the first position of the camera.
                if (!(enableTracking = (zedCamera.EnableTracking(ref zedOrientation, ref zedPosition, enableSpatialMemory, enablePoseSmoothing, estimateInitialPosition, trackingIsStatic, 
                    enableIMUFusion, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
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
                    sl.ERROR_CODE err = zedCamera.EnableStreaming(streamingCodec, (uint)bitrate, (ushort)streamingPort, gopSize, adaptativeBitrate, chunkSize,streamingTargetFramerate);
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

