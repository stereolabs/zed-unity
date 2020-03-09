<!------------------------- Release notes ------------------------------------->


### 3.0.0

  * **Features:**
    * Added object detection module from ZED SDK 3.0.
    * Updated with API rework from SDK 3.0.
    * Added utils function in ZEDCamera.cs to save image, depth and point cloud: *(Those functions are wrapped from ZED SDK C++ functions)*
         * `ZEDCamera.SaveCurrentImageInFile()`   
         * `ZEDCamera.SaveCurrentDepthInFile()`
         * `ZEDCamera.SaveCurrentPointCloudInFile()`      
    * Update Layer system for rendering and AR planes to a single layer used (layer 30).
    * Merge Windows and Linux compatibility branches into a single one.


  * **Bug Fixes:**
    * Fixed display issue with spatial mapping (display/hide mesh was not working).
    * Fixed camera position when SVO is looping back and initial position is estimated. It was previously reset to identity, now it uses the detected position.
    * Fixed Mesh flip when saved into .obj file.


  * **Compatibility**:
    * Compatible with ZED SDK 3.0, CUDA 9.0, 10.0 and 10.2 on Windows and Linux



### 2.8.2

  * **Features:**
    * Added OpenCV ArUco marker detection sample scene. If you've also imported the OpenCV for Unity package from the Asset Store, you can place objects in 3D where ArUco-stlye markers are detected.
    * Added brand new Mixed Reality calibration app as a sample scene. You can now calibrate the ZED with a tracked object for mixed reality VR capture, all from within the Unity editor.

  * **Improvements:**
    * Added Max Depth Range and Confidence Threshold options in ZEDManager's Advanced Settings
    * Added option to disable IMU prior setting in ZEDManager's Advanced Settings
    * Changed how meshes scanned with the Spatial Mapping feature were saved, to avoid them being flipped on the Z axis when re-imported
    * Added option to set which side of the ZED (left or right) that ZEDRenderingPlane retrieves. This also solved an issue where the right camera in the ZED_Rig_Stereo prefab would display the left feed when not in AR pass-through mode

  * **Bug Fixes:**
    * Fixed a crash that would happen if two ZEDManagers were set to use the same Camera ID
    * Fixed ZED_Rig_Stereo sometimes not outputting an image to the Game window in newer versions of Unity
    * Fixed ZED_GreenScreen prefab setting that caused the output to be displayed in only half the screen
    * Fixed depth values being incorrect for occlusion when using OpenGL instead of DirectX


### 2.8.1

  * **Improvements:**

    * * Updated ZEDControllerTracker:
      - Added input support for SteamVR plugin 2.0 and greater
      - Added ZEDControllerTracker_DemoInputs, which inherits from ZEDControllerTracker, but with generic button/axis check functions
      - These functions work whether using SteamVR or Oculus Unity plugins
      -  Several scripts in Example scenes are now simplified to call these functions instead of having plugin-specific code.
    * Removed limit on how long ZEDManager would attempt to connect to the ZED camera - previously 10 seconds
    * ZEDTransformController, used to move object like the Planetarium and Movie Screen in example scenes, now takes into account the Reposition At Start feature added to ZEDManager in 2.8
    * Shader properties that are retrieved or modified more than once per scene now have their property IDs cached, saving lookups
    * ZEDManager can now be used outside of the included prefabs, and is more flexible when determining if it's in "stereo" mode (like the ZED_Rig_Stereo prefab) for loading AR pass-through features.
    * ZEDManager's process for closing cameras when the scene closes is now more stable

  * **Bug Fixes:**

    * Fixed ZEDControllerTracker's latency compensation feature moving the controller incorrectly when using ZED in third person
    * Fixed non-rendering Camera objects not getting properly disposed at scene close, causing performance issues after running scene in editor repeatedly
    * Camera Brightness adjustment now works in deferred rendering (Global Shader value)
    * Fixed #ZED_OCULUS compiler directive not getting activated when newer Oculus package is imported
    * Fixed newer Oculus package causing #ZED_STEAM_VR compiler directive getting activated when importing Oculus package. This would also happen when importing the OpenVR package in Unity 2019
    * Fixed ZEDControllerTracker not updating Oculus Touch controller position if only one controller is connected
    * Fixed ZEDSupportFunctions.GetForwardDistanceAtPixel not properly accounting for screen size
    * Fixed "ZED Disconnected" error message not being locked to the headset when using ZED_Rig_Stereo
    * Fixed Planetarium example scene being way too dark in deferred rendering
    * Fixed plugin not recognizing Dell VISOR or Lenovo Explorer WMR headsets when using them in SteamVR, or any WMR controllers at all.
    * Updated video stream link in Movie Screen example scene as the old video link was taken down (same video, new host)
    * Fixed SVO Loop feature not working when real-time mode is enabled
    * Fixed Multicam crash if both cameras share the same CAMERA_ID


### 2.8.0

   * **Features**:
    * Added Multi ZED Rig support:
      - *Complete refactoring of the wrapper and plugin to change singleton implementation into multi instance support. The maximum number of instance is limited to 4 cameras.*
      - *Each ZEDManager now has a ZED_CAMERA_ID to define its own camera ID.*
      - *All static events in ZEDManager have been replaced by "local" events to make them specific to a rig/camera. Example: OnZEDReady*
      - *Added ZEDManager.GetInstance(sl.ZED_CAMERA_ID) static function to access the ZEDManager in a external script without having the ZEDManager as a parameter of the script. Note that the user must check the return value of this function since it can be null.*
      - *Added ZEDManager.GetInstances() to list all ZEDManagers in the scene.*
      - *Spatial Mapping module and Camera settings module have been moved to ZEDManager to simplify their use in a multi or single ZED configuration.*
      - *Added MultiCam example to show how to use 2 ZEDs in a single application.*
      - *Overhauled many scripts to take advantage of multiple cameras when possible. For example, projectiles in the Drone Shooter sample can collide with the world seen by any camera.*
    * Added Streaming module from ZED SDK 2.8:
      - ***Input*** : *You can now receive a video stream from a ZED over the network, and process it locally. Do this by setting ZEDManager's Input Type to "Stream" in the Inspector, and specify the IP/Port configuration of the sender.*
      - ***Output*** : *To broadcast a stream, set the ZED's Input Type to USB or SVO, and open the "Streaming" section further down. Specify the codec/streaming configuration if necessayr and check "Enable Streaming Output".*
      - *You can adjust camera settings from a receiving PC, allowing you to control an entire scene from a single device.*
    * Added initial camera position estimation option to ZEDManager:
      - *If EnableTracking is activated, estimateInitialPosition will simply activate the TrackingParameters::set_floor_as_origin to estimate the floor position, and therefore the camera position, during tracking initialization. *
      - *If EnableTracking is not used, estimateInitialPosition will try to detect the floor planes multiple times and compute an average camera position from the floor position.*
    * Pose smoothing added to spatial memory feature, so pose corrections no longer appear as "jumps."
    * Added manually turning the ZED's LED light on and off, to be used for debugging. See ZEDCameraSettings.
    * Added option in ZEDManager's Advanced Settings to grey out the skybox when the scene starts. This was done automatically before to avoid affecting AR lighting, but can be disabled for greenscreen use.

    * **Improvements**:
     * Removed ZED rigs' dependence on layers:
       - Previously, the "frame" quads in each ZED rig (including the hidden AR rig) were assigned their own layer, and all other cameras had this layer removed from their culling mask. This made it so no camera would see a frame meant for another eye, but left fewer layers available to the user and made the cameras' culling masks impossible to set from the Inspector.
       - Frames now use the HideFromWrongCamera script to prevent rendering to the wrong cameras without the use of layers. Cameras in ZED_Rig_Mono and ZED_Rig_Stereo can have their culling masks set freely.
       - The hidden AR camera rig still uses a single "AR layer", customizable in ZEDManager's Advanced Settings, so that it can see the frame without seeing any other objects. This was done as an alternative to drawing the quad with Graphics.Draw, which would make it difficult for newer users to understand.
     * Moved all SVO features from ZEDSVOManager (now deprecated) to ZEDManager:
       - Read an existing SVO by setting Input Type to "SVO" and specifying a file.
       - Record a new SVO by opening the "Recording" section. You now need to press "Start Recording" from the editor or call ZEDCamera.EnableRecording() in a script (and ZEDCamera.DisableRecording() to stop).
     * Planes detected by the ZED no longer drawn with  a hidden camera, allowing them to be drawn at the same time as a spatial mapping wireframe.
     * ZEDPlaneDetectionManager can now have "Visible in Scene" disabled and "Visible in Scene" enabled at the same time.

   * **Bug Fixes**:
    * Fixed bug that overwrites camera position if user set one before starting the scene (when tracking is not activated).
    * Fixed latency compensation in ZEDControllerTracker causing the ZED to drift infinitely when a ZED rig was a child of it.
    * Fixed normally-hidden AR rig not appearing in the Hierarchy on start if "Show Final AR Rig" was enabled before runtime.
    * Fixed app taking a long time to close if closed while a ZED was still initializing.
    * Fixed asteroids in Planetarium sample only being drawn to layer 8.


   * **Compatibility**:
    * Compatible with ZED SDK 2.8, CUDA 9.0 and 10.0.
    * Updated controller scripts to work with the new SteamVR Unity plugin v2.0, so long as its Action system has not been activated.




###2.7.1

   * **Features/Improvements**:
     - Added option to enable/disable the skybox at start in ZEDManager -> Advanced Settings. Prior, it was always disabled.  
     - ZEDManager now automatically names the layers it uses (27-30 by default) so long as they don't already have names. This makes them visible in layer drop-downs,
       making it much clearer how the plugin uses these layers. Big thanks to Andrea Brunori for suggesting the method used to achieve this.
     - ZEDManager's default layers changed from 28-31 to 27-30 to avoid conflicting with Unity rendering in the Inspector. Another thanks to Andrea
       for pointing out this conflict.
     - Global lighting settings in all demo scenes are now similar to avoid confusion.
     - Removed outdated controller scripts ZEDOculusControllerManager and ZEDSteamVRControllerManager. These have been deprecated for several versions.
       Use ZEDControllerTracker instead.

   * **Bug Fixes**:
     - Fixed the visibility and physics of planes detected by ZEDPlaneDetectionManager not being updated if the global visibility/physics values
       were updated by a script when the Inspector wasn't visible. See planesHavePhysics, planesVisibleInScene and planesVisibleInGame within ZEDPlaneDetectionManager.
     - Fixed Oculus Integration and SteamVR Plugin packages not being detected if their folders were renamed, or placed anywhere but the root /Assets/ folder.
     - Fixed ZEDControllerTracker causing ZED rig to float away when ZED rig is a child of it (due to drift correction)
     - Fixed hidden AR rig not appearing in the Hierarchy if Show AR Rig in ZEDManager's Advanced Settings was set to true on start.



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
