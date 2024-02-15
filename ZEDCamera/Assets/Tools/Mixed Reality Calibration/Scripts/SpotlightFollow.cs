using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Causes the attached object to follow the target object but only on the X and Z axes: keeps its height. 
/// Used for the spotlight and view screen in the ZED MR Calibration scene. 
/// </summary>
public class SpotlightFollow : MonoBehaviour
{
    /// <summary>
    /// Transform that this object will follow on the X and Z axes. 
    /// </summary>
    [Tooltip("Transform that this object will follow on the X and Z axes. ")]
    public Transform followTransform;
	
	void Update ()
    {
        transform.position = new Vector3(followTransform.position.x, transform.position.y, followTransform.position.z);

	}
}
