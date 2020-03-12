using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables/disables objects and calls events to switch controller-mounted objects so that
/// ones intended for the dominant hand are over the user's preferred hand, and vice-versa with 
/// objects meant for the non-dominant hand. 
/// Used in the ZED MR Calibration scene to switch the hand-mounted menu between hands. 
/// </summary>
public class PrimaryHandSwitcher : MonoBehaviour
{
    /// <summary>
    /// Left controller transform.
    /// </summary>
    [Tooltip("Left controller transform.")]
    public Transform leftHand;
    /// <summary>
    /// Right controller transform.
    /// </summary>
    [Tooltip("Right controller transform.")]
    public Transform rightHand;

    private static GameObject _primaryhandobject;
    /// <summary>
    /// GameObject currently designated as the primary hand. 
    /// </summary>
    public static GameObject primaryHandObject
    {
        get
        {
            if (!_primaryhandobject)
            {
                ZEDXRGrabber primarygrabber = FindObjectOfType<ZEDXRGrabber>();
                _primaryhandobject = primarygrabber.transform.parent.gameObject;
            }
            return _primaryhandobject;
        }
    }

    /// <summary>
    /// Objects that should always be parented to the primary hand, and will be moved when the hands are switched. 
    /// </summary>
    [Space(5)]
    [Tooltip("Objects that should always be parented to the primary hand, and will be moved when the hands are switched.")]
    public List<GameObject> parentToPrimaryHand = new List<GameObject>();
    /// <summary>
    /// Objects that should always be parented to the secondary hand, and will be moved when the hands are switched. 
    /// </summary>
    [Tooltip("Objects that should always be parented to the secondary hand, and will be moved when the hands are switched.")]
    public List<GameObject> parentToSecondaryHand = new List<GameObject>();

    /// <summary>
    /// Objects to enable only when the right hand is primary, and disabled otherwise. 
    /// </summary>
    [Space(5)]
    [Tooltip("Objects to enable only when the right hand is primary, and disabled otherwise. ")]
    public List<GameObject> enableWhenRightIsPrimary = new List<GameObject>();
    /// <summary>
    /// Objects to enable only when the left hand is primary, and disabled otherwise. 
    /// </summary>
    [Tooltip("Objects to enable only when the left hand is primary, and disabled otherwise. ")]
    public List<GameObject> enableWhenLeftIsPrimary = new List<GameObject>();
   

    /// <summary>
    /// Switches the handedness, calling events and shifting transforms around as necessary. 
    /// </summary>
    /// <param name="righthanded"></param>
    public void SetPrimaryHand(bool righthanded)
    {
        print("Switching primary hand to " + (righthanded ? "right" : "left"));

        //First, move all the "parent to ___ hand" objecs to the correct one. 
        Transform primaryhand = righthanded ? rightHand : leftHand;
        Transform secondaryhand = righthanded ? leftHand : rightHand;

        foreach (GameObject pobj in parentToPrimaryHand)
        {
            Vector3 localpos = pobj.transform.localPosition;
            Quaternion localrot = pobj.transform.localRotation;

            pobj.transform.SetParent(primaryhand, false);
        }

        foreach (GameObject sobj in parentToSecondaryHand)
        {
            Vector3 localpos = sobj.transform.localPosition;
            Quaternion localrot = sobj.transform.localRotation;

            sobj.transform.SetParent(secondaryhand, false);
        }

        //Now enable the objects for the correct hand. 
        foreach(GameObject robj in enableWhenRightIsPrimary)
        {
            robj.SetActive(righthanded);
        }

        foreach(GameObject lobj in enableWhenLeftIsPrimary)
        {
            lobj.SetActive(!righthanded);
        }

        _primaryhandobject = primaryhand.gameObject;
    }




}
