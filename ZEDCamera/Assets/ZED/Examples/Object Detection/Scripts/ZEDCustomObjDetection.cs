#if ZED_OPENCV_FOR_UNITY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;

using sl;
using System.Linq;

/// <summary>
/// Example that shows how to use the custom object detection module from ZED SDK.
/// Uses Yolov4 from Opencv. Therefore requires the OpenCVForUnity package.
/// </summary>
public class ZEDCustomObjDetection : MonoBehaviour
{

    [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
    public string model;

    [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
    public string config;

    [TooltipAttribute("Path to a text file with names of classes to label detected objects.")]
    public string classes;

    [TooltipAttribute("Optional list of classes filters. Add classes you want to keep displayed.")]
    public List<string> classesFilter;

    [TooltipAttribute("Confidence threshold.")]
    public float confThreshold = 0.5f;

    [TooltipAttribute("Non-maximum suppression threshold.")]
    public float nmsThreshold = 0.4f;

    private List<string> classNames;
    private List<string> outBlobNames;
    private List<string> outBlobTypes;

    private Net net;
    
    public int inferenceWidth = 416;
    
    public int inferenceHeight = 416;
    
    public float scale = 1.0f;
    
    public Scalar mean = new Scalar(0, 0, 0, 0);


    private Mat bgrMat;

    public ZEDManager zedManager;
   
    /// <summary>
    /// Scene's ZEDToOpenCVRetriever, which creates OpenCV mats and deploys events each time the ZED grabs an image. 
    /// It's how we get the image and required matrices that we use to look for markers. 
    /// </summary>
    public ZEDToOpenCVRetriever imageRetriever;

    public delegate void onNewIngestCustomODDelegate();

    public event onNewIngestCustomODDelegate OnIngestCustomOD;

    public void Start()
    {
        if (!zedManager) zedManager = FindObjectOfType<ZEDManager>();

        if (zedManager.objectDetectionModel != DETECTION_MODEL.CUSTOM_BOX_OBJECTS)
        {
            Debug.LogWarning("sl.DETECTION_MODEL.CUSTOM_BOX_OBJECTS is mandatory for this sample");
        }
        else
        {
            //We'll listen for updates from a ZEDToOpenCVRetriever, which will call an event whenever it has a new image from the ZED. 
            if (!imageRetriever) imageRetriever = ZEDToOpenCVRetriever.GetInstance();
            imageRetriever.OnImageUpdated_LeftRGBA += Run;
        }


        Init();
    }

    public void OnDestroy()
    {
        imageRetriever.OnImageUpdated_LeftRGBA -= Run;

        if (net != null)
            net.Dispose();

        if (bgrMat != null)
            bgrMat.Dispose();
    }

    public void OnValidate()
    {
        if (classesFilter.Count > 0)
        {
            classNames = classesFilter;
        }
        else
            classNames = readClassNames(classes);
    }

    public void Init()
    {
        if (!string.IsNullOrEmpty(classes))
        {
            classNames = readClassNames(classes);
            if (classNames == null)
            {
                Debug.LogError("Classes file is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
        }
        else if (classesFilter.Count > 0)
        {
            classNames = classesFilter;
        }

        if (string.IsNullOrEmpty(model))
        {
            Debug.LogError("Model file is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
        }
        else if (string.IsNullOrEmpty(config))
        {
            Debug.LogError("Config file is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
        }
        else
        {
            net = Dnn.readNet(model, config);
            if (net == null) Debug.LogWarning("network is null");

            outBlobNames = getOutputsNames(net);

            outBlobTypes = getOutputsTypes(net);
        }

    }

    public void Run(Camera cam, Mat camera_matrix, Mat rgbaMat)
    {

        if (!zedManager.IsObjectDetectionRunning) return;
        
        Mat bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

        Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);
        
        // Create a 4D blob from a frame.
        Size infSize = new Size(inferenceWidth > 0 ? inferenceWidth : bgrMat.cols(),
                           inferenceHeight > 0 ? inferenceHeight : bgrMat.rows());
        Mat blob = Dnn.blobFromImage(bgrMat, scale, infSize, mean, true, false);

        // Run a model.
        net.setInput(blob);
        
        if (net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
        {  // Faster-RCNN or R-FCN
            Imgproc.resize(bgrMat, bgrMat, infSize);
            Mat imInfo = new Mat(1, 3, CvType.CV_32FC1);
            imInfo.put(0, 0, new float[] {
                            (float)infSize.height,
                            (float)infSize.width,
                            1.6f
                        });
            net.setInput(imInfo, "im_info");
        }

        List<Mat> outs = new List<Mat>();
        net.forward(outs, outBlobNames);

        postprocess(rgbaMat, outs, net, Dnn.DNN_BACKEND_OPENCV);

        for (int i = 0; i < outs.Count; i++)
        {
            outs[i].Dispose();
        }
        blob.Dispose();
    }

    /// <summary>
    /// Postprocess the specified frame, outs and net.
    /// </summary>
    /// <param name="frame">Frame.</param>
    /// <param name="outs">Outs.</param>
    /// <param name="net">Net.</param>
    /// <param name="backend">Backend.</param>
    protected virtual void postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
    {
        MatOfInt outLayers = net.getUnconnectedOutLayers();
        string outLayerType = outBlobTypes[0];

        List<int> classIdsList = new List<int>();
        List<float> confidencesList = new List<float>();
        List<Rect2d> boxesList = new List<Rect2d>();

        for (int i = 0; i < outs.Count; ++i)
        {
            // Network produces output blob with a shape NxC where N is a number of
            // detected objects and C is a number of classes + 4 where the first 4
            // numbers are [center_x, center_y, width, height]

            //Debug.Log ("outs[i].ToString() "+outs[i].ToString());

            float[] positionData = new float[5];
            float[] confidenceData = new float[outs[i].cols() - 5];
            for (int p = 0; p < outs[i].rows(); p++)
            {
                outs[i].get(p, 0, positionData);
                outs[i].get(p, 5, confidenceData);

                int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                float confidence = confidenceData[maxIdx];
                if (confidence > confThreshold)
                {
                    float centerX = positionData[0] * frame.cols();
                    float centerY = positionData[1] * frame.rows();
                    float width = positionData[2] * frame.cols();
                    float height = positionData[3] * frame.rows();
                    float left = centerX - width / 2;
                    float top = centerY - height / 2;

                    classIdsList.Add(maxIdx);
                    confidencesList.Add((float)confidence);
                    boxesList.Add(new Rect2d(left, top, width, height));
                }
            }
        }

        Dictionary<int, List<int>> class2indices = new Dictionary<int, List<int>>();
        for (int i = 0; i < classIdsList.Count; i++)
        {
            if (confidencesList[i] >= confThreshold)
            {
                if (!class2indices.ContainsKey(classIdsList[i]))
                    class2indices.Add(classIdsList[i], new List<int>());

                class2indices[classIdsList[i]].Add(i);
            }
        }

        List<Rect2d> nmsBoxesList = new List<Rect2d>();
        List<float> nmsConfidencesList = new List<float>();
        List<int> nmsClassIdsList = new List<int>();
        foreach (int key in class2indices.Keys)
        {
            List<Rect2d> localBoxesList = new List<Rect2d>();
            List<float> localConfidencesList = new List<float>();
            List<int> classIndicesList = class2indices[key];
            for (int i = 0; i < classIndicesList.Count; i++)
            {
                localBoxesList.Add(boxesList[classIndicesList[i]]);
                localConfidencesList.Add(confidencesList[classIndicesList[i]]);
            }

            using (MatOfRect2d localBoxes = new MatOfRect2d(localBoxesList.ToArray()))
            using (MatOfFloat localConfidences = new MatOfFloat(localConfidencesList.ToArray()))
            using (MatOfInt nmsIndices = new MatOfInt())
            {
                Dnn.NMSBoxes(localBoxes, localConfidences, confThreshold, nmsThreshold, nmsIndices);
                for (int i = 0; i < nmsIndices.total(); i++)
                {
                    int idx = (int)nmsIndices.get(i, 0)[0];
                    nmsBoxesList.Add(localBoxesList[idx]);
                    nmsConfidencesList.Add(localConfidencesList[idx]);
                    nmsClassIdsList.Add(key);
                }
            }
        }

        boxesList = nmsBoxesList;
        classIdsList = nmsClassIdsList;
        confidencesList = nmsConfidencesList;
        
        ingestCustomData(boxesList, confidencesList, classIdsList);
    }

    private void ingestCustomData(List<Rect2d> boxesList, List<float> confidencesList, List<int> classIdsList)
    {
        List<CustomBoxObjectData> objects_in = new List<CustomBoxObjectData>();
        for (int idx = 0; idx < boxesList.Count; ++idx)
        {
            if (classNames != null && classNames.Count != 0)
            {
                if (classesFilter.Count == 0 || (classIdsList[idx] < (int)classNames.Count && (classesFilter.Contains(classNames[classIdsList[idx]]))))
                { 
                    CustomBoxObjectData tmp = new CustomBoxObjectData();
                    tmp.uniqueObjectID = sl.ZEDCamera.GenerateUniqueID();
                    tmp.label = classIdsList[idx];
                    tmp.probability = confidencesList[idx];

                    Vector2[] bbox = new Vector2[4];
                    bbox[0] = new Vector2((float)boxesList[idx].x, (float)boxesList[idx].y);
                    bbox[1] = new Vector2((float)boxesList[idx].x + (float)boxesList[idx].width, (float)boxesList[idx].y);
                    bbox[2] = new Vector2((float)boxesList[idx].x + (float)boxesList[idx].width, (float)boxesList[idx].y + (float)boxesList[idx].height);
                    bbox[3] = new Vector2((float)boxesList[idx].x, (float)boxesList[idx].y + (float)boxesList[idx].height);

                    tmp.boundingBox2D = bbox;

                    objects_in.Add(tmp);
                }
            }
        }

        zedManager.zedCamera.IngestCustomBoxObjects(objects_in);
        if (OnIngestCustomOD != null)
            OnIngestCustomOD();
    }

    /// <summary>
    /// Reads the class names.
    /// </summary>
    /// <returns>The class names.</returns>
    /// <param name="filename">Filename.</param>
    private List<string> readClassNames(string filename)
    {
        List<string> classNames = new List<string>();

        System.IO.StreamReader cReader = null;
        try
        {
            cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

            while (cReader.Peek() >= 0)
            {
                string name = cReader.ReadLine();
                classNames.Add(name);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
        finally
        {
            if (cReader != null)
                cReader.Close();
        }

        return classNames;
    }

    /// <summary>
    /// Gets the outputs names.
    /// </summary>
    /// <returns>The outputs names.</returns>
    /// <param name="net">Net.</param>
    protected List<string> getOutputsNames(Net net)
    {
        List<string> names = new List<string>();

        MatOfInt outLayers = net.getUnconnectedOutLayers();
        for (int i = 0; i < outLayers.total(); ++i)
        {
            names.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_name());
        }
        outLayers.Dispose();

        return names;
    }

    /// <summary>
    /// Gets the outputs types.
    /// </summary>
    /// <returns>The outputs types.</returns>
    /// <param name="net">Net.</param>
    protected virtual List<string> getOutputsTypes(Net net)
    {
        List<string> types = new List<string>();

        MatOfInt outLayers = net.getUnconnectedOutLayers();
        for (int i = 0; i < outLayers.total(); ++i)
        {
            types.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_type());
        }
        outLayers.Dispose();

        return types;
    }
}
#endif