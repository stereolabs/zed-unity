//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Used in the ZED spatial mapping sample scene to position new nav mesh agents randomly onto a
/// new NaVMesh when they're spawned. 
/// </summary>
public class NavMeshAgentController : MonoBehaviour
{
    /// <summary>
    /// Maximum distance between the starting position where the agent can be randomly placed. 
    /// </summary>
    [Tooltip("Maximum distance between the starting position where the agent can be randomly placed.")]
    public float rangeAroundCurrentPosition = 5.0f;

    /// <summary>
    /// Finds a random position around a given point. The position will always be on the ground.
    /// </summary>
    /// <param name="center">The point around where the random position appears.</param>
    /// <param name="range">The maximum distance from the center that the random position can be.</param>
    /// <param name="result">The random position.</param>
    /// <returns>True if it found a valid location.</returns>
    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        //Try up to 30 times to find a valid point near center. Return true as soon as one is found. 
        for (int i = 0; i < 30; i++)
        {
            Vector3 vector = Random.insideUnitSphere * range;
            if (Vector3.Dot(vector, -Vector3.up) < 0)
            {
                vector = -vector;
            }
            RaycastHit rayHit;
            if (!Physics.Raycast(center + transform.up, vector, out rayHit))
            {
                continue;
            }
            Vector3 randomPoint = rayHit.point;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }


    /// <summary>
    /// Sets the target position of the agent to a random point 
    /// </summary>
    /// <returns>True if it successfully placed the agent. </returns>
    public bool Move()
    {
        Vector3 point;

        if (RandomPoint(transform.position, rangeAroundCurrentPosition, out point))
        {
            transform.position = point;
            return true;
        }
        return false;
    }

}
