using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public KeyCode forwardKey = KeyCode.Z;
    public KeyCode reverseKey = KeyCode.S;
    public KeyCode strafeLeftKey = KeyCode.Q;
    public KeyCode strafeRightKey = KeyCode.D;

    public KeyCode upKey = KeyCode.A;
    public KeyCode downkey = KeyCode.E;

    public KeyCode enableMouseLookKey = KeyCode.Mouse2;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode slowKey = KeyCode.LeftAlt; //Control is nicer but breaks sometimes in the editor. 

    private bool mouseLookActive = false;

    [Space(5)]
    public float normalSpeedMPS = 1.5f;
    public float sprintspeedMPS = 4f;
    public float slowSpeedMPS = 0.4f;
    public float accelTimeSeconds = 0.3f;

    private float speedMPS
    {
        get
        {
            if (Input.GetKey(sprintKey)) return sprintspeedMPS;
            else if (Input.GetKey(slowKey)) return slowSpeedMPS; 
            else return normalSpeedMPS;
        }
    }

    private Vector3 velocity = Vector3.zero;

	// Use this for initialization
	void Start ()
    {
        //Lock cursor if that's the default setting. 
        ToggleMouseLock(mouseLookActive);
    }
	
	// Update is called once per frame
	void Update ()
    {
		//Toggling mouse look. 
        if(Input.GetKeyDown(enableMouseLookKey))
        {
            ToggleMouseLock(!mouseLookActive);
        }

        //Process mouse look. 
        if(mouseLookActive)
        {
            Vector3 rots = new Vector3(0, 0, 0);
            rots.x = -Input.GetAxis("Mouse Y");
            rots.y = Input.GetAxis("Mouse X");

            transform.Rotate(rots, Space.Self);

            //Compensate for the weird Z rotation that happens. 
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);

        }

        //Process movements. For now, you can move even when the mouse isn't locked. Easy fix if we want that later. 
        //Make a vector that points 1 in any direction you want to go, and cancels out if holding two opposite keys. 
        Vector3 rawtranslate = Vector3.zero;
        if (Input.GetKey(forwardKey)) rawtranslate.z += 1;
        if (Input.GetKey(reverseKey)) rawtranslate.z -= 1;
        if (Input.GetKey(strafeLeftKey)) rawtranslate.x -= 1;
        if (Input.GetKey(strafeRightKey)) rawtranslate.x += 1;
        if (Input.GetKey(upKey)) rawtranslate.y += 1;
        if (Input.GetKey(downkey)) rawtranslate.y -= 1;

        //Account for speed and accel time and add to the velocity. 
        float accelmod = Time.deltaTime * normalSpeedMPS / accelTimeSeconds;
        Vector3 accel = rawtranslate * accelmod;
        velocity += accel;

        //Decelerate on any axis where a key isn't held, or is held in the opposite direction. 
        if(rawtranslate.x == 0 || ((rawtranslate.x >= 0) != (velocity.x >= 0)))
        {
            bool waspos = (velocity.x > 0);
            velocity.x -= waspos ? accelmod : -accelmod;
            if (waspos != (velocity.x > 0)) velocity.x = 0;
        }
        if (rawtranslate.y == 0 || ((rawtranslate.y >= 0) != (velocity.y >= 0)))
        {
            bool waspos = (velocity.y > 0);
            velocity.y -= waspos ? accelmod : -accelmod;
            if (waspos != (velocity.y > 0)) velocity.y = 0;
        }
        if (rawtranslate.z == 0 || ((rawtranslate.z >= 0) != (velocity.z >= 0)))
        {
            bool waspos = (velocity.z > 0);
            velocity.z -= waspos ? accelmod : -accelmod;
            if (waspos != (velocity.z > 0)) velocity.z = 0;
        }


        //Clamp velocity at max speeds and apply it. 
        velocity.x = Mathf.Clamp(velocity.x, -speedMPS, speedMPS);
        velocity.y = Mathf.Clamp(velocity.y, -speedMPS, speedMPS);
        velocity.z = Mathf.Clamp(velocity.z, -speedMPS, speedMPS);

        transform.position += transform.rotation * velocity * Time.deltaTime;

    }

    private void ToggleMouseLock(bool state)
    {
        mouseLookActive = state;
        Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !state;
    }
}
