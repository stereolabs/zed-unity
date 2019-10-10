# ARUCO DRONE WARS EXAMPLE

This scene will create a drone character hovering above each marker (0 - 4) from the 4x4 dictionary. 

If one drone sees another drone directly in front of it, and it's within range, it will fire lasers at the other drone until it's destroyed. 
These lasers can be blocked by real-world obstacles, like your hand. 

If a drone is destroyed, you can respawn a new drone over its marker by simply removing the marker from the camera's view and putting it back. 
This happens via the AssertObjectExistenceOnEnable script, found on the Prefab Restorer object on each drone. It will check if the drone has been destroyed 
whenever it's been enabled, and instantiate it if needed.

This sample makes use of the MarkerObject_MoveToMarker script. Each drone already exists in the scene, but is disabled until its corresponding marker is visible. 
Once it's marker is seen, the drone will appear in this position. 

It also implements other useful features: 

- Position/rotation smoothing: Drone poses are smoothed between frames so that noise from marker detection doesn't cause the drones to "vibrate"
- Hidden frame tolerance: A drone's marker must be out of view for several consecutive frames before being hidden, so a single frame of noise doesn't reset the drone's health 
- Pre-instantiated pool: It creates several objects on start and disables them, so you don't encounter a performance drop when you first detect a marker due to instantiation

Note that you must avoid showing multiple copies of the same marker at the same time, or else the corresponding drone will bounce between them. 

## To Use:

- Print out at least two copies of aruco_4x4_0.png in the ArUco Marker Images folder
- Measure their width and put it in the MarkerWidthMeters field in the ArUco Detection Manager object
- Start the scene
- Point the ZED at the printed markers to see the drones appear
- Move one marker to point at the other marker to make it shoot

## Adding More Drones

If you wish to support more than 5 drones at a time, simply make copies of the Drone objects already in the scene, and increase the MarkerID value on each new one
to a higher value (5, 6, 7, etc.)

## Multiple Copies of the Same Marker: 

You can add support for multiple copies of the same marker if needed by replacing the Drone objects with an empty object with the MarkerObject_CreateObjectsAtMarkers 
script. Then assign the "ArUco Drone w Marker Offset" prefab to its Prefab field.

Rather than having an existing instance of this script for every drone, a single instance acts as a manager for all drones that should appear over marker 0 (or whatever 
you set the markerID value to). Each frame, this object makes sure there is an object for each marker that's in view, and keeps existing objects in sync with the nearst 
markers so they move properly when the marker moves. 

This is useful if you want each marker to represent a different kind of object (like different creatures in an AR trading card game) while still supporting multiple copies
of a single object (like playing the same creature card twice). However, it is generally more reliable to use unique markers for every object, so it is preferable
to use MarkerObject_MoveToMarker when you don't have this specific need. 