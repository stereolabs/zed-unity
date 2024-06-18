using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates the object along its axes consistently. 
/// Because its using local axes, it affects the angle of other axes going forward, resulting in wild
/// but possibly desired behavior if multiple axes are rotating at once.
/// Used in the ZED Dark Room example scene to rotate lights. 
/// </summary><remarks>
/// This is an exact copy of RotateOnAxes from the Dark Room sample, created so that the
/// Mixed Reality Calibration scene doesn't depend on the user importing the Dark Room sample. 
/// </remarks>
public class RotateOnAxes_MRCalib : MonoBehaviour
{ 
    /// <summary>
    /// How far it spins on the X axis (pitch) per frame. 
    /// </summary>
    [Tooltip("How far it spins on the X axis (pitch) per frame. ")]
    public float xRevolutionsPerSecond = 0;

    /// <summary>
    /// How far it spins on the Y axis (yaw) per frame. 
    /// </summary>
    [Tooltip("How far it spins on the Y axis (yaw) per frame. ")]
    public float yRevolutionsPerSecond = 0.25f;

    /// <summary>
    /// How far it spins on the Z axis (roll) per frame. 
    /// </summary>
    [Tooltip("How far it spins on the Z axis (roll) per frame. ")]
    public float zRevolutionsPerSecond = 0;

    // Update is called once per frame
    void Update()
    {
        //Rotate on the axes. Note that the order this occurs is important as each rotation changes transform.localRotation. 
        transform.Rotate(transform.localRotation * Vector3.right, xRevolutionsPerSecond * 360 * Time.deltaTime); //Pitch
        transform.Rotate(transform.localRotation * Vector3.up, yRevolutionsPerSecond * 360 * Time.deltaTime); //Yaw
        transform.Rotate(transform.localRotation * Vector3.forward, zRevolutionsPerSecond * 360 * Time.deltaTime); //Roll
    }
}
