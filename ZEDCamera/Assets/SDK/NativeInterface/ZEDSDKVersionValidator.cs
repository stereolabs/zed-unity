//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System;
using System.IO;
using System.Runtime.InteropServices;
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
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
        const uint MB_OK = 0x00000000;
        const uint MB_ICONERROR = 0x00000010;
#endif

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

        static void HandleRuntimeError(string message)
        {
#if !UNITY_EDITOR
#if UNITY_STANDALONE_WIN
            try
            {
                MessageBox(IntPtr.Zero, message, "ZED Plugin - SDK Version Error", MB_OK | MB_ICONERROR);
            }
            catch (Exception) { }
#endif
            Application.Quit(1);
#endif
        }

        static string FindSDKRoot()
        {
            string defaultPath;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            defaultPath = @"C:\Program Files (x86)\ZED SDK";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            defaultPath = "/usr/local/zed";
#else
            defaultPath = null;
#endif
            if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
                return defaultPath;

            string envRoot = Environment.GetEnvironmentVariable("ZED_SDK_ROOT_DIR");
            if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot))
                return envRoot;

            return null;
        }

        public static void Validate()
        {
            ValidationComplete = false;
            IsSDKCompatible = false;

            try
            {
                string sdkRoot = FindSDKRoot();
                if (sdkRoot == null)
                {
                    InstalledSDKVersion = "not found";
                    DetailedMessage = "ZED SDK is not installed. " +
                        $"This plugin requires ZED SDK v{RequiredSDKVersion}. " +
                        "Download it from https://www.stereolabs.com/developers/release";
                    ValidationComplete = true;
                    Debug.LogError($"[ZED Plugin] {DetailedMessage}");
                    HandleRuntimeError(DetailedMessage);
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
                    HandleRuntimeError(DetailedMessage);
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
