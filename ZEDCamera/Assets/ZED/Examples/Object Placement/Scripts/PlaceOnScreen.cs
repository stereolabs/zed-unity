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
    private ZEDManager zedManager;

    /// <summary>
    /// The left camera in the ZED rig. Passed to ZEDSupportFunctions for transforming between camera and world space. 
    /// </summary>
    private Camera cam;

    // Use this for initialization
    void Awake() {
        //zedManager = gameObject.transform.parent.GetComponentInChildren<ZEDManager>();
        zedManager = ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_01);
		cam = zedManager.GetMainCamera();

        Cursor.visible = true; //Make sure cursor is visible so we can click on the world accurately. 
    }

    // Update is called once per frame
    void Update () {
		if(!zedManager.zedCamera.IsCameraReady)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0)) //Checks for left click.
        {			
			/// Mouse Input gives the screen pixel position
            Vector3 ScreenPosition = Input.mousePosition;

            //Get Normal and real world position defined by the pixel .
            Vector3 Normal;
            Vector3 WorldPos;
			ZEDSupportFunctions.GetNormalAtPixel(zedManager.zedCamera,ScreenPosition,sl.REFERENCE_FRAME.WORLD,cam,out Normal);
			ZEDSupportFunctions.GetWorldPositionAtPixel(zedManager.zedCamera,ScreenPosition, cam,out WorldPos);

			//To consider the location as a flat surface, we check that the normal is valid and is closely aligned with gravity.
            bool validFloor = Normal.x != float.NaN && Vector3.Dot(Normal, Vector3.up) > 0.85f;

			//If we've found a floor to place the object, spawn a copy of the prefab. 
			if (validFloor)
            {
                GameObject newgo = Instantiate(ObjectToPlace);
                newgo.transform.localPosition = WorldPos;
                newgo.transform.LookAt(new Vector3(zedManager.transform.position.x, newgo.transform.position.y, zedManager.transform.position.z), Vector3.up);
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
