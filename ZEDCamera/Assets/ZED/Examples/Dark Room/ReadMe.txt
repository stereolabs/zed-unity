Summary: 

Uses ZEDManager's Brightness function to darken your room, then re-illuminate it in interesting ways with a virtual laser light show. 

ZEDManager's brightness slider will decrease the overall real-world brightness without changing the brightness of the virtual objects. 
Environments darkened in this way can be realistically illuminated by virtual lights like the one on the sample scene's red ball. 

Required:

ZED_Rig_Stereo

or 

ZED_Rig_Mono

How to use:

Change the camera brightness of the scene by selecting the ZED rig object, and in the inspector under ZED Manager, 
move the Brightness slider

 or 

Change the camera brightness value by setting the ZEDManager.CameraBrightness value in your custom script

Then, virtual lights will brighten it back up in a realistic way. Note: In forward rendering (the default) you must have a ZEDLight component
attached to the light for it to cast onto the real world, and you are limited to 8 lights at once. 


Music for Dark Room sample is "Calm the **** Down" by Broke for Free, licensed under Creative Commons 3.0 and shortened from its original length.
Author page: http://freemusicarchive.org/music/Broke_For_Free/
