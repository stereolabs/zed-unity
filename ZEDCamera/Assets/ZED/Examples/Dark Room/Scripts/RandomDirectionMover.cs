using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every X seconds, rotates the light to face a new random direction. 
/// Used by the ZED Dark Room example scene and called by RandomDirLightManager.cs. 
/// </summary>
public class RandomDirectionMover : MonoBehaviour
{
    /// <summary>
    /// How long the light takes to point at a new location. 
    /// </summary>
    [Tooltip("How long the light takes to point at a new location. ")]
    public float timeToPoint = 0.1f; 

    /// <summary>
    /// Points the object in a new, random direction. 
    /// Gets called by RandomDirLightManager.cs. 
    /// </summary>
    /// <returns></returns>
    public IEnumerator NewDirection() 
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
