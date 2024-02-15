using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Switches on all objects in its list slowly, pausing two seconds on each one.
/// Used in the ZED planetarium sample to stagger the animations of the 18 SunBurstRoot objects on the sun.
/// </summary>
public class SunBursts : MonoBehaviour
{
    /// <summary>
    /// All objects to be turned on, in order. Script assumes they begin disabled. 
    /// </summary>
    [Tooltip("All objects to be turned on, in order. Script assumes they begin disabled.")]
    public List<GameObject> sunBurstsGO = new List<GameObject>();

	// Use this for initialization
	IEnumerator Start ()
    {
        for (int i = 0; i < sunBurstsGO.Count; i++)
        {
            yield return new WaitForSeconds(2f);
            sunBurstsGO[i].SetActive(true);
        }
	}
}