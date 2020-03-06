using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ZED_STEAM_VR
using Valve.VR;
#endif

/// <summary>
/// Extended version of ZEDControllerTracker that also checks for several inputs in a generic way. 
/// You can check a state with 
/// Used because input methods vary a lot between controllers and between SteamVR (new and old) and Oculus.
/// See base class ZEDControllerTracker for any code that don't directly relate to inputs. 
/// </summary>
public class ZEDControllerTracker_DemoInputs : ZEDControllerTracker
{
    //#if ZED_STEAM_VR
#if ZED_SVR_2_0_INPUT
    /// !! On v2.0, Steam VR action bindings must be done in the inspector ro once steam.initialize(true) has been called !! 
    /// <summary>
    /// SteamVR action to cause a Fire event when checked or subscribed to.
    /// </summary>
    [Header("SteamVR Plugin 2.0 Bindings")]
    [Tooltip("SteamVR action to cause a Fire event when checked or subscribed to.")]
    public SteamVR_Action_Boolean fireBinding;// = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Fire");
    /// <summary>
    /// SteamVR action to cause a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("SteamVR action to cause a Click event when checked or subscribed to.")]
    public SteamVR_Action_Boolean clickBinding;// = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Click");
    /// <summary>
    /// SteamVR action to cause a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("SteamVR action to cause a Back event when checked or subscribed to.")]
    public SteamVR_Action_Boolean backBinding;// = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Back");
    /// <summary>
    /// SteamVR action to cause a Grab event when checked or subscribed to.
    /// </summary>
    [Tooltip("SteamVR action to cause a Grab event when checked or subscribed to.")]
    public SteamVR_Action_Boolean grabBinding;// = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Grab");
    /// <summary>
    /// SteamVR action to cause a Vector2 UI navigation event when checked or subscribed to.
    /// </summary>
    [Tooltip("SteamVR action to cause a UI navigation event when checked or subscribed to.")]
    public SteamVR_Action_Vector2 navigateUIBinding;// = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("NavigateUI");


#elif ZED_STEAM_VR
    /// <summary>
    /// Legacy SteamVR button to cause a Fire event when checked or subscribed to.
    /// </summary>
    [Header("SteamVR Legacy Input Bindings")]
    [Tooltip("Legacy SteamVR button to cause a Fire event when checked or subscribed to.")]
    public EVRButtonId fireBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR button to cause a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Click event when checked or subscribed to.")]
    public EVRButtonId clickBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR button to cause a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Back event when checked or subscribed to.")]
    public EVRButtonId backBinding_Legacy = EVRButtonId.k_EButton_Grip;
    /// <summary>
    /// Legacy SteamVR button to cause a Grip event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Grip event when checked or subscribed to.")]
    public EVRButtonId grabBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR axis to cause a Vector2 Navigate UI event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Vector2 Navigate UI event when checked or subscribed to.")]
    public EVRButtonId navigateUIBinding_Legacy = EVRButtonId.k_EButton_Axis0;
#endif

#if ZED_OCULUS
    /// <summary>
    /// Oculus Button checked to signal a Fire event when checked or subscribed to.
    /// </summary>
    [Header("Oculus Input Bindings")]
    [Tooltip("Oculus Button checked to signal a Fire event when checked or subscribed to")]
    public OVRInput.Button fireButton = OVRInput.Button.PrimaryIndexTrigger;
    /// <summary>
    /// Oculus Button checked to signal a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Click event when checked or subscribed to")]
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;
    /// <summary>
    /// Oculus Button checked to signal a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Back event when checked or subscribed to")]
    public OVRInput.Button backButton = OVRInput.Button.Two; //Y, or B if just right controller is connected. 
    /// <summary>
    /// Oculus Button checked to signal a Grab event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Grab event when checked or subscribed to")]
    public OVRInput.Button grabButton = OVRInput.Button.PrimaryHandTrigger; 
    /// <summary>
    /// Oculus Button checked to signal a Vector2 UI navigation event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Vector2 UI navigation event when checked or subscribed to")]
    public OVRInput.Axis2D navigateUIAxis = OVRInput.Axis2D.PrimaryThumbstick;

    public static bool ovrUpdateCalledThisFrame = false;
#endif

    /// <summary>
    /// Events called when the Fire button/action was just pressed. 
    /// </summary>
    [Header("Events")]
    [Space(5)]
    [Tooltip("Events called when the Fire button/action was just pressed.")]
    public UnityEvent onFireDown;
    /// <summary>
    /// Events called when the Fire button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Fire button/action was just released.")]
    public UnityEvent onFireUp;
    /// <summary>
    /// Events called when the Click button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Click button/action was just pressed.")]
    public UnityEvent onClickDown;
    /// <summary>
    /// Events called when the Click button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Click button/action was just released.")]
    public UnityEvent onClickUp;
    /// <summary>
    /// Events called when the Back button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Back button/action was just pressed.")]
    public UnityEvent onBackDown;
    /// <summary>
    /// Events called when the Back button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Back button/action was just released.")]
    public UnityEvent onBackUp;
    /// <summary>
    /// Events called when the Grab button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Grab button/action was just pressed.")]
    public UnityEvent onGrabDown;
    /// <summary>
    /// Events called when the Grab button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Grab button/action was just released.")]
    public UnityEvent onGrabUp;

    /// <summary>
    /// Returns if the Fire button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckFireButton(ControllerButtonState state)
    {
#if ZED_SVR_2_0_INPUT
        return CheckSteamVRBoolActionState(fireBinding, state);
#elif ZED_STEAM_VR
        return CheckSteamVRButtonState_Legacy(fireBinding_Legacy, state);
#endif

#if ZED_OCULUS
        return CheckOculusButtonState(fireButton, state);
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Click button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckClickButton(ControllerButtonState state)
    {
#if ZED_SVR_2_0_INPUT
        return CheckSteamVRBoolActionState(clickBinding, state);
#elif ZED_STEAM_VR
        return CheckSteamVRButtonState_Legacy(clickBinding_Legacy, state);
#endif

#if ZED_OCULUS
        return CheckOculusButtonState(clickButton, state);
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Back button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckBackButton(ControllerButtonState state)
    {
#if ZED_SVR_2_0_INPUT
        return CheckSteamVRBoolActionState(backBinding, state);
#elif ZED_STEAM_VR
        return CheckSteamVRButtonState_Legacy(backBinding_Legacy, state);
#endif
#if ZED_OCULUS
        return CheckOculusButtonState(backButton, state);
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Grab button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckGrabButton(ControllerButtonState state)
    {
#if ZED_SVR_2_0_INPUT
        return CheckSteamVRBoolActionState(grabBinding, state);
#elif ZED_STEAM_VR
        return CheckSteamVRButtonState_Legacy(grabBinding_Legacy, state);
#endif
#if ZED_OCULUS
        return CheckOculusButtonState(grabButton, state);
#endif
        return false;
    }

    /// <summary>
    /// Returns the current 2D axis value of the NavigateUIAxis button/action. 
    /// </summary>
    public Vector2 CheckNavigateUIAxis()
    {
#if ZED_SVR_2_0_INPUT
        return CheckSteamVR2DAxis(navigateUIBinding);
#elif ZED_STEAM_VR
        return CheckSteamVRAxis_Legacy(navigateUIBinding_Legacy);
#endif

#if ZED_OCULUS
        return CheckOculus2DAxisState(navigateUIAxis);
#endif
        return Vector3.zero;
    }

    protected override void Awake()
    {
        base.Awake();

#if ZED_SVR_2_0_INPUT
        if (!useLegacySteamVRInput)
        {
            if(!SteamVR.active) SteamVR.Initialize(true); //Force SteamVR to activate, so we can use the input system. 

            //script binding example
            //fireBinding = SteamVR_Input._default.inActions.GrabGrip; //...
        }
#endif
    }

    protected override void Update()
    {
        base.Update();

        if (CheckClickButton(ControllerButtonState.Down)) onClickDown.Invoke();
        if (CheckClickButton(ControllerButtonState.Up)) onClickUp.Invoke();
        if (CheckFireButton(ControllerButtonState.Down)) onFireDown.Invoke();
        if (CheckFireButton(ControllerButtonState.Up)) onFireUp.Invoke();
        if (CheckBackButton(ControllerButtonState.Down)) onBackDown.Invoke();
        if (CheckBackButton(ControllerButtonState.Up)) onBackUp.Invoke();
        if (CheckGrabButton(ControllerButtonState.Down)) onGrabDown.Invoke();
        if (CheckGrabButton(ControllerButtonState.Up)) onGrabUp.Invoke();
    }

    protected void LateUpdate()
    {
#if ZED_OCULUS
        ovrUpdateCalledThisFrame = false;
#endif
    }

#if ZED_STEAM_VR
    protected override void UpdateControllerState()
    {
        base.UpdateControllerState();

        //If using legacy SteamVR input, we check buttons directly from the OpenVR API. 
#if ZED_SVR_2_0_INPUT //If using SteamVR plugin 2.0 or higher, give the option to use legacy input. 
        if (useLegacySteamVRInput)
        {
            openvrsystem.GetControllerState((uint)index, ref controllerstate, controllerstatesize);
        }
#else //We're using an older SteamVR plugin, so we need to use the legacy input. 
        openvrsystem.GetControllerState((uint)index, ref controllerstate, controllerstatesize);
#endif
    }
#endif

#if ZED_OCULUS
    /// <summary>
    /// Checks the button state of a given Oculus button.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckOculusButtonState(OVRInput.Button button, ControllerButtonState state)
    {
        if (!ovrUpdateCalledThisFrame)
        {
            OVRInput.Update();
            ovrUpdateCalledThisFrame = true;
        }

        bool result = false;
        switch (state)
        {
            case ControllerButtonState.Down:
                result = OVRInput.GetDown(button, GetOculusController());
                break;
            case ControllerButtonState.Held:
                result = OVRInput.Get(button, GetOculusController());
                break;
            case ControllerButtonState.Up:
                result = OVRInput.GetUp(button, GetOculusController());
                break;
        }
        return result;
    }

    /// <summary>
    /// Returns the axis of a given Oculus axis button/joystick. 
    /// </summary>
    public Vector3 CheckOculus2DAxisState(OVRInput.Axis2D axis)
    {
        if (!ovrUpdateCalledThisFrame)
        {
            OVRInput.Update();
            ovrUpdateCalledThisFrame = true;
        }

        return OVRInput.Get(axis, GetOculusController());
    }

    /// <summary>
    /// Returns the Oculus controller script of the controller currently attached to this object. 
    /// </summary>
    public OVRInput.Controller GetOculusController()
    {
        if (deviceToTrack == Devices.LeftController) return OVRInput.Controller.LTouch;
        else if (deviceToTrack == Devices.RightController) return OVRInput.Controller.RTouch;
        else return OVRInput.Controller.None;
    }

#endif


    //#if ZED_STEAM_VR
#if ZED_SVR_2_0_INPUT
    /// <summary>
    /// Checks the button state of a given SteamVR boolean action.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    protected bool CheckSteamVRBoolActionState(SteamVR_Action_Boolean action, ControllerButtonState buttonstate)
    {
        switch(buttonstate)
        {
            case ControllerButtonState.Down:
                return action.GetLastStateDown(GetSteamVRInputSource());
            case ControllerButtonState.Held:
                return action.GetLastState(GetSteamVRInputSource());
            case ControllerButtonState.Up:
                return action.GetLastStateUp(GetSteamVRInputSource());
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns the axis of a given SteamVR 2D action. 
    /// </summary>
    protected Vector2 CheckSteamVR2DAxis(SteamVR_Action_Vector2 action)
    {
        return action.GetAxis(GetSteamVRInputSource());
    }

    public SteamVR_Input_Sources GetSteamVRInputSource()
    {
        if (deviceToTrack == Devices.LeftController) return SteamVR_Input_Sources.LeftHand;
        else if (deviceToTrack == Devices.RightController) return SteamVR_Input_Sources.RightHand;
        else return SteamVR_Input_Sources.Any;
    }
#elif ZED_STEAM_VR
        public bool CheckSteamVRButtonState_Legacy(EVRButtonId button, ControllerButtonState state)
    {
        switch(state)
        {
            case ControllerButtonState.Down:
                return GetVRButtonDown_Legacy(button);
            case ControllerButtonState.Held:
            default:
                return GetVRButtonHeld_Legacy(button);
            case ControllerButtonState.Up:
                return GetVRButtonReleased_Legacy(button);
        }
    }

    /// <summary>
    /// Returns if the VR controller button with the given ID was pressed for the first time this frame. 
    /// </summary>
    /// <param name="buttonid">EVR ID of the button as listed in OpenVR.</param>
    public bool GetVRButtonDown_Legacy(EVRButtonId buttonid)
    {
        if (openvrsystem == null) return false; //If VR isn't running, we can't check. 

        bool washeldlastupdate = (lastcontrollerstate.ulButtonPressed & (1UL << (int)buttonid)) > 0L;
        if (washeldlastupdate == true) return false; //If the key was held last check, it can't be pressed for the first time now. 

        bool isheld = (controllerstate.ulButtonPressed & (1UL << (int)buttonid)) > 0L;
        return isheld; //If we got here, we know it was not down last frame. 

    }

    /// <summary>
    /// Returns if the VR controller button with the given ID is currently held. 
    /// </summary>
    /// <param name="buttonid">EVR ID of the button as listed in OpenVR.</param>
    public bool GetVRButtonHeld_Legacy(EVRButtonId buttonid)
    {
        if (openvrsystem == null) return false; //If VR isn't running, we can't check. 

        bool isheld = (controllerstate.ulButtonPressed & (1UL << (int)buttonid)) > 0L;
        return isheld;
    }

    /// <summary>
    /// Returns if the VR controller button with the given ID was held last frame, but released this frame. 
    /// </summary>
    /// <param name="buttonid">EVR ID of the button as listed in OpenVR.</param>
    public bool GetVRButtonReleased_Legacy(EVRButtonId buttonid)
    {
        if (openvrsystem == null) return false; //If VR isn't running, we can't check. 

        bool washeldlastupdate = (lastcontrollerstate.ulButtonPressed & (1UL << (int)buttonid)) > 0L;
        if (washeldlastupdate == false) return false; //If the key was held last check, it can't be released now. 

        bool isheld = (controllerstate.ulButtonPressed & (1UL << (int)buttonid)) > 0L;
        return !isheld; //If we got here, we know it was not up last frame. 
    }

    /// <summary>
    /// Returns the value of an axis with the provided ID. 
    /// Note that for single-value axes, the relevant value will be the X in the returned Vector2 (the Y is unused). 
    /// </summary>
    /// <param name="buttonid"></param>
    public Vector2 CheckSteamVRAxis_Legacy(EVRButtonId buttonid)
    {
        //Convert the EVRButtonID enum to the axis number and check if it's not an axis. 
        uint axis = (uint)buttonid - (uint)EVRButtonId.k_EButton_Axis0;
        if (axis < 0 || axis > 4)
        {
            Debug.LogError("Called GetAxis with " + buttonid + ", which is not an axis.");
            return Vector2.zero;
        }

        switch (axis)
        {
            case 0: return new Vector2(controllerstate.rAxis0.x, controllerstate.rAxis0.y);
            case 1: return new Vector2(controllerstate.rAxis1.x, controllerstate.rAxis1.y);
            case 2: return new Vector2(controllerstate.rAxis2.x, controllerstate.rAxis2.y);
            case 3: return new Vector2(controllerstate.rAxis3.x, controllerstate.rAxis3.y);
            case 4: return new Vector2(controllerstate.rAxis4.x, controllerstate.rAxis4.y);
            default: return Vector2.zero;
        }
    }

#endif
}

/// <summary>
/// List of possible button states, used to check inputs. 
/// </summary>
public enum ControllerButtonState
{
    /// <summary>
    /// The button was pressed this frame. 
    /// </summary>
    Down,
    /// <summary>
    /// The button is being held down - it doesn't matter which frame it started being held. 
    /// </summary>
    Held,
    /// <summary>
    /// The button was released this frame. 
    /// </summary>
    Up
}