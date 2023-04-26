using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    public GameObject controllerModel;

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


    void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (checkDeviceIndexEachUpdate)
        {
            if(controllerTracker.deviceToTrack == ZEDControllerTracker.Devices.LeftController)
            {
                SetRenderModelIndex(ChooseTrackedObjectMenu.LEFT);
            }
            else if (controllerTracker.deviceToTrack == ZEDControllerTracker.Devices.RightController)
            {
                SetRenderModelIndex(ChooseTrackedObjectMenu.RIGHT);
            }
        }
	}

    /// <summary>
    /// Makes the controller update its model based on the specified controller index. 
    /// This index is the actual tracked object index in SteamVR, and in Oculus, corresponds to
    /// ChooseTrackedObjectMenu.TOUCH_INDEX_LEFT and _RIGHT.
    /// </summary>
    public void SetRenderModelIndex(int newindex)
    {
        if (newindex == ChooseTrackedObjectMenu.LEFT)
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
        else if (newindex == ChooseTrackedObjectMenu.RIGHT)
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
            Debug.LogError("Passed invalid index to SetControllerSkin: Valid options are " + ChooseTrackedObjectMenu.LEFT +
                " (left controller) or " + ChooseTrackedObjectMenu.RIGHT + " (right controller).");
        }
    }
}
