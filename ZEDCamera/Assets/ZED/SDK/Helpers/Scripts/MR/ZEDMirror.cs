//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.XR;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Rendering;
#endif

/// <summary>
/// In AR mode, displays a full-screen, non-timewarped view of the scene for the editor's Game window.
/// Replaces Unity's default behavior of replicating the left eye view directly,
/// which would otherwise have black borders and move around when the headset moves because of 
/// latency compensation.
/// ZEDManager creates a hidden camera with this script attached when in AR mode (see ZEDManager.CreateMirror()). 
/// </summary>
public class ZEDMirror : MonoBehaviour
{
    /// <summary>
    /// The scene's ZEDManager component, for getting the texture overlay. 
    /// </summary>
    public ZEDManager manager;

    /// <summary>
    /// Reference to the ZEDRenderingPlane that renders the left eye, so we can get its target RenderTexture. 
    /// </summary>
	private ZEDRenderingPlane textureOverlayLeft;

    //private RenderTexture bufferTexture;

    void Start()
    {
        XRSettings.showDeviceView = false; //Turn off default behavior.

#if ZED_LWRP || ZED_HDRP
        RenderPipelineManager.endFrameRendering += OnFrameEnd;
#endif

    }

    private void Update()
    {
        if (textureOverlayLeft == null && manager != null)
        {
			textureOverlayLeft = manager.GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
        }
    }

#if !ZED_LWRP && !ZED_HDRP
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (textureOverlayLeft != null)
        {
            //Ignore source. Copy ZEDRenderingPlane's texture as the final image.
            Graphics.Blit(textureOverlayLeft.target, destination);  
        }
    }

#else
/// <summary>
/// Blits the intermediary targetTexture to the final outputTexture for rendering. Used in SRP because there is no OnRenderImage automatic function. 
/// </summary>
private void OnFrameEnd(ScriptableRenderContext context, Camera[] cams)
{
    if (textureOverlayLeft != null)
    {
        Graphics.Blit(textureOverlayLeft.target, (RenderTexture)null);
    }
}
#endif

    private void OnDestroy()
    {
#if ZED_LWRP || ZED_HDRP
        RenderPipelineManager.endFrameRendering -= OnFrameEnd;
#endif
    }
}