using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour
{
    private ZEDSkeletonAnimator zedik = null;

    public Vector3 manualOffset = Vector3.zero;
    public bool automaticOffset = false;
    public float findFloorDistance = 2f;
    public LayerMask layersToHit;
    private float feetAlpha = .1f;
    private float curheightOffset = 0f;

    private LinkedList<float> feetOffsetBuffer = new LinkedList<float>();
    public int bufferSize = 120;

    float zStep = 0.1f;
    private void Start()
    {
        zedik = GetComponent<ZEDSkeletonAnimator>();
    }

    /// <summary>
    /// Find an automatic offset that sets both feet above ground, and at least one foot on the ground.
    /// Uses the HeightOffsetStabilized bool value from the ZEDSkeletonAnimator to know when to stop and settle on the current offset.
    /// </summary>
    /// <param name="confFootL">Current confidence for the left foot.</param>
    /// <param name="confFootR">Current confidence for the right foot.</param>
    /// <param name="animPosFootL">Current position of the left foot.</param>
    /// <param name="animPosFootR">Current position of the right foot.</param>
    /// <param name="ankleHeightOffset">Height (Y) difference between the "Foot" bone of the animator and the sole of the foot.</param>
    public Vector3 ComputeRootHeightOffsetXFrames(float confFootL, float confFootR, Vector3 animPosFootL, Vector3 animPosFootR, float ankleHeightOffset)
    {
        Vector3 offsetToApply = new Vector3(0, curheightOffset, 0);

        if (automaticOffset)
        {
            // if both feet are visible/detected, attempt to correct the height of the skeleton's root
            if (!float.IsNaN(confFootL) && !float.IsNaN(confFootR) && !zedik.HeightOffsetStabilized)
            {
                Ray rayL = new Ray(animPosFootL + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitL = Physics.Raycast(rayL, out RaycastHit hitL, findFloorDistance * 2, layersToHit);
                Ray rayR = new Ray(animPosFootR + (Vector3.up * findFloorDistance), Vector3.down);
                bool rayUnderFootHitR = Physics.Raycast(rayR, out RaycastHit hitR, findFloorDistance * 2, layersToHit);

                float footFloorDistanceL = 0;
                float footFloorDistanceR = 0;

                //// "Oriented distance" between the soles and the ground (can be negative)
                if (rayUnderFootHitL) { footFloorDistanceL = (animPosFootL.y - ankleHeightOffset - curheightOffset) - hitL.point.y; }
                if (rayUnderFootHitR) { footFloorDistanceR = (animPosFootR.y - ankleHeightOffset - curheightOffset) - hitR.point.y; }

                float minFootFloorDistance = 0;
                float thresholdHeight = -1 * curheightOffset;

                // If both feet are under the ground, use the max value instead of the min value.
                if (footFloorDistanceL < 0 && footFloorDistanceR < 0)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
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
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
                }
                else
                {
                    minFootFloorDistance = -1 * Mathf.Min(footFloorDistanceL, footFloorDistanceR);
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
                }
            }
            offsetToApply = new Vector3(0, curheightOffset, 0);
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
        zedik.RootHeightOffset = manualOffset;
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
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            manualOffset.y += zStep;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            manualOffset.y -= zStep;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            automaticOffset = !automaticOffset;
        }
    }
}
