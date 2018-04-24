//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Sets spot lights on the controllers
/// </summary>
public class ControllerLighter : MonoBehaviour
{


#if ZED_STEAM_VR
    public Color[] listColors =
    {
        new Color(1,0,0,1),
        new Color(0,1,0,1)
    };

    private ZEDSteamVRControllerManager padManager;

    void Start()
    {
        padManager = GetComponent<ZEDSteamVRControllerManager>();
    }

    private void OnEnable()
    {
        ZEDSteamVRControllerManager.ZEDOnPadIndexSet += PadIndexSet;
    }

    private void OnDisable()
    {
        ZEDSteamVRControllerManager.ZEDOnPadIndexSet += PadIndexSet;
    }

    private void PadIndexSet()
    {
        int i = 0;
        foreach (GameObject o in padManager.controllersGameObject)
        {
            if (o != null)
            {

                if (o.GetComponent<Light>() != null)
                {
                    o.GetComponent<Light>().enabled = true;
                }
                else
                {
                    Light l = o.AddComponent<Light>();
                    l.type = LightType.Spot;
                    if (i < listColors.Length)
                    {
                        l.color = listColors[i];
                    }
                    else
                    {
                        l.color = listColors[1];
                    }
                    l.intensity = 2;
                    o.AddComponent<ZEDLight>();
                }
            }

            i++;
        }

    }

#endif
}
