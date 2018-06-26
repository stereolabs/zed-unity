using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates the object on its Y axis
/// </summary>
public class YawPerFrame : MonoBehaviour
{
    public float secondsPerRevolution = 10f; //How long it takes to rotate one full turn

	// Update is called once per frame
	void Update ()
    {
        transform.Rotate(transform.localRotation * Vector3.up, 360 / secondsPerRevolution * Time.deltaTime);
	}
}
