using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Forces Unity's XR tracking mode to be set to room scale. This happens automatically with SteamVR
/// but does not happen with the Oculus platform, and allows object positions to be consistent between the two. 
/// </summary>
public class ForceTrackingSpaceType : MonoBehaviour
{
    public UnityEngine.XR.TrackingSpaceType trackingType = UnityEngine.XR.TrackingSpaceType.RoomScale;

    // Use this for initialization
    void Start ()
    {
        UnityEngine.XR.XRDevice.SetTrackingSpaceType(trackingType);
    }
	
}
