Mixed Reality Calibration

This scene makes it simple to calibrate the position of your ZED camera relative to a tracked object, such as a motion controller or Vive Tracker. 

This calibration is primarily used for third-person mixed reality capture, often with a greenscreen. See our documentation for more information: https://www.stereolabs.com/docs/unity/green-screen-vr/


## Setup

Like all ZED scenes that use motion controllers, this requires you to import either the SteamVR or Oculus Integration packages from the Asset Store: 

- SteamVR: https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647
- Oculus Integration: https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022

Once either is imported, you can run the scene to perform the calibration. Make sure all tracked objects are visible by the tracking system when you click Run. 

If using SteamVR, you should get a notification on import that it has detected an existing binding configuration, and it will ask you if you want to import it. Say yes, then in the bindings menu, open the web GUI 
to bind controls. You need to bind Click and Grab to use this scene. 


## Using the App

The first thing you'll see are cubes for each tracked object you have. Put your controller inside the cube of the object attached to your ZED and click. 

Once you do so, you'll see a big screen showing the ZED feed with virtual motion controllers overlayed. However, the motion controllers will not be positioned properly. When we're done, they will be. 

You'll also see the tracked object and a model representing your ZED camera. The ZED is offset from the tracked object by the current calibration profile, which is empty at first. 

Last, you'll have a menu, either attached to your controller or anchored in front of you (depending on if you have two controllers available). Use this to switch between "Manual" and "Automatic" calibration modes. 


### Manual Mode

In Manual, you will have two controls near the menu for moving the ZED. One has arrows, similar to the Translate controls in Unity, and the other has rings, similar to Unity's Rotate controls. They do the same thing. 

Click and pull on the arrows and rings to move the ZED. Aim to have the virtual ZED offset from the tracked object in the same way it is in real life. Look at the virtual controllers on the screen to fine-tune it.


### Automatic Mode

You'll want to roughly align the camera in Manual Mode before switching to Automatic. 

In Automatic, you'll see five spheres with X icons overhead. Go to a sphere, put your controller inside, and click. You'll notice the video feed on the screen stops, but you can still move the controllers around. 

Position the controller so that it's lined up with its real-world counterpart. Then click again. 

This will cause the video feed to play again, and the ZED will have moved slightly. But it will not be perfectly accurate. Repeat the process with the remaining four spheres. 

If the final results aren't perfect, you can try it again by switching to Manual and back to Automatic. 


### Finishing

When it appears like the controllers are properly aligned, double check this by moving the controllers to the far edges of the screen. Sometimes a bad calibration will appear correct at certain angles. 

Once you're satisfied, press the Save button. This will save your calibration profile to your PC. If you run the Greenscreen scene, it will automatically be loaded. 


## Attributions

UI sounds by ryusa: https://freesound.org/people/ryusa/packs/26457/
Used under Creative Commons license: http://creativecommons.org/licenses/by/3.0/