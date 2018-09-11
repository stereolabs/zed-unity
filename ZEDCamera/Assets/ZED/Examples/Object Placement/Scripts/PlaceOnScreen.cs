using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns an object when you click on the screen at the real-world position clicked. 
/// Will only spawn an object if the surface clicked is facing upward. 
/// Note that the ZED's newer plane detection feature is usually better for this unless
/// you need something very simple. 
/// </summary>
public class PlaceOnScreen : MonoBehaviour
{
    /// <summary>
    /// Prefab object to be instantiated in the real world on a click. 
    /// </summary>
    public GameObject ObjectToPlace;

    /// <summary>
    /// The ZEDManager in the scene.
    /// </summary>
    private ZEDManager ZedManager;

    /// <summary>
    /// The left camera in the ZED rig. Passed to ZEDSupportFunctions for transforming between camera and world space. 
    /// </summary>
    private Camera LeftCamera;

    // Use this for initialization
    void Awake() {
        ZedManager = FindObjectOfType<ZEDManager>();
        LeftCamera = ZedManager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();

        Cursor.visible = true; //Make sure cursor is visible so we can click on the world accurately. 
    }

    // Update is called once per frame
    void Update () {
		if(!sl.ZEDCamera.GetInstance().IsCameraReady)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0)) //Checks for left click.
        {			
			/// Mouse Input gives the screen pixel position
            Vector2 ScreenPosition = Input.mousePosition;

            //Get Normal and real world position defined by the pixel .
            Vector3 Normal;
            Vector3 WorldPos;
			ZEDSupportFunctions.GetNormalAtPixel(ScreenPosition,sl.REFERENCE_FRAME.WORLD,LeftCamera,out Normal);
			ZEDSupportFunctions.GetWorldPositionAtPixel(ScreenPosition, LeftCamera,out WorldPos);

			//To consider the location as a flat surface, we check that the normal is valid and is closely aligned with gravity.
            bool validFloor = Normal.x != float.NaN && Vector3.Dot(Normal, Vector3.up) > 0.85f;

			//If we've found a floor to place the object, spawn a copy of the prefab. 
			if (validFloor)
            {
                GameObject newgo = Instantiate(ObjectToPlace);
                newgo.transform.localPosition = WorldPos;
                newgo.transform.LookAt(new Vector3(ZedManager.transform.position.x, newgo.transform.position.y, ZedManager.transform.position.z), Vector3.up);
            }
            else
            {
				if (Normal.x == float.NaN)
				Debug.Log ("Cannot place object at this position. Normal vector not detected.");
				if (Vector3.Dot(Normal, Vector3.up) <= 0.85f)
					Debug.Log ("Cannot place object at this position. Normal vector angled too far from up: "+Mathf.Acos(Vector3.Dot(Normal, Vector3.up))*Mathf.Rad2Deg + "°");
			}
        }
    }
}
