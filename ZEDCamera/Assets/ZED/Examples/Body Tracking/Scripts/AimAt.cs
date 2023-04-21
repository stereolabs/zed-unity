using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimAt : MonoBehaviour
{
    public ZEDManager zedManager = null;
    public Vector3 target = Vector3.zero;

    private void Awake()
    {
        if (zedManager == null)
        {
            Debug.LogError("zedManager is null. Please set a ZedManager.");
            Application.Quit();
        }
    }

    private void Start()
    {
        zedManager.OnBodyTracking += OnBodyTrackingFrame;
    }

    private void OnBodyTrackingFrame(BodyTrackingFrame bodyFrame)
    {
        if(bodyFrame.bodyCount > 0)
        {
            target = bodyFrame.detectedBodies[0].Get3DWorldPosition();
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt (target);
    }
}
