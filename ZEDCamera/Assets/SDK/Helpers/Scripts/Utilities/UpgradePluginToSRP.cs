using UnityEngine;
using UnityEngine.Rendering;

public static class UpgradePluginToSRP
{
    public static bool UpgradeCameraToSRP(GameObject zedCam)
    {
        var SRP = GraphicsSettings.renderPipelineAsset;

        if (GraphicsSettings.renderPipelineAsset != null)
        {
            var SRPString = SRP.GetType().ToString();
            if (SRPString.Contains("UniversalRenderPipelineAsset"))
            {
                Material Mat_Zed_Forward_Lighting = Resources.Load("Materials/Lighting/Mat_ZED_Forward_Lighting_URP") as Material;

                var cameras = zedCam.GetComponentsInChildren<Camera>();

                foreach (var cam in cameras)
                {
                    cam.transform.Find("Frame").GetComponent<MeshRenderer>().sharedMaterial = Mat_Zed_Forward_Lighting;
                }

                if (cameras.Length > 0)
                {
                    return true;
                }
                else
                {
                    Debug.Log("No camera found in the ZED rig", zedCam);
                }
            }
            else if (SRPString.Contains("HDRenderPipelineAsset"))
            {
                Material Mat_Zed_Forward_Lighting = Resources.Load("Materials/Lighting/Mat_ZED_HDRP_Lit") as Material;

                var cameras = zedCam.GetComponentsInChildren<Camera>();

                foreach (var cam in cameras)
                {
                    cam.transform.Find("Frame").GetComponent<MeshRenderer>().sharedMaterial = Mat_Zed_Forward_Lighting;
                }

                if (cameras.Length > 0)
                {
                    return true;
                }
                else
                {
                    Debug.Log("No camera found in the ZED rig", zedCam);
                }
            }
            else
            {
                Debug.LogWarning("Trying to convert to custom SRP, please update materials manually");
            }
        }
        else
        {
            return false;
        }
        return false;
    }

    public static bool UpgradeGreenScreenToSRP(GameObject greenScreen)
    {
        var SRP = GraphicsSettings.renderPipelineAsset;

        if (GraphicsSettings.renderPipelineAsset != null)
        {
            var SRPString = SRP.GetType().ToString();
            if (SRPString.Contains("UniversalRenderPipelineAsset"))
            {
                Material Mat_Zed_GreenScreen = Resources.Load("Materials/Mat_ZED_GreenScreen_URP") as Material;

                var frame = greenScreen.GetComponentInChildren<Camera>().transform.Find("Frame");

                if (frame && frame.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
                {
                    meshRenderer.sharedMaterial = Mat_Zed_GreenScreen;
                    return true;
                }
                else
                {
                    Debug.LogWarning("Frame is not found", greenScreen);
                }
            }
            else if (SRPString.Contains("HDRenderPipelineAsset"))
            {
                Material Mat_Zed_GreenScreen = Resources.Load("Materials/Mat_ZED_Greenscreen_HDRP_Lit") as Material;

                var frame = greenScreen.GetComponentInChildren<Camera>().transform.Find("Frame");

                if (frame && frame.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
                {
                    meshRenderer.sharedMaterial = Mat_Zed_GreenScreen;
                    return true;
                }
                else
                {
                    Debug.LogWarning("Frame is not found", greenScreen);
                }
            }
            else
            {
                Debug.LogWarning("Green Screen: Failed to convert to custom SRP, please update materials manually.");
            }
        }
        else
        {
            return false;
        }
        return false;
    }

    public static bool UpgradePlanetariumToSRP(GameObject planetarium)
    {
        var SRP = GraphicsSettings.renderPipelineAsset;

        if (GraphicsSettings.renderPipelineAsset != null)
        {
            var SRPString = SRP.GetType().ToString();
            if (SRPString.Contains("UniversalRenderPipelineAsset"))
            {
                Material Mat_Zed_Sun = Resources.Load("Sun/Materials/Sun_URP") as Material;

                var sun = planetarium.transform.Find("Sun").Find("sun");
                if (sun && sun.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    meshRenderer.sharedMaterial = Mat_Zed_Sun;
                    return true;
                }
                else
                {
                    Debug.Log("Sun is not found", planetarium);
                }
            }
            else if (SRPString.Contains("HDRenderPipelineAsset"))
            {
                Material Mat_Zed_Sun = Resources.Load("Sun/Materials/Sun_URP") as Material;

                var sun = planetarium.transform.Find("Sun").Find("sun");
                if (sun && sun.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    meshRenderer.sharedMaterial = Mat_Zed_Sun;
                    return true;
                }
                else
                {
                    Debug.Log("Sun is not found", planetarium);
                }
            }
            else
            {
                Debug.LogWarning("Planetarium: Failed to convert to custom SRP, please update materials manually.\nSet the material of the \"sun\" object to Sun_URP.");
            }
        }
        else
        {
            return false;
        }
        return false;
    }
}