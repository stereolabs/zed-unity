using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a prefab 3D model of the ZED camera or ZED Mini, depending on which is connected. 
/// Used in the MR Calibration scene to visualize its position relative to a tracked object. 
/// </summary>
public class CreateZEDModel : MonoBehaviour
{
    /// <summary>
    /// Prefab of a scaled model of the original ZED camera. 
    /// </summary>
    [Tooltip("Prefab of a scaled model of the original ZED camera.")]
    public GameObject zedModelPrefab;
    /// <summary>
    /// Prefab of a scaled model of the ZED Mini camera.
    /// </summary>
    [Tooltip("Prefab of a scaled model of the ZED Mini camera. ")]
    public GameObject zedMiniModelPrefab;

    /// <summary>
    /// If true, the object is centered on the physical center of the camera. Otherwise, it's centered
    /// on the left lens, which is the main reference point of the ZED's position and the origin of the 
    /// left (primary) video feed. 
    /// </summary>
    [Tooltip("If true, the object is centered on the physical center of the camera. Otherwise, it's centered " +
        "on the left lens, which is the main reference point of the ZED's position and the origin of the " +
        "left (primary) video feed.")]
    public bool centerOnGeometry = false;

    /// <summary>
    /// Camera ID to use to determine which ZED model to display. Should always be 1 if you only have one ZED connected. 
    /// </summary>
    [Tooltip("Camera ID to use to determine which ZED model to display. Should always be 1 if you only have one ZED connected.")]
    public sl.ZED_CAMERA_ID cameraID = sl.ZED_CAMERA_ID.CAMERA_ID_01;

    private GameObject zedModelObject;

    private ZEDManager zedManager
    {
        get
        {
            return ZEDManager.GetInstance(cameraID);
        }
    }

    // Use this for initialization
    void Awake()
    {
        if (zedManager != null)
        {
            //If ZEDManager is already ready, make the model. Otherwise, listen for it to be ready. 
            if (zedManager.IsZEDReady) CreateModel();

            zedManager.OnZEDReady += CreateModel; //Still subscribe anyway in case the ZED is replaced with a different model. 
        }
    }

    /// <summary>
    /// Instantiates the proper prefab for the ZED or ZED Mini as a child of this object. 
    /// </summary>
    private void CreateModel()
    {
        if (zedModelObject != null) Destroy(zedModelObject); //Shouldn't happen but just in case. 

        sl.MODEL model = zedManager.zedCamera.CameraModel;

        if (model == sl.MODEL.ZED)
        {
            if (!zedModelPrefab)
            {
                Debug.LogError("CameraAnchor tried to spawn ZED Model but zedModelPrefab is null.");
                return;
            }

            zedModelObject = Instantiate(zedModelPrefab, transform);
            zedModelObject.layer = gameObject.layer;

        }
        else if (model == sl.MODEL.ZED_M)
        {
            if (!zedMiniModelPrefab)
            {
                Debug.LogError("CameraAnchor tried to spawn ZED Mini Model but zedMiniModelPrefab is null.");
                return;
            }

            zedModelObject = Instantiate(zedMiniModelPrefab, transform, false);
        }

        if (zedModelObject != null && centerOnGeometry == true)
        {
            float halfbaseline = zedManager.zedCamera.Baseline / 2f;
            zedModelObject.transform.localPosition = new Vector3(-halfbaseline, 0, 0);
        }

    }

    private void OnDestroy()
    {
        if(zedManager != null) zedManager.OnZEDReady -= CreateModel;
    }
}
