//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.XR;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// In pass-through AR mode, handles the final output to the VR headset, positioning the final images
/// to make the pass-through effect natural and comfortable. Also moves/rotates the images to
/// compensate for the ZED image's latency using our Video Asynchronous Timewarp.
/// ZEDManager attaches this component to a second stereo rig called "ZEDRigDisplayer" that it
/// creates and hides in the editor at runtime; see ZEDManager.CreateZEDRigDisplayer() to see this process.
///
/// The Timewarp effect is achieved by logging the pose of the headset each time it's available within the
/// wrapper. Then, when a ZED image is available, the wrapper looks up the headset's position using the timestamp
/// of the image, and moves the final viewing planes according to that position. In this way, the ZED's images
/// line up with real-life, even after a ~60ms latency.
/// </summary>
public class ZEDMixedRealityPlugin : MonoBehaviour
{
    #region DLL Calls
	const string nameDll = sl.ZEDCommon.NameDLL;
	[DllImport(nameDll, EntryPoint = "sl_compute_size_plane_with_gamma")]
	private static extern System.IntPtr dllz_compute_size_plane_with_gamma(int width, int height, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal);

	[DllImport(nameDll, EntryPoint = "sl_compute_hmd_focal")]
	private static extern float dllz_compute_hmd_focal(int width, int height, float w, float h);

	/*****LATENCY CORRECTOR***/
	[DllImport(nameDll, EntryPoint = "sl_latency_corrector_add_key_pose")]
	private static extern void dllz_latency_corrector_add_key_pose(ref Vector3 translation, ref Quaternion rotation, ulong timeStamp);

	[DllImport(nameDll, EntryPoint = "sl_latency_corrector_get_transform")]
	private static extern int dllz_latency_corrector_get_transform(ulong timeStamp, bool useLatency,out Vector3 translation, out Quaternion rotation);

	[DllImport(nameDll, EntryPoint = "sl_latency_corrector_initialize")]
	private static extern void dllz_latency_corrector_initialize(int device);

	[DllImport(nameDll, EntryPoint = "sl_latency_corrector_shutdown")]
	private static extern void dllz_latency_corrector_shutdown();

	/****ANTI DRIFT ***/
	[DllImport(nameDll, EntryPoint = "sl_drift_corrector_initialize")]
	public static extern void dllz_drift_corrector_initialize();

	[DllImport(nameDll, EntryPoint = "sl_drift_corrector_shutdown")]
	public static extern void dllz_drift_corrector_shutdown();

	[DllImport(nameDll, EntryPoint = "sl_drift_corrector_get_tracking_data")]
	public static extern void dllz_drift_corrector_get_tracking_data(ref TrackingData trackingData, ref Pose HMDTransform, ref Pose latencyCorrectorTransform, int hasValidTrackingPosition,bool checkDrift);

	[DllImport(nameDll, EntryPoint = "sl_drift_corrector_set_calibration_transform")]
	public static extern void dllz_drift_corrector_set_calibration_transform(ref Pose pose);

	[DllImport(nameDll, EntryPoint = "sl_drift_corrector_set_calibration_const_offset_transform")]
	public static extern void dllz_drift_corrector_set_calibration_const_offset_transform(ref Pose pose);
    #endregion

    /// <summary>
    /// Container for storing historic pose information, used by the latency corrector.
    /// </summary>
    public struct KeyPose
    {
        public Quaternion Orientation;
        public Vector3 Translation;
        public ulong Timestamp;
    };

    /// <summary>
    /// Container for position and rotation. Used when timestamps are not needed or have already
    /// been processed, such as setting the initial camera offset or updating the stereo rig's
    /// transform from data pulled from the wrapper.
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
    public struct Pose
    {
        public Vector3 translation;
        public Quaternion rotation;

        public Pose(Vector3 t, Quaternion q)
        {
            translation = t;
            rotation = q;
        }
    }

    /// <summary>
    ///
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
    public struct TrackingData
    {
        public Pose zedPathTransform;
        public Pose zedWorldTransform;
        public Pose offsetZedWorldTransform;

		public int trackingState;
	}

	/// <summary>
	/// Gameobject holding the camera in the final ZEDRigDisplayer rig, which captures the final images sent to both HMD screens.
	/// </summary>
	public GameObject finalCameraCenter;
	/// <summary>
	/// 'Intermediate' left camera GameObject, which is the one on the regular, always-visible ZED stereo rig (ZED_Rig_Stereo),
	/// usually called 'Left_eye'.
	/// </summary>
	[Tooltip("'Intermediate' left camera GameObject, which is the one on the regular, always-visible ZED stereo rig (ZED_Rig_Stereo), "
        + "usually called 'Left_eye'. ")]
    public GameObject ZEDEyeLeft;
    /// <summary>
    /// 'Intermediate' right camera GameObject, which is the one on the regular, always-visible ZED stereo rig (ZED_Rig_Stereo),
    /// usually called 'Right_eye'.
    /// </summary>
    [Tooltip("'Intermediate' right camera GameObject, which is the one on the regular, always-visible ZED stereo rig (ZED_Rig_Stereo)," +
        "usually called 'Right_eye'. ")]
    public GameObject ZEDEyeRight;

    /// <summary>
    /// 'Intermediate' left screen/canvas object in the always-visible ZED stereo rig.
    /// </summary>
    [Tooltip("")]
    public ZEDRenderingPlane leftScreen;
    /// <summary>
    /// 'Intermediate' right screen/canvas object in the always-visible ZED stereo rig.
    /// </summary>
    [Tooltip("")]
    public ZEDRenderingPlane rightScreen;

	/// <summary>
	/// Final center viewing plane/canvas object in the final ZEDRigDisplayer rig. Displays the image from the center
	/// 'intermediate' cameras (ZEDEyeRight and ZEDEyeRight) and is offset for image comfort and moved each frame for the Timewarp effect.
	/// </summary>
	[Tooltip("")]
	public Transform quadCenter;

	/// <summary>
	/// Camera object in 'finalCameraCenter', which captures the final image output to the headset's left and right screens.
	/// </summary>
	// [Tooltip("")]
	public Camera finalCenterEye;

	/// <summary>
	/// Material from the final center plane. Usually a new instance of Mat_ZED_Unlit.
	/// </summary>
	[Tooltip("Material from the final right plane. Usually a new instance of Mat_ZED_Unlit. ")]
	public Material centerMaterial;

	/// <summary>
	/// Base, pre-Timewarp offset between each final plane and its corresponding camera.
	/// </summary>
	[Tooltip("Offset between each final plane and its corresponding camera.")]
    public Vector3 offset = new Vector3(0, 0, (float)sl.Constant.PLANE_DISTANCE);

    /// <summary>
    /// Distance to set each intermediate camera from the point between them. This is half of the post-calibration
    /// distance between the ZED cameras, so X is usually very close to 0.0315m (63mm / 2).
    /// </summary>
    [Tooltip("")]
    public Vector3 halfBaselineOffset;

    /// <summary>
    /// Reference to the ZEDCamera instance, which communicates with the SDK.
    /// </summary>
    [Tooltip("Reference to the ZEDCamera instance, which communicates with the SDK.")]
    public sl.ZEDCamera zedCamera;

    /// <summary>
    /// Reference to the scene's ZEDManager instance, usually contained in ZED_Rig_Stereo.
    /// </summary>
    [Tooltip("Reference to the scene's ZEDManager instance, usually contained in ZED_Rig_Stereo.")]
    public ZEDManager manager;

    /// <summary>
    /// Flag set to true when the target textures from the ZEDRenderingPlane overlays are ready.
    /// </summary>
    [Tooltip("Flag set to true when the target textures from the ZEDRenderingPlane overlays are ready.")]
    public bool ready = false;

    /// <summary>
    /// Flag set to true when a grab is ready, used to collect a pose from the latest time possible.
    /// </summary>
    [Tooltip("Flag set to true when a grab is ready, used to collect a pose from the latest time possible.")]
    public bool grabSucceeded = false;

    /// <summary>
    /// Flag set to true when the ZED is ready (after ZEDManager.OnZEDReady is invoked).
    /// </summary>
    [Tooltip("Flag set to true when the ZED is ready (after ZEDManager.OnZEDReady is invoked).")]
    public bool zedReady = false;

    /// <summary>
    /// If a VR device is still detected. Updated each frame. Used to know if certain updates should still happen.
    /// </summary>
    private bool hasVRDevice = false;
    public bool HasVRDevice
    {
        get { return hasVRDevice; }
    }

    /// <summary>
    /// The current latency pose - the pose the headset was at when the last ZED frame was captured (based on its timestamp).
    /// </summary>
    private Pose latencyPose;

    /// <summary>
    /// The physical offset of the HMD to the ZED. Represents the offset from the approximate center of the user's
    /// head to the ZED's left sensor.
    /// </summary>
    private Pose hmdtozedCalibration;

    /// <summary>
    /// Public accessor for the physical offset of the HMD to the ZED. Represents the offset from the
    /// approximate center of the user's head to the ZED's left sensor.
    /// </summary>
	public Pose HmdToZEDCalibration
    {
        get { return hmdtozedCalibration; }
    }

    /// <summary>
    /// Whether the latency correction is ready.
    /// </summary>
    private bool latencyCorrectionReady = false;

    /// <summary>
    /// Contains the last position computed by the anti-drift.
    /// </summary>
    public TrackingData trackingData = new TrackingData();

    /// <summary>
    /// Filename of the saved HMD to ZED calibration file loaded into hmdtozedCalibration.
    /// //If it doesn't exist, it's created with hard-coded values.
    /// </summary>
    [Tooltip("")]
    [SerializeField]
    private string calibrationFile = "CalibrationZEDHMD.ini";
    /// <summary>
    /// Path of the saved HMD to ZED calibration file loaded into hmdtozedCalibration.
    /// By default, corresponds to C:/ProgramData/Stereolabs/mr.
    /// </summary>
	private string calibrationFilePath = @"Stereolabs\mr";

    /// <summary>
    /// Delegate for the OnHMDCalibChanged event.
    /// </summary>
    public delegate void OnHmdCalibrationChanged();
    /// <summary>
    /// Event invoked if the calibration file that sets the physical ZED offset is changed at runtime.
    /// Causes ZEDManger.CalibrationHasChanged() to get called, which re-initialized the ZED's position
    /// with ZEDManager.AdjustZEDRigCameraPosition() at the next tracking update.
    /// </summary>
	public static event OnHmdCalibrationChanged OnHmdCalibChanged;

	/// <summary>
	/// Cached property id for _MainTex. use the mainTexID property instead.
	/// </summary>
	private int? _maintexLeftid;
	/// <summary>
	/// Property id for _MainTex, which is the main texture from the ZED.
	/// </summary>
	private int mainTexLeftID
	{
		get
		{
			if (_maintexLeftid == null) _maintexLeftid = Shader.PropertyToID("_MainTexLeft");
			return (int)_maintexLeftid;
		}
	}
	/// <summary>
	/// Cached property id for _MainTex. use the mainTexID property instead.
	/// </summary>
	private int? _maintexRightid;
	/// <summary>
	/// Property id for _MainTex, which is the main texture from the ZED.
	/// </summary>
	private int mainTexRightID
	{
		get
		{
			if (_maintexRightid == null) _maintexRightid = Shader.PropertyToID("_MainTexRight");
			return (int)_maintexRightid;
		}
	}

	List<XRNodeState> nodeStates = new List<XRNodeState>();

    private bool hasXRDevice()
    {
#if UNITY_2020_1_OR_NEWER
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                return true;
            }
        }
        return false;
#else
        return XRDevice.isPresent;
#endif
    }

    private string getXRModelName()
    {
#if UNITY_2019_1_OR_NEWER
        return XRSettings.loadedDeviceName;
#else
        return XRDevice.model;
#endif
    }

    private void Awake()
    {
        //Initialize the latency tracking only if a supported headset is detected.
        //You can force it to work for unsupported headsets by implementing your own logic for calling
        //dllz_latency_corrector_initialize.
        hasVRDevice = hasXRDevice();

		if (hasVRDevice)
        {
            if (getXRModelName().ToLower().Contains("vive")) //Vive or Vive Pro
            {
                dllz_latency_corrector_initialize(0);
            }
            else //Oculus Rift CV1, Rift S, Windows Mixed Reality, Valve Index, etc.
            {
                dllz_latency_corrector_initialize(1);
            }

            dllz_drift_corrector_initialize();
        }
#if UNITY_2017_OR_NEWER

		nodeState.nodeType = VRNode.Head;
		nodes.Add(nodeState);
#endif
    }

    /// <summary>
    /// Sets references not set in ZEDManager.CreateZEDRigDisplayer(), sets materials,
    /// adjusts final plane scale, loads the ZED calibration offset and other misc. values.
    /// </summary>
    void Start()
	{
		hasVRDevice = hasXRDevice();


		//iterate until we found the ZED Manager parent...
		Transform ObjParent = gameObject.transform;
		int tries = 0;
		while (manager == null && tries < 50) {
			if (ObjParent!=null)
				manager= ObjParent.GetComponent<ZEDManager> ();
			if (manager == null && ObjParent!=null)
				ObjParent = ObjParent.parent;
			tries++;
		}

		if (manager != null) {
			manager.OnZEDReady += ZEDReady;
			zedCamera = manager.zedCamera;
		} else
			return;

		leftScreen = ZEDEyeLeft.GetComponent<ZEDRenderingPlane>();
		rightScreen = ZEDEyeRight.GetComponent<ZEDRenderingPlane>();
		finalCenterEye = finalCameraCenter.GetComponent<Camera>();
		centerMaterial = quadCenter.GetComponent<Renderer>().material;
		finalCenterEye.SetReplacementShader(centerMaterial.shader, "");

		finalCenterEye.depth = 0;

		float plane_dist = (float)sl.Constant.PLANE_DISTANCE;
		scale(quadCenter.gameObject, new Vector2(1.78f * plane_dist, 1.0f * plane_dist));

		zedReady = false;

#if ZED_HDRP || ZED_URP
        RenderPipelineManager.beginFrameRendering += SRPStartFrame;
#else
		Camera.onPreRender += PreRender;
#endif

		LoadHmdToZEDCalibration();
	}

	/// <summary>
	/// Computes the size of the final planes.
	/// </summary>
	/// <param name="resolution">ZED's current resolution. Usually 1280x720.</param>
	/// <param name="perceptionDistance">Typically 1.</param>
	/// <param name="eyeToZedDistance">Distance from your eye to the camera. Estimated at 0.1m.</param>
	/// <param name="planeDistance">Distance to final quad (quadLeft or quadRight). Arbitrary but set by offset.z.</param>
	/// <param name="HMDFocal">Focal length of the HMD, retrieved from the wrapper.</param>
	/// <param name="zedFocal">Focal length of the ZED, retrieved from the camera's rectified calibration parameters.</param>
	/// <returns></returns>
	public Vector2 ComputeSizePlaneWithGamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal)
	{
		System.IntPtr p = dllz_compute_size_plane_with_gamma((int)resolution.width, (int)resolution.height, perceptionDistance, eyeToZedDistance, planeDistance, HMDFocal, zedFocal);

		if (p == System.IntPtr.Zero)
		{
			return new Vector2();
		}
		Vector2 parameters = (Vector2)Marshal.PtrToStructure(p, typeof(Vector2));
		return parameters;

	}

	/// <summary>
	/// Compute the focal length of the HMD.
	/// </summary>
	/// <param name="targetSize">Resolution of the headset's eye textures.</param>
	/// <returns></returns>
	public float ComputeFocal(sl.Resolution targetSize)
	{
		float focal_hmd = dllz_compute_hmd_focal((int)targetSize.width, (int)targetSize.height, finalCenterEye.projectionMatrix.m00, finalCenterEye.projectionMatrix.m11);
		return focal_hmd;
	}

    /// <summary>
    /// Called once the ZED is finished initializing. Subscribed to ZEDManager.OnZEDReady in OnEnable.
    /// Uses the newly-available ZED parameters to scale the final planes (quadLeft and quadRight) to appear
    /// properly in the currently-connected headset.
    /// </summary>
	void ZEDReady()
	{
		Vector2 scaleFromZED;
		halfBaselineOffset.x = zedCamera.Baseline / 2.0f;

		float perception_distance = 1.0f;
		float zed2eye_distance = 0.1f; //Estimating 10cm between your eye and physical location of the ZED Mini.
		hasVRDevice = hasXRDevice();

		if (hasVRDevice) {
			sl.CalibrationParameters parameters = zedCamera.CalibrationParametersRectified;
			scaleFromZED = ComputeSizePlaneWithGamma (new sl.Resolution ((uint)zedCamera.ImageWidth, (uint)zedCamera.ImageHeight),
				perception_distance, zed2eye_distance, offset.z,
				ComputeFocal (new sl.Resolution ((uint)XRSettings.eyeTextureWidth, (uint)XRSettings.eyeTextureHeight)),
				parameters.leftCam.fx);

			scale(quadCenter.gameObject, scaleFromZED);
		}

        ready = false;

        // If using Vive, change ZED's settings to compensate for different screen.
        if (getXRModelName().ToLower().Contains("vive"))
        {
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.CONTRAST, 3);
            zedCamera.SetCameraSettings(sl.CAMERA_SETTINGS.SATURATION, 3);
        }

		//Set eye layers to respective eyes.
		finalCenterEye.stereoTargetEye = StereoTargetEyeMask.Both;

        /// AR Passtrough is recommended in 1280x720 at 60, due to FoV, FPS, etc.
        /// If not set to this resolution, warn the user.
        if (zedCamera.ImageWidth != 1280 && zedCamera.ImageHeight != 720)
            Debug.LogWarning("[ZED AR Passthrough] This resolution is not ideal for a proper AR passthrough experience. Recommended resolution is 1280x720.");

		zedReady = true;
	}

    public void OnEnable()
    {
        latencyCorrectionReady = false;
        if (manager != null)
            manager.OnZEDReady += ZEDReady;
    }

    public void OnDisable()
    {
        latencyCorrectionReady = false;
        if (manager != null)
            manager.OnZEDReady -= ZEDReady;
    }

    void OnGrab()
    {
        grabSucceeded = true;
    }

    /// <summary>
    /// Collects the position of the HMD with a timestamp, to be looked up later to correct for latency.
    /// </summary>
    public void CollectPose()
    {
        if (manager == null)
            return;

        KeyPose k = new KeyPose();

        InputTracking.GetNodeStates(nodeStates);
        XRNodeState nodeState = nodeStates.Find(node => node.nodeType == XRNode.Head);
        nodeState.TryGetRotation(out k.Orientation);
        nodeState.TryGetPosition(out k.Translation);

        if (manager.zedCamera.IsCameraReady)
        {
            k.Timestamp = manager.zedCamera.GetCurrentTimeStamp();
            if (k.Timestamp >= 0)
            {
                dllz_latency_corrector_add_key_pose(ref k.Translation, ref k.Orientation, k.Timestamp); //Poses are handled by the wrapper.
            }
        }
    }

    /// <summary>
    /// Returns a pose at a specific time.
    /// </summary>
    /// <param name="r">Rotation of the latency pose.</param>
    /// <param name="t">Translation/position of the latency pose.</param>
    /// <param name="cameraTimeStamp">Timestamp for looking up the pose.</param>
    /// <param name="useLatency">Whether to use latency.</param>
    public int LatencyCorrector(out Quaternion r, out Vector3 t, ulong cameraTimeStamp, bool useLatency)
    {
        return dllz_latency_corrector_get_transform(cameraTimeStamp, useLatency, out t, out r);
    }

    /// <summary>
    /// Sets the GameObject's 3D local scale based on a 2D resolution (Z scale is unchanged).
    /// Used for scaling quadLeft/quadRight.
    /// </summary>
    /// <param name="screen">Target GameObject to scale.</param>
    /// <param name="s">2D scale factor.</param>
	public void scale(GameObject screen,  Vector2 s)
	{
		screen.transform.localScale = new Vector3(s.x, s.y, 1);
	}

    /// <summary>
    /// Set the planes/canvases to the proper position after accounting for latency.
    /// </summary>
    public void UpdateRenderPlane()
    {
        if (manager == null)
            return;

        if (!manager.IsStereoRig)
            return; //Make sure we're in pass-through AR mode.

#if UNITY_2019_3_OR_NEWER
        List<InputDevice> eyes = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, eyes);

        if (eyes.Count > 0) // if a headset is detected
        {
            var eye = eyes[0];
			eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 centerEyePosition);
			eye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion centerEyeRotation);

			finalCenterEye.transform.localPosition = centerEyePosition;
			finalCenterEye.transform.localRotation = centerEyeRotation;
		}
#endif

        Quaternion r;

        //Modified code to ensure view in HMD does not play like a movie screen
        if (manager.inputType == sl.INPUT_TYPE.INPUT_TYPE_SVO || manager.inputType == sl.INPUT_TYPE.INPUT_TYPE_STREAM)
        {
            r = finalCameraCenter.transform.localRotation;
        }
        else
        {
            r = latencyPose.rotation;
        }
        // End of modified code

		//Plane's distance from the final camera never changes, but it's rotated around it based on the latency pose.
		quadCenter.localRotation = r;
		quadCenter.localPosition = finalCenterEye.transform.localPosition + r * (offset);
	}

    /// <summary>
    /// Initialize the ZED's tracking with the current HMD position and HMD-ZED calibration.
    /// This causes the ZED's internal tracking to start where the HMD is, despite being initialized later than the HMD.
    /// </summary>
    /// <returns>Initial offset for the ZED's tracking. </returns>
    public Pose InitTrackingAR()
    {
        if (manager == null)
            return new Pose();

        Transform tmpHMD = transform;

        InputTracking.GetNodeStates(nodeStates);
        XRNodeState nodeState = nodeStates.Find(node => node.nodeType == XRNode.Head);
        nodeState.TryGetRotation(out Quaternion rot);
        nodeState.TryGetPosition(out Vector3 pos);
        Pose hmdTransform = new Pose(pos, rot);

        Quaternion r = Quaternion.identity;
        Vector3 t = Vector3.zero;
        Pose const_offset = new Pose(t, r);
        dllz_drift_corrector_set_calibration_const_offset_transform(ref const_offset);

        zedCamera.ResetTrackingWithOffset(tmpHMD.rotation, tmpHMD.position, HmdToZEDCalibration.rotation, HmdToZEDCalibration.translation);

        return new Pose(tmpHMD.position, tmpHMD.rotation);
    }

    /// <summary>
    /// Sets latencyPose to the pose of the headset at a given timestamp and flags whether or not it's valid for use.
    /// </summary>
    /// <param name="cameraTimeStamp">Timestamp for looking up the pose.</param>
	public void ExtractLatencyPose(ulong cameraTimeStamp)
    {
        Quaternion latency_rot;
        Vector3 latency_pos;
        if (LatencyCorrector(out latency_rot, out latency_pos, cameraTimeStamp, true) == 1)
        {
            latencyPose = new Pose(latency_pos, latency_rot);
            latencyCorrectionReady = true;
        }
        else
            latencyCorrectionReady = false;
    }

    /// <summary>
    /// Returns the most recently retrieved latency pose.
    ///
    /// </summary>
    /// <returns>Last retrieved latency pose. </returns>
	public Pose LatencyPose()
    {
        return latencyPose;
    }

    /// <summary>
    /// Gets the proper position of the ZED virtual camera, factoring in HMD offset, latency, and anti-drift.
    /// Used by ZEDManager to set the pose of Camera_eyes in the 'intermediate' rig (ZED_Rig_Stereo).
    /// </summary>
    /// <param name="position">Current position as returned by the ZED's tracking.</param>
    /// <param name="orientation">Current rotation as returned by the ZED's tracking.</param>
    /// <param name="r">Final rotation.</param>
    /// <param name="t">Final translation/position.</param>
	public void AdjustTrackingAR(Vector3 position, Quaternion orientation, out Quaternion r, out Vector3 t, bool setimuprior)
    {
        hasVRDevice = hasXRDevice();

        InputTracking.GetNodeStates(nodeStates);
        XRNodeState nodeState = nodeStates.Find(node => node.nodeType == XRNode.Head);
        nodeState.TryGetRotation(out Quaternion rot);
        nodeState.TryGetPosition(out Vector3 pos);
        Pose hmdTransform = new Pose(pos, rot);

        trackingData.trackingState = (int)manager.ZEDTrackingState; //Whether the ZED's tracking is currently valid (not off or unable to localize).
        trackingData.zedPathTransform = new Pose(position, orientation);

        if (zedReady && latencyCorrectionReady && setimuprior == true)
        {
            zedCamera.SetIMUOrientationPrior(ref latencyPose.rotation);
        }

        dllz_drift_corrector_get_tracking_data(ref trackingData, ref hmdTransform, ref latencyPose, 0, true);
        r = trackingData.offsetZedWorldTransform.rotation;
        t = trackingData.offsetZedWorldTransform.translation;
    }

    /// <summary>
    /// Close related ZED processes when the application ends.
    /// </summary>
	private void OnApplicationQuit()
    {
        dllz_latency_corrector_shutdown();
        dllz_drift_corrector_shutdown();
    }

    /// <summary>
    /// Collects poses for latency correction, and updates the position of the rendering plane.
    /// Also assigns textures from 'intermediate' cameras to the final quads' materials if ready and not done yet.
    /// Called from ZEDManager.LateUpdate() so that it happens each frame after other tracking processes have finished.
    /// </summary>
	public void LateUpdateHmdRendering()
	{
		if (!ready) //Make sure intermediate cameras are rendering to the quad's materials.
		{
			if (leftScreen.target != null && leftScreen.target.IsCreated())
			{
				centerMaterial.SetTexture(mainTexLeftID, leftScreen.target);
				ready = true;
			}
			else ready = false;
			if (rightScreen.target != null && rightScreen.target.IsCreated())
			{
				centerMaterial.SetTexture(mainTexRightID, rightScreen.target);
				ready = true;
			}
			else ready = false;
		}

		if (hasVRDevice) //Do nothing if we no longer have a HMD connected.
		{
			CollectPose (); //File the current HMD pose into the latency poses to reference later.
			UpdateRenderPlane(); //Reposition the final quads based on the latency pose.
		}
	}

#if ZED_HDRP || ZED_URP
    private void SRPStartFrame(ScriptableRenderContext context, Camera[] cams)
    {
        foreach(Camera cam in cams) {
            if (cam == finalCenterEye)
            {

                if ((!manager.IsZEDReady && manager.IsStereoRig))
                {
                    System.Collections.Generic.List<XRNodeState> nodeStates = new System.Collections.Generic.List<XRNodeState>();
                    InputTracking.GetNodeStates(nodeStates);
                    XRNodeState nodeState = nodeStates.Find(node => node.nodeType == XRNode.Head);
                    nodeState.TryGetRotation(out Quaternion rot);
                    nodeState.TryGetPosition(out Vector3 pos);

                    quadCenter.localRotation = rot;
                    quadCenter.localPosition = pos + quadCenter.localRotation * offset;
                }
            }
        }
    }
#else
	/// <summary>
	/// Before the ZED is ready, lock the quads in front of the cameras as latency correction isn't available yet.
	/// This allows us to see the loading messages (and other virtual objects if desired) while the ZED is still loading.
	/// Called by Camera.OnPreRender anytime any camera renders.
	/// </summary>
	/// <param name="cam">Cam.</param>
	public void PreRender(Camera cam)
	{
		if (cam == finalCenterEye)
		{

			if ((!manager.IsZEDReady && manager.IsStereoRig))
			{
				System.Collections.Generic.List<XRNodeState> nodeStates = new System.Collections.Generic.List<XRNodeState>();
				InputTracking.GetNodeStates(nodeStates);
				XRNodeState nodeState = nodeStates.Find(node => node.nodeType == XRNode.Head);
				nodeState.TryGetRotation(out Quaternion rot);
				nodeState.TryGetPosition(out Vector3 pos);

				quadCenter.localRotation = rot;
				quadCenter.localPosition = pos + quadCenter.localRotation * offset;
			}
		}
	}
#endif

	/// <summary>
	/// Loads the HMD to ZED calibration file and applies it to the hmdtozedCalibration offset.
	/// Note that the file it loads is created using hard-coded values
	/// and the ZED plugin doesn't ever change it. See CreateDefaultCalibrationFile().
	/// </summary>
	public void LoadHmdToZEDCalibration()
	{
		if (hasVRDevice) {
			/// Default calibration (may be changed)
			hmdtozedCalibration.rotation = Quaternion.identity;
			hmdtozedCalibration.translation.x = 0.0315f;//-zedCamera.Baseline/2;
			hmdtozedCalibration.translation.y = 0.0f;
			hmdtozedCalibration.translation.z = 0.11f;


            //if a calibration exists then load it
            //should be in ProgramData/stereolabs/mr/calibration.ini
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            string specificFolder = Path.Combine(folder, @"Stereolabs\mr");
            calibrationFilePath = Path.Combine(specificFolder, calibrationFile);


            // Check if folder exists and if not, create it
            if (!Directory.Exists(specificFolder))
            {
                Directory.CreateDirectory(specificFolder);
            }

            // Check if file exist and if not, create a default one
            if (!ParseCalibrationFile(calibrationFilePath))
                CreateDefaultCalibrationFile(calibrationFilePath);

            // Set the calibration in mr processing
            dllz_drift_corrector_set_calibration_transform(ref hmdtozedCalibration);

            // Create a file system watcher for online modifications
            CreateFileWatcher(specificFolder);
        }
    }

    /// <summary>
    /// Creates a FileSystemEventHandler to watch the HMD-ZED calibration file and update settings if
    /// it changes during runtime. If it does, calls OnChanged to fix tracking.
    /// </summary>
    /// <param name="folder"></param>
	public void CreateFileWatcher(string folder)
    {
        // Create a new FileSystemWatcher and set its properties.
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = folder;
        /* Watch for changes in LastAccess and LastWrite times, and
           the renaming of files or directories. */
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
            | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        // Only watch text files.
        watcher.Filter = calibrationFile;

        // Add event handlers.
        watcher.Changed += new FileSystemEventHandler(OnChanged);

        // Begin watching.
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Reloads ZED-HMD offset calibration file and resets calibration accordintly.
    /// Also calls OnHmdCalibChanged() which ZEDManager uses to run additional reset logic.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        if (hasVRDevice)
        {
            ParseCalibrationFile(calibrationFilePath);
            dllz_drift_corrector_set_calibration_transform(ref hmdtozedCalibration);
            OnHmdCalibChanged();
        }
    }

    /// <summary>
    /// Creates and saves a text file with the default ZED-HMD offset calibration parameters, to be loaded anytime this class runs in the future.
    /// Values correspond to the distance from the center of the user's head to the ZED's left sensor.
    /// </summary>
    /// <param name="path">Path to save the file.</param>
	private void CreateDefaultCalibrationFile(string path)
    {
        //Default Calibration: DO NOT CHANGE.
        hmdtozedCalibration.rotation = Quaternion.identity;
        hmdtozedCalibration.translation.x = -0.0315f;
        hmdtozedCalibration.translation.y = 0.0f;
        hmdtozedCalibration.translation.z = 0.115f;

        //Write calibration file using default calibration.
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
        {
            string node = "[HMD]";
            string tx = "tx=" + hmdtozedCalibration.translation.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Translation x";
            string ty = "ty=" + hmdtozedCalibration.translation.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Translation y";
            string tz = "tz=" + hmdtozedCalibration.translation.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Translation z";
            string rx = "rx=" + hmdtozedCalibration.rotation.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion x";
            string ry = "ry=" + hmdtozedCalibration.rotation.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion y";
            string rz = "rz=" + hmdtozedCalibration.rotation.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion z";
            string rw = "rw=" + hmdtozedCalibration.rotation.w.ToString(System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion w";

            file.WriteLine(node);
            file.WriteLine(tx);
            file.WriteLine(ty);
            file.WriteLine(tz);
            file.WriteLine(rx);
            file.WriteLine(ry);
            file.WriteLine(rz);
            file.WriteLine(rw);

            file.Close();
        }
    }

    /// <summary>
    /// Reads the ZED-HMD offset calibration file, if it exists, and loads calibration values to be applied to the final cameras.
    /// Values correspond to the distance from the center of the user's head to the ZED's left sensor.
    /// </summary>
    /// <param name="path">Path to save the file.</param>
    /// <returns>False if the file couldn't be loaded, whether empty, non-existant, etc.</returns>
	private bool ParseCalibrationFile(string path)
    {
        if (!System.IO.File.Exists(path)) return false;

        string[] lines = null;
        try
        {
            lines = System.IO.File.ReadAllLines(path);
        }
        catch (System.Exception)
        {
            return false;
        }
        if (lines.Length == 0)
            return false;

        //Default to these values (which are the same ones put in the calibration file by default).
        hmdtozedCalibration.rotation = Quaternion.identity;
        hmdtozedCalibration.translation.x = -0.0315f;
        hmdtozedCalibration.translation.y = 0.0f;
        hmdtozedCalibration.translation.z = 0.115f;

        foreach (string line in lines)
        {
            string[] splittedLine = line.Split('=');

            if (splittedLine != null && splittedLine.Length >= 2)
            {
                string key = splittedLine[0];
                string field = splittedLine[1].Split(' ')[0];

                if (key == "tx")
                {
                    hmdtozedCalibration.translation.x = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "ty")
                {
                    hmdtozedCalibration.translation.y = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "tz")
                {
                    hmdtozedCalibration.translation.z = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "rx")
                {
                    hmdtozedCalibration.rotation.x = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "ry")
                {
                    hmdtozedCalibration.rotation.y = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "rz")
                {
                    hmdtozedCalibration.rotation.z = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (key == "rw")
                {
                    hmdtozedCalibration.rotation.w = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }


        //Check if the calibration has values but they're all zeros.
        if (hmdtozedCalibration.translation.x == 0.0f && hmdtozedCalibration.translation.y == 0.0f && hmdtozedCalibration.translation.z == 0.0f)
        {
            CreateDefaultCalibrationFile(path);
        }

        return true;
    }
}
