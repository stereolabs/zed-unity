using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Projectile that spawns an explosion effect on death and deals damage to objects with ILaserable in their root gameobject. 
/// Inherits from Projectile.cs, where most logic not specific to the ZED Drone Battle sample is handled. 
/// </summary>
public class LaserShot_Player : ZEDProjectile
{
    /// <summary>
    /// How much damage we do to a drone, or another object that inherits from ILaserable.
    /// </summary>
	[Tooltip("How much damage we do to a drone, or another object that inherits from ILaserable.")]
	public int Damage = 10;

    /// <summary>
    /// Explosion object to spawn on death.
    /// </summary>
	[Tooltip("Explosion object to spawn on death.")]
	public GameObject explosionPrefab;

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
    /// If the object has a component that implements ILaserable, deal damage and destroy the projectile. 
    /// Otherwise ignore it so the player can't shoot themselves. 
    /// </summary>
    /// <param name="hitinfo"></param>
    public override void OnHitVirtualWorld(RaycastHit hitinfo)
    {
        //Deal damage to an object if it contains ILaserable, such as a drone
        ILaserable laserable = hitinfo.collider.transform.root.GetComponent<ILaserable>();
        if (laserable != null) //We don't want any collision logic if it doesn't have ILaserable, so we can avoid the player shooting themselves in the head. 
        {
            //Inflict damage on the object we hit
            laserable.TakeDamage(Damage);

            //Create the explosion
            SpawnExplosion(hitinfo.point, hitinfo.normal);

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
