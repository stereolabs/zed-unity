using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles fading in/out the material of the object it's attached to for an effect when the player takes damage. 
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class PlayerDamageReceiver : MonoBehaviour
{
    public float SecondsToDisplayEffect = 1f;
    private MeshRenderer _renderer;
    private float _maxColorAlpha; //The highest value the material will be set to

    private float _colorAlpha
    {
        get
        {
            return _renderer.material.color.a;
        }
        set
        {
            _renderer.material.color = new Color(_renderer.material.color.r, _renderer.material.color.g, _renderer.material.color.b, value);
        }
    }

    private AudioSource _audioSource;

    // Use this for initialization
    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _maxColorAlpha = _renderer.material.color.a;

        //Set the alpha to zero as we haven't taken damage yet. 
        _colorAlpha = 0f;

        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        //Tick down the color if it's above zero. 
        if(_colorAlpha > 0f)
        {
            _colorAlpha -= Time.deltaTime / SecondsToDisplayEffect * _maxColorAlpha;
        }
    }

    public void TakeDamage()
    {
        _colorAlpha = _maxColorAlpha;

        if(_audioSource)
        {
            _audioSource.Play();
        }
    }
}