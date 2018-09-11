using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Projectile that spawns an explosion effect on death and causes the player to see a damage effect when hit. 
/// Inherits from Projectile.cs, where most logic not specific to the ZED Drone Battle sample is handled. 
/// </summary>
public class LaserShot_Drone : ZEDProjectile
{
    /// <summary>
    /// Explosion object to spawn on death.
    /// </summary>
	[Tooltip("Explosion object to spawn on death.")]
	public GameObject explosionPrefab;

    /// <summary>
    /// The current distance the object is from the left eye camera, used for collision calculation. 
    /// </summary>
    private float camdistance;

    private void Update()
    {
        camdistance = Vector3.Distance(transform.position, leftcamera.transform.position);
        if (camdistance < 1.5f)
            speed = 1f;
        else
            speed = 16f;
    }

    /// <summary>
    /// Called when the projectile hits the real world. 
    /// As the real world can't be a distinct gameobject (for now) we just spawn FX and destroy the projectile. 
    /// </summary>
    /// <param name="collisionpoint"></param>
    /// <param name="collisionnormal"></param>
    public override void OnHitRealWorld(Vector3 collisionpoint, Vector3 collisionnormal)
    {
        SpawnExplosion(collisionpoint, collisionnormal);
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the projectile hits a virtual object. 
    /// If that object has a PlayerDamageReceiver, deal damage and destroy the projectile. 
    /// Otherwise ignore it so the drone can't shoot itself. 
    /// </summary>
    /// <param name="hitinfo"></param>
    public override void OnHitVirtualWorld(RaycastHit hitinfo)
    {
        //Check to see if it's the player we hit. If so, deal damage. 
        PlayerDamageReceiver receiver = hitinfo.collider.gameObject.GetComponent<PlayerDamageReceiver>();
        if (receiver != null)  //We don't want any collision logic if it's not hitting the player, so we can avoid the drone shooting itself.
        {
            receiver.TakeDamage(); 

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Creates an explosion effect at the desired location, used when the laser is destroyed. 
    /// </summary>
    /// <param name="position">Where to spawn the explosion.</param>
    /// <param name="normal">Which direction to orient the explosion. Should be based off the surface normal of the object the projectile hit.</param>
    void SpawnExplosion(Vector3 position, Vector3 normal)
    {
		if (explosionPrefab) //Only spawn an explosion if we have a prefab to spawn
        {
			GameObject explosion = Instantiate(explosionPrefab);
            explosion.transform.position = position;
            explosion.transform.rotation = Quaternion.LookRotation(normal);
        }
    }
}