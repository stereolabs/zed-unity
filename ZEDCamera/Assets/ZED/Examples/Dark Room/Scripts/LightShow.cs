using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables one of the objects on its list at a time, and switches between them at a fixed time interval. 
/// Used in ZED Dark Room sample to switch the sequence of lights.
/// </summary>
public class LightShow : MonoBehaviour
{
    /// <summary>
    /// How long each "show" lasts before its object is disabled and the next is enabled. 
    /// </summary>
    [Tooltip("How long each 'show' lasts before its object is disabled and the next is enabled. ")]
    public float sequenceDurationSeconds = 16;

    /// <summary>
    /// Each object that holds a "show". Should contain or be a parent of all light objects it interacts with. 
    /// </summary>
    [Tooltip("Each object that holds a 'show'. Should contain or be a parent of all light objects it interacts with.")]
    public List<GameObject> sequenceObjects = new List<GameObject>();

    /// <summary>
    /// Runtime timer that indicates how long the current 'show' has been active. 
    /// Update() increments it and advances the show when it reaches sequenceDurationSeconds, then resets it to 0. 
    /// </summary>
    private float sequencetimer = 0;

    /// <summary>
    /// Index of the sequence. Used to advance through the SequenceObjects list. 
    /// </summary>
    private int sequenceindex = 0;

	// Use this for initialization
	void OnEnable ()
    {
        //set the first show to active and the rest to not active. 
        SwitchToSequence(0);
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        sequencetimer += Time.deltaTime;
        if(sequencetimer >= sequenceDurationSeconds)
        {
            sequenceindex++;
            if(sequenceindex >= sequenceObjects.Count)
            {
                sequenceindex = 0;
            }

            SwitchToSequence(sequenceindex);
            sequencetimer = sequencetimer % sequenceDurationSeconds;
        }
	}

    private void SwitchToSequence(int index)
    {
        //Make sure that's a valid index
        if (sequenceObjects.Count <= index || sequenceObjects[index] == null) return;

        for(int i = 0; i < sequenceObjects.Count; i++)
        {
            sequenceObjects[i].SetActive(i == index);
        }
    }
}
