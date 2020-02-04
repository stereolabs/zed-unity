using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Experimental.Rendering;
#endif

/// <summary>
/// Makes the GameObject turn to face the target object each frame. 
/// Used for an outline effect on the ZED planetarium sample's sun, as it's drawn by a quad. 
/// </summary>
public class LookAtCamera : MonoBehaviour
{


#if !ZED_LWRP && !ZED_HDRP //OnWillRenderObject doesn't work in SRP, so use a callback from RenderPipeline to trigger the facing instead. 
    void OnWillRenderObject()
    {
        FaceCamera(Camera.current);
    }
#else

    /// <summary>
    /// The ZEDManager that the object will face each frame. Faces the left camera. 
    /// </summary>
    [Tooltip("The ZEDManager that the object will face each frame. Faces the left camera.")]
    public ZEDManager zedManager;
    private Camera zedLeftCam;

    private void Start()
    {
        if(!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }
        if (zedManager) zedLeftCam = zedManager.GetLeftCamera();
    }


    private void LateUpdate()
    {
        if (zedLeftCam) FaceCamera(zedLeftCam);
    }
#endif

    void FaceCamera(Camera cam)
    {
        Camera targetcam = cam; //Shorthand. 

        //Make sure the target and this object don't have the same position. This can happen before the cameras are initialized.
        //Calling Quaternion.LookRotation in this case spams the console with errors. 
        if (transform.position - targetcam.transform.position == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(transform.position - targetcam.transform.position);
    }
}