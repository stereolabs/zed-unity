using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Laser spawned from an AruCoDrone. Should be in a prefab given to ArUcoDrone.projectilePrefab.
/// See parent class ZEDProjectile to see how projectile collision tests (real and virtual) are performed. 
/// </summary>
public class ArUcoDroneLaser : ZEDProjectile
{
    /// <summary>
    /// How much damage is dealt to drones we hit.
    /// </summary>
    [Tooltip("How much damage is dealt to drones we hit.")]
    public float damage;
    /// <summary>
    /// Which drone spawned this laser. Used to avoid colliding with it.
    /// </summary>
    [HideInInspector]
    public ArUcoDrone spawningDrone;
    /// <summary>
    /// Object spawned when the laser hits something.
    /// </summary>
    [Tooltip("Object spawned when the laser hits something.")]
    public GameObject explosionPrefab;

    /// <summary>
    /// Called when the projectile hits a virtual object with a collider. 
    /// If the object is an ArUco drone, applies damage. Regardless, spawns an explosion and destroys the projectile.
    /// </summary>
    public override void OnHitVirtualWorld(RaycastHit hitinfo)
    {
        ArUcoDrone otherdrone = hitinfo.transform.GetComponent<ArUcoDrone>();
        if(otherdrone != null && otherdrone != spawningDrone)
        {
            otherdrone.TakeDamage(damage);
        }

        if(explosionPrefab)
        {
            Quaternion explosionrotation = (hitinfo.normal.sqrMagnitude <= 0.001f) ? Quaternion.identity : Quaternion.LookRotation(hitinfo.normal, Vector3.up);
            GameObject explosion = Instantiate(explosionPrefab, hitinfo.point, explosionrotation);
        }

        base.OnHitVirtualWorld(hitinfo); //Destroy. 
    }

    /// <summary>
    /// Called when the projectile hits the real world. Creates an explosion and destroys the projectile.
    /// </summary>
    public override void OnHitRealWorld(Vector3 collisionpoint, Vector3 collisionnormal)
    {
        if (explosionPrefab)
        {
            Quaternion explosionrotation = (collisionnormal.sqrMagnitude <= 0.001f) ? Quaternion.identity : Quaternion.LookRotation(collisionnormal, Vector3.up);
            GameObject explosion = Instantiate(explosionPrefab, collisionpoint, explosionrotation);
        }

        base.OnHitRealWorld(collisionpoint, collisionnormal); //Destroy. 
    }
}
