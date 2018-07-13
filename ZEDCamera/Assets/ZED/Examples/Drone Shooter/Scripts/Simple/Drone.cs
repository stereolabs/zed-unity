using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour, ILaserable
{
	[Tooltip("The Drone's Health Points, synced over the network.")]
	public int Hitpoints = 100;
	[Tooltip("Fire Rate")]
	public float SecondsBetweenLaserShots = 4f;
	[Tooltip("Accuracy of the Shot.")]
	public float laserAccuracy = 0.5f;
	[Tooltip("The object spawned when the drone shoots.")]
	public GameObject LaserPrefab;
	[Tooltip("The object that gets spawned when the drone dies.")]
	public GameObject ExplosionPrefab;
	[Tooltip("How smooth is the movement of the drone.")]
	public float smoothTime = 0.75f;
	[Tooltip("How far a point must be from real geometry to be considered a valid spawn location.")]
	public float ClearRadius = 2f;
	[Tooltip("How many times should we look around our chosen spawnPoint to see if there are any obstacles around it.")]
	public int radiusCheckRate = 100;
	[Tooltip("The maximum amount of collisions detected near a spawn point allowed.")]
	public float percentageThreshold = 0.35f;
	[Tooltip("Angle between drone and target to have before turning the drone towards Target.")]
	public float angleBeforeRotate = 20f;
    [Tooltip("AudioClips to play for the Drone: element 0 is for its laser, 1 is for its destruction.")]
    public AudioClip[] clips;

	private float _laserShotTimer; //Counting down time before shooting.
	private Transform _target; // What we are looking/shooting at.
    private Vector3 _gunTarget; // The Gun's target to rotate towards
    private AudioSource _audioSource; //Reference to the audioSource
    private Camera _leftCamera; // The ZED camera
	private Renderer _renderer; // The Mesh Renderer of the Drone, so we can modify it's material
	private Transform _damageFX; // FX for when we take damage
	private Transform _laserAnchor; // Where we shoot the laser from.
	private ParticleSystem _muzzleFlashFx; // FX for when we shoot
	private Transform _droneArm; // The bone that moves the Drone's Gun.
	private Vector3 _nextPosition; // The next position that the drone need to move to.
	private Vector3 _velocity = Vector3.zero; // A reference for the SmoothDamp of the drone's movement.
    private Light _gunLight; //The light to be enabled then disabled when the Drone has fired.
    private bool canRotate = false; // Can we rotate towards our Target.
	private bool canChangeLocation = false; // Are we moving the drone or not.
	private DroneSpawner mySpawner; // Link with the object that spawned us.

    // Use this for initialization
    void Start ()
    {
		//Cache the ZED's left camera for occlusion testing purposes
		_leftCamera = ZEDManager.Instance.GetLeftCameraTransform().GetComponent<Camera>();

		// Set the default position of the Drone to the one he spawned at.
		_nextPosition = transform.position;

        //Set the countdown timer to fire a laser
        _laserShotTimer = SecondsBetweenLaserShots;

        // Set the audio source.
        _audioSource = GetComponent<AudioSource>();
        if(_audioSource != null && clips.Length > 2)
        {
            _audioSource.clip = clips[2];
            _audioSource.volume = 1f;
            _audioSource.Play();
        }

        // If these variables aren't set, look for their objects by their default name. 
        Transform[] children = transform.GetComponentsInChildren<Transform>();
		foreach (var child in children) {
            if (child.name == "Drone_Mesh")
                _renderer = child.GetComponent<Renderer>();
            else if (child.name == "MuzzleFlash_FX")
                _muzzleFlashFx = child.GetComponent<ParticleSystem>();
            else if (child.name == "Damage_FX")
                _damageFX = child;
            else if (child.name == "Laser_Anchor")
                _laserAnchor = child;
            else if (child.name == "Gun_Arm")
                _droneArm = child;
            else if (child.name == "Point_Light")
                _gunLight = child.GetComponent<Light>();
		}

        //If the Target isn't set, set it to the PlayerDamageReceiver, assuming there is one in the scene. 
        if(!_target)
        {
			_target = FindObjectOfType<PlayerDamageReceiver>().transform;
            _gunTarget = _target.position;
        } 
	}
	
	void Update ()
    {
        //If we've flashed the damage material, lower the blend amount. 
        if (_renderer.material.GetFloat("_Blend") > 0)
        {
            float tmp = _renderer.material.GetFloat("_Blend");
            tmp -= Time.deltaTime / 1.5f;

            if (tmp < 0)
                tmp = 0;

            _renderer.material.SetFloat("_Blend", tmp);
        }

		//Enabling damage FX based on HitPoints left.
		switch (Hitpoints) { 
		case 80:
			_damageFX.GetChild (0).gameObject.SetActive (true);
			break;
		case 50:
			_damageFX.GetChild (1).gameObject.SetActive (true);
			break;
		case 20:
			_damageFX.GetChild (2).gameObject.SetActive (true);
			break;
		}

		//If we can change our position...
		if (canChangeLocation)
        {
			//...then look for a new one until its a positive location.
			if (FindNewMovePosition (out _nextPosition)) 
			{
				canChangeLocation = false;
			}
        }

		//Count down the laser shot timer. If zero, fire and reset it. 
		_laserShotTimer -= Time.deltaTime;
		if (_laserShotTimer <= 0f && _target != null)
		{
            //Apply a degree of accuracy based on the drone distance from the player
            //Take a random point on a radius around the Target's position. That radius becomes smaller as the target is closer to us. 
            Vector3 randompoint = UnityEngine.Random.onUnitSphere * (laserAccuracy * (Vector3.Distance(_target.position, transform.position) / (mySpawner.maxSpawnDistance / 2))) + _target.position;
            //Check if the chosen point is closer to the edge of the camera. We dont want any projectile coming straight in the players eyes.
            if (randompoint.z >= _target.position.z + 0.15f || randompoint.z <= _target.position.z - 0.15f)
            {
                _gunTarget = randompoint;
                //Firing the laser
                FireLaser(randompoint);
                //Reseting the timer.
                _laserShotTimer = SecondsBetweenLaserShots;
            }
		}

		//Drone Movement & Rotation
		if (_target != null) {
			//Get the direction to the target.
			Vector3 targetDir = _target.position - transform.position;

			//Get the angle between the drone and the target.
			float angle = Vector3.Angle(targetDir, transform.forward);

			//Turn the drone to face the target if the angle between them if greater than...
			if (angle > angleBeforeRotate && canRotate == false) {
				canRotate = true;
			}
			if (canRotate == true) {
				var newRot = Quaternion.LookRotation (_target.transform.position - transform.position);
				transform.rotation = Quaternion.Lerp (transform.rotation, newRot, Time.deltaTime * 2f);
				if (angle < 5 && canRotate == true) 
				{
					canRotate = false;
				}
			}

			//Rotate the drone's gun to always face the target.
			_droneArm.rotation = Quaternion.LookRotation (_gunTarget - _droneArm.position);
		}

		//Simply moving _nextPosition to something other than transform.position will cause it to move. 
		if (transform.position != _nextPosition)
        {
			transform.position = Vector3.SmoothDamp(transform.position, _nextPosition,ref _velocity, smoothTime);
        }
    }
		
	/// <summary>
	/// Fires the laser.
	/// </summary>
    private void FireLaser(Vector3 randompoint)
    {
        if (_audioSource.clip != clips[0])
        {
            _audioSource.clip = clips[0];
            _audioSource.volume = 0.2f;
        }
        //Creat a laser object
        GameObject laser = Instantiate(LaserPrefab);
		laser.transform.position = _laserAnchor.transform.position;
		laser.transform.rotation = Quaternion.LookRotation(randompoint - _laserAnchor.transform.position);

        //Play the Particle effect.
        _muzzleFlashFx.Play();

        //Play a sound
        if (_audioSource)
        {
            _audioSource.Play();
        }
        //MuzzleFlashLight
        StartCoroutine(FireLight());
        //StartRelocatingDrone
        StartCoroutine(RelocationDelay());
    }

    IEnumerator RelocationDelay()
    {
        yield return new WaitForSeconds(1f);
        //Allow to relocate if we already reached the current nextPos
        if (Vector3.Distance(transform.position, _nextPosition) <= 0.1f)
            canChangeLocation = true;
    }

	/// <summary>
	/// Takes the damage.
	/// </summary>
	/// <param name="damage">Damage.</param>
    void ILaserable.TakeDamage(int damage)
    {
        //Remove hitpoints as needed
        Hitpoints -= damage;

        //Blend the materials to make it take damage
        _renderer.material.SetFloat("_Blend", 1);

        //Destroy if it's health is below zero
        if (Hitpoints <= 0)
        {
            //Add time to prevent laser firing while we die.
            _laserShotTimer = 99f;

            if(mySpawner) mySpawner.ClearDrone();
            if (ExplosionPrefab)
            {
                Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
            }

            if (_audioSource != null && clips.Length > 1)
            {
                _audioSource.Stop();
                _audioSource.clip = clips[1];
                _audioSource.volume = 1f;
                _audioSource.Play();
            }

            StartCoroutine(DestroyDrone());
            return;
        }
    }

    IEnumerator DestroyDrone()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    private bool FindNewMovePosition(out Vector3 newpos)
    {
        //We can't move if the ZED isn't initialized. 
        if (!ZEDManager.Instance.IsZEDReady)
        {
            newpos = transform.position;
            return false;
        }

        Vector3 randomPosition;
        // Look Around For a New Position
        //If the Drone is on the screen, search around a smaller radius.
        if (ZEDSupportFunctions.CheckScreenView(transform.position, _leftCamera))
            randomPosition = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2f, 3f) + transform.position;
        else //if the drone is outside, look around a bigger radius to find a position which is inside the screen.
            randomPosition = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(4f, 5f) + transform.position;

        // Look For Any Collisions Through The ZED
        bool hit = ZEDSupportFunctions.HitTestAtPoint(_leftCamera, randomPosition);

        if (!hit)
        {
            newpos = transform.position;
            return false;
        }

        //If we spawn the drone at that world point, it'll spawn inside a wall. Bring it closer by a distance of ClearRadius. 
        Quaternion directiontoDrone = Quaternion.LookRotation(_leftCamera.transform.position - randomPosition, Vector3.up);
        Vector3 newPosition = randomPosition + directiontoDrone * Vector3.forward * ClearRadius;

        //Check the new position isn't too close from the camera.
        float dist = Vector3.Distance(_leftCamera.transform.position, randomPosition);
        if (dist < 1f)
        {
            newpos = transform.position;
            return false;
        }

        //Also check nearby points in a sphere of radius to make sure the whole drone has a clear space. 
        if (ZEDSupportFunctions.HitTestOnSphere(_leftCamera, newPosition, 1f, radiusCheckRate, percentageThreshold))
        {
            newpos = transform.position;
            return false;
        }

        //Return true if it's made it this far and out the location we chose. 
        newpos = newPosition;
        return true;
    }

	public void SetMySpawner(DroneSpawner spawner)
	{
		mySpawner = spawner;
	}

    IEnumerator FireLight()
    {
        _gunLight.enabled = true;
        yield return new WaitForSeconds(0.15f);
        _gunLight.enabled = false;
    }
}
