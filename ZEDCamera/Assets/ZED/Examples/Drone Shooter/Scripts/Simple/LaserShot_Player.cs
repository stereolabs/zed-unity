using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Projectile that spawns an explosion effect on death and deals damage to objects with ILaserable in their root gameobject. 
/// </summary>
public class LaserShot_Player : Projectile
{
	[Tooltip("How much damage we do to a drone, or another object that inherits from ILaserable.")]
	public int Damage = 10;
	[Tooltip("Explosion object to spawn on death")]
	public GameObject ExplosionPrefab;

    public override void OnHitRealWorld(Vector3 collisionpoint, Vector3 collisionnormal)
    {
        SpawnExplosion(collisionpoint, collisionnormal);
        Destroy(gameObject);
    }

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

    void SpawnExplosion(Vector3 position, Vector3 normal)
    {
		if (ExplosionPrefab) //Only spawn an explosion if we have a prefab to spawn
        {
			GameObject explosion = Instantiate(ExplosionPrefab);
            explosion.transform.position = position;
            explosion.transform.rotation = Quaternion.LookRotation(normal);
        }
    }
}
