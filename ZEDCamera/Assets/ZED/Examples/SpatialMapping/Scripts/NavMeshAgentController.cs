//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sets the position of all the navMesh agents when they spawn
/// </summary>
public class NavMeshAgentController : MonoBehaviour
{

    /// <summary>
    /// Distance between current position and target
    /// </summary>
    public float rangeAroundCurrentPosition = 5.0f;

    /// <summary>
    /// Random position around an area. The position will always be on the ground
    /// </summary>
    /// <param name="center"></param>
    /// <param name="range"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
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
    /// Sets the target position of the agent
    /// </summary>
    /// <returns></returns>
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
