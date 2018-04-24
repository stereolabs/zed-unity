//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Create a mirror view and replace the view created by Unity
/// </summary>
public class ZEDMirror : MonoBehaviour
{
    /// <summary>
    /// Reference to the ZEDManager to get the texture overlay
    /// </summary>
    public ZEDManager manager;

    /// <summary>
    /// Reference to the texture overlay to get the render texture targeted
    /// </summary>
	private ZEDRenderingPlane textureOverlayLeft;

    void Start()
    {
        UnityEngine.VR.VRSettings.showDeviceView = false;
    }

    private void Update()
    {
        if (textureOverlayLeft == null && manager != null)
        {
			textureOverlayLeft = manager.GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
        }
    }

    private void OnPostRender()
    {

        if (textureOverlayLeft != null)
        {
            Graphics.Blit(textureOverlayLeft.target, null as RenderTexture);
        }

    }
}