using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the ZED camera's offset from its tracked object within the MR calibration scene. 
/// Provides interfaces for translating/rotating gradually or instantly, along with related
/// functions like undo/redo and loading/saving. 
/// </summary>
public class CameraAnchor : MonoBehaviour
{
    /// <summary>
    /// The scene's ZEDManager instance. Should be a child of this object's transform. 
    /// If not assigned, will be set to the ZEDManager for camera index 1. 
    /// </summary>
    [Tooltip("The scene's ZEDManager instance. Should be a child of this object's transform. " +
        "If not assigned, will be set to the ZEDManager for camera index 1.")]
    public ZEDManager zedManager;

    /// <summary>
    /// ZEDControllerTracker assigned to this object. That's what keeps the object's position in sync with the real-world object. 
    /// </summary>
    [HideInInspector]
    public ZEDControllerTracker controllerTracker;

    /// <summary>
    /// Max speed the ZED will move when calling TranslateZEDIncrementally(). This happens when using the manual translation arrows. 
    /// </summary>
    [Space(5)]
    [Tooltip("Max speed the ZED will move when calling TranslateZEDIncrementally(). This happens when using the manual translation arrows.")]
    public float maxTranslateMPS = 0.05f;
    /// <summary>
    /// Max speed the ZED will move when calling RotateZEDIncrementally(). This happens when using the manual rotation rings. 
    /// </summary>
    [Tooltip("Max speed the ZED will move when calling RotateZEDIncrementally(). This happens when using the manual rotation rings.")]
    public float maxRotateDPS = 30f;

    /// <summary>
    /// Delegate that provides a reference for a CameraAnchor, indended to be to this one. 
    /// </summary>
    public delegate void CameraAnchorCreatedDelegate(CameraAnchor anchor);
    /// <summary>
    /// Event called in Start(), confirming that the anchor is ready to be used. 
    /// </summary>
    public static event CameraAnchorCreatedDelegate OnCameraAnchorCreated;

    /// <summary>
    /// Index of the layer hidden from the ZED camera, preventing it from being visible in the 2D view. 
    /// This is NOT enforced in the camera itself via script; you must exclude it from the camera's layer mask manually. 
    /// </summary>
    public const int HIDE_FROM_ZED_LAYER = 17;
    /// <summary>
    /// File path to where the app loads and saves the calibrated offset values - the whole point of the MR calibration scene existing. 
    /// </summary>
    private string filePath
    {
        get
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string specificFolder = System.IO.Path.Combine(folder, @"Stereolabs\steamvr\ZED_Position_Offset.conf");
            return specificFolder;
        }
    }

    /// <summary>
    /// How many undo/redo poses to save before we start discarding them. 
    /// </summary>
    private const int MAX_UNDO_HISTORY = 50;

    private CappedStack<AnchorPose> undoStack = new CappedStack<AnchorPose>(MAX_UNDO_HISTORY);
    private CappedStack<AnchorPose> redoStack = new CappedStack<AnchorPose>(MAX_UNDO_HISTORY);

    //When we call TranslateZEDIncrementally or the Rotate version, we only want to register an undoable pose
    //when you first call it. So we have a timer we start after registering the pose that blocks re-registering
    //until neither incremental function has been called for MIN_TIME_ect seconds. 
    private const float MIN_TIME_BETWEEN_INCREMENTAL_UNDOS = 0.25f;
    private float incrementalUndoTimer = 0f;
    private bool canRegisterIncrementalUndo
    {
        get
        {
            return incrementalUndoTimer <= 0f;
        }
    }

    /// <summary>
    /// Get necessary references and handle startup events. 
    /// </summary>
    void Start()
    {
        if (!controllerTracker) controllerTracker = GetComponent<ZEDControllerTracker>();
        if (!zedManager) zedManager = controllerTracker.zedManager;

        zedManager.OnZEDReady += LoadCalibFile;

        //Dispatch event that this anchor is ready. 
        if (OnCameraAnchorCreated != null) OnCameraAnchorCreated.Invoke(this);

    }

    /// <summary>
    /// Sets the ZED position and rotation to the specified values, instantly. 
    /// Also stores the pose for undoing later. 
    /// </summary>
    public void SetNewZEDPose(Vector3 localpos, Quaternion localrot)
    {
        RegisterUndoablePose();

        zedManager.transform.localPosition = localpos;
        zedManager.transform.localRotation = localrot;
    }

    /// <summary>
    /// Adds the specified translation and rotation to the current ZED transform values. 
    /// </summary>
    /// <param name="posoffset">Direction and amount to translate the ZED.</param>
    /// <param name="rotoffset">Direction and amount to rotate the ZED.</param>
    /// <param name="uselocal">If true, applies to localPosition/localRotation. Otherwise, applies to world-space values.</param>
    public void MoveZEDPose(Vector3 posoffset, Quaternion rotoffset, bool uselocal)
    {
        RegisterUndoablePose();

        if (uselocal) //Local space. 
        {
            zedManager.transform.localPosition += posoffset;
            zedManager.transform.localRotation = rotoffset * zedManager.transform.localRotation;
        }
        else //World space. 
        {
            zedManager.transform.position += posoffset;
            zedManager.transform.rotation = rotoffset * zedManager.transform.rotation;
        }
    }

    /// <summary>
    /// Move the ZED in a direction by an amount governed by maxTranslateMPS.
    /// Provided values should be clamped to -1 and 1, representing the multiple of that max speed used to translate. 
    /// For instance, if max speed is 1 meter per second and this is called with 0, 0.5f, 0, for 1 second, it'll 
    /// move 0.5 meters over the course of that second. 
    /// Meant to be called every frame while dragging a control (like the translate arrows) to slide the ZED gradually. 
    /// </summary>
    /// <param name="translation">What percentage (-1 to 1) of the max speed to move the ZED.</param>
    public void TranslateZEDIncrementally(Vector3 translation) //Input should be clamped to -1 and 1. 1 moves in max direction speed. 
    {
        if (canRegisterIncrementalUndo) //Clear to register the undo. 
        {
            RegisterUndoablePose();
            StartCoroutine(BlockIncrementalUndoRegister());
        }
        else //Timer already started. Don't start a new coroutine, but reset the timer. 
        {
            incrementalUndoTimer = MIN_TIME_BETWEEN_INCREMENTAL_UNDOS;
        }

        Vector3 velocity = translation * maxTranslateMPS * Time.deltaTime;
        zedManager.transform.localPosition += zedManager.transform.localRotation * velocity;
    }

    /// <summary>
    /// Rotates the ZED in a direction by an amount governed by maxRotateDPS.
    /// Provided values should be clamped to -1 and 1, representing the multiple of that max speed used to rotate. 
    /// For instance, if max speed is 100 degrees per second, and this is called with 0,0.5f,0 for 1 second, it'll
    /// rotate 50 degrees on the Y axis over the course of that second. 
    /// Meant to be called every frame while dragging a control (like the translate arrows) to slide the ZED gradually.  
    /// </summary>
    /// <param name="rotation"></param>
    public void RotateZEDIncrementally(Vector3 rotation)//Input should be clamped to -1 and 1. 1 moves in max rotation speed. 
    {
        if (canRegisterIncrementalUndo) //Clear to register the undo. 
        {
            RegisterUndoablePose();
            StartCoroutine(BlockIncrementalUndoRegister());
        }
        else //Timer already started. Don't start a new coroutine, but reset the timer. 
        {
            incrementalUndoTimer = MIN_TIME_BETWEEN_INCREMENTAL_UNDOS;
        }

        Vector3 angvelocity = rotation * maxRotateDPS * Time.deltaTime;
        zedManager.transform.localRotation *= Quaternion.Euler(angvelocity);
        //zedManager.transform.localEulerAngles += angvelocity;
    }

    /// <summary>
    /// Temporarily blocks an undoable position from being registered from the 'incremental' move functions. 
    /// This is done so that when you first start sliding/rotating the ZED over time, it only saves the position
    /// in the first frame, and not repeatedly or at any time until you finish thta action. 
    /// </summary>
    private IEnumerator BlockIncrementalUndoRegister()
    {
        incrementalUndoTimer = MAX_UNDO_HISTORY;

        while (incrementalUndoTimer > 0)
        {
            incrementalUndoTimer -= Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Moves the ZED to the top position on the Undo stack, undoing the last change. 
    /// Also adds the current position to the Redo stack. 
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count == 0) return; //No action to undo. 

        AnchorPose undoPose = undoStack.Pop(); //Get last pose from the stack and remove it. 

        //Save the current pose to the redo stack. 
        AnchorPose pose = new AnchorPose(zedManager.transform.localPosition, zedManager.transform.localRotation);
        redoStack.Push(pose);

        //Apply historical pose to the ZED. 
        zedManager.transform.localPosition = undoPose.position;
        zedManager.transform.localRotation = undoPose.rotation;

        //redoStack.Push(undoPose); //Remember what you undid so it can be redone. 
    }

    /// <summary>
    /// Moves the ZED to the top position on the Redo stack, if any, which is added after
    /// the user calls Undo, so long as another Undo is not registered in the meantime. 
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count == 0) return; //No action to redo. 

        AnchorPose redoPose = redoStack.Pop(); //Get last pose from the stack and remove it. 

        //Re-apply that pose to the ZED. 
        zedManager.transform.localPosition = redoPose.position;
        zedManager.transform.localRotation = redoPose.rotation;

        undoStack.Push(redoPose); //Put that action back on the undo stack so you could repeat this process again if you wanted. 
    }

    /// <summary>
    /// Call before the ZED is moved to make it so you can go back to this position by pressing Undo. 
    /// If you are moving it gradually, call before the gradual movement starts and don't update during. 
    /// Also clears the Redo stack as you've now branched away from it. 
    /// </summary>
    private void RegisterUndoablePose()
    {
        RegisterUndoablePose(zedManager.transform.localPosition, zedManager.transform.localRotation);
    }

    /// <summary>
    /// Call before the ZED is moved to make it so you can go back to this position by pressing Undo. 
    /// If you are moving it gradually, call before the gradual movement starts and don't update during. 
    /// Also clears the Redo stack as you've now branched away from it. 
    /// </summary>
    private void RegisterUndoablePose(Vector3 pos, Quaternion rot)
    {
        AnchorPose pose = new AnchorPose(pos, rot);
        undoStack.Push(pose);

        //Clear the Redo stack as we've now branched away from whatever history it had. 
        redoStack.Clear();
    }

    /// <summary>
    /// Saves the ZED's offset from the tracked object as a file to be loaded within Unity or from any ZED application
    /// built to load such a calibration file. 
    /// <para>Destination directory defined by the CALIB_FILE_PATH constant.</para>
    /// </summary>
    public void SaveCalibFile()
    {
        int slashindex = filePath.LastIndexOf('\\');
        string directory = filePath.Substring(0, slashindex);
        print(directory);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            Vector3 localpos = zedManager.transform.localPosition; //Shorthand.
            Vector3 localrot = zedManager.transform.localRotation.eulerAngles; //Shorthand.

            file.WriteLine("x=" + localpos.x);
            file.WriteLine("y=" + localpos.y);
            file.WriteLine("z=" + localpos.z);
            file.WriteLine("rx=" + localrot.x);
            file.WriteLine("ry=" + localrot.y);
            file.WriteLine("rz=" + localrot.z);

#if ZED_STEAM_VR

            string indexstring = "indexController=";
            if (controllerTracker.index > 0)
            {
                var snerror = Valve.VR.ETrackedPropertyError.TrackedProp_Success;
                var snresult = new System.Text.StringBuilder((int)64);
                indexstring += Valve.VR.OpenVR.System.GetStringTrackedDeviceProperty((uint)controllerTracker.index,
                    Valve.VR.ETrackedDeviceProperty.Prop_SerialNumber_String, snresult, 64, ref snerror);
            }
            else
            {
                indexstring += "NONE";
            }

            file.WriteLine(indexstring);
#endif
            file.Close();
            print("Calibration saved to " + filePath);

            MessageDisplay.DisplayTemporaryMessageAll("Saved calibration file to:\r\n" + filePath);
        }
    }

    /// <summary>
    /// Loads a previously-saved calibration file, if any, and applies the position to the ZED. 
    /// Called when the ZED is first initialized, so you left off where you started. 
    /// </summary>
    public void LoadCalibFile()
    {
        if (!System.IO.File.Exists(filePath))
        {
            print("Did not load values as no previously-saved file was found: " + filePath);
            return;
        }

        string[] lines = null;
        try
        {
            lines = System.IO.File.ReadAllLines(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }

        if (lines == null)
        {
            print("Loaded calibration file was empty: " + filePath);
            return;
        }

        Vector3 localpos = Vector3.zero;
        Vector3 localrot = Vector3.zero; //Euler angles. 

        foreach (string line in lines)
        {
            string[] splitline = line.Split('=');
            if (splitline != null && splitline.Length >= 2)
            {
                string key = splitline[0];
                string value = splitline[1].Split(' ')[0].ToLower(); //Removed space after values if present. 

                if (key == "indexController") continue; //We don't need to load this. 

                //We'll parse the field ahead of time for simplicity, but this only works because all needed values are floats. 
                //This needs to be amended if we ever need to load other values. 
                float parsedval = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

                if (key == "x")
                {
                    localpos.x = parsedval;
                }
                else if (key == "y")
                {
                    localpos.y = parsedval;
                }
                else if (key == "z")
                {
                    localpos.z = parsedval;
                }
                else if (key == "rx")
                {
                    localrot.x = parsedval;
                }
                else if (key == "ry")
                {
                    localrot.y = parsedval;
                }
                else if (key == "rz")
                {
                    localrot.z = parsedval;
                }
            }
        }

        zedManager.transform.localPosition = localpos;
        zedManager.transform.localRotation = Quaternion.Euler(localrot);

        print("Loaded past calibration from file: " + filePath);
    }

    /// <summary>
    /// Very simple version of UnityEngine's Pose class, as it doesn't exist in older versions of Unity. 
    /// </summary>
    internal class AnchorPose
    {
        internal Vector3 position;
        internal Quaternion rotation;

        internal AnchorPose(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }

    }
}
