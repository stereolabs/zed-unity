using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flashes the attached light repetitively by alternating its intensity by 0 and its starting intensity. 
/// </summary>
public class StrobeLight : MonoBehaviour
{
    /// <summary>
    /// Gets added to the time before the first flash. Use to synchromize with music. 
    /// </summary>
    [Tooltip("Gets added to the time before the first flash. Use to synchromize with music. ")]
    public float startDelay = 0f;

    /// <summary>
    /// Time between the start of each flash. Independent of the actual flash duration. 
    /// </summary>
    [Tooltip("Time between the start of each flash. Independent of the actual flash duration. ")]
    public float secondsBetweenFlashes = 0.25f;

    /// <summary>
    /// How long each flash lasts/stays in the On state. 
    /// </summary>
    [Tooltip("How long each flash lasts/stays in the On state. ")]
    public float flashDurationInSeconds = 0.1f; 

    /// <summary>
    /// How long in seconds since the last flash. 
    /// Gets incremented by Time.deltaTime in Update(). Starts a flash and resets when it hits secondsBetweenFlashes. 
    /// </summary>
    private float flashtimer;

    /// <summary>
    /// The Light attached to this object. 
    /// </summary>
    private Light lightcomponent;

    /// <summary>
    /// Cache for the starting intensity, which will be the flash intensity. 
    /// </summary>
    private float maxintensity; 

	// Use this for initialization
	void Start ()
    {
        lightcomponent = GetComponent<Light>();
        maxintensity = lightcomponent.intensity; //Cache the light's intensity. 
        lightcomponent.intensity = 0;

        flashtimer = -startDelay; //Add the start delay.
	}
	
	// Update is called once per frame
	void Update ()
    {
        flashtimer += Time.deltaTime;

        if(flashtimer >= secondsBetweenFlashes) //Let there be light. 
        {
            StartCoroutine(FlashLight());
            flashtimer = flashtimer % secondsBetweenFlashes;
        }
	}

    /// <summary>
    /// Turns on the light, waits for flashDurationInSeconds, then turns it off. 
    /// </summary>
    /// <returns></returns>
    private IEnumerator FlashLight()
    {
        lightcomponent.intensity = maxintensity; //Set the light to be as bright as it started. 

        for(float t = 0; t < flashDurationInSeconds; t += Time.deltaTime) 
        {
            yield return null;
        }

        lightcomponent.intensity = 0;
    }
}
