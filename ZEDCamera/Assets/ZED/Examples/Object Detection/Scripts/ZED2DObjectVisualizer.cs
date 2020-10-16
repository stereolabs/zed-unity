using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For the ZED 2D Object Detection sample. 
/// Listens for new object detections (via the ZEDManager.OnObjectDetection event) and moves + resizes canvas prefabs
/// to represent them. 
/// <para>Works by instantiating a pool of prefabs, and each frame going through the DetectedFrame received from the event
/// to make sure each detected object has a representative GameObject. Also disables GameObjects whose objects are no
/// longer visible and returns them to the pool.</para>
/// </summary>
public class ZED2DObjectVisualizer : MonoBehaviour
{
    /// <summary>
    /// The scene's ZEDManager. 
    /// If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DObjectVisualizer commponents in the scene. 
    /// </summary>
    [Tooltip("The scene's ZEDManager.\r\n" +
    "If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DObjectVisualizer commponents in the scene. ")]
    public ZEDManager zedManager;

    /// <summary>
    /// The scene's canvas. This will be adjusted to have required settings/components so that the bounding boxes
    /// will line up properly with the ZED video feed. 
    /// </summary>
    [Tooltip("The scene's canvas. This will be adjusted to have required settings/components so that the bounding boxes " +
        "will line up properly with the ZED video feed.")]
    public Canvas canvas;

    /// <summary>
    /// If true, the ZED Object Detection manual will be started as soon as the ZED is initiated. 
    /// This avoids having to press the Start Object Detection button in ZEDManager's Inspector. 
    /// </summary>
    [Tooltip("If true, the ZED Object Detection manual will be started as soon as the ZED is initiated. " +
    "This avoids having to press the Start Object Detection button in ZEDManager's Inspector.")]
    public bool startObjectDetectionAutomatically = true;


    /// <summary>
    /// Prefab object that's instantiated to represent detected objects. 
    /// This should ideally be the 2D Bounding Box prefab. But otherwise, it expects the object to have a BBox2DHandler script in the root object, 
    /// and the RectTransform should be bottom-left-aligned (pivot set to 0, 0). 
    /// </summary>
    [Space(5)]
    [Header("Box Appearance")]
    [Tooltip("Prefab object that's instantiated to represent detected objects. " +
    "This class expects the object to have the default Unity cube as a mesh - otherwise, it may be scaled incorrectly.\r\n" +
    "It also expects a BBox3DHandler component in the root object, but you won't have errors if it lacks one. ")]
    public GameObject boundingBoxPrefab;

    /// <summary>
    /// The colors that will be cycled through when assigning colors to new bounding boxes. 
    /// </summary>
    [Tooltip("The colors that will be cycled through when assigning colors to new bounding boxes. ")]
    //[ColorUsage(true, true)] //Uncomment to enable HDR colors in versions of Unity that support it. 
    public List<Color> boxColors = new List<Color>()
    {
        new Color(.231f, .909f, .69f, 1),
        new Color(.098f, .686f, .816f, 1),
        new Color(.412f, .4f, .804f, 1),
        new Color(1, .725f, 0f, 1),
        new Color(.989f, .388f, .419f, 1)
    };

    /// <summary>
    /// When a detected object is first given a box and assigned a color, we store it so that if the object
    /// disappears and appears again later, it's assigned the same color. 
    /// This is also solvable by making the color a function of the ID number itself, but then you can get
    /// repeat colors under certain conditions. 
    /// </summary>
    private Dictionary<int, Color> idColorDict = new Dictionary<int, Color>();

    /// <summary>
    /// If true, draws a 2D mask over where the SDK believes the detected object is. 
    /// </summary>
    //[Space(5)]
    //[Header("Mask")]
    // bool showObjectMask = false;
    /// <summary>
    /// Used to warn the user only once if they enable the mask but the mask was not enabled when object detection was initialized. See OnValidate. 
    /// </summary>
    private bool lastShowObjectMaskValue; 

    /// <summary>
    /// Display bounding boxes of objects that are actively being tracked by object tracking, where valid positions are known. 
    /// </summary>
    [Space(5)]
    [Header("Filters")]
    [Tooltip("Display bounding boxes of objects that are actively being tracked by object tracking, where valid positions are known. ")]
    public bool showONTracked = true;
    /// <summary>
    /// Display bounding boxes of objects that were actively being tracked by object tracking, but that were lost very recently.
    /// </summary>
    [Tooltip("Display bounding boxes of objects that were actively being tracked by object tracking, but that were lost very recently.")]
    public bool showSEARCHINGTracked = false;
    /// <summary>
    /// Display bounding boxes of objects that are visible but not actively being tracked by object tracking (usually because object tracking is disabled in ZEDManager).
    /// </summary>
    [Tooltip("Display bounding boxes of objects that are visible but not actively being tracked by object tracking (usually because object tracking is disabled in ZEDManager).")]
    public bool showOFFTracked = false;

    /// <summary>
    /// Used to know which of the available colors will be assigned to the next bounding box to be used. 
    /// </summary>
    private int nextColorIndex = 0;

    /// <summary>
    /// Pre-instantiated bbox prefabs currently not in use. 
    /// </summary>
    private Stack<GameObject> bboxPool = new Stack<GameObject>();

    /// <summary>
    /// All active RectTransforms within GameObjects that were instantiated to the prefab and that currently represent a detected object. 
    /// Key is the object's objectID. 
    /// </summary>
    private Dictionary<int, RectTransform> liveBBoxes = new Dictionary<int, RectTransform>();

    /// <summary>
    /// List of all 2D masks created in a frame. Used so that they can all be disposed of in the frame afterward. 
    /// </summary>
    private List<Texture2D> lastFrameMasks = new List<Texture2D>();

    private void Start()
    {
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

        zedManager.OnObjectDetection += Visualize2DBoundingBoxes;
        zedManager.OnZEDReady += OnZEDReady;

        if (!canvas) //If we don't have a canvas in the scene, we need one. 
        {
            GameObject canvasgo = new GameObject("Canvas - " + zedManager.name);
            canvas = canvasgo.AddComponent<Canvas>();
        }

        //lastShowObjectMaskValue = showObjectMask;
    }

    private void OnZEDReady()
    {
        if (startObjectDetectionAutomatically && !zedManager.IsObjectDetectionRunning)
        {
            zedManager.StartObjectDetection();
        }

        //Enforce some specific settings on the canvas that are needed for things to line up. 
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = zedManager.GetLeftCamera();
        //Canvas needs to have its plane distance set within the camera's view frustum. 
        canvas.planeDistance = 1;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (!scaler)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(zedManager.zedCamera.ImageWidth, zedManager.zedCamera.ImageHeight);
    }

    //TEST
    private void Update()
    {
        //zedManager.GetLeftCamera().ResetProjectionMatrix();
    }

    /// <summary>
    /// Given a frame of object detections, positions a canvas object to represent every visible object
    /// to encompass the object within the 2D image from the ZED. 
    /// <para>Called from ZEDManager.OnObjectDetection each time there's a new detection frame available.</para> 
    /// </summary>
    public void Visualize2DBoundingBoxes(DetectionFrame dframe)
    {
        //Clear any masks that were displayed last frame, to avoid memory leaks. 
        DestroyLastFrameMaskTextures();

        //Debug.Log("Received frame with " + dframe.detectedObjects.Count + " objects.");
        //Get a list of all active IDs from last frame, and we'll remove each box that's visible this frame. 
        //At the end, we'll clear the remaining boxes, as those are objects no longer visible to the ZED. 
        List<int> activeids = liveBBoxes.Keys.ToList();

        List<DetectedObject> newobjects = dframe.GetFilteredObjectList(showONTracked, showSEARCHINGTracked, showOFFTracked);

        //Test just setting box to first available. 
        foreach (DetectedObject dobj in newobjects)
        {
            //Remove the ID from the list we'll use to clear no-longer-visible boxes. 
            if (activeids.Contains(dobj.id)) activeids.Remove(dobj.id);

            //Get the relevant box. This function will create a new one if it wasn't designated yet. 
            RectTransform bbox = GetBBoxForObject(dobj);


            BBox2DHandler idtext = bbox.GetComponentInChildren<BBox2DHandler>();
            if (idtext)
            {
                float disttobox = Vector3.Distance(dobj.detectingZEDManager.GetLeftCameraTransform().position, dobj.Get3DWorldPosition());
                idtext.SetDistance(disttobox);
            }

#if UNITY_2018_3_OR_NEWER
            float xmod = canvas.GetComponent<RectTransform>().rect.width / zedManager.zedCamera.ImageWidth;
            Rect objrect = dobj.Get2DBoundingBoxRect(xmod);
#else
            Rect objrect = dobj.Get2DBoundingBoxRect();

#endif
            //Adjust the size of the RectTransform to encompass the object. 
            bbox.sizeDelta = new Vector2(objrect.width, objrect.height);
            bbox.anchoredPosition = new Vector2(objrect.x, objrect.y);

            /*
#if UNITY_2018_3_OR_NEWER
            float xmod = canvas.GetComponent<RectTransform>().rect.width / zedManager.zedCamera.ImageWidth;
            bbox.anchoredPosition = new Vector2(bbox.anchoredPosition.x * xmod, bbox.anchoredPosition.y);
            bbox.sizeDelta *= xmod;
#endif
*/


            //Apply the mask. 
            /*if (showObjectMask)
            {
                //Make a new image for this new mask. 
                Texture2D maskimage;
                if (dobj.GetMaskTexture(out maskimage, false))
                {
                    idtext.SetMaskImage(maskimage); //Apply to 2D bbox. 
                    lastFrameMasks.Add(maskimage);   //Cache the texture so it's deleted next time we update our objects. 
                }   
            }*/
        }

        //Remove boxes for objects that the ZED can no longer see. 
        foreach (int id in activeids)
        {
            ReturnBoxToPool(id, liveBBoxes[id]);
        }

        SortActiveObjectsByDepth(); //Sort all object transforms so that ones with further depth appear behind objects that are closer.
    }

    /// <summary>
    /// Returs the RectTransform within the GameObject (instantiated from boundingBoxPrefab) that represents the provided DetectedObject.
    /// If none exists, it retrieves one from the pool (or instantiates a new one if none is available) and 
    /// sets it up with the proper ID and colors. 
    /// </summary>
    private RectTransform GetBBoxForObject(DetectedObject dobj)
    {
        if (!liveBBoxes.ContainsKey(dobj.id))
        {
            GameObject newbox = GetAvailableBBox();
            newbox.transform.SetParent(canvas.transform, false);
            newbox.name = "Object #" + dobj.id;

            Color col;
            if (idColorDict.ContainsKey(dobj.id))
            {
                col = idColorDict[dobj.id];
            }
            else
            {
                col = GetNextColor();
                idColorDict.Add(dobj.id, col);
            }

            BBox2DHandler boxhandler = newbox.GetComponent<BBox2DHandler>();
            if (boxhandler)
            {
                boxhandler.SetColor(col);
                boxhandler.SetID(dobj.id);
            }


            RectTransform newrecttrans = newbox.GetComponent<RectTransform>();
            if (!newrecttrans)
            {
                Debug.LogError("BBox prefab needs a RectTransform in the root object.");
                return null;
            }

            liveBBoxes[dobj.id] = newrecttrans;
            return newrecttrans;
        }
        else return liveBBoxes[dobj.id];

    }

    /// <summary>
    /// Gets an available GameObject (instantiated from boundingBoxPrefab) from the pool, 
    /// or instantiates a new one if none are available. 
    /// </summary>
    /// <returns></returns>
    private GameObject GetAvailableBBox()
    {
        if (bboxPool.Count == 0)
        {
            GameObject newbbox = Instantiate(boundingBoxPrefab);
            newbbox.transform.SetParent(transform, false);
            bboxPool.Push(newbbox);
        }

        GameObject bbox = bboxPool.Pop();
        bbox.SetActive(true);

        return bbox;
    }

    /// <summary>
    /// Disables a RectTransform's GameObject that was being used to represent an object (of the given id) and 
    /// puts it back into the pool for later use. 
    /// </summary>
    private void ReturnBoxToPool(int id, RectTransform bbox)
    {
        bbox.gameObject.SetActive(false);
        bbox.name = "Unused";

        bboxPool.Push(bbox.gameObject);

        if (liveBBoxes.ContainsKey(id))
        {
            liveBBoxes.Remove(id);
        }
        else
        {
            Debug.LogError("Tried to remove object ID " + id + " from active bboxes, but it wasn't in the dictionary.");
        }
    }

    /// <summary>
    /// Returns a color from the boxColors list. 
    /// Colors are returned sequentially in order of their appearance in that list. 
    /// </summary>
    /// <returns></returns>
    private Color GetNextColor()
    {
        if (boxColors.Count == 0)
        {
            return new Color(.043f, .808f, .435f, 1);
        }

        if (nextColorIndex >= boxColors.Count)
        {
            nextColorIndex = 0;
        }

        Color returncol = boxColors[nextColorIndex];

        nextColorIndex++;


        return returncol;
    }

    /// <summary>
    /// Sorts all objects in the canvas based on their distance from the camera, so that closer objects overlap further objects. 
    /// </summary>
    private void SortActiveObjectsByDepth()
    {
        List<BBox2DHandler> handlers = new List<BBox2DHandler>();
        foreach (Transform child in canvas.transform)
        {
            BBox2DHandler handler = child.GetComponent<BBox2DHandler>();
            if (handler) handlers.Add(handler);
        }

        handlers.Sort((x, y) => y.currentDistance.CompareTo(x.currentDistance));

        for (int i = 0; i < handlers.Count; i++)
        {
            handlers[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// Destroys all textures added to the lastFrameMasks the last time Object Detection was called. 
    /// Called when we're done using them (before updating with new data) to avoid memory leaks. 
    /// </summary>
    private void DestroyLastFrameMaskTextures()
    {
        if (lastFrameMasks.Count > 0)
        {
            for (int i = 0; i < lastFrameMasks.Count; i++)
            {
                Destroy(lastFrameMasks[i]);
            }
            lastFrameMasks.Clear();
        }
    }

    /*private void OnValidate()
    {
        //If the user changes the showObjectMask setting to true, warn them if its ZEDManager has objectDetection2DMask set to false, because masks won't show up. 
        if (Application.isPlaying && showObjectMask != lastShowObjectMaskValue)
        {
            lastShowObjectMaskValue = showObjectMask;
            if (!zedManager) zedManager = ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_01);
            if(showObjectMask == true && zedManager != null && zedManager.objectDetection2DMask == false)
            {
                Debug.LogError("ZED2DObjectVisualizer has showObjectMask enabled, but its ZEDManager has objectDetection2DMask disabled. " +
                "objectDetection2DMask must be enabled when Object Detection is started or masks will not be visible.");
            }
        }
    }*/

    private void OnDestroy()
    {
        if (zedManager)
        {
            zedManager.OnObjectDetection -= Visualize2DBoundingBoxes;
            zedManager.OnZEDReady -= OnZEDReady;
        }

        DestroyLastFrameMaskTextures();
    }

}
