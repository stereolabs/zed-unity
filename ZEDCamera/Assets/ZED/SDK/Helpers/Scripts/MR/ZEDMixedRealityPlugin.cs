//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.VR;
using System.IO;

/// <summary>
/// Manages the two finals cameras
/// </summary>
public class ZEDMixedRealityPlugin : MonoBehaviour
{
	const string nameDll = "sl_unitywrapper";
	[DllImport(nameDll, EntryPoint = "dllz_compute_size_plane_with_gamma")]
	private static extern System.IntPtr dllz_compute_size_plane_with_gamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal);

	[DllImport(nameDll, EntryPoint = "dllz_compute_hmd_focal")]
	private static extern float dllz_compute_hmd_focal(sl.Resolution r, float w, float h);

	/*****LATENCY CORRECTOR***/
	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_add_key_pose")]
	private static extern void dllz_latency_corrector_add_key_pose(ref Vector3 translation, ref Quaternion rotation, ulong timeStamp);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_get_transform")]
	private static extern int dllz_latency_corrector_get_transform(ulong timeStamp, bool useLatency,out Vector3 translation, out Quaternion rotation);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_initialize")]
	private static extern void dllz_latency_corrector_initialize(int device);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_shutdown")]
	private static extern void dllz_latency_corrector_shutdown();

	/****ANTI DRIFT ***/
	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_initialize")]
	public static extern void dllz_drift_corrector_initialize();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_shutdown")]
	public static extern void dllz_drift_corrector_shutdown();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_get_tracking_data")]
	public static extern void dllz_drift_corrector_get_tracking_data(ref TrackingData trackingData, ref Pose HMDTransform, ref Pose latencyCorrectorTransform, int hasValidTrackingPosition,bool checkDrift);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_transform")]
	public static extern void dllz_drift_corrector_set_calibration_transform(ref Pose pose);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_const_offset_transform")]
	public static extern void dllz_drift_corrector_set_calibration_const_offset_transform(ref Pose pose);

	/// <summary>
	/// Container for the latency corrector
	/// </summary>
	public struct KeyPose
	{
		public Quaternion Orientation;
		public Vector3 Translation;
		public ulong Timestamp;
	};


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

	[StructLayout(LayoutKind.Sequential)]
	public struct TrackingData
	{
		public Pose zedPathTransform;
		public Pose zedWorldTransform;
		public Pose offsetZedWorldTransform;

		public int trackingState;
	}
	/// <summary>
	/// Final GameObject Left
	/// </summary>
	public GameObject finalCameraLeft;
	/// <summary>
	/// Final GameObject right
	/// </summary>
	public GameObject finalCameraRight;

	/// <summary>
	/// Intermediate camera left
	/// </summary>
	public GameObject ZEDEyeLeft;
	/// <summary>
	/// Inytermediate camera right
	/// </summary>
	public GameObject ZEDEyeRight;

	/// <summary>
	/// Intermediate Left screen
	/// </summary>
	public ZEDRenderingPlane leftScreen;
	/// <summary>
	/// Intermediate right screen
	/// </summary>
	public ZEDRenderingPlane rightScreen;

	/// <summary>
	/// Final quad left
	/// </summary>
	public Transform quadLeft;
	/// <summary>
	/// Final quad right
	/// </summary>
	public Transform quadRight;

	/// <summary>
	/// Final camera left
	/// </summary>
	public Camera finalLeftEye;
	/// <summary>
	/// Final camera right
	/// </summary>
	public Camera finalRightEye;

	/// <summary>
	/// Material from the final right plane
	/// </summary>
	public Material rightMaterial;
	/// <summary>
	/// Material from the final left plane
	/// </summary>
	public Material leftMaterial;

	/// <summary>
	/// Offset between the final plane and the camera
	/// </summary>
	public Vector3 offset = new Vector3(0, 0, (float)sl.Constant.PLANE_DISTANCE);

	/// <summary>
	/// Half baseilne offset to set betwwen the two intermediate cameras
	/// </summary>
	public Vector3 halfBaselineOffset;

	/// <summary>
	/// Reference to the ZEDCamera instance
	/// </summary>
	public sl.ZEDCamera zedCamera;

	/// <summary>
	/// Reference to the ZEDManager
	/// </summary>
	public ZEDManager manager;

	/// <summary>
	/// Flag set to true when the target textures from the overlays are ready
	/// </summary>
	public bool ready = false;

	/// <summary>
	/// Flag grab ready, used to collect pose the latest time possible
	/// </summary>
	public bool grabSucceeded = false;

	/// <summary>
	/// Flag the ZED is ready
	/// </summary>
	public bool zedReady = false;

	/// <summary>
	/// Flag is VRDevice is detected. Updated at every frame
	/// </summary>
	private bool hasVRDevice = false;
	public bool HasVRDevice {
		get { return hasVRDevice; } 
	}

	/// <summary>
	/// The latency pose.
	/// </summary>
	private Pose latencyPose;

	/// <summary>
	/// HMD / ZED calibration
	/// </summary>
	private Pose hmdtozedCalibration;
	public Pose HmdToZEDCalibration {
		get { return hmdtozedCalibration; } 
	}

	/// <summary>
	/// Check if the latency correction is ready
	/// </summary>
	private bool latencyCorrectionReady = false;

	/// <summary>
	/// Contains the last position computed by the anti drift
	/// </summary>
	public TrackingData trackingData = new TrackingData();

	[SerializeField]
	private string calibrationFile = "CalibrationZEDHMD.ini";
	private string calibrationFilePath = @"Stereolabs\mr";


	private RenderTexture finalLeftTexture;



	/// <summary>
	/// Events / Delegate Action (when ZED is ready)
	/// </summary>
	public delegate void OnHmdCalibrationChanged();
	public static event OnHmdCalibrationChanged OnHdmCalibChanged;



	#if UNITY_2017_OR_NEWER
	List<UnityEngine.VR.VRNodeState> nodes = new List<UnityEngine.VR.VRNodeState>();

	UnityEngine.VR.VRNodeState nodeState = new UnityEngine.VR.VRNodeState();
	#endif
	private void Awake()
	{
		hasVRDevice = VRDevice.isPresent;
		if (hasVRDevice) {
			if (VRDevice.model.ToLower().Contains ("vive")) 
				dllz_latency_corrector_initialize (0);
			else if (VRDevice.model.ToLower().Contains ("oculus"))
				dllz_latency_corrector_initialize (1);
	
			dllz_drift_corrector_initialize ();
		}
		#if UNITY_2017_OR_NEWER

		nodeState.nodeType = VRNode.Head;
		nodes.Add(nodeState);
		#endif
	}

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start()
	{
		hasVRDevice = VRDevice.isPresent;
		manager = transform.parent.GetComponent<ZEDManager>();
		zedCamera = sl.ZEDCamera.GetInstance();
		leftScreen = ZEDEyeLeft.GetComponent<ZEDRenderingPlane>();
		rightScreen = ZEDEyeRight.GetComponent<ZEDRenderingPlane>();
		finalLeftEye = finalCameraLeft.GetComponent<Camera>();
		finalRightEye = finalCameraRight.GetComponent<Camera>();

		rightMaterial = quadRight.GetComponent<Renderer>().material;
		leftMaterial = quadLeft.GetComponent<Renderer>().material;
		finalLeftEye.SetReplacementShader(leftMaterial.shader, "");
		finalRightEye.SetReplacementShader(rightMaterial.shader, "");

		float plane_dist = (float)sl.Constant.PLANE_DISTANCE;
		scale(quadLeft.gameObject, finalLeftEye, new Vector2(1.78f*plane_dist, 1.0f*plane_dist));
		scale(quadRight.gameObject, finalRightEye, new Vector2(1.78f*plane_dist, 1.0f*plane_dist));
		zedReady = false;
		Camera.onPreRender += PreRender;

		LoadHmdToZEDCalibration();

	}

	/// <summary>
	/// Compute the size of the final planes
	/// </summary>
	/// <param name="resolution"></param>
	/// <param name="perceptionDistance"></param>
	/// <param name="eyeToZedDistance"></param>
	/// <param name="planeDistance"></param>
	/// <param name="HMDFocal"></param>
	/// <param name="zedFocal"></param>
	/// <returns></returns>
	public Vector2 ComputeSizePlaneWithGamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal)
	{
		System.IntPtr p = dllz_compute_size_plane_with_gamma(resolution, perceptionDistance, eyeToZedDistance, planeDistance, HMDFocal, zedFocal);

		if (p == System.IntPtr.Zero)
		{
			return new Vector2();
		}
		Vector2 parameters = (Vector2)Marshal.PtrToStructure(p, typeof(Vector2));
		return parameters;

	}

	/// <summary>
	/// Compute the focal
	/// </summary>
	/// <param name="targetSize"></param>
	/// <returns></returns>
	public float ComputeFocal(sl.Resolution targetSize)
	{
		float focal_hmd = dllz_compute_hmd_focal(targetSize, finalLeftEye.projectionMatrix.m00,finalLeftEye.projectionMatrix.m11);
		return focal_hmd;
	}

	void ZEDReady()
	{
		Vector2 scaleFromZED;
		halfBaselineOffset.x = zedCamera.Baseline / 2.0f;

		float perception_distance = 1.0f;
		float zed2eye_distance = 0.1f;
		hasVRDevice = VRDevice.isPresent;

		if (hasVRDevice) {
			sl.CalibrationParameters parameters = zedCamera.CalibrationParametersRectified;

			scaleFromZED = ComputeSizePlaneWithGamma (new sl.Resolution ((uint)zedCamera.ImageWidth, (uint)zedCamera.ImageHeight),
				perception_distance, zed2eye_distance, offset.z,
				ComputeFocal (new sl.Resolution ((uint)UnityEngine.VR.VRSettings.eyeTextureWidth, (uint)UnityEngine.VR.VRSettings.eyeTextureHeight)),//571.677612f,
				parameters.leftCam.fx);

			scale (quadLeft.gameObject, finalLeftEye, scaleFromZED);
			scale (quadRight.gameObject, finalRightEye, scaleFromZED);
			ready = false;

		}


		// Only for vive... some image adjustment
		if (VRDevice.model.ToLower().Contains ("vive")) {
			zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.CONTRAST, 3);
			zedCamera.SetCameraSettings (sl.CAMERA_SETTINGS.SATURATION, 3);
		}


        //Set eye layers to respective eyes. They were each set to Both during the loading screen to avoid one eye going blank at some rotations. 
		finalLeftEye.stereoTargetEye = StereoTargetEyeMask.Left;
		finalRightEye.stereoTargetEye = StereoTargetEyeMask.Right;

		/// AR Passtrough is recommended in 1280x720 at 60, due to fov, fps, etc...;
		/// Set Warning for user
		if (zedCamera.ImageWidth != 1280 && zedCamera.ImageHeight != 720)
			Debug.LogWarning ("[ZED AR Passthrough] This resolution is not compatible with proper AR passthrough experience");

		zedReady = true;

	}

	public void OnEnable()
	{
		latencyCorrectionReady = false;
		ZEDManager.OnZEDReady += ZEDReady;
	}

	public void OnDisable()
	{
		latencyCorrectionReady = false;
		ZEDManager.OnZEDReady -= ZEDReady;
	}

	void OnGrab()
	{
		grabSucceeded = true;
	}

	/// <summary>
	/// Collect positions used in the latency corrector
	/// </summary>
	public void CollectPose()
	{
		KeyPose k = new KeyPose();
		k.Orientation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.Head);
		k.Translation = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.Head);
		if (sl.ZEDCamera.GetInstance().IsCameraReady)
		{
			k.Timestamp = sl.ZEDCamera.GetInstance().GetCurrentTimeStamp();
			if (k.Timestamp >= 0)
			{
				dllz_latency_corrector_add_key_pose(ref k.Translation, ref k.Orientation, k.Timestamp);
			}
		}
	}

	/// <summary>
	/// Returns a pose at a specific time
	/// </summary>
	/// <param name="r"></param>
	/// <param name="t"></param>
	public int LatencyCorrector(out Quaternion r, out Vector3 t, ulong cameraTimeStamp,bool useLatency)
	{
		return dllz_latency_corrector_get_transform(cameraTimeStamp,useLatency,out t, out r);
	}

	public void scale(GameObject screen, Camera cam, Vector2 s)
	{
		screen.transform.localScale = new Vector3(s.x, s.y, 1);
	}

	/// <summary>
	/// Set the pose to the final planes with the latency corrector
	/// </summary>
	public void UpdateRenderPlane()
	{
		if (!ZEDManager.IsStereoRig) return;

		Quaternion r;
		r = latencyPose.rotation;
	 
		quadLeft.localRotation = r;
		quadLeft.localPosition = finalLeftEye.transform.localPosition + r * (offset);
		quadRight.localRotation = r;
		quadRight.localPosition = finalRightEye.transform.localPosition + r * (offset);

	}

	/// <summary>
	/// Init the tracking with the HMD IMU
	/// </summary>
	/// <returns></returns>
	public Pose InitTrackingAR()
	{
		Transform tmpHMD = transform;
		tmpHMD.position = InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.Head);
		tmpHMD.rotation = InputTracking.GetLocalRotation (UnityEngine.VR.VRNode.Head);

		Quaternion r = Quaternion.identity;
		Vector3 t = Vector3.zero;
		Pose const_offset = new Pose(t, r);
		dllz_drift_corrector_set_calibration_const_offset_transform(ref const_offset);

		zedCamera.ResetTrackingWithOffset(tmpHMD.rotation,tmpHMD.position,HmdToZEDCalibration.rotation,HmdToZEDCalibration.translation);

		return new Pose(tmpHMD.position, tmpHMD.rotation);
	}


	public void ExtractLatencyPose(ulong cameraTimeStamp)
	{
		Quaternion latency_rot;
		Vector3 latency_pos;
		if (LatencyCorrector (out latency_rot, out latency_pos, cameraTimeStamp, true) == 1) {
			latencyPose = new Pose (latency_pos, latency_rot);
			latencyCorrectionReady = true;
		} else
			latencyCorrectionReady = false;
	}

	public Pose LatencyPose()
	{
		return latencyPose;
	}

	public void AdjustTrackingAR(Vector3 position, Quaternion orientation, out Quaternion r, out Vector3 t)
	{
		hasVRDevice = VRDevice.isPresent;
	
		Pose hmdTransform = new Pose(InputTracking.GetLocalPosition(VRNode.Head), InputTracking.GetLocalRotation(VRNode.Head));
		trackingData.trackingState = (int)manager.ZEDTrackingState;
		trackingData.zedPathTransform = new Pose (position, orientation);

		if (zedReady && latencyCorrectionReady) {
			zedCamera.SetIMUOrientationPrior (ref latencyPose.rotation);
		}

		dllz_drift_corrector_get_tracking_data (ref trackingData, ref hmdTransform, ref latencyPose, 0, true);
		r = trackingData.offsetZedWorldTransform.rotation;
		t = trackingData.offsetZedWorldTransform.translation;
	}


	private void OnApplicationQuit()
	{
		dllz_latency_corrector_shutdown();
		dllz_drift_corrector_shutdown();
	}

	public void LateUpdateHmdRendering()
	{

		if (!ready)
		{
			if (leftScreen.target != null && leftScreen.target.IsCreated())
			{
				leftMaterial.SetTexture("_MainTex", leftScreen.target);
				ready = true;
			}
			else ready = false;
			if (rightScreen.target != null && rightScreen.target.IsCreated())
			{
				rightMaterial.SetTexture("_MainTex", rightScreen.target);
				ready = true;
			}
			else ready = false;
		}


		if (hasVRDevice)
		{
			CollectPose ();
			UpdateRenderPlane();
		}
	}


	/// <summary>
	/// Update Before ZED is actually ready
	/// </summary>
	/// <param name="cam">Cam.</param>
	public void PreRender(Camera cam)
	{
		if (cam == finalLeftEye || cam == finalRightEye)
		{
			if ((!zedReady && ZEDManager.IsStereoRig))
			{
				quadLeft.localRotation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.Head);
				quadLeft.localPosition = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.Head) + quadLeft.localRotation * offset;

				quadRight.localRotation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.Head);
				quadRight.localPosition = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.Head) + quadRight.localRotation * offset;

			}
		}
	}


	/// <summary>
	/// Loads the hmd to ZED calibration.
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
			string folder = System.Environment.GetFolderPath (System.Environment.SpecialFolder.CommonApplicationData);
			string specificFolder = Path.Combine (folder, @"Stereolabs\mr");
			calibrationFilePath = Path.Combine (specificFolder, calibrationFile);


			// Check if folder exists and if not, create it
			if (!Directory.Exists (specificFolder)) {
				Directory.CreateDirectory (specificFolder);
			}

			// Check if file exist and if not, create a default one
			if (!ParseCalibrationFile (calibrationFilePath))
				CreateDefaultCalibrationFile (calibrationFilePath);

			// Set the calibration in mr processing
			dllz_drift_corrector_set_calibration_transform (ref hmdtozedCalibration);
	
			// Create a file system watcher for online modifications
			CreateFileWatcher (specificFolder);
		}
	}

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

	// Define the event handlers.
	private void OnChanged(object source, FileSystemEventArgs e)
	{
		if (hasVRDevice) {
			ParseCalibrationFile (calibrationFilePath);
			dllz_drift_corrector_set_calibration_transform (ref hmdtozedCalibration);
			OnHdmCalibChanged ();
		}
	}

	private void CreateDefaultCalibrationFile(string path)
	{
		//Default Calibration : DO NOT CHANGE
		hmdtozedCalibration.rotation = Quaternion.identity;
		hmdtozedCalibration.translation.x = -0.0315f;
		hmdtozedCalibration.translation.y = 0.0f;
		hmdtozedCalibration.translation.z = 0.115f;

		//Write calibration file using default calibration
		using (System.IO.StreamWriter file = new System.IO.StreamWriter (path)) {
			string node = "[HMD]";
			string tx = "tx=" + hmdtozedCalibration.translation.x.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Translation x";
			string ty = "ty=" + hmdtozedCalibration.translation.y.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Translation y";
			string tz = "tz=" + hmdtozedCalibration.translation.z.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Translation z";
			string rx = "rx=" + hmdtozedCalibration.rotation.x.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion x";
			string ry = "ry=" + hmdtozedCalibration.rotation.y.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion y";
			string rz = "rz=" + hmdtozedCalibration.rotation.z.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion z";
			string rw = "rw=" + hmdtozedCalibration.rotation.w.ToString (System.Globalization.CultureInfo.InvariantCulture) + " //Quaternion w";

			file.WriteLine (node);
			file.WriteLine (tx);
			file.WriteLine (ty);
			file.WriteLine (tz);
			file.WriteLine (rx);
			file.WriteLine (ry);
			file.WriteLine (rz);
			file.WriteLine (rw);

			file.Close ();
		}

		//Default calibration already filled 
	}

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
		if (lines.Length==0) 
			return false;
		
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


		//Check wrong calibration
		if (hmdtozedCalibration.translation.x == 0.0f && hmdtozedCalibration.translation.y == 0.0f && hmdtozedCalibration.translation.z == 0.0f) {
			CreateDefaultCalibrationFile (path);
		}
	
		return true;
	}

 


}
