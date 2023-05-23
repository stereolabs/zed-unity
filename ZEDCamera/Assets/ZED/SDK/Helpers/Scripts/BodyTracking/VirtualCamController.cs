using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualCamController : MonoBehaviour
{
    /// <summary>
    /// The ZEDManager corresponding to the camera.
    /// </summary>
    public ZEDManager zedManager = null;

    public bool followRealCam = true;
    public bool enableControls = true;

    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentPosition = Vector3.zero;
    private float stepTranslation = 0.1f;
    private float stepRotation = 5f;

    private bool initialized = false;

    [SerializeField]
    private KeyCode toLeft = KeyCode.Keypad4;
    [SerializeField]
    private KeyCode toRight = KeyCode.Keypad6;
    [SerializeField]
    private KeyCode forward = KeyCode.Keypad8;
    [SerializeField]
    private KeyCode backward = KeyCode.Keypad2;
    [SerializeField]
    private KeyCode up = KeyCode.Keypad7;
    [SerializeField]
    private KeyCode down = KeyCode.Keypad1;
    [SerializeField]
    private KeyCode rollup = KeyCode.Keypad9;
    [SerializeField]
    private KeyCode rolldown = KeyCode.Keypad3;
    [SerializeField]
    private KeyCode toggleFollow = KeyCode.Keypad5;

    // Start is called before the first frame update
    void Start()
    {
        if(followRealCam)
        {
            transform.position = zedManager.transform.localPosition;
            transform.rotation = zedManager.transform.localRotation;
        }
        else
        {
            currentPosition = transform.position;
            currentRotation = transform.rotation;
            initialized = true;
        }
    }

    private void ResetCurrentCam()
    {
        initialized = true;
        currentPosition = zedManager.transform.localPosition;
        currentRotation = zedManager.transform.localRotation;
    }

    void ManageInput()
    {
        if(enableControls)
        {
            if (Input.GetKeyDown(toLeft)) { currentPosition += new Vector3(-stepTranslation, 0, 0); }
            if (Input.GetKeyDown(toRight)) { currentPosition += new Vector3(stepTranslation, 0, 0); }
            if (Input.GetKeyDown(down)) { currentPosition += new Vector3(0, -stepTranslation, 0); }
            if (Input.GetKeyDown(up)) { currentPosition += new Vector3(0, stepTranslation, 0); }
            if (Input.GetKeyDown(backward)) { currentPosition += new Vector3(0, 0, -stepTranslation); }
            if (Input.GetKeyDown(forward)) { currentPosition += new Vector3(0, 0, stepTranslation); }
            if (Input.GetKeyDown(rolldown)) { currentRotation *= Quaternion.Euler(Vector3.right * stepRotation); }
            if (Input.GetKeyDown(rollup)) { currentRotation *= Quaternion.Euler(Vector3.right * -stepRotation); }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleFollow)) 
        { 
            if (!initialized) { ResetCurrentCam(); } 
            followRealCam = !followRealCam; 
        }

        if(followRealCam)
        {
            transform.position = zedManager.transform.localPosition;
            transform.rotation = zedManager.transform.localRotation;
        }
        else
        {
            ManageInput();
            transform.position = currentPosition; 
            transform.rotation = currentRotation;
        }
    }
}
