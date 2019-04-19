using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CopyToSurface : MonoBehaviour
{
    public RawImage canvasRawImage;

    public Renderer worldRenderer;

    private Camera cam;
    private RenderTexture copyTexture;

	// Use this for initialization
	void Start ()
    {
        cam = GetComponent<Camera>();

        copyTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
        copyTexture.Create();

        if (canvasRawImage) canvasRawImage.texture = copyTexture;
        if (worldRenderer) worldRenderer.material.mainTexture = copyTexture;
    }
	

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, copyTexture);
        Graphics.Blit(source, destination);
    }

    private void OnApplicationQuit()
    {
        if (copyTexture) copyTexture.Release();
    }
}
