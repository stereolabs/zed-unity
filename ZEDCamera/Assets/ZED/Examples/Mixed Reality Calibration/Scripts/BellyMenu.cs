using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps its position at the same as a given transform, and it's Y rotation too, both with deadzones. 
/// Movements aren't instant, to avoid motion sickness and add realism. 
/// Used to make the 'belly menu' near the user in the MR calibration scene, which is enabled if they
/// only have one controller available (because the other is used to anchor the ZED). 
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BellyMenu : MonoBehaviour
{
    /// <summary>
    /// Transform that this object will follow. Should be set to the main VR camera. 
    /// </summary>
    [Tooltip("Transform that this object will follow. Should be set to the main VR camera. ")]
    public Transform followTransform;

    /// <summary>
    /// How far the follow transform has to be before this transform starts moving toward it.
    /// </summary>
    [Tooltip("How far the follow transform has to be before this transform starts moving toward it.")]
    public float moveDeadzoneMeters = 0.2f;
    /// <summary>
    /// How much of an angle the follow transform has to be before this transform starts rotating toward it.
    /// </summary>
    [Tooltip("How much of an angle the follow transform has to be before this transform starts rotating toward it.")]
    public float turnDeadzoneDegrees = 40f;
    /// <summary>
    /// Maximum force used to translate this object, when using physics. 
    /// </summary>
    [Tooltip("Maximum force used to translate this object, when using physics. ")]
    public float maxMoveForce = 0.3f;
    /// <summary>
    /// The fastest this transform will rotate to keep up with the follow transform. 
    /// </summary>
    [Tooltip("The fastest this transform will rotate to keep up with the follow transform. ")]
    public float maxDegreesPerSecond = 60f;

    /// <summary>
    /// If true, translates gradually to keep up with the follow transform. Otherwise, movement is instant. 
    /// </summary>
    [Tooltip("If true, translates gradually to keep up with the follow transform. Otherwise, movement is instant. ")]
    public bool gradualMove = false;
    /// <summary>
    /// If true, rotates gradually to keep up with the follow transform. Otherwise, rotation is instant. 
    /// </summary>
    [Tooltip("If true, rotates gradually to keep up with the follow transform. Otherwise, rotation is instant. ")]
    public bool gradualTurn = true;

    /// <summary>
    /// What multiple of the HMD's height should the object move toward. For instance, 0.5 will go to 100cm on a 200cm person. 
    /// This is designed for following a Camera for a person in a VR headset. 
    /// </summary>
    [Tooltip("What multiple of the HMD's height should the object move toward. For instance, 0.5 will go to 100cm on a 200cm person. " +
        "This is designed for following a Camera for a person in a VR headset.")]
    public float waistHeightMultiple = 0.65f;

    private Rigidbody _rb;

	// Use this for initialization
	void Awake ()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;

        if(!followTransform)
        {
            followTransform = Camera.main.transform;
        }
	}

    // Update is called once per frame
    void FixedUpdate ()
    {
        //First, position. 
        if (gradualMove)
        {
            Vector3 targetarea = followTransform.position;
            targetarea.y = followTransform.position.y * waistHeightMultiple; //So it'll be around their waste. 
            Vector3 posdiff = targetarea - transform.position;
            if (posdiff.magnitude > moveDeadzoneMeters) //Too far away.
            {
                _rb.AddForce(posdiff.normalized * maxMoveForce);
            }
            else if (posdiff.magnitude < moveDeadzoneMeters / 2f) //Too close. Slow down extra fast. 
            {
                _rb.velocity *= 0.9f;
                if (_rb.velocity.magnitude < 0.05f) _rb.velocity = Vector3.zero;
            }
        }
        else //Not gradual move. 
        {
            Vector3 targetarea = followTransform.position;
            targetarea.y = followTransform.position.y * waistHeightMultiple; //So it'll be around their waste. 
            transform.position = targetarea;
        }

        //Now rotation. 
        if (gradualTurn)
        {

            Vector3 thisforward = transform.forward;
            thisforward.y = 0f;
            Vector3 followforward = followTransform.forward;
            followforward.y = 0f;

            float angle = Vector3.SignedAngle(thisforward, followforward, Vector3.up);

            if (Mathf.Abs(angle) > turnDeadzoneDegrees / 2f)
            {
                float speed = Mathf.InverseLerp(turnDeadzoneDegrees / 2f, turnDeadzoneDegrees, Mathf.Abs(angle));
                Vector3 newforward = Vector3.RotateTowards(thisforward, followforward, maxDegreesPerSecond * speed * Time.fixedDeltaTime * Mathf.Deg2Rad, 0f);
                transform.rotation = Quaternion.LookRotation(newforward, Vector3.up);
            }
        }
        else
        {
            //Vector3 targetrot = new Vector3(0, followTransform.eulerAngles.y, 0);
            transform.eulerAngles = new Vector3(0, followTransform.eulerAngles.y, 0);
        }
    }
}
