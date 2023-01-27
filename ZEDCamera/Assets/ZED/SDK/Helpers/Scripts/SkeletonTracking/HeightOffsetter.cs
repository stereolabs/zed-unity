using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightOffsetter : MonoBehaviour
{
    public Vector3 manualOffset = Vector3.zero;
    public bool automaticOffset = false;

    // Update is called once per frame
    public void ApplyOffset()
    {
        Vector3 offsetToApply = Vector3.zero;
        if (automaticOffset)
        {
            Debug.Log("WIP: automaticOffset");
        }
        else
        {
            offsetToApply = manualOffset;
        }
        transform.position += offsetToApply;
    }
}
