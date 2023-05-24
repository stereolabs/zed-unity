using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Forces Unity's XR tracking mode to be set to room scale. This happens automatically with SteamVR
/// but does not happen with the Oculus platform, and allows object positions to be consistent between the two. 
/// </summary>
public class ForceTrackingSpaceType : MonoBehaviour
{

    public TrackingOriginModeFlags trackingType = TrackingOriginModeFlags.Floor;

    // Use this for initialization
    void Start()
    {
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);
        for (int i = 0; i < subsystems.Count; i++)
        {
            subsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }

}
