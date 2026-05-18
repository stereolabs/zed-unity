#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Manages compiler defines for optional ZED plugin dependencies (e.g. OpenCV for Unity).
/// Runs on domain reload and after asset changes to keep defines in sync.
/// </summary>
[InitializeOnLoad]
public class ZEDDefineHandler : AssetPostprocessor
{
    const string OpenCVAssemblyName = "opencvforunity";
    const string OpenCVDefine = "ZED_OPENCV_FOR_UNITY";

    static ZEDDefineHandler()
    {
        UpdateDefines();
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        UpdateDefines();
    }

    static void UpdateDefines()
    {
        if (IsAssemblyLoaded(OpenCVAssemblyName))
            AddDefine(OpenCVDefine);
        else
            RemoveDefine(OpenCVDefine);
    }

    static bool IsAssemblyLoaded(string name)
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (assembly.GetName().Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static void AddDefine(string define)
    {
        var defines = GetDefines();
        if (!defines.Contains(define))
        {
            defines.Add(define);
            SetDefines(defines);
        }
    }

    static void RemoveDefine(string define)
    {
        var defines = GetDefines();
        if (defines.Contains(define))
        {
            defines.Remove(define);
            SetDefines(defines);
        }
    }

    static List<string> GetDefines()
    {
        var target = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
        var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
        return defines.Split(';').Where(d => !string.IsNullOrEmpty(d)).ToList();
    }

    static void SetDefines(List<string> definesList)
    {
        var target = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), string.Join(";", definesList));
    }
}

#endif
