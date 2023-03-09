using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour
{
    #region vars

    private ZEDSkeletonAnimator zedSkeletonAnimator = null;

    [Header("Main settings")]

    [Tooltip("Height offset applied to transform each frame.")]
    public Vector3 manualOffset = Vector3.zero;

    [Tooltip("Automatic offset adjustment: Finds an automatic offset that sets both feet above ground, and at least one foot on the ground.")]
    public bool automaticOffset = false;

    [Tooltip("Number of camera grabs on which both feet are visible that will be used to stabilize the automatic offset." +
        "\nThis is technically a countdown at the end of which the automatic offset will be set.")]
    public int nbCalibrationFrames = 150;
    private int curCalibrationFrames = -1;
    private float currentheightOffset = 0f;

    [Tooltip("This value is used to store the minimal offset when both feet are above  the ground, to allow jumping." +
        "\nThe value used for the offset is the minimal distance between the feet and the ground stored in the list of size \"buffersize\"." +
        "\nThis list is a queue update every frame (not every grab).")]
    public int bufferSize = 120;
    private LinkedList<float> feetOffsetBuffer = new LinkedList<float>();

    [Header("Finding the ground")]

    [Tooltip("Max height difference between feet and floor that will trigger the application of the offset." +
        "\nIf the floor is further than this value, above or under, the height will not be offset.")]
    public float findFloorDistance = 2f;
    [Tooltip("Which layers to use when searching the floor above or under.")]
    public LayerMask layersToHit;

    [Header("Keyboard controls")]
    public KeyCode increaseOffsetKey = KeyCode.UpArrow;
    public KeyCode decreaseOffsetKey = KeyCode.DownArrow;
    [Tooltip("Toggle Manual/Automatic offset.")]
    public KeyCode toggleAutomaticOffsetKey = KeyCode.O;
    [Tooltip("Step in increase/decrease of offset.")]
    public float offsetStep = 0.1f;

    private readonly float feetAlpha = .1f;

#endregion

    public int CurCalibrationFrames { get => curCalibrationFrames; set => curCalibrationFrames = value; }
    public float CurrentheightOffset { get => currentheightOffset; set => currentheightOffset = value; }

    private void Start()
    {
        zedSkeletonAnimator = GetComponent<ZEDSkeletonAnimator>();
    }

    /// <summary>
    /// Find an automatic offset that sets both feet above ground, and at least one foot on the ground.
    /// Uses the HeightOffsetStabilized bool value from the ZEDSkeletonAnimator to know when to stop and settle on the current offset.
    /// </summary>
    /// <param name="confFootL">Current confidence for the left foot.</param>
    /// <param name="confFootR">Current confidence for the right foot.</param>
    /// <param name="lastPosFootL">Position of the left foot at the end of last frame.</param>
    /// <param name="lastPosFootR">Position of the right foot at the end of last frame.</param>
    /// <param name="ankleHeightOffset">Height (Y) difference between the "Foot" bone of the animator and the sole of the foot.</param>
    public Vector3 ComputeRootHeightOffsetXFrames(float confFootL, float confFootR, Vector3 lastPosFootL, Vector3 lastPosFootR, float ankleHeightOffset)
    {
        Vector3 offsetToApply = new Vector3(0, CurrentheightOffset, 0);

        if (automaticOffset)
        {
            // if both feet are visible/detected, attempt to correct the height of the skeleton's root
            if (!float.IsNaN(confFootL) && confFootL > 0 && !float.IsNaN(confFootR) && confFootR > 0 && !zedSkeletonAnimator.HeightOffsetStabilized)
            {
                Ray rayL = new Ray(lastPosFootL + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitL = Physics.Raycast(rayL, out RaycastHit hitL, findFloorDistance * 2, layersToHit);
                Ray rayR = new Ray(lastPosFootR + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitR = Physics.Raycast(rayR, out RaycastHit hitR, findFloorDistance * 2, layersToHit);

                float footFloorDistanceL = 0;
                float footFloorDistanceR = 0;

                //// "Oriented distance" between the soles and the ground (can be negative)
                if (rayUnderFootHitL) { footFloorDistanceL = (lastPosFootL.y - ankleHeightOffset - CurrentheightOffset) - hitL.point.y; }
                if (rayUnderFootHitR) { footFloorDistanceR = (lastPosFootR.y - ankleHeightOffset - CurrentheightOffset) - hitR.point.y; }

                float minFootFloorDistance = 0;
                float thresholdHeight = -1 * CurrentheightOffset;

                // If both feet are under the ground, use the max value instead of the min value.
                if (footFloorDistanceL < 0 && footFloorDistanceR < 0)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    CurrentheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * CurrentheightOffset;
                }
                else if (footFloorDistanceL >= 0 && footFloorDistanceR >= 0)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));

                    // The feet offset is added in the buffer of size "bufferSize". If the buffer is already full, remove the oldest value (the first)
                    if (feetOffsetBuffer.Count >= bufferSize)
                    {
                        feetOffsetBuffer.RemoveFirst();
                    }
                    feetOffsetBuffer.AddLast(minFootFloorDistance);

                    // Continuous adjustment: The feet offset is the min element of this buffer.
                    minFootFloorDistance = -1 * MinOfLinkedList(ref feetOffsetBuffer);
                    CurrentheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * CurrentheightOffset;
                }
                else
                {
                    minFootFloorDistance = -1 * Mathf.Min(footFloorDistanceL, footFloorDistanceR);
                    CurrentheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * CurrentheightOffset;
                }
            }
            offsetToApply = new Vector3(0, CurrentheightOffset, 0);
        }
        else
        {
            offsetToApply = manualOffset;
        }

        return offsetToApply;
    }

    /// <summary>
    /// Sets the manual offset as height offset.
    /// </summary>
    public void ComputeRootHeightOffset()
    {
        zedSkeletonAnimator.RootHeightOffset = manualOffset;
    }

    /// <summary>
    /// Utility function to find the minimum value in a buffer.
    /// </summary>
    /// <param name="buf">The buffer.</param>
    /// <returns>The minimal value.</returns>
    private float MinOfLinkedList(ref LinkedList<float> buf)
    {
        float min = float.MaxValue;

        foreach (float e in buf)
        {
            if (e < min)
                min = e;
        }
        return min;
    }

    /// <summary>
    /// Manage offset with keyboard.
    /// </summary>
    public void Update()
    {
        if (Input.GetKeyDown(increaseOffsetKey))
        {
            manualOffset.y += offsetStep;
        }
        else if (Input.GetKeyDown(decreaseOffsetKey))
        {
            manualOffset.y -= offsetStep;
        }

        if (Input.GetKeyDown(toggleAutomaticOffsetKey))
        {
            automaticOffset = !automaticOffset;
        }
    }
}
