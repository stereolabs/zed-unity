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
    /// If true, all active ZEDs are used to check for collisions. 
    /// <para>This can mean being able to shoot things that aren't visible to the first player, but due to the 
    /// nature of collisions testing if virtual objects are behind the real world, can lead to 
    /// false positives if, for instance, a player in pass-through AR is between a projectile and a third-person ZED. 
    /// Use the realWorldThickness value to help mitigate this.</para>
    /// </summary>
    [Tooltip("If true, all active ZEDs are used to check for collisions. " +
        "This can mean being able to shoot things that aren't visible to the first player, but due to the " +
        "nature of collisions testing if virtual objects are behind the real world, can lead to " +
        "false positives if, for instance, a player in pass-through AR is between a projectile and a third-person ZED. " +
        "Use the realWorldThickness value to help mitigate this.")]
    public bool testCollisionsUsingAllZEDs = true;

    /// <summary>
    /// Primary ZEDManager, used for collision detection when testCollisionsUsingAllZEDs is set to false. 
    /// </summary>
    public ZEDManager zedManager = null;
    /// <summary>
    /// The ZED camera reference, used for calling ZEDSupportFunctions to transform depth data from the ZED into world space. 
    /// </summary>
	protected Camera leftcamera;

    private void Awake()
    {
        if (zedManager == null)
        {
            zedManager = FindObjectOfType<ZEDManager>();
            //If this happenend when only using a primary ZED for collisions but there are multiple ZEDs, collisions will be
            //calculated using an arbitrary camera. Warn the user. 
            if (testCollisionsUsingAllZEDs == false && ZEDManager.GetInstances().Count > 1)
            {
                Debug.LogWarning("Warning: ZEDProjectile's zedManager value was not specified, resulting in assigning to first available " +
                    " camera, but there are multiple cameras in the scene. This can cause strange collision test behavior.");
            }
        }

        if (!leftcamera)
        {
            leftcamera = zedManager.GetLeftCamera();
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
        Vector3 collisionnormal;

        //First, test the primary ZED. Collisions will look the most accurate if calculated from this one. 
        bool primaryhit = ZEDSupportFunctions.HitTestOnRay(zedManager.zedCamera, leftcamera, newpos, transform.rotation, Vector3.Distance(transform.position, newpos), 
            distanceBetweenRayChecks, out collisionpoint, false, realWorldThickness);
        if (primaryhit)
        {
            //Call the function to resolve the impact. This allows us to easily override what happens on collisions with classes that inherit this one. 
            ZEDSupportFunctions.GetNormalAtWorldLocation(zedManager.zedCamera, collisionpoint, sl.REFERENCE_FRAME.WORLD, leftcamera, out collisionnormal);

            OnHitRealWorld(collisionpoint, collisionnormal);
        }

        if (!primaryhit && testCollisionsUsingAllZEDs) //If set to true, test the rest of the ZEDs as well. 
        {
            foreach (ZEDManager manager in ZEDManager.GetInstances())
            {
                if (manager == zedManager) continue; //If it's the primary ZED, skip as we've already tested that one. 

                if (ZEDSupportFunctions.HitTestOnRay(manager.zedCamera, manager.GetLeftCamera(), newpos, transform.rotation, Vector3.Distance(transform.position, newpos), distanceBetweenRayChecks, out collisionpoint, false, realWorldThickness))
                {
                    //Call the function to resolve the impact. This allows us to easily override what happens on collisions with classes that inherit this one. 
                    ZEDSupportFunctions.GetNormalAtWorldLocation(manager.zedCamera, collisionpoint, sl.REFERENCE_FRAME.WORLD, manager.GetLeftCamera(), out collisionnormal);

                    OnHitRealWorld(collisionpoint, collisionnormal);
                    break; //No need to test the rest of the ZEDs. 
                }
            }
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
