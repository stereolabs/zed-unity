#if ZED_OPENCV_FOR_UNITY

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using sl;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Whenever the ZED captures an image, this calls events with an openCV version of that image along with
/// camera information, for the purposes of making other OpenCV calls like marker detection. 
/// <para>Currently only retrieves Left RGB and Left Grayscale images, but you can add any other kinds of 
/// sl.VIEW images by creating a new ImageUpdatedEvent event and adding a call to DeployGrabbedEvent() in OnImageUpdated().
///</para></summary>
public class ZEDToOpenCVRetriever : MonoBehaviour
{
#region Singleton Implementation

    private static ZEDToOpenCVRetriever _instance;
    /// <summary>
    /// Finds the first available ZEDToOpenCVRetriever in the scene, or creates a new one (Singleton).
    /// Note that it's better to have the component in the Scene ahead of time, especially if using multiple ZED cameras. 
    /// </summary>
    /// <returns></returns>
    public static ZEDToOpenCVRetriever GetInstance()
    {
        if(!_instance)
        {
            GameObject go = new GameObject("ZED to OpenCV Retriever");
            _instance = go.AddComponent<ZEDToOpenCVRetriever>();
        }
        return _instance;
    }

#endregion

    /// <summary>
    /// ZEDManager in the scene used to grab the image. 
    /// Note this script isn't currently designed for multiple ZED's all detecting markers.
    /// </summary>
    [Tooltip("ZEDManager in the scene used to grab the image." +
        "Note this script isn't currently designed for multiple ZEDs.")]
    public ZEDManager zedManager;

    /// <summary>
    /// Left camera on the scene's ZED rig. Assigned with ZEDManager.zedCamera. 
    /// </summary>
    private ZEDCamera zedCam;
    /// <summary>
    /// OpenCV matrix representing the projection matrix of the ZED camera. 
    /// </summary>
    private Mat camMat;

    public delegate void ImageUpdatedEvent(Camera cam, Mat camMat, Mat imageMat);
    /// <summary>
    /// Called whenever the ZED grabs an image from the left camera, and invoked with an OpenCV Mat of the color version of that image (BGRA format). 
    /// </summary>
    public event ImageUpdatedEvent OnImageUpdated_LeftBGRA;
    /// <summary>
    /// Called whenever the ZED grabs an image from the left camera, and invoked with an OpenCV Mat of the color version of that image (BGRA format). 
    /// </summary>
    public event ImageUpdatedEvent OnImageUpdated_LeftBGR;
    /// <summary>
    /// Called whenever the ZED grabs an image from the left camera, and invoked with an OpenCV Mat of the color version of that image (RGBA format). 
    /// </summary>
    public event ImageUpdatedEvent OnImageUpdated_LeftRGBA;
    /// <summary>
    /// Called whenever the ZED grabs an image from the left camera, and invoked with an OpenCV Mat of the color version of that image (RGB format). 
    /// </summary>
    public event ImageUpdatedEvent OnImageUpdated_LeftRGB;
    /// <summary>
    /// Called whenever the ZED grabs an image from the left camera, and invoked with an OpenCV Mat of the grayscale version of that image
    /// </summary>
    public event ImageUpdatedEvent OnImageUpdated_LeftGrayscale;

    //Cached matrices so we don't have to create new ones each frame. 
    private ZEDMat zedLeftBGRAMat;
    private ZEDMat zedLeftGrayMat;

    /// <summary>
    /// Used to store OpenCV Mat buffers we use to copy from the ZED to OpenCV, before any relevant conversions. 
    /// Key is the int that corresponds to a CvType. (It's not an enum for some reason)
    /// </summary>
    private Dictionary<int, Mat> openCVBufferMats = new Dictionary<int, Mat>();

    private Mat cvLeftBGRAMat;
    private Mat cvLeftBGRMat;
    private Mat cvLeftRGBAMat;
    private Mat cvLeftRGBMat;
    private Mat cvLeftGrayMat;

    /// <summary>
    /// Whether the camera has been set up yet, used for blocking calls made too early. 
    /// </summary>
    private bool isInit = false;

    private void Awake()
    {
        _instance = this; //Singleton implementation. 
    }

    // Use this for initialization
    void Start ()
    {
        if (!zedManager) zedManager = FindObjectOfType<ZEDManager>();

        //We can't call initialize() now, because it needs the ZED's projection matrix to build an OpenCV Mat of it. 
        //So we wait until OnZEDReady is called. Then we can get the ZED's calibration parameters. 
        zedManager.OnZEDReady += Initialize;

        //Each grab, we'll fill the zedMat from the ZED SDK, copy it to cvMat, and call relevant events. 
        zedManager.OnGrab += OnZEDGrabbed; 
    }

    /// <summary>
    /// Creates an OpenCV matrix based on the ZED camera's rectified parameters. This is specific to each camera. 
    /// </summary>
    private void Initialize()
    {
        zedCam = zedManager.zedCamera;
        sl.CameraParameters camparams = zedCam.CalibrationParametersRectified.leftCam;

        //Shorthand. 
        double fx = camparams.fx;
        double fy = camparams.fy;
        double cx = camparams.cx;
        double cy = camparams.cy;

        //Build a rough 3x3 matrix for OpenCV with the camera's parameters. 
        camMat = new Mat(3, 3, CvType.CV_64FC1);

        camMat.put(0, 0, fx);
        camMat.put(0, 1, 0);
        camMat.put(0, 2, cx);
        camMat.put(1, 0, 0);
        camMat.put(1, 1, fy);
        camMat.put(1, 2, cy);
        camMat.put(2, 0, 0);
        camMat.put(2, 1, 0);
        camMat.put(2, 2, 0.0f);

        Size imageSize = new Size(zedCam.ImageWidth, zedCam.ImageHeight);

        //Make ZED and OpenCV versions of the image matrices. 
        //Each grab, we'll fill the zedMat from the ZED SDK, copy it to cvMat, and run marker detection on it. 
        //8U_C1(8 bits, one channel) as we're using a grayscale image for performance. 
        //zedMat = new ZEDMat((uint)zedCam.ImageWidth, (uint)zedCam.ImageHeight, sl.ZEDMat.MAT_TYPE.MAT_8U_C1);
        //cvMat = SLMat2CVMat(zedMat, ZEDMat.MAT_TYPE.MAT_8U_C1);

        isInit = true;
    }

    /// <summary>
    /// Gets all images needed for any of the listeners for any of the OnImageUpdated events, converts them to an OpenCV Mat, and calls the events.
    /// Called from zedManager.OnGrab() whenever the ZED updates the image. 
    /// You can extend this event to add other kinds of ZED Images, like VIEW.RIGHT, and add more events appropriately. 
    /// </summary>
    private void OnZEDGrabbed()
    {
        if (!isInit) return; //We haven't set up the camera mat yet, so we can't call any of the events that need it. 

        if(OnImageUpdated_LeftBGRA != null && OnImageUpdated_LeftBGRA.GetInvocationList().Length > 0) //Regular left color image as BGRA. 
        {
            DeployGrabbedEvent(zedManager.GetLeftCamera(), ref zedLeftBGRAMat, VIEW.LEFT, ZEDMat.MAT_TYPE.MAT_8U_C4, ref cvLeftBGRAMat, OnImageUpdated_LeftBGRA);
        }

        if (OnImageUpdated_LeftBGR != null && OnImageUpdated_LeftBGR.GetInvocationList().Length > 0) //Regular left color image as BGRA. 
        {
            DeployGrabbedEvent(zedManager.GetLeftCamera(), ref zedLeftBGRAMat, VIEW.LEFT, ZEDMat.MAT_TYPE.MAT_8U_C4, ref cvLeftBGRMat, OnImageUpdated_LeftBGR, OpenCVConversion.BGRA2BGR);
        }

        if (OnImageUpdated_LeftRGBA != null && OnImageUpdated_LeftRGBA.GetInvocationList().Length > 0) //Regular left color image as BGRA. 
        {
            DeployGrabbedEvent(zedManager.GetLeftCamera(), ref zedLeftBGRAMat, VIEW.LEFT, ZEDMat.MAT_TYPE.MAT_8U_C4, ref cvLeftRGBAMat, OnImageUpdated_LeftRGBA, OpenCVConversion.BGRA2RGBA);
        }

        if (OnImageUpdated_LeftRGB != null && OnImageUpdated_LeftRGB.GetInvocationList().Length > 0) //Regular left color image as BGRA. 
        {
            DeployGrabbedEvent(zedManager.GetLeftCamera(), ref zedLeftBGRAMat, VIEW.LEFT, ZEDMat.MAT_TYPE.MAT_8U_C4, ref cvLeftRGBMat, OnImageUpdated_LeftRGB, OpenCVConversion.BGRA2RGB);
        }

        if (OnImageUpdated_LeftGrayscale != null && OnImageUpdated_LeftGrayscale.GetInvocationList().Length > 0) //Left grayscale image. 
        {
            DeployGrabbedEvent(zedManager.GetLeftCamera(), ref zedLeftGrayMat, VIEW.LEFT_GREY, ZEDMat.MAT_TYPE.MAT_8U_C1, ref cvLeftGrayMat, OnImageUpdated_LeftGrayscale);
        }
    }

    /// <summary>
    /// Copies the given ZEDMat to a given OpenCV mat, creating either or both mats if necessary, then calls an ImageUpdatedEvent with them. 
    /// Used in OnZEDGrabbed to call different events, and to make it easy to add more kinds of images/events by just adding more calls to this method. 
    /// </summary>
    /// <param name="cam">Unity Camera object that represents the ZED camera. Usually from ZEDManager.GetLeftCamera() or ZEDManager.GetRightCamera().</param>
    /// <param name="zedmat">ZEDMat used to get the ZED image. Passing an empty one is okay - it'll get filled appropriately.</param>
    /// <param name="view">Type of image requested, like LEFT or LEFT_GRAY.</param>
    /// <param name="mattype">Data type and channel of required ZEDMat. See summaries over each enum entry to know which is correct for your image type.</param>
    /// <param name="cvMat">OpenCV mat to copy to. Passing an empty one is okay - it'll get filled appropriately.</param>
    /// <param name="updateevent">Event to call if the method retrieves the image successfully.</param>
    private void DeployGrabbedEvent(Camera cam, ref ZEDMat zedmat, VIEW view, ZEDMat.MAT_TYPE mattype, ref Mat cvMat, ImageUpdatedEvent updateevent, 
        OpenCVConversion conversionatend = OpenCVConversion.NONE)
    {
        if(zedmat == null)
        {
            zedmat = new ZEDMat();
            zedmat.Create(new sl.Resolution((uint)zedCam.ImageWidth, (uint)zedCam.ImageHeight), mattype);
        }


        ERROR_CODE err = zedManager.zedCamera.RetrieveImage(zedmat, view, ZEDMat.MEM.MEM_CPU, zedmat.GetResolution());
        
        if (err == ERROR_CODE.SUCCESS)
        {
            Mat buffermat = GetOpenCVBufferMat(zedCam.ImageHeight, zedCam.ImageWidth, SLMatType2CVMatType(mattype));

            //copyToMat(zedmat.GetPtr(), cvMat);
            Utils.copyToMat(zedmat.GetPtr(), buffermat);
            
            ConvertColorSpace(buffermat, ref cvMat, conversionatend); 
            //Mat convertedmat = ConvertColorSpace(buffermat, conversionatend); 

            //updateevent.Invoke(cam, camMat, cvMat);
            updateevent.Invoke(cam, camMat, cvMat);
        }
    }

    /// <summary>
    /// Creates an OpenCV version of a ZEDMat. 
    /// In this sample, we only ever use 8U_C1 (for grayscale image) but you can call it yourself
    /// for any ZEDMat type and get a properly-formatted OpenCV Mat. 
    /// </summary>
    /// <param name="zedmat">Source ZEDMat.</param>
    /// <param name="zedmattype">Type of ZEDMat - data type and channel number. Depends on the type of image
    /// it represents. See summaries of each enum value to choose the type, as you can't currently
    /// retrieve the material type from an instantiated ZEDMat.</param>
    /// <returns>OpenCV Mat formatted correctly so that you can copy data from the source ZEDMat into it.</returns>
    private static Mat SLMat2CVMat(sl.ZEDMat zedmat, ZEDMat.MAT_TYPE zedmattype)
    {
        int cvmattype = SLMatType2CVMatType(zedmattype);
        Mat cvmat = new Mat(zedmat.GetHeight(), zedmat.GetWidth(), cvmattype);

        return cvmat;
    }

    /// <summary>
    /// Returns the OpenCV type that corresponds to a given ZED Mat type. 
    /// </summary>
    private static int SLMatType2CVMatType(ZEDMat.MAT_TYPE zedmattype)
    {
        switch (zedmattype)
        {
            case ZEDMat.MAT_TYPE.MAT_32F_C1:
                return CvType.CV_32FC1;
            case ZEDMat.MAT_TYPE.MAT_32F_C2:
                return CvType.CV_32FC2;
            case ZEDMat.MAT_TYPE.MAT_32F_C3:
                return CvType.CV_32FC3;
            case ZEDMat.MAT_TYPE.MAT_32F_C4:
                return CvType.CV_32FC4;
            case ZEDMat.MAT_TYPE.MAT_8U_C1:
                return CvType.CV_8UC1;
            case ZEDMat.MAT_TYPE.MAT_8U_C2:
                return CvType.CV_8UC2;
            case ZEDMat.MAT_TYPE.MAT_8U_C3:
                return CvType.CV_8UC3;
            case ZEDMat.MAT_TYPE.MAT_8U_C4:
                return CvType.CV_8UC4;
            default:
                return -1;
        }
    }

    /// <summary>
    /// Returns a designated buffer mat for the OpenCV mat type (Defined in the CvType class). 
    /// If none exists, creates one. 
    /// Important: This assumes the height and width of the image stays static, since all ZED images do.
    /// </summary>
    /// <param name="height">Height of the ZED image.</param>
    /// <param name="width">Widht of the ZED image.</param>
    /// <param name="cvtype">Type of the OpenCV mat - see CvType.cs.</param>
    /// <returns>Pre-created buffer material.</returns>
    private Mat GetOpenCVBufferMat(int height, int width, int cvtype)
    {
        if(!openCVBufferMats.ContainsKey(cvtype))
        {
            openCVBufferMats.Add(cvtype, new Mat(height, width, cvtype));
        }
        return openCVBufferMats[cvtype];
    }

    /// <summary>
    /// If we want an output format that differs from the default for ZED's image, here's where we change it. 
    /// For example, the SDK provides the left view image in BGRA format. But we may want BGR (no alpha) or RGB. 
    /// This gets called in DeployGrabbedEvent. 
    private void ConvertColorSpace(Mat source, ref Mat dest, OpenCVConversion converttype)
    {
        switch (converttype)
        {
            case OpenCVConversion.NONE:
            default:
                dest = source;
                break;
            case OpenCVConversion.BGRA2BGR:
                //Mat bgrimage = new Mat(source.rows(), source.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
                if(dest == null) dest = new Mat(source.rows(), source.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
                Imgproc.cvtColor(source, dest, Imgproc.COLOR_BGRA2BGR);
                break;
            case OpenCVConversion.BGRA2RGB:
                if(dest == null) dest = new Mat(source.rows(), source.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
                Imgproc.cvtColor(source, dest, Imgproc.COLOR_BGRA2RGB);
                break;
            case OpenCVConversion.BGRA2RGBA:
                //Mat rgbaimage = new Mat(source.rows(), source.cols(), CvType.CV_8UC4, new Scalar(0, 0, 0));
                if (dest == null) dest = new Mat(source.rows(), source.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
                Imgproc.cvtColor(source, dest, Imgproc.COLOR_BGRA2RGBA);
                break;
        }
    }

    /// <summary>
    /// Holds what kind of color format conversion you would like to perform on an OpenCV image within DeployGrabbedEvent. 
    /// Each item corresponds to a const within OpenCVForUnity.ImgprocModule.Imgproc.cs. 
    /// </summary>
    private enum OpenCVConversion
    {
        NONE,
        BGRA2BGR,
        BGRA2RGBA,
        BGRA2RGB
    }

}

#endif