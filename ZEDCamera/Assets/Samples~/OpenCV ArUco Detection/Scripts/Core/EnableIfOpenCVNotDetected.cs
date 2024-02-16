using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enables the attached GameObject if the OpenCV for Unity package isn't detected. 
/// Used in the OpenCV ArUco example scene to indicate the package is missing via a Text object on the canvas. 
/// </summary>
public class EnableIfOpenCVNotDetected : MonoBehaviour
{
    public GameObject objectToEnable;
    
	void Awake ()
    {
#if !ZED_OPENCV_FOR_UNITY
     if(!objectToEnable) 
        {
            objectToEnable = GetComponentInChildren<Text>().gameObject;
        }

        objectToEnable.gameObject.SetActive(true);
#endif
    }
	

}
