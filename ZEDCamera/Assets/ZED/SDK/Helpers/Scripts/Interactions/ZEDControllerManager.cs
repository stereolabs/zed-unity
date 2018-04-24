//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Overhead to control SteamVR and Oculus in the same way
/// </summary>
namespace sl
{
    /// <summary>
    /// Overhead on the button
    /// </summary>
    public enum CONTROLS_BUTTON
    {
        THREE,
        ONE,
        PRIMARY_THUBMSTICK,
        SECONDARY_THUMBSTICK
    }

    /// <summary>
    /// Overhead on the trackpad and sticks
    /// </summary>
    public enum CONTROLS_AXIS2D
    {
        PRIMARY_THUBMSTICK,
        SECONDARY_THUMBSTICK
    }

    /// <summary>
    /// Overhead on the triggers
    /// </summary>
    public enum CONTROLS_AXIS1D
    {
        PRIMARY_INDEX_TRIGGER,
        SECONDARY_INDEX_TRIGGER,
        PRIMARY_HAND_TRIGGER,
        SECONDARY_HAND_TRIGGER
    }
}

public interface ZEDControllerManager {
   
    /// <summary>
    /// Checks if pads are init
    /// </summary>
	bool PadsAreInit { get;}

    /// <summary>
    /// Checks if a button is down
    /// </summary>
    /// <param name="button"></param>
    /// <param name="idPad"></param>
    /// <returns></returns>
    bool GetDown(sl.CONTROLS_BUTTON button, int idPad = -1);

    /// <summary>
    /// Checks if a trigger is down
    /// </summary>
    /// <param name="button"></param>
    /// <param name="idPad"></param>
    /// <returns></returns>
    float GetHairTrigger(sl.CONTROLS_AXIS1D button, int idPad = -1);

    /// <summary>
    /// Gets the ID of the right controller
    /// </summary>
    /// <returns></returns>
    int GetRightIndex();

    /// <summary>
    /// Gets the ID of the left controller
    /// </summary>
    /// <returns></returns>
    int GetLeftIndex();

    /// <summary>
    /// Get the local position of a controller
    /// </summary>
    /// <param name="IDPad"></param>
    /// <returns></returns>
    Vector3 GetPosition(int IDPad);

    /// <summary>
    /// Load the index according to the calibration tool
    /// </summary>
    /// <param name="path"></param>
    void LoadIndex(string path);

    /// <summary>
    /// Gets the current ZEDHolder
    /// </summary>
    int ControllerIndexZEDHolder { get; }
}
