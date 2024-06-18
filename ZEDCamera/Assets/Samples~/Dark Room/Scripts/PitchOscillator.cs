using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes an object slide on its X axis according to the AnimationCurve specified.
/// Used in the ZED Dark Room example scene to make lights sweep back and forth. 
/// </summary>
public class PitchOscillator : MonoBehaviour
{
    /// <summary>
    /// The path it takes to oscillate. Makes it simple to set a pattern in the Inspector. 
    /// </summary>
    [Tooltip("The path it takes to oscillate. Makes it simple to set a pattern in the Inspector. ")]
    public AnimationCurve animationCurve;

    /// <summary>
    /// How long a full oscillation lasts (from start to finish of animationCurve). 
    /// </summary>
    [Tooltip("How long a full oscillation lasts (from start to finish of animationCurve). ")]
    public float secondsPerOscillation = .95f;

    /// <summary>
    /// Scales the values in animationCurve, since it's difficult to specify values outside -1 and 1 in the Inspector. 
    /// </summary>
    [Tooltip("Scales the values in animationCurve, since it's difficult to specify values outside -1 and 1 in the Inspector. ")]
    public float distanceScale = 2; 

    /// <summary>
    /// How long through the animation it has played. 
    /// Incremented by Time.deltaTime / distanceScale each Update(). 
    /// </summary>
    private float timer = 0f;

    /// <summary>
    /// Cache for the starting position, so oscillations can be done relative to it after it moves. 
    /// </summary>
    private Vector3 startposition; //In local space

	// Use this for initialization
	void Start ()
    {
        startposition = transform.localPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Update the timer and restart the animationCurve if finished. 
        timer += Time.deltaTime; 
        if(timer >= secondsPerOscillation)
        {
            timer = timer % secondsPerOscillation;
        }

        //Move the light according to the curve. 
        float newxpos = animationCurve.Evaluate(timer / secondsPerOscillation) * distanceScale;
        transform.localPosition = startposition + transform.localRotation * Vector3.right * newxpos;
	}
}
