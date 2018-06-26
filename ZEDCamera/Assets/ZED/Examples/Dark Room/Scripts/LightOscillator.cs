using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the light slide on its X axis according to the AnimationCurve specified.
/// </summary>
public class LightOscillator : MonoBehaviour
{

    public AnimationCurve animationCurve; //The path it takes to oscillate
    public float secondsPerOscillation = .95f;
    public float distanceScale = 2; //The bounds of the distance it travels

    private float timer = 0f;
    private Vector3 startposition; //In local space

	// Use this for initialization
	void Start ()
    {
        startposition = transform.localPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        timer += Time.deltaTime;
        if(timer >= secondsPerOscillation)
        {
            timer = timer % secondsPerOscillation;
        }

        float newxpos = animationCurve.Evaluate(timer / secondsPerOscillation) * distanceScale;
        transform.localPosition = startposition + transform.localRotation * Vector3.right * newxpos;
	}
}
