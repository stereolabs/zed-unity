using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallParentRotator : MonoBehaviour
{
    public float SecondsPerRevolution = 10f;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.Rotate(transform.localRotation * Vector3.up, 360 / SecondsPerRevolution * Time.deltaTime);
	}
}
