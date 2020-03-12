using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Switches between manual and automatic calibration mode, enabling and disabling objects as specified.
/// </summary>
public class CalibModeSwitcher : MonoBehaviour
{
    /// <summary>
    /// Objects that will only be enabled when in manual mode, and disabled otherwise. 
    /// </summary>
    [Tooltip("Objects that will only be enabled when in manual mode, and disabled otherwise. ")]
    public List<GameObject> enabledInManualMode = new List<GameObject>();
    /// <summary>
    /// Objects that will only be enabled when in automatic mode, and disabled otherwise. 
    /// </summary>
    [Tooltip("Objects that will only be enabled when in automatic mode, and disabled otherwise. ")]
    public List<GameObject> enabledInAutomaticMode = new List<GameObject>();

    /// <summary>
    /// Methods called when switching to manual mode. 
    /// </summary>
    [Space(5)]
    [Tooltip("Methods called when switching to manual mode. ")]
    public UnityEvent OnManualModeEntered;
    /// <summary>
    /// Methods called when switching to automatic mode. 
    /// </summary>
    [Tooltip("Methods called when switching to automatic mode. ")]
    public UnityEvent OnAutomaticModeEntered;

    /// <summary>
    /// The mode options, set by SetCalibrationMode.
    /// </summary>
    public enum MRCalibrationMode
    {
        Manual = 0,
        Automatic = 1
    }

    /// <summary>
    /// Changes the calibration mode to the one provided by enabling/disabling objecs as needed
    /// and calling relevant events. 
    /// Override that takes an int, so that it can be set via Unity's Inspector. 0 = Manual, 1 = Automatic.
    /// </summary>
    public void SetCalibrationMode(int newmode)
    {
        SetCalibrationMode((MRCalibrationMode)newmode);
    }

    /// <summary>
    /// Changes the calibration mode to the one provided by enabling/disabling objecs as needed
    /// and calling relevant events. 
    /// </summary>
    public void SetCalibrationMode(MRCalibrationMode newmode)
    {
        foreach(GameObject mobj in enabledInManualMode)
        {
            mobj.SetActive(newmode == MRCalibrationMode.Manual);
        }

        foreach(GameObject aobj in enabledInAutomaticMode)
        {
            aobj.SetActive(newmode == MRCalibrationMode.Automatic);
        }

        if (newmode == MRCalibrationMode.Manual) OnManualModeEntered.Invoke();
        else if (newmode == MRCalibrationMode.Automatic) OnAutomaticModeEntered.Invoke();
    }
}
