//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using UnityEngine.UI;
using System.Collections;


/// <summary>
/// Creates multiple balls with a physic materials and launch them
/// </summary>
public class BallLauncher : MonoBehaviour
{
    /// <summary>
    /// Model of the projectile
    /// </summary>
    public GameObject projectile;

    /// <summary>
    /// list of all the projectiles
    /// </summary>
    private GameObject[] projectiles;

    /// <summary>
    /// Launch intensity for the init velocity of the ball
    /// </summary>
    public int LaunchIntensity = 750;

    /// <summary>
    /// Number of spheres max
    /// </summary>
    private const int SPHERE_NB = 100;

    /// <summary>
    /// Distance max betwween the user and the speher before the sphere is disabled
    /// </summary>
    private const int DISTANCE_MAX = 50;

    /// <summary>
    /// Time max a sphere can be alived before disabling the sphere
    /// </summary>
    private const int TIME_MAX = 30;

    /// <summary>
    /// Colors of the balls
    /// </summary>
    private Color[] ballColors;

    /// <summary>
    /// Times for each sphere
    /// </summary>
    private float[] times;

    /// <summary>
    /// Id of the next sphere to launch
    /// </summary>
    private int countSphere = 0;

    /// <summary>
    /// Timer between the throwing of balls
    /// </summary>
    private float timeBallMax = 0.05f;
    private float timerBall = 0.0f;

    /// <summary>
    /// Offset of the launcher
    /// </summary>
    private Vector3 offset = new Vector3(0.1f, -0.1f, 0.0f);

    /// <summary>
    /// The launcher
    /// </summary>
    private GameObject launcher;

    // Use this for initialization
    void Start()
    {
        launcher = new GameObject("Launcher");
        launcher.hideFlags = HideFlags.HideAndDontSave;
        launcher.transform.parent = transform;
        launcher.transform.localPosition = offset;
        ballColors = new Color[10];
        for (int i = 0; i < 10; i++)
        {
            ballColors[i] = Color.HSVToRGB(0.1f * i, 0.8f, 1.0f);
        }
        projectiles = new GameObject[SPHERE_NB];
        times = new float[SPHERE_NB];

        int count = 0;

        for (int i = 0; i < SPHERE_NB; i++)
        {
            projectiles[i] = Instantiate(projectile, launcher.transform.position, launcher.transform.rotation);
            projectiles[i].transform.localScale = new Vector3(0.10f, 0.10f, 0.10f);
            projectiles[i].GetComponent<MeshRenderer>().material.color = ballColors[count];
            Light l = projectiles[i].AddComponent<Light>();
            l.color = ballColors[count];
            l.intensity = 2;
            l.range = 1.0f;
            projectiles[i].AddComponent<ZEDLight>();

            count++;
            if (count == 10)
                count = 0;
            projectiles[i].SetActive(false);
            projectiles[i].hideFlags = HideFlags.HideInHierarchy;
            times[i] = 0;
        }       

    }

    private void OnEnable()
    {
		ZEDManager.OnZEDReady += ZedReady;
    }
    private bool ready = false;

	void ZedReady()
	{
		ready = true;
	}


    static float EaseIn(float t, float b, float c, float d)
    {
        return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
    }

    // Update is called once per frame
    void Update()
    {
  
		if (ready && (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1")))
        {
            if(timerBall > timeBallMax)
            {
                timerBall = 0.0f;
                if(!projectiles[countSphere % SPHERE_NB].activeInHierarchy)
                {
                    projectiles[countSphere % SPHERE_NB].SetActive(true);
                }
                projectiles[countSphere % SPHERE_NB].transform.rotation = launcher.transform.rotation;
                projectiles[countSphere % SPHERE_NB].transform.position = launcher.transform.position;
                float offsetAngleX = 0.0f;
                float offsetAngleY = 0.0f;

                
                launcher.transform.localRotation = Quaternion.Euler(-offsetAngleY * Mathf.Rad2Deg, -offsetAngleX * Mathf.Rad2Deg, 0);
                projectiles[countSphere % SPHERE_NB].GetComponent<BallTrigger>().ResetValues();
                Rigidbody rigidBody = projectiles[countSphere % SPHERE_NB].GetComponent<Rigidbody>();
                rigidBody.velocity = Vector3.zero;
                rigidBody.isKinematic = false;
                rigidBody.useGravity = true;

                rigidBody.AddForce(launcher.transform.forward * LaunchIntensity);

                times[countSphere % SPHERE_NB] = 0;
                countSphere++;
            }
            timerBall += Time.deltaTime;
        }
        for (int i = 0; i < SPHERE_NB; i++)
        {
            if (projectiles[i].activeSelf)
            {
                if (Vector3.Distance(projectiles[i].transform.position, Vector3.zero) > DISTANCE_MAX || times[i] > TIME_MAX)
                {
                    projectiles[i].SetActive(false);
                    times[i] = 0;
                }
                else
                {
                    times[i] += Time.deltaTime;
                }
            }
        }
    }

}
