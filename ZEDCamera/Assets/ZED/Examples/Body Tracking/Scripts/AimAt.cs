using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimAt : MonoBehaviour
{
    public ZEDManager zedManager = null;
    public ZEDBodyTrackingManager zedBodyTrackingManager = null;
    public UnityEngine.UI.Text txtfps = null;
    public Vector3 target = Vector3.zero;
    private bool mirrored = false;

    private void Awake()
    {
        if (zedManager == null)
        {
            Debug.LogError("zedManager is null. Please set a ZedManager.");
            Application.Quit();
        }
        if (zedBodyTrackingManager == null)
        {
            Debug.LogError("zedBodyTrackingManager is null. Please set a ZedManager.");
            Application.Quit();
        }
    }

    private void Start()
    {
        zedManager.OnBodyTracking += OnBodyTrackingFrame;
        mirrored = zedBodyTrackingManager.mirrorMode;
        if(txtfps != null) { StartCoroutine(UpdateFPS()); }
    }

    private void OnBodyTrackingFrame(BodyTrackingFrame bodyFrame)
    {
        if(bodyFrame.bodyCount > 0)
        {
            target = bodyFrame.detectedBodies[0].Get3DWorldPosition();
            if (mirrored)
            {
                target = new Vector3(-target.x, target.y, target.z);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt (target);
    }

    private IEnumerator UpdateFPS()
    {
        while(true) {
            txtfps.text = Mathf.Floor(1.0f / Time.deltaTime) + "\nfps";
            yield return new WaitForSeconds(.1f);
        }

    }
}
