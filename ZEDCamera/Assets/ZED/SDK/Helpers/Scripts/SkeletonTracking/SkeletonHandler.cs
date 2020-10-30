//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;


public class SkeletonHandler : ScriptableObject {

	private const int
	// JointType
	JointType_Head= 0,
	JointType_Neck= 1,
	JointType_ShoulderRight= 2,
	JointType_ElbowRight= 3,
	JointType_WristRight= 4,
	JointType_ShoulderLeft= 5,
	JointType_ElbowLeft= 6,
	JointType_WristLeft= 7,
	JointType_HipRight= 8,
	JointType_KneeRight= 9,
	JointType_AnkleRight= 10,
	JointType_HipLeft= 11,
	JointType_KneeLeft= 12,
	JointType_AnkleLeft= 13,
	JointType_EyesRight= 14,
	JointType_EyesLeft= 15,
	JointType_HearRight= 16,
	JointType_HearLeft= 17,
	JointType_SpineBase = 18,  //Not in the list but created from 8 + 11
    JointType_Nose = 19,
	jointCount = 20;

	private static int[] jointSegment = new int[] {
	JointType_SpineBase, JointType_Neck,                 // Spine
	JointType_Neck, JointType_Head,                      // Neck
	// left
	JointType_ShoulderLeft, JointType_ElbowLeft,         // LeftUpperArm
	JointType_ElbowLeft, JointType_WristLeft,            // LeftLowerArm
	JointType_HipLeft, JointType_KneeLeft,               // LeftUpperLeg
	JointType_KneeLeft, JointType_AnkleLeft,             // LeftLowerLeg6
	// right
	JointType_ShoulderRight, JointType_ElbowRight,       // RightUpperArm
	JointType_ElbowRight, JointType_WristRight,          // RightLowerArm
	JointType_HipRight, JointType_KneeRight,             // RightUpperLeg
	JointType_KneeRight, JointType_AnkleRight,           // RightLowerLeg
	};

    private static readonly int[] bonesList = new int[] {
    JointType_SpineBase, JointType_Neck,                 // Spine                     // Neck
    JointType_HipLeft, JointType_HipRight,
    JointType_HearRight, JointType_EyesRight,
    JointType_HearLeft, JointType_EyesLeft,
    JointType_EyesRight, JointType_Nose,
    JointType_EyesLeft, JointType_Nose,
    JointType_Nose, JointType_Neck,
	// left
    JointType_Neck, JointType_ShoulderLeft,
    JointType_ShoulderLeft, JointType_ElbowLeft,         // LeftUpperArm
	JointType_ElbowLeft, JointType_WristLeft,            // LeftLowerArm
	JointType_HipLeft, JointType_KneeLeft,               // LeftUpperLeg
	JointType_KneeLeft, JointType_AnkleLeft,             // LeftLowerLeg6
	// right
    JointType_Neck, JointType_ShoulderRight,
    JointType_ShoulderRight, JointType_ElbowRight,       // RightUpperArm
	JointType_ElbowRight, JointType_WristRight,          // RightLowerArm
	JointType_HipRight, JointType_KneeRight,             // RightUpperLeg
	JointType_KneeRight, JointType_AnkleRight,           // RightLowerLeg
	};

    private static readonly int[] sphereList = new int[] {
    JointType_SpineBase,
    JointType_Neck,                     
    JointType_HipLeft,
    JointType_HipRight,
    JointType_ShoulderLeft,
    JointType_ElbowLeft,         
	JointType_WristLeft,           
	JointType_KneeLeft,
	JointType_AnkleLeft,    
    JointType_ShoulderRight,
    JointType_ElbowRight,
	JointType_WristRight,
	JointType_KneeRight,
	JointType_AnkleRight,
    JointType_EyesLeft,
    JointType_EyesRight,
    JointType_HearRight,
    JointType_HearLeft,
    JointType_Nose
	};

    public Vector3[] joint = new Vector3[jointCount];

	Dictionary<HumanBodyBones,Vector3> trackingSegment = null;

    GameObject skeleton;
    public GameObject[] bones;
    public GameObject[] spheres;

    private static HumanBodyBones[] humanBone = new HumanBodyBones[] {
	HumanBodyBones.Hips,
	HumanBodyBones.Spine,
	HumanBodyBones.UpperChest,
	HumanBodyBones.Neck,
	HumanBodyBones.Head,
	HumanBodyBones.LeftUpperArm,
	HumanBodyBones.LeftLowerArm,
	HumanBodyBones.LeftHand,
	HumanBodyBones.LeftUpperLeg,
	HumanBodyBones.LeftLowerLeg,
	HumanBodyBones.RightUpperArm,
	HumanBodyBones.RightLowerArm,
	HumanBodyBones.RightHand,
	HumanBodyBones.RightUpperLeg,
	HumanBodyBones.RightLowerLeg,
	};

	private static HumanBodyBones[] targetBone = new HumanBodyBones[] {
	HumanBodyBones.Spine,
    HumanBodyBones.Neck,
	HumanBodyBones.LeftUpperArm,
	HumanBodyBones.LeftLowerArm,
	HumanBodyBones.LeftUpperLeg,
	HumanBodyBones.LeftLowerLeg,
	HumanBodyBones.RightUpperArm,
	HumanBodyBones.RightLowerArm,
	HumanBodyBones.RightUpperLeg,
	HumanBodyBones.RightLowerLeg,
	};

    private Color[] colors = new Color[]{
    new Color(0.0f, 0.0f, 1.0f),
    new Color(1.0f, 0.0f, 0.0f),
    new Color(0.0f, 1.0f, 0.0f),
    new Color(0.0f, 1.0f, 1.0f),
    new Color(1.0f, 0.0f, 1.0f)
    };

    private GameObject humanoid;
	private Dictionary<HumanBodyBones, RigBone> rigBone = null;
	private Dictionary<HumanBodyBones, Quaternion> rigBoneTarget = null;

	private List<GameObject> sphere = new List<GameObject>();// = GameObject.CreatePrimitive (PrimitiveType.Sphere);

    Quaternion oldwaistrot = Quaternion.identity;
    Quaternion oldshoulderrot = Quaternion.identity;

    private Vector3 targetBodyPosition = new Vector3(0.0f,0.0f,0.0f);
	public Quaternion targetBodyOrientation = Quaternion.identity;

    private bool isInit = false;

	private float smoothFactor = 0.5f;
	/// <summary>
	/// Sets the smooth factor.
	/// </summary>
	/// <param name="smooth">Smooth.</param>
	public void SetSmoothFactor(float smooth)
	{
		smoothFactor = smooth;
	}

	/// <summary>
	/// Create the avatar control
	/// </summary>
	/// <param name="h">The height.</param>
	public void Create(GameObject h, Vector3 spawnPosition)
	{

        humanoid = (GameObject)Instantiate( h, spawnPosition, Quaternion.identity);
	    var invisiblelayer = LayerMask.NameToLayer("tagInvisibleToZED");
	    humanoid.layer = invisiblelayer;

	    foreach (Transform child in humanoid.transform)
	    {
				child.gameObject.layer = invisiblelayer;
	    }

	    rigBone = new Dictionary<HumanBodyBones, RigBone>();
		  rigBoneTarget = new Dictionary<HumanBodyBones, Quaternion>();
	    foreach (HumanBodyBones bone in humanBone) {
	      rigBone[bone] = new RigBone(humanoid,bone);
		  rigBoneTarget [bone] = Quaternion.identity;
 
	    }

	  
	    trackingSegment = new Dictionary<HumanBodyBones,Vector3>(targetBone.Length);

		for (int i = 0; i < targetBone.Length; i++) {
			trackingSegment [targetBone [i]] = Vector3.zero;
        }
    }

    public void Destroy()
    {
        GameObject.Destroy(humanoid);
        GameObject.Destroy(skeleton);
        rigBone.Clear();
        rigBoneTarget.Clear();
        Array.Clear(bones, 0, bones.Length);
        Array.Clear(spheres, 0, spheres.Length);
    }

	/// <summary>
	/// For Debug Only. Set Ideal Joint position for specific position
	/// </summary>
	/// <param name="type">Type.</param>
	public void setIdealPosition(int type)
	{
		//left knee up in front of us + "plane" down left to up right
		if (type == 0) {
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f); 
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f); 
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f); 

			joint [JointType_ShoulderRight] = new Vector3 (-0.5f, 1.70f, 0.0f); 
			joint [JointType_ElbowRight] = new Vector3 (-1.0f, 1.90f, 0.0f); 
			joint [JointType_WristRight] = new Vector3 (-1.5f, 2.10f, 0.0f); 

			joint [JointType_HipRight] = new Vector3 (-0.5f, 1.0f, 0.0f); 
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.0f); 
			joint [JointType_AnkleRight] = new Vector3 (-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 1.70f, 0.0f);
            joint [JointType_ElbowLeft] = new Vector3 (1.0f, 1.50f, 0.0f); 
			joint [JointType_WristLeft] = new Vector3 (1.5f, 1.30f, 0.0f); 

			joint [JointType_HipLeft] = new Vector3 (0.5f, 1.0f, 0.0f); 
			joint [JointType_KneeLeft] = new Vector3 (0.6f, 1.0f, -1.0f); 
			joint [JointType_AnkleLeft] = new Vector3 (0.5f, 0.0f, -1.0f);

            joint[JointType_HearLeft] = new Vector3(0.5f, 2.0f, 0f);
            joint[JointType_HearRight] = new Vector3(-0.5f, 2.0f, 0f);
        } 
		else if (type == 1) // right knee up back to camera + "plane" down left to up right
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f); 
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f); 
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f); 

			joint [JointType_ShoulderRight] = new Vector3 (0.5f, 1.70f, 0.0f); 
			joint [JointType_ElbowRight] = new Vector3 (1.0f, 1.90f, 0.0f); 
			joint [JointType_WristRight] = new Vector3 (1.5f, 2.10f, 0.0f); 
 
			joint [JointType_HipRight] = new Vector3 (0.5f, 1.0f, 0.0f);
			joint [JointType_KneeRight] = new Vector3 (0.5f, 1.0f, 0.5f);
			joint [JointType_AnkleRight] = new Vector3 (0.5f, 0.0f, 0.5f);


			joint [JointType_ShoulderLeft] = new Vector3 (-0.5f, 1.70f, 0.0f);
			joint [JointType_ElbowLeft] = new Vector3 (-1.0f, 1.50f, 0.0f);
			joint [JointType_WristLeft] = new Vector3 (-1.5f, 1.30f, 0.0f);

			joint [JointType_HipLeft] = new Vector3 (-0.5f, 1.0f, 0.0f);
			joint [JointType_KneeLeft] = new Vector3 (-0.5f, 0.50f, 0.0f);
			joint [JointType_AnkleLeft] = new Vector3 (-0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(-0.25f, 2.2f, 0f);
            joint[JointType_HearRight] = new Vector3(0.25f, 1.8f, 0f);

            joint[JointType_EyesLeft] = new Vector3(-0.20f, 2.1f, 0f);
            joint[JointType_EyesRight] = new Vector3(0.20f, 1.9f, 0f);

        }
		else if (type == 2) //On knees, profile , "prayer style"
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (0.0f, 1.70f, -0.50f);
			joint [JointType_ElbowRight] = new Vector3 (0.5f, 1.70f, -0.50f);
			joint [JointType_WristRight] = new Vector3 (0.5f, 2.00f, -0.1f);

			joint [JointType_HipRight] = new Vector3 (0.0f, 1.0f, -0.25f);
			joint [JointType_KneeRight] = new Vector3 (0.0f, 0.0f, -0.25f);
			joint [JointType_AnkleRight] = new Vector3 (-1.5f, 0.0f,-0.25f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.0f, 1.70f, 0.50f);
			joint [JointType_ElbowLeft] = new Vector3 (0.5f, 1.70f, 0.50f);
			joint [JointType_WristLeft] = new Vector3 (0.5f, 2.00f, 0.1f);

			joint [JointType_HipLeft] = new Vector3 (0.0f, 1.0f, 0.25f);
			joint [JointType_KneeLeft] = new Vector3 (0.0f, 0.00f, 0.25f);
			joint [JointType_AnkleLeft] = new Vector3 (-1.5f, 0.0f, 0.25f);


		}

		else if (type == 3) //squat
		{
			joint [JointType_Head] = new Vector3 (-0.3f, 2.0f, 0.0f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (0.0f, 1.70f, 0.50f);
			joint [JointType_ElbowRight] = new Vector3 (-0.5f, 1.70f, 0.50f);
			joint [JointType_WristRight] = new Vector3 (-1.0f, 1.70f, 0.5f);

			joint [JointType_HipRight] = new Vector3 (0.0f, 1.0f, 0.25f);
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.25f);
			joint [JointType_AnkleRight] = new Vector3 (-0.0f, 0.0f,0.25f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.0f, 1.70f, -0.50f);
			joint [JointType_ElbowLeft] = new Vector3 (-0.5f, 1.70f, -0.50f);
			joint [JointType_WristLeft] = new Vector3 (-1.0f, 1.70f,- 0.5f);

			joint [JointType_HipLeft] = new Vector3 (0.0f, 1.0f, -0.25f);
			joint [JointType_KneeLeft] = new Vector3 (-0.5f, 0.5f, -0.25f);
			joint [JointType_AnkleLeft] = new Vector3 (-0.0f, 0.0f,-0.25f);

            joint[JointType_HearLeft] = new Vector3(-0.25f, 1.8f, 0f);
            joint[JointType_HearRight] = new Vector3(0.25f, 2.2f, 0f);

            joint[JointType_EyesLeft] = new Vector3(-0.20f, 1.9f, 0f);
            joint[JointType_EyesRight] = new Vector3(0.20f, 2.1f, 0f);
        }

		else if (type == 4) //squat é
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (0.0f, 1.70f, 0.50f);
			joint [JointType_ElbowRight] = new Vector3 (0.0f, 1.70f, 1.00f);
			joint [JointType_WristRight] = new Vector3 (0.0f, 2.10f, 1.0f);

			joint [JointType_HipRight] = new Vector3 (0.0f, 1.0f, 0.25f);
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.25f);
			joint [JointType_AnkleRight] = new Vector3 (-0.0f, 0.0f,0.25f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.0f, 1.70f, -0.50f);
			joint [JointType_ElbowLeft] = new Vector3 (0.0f, 1.70f, -1.00f);
			joint [JointType_WristLeft] = new Vector3 (0.0f, 1.70f,- 1.5f);

			joint [JointType_HipLeft] = new Vector3 (0.0f, 1.0f, -0.25f);
			joint [JointType_KneeLeft] = new Vector3 (-0.5f, 0.5f, -0.25f);
			joint [JointType_AnkleLeft] = new Vector3 (-0.0f, 0.0f,-0.25f);

		}

		else if (type == 5) //chest move
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (-0.5f, 1.70f, 0.5f);
			joint [JointType_ElbowRight] = new Vector3 (-1.0f, 1.90f, 0.9f);
			joint [JointType_WristRight] = new Vector3 (-1.5f, 2.10f, 1.3f);

			joint [JointType_HipRight] = new Vector3 (-0.5f, 1.0f, 0.0f);
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.0f);
			joint [JointType_AnkleRight] = new Vector3 (-0.5f, 0.0f, 0.0f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.5f, 1.70f, -0.5f);
			joint [JointType_ElbowLeft] = new Vector3 (1.0f, 1.50f, -0.9f);
			joint [JointType_WristLeft] = new Vector3 (1.5f, 1.30f, -1.4f);

			joint [JointType_HipLeft] = new Vector3 (0.5f, 1.0f, 0.0f);
			joint [JointType_KneeLeft] = new Vector3 (0.6f, 1.0f, -1.0f);
			joint [JointType_AnkleLeft] = new Vector3 (0.5f, 0.0f, -1.0f);


		}
		else if (type == 6 ) //wondering position
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, 0.0f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0.0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, 0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (-0.5f, 1.70f, 0.0f);
			joint [JointType_ElbowRight] = new Vector3 (-1.0f, 1.50f, -0.15f);
			joint [JointType_WristRight] = new Vector3 (-0.5f, 1.30f, -0.3f);

			joint [JointType_HipRight] = new Vector3 (-0.5f, 1.0f, 0.0f);
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.0f);
			joint [JointType_AnkleRight] = new Vector3 (-0.5f, 0.0f, 0.0f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.5f, 1.70f, 0.0f);
			joint [JointType_ElbowLeft] = new Vector3 (1.0f, 1.50f, -0.15f);
			joint [JointType_WristLeft] = new Vector3 (0.5f, 1.30f, -0.3f);

			joint [JointType_HipLeft] = new Vector3 (0.5f, 1.0f, 0.0f);
			joint [JointType_KneeLeft] = new Vector3 (0.6f, 0.50f, 0.0f);
			joint [JointType_AnkleLeft] = new Vector3 (0.5f, 0.0f, 0.0f);

		}
		else if (type == 7 ) // stretching
		{
            joint[JointType_Head] = new Vector3(0.0f, 2.0f, 0.5f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.75f, 0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 1.0f, -0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 1.65f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-1.0f, 1.50f, -0.15f);
            joint[JointType_WristRight] = new Vector3(-0.75f, 1.30f, -0.3f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 1.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.5f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 1.85f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(1.0f, 1.50f, -0.15f);
            joint[JointType_WristLeft] = new Vector3(0.75f, 1.30f, -0.3f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 1.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.75f, 0.50f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.75f, 0.25f, 0.50f);

            joint[JointType_HearLeft] = new Vector3(0.30f, 2.0f, -0.25f);
            joint[JointType_HearRight] = new Vector3(-0.30f, 2.0f, -0.25f);

            joint[JointType_EyesLeft] = new Vector3(0.25f, 2.0f, -0.5f);
            joint[JointType_EyesRight] = new Vector3(-0.25f, 2.0f, -0.5f);

        }
		else if (type == 8 ) // stretching 2
		{
			joint [JointType_Head] = new Vector3 (0.0f, 2.0f, -0.5f);
			joint [JointType_Neck] = new Vector3 (0.0f, 1.75f, 0f);
			joint [JointType_SpineBase] = new Vector3 (0.0f, 1.0f, -0.0f);

			joint [JointType_ShoulderRight] = new Vector3 (-0.5f, 1.65f, 0.0f);
			joint [JointType_ElbowRight] = new Vector3 (-1.0f, 1.50f, -0.15f);
			joint [JointType_WristRight] = new Vector3 (-0.75f, 1.30f, -0.3f);

			joint [JointType_HipRight] = new Vector3 (-0.5f, 1.0f, 0.0f);
			joint [JointType_KneeRight] = new Vector3 (-0.5f, 0.5f, 0.0f);
			joint [JointType_AnkleRight] = new Vector3 (-0.5f, 0.0f, 0.0f);

			joint [JointType_ShoulderLeft] = new Vector3 (0.5f, 1.85f, 0.0f);
			joint [JointType_ElbowLeft] = new Vector3 (1.0f, 1.50f, -0.15f);
			joint [JointType_WristLeft] = new Vector3 (0.75f, 1.30f, -0.3f);

			joint [JointType_HipLeft] = new Vector3 (0.5f, 1.0f, 0.0f);
			joint [JointType_KneeLeft] = new Vector3 (0.75f, 0.50f, 0.0f);
			joint [JointType_AnkleLeft] = new Vector3 (0.75f, 0.25f, 0.50f);

            joint[JointType_HearLeft] = new Vector3(0.30f, 2.0f, -0.25f);
            joint[JointType_HearRight] = new Vector3(-0.30f, 2.0f, -0.25f);

            joint[JointType_EyesLeft] = new Vector3(0.25f, 2.0f, -0.5f);
            joint[JointType_EyesRight] = new Vector3(-0.25f, 2.0f, -0.5f);

        }
        else if (type == 9) //
        {

            joint[JointType_Head] = new Vector3(0.00f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_WristRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_WristLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(0.0f, 2.0f, 1f);
            joint[JointType_HearRight] = new Vector3(0.0f, 2.0f, -1f);

            joint[JointType_EyesLeft] = new Vector3(0.0f, 2.0f, 0.5f);
            joint[JointType_EyesRight] = new Vector3(0.0f, 2.0f, -0.5f);
        }
        else if (type == 10) //
        {
            joint[JointType_Head] = new Vector3(0.00f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_WristRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_WristLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(0.0f, 2.0f, -1f);
            joint[JointType_HearRight] = new Vector3(0.0f, 2.0f, 1f);

            joint[JointType_EyesLeft] = new Vector3(0.0f, 2.0f, -0.5f);
            joint[JointType_EyesRight] = new Vector3(0.0f, 2.0f, 0.5f);
        }
        else if (type == 11) //
        {

            joint[JointType_Head] = new Vector3(0.5f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_WristRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_WristLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(0.75f, 1.75f, 0.0f);
            joint[JointType_HearRight] = new Vector3(0.15f, 2.25f, 0.0f);

            joint[JointType_EyesLeft] = new Vector3(0.65f, 1.85f, 0.0f);
            joint[JointType_EyesRight] = new Vector3(0.25f, 2.15f, 0.0f);
        }
        else if (type == 12) //
        {

            joint[JointType_Head] = new Vector3(-0.5f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_WristRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_WristLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(-0.15f,2.25f, 0.0f);
            joint[JointType_HearRight] = new Vector3(-0.75f,1.75f, 0.0f);

            joint[JointType_EyesLeft] = new Vector3(-0.25f,2.15f, 0.0f);
            joint[JointType_EyesRight] = new Vector3(-0.65f, 1.85f, 0.0f);
        }
        else if (type == 13) //
        {

            joint[JointType_Head] = new Vector3(-0.3f, 2.0f, -0.5f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_WristRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_HipRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_KneeRight] = new Vector3(-0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleRight] = new Vector3(-0.5f, 0.0f, 0.0f);

            joint[JointType_ShoulderLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_ElbowLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_WristLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HipLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_KneeLeft] = new Vector3(0.5f, 0.0f, 0.0f);
            joint[JointType_AnkleLeft] = new Vector3(0.5f, 0.0f, 0.0f);

            joint[JointType_HearLeft] = new Vector3(-0.25f, 2.35f, -0.55f);
            joint[JointType_HearRight] = new Vector3(-0.65f, 1.65f, -0.45f);

            joint[JointType_EyesLeft] = new Vector3(-0.30f, 2.25f, -0.55f);
            joint[JointType_EyesRight] = new Vector3(-0.60f, 1.80f, -0.45f);
        }
        else if (type == 14) //
        {
            joint[JointType_Head] = new Vector3(0.00f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_ElbowRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_WristRight] = new Vector3(-0.05f, 0.0f, 0.4f);

            joint[JointType_HipRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_KneeRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_AnkleRight] = new Vector3(-0.05f, 0.0f, 0.40f);

            joint[JointType_ShoulderLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_ElbowLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_WristLeft] = new Vector3(0.05f, 0.0f,- 0.40f);

            joint[JointType_HipLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_KneeLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_AnkleLeft] = new Vector3(0.05f, 0.0f, -0.40f);

            joint[JointType_HearLeft] = new Vector3(0.30f, 2.0f, 0.0f);
            joint[JointType_HearRight] = new Vector3(-0.30f, 2.0f, 0.0f);

            joint[JointType_EyesLeft] = new Vector3(0.25f, 2.0f, 0.0f);
            joint[JointType_EyesRight] = new Vector3(-0.25f, 2.0f, 0.0f);
        }
        else if (type == 15) //
        {
            joint[JointType_Head] = new Vector3(0.00f, 2.0f, 0f);
            joint[JointType_Neck] = new Vector3(0.0f, 1.5f, 0.0f);
            joint[JointType_SpineBase] = new Vector3(0.0f, 0.0f, 0.0f);

            joint[JointType_ShoulderRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_ElbowRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_WristRight] = new Vector3(-0.05f, 0.0f, 0.4f);

            joint[JointType_HipRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_KneeRight] = new Vector3(-0.05f, 0.0f, 0.4f);
            joint[JointType_AnkleRight] = new Vector3(-0.05f, 0.0f, 0.40f);

            joint[JointType_ShoulderLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_ElbowLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_WristLeft] = new Vector3(0.05f, 0.0f, -0.40f);

            joint[JointType_HipLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_KneeLeft] = new Vector3(0.05f, 0.0f, -0.40f);
            joint[JointType_AnkleLeft] = new Vector3(0.05f, 0.0f, -0.40f);

            joint[JointType_HearLeft] = new Vector3(-0.30f, 2.0f, 0.0f);
            joint[JointType_HearRight] = new Vector3(0.30f, 2.0f, 0.0f);

            joint[JointType_EyesLeft] = new Vector3(-0.25f, 2.0f, 0.0f);
            joint[JointType_EyesRight] = new Vector3(0.25f, 2.0f, 0.0f);
        }

    }

	/// <summary>
	/// Sets the fake position test.
	/// </summary>
	public void setFakeTest(int index)
	{
		setIdealPosition (index);
		setHumanPoseControl(new Vector3(0.0f,0.0f,0.0f));
	}

	/// <summary>
	/// Function that handles the humanoid position, rotation and bones movement
	/// </summary>
	/// <param name="position_center">Position center.</param>
	private void setHumanPoseControl(Vector3 position_center)
	{
        Vector3 waist;
        Quaternion waistrot = oldwaistrot;
        Quaternion inv_waistrot =  Quaternion.Inverse(waistrot);

        if (!ZEDSupportFunctions.IsVector3NaN(joint[JointType_HipRight]) && !ZEDSupportFunctions.IsVector3NaN(joint[JointType_HipLeft]))
        {
            waist = joint[JointType_HipRight] - joint[JointType_HipLeft];
            waist = new Vector3(waist.x, 0, waist.z);
            waistrot = Quaternion.FromToRotation(Vector3.right, waist);
            inv_waistrot = Quaternion.Inverse(waistrot);

        }

        Vector3 shoulder;
        Quaternion shoulderrot = oldshoulderrot;
        Quaternion inv_shoulderrot =  Quaternion.Inverse(waistrot);

        if (!ZEDSupportFunctions.IsVector3NaN(joint[JointType_ShoulderRight]) && !ZEDSupportFunctions.IsVector3NaN(joint[JointType_ShoulderLeft]))
        {

            shoulder = joint[JointType_ShoulderRight] - joint[JointType_ShoulderLeft];
            shoulder = new Vector3(shoulder.x, 0, shoulder.z);
            shoulderrot = Quaternion.FromToRotation(Vector3.right, shoulder);
            inv_shoulderrot = Quaternion.Inverse(shoulderrot);
        }

        if (Quaternion.Angle(waistrot, shoulderrot) > 45 || Quaternion.Angle(waistrot, shoulderrot) < -45)
        {
            shoulderrot = oldshoulderrot;
        }

        for (int i=0; i<targetBone.Length; i++) {
			int s = jointSegment[2*i], e = jointSegment[2*i+1];
			if (!ZEDSupportFunctions.IsVector3NaN (joint [e]) && !ZEDSupportFunctions.IsVector3NaN (joint [s])) {
				trackingSegment [targetBone [i]] = (joint [e] - joint [s]).normalized;
			}
		}

        foreach (HumanBodyBones bone in targetBone)
        {
            rigBoneTarget[bone] = waistrot * Quaternion.identity;
        }

        Vector3 eyesVector = (joint[JointType_EyesLeft] + joint[JointType_HearLeft]) / 2 - (joint[JointType_EyesRight] + joint[JointType_HearRight]) / 2;
        Vector3 headVector = joint[JointType_Head] - joint[JointType_Neck];
        Vector3 headOrientationVector = Vector3.Cross(headVector, eyesVector);

        if (headOrientationVector != Vector3.zero && headVector != Vector3.zero && !ZEDSupportFunctions.IsVector3NaN(headOrientationVector) && !ZEDSupportFunctions.IsVector3NaN(headVector)) 
            rigBoneTarget[HumanBodyBones.Neck] = Quaternion.LookRotation(headOrientationVector, headVector);
        else
            rigBoneTarget[HumanBodyBones.Neck] = Quaternion.FromToRotation(shoulderrot * Vector3.up, trackingSegment[HumanBodyBones.Spine]) * shoulderrot;

        rigBoneTarget[HumanBodyBones.Spine] = Quaternion.FromToRotation (waistrot * Vector3.up, trackingSegment [HumanBodyBones.Spine]) * waistrot ;

        rigBoneTarget[HumanBodyBones.LeftUpperArm] = Quaternion.FromToRotation (shoulderrot * Vector3.left, trackingSegment [HumanBodyBones.LeftUpperArm]) * shoulderrot;
		rigBoneTarget [HumanBodyBones.LeftLowerArm] = Quaternion.FromToRotation (shoulderrot * Vector3.left, trackingSegment [HumanBodyBones.LeftLowerArm]) * shoulderrot;
        rigBoneTarget [HumanBodyBones.RightUpperArm] = Quaternion.FromToRotation (shoulderrot * Vector3.right, trackingSegment [HumanBodyBones.RightUpperArm]) * shoulderrot;
		rigBoneTarget [HumanBodyBones.RightLowerArm] = Quaternion.FromToRotation (shoulderrot * Vector3.right, trackingSegment [HumanBodyBones.RightLowerArm]) * shoulderrot;

        rigBoneTarget [HumanBodyBones.LeftUpperLeg] = Quaternion.FromToRotation (waistrot * Vector3.down, trackingSegment [HumanBodyBones.LeftUpperLeg]) * waistrot ;
		rigBoneTarget [HumanBodyBones.LeftLowerLeg] = Quaternion.FromToRotation (waistrot * Vector3.down, trackingSegment [HumanBodyBones.LeftLowerLeg]) * waistrot ;
		rigBoneTarget [HumanBodyBones.RightUpperLeg] = Quaternion.FromToRotation (waistrot * Vector3.down, trackingSegment [HumanBodyBones.RightUpperLeg]) * waistrot ;
		rigBoneTarget [HumanBodyBones.RightLowerLeg] = Quaternion.FromToRotation (waistrot * Vector3.down, trackingSegment [HumanBodyBones.RightLowerLeg]) * waistrot ;

		rigBone [HumanBodyBones.UpperChest].offset (inv_waistrot * shoulderrot);
        targetBodyOrientation = waistrot;

        targetBodyPosition = new Vector3(position_center.x,position_center.y,position_center.z);

        oldshoulderrot = shoulderrot;
        oldwaistrot = waistrot;
    }

    public void initSkeleton(int person_id)
    {
        bones = new GameObject[bonesList.Length / 2];
        spheres = new GameObject[sphereList.Length];
        skeleton = new GameObject();
        skeleton.name = "Skeleton_ID_" + person_id;
        float width = 0.025f;

        Color color = colors[person_id % colors.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.GetComponent<Renderer>().material.color = color;
            cylinder.transform.parent = skeleton.transform;
            bones[i] = cylinder;
        }
        for (int j = 0; j < spheres.Length; j++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = color;
            sphere.transform.localScale = new Vector3(width * 2, width * 2, width * 2);
            sphere.transform.parent = skeleton.transform;
            spheres[j] = sphere;
        }
    }

    void updateSkeleton()
    {
        float width = 0.025f;

        for (int j = 0; j < spheres.Length; j++)
        {
            if (ZEDSupportFunctions.IsVector3NaN(joint[sphereList[j]]))
            {
                spheres[j].transform.position = Vector3.zero;
                spheres[j].SetActive(false);
            }
            else
            {
                spheres[j].transform.position = joint[sphereList[j]];
                spheres[j].SetActive(true);
            }
        }

        for (int i = 0; i < bones.Length; i++)
        {
            Vector3 start = spheres[Array.IndexOf(sphereList, bonesList[2 * i])].transform.position;
            Vector3 end = spheres[Array.IndexOf(sphereList, bonesList[2 * i + 1 ])].transform.position;

            if (start == Vector3.zero || end == Vector3.zero)
            {
                bones[i].SetActive(false);
                continue;
            }

            bones[i].SetActive(true);
            Vector3 offset = end - start;
            Vector3 scale = new Vector3(width, offset.magnitude / 2.0f, width);
            Vector3 position = start + (offset / 2.0f);

            bones[i].transform.position = position;
            bones[i].transform.up = offset;
            bones[i].transform.localScale = scale;

        }
    }

    /// <summary>
    /// Sets the avatar control with joint position.
    /// </summary>
    /// <param name="jt">Jt.</param>
    /// <param name="position_center">Position center.</param>
    public void setControlWithJointPosition(Vector3[] jt, Vector3 position_center, bool useAvatar)
	{
		for (int i=0; i<jointCount; i++) {
			joint [i] = new Vector3(jt[i].x,jt[i].y,jt[i].z);
		}

        humanoid.SetActive(useAvatar);
        skeleton.SetActive(!useAvatar);

        if (useAvatar) setHumanPoseControl(position_center);
        else updateSkeleton();

	}

	/// <summary>
	/// For Debug only. Set the joint position as sphere.
	/// </summary>
	/// <param name="jt">Jt.</param>
	public void setJointSpherePoint(Vector3[] jt)
	{
		if (sphere.Count != 18) {
			for (int i = 0; i < jointCount; i++) {
				sphere.Add (GameObject.CreatePrimitive (PrimitiveType.Sphere));
			}
		}

		for (int i=0; i<jointCount; i++) {
            if (ZEDSupportFunctions.IsVector3NaN(joint[i])) continue;

            joint [i] = new Vector3(jt[i].x,jt[i].y,jt[i].z);

			sphere[i].transform.localScale = new Vector3 (0.05f, 0.05f, 0.05f);
			sphere[i].transform.position = joint [i];
		}
	}


	/// <summary>
	/// Set Humanoid position. Called in Update() function
	/// </summary>
	public void MoveAvatar()
	{
        if (isInit)
        {
            humanoid.transform.position = smoothFactor != 0f ? Vector3.Lerp(humanoid.transform.position, targetBodyPosition, smoothFactor) : targetBodyPosition;
            humanoid.transform.rotation = smoothFactor != 0f ? Quaternion.Lerp(humanoid.transform.rotation, targetBodyOrientation, smoothFactor) : targetBodyOrientation;
        }
        else
        {
            humanoid.transform.position = targetBodyPosition;
            humanoid.transform.rotation = targetBodyOrientation;
            isInit = true;
        }

        foreach (HumanBodyBones bone in targetBone) {
            if (smoothFactor != 0f)
                rigBone[bone].transform.rotation = Quaternion.Slerp(rigBone[bone].transform.rotation, rigBoneTarget[bone], smoothFactor);
            else
                rigBone[bone].transform.rotation = rigBoneTarget[bone];
		}
	}

	/// <summary>
	/// Update Engine function (move this avatar)
	/// </summary>
	public void Move()
	{
 		MoveAvatar ();
	}



}
