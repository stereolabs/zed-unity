# ZED OBJECT DETECTION

These scripts and samples demonstrate how to use the ZED SDK's Object Detection module in Unity. Note that Object Detection requires a ZED2, *not* a ZED or ZED Mini.


## What's Object Detection? 

The ZED SDK Object Detection module uses a highly-optimized AI model to recognize specific objects (currently people and vehicles) within the video feed. Using depth, it goes a step further than similar algorithms to calculate the object’s 3D position in the world, not just within the 2D image.

When Object Detection is running, the ZED will regularly output a Frame object, received in Unity as an ObjectsFrameSDK object. In it is some metadata and a list of individual object detections, contained
in an array of ObjectDataSDK. 

Each detection holds the following info: 

- ID - unique ID assigned to each object for tracking them between frames.
- Category - If the object is a person or vehicle.
- Tracking State - If the position of the object is understood relative to the floor/world.
- Confidence - How sure the SDK is that this object exists, on a scale from 0 to 99.
- 2D Bounding Box - 2D coordinates of the four corners on the ZED image that define where in the image the object is.
- Mask - 2D grayscale image that defines exactly which pixels within the 2D bounding box are over the object.
- 3D Position - Where the camera is in 3D space.
- 3D Velocity - Direction and speed that the object is moving. 
- 3D Bounding Box - 3D coordinates of the eight corners that make up a box that encapsulates the object. 


## Setting Up Object Detection: 

Hardware-wise, you'll want your ZED2 pointed so that a significant part of the floor is visible. This is because the ZED SDK will detect the floor when it starts, and uses it to better understand the position and movement of objects in 3D. 

The fastest way to try the Object Detection module is to load one of the two sample scenes and run it. Then point your ZED2 at yourself or some other people. The 2D Object Detection scene will draw the 2D bounding box and mask over detected objects. The 3D scene will draw the 3D bounding box around them. 

To make your own scene without doing any scripting, do the following: 

- Put the ZED_Rig_Mono prefab in the scene
- Make sure ZEDManager has Enable Tracking and Estimate Initial Position enabled in the Tracking options.
- *(Optional)* Set the Depth Mode to ULTRA and use at least 1080p resolution to ensure the highest quality detections. 
- Make an object with either a ZED2DObjectVisualizer or ZED3DObjectVisualized component (or both) attached, and fill their boundingBoxPrefab values with the appropriate prefab in ZED->Examples->Object Detection -> Prefabs. 


## Configuration

In ZEDManager's Inspector, there is a section dedicated to Object Detection. In it are two categories of settings: Initialization and Runtime. Initialization settings must be set before Object Detection is started. Runtime settings can be adjusted whenever you want, and will be applied instantly. 

### Initialization Settings: 

- **Image Sync:** If enabled, the ZED SDK will update object detection data at the same rate as images are published. This costs performance and is not usually necessary, especially for realtime applications. However, it can ensure that there is no latency between the video feed and object detection. 
- **Object Tracking:** If enabled, the ZED SDK will track objects between frames, providing more accurate data and giving access to more information, such as velocity. 
- **Enable 2D Mask:** Enabling this allows scripts to access a 2D image that shows exactly which pixels in an object’s 2D bounding box belong to the object. Only enable if you want to view this mask, and are using ZED2DObjectVisualizer with its showObjectMask value set to true. 

### Runtime Settings: 

- **Detection Threshold**: Sets the minimum confidence value for a detected object to be published. Ex: If set to 40, the ZED SDK needs to be at least 40% confident that a detected object exists.
- **Person/Vehicle Filters:** Whether you want detected objects to include people, vehicles, or both. 

Note the "Start Object Detection" button at the end. Object Detection requires a lot of performance, so there is not an option in ZEDManager to start it automatically. To start it, you must either press that button yourself, or have a script start it. Both the ZED2DObjectVisualizer and ZED3DObjectVisualizer scripts will do this for you, provided this option is checked in their own Inspectors. 


## Accessing Object Detection Data

If you're writing your own scripts to make use of Object Detection data, there are several ways of getting to it. 

First, it's worth noting that the data *directly* from the SDK was not designed for use within Unity. This data is stored in the ObjectsFrameSDK and ObjectDataSDK structs. Many values in these structs require additional transforms to be useful in Unity. For example, many 3D coordinates are not in world space, and 2D coordinates have their Y values inverted from Unity convention. Being structs, they also lack helper functions. However, as these structs are very close to how they are presented in the C++ ZED SDK, they can be useful for understanding how the object detection works on the inside. 

It is almost always better to use the DetectionFrame and DetectedObject classes. These are abstracted versions of ObjectsFrameSDK and ObjectDataSDK that provide access to the data in forms much more aligned with Unity standard practice, along with additional helper functions that give you options for presenting/transforming the data. Each one also has a reference to the original ObjectsFrameSDK or ObjectDataSDK object it was created from, so you can always get the "original" data from these classes if you need. 

You get the Object Detection data from ZEDManager and can access it once Object Detection has been started. Your options are: 

- GetDetectionFrame(): Returns the abstracted DetectionFrame object from the most recently-detected frame. 
- GetSDKObjectsFrame(): Returns the original ObjectsFrameSDK object from the most recently-detected frame. 
- OnObjectDetection: An event that is called whenever a new frame has been detected, with the abstracted DetectionFrame object as the argument. 
- OnObjectDetection_SDKData: An event that is called whenever a new frame has been detected, with the original ObjectsFrameSDK object as the argument. 

Find additional documentation on the ZED's Object Detection module, including a Unity tutorial, at https://www.stereolabs.com/docs/.

