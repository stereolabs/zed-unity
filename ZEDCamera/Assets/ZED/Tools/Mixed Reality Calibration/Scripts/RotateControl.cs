using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Receives rotations from separate RotateRing objects, which should be child objects, 
/// and applies them to the ZED via CameraAnchor. Also rotates the ring holder object accordingly.
/// When this is held at an angle, the size of the angle affects how quickly the ZED moves.
/// See parent class TransformControl to see how visual elements work. 
/// </summary>
public class RotateControl : TransformControl
{
    /// <summary>
    /// Full range that you can turn the control on any one axis. 
    /// </summary>
    [Tooltip("Full range that you can turn the control on any one axis.")]
    public float maxUIRotateDegrees = 90f;

    private Quaternion startRot;

    protected override void Awake ()
    {
        base.Awake();
        if (!anchor) anchor = FindObjectOfType<CameraAnchor>(); 
        CameraAnchor.OnCameraAnchorCreated += SetNewAnchor;

        startRot = visualsParent.localRotation;
    }

    /// <summary>
    /// Takes how many degrees a given ring has been turned, and translates it to a call to 
    /// CameraAnchor to move the ZED gradually. 
    /// </summary>
    public void Rotate(Vector3 degrees)
    {
        Vector3 oldeulers = StandardizeEulers(visualsParent.localEulerAngles);

        //Reflect rotation in UI element (this thing). 
        Vector3 clampdegrees;
        clampdegrees.x = Mathf.Clamp(degrees.x, -maxUIRotateDegrees, maxUIRotateDegrees);
        clampdegrees.y = Mathf.Clamp(degrees.y, -maxUIRotateDegrees, maxUIRotateDegrees);
        clampdegrees.z = Mathf.Clamp(degrees.z, -maxUIRotateDegrees, maxUIRotateDegrees);

        visualsParent.localRotation = startRot * Quaternion.Euler(clampdegrees);

        //Get clamped value and send to anchor to move the actual ZED. 
        if (anchor)
        {
            Vector3 finaldegrees = clampdegrees / maxUIRotateDegrees;
            anchor.RotateZEDIncrementally(finaldegrees);
        }

        PlayTapSoundIfNeeded(oldeulers / maxUIRotateDegrees, StandardizeEulers(visualsParent.localEulerAngles) / maxUIRotateDegrees);
    }

    /// <summary>
    /// Changes the anchor object. 
    /// </summary>
    private void SetNewAnchor(CameraAnchor newanchor)
    {
        anchor = newanchor;
    }

    /// <summary>
    /// Since Unity provides Euler angles inconsistently (ex. 270 = -90 = -450) this function adds or subtracts 360
    /// as needed to ensure the angle is between -180 and 180, allowing us to use that range for logic. 
    /// </summary>
    private Vector3 StandardizeEulers(Vector3 input)
    {
        Vector3 standardized;
        standardized.x = StandardizeAngle(input.x);
        standardized.y = StandardizeAngle(input.y);
        standardized.z = StandardizeAngle(input.z);

        return standardized;
    }

    /// <summary>
    /// Makes a single angle (in degrees) be represented as between -180 and 180. 
    /// </summary>
    private float StandardizeAngle(float input)
    {
        float standardized = input - Mathf.Floor(input / 360f); //Puts it to within -360 and 360.

        if (standardized > 180) standardized -= 360;
        else if (standardized < -180) standardized += 360;

        return standardized;
    }
}
