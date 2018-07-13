using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneSpawner : MonoBehaviour
{
	[Tooltip("The Drone prefab to spawn.")]
	public GameObject dronePrefab;
    [Tooltip("The warning message to spawn.")]
    public GameObject spawnWarning;
    [Tooltip("How far a point must be from real geometry to be considered a valid spawn location.")]
	public float clearRadius = 1f;
	[Tooltip("How many times should we look around our chosen spawnPoint to see if there are any obstacles around it.")]
	public int radiusCheckRate = 200;
	[Tooltip("The maximum amount of collisions detected near a spawn point allowed.")]
	public float percentagethreshold = 0.4f;
	[Tooltip("How long we wait after a drone's death before spawning a new one")]
	public float respawnTimer = 2f;
	[Tooltip("What is the maximum distance the drone can be spawned.")]
	public float maxSpawnDistance = 6f;

	private Vector3 _randomPosition; //The random position to be used when spawning a new drone.
	private float _respawnCountdown; //Time counting down before spawning a new dorne.
	private Drone _currentDrone; //The last spawned drone that is still active.
	private Camera _leftCamera; //Needed for depth calculation
    private bool _warning; //Did we display the warning message before spawning the Drones.
    private bool _canSpawn; //Last check before starting spawning to allow warning message being displayed.
    
    private bool _readyToSpawn
    {
        get
        {
            if (_currentDrone == null && _respawnCountdown <= 0) return true;
            else return false;
        }
    }

    // Use this for initialization
    void Start()
    {
		//Set the countdown Timer;
		_respawnCountdown = respawnTimer;
		//Get the ZED camera
		_leftCamera = ZEDManager.Instance.GetLeftCameraTransform().GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        //Reposition the screen in front our the Camera when its ready
        if (ZEDManager.Instance.IsZEDReady && !_warning)
        {
            StartCoroutine(WarningDisplay());
            _warning = true;
        }

        if (!_canSpawn)
            return;

        //Tick down the respawn timer if applicable
        if (_respawnCountdown > 0)
        {
            _respawnCountdown -= Time.deltaTime;
        }

        if (_readyToSpawn) //We've got no drone and the respawn timer has elapsed
        {
            //Try to spawn a drone in a random place in front of the player. We'll do once per frame for now. 
			if (CheckRandomSpawnLocation(out _randomPosition))
            {
				_currentDrone = SpawnDrone(_randomPosition);
            }
        }

		//Debug Solution to spawn drone multiple times for testing.
		//On Input Down, destroy current drone.
		if (Input.GetKeyDown (KeyCode.Mouse1)) {
            //Destroys the current Drone.
            Destroy(_currentDrone.gameObject);
			ClearDrone ();
		}
    }

    Text msg;
    IEnumerator WarningDisplay()
    {
        GameObject warningMsg = Instantiate(spawnWarning);

        warningMsg.transform.position = ZEDManager.Instance.OriginPosition + ZEDManager.Instance.OriginRotation * (Vector3.forward*2);
        Quaternion newRot = Quaternion.LookRotation(ZEDManager.Instance.OriginPosition - warningMsg.transform.position, Vector3.up);
        warningMsg.transform.eulerAngles = new Vector3(0, newRot.eulerAngles.y + 180, 0);

        Text msg = warningMsg.GetComponentInChildren<Text>();

        yield return new WaitForSeconds(1f);

        int i = 0;
        string oldText = "WARNING!  DEPLOYING  DRONES!";
        string newText = "";
        while (i < oldText.Length)
        {
            newText += oldText[i++];
            yield return new WaitForSeconds(0.15F);
            msg.text = newText;
        }

        yield return new WaitForSeconds(3f);

        i = 0;
        oldText = "DEFEND  YOURSELF!";
        newText = "";
        while (i < oldText.Length)
        {
            newText += oldText[i++];
            yield return new WaitForSeconds(0.1F);
            msg.text = newText;
        }

        yield return new WaitForSeconds(3f);
        Destroy(warningMsg);
        _canSpawn = true;
    }

	/// <summary>
	/// looks for a random point in a radius around itself. 
	/// Upon collision, the point is moved slightly towards the camera and if its too far it's set to "maxSpawnDistance".
	/// A more thorough search is then done for any other obstacles around it, also, in a radius.
	/// If the number of collision doesn't exeeds the set threshold, then return true and output the new position.
	/// </summary>
	/// <returns><c>true</c>, if random spawn location was checked, <c>false</c> otherwise.</returns>
	/// <param name="newRandomPos">Random position.</param>
	private bool CheckRandomSpawnLocation(out Vector3 newRandomPos)
    {
		//We can't do anything if the ZED isn't initialized. 
		if(!ZEDManager.Instance.IsZEDReady)
		{
			newRandomPos = Vector3.zero;
			return false;
		}

        //Pick a screen position at random between 0.25 and 0.75. 
		Vector2 randomScreenPoint = new Vector2(Random.Range(0.25f, 0.75f) * Screen.width, Random.Range(0.25f, 0.75f) * Screen.height);

        //Get the world position of that position in the real world
		Vector3 randomWorldPoint;
		bool foundWorldPoint = ZEDSupportFunctions.GetWorldPositionAtPixel(randomScreenPoint, _leftCamera, out randomWorldPoint);

		if (!foundWorldPoint) //We can't read depth from that point. 
        {
            newRandomPos = Vector3.zero;
            return false;
        }
			
		float firstDistance = Vector3.Distance (_leftCamera.transform.position, randomWorldPoint);
		float newClearRadius;

		//Check that the distance isn't too far.
		if (firstDistance > maxSpawnDistance)
			newClearRadius = firstDistance - maxSpawnDistance;
		else
			newClearRadius = clearRadius;

        //If we spawn the drone at that world point, it'll spawn inside a wall. Bring it between you and that wall. 
		Quaternion directionToCamera = Quaternion.LookRotation(_leftCamera.transform.position - randomWorldPoint, Vector3.up);
		Vector3 closerWorldPoint = randomWorldPoint + directionToCamera * Vector3.forward * newClearRadius;

        //Check that distance isn't too close
		float secondDistance = Vector3.Distance(_leftCamera.transform.position, closerWorldPoint);
		if (secondDistance < 1f)
        {
            newRandomPos = Vector3.zero;
            return false;
        }

		//Also check nearby points in a sphere of radius=ClearRadius to make sure the whole drone has a clear space. 
		if (ZEDSupportFunctions.HitTestOnSphere(_leftCamera, closerWorldPoint, 1f, radiusCheckRate, percentagethreshold))
        {
            //Not clear. 
            newRandomPos = Vector3.zero;
            return false;
        }
        else
        {
            //Clear. 
			newRandomPos = closerWorldPoint;
            return true; 
        }
    }

	/// <summary>
	/// Spawns the drone.
	/// </summary>
	/// <returns>The drone.</returns>
	/// <param name="spawnPosition">Spawn position.</param>
    public Drone SpawnDrone(Vector3 spawnPosition)
    {
        //Spawn the drone
        GameObject dronego = Instantiate(dronePrefab);
        Drone dronecomponent = dronego.GetComponentInChildren<Drone>();

        if (!dronecomponent)
        {
            Debug.Log("Drone prefab spawned by DroneSpawner does not contain the Drone.cs component.");
        }

        //Give the drone a reference to this object so it can clear its reference and set the timer properly when it dies
		dronecomponent.SetMySpawner(this);

        //Set the drone's transform values
		dronego.transform.position = _randomPosition; //Assign the random Pos generated in CheckRandomSpawnLocation();
		dronego.transform.rotation = Quaternion.LookRotation(ZEDManager.Instance.GetLeftCameraTransform().position - spawnPosition, Vector3.up); //Make it look at the player.

        return dronecomponent;

    }

    public void ClearDrone() //To be called by a drone when it dies. 
    {
        _currentDrone = null;
        _respawnCountdown = respawnTimer;

        print("Drone destroyed");
    }

}
