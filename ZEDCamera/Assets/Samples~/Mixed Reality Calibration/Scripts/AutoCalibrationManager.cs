using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles automatic calibration mode in the ZED MR calibration scene. 
/// When set up, it places clickable balls in front of the ZED in positions listed in spherePositions.
/// When you click on the balls and re-align the controllers as prompted, it'll call a sl.ZEDCamera function
/// to calculate a new camera position. 
/// </summary>
public class AutoCalibrationManager : MonoBehaviour
{
    /// <summary>
    /// Prefab of the balls you click on to add a reference point. Should be Prefabs/AutoCalibration Ball.
    /// </summary>
    [Tooltip("Prefab of the balls you click on to add a reference point. Should be Prefabs/AutoCalibration Ball.")]
    public GameObject autoCalibBallPrefab;

    /// <summary>
    /// CameraAnchor in the scene that's holding the ZED camera. 
    /// </summary>
    [Tooltip("CameraAnchor in the scene that's holding the ZED camera. ")]
    public CameraAnchor camAnchor;


    private List<AutoCalibBall> balls = new List<AutoCalibBall>();

    /// <summary>
    /// The positions used to calibrate the ZED. Should all be visible from the ZED,
    /// and vary on all three axes to make it easy to calibrate. 
    /// </summary>
    [Tooltip("The positions used to calibrate the ZED. Should all be visible from the ZED, " +
        "and vary on all three axes to make it easy to calibrate. ")]
    public Vector3[] spherePositions = { new Vector3(0.0f, -0.4f, -0.5f),
                                         new Vector3(0.4f, -0.1f, -0.5f),
                                         new Vector3(-0.4f, -0.4f, -0.3f),
                                         new Vector3(-0.4f, -0.1f, -0.1f),
                                         new Vector3(0.4f, -0.2f, -0.1f)};

    /// <summary>
    /// The scene's ZEDManager. Will be assigned to ZEDManager of camera index 1 if not set. 
    /// </summary>
    [Tooltip("The scene's ZEDManager. Will be assigned to ZEDManager of camera index 1 if not set. ")]
    [Space(5)]
    public ZEDManager zedManager;

    private Vector3[] virtualPositions;
    private Vector3[] realPositions;

    private const float angleMAX = 35;

    private bool isSetup = false;

    private void Awake()
    {
        if (!zedManager)
        {
            zedManager = ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_01);
        }

        if(!camAnchor)
        {
            camAnchor = FindObjectOfType<CameraAnchor>();
        }

        SetUpBalls(false);

    }



    /// <summary>
    /// Creates the ball objects and other minor setup. "ForceReset" will clear existing balls if
    /// they were aleady set up, otherwise nothing will happen. 
    /// </summary>
    public void SetUpBalls(bool forcereset = true)
    {
        if(isSetup)
        {
            if(forcereset) //If we're resetting, clear all existing balls and positions. 
            {
                CleanUpBalls();

            }
            else //Already set up but we don't want to reset. This is likely unintentional.
            {
                Debug.LogError("Called AutomatedCalibration.Setup when it was already set up, but without requesting a reset.");
                return;
            }
        }

        Transform zedtrans = zedManager.transform; //Shorthand
        //Create the balls. 

        for (int i = 0; i < spherePositions.Length; i++)
        {
            virtualPositions = new Vector3[spherePositions.Length];
            realPositions = new Vector3[spherePositions.Length];

            GameObject newballgo = Instantiate(autoCalibBallPrefab, zedtrans, false);
            newballgo.transform.localPosition = spherePositions[i];

            AutoCalibBall newball = newballgo.GetComponentInChildren<AutoCalibBall>();
            if (!newball)
            {
                throw new System.Exception("No AutoCalibBall script on autoCalibBallPrefab.");
            }

            newball.Setup(this, i);

            balls.Add(newball);

        }

        isSetup = true;

        //Update all message displays with instructions.
        MessageDisplay.DisplayMessageAll("AUTOMATIC MODE\r\nPut your controller inside a ball and click.\r\n" +
            "If the virtual ZED isn't facing you, use Manual Mode to get the image roughly aligned.");
    }

    /// <summary>
    /// Destroys all existing balls. Use when switching out of Automatic mode. 
    /// </summary>
    public void CleanUpBalls()
    {
        for (int i = 0; i < balls.Count; i++)
        {
            Destroy(balls[i].gameObject);
        }
        balls.Clear();
    }

    /// <summary>
    /// Registers a new combination of real and virtual positions to be used to calculate the camera position.
    /// This is called after a single ball has been used and set. 
    /// </summary>
    /// <param name="index">Index of the ball within this class, used in both virtualPositions and realPositions.</param>
    /// <param name="virtualpos">Position of the controller when the ball was first activated.</param>
    /// <param name="realpos">Position of the controller after the user has aligned it with the real world.</param>
    public void AddNewPositions(int index, Vector3 virtualpos, Vector3 realpos)
    {
        if (virtualPositions == null || virtualPositions.Length == 0) virtualPositions = new Vector3[spherePositions.Length];
        if (realPositions == null || realPositions.Length == 0) realPositions = new Vector3[spherePositions.Length];

        if (index >= spherePositions.Length)
        {
            throw new System.Exception("Invalid index passed to AutomatedCalibration.AddNewPositions. Passed " +
                index + ", max is " + (spherePositions.Length - 1).ToString() + ".");
        }

        virtualPositions[index] = virtualpos;
        realPositions[index] = realpos;

        UpdateZEDPosition();
    }

    /// <summary>
    /// Prepare the current virtual and real positions (provided by AddNewPositions after using the balls) into
    /// data to be used by ZEDCamera.ComputeOffset, and call it. This will update the camera's position based on
    /// the inputs the user provided. 
    /// </summary>
    private void UpdateZEDPosition() 
    {
        List<Vector3> validvirtposes = new List<Vector3>();
        List<Vector3> validrealposes = new List<Vector3>();

        for (int i = 0; i < virtualPositions.Length; i++)
        {
            if (virtualPositions[i] != null && realPositions[i] != null) //Don't add either unless both are valid. 
            {
                validvirtposes.Add(virtualPositions[i]);
                validrealposes.Add(realPositions[i]);
            }
        }

        if (validvirtposes.Count == 0) return;

        int posecount = validvirtposes.Count; //Shorthand.

        //Make array of floats used to call ZEDCamera.ComputeOffset. We use arrays because it's turned into a matrix internally. 
        float[] inputA = new float[posecount * 4];
        float[] inputB = new float[posecount * 4];

        for (int i = 0; i < posecount; i++)
        {
            inputA[i * 4 + 0] = validvirtposes[i].x;
            inputA[i * 4 + 1] = validvirtposes[i].y;
            inputA[i * 4 + 2] = validvirtposes[i].z;
            inputA[i * 4 + 3] = 1; //Will be W in the matrix. 

            inputB[i * 4 + 0] = validrealposes[i].x;
            inputB[i * 4 + 1] = validrealposes[i].y;
            inputB[i * 4 + 2] = validrealposes[i].z;
            inputB[i * 4 + 3] = 1; //Will be W in the matrix. 
        }

        Vector3 newtranslation = new Vector3();
        Quaternion newrotation = Quaternion.identity;
        sl.ZEDCamera.ComputeOffset(inputA, inputB, posecount, ref newrotation, ref newtranslation);

        if (IsInsideRangeAngle(newrotation))
        {
            /* //This was in original calibration app but I suspect it caused issues. 
            for (int j = 0; j < (calibrationStage + 1) * 0.5; j++)
            {
                realPositions[j] = Quaternion.Inverse(rotation) * (realPositions[j] - translation);
            }*/ //Okay nevermind let's try it. 

            for(int i = 0; i < realPositions.Length; i++)
            {
                if(realPositions[i] != null && realPositions[i] != Vector3.zero) //May not need that second half. We'll see. 
                {
                    realPositions[i] = Quaternion.Inverse(newrotation) * (realPositions[i] - newtranslation);
                }
            }

            //zedManager.transform.position -= Quaternion.Inverse(newrotation) * newtranslation;
            //zedManager.transform.rotation = Quaternion.Inverse(newrotation) * zedManager.transform.rotation;
            camAnchor.MoveZEDPose(-(Quaternion.Inverse(newrotation) * newtranslation), Quaternion.Inverse(newrotation), false);
        }
        else
        {
            print("Calibration is not yet precise. Continue adding points and consider redoing existing points.");
        }

    }

    private void OnDisable()
    {
        CleanUpBalls();
    }


    /// <summary>
    /// Checks whether or not the current alignment is reasonably accurate. 
    /// </summary>
    /// <param name="rotation"></param>
    /// <returns></returns>
    private bool IsInsideRangeAngle(Quaternion rotation)
    {
        Vector3 angle = rotation.eulerAngles;
        return (angle.x < angleMAX || angle.x > 360 - angleMAX)
                   && (angle.y < angleMAX || angle.y > 360 - angleMAX)
                   && (angle.z < angleMAX || angle.z > 360 - angleMAX);

    }
}
