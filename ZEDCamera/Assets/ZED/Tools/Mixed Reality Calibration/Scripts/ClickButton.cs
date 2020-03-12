using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 3D button that, when clicked by ZEDXRGrabber, fires a single event, and plays an animation to 
/// depress it temporarily, like a real button sliding into its slot. Also darkens it slightly. 
/// See parent class, Button3D, for more details. 
/// </summary>
public class ClickButton : Button3D
{
    /// <summary>
    /// Called when the button is clicked by ZEDXRGrabber. 
    /// </summary>
    public UnityEvent OnClicked;

    [Space(5)]
    public float pressedSeconds = 0.2f;

    /// <summary>
    /// Invoke the event and play the animation that shrinks and darkens the button temporarily. 
    /// </summary>
    /// <param name="clicker"></param>
    public override void OnClick(ZEDXRGrabber clicker)
    {
        OnClicked.Invoke();

        StartCoroutine(DisplayClick());
    }

    /// <summary>
    /// Darkens the button slightly and causes it to shrink, as if depressing into its slot. 
    /// Happens as an animation over time. 
    /// </summary>
    private IEnumerator DisplayClick()
    {
        col.enabled = false;
        brightness = pressedDarkness;

        Vector3 scalediff = Vector3.one - pressedScaleMult;

        for (float t = 0; t < pressedSeconds / 2f; t += Time.deltaTime)
        {
            transform.localScale -= scalediff * (Time.deltaTime / (pressedSeconds / 2f));
            brightness = Mathf.Lerp(unpressedDarkness, pressedDarkness, t / (pressedSeconds / 2f));
            yield return null;
        }

        for (float t = 0; t < pressedSeconds / 2f; t += Time.deltaTime)
        {
            transform.localScale += scalediff * (Time.deltaTime / (pressedSeconds / 2f));
            brightness = Mathf.Lerp(pressedDarkness, unpressedDarkness, t / (pressedSeconds / 2f));
            yield return null;
        }

        transform.localScale = startScale;
        brightness = unpressedDarkness;
        col.enabled = true;
        brightness = unpressedDarkness;
        
    }
}
