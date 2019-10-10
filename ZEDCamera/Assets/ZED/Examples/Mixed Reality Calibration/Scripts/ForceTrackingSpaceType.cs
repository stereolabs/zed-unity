using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Forces Unity's XR tracking mode to be set to room scale. This happens automatically with SteamVR
/// but does not happen with the Oculus platform, and allows object positions to be consistent between the two. 
/// </summary>
public class ForceTrackingSpaceType : MonoBehaviour
{
    public UnityEngine.VR.TrackingSpaceType trackingType = UnityEngine.VR.TrackingSpaceType.RoomScale;

    // Use this for initialization
    void Start ()
    {
        UnityEngine.VR.VRDevice.SetTrackingSpaceType(trackingType);
    }
	
}
