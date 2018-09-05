//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

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

    void Start()
    {
        UnityEngine.VR.VRSettings.showDeviceView = false; //Turn off default behavior.
    }

    private void Update()
    {
        if (textureOverlayLeft == null && manager != null)
        {
			textureOverlayLeft = manager.GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
        }
    }

    private void OnPostRender() //Called after the Camera component in this GameObject has rendered. 
    {
        if (textureOverlayLeft != null)
        {
            Graphics.Blit(textureOverlayLeft.target, null as RenderTexture); //Copy ZEDRenderingPlane's texture as the final image. 
        }

    }
}