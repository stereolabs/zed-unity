using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour
{
    #region vars

    [Header("Main settings")]

    ZEDBodyTrackingManager bodyTrackingManager;

    [SerializeField]
    private float currentAutoHeightOffset = 0f;

    [Tooltip("Lenght in sec of the buffer used to compute the automatic offset.")]
    public float bufferSize_Sec = 2.0f;
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

    private readonly float feetAlpha = 1.0f;

    // Threshold to detect height offset error, in meters.
    [SerializeField] private float thresholdDistCloseToFloor = 0.02f;

    // Duration of successive height offset error frames that should trigger a reset of the auto offset, in seconds.
    [SerializeField] private float thresholdDurationOffsetError = 3f;
    private float durationOffsetError = 4f;
    private float lastCallTime = 0f;

    #endregion

    public float CurrentheightOffset { get => currentAutoHeightOffset; set => currentAutoHeightOffset = value; }


    private void Awake()
    {
        bodyTrackingManager = FindObjectOfType<ZEDBodyTrackingManager>();
        if (bodyTrackingManager == null)
        {
            Debug.LogError("ZEDManagerIK: No body tracking manager loaded!");
        }
    }

    /// <summary>
    /// Find an automatic offset that sets both feet above ground, and at least one foot on the ground.
    /// </summary>
    /// <param name="confFootL">Current confidence for the left foot.</param>
    /// <param name="confFootR">Current confidence for the right foot.</param>
    /// <param name="lastPosFootL">Current position of the left foot.</param>
    /// <param name="lastPosFootR">Current position of the right foot.</param>
    /// <param name="ankleHeightOffset">Height (Y) difference between the "Foot" bone of the animator and the sole of the foot.</param>
    public Vector3 ComputeRootHeightOffsetXFrames(float confFootL, float confFootR, Vector3 lastPosFootL, Vector3 lastPosFootR, float ankleHeightOffset)
    {
        Vector3 offsetToApply = new Vector3(0, currentAutoHeightOffset, 0);

        if (bodyTrackingManager.automaticOffset)
        {
            // if both feet are visible/detected, attempt to correct the height of the skeleton's root
            if (!float.IsNaN(confFootL) && confFootL > 0 && !float.IsNaN(confFootR) && confFootR > 0)
            {
                Ray rayL = new Ray(lastPosFootL + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitL = Physics.Raycast(rayL, out RaycastHit hitL, findFloorDistance * 2, layersToHit);
                Ray rayR = new Ray(lastPosFootR + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitR = Physics.Raycast(rayR, out RaycastHit hitR, findFloorDistance * 2, layersToHit);

                float footFloorDistanceL = 0;
                float footFloorDistanceR = 0;

                //// "Oriented distance" between the soles and the ground (can be negative)
                /// Positive is above floor, negative is under floor.
                if (rayUnderFootHitL) { footFloorDistanceL = (lastPosFootL.y - ankleHeightOffset) - hitL.point.y; }
                if (rayUnderFootHitR) { footFloorDistanceR = (lastPosFootR.y - ankleHeightOffset) - hitR.point.y; }

                float minFootFloorDistance;

                // If both feet are under the ground, use the max value instead of the min value.
                if (footFloorDistanceL < 0 && footFloorDistanceR < 0)
                {
                    minFootFloorDistance = -1.0f * Mathf.Max(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    currentAutoHeightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * currentAutoHeightOffset;
                }
                else if (footFloorDistanceL >= 0 && footFloorDistanceR >= 0)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));

                    // The feet offset is added in the buffer of size "bufferSize". If the buffer is already full, remove the oldest value (the first)
                    if (feetOffsetBuffer.Count >= bufferSize_Sec * 1.0 / Time.deltaTime)
                    {
                        feetOffsetBuffer.RemoveFirst();
                    }
                    feetOffsetBuffer.AddLast(minFootFloorDistance);

                    // Continuous adjustment: The feet offset is the min element of this buffer.
                    minFootFloorDistance = /*-1 **/ MinOfLinkedList(ref feetOffsetBuffer);
                    currentAutoHeightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * currentAutoHeightOffset;
                }
                else
                {
                    minFootFloorDistance = /*-1 **/ Mathf.Min(footFloorDistanceL, footFloorDistanceR);
                    currentAutoHeightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * currentAutoHeightOffset;
                }
            }
            offsetToApply = new Vector3(0, currentAutoHeightOffset, 0);
                }
        else
        {
            offsetToApply = bodyTrackingManager.manualOffset;
        }

        return offsetToApply;
            }

    /// <summary>
    /// Find an offset that sets both feet above ground, and at least one foot on the ground.
    /// </summary>
    /// <param name="confFootL">Current confidence for the left foot.</param>
    /// <param name="confFootR">Current confidence for the right foot.</param>
    /// <param name="lastPosFootL">Current position of the left foot.</param>
    /// <param name="lastPosFootR">Current position of the right foot.</param>
    /// <param name="ankleHeightOffset">Height (Y) difference between the "Foot" bone of the animator and the sole of the foot.</param>
    /// <returns>Offset to apply to avatar to stick it to the ground. Offset should be added, not subtracted.</returns>
    public Vector3 ComputeRootHeightOffsetXFramesV2(float confFootL, float confFootR, Vector3 lastPosFootL, Vector3 lastPosFootR, float ankleHeightOffset)
    {
        // offset vector
        Vector3 offsetToApply = new Vector3(0, currentAutoHeightOffset, 0);
        Vector3 ankleHeightVector = new Vector3(0, ankleHeightOffset, 0);

        // Check distance
        // --------------------------
        Ray rayL = new Ray(lastPosFootL + (Vector3.up * findFloorDistance) - ankleHeightVector, Vector3.down);
        posStartRayLeft = lastPosFootL - ankleHeightVector;
        bool rayUnderFootHitL = Physics.Raycast(rayL, out RaycastHit hitL, findFloorDistance * 2, layersToHit);
        Ray rayR = new Ray(lastPosFootR + (Vector3.up * findFloorDistance) - ankleHeightVector, Vector3.down);
        bool rayUnderFootHitR = Physics.Raycast(rayR, out RaycastHit hitR, findFloorDistance * 2, layersToHit);

        float footFloorDistanceL = 0;
        float footFloorDistanceR = 0;

        // "Oriented distance" between the soles and the ground (can be negative)
        // Positive is above floor, negative is under floor.
        if (rayUnderFootHitL) { footFloorDistanceL = (lastPosFootL.y - ankleHeightOffset) - hitL.point.y; }
        if (rayUnderFootHitR) { footFloorDistanceR = (lastPosFootR.y - ankleHeightOffset) - hitR.point.y; }

        if (Mathf.Abs(footFloorDistanceL) < thresholdDistCloseToFloor || Mathf.Abs(footFloorDistanceR) < thresholdDistCloseToFloor)
        {
            durationOffsetError = 0f;
        }
        else
        {
            float time = Time.time;
            durationOffsetError += time - lastCallTime;
            lastCallTime = time;
        }
        // --------------------------

        // manual offset
        if (!bodyTrackingManager.automaticOffset)
        {
            offsetToApply = bodyTrackingManager.manualOffset;
        }
        else if (durationOffsetError < thresholdDurationOffsetError) // auto offset
        {
            offsetToApply = new Vector3(0, currentAutoHeightOffset, 0);
        }
        else // recalculate auto offset if threshold passed
        {
            float startAutoHeightOffset = currentAutoHeightOffset;
            // if both feet are visible/detected, attempt to correct the height of the skeleton's root
            if (true || (!float.IsNaN(confFootL) && confFootL > 0 && !float.IsNaN(confFootR) && confFootR > 0))
            {
                // If both feet are under the ground, max distance, positived.
                if (footFloorDistanceL < 0 && footFloorDistanceR < 0)
                {
                    currentAutoHeightOffset = Mathf.Max(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    Debug.LogWarning("1:" + currentAutoHeightOffset);
                }
                // If both feet are above the ground, min distance, negatived.
                else if (footFloorDistanceL >= 0 && footFloorDistanceR >= 0)
                {
                    currentAutoHeightOffset = -1f * Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    Debug.LogWarning("2:" + currentAutoHeightOffset);
                }
                // If one foot under and one above the ground, min distance (negative), positived.
                else
                {
                    currentAutoHeightOffset = Mathf.Abs( Mathf.Min(footFloorDistanceL, footFloorDistanceR));
                    Debug.LogWarning("3:" + currentAutoHeightOffset);
                }
            }
            //offsetToApply = new Vector3(0, currentAutoHeightOffset, 0);
            StartCoroutine(LerpAutoOffset(1f,startAutoHeightOffset,currentAutoHeightOffset));
            Debug.Log("" + durationOffsetError.ToString("0.00") + "s of error. New offset: " + (currentAutoHeightOffset * 100).ToString("00.00") + "cm");
            durationOffsetError = 0f;
        }

        return offsetToApply;
    }

    IEnumerator LerpAutoOffset(float timeToLerp, float startValue, float targetValue)
    {
        if(timeToLerp == 0) { currentAutoHeightOffset = targetValue; yield break; }

        float lerptime = 0;
        float lerpVal;

        while (lerptime < timeToLerp)
        {
            lerpVal = lerptime / timeToLerp;
            currentAutoHeightOffset = Mathf.Lerp(startValue, targetValue, lerpVal);
            lerptime += Time.deltaTime;
            yield return 0;
        }
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
            bodyTrackingManager.manualOffset.y += offsetStep;
        }
        else if (Input.GetKeyDown(decreaseOffsetKey))
        {
            bodyTrackingManager.manualOffset.y -= offsetStep;
        }

        if (Input.GetKeyDown(toggleAutomaticOffsetKey))
        {
            bodyTrackingManager.automaticOffset = !bodyTrackingManager.automaticOffset;
        }
    }
}
