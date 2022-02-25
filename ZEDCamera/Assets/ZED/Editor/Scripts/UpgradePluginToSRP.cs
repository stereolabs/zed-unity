using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class EditPrefabAssetScope : IDisposable
{

    public readonly string assetPath;
    public readonly GameObject prefabRoot;

    public EditPrefabAssetScope(string assetPath)
    {
        this.assetPath = assetPath;
        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
    }

    public void Dispose()
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}


public class UpgradePluginToSRP
{

    [MenuItem("ZED/Upgrade ZED Plugin to URP")]
    static void UpgradeToURP()
    {
        var SRP = GraphicsSettings.renderPipelineAsset;

        string zedRigMonoPath = "Assets/ZED/Prefabs/ZED_Rig_Mono.prefab";
        string zedRigStereoPath = "Assets/ZED/Prefabs/ZED_Rig_Stereo.prefab";
        string zedGreenScreenPath = "Assets/ZED/Examples/GreenScreen/Prefabs/ZED_GreenScreen.prefab";
        string zedPlanetariumPath = "Assets/ZED/Examples/Planetarium/Prefabs/Planetarium.prefab";


        if (GraphicsSettings.renderPipelineAsset != null)
        {
            if (SRP.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
            {

                RenderingPipelineDefines.AddDefine("ZED_URP");
                RenderingPipelineDefines.RemoveDefine("ZED_HDRP");

                Material Mat_Zed_Forward_Lighting = Resources.Load("Materials/Lighting/Mat_ZED_Forward_Lighting_URP") as Material;
                Material Mat_Zed_GreenScreen = Resources.Load("Materials/Mat_ZED_GreenScreen_URP") as Material;
                Material Mat_Zed_Sun = Resources.Load("Sun/Materials/Sun_URP") as Material;


                // Modify prefab contents and save it back to the Prefab Asset
                using (var editScope = new EditPrefabAssetScope(zedRigMonoPath))
                {
                    Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

                    if (frame)
                    {
                        frame.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                    }
                    else
                    {
                        Debug.Log("Frame is not found");
                    }
                }
                using (var editScope = new EditPrefabAssetScope(zedRigStereoPath))
                {
                    Transform frame_left = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Left_eye").Find("Frame");

                    if (frame_left)
                    {
                        frame_left.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                    }
                    else
                    {
                        Debug.Log("Frame Left is not found");
                    }

                    Transform frame_right = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Right_eye").Find("Frame");

                    if (frame_right)
                    {
                        frame_right.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                    }
                    else
                    {
                        Debug.Log("Frame Right is not found");
                    }
                }
                // Modify prefab contents and save it back to the Prefab Asset
                using (var editScope = new EditPrefabAssetScope(zedGreenScreenPath))
                {
                    Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

                    if (frame)
                    {
                        frame.GetComponent<MeshRenderer>().material = Mat_Zed_GreenScreen;
                    }
                    else
                    {
                        Debug.Log("Frame is not found");
                    }
                }
                using (var editScope = new EditPrefabAssetScope(zedPlanetariumPath))
                {
                    Transform sun = editScope.prefabRoot.transform.Find("Sun").Find("sun");

                    if (sun)
                    {
                        sun.GetComponent<MeshRenderer>().material = Mat_Zed_Sun;
                    }
                    else
                    {
                        Debug.Log("Planetarium is not found");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Trying to convert to URP without using URP !!");
            }
        }

    }

    [MenuItem("ZED/Upgrade ZED Plugin to HDRP")]
    static void UpgradeToHDRP()
    {
        var SRP = GraphicsSettings.renderPipelineAsset;

        string zedRigMonoPath = "Assets/ZED/Prefabs/ZED_Rig_Mono.prefab";
        string zedRigStereoPath = "Assets/ZED/Prefabs/ZED_Rig_Stereo.prefab";
        string zedGreenScreenPath = "Assets/ZED/Examples/GreenScreen/Prefabs/ZED_GreenScreen.prefab";
        string zedPlanetariumPath = "Assets/ZED/Examples/Planetarium/Prefabs/Planetarium.prefab";

        if (SRP.GetType().ToString().Contains("HDRenderPipelineAsset"))
        {

            RenderingPipelineDefines.AddDefine("ZED_HDRP");
            RenderingPipelineDefines.RemoveDefine("ZED_URP");

            Material Mat_Zed_Forward_Lighting = Resources.Load("Materials/Lighting/Mat_ZED_HDRP_Lit") as Material;
            Material Mat_Zed_GreenScreen = Resources.Load("Materials/Mat_ZED_Greenscreen_HDRP_Lit") as Material;
            Material Mat_Zed_Sun = Resources.Load("Sun/Materials/Sun_URP") as Material;

            // Modify prefab contents and save it back to the Prefab Asset
            using (var editScope = new EditPrefabAssetScope(zedRigMonoPath))
            {
                Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

                if (frame)
                {
                    frame.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                }
                else
                {
                    Debug.Log("Frame is not found");
                }
            }

            using (var editScope = new EditPrefabAssetScope(zedRigStereoPath))
            {
                Transform frame_left = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Left_eye").Find("Frame");

                if (frame_left)
                {
                    frame_left.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                }
                else
                {
                    Debug.Log("Frame Left is not found");
                }

                Transform frame_right = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Right_eye").Find("Frame");

                if (frame_right)
                {
                    frame_right.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
                }
                else
                {
                    Debug.Log("Frame Right is not found");
                }
            }
            // Modify prefab contents and save it back to the Prefab Asset
            using (var editScope = new EditPrefabAssetScope(zedGreenScreenPath))
            {
                Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

                if (frame)
                {
                    frame.GetComponent<MeshRenderer>().material = Mat_Zed_GreenScreen;
                }
                else
                {
                    Debug.Log("Frame is not found");
                }
            }
            using (var editScope = new EditPrefabAssetScope(zedPlanetariumPath))
            {
                Transform sun = editScope.prefabRoot.transform.Find("Sun").Find("sun");

                if (sun)
                {
                    sun.GetComponent<MeshRenderer>().material = Mat_Zed_Sun;
                }
                else
                {
                    Debug.Log("Planetarium is not found");
                }
            }
        }
        else
        {
            Debug.LogWarning("Trying to convert to HDRP without using HDRP !!");
        }
    }

    /*[MenuItem("ZED/Revert ZED Plugin to built-in RP")]
    static void RevertToBuiltInRP()
    {
        RenderingPipelineDefines.RemoveDefine("ZED_HDRP");
        RenderingPipelineDefines.RemoveDefine("ZED_URP");

        string zedRigMonoPath = "Assets/ZED/Prefabs/ZED_Rig_Mono.prefab";
        string zedRigStereoPath = "Assets/ZED/Prefabs/ZED_Rig_Stereo.prefab";
        string zedGreenScreenPath = "Assets/ZED/Examples/GreenScreen/Prefabs/ZED_GreenScreen.prefab";
        string zedPlanetariumPath = "Assets/ZED/Examples/Planetarium/Prefabs/Planetarium.prefab";

        Material Mat_Zed_Forward_Lighting = Resources.Load("Materials/Lighting/Mat_ZED_Forward_Lighting") as Material;
        Material Mat_Zed_GreenScreen = Resources.Load("Materials/Mat_ZED_GreenScreen_URP") as Material;
        Material Mat_Zed_Sun = Resources.Load("Sun/Materials/Sun_URP") as Material;

        // Modify prefab contents and save it back to the Prefab Asset
        using (var editScope = new EditPrefabAssetScope(zedRigMonoPath))
        {
            Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

            if (frame)
            {
                frame.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
            }
            else
            {
                Debug.Log("Frame is not found");
            }
        }
        using (var editScope = new EditPrefabAssetScope(zedRigStereoPath))
        {
            Transform frame_left = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Left_eye").Find("Frame");

            if (frame_left)
            {
                frame_left.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
            }
            else
            {
                Debug.Log("Frame Left is not found");
            }

            Transform frame_right = editScope.prefabRoot.transform.Find("Camera_eyes").Find("Right_eye").Find("Frame");

            if (frame_right)
            {
                frame_right.GetComponent<MeshRenderer>().material = Mat_Zed_Forward_Lighting;
            }
            else
            {
                Debug.Log("Frame Right is not found");
            }
        }
        // Modify prefab contents and save it back to the Prefab Asset
        using (var editScope = new EditPrefabAssetScope(zedGreenScreenPath))
        {
            Transform frame = editScope.prefabRoot.transform.Find("Camera_Left").Find("Frame");

            if (frame)
            {
                frame.GetComponent<MeshRenderer>().material = Mat_Zed_GreenScreen;
            }
            else
            {
                Debug.Log("Frame is not found");
            }
        }
        using (var editScope = new EditPrefabAssetScope(zedPlanetariumPath))
        {
            Transform sun = editScope.prefabRoot.transform.Find("Sun").Find("sun");

            if (sun)
            {
                sun.GetComponent<MeshRenderer>().material = Mat_Zed_Sun;
            }
            else
            {
                Debug.Log("Planetarium is not found");
            }
        }
    }*/
}


