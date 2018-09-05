using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Checks for collisions with both the real and virtual world and moves forward each frame. 
/// Inherit from this class to easily make your own projectile that can hit both the virtual and real. 
/// See LaserShot_Drone and LaserShot_Player in the ZED Drone Battle sample for examples. 
/// Real world "collisions" test if it's behind the real world using the static hit test functions in ZEDSupportFunctions. 
/// </summary>
public class ZEDProjectile : MonoBehaviour
{
    /// <summary>
    /// How fast the projectile moves forward in meters per second.
    /// </summary>
	[Tooltip("How fast the projectile moves forward in meters per second.")]
	public float speed = 10f;

    /// <summary>
    /// How long the projectile lasts before being destroyed, even if it doesn't collide with anything.
    /// </summary>
	[Tooltip("How long the projectile lasts before being destroyed, even if it doesn't collide with anything.")]
	public float lifespan = 8f;

    /// <summary>
    /// How granular the dots are along the fake raycast done for real-world collisions. 
    /// </summary>
	[Tooltip("How granular the dots are along the fake raycast done for real-world collisions.")]
	public float distanceBetweenRayChecks = 0.01f;

    /// <summary>
    /// How far behind a real pixel can a collision check decide it's not a collision.
    /// </summary>
	[Tooltip("How far behind a real pixel can a collision check decide it's not a collision.")]
	public float realWorldThickness = 0.05f;

    /// <summary>
    /// The ZED camera reference, used for calling ZEDSupportFunctions to transform depth data from the ZED into world space. 
    /// </summary>
    protected Camera leftcamera;

    private void Awake()
    {
        if (!leftcamera)
        {
            leftcamera = ZEDManager.Instance.GetLeftCameraTransform().GetComponent<Camera>();
        }
    }

    /// <summary>
    /// Handles movements and collisions on a constant basis. 
    /// </summary>
    void FixedUpdate()
    {
        //Calculate where the object should move this frame
        Vector3 newpos = transform.position + transform.rotation * Vector3.forward * (speed * Time.deltaTime);

        //Collisions with the real World. As the object moves, collisions checks are made each frame at the next position.
        Vector3 collisionpoint;
        if (ZEDSupportFunctions.HitTestOnRay(leftcamera, newpos, transform.rotation, Vector3.Distance(transform.position, newpos), distanceBetweenRayChecks, out collisionpoint, false, realWorldThickness))
        {
            //Call the function to resolve the impact. This allows us to easily override what happens on collisions with classes that inherit this one. 
            Vector3 collisionnormal;
            ZEDSupportFunctions.GetNormalAtWorldLocation(collisionpoint, sl.REFERENCE_FRAME.WORLD, leftcamera, out collisionnormal);

            OnHitRealWorld(collisionpoint, collisionnormal);
        }

        //Collisions with virtual objects
        //Cast a ray to check collisions between here and the intended move point for virtual objects. 
        RaycastHit hitinfo;
        if (Physics.Raycast(transform.position, newpos - transform.position, out hitinfo, Vector3.Distance(transform.position, newpos)))
        {
            //Call the function to resolve the impact. This allows us to easily override what happens on collisions with classes that inherit this one. 
            OnHitVirtualWorld(hitinfo);
        }

        //Move it to this new place
        transform.position = newpos;

        //Tick down its lifespan and check if we should destroy it.
        lifespan -= Time.deltaTime;
        if (lifespan <= 0f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the projectile hits real geometry. Override to spawn effects, play sounds, etc. 
    /// </summary>
    /// <param name="collisionpoint"></param>
    /// <param name="collisionnormal"></param>
    public virtual void OnHitRealWorld(Vector3 collisionpoint, Vector3 collisionnormal)
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the projectile hits a virtual collider. Override to spawn effects, inflict damage, etc. 
    /// </summary>
    /// <param name="hitinfo">The RayCastHit supplied by the raycast used to detect the collision.</param>
    public virtual void OnHitVirtualWorld(RaycastHit hitinfo)
    {
        Destroy(gameObject);
    }
}
