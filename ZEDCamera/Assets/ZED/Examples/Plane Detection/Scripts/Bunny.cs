using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonoBehaviour {

    //Reference to the Rigidbody of this object.
    private Rigidbody _rb;
    //Variable to store our initial position when we are enabled/spawned into the scene.
    public Vector3 _savedPos { get; private set; }
    //Boolean that is true whenever this object is moved by the baseball bat.
    public bool _moving { get; private set; }
    //Reference to the ZED's left camera gameobject.
    private Camera _leftCamera;
    //Reference to the BunnySpawner that spawned this gameObject.
    private BunnySpawner _mySpawner;
    //The transform to be used as new center/pivot point for this gameObject when checking for collisions with the real world.
    private Transform _centerPoint;
    //Reference to the ZED Plane Detection Manager.
    private ZEDPlaneDetectionManager _zedPlane;
    //Reference to the Animator.
    [HideInInspector]
    public Animator anim;
    /// <summary>
    /// Use this for initialization.
    /// Setting up the variables, and start up states.
    /// </summary>
    void Start()
    {
        //Find the left camera object if we didn't assign it at start. 
        if (!_leftCamera)
        {
            _leftCamera = ZEDManager.Instance.GetLeftCameraTransform().GetComponent<Camera>();
        }
        //we're not moving at start.
        _moving = false;
        //Get our Rigidbody component.
        _rb = GetComponent<Rigidbody>();
        //Saving our initial position.
        _savedPos = transform.position;
        //Getting the ZEDPlaneDetectionManager.cs component.
        _zedPlane = FindObjectOfType<ZEDPlaneDetectionManager>();
        //If there is a child in position 2, use it as new centerPoint.
        if (transform.GetChild(2) != null)
            _centerPoint = transform.GetChild(2);
        else //use this transform.
            _centerPoint = transform;
        //Get the Animator component.
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Sets the BunnySpawner.
    /// </summary>
    /// <param name="spawner"></param>
    public void SetMySpawner(BunnySpawner spawner)
    {
        _mySpawner = spawner;
    }

    /// <summary>
    /// Starts the HitDelay coroutine.
    /// </summary>
    public void GetHit(bool hit)
    {
        GetComponent<Rigidbody>().drag = 0f;
        GetComponent<Rigidbody>().angularDrag = 0.5f;
        StartCoroutine(HitDelay(hit));
    }

    /// <summary>
    /// Coroutine used to delay the collision detection of the Bunny.
    /// Setting the _moving variable after waiting X seconds.
    /// </summary>
    /// <returns></returns>
    IEnumerator HitDelay(bool hit)
    {
        //Wait for X amount of seconds...
        yield return new WaitForSeconds(0.1f);
        if (hit)
        {
            //... then set _moving to true, and allow collision detection in FixedUpdate().
            _moving = true;
        }
        else
        {
            _rb.isKinematic = true; //Freeze the object at the current position.
            yield return new WaitForSeconds(1f);
            _mySpawner.SpawnUI(transform.position);
        }

        //Clearing the scene from any Planes created by the ZED Plane Detection Manager.
		for (int i = 0; i < _zedPlane.hitPlaneList.Count; i++)
        {
			Destroy(_zedPlane.hitPlaneList[i].gameObject);
			_zedPlane.hitPlaneList.RemoveAt(i);
        }
    }

    /// <summary>
    /// This function is called every fixed framerate frame
    /// Here we take care of enabling & disabling the laser pointer.
    /// </summary>
    private void FixedUpdate()
    {
        //If we have been moved by the baseball bat
        if (_moving)
        {
            //Look for our next position based on our current velocity.
            Vector3 predictedPos = _centerPoint.position + (_rb.velocity * (Time.deltaTime * 2.5f));
            transform.rotation = Quaternion.LookRotation(_rb.velocity.normalized);
            //Collision check with the real world at that next position.
            if (ZEDSupportFunctions.HitTestAtPoint(_leftCamera, predictedPos))
            {
                //We hit something, but is it a flat surface?
                if (_zedPlane.DetectPlaneAtHit(_leftCamera.WorldToScreenPoint(predictedPos)))
                {
                    _mySpawner.SpawnUI(predictedPos);
                    _moving = false;
                }
                else//If not freeze on hit.
                {
                    //_rb.isKinematic = true; //Freeze the object at the current position.
                    _moving = false; //Not moving anymore, so update our state.
                    _mySpawner.SpawnUI(predictedPos); //Start spawning the UI on our current location.
                    _rb.velocity = Vector3.Reflect(_rb.velocity /2 , transform.forward);
                }
            }
        }
    }

}
