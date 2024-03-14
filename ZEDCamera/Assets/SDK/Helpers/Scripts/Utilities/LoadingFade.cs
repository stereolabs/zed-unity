//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Fades the screen from black to the ZED image when the ZED camera is first opened. 
/// Added to the relevant cameras by ZEDRenderingPlane when ZED is initialized. 
/// </summary>
public class LoadingFade : MonoBehaviour
{
    /// <summary>
    /// Material used to perform the fade.
    /// </summary>
    private Material fader;

    /// <summary>
    /// Current alpha value of the black overlay used to darken the image. 
    /// </summary>
    private float alpha;

    /// <summary>
    /// Start flag. Set to true when the ZED is opened.
    /// </summary>
    private bool start = false;

    /// <summary>
    /// Sets the alpha to above 100% (to add a delay to the effect) and loads the fade material. 
    /// </summary>
	void Start ()
    {
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

    /// <summary>
    /// Applies the darkening effect to the camera's image. 
    /// Called by Unity every time the camera it's attached to renders an image.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (start)
        {
            //Lower the alpha. We use hard-coded values instead of using Time.deltaTime
            //to simplify things, but the function is quite customizable. 
            alpha -= EaseIn(0.4f, 0, 0.5f, 1.5f); 
        }
        alpha = alpha < 0 ? 0 : alpha; //Clamp the alpha at 0.
        fader.SetFloat("_Alpha", alpha); //Apply the new alpha to the fade material. 
        
        Graphics.Blit(source, destination, fader); //Render the image effect from the camera's output.
        if (alpha == 0) Destroy(this); //Remove the component when the fade is over. 
    }

    /// <summary>
    /// An ease-in function for reducing the alpha value each frame.
    /// </summary>
    /// <param name="t">Current time.</param>
    /// <param name="b">Start value.</param>
    /// <param name="c">Value change multiplier.</param>
    /// <param name="d">Duration.</param>
    /// <returns>New alpha value.</returns>
    static float EaseIn(float t, float b, float c, float d)
    {
        return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
    }

}
