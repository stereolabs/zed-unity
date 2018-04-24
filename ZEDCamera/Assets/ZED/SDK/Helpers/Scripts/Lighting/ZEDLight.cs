//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers the light in a common structure and checks if the light can be displayed
/// </summary>
[RequireComponent(typeof(Light))]
public class ZEDLight : MonoBehaviour
{
    /// <summary>
    /// Static structures shared among all the instances of ZEDLight
    /// </summary>
    [HideInInspector]
    public static List<ZEDLight> s_lights = new List<ZEDLight>();

    /// <summary>
    /// Cached the light
    /// </summary>
    [HideInInspector]
    public Light cachedLight;

    /// <summary>
    /// Interior cone of the spot light.
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
    /// Checks if a light is enable or if it is lighting
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
