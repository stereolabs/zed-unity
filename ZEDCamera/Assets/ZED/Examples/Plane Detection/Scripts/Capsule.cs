using UnityEngine;
public class Capsule : MonoBehaviour
{
    [SerializeField]
    private CapsuleFollower _capsuleFollowerPrefab;

    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    private void Awake()
    {
        //Instantiate the prefab.
        var follower = Instantiate(_capsuleFollowerPrefab);
        //Set it to this object's position.
        follower.transform.position = transform.position;
        //Assign itself to its target to follow.
        follower.SetFollowTarget(this);
    }
}