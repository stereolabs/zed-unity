using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// For the ZED 3D Object Detection sample. 
/// Listens for new object detections (via the ZEDManager.OnObjectDetection event) and moves + resizes cube prefabs
/// to represent them. 
/// <para>Works by instantiating a pool of prefabs, and each frame going through the DetectedFrame received from the event
/// to make sure each detected object has a representative GameObject. Also disables GameObjects whose objects are no
/// longer visible and returns them to the pool.</para>
/// </summary>
public class ZED3DObjectVisualizer : MonoBehaviour
{
    /// <summary>
    /// The scene's ZEDManager. 
    /// If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DObjectVisualizer commponents in the scene. 
    /// </summary>
    [Tooltip("The scene's ZEDManager.\r\n" +
        "If you want to visualize detections from multiple ZEDs at once you will need multiple ZED3DObjectVisualizer commponents in the scene. ")]
    public ZEDManager zedManager;

    /// <summary>
    /// If true, the ZED Object Detection manual will be started as soon as the ZED is initiated. 
    /// This avoids having to press the Start Object Detection button in ZEDManager's Inspector. 
    /// </summary>
    [Tooltip("If true, the ZED Object Detection manual will be started as soon as the ZED is initiated. " +
        "This avoids having to press the Start Object Detection button in ZEDManager's Inspector.")]
    public bool startObjectDetectionAutomatically = true;

    /// <summary>
    /// Prefab object that's instantiated to represent detected objects. 
    /// This class expects the object to have the default Unity cube as a mesh - otherwise, it may be scaled incorrectly. 
    /// It also expects a BBox3DHandler component in the root object, but you won't have errors if it lacks one. 
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
    /// If true, bounding boxes are rotated to face the camera that detected them. This has more parity with the SDK and will generally result in more accurate boxes. 
    /// If false, the boxes are calculated from known bounds to face Z = 1. 
    /// </summary>
    [Space(5)]
    [Header("Box Transform")]
    [Tooltip("If true, bounding boxes are rotated to face the camera that detected them. If false, the boxes are calculated from known bounds to face Z = 1. " +
        "'False' has more parity with the SDK and will generally result in more accurate boxes.")]
    public bool boxesFaceCamera = false;
    /// <summary>
    /// If true, transforms the localScale of the root bounding box transform to match the detected 3D bounding box. 
    /// </summary>
    [Tooltip("If true, transforms the localScale of the root bounding box transform to match the detected 3D bounding box. ")]
    public bool transformBoxScale = true;
    /// <summary>
    /// If true, and transformBoxScale is also true, modifies the center and Y bounds of each detected bounding box so that its
    /// bottom is at floor level (Y = 0) while keeping the other corners at the same place. 
    /// </summary>
    [Tooltip("If true, and transformBoxScale is also true, modifies the center and Y bounds of each detected bounding box so that its " +
        "bottom is at floor level (Y = 0) while keeping the other corners at the same place. ")]
    public bool transformBoxToTouchFloor = true;
    /// <summary>
    /// If true, sets the Y value of the center of the bounding box to 0. Use for bounding box prefabs meant to be centered at the user's feet. 
    /// </summary>
    [Tooltip("If true, sets the Y value of the center of the bounding box to 0. Use for bounding box prefabs meant to be centered at the user's feet. ")]
    [LabelOverride("Box Center Always On Floor")]
    public bool floorBBoxPosition = false;

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
    /// How wide a bounding box has to be in order to be displayed. Use this to remove tiny bounding boxes from partially-occluded objects. 
    /// (If you have this issue, it can also be helpful to set showSEARCHINGTracked to OFF.)
    /// </summary>
    [Tooltip("How wide a bounding box has to be in order to be displayed. Use this to remove tiny bounding boxes from partially-occluded objects.\r\n" +
        "(If you have this issue, it can also be helpful to set showSEARCHINGTracked to OFF.)")]
    public float minimumWidthToDisplay = 0.3f;


    /// <summary>
    /// When a detected object is first given a box and assigned a color, we store it so that if the object
    /// disappears and appears again later, it's assigned the same color. 
    /// This is also solvable by making the color a function of the ID number itself, but then you can get
    /// repeat colors under certain conditions. 
    /// </summary>
    private Dictionary<int, Color> idColorDict = new Dictionary<int, Color>();

    /// <summary>
    /// Pre-instantiated bbox prefabs currently not in use. 
    /// </summary>
    private Stack<GameObject> bboxPool = new Stack<GameObject>();

    /// <summary>
    /// All active GameObjects that were instantiated to the prefab and that currently represent a detected object. 
    /// Key is the object's objectID. 
    /// </summary>
    private Dictionary<int, GameObject> liveBBoxes = new Dictionary<int, GameObject>();

    /// <summary>
    /// Used to know which of the available colors will be assigned to the next bounding box to be used. 
    /// </summary>
    private int nextColorIndex = 0;

    // Use this for initialization
    void Start()
    {
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

        zedManager.OnObjectDetection += Visualize3DBoundingBoxes;
        zedManager.OnZEDReady += OnZEDReady;


    }

    private void OnZEDReady()
    {
        if (startObjectDetectionAutomatically && !zedManager.IsObjectDetectionRunning)
        {
            zedManager.StartObjectDetection();
        }
    }


    /// <summary>
    /// Given a frame of object detections, positions a GameObject to represent every visible object
    /// in that object's actual 3D location within the world. 
    /// <para>Called from ZEDManager.OnObjectDetection each time there's a new detection frame available.</para> 
    /// </summary>
    private void Visualize3DBoundingBoxes(DetectionFrame dframe)
    {
        //Get a list of all active IDs from last frame, and we'll remove each box that's visible this frame. 
        //At the end, we'll clear the remaining boxes, as those are objects no longer visible to the ZED. 
        List<int> activeids = liveBBoxes.Keys.ToList();

        List<DetectedObject> newobjects = dframe.GetFilteredObjectList(showONTracked, showSEARCHINGTracked, showOFFTracked);

        foreach (DetectedObject dobj in newobjects)
        {
            Bounds objbounds = dobj.Get3DWorldBounds();

            //Make sure the object is big enough to count. We filter out very small boxes. 
            if (objbounds.size.x < minimumWidthToDisplay) continue;

            //Remove the ID from the list we'll use to clear no-longer-visible boxes. 
            if (activeids.Contains(dobj.id)) activeids.Remove(dobj.id);

            //Get the box and update its distance value. 
            GameObject bbox = GetBBoxForObject(dobj);

            //Move the box into position. 
            bbox.transform.position = dobj.Get3DWorldPosition();
            if (floorBBoxPosition)
            {
                bbox.transform.position = new Vector3(bbox.transform.position.x, 0, bbox.transform.position.z);
            }

            bbox.transform.rotation = dobj.Get3DWorldRotation(boxesFaceCamera); //Rotate them. 


            //Transform the box if desired. 
            if (transformBoxScale)
            {
                //We'll scale the object assuming that it's mesh is the default Unity cube, or something sized equally. 
                if (transformBoxToTouchFloor)
                {
                    Vector3 startscale = objbounds.size;
                    float distfromfloor = bbox.transform.position.y - (objbounds.size.y / 2f);
                    bbox.transform.localScale = new Vector3(objbounds.size.x, objbounds.size.y + distfromfloor, objbounds.size.z);

                    Vector3 newpos = bbox.transform.position;
                    newpos.y -= (distfromfloor / 2f);

                    bbox.transform.position = newpos;

                }
                else
                {
                    bbox.transform.localScale = objbounds.size;
                }
            }

            //Now that we've adjusted position, tell the handler on the prefab to adjust distance display.. 
            BBox3DHandler boxhandler = bbox.GetComponent<BBox3DHandler>();
            if (boxhandler)
            {
                float disttobox = Vector3.Distance(dobj.detectingZEDManager.GetLeftCameraTransform().position, dobj.Get3DWorldPosition());
                boxhandler.SetDistance(disttobox);

                boxhandler.UpdateBoxUVScales();
                boxhandler.UpdateLabelScaleAndPosition();
            }

            //DrawDebugBox(dobj);
        }

        //Remove boxes for objects that the ZED can no longer see. 
        foreach (int id in activeids)
        {
            ReturnBoxToPool(id, liveBBoxes[id]);
        }

    }

    /// <summary>
    /// Returs the GameObject (instantiated from boundingBoxPrefab) that represents the provided DetectedObject.
    /// If none exists, it retrieves one from the pool (or instantiates a new one if none is available) and 
    /// sets it up with the proper ID and colors. 
    /// </summary>
    private GameObject GetBBoxForObject(DetectedObject dobj)
    {
        if (!liveBBoxes.ContainsKey(dobj.id))
        {
            GameObject newbox = GetAvailableBBox();
            newbox.name = "Object #" + dobj.id;

            BBox3DHandler boxhandler = newbox.GetComponent<BBox3DHandler>();

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

            if (boxhandler)
            {
                boxhandler.SetColor(col);
                boxhandler.SetID(dobj.id);
            }

            liveBBoxes[dobj.id] = newbox;
            return newbox;
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
    /// Disables a GameObject that was being used to represent an object (of the given id) and puts it back
    /// into the pool for later use. 
    /// </summary>
    private void ReturnBoxToPool(int id, GameObject bbox)
    {
        bbox.SetActive(false);
        bbox.name = "Unused";

        bboxPool.Push(bbox);

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

    private void OnDestroy()
    {
        if (zedManager)
        {
            zedManager.OnObjectDetection -= Visualize3DBoundingBoxes;
            zedManager.OnZEDReady -= OnZEDReady;
        }
    }

    /// <summary>
    /// Draws a bounding box in the Scene window. Useful for debugging a 3D bbox's position relative to it. 
    /// </summary>
    private void DrawDebugBox(DetectedObject dobj)
    {
        //Test bbox orientation.
        Transform camtrans = dobj.detectingZEDManager.GetLeftCameraTransform();
        Vector3[] corners = dobj.rawObjectData.worldBoundingBox;
        Vector3[] rotcorners = new Vector3[8];
        //Vector3[] corners3d = new Vector3[8];
        Vector3[] corners3d = dobj.Get3DWorldCorners();
        for (int i = 0; i < 8; i++)
        {
            Vector3 fixrot = camtrans.InverseTransformPoint(corners[i]);
            rotcorners[i] = fixrot;
        }


        Debug.DrawLine(corners3d[0], corners3d[1], Color.red);
        Debug.DrawLine(corners3d[1], corners3d[2], Color.red);
        Debug.DrawLine(corners3d[2], corners3d[3], Color.red);
        Debug.DrawLine(corners3d[3], corners3d[0], Color.red);

        Debug.DrawLine(corners3d[4], corners3d[5], Color.red);
        Debug.DrawLine(corners3d[5], corners3d[6], Color.red);
        Debug.DrawLine(corners3d[6], corners3d[7], Color.red);
        Debug.DrawLine(corners3d[7], corners3d[4], Color.red);

        Debug.DrawLine(corners3d[0], corners3d[4], Color.red);
        Debug.DrawLine(corners3d[1], corners3d[5], Color.red);
        Debug.DrawLine(corners3d[2], corners3d[6], Color.red);
        Debug.DrawLine(corners3d[3], corners3d[7], Color.red);
    }
}
