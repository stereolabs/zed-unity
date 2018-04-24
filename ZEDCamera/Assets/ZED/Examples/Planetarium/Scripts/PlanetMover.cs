using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlanetMover : MonoBehaviour {

    public Transform center;
    public float speedRevolution = 10;

    public Vector3 axis = Vector3.up;
    public float speed = 10.0f;
    private Vector3 dir;

    private Vector3 originPos;
    private void OnEnable()
    {
        //originPos = transform.localPosition;
    }

    void Start () {
        dir = center.up;
        //transform.position = originPos;
	}
	
	// Update is called once per frame
	void Update () {
            transform.RotateAround(center.position, center.TransformDirection(dir), Time.deltaTime * speedRevolution);
            transform.Rotate(axis, speed*Time.deltaTime, Space.Self);
        
    }
}
