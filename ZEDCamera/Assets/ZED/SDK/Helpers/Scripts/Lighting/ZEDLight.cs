//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Causes the attached Light component to cast light on the real world, if visible by the ZED. 
/// Must be a point, spot, or directional light. Directional lights will also cast shadows on real objects. 
/// Works by registering the Light component  to a static list (contained within) that's checked by ZEDRenderingPlane. 
/// For more information, see our Lighting guide: https://docs.stereolabs.com/mixed-reality/unity/lighting/
/// </summary>
[RequireComponent(typeof(Light))]
public class ZEDLight : MonoBehaviour
{
    /// <summary>
    /// List of all ZEDLights in the scene. 
    /// </summary>
    [HideInInspector]
    public static List<ZEDLight> s_lights = new List<ZEDLight>();

    /// <summary>
    /// Light component attached to this GameObject. 
    /// </summary>
    [HideInInspector]
    public Light cachedLight;

    /// <summary>
    /// Interior cone of the spotlight, if the Light is a spotlight.
    /// </summary>
    [HideInInspector]
    public float interiorCone = 0.1f;

    // Use this for initialization
    void OnEnable()
    {
        if (!s_lights.Contains(this))
        {
            s_lights.Add(this);
            cachedLight = GetComponent<Light>();
        }
    }

    void OnDisable()
    {
        if (s_lights != null)
        {
            s_lights.Remove(this);
        }
    }


    /// <summary>
    /// Checks if a light is both enabled and has above-zero range and intensity. 
    /// Used by ZEDRenderingPlane to filter out lights that won't be visible. 
    /// </summary>
    /// <returns></returns>
    public bool IsEnabled()
    {
        if (!cachedLight.enabled)
        {
            return false;
        }

        if (cachedLight.range <= 0 || cachedLight.intensity <= 0)
        {
            return false;
        }

        return true;
    }
}
