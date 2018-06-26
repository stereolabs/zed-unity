using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flashes the attached light repetitively by alternating its intensity by 0 and its starting intensity. 
/// </summary>
public class StrobeLight : MonoBehaviour
{
    public float startDelay = 0f; //Gets added to the time before the first flash. 
    public float secondsBetweenFlashes = 0.25f; //Independent of the actual flash duration. 
    public float flashDurationInSeconds = 0.1f; //How long the flash stays in the On state. 

    private float flashtimer;

    private Light lightcomponent;
    private float maxintensity;

	// Use this for initialization
	void Start ()
    {
        lightcomponent = GetComponent<Light>();
        maxintensity = lightcomponent.intensity;
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

    private IEnumerator FlashLight()
    {
        lightcomponent.intensity = maxintensity;

        for(float t = 0; t < flashDurationInSeconds; t += Time.deltaTime)
        {
            yield return null;
        }

        lightcomponent.intensity = 0;
    }
}
