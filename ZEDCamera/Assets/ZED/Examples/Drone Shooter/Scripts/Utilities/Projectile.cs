using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Moves a gameobject forward each FixedUpdate, checks for collisions with both the real and virtual world, and dies after a set lifespan. 
/// Inherit from this class to easily make your own projectile. 
/// </summary>
public class Projectile : MonoBehaviour
{
	[Tooltip("In meters per second")]
	public float Speed = 10f;
	[Tooltip("In seconds")]
	public float Lifespan = 8f;
	[Tooltip("How granular the dots are along the fake raycast done for collisions.")]
	public float DistanceBetweenRayChecks = 0.01f;
	[Tooltip("How far behind a real pixel can a collision check decide it's not a collision.")]
	public float RealWorldThickness = 0.05f;
	[Tooltip("Explosion object to spawn on death")]

    //Needed for depth calculation
    [HideInInspector]
    public Camera _leftCamera;
    private void Awake()
    {
        if (!_leftCamera)
        {
            _leftCamera = ZEDManager.Instance.GetLeftCameraTransform().GetComponent<Camera>();
        }
    }

    /// <summary>
    /// Handles movements and collisions on a constant basis. 
    /// </summary>
    void FixedUpdate()
    {
        //Calculate where the object should move this frame
        Vector3 newpos = transform.position + transform.rotation * Vector3.forward * (Speed * Time.deltaTime);

        //Collisions with the real World. As the object moves, collisions checks are made each frame at the next position.
        Vector3 collisionpoint;
        if (ZEDSupportFunctions.HitTestOnRay(_leftCamera, newpos, transform.rotation, Vector3.Distance(transform.position, newpos), DistanceBetweenRayChecks, out collisionpoint, false, RealWorldThickness))
        {
            //Call the function to resolve the impact. This allows us to easily override what happens on collisions with classes that inherit this one. 
            Vector3 collisionnormal;
            ZEDSupportFunctions.GetNormalAtWorldLocation(collisionpoint, sl.REFERENCE_FRAME.WORLD, _leftCamera, out collisionnormal);

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
        Lifespan -= Time.deltaTime;
        if (Lifespan <= 0f)
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
    /// <param name="hitinfo"></param>
    public virtual void OnHitVirtualWorld(RaycastHit hitinfo)
    {
        Destroy(gameObject);
    }
}
