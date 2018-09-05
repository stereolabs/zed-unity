using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves around the placeholder object in the ZED place detection demo. 
/// Also spawns the real object if BunnyPlacement.cs reports the placeholder is in a valid position. 
/// </summary>
public class BunnySpawner : MonoBehaviour
{
    /// <summary>
    /// Prefab of the Bunny object that we're going to spawn.
    /// </summary>
    [Tooltip("Prefab of the Bunny object that we're going to spawn.")]
    public GameObject bunnyPrefab;

    /// <summary>
    /// Prefab of the pointer/placeholder that indicates if we can place the object there or not.
    /// </summary>
    [Tooltip("Prefab of the pointer/placeholder that indicates if we can place the object there or not.")]
    public GameObject pointerPrefab;

    /// <summary>
    /// Prefab of the flag UI that spawns when the Bunny collides with anything.
    /// </summary>
    [Tooltip("Prefab of the flag UI that spawns when the Bunny collides with anything.")]
    public GameObject uiPrefab;

    /// <summary>
    /// The Material used on the placeHolder of the pointer prefab. Reference used to change colors based on placement validity.
    /// </summary>
    [Tooltip("The Material used on the placeholder of the pointer prefab. Reference used to change colors based on placement validity.")]
    public Material[] placeHolderMat;

    /// <summary>
    /// The origin position for the pointer, from which you aim the ray to check for a valid placement location. 
    /// If null (such as in the non-VR sample) it takes this script's GameObject position.
    /// </summary>
    [Tooltip("The origin position for the pointer, from which you aim the ray to check for a valid placement location. " + 
        "If null (such as in the non-VR sample) it takes this script's GameObject position.")]
    public Transform rayOrigin;

    /// <summary>
    /// Whether or not we can spawn multiple bunnies at once.
    /// </summary>
    [Tooltip("Whether or not we can spawn multiple bunnies at once.")]
    public bool canSpawnMultipleBunnies = false;

    /// <summary>
    /// Whether or not we can display the placeHolder bunny.
    /// </summary>
    [HideInInspector]
    public bool canDisplayPlaceholder;

    /// <summary>
    /// The last UI spawned for a Bunny collision.
    /// </summary>
    private GameObject currentui;

    /// <summary>
    /// The last Bunny gameObject spawned in the scene.
    /// </summary>
    [HideInInspector] 
    public GameObject currentBunny;

    /// <summary>
    /// Reference to the object that holds the laser as an anchor.
    /// </summary>
    private GameObject pointer;

    /// <summary>
    /// Reference to the object that will be placed at the end of the laser.
    /// </summary>
    private GameObject placeholder;

    /// <summary>
    /// Reference to the ZED's left camera gameobject.
    /// </summary>
    private Camera leftcamera;

    /// <summary>
    /// The text componenet of the current UI gameObject for displaying the score (Distance) of how far the Bunny was sent.
    /// </summary>
    private Text distancetext;

    /// <summary>
    /// The gameObject that holds the 3D Model of the Baseball Bat, so we can Show/Hide it.
    /// </summary>
    [HideInInspector] 
    public GameObject baseballBat;

    void Awake ()
    {
        //Find the left camera object if we didn't assign it at start. 
        if (!leftcamera)
        {
            leftcamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }

        //Check if there is a Object Tracker on this object for VR controls. 
        var tracker = GetComponent<ZEDControllerTracker>();
        if(tracker != null)
        {
            //Get the parent object of the baseball bat.
            if (transform.childCount > 1)
            {
                baseballBat = transform.GetChild(1).gameObject;
                baseballBat.SetActive(false); //Hide it by default. It'll get revealed once we place a bunny. 
            }
        }
        //Instantiate the pointer prefab and assign it to our variables.
        if (pointerPrefab != null)
        {
            pointer = Instantiate(pointerPrefab) as GameObject; //Get the Anchor/root of the pointerBead.
            placeholder = pointer.transform.GetChild(0).gameObject; //Get the laser's pointerBead.
        }
        //If we didn't set a transform for the pointer's origin position...
        if (rayOrigin == null)
        {
            rayOrigin = transform; //...then take our local position.
        }

        //Set the PlaneManager's reference to our placeholder.
        GetComponent<BunnyPlacement>().SetPlaceholder(placeholder.transform);
    }

    /// <summary>
    /// This function is called every fixed framerate frame
    /// Here we take care of enabling & disabling the laser pointer by looking for collisions with the real world.
    /// </summary>
    void FixedUpdate ()
    {
        //Do we have a Pointer Bead to position in the world?
		if (pointer != null && GetComponent<BunnyPlacement>().button != BunnyPlacement.state.Idle)
        {
            Vector3 pointerposition;
            //Point the bead at the closest thing in front of the camera. 
            if (ZEDManager.Instance.IsZEDReady && FindPointerPosition(out pointerposition) && canDisplayPlaceholder)
            {
                //We hit something. Make sure the bead is active.
                pointer.SetActive(true);
                //Position the bead a the collision point, and make it face you. 
                pointer.transform.position = pointerposition;
                Quaternion rot = Quaternion.LookRotation(leftcamera.transform.position - pointer.transform.position);
                pointer.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);
            }
            else
            {
                //We didn't hit anything. Disable the bead object. 
                pointer.SetActive(false);
            }
        }
		else
			pointer.SetActive(false);
    }

    /// <summary>
    /// Spawning the Bunny prefab.
    /// </summary>
    /// <param name="spawnPos">Where we'll spawn the bunny.</param>
    public void SpawnBunny(Vector3 spawnPos)
    {
        //Instantiating the prefab.
        GameObject newBunny = Instantiate(bunnyPrefab, spawnPos, Quaternion.identity, null) as GameObject;
        //Make the UI to face the camera only on the Y axis.
        Quaternion rot = Quaternion.LookRotation(leftcamera.transform.position - spawnPos);
        newBunny.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);
        //Set this script as the BunnySpawner of the instantiated Bunny.
        newBunny.GetComponent<Bunny>().SetMySpawner(this);
        //Assigning it to the currentBunny variable.
        currentBunny = newBunny;
        //Start the coroutine that will enable/show the baseball bat.
        StartCoroutine(EnableBat());
    }

    /// <summary>
    /// Waits for X seconds (to let the bunny fall into place) before activating the bat object.
    /// </summary>
    /// <returns></returns>
    IEnumerator EnableBat()
    {
        //Wait for X seconds...
        yield return new WaitForSeconds(1f);

        //...then enable/show the baseball bat. 
        if (!canDisplayPlaceholder && baseballBat != null)
        {
            baseballBat.SetActive(true);  //It's wabbit season. 
        }
    }

    /// <summary>
    /// Instantiating the flag UI prefab.
    /// </summary>
    /// <param name="spawnPos"></param>
    public void SpawnUI(Vector3 spawnPos)
    {
        //Hide the baseball bat.
        if(baseballBat != null)
        baseballBat.SetActive(false);

        //Destroy the last UI that was spawned, if any.
        if (currentui != null)
        {
            Destroy(currentui);
            currentui = null;
        }

        //Instantiate a new UI GameObject and position it. 
        GameObject newUI = Instantiate(uiPrefab, null);
        newUI.transform.position = spawnPos;

        //Make the UI to face the camera only on the Y axis.
        Quaternion rot = Quaternion.LookRotation(leftcamera.transform.position - spawnPos);
        newUI.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);

        //Configure the flag UI to be spawned. 
        currentui = newUI;
        distancetext = currentui.GetComponentInChildren<Text>();

        //Update the distanceText value to the current Distance between the first/spawned position of the Bunny, and his current one.
        distancetext.text = Vector3.Distance(currentBunny.GetComponent<Bunny>().InitialPosition, spawnPos).ToString("F2") + " Meters";

        //If the UI position is higher than ours, and for a minimum of 1.5 meters, then rotate it 180 on the X axis so it's turned upside down.
        if (currentui.transform.position.y > leftcamera.transform.position.y && currentui.transform.position.y > 1.5f)
        {
            currentui.transform.eulerAngles = new Vector3(180f, currentui.transform.rotation.eulerAngles.y, 0f);
            distancetext.transform.localEulerAngles = new Vector3(180f, 0f, 0f);
        }
    }

    /// <summary>
    /// Tests the depth of the real world based on the pointer origin position and rotation.
    /// Returns the world position if it collided with anything.
    /// </summary>
    /// <param name="pointerbeadpoint">The world space position where the pointer is pointing.</param>
    /// <returns>True if a valid real world point was found.</returns>
    bool FindPointerPosition(out Vector3 pointerbeadpoint)
    {
        //Find the distance to the real world. The bool will be false if there is an error reading the depth at the center of the screen. 
        Vector3 realpoint;
       bool foundrealdistance = ZEDSupportFunctions.HitTestOnRay(leftcamera, rayOrigin.position, rayOrigin.rotation, 5.0f, 0.05f, out realpoint);
        
        //If we didn't find, return false so the laser and bead can be disabled. 
        if (!foundrealdistance)
        {
            pointerbeadpoint = Vector3.zero;
            return false;
        }
        else //Output the world position of the collision.
        {
            pointerbeadpoint = realpoint;
            return true;
        }


    }
}
