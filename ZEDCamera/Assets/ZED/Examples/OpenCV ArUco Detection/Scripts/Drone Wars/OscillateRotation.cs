using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OscillateRotation : MonoBehaviour
{
    /// <summary>
    /// Curve that defines the pattern we rotate the transform over time.
    /// </summary>
    [Tooltip("Curve that defines the pattern we rotate the transform over time.")]
    public AnimationCurve rotateCurve = new AnimationCurve(new Keyframe[5]
    {
        new Keyframe(0, 0, 5, 5),
        new Keyframe(0.25f, 1, 0, 0),
        new Keyframe(0.5f, 0, -5, -5),
        new Keyframe(0.75f, -1, 0, 0),
        new Keyframe(1, 0, 5, 5)
    });
    /// <summary>
    /// How far the transform rotates on points on the curve equal to 1 or -1. (Technically not "max" but more intuitive than "normalized" or something.)
    /// </summary>
    [Tooltip("How far the transform rotates on points on the curve equal to 1 or -1. (Technically not 'max' but more intuitive than 'normalized' or something.)")]
    public float maxDegrees = 20f;
    /// <summary>
    /// How long it takes for a full cycle, ie. playing through rotateCurve all the way.
    /// </summary>
    [Tooltip("How long it takes for a full cycle, ie. playing through rotateCurve all the way.")]
    public float cycleTimeSeconds = 2f;
    /// <summary>
    /// Timer that keeps track of where we are in the rotateCurve at any given moment. 
    /// </summary>
    private float cycleTimer = 0f;

    /// <summary>
    /// True to rotate on the X axis.
    /// </summary>
    [Space(5)]
    [Tooltip("True to rotate on the X axis.")]
    public bool rotateOnX = false;
    /// <summary>
    /// True to rotate on the Y axis.
    /// </summary>
    [Tooltip("True to rotate on the Y axis.")]
    public bool rotateOnY = false;
    /// <summary>
    /// True to rotate on the Z axis.
    /// </summary>
    [Tooltip("True to rotate on the Z axis.")]
    public bool rotateOnZ = true;

    public Vector3 startRotationEulers;

    // Use this for initialization
    void Start ()
    {
        startRotationEulers = transform.localRotation.eulerAngles;
    }
	
	// Update is called once per frame
	void Update ()
    {
        cycleTimer += Time.deltaTime;
        if (cycleTimer > cycleTimeSeconds) cycleTimer %= cycleTimeSeconds; 

        float rotateamount = rotateCurve.Evaluate(cycleTimer / cycleTimeSeconds) * maxDegrees;

        float xfinal = rotateOnX ? startRotationEulers.x + rotateamount : transform.localEulerAngles.x;
        float yfinal = rotateOnY ? startRotationEulers.y + rotateamount : transform.localEulerAngles.y;
        float zfinal = rotateOnZ ? startRotationEulers.z + rotateamount : transform.localEulerAngles.z;

        transform.localEulerAngles = new Vector3(xfinal, yfinal, zfinal);
    }
}
