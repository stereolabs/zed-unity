//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Helper functions for the post processing effects applied to the final mixed reality image.
/// Used by ZEDRenderingPlane when Post-Processing is enabled, and always used by GreenScreenManager. 
/// </summary>
public class ZEDPostProcessingTools
{
    /// <summary>
    /// Returns the gaussian value for f(x) = (1/2*3.14*s)*e(-x*x/(2*s)).
    /// </summary>
    /// <param name="x"></param>
    /// <param name="sigma"></param>
    /// <returns></returns>
    public static float Gaussian(float x, float sigma)
    {
        return (1.0f / (2.0f * Mathf.PI * sigma)) * Mathf.Exp(-((x * x) / (2.0f * sigma)));
    }

    /// <summary>
    /// Computes weights to be sent to the blur shader.
    /// </summary>
    /// <param name="sigma"></param>
    /// <param name="weights_"></param>
    /// <param name="offsets_"></param>
    public static void ComputeWeights(float sigma, out float[] weights_, out float[] offsets_)
    {
        float[] weights = new float[5];
        float[] offsets = { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f };

        weights_ = new float[5];
        offsets_ = new float[5] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f };

        // Calculate the weights 
        weights[0] = Gaussian(0, sigma);
        if (sigma != 0)
        {
            float sum = weights[0];
            for (int i = 1; i < 5; ++i)
            {
                weights[i] = Gaussian(offsets[i], sigma);
                sum += 2.0f * weights[i];
            }

            for (int i = 0; i < 5; ++i)
            {
                weights[i] /= sum;
            }

            //Fix for just 3 fetches 
            weights_[0] = weights[0];
            weights_[1] = weights[1] + weights[2];
            weights_[2] = weights[3] + weights[4];

            offsets_[0] = 0.0f;
            offsets_[1] = ((weights[1] * offsets[1]) + (weights[2] * offsets[2])) / weights_[1];
            offsets_[2] = ((weights[3] * offsets[3]) + (weights[4] * offsets[4])) / weights_[2];
        }
    }

    /// <summary>
    /// Blurs a render texture.
    /// </summary>
    /// <param name="source">Source RenderTexture.</param>
    /// <param name="dest">RenderTexture that the blurred version will be rendered into.</param>
    /// <param name="mat">Material used to blur.</param>
    /// <param name="pass">The pass used by the material.</param>
    /// <param name="numberIterations">More iterations means a more prominent blur.</param>
    /// <param name="downscale">The downscale of the source, which increases blur and decreases computation time.</param>
    public static void Blur(RenderTexture source, RenderTexture dest, Material mat, int pass, int numberIterations = -1, int downscale = 2)
    {

        if (numberIterations == -1 || numberIterations == 0)
        {
            Graphics.Blit(source, dest, mat, pass);
            return;
        }

        RenderTexture buffer = RenderTexture.GetTemporary(source.width / downscale, source.height / downscale, source.depth, source.format, RenderTextureReadWrite.Default);

        if (mat == null)
        {
            Graphics.Blit(source, buffer);
            Graphics.Blit(buffer, dest);
        }
        else
        {
            bool oddEven = false;

            //Create two buffers to make a multi-pass blur.
            RenderTexture buffer2 = RenderTexture.GetTemporary(source.width / downscale, source.height / downscale, source.depth, source.format, RenderTextureReadWrite.Default);


            Graphics.Blit(source, buffer);
            //To each pass, alternate the buffer, and set the blur direction.
            for (int i = 0; i < numberIterations * 2; i++)
            {
                mat.SetInt("horizontal", System.Convert.ToInt32(oddEven));
                if (i < numberIterations * 2 - 1)
                {
                    Graphics.Blit(oddEven ? buffer2 : buffer, !oddEven ? buffer2 : buffer, mat, pass);
                    oddEven = !oddEven;
                }
                else
                {
                    mat.SetInt("horizontal", System.Convert.ToInt32(oddEven));

                    //Copy the buffer to the final texture.
                    if (oddEven)
                    {
                        Graphics.Blit(buffer2, dest, mat, pass);
                    }
                    else
                    {
                        Graphics.Blit(buffer, dest, mat, pass);
                    }
                }
            }
            //Destroy all the temporary buffers.
            RenderTexture.ReleaseTemporary(buffer2);
        }
        RenderTexture.ReleaseTemporary(buffer);
    }

    /// <summary>
    /// Holds IDs of shader properties. Used because setting a property by ID is faster than
    /// setting it by a property name. 
    /// </summary>
    static class Uniforms
    {
        internal static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        internal static readonly int _TempRT = Shader.PropertyToID("_TempRT");
        internal static readonly int _TempRT2 = Shader.PropertyToID("_TempRT2");
        internal static readonly int _TempRT3 = Shader.PropertyToID("_TempRT3");

    }

    /// <summary>
    /// Blurs a render texture.
    /// </summary>
    /// <param name="cb">CommandBuffer from where the rendertexture is taken.</param>
    /// <param name="dest">RenderTexture to be blurred.</param>
    /// <param name="mat">Material used to blur</param>
    /// <param name="pass">The pass used by the material</param>
    /// <param name="numberIterations">More iterations means a more prominent blur</param>
    /// <param name="downscale">The downscale of the source, which increases blur and decreases computation time.</param>
    public static void Blur(CommandBuffer cb, RenderTexture texture, Material mat, int pass, int numberIterations = -1, int downscale = 2)
    {

        if (numberIterations == -1 || numberIterations == 0)
        {
            cb.GetTemporaryRT(Uniforms._TempRT, texture.width, texture.height, texture.depth);
            cb.Blit(texture, Uniforms._TempRT, mat, pass);
            cb.Blit(Uniforms._TempRT, texture);
            cb.ReleaseTemporaryRT(Uniforms._TempRT);
            return;
        }

        cb.GetTemporaryRT(Uniforms._TempRT, texture.width / downscale, texture.height / downscale, texture.depth, FilterMode.Bilinear, texture.format, RenderTextureReadWrite.Default);
        cb.GetTemporaryRT(Uniforms._TempRT2, texture.width / downscale, texture.height / downscale, texture.depth, FilterMode.Bilinear, texture.format, RenderTextureReadWrite.Default);

        bool oddEven = false;

        //Create two buffers to make a multi-pass blur
        cb.Blit(texture, Uniforms._TempRT);
        //To each pass alternate the buffer, and set the blur direction
        for (int i = 0; i < numberIterations * 2; i++)
        {
            mat.SetInt("horizontal", System.Convert.ToInt32(oddEven));
            if (i < numberIterations * 2 - 1)
            {
                cb.Blit(oddEven ? Uniforms._TempRT2 : Uniforms._TempRT, !oddEven ? Uniforms._TempRT2 : Uniforms._TempRT, mat, pass);
                oddEven = !oddEven;
            }
            else
            {
                mat.SetInt("horizontal", System.Convert.ToInt32(oddEven));

                //Copy the buffer to the final texture
                if (oddEven)
                {
                    cb.Blit(Uniforms._TempRT2, texture, mat, pass);
                }
                else
                {
                    cb.Blit(Uniforms._TempRT, texture, mat, pass);
                }
            }
        }
        //Destroy all the temporary buffers
        cb.ReleaseTemporaryRT(Uniforms._TempRT);
        cb.ReleaseTemporaryRT(Uniforms._TempRT2);
    }

    public static void ComposeMask(CommandBuffer cb, RenderTexture mask, Material matStencilToMask, Material matComposeMask)
    {
        cb.GetTemporaryRT(Uniforms._TempRT, mask.width, mask.height, mask.depth, mask.filterMode, mask.format, RenderTextureReadWrite.Default);
        cb.GetTemporaryRT(Uniforms._TempRT2, -1, -1, 24);
        cb.Blit(mask, Uniforms._TempRT);

        //This is fine, set up the target and clear.
        cb.SetRenderTarget(Uniforms._TempRT2);
        cb.ClearRenderTarget(false, true, Color.black);

        //To keep the stencil in post process.
        cb.SetRenderTarget(Uniforms._TempRT2, BuiltinRenderTextureType.CurrentActive);
        cb.Blit(BuiltinRenderTextureType.CameraTarget, Uniforms._TempRT2, matStencilToMask);

        //Compose the second mask retrieved in the forward pass. The shader should set the stencil to 148.
        //cb.Blit(mask, Uniforms._TempRT);
        cb.SetGlobalTexture("_Mask", Uniforms._TempRT);
       // matComposeMask.set("_Mask", Uniforms._TempRT);
        cb.Blit(Uniforms._TempRT2, mask, matComposeMask);
    }
}
