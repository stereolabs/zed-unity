using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallOscillator : MonoBehaviour
{
    public AnimationCurve OscillationPath = new AnimationCurve();
    public float SecondsPerOscillation = 1f;

    private Vector3 startPosition;
    private float animationFrame = 0;

	// Use this for initialization
	void Start ()
    {
        startPosition = transform.localPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        animationFrame += Time.deltaTime / SecondsPerOscillation;
        if(animationFrame >= 1)
        {
            animationFrame = animationFrame % 1f;
        }

        float yvalue = startPosition.y + OscillationPath.Evaluate(animationFrame);
        transform.localPosition = new Vector3(transform.localPosition.x, yvalue, transform.localPosition.z);
	}
}
