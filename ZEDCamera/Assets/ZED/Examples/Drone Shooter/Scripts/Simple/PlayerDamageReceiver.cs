using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles fading in/out the material of the object it's attached to for an effect when the player takes damage. 
/// in the ZED Drone Battle sample, this makes a sphere around the player's head turn red and fade out over a second. Also plays a sound. 
/// Used by LaserShot_Drone to know when a laser hit the player's head, which then also calls TakeDamage() to make it happen. 
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class PlayerDamageReceiver : MonoBehaviour
{
    /// <summary>
    /// How long to display the damage effect. 
    /// </summary>
    [Tooltip("How long to display the damage effect. ")]
    public float secondsToDisplayEffect = 1f;

    /// <summary>
    /// The highest value the damage sphere's material color will be set to
    /// </summary>
    private float maxcoloralpha; 

    /// <summary>
    /// The current alpha value of the damage sphere's material color. 
    /// </summary>
    private float coloralpha
    {
        get
        {
            return meshrenderer.material.color.a;
        }
        set
        {
            meshrenderer.material.color = new Color(meshrenderer.material.color.r, meshrenderer.material.color.g, meshrenderer.material.color.b, value);
        }
    }

    /// <summary>
    /// The MeshRenderer attached to this GameObject. 
    /// </summary>
    private MeshRenderer meshrenderer;

    /// <summary>
    /// The AudioSource attached to this GameObject for playing the hurt sound. 
    /// </summary>
    private AudioSource _audioSource;

    // Use this for initialization
    void Start()
    {
        meshrenderer = GetComponent<MeshRenderer>();
        maxcoloralpha = meshrenderer.material.color.a;

        //Set the alpha to zero as we haven't taken damage yet. 
        coloralpha = 0f;

        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        //Tick down the color if it's above zero. 
        if(coloralpha > 0f)
        {
            coloralpha -= Time.deltaTime / secondsToDisplayEffect * maxcoloralpha;
        }
    }

    /// <summary>
    /// Causes the damage effect to play once. 
    /// </summary>
    public void TakeDamage()
    {
        coloralpha = maxcoloralpha; //Set the damage sphere to as high as we want it to ever get

        if(_audioSource)
        {
            _audioSource.Play(); //Play the "ouch" sound. 
        }
    }
}