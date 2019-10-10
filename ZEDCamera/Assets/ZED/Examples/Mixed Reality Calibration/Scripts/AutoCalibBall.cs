using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the clickable ball objects spawned by AutoCalibrationManager, which lets the user set reference points.
/// Should be assigned to a prefab and instantiated with it. 
/// Also draws the current primary controller model in its center, facing the camera. 
/// </summary>
[RequireComponent(typeof(Collider))]
public class AutoCalibBall : MonoBehaviour, IXRHoverable, IXRClickable
{
    /// <summary>
    /// Reference to the manager that spawned this ball. Used for reporting its real and virtual positions when assigned.
    /// </summary>
    [Tooltip("Reference to the manager that spawned this ball. Used for reporting its real and virtual positions when assigned.")]
    public AutoCalibrationManager autoCalib;

    /// <summary>
    /// Material used to draw the controller object each frame. 
    /// </summary>
    [Space(5)]
    [Tooltip("Material used to draw the controller object each frame. ")]
    public Material controllerDrawMat;

    /// <summary>
    /// Reference to the object that's enabled when this ball has been used to add a reference point. Checkmark by default. 
    /// </summary>
    [Space(5)]
    [Tooltip("Reference to the object that's enabled when this ball has been used to add a reference point. Checkmark by default. ")]
    public GameObject checkmarkDisplay;
    /// <summary>
    /// Reference to the object that's enabled before this ball has been used to add a reference point. Cross by default. 
    /// </summary>
    [Tooltip("Reference to the object that's enabled before this ball has been used to add a reference point. Cross by default. ")]
    public GameObject crossDisplay;

    private DisplayState currentStage = DisplayState.Cross;

    private Vector3 virtualPos;
    private Vector3 realPos;
    /// <summary>
    /// Whether or not the user has assigned a reference using this ball. Used for display purposes. 
    /// </summary>
    private bool hasBeenSet = false; 

    /// <summary>
    /// Unique index of the ball, to be used to set the real and virtual positions to the proper index within AutoCalibrationManager.
    /// </summary>
    private int ballIndex;
    /// <summary>
    /// Whether or not AutoCalibrationManager has called Setup() on this object. Necessary before it can be used.  
    /// </summary>
    private bool isSetup = false;

    private Collider col;

    private ZEDManager zedManager
    {
        get
        {
            return autoCalib.zedManager;
        }
    }

    /// <summary>
    /// Material applied to the outside ball when hovered over by ZEDXRGrabber. Loads one from the Resources folder if not set manually. 
    /// </summary>
    [Space(5)]
    [Tooltip("Material applied to the outside ball when hovered over by ZEDXRGrabber. Loads one from the Resources folder if not set manually. ")]
    public Material hoverMaterial;
    private Dictionary<Renderer, Material> baseMaterials = new Dictionary<Renderer, Material>();

    public void Awake()
    {
        if(!hoverMaterial)
        {
            hoverMaterial = Resources.Load<Material>("HoverMatAlpha");
        }

        col = GetComponent<Collider>();
        col.isTrigger = true;

        //Make the proper overhead display appear (the checkmark or cross)
        SetBallStateDisplay(currentStage); //Should be NotSet. 
    }

    /// <summary>
    /// Assigns the manager and index for this ball, both necessary before it can be used to set a reference point. 
    /// </summary>
    public void Setup(AutoCalibrationManager calibmanager, int ballindex)
    {
        autoCalib = calibmanager;
        ballIndex = ballindex;

        isSetup = true;
    }

    public void Update()
    {
        //Draw the primary hand controller on the inside. 
        SetControllerSkin skin = PrimaryHandSwitcher.primaryHandObject.GetComponentInChildren<SetControllerSkin>();
        if(skin != null)
        {
            Mesh mesh = skin.GetFirstControllerMesh();
            if (mesh)
            {
                Quaternion drawrot = Quaternion.LookRotation(zedManager.transform.position - transform.position, Vector3.up);
                Vector3 drawpos = transform.position - drawrot * mesh.bounds.center;
                

                controllerDrawMat.SetPass(0);
                Graphics.DrawMesh(mesh, drawpos, drawrot, controllerDrawMat, CameraAnchor.HIDE_FROM_ZED_LAYER);
            }
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// Starts the process of adding a virtual and real reference point. Only called when first activated, as the collider is disabled afterward. 
    /// </summary>
    /// <param name="clicker"></param>
    void IXRClickable.OnClick(ZEDXRGrabber clicker)
    {
        //TODO: Graphics stuff. 
        StartCoroutine(PlacingBall(clicker));
    }

    /// <summary>
    /// Applies the hover material to the outside ball and temporarily hides the overhead checkmark/cross.
    /// </summary>
    void IXRHoverable.OnHoverStart()
    {
        SetBallStateDisplay(hasBeenSet ? DisplayState.Checkmark : DisplayState.Nothing);

        foreach (Renderer rend in GetComponentsInChildren<Renderer>()) //TODO: Lots of code repetition here. Make a static utility somewhere. 
        {
            if (!baseMaterials.ContainsKey(rend))
            {
                baseMaterials.Add(rend, rend.material);
            }

            rend.material = hoverMaterial;
        }
    }

    /// <summary>
    /// Removes the hover material from the outside ball and re-enables the checkmark or cross, depending on whether it's been set up. 
    /// </summary>
    void IXRHoverable.OnHoverEnd()
    {
        if (col.enabled == true)
        {
            SetBallStateDisplay(hasBeenSet ? DisplayState.Checkmark : DisplayState.Cross);
        }

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
    /// Handles the process of adding a reference point. First logs the starting point as the "virtual" point, and freezes the 
    /// 2D video so the user can align the virtual controller with the real-world image. When they click again, this adds
    /// the "real" point and sends it to the AutoCalibrationManager. 
    /// </summary>
    private IEnumerator PlacingBall(ZEDXRGrabber clicker)
    {
        if(!isSetup)
        {
            throw new System.Exception("Tried to use AutoCalibBall to calibrate before Setup was called on it.");
        }
        col.enabled = false;

        SetBallStateDisplay(DisplayState.Nothing);

        //Display new message.
        MessageDisplay.DisplayMessageAll("AUTOMATIC MODE\r\nLine up the virtual controller with its real-world counterpart. Then click again.");

        virtualPos = zedManager.transform.InverseTransformPoint(clicker.transform.position);

        //DrawOutputToPlane.pauseTextureUpdate = true; //Pause the video
        zedManager.pauseLiveReading = true; 
        //TODO: Graphics stuff.
        //TODO: Hide all other calibration balls. 
        //print("Paused - " + ballIndex);

        //First wait to make sure the click key is no longer held, so we can click it a second time separately. 
        while (clicker.zedController.CheckClickButton(ControllerButtonState.Down))
        {
            yield return null;
        }

        //Now wait for it to go down again. 
        while (!clicker.zedController.CheckClickButton(ControllerButtonState.Down))
        {
            yield return null;
        }

        realPos = zedManager.transform.InverseTransformPoint(clicker.transform.position);

        autoCalib.AddNewPositions(ballIndex, virtualPos, realPos);

        zedManager.pauseLiveReading = false; 
        col.enabled = true;

        MessageDisplay.DisplayMessageAll("AUTOMATIC MODE\r\nRepeat this process with the rest of the balls, or stop when you're satisfied.");
        hasBeenSet = true;
        SetBallStateDisplay(DisplayState.Checkmark);
    }

    /// <summary>
    /// Enables/disables the overhead checkmark or cross, depending on the provided ball stage. 
    /// </summary>
    /// <param name="newstage"></param>
    private void SetBallStateDisplay(DisplayState newstage)
    {
        switch(newstage)
        {
            case DisplayState.Cross:
                if (checkmarkDisplay) checkmarkDisplay.SetActive(false);
                if (crossDisplay) crossDisplay.SetActive(true);
                break;
            default:
            case DisplayState.Nothing:
                if (checkmarkDisplay) checkmarkDisplay.SetActive(false);
                if (crossDisplay) crossDisplay.SetActive(false);
                break;
            case DisplayState.Checkmark:
                if (checkmarkDisplay) checkmarkDisplay.SetActive(true);
                if (crossDisplay) crossDisplay.SetActive(false);
                break;
        }

        currentStage = newstage;
    }

    private void OnDestroy()
    {
        StopAllCoroutines(); //Prevents weird stuff from happening if you leave Automatic mode while adding a reference point. 
    }

    /// <summary>
    /// Options for what to display overhead. Used by SetBallStateDisplay.  
    /// </summary>
    private enum DisplayState
    {
        Nothing,
        Cross,
        Checkmark
    }
}
