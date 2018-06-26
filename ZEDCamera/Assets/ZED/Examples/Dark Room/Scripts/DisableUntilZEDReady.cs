using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sl;

/// <summary>
/// Causes the attached GameObject to get disabled until the ZED camera finishes initializing. 
/// Used in the ZED Dark Room sample to synchronize the lights and music with the ZED's start. 
/// </summary>
public class DisableUntilZEDReady : MonoBehaviour
{
	// Use this for initialization
	void Awake()
    {
        ZEDManager.OnZEDReady += EnableThisObject;

        gameObject.SetActive(false);
	}
	
    void EnableThisObject()
    {
        gameObject.SetActive(true);
    }
}
