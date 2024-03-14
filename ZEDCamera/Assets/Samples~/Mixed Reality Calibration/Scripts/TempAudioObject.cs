using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// After Setup() is called, plays a sound file once and then destroys itself. 
/// Used by ZEDXRGrabber to play a sound file that will continue to play even if it's disabled. 
/// <para>To use, instantiate a gameObject and put this on it (or use a prefab) and call Setup() with the clip to be played.</para>
/// </summary>
public class TempAudioObject : MonoBehaviour
{
    private AudioSource source;
    private bool isSetup = false;

    /// <summary>
    /// Tells this object which clip to play, and causes it to be destroyed as soon as it's done playing. 
    /// </summary>
	public void Setup(AudioClip clip)
    {
        source = gameObject.AddComponent<AudioSource>();
        //source.clip = clip;
        source.PlayOneShot(clip);

        isSetup = true;
    }

    /// <summary>
    /// If we've started playing, make sure we haven't finished playing it. If we have, destroy this object. 
    /// </summary>
    private void Update()
    {
        if(isSetup == true && source.isPlaying == false)
        {
            Destroy(gameObject);
        }
    }
}
