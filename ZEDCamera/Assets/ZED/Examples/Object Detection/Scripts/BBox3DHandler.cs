using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Rendering;
#endif

/// <summary>
/// For the ZED 3D Object Detection sample. Handles the cube objects that are moved and resized
/// to represent objects detected in 3D. 
/// This was designed specifically for the 3D Bounding Box prefab, and expects it to use the default
/// Unity cube model, the TopBottomBBoxMat, and a label object that floats adjacently.
/// </summary>
public class BBox3DHandler : MonoBehaviour
{
    /// <summary>
    /// Root transform of the child object that's attached nearby and holds information on the object's ID and distance.
    /// </summary>
    [Header("Label")]
    public Transform labelRoot;
    /// <summary>
    /// Text component that displays the object's ID and distance from the camera. 
    /// </summary>
    [Tooltip("Text component that displays the object's ID and distance from the camera. ")]
    public Text text2D;
    /// <summary>
    /// Background image on the label. 
    /// </summary>
    [Tooltip("Background image on the label.")]
    public Image backgroundImage;
    /// <summary>
    /// Outline component around the background image on the label. Should reference the same object as backgroundImage.
    /// </summary>
    [Tooltip("Outline component around the background image on the label. Should reference the same object as backgroundImage.")]
    public Outline backgroundOutline;
    /// <summary>
    /// The Y difference between the center of the label and the top of the bounding box. 
    /// This is used to keep the box at a consistent position, even as the box itself changes in scale. 
    /// </summary>
    [Tooltip("The Y difference between the center of the label and the top of the bounding box. " +
        "This is used to keep the box at a consistent position, even as the box itself changes in scale.")]
    public float heightFromBoxCeiling = -0.05f;

    /// <summary>
    /// If true, the text2D's color will be changed when you call SetColor().
    /// </summary>
    [Space(2)]
    [Tooltip("If true, the text2D's color will be changed when you call SetColor().")]
    public bool applyColorToText2D = true;
    /// <summary>
    /// If true, the backgroundImage's color will be changed when you call SetColor().
    /// </summary>
    [Tooltip("If true, the backgroundImage's color will be changed when you call SetColor().")]
    public bool applyColorToBackgroundImage = false;
    /// <summary>
    /// If true, the backgroundOutline's color will be changed when you call SetColor().
    /// </summary>
    [Tooltip("If true, the backgroundOutline's color will be changed when you call SetColor().")]
    public bool applyColorToBackgroundOutline = true;

    /// <summary>
    /// If true, the object's ID will be displayed in text2D, assuming it's been updated. 
    /// </summary>
    [Space(5)]
    [Tooltip("If true, the object's ID will be displayed in text2D, assuming it's been updated.")]
    public bool showID = true;
    /// <summary>
    /// If true, the object's distance from the detecting camera will be diplayed in text2D, assuming it's been updated. 
    /// </summary>
    [Tooltip("If true, the object's distance from the detecting camera will be diplayed in text2D, assuming it's been updated.")]
    public bool showDistance = true;

    /// <summary>
    /// If true, the label will increase size when further than maxDistBeforeScaling from each rendering camera so it's never too small. 
    /// </summary>
    [Space(5)]
    [Tooltip("If true, the label will increase size when further than maxDistBeforeScaling from each rendering camera so it's never too small.")]
    public bool useLabelMaxDistScaling = true;
    /// <summary>
    /// If useLabelMaxDistScaling is true, this defines how far the label must be from a rendering camera to scale up. 
    /// </summary>
    [Tooltip("If useLabelMaxDistScaling is true, this defines how far the label must be from a rendering camera to scale up.")]
    public float maxDistBeforeScaling = 12f;
    /// <summary>
    /// Cache for the label's calculated scale. If useLabelMaxDistScaling is enabled, this holds the scale until Camera.onPreRender where the scale is applied. 
    /// </summary>
    private Vector3 thisFrameScale;

    /// <summary>
    /// Lossy (world) scale of the label on start. Used to offset changes in the parent bounding box transform each frame. 
    /// </summary>
    private Vector3 labelStartLossyScale;

    /// <summary>
    /// ID of the object that this instance is currently representing.
    /// </summary>
    private int currentID = -1;
    /// <summary>
    /// Distance from this object to the ZED camera that detected it.
    /// </summary>
    private float currentDistance = -1f;

    /// <summary>
    /// MeshRenderer attached to this bounding box. 
    /// </summary>
    private MeshRenderer rend;

#if ZED_HDRP
    /// <summary>
    /// If using HDRP, you can't move or modify objects on a per-camera basis: all "beginCameraRendering" behavior appears to run before the first camera starts rendering, 
    /// so the object state during the last camera to render is how it appears in all cameras. 
    /// As such, we just pick one single camera (by default the left camera of the first ZEDManager we find) and make labels face that one. 
    /// </summary>
    private Camera labelFacingCamera;
#endif
    #region Shader ID caches
    //We look up the IDs of shader properties once, to save a lookup (and performance) each time we access them. 

    private static int boxBGColorIndex;
    private static int boxTexColorIndex;
    private static int edgeColorIndex;
    private static int xScaleIndex;
    private static int yScaleIndex;
    private static int zScaleIndex;
    private static int floorHeightIndex;
    private static bool shaderIndexIDsSet = false;

    #endregion

    // Use this for initialization
    void Awake ()
    {
        if (!text2D) text2D = labelRoot.GetComponentInChildren<Text>();

        thisFrameScale = labelRoot.localScale;
        labelStartLossyScale = labelRoot.lossyScale;

        if(!shaderIndexIDsSet)
        {
            FindShaderIndexes();
        }

#if !ZED_LWRP && !ZED_HDRP
        Camera.onPreCull += OnCameraPreRender;
#elif ZED_LWRP
        RenderPipelineManager.beginCameraRendering += LWRPBeginCamera;
#elif ZED_HDRP
        ZEDManager manager = FindObjectOfType<ZEDManager>();
        labelFacingCamera = manager.GetLeftCamera();
        RenderPipelineManager.beginFrameRendering += HDRPBeginFrame;
#endif
    }

    private void Update()
    {
        //Position the label so that it stays at a consistent place relative to the box's scale. 
        UpdateLabelScaleAndPosition();
        UpdateBoxUVScales();
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

    /// <summary>
    /// Sets the color of the box and label elements. 
    /// Use this to alternate between several colors if you have multiple detected objects, and
    /// keep them distinguished from one another. Or use to easily customize the visuals however you'd like. 
    /// </summary>
    public void SetColor(Color col)
    {
        if (text2D && applyColorToText2D)
        {
            text2D.color = new Color(col.r, col.g, col.b, text2D.color.a);
        }
        if (backgroundImage && applyColorToBackgroundImage)
        {
            backgroundImage.color = new Color(col.r, col.g, col.b, backgroundImage.color.a);
        }
        if (backgroundOutline && applyColorToBackgroundOutline)
        {
            backgroundOutline.effectColor = new Color(col.r, col.g, col.b, backgroundOutline.effectColor.a);
        }

        ApplyColorToBoxMats(col);
    }

    /// <summary>
    /// Tells the TopBottomBBoxMat material attached to the box what the transform's current scale is, 
    /// so that the UVs can be scaled appropriately and avoid stretching. 
    /// </summary>
    public void UpdateBoxUVScales() //TODO: Cache shader IDs. 
    {
        MeshRenderer rend = GetComponentInChildren<MeshRenderer>();
        if (rend)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty(xScaleIndex))
                {
                    mat.SetFloat(xScaleIndex, transform.lossyScale.x);
                }
                if (mat.HasProperty(yScaleIndex))
                {
                    mat.SetFloat(yScaleIndex, transform.lossyScale.y);
                }
                if (mat.HasProperty(zScaleIndex))
                {
                    mat.SetFloat(zScaleIndex, transform.lossyScale.z);
                }
                if (mat.HasProperty(floorHeightIndex))
                {
                    float height = transform.position.y - transform.lossyScale.y / 2f;
                    mat.SetFloat(floorHeightIndex, height);
                }
            }
        }
    }

    /// <summary>
    /// Adjusts the label's scale and position to compensate for any changes in the bounding box's scale. 
    /// </summary>
    public void UpdateLabelScaleAndPosition()
    {
        float lossyxdif = labelStartLossyScale.x / labelRoot.lossyScale.x;
        float lossyydif = labelStartLossyScale.y / labelRoot.lossyScale.y;
        float lossyzdif = labelStartLossyScale.z / labelRoot.lossyScale.z;

        thisFrameScale = new Vector3(labelRoot.localScale.x * lossyxdif,
            labelRoot.localScale.y * lossyydif,
            labelRoot.localScale.z * lossyzdif);

        labelRoot.localPosition = new Vector3(labelRoot.localPosition.x, 0.5f + heightFromBoxCeiling / transform.localScale.y, labelRoot.localPosition.z);

        if(!useLabelMaxDistScaling) //If we're using this, we don't apply the scale until the OnPreRender event to add additional scaling effects. 
        {
            labelRoot.localScale = thisFrameScale;
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

        if (text2D) text2D.text = newtext;
    }

    /// <summary>
    /// Updates the colors of the 3D box materials to the given color. 
    /// </summary>
    private void ApplyColorToBoxMats(Color col)
    {
        if (!rend) rend = GetComponent<MeshRenderer>();

        Material[] mats = rend.materials;

        if(!shaderIndexIDsSet)
        {
            FindShaderIndexes();
        }

        for (int m = 0; m < mats.Length; m++)
        {
            Material mat = new Material(rend.materials[m]);
            if (mat.HasProperty(boxTexColorIndex))
            {
                float texalpha = mat.GetColor(boxTexColorIndex).a;
                mat.SetColor(boxTexColorIndex, new Color(col.r, col.g, col.b, texalpha));
            }
            if (mat.HasProperty(boxBGColorIndex))
            {
                float bgalpha = mat.GetColor(boxBGColorIndex).a;
                mat.SetColor(boxBGColorIndex, new Color(col.r, col.g, col.b, bgalpha));
            }
            if (mat.HasProperty(edgeColorIndex))
            {
                float bgalpha = mat.GetColor(edgeColorIndex).a;
                mat.SetColor(edgeColorIndex, new Color(col.r, col.g, col.b, bgalpha));
            }
            mats[m] = mat;

        }

        rend.materials = mats;
    }

#if ZED_LWRP
    private void LWRPBeginCamera(ScriptableRenderContext context, Camera rendcam)
    {
        OnCameraPreRender(rendcam);
    }
#elif ZED_HDRP
    private void HDRPBeginFrame(ScriptableRenderContext context, Camera[] rendcams)
    {
        OnCameraPreRender(labelFacingCamera);
    }
#endif

    private void OnCameraPreRender(Camera cam)
    {
        if (!useLabelMaxDistScaling) return;
        if (cam.name.Contains("Scene")) return;

        //float dist = Vector3.Distance(cam.transform.position, labelRoot.transform.position);
        float depth = cam.WorldToScreenPoint(labelRoot.transform.position).z;

        if (depth > maxDistBeforeScaling)
        {
            labelRoot.localScale = thisFrameScale * (depth / maxDistBeforeScaling);
        }
        else
        {
            labelRoot.localScale = thisFrameScale;
        }
    }

    /// <summary>
    /// Finds and sets the static shader indexes for the properties that we'll set. 
    /// Used so we can call those indexes when we set the properties, which avoids a lookup
    /// and increases performance. 
    /// </summary>
    private static void FindShaderIndexes()
    {
        boxBGColorIndex = Shader.PropertyToID("_BGColor");
        boxTexColorIndex = Shader.PropertyToID("_Color");
        edgeColorIndex = Shader.PropertyToID("_EdgeColor");
        xScaleIndex = Shader.PropertyToID("_XScale");
        yScaleIndex = Shader.PropertyToID("_YScale");
        zScaleIndex = Shader.PropertyToID("_ZScale");
        floorHeightIndex = Shader.PropertyToID("_FloorHeight");

        shaderIndexIDsSet = true;
    }

    private void OnDestroy()
    {
#if !ZED_LWRP && !ZED_HDRP
        Camera.onPreCull -= OnCameraPreRender;
#elif ZED_LWRP
        RenderPipelineManager.beginCameraRendering -= LWRPBeginCamera;
#elif ZED_HDRP

        RenderPipelineManager.beginFrameRendering -= HDRPBeginFrame;
#endif
    }
}
