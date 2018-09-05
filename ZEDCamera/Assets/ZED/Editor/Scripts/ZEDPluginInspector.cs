//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Checks your system for the required ZED SDK version, and displays an error window with instructions if it's missing. 
/// Runs automatically when Unity loads. Remove the [InitializeOnLoad] tag to disable this.
/// </summary>
[InitializeOnLoad]
public class ZEDPluginInspector : EditorWindow
{
    /// <summary>
    /// ZED unity logo
    /// </summary>
    static Texture2D image = null;

    private static EditorWindow window;

	private static bool showErrorMode = false;
	private static bool showSettingsMode = false;
    private static string errorMessage = "";

	const bool forceSettingsShow = false;
	bool showErrorPlugin = false;

	const string ignore = "ignore.";
	const string useRecommended = "Use recommended ({0})";
	const string currentValue = " (current = {0})";

	const string buildTarget = "Build Target";
	const string showUnitySplashScreen = "Show Unity Splashscreen";
	const string displayResolutionDialog = "Display Resolution Dialog";
	const string resizableWindow = "Resizable Window";
	const string colorSpace = "Color Space";
	const string gpuSkinning = "GPU Skinning";
	const string MSAAValue = "Anti Aliasing";
	const string runInBackground = "Run In Background";
	const string visibleInBackground = "Visible In Background";


	const BuildTarget needed_BuildTarget = BuildTarget.StandaloneWindows64;
	const bool needed_ShowUnitySplashScreen = false;
	const ResolutionDialogSetting needed_DisplayResolutionWindow = ResolutionDialogSetting.HiddenByDefault;
	const bool needed_ResizableWindow = true;
	const ColorSpace needed_ColorSpace = ColorSpace.Linear;
	const bool needed_GPUSkinning = true;
	const int needed_MSAAValue = 4;
	const bool needed_RunInBackground = true;
	const bool  needed_VisibleInBackground = true;
	static ZEDPluginInspector()
    {
		EditorApplication.update += OnInit;
    }

	static void OnInit()
	{
		EditorApplication.update -= OnInit;
		if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating) {
			if (!EditorPrefs.HasKey("ZED_NoWarning_Plugin"))
				EditorApplication.update += UpdateWnd;
		}
		else
			EditorApplication.update += UpdateLog;

		EditorApplication.update += UpdateSettingsWnd;

	}

	void OnEnable()
	{
		addMissingTag ();
	}

    /// <summary>
    /// Makes sure the project's tags are loaded, as they are used in some samples but may get deleted on import or
    /// if shared via source control. 
    /// </summary>
	static public void addMissingTag()
	{
		// Open tag manager
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		SerializedProperty tagsProp = tagManager.FindProperty("tags");
		// Adding a Tag
		string s = "HelpObject";

		// Check if not here already
		bool found = false;
		for (int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if (t.stringValue.Equals(s)) { found = true; break; }
		}

		//If not found, add it since we use it in GreenScreen. 
		//This tag may be used anywhere, since it tags helper object that may have a specific behavior

		if (!found)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
			n.stringValue = s;
		}


		// and to save the changes
		tagManager.ApplyModifiedProperties();
	}

	static void UpdateLog()
	{
		if (!sl.ZEDCamera.CheckPlugin ()) {
			Debug.Log ("ZED SDK is not installed or needs to be updated");
		}

		EditorApplication.update -= UpdateLog;
	}



	static void UpdateSettingsWnd()
	{
		showSettingsMode =
			(!EditorPrefs.HasKey(ignore + buildTarget) &&
				EditorUserBuildSettings.activeBuildTarget != needed_BuildTarget) ||
			(!EditorPrefs.HasKey(ignore + showUnitySplashScreen) &&
				PlayerSettings.SplashScreen.show != needed_ShowUnitySplashScreen) ||
			(!EditorPrefs.HasKey(ignore + displayResolutionDialog) &&
				PlayerSettings.displayResolutionDialog != needed_DisplayResolutionWindow) ||
			(!EditorPrefs.HasKey(ignore + resizableWindow) &&
				PlayerSettings.resizableWindow != needed_ResizableWindow) ||
			(!EditorPrefs.HasKey(ignore + colorSpace) &&
				PlayerSettings.colorSpace != needed_ColorSpace) ||
			(!EditorPrefs.HasKey(ignore + gpuSkinning) &&
				PlayerSettings.gpuSkinning != needed_GPUSkinning) ||
			(!EditorPrefs.HasKey(ignore + runInBackground) &&
				PlayerSettings.runInBackground != needed_RunInBackground) ||
			(!EditorPrefs.HasKey(ignore + visibleInBackground) &&
				PlayerSettings.visibleInBackground != needed_VisibleInBackground) ||
			(!EditorPrefs.HasKey(ignore + MSAAValue) &&
				QualitySettings.antiAliasing != needed_MSAAValue) ||			
			forceSettingsShow;

		if (showSettingsMode) {
			window = GetWindow<ZEDPluginInspector> (true);
			window.maxSize = new Vector2 (400, 400);
			window.minSize = window.maxSize;
			window.Show (true);
		}


		EditorApplication.update -= UpdateSettingsWnd;
	}

	static void UpdateWnd()
    {
		
		if (!sl.ZEDCamera.CheckPlugin ()) {
				errorMessage = ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.SDK_DEPENDENCIES_ISSUE);
				showErrorMode = true;
				window = GetWindow<ZEDPluginInspector> (true);
				window.maxSize = new Vector2 (400, 600);
				window.minSize = window.maxSize;
				window.Show (true);
		} 

		EditorApplication.update -= UpdateWnd;

    }



    void OnGUI()
	{
		if (showErrorMode) {
			showErrorWindow ();
		} else if (showSettingsMode) {
			showSettingsWindow();
		}
	}

    /// <summary>
    /// Displays a popup window when the correct ZED SDK version isn't installed.
    /// </summary>
	public void showErrorWindow()
	{
		if (image == null) {
			image = Resources.Load ("Textures/logo", typeof(Texture2D)) as Texture2D;

		}
		var rect = GUILayoutUtility.GetRect (position.width, 150, GUI.skin.box);

		if (image) {
			GUI.DrawTexture (rect, image, ScaleMode.ScaleToFit);
		}
		GUIStyle myStyle = new GUIStyle (GUI.skin.label);
		myStyle.normal.textColor = Color.red;
		myStyle.fontStyle = FontStyle.Bold;

		GUILayout.Space (20);
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.Label ("ZED SDK is not installed or needs to be updated", myStyle);
		GUILayout.FlexibleSpace ();
		GUILayout.EndHorizontal ();
		myStyle = new GUIStyle (GUI.skin.box);
		myStyle.normal.textColor = Color.red;


		GUI.Box (new Rect (0, position.height / 2, position.width, 100), errorMessage, myStyle);
 
		GUILayout.FlexibleSpace ();
		GUILayout.BeginHorizontal ();
		myStyle.normal.textColor = Color.black;
		myStyle.fontStyle = FontStyle.Bold;
		GUILayout.Label ("Do not ask me again...");
		showErrorPlugin = EditorGUILayout.Toggle ("",showErrorPlugin);
		GUILayout.EndHorizontal ();


		GUILayout.Space (10);
		if (GUILayout.Button ("Close")) {

			if (showErrorPlugin) {
				EditorPrefs.SetBool ("ZED_NoWarning_Plugin", true);
			}

			this.Close ();
		}

	
		


	}


	Vector2 scrollPosition;
    /// <summary>
    /// Shows a window prompting the user to change project settings to recommended settings, with
    /// buttons to automatically do so. 
    /// </summary>
	public void showSettingsWindow()
	{
		if (image == null) {
			image = Resources.Load ("Textures/logo", typeof(Texture2D)) as Texture2D;

		}
		var rect = GUILayoutUtility.GetRect (position.width, 150, GUI.skin.box);

		if (image) {
			GUI.DrawTexture (rect, image, ScaleMode.ScaleToFit);
		}

		EditorGUILayout.HelpBox("Recommended project settings for ZED Unity Plugin", MessageType.Warning);

		scrollPosition = GUILayout.BeginScrollView(scrollPosition);

		int numItems = 0;

		if (!EditorPrefs.HasKey(ignore + buildTarget) &&
			EditorUserBuildSettings.activeBuildTarget != needed_BuildTarget)
		{
			++numItems;

			GUILayout.Label(buildTarget + string.Format(currentValue, EditorUserBuildSettings.activeBuildTarget));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_BuildTarget)))
			{
				EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, needed_BuildTarget);
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + buildTarget, true);
			}

			GUILayout.EndHorizontal();
		}

		if (!EditorPrefs.HasKey(ignore + showUnitySplashScreen) &&
			PlayerSettings.SplashScreen.show != needed_ShowUnitySplashScreen)
		{
			++numItems;

			GUILayout.Label(showUnitySplashScreen + string.Format(currentValue, PlayerSettings.SplashScreen.show));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_ShowUnitySplashScreen)))
			{
				PlayerSettings.SplashScreen.show = needed_ShowUnitySplashScreen;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + showUnitySplashScreen, true);
			}

			GUILayout.EndHorizontal();
		}
	
		if (!EditorPrefs.HasKey(ignore + displayResolutionDialog) &&
			PlayerSettings.displayResolutionDialog != needed_DisplayResolutionWindow)
		{
			++numItems;

			GUILayout.Label(displayResolutionDialog + string.Format(currentValue, PlayerSettings.displayResolutionDialog));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_DisplayResolutionWindow)))
			{
				PlayerSettings.displayResolutionDialog = needed_DisplayResolutionWindow;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + displayResolutionDialog, true);
			}

			GUILayout.EndHorizontal();
		}

		if (!EditorPrefs.HasKey(ignore + resizableWindow) &&
			PlayerSettings.resizableWindow != needed_ResizableWindow)
		{
			++numItems;

			GUILayout.Label(resizableWindow + string.Format(currentValue, PlayerSettings.resizableWindow));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_ResizableWindow)))
			{
				PlayerSettings.resizableWindow = needed_ResizableWindow;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + resizableWindow, true);
			}

			GUILayout.EndHorizontal();
		}

		if (!EditorPrefs.HasKey(ignore + visibleInBackground) &&
			PlayerSettings.visibleInBackground != needed_VisibleInBackground)
		{
			++numItems;

			GUILayout.Label(visibleInBackground + string.Format(currentValue, PlayerSettings.visibleInBackground));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_VisibleInBackground)))
			{
				PlayerSettings.visibleInBackground = needed_VisibleInBackground;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + visibleInBackground, true);
			}

			GUILayout.EndHorizontal();
		}

		if (!EditorPrefs.HasKey(ignore + runInBackground) &&
			PlayerSettings.runInBackground != needed_RunInBackground)
		{
			++numItems;

			GUILayout.Label(runInBackground + string.Format(currentValue, PlayerSettings.runInBackground));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_RunInBackground)))
			{
				PlayerSettings.runInBackground = needed_RunInBackground;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + runInBackground, true);
			}

			GUILayout.EndHorizontal();
		}


		if (!EditorPrefs.HasKey(ignore + gpuSkinning) &&
			PlayerSettings.gpuSkinning != needed_GPUSkinning)
		{
			++numItems;

			GUILayout.Label(gpuSkinning + string.Format(currentValue, PlayerSettings.gpuSkinning));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_GPUSkinning)))
			{
				PlayerSettings.gpuSkinning = needed_GPUSkinning;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + gpuSkinning, true);
			}

			GUILayout.EndHorizontal();
		}

		if (!EditorPrefs.HasKey(ignore + colorSpace) &&
			PlayerSettings.colorSpace != needed_ColorSpace)
		{
			++numItems;

			GUILayout.Label(colorSpace + string.Format(currentValue, PlayerSettings.colorSpace));

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_ColorSpace) + " - requires reloading scene"))
			{
				PlayerSettings.colorSpace = needed_ColorSpace;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + colorSpace, true);
			}

			GUILayout.EndHorizontal();
		}


		if (!EditorPrefs.HasKey(ignore + MSAAValue) &&
			QualitySettings.antiAliasing != needed_MSAAValue)
		{
			++numItems;

			GUILayout.Label(MSAAValue + string.Format(currentValue, QualitySettings.antiAliasing)+"x Multi Sampling");

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format(useRecommended, needed_MSAAValue)))
			{
				QualitySettings.antiAliasing = needed_MSAAValue;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Ignore"))
			{
				EditorPrefs.SetBool(ignore + MSAAValue, true);
			}

			GUILayout.EndHorizontal();
		}


		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		if (GUILayout.Button("Clear All Ignores"))
		{
			EditorPrefs.DeleteKey(ignore + buildTarget);
			EditorPrefs.DeleteKey(ignore + showUnitySplashScreen);
			EditorPrefs.DeleteKey(ignore + displayResolutionDialog);
			EditorPrefs.DeleteKey(ignore + resizableWindow);
			EditorPrefs.DeleteKey(ignore + colorSpace);
			EditorPrefs.DeleteKey(ignore + gpuSkinning);
			EditorPrefs.DeleteKey(ignore + MSAAValue);
			EditorPrefs.DeleteKey(ignore + visibleInBackground);
			EditorPrefs.DeleteKey(ignore + runInBackground);
			EditorPrefs.DeleteKey(ignore + MSAAValue);

		}

 
		GUILayout.EndHorizontal();

		GUILayout.EndScrollView();

		GUILayout.FlexibleSpace();

		GUILayout.BeginHorizontal();

		if (numItems > 0)
		{
			if (GUILayout.Button("Accept All"))
			{
				// Only set those that have not been explicitly ignored.
				if (!EditorPrefs.HasKey(ignore + buildTarget))
					EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, needed_BuildTarget);
				if (!EditorPrefs.HasKey(ignore + showUnitySplashScreen))
					PlayerSettings.SplashScreen.show = needed_ShowUnitySplashScreen;		
				if (!EditorPrefs.HasKey(ignore + displayResolutionDialog))
					PlayerSettings.displayResolutionDialog = needed_DisplayResolutionWindow;
				if (!EditorPrefs.HasKey(ignore + resizableWindow))
					PlayerSettings.resizableWindow = needed_ResizableWindow;
				if (!EditorPrefs.HasKey(ignore + colorSpace))
					PlayerSettings.colorSpace = needed_ColorSpace;
				if (!EditorPrefs.HasKey(ignore + gpuSkinning))
					PlayerSettings.gpuSkinning = needed_GPUSkinning;
				if (!EditorPrefs.HasKey(ignore + runInBackground))
					PlayerSettings.runInBackground = needed_RunInBackground;
				if (!EditorPrefs.HasKey(ignore + visibleInBackground))
					PlayerSettings.visibleInBackground = needed_VisibleInBackground;
				if (!EditorPrefs.HasKey(ignore + MSAAValue))
					QualitySettings.antiAliasing = needed_MSAAValue;

				EditorUtility.DisplayDialog("Accept All", "Settings applied", "Ok");
				Close();
			}

			if (GUILayout.Button("Ignore All"))
			{
				if (EditorUtility.DisplayDialog("Ignore All", "Are you sure?", "Yes, Ignore All", "Cancel"))
				{
					// Only ignore those that do not currently match our recommended settings.
					if (EditorUserBuildSettings.activeBuildTarget != needed_BuildTarget)
						EditorPrefs.SetBool(ignore + buildTarget, true);
					if (PlayerSettings.SplashScreen.show != needed_ShowUnitySplashScreen)
						EditorPrefs.SetBool(ignore + showUnitySplashScreen, true);
					if (PlayerSettings.displayResolutionDialog != needed_DisplayResolutionWindow)
						EditorPrefs.SetBool(ignore + displayResolutionDialog, true);
					if (PlayerSettings.resizableWindow != needed_ResizableWindow)
						EditorPrefs.SetBool(ignore + resizableWindow, true);
					if (PlayerSettings.colorSpace != needed_ColorSpace)
						EditorPrefs.SetBool(ignore + colorSpace, true);
					if (PlayerSettings.gpuSkinning != needed_GPUSkinning)
						EditorPrefs.SetBool(ignore + gpuSkinning, true);
					if (PlayerSettings.runInBackground != needed_RunInBackground)
						EditorPrefs.SetBool(ignore + runInBackground, true);
					if (PlayerSettings.visibleInBackground != needed_VisibleInBackground)
						EditorPrefs.SetBool(ignore + visibleInBackground, true);
					if (QualitySettings.antiAliasing != needed_MSAAValue)
						EditorPrefs.SetBool(ignore + MSAAValue, true);

					Close();
				}
			}
		}
		else if (GUILayout.Button("Close"))
		{
			Close();
		}

		GUILayout.EndHorizontal();
	}
}

