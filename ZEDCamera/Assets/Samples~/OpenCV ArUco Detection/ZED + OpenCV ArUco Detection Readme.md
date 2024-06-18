# ZED OPENCV ARUCO MARKERS IN UNITY

These scripts and samples demonstrate how to use the ArUco marker detection functions of OpenCV with the ZED camera and SDK in Unity. 

The ZEDToOpenCVRetriever script can also be used to supply a proper OpenCV mat of the camera's images for other OpenCV uses. 

## Requirements: 

 - ZED or ZED Mini camera and the ZED SDK - Find more info at www.stereolabs.com
 - OpenCV for Unity plugin by Enox Software - Purchase* from the Asset Store: https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088

*Note: The free trial version of OpenCV for Unity will work in the Unity Editor, but you will not be able to build into a standalone app. 

## Setting Up Markers:

You'll need to use marker images from a pre-defined dictionary within the ArUco module of OpenCV. For convienence, we've included the first five images
from the 4x4 dictionary in this plugin. Other images are available on the Internet. At time of writing, the first 50 markers of the 4x4 dictionary
can be downloaded at http://jevois.org/data/ArUco.zip. 

If you print the markers, make sure they lay flat. It can be good to secure them onto a more rigid surface like a cardboard square. It is also good to include a border around
the black edges of the marker; otherwise, OpenCV will not be able to detect the marker if it's edges are touching a black object. 

You can also display the markers on a screen, like a phone, which can be especially convenient during development. Just be sure that the brightness is turned up, and that you keep the 
size of the marker static. 

## Building A Scene:

A basic marker detection scene needs four things: 

- The ZED_Rig_Mono or ZED_Rig_Stereo prefab, included with the ZED Unity plugin. Choose ZED_Rig_Stereo if using a VR headset. Otherwise, choose ZED_Rig_Mono.
- A ZEDToOpenCVRetriever component. This takes each new image from the ZED, converts it to an OpenCV Mat format and calls an event with it. 
- A ZEDArUcoDetectionManager component. This takes the Mat from ZEDToOpenCVRetriever, detects all markers in it, finds their world poses, and calls events and functions within MarkerObject.
- At component that inherits from the MarkerObject script, like MarkerObject_MoveToMarker. MarkerObject registers itself to ZEDArUcoDetectionManager and has functions called when markers were detected. 

*Note: The MarkerObject can be substituted for another script if it subscribes to ZEDArUcoDetectionManager.OnMarkersDetected, which lists all markers detected each frame.*

The most basic scene, which moves a virtual object to a marker when it's detected, would use the MarkerObject_MoveToMarker script. With those four objects in the scene, do the following:

- Measure the width of your ArUco marker in meters (100cm = 1m)
- In your ArUcoDetectionManager component, fill in this number in the MarkerWidthMeters field
- If you are using a marker dictionary other than 4x4, also set this in your ArUcoDetectionManager component
- On your MarkerObject component, set the MarkerID value to the index of the marker in the dictionary (ex: 0) 
- Make whatever virtual object that you want be a child of the MarkerObject. For testing, you can use a cube and set its scale to something small like (.1, .1, .1)

Run the scene, point the ZED at the marker, and the 3D object should move to it. 

*Note: MarkerObject_MoveToMarker isn't built to handle detecting multiple instances of the same marker at the same time. If you want multiple copies of a virtual object in this case, see MarkerObject_CreateObjectsAtMarkers.*

## Inheriting from MarkerObject:

The MarkerObject script is an abstract class that will automatically register itself with the scene's ZEDArUcoDetectionManager on start. It has functions that will be called with information
about the marker's detection: 

- **MarkerDetectedSingle(Vector3 worldposition, Quaternion worldrotation):** Called every time the marker of MarkerID is detected with its world pose. Use to move an object somewhere. 
- **MarkerDetectedAll(List<sl.Pose> worldposes):** Called after all markers have been detected, with a list of all detected poses in world space. 
- **MarkerNotDetected():** Called each detection phase if no markers of the MarkerID have been detected. Use to clear/reset things you had previously positioned by a marker.

For a barebones reference, see MarkerObject_MoveToMarkerSimple. This is a very barebones implementation of MarkerObject, which moves the object to its marker when detected. It lacks smoothing
and other features of MarkerObject_MoveToMarker, showing only what's required to make a class properly inherit from MarkerObject. 