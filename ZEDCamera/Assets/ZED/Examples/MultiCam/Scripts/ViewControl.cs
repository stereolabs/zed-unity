using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ViewControl : MonoBehaviour {

    [Header("Camera")]
    public Camera cam;
    public CanvasScaler canvasScl; //Leave empty if no UI canvas to attach to this camera
	ViewControl[] camControllers;

    [Header("Scale")]
	public Vector2 camScale = new Vector2 (0.45f, 0.45f);
    [Header("Position")]
    public Vector2 camPos;


    /// <summary>
    /// Get needed variables.
    /// </summary>
    private void Start()
    {
        cam = GetComponent<Camera>();
		camControllers = FindObjectsOfType<ViewControl>();
    }


	private void Update()
    {
        //Update this camera's rect
        cam.rect = new Rect(camPos, camScale);
        //And its canvas
        if(canvasScl != null)
        canvasScl.scaleFactor = camScale.x;
    }


}
