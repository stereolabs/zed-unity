using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;


/// <summary>
/// Manages the various compiler defines that the ZED Unity plugin uses to enable and disable features that are dependent on specific packages.
/// This includes the SteamVR and Oculus plugins (for controller interaction) and OpenCV for Unity (for ArUco detection).
/// </summary>
[InitializeOnLoad]
public class ZEDDefineHandler : AssetPostprocessor
{
    static ZEDDefineHandler()
    {
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
#region VR Plugins
        if (CheckPackageExists("OVRManager"))
        {
            ActivateDefine("Oculus", "ZED_OCULUS");
        }
        else
        {
            DeactivateDefine("Oculus", "ZED_OCULUS");
        }

        if (CheckPackageExists("SteamVR_Camera")) //"OpenVR" and "SteamVR" exist in script names in the Oculus plugin.
        {
            ActivateDefine("SteamVR", "ZED_STEAM_VR");
        }
        else
        {
            DeactivateDefine("SteamVR", "ZED_STEAM_VR");
        }

        if (CheckPackageExists("SteamVR_Input_Sources"))
        {
            ActivateDefine("SteamVR_2_0_Input", "ZED_SVR_2_0_INPUT");
        }
        else
        {
            DeactivateDefine("SteamVR_2_0_Input", "ZED_SVR_2_0_INPUT");
        }
#endregion

#region OpenCV
        string opencvfilename = "opencvforunity.dll";
        opencvfilename = "opencvforunity";


        //if(EditorPrefs.GetBool("ZEDOpenCV") == false && CheckPackageExists(opencvfilename))
        if (CheckPackageExists(opencvfilename))
        {
            ActivateDefine("ZEDOpenCV", "ZED_OPENCV_FOR_UNITY");
        }
        else
        {
            DeactivateDefine("ZEDOpenCV", "ZED_OPENCV_FOR_UNITY");
        }

#endregion
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
    public static void ActivateDefine(string packageName, string defineName)
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

        // FOR OCULUS QUEST
        string defines_android = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
        if (defines_android.Length != 0)
        {
            if (!defines_android.Contains(defineName))
            {
                defines_android += ";" + defineName;
            }
        }
        else
        {
            if (!defines_android.Contains(defineName))
            {
                defines_android += defineName;
            }
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines_android);
    }

    /// <summary>
    /// Removes a define tag from the project.
    /// Called whenever a package is checked for but not found.
    /// Removing the define tags will prevent compilation of code marked with that tag, like #if ZED_OCULUS.
    /// </summary>
    public static void DeactivateDefine(string packagename, string defineName)
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
