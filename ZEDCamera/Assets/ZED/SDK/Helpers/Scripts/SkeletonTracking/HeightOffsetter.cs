using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour
{
    private ZEDManagerIK zedik = null;

    public Vector3 manualOffset = Vector3.zero;
    public bool automaticOffset = false;
    public float findFloorDistance = 2f;
    public LayerMask layersToHit;
    private float feetAlpha = .1f;
    private float curheightOffset = 0f;

    private Vector3 yellowCubeL = Vector3.zero;
    private Vector3 cyanCubeR = Vector3.zero;
    private Vector3 redCubeR = Vector3.zero;
    private Vector3 magentaCube = Vector3.zero;
    public float gizmoSize = .1f;

    // LinkedList has functions with O(1) complexity for removing/add first/last elements
    private LinkedList<float> feetOffsetBuffer = new LinkedList<float>();
    public int bufferSize = 120;

    private Vector3 debutLigneL = Vector3.zero;
    private Vector3 debutLigneR = Vector3.zero;
    private Vector3 finLigneL = Vector3.zero;
    private Vector3 finLigneR = Vector3.zero;

    private void Start()
    {
        zedik = GetComponent<ZEDManagerIK>();
    }

    // Moves the actor to get 
    public void ComputeRootHeightOffset(float confFootL, float confFootR, Vector3 animPosFootL, Vector3 animPosFootR, float ankleHeightOffset)
    {
        Vector3 offsetToApply = new Vector3(0,curheightOffset,0);

        //yellowCubeL = prevPosFootR - new Vector3(0, ankleHeightOffset - curheightOffset, 0);
        //redCubeR = prevPosFootR;

        if (automaticOffset)
        {
            // if both feet are visible/detected, attempt to correct the height of the skeleton's root
            if (!float.IsNaN(confFootL) && !float.IsNaN(confFootR) && confFootL > 0 && confFootR > 0)
            {
                Ray rayL = new Ray(animPosFootL + (Vector3.up * findFloorDistance), Vector3.down);
                debutLigneL = rayL.origin;
                bool rayUnderFootHitL = Physics.Raycast(rayL, out RaycastHit hitL, findFloorDistance*2, layersToHit);
                Ray rayR = new Ray(animPosFootR + (Vector3.up * findFloorDistance), Vector3.down);
                debutLigneR = rayR.origin;
                bool rayUnderFootHitR = Physics.Raycast(rayR, out RaycastHit hitR, findFloorDistance*2, layersToHit);
                cyanCubeR = hitR.point;
                // redCubeR = new Vector3(cyanCubeR.x, transform.position.y, cyanCubeR.z);

                float footFloorDistanceL = 0;
                float footFloorDistanceR = 0;
                finLigneL = debutLigneL;
                finLigneR = debutLigneR;

                //// "Oriented distance" between the soles and the ground (can be negative)
                //if (rayUnderFootHitL) { footFloorDistanceL = (animPosFootL - new Vector3(0, footHeightOffset, 0) - hitL.point).y; }
                //if (rayUnderFootHitR) { footFloorDistanceR = (animPosFootR - new Vector3(0, footHeightOffset, 0) - hitR.point).y; }
                if (rayUnderFootHitL) { footFloorDistanceL = (animPosFootL.y - ankleHeightOffset - curheightOffset) - hitL.point.y; finLigneL = hitL.point; }
                if (rayUnderFootHitR) { footFloorDistanceR = (animPosFootR.y - ankleHeightOffset - curheightOffset) - hitR.point.y; finLigneR = hitR.point; }
                magentaCube = new Vector3(animPosFootR.x, footFloorDistanceR, animPosFootR.z);
                magentaCube += new Vector3(0,animPosFootR.y,0);

                //Debug.Log("ffdL[" + footFloorDistanceL + "] ffdR[" + footFloorDistanceR + "] sum["+ (footHeightOffset+curFeetOffset) + "] fho[" + footHeightOffset + "] cfo:" + curFeetOffset);

                float minFootFloorDistance = 0;
                float thresholdHeight = -1* /*(footHeightOffset + */curheightOffset/*)*/;
                //Debug.Log(thresholdHeight + " / " + footFloorDistanceL + " / " + footFloorDistanceR);

                // If both feet are under the ground, use the max value instead of the min value.
                //if (footFloorDistanceL < thresholdHeight && footFloorDistanceR < thresholdHeight)
                if (footFloorDistanceL < 0 && footFloorDistanceR < 0)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
                    Debug.Log("both under: " + curheightOffset.ToString("0.00"));
                }
                else if (footFloorDistanceL >= 0 && footFloorDistanceR >= 0)
                //else if (footFloorDistanceL > thresholdHeight && footFloorDistanceR > thresholdHeight)
                {
                    minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));

                    // The feet offset is added in the buffer of size "bufferSize". If the buffer is already full, remove the oldest value (the first)
                    if(feetOffsetBuffer.Count >= bufferSize)
                    {
                        feetOffsetBuffer.RemoveFirst();
                    }
                    feetOffsetBuffer.AddLast(minFootFloorDistance);

                    // Continuous adjustment: The feet offset is the min element of this buffer.
                    minFootFloorDistance = -1 * MinOfLinkedList(ref feetOffsetBuffer);
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
                    Debug.Log("both above: " + curheightOffset.ToString("0.00"));
                }
                else
                {
                    minFootFloorDistance = -1 * Mathf.Min(footFloorDistanceL, footFloorDistanceR);
                    //minFootFloorDistance = Mathf.Min(Mathf.Abs(footFloorDistanceL), Mathf.Abs(footFloorDistanceR));
                    curheightOffset = feetAlpha * minFootFloorDistance + (1 - feetAlpha) * curheightOffset;
                    Debug.Log("one one: " + curheightOffset.ToString("0.00"));
                }

                offsetToApply = new Vector3(0,curheightOffset, 0);
            }
            // if the feet are not visible/detected, offset is (0,0,0)
        }
        else
        {
            offsetToApply = manualOffset;
        }

        zedik.RootHeightOffset = offsetToApply;
    }

    public void ComputeRootHeightOffset()
    {
        zedik.RootHeightOffset = manualOffset;
    }

    private float MinOfLinkedList(ref LinkedList<float> buf)
    {
        float min = float.MaxValue;

        foreach(float e in buf)
        {
            if (e < min)
                min = e;
        }
        return min;
    }

    //private float prevHDif = 0f;

    //public void MaybeComputeRootHeightOffset(Vector3 footLpos, Vector3 footRpos)
    //{
    //    Vector3 pelvispos = transform.position;
    //    bool footLPosIsFurthest = footLpos.y < footRpos.y;
    //    float offsetPtoFY = footLPosIsFurthest ? pelvispos.y - footLpos.y : pelvispos.y - footRpos.y;
    //    Vector2 offsetPtoFXZ =
    //        footLPosIsFurthest
    //        ? new Vector2(footLpos.x, footLpos.z) - new Vector2(pelvispos.x, pelvispos.z)
    //        : new Vector2(footRpos.x, footRpos.z) - new Vector2(pelvispos.x, pelvispos.z);

    //    Ray ray = new Ray((footLPosIsFurthest ? footLpos : footRpos) + (Vector3.up * findFloorDistance), Vector3.down);
    //    bool rayUnderFootHit = Physics.Raycast(ray, out RaycastHit hit, findFloorDistance * 2, layersToHit);

    //    if (rayUnderFootHit)
    //    {
    //        transform.position = hit.point - new Vector3(offsetPtoFXZ.x, -offsetPtoFY, offsetPtoFXZ.y);
    //    } else
    //    {
    //        // nothing
    //    }
    //}

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawCube(yellowCubeL, new Vector3(gizmoSize,gizmoSize,gizmoSize));
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawCube(cyanCubeR, new Vector3(gizmoSize, gizmoSize, gizmoSize));
        //Gizmos.color = Color.red;
        //Gizmos.DrawCube(redCubeR, new Vector3(gizmoSize, gizmoSize, gizmoSize));
        //Gizmos.color = Color.magenta;
        //Gizmos.DrawCube(magentaCube, new Vector3(gizmoSize, gizmoSize, gizmoSize));

        Gizmos.color = Color.magenta;
        //Gizmos.DrawLine(debutLigneL, finLigneL);
        Gizmos.DrawSphere(finLigneL, 0.03f);
        Gizmos.color = Color.red;
        //Gizmos.DrawLine(debutLigneR, finLigneR);
        Gizmos.DrawSphere(finLigneR, 0.03f);
    }
}
