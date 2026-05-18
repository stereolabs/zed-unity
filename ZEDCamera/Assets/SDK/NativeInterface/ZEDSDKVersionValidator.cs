//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace sl
{
    /// <summary>
    /// Detects the installed ZED SDK version via filesystem (no native DLL loading)
    /// and blocks ZED initialization if the version is incompatible with this plugin.
    ///
    /// This runs at SubsystemRegistration time, before any MonoBehaviour.Awake,
    /// so ZEDManager can check IsSDKCompatible before triggering any P/Invoke calls
    /// that would load the native DLLs and potentially crash.
    /// </summary>
    public static class ZEDSDKVersionValidator
    {
        public static bool IsSDKCompatible { get; private set; } = false;
        public static bool ValidationComplete { get; private set; } = false;
        public static string InstalledSDKVersion { get; private set; } = "unknown";
        public static string RequiredSDKVersion => $"{ZEDCamera.PluginVersion.Major}.{ZEDCamera.PluginVersion.Minor}";
        public static string DetailedMessage { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ValidateOnStartup()
        {
            Validate();
        }

        public static void Validate()
        {
            ValidationComplete = false;
            IsSDKCompatible = false;

            try
            {
                string sdkRoot = Environment.GetEnvironmentVariable("ZED_SDK_ROOT_DIR");
                if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    InstalledSDKVersion = "not found";
                    DetailedMessage = "ZED SDK is not installed (ZED_SDK_ROOT_DIR environment variable not set). " +
                        $"This plugin requires ZED SDK v{RequiredSDKVersion}. " +
                        "Download it from https://www.stereolabs.com/developers/release";
                    ValidationComplete = true;
                    Debug.LogError($"[ZED Plugin] {DetailedMessage}");
                    return;
                }

                string versionFile = Path.Combine(sdkRoot, "zed-config-version.cmake");
                if (!File.Exists(versionFile))
                {
                    InstalledSDKVersion = "unknown";
                    DetailedMessage = "Could not determine installed ZED SDK version. " +
                        "The ZED SDK installation may be corrupted. " +
                        $"This plugin requires ZED SDK v{RequiredSDKVersion}.";
                    ValidationComplete = true;
                    IsSDKCompatible = true;
                    Debug.LogWarning($"[ZED Plugin] {DetailedMessage}");
                    return;
                }

                string content = File.ReadAllText(versionFile);
                var match = Regex.Match(content, @"set\(PACKAGE_VERSION\s+""(\d+)\.(\d+)\.(\d+)""\)");
                if (!match.Success)
                {
                    InstalledSDKVersion = "unknown";
                    DetailedMessage = "Could not parse ZED SDK version from installation.";
                    ValidationComplete = true;
                    IsSDKCompatible = true;
                    Debug.LogWarning($"[ZED Plugin] {DetailedMessage}");
                    return;
                }

                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int patch = int.Parse(match.Groups[3].Value);
                InstalledSDKVersion = $"{major}.{minor}.{patch}";

                if (major == ZEDCamera.PluginVersion.Major && minor == ZEDCamera.PluginVersion.Minor)
                {
                    IsSDKCompatible = true;
                    DetailedMessage = "";
                }
                else
                {
                    IsSDKCompatible = false;
                    DetailedMessage = $"ZED SDK version mismatch: installed v{InstalledSDKVersion}, " +
                        $"but this plugin requires v{RequiredSDKVersion}.x. " +
                        $"Please install ZED SDK v{RequiredSDKVersion} from https://www.stereolabs.com/developers/release " +
                        "or update the ZED Unity plugin to match your installed SDK.";
                    Debug.LogError($"[ZED Plugin] {DetailedMessage}");
                }
            }
            catch (Exception e)
            {
                InstalledSDKVersion = "error";
                DetailedMessage = $"Error checking ZED SDK version: {e.Message}";
                IsSDKCompatible = true;
                Debug.LogWarning($"[ZED Plugin] {DetailedMessage}");
            }

            ValidationComplete = true;
        }
    }
}
