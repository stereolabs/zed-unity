using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the visible, interactable arrow controls in the ZED MR Calibration scene - the red, blue and green arrows
/// that you can grab to translate the ZED. 
/// See parent class TransformGrabbable to see how visuals and enabling/disabling other objects works. 
/// </summary>
public class TranslateArrow : TransformGrabbable
{
    /// <summary>
    /// TranslateControl object that governs the actual ZED rotations. When TranslateArrow is moved, it sends movements to this object. 
    /// </summary>
    public TranslateControl transControl;
    /// <summary>
    /// Multiplies how the controller's movements move each axis. Set to 1 for axes this should control and 0 for the others. 
    /// </summary>
    public Vector3 axisFactor = Vector3.right;

    private Vector3 grabStartOffset = Vector3.zero;
    private Vector3 grabStartControlLocalPos = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        if (!transControl)
        {
            transControl = GetComponentInParent<TranslateControl>();
        }
    }

    /// <summary>
    /// If being grabbed, sends its current positional offset from the center to the TranslateControl, which applies the translation. 
    /// </summary>
    private void Update()
    {
        if (isGrabbed)
        {
            Vector3 currentlocal = grabbingTransform.position;
            Vector3 currentoffset = transControl.transform.InverseTransformPoint(grabbingTransform.position);
            currentoffset += (transControl.transform.localPosition - grabStartControlLocalPos);

            Vector3 dist = currentoffset - grabStartOffset;
            Vector3 moddist = new Vector3(dist.x * axisFactor.x, dist.y * axisFactor.y, dist.z * axisFactor.z);
            transControl.Translate(moddist);
        }
    }

    /// <summary>
    /// What happens when ZEDXRGrabber first grabs it. From IXRGrabbable. Stores the current positions for determining the change later. 
    /// </summary>
    public override void OnGrabStart(Transform grabtransform)
    {
        base.OnGrabStart(grabtransform);

        grabStartControlLocalPos = transControl.transform.localPosition;
        grabStartOffset = transControl.transform.InverseTransformPoint(grabbingTransform.position);
    }

    /// <summary>
    /// What happens when ZEDXRGrabber stops grabbing it. From IXRGrabbable. 
    /// </summary>
    public override void OnGrabEnd()
    {
        transControl.Translate(Vector3.zero);

        base.OnGrabEnd();
    }
}
