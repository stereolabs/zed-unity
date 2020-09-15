//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Text;
using System;
using System.Globalization;

/// <summary>
/// Processes the mesh taken from the ZED's Spatial Mapping feature so it can be used within Unity.
/// Handles the real-time updates as well as the final processing. 
/// Note that ZEDSpatialMappingManager is more user-friendly/high-level, designed to hide the complexities of this class. 
/// </summary>
public class ZEDSpatialMapping
{
    /// <summary>
    /// Submesh created by ZEDSpatialMapping. The scan is made of multiple chunks. 
    /// </summary>
    public struct Chunk
    {
        /// <summary>
        /// Reference to the GameObject that holds the MeshFilter. 
        /// </summary>
        public GameObject o;
        /// <summary>
        /// Dynamic mesh data that will change throughout the spatial mapping.
        /// </summary>
        public ProceduralMesh proceduralMesh;
        /// <summary>
        /// Final mesh, assigned to once the spatial mapping is over and done processing. 
        /// </summary>
        public Mesh mesh;
    }

    /// <summary>
    /// Structure to contain a temporary buffer that holds triangles and vertices. 
    /// </summary>
    public struct ProceduralMesh
    {
        /// <summary>
        /// List of vertex indexes that make up triangles. 
        /// </summary>
        public int[] triangles;
        /// <summary>
        /// List of vertices in the mesh. 
        /// </summary>
        public Vector3[] vertices;
        /// <summary>
        /// MeshFilter of a GameObject that holds the chunk this ProceduralMesh represents. 
        /// </summary>
        public MeshFilter mesh;
    };



    /// <summary>
    /// Spatial mapping depth resolution presets.
    /// </summary>
    public enum RESOLUTION
    {
        /// <summary>
        /// Create detailed geometry. Requires lots of memory.
        /// </summary>
        HIGH,
        /// <summary>
        /// Small variations in the geometry will disappear. Useful for large objects.
        /// </summary>
        ///
        MEDIUM,
        /// <summary>
        /// Keeps only large variations of the geometry. Useful for outdoors.
        /// </summary>
        LOW
    }

    /// <summary>
    ///  Spatial mapping depth range presets.
    /// </summary>
    public enum RANGE
    {
        /// <summary>
        /// Geometry within 3.5 meters of the camera will be mapped. 
        /// </summary>
        NEAR,
        /// <summary>
        /// Geometry within 5 meters of the camera will be mapped. 
        /// </summary>
        MEDIUM,
        /// <summary>
        /// Objects as far as 10 meters away are mapped. Useful for outdoors.
        /// </summary>
        FAR
    }

    /// <summary>
    /// Current instance of the ZED Camera.
    /// </summary>
    private sl.ZEDCamera zedCamera;

    /// <summary>
    /// Instance of an internal helper class for low-level mesh processing.
    /// </summary>
    private ZEDSpatialMappingHelper spatialMappingHelper;

    /// <summary>
    /// Amount of filtering to apply to the mesh. Higher values result in lower face counts/memory usage, but also lower precision. 
    /// </summary>
    public sl.FILTER filterParameters = sl.FILTER.MEDIUM;

    /// <summary>
    /// True when RequestSaveMesh has been called, so that ongoing threads know to stop and save the mesh
    /// when everything is finished processing. 
    /// </summary>
    private bool saveRequested = false;
    /// <summary>
    /// Where the new mesh will be saved. Should end in .obj.
    /// If textured, a .mtl (material) file and .png file will appear in the same folder with the same base filename. 
    /// </summary>
    private string savePath = "Assets/ZEDMesh.obj";

#if UNITY_EDITOR
    /// <summary>
    /// Color of the wireframe mesh to be drawn in Unity's Scene window. 
    /// </summary>
    private Color colorMesh = new Color(0.35f, 0.65f, 0.95f);
#endif

    /// <summary>
    /// Offset for the triangles buffer, so that new triangles are copied into the dynamic mesh starting at the correct index. 
    /// </summary>
    private int trianglesOffsetLastFrame;
    /// <summary>
    /// Offset for the vertices buffer, so that new vertices are copied into the dynamic mesh starting at the correct index. 
    /// </summary>
    private int verticesOffsetLastFrame;
    /// <summary>
    /// Offset for the UVs buffer, so that new UV coordinates are copied into the dynamic mesh starting at the correct index. 
    /// </summary>
    private int uvsOffsetLastFrame;
    /// <summary>
    /// Index of the mesh that was updated last frame. 
    /// </summary>
    private int indexLastFrame;
    /// <summary>
    /// Flag set to true if there were meshes what weren't completely updated last frame due to lack of time. 
    /// </summary>
    private bool remainMeshes = false;

    /// <summary>
    /// The user has requested to stop spatial mapping. 
    /// </summary>
    private bool stopWanted = false;

    /// <summary>
    /// Whether the mesh is in the filtering stage of processing. 
    /// </summary>
    private bool isFiltering = false;

    /// <summary>
    /// Whether the filtering stage of the mesh's processing has started and finished. 
    /// </summary>
    private bool isFilteringOver = false;

    /// <summary>
    /// Whether the update thread will stop running.
    /// </summary>
    private bool stopRunning = false;

    /// <summary>
    /// Whether any part of spatial mapping is running. Set to true when scanning has started
    /// and set to false after the scanned mesh has finished bring filtered, textured, etc. 
    /// </summary>
    private bool running = false;

    /// <summary>
    /// Flag that causes spatial mapping to pause when true. Use SwitchPauseState() to change. 
    /// </summary>
    private bool pause = false;

    /// <summary>
    /// Returns true if spatial mapping has been paused. This can be set to true even if spatial mapping isn't running. 
    /// </summary>
    public bool IsPaused
    {
        get { return pause; }
    }

    /// <summary>
    /// Whether scanned meshes are visible or not. 
    /// </summary>
    public bool display = false;

    /// <summary>
    /// State of the scanning during its initialization. Used to know if it has started successfully. 
    /// </summary>
    private sl.ERROR_CODE scanningInitState;

    /// <summary>
    /// Delegate for the OnMeshUpdate event, which is called every time a new chunk/submesh is processed. 
    /// </summary>
    public delegate void OnNewMesh();
    /// <summary>
    /// Events called every time a new chunk/submesh has been processed. It's called many times during the scan.
    /// </summary>
    public event OnNewMesh OnMeshUpdate;

    /// <summary>
    /// Delegate for OnMeshReady, which is called when spatial mapping has finished. 
    /// </summary>
    public delegate void OnSpatialMappingEnded();
    /// <summary>
    /// Event called when spatial mapping has finished. 
    /// </summary>
    public event OnSpatialMappingEnded OnMeshReady;

    /// <summary>
    /// Delegate for OnMeshStarted, which is called when spatial mapping has started. 
    /// </summary>
    public delegate void OnSpatialMappingStarted();
    /// <summary>
    /// Event called when spatial mapping has started.
    /// </summary>
    public event OnSpatialMappingStarted OnMeshStarted;

    /// <summary>
    /// GameObject to which every chunk of the mesh is parented. Represents the scanned mesh in Unity's Hierarchy. 
    /// </summary>
    private GameObject holder = null;

    /**** Threading Variables ****/
    /// <summary>
    /// True if the mesh has been updated, and needs to be processed.
    /// </summary>
    private bool meshUpdated = false;

    /// <summary>
    /// True if the mesh update thread is running.
    /// </summary>
    private bool updateThreadRunning = false;
    /// <summary>
    /// Public accessor for whether the mesh update thread is running. 
    /// </summary>
    public bool IsUpdateThreadRunning
    {
        get { return updateThreadRunning; }
    }

    /// <summary>
    /// True if the user has requested that spatial mapping start. 
    /// </summary>
    private bool spatialMappingRequested = false;

    /// <summary>
    /// True if the real-world texture needs to be updated. 
    /// This only happens after scanning is finished and if Texturing (isTextured) is enabled. 
    /// </summary>
    private bool updateTexture = false;

    /// <summary>
    /// True if the real-world texture has been updated. 
    /// </summary>
    private bool updatedTexture = false;

    /// <summary>
    /// Thread that retrieves the size of the submeshes.
    /// </summary>
    private Thread scanningThread;

    /// <summary>
    /// Thread that filters the mesh once scanning has finished. 
    /// </summary>
    private Thread filterThread;
    /// <summary>
    /// Mutex for threaded spatial mapping. 
    /// </summary>
    private object lockScanning = new object();

    /// <summary>
    /// Maximum time in milliseconds that can be spent processing retrieved meshes each frame. If time is exceeded, remaining meshes will be processed next frame. 
    /// </summary>
    private const int MAX_TIME = 5;

    /// <summary>
    /// True if the thread that updates the real-world texture is running. 
    /// </summary>
    private bool texturingRunning = false;

    /// <summary>
    /// Gravity direction vector relative to ZEDManager's orientation. Estimated after spatial mapping is finished. 
    /// Note that this will always be empty if using the ZED Mini as gravity is determined from its IMU at start. 
    /// </summary>
    public Vector3 gravityEstimation;

    /// <summary>
    /// Public accessor for texturingRunning, which is whether the thread that updates the real-world texture is running. 
    /// </summary>
    public bool IsTexturingRunning
    {
        get
        {
            return texturingRunning;
        }
    }
    /// <summary>
    /// If true, the script will add MeshColliders to all scanned chunks to allow physics collisions. 
    /// </summary>
    private bool hasColliders = true;

    /// <summary>
    /// True if texture from the real world should be applied to the mesh. If true, texture will be applied after scanning is finished. 
    /// </summary>
    private bool isTextured = false;

    /// <summary>
    /// Flag to check if we have attached ZEDMeshRenderer components to the ZED rig camera objects. 
    /// This is done in Update() if it hasn't been done yet. 
    /// </summary>
    private bool setMeshRenderer = false;

    /// <summary>
    /// References to the ZEDMeshRenderer components attached to the ZED rig camera objects. 
    /// [0] is the one attached to the left camera. [1] is the right camera, if it exists. 
    /// </summary>
    private ZEDMeshRenderer[] meshRenderer = new ZEDMeshRenderer[2];

    /// <summary>
    /// The scene's ZEDManager component, usually attached to the ZED rig GameObject (ZED_Rig_Mono or ZED_Rig_Stereo). 
    /// </summary>
    private ZEDManager zedManager;

    /// <summary>
    /// All chunks/submeshes with their indices. Only used while spatial mapping is running, as meshes are consolidated from 
    /// many small meshes into fewer, larger meshes when finished. See ChunkList for final submeshes. 
    /// </summary>
    public Dictionary<int, ZEDSpatialMapping.Chunk> Chunks
    {
        get { return spatialMappingHelper.chunks; }
    }
    /// <summary>
    /// List of the final mesh chunks created after scanning is finished. This is not filled beforehand because we use
    /// many small chunks during scanning, and consolidate them afterward. See Chunks for runtime submeshes. 
    /// </summary>
    public List<ZEDSpatialMapping.Chunk> ChunkList = new List<ZEDSpatialMapping.Chunk>();

    /// <summary>
    /// Constructor. Spawns the holder GameObject to hold scanned chunks and the ZEDSpatialMappingHelper to handle low-level mesh processing. 
    /// </summary>
    /// <param name="transform">Transform of the scene's ZEDSpatialMappingManager.</param>
    /// <param name="zedCamera">Reference to the ZEDCamera instance.</param>
    /// <param name="zedManager">The scene's ZEDManager component.</param>
    public ZEDSpatialMapping(Transform transform, ZEDManager zedManager)
    {
        //Instantiate the low-level mesh processing helper. 
        spatialMappingHelper = new ZEDSpatialMappingHelper(zedManager.zedCamera, Resources.Load("Materials/SpatialMapping/Mat_ZED_Texture") as Material, Resources.Load("Materials/SpatialMapping/Mat_ZED_Geometry_Wireframe") as Material);

        //Assign basic values. 
        this.zedCamera = zedManager.zedCamera;
        this.zedManager = zedManager;
        scanningInitState = sl.ERROR_CODE.FAILURE;


    }

    /// <summary>
    /// Begins the spatial mapping process. This is called when you press the "Start Spatial Mapping" button in the Inspector. 
    /// </summary>
    /// <param name="resolutionPreset">Resolution setting - how detailed the mesh should be at scan time.</param>
    /// <param name="rangePreset">Range setting - how close geometry must be to be scanned.</param>
    /// <param name="isTextured">Whether to scan texture, or only the geometry.</param>
    public void StartStatialMapping(sl.SPATIAL_MAP_TYPE type, RESOLUTION resolutionPreset, RANGE rangePreset, bool isTextured)
    {
        //Create the Holder object, to which all scanned chunks will be parented.
        holder = new GameObject();
        holder.name = "[ZED Mesh Holder (" + zedManager.name + ")]";
        holder.transform.position = Vector3.zero;
        holder.transform.rotation = Quaternion.identity;
        StaticBatchingUtility.Combine(holder);

        holder.transform.position = Vector3.zero;
        holder.transform.rotation = Quaternion.identity;
        spatialMappingRequested = true;
        if (spatialMappingRequested && scanningInitState != sl.ERROR_CODE.SUCCESS)
        {
            scanningInitState = EnableSpatialMapping(type, resolutionPreset, rangePreset, isTextured);
        }

        zedManager.gravityRotation = Quaternion.identity;

        pause = false; //Make sure the scanning doesn't start paused because it was left paused at the last scan. 

    }

    /// <summary>
    /// Initializes flags used during scan, tells ZEDSpatialMappingHelper to activate the ZED SDK's scanning, and 
    /// starts the thread that updates the in-game chunks with data from the ZED SDK. 
    /// </summary>
    /// <param name="resolutionPreset">Resolution setting - how detailed the mesh should be at scan time.</param>
    /// <param name="rangePreset">Range setting - how close geometry must be to be scanned.</param>
    /// <param name="isTextured">Whether to scan texture, or only the geometry.</param>
    /// <returns></returns>
    private sl.ERROR_CODE EnableSpatialMapping(sl.SPATIAL_MAP_TYPE type,RESOLUTION resolutionPreset, RANGE rangePreset, bool isTextured)
    {
        sl.ERROR_CODE error;
        this.isTextured = isTextured;

        //Tell the helper to start scanning. This call gets passed directly to the wrapper call in ZEDCamera. 
        error = spatialMappingHelper.EnableSpatialMapping(type,ZEDSpatialMappingHelper.ConvertResolutionPreset(resolutionPreset), ZEDSpatialMappingHelper.ConvertRangePreset(rangePreset), isTextured);
        if (meshRenderer[0]) meshRenderer[0].isTextured = isTextured;
        if (meshRenderer[1]) meshRenderer[1].isTextured = isTextured;
        stopWanted = false;
        running = true;

        if (error == sl.ERROR_CODE.SUCCESS) //If the scan was started successfully. 
        {
            //Set default flag settings.
            display = true;
            meshUpdated = false;
            spatialMappingRequested = false;
            updateTexture = false;
            updatedTexture = false;

            //Clear all previous meshes. 
            ClearMeshes();

            //Request the first mesh update. Later, this will get called continuously after each update is applied.
            zedCamera.RequestMesh();

            //Launch the thread to retrieve the chunks and their sizes from the ZED SDK. 
            scanningThread = new Thread(UpdateMesh);
            updateThreadRunning = true;
            if (OnMeshStarted != null)
            {
                OnMeshStarted(); //Invoke the event for other scripts, like ZEDMeshRenderer. 
            }
            scanningThread.Start();
        }
        return error;
    }

    /// <summary>
    /// Attach a new ZEDMeshRenderer to the ZED rig cameras. This is necessary to see the mesh. 
    /// </summary>
    public void SetMeshRenderer()
    {
        if (!setMeshRenderer) //Make sure we haven't do this yet. 
        {
            if (zedManager != null)
            {
                Transform left = zedManager.GetLeftCameraTransform(); //Find the left camera. This exists in both ZED_Rig_Mono and ZED_Rig_Stereo. 
                if (left != null)
                {
                    meshRenderer[0] = left.gameObject.GetComponent<ZEDMeshRenderer>();
                    if (!meshRenderer[0])
                    {
                        meshRenderer[0] = left.gameObject.AddComponent<ZEDMeshRenderer>();
                    }
                    meshRenderer[0].Create();
                }
                Transform right = zedManager.GetRightCameraTransform(); //Find the right camera. This only exists in ZED_Rig_Stereo or a similar stereo rig. 
                if (right != null)
                {
                    meshRenderer[1] = right.gameObject.GetComponent<ZEDMeshRenderer>();
                    if (!meshRenderer[1])
                    {
                        meshRenderer[1] = right.gameObject.AddComponent<ZEDMeshRenderer>();
                    }

                    meshRenderer[1].Create();
                }
                setMeshRenderer = true;
            }
        }
    }

    /// <summary>
    /// Updates the current mesh, if scanning, and manages the start and stop states.
    /// </summary>
    public void Update()
    {
        SetMeshRenderer(); //Make sure we have ZEDMeshRenderers on the cameras, so we can see the mesh. 

        if (meshUpdated || remainMeshes)
        {
            UpdateMeshMainthread();
            meshUpdated = false;
        }

        if (stopWanted && !remainMeshes)
        {
            stopRunning = true;
            stopWanted = false;
            Stop();

        }

        //If it's time to stop the scan, disable the spatial mapping and store the gravity estimation in ZEDManager.
        if (stopRunning && !isFiltering && isFilteringOver)
        {
            isFilteringOver = false;
            UpdateMeshMainthread(false);

            Thread disabling = new Thread(DisableSpatialMapping);
            disabling.Start();
            if (hasColliders)
            {
                if (!zedManager.IsStereoRig && gravityEstimation != Vector3.zero && zedManager.transform.parent != null)
                {
                    Quaternion rotationToApplyForGravity = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.up, -gravityEstimation.normalized));
                    holder.transform.localRotation = rotationToApplyForGravity;

                    zedManager.gravityRotation = rotationToApplyForGravity;
                }

                UpdateMeshCollider();
            }
            else
            {
                running = false;
            }
            stopRunning = false;
        }
    }


    /// <summary>
    /// Gets the mesh data from the ZED SDK and stores it for later update in the Unity mesh.
    /// </summary>
    private void UpdateMesh()
    {
        while (updateThreadRunning)
        {
            if (!remainMeshes) //If we don't have leftover meshes to apply from the last update. 
            {
                lock (lockScanning)
                {
                    if (meshUpdated == false && updateTexture) //If we need to update the texture, prioritize that. 
                    {
                        //Get the last size of the mesh and get the texture size.
                        spatialMappingHelper.ApplyTexture();
                        meshUpdated = true;
                        updateTexture = false;
                        updatedTexture = true;
                        updateThreadRunning = false;
                    }
                    else if (zedCamera.GetMeshRequestStatus() == sl.ERROR_CODE.SUCCESS && !pause && meshUpdated == false)
                    {
                        spatialMappingHelper.UpdateMesh(); //Tells the ZED SDK to update its internal mesh.
                        spatialMappingHelper.RetrieveMesh(); //Applies the ZED SDK's internal mesh to values inside spatialMappingHelper. 
                        meshUpdated = true;
                    }
                }
                //Time to process all the meshes spread on multiple frames.
                Thread.Sleep(5);
            }
            else //If there are meshes that were collected but not processed yet. Happens if the last update took too long to process. 
            {
                //Check every 5ms if the meshes are done being processed. 
                Thread.Sleep(5);
            }

        }
    }

    /// <summary>
    /// Destroys all submeshes. 
    /// </summary>
    private void ClearMeshes()
    {
        if (holder != null)
        {
            foreach (Transform child in holder.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            spatialMappingHelper.Clear();
        }
    }

    /// <summary>
    /// Measures time since the provided start time. Used in UpdateMeshMainthread() to check if computational time for mesh updates
    /// has exceeded the MAX_TIME time limit (usually 5ms), so that it can hold off processing remaining meshes until the next frame.
    /// </summary>
    /// <param name="startTimeMS">Time.realtimeSinceStartup value when the process began.</param>
    /// <returns><c>True</c> if more than MAX_TIME has elapsed since startTimeMS.</returns>
    private bool GoneOverTimeBudget(int startTimeMS)
    {
        return (Time.realtimeSinceStartup * 1000) - startTimeMS > MAX_TIME;
    }


    /// <summary>
    /// Update the Unity mesh with the last data retrieved from the ZED, creating a new submesh if needed.
    /// Also launches the OnMeshUpdate event when the update is finished.
    /// <param name="spreadUpdateOverTime">If <c>true</c>, caps time spent on updating meshes each frame, leaving 'leftover' meshes for the next frame.</param>
    /// </summary>
    private void UpdateMeshMainthread(bool spreadUpdateOverTime = true)
    {
        //Cache the start time so we can measure how long this function is taking.
        //We'll check when updating the submeshes so that if it takes too long, we'll stop updating until the next frame. 
        int startTimeMS = (int)(Time.realtimeSinceStartup * 1000);
        int indexUpdate = 0;
        lock (lockScanning) //Don't update if another thread is accessing. 
        {
            if (updatedTexture)
            {
                spreadUpdateOverTime = false;
            }

            //Set the offset of the buffers to the offset of the last frame.
            int verticesOffset = 0, trianglesOffset = 0, uvsOffset = 0;
            if (remainMeshes && spreadUpdateOverTime)
            {
                verticesOffset = verticesOffsetLastFrame;
                trianglesOffset = trianglesOffsetLastFrame;
                uvsOffset = uvsOffsetLastFrame;
                indexUpdate = indexLastFrame;
            }

            //Clear all existing meshes and process the last ones.
            if (updatedTexture)
            {
                ClearMeshes();
                spatialMappingHelper.SetMeshAndTexture();
                if (meshRenderer[0]) meshRenderer[0].isTextured = isTextured;
                if (meshRenderer[1]) meshRenderer[1].isTextured = isTextured;

            }

            //Process the last meshes.
            for (; indexUpdate < spatialMappingHelper.NumberUpdatedSubMesh; indexUpdate++)
            {

                spatialMappingHelper.SetMesh(indexUpdate, ref verticesOffset, ref trianglesOffset, ref uvsOffset, holder.transform, updatedTexture);
                if (spreadUpdateOverTime && GoneOverTimeBudget(startTimeMS)) //Check if it's taken too long this frame. 
                {
                    remainMeshes = true; //It has. Set this flag so we know to pick up where we left off next frame. 
                    break;
                }
            }
            if (spreadUpdateOverTime)
            {
                indexLastFrame = indexUpdate;
            }
            else
            {
                indexLastFrame = 0;
            }


            //If all the meshes have been updated, reset values used to process 'leftover' meshes and get more data from the ZED.
            if ((indexUpdate == spatialMappingHelper.NumberUpdatedSubMesh) || spatialMappingHelper.NumberUpdatedSubMesh == 0)
            {
                verticesOffsetLastFrame = 0;
                trianglesOffsetLastFrame = 0;
                uvsOffsetLastFrame = 0;
                indexLastFrame = 0;
                remainMeshes = false;
                meshUpdated = false;
                zedCamera.RequestMesh();
            }
            //If some meshes still need updating, we'll save the offsets so we know where to start next frame. 
            else if (indexUpdate != spatialMappingHelper.NumberUpdatedSubMesh)
            {
                remainMeshes = true;
                indexLastFrame = indexUpdate + 1;

                verticesOffsetLastFrame = verticesOffset;
                trianglesOffsetLastFrame = trianglesOffset;
                uvsOffsetLastFrame = uvsOffset;
            }
            //Save the mesh here if we requested it to be saved, as we just updated the meshes, including textures, if applicable. 
            if (saveRequested && remainMeshes == false)
            {
                if (!isTextured || updatedTexture)
                {
                    SaveMeshNow(savePath);
                    saveRequested = false;
                }
            }
        }


        if (OnMeshUpdate != null)
        {
            OnMeshUpdate(); //Call the event if it has at least one listener. 
        }

        //The texture update is done in one pass, so this is only called once after the mesh has stopped scanning. 
        if (updatedTexture)
        {
            DisableSpatialMapping();
            updatedTexture = false;
            running = false;
            texturingRunning = false;
            if (hasColliders)
            {
                UpdateMeshCollider();
            }
            else
            {
                running = false;
            }
        }
    }


    public void ClearAllMeshes()
    {

        GameObject[] gos = GameObject.FindObjectsOfType<GameObject>() as GameObject[];

        spatialMappingHelper.Clear();
        for (int i = 0; i < gos.Length; i++)
        {
            string targetName = "[ZED Mesh Holder (" + zedManager.name + ")]";
            if (gos[i] != null && gos[i].name.Contains(targetName))
            {
                GameObject.Destroy(gos[i]);
            }
        }
    }

    /// <summary>
    /// Changes the visibility state of the meshes. 
    /// This is what's called when the Hide/Display Mesh button is clicked in the Inspector. 
    /// </summary>
    /// <param name="newDisplayState"> If true, the mesh will be displayed, else it will be hide. </param>
    public void SwitchDisplayMeshState(bool newDisplayState)
    {
        display = newDisplayState;
    }

    /// <summary>
    /// Pauses or resumes spatial mapping. If the spatial mapping is not enabled, nothing will happen.
    /// </summary>
    /// <param name="newPauseState"> If true, the spatial mapping will be paused, else it will be resumed. </param>
    public void SwitchPauseState(bool newPauseState)
    {
        pause = newPauseState;
        zedCamera.PauseSpatialMapping(newPauseState);
    }

    /// <summary>
    /// Update the mesh collider with the current mesh so it can handle physics.
    /// Calling it is slow, so it's only called after a scan is finished (or loaded). 
    /// </summary>
    public void UpdateMeshCollider(bool timeSlicing = false)
    {
        ChunkList.Clear();
        foreach (var submesh in Chunks)
        {
            ChunkList.Add(submesh.Value);
        }
        lock (lockScanning)
        {
            spatialMappingHelper.UpdateMeshCollider(ChunkList);
        }
        if (OnMeshReady != null)
        {
            OnMeshReady();
        }
        running = false;
    }

    /// <summary>
    /// Properly clears existing scan data when the application is closed. 
    /// Called by OnApplicationQuit() when the application closes. 
    /// </summary>
    public void Dispose()
    {
        if (scanningThread != null)
        {
            updateThreadRunning = false;
            scanningThread.Join();
        }
        ClearMeshes();
        GameObject.Destroy(holder);
        DisableSpatialMapping();
    }

    /// <summary>
    /// Disable the ZED's spatial mapping. The mesh will no longer be updated, but it is not deleted.
    /// This gets called in Update() if the user requested a stop, and will execute once the scanning thread is free. 
    /// </summary>
    private void DisableSpatialMapping()
    {
        lock (lockScanning)
        {
            updateThreadRunning = false;
            spatialMappingHelper.DisableSpatialMapping();
            scanningInitState = sl.ERROR_CODE.FAILURE;
            spatialMappingRequested = false;

        }
    }

    /// <summary>
    /// Save the mesh as an .obj file, and the area database as an .area file. 
    /// This can be quite time-comsuming if you mapped a large area.
    /// </summary>
    public void RequestSaveMesh(string meshFilePath = "Assets/ZEDMesh.obj")
    {
        saveRequested = true;
        savePath = meshFilePath;

        if (updateThreadRunning)
        {
            StopStatialMapping(); //Stop the mapping if it hasn't stopped already. 
        }

    }

    /// <summary>
    /// Loads the mesh and the corresponding area file if it exists. It can be quite time-comsuming if you mapped a large area.
    /// Note that if there are no .area files found in the same folder, the mesh will not be loaded either.
    /// Loading a mesh this way also loads relevant data into buffers, so it's as if a scan was just finished
    /// rather than a mesh asset being dropped into Unity. 
    /// <returns><c>True</c> if loaded successfully, otherwise <c>flase</c>.</returns>
    /// </summary>
    public bool LoadMesh(string meshFilePath = "ZEDMesh.obj")
    {
        if (holder == null)
        {
            holder = new GameObject();
            holder.name = "[ZED Mesh Holder (" + zedManager.name + ")]";
            holder.transform.position = Vector3.zero;
            holder.transform.rotation = Quaternion.identity;
            StaticBatchingUtility.Combine(holder);
        }


        if (OnMeshStarted != null)
        {
            OnMeshStarted();
        }
        //If spatial mapping has started, disable it.
        DisableSpatialMapping();

        //Find and load the area
        string basePath = meshFilePath.Substring(0, meshFilePath.LastIndexOf("."));
        if (!System.IO.File.Exists(basePath + ".area"))
        {
            Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_BASE_AREA_NOT_FOUND));
        }

        zedCamera.DisableTracking();
        Quaternion quat = Quaternion.identity; Vector3 tr = Vector3.zero;
		if (zedCamera.EnableTracking(ref quat, ref tr, true,false,false, false, true, System.IO.File.Exists(basePath + ".area") ? basePath + ".area" : "") != sl.ERROR_CODE.SUCCESS)
        {
            Debug.LogWarning(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
        }

        updateTexture = false;
        updatedTexture = false;
        bool meshUpdatedLoad = false;
        lock (lockScanning)
        {
            ClearMeshes();
            meshUpdatedLoad = spatialMappingHelper.LoadMesh(meshFilePath);
            if (meshUpdatedLoad)
            {
                //Checks if a texture exists.
                if (spatialMappingHelper.GetWidthTexture() != -1)
                {
                    updateTexture = true;
                    updatedTexture = true;
                }

                //Retrieves the mesh sizes to be updated in the Unity's buffer later.
                if (!updateTexture)
                {
                    spatialMappingHelper.RetrieveMesh();
                }
            }
        }

        if (meshUpdatedLoad)
        {
            //Update the buffer on Unity's side.
            UpdateMeshMainthread(false);

            //Add colliders and scan for gravity.
            if (hasColliders)
            {
                if (!zedManager.IsStereoRig && gravityEstimation != Vector3.zero)
                {
                    Quaternion rotationToApplyForGravity = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.up, -gravityEstimation.normalized));
                    holder.transform.rotation = rotationToApplyForGravity;
                    zedManager.gravityRotation = rotationToApplyForGravity;
                }
                UpdateMeshCollider();
                foreach (Chunk c in ChunkList)
                {
                    c.o.transform.localRotation = Quaternion.identity;
                }

            }

            if (OnMeshReady != null)
            {
                OnMeshReady(); //Call the event if it has at least one listener. 
            }

            if (meshRenderer[0]) meshRenderer[0].UpdateRenderingPlane(true);
            if (meshRenderer[1]) meshRenderer[1].UpdateRenderingPlane(true);


            return true;
        }
        return false;
    }

    /// <summary>
    /// Filters the mesh with the current filtering parameters.
    /// This reduces the total number of faces. More filtering means fewer faces. 
    /// </summary>
    public void FilterMesh()
    {
        lock (lockScanning) //Wait for the thread to be available. 
        {
            spatialMappingHelper.FilterMesh(filterParameters);
            spatialMappingHelper.ResizeMesh();
            spatialMappingHelper.RetrieveMesh();
            meshUpdated = true;
        }
    }

    /// <summary>
    /// Begin mesh filtering, and consolidate chunks into a reasonably low number when finished. 
    /// </summary>
    /// <param name="filter"></param>
    void PostProcessMesh(bool filter = true)
    {
        isFiltering = true;
        if (filter)
        {
            FilterMesh();
        }
        MergeChunks();
    }

    /// <summary>
    /// Consolidates meshes to get fewer chunks - one for every MAX_SUBMESH vertices. Then applies to
    /// actual meshes in Unity. 
    /// </summary>
    public void MergeChunks()
    {
        lock (lockScanning)
        {
            spatialMappingHelper.MergeChunks();
            spatialMappingHelper.ResizeMesh();
            spatialMappingHelper.RetrieveMesh();
            meshUpdated = true;
        }
        isFiltering = false;
        isFilteringOver = true;
    }

    /// <summary>
    /// Multi-threaded component of ApplyTexture(). Filters, then updates the mesh once, but as
    /// updateTexture is set to true when this is called, UpdateMesh() will also handle applying the texture. 
    /// </summary>
    void ApplyTextureThreaded()
    {
        FilterMesh();
        UpdateMesh();
    }


    /// <summary>
    /// Stops the spatial mapping and begins the final processing, including adding texture. 
    /// </summary>
    public bool ApplyTexture()
    {
        updateTexture = true;
        if (updateThreadRunning)
        {
            updateThreadRunning = false;
            scanningThread.Join();
        }
        scanningThread = new Thread(ApplyTextureThreaded);
        updateThreadRunning = true;
        scanningThread.Start();
        texturingRunning = true;
        return true;
    }

    /// <summary>
    /// Stop the spatial mapping and calls appropriate functions to process the final mesh. 
    /// </summary>
    private void Stop()
    {
        gravityEstimation = zedCamera.GetGravityEstimate();

        if (isTextured)
        {
            ApplyTexture();
        }
        else
        {
            stopRunning = false;
            if (updateThreadRunning)
            {
                updateThreadRunning = false;
                scanningThread.Join();
            }

            ClearMeshes();
            PostProcessMesh(true);
            //filterThread = new Thread(() => PostProcessMesh(true));
            //filterThread.Start();
            stopRunning = true;
        }
        SwitchDisplayMeshState(true); //Make it default to visible. 
    }

    /// <summary>
    /// Returns true from the moment a scan has started until the post-process is finished.
    /// </summary>
    /// <returns></returns>
    public bool IsRunning()
    {
        return running;
    }

    /// <summary>
    /// Sets a flag that will cause spatial mapping to stop at the next Update() call after all meshes already retrieved from the ZED are applied.
    /// </summary>
    public void StopStatialMapping()
    {
        stopWanted = true;
    }

    /// <summary>
    /// Combines the meshes from all the current chunks and saves them into a single mesh. If textured, 
    /// will also save a .mtl file and .png file. 
    /// This must only be called once all the chunks are completely finalized, or else they won't be filtered
    /// or have their UVs set. 
    /// Called after RequestSaveMesh has been called after the main thread has the chance to stop the scan
    /// and finalize everything.
    /// </summary>
    /// <param name="meshFilePath">Where the mesh, material, and texture files will be saved.</param>
    private void SaveMeshNow(string meshFilePath = "Assets/ZEDMesh.obj")
    {
        // Make sure we are in invariant culture to get . notation for decimals.
        CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture; // Save the old culture to set it back once we are done
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        //Make sure the destination file ends in .obj - only .obj file format is supported. 
        string extension = meshFilePath.Substring(meshFilePath.Length - 4);
        if (extension.ToLower() != ".obj")
        {
            Debug.LogError("Couldn't save to " + meshFilePath + ": Must save as .obj.");
        }

        lock (lockScanning)
        {
            Debug.Log("Saving mesh to " + meshFilePath);
            //Count how many vertices and triangles are in all the chunk meshes so we know how large of an array to allocate. 
            int vertcount = 0;
            int tricount = 0;

            foreach (Chunk chunk in Chunks.Values)
            {
                vertcount += chunk.mesh.vertices.Length;
                tricount += chunk.mesh.triangles.Length;
            }
            if (vertcount == 0) return;

            Vector3[] vertices = new Vector3[vertcount];
            Vector2[] uvs = new Vector2[vertcount];
            Vector3[] normals = new Vector3[vertcount];
            int[] triangles = new int[tricount];

            int vertssofar = 0; //We keep an ongoing tally of how many verts/tris we've put so far so as to increment
            int trissofar = 0; //where we copy to in the arrays, and also to increment the vertex indices in the triangle array. 

            for (int i = 0; i < Chunks.Keys.Count; i++)
            {
                Mesh chunkmesh = Chunks[i].mesh; //Shorthand.

                Array.Copy(chunkmesh.vertices, 0, vertices, vertssofar, chunkmesh.vertices.Length);
                Array.Copy(chunkmesh.uv, 0, uvs, vertssofar, chunkmesh.uv.Length);

                chunkmesh.RecalculateNormals();
                Array.Copy(chunkmesh.normals, 0, normals, vertssofar, chunkmesh.normals.Length);

                Array.Copy(chunkmesh.triangles, 0, triangles, trissofar, chunkmesh.triangles.Length);
                for (int t = trissofar; t < trissofar + chunkmesh.triangles.Length; t++)
                {
                    triangles[t] += vertssofar;
                }

                vertssofar += chunkmesh.vertices.Length;
                trissofar += chunkmesh.triangles.Length;
            }

            Material savemat = Chunks[0].o.GetComponent<MeshRenderer>().material; //All chunks share the same material.

            //We'll need to know the base file name for this and the .mtl file. We'll extract it. 
            //Since both forward and backslashes are valid for the file pack, determine which they used last. 
            int forwardindex = meshFilePath.LastIndexOf('/');
            int backindex = meshFilePath.LastIndexOf('\\');
            int slashindex = forwardindex > backindex ? forwardindex : backindex;
            string basefilename = meshFilePath.Substring(slashindex + 1, meshFilePath.LastIndexOf(".") - slashindex - 1);

            //Create the string file. 
            //Importantly, we flip the X value (and reverse the triangles) since the scanning module uses a different handedness than Unity. 
            StringBuilder objstring = new StringBuilder();
            objstring.Append("## Mesh generated by ZED\n");

            if (isTextured)
                objstring.Append("mtllib " + basefilename + ".mtl"+ "\n");

            foreach (Vector3 vec in vertices)
            {
                //X is flipped because of Unity's handedness. 
                objstring.Append(string.Format("v {0} {1} {2}\n", -vec.x, vec.y, vec.z)); 
            }
            objstring.Append("\n");
            foreach (Vector2 uv in uvs)
            {
                objstring.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
            }
            objstring.Append("\n");
            foreach (Vector3 norm in normals)
            {
                objstring.Append(string.Format("vn {0} {1} {2}\n", -norm.x, norm.y, norm.z));
            }
            objstring.Append("\n");

            if (isTextured)
            {
                objstring.Append("usemtl ").Append(basefilename).Append("\n");
                objstring.Append("usemap ").Append(basefilename).Append("\n");
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                //Triangles are reversed so that surface normals face the right way after the X vertex flip. 
                objstring.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i + 2] + 1, triangles[i + 1] + 1, triangles[i + 0] + 1));
            }


            System.IO.StreamWriter swriter = new System.IO.StreamWriter(meshFilePath);
            swriter.Write(objstring.ToString());
            swriter.Close();

            //Create a texture and .mtl file for your scan, if textured. 
            if (isTextured)
            {
                //First, the texture. 
                //You can't save a Texture2D directly to a file since it's stored on the GPU. 
                //So we use a RenderTexture as a buffer, which we can read into a new Texture2D on the CPU-side. 
                Texture textosave = savemat.mainTexture;

                RenderTexture buffertex = new RenderTexture(textosave.width, textosave.height, 0);
                Graphics.Blit(textosave, buffertex);

                RenderTexture oldactivert = RenderTexture.active;
                RenderTexture.active = buffertex;

                Texture2D texcopy = new Texture2D(textosave.width, textosave.height);
                texcopy.ReadPixels(new Rect(0, 0, buffertex.width, buffertex.height), 0, 0); 
                texcopy.Apply(); //It's now on the CPU! 

                byte[] imagebytes = texcopy.EncodeToPNG();
                string imagepath = meshFilePath.Substring(0, meshFilePath.LastIndexOf(".")) + ".png";
                System.IO.File.WriteAllBytes(imagepath, imagebytes);

                RenderTexture.active = oldactivert;

                //Now the material file. 
                StringBuilder mtlstring = new StringBuilder();

                mtlstring.Append("newmtl " + basefilename + "\n");

                mtlstring.Append("Ka 1.000000 1.000000 1.000000\n");
                mtlstring.Append("Kd 1.000000 1.000000 1.000000\n");
                mtlstring.Append("Ks 0.000000 0.000000 0.000000\n");
                mtlstring.Append("Tr 1.000000\n");
                mtlstring.Append("illum 1\n");
                mtlstring.Append("Ns 1.000000\n");
                mtlstring.Append("map_Kd " + basefilename + ".png");

                string mtlpath = meshFilePath.Substring(0, meshFilePath.LastIndexOf(".")) + ".mtl";
                swriter = new System.IO.StreamWriter(mtlpath);
                swriter.Write(mtlstring.ToString());
                swriter.Close();
            }
        }

        //Save the .area file for spatial memory. 
        string areaName = meshFilePath.Substring(0, meshFilePath.LastIndexOf(".")) + ".area";
        zedCamera.SaveCurrentArea(areaName);
        Thread.CurrentThread.CurrentCulture = oldCulture;
    }

    /// <summary>
    /// Used by Unity to draw the meshes in the editor with a double pass shader. 
    /// </summary>
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = colorMesh;

        if (!IsRunning() && spatialMappingHelper != null && spatialMappingHelper.chunks.Count != 0 && display)
        {
            foreach (var submesh in spatialMappingHelper.chunks)
            {
                if (submesh.Value.proceduralMesh.mesh != null)
                {
                    Gizmos.DrawWireMesh(submesh.Value.proceduralMesh.mesh.sharedMesh, submesh.Value.o.transform.position, submesh.Value.o.transform.rotation);
                }
            }
        }
    }
#endif


    /// <summary>
    /// Low-level spatial mapping class. Calls SDK wrapper functions to get mesh data and applies it to Unity meshes. 
    /// Functions are usually called from ZEDSpatialMapping, but buffer data is held within. 
    /// Note that some values are updated directly from the ZED wrapper dll, so such assignments aren't visible in the plugin. 
    /// </summary>
    private class ZEDSpatialMappingHelper
    {
        /// <summary>
        /// Reference to the ZEDCamera instance. Used to call SDK functions. 
        /// </summary>
        private sl.ZEDCamera zedCamera;
        /// <summary>
        /// Maximum number of chunks. It's best to get relatively few chunks and to update them quickly.
        /// </summary>
        private const int MAX_SUBMESH = 1000;

        /*** Number of vertices/triangles/indices per chunk***/
        /// <summary>
        /// Total vertices in each chunk/submesh. 
        /// </summary>
        private int[] numVerticesInSubmesh = new int[MAX_SUBMESH];
        /// <summary>
        /// Total triangles in each chunk/submesh. 
        /// </summary>
        private int[] numTrianglesInSubmesh = new int[MAX_SUBMESH];
        /// <summary>
        /// Total indices per chunk/submesh. 
        /// </summary>
        private int[] UpdatedIndices = new int[MAX_SUBMESH];

        /*** Number of vertices/uvs/indices at the moment**/
        /// <summary>
        /// Vertex count in current submesh. 
        /// </summary>
        private int numVertices = 0;
        /// <summary>
        /// Triangle point counds in current submesh. (Every three values are the indexes of the three vertexes that make up one triangle)
        /// </summary>
        private int numTriangles = 0;
        /// <summary>
        /// How many submeshes were updated. 
        /// </summary>
        private int numUpdatedSubmesh = 0;

        /*** The current data in the current submesh***/
        /// <summary>
        /// Vertices of the current submesh. 
        /// </summary>
        private Vector3[] vertices;
        /// <summary>
        /// UVs of the current submesh. 
        /// </summary>
        private Vector2[] uvs;
        /// <summary>
        /// Triangles of the current submesh. (Each int refers to the index of a vertex)
        /// </summary>
        private int[] triangles;
        /// <summary>
        /// Width and height of the mesh texture, if any. 
        /// </summary>
        private int[] texturesSize = new int[2];

        /// <summary>
        /// Dictionary of all existing chunks. 
        /// </summary>
        public Dictionary<int, ZEDSpatialMapping.Chunk> chunks = new Dictionary<int, ZEDSpatialMapping.Chunk>(MAX_SUBMESH);
        /// <summary>
        /// Material with real-world texture, applied to the mesh when Texturing (isTextured) is enabled. 
        /// </summary>
        private Material materialTexture;
        /// <summary>
        /// Material used to draw the mesh. Applied to chunks during the scan, and replaced with materialTexture 
        /// only if Texturing (isTextured) is enabled. 
        /// </summary>
        private Material materialMesh;

        /// <summary>
        /// Public accessor for the number of chunks that have been updated. 
        /// </summary>
        public int NumberUpdatedSubMesh
        {
            get { return numUpdatedSubmesh; }
        }

        /// <summary>
        /// Gets the material used to draw spatial mapping meshes without real-world textures. 
        /// </summary>
        /// <returns></returns>
        public Material GetMaterialSpatialMapping()
        {
            return materialMesh;
        }

        /// <summary>
        /// Constructor. Gets the ZEDCamera instance and sets materials used on the meshes. 
        /// </summary>
        /// <param name="materialTexture"></param>
        /// <param name="materialMesh"></param>
		public ZEDSpatialMappingHelper(sl.ZEDCamera camera, Material materialTexture, Material materialMesh)
        {
            zedCamera = camera;
            this.materialTexture = materialTexture;
            this.materialMesh = materialMesh;
        }

        /// <summary>
        /// Updates the range to match the specified preset.
        /// </summary>
        static public float ConvertRangePreset(RANGE rangePreset)
        {

            if (rangePreset == RANGE.NEAR)
            {
                return 3.5f;
            }
            else if (rangePreset == RANGE.MEDIUM)
            {
                return 5.0f;
            }
            if (rangePreset == RANGE.FAR)
            {
                return 10.0f;
            }
            return 5.0f;
        }

        /// <summary>
        /// Updates the resolution to match the specified preset.
        /// </summary>
        static public float ConvertResolutionPreset(RESOLUTION resolutionPreset)
        {
            if (resolutionPreset == RESOLUTION.HIGH)
            {
                return 0.05f;
            }
            else if (resolutionPreset == RESOLUTION.MEDIUM)
            {
                return 0.10f;
            }
            if (resolutionPreset == RESOLUTION.LOW)
            {
                return 0.15f;
            }
            return 0.10f;
        }

        /// <summary>
        /// Tells the ZED SDK to begin spatial mapping. 
        /// </summary>
        /// <returns></returns>

        public sl.ERROR_CODE EnableSpatialMapping(sl.SPATIAL_MAP_TYPE type,float resolutionMeter, float maxRangeMeter, bool saveTexture)
        {
            return zedCamera.EnableSpatialMapping(type,resolutionMeter, maxRangeMeter, saveTexture);
        }

        /// <summary>
        /// Tells the ZED SDK to stop spatial mapping. 
        /// </summary>
        public void DisableSpatialMapping()
        {
            zedCamera.DisableSpatialMapping();
        }

        /// <summary>
        /// Create a new submesh to contain the data retrieved from the ZED.
        /// </summary>
        public ZEDSpatialMapping.Chunk CreateNewMesh(int i, Material meshMat, Transform holder)
        {
            //Initialize the chunk and create a GameObject for it. 
            ZEDSpatialMapping.Chunk chunk = new ZEDSpatialMapping.Chunk();
            chunk.o = GameObject.CreatePrimitive(PrimitiveType.Quad);
            chunk.o.layer = zedCamera.TagInvisibleToZED;
            chunk.o.GetComponent<MeshCollider>().sharedMesh = null;
            chunk.o.name = "Chunk" + chunks.Count;
            chunk.o.transform.localPosition = Vector3.zero;
            chunk.o.transform.localRotation = Quaternion.identity;

            Mesh m = new Mesh();
            m.MarkDynamic(); //Allows it to be updated regularly without performance issues. 
            chunk.mesh = m;

            //Set graphics settings to not treat the chunk like a physical object (no shadows, no reflections, no lights, etc.).
            MeshRenderer meshRenderer = chunk.o.GetComponent<MeshRenderer>();
            meshRenderer.material = meshMat;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = true;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            //Sets the position and parent of the chunk.
            chunk.o.transform.parent = holder;
            chunk.o.layer = zedCamera.TagInvisibleToZED;

            //Add the chunk to the dictionary.
            chunk.proceduralMesh.mesh = chunk.o.GetComponent<MeshFilter>();
            chunks.Add(i, chunk);
            return chunk;
        }

        /// <summary>
        /// Adds a MeshCollider to each chunk for physics. This is time-consuming, so it's only called
        /// once scanning is finished and the final mesh is being processed. 
        /// </summary>
        public void UpdateMeshCollider(List<ZEDSpatialMapping.Chunk> listMeshes, int startIndex = 0)
        {
            List<int> idsToDestroy = new List<int>(); //List of meshes that are too small for colliders and will be destroyed. 

            //Update each mesh with a collider.
            for (int i = startIndex; i < listMeshes.Count; ++i)
            {
                var submesh = listMeshes[i];
                MeshCollider m = submesh.o.GetComponent<MeshCollider>();

                if (m == null)
                {
                    m = submesh.o.AddComponent<MeshCollider>();
                }

                //If a mesh has 2 or fewer vertices, it's useless, so queue it up to be destroyed. 
                Mesh tempMesh = submesh.o.GetComponent<MeshFilter>().sharedMesh;
                if (tempMesh.vertexCount < 3)
                {
                    idsToDestroy.Add(i);
                    continue;
                }

                m.sharedMesh = tempMesh;

                m.sharedMesh.RecalculateNormals();
                m.sharedMesh.RecalculateBounds();
            }

            //Destroy all useless meshes now that we've iterated through all the meshes. 
            for (int i = 0; i < idsToDestroy.Count; ++i)
            {
                GameObject.Destroy(chunks[idsToDestroy[i]].o);
                chunks.Remove(idsToDestroy[i]);
            }
            Clear(); //Clear the buffer data now that we have Unity meshes. 
        }

        /// <summary>
        /// Tells the ZED SDK to calculate the size of the texture and the UVs.
        /// </summary>
        public void ApplyTexture()
        {
            zedCamera.ApplyTexture(numVerticesInSubmesh, numTrianglesInSubmesh, ref numUpdatedSubmesh, UpdatedIndices, ref numVertices, ref numTriangles, texturesSize, MAX_SUBMESH);
        }

        /// <summary>
        /// Tells the ZED SDK to update its internal mesh from spatial mapping. The resulting mesh will later be retrieved with RetrieveMesh(). 
        /// </summary>
        public void UpdateMesh()
        {
            zedCamera.UpdateMesh(numVerticesInSubmesh, numTrianglesInSubmesh, ref numUpdatedSubmesh, UpdatedIndices, ref numVertices, ref numTriangles, MAX_SUBMESH);
            ResizeMesh();
        }

        /// <summary>
        /// Retrieves the mesh vertices and triangles from the ZED SDK. This must be called after UpdateMesh() has been called. 
        /// Note that the actual assignment to vertices and triangles happens from within the wrapper .dll via pointers, not a C# script. 
        /// </summary>
        public void RetrieveMesh()
        {
            zedCamera.RetrieveMesh(vertices, triangles, MAX_SUBMESH, null, System.IntPtr.Zero);
        }

        /// <summary>
        /// Clear the current buffer data.
        /// </summary>
        public void Clear()
        {
            chunks.Clear();

            vertices = new Vector3[0];
            triangles = new int[0];
            uvs = new Vector2[0];

            System.Array.Clear(vertices, 0, vertices.Length);
            System.Array.Clear(triangles, 0, triangles.Length);
        }

        /// <summary>
        /// Process data from a submesh retrieved from the ZED SDK into a chunk, which includes a GameObject and visible mesh. 
        /// </summary>
        /// <param name="indexUpdate">Index of the submesh/chunk to be updated.</param>
        /// <param name="verticesOffset">Starting index in the vertices stack.</param>
        /// <param name="trianglesOffset">Starting index in the triangles stack.</param>
        /// <param name="uvsOffset">Starting index in the UVs stack.</param>
        /// <param name="transform">Transform of the holder object to which all chunks are parented.</param>
        /// <param name="updatedTex"><c>True</c> if the world texture has been updated so we know to update UVs.</param>
        public void SetMesh(int indexUpdate, ref int verticesOffset, ref int trianglesOffset, ref int uvsOffset, Transform holder, bool updatedTex)
        {
            ZEDSpatialMapping.Chunk subMesh;
            int updatedIndex = UpdatedIndices[indexUpdate];
            if (!chunks.TryGetValue(updatedIndex, out subMesh)) //Use the existing chunk/submesh if already in the dictionary. Otherwise, make a new one. 
            {
                subMesh = CreateNewMesh(updatedIndex, materialMesh, holder);
            }

            Mesh currentMesh = subMesh.mesh;
            ZEDSpatialMapping.ProceduralMesh dynamicMesh = subMesh.proceduralMesh;
            //If the dynamicMesh's triangle and vertex arrays are unassigned or are the wrong size, redo the array. 
            if (dynamicMesh.triangles == null || dynamicMesh.triangles.Length != 3 * numTrianglesInSubmesh[indexUpdate])
            {
                dynamicMesh.triangles = new int[3 * numTrianglesInSubmesh[indexUpdate]];
            }
            if (dynamicMesh.vertices == null || dynamicMesh.vertices.Length != numVerticesInSubmesh[indexUpdate])
            {
                dynamicMesh.vertices = new Vector3[numVerticesInSubmesh[indexUpdate]];
            }

            //Clear the old mesh data. 
            currentMesh.Clear();

            //Copy data retrieved from the ZED SDK into the ProceduralMesh buffer in the current chunk. 
            System.Array.Copy(vertices, verticesOffset, dynamicMesh.vertices, 0, numVerticesInSubmesh[indexUpdate]);
            verticesOffset += numVerticesInSubmesh[indexUpdate];
            System.Buffer.BlockCopy(triangles, trianglesOffset * sizeof(int), dynamicMesh.triangles, 0, 3 * numTrianglesInSubmesh[indexUpdate] * sizeof(int)); //Block copy has better performance than Array.
            trianglesOffset += 3 * numTrianglesInSubmesh[indexUpdate];
            currentMesh.vertices = dynamicMesh.vertices;
            currentMesh.SetTriangles(dynamicMesh.triangles, 0, false);

            dynamicMesh.mesh.sharedMesh = currentMesh;

            //If textured, add UVs. 
            if (updatedTex)
            {
                Vector2[] localUvs = new Vector2[numVerticesInSubmesh[indexUpdate]];
                subMesh.o.GetComponent<MeshRenderer>().sharedMaterial = materialTexture;
                System.Array.Copy(uvs, uvsOffset, localUvs, 0, numVerticesInSubmesh[indexUpdate]);
                uvsOffset += numVerticesInSubmesh[indexUpdate];
                currentMesh.uv = localUvs;

            }
        }

        /// <summary>
        /// Retrieves the entire mesh and texture (vertices, triangles, and uvs) from the ZED SDK. 
        /// Differs for normal retrieval as the UVs and texture are retrieved. 
        /// This is only called after scanning has been stopped, and only if Texturing is enabled. 
        /// </summary>
        public void SetMeshAndTexture()
        {
            //If the texture is too large, it's impossible to add the texture to the mesh. 
            if (texturesSize[0] > 8192) return;

            Texture2D textureMesh = new Texture2D(texturesSize[0], texturesSize[1], TextureFormat.RGB24, false);

            if (textureMesh != null)
            {
                System.IntPtr texture = textureMesh.GetNativeTexturePtr();
                materialTexture.SetTexture("_MainTex", textureMesh);

                vertices = new Vector3[numVertices];
                uvs = new Vector2[numVertices];
                triangles = new int[3 * numTriangles];

                zedCamera.RetrieveMesh(vertices, triangles, MAX_SUBMESH, uvs, texture);
            }
        }

        /// <summary>
        /// Loads a mesh from a file path and allocates the buffers accordingly.
        /// </summary>
        /// <param name="meshFilePath">Path to the mesh file.</param>
        /// <returns></returns>
        public bool LoadMesh(string meshFilePath)
        {
            bool r = zedCamera.LoadMesh(meshFilePath, numVerticesInSubmesh, numTrianglesInSubmesh, ref numUpdatedSubmesh, UpdatedIndices, ref numVertices, ref numTriangles, MAX_SUBMESH, texturesSize);
            if (!r) Debug.LogWarning("[ZED] Failed to load mesh: "+ meshFilePath);
            vertices = new Vector3[numVertices];
            uvs = new Vector2[numVertices];
            triangles = new int[3 * numTriangles];
            return r;
        }

        /// <summary>
        /// Gets the width of the scanned texture file. Note that if this is over 8k, the texture will not be taken.
        /// </summary>
        /// <returns>Texture width in pixels.</returns>
        public int GetWidthTexture()
        {
            return texturesSize[0];
        }

        public int GetHeightTexture()
        {
            return texturesSize[1];
        }

        /// <summary>
        /// Resize the mesh buffer according to how many vertices are needed by the current submesh/chunk. 
        /// </summary>
        public void ResizeMesh()
        {
            if (vertices.Length < numVertices)
            {
                vertices = new Vector3[numVertices * 2]; //Allocation is faster than resizing.
            }

            if (triangles.Length < 3 * numTriangles)
            {
                triangles = new int[3 * numTriangles * 2];
            }
        }

        /// <summary>
        /// Filters the mesh with predefined parameters.
        /// </summary>
        /// <param name="filterParameters">Filter setting. A higher setting results in fewer faces.</param>
        public void FilterMesh(sl.FILTER filterParameters)
        {
            zedCamera.FilterMesh(filterParameters, numVerticesInSubmesh, numTrianglesInSubmesh, ref numUpdatedSubmesh, UpdatedIndices, ref numVertices, ref numTriangles, MAX_SUBMESH);
        }

        /// <summary>
        /// Tells the ZED SDK to consolidate the chunks into a smaller number of large chunks. 
        /// Useful because having many small chunks is more performant for scanning, but fewer large chunks are otherwise easier to work with. 
        /// </summary>
        public void MergeChunks()
        {
            zedCamera.MergeChunks(MAX_SUBMESH, numVerticesInSubmesh, numTrianglesInSubmesh, ref numUpdatedSubmesh, UpdatedIndices, ref numVertices, ref numTriangles, MAX_SUBMESH);
        }

    }
}
