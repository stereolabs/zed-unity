using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableWhenReady : MonoBehaviour {


	// Use this for initialization
	void Start () {
		if (ZEDManager.GetInstance (sl.ZED_CAMERA_ID.CAMERA_ID_01) != null) 
			ZEDManager.GetInstance (sl.ZED_CAMERA_ID.CAMERA_ID_01).OnZEDReady += zedIsReady;

		if (ZEDManager.GetInstance (sl.ZED_CAMERA_ID.CAMERA_ID_02) != null) 
			ZEDManager.GetInstance (sl.ZED_CAMERA_ID.CAMERA_ID_02).OnZEDReady += zedIsReady;

        if (ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_03) != null)
            ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_03).OnZEDReady += zedIsReady;

        if (ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_04) != null)
            ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_04).OnZEDReady += zedIsReady;

    }
	
	// Update is called once per frame
	void zedIsReady () {
 
		foreach (var tr in gameObject.GetComponentsInChildren<Transform>(true))
			tr.gameObject.SetActive (true);
	}
}
