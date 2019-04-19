You can use Spatial Mapping in any scene with a ZEDManager in it. 

- Select the object with ZEDManager on it. This is usually the root object of ZED_Rig_Mono or ZED_Rig_Stereo prefab.
- In the Inspector, open the foldout arrow besides "Spatial Mapping"
- Adjust the settings as you see fit
- While the scene is running, click "Start Spatial Mapping"
- When you are done scanning, press the same button which will read "Stop Spatial Mapping"
 

In this sample, scanning the area will cause a bunny to spawn in your environment once you press the "Stop Spatial Mapping" button, so long as there is enough flat, upward-facing surface. 

This happens because the NavMeshSurface script on the Navigation object subscribes to the ZEDSpatialMapping.OnMeshReady event. When that event is called, it builds a NavMesh, and calls its own event, OnNavMeshReady. Then, EnemyManager, which is subscribed to that OnNavMeshReady event, will attempt to place a bunny on the nav mesh. 

You can find more information on using the ZED's spatial mapping features in Unity here: https://www.stereolabs.com/docs/unity/spatial-mapping-unity/
For more general information about the ZED's spatial mapping features, click here: https://www.stereolabs.com/docs/spatial-mapping/
