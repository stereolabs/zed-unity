using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the transform face any camera that renders, other than Unity Scene (editor) cameras. 
/// Will only turn on axes that you specify. 
/// <para>Used in the ZED MR calibration scene to make the 2D real-world screen always visible.</para>
/// </summary>
public class LookAtCameraPartialAxis : MonoBehaviour
{
    /// <summary>
    /// Whether to rotate to follow the camera on the X axis. 
    /// </summary>
    [Tooltip("Whether to follow the camera on the X axis.")]
    public bool followX = false;
    /// <summary>
    /// Whether to rotate to follow the camera on the Y axis. 
    /// </summary>
    [Tooltip("Whether to follow the camera on the Y axis.")]
    public bool followY = true;
    /// <summary>
    /// Whether to rotate to follow the camera on the Z axis. 
    /// </summary>
    [Tooltip("Whether to follow the camera on the Z axis.")]
    public bool followZ = false;

    private void Start()
    {
        Camera.onPreRender += LookAtCamera;
    }

    private void OnDestroy()
    {
        Camera.onPreRender -= LookAtCamera;
    }


    /// <summary>
    /// Rotates the transform to face the target camera on all enabled axes. 
    /// </summary>
    /// <param name="cam"></param>
    void LookAtCamera(Camera cam)
    {
        //Camera cam = Camera.current;
        if (cam.name.Contains("Editor")) return;

        Quaternion lookrot = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        Vector3 lookeuler = lookrot.eulerAngles;

        float newx = followX ? lookeuler.x : transform.eulerAngles.x;
        float newy = followY ? lookeuler.y : transform.eulerAngles.y;
        float newz = followZ ? lookeuler.z : transform.eulerAngles.z;

        transform.rotation = Quaternion.Euler(newx, newy, newz);
	}
}
