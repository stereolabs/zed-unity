using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every X seconds, changes the lights in all child objects to a random (smae) color and rotates them to new, random directions
/// if it contains a RandomDirectionMover object. 
/// Used for the ZED Dark Room example scene.
/// </summary>
public class RandomDirLightManager : MonoBehaviour
{
    /// <summary>
    /// Time between each pulse. Should match the beats-per-second of the music. 
    /// </summary>
    [Tooltip("Time between each pulse. Should match the beats-per-second of the music. ")]
    public float secondsBetweenPulses = 0.5f; 

    /// <summary>
    /// How long to wait after start to start pulsing. Use to sync to music. 
    /// </summary>
    [Tooltip("How long to wait after start to start pulsing. Use to sync to music.")]
    public float startDelay = 0.1f;

    /// <summary>
    /// Pool of colors that the lights could become on each pulse. 
    /// </summary>
    [Tooltip("Pool of colors that the lights could become on each pulse. ")]
    public List<Color> ColorOptions = new List<Color>();

    /// <summary>
    /// Length of time in seconds since the last pulse.
    /// Gets incremented by Time.deltaTime in Update(). When it hits secondsBetweenPulses, it triggers a pulse and resets. 
    /// </summary>
    private float pulseTimer;

    /// <summary>
    /// List of all Light components in this object and its children. Filled in Start(). 
    /// </summary>
    private List<Light> lightList;

    /// <summary>
    /// List of all RandomDirectionMover components in this object and its children. Filled in Start. 
    /// </summary>
    private List<RandomDirectionMover> randPointerList;

    /// <summary>
    /// Stores the index of the color we used during the last pulse. 
    /// Used to prevent the same color appearing twice in a row. 
    /// </summary>
    private int lastcolorindex = -1;

	// Use this for initialization
	void Start ()
    {
	    lightList = new List<Light>(GetComponentsInChildren<Light>());
        randPointerList = new List<RandomDirectionMover>(GetComponentsInChildren<RandomDirectionMover>());

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

    /// <summary>
    /// Picks a random color, sets all Lights to that color, and tells all RandomDirectionMovers to move randomly. 
    /// </summary>
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

                lastcolorindex = newcolorindex;
            }
            else newcolorindex = 0;
            
            foreach(Light light in lightList)
            {
                light.color = ColorOptions[newcolorindex];
            }
        }

        foreach(RandomDirectionMover pointer in randPointerList)
        {
            StartCoroutine(pointer.NewDirection()); //Causes it to move to a random direction. 
        }
    }
}
