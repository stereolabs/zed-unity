# SIMPLE MARKER PLACEMENT EXAMPLE

This scene moves colored tiles to your markers whenever you see them. 

To use it, simply print/display 1 to 5 of the markers in the ArUco Marker Images folder, run the scene, and have the ZED look at them. 

It is slightly more advanced than the basic scene described in the project readme: 

- There are five marker objects, each bound to a different marker index. 
- There is a script on each object that scales the cubes to fit the size of your markers. 

It does not support more than one instance of the same marker at once. For instance, seeing one of all five markers at once will work, but
seeing five copies of marker 0 will cause the first cube (red) to stick to one marker arbitrarily. For handling multiple markers, see 
the MarkerObject_CreateObjectsAtMarkers script instead of MarkerObject_MoveToMarker.