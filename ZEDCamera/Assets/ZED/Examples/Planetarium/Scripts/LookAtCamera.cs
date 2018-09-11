using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the GameObject turn to face the target object each frame. 
/// Used for an outline effect on the ZED planetarium sample's sun, as it's drawn by a quad. 
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    /// <summary>
    /// The target transform/object to face. 
    /// </summary>
    [Tooltip("The target transform/object to face.")]
    public Transform target;

    /// <summary>
    /// True if this object and the target object do *not* have the same position. 
    /// The script will only look at the target when true.
    /// Otherwise, Quaternion.LookRotation will spam console errors.
    /// </summary>
    bool canLook = false;

    private void Start()
    {
        if(!target) //If we didn't set the target object, try to find the best object for it. 
        {
            target = GameObject.Find("Camera_eyes").transform; //Try the center object on the ZED stereo rig. 
            if (!target)
            {
                target = ZEDManager.Instance.GetLeftCameraTransform(); //Try the ZED's left eye. Works for the ZED mono rig.
            }
            if(!target) 
            {
                target = Camera.main.transform; //If no ZED rig is availables, use the main camera. 
            }
        }
    }

    void Update ()
    {
        //Make sure the target and this object don't have the same position. 
        if (!canLook && transform.position - target.position != Vector3.zero) 
        {
            canLook = true;
        }

        if (canLook)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - target.position);
        }
	}
}
