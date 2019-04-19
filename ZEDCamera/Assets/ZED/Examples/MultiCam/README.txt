Summary: 

Uses Multi-cam capabilities since plugin 2.8.

* Two ZED_Rig (mono) are used in the scene (each one with its CAMERA_ID). 
* Images are then shown on the canvas.
 

Notes : 
- There is a current limitation of 4 cameras on the plugin (CAMERA_ID up to 4).
- Make sure that if you have multiple ZEDManager (or ZED Prefabs) in the scene, they don't share the same CAMERA_ID, otherwise undefined behavior may occur.

