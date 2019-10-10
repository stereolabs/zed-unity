using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for controls that change the ZED's transform in the ZED MR Calibration scene. See TranslateControl and RotateControl. 
/// Mostly handles the audio sounds they both play since it's fairly complicated. 
/// </summary>
public abstract class TransformControl : MonoBehaviour
{
    /// <summary>
    /// CameraAnchor object holding the ZED camera. Inheriting classes send transform updates to this.
    /// </summary>
    [Tooltip("CameraAnchor object holding the ZED camera. Inheriting classes send transform updates to this.")]
    public CameraAnchor anchor;
    /// <summary>
    /// Transform that holds all the visuals, like RotateRings and TransformArrows, and visual indicators of their movement.
    /// This should be a child of this script's transform, as it's moved/rotated with the visuals, but this object should
    /// not be moved separately from its parent as its used for calculating the controller's offset. 
    /// </summary>
    [Tooltip("Transform that holds all the visuals, like RotateRings and TransformArrows, and visual indicators of their movement.\r\n" +
        "This should be a child of this script's transform, as it's moved/rotated with the visuals, but this object should " +
        "not be moved separately from its parent as its used for calculating the controller's offset.")]
    public Transform visualsParent;

    /// <summary>
    /// How many 'notches' (intervals) between a control being moved at 0 and 1 where should a tap sound play as it's crossed. 
    /// </summary>
    [Tooltip("How many 'notches' (intervals) between a control being moved at 0 and 1 where should a tap sound play as it's crossed. ")]
    [Header("Sounds")]
    public int tapLevels = 5;
    /// <summary>
    /// Minimum time that must pass between playing tap sounds, so it doesn't sound caustic when you move the controller very quickly. 
    /// </summary>
    [Tooltip("Minimum time that must pass between playing tap sounds, so it doesn't sound caustic when you move the controller very quickly. ")]
    public float secondsBetweenTaps = 0.05f;
    /// <summary>
    /// Pitch multiplier of the tap sounds when at 0. Taps in between 0 and 1 have the pitch lerped between the min and max. 
    /// </summary>
    [Tooltip("Pitch multiplier of the tap sounds when at 0.\r\n" +
        "Taps in between 0 and 1 have the pitch lerped between the min and max. ")]
    public float minPitch = 1f;
    /// <summary>
    /// Pitch multiplier of the tap sounds when fully articulated to 1. Taps in between 0 and 1 have the pitch lerped between the min and max. 
    /// </summary>
    [Tooltip("Pitch multiplier of the tap sounds when fully articulated to 1.\r\r" +
        "Taps in between 0 and 1 have the pitch lerped between the min and max. ")]
    public float maxPitch = 2f;

    private float tapTimer = 0f;
    private AudioSource audioSource;
    private float tapIncrement
    {
        get
        {
            return 1 / (float)tapLevels;
        }
    }

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    protected virtual void Update()
    {
        if (tapTimer > 0f) tapTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Determines if we need to play a tapping sound based on the latest change in articulation. 
    /// Also makes sure there's an attached AudioSource and that enough time has passed since the last tap sound. 
    /// </summary>
    /// <param name="oldvector">Amount of articulation of X, Y and Z (clamped at -1 to 1) before the latest change.</param>
    /// <param name="newvector">Amount of articulation of X, Y and Z (clamped at -1 to 1) as of the latest change.</param>
    protected void PlayTapSoundIfNeeded(Vector3 oldvector, Vector3 newvector)
    {
        if (!audioSource) return;
        if (tapTimer > 0f) return;

        float oldsumdirection = oldvector.x + oldvector.y + oldvector.z;
        float newsumdirection = newvector.x + newvector.y + newvector.z;
        int oldlevel = Mathf.RoundToInt(oldsumdirection / tapIncrement);
        int newlevel = Mathf.RoundToInt(newsumdirection / tapIncrement);

        if (newlevel != oldlevel) //We've passed a tapping point. 
        {
            float newpitch = Mathf.Lerp(minPitch, maxPitch, (Mathf.Abs(newlevel) + 1) / (float)(tapLevels + 1));

            audioSource.pitch = newpitch;
            audioSource.Play();
            tapTimer = secondsBetweenTaps;
        }
        else if (newlevel == 0f)
        {
            if ((oldsumdirection >= 0 && newsumdirection < 0) || (oldsumdirection <= 0 && newsumdirection > 0))
            {
                audioSource.pitch = minPitch;
                audioSource.Play();
                tapTimer = secondsBetweenTaps;
            }
        }
    }

}
