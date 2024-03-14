using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Causes the attached transform to change its parent to its parent's parent on Start. 
/// This is done so you can conveniently keep this object in the same position as its parent when 
/// working in the editor, but it'll stop inheriting changes to that transform at runtime. 
/// Done for the MR calibration transform controls (arrows/rings) to hold the "limit" indicators, 
/// which should not move when the user clicks and drags the control.
/// </summary>
public class ParentToGrandparentOnStart : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
		if(transform.parent != null && transform.parent.parent != null)
        {
            transform.SetParent(transform.parent.parent);
        }
        else
        {
            Debug.LogWarning("Could not set parent to transform's parent because either this transform or its " +
                "current parent do not have a parent."); //Try saying THAT five times fast. 
        }
	}
	

}
