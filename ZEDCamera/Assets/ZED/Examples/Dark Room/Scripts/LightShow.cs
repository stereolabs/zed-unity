using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables one of the objects on its list at a time, and switches between them at a fixed time interval. 
/// Used in ZED Dark Room sample to switch the sequence of lights.
/// </summary>
public class LightShow : MonoBehaviour
{
    public float SequenceDurationSeconds = 16;
    public float _sequenceTimer = 0;

    public List<GameObject> SequenceObjects = new List<GameObject>();

    private int _sequenceIndex = 0;

	// Use this for initialization
	void OnEnable ()
    {
        //set the first to active and the rest to not active. 
        SwitchToSequence(0);
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        _sequenceTimer += Time.deltaTime;
        if(_sequenceTimer >= SequenceDurationSeconds)
        {
            _sequenceIndex++;
            if(_sequenceIndex >= SequenceObjects.Count)
            {
                _sequenceIndex = 0;
            }

            SwitchToSequence(_sequenceIndex);
            _sequenceTimer = _sequenceTimer % SequenceDurationSeconds;
        }
	}

    private void SwitchToSequence(int index)
    {
        //Make sure that's a valid index
        if (SequenceObjects.Count <= index || SequenceObjects[index] == null) return;

        for(int i = 0; i < SequenceObjects.Count; i++)
        {
            SequenceObjects[i].SetActive(i == index);
        }
    }
}
