
//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Renders planes detected by ZEDPlaneDetectionManager in a second, hidden camera created at runtume. 
/// This gets the alpha mesh style with no performance loss.
/// This script is very similar to how ZEDMeshRenderer works for spatial mapping. 
/// </summary>
public class ZEDPlaneRenderer : MonoBehaviour
{
	/// <summary>
	/// Reference to the hidden camera we create at runtime. 
	/// </summary>
	private Camera cam;

	/// <summary>
	/// Target texture of the rendering done by the new camera.
	/// </summary>
	private RenderTexture planeTex;

	/// <summary>
	/// Checks if the ZED and this script have both finished initializing. 
	/// </summary>
	private bool isReady = false;

	/// <summary>
	/// Reference to the ZEDRenderingPlane component of the camera we copy. 
	/// </summary>
	private ZEDRenderingPlane renderingPlane;

    /// <summary>
    /// Creates the duplicate camera that renders only the planes. 
    /// Rendering targets a RenderTexture that ZEDRenderingPlane will blend in at OnRenderImage(). 
    /// This gets called by ZEDManager.OnZEDReady when the ZED is finished initializing. 
    /// </summary>
	void ZEDReady()
	{
        //Create the new GameObject and camera as a child of the corresponding ZED rig camera.
		GameObject go = new GameObject("PlaneCamera");
		go.transform.parent = transform;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		cam = go.AddComponent<Camera>();
		go.hideFlags = HideFlags.HideAndDontSave; //This hides the new camera from scene view. Comment this out to see it in the hierarchy. 

        //Set the target texture to a new RenderTexture that will be passed to ZEDRenderingPlane for blending. 
		if (sl.ZEDCamera.GetInstance().IsCameraReady)
		{
			planeTex = new RenderTexture(sl.ZEDCamera.GetInstance().ImageWidth, sl.ZEDCamera.GetInstance().ImageHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			planeTex.Create();
		}

        //Set the camera's parameters. 
		cam.enabled = false;
		cam.cullingMask = (1 << sl.ZEDCamera.TagOneObject); //Layer set aside for planes and spatial mapping meshes. 
		cam.targetTexture = planeTex;
		cam.nearClipPlane = 0.1f;
		cam.farClipPlane = 100.0f;
		cam.fieldOfView = sl.ZEDCamera.GetInstance().GetFOV() * Mathf.Rad2Deg;
		cam.projectionMatrix = sl.ZEDCamera.GetInstance().Projection;
		cam.backgroundColor = new Color(0, 0, 0, 0);
		cam.clearFlags = CameraClearFlags.Color;
		cam.renderingPath = RenderingPath.VertexLit;
		cam.depth = 0;
		cam.depthTextureMode = DepthTextureMode.None;

		#if UNITY_5_6_OR_NEWER
		cam.allowMSAA = false;
		cam.allowHDR = false;
		#endif

		cam.useOcclusionCulling = false;

        //Set the ZEDRenderingPlane blend texture to the one the new camera renders to.
        renderingPlane = GetComponent<ZEDRenderingPlane>();
		renderingPlane.SetTextureOverlayMapping(planeTex); 

		isReady = true;
	}


    /// <summary>
    /// Subscribes to ZEDManager.OnZEDReady. 
    /// </summary>
	private void OnEnable()
	{
		ZEDManager.OnZEDReady += ZEDReady;
	}

    /// <summary>
    /// Unsubscribes from ZEDManager.OnZEDReady. 
    /// </summary>
	private void OnDisable()
	{
		ZEDManager.OnZEDReady -= ZEDReady;
	}


	/// <summary>
    /// Renders the plane each frame, before cameras normally update, so the RenderTexture is ready to be blended. 
    /// </summary>
	void Update()
	{
		if (isReady)
		{
			cam.enabled = true;
            cam.Render();
			cam.enabled = false;
		}
	}

    /// <summary>
    /// Releases the target RenderTexture when the application quits. 
    /// </summary>
	private void OnApplicationQuit()
	{
		if (planeTex != null && planeTex.IsCreated())
		{
			planeTex.Release();
		}
	}
}
