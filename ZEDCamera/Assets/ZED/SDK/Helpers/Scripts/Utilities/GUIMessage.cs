//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.VR;

/// <summary>
/// Controls the message displayed as the zed is initialized, and if it becomes disconnected. 
/// Needs to be pre-attached to the ZED rig to work; not added in programmatically. 
/// </summary><remarks>
/// There are separate text elements for the mono view and the stereo view to account for the 
/// difference in display resolutions. 'Mono' elements are displayed in a 'Screen Space - Overlay'
/// canvas, and 'Stereo' elements in a 'Screen Space - Camera' canvas. 
/// </remarks>
public class GUIMessage : MonoBehaviour
{
    /// <summary>
    /// Text under the loading sign for the mono rig ("Loading...", "Camera is not detected", etc.)
    /// </summary>
    private UnityEngine.UI.Text textmono;

    /// <summary>
    /// Text under the loading sign for stereo rig's left eye ("Loading...", "Camera is not detected", etc.)
    /// </summary>
    private UnityEngine.UI.Text textleft;

    /// <summary>
    /// Text under the loading sign for stereo rig's right eye ("Loading...", "Camera is not detected", etc.)
    /// </summary>
    private UnityEngine.UI.Text textright;

    /// <summary>
    /// Flag set to true when the ZED is finished initializing.
    /// Starts a timer to wait for the ZED's textures to be loaded.
    /// </summary>
    private bool ready = false;

    /// <summary>
    /// Warning container for the mono rig. Contains the text, background, and loading graphic.
    /// </summary>
    private GameObject warningmono;

    /// <summary>
    /// Warning container for the stereo rig's left eye. Contains the text, background, and loading graphic.
    /// </summary>
    private GameObject warningleft;

    /// <summary>
    /// Warning container for the stereo rig's right eye. Contains the text, background, and loading graphic.
    /// </summary>
    private GameObject warningright;

    /// <summary>
    /// Timer used to add a 0.5 second delay between the ZED being initialized and the message disappearing.
    /// This is done to let the ZED textures to finish being made. 
    /// </summary>
    private float timerWarning = 0.0f;

    /// <summary>
    /// If true, stops calling most of the logic in Update() which updates the canvas elements.
    /// Called once the ZED is ready and all elements have been properly disabled. 
    /// </summary>
    private bool init = false;

    /// <summary>
    /// Timer used to delay clearing the text by 0.2 seconds once the camera is initialized. 
    /// </summary>
    private float timer;

    /// <summary>
    /// Reference to the loading spinner animation for the mono rig.
    /// </summary>
    private GameObject imagemono;

    /// <summary>
    /// Reference to the loading spinner animation for the stereo rig's left eye.
    /// </summary>
    private GameObject imageleft;

    /// <summary>
    /// Reference to the loading spinner animation for the stereo rig's right eye.
    /// </summary>
    private GameObject imageright;

    /// <summary>
    /// Opening status given during the ZED's last attempt to initialize.
    /// Used to check if an error has gone on for more than one frame before changing text. 
    /// </summary>
    private sl.ERROR_CODE oldInitStatus;

    /// <summary>
    /// The zed manager.
    /// </summary>
    private ZEDManager zedManager;

    /// <summary>
    /// Creates canvas(es) and canvas elements depending on whether the ZED rig is mono (ZED_Rig_Mono) 
    /// or stereo (ZED_Rig_Stereo). 
    /// </summary>
    private void Awake()
    {
        zedManager = GetComponent<ZEDManager>();
        oldInitStatus = sl.ERROR_CODE.ERROR_CODE_LAST;
        if (!zedManager.IsStereoRig) //Without VR, we use a Screen Space - Overlay canvas. 
        {
            //Instantiate the mono warning prefab and set basic settings for it. 
            warningmono = Instantiate(Resources.Load("PrefabsUI/Warning") as GameObject, transform);
            warningmono.SetActive(true);
            warningmono.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;

            //Set the target camera to whichever mono camera in the rig has the highest depth. 
            Camera highestdepthzedcam = zedManager.GetLeftCamera();
            if (zedManager.GetRightCamera() != null && (highestdepthzedcam == null || zedManager.GetRightCamera().depth > highestdepthzedcam.depth))
            {
                highestdepthzedcam = zedManager.GetRightCamera();
            }

            warningmono.GetComponent<Canvas>().worldCamera = highestdepthzedcam;

            textmono = warningmono.GetComponentInChildren<UnityEngine.UI.Text>();
            textmono.color = Color.white;

            if (!sl.ZEDCamera.CheckPlugin())
            {
                textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_NOT_INSTALLED);
            }
            imagemono = warningmono.transform.GetChild(0).GetChild(1).gameObject;
            imagemono.transform.parent.gameObject.SetActive(true);
            ready = false;
        }
        else //In VR, we use two Screen Space - Camera canvases, one for each eye. 
        {
            //Instantiate the left warning prefab and set basic settings for it.
            warningleft = Instantiate(Resources.Load("PrefabsUI/Warning_VR") as GameObject, zedManager.GetLeftCameraTransform());
            warningleft.SetActive(true);
            warningleft.GetComponent<Canvas>().worldCamera = zedManager.GetLeftCamera();
            warningleft.GetComponent<Canvas>().planeDistance = 1;
            textleft = warningleft.GetComponentInChildren<UnityEngine.UI.Text>();
            textleft.color = Color.white;
            imageleft = warningleft.transform.GetChild(0).GetChild(1).gameObject;
            imageleft.transform.parent.gameObject.SetActive(true);

            //Instantiate the right warning prefab and set basic settings for it.
            warningright = Instantiate(Resources.Load("PrefabsUI/Warning_VR") as GameObject, zedManager.GetRightCameraTransform());
            warningright.SetActive(true);
            warningright.GetComponent<Canvas>().worldCamera = zedManager.GetRightCamera();
            warningright.GetComponent<Canvas>().planeDistance = 1;
            textright = warningright.GetComponentInChildren<UnityEngine.UI.Text>();
            textright.color = Color.white;
            imageright = warningright.transform.GetChild(0).GetChild(1).gameObject;
            imageright.transform.parent.gameObject.SetActive(true);

            if (!sl.ZEDCamera.CheckPlugin()) //Warn the use there's no SDK installed. 
            {
                textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_NOT_INSTALLED);
                textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_NOT_INSTALLED);
            }

            ready = false;
        }
    }

    /// <summary>
    /// Subscribe to OnZedReady and OnZEDDisconnected events. 
    /// </summary>
    private void OnEnable()
    {
        zedManager.OnZEDReady += Ready;
        zedManager.OnZEDDisconnected += ZEDDisconnected;
    }

    /// <summary>
    /// Unsubscribe from OnZedReady and OnZEDDisconnected events. 
    /// </summary>
    private void OnDisable()
    {
        zedManager.OnZEDReady -= Ready;
        zedManager.OnZEDDisconnected -= ZEDDisconnected;
    }

    /// <summary>
    /// Re-enable canvas elements and change message when ZED is disconnected. 
    /// GameObjects were disabled before because the ZED had to have finished initializing before. 
    /// </summary>
    void ZEDDisconnected()
    {
        if (warningmono) //Using the mono rig. 
        {
            warningmono.SetActive(true);
            imagemono.SetActive(true);

            warningmono.transform.GetChild(0).gameObject.SetActive(true);
            textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.ZED_IS_DISCONNECETD);
            warningmono.layer = 30;

            ready = false;
        }

        if (warningleft) //Using the stereo rig. 
        {
            warningleft.SetActive(true);
            imageleft.SetActive(true);
            warningleft.transform.GetChild(0).gameObject.SetActive(true);
            textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.ZED_IS_DISCONNECETD);
            warningleft.layer = 30;

            warningright.SetActive(true);
            imageright.SetActive(true);
            warningright.transform.GetChild(0).gameObject.SetActive(true);
            textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.ZED_IS_DISCONNECETD);
            warningright.layer = 30;

            ready = false;
        }
    }

    /// <summary>
    /// If visible, print the loading status of the ZED, including relevant errors. 
    /// </summary>
    void Update()
    {
        if (!init) //This check will pass until 0.5 seconds after the ZED is done initializing. 
        {
            sl.ERROR_CODE e = zedManager.LastInitStatus;

            if (e == sl.ERROR_CODE.SUCCESS) //Last initialization attempt was successful. 
            {
                if (!ready)
                {

                    if (textmono)
                    {
                        textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_MODULE_LOADING);
                    }
                    else if (textleft)
                    {
                        textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_MODULE_LOADING);
                        textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SDK_MODULE_LOADING);
                    }


                }
                else
                {
                    timer += Time.deltaTime; //Clear text after a short delay. 
                    if (timer > 0.2f)
                    {
                        if (textmono)
                        {
                            textmono.text = "";
                        }
                        else if (textleft)
                        {
                            textleft.text = "";
                            textright.text = "";
                        }

                    }

                    if (imagemono)
                    { //Disable mono rig canvas. 
                        imagemono.gameObject.SetActive(false);
                    }
                    else if (imageleft)
                    { //Disable stereo rig canvases. 
                        imageleft.gameObject.SetActive(false);
                        imageright.gameObject.SetActive(false);
                    }
                }
            }
            else if (e == sl.ERROR_CODE.ERROR_CODE_LAST) //"Loading..."
            {
                //Initial error code set before an initialization attempt has returned successful or failed. 
                //In short, it means it's still loading. 
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_LOADING);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_LOADING);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_LOADING);
                }

            }
            else if (e == sl.ERROR_CODE.CAMERA_NOT_DETECTED && oldInitStatus == sl.ERROR_CODE.CAMERA_NOT_DETECTED) //"Camera not detected"
            {
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.UNABLE_TO_OPEN_CAMERA);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.UNABLE_TO_OPEN_CAMERA);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.UNABLE_TO_OPEN_CAMERA);
                }
            }
            else if (e == sl.ERROR_CODE.CAMERA_DETECTION_ISSUE && oldInitStatus == sl.ERROR_CODE.CAMERA_DETECTION_ISSUE) //"Unable to open camera"
            {
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_DETECTION_ISSUE);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_DETECTION_ISSUE);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_DETECTION_ISSUE);
                }
            }
            else if (e == sl.ERROR_CODE.SENSOR_NOT_DETECTED && oldInitStatus == sl.ERROR_CODE.SENSOR_NOT_DETECTED) //"Camera motion sensor not detected"
            {
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SENSOR_NOT_DETECTED);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SENSOR_NOT_DETECTED);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SENSOR_NOT_DETECTED);
                }
            }
            else if (e == sl.ERROR_CODE.LOW_USB_BANDWIDTH && oldInitStatus == sl.ERROR_CODE.LOW_USB_BANDWIDTH)//"Low USB bandwidth"
            {
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.LOW_USB_BANDWIDTH);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.LOW_USB_BANDWIDTH);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.LOW_USB_BANDWIDTH);
                }
            }
            else if (e == sl.ERROR_CODE.INVALID_SVO_FILE && oldInitStatus == sl.ERROR_CODE.INVALID_SVO_FILE)
            {
                if (textmono)
                {
                    textmono.text = "Invalid SVO File/Path";
                }
                else if (textleft)
                {
                    textleft.text = "Invalid SVO File/Path";
                    textright.text = "Invalid SVO File/Path";
                }
            }
            else if (e == oldInitStatus)
            {
                if (textmono)
                {
                    textmono.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_NOT_INITIALIZED);
                }
                else if (textleft)
                {
                    textleft.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_NOT_INITIALIZED);
                    textright.text = ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.CAMERA_NOT_INITIALIZED);
                }
            }
            oldInitStatus = e;
        }

        if (ready) //ZED has finished initializing. Set a timer, then disable texts after it expires. 
        {
            timerWarning += Time.deltaTime;
            if (timerWarning > 0.5f)
            {
                if (warningmono)
                {
                    warningmono.SetActive(false);
                }
                else if (warningleft)
                {
                    warningleft.SetActive(false);
                    warningright.SetActive(false);
                }
            }

            init = true; //Prevents logic above the if (ready) check from running each frame. 

            if (imagemono)
            {
                imagemono.gameObject.transform.parent.gameObject.SetActive(false);
            }
            else if (imageleft)
            {
                imageleft.gameObject.transform.parent.gameObject.SetActive(false);
                imageright.gameObject.transform.parent.gameObject.SetActive(false);
            }
        }

    }

    /// <summary>
    /// Set a flag to run timer and disable text. Called by ZEDManager.OnZEDReady when ZED finishes initializing. 
    /// </summary>
    private void Ready()
    {
        ready = true;
    }
}
