using UnityEngine;

/// <summary>
/// Follows a given Capsule object. Used because VR controller movements don't allow for direct physics simulation.
/// Instead, we apply phyics forces by script to make separate capsule colliders follow that controller,
/// which then apply their forces to objects that this object would normally hit. 
/// This script is attached to those follower objects. The object being followed has the Capsule component. 
/// Used in the ZED VR plane detection sample for the baseball bat. 
/// </summary>
public class CapsuleFollower : MonoBehaviour
{
    /// <summary>
    /// Object this capsule will follow. 
    /// </summary>
    private Capsule target;

    /// <summary>
    /// Rigidbody attached to this object. 
    /// </summary>
    private Rigidbody rb;

    /// <summary>
    /// The velocity the rigidbody needs to be set to. Used so we can update the target velocity
    /// in Update() but apply to the rigidbody in FixedUpdate() (which is where physics is actually run). 
    /// </summary>
    private Vector3 velocity;

    /// <summary>
    /// The collider attached to this object. 
    /// </summary>
    private Collider capsulecollider;

    /// <summary>
    /// Multiplier used to govern how quickly this follower follows its target when it moves. 
    /// Higher values will make it lag behind less but may cause it to overshoot. 
    /// </summary>
    [SerializeField]
    private float sensitivity = 50;

    /// <summary>
    /// Awake is used to initialize any variables or game states before the game starts.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsulecollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Update is called every frame.
    /// Here we enable/disable the collider whenever baseball bat is active or not.
    /// </summary>
    private void Update()
    {
        if (target.transform.parent.gameObject.activeInHierarchy)
        {
            capsulecollider.enabled = true;
        }
        else
            capsulecollider.enabled = false;
    }

    /// <summary>
    /// This function is called every fixed framerate frame.
    /// Here we calculate the velocity of our rigidbody based on the movement towards the target.
    /// </summary>
    private void FixedUpdate()
    {
        Vector3 destination = target.transform.position;
        rb.transform.rotation = target.transform.rotation;

        velocity = (destination - rb.transform.position) * sensitivity;

        rb.velocity = velocity;
    }

    /// <summary>
    /// When another collider enters ours, we assign our rigidbody's velocity to its
    /// In the ZED VR plane detection sample, this is how the bunny gets launched. .
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        Bunny colBunny = other.GetComponent<Bunny>();
        //Checking if its a Bunny, with a Rigidbody and that is not moving.
        if (colBunny != null)
        {
            if (other.GetComponent<Rigidbody>() && !colBunny.IsMoving)
            {
                if (rb.velocity.y <= -2)
                {
                    colBunny.anim.SetTrigger("Squeeze");
                    colBunny.GetHit(hit: false);
                }
                else if (rb.velocity.magnitude > 2f)
                {
                    //Send a call to GetHit() which delays for X seconds the Bunny's detection with the real world.
                    //Since the Bunny is already on the floor, it might return true for collision the moment the baseball bat touches it.
                    colBunny.GetHit(hit: true);

                    //Assign our velocity with some changes. Halving the velocity makes it feel more natural when hitting the bunny. 
                    other.GetComponent<Rigidbody>().velocity = rb.velocity / 2;
                }
            }
        }
    }

    /// <summary>
    /// Sets the target to follow. Called by Capsule. 
    /// </summary>
    /// <param name="myTarget"></param>
    public void SetFollowTarget(Capsule myTarget)
    {
        target = myTarget;
    }
}