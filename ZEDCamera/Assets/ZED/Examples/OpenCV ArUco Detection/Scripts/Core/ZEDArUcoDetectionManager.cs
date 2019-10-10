#if ZED_OPENCV_FOR_UNITY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.Calib3dModule;

/// <summary>
/// Whenever the ZED grabs/captures an image, uses OpenCV to detect ArUCO markers, calculates their
/// world positions and rotations, and uses them to call the appropriate functions on any MarkerObject components in the scene. 
/// </summary>
public class ZEDArUcoDetectionManager : MonoBehaviour
{
    /// <summary>
    /// Scene's ZEDToOpenCVRetriever, which creates OpenCV mats and deploys events each time the ZED grabs an image. 
    /// It's how we get the image and required matrices that we use to look for markers. 
    /// </summary>
    public ZEDToOpenCVRetriever imageRetriever;

    /// <summary>
    /// Physical width of the printed ArUco markers. 
    /// Used to find the proper world position of the markers, so make sure this is accurate. 
    /// </summary>
    [Tooltip("Physical width of the printed ArUco markers.\r\n" +
        "Used to find the proper world position of the markers, so make sure this is accurate.")]
    public float markerWidthMeters = 0.2f;

    /// <summary>
    /// Pre-defined OpenCV dictionary of marker images used to identify markers in the scene. 
    /// Note that images included in this project in the Resources folder are from the DICT_4X4 ones. 
    /// See: https://docs.opencv.org/trunk/d9/d6a/group__aruco.html#gaf5d7e909fe8ff2ad2108e354669ecd17
    /// You can access any of the pre-defined dictionaries via this project: https://github.com/okalachev/arucogen
    /// </summary>
    [Tooltip("Pre-defined OpenCV dictionary of marker images used to identify markers in the scene.\r\n" +
        "Note that images included in this project in the Resources folder are from the DICT_4X4 ones.")]
    public PreDefinedmarkerDictionary markerDictionary = PreDefinedmarkerDictionary.DICT_4X4_50;

    /// <summary>
    /// Contains all MarkerObjects that have registered, sorted by their markerID values. 
    /// We iterate through them each grab, calling MarkerDetected() on each when their 
    /// corresponding markers are visible, and MarkerNotDetected() otherwise. 
    /// </summary>
    private static Dictionary<int, List<MarkerObject>> registeredMarkers = new Dictionary<int, List<MarkerObject>>();

    public delegate void MarkersDetectedEvent(Dictionary<int, List<sl.Pose>> detectedposes);
    public event MarkersDetectedEvent OnMarkersDetected;

    /// <summary>
    /// Adds a MarkerObject to the registeredMarkers dictionary, so its MarkerDetected() function will get called when 
    /// a marker is detected with its corresponding markerID. 
    /// </summary>
    /// <param name="marker"></param>
    public static void RegisterMarker(MarkerObject marker)
    {
        if (!registeredMarkers.ContainsKey(marker.markerID))
        {
            registeredMarkers.Add(marker.markerID, new List<MarkerObject>());
        }

        List<MarkerObject> idlist = registeredMarkers[marker.markerID];

        if (!idlist.Contains(marker))
        {
            idlist.Add(marker);
        }
        else
        {
            Debug.LogError("Tried to register " + marker.gameObject.name + " as a new marker, but it was already registered.");
        }
    }


    /// <summary>
    /// Removes a MarkerObject from the registeredMarkers dictionary. Called when a MarkerObject is destroyed. 
    /// </summary>
    /// <param name="marker"></param>
    public static void DeregisterMarker(MarkerObject marker)
    {

        if (registeredMarkers.ContainsKey(marker.markerID) && registeredMarkers[marker.markerID].Contains(marker))
        {
            registeredMarkers[marker.markerID].Remove(marker);
        }
        else
        {
            Debug.LogError("Tried to deregister " + marker.gameObject.name + " but it wasn't registered.");
        }
    }

    
    void Start()
    {
        //We'll listen for updates from a ZEDToOpenCVRetriever, which will call an event whenever it has a new image from the ZED. 
        if (!imageRetriever) imageRetriever = ZEDToOpenCVRetriever.GetInstance();
        imageRetriever.OnImageUpdated_LeftGrayscale += ImageUpdated;
    }

    /// <summary>
    /// Looks for markers in the most recent ZED image, and updates all registered MarkerObjects accordingly. 
    /// </summary>
    private void ImageUpdated(Camera cam, Mat camMat, Mat iamgeMat)
    {
        Dictionary predict = Aruco.getPredefinedDictionary((int)markerDictionary); //Load the selected pre-defined dictionary. 

        //Create empty structures to hold the output of Aruco.detectMarkers. 
        List<Mat> corners = new List<Mat>();
        Mat ids = new Mat();
        DetectorParameters detectparams = DetectorParameters.create();
        List<Mat> rejectedpoints = new List<Mat>(); //There is no overload for detectMarkers that will take camMat without this also. 

        //Call OpenCV to tell us which markers were detected, and give their 2D positions. 
        Aruco.detectMarkers(iamgeMat, predict, corners, ids, detectparams, rejectedpoints, camMat);

        //Make matrices to hold the output rotations and translations of each detected marker. 
        Mat rotvectors = new Mat();
        Mat transvectors = new Mat();

        //Convert the 2D corner positions into a 3D pose for each detected marker. 
        Aruco.estimatePoseSingleMarkers(corners, markerWidthMeters, camMat, new Mat(), rotvectors, transvectors);

        //Now we have ids, rotvectors and transvectors, which are all vertical arrays holding info about each detection:
        // - ids: An Nx1 array (N = number of markers detected) where each slot is the ID of a detected marker in the dictionary. 
        // - rotvectors: An Nx3 array where each row is for an individual detection: The first row is the rotation of the marker
        //    listed in the first row of ids, etc. The columns are the X, Y and Z angles of that marker's rotation, BUT they are not
        //    directly usable in Unity because they're calculated very differetly. We'll deal with that soon. 
        // - transvectors: An Nx1 array like rotvectors with each row corresponding to a detected marker, with a double[3] array with the X, Y and Z positions.
        //    positions. These three values are usable in Unity - they're just relative to the camera, not the world, which is easy to fix. 

        //Convert matrix of IDs into a List, to simply things for those not familiar with using Matrices. 
        List<int> detectedIDs = new List<int>();
        for (int i = 0; i < ids.height(); i++)
        {
            int id = (int)ids.get(i, 0)[0];
            if (!detectedIDs.Contains(id)) detectedIDs.Add(id);
        }

        //We'll go through every ID that's been registered into registered Markers, and see if we found any markers in the scene with that ID. 
        Dictionary<int, List<sl.Pose>> detectedWorldPoses = new Dictionary<int, List<sl.Pose>>(); //Key is marker ID, value is world space poses.
        //foreach (int id in registeredMarkers.Keys) 
        for(int index = 0; index < transvectors.rows(); index++)
        {
            int id = (int)ids.get(index, 0)[0];
            if (!registeredMarkers.ContainsKey(id) || registeredMarkers[id].Count == 0) continue; //Don't waste time if the list is empty. Can happen if markers are added, then removed. 

            if (detectedIDs.Contains(id)) //At least one MarkerObject needs to be updated. Convert pose to world space and call MarkerDetected() on it. 
            {
                //Translation is just pose relative to camera. But we need to flip Y because of OpenCV's different coordinate system. 
                Vector3 localpos;
                localpos.x = (float)transvectors.get(index, 0)[0];
                localpos.y = -(float)transvectors.get(index, 0)[1];
                localpos.z = (float)transvectors.get(index, 0)[2];

                Vector3 worldpos = cam.transform.TransformPoint(localpos); //Convert from local to world space. 

                //Because of different coordinate frame, we need to flip the Y direction, which is pointing down instead of up. 
                //We need to do this before we calculate the 3x3 rotation matrix soon, as that makes it muuuch harder to work with. 
                double[] flip = rotvectors.get(index, 0);
                flip[1] = -flip[1];
                rotvectors.put(index, 0, flip);
                
                //Convert this rotation vector to a 3x3 matrix, which will hold values we can use in Unity. 
                Mat rotmatrix = new Mat();
                Calib3d.Rodrigues(rotvectors.row(index), rotmatrix); 

                //This new 3x3 matrix holds a vector pointing right in the first column, a vector pointing up in the second, 
                //and a vector pointing forward in the third column. Rows 0, 1 and 2 are the X, Y and Z values of each vector. 
                //We'll grab the forward and up vectors, which we can put into Quaternion.LookRotation() to get a representative Quaternion. 
                Vector3 forward;
                forward.x = (float)rotmatrix.get(2, 0)[0];
                forward.y = (float)rotmatrix.get(2, 1)[0];
                forward.z = (float)rotmatrix.get(2, 2)[0];

                Vector3 up;
                up.x = (float)rotmatrix.get(1, 0)[0];
                up.y = (float)rotmatrix.get(1, 1)[0];
                up.z = (float)rotmatrix.get(1, 2)[0];

                Quaternion rot = Quaternion.LookRotation(forward, up);

                //Compensate for flip on Z axis. 
                rot *= Quaternion.Euler(0, 0, 180); 

                //Convert from local space to world space by multiplying the camera's world rotation with it. 
                Quaternion worldrot = cam.transform.rotation * rot;

                if(!detectedWorldPoses.ContainsKey(id))
                {
                    detectedWorldPoses.Add(id, new List<sl.Pose>());
                }
                detectedWorldPoses[id].Add(new sl.Pose() { translation = worldpos, rotation = worldrot });

                foreach (MarkerObject marker in registeredMarkers[id])
                {
                    marker.MarkerDetectedSingle(worldpos, worldrot);
                }
            }
        }

        //Call the event that gives all marker world poses, if any listeners. 
        if (OnMarkersDetected != null) OnMarkersDetected.Invoke(detectedWorldPoses);

        //foreach (int detectedid in detectedWorldPoses.Keys)
        foreach(int key in registeredMarkers.Keys)
        {
            if (detectedWorldPoses.ContainsKey(key))
            {
                foreach (MarkerObject marker in registeredMarkers[key])
                {
                    marker.MarkerDetectedAll(detectedWorldPoses[key]);
                }
            }
            else
            {
                foreach (MarkerObject marker in registeredMarkers[key])
                {
                    marker.MarkerNotDetected();
                }
            }
        }
    }
   

    /// <summary>
    /// Enum of OpenCV pre-defined dictionary indexes. Used for calling Aruco.getPredefinedDictionary().  
    /// Allows for listing the dictionaries in Unity's inspector. 
    /// </summary>
    public enum PreDefinedmarkerDictionary
    {
        DICT_4X4_50 = Aruco.DICT_4X4_50,
        DICT_4X4_100 = Aruco.DICT_4X4_100,
        DICT_4X4_250 = Aruco.DICT_4X4_250,
        DICT_4X4_1000 = Aruco.DICT_4X4_1000,
        DICT_5X5_50 = Aruco.DICT_5X5_50,
        DICT_5X5_100 = Aruco.DICT_5X5_100,
        DICT_5X5_250 = Aruco.DICT_5X5_250,
        DICT_5X5_1000 = Aruco.DICT_5X5_1000,
        DICT_6X6_50 = Aruco.DICT_6X6_50,
        DICT_6X6_100 = Aruco.DICT_6X6_100,
        DICT_6X6_250 = Aruco.DICT_6X6_250,
        DICT_6X6_1000 = Aruco.DICT_6X6_1000,
        DICT_7X7_50 = Aruco.DICT_7X7_50,
        DICT_7X7_100 = Aruco.DICT_7X7_100,
        DICT_7X7_250 = Aruco.DICT_7X7_250,
        DICT_7X7_1000 = Aruco.DICT_7X7_1000,
        DICT_ARUCO_ORIGINAL = Aruco.DICT_ARUCO_ORIGINAL,
        DICT_APRILTAG_16h5 = Aruco.DICT_APRILTAG_16h5,
        DICT_APRILTAG_25h9 = Aruco.DICT_APRILTAG_25h9,
        DICT_APRILTAG_36h10 = Aruco.DICT_APRILTAG_36h10,
        DICT_APRILTAG_36h11 = Aruco.DICT_APRILTAG_36h11
    }
}

#endif