using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior for drone objects in the ZED ArUco Drone Wars sample. 
/// When other drones are in front of this drone and within range, will fire projectiles until it's dead. 
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArUcoDrone : MonoBehaviour
{
    /// <summary>
    /// How close other drones have to be for this drone to start firing. Must also be directly in front of the drone.
    /// </summary>
    [Tooltip("How close other drones have to be for this drone to start firing. Must also be directly in front of the drone.")]
    public float shootRangeMeters = 0.25f;
    /// <summary>
    /// Time between each shot when firing.
    /// </summary>
    [Tooltip("Time between each shot when firing.")]
    public float shootCooldownSeconds = 0.33f;
    /// <summary>
    /// How much damage this drone can take before dying.
    /// </summary>
    [Tooltip("How much damage this drone can take before dying.")]
    public float maxHealth = 10;
    /// <summary>
    /// How much health the drone currently has. Goes down when a ArUcoDroneLaser hits it and calls TakeDamage().
    /// </summary>
    private float currentHealth;
    /// <summary>
    /// Time since last shot. Set to zero after it exceeds shootCooldownSeconds. 
    /// </summary>
    private float shootTimer = 0f;
    /// <summary>
    /// Whether the drone can shoot right now. Simply checks if shootTimer is 0, which the Shoot() coroutine will set it to when finished. 
    /// </summary>
    private bool canShoot
    {
        get
        {
            return shootTimer <= 0f;
        }
    }

    /// <summary>
    /// Prefab of the projectile object the drone shoots. Should have an ArUcoDroneLaser component on it.
    /// </summary>
    [Tooltip("Prefab of the projectile object the drone shoots. Should have an ArUcoDroneLaser component on it.")]
    public GameObject projectilePrefab;
    /// <summary>
    /// Transform where laser shots are spawned, and from where the drone's laser range is tested.
    /// </summary>
    [Tooltip("Transform where laser shots are spawned, and from where the drone's laser range is tested.")]
    public Transform shootAnchorObject;
    /// <summary>
    /// Object spawned when the drone dies.
    /// </summary>
    [Tooltip("Object spawned when the drone dies.")]
    public GameObject explosionPrefab;

    private Collider col;
    /// <summary>
    /// Sound played every time the drone fires a laser. Requires an AudioSource attached to this GameObject.
    /// </summary>
    [Tooltip("Sound played every time the drone fires a laser. Requires an AudioSource attached to this GameObject.")]
    public AudioClip laserShootSound;
    private AudioSource audioSource;
    /// <summary>
    /// If true, will use the ZED's depth to check if there's a real-world object between this drone and its target.
    /// </summary>
    [Tooltip("If true, will use the ZED's depth to check if there's a real-world object between this drone and its target.")]
    public bool checkRealWorldObstaclesBeforeShooting = false;

	// Use this for initialization
	void Start ()
    {
        col = GetComponent<Collider>();
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
	}

    private void OnEnable()
    {
        shootTimer = 0f; //If the drone was disabled from being hidden, this prevents laser not being able to fire. 
    }

    // Update is called once per frame
    void Update ()
    {
		if(canShoot == true && IsAimingAtTarget() == true)
        {
            StartCoroutine(Shoot());
        }
	}

    /// <summary>
    /// Checks if another ArUcoDrone is in front of the drone and within range. 
    /// Will also check if there's a real object in the way, if checkRealWorldObstaclesBeforeShooting is true. 
    /// </summary>
    /// <returns>True if there's a valid target in front of the drone.</returns>
    private bool IsAimingAtTarget()
    {
        //First make sure there's a valid virtual target in front of the drone. 
        Ray ray = new Ray(shootAnchorObject.position, shootAnchorObject.rotation * Vector3.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, shootRangeMeters);

        bool foundvirtualtarget = false;
        float nearestdist = Mathf.Infinity;
        foreach(RaycastHit hit in hits)
        {
            ArUcoDrone otherdrone = hit.transform.GetComponent<ArUcoDrone>();

            if(otherdrone != null && otherdrone != this)
            {
                foundvirtualtarget = true;
                if (hit.distance < nearestdist) nearestdist = hit.distance;
            }
        }

        if (!foundvirtualtarget) return false;

        if (checkRealWorldObstaclesBeforeShooting) //Make sure there's not a real-world obstacle in the way of the target. 
        {
            //If there is one, check to make sure there's not a real-world object in the way. 
            Vector3 collisionpoint; //Not used but required for HitTestOnRay function.
            foreach (ZEDManager manager in ZEDManager.GetInstances())
            {
                bool hitreal = ZEDSupportFunctions.HitTestOnRay(manager.zedCamera, manager.GetLeftCamera(), shootAnchorObject.transform.position, shootAnchorObject.transform.rotation,
                    nearestdist, 0.01f, out collisionpoint, false, 0.1f);

                if (hitreal) return false;
            }

            return true;
        }
        else return true; //We're not checking against the real world, and we already found a virtual object, so fire. 
    }

    /// <summary>
    /// Instantiate a projectilePrefab at the anchor point. 
    /// </summary>
    private void SpawnProjectile()
    {
        //SHOOT STUFF.
        if (!projectilePrefab) return;


        GameObject lasergo = Instantiate(projectilePrefab, shootAnchorObject.transform.position, shootAnchorObject.transform.rotation);
        //lasergo.transform.localScale = transform.lossyScale;
        lasergo.name = gameObject.name + "'s Laser";

        ArUcoDroneLaser laser = lasergo.GetComponentInChildren<ArUcoDroneLaser>();
        if(!laser)
        {
            Debug.LogError("projectilePrefab assigned to " + gameObject.name + " does not have an ArUcoDroneLaser component.");
            Destroy(lasergo);
            return;
        }

        laser.spawningDrone = this;

        if(audioSource && laserShootSound)
        {
            audioSource.PlayOneShot(laserShootSound);
        }

    }

    /// <summary>
    /// Spawns a projectile and advances the shootTimer until the cooldown period is over - then resets it, allowing another shot.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Shoot()
    {
        SpawnProjectile();

        while(shootTimer < shootCooldownSeconds)
        {
            shootTimer += Time.deltaTime;
            yield return null;
        }

        shootTimer = 0f;
    }

    /// <summary>
    /// Reduces the drone's health, and destroys it if it goes below zero. 
    /// Called by ArUcoDroneLaser when it hits this drone's collider.
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        //print("Took " + damage + " damage. Now at " + currentHealth + " HP.");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Spawns an explosion and destroys this object.
    /// </summary>
    public void Die()
    {
        print(gameObject.name + " destroyed.");

        if(explosionPrefab)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
