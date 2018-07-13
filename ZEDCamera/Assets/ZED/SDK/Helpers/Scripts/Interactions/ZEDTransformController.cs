using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZEDTransformController : MonoBehaviour
{
    public enum relativeMotion
    {
        Itself,
        Camera
    }
    [Header("Object motion relative to:")]
    public relativeMotion motion;
    public bool invertRotation;
    [Space(5)]
    [Header("Input Mode")]
    [Tooltip("If VR Controls are disabled, then the keyboard shortcuts will be applied.")]
    public bool VRControls;
    [Space(5)]
    [Header("Motion Options.")]

    //Public variables
    public float MovementSpeed = 0.5F;
    public float RotationSpeed = 0.5F;
    public float ScaleSpeed = 0.25F;
    public float maxScale = 2.0F;
    public float minScale = 0.25F;
    public Light spotLight;

    //Private variables
    private List<ZEDControllerTracker> objectTrackers = new List<ZEDControllerTracker>();
    private float inputScale;
    private float inputRotation;
    private Camera LeftCamera;
    private bool reposition = false;
    private ZEDManager zManager;
    private bool isMoving;

    private IEnumerator Start()
    {
        isMoving = false;
        //Find the left camera object if we didn't assign it at start. 
        if (!LeftCamera)
        {
            zManager = ZEDManager.Instance;
            LeftCamera = zManager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        }

        //Finding the available VR controllers and assigning them to our List.
        yield return new WaitForSeconds(1f);

        var trackers = FindObjectsOfType<ZEDControllerTracker>();
        foreach (ZEDControllerTracker tracker in trackers)
        {
            objectTrackers.Add(tracker);
        }

#if ZED_STEAM_VR
        if (objectTrackers.Count > 0)
        {
            for (int i = 0; i < objectTrackers.Count; i++)
            {
                if (objectTrackers[i].index >= 0)
                    VRControls = true;
            }
        }
#endif

#if ZED_OCULUS
        if (OVRManager.isHmdPresent)
        {
            if (OVRInput.GetConnectedControllers().ToString() == "Touch")
                VRControls = true;
        }
#endif
    }

    private void Update()
    {
        //Reposition the screen in front our the Camera when its ready
        if (ZEDManager.Instance.IsZEDReady && reposition == false)
        {
            transform.position = ZEDManager.Instance.OriginPosition + ZEDManager.Instance.OriginRotation * (Vector3.forward);
            Quaternion newRot = Quaternion.LookRotation(ZEDManager.Instance.OriginPosition - transform.position, Vector3.up);
            transform.eulerAngles = new Vector3(0, newRot.eulerAngles.y + 180, 0);
            reposition = true;
        }
        
        Vector3 moveAxis = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
        {
                inputRotation = -1 * (RotationSpeed * 50) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E))
        {
                inputRotation = 1 * (RotationSpeed * 50) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveAxis = Vector3.forward * MovementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveAxis = Vector3.back * MovementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveAxis = Vector3.left * MovementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveAxis = Vector3.right * MovementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.R))
        {
            moveAxis = Vector3.up * MovementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.F))
        {
            moveAxis = Vector3.down * MovementSpeed * Time.deltaTime;
        }

        Quaternion gravity = Quaternion.identity;

        if (!VRControls)
        {
            if (moveAxis != Vector3.zero)
            {
                isMoving = true;
                if(motion == relativeMotion.Itself)
                transform.Translate(moveAxis.x, moveAxis.y, moveAxis.z);
                else if (motion == relativeMotion.Camera)
                {
                    gravity = Quaternion.FromToRotation(zManager.GetZedRootTansform().up, Vector3.up);
                    transform.localPosition += zManager.GetLeftCameraTransform().right * moveAxis.x;
                    transform.localPosition += zManager.GetLeftCameraTransform().forward * moveAxis.z;
                    transform.localPosition += gravity * zManager.GetLeftCameraTransform().up * moveAxis.y;
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
        }
        else
        {
            if (zManager)
            {
#if ZED_OCULUS
                if(UnityEngine.VR.VRSettings.loadedDeviceName == "Oculus")
                { 
                    if (OVRInput.GetConnectedControllers().ToString() == "Touch")
                    {
                        if (objectTrackers.Count > 0)
                        {
                            moveAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
                            inputRotation = moveAxis.x * RotationSpeed;

                            gravity = Quaternion.FromToRotation(zManager.GetZedRootTansform().up, Vector3.up);
                            transform.localPosition += gravity * zManager.GetLeftCameraTransform().up * moveAxis.y * MovementSpeed * Time.deltaTime;

                            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.75f)
                                inputScale = 1f;
                        }

                        if (objectTrackers.Count > 1)
                        {
                            moveAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
                            if (moveAxis.x != 0 || moveAxis.y != 0)
                            {
                                isMoving = true;
                                gravity = Quaternion.FromToRotation(zManager.GetZedRootTansform().up, Vector3.up);
                                transform.localPosition += zManager.GetLeftCameraTransform().right * moveAxis.x * MovementSpeed * Time.deltaTime;
                                transform.localPosition += gravity * zManager.GetLeftCameraTransform().forward * moveAxis.y * MovementSpeed * Time.deltaTime;
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
                    //Looks for any input from this controller through SteamVR
                    if (objectTrackers.Count > 0 && objectTrackers[0].index >= 0)
                    {
                        moveAxis = SteamVR_Controller.Input((int)objectTrackers[0].index).GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
                        inputRotation = moveAxis.x * RotationSpeed;

                        gravity = Quaternion.FromToRotation(zManager.GetZedRootTansform().up, Vector3.up);
                        transform.localPosition += gravity * zManager.GetLeftCameraTransform().up * moveAxis.y * MovementSpeed * Time.deltaTime;

                        if (objectTrackers[0].index > 0 && SteamVR_Controller.Input((int)objectTrackers[0].index).GetHairTrigger())
                            inputScale = 1f;
                    }

                    if (objectTrackers.Count > 1 && objectTrackers[1].index >= 0)
                    {
                        moveAxis = SteamVR_Controller.Input((int)objectTrackers[1].index).GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);

                        if (moveAxis.x != 0 || moveAxis.y != 0)
                        {
                            isMoving = true;
                            gravity = Quaternion.FromToRotation(zManager.GetZedRootTansform().up, Vector3.up);
                            transform.localPosition += zManager.GetLeftCameraTransform().right * moveAxis.x * MovementSpeed * Time.deltaTime;
                            transform.localPosition += gravity * zManager.GetLeftCameraTransform().forward * moveAxis.y * MovementSpeed * Time.deltaTime;
                        }
                        else
                            isMoving = false;

                        if (objectTrackers[1].index > 0 && SteamVR_Controller.Input((int)objectTrackers[1].index).GetHairTrigger())
                            inputScale = -1f;
                    }
                }
#endif
            }
        }

        //Rotation
        float h = inputRotation;

        if (invertRotation)
            transform.Rotate(0, h, 0);
        else
            transform.Rotate(0, -h, 0);

        //Reset Rotation for next frame
        inputRotation = 0f;

        //Scale
        float s = ScaleSpeed * (inputScale * Time.deltaTime);

        //Reset scale for next frame
        inputScale = 0f;

        transform.localScale = new Vector3(transform.localScale.x + s,
                                           transform.localScale.y + s,
                                           transform.localScale.z + s);

        if (transform.localScale.x > maxScale)
            transform.localScale = new Vector3(maxScale, maxScale, maxScale);
        else if (transform.localScale.x < minScale)
            transform.localScale = new Vector3(minScale, minScale, minScale);

        //Enable/Disable light
        if (spotLight != null)
            EnableLights();
    }

    void EnableLights()
    {
        //Enable / Disable Light if there is any and the object is moving.
        if (!spotLight.enabled && isMoving)
        {
            spotLight.enabled = true;
        }
        else if (spotLight.enabled && !isMoving)
        {
            spotLight.enabled = false;
        }

        //Scale Light with Object Size
        if (spotLight.enabled && spotLight.type == LightType.Spot)
        {
            spotLight.spotAngle = transform.localScale.x * 180 * 2;
            spotLight.range = transform.localScale.x * 4f;
            if (spotLight.range > 2)
                spotLight.range = 2;
        }
    }
}