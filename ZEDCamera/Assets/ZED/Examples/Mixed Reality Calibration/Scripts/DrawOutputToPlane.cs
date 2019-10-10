using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Copies an attached camera's output to a target RenderTexture each frame. 
/// Also includes an option to pause this. 
/// </summary><remarks>The pause feature is not used in the MR calibration scene.</remarks>
[RequireComponent(typeof(Camera))]
public class DrawOutputToPlane : MonoBehaviour
{
    /// <summary>
    /// Renderer onto whose material this class will copy the camera's output. 
    /// </summary>
    public MeshRenderer targetRenderer;
    /// <summary>
    /// Intermediary texture we copy to when unpaused, which we also set as the target Renderer's main texture. 
    /// </summary>
    private RenderTexture targetTexture;

    /// <summary>
    /// Set to true to pause the updates, leaving the texture to show the last rendered frame until unpaused. 
    /// </summary>
    public static bool pauseTextureUpdate = false;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();

        targetTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
        targetTexture.Create();

        if (targetRenderer) targetRenderer.material.mainTexture = targetTexture;

    }

    /// <summary>
    /// Copies the output to the intermediary texture whenever the attached camera renders.
    /// </summary>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (targetTexture != null && pauseTextureUpdate != true)
        {
            Graphics.Blit(source, targetTexture);
        }

        Graphics.Blit(source, destination); //This ensures that the output image still goes where intended. 
    }
}
