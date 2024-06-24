using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using System.Linq;
using sl;
using System.IO;

public class ZEDImporters : Editor
{
    static readonly string PackageName = "com.stereolabs.zed";
    static readonly string PackageVersion = ZEDCamera.PluginVersion.ToString();

    [MenuItem("ZED/Import All Samples", false, 0)]
    public static void ImportAllSamplesFromPackage()
    {
        var samples = Sample.FindByPackage(PackageName, PackageVersion);

        if(samples.Any<Sample>() == false)
        {
            Debug.LogWarning("No samples found");
            return;
        }

        foreach (var sample in samples)
        {
            Debug.Log($"Importing ZED Sample: {sample.displayName}");
            sample.Import();
        }

        Debug.Log("ZED Samples imported");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("ZED/Import Prefabs", false, 1)]
    public static void CopyPrefabsToZEDPrefabs()
    {
        string sourcePath = $"Packages/{PackageName}/Prefabs";
        string destinationPath = "Assets/ZED/Prefabs";

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError($"Source path does not exist: {sourcePath}");
            return;
        }

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        CopyDirectory(sourcePath, destinationPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ZED Prefabs Import");
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            Debug.LogError($"Source dir does not exist: {sourceDir}");
            return;
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // Get the files in the directory and copy them to the new location
        foreach (FileInfo file in dir.GetFiles())
        {
            string tempPath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(tempPath, false);
        }

        // Recursively copy subdirectories
        foreach (DirectoryInfo subdir in dirs)
        {
            string tempPath = Path.Combine(destinationDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}