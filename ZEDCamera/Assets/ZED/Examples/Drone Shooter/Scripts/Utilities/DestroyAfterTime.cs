using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Destroys the object it's attached to after a pre-specified amount of time. Used for explosion effects. 
/// Similar to Unity's Destroy(GameObject, float) overload, but allows it to be set easily in the Inspector. 
/// </summary>
public class DestroyAfterTime : MonoBehaviour
{
    /// <summary>
    /// How long the gameobject exists.
    /// </summary>
    [Tooltip("How long the gameobject exists.")]
    public float DeathClock = 2f;

	// Update is called once per frame
	void Update ()
    {
        DeathClock -= Time.deltaTime;

        if(DeathClock <= 0f)
        {
            Destroy(gameObject);
        }
        
	}
}
