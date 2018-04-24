//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Set box and rigidbodies on the Controllers
/// </summary>
public class ControllerCollider : MonoBehaviour {

#if ZED_STEAM_VR

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

                Setcollider(o);
            }

            i++;
        }

    }

    private void Setcollider(GameObject o)
    {
        MeshFilter[] listMesh = o.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in listMesh)
        {
            if (mf.name == "body")
            {
                MeshCollider mesh = mf.gameObject.GetComponent<MeshCollider>();
                if (!mesh)
                {
                    mf.gameObject.AddComponent<MeshCollider>();
                }
                Rigidbody rigid = mf.gameObject.GetComponent<Rigidbody>();
                if (!rigid)
                {
                    rigid = mf.gameObject.AddComponent<Rigidbody>();
                }
                rigid.useGravity = false;
                rigid.isKinematic = true;
            }
        }

    }
#endif
}
