using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//#define __ENABLE__SOUND__

/// <summary>
/// Lets the user move, rotate and scale the planetarium object in the ZED planetarium sample scene. 
/// Also makes sure that relevant fields not tied directly to a transform, like sound distance and light radius, 
/// get properly scaled with the planetarium itself. 
/// </summary>
public class PlanetariumMover : MonoBehaviour
{
    /// <summary>
    /// How fast the solar system changes size when scaled. 
    /// </summary>
    [Tooltip("How fast the solar system changes size when scaled. ")]
    public float speedGrowth = 1.0f;

    /// <summary>
    /// How quickly the solar system moves up, right, forward, etc. 
    /// </summary>
    [Tooltip("How quickly the solar system moves up, right, forward, etc. ")]
    public float speedMove = 1.0f;

    /// <summary>
    /// How quickly the solar system rotates on the Y axis when the user rotates it (unrelated to planet orbits). 
    /// </summary>
    [Tooltip("How quickly the solar system rotates on the Y axis when the user rotates it (unrelated to planet orbits). ")]
    public float speedRotation = 20.0f;

    /// <summary>
    /// The Planetarium object being governed. This is the 'Planetarium' GameObject in the ZED planetarium scene. 
    /// </summary>
    [Tooltip("The Planetarium object being governed. This is the 'Planetarium' GameObject in the ZED planetarium scene.")]
    public GameObject planetarium;

    //public static bool scaling = false;

    /// <summary>
    /// The scene's ZEDManager component. 
    /// </summary>
    private ZEDManager manager;

    /// <summary>
    /// The Sun GameObject. 
    /// </summary>
    private GameObject suncontainer;

    /// <summary>
    /// Light component on the Sun GameObject. 
    /// </summary>
    private Light sunlight;

    /// <summary>
    /// Light component on the SunSpotLight GameObject. 
    /// </summary>
    private Light spotLightSun;

    /// <summary>
    /// Light component on the SunHaloLight GameObject. 
    /// </summary>
    private Light halolightsun; //for Halo

    /// <summary>
    /// The current scale setting. Equal to transform.scale.x, which is used as a proxy for all three values. 
    /// </summary>
    private float currentscale;

    /// <summary>
    /// The range of the sunlight Light component. 
    /// </summary>
    private float currentlightrange = 1;

    /// <summary>
    /// The range of the SunSpotLight's Light component. 
    /// </summary>
    private float currentlightrangesunspot = 1;

    /// <summary>
    /// The range of the SunHaloLight's Light component. 
    /// </summary>
    private float currentlightrangesunhalo = 0.6f;

    /// <summary>
    /// The largest you can scale the planetarium. 
    /// </summary>
    private const float MAX_LIMIT_SCALE = 3.0f;

    /// <summary>
    /// The smallest you can scale the planetarium. 
    /// </summary>
    private const float MIN_LIMIT_SCALE = 0.05f;

    /// <summary>
    /// When scaling the planetarium, the amount changed each frame is divided by this number. 
    /// </summary>
    private float scaler = 5;



#if __ENABLE__SOUND__
    public AudioSource sunSound;
    public AudioSource jupiterSound;
#endif

    private float currentMaxSoundDistanceSun;
    private float currentMaxSoundDistanceJupiter;

    void Start()
    {
        if (!planetarium)
        {
            planetarium = GameObject.Find("Planetarium");
        }

        currentscale = planetarium.transform.localScale.x;
        suncontainer = planetarium.transform.Find("Sun").gameObject;
        sunlight = suncontainer.GetComponent<Light>();

        currentlightrange = sunlight.range * (1 / currentscale);


		manager = planetarium.transform.parent.GetComponentInChildren<ZEDManager>();

        spotLightSun = suncontainer.transform.Find("SunSpotLight").GetComponent<Light>();
        halolightsun = suncontainer.transform.Find("SunHaloLight").GetComponent<Light>();

        currentlightrangesunspot = spotLightSun.range * (1 / currentscale);
        currentlightrangesunhalo = halolightsun.range * (1 / currentscale);

#if __ENABLE__SOUND__
        currentMaxSoundDistanceJupiter = jupiterSound.maxDistance * (1 / currentScale);
        currentMaxSoundDistanceSun = sunSound.maxDistance * (1 / currentScale);
#endif
    }

    private void OnEnable()
    {
		manager.OnZEDReady += ZEDReady;
    }

    private void OnDisable()
    {
		manager.OnZEDReady -= ZEDReady;

    }

    /// <summary>
    /// Called when the ZED is finished initializing, using the ZEDManager.OnZEDReady callback. 
    /// </summary>
    void ZEDReady()
    {
		if (manager)
        planetarium.transform.position = manager.OriginPosition + manager.OriginRotation * Vector3.forward;
    }

    // Update is called once per frame
    void Update()
    {
        string[] names = Input.GetJoystickNames();
        bool hasJoystick = false;

        if (names.Length > 0)
            hasJoystick = names[0].Length > 0;


        /// Adjust Planetarium X/Y/Z position 
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");

        Quaternion gravity = Quaternion.identity;

        gravity = Quaternion.FromToRotation(manager.GetZedRootTansform().up, Vector3.up);
        planetarium.transform.localPosition += manager.GetLeftCameraTransform().right * axisH * speedMove * Time.deltaTime;
        planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform().forward * axisV * speedMove * Time.deltaTime;

        /// Adjust Scale of Virtual objects,lights, sounds
        bool ScaleUpButton = Input.GetButton("Fire1") || Input.GetKey(KeyCode.JoystickButton5) || (Input.GetAxis("Fire1") >= 1);
        bool ScaleDownButton = Input.GetButton("Fire2") || (Input.GetAxis("Fire2") >= 1);

        currentscale += System.Convert.ToInt32(ScaleUpButton) * speedGrowth * Time.deltaTime / scaler;
        currentscale -= System.Convert.ToInt32(ScaleDownButton) * speedGrowth * Time.deltaTime / scaler;
        if (currentscale < MIN_LIMIT_SCALE) currentscale = MIN_LIMIT_SCALE;
        if (currentscale > MAX_LIMIT_SCALE) currentscale = MAX_LIMIT_SCALE;
        planetarium.transform.localScale = new Vector3(currentscale, currentscale, currentscale);
        sunlight.range = currentlightrange * currentscale;
        spotLightSun.range = currentlightrangesunspot * currentscale;
        halolightsun.range = currentlightrangesunhalo * currentscale;

#if __ENABLE__SOUND__
        jupiterSound.maxDistance = currentMaxSoundDistanceJupiter * currentScale;
        sunSound.maxDistance = currentMaxSoundDistanceSun * currentScale;
#endif

        /// Adjust Rotation of Planetarium
        if (CheckAxes("DPad X") && hasJoystick)
        {
            float axisX = Input.GetAxis("DPad X"); //multiply by 10 since sensibility is at 0.1 by default
            planetarium.transform.Rotate(gravity * manager.GetLeftCameraTransform().up * axisX * speedRotation, Space.World);
        }
        else
        {
            float axisX = System.Convert.ToInt32(Input.GetKey(KeyCode.R));
            planetarium.transform.Rotate(gravity * manager.GetLeftCameraTransform().up * axisX * speedRotation, Space.World);
        }


        //adjust Height of Planetarium
        if (CheckAxes("DPad Y") && hasJoystick)
        {
            float axisY = Input.GetAxis("DPad Y");
            planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform().up * axisY * speedMove * Time.deltaTime;
        }
        else
        {
            float axisY = System.Convert.ToInt32(Input.GetKey(KeyCode.PageUp)) - System.Convert.ToInt32(Input.GetKey(KeyCode.PageDown));
            planetarium.transform.localPosition += gravity * manager.GetLeftCameraTransform().up * axisY * speedMove * Time.deltaTime;
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

        for (int i = 0; i < axisArray.arraySize; ++i)
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
