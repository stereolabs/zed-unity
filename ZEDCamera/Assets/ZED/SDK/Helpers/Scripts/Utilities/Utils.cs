using UnityEngine;
#if UNITY_EDITOR
 using UnityEditor;
#endif
public class LabelOverride : PropertyAttribute
{
    public string label;
	public string optTooltip;
	public LabelOverride(string label,string tooltip="")
    {
        this.label = label;
		this.optTooltip = tooltip;
    }

#if UNITY_EDITOR
     [CustomPropertyDrawer( typeof(LabelOverride) )]
     public class ThisPropertyDrawer : PropertyDrawer
     {
         public override void OnGUI ( Rect position , SerializedProperty property , GUIContent label )
         {
             var propertyAttribute = this.attribute as LabelOverride;
             label.text = propertyAttribute.label;
			 label.tooltip = propertyAttribute.optTooltip;
             EditorGUI.PropertyField( position , property , label );
         }
     }
#endif
}


public class ReadOnlyAttribute : PropertyAttribute
{
	public string label;
	public ReadOnlyAttribute(string label)
	{
		this.label = label;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{

  
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        GUI.enabled = false;
		var propertyAttribute = this.attribute as ReadOnlyAttribute;
		label.text = propertyAttribute.label;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
 
}
#endif