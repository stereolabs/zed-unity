//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Previously used in the ZED spatial mapping sample scene. 
/// Sends events when the ball the script is attached to has touched an object.
/// </summary>
public class BallTrigger : MonoBehaviour
{
    private Rigidbody body;
    private bool hasDamaged = false;
    private const float dammage = 1.0f;
    private const float minVelocityDammage = 5;
    private void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    public void ResetValues()
    {
        hasDamaged = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasDamaged || body.velocity.magnitude < minVelocityDammage) return;
        if (other.gameObject.name.Contains("ZomBunny"))
        {
            hasDamaged = true;
            other.gameObject.GetComponent<EnemyBehavior>().Dammage(dammage * body.velocity.magnitude);
        }
    }
}
