using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ZED_STEAM_VR
using Valve.VR;
#endif

/// <summary>
/// Lets the user choose the tracked object that the ZED is anchored to, for the MR calibration scene. 
/// Works by spawning a prefab button for each tracked object, complete with graphics, and arranging them
/// into a grid. Each is assigned a callback that will enable the scene's CameraAnchor object and set its 
/// tracked object accordingly. 
/// </summary>
public class ChooseTrackedObjectMenu : MonoBehaviour
{
    /// <summary>
    /// Prefab object for the buttons you press to choose the anchor. 
    /// By default, it's Prefabs/ChooseTrackedObjectButton.
    /// </summary>
    [Tooltip("Prefab object for the buttons you press to choose the anchor. " +
        "By default, it's Prefabs/ChooseTrackedObjectButton.")]
    public GameObject chooseObjectButtonPrefab;

    public bool updateTrackedObjects = true;
    public float updateIntervalSeconds = 1f;
    private float updateTimer = 0f;

    /// <summary>
    /// How wide the grid of tracked object buttons can be. Objects are stacked upwards. 
    /// </summary>
    [Tooltip("How wide the grid of tracked object buttons can be. Objects are stacked upwards. ")]
    [Space(5)]
    [Header("Grid")]
    public int maxColumns = 3;
    /// <summary>
    /// Width of the button prefab object. Used for spacing them in the grid horizontally. 
    /// </summary>
    [Tooltip("Width of the button prefab object. Used for spacing them in the grid horizontally. ")]
    public float objectWidth = 0.3f;
    /// <summary>
    /// Width of the button prefab object. Used for spacing them in the grid vertically. 
    /// </summary>
    [Tooltip("Width of the button prefab object. Used for spacing them in the grid vertically. ")]
    public float objectHeight = 0.4f;

    /// <summary>
    /// All button objects instantiated at runtime. 
    /// </summary>
    private List<ChooseTrackedObjectButton> allButtons = new List<ChooseTrackedObjectButton>();


    /// <summary>
    /// Temporary left controller object that's created at spawn so that both left and right controllers
    /// can be used to choose an anchor. Destroyed once an anchor is chosen. 
    /// </summary>
    [Space(5)]
    [Header("Anchors")]
    [Tooltip("Temporary left controller object that's created at spawn so that both left and right controllers " +
        "can be used to choose an anchor. Destroyed once an anchor is chosen. ")]
    public ZEDControllerTracker tempLeftController;
    /// <summary>
    /// The scene's CameraAnchor object, which holds the ZED camera. Should be disabled at start. 
    /// </summary>
    [Tooltip("The scene's CameraAnchor object, which holds the ZED camera. Should be disabled at start. ")]
    public CameraAnchor zedAnchor;
    /// <summary>
    /// The scene's left controller. Should be disabled at start, since the left controller has the menu by default, 
    /// which doesn't do anything until you've chosen an anchor. 
    /// </summary>
    [Tooltip("The scene's left controller. Should be disabled at start, since the left controller has the menu by default, " +
        "which doesn't do anything until you've chosen an anchor. ")]
    public ZEDControllerTracker leftController;
    /// <summary>
    /// The scene's right controller. By default, this can interact with buttons/grabbables, so it's always enabled
    /// unless the right controller is set to be the ZED's anchor. 
    /// </summary>
    [Tooltip("The scene's right controller. By default, this can interact with buttons/grabbables, so it's always enabled " +
        "unless the right controller is set to be the ZED's anchor. ")]
    public ZEDControllerTracker rightController;
    /// <summary>
    /// The "belly" menu controller, which provides the menu normally on the left (or right) hands when a Tracker is 
    /// the ZED's anchor. Disabled by default. 
    /// </summary>
    [Tooltip("The 'belly' menu controller, which provides the menu normally on the left (or right) hands when a Tracker is " +
        "the ZED's anchor. Disabled by default. ")]
    public BellyMenu bellyMenu;
    /// <summary>
    /// The transform holding the 2D floating view screen object. Disabled at start, enabled once the user chooses an anchor. 
    /// </summary>
    [Tooltip("The transform holding the 2D floating view screen object. Disabled at start, enabled once the user chooses an anchor. ")]
    public GameObject viewScreen;

    /// <summary>
    /// Transform holding the 3D textual instructions for choosing an anchor. Used to move it above the procedural grid of buttons. 
    /// </summary>
    [Space(5)]
    [Tooltip("Transform holding the 3D textual instructions for choosing an anchor. Used to move it above the procedural grid of buttons. ")]
    public Transform instructionsText;
    public float textBottomMargin = 0.2f;

#if ZED_STEAM_VR
    /// <summary>
    /// Action we'll invoke when a controller is connected/disconnected. Cached as it's a lambda function, and we need to
    /// remove that listener in OnDestroy(). 
    /// </summary>
    private UnityAction<int, bool> deviceConnectedAction;
#endif

#if ZED_OCULUS
    public const int TOUCH_INDEX_LEFT = 0;
    public const int TOUCH_INDEX_RIGHT = 1;
#endif

    // Use this for initialization
    void Start()
    {
        FindTrackedObjects(); //TODO: Make this happen in Update() at regular intervals, and clean up existing devices. 
        MessageDisplay.DisplayMessageAll("Which object is holding the ZED?");

#if ZED_STEAM_VR
        //Subscribe to controllers being connected/disconnected so we can update the available devices. 
        deviceConnectedAction = (x, y) => FindTrackedObjects();
        SteamVR_Events.DeviceConnected.AddListener(deviceConnectedAction);
#endif
    }


    /// <summary>
    /// Sets the chosen device as the anchor object, and enables/disables scene objects appropriately. 
    /// Called by ChooseTrackedObjectMenu when the user clicks it. 
    /// </summary>
    /// <param name="deviceindex">Index of the tracked object.</param>
    private void OnTrackedObjectSelected(int deviceindex)
    {
        //Make sure it's valid. 
        ZEDControllerTracker.Devices trackeddevice = ZEDControllerTracker.Devices.Hmd; //Can't be HMD at end - will catch. 

#if ZED_STEAM_VR
        ETrackedDeviceClass dclass = OpenVR.System.GetTrackedDeviceClass((uint)deviceindex);
        if (dclass == ETrackedDeviceClass.GenericTracker) trackeddevice = ZEDControllerTracker.Devices.ViveTracker;
        else if (dclass == ETrackedDeviceClass.Controller)
        {
            ETrackedControllerRole role = OpenVR.System.GetControllerRoleForTrackedDeviceIndex((uint)deviceindex);
            if (role == ETrackedControllerRole.LeftHand) trackeddevice = ZEDControllerTracker.Devices.LeftController;
            else if (role == ETrackedControllerRole.RightHand) trackeddevice = ZEDControllerTracker.Devices.RightController;
            else
            {
                Debug.Log("Couldn't assign to device of index " + deviceindex + " as it's a controller with an unknown role.");
            }
            //TODO: Make sure this works for a third controller. 
        }
#endif

#if ZED_OCULUS
        if(deviceindex == TOUCH_INDEX_LEFT)
        {
            trackeddevice = ZEDControllerTracker.Devices.LeftController;
        }
        else if (deviceindex == TOUCH_INDEX_RIGHT)
        {
            trackeddevice = ZEDControllerTracker.Devices.RightController;
        }
#endif
        //Set up the anchor object, and readjust controllers if needed. 
        zedAnchor.gameObject.SetActive(true);

        zedAnchor.controllerTracker.deviceToTrack = trackeddevice;

#if ZED_STEAM_VR
        leftController.index = tempLeftController.index;
#endif

        switch (trackeddevice)
        {
#if ZED_STEAM_VR
            case ZEDControllerTracker.Devices.ViveTracker:
                leftController.index = tempLeftController.index;
                leftController.gameObject.SetActive(true);
                break;
#endif
            case ZEDControllerTracker.Devices.LeftController:
                bellyMenu.gameObject.SetActive(true);
                break;
            case ZEDControllerTracker.Devices.RightController:
                PrimaryHandSwitcher switcher = leftController.GetComponentInChildren<PrimaryHandSwitcher>(true);
                if (!switcher) break;
                ToggleGroup3D handtoggle = switcher.transform.GetComponentInChildren<ToggleGroup3D>(true);
                handtoggle.toggleAtStart = false;
                leftController.gameObject.SetActive(true);
                switcher.SetPrimaryHand(false); //Switch to being left-handed.
                Destroy(rightController.gameObject);
                rightController = null;
                bellyMenu.gameObject.SetActive(true);
                break;
            case ZEDControllerTracker.Devices.Hmd:
            default:
                Debug.LogError("trackeddevice value somehow not set to valid value - must be a controller or tracker. ");
                break;
        }

        if (viewScreen) viewScreen.SetActive(true);

        Destroy(tempLeftController.gameObject);
        tempLeftController = null;

        //We're all done with this menu, so destroy it all. 
        Destroy(gameObject);

    }

    private void OnDestroy()
    {
#if ZED_STEAM_VR
        SteamVR_Events.DeviceConnected.RemoveListener(deviceConnectedAction);
#endif
    }

    /// <summary>
    /// Detects all tracked objects that could potentially be the ZED's anchor, and makes a button for each. 
    /// </summary>
    public void FindTrackedObjects()
    {
        //If there are any existing buttons, destroy them all. 
        foreach(ChooseTrackedObjectButton oldbutton in allButtons)
        {
            Destroy(oldbutton.gameObject);
        }
        allButtons.Clear();

        List<ChooseTrackedObjectButton> newbuttons = new List<ChooseTrackedObjectButton>();

#if ZED_STEAM_VR
        for (uint i = 0; i < 14; i++)
        {
            var error = ETrackedPropertyError.TrackedProp_Success;
            var chsnresult = new System.Text.StringBuilder((int)64);
            OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String, chsnresult, 64, ref error);

            if (error == ETrackedPropertyError.TrackedProp_Success && chsnresult.ToString() != "")
            {
                //Create tracked object button. 
                ChooseTrackedObjectButton button;
                CreateTrackedObjectPrefab(i, out button);
                if (button != null)
                {
                    newbuttons.Add(button);
                }
            }
        }

        //allButtons.Clear();
        foreach (ChooseTrackedObjectButton button in newbuttons)
        {
            allButtons.Add(button);
        }
#elif ZED_OCULUS
        /*//Warn the user if they don't have both controllers connected, because they need one to hold the ZED
        //and another to calibrate. 
        OVRInput.Controller controllers = OVRInput.GetConnectedControllers();
        if (controllers != OVRInput.Controller.Touch)
        {
            Debug.Log("Warning: You need at least two controllers connected to use the calibration app: One to hold " +
                "the ZED and another to interact with the app.");
        }*/

        //Make a left controller button. 
        ChooseTrackedObjectButton leftButton;
        CreateTrackedObjectPrefab(TOUCH_INDEX_LEFT, out leftButton);
        if (leftButton != null) allButtons.Add(leftButton);

        //Make a right controller button. 
        ChooseTrackedObjectButton rightButton;
        CreateTrackedObjectPrefab(TOUCH_INDEX_RIGHT, out rightButton);
        if (rightButton != null) allButtons.Add(rightButton);
#endif
        ArrangeIntoGrid(allButtons, objectWidth, objectHeight, maxColumns);
    }

    /// <summary>
    /// Creates a button for a tracked object detected in FindTrackedObjects() and sets it up. 
    /// </summary>
    /// <param name="deviceindex">Index of the tracked object.</param>
    /// <param name="scriptref">Required ChooseTrackedObjectButton script that must be on the prefab.</param>
    private GameObject CreateTrackedObjectPrefab(uint deviceindex, out ChooseTrackedObjectButton scriptref)
    {
        string label = "ERROR";

#if ZED_STEAM_VR
        ETrackedDeviceClass dclass = OpenVR.System.GetTrackedDeviceClass(deviceindex);
        ETrackedControllerRole role = OpenVR.System.GetControllerRoleForTrackedDeviceIndex(deviceindex);

        if (!(dclass == ETrackedDeviceClass.Controller || dclass == ETrackedDeviceClass.GenericTracker))
        {
            //Debug.LogError("Tried to create button for tracked device of index " + deviceindex + ", but it's role is " + dclass + ".");
            scriptref = null;
            return null;
        }

        if (role == ETrackedControllerRole.LeftHand)
        {
            label = "Left\r\nController";
        }
        else if (role == ETrackedControllerRole.RightHand)
        {
            label = "Right\r\nController";
        }
        else if (dclass == ETrackedDeviceClass.GenericTracker)
        {
            label = "Tracker";
        }
        else
        {
            //Debug.LogError("Tried to create button for tracked device of index " + deviceindex + " with an invalid role/device class combo: " +
            //    role + " / " + dclass);
            scriptref = null;
            return null;
        }
#elif ZED_OCULUS
        if (deviceindex == TOUCH_INDEX_LEFT)
        {
            label = "Left\r\nController";
        }
        else if (deviceindex == TOUCH_INDEX_RIGHT)
        {
            label = "Right\r\nController";
        }
#endif

        GameObject buttongo = Instantiate(chooseObjectButtonPrefab, transform, false);
        scriptref = buttongo.GetComponentInChildren<ChooseTrackedObjectButton>();

        scriptref.SetDeviceIndex((int)deviceindex);
        scriptref.SetLabel(label);
        buttongo.name = "Select " + label + " Button";
        scriptref.OnTrackedObjectSelected += OnTrackedObjectSelected;

        return buttongo;
    }

    /// <summary>
    /// Creates a button for a tracked object detected in FindTrackedObjects() and sets it up. 
    /// Overload that doesn't require you to output a ChooseTrackedObjectButton reference. 
    /// </summary>
    /// <param name="deviceindex">Index of the tracked object.</param>
    private GameObject CreateTrackedObjectPrefab(uint deviceindex)
    {
        ChooseTrackedObjectButton throwaway;
        return CreateTrackedObjectPrefab(deviceindex, out throwaway);
    }

    /// <summary>
    /// Arranges the anchor buttons into a grid, spaced based on the total number and user preferences. 
    /// Also adjusts the text object (instructionsText) to be above the grid.
    /// </summary>
    /// <param name="buttonlist">List of all instantiated buttons</param>
    /// <param name="objwidth">Width of each prefab, used for spacing.</param>
    /// <param name="objheight">Height of each prefab, used for spacing.</param>
    /// <param name="maxcolumns">How wide the grid can be before it stacks to a new row.</param>
    private void ArrangeIntoGrid(List<ChooseTrackedObjectButton> buttonlist, float objwidth, float objheight, int maxcolumns)
    {
        int numrows = Mathf.CeilToInt(buttonlist.Count / (float)maxcolumns);

        int remainingobjects = buttonlist.Count;

        float ycenterpoint = numrows / 2f + 0.5f;
        ycenterpoint -= 1;

        for (int v = 0; v < numrows; v++)
        {
            //Row height info.
            float yoffset = v - ycenterpoint;
            yoffset *= objheight;

            int thisrowwidth = (maxcolumns < remainingobjects) ? maxcolumns : remainingobjects;

            float xcenterpoint = thisrowwidth / 2f + 0.5f; //If you drew a line in the middle of a row of objects, where would it fall?
            xcenterpoint -= 1; //To account for index starting at 0. (yes we could just subtract 0.5 instead of adding but that's less clear.)

            for (int u = 0; u < thisrowwidth; u++)
            {
                int goindex = v * maxcolumns + u;
                float xoffset = u - xcenterpoint;
                xoffset *= objwidth;
                //TODO: Y offset
                Vector3 oldpos = buttonlist[goindex].transform.localPosition;
                buttonlist[goindex].transform.localPosition = new Vector3(xoffset, yoffset, oldpos.z);
            }

            remainingobjects -= thisrowwidth; //To simplify process of deciding how long the rows should be. 
        }

        //Reposition the instruction text to be just above the grid. 
        instructionsText.localPosition = new Vector3(instructionsText.localPosition.x, numrows / 2f * objheight + textBottomMargin, instructionsText.localPosition.z);
    }

}
