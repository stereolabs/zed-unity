using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles an interactable button spawned by ChooseTrackedObjectMenu for choosing which
/// object the ZED is anchored to. Should be attached to a prefab. 
/// Each is assigned a specific tracked object, like a Vive Tracker or controller. 
/// </summary>
public class ChooseTrackedObjectButton : MonoBehaviour, IXRHoverable, IXRClickable
{
    /// <summary>
    /// Index of the tracked device that this object represents. 
    /// Note that if using the Oculus platform, this is defined by constants in ChooseTrackedObjectMenu, TOUCH_INDEX_LEFT and _RIGHT.
    /// </summary>
    [HideInInspector]
    public int deviceIndex = -1;

    /// <summary>
    /// 3D text object attached to this hbject, used to label it as a Left Controller, Tracker, etc. 
    /// </summary>
    [Tooltip("3D text object attached to this hbject, used to label it as a Left Controller, Tracker, etc. ")]
    public TextMesh labelText;

    /// <summary>
    /// All objects that should be enabled when ZEDXRGrabber is hovering over it, and disabled otherwise. 
    /// Used to enable a yellow box when hovered over. Done this way to give more flexibility to the prefab. 
    /// </summary>
    [Space(5)]
    [Tooltip("All objects that should be enabled when ZEDXRGrabber is hovering over it, and disabled otherwise. " +
        "Used to enable a yellow box when hovered over. Done this way to give more flexibility to the prefab.")]
    public List<GameObject> enabledWhenHighlighted = new List<GameObject>();

    /// <summary>
    /// How quickly the controller renderer in the middle of the prefab spins around. 
    /// </summary>
    [Space(5)]
    [Tooltip("How quickly the controller renderer in the middle of the prefab spins around. ")]
    public float secondsPerRevolution = 10f;
    /// <summary>
    /// The axis around which the controller renderer in the middle of the prefab spins.
    /// </summary>
    [Tooltip("The axis around which the controller renderer in the middle of the prefab spins.")]
    public Vector3 rotationAxis = Vector3.up;

    private SetControllerSkin skin;

    /// <summary>
    /// Delegate for an event that supplies this device index. 
    /// </summary>
    public delegate void TrackedObjectSelectedDelegate(int deviceindex);
    /// <summary>
    /// Event called when the user has clicked on this button to choose its tracked object. 
    /// </summary>
    public event TrackedObjectSelectedDelegate OnTrackedObjectSelected;

    public void Awake()
    {
        //if (!controllerTracker) Debug.LogError(gameObject + " controllerTracker value not set.");

        skin = GetComponentInChildren<SetControllerSkin>();
        if (!skin) skin = gameObject.AddComponent<SetControllerSkin>();
        skin.checkDeviceIndexEachUpdate = false;

        if (deviceIndex != -1) SetDeviceIndex(deviceIndex);

        //Make sure we have a collider. 
        Collider collider = GetComponent<Collider>();
        if (!collider)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)collider).radius = 0.15f;
        }
        collider.isTrigger = true;

        if(!labelText)
        {
            labelText = GetComponentInChildren<TextMesh>();
        }
    }

    /// <summary>
    /// Changes the text on the attached 3D text field. Used to indicate what kind of object it is (Tracker, Left Controller, etc.)
    /// </summary>
    public void SetLabel(string text)
    {
        if (!labelText) return;

        labelText.text = text;
    }

    private void Update()
    {
        //Rotate the controller object slightly. 
        if (rotationAxis.sqrMagnitude > 0f)
        {
            float degrees = 360f / secondsPerRevolution * Time.deltaTime;
            skin.transform.Rotate(rotationAxis, degrees, Space.World);

            Mesh mesh = skin.GetFirstControllerMesh();
            if (mesh)
            {
                Vector3 center = mesh.bounds.center;
                skin.transform.localPosition = -(skin.transform.localRotation * center);
            }
        }
    }

    /// <summary>
    /// Change this device's index and change the controller renderer's model accordingly. 
    /// </summary>
    /// <param name="index"></param>
    public void SetDeviceIndex(int index)
    {
        deviceIndex = index;
        skin.SetRenderModelIndex(index);
    }


    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// Invokes the OnTrackedObjectSelected event to signal that the user chose this tracked object. 
    /// </summary>
    /// <param name="clicker"></param>
    void IXRClickable.OnClick(ZEDXRGrabber clicker)
    {
        if(OnTrackedObjectSelected != null)
        {
            OnTrackedObjectSelected.Invoke(deviceIndex);
        }
    }

    /// <summary>
    /// Enables the hover indicator object(s). 
    /// </summary>
    void IXRHoverable.OnHoverStart()
    {
        foreach(GameObject go in enabledWhenHighlighted)
        {
            go.SetActive(true);
        }
    }

    /// <summary>
    /// Disables the hover indicator object(s).
    /// </summary>
    void IXRHoverable.OnHoverEnd()
    {
        foreach (GameObject go in enabledWhenHighlighted)
        {
            go.SetActive(false);
        }
    }

}
