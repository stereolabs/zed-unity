using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Rotates this object around the specified center transform, as well as around itself on the specified axis. 
/// Used in the ZED planetarium sample on each planet/moon to make it orbit the sun and also spin on its own axis. 
/// </summary>
public class PlanetMover : MonoBehaviour
{
    /// <summary>
    /// The transform this object revolves around. Always the Sun in the ZED planetarium sample, except for the moon. 
    /// </summary>
    [Tooltip("The transform this object revolves around. Always the Sun in the ZED planetarium sample, except for the moon.")]
    public Transform center;

    /// <summary>
    /// Degrees per second this object moves around its orbit.
    /// </summary>
    [Tooltip("Degrees per second this object moves around its orbit.")]
    public float speedRevolution = 10;

    /// <summary>
    /// The axis of rotation around its poles, ie, the direction from the planet's south pole to the north pole. 
    /// </summary>
    [Tooltip("The axis of rotation around its poles, ie, the direction from the planet's south pole to the north pole. ")]
    public Vector3 axis = Vector3.up;

    /// <summary>
    /// Degrees per second the object rotates on its own axis. 
    /// </summary>
    [Tooltip("Degrees per second the object rotates on its own axis. ")]
    public float speed = 10.0f;

    /// <summary>
    /// Axis the planet revolves around on its orbit. 
    /// </summary>
    private Vector3 dir;

    private void Start()
    {
        dir = center.up; //Get the axis of rotation from the object we're rotating. 
    }

    // Update is called once per frame
    void Update () {

        transform.RotateAround(center.position, center.TransformDirection(dir), Time.deltaTime * speedRevolution); //Rotating around the sun (orbit).

        transform.Rotate(axis, speed*Time.deltaTime, Space.Self); //Rotating around its own axis (night/day). 
        
    }
}
