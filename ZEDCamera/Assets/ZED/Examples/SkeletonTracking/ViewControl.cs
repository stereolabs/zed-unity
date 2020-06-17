using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace sl
{
public class ViewControl : MonoBehaviour {

    [Header("Camera")]
    public Camera cam;
    public CanvasScaler canvasScl; //Leave empty if no UI canvas to attach to this camera
	ViewControl[] camControllers;

    [Header("Scale")]
    public Vector2 startScale = new Vector2(0.2f, 0.2f);
    public Vector2 endScale = new Vector2(1f, 1f);
    private Vector2 camScale;
    [Header("Position")]
    public Vector2 startPos = new Vector2(0.33f, 0.01f);
    public Vector2 endPos = new Vector2(0f, 0f);
    private Vector2 camPos;

    //Can the camera rect be moved.
    bool canMove;

    //Can the camera rect be scaled.
    bool canScale;

    //Is this camera rect being selected.
    public bool selected;

    // starting value for the Lerp
    static float t = 0.0f;

    //Is this camera zoomed in.
    public bool zoomIn;

    /// <summary>
    /// Get needed variables.
    /// </summary>
    private void Start()
    {
        cam = GetComponent<Camera>();
		camControllers = FindObjectsOfType<ViewControl>();
    }

    //Storing cursor Pos on input for correctly repositionning the cam rect.
    Vector2 tmpCursorPos;
    //Counting mouse click for when zooming in or out.
    int clickCount;
    //Counting time frame for double click zoom in/out.
    float timer;
    //Storing cursor position on double click to check if it left after the 1st input.
    Vector3 oldPos;
    //Special time frame where no click is allowed.
    float noClickTimeFrame;

    private void Update()
    {
        //Change Camera Layer mask to display or not certain layers
        if (cam.name.Contains("Left"))
        {
            if ((cam.cullingMask & (1 << 20)) != 0)
            {
                var newMask1 = cam.cullingMask & ~(1 << 20);
                cam.cullingMask = newMask1;
                var newMask2 = cam.cullingMask & ~(1 << 21);
                cam.cullingMask = newMask2;
            }
        }
        if (cam.name.Contains("Virtual"))
        {
            if ((cam.cullingMask | (1 << 20)) != 0)
            {
                var newMask1 = cam.cullingMask | (1 << 20);
                cam.cullingMask = newMask1;
            } 
        }

        // animate the position and scale of the camera...
        if (zoomIn == true)
        {
            camScale = new Vector2(Mathf.Lerp(camScale.x, endScale.x, t), Mathf.Lerp(camScale.y, endScale.y, t));
            camPos = new Vector2(Mathf.Lerp(camPos.x, endPos.x, t), Mathf.Lerp(camPos.y, endPos.y, t));
        }
        else if(zoomIn == false)
        {
            camScale = new Vector2(Mathf.Lerp(camScale.x, startScale.x, t), Mathf.Lerp(camScale.y, startScale.y, t));
            camPos = new Vector2(Mathf.Lerp(camPos.x, startPos.x, t), Mathf.Lerp(camPos.y, startPos.y, t));
        }


        // .. and increase the t interpolater
        t += 0.5f * Time.deltaTime;

        //No Click Allowed For X seconds After Zooming In or Out
        noClickTimeFrame += Time.deltaTime;
        if (noClickTimeFrame > 0.5f)
        {
            //If on input the cursor is inside this camera rect.
            if (Input.GetKeyDown(KeyCode.Mouse0) && cam.pixelRect.Contains(Input.mousePosition))
            {
                //Check if another camera has been selected
                int selectedCameras = 0;
                float tmpDepth = 0;
                for (int i = 0; i < camControllers.Length; i++)
                {
                    if (camControllers[i].selected)
                        selectedCameras++;
                    if (camControllers[i].cam.depth > tmpDepth)
                        tmpDepth = camControllers[i].cam.depth;
                }

                //A camera has already been selected
                if (selectedCameras != 0)
                {
                    selected = false;
                    
                    //If that cameras depth is the same as "this" camera, then ignore it and take "this" one.
                    if(tmpDepth == 2 && cam.depth == 2)
                    {
                        for (int i = 0; i < camControllers.Length; i++)
                        {
                            camControllers[i].selected = false;
                        }
                        selected = true;
                        //Reset double click timer
                        t = 0f;
                        tmpCursorPos = (Vector2)Input.mousePosition - cam.pixelRect.position;

                        //Allow for this camera rect to be move only under a certain screen size.
                        if (cam.rect.width <= 0.9f && cam.rect.height <= 0.9f)
                            canMove = true;
                        //If this is the second click, save the cursor position, to be compared inside "OnKeyUp".
                        if (clickCount == 1)
                            oldPos = Input.mousePosition;
                        //Hide the cursor
                        Cursor.visible = false;
                    }
                }
                else //Select this camera
                {
                    selected = true;
                    //Reset double click timer
                    t = 0f;
                    tmpCursorPos = (Vector2)Input.mousePosition - cam.pixelRect.position;

                    //Allow for this camera rect to be move only under a certain screen size.
                    if (cam.rect.width <= 0.9f && cam.rect.height <= 0.9f)
                        canMove = true;
                    //If this is the second click, save the cursor position, to be compared inside "OnKeyUp".
                    if (clickCount == 1)
                        oldPos = Input.mousePosition;
                    //Hide the cursor
                    Cursor.visible = false;
                }
            }

            //If on input the cursor is inside this camera rect.
            if (Input.GetKeyDown(KeyCode.Mouse1) && cam.pixelRect.Contains(Input.mousePosition))
            {
                //And the cam rect insn't full screen.
                if (cam.pixelRect.width == Screen.width && cam.pixelRect.height == Screen.height && cam.depth == 1)
                {
                    /* 
                     * What's inside OptionView.cs
                    */
                }
                else
                {
                    //Check if another camera has been selected.
                    int selectedCameras = 0;
                    for (int i = 0; i < camControllers.Length; i++)
                    {
                        if (camControllers[i].selected)
                            selectedCameras++;
                    }
                    if (selectedCameras != 0)
                        selected = false;
                    //If there's none, make this one selected.
                    else
                    {
                        selected = true;
                        canScale = true;
                        t = 0f;
                        Cursor.visible = false;
                    }
                }
            }

            //Double Click Zoom
            timer += Time.deltaTime;
            if (timer > 0.5f)
            {
                timer = 0f;
                clickCount = 0;
            }

            //EDIT CAM RECT POSITION
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (canMove && !zoomIn)
                {
                    startPos.x = (Input.mousePosition.x - tmpCursorPos.x) / Screen.width;
                    startPos.y = (Input.mousePosition.y - tmpCursorPos.y) / Screen.height;

                    //Maximum Position
                    if ((Input.mousePosition.x - tmpCursorPos.x) + cam.pixelRect.width > Screen.width)
                    {
                        startPos.x = (Screen.width - cam.pixelRect.width) / Screen.width;
                    }
                    if((Input.mousePosition.y - tmpCursorPos.y) + cam.pixelRect.height > Screen.height)
                    {
                        startPos.y = (Screen.height - cam.pixelRect.height) / Screen.height;
                    }

                    //Minimum Position
                    if (Input.mousePosition.x - tmpCursorPos.x < 0)
                    {
                        startPos.x = 0;
                    }
                    if (Input.mousePosition.y - tmpCursorPos.y < 0)
                    {
                        startPos.y = 0;
                    }
                }
            }

            //EDIT CAM RECT SCALE
            if (Input.GetKey(KeyCode.Mouse1))
            {
                if (canScale && !zoomIn)
                {
                    startScale.x = Input.mousePosition.x / Screen.width;
                    startScale.y = startScale.x;

                    //Max scale
                    if (startScale.x > 0.5f)
                    {
                        startScale = new Vector2(0.5f, 0.5f);
                    }
                    //Min scale
                    else if(startScale.x < 0.25f)
                    {
                        startScale = new Vector2(0.25f, 0.25f);
                    }
                }               
            }

            //ON KEY UP
            if (Input.GetKeyUp(KeyCode.Mouse0) && cam.pixelRect.Contains(Input.mousePosition))
            {
                if (timer < 0.5f)
                {
                    timer = 0f;
                    clickCount++;
                    if (clickCount > 2)
                        clickCount = 0;
                }
                //If the mouse click was inside this camera, zoom in on it.
                if (clickCount >= 2 && Input.mousePosition == oldPos)
                {
                    Zoom();
                    t = 0f;
                    timer = 0f;
                    clickCount = 0;
                }
            }

            if (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse1))
            {
                canMove = false;
                canScale = false;
                Cursor.visible = true;
                selected = false;
            }
        }

        //Return all cameras to default
        if(Input.GetKeyDown(KeyCode.Space))
            DefaultSetup();

        //Update this camera's rect
        cam.rect = new Rect(camPos, camScale);
        //And its canvas
        if(canvasScl != null)
        canvasScl.scaleFactor = camScale.x;
    }

    void DefaultSetup()
    {
        //return all camera views to default position and scale.
    }

    public void Zoom()
    {
        //If we're zoomed In (full screen), then zoom out.
        if (zoomIn)
        {
            zoomIn = false;
            for (int i = 0; i < camControllers.Length; i++)
            {
                camControllers[i].cam.depth = 0;
            }
        }
        else //make this camera view full screen and on top of the rest
        {
            zoomIn = true;
            cam.depth = 0;
        }
        //Reset no click time
        noClickTimeFrame = 0f;
    }
}
};

