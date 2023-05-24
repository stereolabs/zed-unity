using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

/// <summary>
/// Extended version of ZEDControllerTracker that also checks for several inputs in a generic way.
/// You can check a state with
/// Used because input methods vary a lot between controllers and between SteamVR (new and old) and Oculus.
/// See base class ZEDControllerTracker for any code that don't directly relate to inputs.
/// </summary>
public class ZEDControllerTracker_DemoInputs : ZEDControllerTracker
{
    /// <summary>
    /// Input Button checked to signal a Fire event when checked or subscribed to.
    /// </summary>
    [Header("Input Bindings")]
    [Tooltip("Input Button checked to signal a Fire event when checked or subscribed to")]
    public InputFeatureUsage<bool> fireButton = CommonUsages.triggerButton;
    /// <summary>
    /// Input Button checked to signal a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Click event when checked or subscribed to")]
    public InputFeatureUsage<bool> clickButton = CommonUsages.triggerButton;
    /// <summary>
    /// Input Button checked to signal a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Back event when checked or subscribed to")]
    public InputFeatureUsage<bool> backButton = CommonUsages.secondaryButton; //Y, or B if just right controller is connected. 
    /// <summary>
    /// Input Button checked to signal a Grab event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Grab event when checked or subscribed to")]
    public InputFeatureUsage<bool> grabButton = CommonUsages.gripButton;
    /// <summary>
    /// Input Button checked to signal a Vector2 UI navigation event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Vector2 UI navigation event when checked or subscribed to")]
    public InputFeatureUsage<Vector2> navigateUIAxis = CommonUsages.primary2DAxis;

    private bool fireActive = false;
    private bool clickActive = false;
    private bool backActive = false;
    private bool grabActive = false;

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
        return CheckButtonState(fireButton, state, fireActive);
    }

    /// <summary>
    /// Returns if the Click button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckClickButton(ControllerButtonState state)
    {
        return CheckButtonState(clickButton, state, clickActive);
    }

    /// <summary>
    /// Returns if the Back button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckBackButton(ControllerButtonState state)
    {
        return CheckButtonState(backButton, state, backActive);
    }

    /// <summary>
    /// Returns if the Grab button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckGrabButton(ControllerButtonState state)
    {
        return CheckButtonState(grabButton, state, grabActive);
    }

    /// <summary>
    /// Returns the current 2D axis value of the NavigateUIAxis button/action.
    /// </summary>
    public Vector2 CheckNavigateUIAxis()
    {
        return Check2DAxisState(navigateUIAxis);
    }

    protected override void Awake()
    {
        base.Awake();
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

    }
    
    public bool CheckButtonState(InputFeatureUsage<bool> button, ControllerButtonState state, bool isActive){

        bool down = false;
        bool up = false;
        InputDevice device = new InputDevice();

        if (deviceToTrack == Devices.LeftController)
            device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        else device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        ProcessInputDeviceButton(device, button, ref isActive,
            () => // On Button Down
            {
                down = true;
            },
            () => // On Button Up
            {
                up =  true;
        });

        if (state == ControllerButtonState.Down) return down;
        if (state == ControllerButtonState.Up) return up;
        else return false;
    }

    public Vector2 Check2DAxisState(InputFeatureUsage<Vector2> navigateUIAxis){

        InputDevice device = new InputDevice();

        if (deviceToTrack == Devices.LeftController)
            device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        else device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        Vector2 result = Vector2.zero;
        if (device.TryGetFeatureValue(navigateUIAxis, out Vector2 value))
            result = value;
            
        return result;
    }

    private void ProcessInputDeviceButton(InputDevice inputDevice, InputFeatureUsage<bool> button, ref bool _wasPressedDownPreviousFrame, Action onButtonDown = null, Action onButtonUp = null, Action onButtonHeld = null)
    {
        if (inputDevice.TryGetFeatureValue(button, out bool isPressed) && isPressed)
        {
            if (!_wasPressedDownPreviousFrame) // // this is button down
            {
                onButtonDown?.Invoke();
            }
 
            _wasPressedDownPreviousFrame = true;
            onButtonHeld?.Invoke();
        }
        else
        {
            if (_wasPressedDownPreviousFrame) // this is button up
            {
                onButtonUp?.Invoke();
            }
 
            _wasPressedDownPreviousFrame = false;
        }
    }
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
