using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Rendering;
#endif

/// <summary>
/// Copies the output of the camera to the selected RawImage, and/or Renderer as a material. 
/// Use to look at the camera on something other than the final screen output, like a plane or UI element. 
/// </summary>
[RequireComponent(typeof(Camera))]
public class CopyToSurface : MonoBehaviour
{
    /// <summary>
    /// 2D Raw Image object that you can have the camera output copied to. 
    /// </summary>
    [Tooltip("2D Raw Image object that you can have the camera output copied to.")]
    public RawImage canvasRawImage;

    /// <summary>
    /// 3D Renderer that will have its mainTexture set to the camera texture.  
    /// </summary>
    [Tooltip("3D Renderer that will have its mainTexture set to the camera texture.")]
    public Renderer worldRenderer;

    /// <summary>
    /// If worldRenderer is set, this is the name of the texture property that will be set with the camera image. 
    /// "_MainTex" works for most Standard render pipeline materials. "_BaseMap" works for most lit LWRP materials and "_BaseColorMap" for lit HDRP materials.
    /// </summary>
    [Tooltip("If worldRenderer is set, this is the name of the texture property that will be set with the camera image.\r\n\n" +
        "'_MainTex' works for most Standard render pipeline materials, '_BaseMap' works for most lit LWRP materials and '_BaseColorMap' for lit HDRP materials.")]
#if !ZED_LWRP && !ZED_HDRP
    public string rendererTextureProperty = "_MainTex";
#elif ZED_LWRP
    public string rendererTextureProperty = "_BaseMap";
#elif ZED_HDRP
    public string rendererTextureProperty = "_BaseColorMap";
#endif
    private Camera cam;
    private RenderTexture copyTexture;

    // Use this for initialization
    void Start()
    {
        cam = GetComponent<Camera>();

#if !ZED_LWRP
        copyTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
#else
        copyTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32);
#endif
        copyTexture.Create();

        if (canvasRawImage) canvasRawImage.texture = copyTexture;
        if (worldRenderer) worldRenderer.material.SetTexture(rendererTextureProperty, copyTexture);

#if ZED_LWRP || ZED_HDRP

        RenderPipelineManager.beginFrameRendering += SRPStartDraw;
        RenderPipelineManager.endFrameRendering += SRPEndDraw;
#endif
    }

#if !ZED_LWRP && !ZED_HDRP
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, copyTexture);
        Graphics.Blit(source, destination);
    }
#else

    private void SRPStartDraw(ScriptableRenderContext context, Camera[] rendcam)
    {
        cam.targetTexture = copyTexture;
    }

    private void SRPEndDraw(ScriptableRenderContext context, Camera[] rendcam)
    {
        cam.targetTexture = null;
        Graphics.Blit(copyTexture, (RenderTexture)null);
    }
#endif

    private void OnApplicationQuit()
    {
        if (copyTexture) copyTexture.Release();

#if ZED_LWRP || ZED_HDRP

        RenderPipelineManager.beginFrameRendering -= SRPStartDraw;
        RenderPipelineManager.endFrameRendering -= SRPEndDraw;
#endif
    }
}
