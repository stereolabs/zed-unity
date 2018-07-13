How it works: GreenScreen Demo

Required:
ZED camera
Green screen
HTC Vive headset or Oculus Rift
2x Vive controllers, 1x Vive Tracker and 2x controllers, or 2x Oculus Touch controllers.

First, you will need to mount a controller/tracker onto the ZED and make sure they are firmly attached. 
The controller will let you track the ZED in the same space as the HMD and allow you to move the camera while filming. 
To attach the ZED and controller/tracker, you can download and 3D print the support available here:
https://docs.stereolabs.com/mixed-reality/unity/images/ZED_MR_support.zip

How It Works
To start recording mixed reality footage, we will go through the following steps:

Create a new Unity project and import the ZED_Green_Screen prefab
Adjust ZED camera settings to improve image quality
Adjust chroma key to remove the green screen
Add the headset

MANUAL CALIBRATION

We provide a semi-automated calibration app for SteamVR that lets you create an offset file automatically.
It calibrates the relative position of the ZED and Vive/Oculus play space. You can download a beta version here:
https://www.stereolabs.com/developers/downloads/unity/ZED_Greenscreen_Calibration_v2.4.0.zip

The Script ZED Offset Controller takes the calibration file generated.
And applies it to the gameObject which it's attached to.