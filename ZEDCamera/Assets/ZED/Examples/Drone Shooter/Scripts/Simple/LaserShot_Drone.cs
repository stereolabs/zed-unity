using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Projectile that spawns an explosion effect on death and causes the player to see a damage effect when hit. 
/// </summary>
public class LaserShot_Drone : Projectile
{
	[Tooltip("Explosion object to spawn on death")]
	public GameObject ExplosionPrefab;
    private float camDistance;

    private void Update()
    {
        camDistance = Vector3.Distance(transform.position, _leftCamera.transform.position);
        if (camDistance < 1.5f)
            Speed = 1f;
        else
            Speed = 16f;
    }

    public override void OnHitRealWorld(Vector3 collisionpoint, Vector3 collisionnormal)
    {
        SpawnExplosion(collisionpoint, collisionnormal);
        Destroy(gameObject);
    }

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