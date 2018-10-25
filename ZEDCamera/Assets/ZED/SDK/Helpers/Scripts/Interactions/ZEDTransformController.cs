using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves/rotates the attached object using the keyboard or, if the
/// Oculus/SteamVR plugins are imported, by buttons on the VR controllers.
/// To use VR controllers, you must also have a ZEDControllerTracker component in the scene
/// that's set to track the controller you want to use. 
/// Used in the ZED Planetarium and Movie Screen example scenes to move the solar system/screen. 
/// </summary>
public class ZEDTransformController : MonoBehaviour
{
    //Public variables

    /// <summary>
    /// Rotational reference point for moving forward, backward, left, right, etc. 
    /// </summary>
    [Header("Object motion relative to:")]
    [Tooltip("Rotational reference point for moving forward, backward, left, right, etc.")]
    public RelativeMotion motion;

    /// <summary>
    /// Whether to rotate in the opposite direction specified, eg rotating 'right' means counter-clockwise.
    /// </summary>
    [Tooltip("Whether to rotate in the opposite direction specified, eg rotating 'right' means counter-clockwise.")]
    public bool invertRotation;

    /// <summary>
    /// If true, the object will teleport to a meter in front of the ZED once it's finished initializing. 
    /// </summary>
    [Tooltip("If true, the object will teleport to a meter in front of the ZED once it's finished initializing.")]
    public bool repositionAtStart = true;

    /// <summary>
    /// How fast the object moves/translates, in meters per second. 
    /// </summary>
    [Space(5)]
    [Header("Motion Options")]
    [Tooltip("How fast the object moves/translates, in meters per second. ")]
    public float movementSpeed = 0.5F;

    /// <summary>
    /// How fast the object rotates, in revolutions per second.
    /// </summary>
    [Tooltip("How fast the object rotates, in revolutions per second.")]
    public float rotationSpeed = 0.1f;

    /// <summary>
    /// How quickly an object gets bigger or smaller. 
    /// Scale increases/decreases by this factor every second. 
    /// </summary>
    [Tooltip("How quickly an object gets bigger or smaller. Scale increases/decreases by this factor every second.")]
    public float scaleSpeed = 0.25F;

    /// <summary>
    /// The largest amount to which the object can scale.
    /// </summary>
    [Tooltip("The largest amount to which the object can scale.")]
    public float maxScale = 2.0F;

    /// <summary>
    /// The smallest amount down to which the object can scale. 
    /// </summary>
    [Tooltip("The smallest amount down to which the object can scale. ")]
    public float minScale = 0.25F;

    /// <summary>
    /// Optional reference to a light that is enabled only when moving. 
    /// Used in the ZED Movie Screen sample to project a light underneath the screen when moved. 
    /// </summary>
    [Space(5)]
    [Tooltip("Optional reference to a light that is enabled only when moving.")]
    public Light spotLight;

    //Private variables

    /// <summary>
    /// List of all ZEDControllerTrackers in the scene. Used to get input in an SDK/agnostic way. 
    /// </summary>
    private List<ZEDControllerTracker> objectTrackers = new List<ZEDControllerTracker>();

    /// <summary>
    /// Left Camera component in the ZED rig, that represents the ZED's left sensor. 
    /// Used when RelativeMotion is set to Camera for providing relative values. 
    /// </summary>
    private Camera leftCamera;

    /// <summary>
    /// Reference to the scene's ZEDManager component. 
    /// Used when RelativeMotion is set to Camera, for finding the current position of the ZED.
    /// </summary>
    private ZEDManager zedManager;

    /// <summary>
    /// Whether the object is moving/translating. 
    /// </summary>
    private bool isMoving;

    private IEnumerator Start()
    {
        isMoving = false;
        zedManager = ZEDManager.Instance;

        //Find the available VR controllers and assigning them to our List.
        yield return new WaitForSeconds(1f);

        var trackers = FindObjectsOfType<ZEDControllerTracker>();
        foreach (ZEDControllerTracker tracker in trackers)
        {
            objectTrackers.Add(tracker);
        }

        if (repositionAtStart) //If the user wants, move the object in front of the ZED once it's initialized. 
        {
            ZEDManager.OnZEDReady += RepositionInFrontOfZED;
        }
    }

    private void Update()
    {
        Vector3 moveAxis = Vector3.zero; //Translation. Used by keyboard only. 
        float inputRotation = 0f; //Applied rotation, between -1 and 1. Cumulative between keyboard and controllers. 
        float inputScale = 0f; //Applied scale change, either -1, 0 or 1. Cumulative between keyboard and controllers. 

        //Keyboard inputs. 
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
        {
            inputRotation = -1 * (rotationSpeed * 360) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E))
        {
            inputRotation = 1 * (rotationSpeed * 360) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveAxis = Vector3.forward * movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveAxis = Vector3.back * movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveAxis = Vector3.left * movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveAxis = Vector3.right * movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.R))
        {
            moveAxis = Vector3.up * movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.F))
        {
            moveAxis = Vector3.down * movementSpeed * Time.deltaTime;
        }

        Quaternion gravity = Quaternion.identity;


        if (moveAxis != Vector3.zero)
        {
            isMoving = true;
            if (motion == RelativeMotion.Itself)
            {
                transform.Translate(moveAxis.x, moveAxis.y, moveAxis.z);
            }
            else if (motion == RelativeMotion.Camera)
            {
                gravity = Quaternion.FromToRotation(zedManager.GetZedRootTansform().up, Vector3.up);
                transform.localPosition += zedManager.GetLeftCameraTransform().right * moveAxis.x;
                transform.localPosition += zedManager.GetLeftCameraTransform().forward * moveAxis.z;
                transform.localPosition += gravity * zedManager.GetLeftCameraTransform().up * moveAxis.y;
            }
        }
        else
        {
            isMoving = false;
        }

        if (Input.GetKey(KeyCode.Mouse0))
            inputScale = 1f;
        else if (Input.GetKey(KeyCode.Mouse1))
            inputScale = -1f;

        if (zedManager)
        {
#if ZED_OCULUS
            if (UnityEngine.VR.VRSettings.loadedDeviceName == "Oculus")
            {
                if (OVRInput.GetConnectedControllers().ToString() == "Touch")
                {
                    Vector3 moveaxisoculus = new Vector3(); //Position change by controller. Added to keyboard version if both are applied. 

                    if (objectTrackers.Count > 0)
                    {

                        moveaxisoculus = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
                        inputRotation += moveaxisoculus.x * rotationSpeed * 360 * Time.deltaTime;


                        gravity = Quaternion.FromToRotation(zedManager.GetZedRootTansform().up, Vector3.up);
                        transform.localPosition += gravity * zedManager.GetLeftCameraTransform().up * moveaxisoculus.y * movementSpeed * Time.deltaTime;

                        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.75f)
                            inputScale = 1f;
                    }

                    if (objectTrackers.Count > 1)
                    {
                        moveaxisoculus = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
                        if (moveaxisoculus.x != 0 || moveaxisoculus.y != 0)
                        {
                            isMoving = true;
                            gravity = Quaternion.FromToRotation(zedManager.GetZedRootTansform().up, Vector3.up);
                            transform.localPosition += zedManager.GetLeftCameraTransform().right * moveaxisoculus.x * movementSpeed * Time.deltaTime;
                            transform.localPosition += gravity * zedManager.GetLeftCameraTransform().forward * moveaxisoculus.y * movementSpeed * Time.deltaTime;
                        }
                        else
                            isMoving = false;

                        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) > 0.75f)
                            inputScale = -1f;
                    }
                }

                OVRInput.Update();
            }
#endif
#if ZED_STEAM_VR
            if (UnityEngine.VR.VRSettings.loadedDeviceName == "OpenVR")
            {
                Vector3 moveaxissteamvr = new Vector3(); //Position change by controller. Added to keyboard version if both are applied. 

                //Looks for any input from this controller through SteamVR.
                if (objectTrackers.Count > 0 && objectTrackers[0].index >= 0)
                {
                    //moveaxissteamvr = SteamVR_Controller.Input((int)objectTrackers[0].index).GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                    moveaxissteamvr = objectTrackers[0].GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);

                    inputRotation += moveaxissteamvr.x * rotationSpeed * 360f * Time.deltaTime;

                    gravity = Quaternion.FromToRotation(zedManager.GetZedRootTansform().up, Vector3.up);
                    transform.localPosition += gravity * zedManager.GetLeftCameraTransform().up * moveaxissteamvr.y * movementSpeed * Time.deltaTime;

                    //if (objectTrackers[0].index > 0 && SteamVR_Controller.Input((int)objectTrackers[0].index).GetHairTrigger())
                    if (objectTrackers[0].index > 0 && objectTrackers[0].GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x > 0.1f)
                    {
                        inputScale = 1f;
                    }
                }

                if (objectTrackers.Count > 1 && objectTrackers[1].index >= 0)
                {
                    //moveaxissteamvr = SteamVR_Controller.Input((int)objectTrackers[1].index).GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                    moveaxissteamvr = objectTrackers[1].GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);

                    if (moveaxissteamvr.x != 0 || moveaxissteamvr.y != 0)
                    {
                        isMoving = true;
                        gravity = Quaternion.FromToRotation(zedManager.GetZedRootTansform().up, Vector3.up);
                        transform.localPosition += zedManager.GetLeftCameraTransform().right * moveaxissteamvr.x * movementSpeed * Time.deltaTime;
                        transform.localPosition += gravity * zedManager.GetLeftCameraTransform().forward * moveaxissteamvr.y * movementSpeed * Time.deltaTime;
                    }
                    else
                        isMoving = false;

                    //if (objectTrackers[1].index > 0 && SteamVR_Controller.Input((int)objectTrackers[1].index).GetHairTrigger())
                    if (objectTrackers[1].index > 0 && objectTrackers[1].GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x > 0.1f)
                    {
                        inputScale = -1f;
                    }
                }
            }
#endif

        }

        //Rotation
        float h = inputRotation;

        if (invertRotation)
            transform.Rotate(0, h, 0);
        else
            transform.Rotate(0, -h, 0);

        //Scale
        float s = scaleSpeed * (inputScale * Time.deltaTime);

        transform.localScale = new Vector3(transform.localScale.x + s,
                                           transform.localScale.y + s,
                                           transform.localScale.z + s);

        if (transform.localScale.x > maxScale)
            transform.localScale = new Vector3(maxScale, maxScale, maxScale);
        else if (transform.localScale.x < minScale)
            transform.localScale = new Vector3(minScale, minScale, minScale);

        //Enable/disable light if moving. 
        if (spotLight != null)
        {
            SetMovementLight();
        }
    }

    /// <summary>
    /// Turns the optional spotLight on or off depending on if the object is moving/translating. 
    /// Also scales the light to match the object's own scale. 
    /// </summary>
    void SetMovementLight()
    {
        //Enable/disable Light if the object is moving.
        if (!spotLight.enabled && isMoving)
        {
            spotLight.enabled = true;
        }
        else if (spotLight.enabled && !isMoving)
        {
            spotLight.enabled = false;
        }

        //Scale light with object size.
        if (spotLight.enabled && spotLight.type == LightType.Spot)
        {
            spotLight.spotAngle = transform.localScale.x * 180 * 2;
            spotLight.range = transform.localScale.x * 4f;
            if (spotLight.range > 2)
                spotLight.range = 2;
        }
    }

    /// <summary>
    /// Repositions the object to a meter in front of the ZED. 
    /// Called by ZEDManager.OnZEDReady if repositionAtStart is enabled. 
    /// </summary>
    void RepositionInFrontOfZED()
    {
        transform.position = ZEDManager.Instance.OriginPosition + ZEDManager.Instance.OriginRotation * (Vector3.forward);
        Quaternion newRot = Quaternion.LookRotation(ZEDManager.Instance.OriginPosition - transform.position, Vector3.up);
        transform.eulerAngles = new Vector3(0, newRot.eulerAngles.y + 180, 0);
    }

    /// <summary>
    /// Options for what movement will be relevant to. 
    /// </summary>
    public enum RelativeMotion
    {
        Itself, //Relative to its own rotation, eg. moving forward moves where the object is facing. 
        Camera //Relative to the camera's rotation, eg. moving forward moves where the camera/player is facing. 
    }
}