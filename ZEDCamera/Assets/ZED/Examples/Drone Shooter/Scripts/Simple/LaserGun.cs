﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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
    /// Reference to the scene's primary ZEDManager component. Used for placing the crosshair.
    /// </summary>
    [Tooltip("Reference to the scene's primary ZEDManager component. Used for placing the crosshair.")]
    public ZEDManager zedManager = null;

    /// <summary>
    /// Reference to the object that will be placed at the end of the laser.
    /// </summary>
    private GameObject pointerbead;

    /// <summary>
    /// Reference to the audio source
    /// </summary>
    private AudioSource audiosource;

    /// <summary>
    /// The gameobject's controller for tracking and input.
    /// </summary>
    private ZEDControllerTracker_DemoInputs objecttracker;

    IEnumerator Start()
    {
        audiosource = GetComponent<AudioSource>();

        if (zedManager == null)
        {
            zedManager = FindObjectOfType<ZEDManager>();
            if (ZEDManager.GetInstances().Count > 1)
            {
                Debug.Log("Warning: " + gameObject + " ZEDManager reference not set, but there are multiple ZEDManagers in the scene. " +
                    "Setting to first available ZEDManager, which may cause undesirable crosshair positions.");
            }
        }

        if (laserPointerBeadHolder != null)
        {
            //Get the laser bead from the parent/achor object.
            pointerbead = laserPointerBeadHolder.transform.GetChild(0).gameObject;
            //Disable the laser bead to wait for the ZED to initialize.
            pointerbead.SetActive(false);
        }

        //Wait for VR Controllers to initilize
        yield return new WaitForSeconds(1f);

        objecttracker = GetComponent<ZEDControllerTracker_DemoInputs>();

        if (objecttracker != null)
        {

            InputDeviceCharacteristics TrackedControllerFilter = InputDeviceCharacteristics.HeldInHand;
            List<InputDevice> foundControllers = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(TrackedControllerFilter, foundControllers);
            if (foundControllers.Count > 0) { yield break; }

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

            if (otherObjectTracker != null)
            {
                int children = transform.childCount;

                for (int i = 0; i < children; ++i)
                    transform.GetChild(i).gameObject.SetActive(false);

                yield break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Do we have a Pointer Bead to position in the world?
        if (laserPointerBeadHolder != null)
        {
            Vector3 crosshairpoint;
            Vector3 crosshairnormal;
            //Point the bead at the closest thing in front of the camera.
            if (FindCrosshairPosition(out crosshairpoint, out crosshairnormal))
            {
                //We hit something. Make sure the bead is active.
                pointerbead.SetActive(true);

                //Position the bead a the collision point, and make it face you.
                pointerbead.transform.position = crosshairpoint;
                if (crosshairnormal.magnitude > 0.0001f)
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
        //Use objecttracker to know if this controller was intended to be on a VR controller.
        if (objecttracker == null && (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Space)))
        {
            buttondown = true;
        }

        //Update whether the Touch controllers are active.
        int children = transform.childCount;
        InputDeviceCharacteristics TrackedControllerFilter = InputDeviceCharacteristics.HeldInHand;
        List<InputDevice> foundControllers = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(TrackedControllerFilter, foundControllers);

        if (foundControllers.Count > 0)
        {
            for (int i = 0; i < children; ++i)
                transform.GetChild(i).gameObject.SetActive(true);
        }
        else
        {
            for (int i = 0; i < children; ++i)
                transform.GetChild(i).gameObject.SetActive(false);
        }
         
        //We're controlling the fire Rate.
        if (objecttracker != null)
        {
            buttondown = objecttracker.CheckFireButton(ControllerButtonState.Down);
        }

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
        Vector3 realpoint = Vector3.zero;
        float realdistance = 20f; //Arbitrary distance to put the crosshair if it hits nothing at all. Chosen by ZED's max range.
        bool foundrealdistance = false;

        if (ZEDSupportFunctions.HitTestOnRay(zedManager.zedCamera, zedManager.GetMainCamera(), laserPointerBeadHolder.transform.position,
            laserPointerBeadHolder.transform.rotation, 5f, 0.01f, out realpoint))
        {
            realdistance = Vector3.Distance(laserPointerBeadHolder.transform.position, realpoint);
            foundrealdistance = true;
        }


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
        if (!foundvirtualdistance || realdistance < hitinfo.distance)
        {
            //The real world is closer. Give the position of the real world pixel and return true.
            crosshairpoint = realpoint;
            ZEDSupportFunctions.GetNormalAtWorldLocation(zedManager.zedCamera, realpoint, sl.REFERENCE_FRAME.WORLD, zedManager.GetMainCamera(), out collisionnormal);
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

        if (laserPointerBeadHolder != null && pointerbead.transform.localPosition != Vector3.zero)
        {
            blastershot.transform.LookAt(pointerbead.transform.position);
        }

        //Play a sound
        if (audiosource && audiosource.enabled)
        {
            audiosource.Play();
        }
    }

    /// <summary>
    /// Returns if this script is bound to an Oculus Touch controller that is currently not connected.
    /// For example, if it's a Right Controller but only the left is connected, it returns false.
    /// If not bound to a controller, returns true.
    /// </summary>
    /// <returns></returns>
    private bool IsConnectedController()
    {
        if (!objecttracker) return true; //Not attached to a tracker. Return true since it doesn't depend on a controller to be alive.
        if (objecttracker.deviceToTrack != ZEDControllerTracker.Devices.LeftController && objecttracker.deviceToTrack != ZEDControllerTracker.Devices.RightController)
            return true; //Not bound to a left or right controller, so let it live.

        InputDeviceCharacteristics leftTrackedControllerFilter = InputDeviceCharacteristics.Left;
        InputDeviceCharacteristics rightTrackedControllerFilter = InputDeviceCharacteristics.Right;
        List<InputDevice> foundLeftControllers = new List<InputDevice>();
        List<InputDevice> foundRightControllers = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(leftTrackedControllerFilter, foundLeftControllers);
        InputDevices.GetDevicesWithCharacteristics(rightTrackedControllerFilter, foundRightControllers);

        if (objecttracker.deviceToTrack == ZEDControllerTracker.Devices.LeftController && foundLeftControllers.Count > 0) return true; //Left controller only.
        if (objecttracker.deviceToTrack == ZEDControllerTracker.Devices.RightController && foundRightControllers.Count > 0) return true; //Right controller only.


        return false;
    }

}
