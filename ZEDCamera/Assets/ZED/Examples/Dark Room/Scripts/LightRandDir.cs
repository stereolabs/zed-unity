using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every [timeToPoint] seconds, rotates the light to face a new random direction. 
/// </summary>
public class LightRandDir : MonoBehaviour
{
    public float timeToPoint = 0.1f; //How long it takes to point at the new location

    public IEnumerator NewDirection() //Gets called by RandomDirLightManager
    {
        Vector3 startdir = transform.localRotation * Vector3.forward;
        Vector3 randdir = new Vector3(Random.Range(0f, 2f) - 1f, Random.Range(0f, .25f) - .125f, Random.Range(0f, 2f) - 1f); //Weighted towards horizon to make it more likely to be visible

        float angledif = Mathf.Abs(Vector3.Angle(startdir, randdir)); //How we know how quickly to rotate each frame, and when to stop

        Quaternion targetrot = Quaternion.FromToRotation(startdir, randdir);

        for(float t = 0; t < timeToPoint; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetrot, angledif / timeToPoint * Time.deltaTime);
            yield return null;
        }


    }
}
