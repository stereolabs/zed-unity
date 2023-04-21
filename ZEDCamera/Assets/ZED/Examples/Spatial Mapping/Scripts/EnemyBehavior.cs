//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Previously used in the ZED spatial mapping sample scene. 
/// Spawns and destroys the bunnies. 
/// </summary>
public class EnemyBehavior : MonoBehaviour {

    private const float lifeMax = 100;
    public float life = lifeMax;
    private SphereCollider capsuleCollider;
    private bool isDying = false;
    private void Start()
    {
        life = lifeMax;
        capsuleCollider = GetComponent<SphereCollider>();
        capsuleCollider.enabled = true;
        isDying = false;
    }

    // Update is called once per frame
    void Update () {

    }

    /// <summary>
    /// Set the dammage to an object
    /// </summary>
    /// <param name="value"></param>
    public void Dammage(float value)
    {
        if (!isDying)
        {
            life -= value;
            if (life < 0)
            {
                Dead();
            }
        }
    }

    /// <summary>
    /// Disables the gameobject
    /// </summary>
    public void StartSinking()
    {
        capsuleCollider.enabled = false;
        Destroy(gameObject, 2.0f);

    }

    /// <summary>
    /// Play the animation of dead
    /// </summary>
    private void Dead()
    {
        isDying = true;
        GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        GetComponent<Animator>().SetTrigger("Dead");
      
    }

    private void OnDestroy()
    {
        isDying = false;
    }
}
