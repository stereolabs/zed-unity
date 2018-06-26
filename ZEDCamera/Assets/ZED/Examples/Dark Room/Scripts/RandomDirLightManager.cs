using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every X seconds, changes the lights to a random color and points them in new, random directions. 
/// </summary>
public class RandomDirLightManager : MonoBehaviour
{
    public float secondsBetweenPulses = 0.5f; //Seconds between 
    private float pulseTimer;

    public float startDelay = 0.1f;

    public List<Color> ColorOptions = new List<Color>(); //Potential colors the lights can become

    private List<Light> lightList;
    private List<LightRandDir> randPointerList;

    private int lastcolorindex = -1;

	// Use this for initialization
	void Start ()
    {
	    lightList = new List<Light>(GetComponentsInChildren<Light>());
        randPointerList = new List<LightRandDir>(GetComponentsInChildren<LightRandDir>());

        pulseTimer = -startDelay;
    }
	
	// Update is called once per frame
	void Update ()
    {
        pulseTimer += Time.deltaTime;

        if(pulseTimer > secondsBetweenPulses)
        {
            PulseLights();
            pulseTimer = pulseTimer % secondsBetweenPulses;
        }
	}

    private void PulseLights()
    {
        if (ColorOptions.Count > 0) //We have at least one color indexed, so we can pick a color from the list.
        {
            int newcolorindex;

            if (ColorOptions.Count > 1)
            {
                newcolorindex = Random.Range(0, ColorOptions.Count - 1);
                while (newcolorindex == lastcolorindex) //Don't pick the same color twice in a row if we have more than one color available. 
                {
                    newcolorindex = Random.Range(0, ColorOptions.Count - 1);
                }
            }
            else newcolorindex = 0;
            
            foreach(Light light in lightList)
            {
                light.color = ColorOptions[newcolorindex];
            }
        }

        foreach(LightRandDir pointer in randPointerList)
        {
            StartCoroutine(pointer.NewDirection());
        }
       
    }
}
