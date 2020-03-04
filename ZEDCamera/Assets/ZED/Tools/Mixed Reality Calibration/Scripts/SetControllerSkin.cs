using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if ZED_STEAM_VR
using Valve.VR;
#endif

/// <summary>
/// Creates a model to match whatever tracked object it's assigned to. Used alongsize ZEDControllerTracker. 
/// In SteamVR, uses SteamVR_RenderModel to handle the model. In Oculus, it loads a prefab from Resources. 
/// </summary>
public class SetControllerSkin : MonoBehaviour
{
    /// <summary>
    /// Associated ZEDControllerTracker with this object. If checkDeviceIndexEachUpdate is true, this is
    /// where we check the index. 
    /// </summary>
    [Tooltip("Associated ZEDControllerTracker with this object. If checkDeviceIndexEachUpdate is true, this is " +
        "where we check the index. ")]
    public ZEDControllerTracker controllerTracker;
    /// <summary>
    /// Whether to regularly check the ZEDControllerTracker to see if its tracked index has changed. 
    /// </summary>
    [Tooltip("Whether to regularly check the ZEDControllerTracker to see if its tracked index has changed.")]
    public bool checkDeviceIndexEachUpdate = true;

#if ZED_OCULUS
    public GameObject controllerModel;
#endif

    /// <summary>
    /// Returns the first MeshFilter attached to or parented to this object. 
    /// Used when other scripts want to draw copies of it with Graphics.DrawMesh.
    /// </summary>
    public Mesh GetFirstControllerMesh()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter != null) return filter.mesh;
        else return null;
    }

    /// <summary>
    /// Returns the Meshes of all MeshFilters attached to or parented to this object. 
    /// </summary>
    public Mesh[] GetControllerMeshes()
    {
        return GetComponentsInChildren<MeshFilter>().Select(x => x.mesh).ToArray();
    }


#if ZED_STEAM_VR
    private SteamVR_RenderModel renderModel;
#endif

    void Start ()
    {
        //controllerTracker = GetComponent<ZEDControllerTracker>();
#if ZED_STEAM_VR
        renderModel = GetComponent<SteamVR_RenderModel>();
        if(!renderModel)
        {
            renderModel = gameObject.AddComponent<SteamVR_RenderModel>();
        }
#endif
        
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (checkDeviceIndexEachUpdate)
        {
#if ZED_STEAM_VR
            if ((int)controllerTracker.index != (int)renderModel.index)
            {
                //renderModel.SetDeviceIndex((int)controllerTracker.index);
                SetRenderModelIndex((int)controllerTracker.index);
            }
#endif
#if ZED_OCULUS
            if(controllerTracker.deviceToTrack == ZEDControllerTracker.Devices.LeftController)
            {
                SetRenderModelIndex(ChooseTrackedObjectMenu.TOUCH_INDEX_LEFT);
            }
            else if (controllerTracker.deviceToTrack == ZEDControllerTracker.Devices.RightController)
            {
                SetRenderModelIndex(ChooseTrackedObjectMenu.TOUCH_INDEX_RIGHT);
            }
#endif
        }
	}

    /// <summary>
    /// Makes the controller update its model based on the specified controller index. 
    /// This index is the actual tracked object index in SteamVR, and in Oculus, corresponds to
    /// ChooseTrackedObjectMenu.TOUCH_INDEX_LEFT and _RIGHT.
    /// </summary>
    public void SetRenderModelIndex(int newindex)
    {
#if ZED_STEAM_VR
        if (!renderModel)
        {
            renderModel = gameObject.AddComponent<SteamVR_RenderModel>();
        }

        renderModel.SetDeviceIndex(newindex);
#elif ZED_OCULUS
        if (newindex == ChooseTrackedObjectMenu.TOUCH_INDEX_LEFT)
        {
            //if(controllerModel != null && controllerModel.name != "TouchControl_L")
            if (controllerModel != null)
            {
                Destroy(controllerModel);
                controllerModel = null;
            }
            GameObject lcontrolprefab = Resources.Load<GameObject>("TouchControl_L");
            controllerModel = Instantiate(lcontrolprefab, transform, false);
        }
        else if (newindex == ChooseTrackedObjectMenu.TOUCH_INDEX_RIGHT)
        {
            //if (controllerModel != null && controllerModel.name != "TouchControl_R")
            if (controllerModel != null)
            {
                Destroy(controllerModel);
                controllerModel = null;
            }
            GameObject rcontrolprefab = Resources.Load<GameObject>("TouchControl_R");
            controllerModel = Instantiate(rcontrolprefab, transform, false);
        }
        else
        {
            Debug.LogError("Passed invalid index to SetControllerSkin: Valid options are " + ChooseTrackedObjectMenu.TOUCH_INDEX_LEFT +
                " (left controller) or " + ChooseTrackedObjectMenu.TOUCH_INDEX_RIGHT + " (right controller).");
        }
#endif
    }

}
