using UnityEngine;

public class CapsuleFollower : MonoBehaviour
{
    private Capsule _target;
    private Rigidbody _rigidbody;
    private Vector3 _velocity;
    private Collider _collider;
    [SerializeField]
    private float _sensitivity = 50;

    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    /// <summary>
    /// Update is called every frame.
    /// Here we enable.disable the collider whenever baseball bat is active or not.
    /// </summary>
    private void Update()
    {
        if (_target.transform.parent.gameObject.activeInHierarchy)
        {
            _collider.enabled = true;
        }
        else
            _collider.enabled = false;
    }

    /// <summary>
    /// This function is called every fixed framerate frame.
    /// Here we calculate the velocity of our rigidbody based on the movement towards the target.
    /// </summary>
    private void FixedUpdate()
    {
        Vector3 destination = _target.transform.position;
        _rigidbody.transform.rotation = _target.transform.rotation;

        _velocity = (destination - _rigidbody.transform.position) * _sensitivity;

        _rigidbody.velocity = _velocity;
    }

    /// <summary>
    /// When another collider enters ours, we assign our rigidbody's velocity to his.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        Bunny colBunny = other.GetComponent<Bunny>();
        //Checking if its a Bunny, with a Rigidbody and that is not moving.
        if (colBunny != null)
        {
            if (other.GetComponent<Rigidbody>() && !colBunny._moving)
            {
                if (_rigidbody.velocity.y <= -2)
                {
                    colBunny.anim.SetTrigger("Squeeze");
                    colBunny.GetHit(hit: false);
                }
                else if (_rigidbody.velocity.magnitude > 2f)
                {
                    //Send a call to GetHit() which delays for X seconds the Bunny's detection with the real world.
                    //Since the Bunny is already on the floor, it might return true for collision the moment the baseball bat touches it.
                    colBunny.GetHit(hit: true);
                    //Assign our velocity with some changes. I found that it feels better when it's half the force.
                    other.GetComponent<Rigidbody>().velocity = _rigidbody.velocity / 2;
                }
            }
        }
    }

    /// <summary>
    /// Sets the target to follow.
    /// </summary>
    /// <param name="batFollower"></param>
    public void SetFollowTarget(Capsule myTarget)
    {
        _target = myTarget;
    }
}