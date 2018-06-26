using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates the object wildly along its axes. 
/// </summary>
public class LightRotator : MonoBehaviour
{
    public float xRevolutionsPerSecond = 0.5f;
    public float yRevolutionsPerSecond = 0.25f;
    public float zRevolutionsPerSecond = 0;
	
	// Update is called once per frame
	void Update ()
    {
        transform.Rotate(transform.localRotation * Vector3.right, xRevolutionsPerSecond * 360 * Time.deltaTime);
        transform.Rotate(transform.localRotation * Vector3.up, yRevolutionsPerSecond * 360 * Time.deltaTime);
        transform.Rotate(transform.localRotation * Vector3.forward, zRevolutionsPerSecond * 360 * Time.deltaTime);
    }
}
