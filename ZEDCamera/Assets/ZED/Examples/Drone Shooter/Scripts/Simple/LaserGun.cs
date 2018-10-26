using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fires a laser when the user issues a command. 
/// If there is a ZEDControllerTracker in the same object, and the Oculus Integration or SteamVR plugins are installed, 
/// it'll automatically check them for inputs. 
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LaserGun : MonoBehaviour
{
    /// <summary>
    /// What we spawn when the trigger is pulled.
    /// </summary>
    [Tooltip("What we spawn when the trigger is pulled.")]
    public GameObject laserShotPrefab;

    /// <summary>
    /// Anchor object of the PointerBead, which works as a crosshair.
    /// </summary>
    [Tooltip("Anchor object of the PointerBead, which works as a crosshair.")]
    public GameObject laserPointerBeadHolder;

    /// <summary>
    /// Anchor point where the laser spawns.
    /// </summary>
    [Tooltip("Anchor point where the laser spawns.")]
    public Transform laserSpawnLocation;

    /// <summary>
    /// Reference to the object that will be placed at the end of the laser.
    /// </summary>
    private GameObject pointerbead;

    /// <summary>
    /// Reference to the audio source
    /// </summary>
    private AudioSource audiosource;

    /// <summary>
    /// Reference to the ZED's left camera gameobject. Used to pass to ZEDSupportFunctions.cs so it can transform ZED depth info into world space. 
    /// </summary>
    private Camera leftcamera;

    /// <summary>
    /// The gameobject's controller for tracking and input.
    /// </summary>
    private ZEDControllerTracker objecttracker;

#if ZED_OCULUS
    private int fireCount = 0;
#endif

    IEnumerator Start()
    {
        //Find the left camera object if we didn't assign it at start. 
        if (!leftcamera) 
        {
            leftcamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }

        audiosource = GetComponent<AudioSource>();

        if (laserPointerBeadHolder != null)
        {
            //Get the laser bead from the parent/achor object.
            pointerbead = laserPointerBeadHolder.transform.GetChild(0).gameObject;
            //Disable the laser bead to wait for the ZED to initialize. 
            pointerbead.SetActive(false);
        }

        //Wait for VR Controllers to initilize
        yield return new WaitForSeconds(1f);

        objecttracker = GetComponent<ZEDControllerTracker>();

        if (objecttracker != null)
        {

#if ZED_STEAM_VR
            if (objecttracker.index >= 0)
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
            //this.enabled = false;
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

                        //this.enabled = false;
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
        if (laserPointerBeadHolder != null)
        {
            Vector3 crosshairpoint;
            Vector3 crosshairnormal;
            //Point the bead at the closest thing in front of the camera. 
            if (ZEDManager.Instance.IsZEDReady && FindCrosshairPosition(out crosshairpoint, out crosshairnormal))
            {
                //We hit something. Make sure the bead is active.
                pointerbead.SetActive(true);

                //Position the bead a the collision point, and make it face you. 
                pointerbead.transform.position = crosshairpoint;
                if(crosshairnormal.magnitude > 0.0001f)
                pointerbead.transform.rotation = Quaternion.LookRotation(crosshairnormal);
            }
            else
            {
                //We didn't hit anything. Disable the bead object. 
                pointerbead.SetActive(false);
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

        //Update whether the Touch controllers are active. 
        int children = transform.childCount;
        if (OVRManager.isHmdPresent)
        {
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
            {
                for (int i = 0; i < children; ++i)
                    transform.GetChild(i).gameObject.SetActive(true);

            }
            else
            {
                for (int i = 0; i < children; ++i)
                    transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        //We're controlling the fire Rate.  OVRInput doesn't have a GetDown function for the IndexTrigger. Only an axis output.
        if (objecttracker != null)
        {
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
            {
                if ((int)objecttracker.deviceToTrack == 0)
                {
                    if (fireCount == 0 && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.75f)
                    {
                        buttondown = true;
                        fireCount++;
                    }
                    else if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) < 0.75f)
                        fireCount = 0;

                }
                if ((int)objecttracker.deviceToTrack == 1)
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
        if (objecttracker != null)
        {
            if ((int)objecttracker.deviceToTrack == 0 && objecttracker.index >= 0)
                //if (SteamVR_Controller.Input((int)objecttracker.index).GetHairTriggerDown())
                if(objecttracker.GetVRButtonDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    buttondown = true;
                }
            if ((int)objecttracker.deviceToTrack == 1 && objecttracker.index >= 0)
                //if (SteamVR_Controller.Input((int)objecttracker.index).GetHairTriggerDown())
                if (objecttracker.GetVRButtonDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
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

    /// <summary>
    /// Tests the depth of both the real and virtual in the center of the screen, and returns the world position of the closest one. 
    /// </summary>
    /// <param name="crosshairpoint">Where the crosshair should be rendered.</param>
    /// <param name="collisionnormal">The normal vector of the surface aimed at, for rotating the crosshair accordingly if desired.</param>
    /// <returns>False if there is no valid object, real or virtual, on which to place the crosshair. </returns>
    private bool FindCrosshairPosition(out Vector3 crosshairpoint, out Vector3 collisionnormal)
    {
        //Find the distance to the real world. The bool will be false if there is an error reading the depth at the center of the screen. 
        Vector3 realpoint;
        bool foundrealdistance = ZEDSupportFunctions.HitTestOnRay(leftcamera, laserPointerBeadHolder.transform.position, laserPointerBeadHolder.transform.rotation, 5f, 0.01f, out realpoint);
        float realdistance = Vector3.Distance(laserPointerBeadHolder.transform.position, realpoint);

        //Find the distance to the virtual. The bool will be false if there are no colliders ahead of you. 
        RaycastHit hitinfo;
        bool foundvirtualdistance = Physics.Raycast(laserPointerBeadHolder.transform.position, laserPointerBeadHolder.transform.rotation * Vector3.forward, out hitinfo);

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
            ZEDSupportFunctions.GetNormalAtWorldLocation(realpoint, sl.REFERENCE_FRAME.WORLD, leftcamera, out collisionnormal);
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

    /// <summary>
    /// Spawns the laser prefab at the spawn anchor. 
    /// </summary>
    void Fire()
    {
        //Create the shot and position/rotate it accordingly. 
        GameObject blastershot = Instantiate(laserShotPrefab);
        blastershot.transform.position = laserSpawnLocation != null ? laserSpawnLocation.transform.position : transform.position;
        blastershot.transform.rotation = laserSpawnLocation != null ? laserSpawnLocation.transform.rotation : transform.rotation;
        if (laserPointerBeadHolder != null)
        blastershot.transform.LookAt(pointerbead.transform.position);

        //Play a sound
        if (audiosource)
        {
            audiosource.Play();
        }
    }
}
