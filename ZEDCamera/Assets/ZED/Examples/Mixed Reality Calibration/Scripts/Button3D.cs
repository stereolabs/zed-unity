using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clickable 3D object button that will do something when clicked by a ZEDXRGrabber. 
/// Governs only hover-ability, click-ability, and related graphics. Inherited by ClickButton and ToggleButton, which add logic.  
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(MeshRenderer))]
public abstract class Button3D : MonoBehaviour, IXRHoverable, IXRClickable
{
    /// <summary>
    /// Material applied when a ZEDXRGrabber hovers over it. 
    /// </summary>
    [Tooltip("Material applied when a ZEDXRGrabber hovers over it. ")]
    public Material hoverMaterial;

    protected Material buttonMat;
    protected MeshRenderer mRenderer;
    protected Collider col;

    /// <summary>
    /// Scale applied to the transform when clicked, to handle its depressed state. 
    /// <para>This scale is not applied by Button3D itself - only implemented in child classes.</para>
    /// </summary>
    [Tooltip("Scale applied to the transform when clicked, to handle its depressed state.")]
    public Vector3 pressedScaleMult = new Vector3(1, 0.5f, 1); 
    protected Vector3 startScale;

    /// <summary>
    /// Brightness setting of the material when not clicked/toggled/etc. Assumes using a shader with a _Brightness property.
    /// <para>Not actually set by Button3D; only child classes.</para>
    /// </summary>
    [Space(5)]
    [Tooltip("Brightness setting of the material when not clicked/toggled/etc. Assumes using a shader with a _Brightness property.")]
    [Range(0, 1)]
    public float unpressedDarkness = 1f;
    /// <summary>
    /// Brightness setting of the material when clicked/toggled/etc. Assumes using a shader with a _Brightness property.
    /// <para>Not actually set by Button3D; only child classes.</para>
    /// </summary>
    [Tooltip("Brightness setting of the material when clicked/toggled/etc. Assumes using a shader with a _Brightness property.")]
    [Range(0, 1)]
    public float pressedDarkness = 0.15f;

    /// <summary>
    /// Value of _Brightness property on attached material. 
    /// </summary>
    protected float brightness
    {
        get
        {
            return buttonMat.GetFloat("_Brightness");
        }
        set
        {
            buttonMat.SetFloat("_Brightness", value);
        }
    }

    // Use this for initialization
    protected virtual void Awake ()
    {
        mRenderer = GetComponent<MeshRenderer>();
        col = GetComponent<Collider>();
        col.isTrigger = true;

        //Make the button material an instance so we can modify its properties individually. 
        buttonMat = new Material(mRenderer.material);
        mRenderer.material = buttonMat;

        if (!hoverMaterial)
        {
            hoverMaterial = Resources.Load<Material>("HoverMat");
        }

        startScale = transform.localScale;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public abstract void OnClick(ZEDXRGrabber clicker);

    void IXRHoverable.OnHoverStart()
    {
        mRenderer.material = hoverMaterial;
    }

    void IXRHoverable.OnHoverEnd()
    {
        mRenderer.material = buttonMat;
    }
}
