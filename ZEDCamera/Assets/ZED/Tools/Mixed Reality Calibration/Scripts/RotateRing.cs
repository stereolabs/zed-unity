using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the visible, interactable ring controls in the ZED MR Calibration scene - the red, blue and green circles
/// that you can grab to rotate the ZED. 
/// See parent class TransformGrabbable to see how visuals and enabling/disabling other objects works. 
/// </summary>
public class RotateRing : TransformGrabbable
{
    /// <summary>
    /// RotateControl object that governs the actual ZED rotations. When RotateRing is moved, it sends movements to this object. 
    /// </summary>
    [Tooltip("RotateControl object that governs the actual ZED rotations. When RotateRing is moved, it sends movements to this object. ")]
    public RotateControl rotControl;
    /// <summary>
    /// Multiplies how the controller's movements move each axis. Set to 1 for axes this should control and 0 for the others. 
    /// </summary>
    [Tooltip("Multiplies how the controller's movements move each axis. Set to 1 for axes this should control and 0 for the others. ")]
    public Vector3 axisFactor = Vector3.right;

    private Vector3 grabStartDirection = Vector3.zero;
    private Quaternion grabStartRotation = Quaternion.identity;

    protected override void Awake()
    {
        base.Awake();
		if(!rotControl)
        {
            rotControl = GetComponentInParent<RotateControl>();
        }
	}

    /// <summary>
    /// If being grabbed, calculates the angle offset of the controller relative to the start, and sends that angle to the RotateControl. 
    /// </summary>
    void Update()
    {
        if(isGrabbed)
        {
            Vector3 currentvec = rotControl.transform.InverseTransformDirection(grabbingTransform.position - rotControl.transform.position);
            currentvec.Normalize();

            float xangle = Vector3.SignedAngle(new Vector3(0, grabStartDirection.y, grabStartDirection.z), new Vector3(0, currentvec.y, currentvec.z), Vector3.right);
            float yangle = Vector3.SignedAngle(new Vector3(grabStartDirection.x, 0, grabStartDirection.z), new Vector3(currentvec.x, 0, currentvec.z), Vector3.up);
            float zangle = Vector3.SignedAngle(new Vector3(grabStartDirection.x, grabStartDirection.y, 0), new Vector3(currentvec.x, currentvec.y, 0), Vector3.forward);

            Vector3 finalangle = new Vector3(xangle * axisFactor.x, yangle * axisFactor.y, zangle * axisFactor.z);

            rotControl.Rotate(finalangle);
        }
    }

    /// <summary>
    /// What happens when ZEDXRGrabber first grabs it. From IXRGrabbable. Stores the current positions for determining the change later. 
    /// </summary>
    public override void OnGrabStart(Transform grabtransform)
    {
        base.OnGrabStart(grabtransform);

        grabStartRotation = rotControl.transform.localRotation;
        grabStartDirection = rotControl.transform.InverseTransformDirection(grabbingTransform.position - rotControl.transform.position);

        grabStartDirection.Normalize();
    }

    /// <summary>
    /// What happens when ZEDXRGrabber stops grabbing it. From IXRGrabbable. 
    /// </summary>
    public override void OnGrabEnd()
    {
        rotControl.Rotate(Vector3.zero);

        base.OnGrabEnd();
    }
}



