using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for the visible, interactible transform controls in the ZED MR Calibration scene. 
/// Handles the visuals, enabling/disabling objects when grabbed or let go, and knows what grabbed it. 
/// Behaviors to actually move the ZED are in inheriting classes. 
/// </summary>
public abstract class TransformGrabbable : MonoBehaviour, IXRHoverable, IXRGrabbable
{
    /// <summary>
    /// Calls SetActive(true) on all objects when a grab starts, and SetActive(false) when it ends. 
    /// <para>Used to enable/disable the transform and limit indicators.</para>
    /// </summary>
    [Tooltip("Calls SetActive(true) on all objects when a grab starts, and SetActive(false) when it ends. " +
        "Used to enable/disable the transform and limit indicators.")]
    public List<GameObject> enableOnlyWhenGrabbed = new List<GameObject>();

    /// <summary>
    /// Material applied when ZEDXRGrabber hovers over it. If empty, a default one is loaded from Resources. 
    /// </summary>
    [Tooltip("Material applied when ZEDXRGrabber hovers over it. If empty, a default one is loaded from Resources. ")]
    public Material hoverMaterial;
    private Dictionary<Renderer, Material> baseMaterials = new Dictionary<Renderer, Material>();

    protected Transform grabbingTransform;
    protected bool isGrabbed
    {
        get
        {
            return grabbingTransform != null;
        }
    }

    /// <summary>
    /// Make sure we have a hovermat assigned. 
    /// </summary>
    protected virtual void Awake()
    {
        if (!hoverMaterial)
        {
            hoverMaterial = Resources.Load<Material>("HoverMat");
        }
    }

    /// <summary>
    /// Returns the transform. Exists as the IXRGrabbable and IXRHoverable interfaces can't inherit from Monobehaviours. 
    /// </summary>
    /// <returns></returns>
    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// Apply the hover material to all Renderers on and parented to this transform. 
    /// </summary>
    void IXRHoverable.OnHoverStart()
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (!baseMaterials.ContainsKey(rend))
            {
                baseMaterials.Add(rend, rend.material);
            }

            rend.material = hoverMaterial;
        }
    }

    /// <summary>
    /// Return the original materials to all the Renderers on and parented to this transform. 
    /// </summary>
    void IXRHoverable.OnHoverEnd()
    {
        if (this == null) return;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (baseMaterials.ContainsKey(rend))
            {
                rend.material = baseMaterials[rend];
            }
            else Debug.LogError("Starting material for " + rend.gameObject + " wasn't cached before hover started.");
        }
    }

    /// <summary>
    /// What happens when ZEDXRGrabber first grabs it. From IXRGrabbable. 
    /// </summary>
    public virtual void OnGrabStart(Transform grabtransform)
    {
        grabbingTransform = grabtransform;

        //Enable all indicators.
        foreach (GameObject go in enableOnlyWhenGrabbed)
        {
            go.SetActive(true);
        }
    }

    /// <summary>
    /// What happens when ZEDXRGrabber stops grabbing it. From IXRGrabbable. 
    /// </summary>
    public virtual void OnGrabEnd()
    {
        grabbingTransform = null;

        //Disable all indicators.
        foreach (GameObject go in enableOnlyWhenGrabbed)
        {
            go.SetActive(false);
        }
    }

}
