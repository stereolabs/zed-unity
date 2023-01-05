using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ZEDFusionManager : MonoBehaviour
{
    [HideInInspector]
    public sl.InitFusionParameters InitFusionParameters;

    [HideInInspector]
    public float maxInputFps = 15.0f;

    [HideInInspector]
    public sl.COORDINATE_SYSTEM coordinateSystem = sl.COORDINATE_SYSTEM.LEFT_HANDED_Y_UP;

    [HideInInspector]
    public bool outputPerformanceMetrics = false;

    [HideInInspector]
    public bool enableSVOMode = false;

    [HideInInspector]
    public sl.ObjectDetectionFusionParameters objectDetectionFusionParameters;

    [HideInInspector]
    public sl.DETECTION_MODEL detectionModel = sl.DETECTION_MODEL.HUMAN_BODY_ACCURATE;

    [HideInInspector]
    public sl.BODY_FORMAT bodyFormat = sl.BODY_FORMAT.POSE_34;

    [HideInInspector]
    public bool enableTracking = true;

    [HideInInspector]
    public bool enableBodyFitting = true;

    [HideInInspector]
    public sl.ObjectDetectionFusionRuntimeParameters ObjectDetectionFusionRuntimeParameters;

    /// <summary>
    /// if the fused skeleton has less than skeleton_minimum_allowed_keypoints keypoints, it will be discarded. Default is -1.
    /// </summary>
    public int skeletonMinimumAllowedKeypoints;

    /// <summary>
    /// if a skeleton was detected in less than skeleton_minimum_allowed_camera cameras, it will be discarded
    /// </summary>
    public int skeletonMinimumAllowedCamera;

    /// <summary>
    /// this value controls the smoothing of the tracked or fitted fused skeleton.
    /// it is ranged from 0 (low smoothing) and 1 (high smoothing)
    /// </summary>
    public float skeletonSmoothing;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[CustomEditor(typeof(ZEDFusionManager))]
class ZEDFusionManagerEditor : Editor
{

    // Init Fusion Parameters

    SerializedProperty maxInputFps;

    SerializedProperty outputPerformanceMetrics;

    SerializedProperty enableSVOMode;

    // Object Detection Fusion Parameters

    SerializedProperty detectionModel;

    SerializedProperty enableTracking;

    SerializedProperty enableBodyFitting;

    // Object Detection Fusion Runtime Parameters

    SerializedProperty skeletonMinimumAllowedKeypoints;

    SerializedProperty skeletonMinimumAllowedCamera;

    SerializedProperty skeletonSmoothing;

    void OnEnable()
    {
        maxInputFps = serializedObject.FindProperty("maxInputFps");
        outputPerformanceMetrics = serializedObject.FindProperty("outputPerformanceMetrics");
        enableSVOMode = serializedObject.FindProperty("enableSVOMode");

        detectionModel = serializedObject.FindProperty("detectionModel");
        enableTracking = serializedObject.FindProperty("enableTracking");
        enableBodyFitting = serializedObject.FindProperty("enableBodyFitting");

        skeletonMinimumAllowedKeypoints = serializedObject.FindProperty("skeletonMinimumAllowedKeypoints");
        skeletonMinimumAllowedCamera = serializedObject.FindProperty("skeletonMinimumAllowedCamera");
        skeletonSmoothing = serializedObject.FindProperty("skeletonSmoothing");
    }

    public override void OnInspectorGUI()
    {
        /*        if (GUILayout.Button("Test"))
                {
                    Debug.Log("It's alive: " + target.name);
                }*/



        GUIStyle boldfoldout = new GUIStyle(EditorStyles.foldout);
        boldfoldout.fontStyle = FontStyle.Bold;

        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth * 0.4f;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Init Parameters", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(maxInputFps);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(outputPerformanceMetrics);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(enableSVOMode);
        serializedObject.ApplyModifiedProperties();

        EditorGUI.indentLevel--;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Object Detection Fusion Parameters", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(detectionModel);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(enableTracking);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(enableBodyFitting);
        serializedObject.ApplyModifiedProperties();

        EditorGUI.indentLevel--;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Object Detection Fusion Runtime Parameters", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(skeletonMinimumAllowedKeypoints);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(skeletonMinimumAllowedCamera);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.PropertyField(skeletonSmoothing);
        serializedObject.ApplyModifiedProperties();
    }
}