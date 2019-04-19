#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Custom Editor that extends the default settings shown in the Inspector. 
/// Allows it to add buttons and hidden elements for the garbage matte and config file. 
/// </summary>
[CustomEditor(typeof(GreenScreenManager))]
class GreenScreenManagerEditor : Editor
{
    private GreenScreenManager greenScreen;
    private GUILayoutOption[] optionsButton = { GUILayout.MaxWidth(100) };

    private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

    private SerializedProperty keyColors;
    private SerializedProperty range;
    private SerializedProperty smoothness;
    private SerializedProperty whiteclip;
    private SerializedProperty blackclip;
    private SerializedProperty erosion;
    private SerializedProperty sigma;
    private SerializedProperty despill;
    private SerializedProperty pathfileconfig;


    private GarbageMatte matte;
    private SerializedProperty enableGrabageMatte;


    private static GUIStyle ToggleButtonStyleNormal = null;
    private static GUIStyle ToggleButtonStyleToggled = null;

    public void OnEnable()
    {
        greenScreen = (GreenScreenManager)target;

        //Create serialized properties for all the GreenScreenManager's fields so they can be modified and saved/serialized properly. 
        keyColors = serializedObject.FindProperty("keyColors");
        erosion = serializedObject.FindProperty("erosion");

        range = serializedObject.FindProperty("range");
        smoothness = serializedObject.FindProperty("smoothness");
        whiteclip = serializedObject.FindProperty("whiteClip");
        blackclip = serializedObject.FindProperty("blackClip");
        sigma = serializedObject.FindProperty("sigma_");
        despill = serializedObject.FindProperty("spill");
        pathfileconfig = serializedObject.FindProperty("pathFileConfig");

        enableGrabageMatte = serializedObject.FindProperty("enableGarbageMatte");


    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        greenScreen = (GreenScreenManager)target;
        key_colors();
    

        if (ToggleButtonStyleNormal == null)
        {
            ToggleButtonStyleNormal = "Button";
            ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
            ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
        }

        //matte = (ZEDGarbageMatte)target;  
        matte = greenScreen.garbageMatte;
        GUI.enabled = greenScreen.screenManager != null && greenScreen.screenManager.ActualRenderingPath == RenderingPath.Forward;
        enableGrabageMatte.boolValue = EditorGUILayout.Toggle("Enable Garbage Matte", enableGrabageMatte.boolValue);
        GUI.enabled = true;

        //Show the garbage matte section, only if the scene is running and it's enabled. 
        if (enableGrabageMatte.boolValue && greenScreen.screenManager != null && greenScreen.screenManager.ActualRenderingPath == RenderingPath.Forward)
        {
            //serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Markers", matte.editMode ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                matte.editMode = !matte.editMode;
                if (matte.editMode)
                {
                    matte.EnterEditMode();
                }

            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Last Marker"))
            {
                matte.RemoveLastPoint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Garbage Matte"))
            {
                matte.ApplyGarbageMatte();
            }
            EditorGUILayout.EndHorizontal();

        
            GUI.enabled = true;

            if (GUILayout.Button("Reset"))
            {
                matte.ResetPoints(true);
            }
        } 

        //Draw the configuration file path and Save/Load buttons. 
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        pathfileconfig.stringValue = EditorGUILayout.TextField("Save Config", greenScreen.pathFileConfig);
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("...", optionsButtonBrowse))
        {
            pathfileconfig.stringValue = EditorUtility.OpenFilePanel("Load save file", "", "json");
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save"))
        {
            greenScreen.SaveData(greenScreen.RegisterDataChromaKeys(), greenScreen.garbageMatte.RegisterData());
        }
        GUI.enabled = File.Exists(greenScreen.pathFileConfig);
        if (GUILayout.Button("Load"))
        {
            greenScreen.LoadGreenScreenData(true);

            keyColors.colorValue = greenScreen.keyColors;
            range.floatValue = greenScreen.range;
            smoothness.floatValue = greenScreen.smoothness;
            whiteclip.floatValue = greenScreen.whiteClip;
            blackclip.floatValue = greenScreen.blackClip;
            erosion.intValue = greenScreen.erosion;
            sigma.floatValue = greenScreen.sigma_;
            despill.floatValue = greenScreen.spill;
        }
		if (GUILayout.Button ("Clean")) {
			matte.CleanSpheres ();
		}
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();
    }

    void key_colors()
    {
        //GUILayout.Label("Chroma Key", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        greenScreen.canal = (GreenScreenManager.CANAL)EditorGUILayout.EnumPopup("View", greenScreen.canal);
        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
        {
            greenScreen.UpdateCanal();
        }
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Screen", EditorStyles.boldLabel);

        keyColors.colorValue = EditorGUILayout.ColorField("Color", keyColors.colorValue);

        range.floatValue = EditorGUILayout.Slider("Range", range.floatValue, 0.0f, 1.0f);
        smoothness.floatValue = EditorGUILayout.Slider("Smoothness", smoothness.floatValue, 0, 1.0f);

        EditorGUILayout.Space();
        GUILayout.Label("Foreground", EditorStyles.boldLabel);
        whiteclip.floatValue = EditorGUILayout.Slider("Clip White", whiteclip.floatValue, 0.0f, 1.0f);
        blackclip.floatValue = EditorGUILayout.Slider("Clip Black", blackclip.floatValue, 0.0f, 1.0f);

        erosion.intValue = EditorGUILayout.IntSlider("Erosion", erosion.intValue, 0, 5);
        //sigma.floatValue = EditorGUILayout.Slider("Edges Softness", sigma.floatValue, 0.1f, 2.0f);

        despill.floatValue = EditorGUILayout.Slider("Despill", despill.floatValue, 0f, 1f);

        //Button to reset to default settings. 
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Default", optionsButton))
        {
            greenScreen.SetDefaultValues();

            keyColors.colorValue = greenScreen.keyColors;
            range.floatValue = greenScreen.range;
            smoothness.floatValue = greenScreen.smoothness;
            whiteclip.floatValue = greenScreen.whiteClip;
            blackclip.floatValue = greenScreen.blackClip;
            erosion.intValue = greenScreen.erosion;
            sigma.floatValue = greenScreen.sigma_;
            despill.floatValue = greenScreen.spill;

            Repaint();
        }
        GUILayout.EndHorizontal();


        if (EditorGUI.EndChangeCheck())
        {
            greenScreen.UpdateShader();
        }


    }
}
#endif