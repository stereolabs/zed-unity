using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For the ZED 2D Object Detection sample. Handles the canvas elements that are moved and resized
/// to represent objects detected in 2D. 
/// This was designed specifically for the 2D Bounding Box prefab. 
/// </summary>
public class BBox2DHandler : MonoBehaviour
{
    /// <summary>
    /// Text object that displays the ID and distance values of the object. 
    /// </summary>
    [Tooltip("Text object that displays the ID and distance values of the object.")]
    public Text infoText;

    /// <summary>
    /// Outline component around the Image component that surrounds the Text component. 
    /// </summary>
    [Tooltip("Outline component around the Image component that surrounds the Text component.")]
    public Outline boxOutline;

    /// <summary>
    /// All images in the prefab that should be colored when SetColor() is called.
    /// </summary>
    [Tooltip("All images in the prefab that should be colored when SetColor() is called.")]
    public List<Image> imagesToColor = new List<Image>();

    /// <summary>
    /// RawImage object on the prefab to be used to display the object's mask, if enabled. 
    /// </summary>
    [Space(5)]
    [Tooltip("RawImage object on the prefab to be used to display the object's mask, if enabled.")]
    public RawImage maskImage;

    /// <summary>
    /// If true, infoText will display the ID value of the object, assuming it's been set. 
    /// </summary>
    [Space(5)]
    [Tooltip("If true, infoText will display the ID value of the object, assuming it's been set.")]
    public bool showID = true;
    /// <summary>
    /// If true, infoText will display the distance value of the object from its capturing camera, assuming it's been set. 
    /// </summary>
    [Tooltip("If true, infoText will display the ID value of the object, assuming it's been set.")]
    public bool showDistance = true;

    /// <summary>
    /// ID of the object that this instance is currently representing. 
    /// </summary>
    public int currentID { get; private set; }
    /// <summary>
    /// Distance from this object to the ZED camera that detected it. 
    /// </summary>
    public float currentDistance { get; private set; }

    private void OnEnable()
    {
        if(maskImage) //Disable the mask image at first, so you don't see it if we're not applying a mask. 
        {
            maskImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Changes the color of all relevant elements in the prefab (box, text, etc.) to the color provided. 
    /// Ignores input alpha and uses existing alpha of prefab's components. 
    /// </summary>
    public void SetColor(Color col)
    {
        foreach (Image img in imagesToColor)
        {
            float oldimgalpha = img.color.a;
            img.color = new Color(col.r, col.g, col.b, oldimgalpha);
        }

        if (infoText)
        {
            float oldtextalpha = infoText.color.a;
            infoText.color = new Color(col.r, col.g, col.b, oldtextalpha);
        }

        if(boxOutline)
        {
            float oldtextalpha = boxOutline.effectColor.a;
            boxOutline.effectColor = new Color(col.r, col.g, col.b, oldtextalpha);
        }

        if(maskImage)
        {
            float oldmaskalpha = maskImage.color.a;
            maskImage.color = new Color(col.r, col.g, col.b, oldmaskalpha);
        }

    }

    /// <summary>
    /// Sets the ID value that will be displayed on the box's label. 
    /// Usually set when the box first starts representing a detected object. 
    /// </summary>
    public void SetID(int id)
    {
        currentID = id;
        UpdateText(currentID, currentDistance);
    }

    /// <summary>
    /// Sets the distance value that will be displayed on the box's label. 
    /// Designed to indicate the distance from the camera that saw the object. 
    /// Value is expected in meters. Should be updated with each new detection. 
    /// </summary>
    public void SetDistance(float dist)
    {
        currentDistance = dist;
        UpdateText(currentID, currentDistance);
    }

    public void SetMaskImage(Texture mask)
    {
        if(maskImage)
        {
            maskImage.gameObject.SetActive(true);

            maskImage.texture = mask;
        }
    }

    /// <summary>
    /// Updates the text in the label based on the given ID and distance values. 
    /// </summary>
    private void UpdateText(int id, float dist)
    {
        string newtext = "";
        if (showID) newtext += "ID: " + id.ToString();
        if (showID && showDistance) newtext += "\r\n";
        if (showDistance) newtext += dist.ToString("F2") + "m";

        if (infoText) infoText.text = newtext;
    }

    private void OnDestroy()
    {
        if(maskImage) //Makes sure textures left over on the masks are cleaned up. 
        {
            Destroy(maskImage.texture);
        }
    }
}
