using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class BunnyPlacement : MonoBehaviour
{
    //Positive Color for placement
    public Texture[] goodPlacementTex;
    //Negative Color for placement
    public Texture[] badPlacementTex;
    //Point Light
    private Light pointLight;
    // Reference to the Object Tracker.
    private ZEDControllerTracker trackedObj;
#if ZED_STEAM_VR
    // Reference to the SteamVR Controller for Input purposes.
    SteamVR_Controller.Device device;
#endif
    //Reference to the ZED Plane Detection Manager.
    private ZEDPlaneDetectionManager zedPlane;
    //Reference to the BunnySpawner.
    private BunnySpawner bunnySpawner;
    //Reference to the ZED's left camera gameobject.
    private Camera leftCamera;
    //Reference to the pointerBead transform.
    private Transform pointerBead;
    //Boolean that enables us to spawn or not the Bunny prefab.
    private bool _canSpawnBunny;
    public enum state
    {
        Idle,
        Down,
        Press,
        Up
    };
    //The Buttons State
    private state button;
	public state Button {
		get {
			return button;
		}
	}

    /// <summary>
    /// Awake is used to initialize any variables or game state before the game starts.
    /// </summary>
    void Awake()
    {
        _canSpawnBunny = false;
        trackedObj = GetComponent<ZEDControllerTracker>();
        zedPlane = FindObjectOfType<ZEDPlaneDetectionManager>();
        bunnySpawner = GetComponent<BunnySpawner>();
        if (!leftCamera)
        {
            leftCamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }
    }

    /// <summary>
    /// Sets a reference to the pointerBead.
    /// </summary>
    /// <param name="_pointer"></param>
    public void SetPoinerBead(Transform _pointer)
    {
        pointerBead = _pointer;
        pointLight = _pointer.GetChild(0).GetComponentInChildren<Light>();
    }

    /// <summary>
    /// Update is called every frame.
    /// Here we receive the input from the Controller.
    /// Then we decide what to do in each case.
    /// </summary>

    private void Update()
    {
        if (trackedObj == null)
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
            //Check if a Controller tracked.
            if ((int)trackedObj.index > 0)
            {
                device = SteamVR_Controller.Input((int)trackedObj.index);
            }

            //SteamVR provides OnButton responses for the Trigger input.
            //When pressing Down, Holding it, or Releasing it.
            if ((int)trackedObj.index > 0)
            {
                if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    button = state.Down;
                }
                else if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger))
                {
                    button = state.Press;
                }
                else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                {
                    button = state.Up;
                }
                else
                    button = state.Idle;
            }
#elif ZED_OCULUS
        //Check if a Controller is tracked.
        if ((int)trackedObj.deviceToTrack == 0)
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

        if ((int)trackedObj.deviceToTrack == 1)
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
                //Enable the bunnySpawner to display the pointerBead.
                bunnySpawner._canPlaceHolder = true;
                //Hide the baseball.
                if(bunnySpawner._baseballBat != null)
                bunnySpawner._baseballBat.SetActive(false);
                //Clear the list of detected planes by the Manager.
				if (zedPlane.hitPlaneList.Count > 0)
                {
					for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                    {
						Destroy(zedPlane.hitPlaneList[i].gameObject);
						zedPlane.hitPlaneList.RemoveAt(i);
                    }
                }
                //Destroy the current Bunny, if any, on the scene.
                if (!bunnySpawner.canSpawnMoreBunnies && bunnySpawner._currentBunny != null)
                {
                    Destroy(bunnySpawner._currentBunny);
                    bunnySpawner._currentBunny = null;
                }
            }

            //From the first input to the next ones as it keeps being hold down.
            if (button == state.Press || button == state.Down)
            {
				if (zedPlane.hitPlaneList.Count == 0)
                {
                    //Launch the detection of Planes through the ZED Plane Detection Manager.
                    if (zedPlane.DetectPlaneAtHit(leftCamera.WorldToScreenPoint(pointerBead.position)))
                    {
                        //Get the normal of the plane.
						ZEDPlaneGameObject currentPlane = zedPlane.getHitPlane(zedPlane.hitPlaneList.Count - 1);
						Vector3 planeNormal = currentPlane.worldNormal;
                        //Check if the plane has a normal close enough to Y (horizontal surface) to be stable for the Bunny to spawn into.
                        if (Vector3.Dot(planeNormal, Vector3.up) > 0.85f)
                        {
                            //Allow to spawn the Bunny, and set the pointerBead to a positive color.
                            if (_canSpawnBunny == false)
                            {
                                _canSpawnBunny = true;
                                bunnySpawner._placeHolderMat[0].mainTexture = goodPlacementTex[0];
                                bunnySpawner._placeHolderMat[1].mainTexture = goodPlacementTex[1];
                                pointLight.color = Color.blue;
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
                            //Don't allow for the Bunny to spawn,  and set the pointerBead to a negative color.
                            _canSpawnBunny = false;
                            bunnySpawner._placeHolderMat[0].mainTexture = badPlacementTex[0];
                            bunnySpawner._placeHolderMat[1].mainTexture = badPlacementTex[1];
                            pointLight.color = Color.red;
                            //Clear the list of planes.
							for (int i = 0; i < zedPlane.hitPlaneList.Count; i++)
                            {
								Destroy(zedPlane.hitPlaneList[i].gameObject);
								zedPlane.hitPlaneList.RemoveAt(i);
                            }
                        }

                    }
                }

				else if (zedPlane.hitPlaneList.Count > 0)
                {
                    if (!Physics.Raycast(transform.position, pointerBead.position - transform.position))
                    {
                        //Don't allow for the Bunny to spawn,  and set the pointerBead to a negative color.
                        _canSpawnBunny = false;
                        bunnySpawner._placeHolderMat[0].mainTexture = badPlacementTex[0];
                        bunnySpawner._placeHolderMat[1].mainTexture = badPlacementTex[1];
                        pointLight.color = Color.red;
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
                if (_canSpawnBunny)
                {
					bunnySpawner.SpawnBunny(pointerBead.position);
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
                _canSpawnBunny = false;
                bunnySpawner._canPlaceHolder = false;
            }
        }
    }
}
