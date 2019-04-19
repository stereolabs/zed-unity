using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Handles detecting whether or not a real-world location is valid for placing a Bunny object
/// in the ZED plane detection samples. To be valid, it must find a plane at the given location
/// and that plane must face upward. Turns a placeholder object blue when valid, and red when not. 
/// Also works with VR controllers if the SteamVR or Oculus Integration plugins are installed. 
/// </summary>
public class BunnyPlacement : MonoBehaviour
{
    /// <summary>
    /// Textures assigned to the placeholder object when placement is valid. 
    /// Index of each texture corresponds to the index of the material on the placeholder object. 
    /// </summary>
    [Tooltip("Textures assigned to the placeholder object when placement is valid. " +
        "Index of each texture corresponds to the index of the material on the placeholder object.")]
    public Texture[] goodPlacementTex;

    /// <summary>
    /// Textures assigned to the placeholder object when placement is not valid. 
    /// Index of each texture corresponds to the index of the material on the placeholder object. 
    /// </summary>
    [Tooltip("Textures assigned to the placeholder object when placement is not valid. " +
        "Index of each texture corresponds to the index of the material on the placeholder object.")]
    public Texture[] badPlacementTex;

    /// <summary>
    /// Light object in the placeholder object. We change its color based on placement validity. 
    /// </summary>
    private Light pointlight;

    /// <summary>
    /// The ZEDControllerTracker object in the VR controller used to place the object, if applicable. 
    /// </summary>
    private ZEDControllerTracker tracker;

    /// <summary>
    /// The scene's ZED Plane Detection Manager.
    /// </summary>
    private ZEDPlaneDetectionManager zedPlane;

    /// <summary>
    /// The BunnySpawner object, normally on the same object as this component. 
    /// </summary>
    private BunnySpawner bunnySpawner;

    /// <summary>
    /// The placeholder object's transform.
    /// </summary>
    private Transform placeholder;

    /// <summary>
    /// Whether or not we are able to spawn a bunny here. 
    /// </summary>
    private bool canspawnbunny;

    /// <summary>
    /// Possible states of the button used for input, whether the spacebar, a VR controller trigger, etc. 
    /// </summary>
    public enum state
    {
        Idle,
        Down,
        Press,
        Up
    };

    /// <summary>
    /// The current state of the button used for input. 
    /// </summary>
    public state button { get; private set; }

    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    void Awake()
    {
        canspawnbunny = false;
        tracker = GetComponent<ZEDControllerTracker>();
        zedPlane = FindObjectOfType<ZEDPlaneDetectionManager>();
        bunnySpawner = GetComponent<BunnySpawner>();
    }

    /// <summary>
    /// Sets a reference to the placeholder object. Set from BunnySpawner.cs. 
    /// </summary>
    /// <param name="pointer">The placeholder object.</param>
    public void SetPlaceholder(Transform pointer)
    {
        placeholder = pointer;
        pointlight = pointer.GetChild(0).GetComponentInChildren<Light>();
    }

    /// <summary>
    /// Update is called every frame.
    /// Here we receive the input from the Controller.
    /// Then we decide what to do in each case.
    /// </summary>
    private void Update()
    {
        if (tracker == null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                button = state.Down;
            else if (Input.GetKey(KeyCode.Space))
                button = state.Press;
            else if (Input.GetKeyUp(KeyCode.Space))
                button = state.Up;
            else
                button = state.Idle;
        }
        else
        {
#if ZED_STEAM_VR
            //SteamVR provides OnButton responses for the Trigger input.
            //When pressing down, holding it, or releasing it.
            if ((int)tracker.index > 0)
            {
                //if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                if (tracker.GetVRButtonDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    button = state.Down;
                }
                //else if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger))
                else if (tracker.GetVRButtonHeld(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    button = state.Press;
                }
                //else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                else if (tracker.GetVRButtonReleased(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    button = state.Up;
                }
                else
                    button = state.Idle;
            }
#elif ZED_OCULUS
        //Check if a Controller is tracked.
        if ((int)tracker.deviceToTrack == 0)
        {
            //Oculus Touch Triggers aren't of Button type, but Axis.
            //So we have to create our own state for this Input, based on sensitivity from 0 to 1.
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.5f)
            {
                if (button == state.Idle)
                {
                    button = state.Down;
                }
                else if (button == state.Down)
                {
                    button = state.Press;
                }
            }
            else
            {
                if (button == state.Press || button == state.Down)
                {
                    button = state.Up;
                }
                else if (button == state.Up)
                {
                    button = state.Idle;
                }
            }
        }

        if ((int)tracker.deviceToTrack == 1)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) > 0.5f)
            {
                if (button == state.Idle)
                {
                    button = state.Down;
                }
                else if (button == state.Down)
                {
                    button = state.Press;
                }
            }
            else
            {
                if (button == state.Press || button == state.Down)
                {
                    button = state.Up;
                }
                else if (button == state.Up)
                {
                    button = state.Idle;
                }
            }
        }

#endif
        }
        //If the Trigger Button is being used.
        if (button != state.Idle)
        {
            //It just got pressed.
            if (button == state.Down)
            {
                //Enable the bunnySpawner to display the placeholder.
                bunnySpawner.canDisplayPlaceholder = true;

                //If we were holding the baseball bat but the user wants to re-place the bunny, hide the baseball bat.
                if (bunnySpawner.baseballBat != null)
                    bunnySpawner.baseballBat.SetActive(false);

                //Clean up the list of detected planes.
                if (zedPlane.hitPlaneList.Count > 0)
                {
                    for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                    {
                        Destroy(zedPlane.hitPlaneList[i].gameObject);
                        zedPlane.hitPlaneList.RemoveAt(i);
                    }
                }
                //Destroy the current Bunny, if any, on the scene.
                if (!bunnySpawner.canSpawnMultipleBunnies && bunnySpawner.currentBunny != null)
                {
                    Destroy(bunnySpawner.currentBunny);
                    bunnySpawner.currentBunny = null;
                }
            }

            //From the first input to the next ones as it keeps being hold down.
            if (button == state.Press || button == state.Down)
            {
                if (zedPlane.hitPlaneList.Count == 0)
                {
                    //Start detecting planes through the ZED Plane Detection Manager.
                    foreach (ZEDManager manager in ZEDManager.GetInstances()) //Check all active ZED cameras for planes. 
                    {
                        if (zedPlane.DetectPlaneAtHit(manager, manager.GetLeftCamera().WorldToScreenPoint(placeholder.position)))
                        {
                            //Get the normal of the plane.
                            ZEDPlaneGameObject currentPlane = zedPlane.getHitPlane(zedPlane.hitPlaneList.Count - 1);
                            Vector3 planeNormal = currentPlane.worldNormal;

                            //Check if the plane has a normal close enough to Y (horizontal surface) to be stable for the Bunny to spawn into.
                            if (Vector3.Dot(planeNormal, Vector3.up) > 0.85f)
                            {
                                //Allow spawning the Bunny, and set the placeholder to a positive color.
                                if (canspawnbunny == false)
                                {
                                    canspawnbunny = true;
                                    bunnySpawner.placeHolderMat[0].mainTexture = goodPlacementTex[0];
                                    bunnySpawner.placeHolderMat[1].mainTexture = goodPlacementTex[1];
                                    pointlight.color = Color.blue;
                                }
                                else //Clear the list of planes.
                                {
                                    for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                                    {
                                        if (i == 0)
                                        {
                                            Destroy(zedPlane.hitPlaneList[i].gameObject);
                                            zedPlane.hitPlaneList.RemoveAt(i);
                                        }
                                    }
                                }
                            }
                            else //Surface wasn't horizontal enough
                            {
                                //Don't allow the Bunny to spawn, and set the placeholder to a negative color.
                                canspawnbunny = false;
                                bunnySpawner.placeHolderMat[0].mainTexture = badPlacementTex[0];
                                bunnySpawner.placeHolderMat[1].mainTexture = badPlacementTex[1];
                                pointlight.color = Color.red;

                                //Clear the list of planes.
                                for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                                {
                                    Destroy(zedPlane.hitPlaneList[i].gameObject);
                                    zedPlane.hitPlaneList.RemoveAt(i);
                                }
                            }
                            break; //If we detected a plane in one view, no need to go through the rest of the cameras. 
                        }
                    }
                }

                else if (zedPlane.hitPlaneList.Count > 0)
                {
                    if (!Physics.Raycast(transform.position, placeholder.position - transform.position))
                    {
                        //Don't allow for the Bunny to spawn,  and set the placeholder to a negative color.
                        canspawnbunny = false;
                        bunnySpawner.placeHolderMat[0].mainTexture = badPlacementTex[0];
                        bunnySpawner.placeHolderMat[1].mainTexture = badPlacementTex[1];
                        pointlight.color = Color.red;
                        //Clear the list of planes.
                        for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                        {
                            Destroy(zedPlane.hitPlaneList[i].gameObject);
                            zedPlane.hitPlaneList.RemoveAt(i);
                        }
                    }
                }
            }

            //Button is released.
            if (button == state.Up)
            {
                //If at that moment the bunny was allowed to spawn, proceed ot make the call.
                if (canspawnbunny)
                {
                    bunnySpawner.SpawnBunny(placeholder.position);
                }
                else //Clear the list of planes.
                {
                    for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                    {
                        Destroy(zedPlane.hitPlaneList[i].gameObject);
                        zedPlane.hitPlaneList.RemoveAt(i);
                    }
                }

                //Reset the booleans.
                canspawnbunny = false;
                bunnySpawner.canDisplayPlaceholder = false;
            }
        }
    }
}
