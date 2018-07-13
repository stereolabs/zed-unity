using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LaserGun : MonoBehaviour
{
    [Tooltip("What we spawn when the trigger is pulled")]
    public GameObject LaserShotPrefab;
    [Tooltip("Anchor object of the PointerBead")]
    public GameObject LaserPointerBeadHolder;
    [Tooltip("Anchor point to spawn the Laser.")]
    public Transform LaserSpawnLocation;

    private GameObject PointerBead; //Reference to the object that will be placed at the end of the laser.
    private AudioSource _audioSource; //Reference to the audioSource
    private Camera LeftCamera; //Reference to the ZED's left camera gameobject. 

    //The gameobject's controller for tracking & input.
    private ZEDControllerTracker objectTracker;
#if ZED_OCULUS
    private int fireCount = 0;
#endif

    IEnumerator Start()
    {
        //Find the left camera object if we didn't assign it at start. 
        if (!LeftCamera) 
        {
            LeftCamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }

        _audioSource = GetComponent<AudioSource>();

        if (LaserPointerBeadHolder != null)
        {
            //Get the laser bead from the parent/achor object.
            PointerBead = LaserPointerBeadHolder.transform.GetChild(0).gameObject;
            //Disable the laser bead to wait for the ZED to initialize. 
            PointerBead.SetActive(false);
        }

        //Wait for VR Controllers to initilize
        yield return new WaitForSeconds(1f);

        objectTracker = GetComponent<ZEDControllerTracker>();

        if (objectTracker != null)
        {

#if ZED_STEAM_VR
            if (objectTracker.index >= 0)
                yield break;
#endif
#if ZED_OCULUS
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
                yield break;
#endif
            // If it got here then there's no VR Controller connected
            int children = transform.childCount;
            for (int i = 0; i < children; ++i)
                transform.GetChild(i).gameObject.SetActive(false);
            this.enabled = false;
        }
        else
        {
            //If its not attached to an object tracker
            var otherObjectTracker = FindObjectsOfType<ZEDControllerTracker>();

            if(otherObjectTracker != null)
            {
                int children = transform.childCount;
#if ZED_STEAM_VR
                foreach (ZEDControllerTracker trackers in otherObjectTracker)
                {
                    if (trackers.index >= 0)
                    {
                        for (int i = 0; i < children; ++i)
                        transform.GetChild(i).gameObject.SetActive(false);

                        this.enabled = false;
                        yield break;
                    }
                }
#endif
#if ZED_OCULUS
                if (OVRManager.isHmdPresent)
                {
                    if (OVRInput.GetConnectedControllers().ToString() == "Touch")
                    {
                        for (int i = 0; i < children; ++i)
                            transform.GetChild(i).gameObject.SetActive(false);

                        this.enabled = false;
                        yield break;
                    }
                }
#endif
            }
        }
    }

    // Update is called once per frame
    void Update ()
    {
        //Do we have a Pointer Bead to position in the world?
        if (LaserPointerBeadHolder != null)
        {
            Vector3 crosshairpoint;
            Vector3 crosshairnormal;
            //Point the bead at the closest thing in front of the camera. 
            if (ZEDManager.Instance.IsZEDReady && FindCrosshairPosition(out crosshairpoint, out crosshairnormal))
            {
                //We hit something. Make sure the bead is active.
                PointerBead.SetActive(true);

                //Position the bead a the collision point, and make it face you. 
                PointerBead.transform.position = crosshairpoint;
                if(crosshairnormal.magnitude > 0.0001f)
                PointerBead.transform.rotation = Quaternion.LookRotation(crosshairnormal);
            }
            else
            {
                //We didn't hit anything. Disable the bead object. 
                PointerBead.SetActive(false);
            }
        }

        //Check to see if any valid fire keys are triggered. 
        //This is more complex than often necessary so as to work out-of-the-box for a variety of configurations. 
        //If using/extending this script for your own project, it's recommended to use Input.GetButtonDown() with a custom Input, use Unity.XR, or interface with a VR SDK. 
        bool buttondown = false;

        //Check for keys present on all systems 
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Space))
        {
            buttondown = true;
        }

#if ZED_OCULUS

        //We're controlling the fire Rate  OVRInput doesn't have a GetDown function for the IndexTrigger. Only an axis output.

        if (objectTracker != null)
        {
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
            {
                if ((int)objectTracker.deviceToTrack == 0)
                {
                    if (fireCount == 0 && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.75f)
                    {
                        buttondown = true;
                        fireCount++;
                    }
                    else if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) < 0.75f)
                        fireCount = 0;

                }
                if ((int)objectTracker.deviceToTrack == 1)
                {
                    if (fireCount == 0 && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) > 0.75f)
                    {
                        buttondown = true;
                        fireCount++;
                    }
                    else if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) < 0.75f)
                        fireCount = 0;
                }
            }
            OVRInput.Update();
        }
#endif
#if ZED_STEAM_VR
        //Looks for any input from this controller through SteamVR
        if (objectTracker != null)
        {
            if ((int)objectTracker.deviceToTrack == 0 && objectTracker.index >= 0)
                if (SteamVR_Controller.Input((int)objectTracker.index).GetHairTriggerDown())
                {
                    buttondown = true;
                }
            if ((int)objectTracker.deviceToTrack == 1 && objectTracker.index >= 0)
                if (SteamVR_Controller.Input((int)objectTracker.index).GetHairTriggerDown())
                {
                    buttondown = true;
                }
        }
#endif

        if (buttondown)
        {
            Fire();
        }
    }

    bool FindCrosshairPosition(out Vector3 crosshairpoint, out Vector3 collisionnormal)
    {
        //Tests the depth of both the real and virtual in the center of the screen, and returns the world position of the closest one. 

        //Find the distance to the real world. The bool will be false if there is an error reading the depth at the center of the screen. 
        Vector3 realpoint;
        bool foundrealdistance = ZEDSupportFunctions.HitTestOnRay(LeftCamera, LaserPointerBeadHolder.transform.position, LaserPointerBeadHolder.transform.rotation, 5f, 0.01f, out realpoint);
        float realdistance = Vector3.Distance(LaserPointerBeadHolder.transform.position, realpoint);

        //Find the distance to the virtual. The bool will be false if there are no colliders ahead of you. 
        RaycastHit hitinfo;
        bool foundvirtualdistance = Physics.Raycast(LaserPointerBeadHolder.transform.position, LaserPointerBeadHolder.transform.rotation * Vector3.forward, out hitinfo);

        //If we didn't find either, return false so the laser and bead can be disabled. 
        if (!foundrealdistance && !foundvirtualdistance)
        {
            crosshairpoint = Vector3.zero;
            collisionnormal = Vector3.zero;
            return false;
        }

        //Decide if we use the real or virtual distance
        if(!foundvirtualdistance || realdistance < hitinfo.distance)
        {
            //The real world is closer. Give the position of the real world pixel and return true. 
            crosshairpoint = realpoint;
            ZEDSupportFunctions.GetNormalAtWorldLocation(realpoint, sl.REFERENCE_FRAME.WORLD, LeftCamera, out collisionnormal);
            return true;
        }
        else
        {
            //The virtual world is closer, or they're tied. Return the world posiiton where the raycast hit the virtual collider. 
            crosshairpoint = hitinfo.point;
            collisionnormal = hitinfo.normal;
            return true;
        }
        
    }

    void Fire()
    {
        //Create the shot and position/rotate it accordingly. 
        GameObject blastershot = Instantiate(LaserShotPrefab);
        blastershot.transform.position = LaserSpawnLocation != null ? LaserSpawnLocation.transform.position : transform.position;
        blastershot.transform.rotation = LaserSpawnLocation != null ? LaserSpawnLocation.transform.rotation : transform.rotation;
        if (LaserPointerBeadHolder != null)
        blastershot.transform.LookAt(PointerBead.transform.position);

        //Play a sound
        if (_audioSource)
        {
            _audioSource.Play();
        }
    }
}
