using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//#define __ENABLE__SOUND__

public class PlanetariumMover : MonoBehaviour
{

    public float speedGrowth = 1.0f;
    public float speedMove = 1.0f;
    public float speedRotation = 20.0f;

    private ZEDManager manager;
    public GameObject planetarium;
    private Light sunLight;
    private GameObject sunContainer;

    private float currentScale;

    private float currentLighRange = 1;
    private float currentLightRangeSunSpot = 1;
	private float currentLightRangeSunHalo = 0.6f;

    private const float MAX_LIMIT_SCALE = 3.0f;
    private const float MIN_LIMIT_SCALE = 0.05f;


    public static bool scaling = false;
    private Light spotLightSun;
	private Light pointLightSun; //for Halo


	private float scaler = 5;



#if __ENABLE__SOUND__
    public AudioSource sunSound;
    public AudioSource jupiterSound;
#endif

    private float currentMaxSoundDistanceSun;
    private float currentMaxSoundDistanceJupiter;

    void Start()
    {
        planetarium = GameObject.Find("Planetarium");
        currentScale = planetarium.transform.localScale.x;
        sunContainer = planetarium.transform.Find("Sun").gameObject;
		sunLight = sunContainer.GetComponent<Light>();
 



        currentLighRange = sunLight.range * (1 / currentScale);
        if (manager == null)
        {
            GameObject m = GameObject.Find("ZED_Rig_Stereo");
            if(m == null)
                m = GameObject.Find("ZED_Rig_Mono");
			if(m == null)
				m = GameObject.Find("ZED_GreenScreen"); 

			if (m != null)
				manager = m.GetComponent<ZEDManager> ();
			else
				manager = null;
        }
        spotLightSun = sunContainer.transform.Find("SunSpotLight").GetComponent<Light>();
		pointLightSun = sunContainer.transform.Find("SunHaloLight").GetComponent<Light>();

        currentLightRangeSunSpot = spotLightSun.range*(1/currentScale);
		currentLightRangeSunHalo = pointLightSun.range*(1/currentScale);

#if __ENABLE__SOUND__
        currentMaxSoundDistanceJupiter = jupiterSound.maxDistance * (1 / currentScale);
        currentMaxSoundDistanceSun = sunSound.maxDistance * (1 / currentScale);
#endif

 

    }

    private void OnEnable()
    {
        ZEDManager.OnZEDReady += ZEDReady;
    }

    private void OnDisable()
    {
        ZEDManager.OnZEDReady -= ZEDReady;

    }

    void ZEDReady()
    {
		if (manager) planetarium.transform.position = manager.OriginPosition + manager.OriginRotation * Vector3.forward;
    }

    // Update is called once per frame
    void Update()
    {
		string [] names = Input.GetJoystickNames();
		bool hasJoystick = false;

		if (names.Length>0)
			hasJoystick = names[0].Length > 0;
	 
			
		/// Adjust Planetarium X/Y/Z position 
		float axisH = Input.GetAxis("Horizontal");
		float axisV = Input.GetAxis("Vertical");

		Quaternion gravity = Quaternion.identity;

		if (manager) {
			gravity = Quaternion.FromToRotation (manager.GetZedRootTansform ().up, Vector3.up);
			planetarium.transform.localPosition += manager.GetLeftCameraTransform ().right * axisH * speedMove * Time.deltaTime;
			planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform ().forward * axisV * speedMove * Time.deltaTime;
		}

		/// Adjust Scale of Virtual objects,lights, sounds
		bool ScaleUpButton = Input.GetButton("Fire1") || Input.GetKey (KeyCode.JoystickButton5) || (Input.GetAxis("Fire1")>=1);
		bool ScaleDownButton = Input.GetButton ("Fire2") || (Input.GetAxis ("Fire2") >= 1);

		currentScale += System.Convert.ToInt32(ScaleUpButton) * speedGrowth * Time.deltaTime / scaler;
		currentScale -= System.Convert.ToInt32(ScaleDownButton) * speedGrowth * Time.deltaTime / scaler;
        if (currentScale < MIN_LIMIT_SCALE) currentScale = MIN_LIMIT_SCALE;
        if (currentScale > MAX_LIMIT_SCALE) currentScale = MAX_LIMIT_SCALE;
        planetarium.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
		sunLight.range = currentLighRange * currentScale;
        spotLightSun.range = currentLightRangeSunSpot * currentScale;   
		pointLightSun.range = currentLightRangeSunHalo * currentScale;  

		#if __ENABLE__SOUND__
        jupiterSound.maxDistance = currentMaxSoundDistanceJupiter * currentScale;
        sunSound.maxDistance = currentMaxSoundDistanceSun * currentScale;
		#endif

		/// Adjust Rotation of Planetarium
		if (CheckAxes ("DPad X") && hasJoystick) {
			float axisX = Input.GetAxis ("DPad X"); //multiply by 10 since sensibility is at 0.1 by default
			if (manager) planetarium.transform.Rotate (gravity * manager.GetLeftCameraTransform ().up * axisX * speedRotation, Space.World);
		} else {
			float axisX = System.Convert.ToInt32(Input.GetKey (KeyCode.R));
			if (manager) planetarium.transform.Rotate (gravity * manager.GetLeftCameraTransform ().up * axisX * speedRotation, Space.World);
		}


		//adjust Height of Planetarium
		if (CheckAxes ("DPad Y") && hasJoystick) {
			float axisY = Input.GetAxis ("DPad Y");
			if (manager) planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform ().up * axisY * speedMove * Time.deltaTime;
		} else {
			float axisY = System.Convert.ToInt32(Input.GetKey (KeyCode.PageUp)) - System.Convert.ToInt32(Input.GetKey(KeyCode.PageDown));
			if (manager) planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform ().up * axisY * speedMove * Time.deltaTime;
		}

 

    }

 


	public static bool CheckAxes(string choice)
     {
#if UNITY_EDITOR
         var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
 
         SerializedObject obj = new SerializedObject(inputManager);
 
         SerializedProperty axisArray = obj.FindProperty("m_Axes");
 
         if (axisArray.arraySize == 0)
             Debug.Log("No Axes");
 
         for( int i = 0; i < axisArray.arraySize; ++i )
         {
             var axis = axisArray.GetArrayElementAtIndex(i);
             var name = axis.FindPropertyRelative("m_Name").stringValue;  
			if (name == choice)
				return true;

         }


		return false;
#else
		return true;
#endif
     }

}
