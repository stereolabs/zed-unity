using UnityEngine;
#if UNITY_EDITOR
 using UnityEditor;
#endif

///This file contains classes used to add custom attributes to fields that will 
///cause them to be drawn differently in the Inspector, without the need for custom editors. 

/// <summary>
/// Adds a [LabelOverride(string)] attribute that causes a public field drawn in the default 
/// Inspector to have a customized label, rather than Unity generating one from the name. 
/// </summary>
public class LabelOverride : PropertyAttribute
{
    /// <summary>
    /// String to override the default label with.
    /// </summary>
    public string label;
    /// <summary>
    /// Tooltip to add to the label, if set. 
    /// </summary>
	public string optTooltip;

    /// <summary>
    /// Constructor. Called by the [LabelOverride(string)] tag with the params inside the parenthesis.
    /// </summary>
    /// <param name="label">String to override the default label with.</param>
    /// <param name="tooltip">Tooltip to add to the label, if set. </param>
	public LabelOverride(string label,string tooltip="")
    {
        this.label = label;
		this.optTooltip = tooltip;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for fields with a [LabelOverride(string)] attribute.
    /// The label on the drawer will be set to the label value in the parameter instead of the default one. 
    /// </summary>
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

/// <summary>
/// Adds a [ReadOnly(string)] attribute that will cause tagged fields to be drawn
/// with ReadOnlyDrawer in the Inspector, preventing them from being edited. 
/// Used by ZEDManager to draw the status texts ("Version, Engine FPS, etc.")
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
    /// <summary>
    /// String to override the default label with.
    /// </summary>
	public string label;

    /// <summary>
    /// Constructor. Called by the [ReadOnly(string)] tag with parameter in the parenthesis. 
    /// </summary>
    /// <param name="label"></param>
	public ReadOnlyAttribute(string label)
	{
		this.label = label;
	}
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for fields with a [ReadOnly(string)] attribute
/// that displays an uneditable text field in the Inspector. 
/// </summary>
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