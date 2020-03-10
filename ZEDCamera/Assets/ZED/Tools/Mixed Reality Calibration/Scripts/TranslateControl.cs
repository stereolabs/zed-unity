using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Receives translations from separate TranslateArrow objects, which should be child objects, 
/// and applies them to the ZED via CameraAnchor. Also moves the arrow holder object accordingly.
/// When those children are dragged, the distance from this transform affects how quickly the ZED moves.
/// See parent class TransformControl to see how visual elements work. 
/// </summary>
public class TranslateControl : TransformControl
{
    /// <summary>
    /// Max distance that you can drag the arrows on any axis. 
    /// </summary>
    public float maxUIMoveDistance = 0.1f; 

    private Vector3 startPosLocal = Vector3.zero;

    protected override void Awake ()
    {
        base.Awake();

        if (!anchor) anchor = FindObjectOfType<CameraAnchor>(); //For now. 
        CameraAnchor.OnCameraAnchorCreated += SetNewAnchor;

        startPosLocal = visualsParent.localPosition;
	}

    /// <summary>
    /// Takes how far a given arrow has been moved, and turns it to a call to CameraAnchor
    /// to move the ZED gradually. 
    /// </summary>
    public void Translate(Vector3 localdistmoved)
    {
        Vector3 oldvector = visualsParent.localPosition;

        //Reflect translation in UI element (this thing). 
        Vector3 clampdist;
        clampdist.x = Mathf.Clamp(localdistmoved.x, -maxUIMoveDistance, maxUIMoveDistance);
        clampdist.y = Mathf.Clamp(localdistmoved.y, -maxUIMoveDistance, maxUIMoveDistance);
        clampdist.z = Mathf.Clamp(localdistmoved.z, -maxUIMoveDistance, maxUIMoveDistance);

        //transform.localPosition = startPosLocal + transform.rotation * clampdist;
        visualsParent.localPosition = startPosLocal + clampdist;

        //Get clamped value and send to anchor to move the actual ZED. 
        if (anchor)
        {
            Vector3 finaldist = clampdist / maxUIMoveDistance;
            anchor.TranslateZEDIncrementally(finaldist);
        }

        PlayTapSoundIfNeeded(oldvector / maxUIMoveDistance, visualsParent.localPosition / maxUIMoveDistance);
    }

    /// <summary>
    /// Changes the anchor object. 
    /// </summary>
    private void SetNewAnchor(CameraAnchor newanchor)
    {
        anchor = newanchor;
        //TODO: Reposition arrows accordingly. 
    }

    /// <summary>
    /// Displays the instructions for calibration in manual mode.
    /// <para>his could have been put in any class used for manual transform mode exclusively - this was chosen arbitrarily.1</para>T
    /// </summary>
    private void OnEnable()
    {
        MessageDisplay.DisplayMessageAll("MANUAL MODE\r\nGrab the colored arrows/circles to move the ZED.\r\n" + 
            "Position the 3D model of the ZED like it is in real-life.");
    }
    
}
