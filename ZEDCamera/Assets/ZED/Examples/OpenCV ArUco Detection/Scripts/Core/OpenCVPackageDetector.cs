#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


/// <summary>
/// Enables/disables the ZED_OPENCV_FOR_UNITY compiler directive, depending on whether the ZED for OpenCV asset has been detected. 
/// You need this package in your project to use the ZED plugin's ArUco detection classes and to run the example scenes. 
/// You can get the package on the Unity Asset Store: https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088
/// </summary>
public class OpenCVPackageDetector : AssetPostprocessor
{
    [SerializeField]
    static string defineName;
    static string packageName;

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        string opencvfilename = "opencvforunity.dll";
        opencvfilename = "opencvforunity";
 
        //if(EditorPrefs.GetBool("ZEDOpenCV") == false && CheckPackageExists(opencvfilename))
        if(CheckPackageExists(opencvfilename))
        {
            defineName = "ZED_OPENCV_FOR_UNITY";
            packageName = "ZEDOpenCV";
            ActivateDefine();
        }
        //else if (EditorPrefs.GetBool("ZEDOpenCV") == true)
        else
        {
            defineName = "ZED_OPENCV_FOR_UNITY";
            DeactivateDefine("ZEDOpenCV");
        }
    }

    /// <summary>
    /// Finds if a folder in the project exists with the specified name. 
    /// Used to check if a plugin has been imported, as the relevant plugins are placed
    /// in a folder named after the package. Example: "Assets/Oculus". 
    /// </summary>
    /// <param name="name">Package name.</param>
    /// <returns></returns>
    public static bool CheckPackageExists(string name)
    {
        string[] packages = AssetDatabase.FindAssets(name);
        return packages.Length != 0;
    }


    /// <summary>
    /// Activates a define tag in the project. Used to enable compiling sections of scripts with that tag enabled. 
    /// For instance, parts of this script under a #if ZED_STEAM_VR statement will be ignored by the compiler unless ZED_STEAM_VR is enabled. 
    /// </summary>
    public static void ActivateDefine()
    {
        EditorPrefs.SetBool(packageName, true);
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (defines.Length != 0)
        {
            if (!defines.Contains(defineName))
            {
                defines += ";" + defineName;
            }
        }
        else
        {
            if (!defines.Contains(defineName))
            {
                defines += defineName;
            }
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
    }

    /// <summary>
    /// Removes a define tag from the project. 
    /// Called whenever a package is checked for but not found. 
    /// Removing the define tags will prevent compilation of code marked with that tag, like #if ZED_OCULUS.
    /// </summary>
    public static void DeactivateDefine(string packagename)
    {
        EditorPrefs.SetBool(packagename, false);
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (defines.Length != 0)
        {
            if (defineName != null && defines.Contains(defineName))
            {
                defines = defines.Remove(defines.IndexOf(defineName), defineName.Length);

                if (defines.LastIndexOf(";") == defines.Length - 1 && defines.Length != 0)
                {
                    defines.Remove(defines.LastIndexOf(";"), 1);
                }
            }
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
    }

}

#endif

