<!------------------------- Release notes ------------------------------------->
### 2.7.0

   * **Features**:
    - Added toggle box to reveal a camera rig used for final HMD output, normally hidden, for advanced users
    - Added toggle boxes for the fade-in effect when the ZED initializes, and setting the ZED rig Gameobject to "DontDestroyOnLoad" on start


   * **Bug Fixes**:
    - Fixed Rift/Vive controller drift when using ZED's tracking instead of the HMD's  
    - Changed the previously hard-coded layers the ZED plugin used (8-11) to be adjusted in the Inspector, and set them to 28-31 by default
    - Changed greenscreen config file loading so that it will work in a build when placed in a Resources folder, and changed default save path accordingly
    - Clarified error messages from playing SVOs in loop mode with tracking enabled


   * **Compatibility**:
    - Compatible with ZED SDK 2.7, CUDA 9.0 or 10.0.


### 2.6.0
   * **Features/Bug Fixes**:
     - Add WindowsMR support through SteamVR Only (Beta).
     - Fixed overwriting old mesh textures when Spatial mapping button is used while Save Mesh is checked.
     - Forced pasue state to false when stating a scan in case it's been left in a pause state from a previous scan.
     - Fixed restart (not) required when changing post-processing settings.
     - Fixed repetitve UI error when not using plugin in VR mode in ZEDControllerTracker.cs. [ssue #21 reported on github].


  * **Documentation**:
    - Full rework of comments in source code.


   * **Compatibility**:
     - Compatible with ZED SDK 2.6, CUDA 9.0 or 9.2.


### 2.5.0
   * **Features**:
     - Add USB Headset detection function in non-play mode.
     - Improve tracking status with "Camera not tracked" status if no headset connected and camera tracking is disabled.


  * **Examples**:
    - Add Drone Shooter example. This example reproduces the ZEDWorld drone demo app.
    - Add Movie Screen example. This example reproduces the ZEDWorld movie demo app.
    - Add VR Only Plane Detection. Advanced plane detection sample to show how the place the bunny and make collisions.


   * **Compatibility**:
     - Compatible with ZED SDK 2.5, CUDA 9.0 or 9.2.


### 2.4.1
  - **Features**:
    - Add Full IMUData access to get angular velocity and linear acceleration through ZEDCamera.GetInternalIMUData() function.

  - **Examples**:
    - Modified Dark room sample into a AR "night club" light show to show how to use ZED Manager's camera brightness settings and ZEDLight script.
    - Update Object Placement sample and doc.

  - **Bug Fixes**:
    - Fix ZEDManager's camera brightness settings through scripting.
    - Fix Normal and Center on plane's detection. Refactor ZEDPlaneGameObject as MonoBehavior.
    - Fix ZED Rig Mono image when game is built due to rendering path.
    - Fix crash when moving SVO playback playhead.
    - Fix Camera settings spamming errors due to pluginIsReady value not set.
    - Fix loading screen blank on left eye at specific rotations (180deg).
    - Fix Depth occlusion setting in Rig mono.
    - Fix greenscreen wrong config file that breaks the GreenScreen example if not found.
    - Remove "Restart Camera" when changing Depth occlusions as it was not needed.

  - **Compatibility**:
    - Add Vive Pro support as VR headset (not with the Vive's cameras).


### 2.4.0
 - **Features**:
   - Added plane detection :
      - ZEDPlaneDetectionManager.cs interfaces with the SDK. Click on the screen to add planes when detected, or specify a screen-space location with DetectPlaneAtHit(). A specific function for floor detection DetectFloorPlane() is provided (as well as a GUI button access). ZEDPlaneDetectionManager inherits from MonoBehavior and must be used to have access to plane detection in Unity Scene.
      - ZEDPlaneGameObject.cs represents a plane with info about its position, rotation, boundaries, etc. It also creates a GameObject representation of the plane that supports collisions and rendering.
      - ZEDPlaneRenderer.cs (automatically created by ZEDPlaneDetectionManager) displays a scene's planes overlapping other objects so that planes highlight real-world features instead of appearing distinct from them.
   - Added HitTest multiple support functions for Real / Virtual world collision when no spatial mapping is used (Beta).
   - Added prompt for recommended settings on import, most notably MSAA x4 and Linear color space.
   - Added changing resolution, depth mode, and other options in ZEDManager at runtime (requires restarting the camera).
   - The ZED can now be reconnected mid-scene after being disconnected.
   - Scanned meshes, SVOs, greenscreen configuration files and .area files are now saved in /Assets/ by default rather than the root project folder. This makes them accessible in the Project tab within Unity.

 - **Bug Fixes**:
   - Fixed wrong transformation for Normal vector in World reference frame, causing object placement sometimes unstable.
   - Fixed incorrect reports of bandwidth issues when set to 1080p resolution.
   - Fixed alert about missing IMU before enough time has passed to find it.
   - Updated broken prefab references in Spatial Mapping sample and prefabs
   - Fixed Depth Occlusion option on ZEDManager only being changable at runtime, and only applying to the left eye in stereo mode.
   - Added proper error message when scene is started without the ZED SDK installed
   - Removed missing sdk window popup when play/pause is pressed. This window now only shows up when Unity is loaded.
   - Fixed broken Pause button on ZEDSVOManager.
   - Fixed the ZED stream not properly closing if ZEDManager was disabled or destroyed when the scene ended. This would result in problems connecting to the ZED until the ZED is replugged or the computer is restarted.  
   - Fixed issue causing the loading screen to be heavily pixelated in stereo mode.
   - Fixed flashing errors in loading screen (when an error occurs).
   - Specifying an invalid .area file for spatial memory no longer causes tracking to be disabled; the file is now ignored.
   - Fixed directionnal light on Object Placement scene.
   - Hide Rendering planes of ZED_Rig_Stereo prefab in the scene when game is not started.

 - **Examples**:
   - Modified lighting, animations and navigation in ZomBunny prefab for Spatial Mapping and Object Placement samples to make it appear more realistic.
   - PlaceOnScreen.cs (Object Placement) now instantiates new objects on click rather than moving around a pre-existing object.

 - **Renaming**:
   - Renaming getDepthAtxxx() and getDistanceAtxxx() support functions in getForwardDistanceAtxxx() and getEuclideanDistanceAtxxx() for better understanding.

 - **Compatibility**:
   - Updated Oculus Package detection ("OVR" becomes "Oculus") in ZEDOculusControllerManager.cs


### 2.3.3
- **Features**:
   - Updated ZEDCamera.cs script to include GetInternalIMUOrientation function to get access to internal imu quaternion of the ZED-M.
   - Improved VAR (timewarp) when using HTC Vive in stereo pass-through.

- **Bug Fixes**:
   - Fixed camera FPS stats display when camera is disconnected. It shows now "disconnected" instead of the last camera FPS.



### 2.3.2
- **Features**:
   - Updated ZEDCamera.cs script to include setIMUPrior(sl::Transform) function used for video pass-through.
   - Added Camera FPS, Engine FPS, Tracking Status, HMD and Plugin version in ZED Manager panel. These status will help developers see where the application's bottlenecks are.
A Camera FPS below 60FPS indicates a USB bandwidth issue. An Engine FPS below 90FPS indicates that rendering is the limiting factor. Both will induce drop frames.
Note that building and running your application greatly improves performance compared to playing the scene in Unity Editor
   - Added automatic detection of OVR package to avoid having to manually define "ZED_OCULUS" in project settings when using ZEDOculusControllerManager.
   - Added compression settings (RAW, LOSSLESS, LOSSY) in ZEDSVOManager.cs script.

- **Bug Fixes**:
   - Fixed initial camera position when using ZED Mini and an HMD. This had an impact on virtual objects created with physical gravity up and spatial mapping mesh origin.
   - Fixed enable/disable depth occlusions settings in Deferred rendering mode.
   - Fixed resize of halo effect in the Planetarium example.
   - Fixed garbage matte behavior in GreenScreen example that displayed anchor spheres in the scene after loading a matte
   - Fixed ZED Manager instance creation to respect MonoBehavior implementation. Only one ZED manager instance is available at a time for an application.
   - Fix Loading message when ZED tries to open.
   - Remove BallLauncher message instruction as gameobject, as it was not used.

### 2.3.1
- **Minor Bug fixes and Features** :
   - Fix GreenScreen broken prefab in Canvas.
   - Fix default spatial memory path when enableTracking is true. Could throw an exception when tracking was activated in green screen prefab.
   - Fix missing but unused script in Planetarium prefab
   - Added Unity Plugin version in ZEDCamera.cs

### 2.3.0
- **Features**:
   - Added support for ZED mini.
   - Added beta stereo passthrough feature with optimized rendering in VR headsets (only with ZED mini)
- **Prefabs**:
   - Added ZED_Rig_Stereo prefab, with stereo capture and stereo rendering to VR headsets (beta version)
   - Renamed ZED_Rig_Left in ZED_Rig_Mono, for better mono/stereo distinction.
- **Examples**:
   - Added Planetarium scene to demonstrate how to re-create the solar system in the real world. This is a simplified version of the ZEDWorld app.
   - Added Dark Room scene to demonstrate how to decrease the brightness of the real world using the "Real Light Brightness" setting in ZEDManager.cs.
   - Added Object Placement scene to demonstrate how to place an object on a horizontal plane in the real world.
- **Scripts**:
   - Added ZEDSupportFunctions.cs to help using depth and normals at a screen or world position. Some of these functions are used in ObjectPlacement scene.
   - Added ZEDMixedRealityPlugin.cs to handle stereo passthrough in Oculus Rift or HTC Vive Headset.
- **Renaming**:
  -  ZEDTextureOverlay.cs has been renamed ZEDRenderingPlane.cs.
- **Compatibility**:
  - Supports ZED SDK v2.3.0
  - Supports Unity 2017.x.y (with automatic updates from Unity).
- **Known Issues**:
  - On certain configurations, VRCompositor in SteamVR can freeze when using HTC Vive and ZED. Disabling Async Reprojection in SteamVR's settings can fix the issue.
  - The stereo passthrough experience is highly sensitive to Capture/Engine FPS. Here are some tips:
            * Make sure your PC meets the requirements for stereo pass-through AR (GTx 1070 or better).
            * Make sure you don't have other resource-intensive applications running on your PC at the same time as Unity.
            * Test your application in Build mode, rather than the Unity editor, to have the best FPS available.

### 2.2.0/2.1.0/2.0.0
See ZED SDK release notes.
