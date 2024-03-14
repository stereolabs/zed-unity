using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 3D button with an on and off state. Meant to be tied to a ToggleGroup3D. 
/// Only pressable when not already pressed - relies on ToggleGroup3D to signify when another button has been pressed
/// in order to raise this one. Similar to UI.Toggle and UI.ToggleGroup for the 2D canvas. 
/// <para>Used in the ZED MR Calibration scene for the Mode and Hands toggles.</para>
/// <para>See parent class Button3D for hover effects and basic interactions.</para>
/// </summary>
public class ToggleButton : Button3D
{
    /// <summary>
    /// Toggle group that this button belongs to.
    /// </summary>
    [Tooltip("Toggle group that this button belongs to.")]
    public ToggleGroup3D toggleGroup;
    /// <summary>
    /// Index of this button within the toggle group. Not set by ToggleGroup3D: should be set manually or via a different script. 
    /// </summary>
    [Tooltip("Index of this button within the toggle group. Not set by ToggleGroup3D: should be set manually or via a different script.")]
    public int index;

    /// <summary>
    /// Events called when this button just became toggled.
    /// </summary>
    [Tooltip("Events called when this button just became toggled. ")]
    [Space(5)]
    public UnityEvent OnToggled;
    /// <summary>
    /// Events called when this button just because un-toggled.
    /// </summary>
    [Tooltip("Events called when this button just because un-toggled.")]
    public UnityEvent OnUnToggled;

    /// <summary>
    /// What happens when this object is clicked. From IXRClickable. 
    /// </summary>
    /// <param name="clicker"></param>
    public override void OnClick(ZEDXRGrabber clicker)
    {
        RequestStateChange();
    }

    /// <summary>
    /// Tells the applicable ToggleGroup3D that this button was pressed and should be switched to. 
    /// </summary>
    public void RequestStateChange()
    {
        toggleGroup.ToggleNewButton(index);
    }

    /// <summary>
    /// Set cosmetics and invokes relevant event for a new toggle state. Called by ToggleGroup3D,
    /// usually after RequestStateChange is called here. 
    /// </summary>
    public void ChangeToggleState(bool state)
    {
        if(state == true)
        {
            col.enabled = false;
            brightness = pressedDarkness;
            transform.localScale = new Vector3(startScale.x * pressedScaleMult.x, startScale.y * pressedScaleMult.y, startScale.z * pressedScaleMult.z);

            OnToggled.Invoke();
        }
        else //State = false.
        {
            col.enabled = true;
            brightness = unpressedDarkness;
            transform.localScale = startScale;

            OnUnToggled.Invoke();
        }
    }



}
