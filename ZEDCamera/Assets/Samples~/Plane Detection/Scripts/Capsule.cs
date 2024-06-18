using UnityEngine;

/// <summary>
/// Creates a CapsuleFollower object that follows this one but that has proper physics simulation. 
/// Used because VR controller movements don't allow for direct physics simulation.
/// In the ZED VR plane detection sample, this is attached to four parts of the baseball bat used to hit the bunny.
/// See CapsuleFollower.cs for more information. 
/// </summary>
public class Capsule : MonoBehaviour
{
    /// <summary>
    /// CapsuleFollower script within a prefab that contains the script, a collider, and a rigidbody. 
    /// </summary>
    [SerializeField]
    [Tooltip("CapsuleFollower script within a prefab that contains the script, a collider, and a rigidbody.")]
    private CapsuleFollower capsulefollowerprefab;

    private void Awake()
    {
        var follower = Instantiate(capsulefollowerprefab); //Instantiate the prefab.
        follower.transform.position = transform.position; //Set it to this object's position.
        follower.SetFollowTarget(this); //Assign this as its target to follow.
    }
}