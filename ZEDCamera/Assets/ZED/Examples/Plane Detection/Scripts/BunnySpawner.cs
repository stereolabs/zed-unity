using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BunnySpawner : MonoBehaviour {

    [Tooltip("Prefab of the Bunny that we're going to spawn.")]
    public GameObject _bunnyPrefab;
    [Tooltip("Prefab of the pointer which is a placeHolder that indicates if we can place or not a Bunny.")]
    public GameObject _laserPointerPrefab;
    //Reference to the object that holds the laser as an anchor.
    private GameObject _laserPointerBeadHolder;
    //Reference to the object that will be placed at the end of the laser.
    private GameObject _pointerBead;
    //Reference to the ZED's left camera gameobject.
    private Camera _leftCamera;
    [HideInInspector] //Boolean that decides if we can display the placeHolder bunny.
    public bool _canPlaceHolder;
    [Tooltip("Prefab of the UI that spawns when the Bunny collides with anything.")]
    public GameObject _uiPrefab;
    //The last UI spawned for a Bunny collision.
    private GameObject _currentUI;
    [HideInInspector] //The last Bunny gameObject spawned in the scene.
    public GameObject _currentBunny;
    //The text componenet of the current UI gameObject for displaying the score (Distance) of how far the Bunny was sent.
    private Text _distanceText;
    [HideInInspector] //The gameObject that holds the 3D Model of the Baseball Bat, so we can Show/Hide it.
    public GameObject _baseballBat;
    [Tooltip("The Material used on the placeHolder of the pointer Prefab.")]
    public Material[] _placeHolderMat;
    [Tooltip("The origin position for the Laser Pointer. If null, it takes this script's gameObject position.")]
    public Transform _rayOrigin;
    [Tooltip("Allow Multiple Bunnys to be spawn")]
    public bool canSpawnMoreBunnies = false;
    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    void Awake ()
    {
        //Find the left camera object if we didn't assign it at start. 
        if (!_leftCamera)
        {
            _leftCamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }

        //Check if there is a Object Tracker on this object
        var tracker = GetComponent<ZEDControllerTracker>();
        if(tracker != null)
        {
            //Get the parent object of the baseball bat.
            if (transform.childCount > 1)
            {
                _baseballBat = transform.GetChild(1).gameObject;
                //Hide it.
                _baseballBat.SetActive(false);
            }
        }
        //Instantiate the Laser Pointer Prefab and assign it to our variables.
        if (_laserPointerPrefab != null)
        {
            _laserPointerBeadHolder = Instantiate(_laserPointerPrefab) as GameObject; //Get the Anchor/root of the pointerBead.
            _pointerBead = _laserPointerBeadHolder.transform.GetChild(0).gameObject; //Get the laser's pointerBead.
        }
        //If we didn't set a Transform for the laser's origin position...
        if (_rayOrigin == null)
            _rayOrigin = transform; //...then take our local position.

        //Set the PlaneManager's reference to our pointerbead.
        GetComponent<BunnyPlacement>().SetPoinerBead(_pointerBead.transform);
    }

    /// <summary>
    /// This function is called every fixed framerate frame
    /// Here we take care of enabling & disabling the laser pointer by looking for collisions with the real world.
    /// </summary>
    void FixedUpdate ()
    {
        //Do we have a Pointer Bead to position in the world?
		if (_laserPointerBeadHolder != null && GetComponent<BunnyPlacement>().Button != BunnyPlacement.state.Idle)
        {
            Vector3 pointerBeadPoint;
            //Point the bead at the closest thing in front of the camera. 
            if (ZEDManager.Instance.IsZEDReady && FindPointerBeadPosition(out pointerBeadPoint) && _canPlaceHolder)
            {
                //We hit something. Make sure the bead is active.
                _laserPointerBeadHolder.SetActive(true);
                //Position the bead a the collision point, and make it face you. 
                _laserPointerBeadHolder.transform.position = pointerBeadPoint;
                Quaternion rot = Quaternion.LookRotation(_leftCamera.transform.position - _laserPointerBeadHolder.transform.position);
                _laserPointerBeadHolder.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);
            }
            else
            {
                //We didn't hit anything. Disable the bead object. 
                _laserPointerBeadHolder.SetActive(false);
            }
        }
		else
			_laserPointerBeadHolder.SetActive(false);
    }

    /// <summary>
    /// Spawning the Bunny prefab.
    /// </summary>
    /// <param name="spawnPos"></param>
    public void SpawnBunny(Vector3 spawnPos)
    {
        //Instantiating the prefab.
        GameObject newBunny = Instantiate(_bunnyPrefab, spawnPos, Quaternion.identity, null) as GameObject;
        //Make the UI to face the camera only on the Y axis.
        Quaternion rot = Quaternion.LookRotation(_leftCamera.transform.position - spawnPos);
        newBunny.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);
        //Set this script as the BunnySpawner of the instantiated Bunny.
        newBunny.GetComponent<Bunny>().SetMySpawner(this);
        //Assigning it to the currentBunny variable.
        _currentBunny = newBunny;
        //Start the coroutine that will enable/show the baseball bat.
        StartCoroutine(EnableBat());
    }

    /// <summary>
    /// Coroutine that waits for X seconds before doing something.
    /// </summary>
    /// <returns></returns>
    IEnumerator EnableBat()
    {
        //Wait for X seconds...
        yield return new WaitForSeconds(1f);
        //then enable/show the baseball bat.
        if(!_canPlaceHolder && _baseballBat != null)
        _baseballBat.SetActive(true);
    }

    /// <summary>
    /// Instantiating the UI prefab.
    /// </summary>
    /// <param name="spawnPos"></param>
    public void SpawnUI(Vector3 spawnPos)
    {
        //Hide the baseball bat.
        if(_baseballBat != null)
        _baseballBat.SetActive(false);
        //Destroy the last UI that was spawned, if any.
        if (_currentUI != null)
        {
            Destroy(_currentUI);
            _currentUI = null;
        }
        //Instantiate a new UI gameObject.
        GameObject newUI = Instantiate(_uiPrefab, null);
        //Reposition the UI.
        newUI.transform.position = spawnPos;
        //Make the UI to face the camera only on the Y axis.
        Quaternion rot = Quaternion.LookRotation(_leftCamera.transform.position - spawnPos);
        newUI.transform.eulerAngles = new Vector3(0f, rot.eulerAngles.y, 0f);
        //Assigning the UI to the currentUI variable.
        _currentUI = newUI;
        //Assigning the UI's text component to the distanceText variable.
        _distanceText = _currentUI.GetComponentInChildren<Text>();
        //Updating the distanceText value to the current Distance between the first/spawned position of the Bunny, and his current one.
        _distanceText.text = Vector3.Distance(_currentBunny.GetComponent<Bunny>()._savedPos, spawnPos).ToString("F2") + " Meters";
        //If the UI position is higher than ours, and for a minimum of 1.5 meters, then rotate it 180 on the X axis so it's turned upside down.
        if (_currentUI.transform.position.y > _leftCamera.transform.position.y && _currentUI.transform.position.y > 1.5f)
        {
            _currentUI.transform.eulerAngles = new Vector3(180f, _currentUI.transform.rotation.eulerAngles.y, 0f);
            _distanceText.transform.localEulerAngles = new Vector3(180f, 0f, 0f);
        }
    }

    /// <summary>
    /// Tests the depth of the real world based on the laser origin position and rotation.
    /// Returns the world position if it collided with anything.
    /// </summary>
    /// <param name="crosshairpoint"></param>
    /// <returns></returns>
    bool FindPointerBeadPosition(out Vector3 pointerBeadPoint)
    {
        //Find the distance to the real world. The bool will be false if there is an error reading the depth at the center of the screen. 
        Vector3 realpoint;
       bool foundrealdistance = ZEDSupportFunctions.HitTestOnRay(_leftCamera, _rayOrigin.position, _rayOrigin.rotation, 5.0f, 0.05f, out realpoint);
        
        //If we didn't find, return false so the laser and bead can be disabled. 
        if (!foundrealdistance)
        {
            pointerBeadPoint = Vector3.zero;
            return false;
        }
        else //send the world position of the collision.
        {
            pointerBeadPoint = realpoint;
            return true;
        }


    }
}
