using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the GameObject turn to face the target object each frame. 
/// Used for an outline effect on the ZED planetarium sample's sun, as it's drawn by a quad. 
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    void OnWillRenderObject()
    {
        Camera targetcam = Camera.current; //Shorthand. 

        //Make sure the target and this object don't have the same position. This can happen before the cameras are initialized.
        //Calling Quaternion.LookRotation in this case spams the console with errors. 
        if (transform.position - targetcam.transform.position == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(transform.position - targetcam.transform.position);

    }
}
