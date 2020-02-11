using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour, ILaserable
{
    /// <summary>
    /// The Drone's Health Points.
    /// </summary>
	[Tooltip("The Drone's Health Points.")]
	public int Hitpoints = 100;

    /// <summary>
    /// How long between each laser shot. 
    /// </summary>
	[Tooltip("How long between each laser shot.e")]
	public float SecondsBetweenLaserShots = 4f;

    /// <summary>
    /// Accuracy of each shot. The laser will aim at a random point in a sphere around the user. This value sets that sphere's radius. 
    /// </summary>
	[Tooltip("Accuracy of each shot. The laser will aim at a random point in a sphere around the user. This value sets that sphere's radius.")]
	public float laserAccuracy = 0.5f;

    /// <summary>
    /// The object spawned when the drone shoots.
    /// </summary>
	[Tooltip("The object spawned when the drone shoots.")]
	public GameObject LaserPrefab;

    /// <summary>
    /// The object that gets spawned when the drone dies. Intended to be an explosion.
    /// </summary>
	[Tooltip("The object that gets spawned when the drone dies. Intended to be an explosion.")]
	public GameObject ExplosionPrefab;

    /// <summary>
    /// How long it takes the drone to move to a new position.
    /// </summary>
	[Tooltip("How long it takes the drone to move to a new position.")]
	public float smoothTime = 0.75f;

    /// <summary>
    /// How far a potential movement point must be from real geometry to be considered valid . 
    /// Set to higher values if the drone is moving into walls or other objects. 
    /// </summary>
	[Tooltip("How far a potential movement point must be from real geometry to be considered valid. " + 
        "Set to higher values if the drone is moving alongside walls or other objects. ")]
	public float ClearRadius = 2f;

    /// <summary>
    /// How many times we check near a potential movement point to see if there are any obstacles around it.
    /// Higher values make it less likely a drone will move inside an object, but may cause noticeable stutter.
    /// </summary>
	[Tooltip("How many times we check near a potential movement point to see if there are any obstacles around it. " +
        "Higher values make it less likely a drone will move inside an object, but may cause noticeable stutter.")]
    public int radiusCheckRate = 100;

    /// <summary>
    /// The maximum amount of collisions detected near a movement point allowed.
    /// Higher values make it less likely for a drone to move inside an object, but too high and it may not move at all. 
    /// </summary>
	[Tooltip("The maximum amount of collisions detected near a movement point allowed. " +
        "Higher values make it less likely for a drone to move inside an object, but too high and it may not move at all.")]
    public float percentageThreshold = 0.35f;

    /// <summary>
    /// Maximum angle between drone and its target to have before the drone will turn to face it.
    /// </summary>
	[Tooltip("Maximum angle between drone and its target to have before the drone will turn to face it.")]
	public float angleBeforeRotate = 20f;

    /// <summary>
    /// "AudioClips to play for the Drone. Element 0 is for its laser, 1 is for its destruction."
    /// </summary>
    [Tooltip("AudioClips to play for the Drone. Element 0 is for its laser, 1 is for its destruction.")]
    public AudioClip[] clips;

    /// <summary>
    /// Counts down the time between shots.
    /// </summary>
	private float lasershottimer; 

    /// <summary>
    /// What the drone is looking/shooting at.
    /// </summary>
	private Transform target; 

    /// <summary>
    /// The gun's target to rotate towards.
    /// </summary>
    private Vector3 guntarget; 

    /// <summary>
    /// Reference to the audio source.
    /// </summary>
    private AudioSource audiosource; 

	/// <summary>
	/// Main ZEDManager to use for determining where the drone spawns and what it attacks. 
    /// If left empty, will choose the first available ZEDManager in the scene. 
	/// </summary>
    [Tooltip("Main ZEDManager to use for determining where the drone spawns and what it attacks. " +
        "If left empty, will choose the first available ZEDManager in the scene. ")]
	public ZEDManager zedManager = null;

    /// <summary>
    /// The Mesh Renderer of the drone, so we can modify its material when it takes damage.
    /// </summary>
	private Renderer meshrenderer; 

    /// <summary>
    /// FX for when we take damage.
    /// </summary>
	private Transform damagefx; 

    /// <summary>
    /// Where we shoot the laser from.
    /// </summary>
	private Transform laseranchor; 

    /// <summary>
    /// FX for when we shoot.
    /// </summary>
	private ParticleSystem muzzleflashfx; 

    /// <summary>
    /// The bone that moves the drone's gun.
    /// </summary>
	private Transform dronearm; 

    /// <summary>
    /// The next position that the drone needs to move to. Set only after a valid move location has been confirmed. 
    /// </summary>
	private Vector3 nextposition; 

    /// <summary>
    /// A reference for the SmoothDamp of the drone's movement.
    /// </summary>
	private Vector3 velocity = Vector3.zero; 

    /// <summary>
    /// The light to be enabled briefly when the drone has fired.
    /// </summary>
    private Light gunlight; 

    /// <summary>
    /// Whether or not we can rotate towards our target.
    /// </summary>
    private bool canrotate = false; 

    /// <summary>
    /// Are we moving the drone or not.
    /// </summary>
	private bool canchangerotation = false; 

    /// <summary>
    /// Link with the object that spawned this instance. Used to clear the spawner's reference in OnDestroy(). 
    /// </summary>
	private DroneSpawner spawner; 

    private float damageFlashAmount
    {
#if !ZED_LWRP && !ZED_HDRP
        get
        {
            return meshrenderer.material.GetFloat("_Blend");
        }
        set
        {
            meshrenderer.material.SetFloat("_Blend", value);
        }
#elif ZED_HDRP
        get
        {
            return meshrenderer.material.GetColor("_UnlitColor").a;
        }
        set
        {
            Color newcol = meshrenderer.material.GetColor("_UnlitColor");
            newcol.a = value;
            meshrenderer.material.SetColor("_UnlitColor", newcol);
        }
#elif ZED_LWRP
        get
        {
            return meshrenderer.material.GetColor("_BaseColor").a;
        }
        set
        {
            Color newcol = meshrenderer.material.GetColor("_BaseColor");
            newcol.a = value;
            meshrenderer.material.SetColor("_BaseColor", newcol);
        }
#endif
}


    // Use this for initialization
    void Start ()
    {
		//Cache the ZED's left camera for occlusion testing purposes
		if (zedManager==null)
			zedManager = FindObjectOfType<ZEDManager>();

		// Set the default position of the Drone to the one he spawned at.
		nextposition = transform.position;

        //Set the countdown timer to fire a laser
        lasershottimer = SecondsBetweenLaserShots;

        // Set the audio source.
        audiosource = GetComponent<AudioSource>();
        if(audiosource != null && clips.Length > 2)
        {
            audiosource.clip = clips[2];
            audiosource.volume = 1f;
            audiosource.Play();
        }

        // If these variables aren't set, look for their objects by their default name. 
        Transform[] children = transform.GetComponentsInChildren<Transform>();
		foreach (var child in children) {
            if (child.name == "Drone_Mesh")
                meshrenderer = child.GetComponent<Renderer>();
            else if (child.name == "MuzzleFlash_FX")
                muzzleflashfx = child.GetComponent<ParticleSystem>();
            else if (child.name == "Damage_FX")
                damagefx = child;
            else if (child.name == "Laser_Anchor")
                laseranchor = child;
            else if (child.name == "Gun_Arm")
                dronearm = child;
            else if (child.name == "Point_Light")
                gunlight = child.GetComponent<Light>();
		}

        //If the _target isn't set, set it to the PlayerDamageReceiver, assuming there is one in the scene. 
        if(!target)
        {
			target = FindObjectOfType<PlayerDamageReceiver>().transform;
            guntarget = target.position;
        } 
	}
	
	void Update ()
    {
        //If we've flashed the damage material, lower the blend amount. 
        if (damageFlashAmount > 0)
        {
            float tmp = damageFlashAmount;
            tmp -= Time.deltaTime / 1.5f;

            if (tmp < 0)
                tmp = 0;

            damageFlashAmount = tmp;
        }

		//Enabling damage FX based on HitPoints left.
		switch (Hitpoints) { 
		case 80:
			damagefx.GetChild (0).gameObject.SetActive (true);
			break;
		case 50:
			damagefx.GetChild (1).gameObject.SetActive (true);
			break;
		case 20:
			damagefx.GetChild (2).gameObject.SetActive (true);
			break;
		}

		//If its time to can change our position...
		if (canchangerotation)
        {
			//...then look for a new one until it's a valid location.
			if (FindNewMovePosition (out nextposition)) 
			{
				canchangerotation = false;
			}
        }

		//Count down the laser shot timer. If zero, fire and reset it. 
		lasershottimer -= Time.deltaTime;
		if (lasershottimer <= 0f && target != null)
		{
            //Apply a degree of accuracy based on the drone distance from the player
            //Take a random point on a radius around the Target's position. That radius becomes smaller as the target is closer to us. 
            Vector3 randompoint = UnityEngine.Random.onUnitSphere * (laserAccuracy * (Vector3.Distance(target.position, transform.position) / (spawner.maxSpawnDistance / 2))) + target.position;
            //Check if the chosen point is closer to the edge of the camera. We dont want any projectile coming straight in the players eyes.
            if (randompoint.z >= target.position.z + 0.15f || randompoint.z <= target.position.z - 0.15f)
            {
                guntarget = randompoint;
                //Firing the laser
                FireLaser(randompoint);
                //Reseting the timer.
                lasershottimer = SecondsBetweenLaserShots;
            }
		}

		//Drone Movement & Rotation
		if (target != null)
        {
			//Get the direction to the target.
			Vector3 targetDir = target.position - transform.position;

			//Get the angle between the drone and the target.
			float angle = Vector3.Angle(targetDir, transform.forward);

			//Turn the drone to face the target if the angle between them if greater than...
			if (angle > angleBeforeRotate && canrotate == false)
            {
				canrotate = true;
			}
			if (canrotate == true) {
				var newRot = Quaternion.LookRotation (target.transform.position - transform.position);
				transform.rotation = Quaternion.Lerp (transform.rotation, newRot, Time.deltaTime * 2f);
				if (angle < 5 && canrotate == true) 
				{
					canrotate = false;
				}
			}

			//Rotate the drone's gun to always face the target.
			dronearm.rotation = Quaternion.LookRotation (guntarget - dronearm.position);
		}

		//Simply moving nextposition to something other than transform.position will cause it to move. 
		if (transform.position != nextposition)
        {
			transform.position = Vector3.SmoothDamp(transform.position, nextposition, ref velocity, smoothTime);
        }
    }
		
	/// <summary>
	/// Fires the laser.
	/// </summary>
    private void FireLaser(Vector3 randompoint)
    {
        if (audiosource.clip != clips[0])
        {
            audiosource.clip = clips[0];
            audiosource.volume = 0.2f;
        }
        //Creat a laser object
        GameObject laser = Instantiate(LaserPrefab);
		laser.transform.position = laseranchor.transform.position;
		laser.transform.rotation = Quaternion.LookRotation(randompoint - laseranchor.transform.position);

        //Play the Particle effect.
        muzzleflashfx.Play();

        //Play a sound
        if (audiosource)
        {
            audiosource.Play();
        }
        //MuzzleFlashLight
        StartCoroutine(FireLight());
        //StartRelocatingDrone
        StartCoroutine(RelocationDelay());
    }

    /// <summary>
    /// Forces a delay before moving, and then only allows rotation if the drone has reached its destination. 
    /// </summary>
    /// <returns></returns>
    IEnumerator RelocationDelay()
    {
        yield return new WaitForSeconds(1f);
        //Allow another relocation if we have already reached the current nextposition.
        if (Vector3.Distance(transform.position, nextposition) <= 0.1f)
        {
            canchangerotation = true;
        }
    }

	/// <summary>
	/// What happens when the drone gets damaged. In the ZED drone demo, Lasershot_Player calls this. 
	/// </summary>
	/// <param name="damage">Damage.</param>
    void ILaserable.TakeDamage(int damage)
    {
        //Remove hitpoints as needed
        Hitpoints -= damage;

        //Blend the materials to make it take damage
        //meshrenderer.material.SetFloat("_Blend", 1);
        damageFlashAmount = 1f;

        //Destroy if it's health is below zero
        if (Hitpoints <= 0)
        {
            //Add time to prevent laser firing while we die.
            lasershottimer = 99f;

            if(spawner) spawner.ClearDrone();
            if (ExplosionPrefab)
            {
                Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
            }

            if (audiosource != null && clips.Length > 1)
            {
                audiosource.Stop();
                audiosource.clip = clips[1];
                audiosource.volume = 1f;
                audiosource.Play();
            }

            StartCoroutine(DestroyDrone());
            return;
        }
    }

    /// <summary>
    /// Plays explosion FX, then destroys the drone. 
    /// </summary>
    /// <returns></returns>
    IEnumerator DestroyDrone()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Checks nearby for valid places for the drone to move. 
    /// Valid places must be in front of the player, and not intersect any objects within a reasonable tolerance.
    /// Use radiusCheckRate and percentageThreshold to tweak what counts as a valid location. 
    /// </summary>
    /// <param name="newpos"></param>
    /// <returns></returns>
    private bool FindNewMovePosition(out Vector3 newpos)
    {
        //We can't move if the ZED isn't initialized. 
		if (!zedManager.IsZEDReady)
        {
            newpos = transform.position;
            return false;
        }

        Vector3 randomPosition;
        // Look Around For a New Position
        //If the Drone is on the screen, search around a smaller radius.
        //Note that we only check the primary ZEDManager because we only want the drone to spawn in front of the player anyway. 
        if (ZEDSupportFunctions.CheckScreenView(transform.position, zedManager.GetMainCamera()))
            randomPosition = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2f, 3f) + transform.position;
        else //if the drone is outside, look around a bigger radius to find a position which is inside the screen.
            randomPosition = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(4f, 5f) + transform.position;

        // Look For Any Collisions Through The ZED
		bool hit = ZEDSupportFunctions.HitTestAtPoint(zedManager.zedCamera, zedManager.GetMainCamera(), randomPosition);

        if (!hit)
        {
            newpos = transform.position;
            return false;
        }

        //If we spawn the drone at that world point, it'll spawn inside a wall. Bring it closer by a distance of ClearRadius. 
        Quaternion directiontoDrone = Quaternion.LookRotation(zedManager.GetMainCameraTransform().position - randomPosition, Vector3.up);
        Vector3 newPosition = randomPosition + directiontoDrone * Vector3.forward * ClearRadius;

        //Check the new position isn't too close from the camera.
        float dist = Vector3.Distance(zedManager.GetMainCamera().transform.position, randomPosition);
        if (dist < 1f)
        {
            newpos = transform.position;
            return false;
        }

        //Also check nearby points in a sphere of radius to make sure the whole drone has a clear space. 
		if (ZEDSupportFunctions.HitTestOnSphere(zedManager.zedCamera, zedManager.GetMainCamera(), newPosition, 1f, radiusCheckRate, percentageThreshold))
        {
            newpos = transform.position;
            return false;
        }

        //Return true if it's made it this far and out the location we chose. 
        newpos = newPosition;
        return true;
    }

    /// <summary>
    /// Sets a reference to the Drone spawner governing its spawning. 
    /// Used to notify the spawner when it's destroyed. 
    /// </summary>
    /// <param name="spawner">Reference to the scene's DroneSpawner component.</param>
	public void SetMySpawner(DroneSpawner spawner)
	{
		this.spawner = spawner;
	}

    /// <summary>
    /// Turns on the drone gun's light briefly to simulate a muzzle flash. Because lasers totally have muzzle flashes. 
    /// </summary>
    /// <returns></returns>
    IEnumerator FireLight()
    {
        gunlight.enabled = true;
        yield return new WaitForSeconds(0.15f);
        gunlight.enabled = false;
    }
}
