using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sl;

/// <summary>
/// Causes the attached GameObject to get disabled until a ZED camera finishes initializing. 
/// Used in the ZED Dark Room sample to synchronize the lights and music with the ZED's start. 
/// </summary>
public class DisableUntilZEDReady : MonoBehaviour
{
    /// <summary>
    /// The ZEDManager to wait for. If left empty, it will select the first ZED available. 
    /// This may cause unwanted behavior if multiple ZEDManagers are in the scene.
    /// </summary>
    [Tooltip("The ZEDManager to wait for. If left empty, it will select the first ZED available. " +
        "This may cause unwanted behavior if multiple ZEDManagers are in the scene.")]
	public ZEDManager zedManager = null;

	// Use this for initialization
	void Awake()
    {
		if(!zedManager) zedManager = FindObjectOfType<ZEDManager> (); //Selects the first available ZEDManager if none was set before. 

		if (zedManager) zedManager.OnZEDReady += EnableThisObject;
	}


    void EnableThisObject()
    {
        gameObject.SetActive(true);
    }
}
