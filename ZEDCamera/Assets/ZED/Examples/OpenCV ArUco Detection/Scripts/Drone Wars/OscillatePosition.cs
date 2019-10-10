using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the object move back and forth in a cycle, on any axis you choose, relative to its starting local position.
/// </summary>
public class OscillatePosition : MonoBehaviour
{
    /// <summary>
    /// Curve that defines the pattern we move the transform over time.
    /// </summary>
    [Tooltip("Curve that defines the pattern we move the transform over time.")]
    public AnimationCurve moveCurve = new AnimationCurve(new Keyframe[5]
    {
        new Keyframe(0, 0, 5, 5),
        new Keyframe(0.25f, 1, 0, 0),
        new Keyframe(0.5f, 0, -5, -5),
        new Keyframe(0.75f, -1, 0, 0),
        new Keyframe(1, 0, 5, 5)
    });
    /// <summary>
    /// How far the transform travels on points on the curve equal to 1 or -1. (Technically not "max" but more intuitive than "normalized" or something.)
    /// </summary>
    [Tooltip("How far the transform travels on points on the curve equal to 1 or -1. (Technically not 'max' but more intuitive than 'normalized' or something.)")]
    public float maxDistance = 1f;
    /// <summary>
    /// How long it takes for a full cycle, ie. playing through moveCurve all the way.
    /// </summary>
    [Tooltip("How long it takes for a full cycle, ie. playing through moveCurve all the way.")]
    public float cycleTimeSeconds = 2f;
    /// <summary>
    /// Timer that keeps track of where we are in the moveCurve at any given moment. 
    /// </summary>
    private float cycleTimer = 0f;

    /// <summary>
    /// True to move on the X axis.
    /// </summary>
    [Space(5)]
    [Tooltip("True to move on the X axis.")]
    public bool moveOnX = false;
    /// <summary>
    /// True to move on the Y axis.
    /// </summary>
    [Tooltip("True to move on the Y axis.")]
    public bool moveOnY = true;
    /// <summary>
    /// True to move on the Z axis.
    /// </summary>
    [Tooltip("True to move on the Z axis.")]
    public bool moveOnZ = false;

    /// <summary>
    /// Transform's localPosition at start. We move the transform relative to this each frame. 
    /// </summary>
    private Vector3 startPosition;

	// Use this for initialization
	void Start ()
    {
        startPosition = transform.localPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        cycleTimer += Time.deltaTime;
        if (cycleTimer > cycleTimeSeconds) cycleTimer %= cycleTimeSeconds; //Didn't know %= was a thing until now. :D

        float moveamount = moveCurve.Evaluate(cycleTimer / cycleTimeSeconds) * maxDistance;

        float xfinal = moveOnX ? startPosition.x + moveamount : transform.localPosition.x;
        float yfinal = moveOnY ? startPosition.y + moveamount : transform.localPosition.y;
        float zfinal = moveOnZ ? startPosition.z + moveamount : transform.localPosition.z;

        transform.localPosition = new Vector3(xfinal, yfinal, zfinal);
	}
}
