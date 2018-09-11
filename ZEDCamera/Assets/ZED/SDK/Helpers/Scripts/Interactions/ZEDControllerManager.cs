//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//       ##DEPRECATED


using UnityEngine;

/// <summary>
/// Interface for handling SteamVR and Oculus tracked controllers in the same way.
/// Implemented in ZEDSteamVRControllerManager and ZEDOculusControllerManager. 
/// </summary>
public interface ZEDControllerManager
{
    /// <summary>
    /// Whether controllers have been initialized. 
    /// </summary>
	bool PadsAreInit { get;}

    /// <summary>
    /// Checks if a button is down.
    /// </summary>
    /// <param name="button">Button to check.</param>
    /// <param name="controllerid">ID of the controller to check.</param>
    /// <returns></returns>
    bool GetDown(sl.CONTROLS_BUTTON button, int controllerid = -1);

    /// <summary>
    /// Checks if a trigger is down.
    /// </summary>
    /// <param name="button">Trigger to check.</param>
    /// <param name="controllerID">ID of the controller to check.</param>
    /// <returns></returns>
    float GetHairTrigger(sl.CONTROLS_AXIS1D button, int controllerID = -1);

    /// <summary>
    /// Gets the ID of the right controller.
    /// </summary>
    /// <returns></returns>
    int GetRightIndex();

    /// <summary>
    /// Gets the ID of the left controller.
    /// </summary>
    /// <returns></returns>
    int GetLeftIndex();

    /// <summary>
    /// Gets the local position of a controller.
    /// </summary>
    /// <param name="IDPad"></param>
    /// <returns></returns>
    Vector3 GetPosition(int IDPad);

    /// <summary>
    /// Loads the index of a controller according to files created from the ZED calibration tool.
    /// </summary>
    /// <param name="path"></param>
    void LoadIndex(string path);

    /// <summary>
    /// Gets the index of the current ZEDHolder object. 
    /// </summary>
    int ControllerIndexZEDHolder { get; }
}

namespace sl
{
    /// <summary>
    /// VR controller button press sources. 
    /// </summary>
    public enum CONTROLS_BUTTON
    {
        THREE,
        ONE,
        PRIMARY_THUMBSTICK,
        SECONDARY_THUMBSTICK
    }

    /// <summary>
    /// VR controller trackpad/analog stick movement sources. 
    /// </summary>
    public enum CONTROLS_AXIS2D
    {
        PRIMARY_THUBMSTICK,
        SECONDARY_THUMBSTICK
    }

    /// <summary>
    /// VR controller trigger movement sources. 
    /// </summary>
    public enum CONTROLS_AXIS1D
    {
        PRIMARY_INDEX_TRIGGER,
        SECONDARY_INDEX_TRIGGER,
        PRIMARY_HAND_TRIGGER,
        SECONDARY_HAND_TRIGGER
    }
}
