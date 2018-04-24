//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
/// <summary>
/// Fade the screen from black to the ZED image after the opening of the ZED
/// </summary>
public class LoadingFade : MonoBehaviour {
    /// <summary>
    /// Material used to fade
    /// </summary>
    private Material fader;

    /// <summary>
    /// Current alpha value
    /// </summary>
    private float alpha;

    /// <summary>
    /// start flag, set to true when the ZED is opened
    /// </summary>
    private bool start = false;
	void Start () {
        alpha = 1.5f;
        fader = new Material(Resources.Load("Materials/GUI/Mat_ZED_Fade") as Material);
	}

    private void OnEnable()
    {
		start = true;
    }

    private void OnDisable()
    {
		start = false;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (start)
        {
            alpha -= EaseIn(0.4f, 0, 0.5f, 1.5f);
        }
        alpha = alpha < 0 ? 0 : alpha;
        fader.SetFloat("_Alpha", alpha);
        
        Graphics.Blit(source, destination, fader);
        if (alpha == 0) Destroy(this);
    }

    /// <summary>
    /// Reproduces the ease in function
    /// </summary>
    /// <param name="t"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    static float EaseIn(float t, float b, float c, float d)
    {
        return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
    }

}
