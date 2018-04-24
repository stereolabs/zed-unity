//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sets the agent to randomly walk in a certain area
/// </summary>
public class RandomWalk : MonoBehaviour
{
    /// <summary>
    /// Range of search available
    /// </summary>
    public float m_Range = 25.0f;

    /// <summary>
    /// Reference to the component agent
    /// </summary>
    private NavMeshAgent m_agent;

    /// <summary>
    /// flag to set the agent to walk
    /// </summary>
    private bool startWalking = false;

    /// <summary>
    /// Destination to walk to
    /// </summary>
    private Vector3 destination;

    /// <summary>
    /// Reduce the range of research if the future position is hard to find
    /// </summary>
    private uint reduceFactor = 0;
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
                destination = (Mathf.Abs(m_Range) - reduceFactor) * Random.insideUnitSphere;
                m_agent.destination = destination;                
                if(reduceFactor < Mathf.Abs(m_Range)/2) reduceFactor++;
            }
        }
    }
}