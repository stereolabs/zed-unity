How it works: Plane Detection Demo

Required:
-ZED_Rig_Stereo prefab.
-VR Hmd with at least 1 Controller (Oculus Touch or Vive Controller).
-Oculus Integration OR SteamVR Unity plugin.

How to use without VR:
- Open the "Simple Plane Detection" sample scene.
- Look with the Zed camera somewhere on a somewhat flat surface.
- Hold the "Space" bar key to start looking for a positive surface.
- If the Bunny placeholder is Red, it won't be able to spawn there.
- If it's Blue, then It will spawn a Bunny when the key is released from holding down.

How to use with VR:
- Open the "VR Only Plane Detection" sample scene.
- Point with the controller towards a flat surface (floor, table etc...).
- Hold the [Trigger Button] of the controller to start checking the area pointed for a positive surface.
- When the Bunny Placeholder is RED, it won't be able to spawn a Bunny.
- If its Blue, then you can spawn a Bunny on that surface.
- Release the [Trigger Button] when the placeholder is white, to spawn a Bunny.
- A baseball bat will appear at the end of the Controller.
- Use it to hit the bunny and throw it as far as possible in the available space around you.

Checking the variable "Can Spawn Multiple Bunnies" will no longer delete the previous Spawned Bunny.
///////////////////////////////////////////////////

The Prefab "VR_Controller" contains three major scripts;
......................................................
- VR_ObjectTracjer
This script tracks the VR devices that can be selected through the "Device To Track" variable on the Inspector.
It works for SteamVR & Oculus plugins. 
......................................................
- Plane Manager
This script manages when to give the order to the ZED Plane Detection Manager to detect a plane at a given world space point.
It checks for inputs from the VR Controllers connected, and depending on the state of that input, it starts looking for planes.

Whenever the ZED Plane Detection Manager finds something, we look at how the normal of the plane it:

[If the surface is horizontal enough, then proceed to spawn our Bunny]

[Else, clear everything and start again]

Here is also managed the display of the place holder for the Bunny.
......................................................
- Bunny Spawner
This script manages not only the spawning of the Bunny, but also the display of the pointerBead laser, the display of the baseball bat.
The UI Panel is also spawned through here, displaying the distance traveled by the Bunny since hit by the baseball bat.

///////////////////////////////////////////////////

The Prefab "Zombunny" contains the Script "Bunny", and is the object spawned when the Plane Manager finds a good plane for it.
......................................................
At spawn, its Rigidbody component is not Kinematic so its allowed to interact with the baseball bat and move.
When it get hits by the baseball bat, a delayed is applied before starting looking for collisions with the real world.

This delay is important since the Bunny is already on the ground, so we wait for it to have moved a bit before starting detecting.

///////////////////////////////////////////////////

The Prefab "Capsule_Follower" with the script of the same name.
......................................................
This gameObject is set to follow other gameObject which are set as children to the baseball bat.
It then calculates a velocity base on its movements towards those objects.

When it collides with a Bunny;

[If the vertical velocity if negative, then the bunny gets squeeze to the ground].

[Else if the velocity magnitude is high enough, it gets transmited to the Bunny, which GetsHit and is sent flying].
