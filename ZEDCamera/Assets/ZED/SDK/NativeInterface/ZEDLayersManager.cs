#if UNITY_EDITOR
using UnityEditor;


/// <summary>
/// This class creates automaticaly layers on load
/// </summary>

[InitializeOnLoad]
public static class ZEDLayersManager
{
    public static int tagInvisibleToZED = 16;
    public static string ID_tagInvisibleToZED = "tagInvisibleToZED";
    public static int arlayer = 30;
    public static string ID_arlayer = "arlayer";


    static ZEDLayersManager()
    {
        CreateLayer(ID_tagInvisibleToZED, tagInvisibleToZED);
        CreateLayer(ID_arlayer, arlayer);
    }

    public static void CreateLayer(string layerName, int layerIndex)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (layerIndex < 7 || layerIndex > 31) return; //Invalid ID. 
        if ((asset != null) && (asset.Length > 0))
        {
            SerializedObject serializedObject = new SerializedObject(asset[0]);
            SerializedProperty layers = serializedObject.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; ++i)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    layerIndex = i;
                    return;     // Layer already present, update layerindex value.
                }
            }

            if (layers.GetArrayElementAtIndex(layerIndex).stringValue == "")
            {
                layers.GetArrayElementAtIndex(layerIndex).stringValue = layerName;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                if (layers.GetArrayElementAtIndex(layerIndex).stringValue == layerName)
                {
                    return;     // to avoid unity locked layer
                }
            }
        }
    }

    public static void ClearLayer(string layerName)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if ((asset != null) && (asset.Length > 0))
        {
            SerializedObject serializedObject = new SerializedObject(asset[0]);
            SerializedProperty layers = serializedObject.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; ++i)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    layers.GetArrayElementAtIndex(i).stringValue = "";
                }
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}
#endif