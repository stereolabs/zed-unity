using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Governs the FlyingBunny prefab used in the ZED plane detection samples. 
/// Checks for collisions in front of it in midair, and spawns the flag when it hits the ground. 
/// </summary>
public class Bunny : MonoBehaviour
{
    /// <summary>
    /// Stores initial position when we are enabled/spawned into the scene.
    /// Used to calculate how far the bunny traveled after we hit it in the VR plane detection demo. 
    /// </summary>
    public Vector3 InitialPosition { get; private set; } 

    /// <summary>
    /// True whenever this object is moved by the baseball bat. 
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Reference to the BunnySpawner that spawned this GameObject.
    /// </summary>
    private BunnySpawner bunnyspawner;

    /// <summary>
    /// The transform used as center/pivot point for this GameObject when checking for collisions with the real world.
    /// </summary>
    private Transform centerpoint;

    /// <summary>
    /// Reference to the scene's ZED Plane Detection Manager.
    /// </summary>
    private ZEDPlaneDetectionManager planeManager;

    /// <summary>
    /// The bunny's Rigidbody component. 
    /// </summary>
    private Rigidbody rb;

    /// <summary>
    /// The Bunny's Animator component.
    /// </summary>
    [HideInInspector]
    public Animator anim;

    // Use this for initialization.
    void Start()
    {      
        IsMoving = false; //we're not moving at start.

        //Caching
        planeManager = FindObjectOfType<ZEDPlaneDetectionManager>();
        rb = GetComponent<Rigidbody>(); //Get our Rigidbody component.
        anim = GetComponent<Animator>();

        InitialPosition = transform.position; //Save for calculating distance traveled later. 

        //If there is a child in position 2, use it as new centerPoint.
        if (transform.GetChild(2) != null)
        {
            centerpoint = transform.GetChild(2);
        }
        else //If not, use this transform.
        {
            centerpoint = transform;
        }

    }

    /// <summary>
    /// Sets the BunnySpawner.
    /// </summary>
    /// <param name="spawner"></param>
    public void SetMySpawner(BunnySpawner spawner)
    {
        bunnyspawner = spawner;
    }

    /// <summary>
    /// Starts the HitDelay coroutine.
    /// </summary>
    public void GetHit(bool hit)
    {
        GetComponent<Rigidbody>().drag = 0f;
        GetComponent<Rigidbody>().angularDrag = 0.5f;
        StartCoroutine(HitDelay(hit));
    }

    /// <summary>
    /// Coroutine used to delay the collision detection of the Bunny.
    /// Setting the _moving variable after waiting X seconds.
    /// </summary>
    IEnumerator HitDelay(bool hit)
    {
        //Wait for X amount of seconds...
        yield return new WaitForSeconds(0.1f);
        if (hit)
        {
            //... then set IsMoving to true, and allow collision detection in FixedUpdate().
            IsMoving = true;
        }
        else
        {
            rb.isKinematic = true; //Freeze the object at the current position.
            yield return new WaitForSeconds(1f);
            bunnyspawner.SpawnUI(transform.position);
        }

        //Clearing the scene from any Planes created by the ZED Plane Detection Manager.
		for (int i = 0; i < planeManager.hitPlaneList.Count; i++)
        {
			Destroy(planeManager.hitPlaneList[i].gameObject);
			planeManager.hitPlaneList.RemoveAt(i);
        }
    }

    /// <summary>
    /// This function is called every fixed framerate frame
    /// Here we take care of enabling & disabling the laser pointer.
    /// </summary>
    private void FixedUpdate()
    {
        //If we have been moved by the baseball bat
        if (IsMoving)
        {
            //Look for our next position based on our current velocity.
            Vector3 predictedPos = centerpoint.position + (rb.velocity * (Time.deltaTime * 2.5f));
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized);

            //Collision check with the real world at that next position.
            foreach (ZEDManager manager in ZEDManager.GetInstances()) //Check all active cameras. 
            {
                if (ZEDSupportFunctions.HitTestAtPoint(manager.zedCamera, manager.GetMainCamera(), predictedPos))
                {
                    //We hit something, but is it a flat surface?
                    if (planeManager.DetectPlaneAtHit(manager, manager.GetMainCamera().WorldToScreenPoint(predictedPos)))
                    {
                        bunnyspawner.SpawnUI(predictedPos);
                        IsMoving = false;
                    }
                    else//If not, bounce off of it but still show the flag. 
                    {
                        IsMoving = false; //Not moving anymore, so update our state.
                        bunnyspawner.SpawnUI(predictedPos); //Start spawning the UI on our current location.
                        rb.velocity = Vector3.Reflect(rb.velocity / 2, transform.forward); //Bounce off the surface we hit 
                    }

                    break; //If it hit the real world in one camera's view, no need to check the other cameras. 
                }
            }
        }
    }

}
