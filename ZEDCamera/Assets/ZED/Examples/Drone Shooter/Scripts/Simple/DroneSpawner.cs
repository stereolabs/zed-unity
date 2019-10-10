using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns a given Drone prefab when there is not one already in the scene. 
/// It will only spawn drones in front of the user and when it can find a location where it wouldn't intersect the real world. 
/// It will also not spawn drones if one already exists, or if _canSpawn is set to false, which is the case on start until the warning message finishes displaying). 
/// Used in the ZED Drone Battle example scene. 
/// </summary>
public class DroneSpawner : MonoBehaviour
{
    /// <summary>
    /// The Drone prefab to spawn.
    /// </summary>
	[Tooltip("The Drone prefab to spawn.")]
	public GameObject dronePrefab;

    /// <summary>
    /// The warning message to spawn at the start of the scene. Text is set by WarningDisplay() but the prefab holds the UI elements.
    /// </summary>
    [Tooltip("The warning message to spawn at the start of the scene. Text is set by WarningDisplay() but the prefab holds the UI elements.")]
    public GameObject spawnWarning;

    /// <summary>
    /// How far a point must be from real geometry to be considered a valid spawn location.
    /// </summary>
    [Tooltip("How far a point must be from real geometry to be considered a valid spawn location.")]
	public float clearRadius = 1f;

    /// <summary>
    /// How many times should we check near a potential spawn point to see if there are any obstacles around it.
    /// Higher numbers reduce the chance of spawning partially inside an object but may cause stutters.
    /// </summary>
	[Tooltip("How many times should we check near a potential spawn point to see if there are any obstacles around it. " +
        "Higher numbers reduce the chance of spawning partially inside an object but may cause stutters.")]
    public int radiusCheckRate = 200;

    /// <summary>
    /// The maximum number of collisions detected near a spawn point allowed. Higher values make it less likely for a drone to move inside an object, but too high and it may not move at all.
    /// </summary>
	[Tooltip("The maximum number of collisions detected near a spawn point allowed. " + 
        "Higher values make it less likely for a drone to spawn inside an object, but too high and it may not spawn at all.")]
	public float percentagethreshold = 0.4f;

    /// <summary>
    /// How long we wait (in seconds) after a drone's death before spawning a new one.
    /// </summary>
	[Tooltip("How long we wait (in seconds) after a drone's death before spawning a new one.")]
	public float respawnTimer = 2f;

    /// <summary>
    /// The maximum distance from a player that a drone can be spawned.
    /// </summary>
	[Tooltip("The maximum distance from a player that a drone can be spawned.")]
	public float maxSpawnDistance = 6f;

    /// <summary>
    /// The random position to be used when spawning a new drone.
    /// Assigned only once a valid position has been found. 
    /// </summary>
	private Vector3 newspawnposition;

    /// <summary>
    /// Time counting down before spawning a new drone.
    /// </summary>
	private float respawncountdown;

    /// <summary>
    /// The last spawned drone that is still active.
    /// </summary>
	private Drone currentdrone;

    /// <summary>
    /// Main ZEDManager to use for determining where the drone spawns and what it's looking at when it does. 
    /// If left empty, will choose the first available ZEDManager in the scene. 
    /// </summary>
    [Tooltip("Main ZEDManager to use for determining where the drone spawns and what it's looking at when it does. " +
        "If left empty, will choose the first available ZEDManager in the scene. ")]
    public ZEDManager zedManager = null;
    /// <summary>
    /// Needed for various calls to ZEDSupportFunctions.cs, so it can transform ZED depth info into world space. 
    /// </summary>
	private Camera cam;

    /// <summary>
    /// Whether we already displayed the warning message before spawning the drones.
    /// </summary>
    private bool displayedwarning;

    /// <summary>
    /// Last check before starting to spawn drones, to allow warning message to be displayed.
    /// </summary>
    private bool canspawn;
    
    private bool _readyToSpawn
    {
        get
        {
            if (currentdrone == null && respawncountdown <= 0) return true;
            else return false;
        }
    }

    // Use this for initialization
    void Start()
    {
		zedManager = FindObjectOfType<ZEDManager> ();
		//Set the countdown Timer;
		respawncountdown = respawnTimer;
		//Get the ZED camera
		cam = zedManager.GetMainCamera();
    }

    // Update is called once per frame
    void Update()
    {
        //Reposition the screen in front our the Camera when its ready
		if (zedManager && zedManager.IsZEDReady && !displayedwarning)
        {
            StartCoroutine(WarningDisplay());
            displayedwarning = true;
        }

        if (!canspawn)
            return;

        //Tick down the respawn timer if applicable
        if (respawncountdown > 0)
        {
            respawncountdown -= Time.deltaTime;
        }

        if (_readyToSpawn) //We've got no drone and the respawn timer has elapsed
        {
            //Try to spawn a drone in a random place in front of the player. We'll do this only once per frame to avoid stuttering.
			if (CheckRandomSpawnLocation(out newspawnposition))
            {
				currentdrone = SpawnDrone(newspawnposition);
            }
        }

    }

    Text msg;
    /// <summary>
    /// Positions, configures and displays the warning text at the start of the game.
    /// Sets canspawn (which defaults to false) to true once finished, so that drones only spawn afterward. 
    /// </summary>
    /// <returns></returns>
    IEnumerator WarningDisplay() 
    {
        GameObject warningMsg = Instantiate(spawnWarning); //Spawn the message prefab, which doesn't have the correct text yet. 

		if (zedManager != null) {
			warningMsg.transform.position = zedManager.OriginPosition + zedManager.OriginRotation * (Vector3.forward * 2);
			Quaternion newRot = Quaternion.LookRotation (zedManager.OriginPosition - warningMsg.transform.position, Vector3.up);
			warningMsg.transform.eulerAngles = new Vector3 (0, newRot.eulerAngles.y + 180, 0);
		}

        Text msg = warningMsg.GetComponentInChildren<Text>(); //Find the text in the prefab. 

        yield return new WaitForSeconds(1f);

        //Add the letters to the message one at a time for effect. 
        int i = 0;
        string oldText = "WARNING!  DEPLOYING  DRONES!";
        string newText = "";
        while (i < oldText.Length) 
        {
            newText += oldText[i++];
            yield return new WaitForSeconds(0.15F); 
            msg.text = newText;
        }

        yield return new WaitForSeconds(3f); //Let the user read it for a few seconds. 

        //Change the warning message by clearing it and adding letters one at a time like before. 
        i = 0;
        oldText = "DEFEND  YOURSELF!";
        newText = "";
        while (i < oldText.Length)
        {
            newText += oldText[i++];
            yield return new WaitForSeconds(0.1F);
            msg.text = newText;
        }

        yield return new WaitForSeconds(3f);//Let the user read it for a few seconds. 

        Destroy(warningMsg);
        canspawn = true; //Drones can now spawn. 
    }

	/// <summary>
	/// Looks for a random point in a radius around itself. 
	/// Upon collision, the point is moved slightly towards the camera and if its too far it's set to "maxSpawnDistance".
	/// A more thorough search is then done for any other obstacles around it, also, in a radius.
	/// If the number of collision doesn't exeeds the set threshold, then return true and output the new position.
	/// </summary>
	/// <returns><c>true</c>, if random spawn location was checked, <c>false</c> otherwise.</returns>
	/// <param name="newRandomPos">Random position.</param>
	private bool CheckRandomSpawnLocation(out Vector3 newRandomPos)
    {
		//We can't do anything if the ZED isn't initialized. 
		if(!zedManager.IsZEDReady)
		{
			newRandomPos = Vector3.zero;
			return false;
		}

        //Pick a screen position at random between 0.25 and 0.75. 
		Vector2 randomScreenPoint = new Vector2(Random.Range(0.25f, 0.75f) * Screen.width, Random.Range(0.25f, 0.75f) * Screen.height);

        //Get the world position of that position in the real world
		Vector3 randomWorldPoint;
		bool foundWorldPoint = ZEDSupportFunctions.GetWorldPositionAtPixel(zedManager.zedCamera,randomScreenPoint, cam, out randomWorldPoint);

		if (!foundWorldPoint) //We can't read depth from that point. 
        {
            newRandomPos = Vector3.zero;
            return false;
        }
			
		float firstDistance = Vector3.Distance (cam.transform.position, randomWorldPoint);
		float newClearRadius;

		//Check that the distance isn't too far.
		if (firstDistance > maxSpawnDistance)
			newClearRadius = firstDistance - maxSpawnDistance;
		else
			newClearRadius = clearRadius;

        //If we spawn the drone at that world point, it'll spawn inside a wall. Bring it between you and that wall. 
		Quaternion directionToCamera = Quaternion.LookRotation(cam.transform.position - randomWorldPoint, Vector3.up);
		Vector3 closerWorldPoint = randomWorldPoint + directionToCamera * Vector3.forward * newClearRadius;

        //Check that distance isn't too close
		float secondDistance = Vector3.Distance(cam.transform.position, closerWorldPoint);
		if (secondDistance < 1f)
        {
            newRandomPos = Vector3.zero;
            return false;
        }

		//Also check nearby points in a sphere of radius=ClearRadius to make sure the whole drone has a clear space. 
		if (ZEDSupportFunctions.HitTestOnSphere(zedManager.zedCamera, cam, closerWorldPoint, 1f, radiusCheckRate, percentagethreshold))
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
            Debug.LogError("Drone prefab spawned by DroneSpawner does not contain the Drone.cs component.");
        }

        //Give the drone a reference to this object so it can clear its reference and set the timer properly when it dies
		dronecomponent.SetMySpawner(this);

        //Set the drone's transform values
		dronego.transform.position = newspawnposition; //Assign the random Pos generated in CheckRandomSpawnLocation();
		dronego.transform.rotation = Quaternion.LookRotation(zedManager.GetMainCameraTransform().position - spawnPosition, Vector3.up); //Make it look at the player.

        return dronecomponent;

    }

    /// <summary>
    /// Clears the drone reference, which will cause the script to start trying to spawn a new one again. 
    /// Should be called by the drone itself in its OnDestroy(). 
    /// </summary>
    public void ClearDrone() 
    {
        currentdrone = null;
        respawncountdown = respawnTimer;

        print("Drone destroyed");
    }

}
