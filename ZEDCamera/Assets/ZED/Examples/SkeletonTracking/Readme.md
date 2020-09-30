# ZED Body tracking

These scripts and sample demonstrate how to use the ZED SDK's Body tracking module in Unity. Note that Body tracking requires a ZED2, *not* a ZED or ZED Mini.

## What's Body tracking?

The ZED SDK Body tracking module uses a highly-optimized AI model to detect and track human body skeletons in real time. Using depth, it goes a step further than similar algorithms to calculate the object’s 3D position in the world, not just within the 2D image.

When Object Detection is running, the ZED will regularly output a Frame object, received in Unity as an ObjectsFrameSDK object. In it is some metadata and a list of individual object detections, contained
in an array of ObjectDataSDK.

Each detection holds the following info:

- ID - unique ID assigned to each object for tracking them between frames.
- Category - If the object is a person or vehicle.
- Tracking State - If the position of the object is understood relative to the floor/world.
- Confidence - How sure the SDK is that this object exists, on a scale from 0 to 99.
- 2D Bounding Box - 2D coordinates of the four corners on the ZED image that define where in the image the object is.
- 3D Position - Where the camera is in 3D space.
- 3D Velocity - Direction and speed that the object is moving.
- 3D Bounding Box - 3D coordinates of the eight corners that make up a box that encapsulates the object.
- 3D Skeleton joints - 3D coordinates of the joints.

## Setting Up Body tracking:

Hardware-wise, you'll want your ZED2 pointed so that a significant part of the floor is visible. This is because the ZED SDK will detect the floor when it starts, and uses it to better understand the position and movement of objects in 3D.

To make your own scene without doing any scripting, do the following:

- Put the ZED_Rig_Mono prefab in the scene
- Make sure ZEDManager has Enable Tracking and Estimate Initial Position enabled in the Tracking options.
- Set the Object Detection Model to HUMAN_BODY_FAST or HUMAN_BODY_ACCURATE.
- *(Optional)* Set the Depth Mode to ULTRA and use at least 1080p resolution to ensure the highest quality detections.
- Make an object with a ZEDSkeletonTrackingViewer component attached, and fill its Avatar values with the appropriate prefab in ZED -> Examples -> SkeletonTracking -> Ressources -> Prefabs. You can also disable the **Use Avatar** paramater
to change the visualization mode and only see the skeleton. Pressing the **Space** key of your keyboard allows you to switch between Avatar and 3D skeleton mode.

## Configuration

In ZEDManager's Inspector, there is a section dedicated to Object Detection/Skeleton tracking. In it are two categories of parameters: Init and Runtime. Init parameters must be set before Object Detection is started. Runtime parameters can be adjusted whenever you want, and will be applied instantly.

### Initialization Parameters:

- **Enable Object Tracking:** If enabled, the ZED SDK will track objects between frames, providing more accurate data and giving access to more information, such as velocity.
- **Enable Body Fitting**

### Runtime Parameters:

- **Confidence Threshold**: Sets the minimum confidence value for a detected object to be published. Ex: If set to 40, the ZED SDK needs to be at least 40% confident that a detected object exists.

Note the "Start Object Detection" button at the end. Object Detection requires a lot of performance, so there is not an option in ZEDManager to start it automatically. To start it, you must either press that button yourself, or have a script start it. The ZEDSkeletonTrackingViewer script will do this for you, provided this option is checked in the Inspector.

## Accessing Object Detection Data

If you're writing your own scripts to make use of Object Detection data, there are several ways of getting to it.

First, it's worth noting that the data *directly* from the SDK was not designed for use within Unity. This data is stored in the ObjectsFrameSDK and ObjectDataSDK structs. Many values in these structs require additional transforms to be useful in Unity. For example, many 3D coordinates are not in world space, and 2D coordinates have their Y values inverted from Unity convention. Being structs, they also lack helper functions. However, as these structs are very close to how they are presented in the C++ ZED SDK, they can be useful for understanding how the object detection works on the inside.

It is almost always better to use the DetectionFrame and DetectedObject classes. These are abstracted versions of ObjectsFrameSDK and ObjectDataSDK that provide access to the data in forms much more aligned with Unity standard practice, along with additional helper functions that give you options for presenting/transforming the data. Each one also has a reference to the original ObjectsFrameSDK or ObjectDataSDK object it was created from, so you can always get the "original" data from these classes if you need.

You get the Object Detection data from ZEDManager and can access it once Object Detection has been started. Your options are:

- GetDetectionFrame(): Returns the abstracted DetectionFrame object from the most recently-detected frame.
- GetSDKObjectsFrame(): Returns the original ObjectsFrameSDK object from the most recently-detected frame.
- OnObjectDetection: An event that is called whenever a new frame has been detected, with the abstracted DetectionFrame object as the argument.
- OnObjectDetection_SDKData: An event that is called whenever a new frame has been detected, with the original ObjectsFrameSDK object as the argument.
