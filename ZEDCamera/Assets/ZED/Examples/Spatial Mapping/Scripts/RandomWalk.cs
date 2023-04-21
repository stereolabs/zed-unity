//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tells the attached NavMeshAgent component to walk to a random location, and finds a new location
/// each time it arrives. 
/// Used in the ZED spatial mapping sample to make the bunny character walk around once you've finished
/// scanning your environment. 
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RandomWalk : MonoBehaviour
{
    /// <summary>
    /// Maximum distance of the next random point to walk to. 
    /// </summary>
    [Tooltip("Maximum distance of the next random point to walk to.")]
    public float maxRange = 25.0f;

    /// <summary>
    /// The NavMeshAgent component attached to this GameObject. 
    /// </summary>
    private NavMeshAgent m_agent;

    /// <summary>
    /// Whether the agent should be walking. 
    /// </summary>
    private bool startWalking = false;

    /// <summary>
    /// Current random destination the agent is walking toward. 
    /// </summary>
    private Vector3 destination;

    /// <summary>
    /// Factor used to narrow the range of possible random destinations if positions at the range are difficult to find. 
    /// </summary>
    private uint reduceFactor = 0;

    /// <summary>
    /// Enables the agent component and begins walking. 
    /// Called by EnemyManager once the agent is successfully placed. 
    /// </summary>
    public void Activate()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_agent.enabled = true;
        startWalking = true;
    }

    void Update()
    {
        if (startWalking)
        {
            if (m_agent.enabled)
            {
                if (m_agent.pathPending || m_agent.remainingDistance > 0.1f)
                {
                    reduceFactor = 0;
                    return;
                }
                destination = (Mathf.Abs(maxRange) - reduceFactor) * Random.insideUnitSphere;
                m_agent.destination = destination;                
                if(reduceFactor < Mathf.Abs(maxRange)/2) reduceFactor++;
            }
        }
    }
}