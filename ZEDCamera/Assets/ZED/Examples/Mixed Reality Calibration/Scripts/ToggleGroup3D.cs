using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages multiple ToggleButtons. When one requests a push, it causes the current toggled button to be untoggled
/// before calling that button's toggle method. Similar to UI.ToggleGroup for Unity's 2D canvases. 
/// </summary>
public class ToggleGroup3D : MonoBehaviour
{
    /// <summary>
    /// List of all toggle buttons within the group.
    /// </summary>
    [Tooltip("List of all toggle buttons within the group. ")]
    public List<ToggleButton> buttons = new List<ToggleButton>();

    /// <summary>
    /// Index of the currently toggled button.
    /// </summary>
    [Tooltip("Index of the currently toggled button.")]
    public int toggledIndex = 0;

    /// <summary>
    /// Whether to call the toggle action of the button toggledIndex in Start(). 
    /// Usually best to set to true, but you may want to set its toggle effects elsewhere for timing reasons. 
    /// </summary>
    [Tooltip("Whether to call the toggle action of the button toggledIndex in Start(). " +
        "Usually best to set to true, but you may want to set its toggle effects elsewhere for timing reasons.")]
    public bool toggleAtStart = false;

	// Use this for initialization
	void Start ()
    {
        for(int i = 0; i < buttons.Count; i++)
        {
            buttons[i].toggleGroup = this;
            buttons[i].index = i;
        }

        if(toggleAtStart) ToggleNewButton(toggledIndex);
	}
	
    /// <summary>
    /// Changes the toggle index to a new button, calling all relevant toggle/untoggle methods in all buttons. 
    /// </summary>
	public void ToggleNewButton(int index)
    {
        if(buttons.Count <= index)
        {
            throw new System.Exception("Called ToggleNewButton with index " + index + " but there are only " + buttons.Count + " buttons registered.");
        }

        for(int i = 0; i < buttons.Count; i++)
        {
            buttons[i].ChangeToggleState(i == index);
        }

        toggledIndex = index;
    }
}
