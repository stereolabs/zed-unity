using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Rendering;
#endif

/// <summary>
/// Copies an attached camera's output to a target RenderTexture each frame. 
/// Also includes an option to pause this. 
/// </summary><remarks>The pause feature is not used in the MR calibration scene.</remarks>
[RequireComponent(typeof(Camera))]
public class DrawOutputToPlane : MonoBehaviour
{
    /// <summary>
    /// Intermediary texture we copy to when unpaused, which we also set as the target Renderer's main texture. 
    /// </summary>
    private RenderTexture targetTexture;
    /// <summary>
    /// Texture applied to the final material. 
    /// </summary>
    private RenderTexture outputTexture;
    /// <summary>
    /// Renderer onto whose material this class will copy the camera's output. 
    /// </summary>
    public MeshRenderer outputRenderer;

    /// <summary>
    /// Set to true to pause the updates, leaving the texture to show the last rendered frame until unpaused. 
    /// </summary>
    public static bool pauseTextureUpdate = false;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();

        targetTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
        targetTexture.Create();

        outputTexture = new RenderTexture(targetTexture);
        outputTexture.Create();

        cam.targetTexture = targetTexture;

        if (outputRenderer) outputRenderer.material.SetTexture("_MainTex", outputTexture); //TODO: Cache shader ID. 
                                                                                                 // (targetRenderer) targetRenderer.material.mainTexture = targetTexture;

#if ZED_LWRP || ZED_HDRP
        RenderPipelineManager.endFrameRendering += OnFrameEnd;
        if (outputRenderer) outputRenderer.material.mainTexture = targetTexture;
#endif
    }
#if ZED_LWRP || ZED_HDRP
    /// <summary>
    /// Blits the intermediary targetTexture to the final outputTexture for rendering. Used in SRP because there is no OnRenderImage automatic function. 
    /// </summary>
    private void OnFrameEnd(ScriptableRenderContext context, Camera[] cams)
    {
        if (targetTexture != null && pauseTextureUpdate != true)
        {
            Graphics.Blit(targetTexture, outputTexture);
        }
    }
#endif

    /// <summary>
    /// Copies the output to the intermediary texture whenever the attached camera renders.
    /// </summary>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (targetTexture != null && pauseTextureUpdate != true)
        {
            Graphics.Blit(source, outputTexture);
        }
    }
}
