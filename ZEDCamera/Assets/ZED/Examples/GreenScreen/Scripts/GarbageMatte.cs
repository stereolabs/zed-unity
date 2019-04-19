//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates a garbage matte mask from its position and apply it on the pipeline. 
/// It's created by GreenScreenManager as a 3D object, that you can create from the Inspector by placing the bounds of the mesh. 
/// The garbage matte represents a region of the screen where no real pixels will be rendered, regardless of depth. 
/// This can be used to extend the bounds of a greenscreen when the physical screen isn't large enough to fill the background. 
/// </summary>
[RequireComponent(typeof(GreenScreenManager))]
[RequireComponent(typeof(Camera))]
public class GarbageMatte
{
    /// <summary>
    /// Reference to the ZEDCamera. 
    /// </summary>
    private sl.ZEDCamera zed;

    /// <summary>
    /// Position in the render queue used by Unity's renderer to render a mesh transparent. This gets appled to the shader. 
    /// </summary>
    private const int QUEUE_TRANSPARENT_VALUE = 3000;

    /// <summary>
    /// List of 3D points used to make the matte mesh, eg the "corners". 
    /// </summary>
    private List<Vector3> points = new List<Vector3>();

    /// <summary>
    /// The current camera looking at the scene, used to transform ScreenPosition to worldPosition when placing the boundary points. 
    /// </summary>
    private Camera cam;

    /// <summary>
    /// List of the gameObjects used by the mesh.
    /// </summary>
    private List<GameObject> go = null;

    /// <summary>
    /// List of the meshes.
    /// </summary>
    private List<MeshFilter> meshFilters;

    /// <summary>
    /// Triangles of the current mesh.
    /// </summary>
    private List<int> triangles = new List<int>();

    /// <summary>
    /// The sphere objects the user places via GreenScreenManager to define the bounds of the matte object. 
    /// </summary>
    private List<GameObject> borderspheres = new List<GameObject>();

    /// <summary>
    /// The ZED greenscreen material. Usually Mat_ZED_GreenScreen. 
    /// </summary>
    private Material shader_greenScreen;

    /// <summary>
    /// The material used on the spheres that the user places via GreenScreenManager to define the bounds of the matte object. 
    /// Usually Mat_ZED_Outlined.
    /// </summary>
    private Material outlineMaterial;

    /// <summary>
    /// Whether or not the maatte is currently being edited. 
    /// </summary>
    private bool isClosed = false;

    /// <summary>
    /// The index of meshFilters that refers to the plane mesh we're currently editing, if applicable. 
    /// </summary>
    private int currentPlaneIndex;

    /// <summary>
    /// The Unity layer where spheres exist, for visibility reasons. 
    /// </summary>
    private int sphereLayer = 21;

    /// <summary>
    /// The Unity CommandBuffer that gets applied to the camera, which results in the matte getting rendered. 
    /// </summary>
    private CommandBuffer commandBuffer;

    [SerializeField]
    [HideInInspector]
    public string garbageMattePath = "garbageMatte.cfg";

    [SerializeField]
    [HideInInspector]
    public bool editMode = true;

    [SerializeField]
    [HideInInspector]
    public bool loadAtStart = false;
    private Transform target;
    private bool isInit = false;
    public bool IsInit
    {
        get { return isInit; }
    }
    /// <summary>
    /// Constructor that sets up the garbage matte for the desired camera in the desired place. 
    /// </summary>
    /// <param name="cam">Camera in which to apply the matte effect</param>
    /// <param name="greenScreenMaterial">Material reference, usually Mat_ZED_Greenscreen</param>
    /// <param name="target">Center location of the matte effect</param>
    /// <param name="matte">Optional reference to another garbage matte, used to copy its current edit mode. </param>
	public GarbageMatte(ZEDManager camManager, Material greenScreenMaterial, Transform target, GarbageMatte matte)
    {
        this.target = target;
        currentPlaneIndex = 0;


		zed = camManager.zedCamera;
		this.cam = camManager.GetComponentInChildren<Camera>();
        points.Clear();

        outlineMaterial = Resources.Load("Materials/Mat_ZED_Outlined") as Material;

        go = new List<GameObject>();
        meshFilters = new List<MeshFilter>();

        shader_greenScreen = greenScreenMaterial;
        ResetPoints(false);
        if (matte != null)
        {
            editMode = matte.editMode;
        }
        if (commandBuffer == null)
        {
            //Create a command buffer to clear the depth and stencil
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "GarbageMatte";
            commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.Depth);
            
            //Remove the previous command buffer to set the garbage matte first
            CommandBuffer[] cmd = cam.GetCommandBuffers(CameraEvent.BeforeDepthTexture);
            cam.RemoveCommandBuffers(CameraEvent.BeforeDepthTexture);
            if(cmd.Length > 0)
            {
                cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, commandBuffer);
                for(int i = 0; i < cmd.Length; ++i)
                {
                    cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, cmd[i]);
                }
            }
        }
        if (loadAtStart && Load())
        {
            Debug.Log("Config garbage matte found, and loaded ( " + garbageMattePath + " )");
            ApplyGarbageMatte();
            editMode = false;
        }

        isInit = true;
    }

    /// <summary>
    /// Constructor to create a dummy garbage matte that does nothing. Should be used only as a cache in memory
    /// </summary>
	public GarbageMatte()
    {
        isInit = false;

    }

    private List<int> indexSelected = new List<int>();
    private List<GameObject> currentGOSelected = new List<GameObject>();
    private List<MeshFilter> meshFilterSelected = new List<MeshFilter>();
    private List<int> planeSelectedIndex = new List<int>();
    private int numberSpheresSelected = -1;
    
    /// <summary>
    /// Update the garbage matte and manage the movement of the spheres. 
    /// </summary>
    public void Update()
    {
        if (editMode)
        {
            // if at least a sphere is selected
            if (numberSpheresSelected != -1)
            {
				if (zed.IsCameraReady)
                {
                    Vector3 vec = cam.ScreenToWorldPoint(new Vector4(Input.mousePosition.x, Input.mousePosition.y, zed.GetDepthValue(Input.mousePosition)));
                    // For each sphere selected move their position with the mouse
                    for (int i = 0; i < currentGOSelected.Count; ++i)
                    {
                        currentGOSelected[i].transform.position = vec;
                    }
                }
            }


			if (zed != null && zed.IsCameraReady)
            {
                //If left mouse is clicked, add a sphere
                if (Input.GetMouseButtonDown(0))
                {
                    //Add a new plane if needed
                    if (go.Count - 1 < currentPlaneIndex)
                    {

                        go.Add(CreateGameObject());

                        go[currentPlaneIndex].GetComponent<MeshRenderer>().material.renderQueue = QUEUE_TRANSPARENT_VALUE + 5;
                        meshFilters[currentPlaneIndex] = go[currentPlaneIndex].GetComponent<MeshFilter>();
                        meshFilters[currentPlaneIndex].sharedMesh = CreateMesh();
                        meshFilters[currentPlaneIndex].sharedMesh.MarkDynamic();
                    }

                    
                    if (numberSpheresSelected != -1)
                    {
                        //Remove outline from the sphere cause a sphere was selected
                        //Clear the meshes and spheres selected
                        for (int i = 0; i < currentGOSelected.Count; ++i)
                        {
                            currentGOSelected[i].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.00f);
                            currentGOSelected[i] = null;
                        }
                        currentGOSelected.Clear();

                        for (int i = 0; i < meshFilterSelected.Count; ++i)
                        {
                            meshFilterSelected[i].mesh.Clear();
                        }
                        meshFilterSelected.Clear();

                        //Create the planes if needed
                        for (int i = 0; i < planeSelectedIndex.Count; ++i)
                        {
                            if ((borderspheres.Count - planeSelectedIndex[i] * 4) < 4)
                            {
                                numberSpheresSelected = -1;
                                planeSelectedIndex.Clear();
                                return;
                            }
                            List<int> triangles = new List<int>();
                            points = new List<Vector3>();
                            for (int j = planeSelectedIndex[i] * 4; j < (planeSelectedIndex[i] + 1) * 4; j++)
                            {
                                points.Add(borderspheres[j].transform.position);
                            }

                            CloseShape(triangles, points, planeSelectedIndex[i]);
                        }

                        numberSpheresSelected = -1;
                        return;
                    }
                    // Add a sphere
					else if (points.Count < 100 && !isClosed)
                    {
                        Vector3 vec = cam.ScreenToWorldPoint(new Vector4(Input.mousePosition.x, Input.mousePosition.y, zed.GetDepthValue(Input.mousePosition)));
                        RaycastHit hit;
                        if (Physics.Raycast(target.position, (vec - target.position), out hit, 10, (1 << sphereLayer)))
                        {
                            int hitIndex = borderspheres.IndexOf(hit.transform.gameObject);
                            vec = borderspheres[hitIndex].transform.position;
                        }
                        points.Add(vec);

                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
						sphere.hideFlags = HideFlags.HideInHierarchy;
						sphere.tag = "HelpObject";
                        sphere.GetComponent<MeshRenderer>().material = outlineMaterial;
                        sphere.GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.02f);

                        sphere.transform.position = points[points.Count - 1];
                        sphere.layer = sphereLayer;
                        borderspheres.Add(sphere);
                        if (borderspheres.Count >= 2)
                        {
                            borderspheres[borderspheres.Count - 2].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.00f);
                        }

                        if (borderspheres.Count % 4 == 0)
                        {
                            points = new List<Vector3>();
                            for (int i = currentPlaneIndex * 4; i < (currentPlaneIndex + 1) * 4; i++)
                            {
                                points.Add(borderspheres[i].transform.position);
                            }
                            CloseShape(triangles, points, currentPlaneIndex);
                            EndPlane();
                        }
                    }
                }

                //Select the sphere, to move them
                if (Input.GetMouseButtonDown(1))
                {
                    if (numberSpheresSelected != -1) return;
                    Vector3 vec = cam.ScreenToWorldPoint(new Vector4(Input.mousePosition.x, Input.mousePosition.y, zed.GetDepthValue(Input.mousePosition)));
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(target.position, (vec - target.position), 10, (1 << sphereLayer));
                    if (hits.Length > 0)
                    {
                        indexSelected.Clear();
                        currentGOSelected.Clear();
                        planeSelectedIndex.Clear();
                        meshFilterSelected.Clear();

                        for (int i = 0; i < hits.Length; ++i)
                        {
                            int hitIndex = borderspheres.IndexOf(hits[i].transform.gameObject);

                            indexSelected.Add(hitIndex);
                            currentGOSelected.Add(borderspheres[hitIndex]);
                            planeSelectedIndex.Add(hitIndex / 4);
                            meshFilterSelected.Add(meshFilters[planeSelectedIndex[planeSelectedIndex.Count - 1]]);

                            borderspheres[hitIndex].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.02f);
                        }
                        numberSpheresSelected = hits.Length;
                        borderspheres[borderspheres.Count - 1].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.00f);

                    }
                    else
                    {
                        numberSpheresSelected = -1;
                    }

                }
            }

        }
    }


    /// <summary>
    /// Finishes the current plane and increases the index of the plane
    /// </summary>
    private void EndPlane()
    {
        currentPlaneIndex++;
        ResetDataCurrentPlane();
    }

    /// <summary>
    /// Enables editing the matte. 
    /// </summary>
    public void EnterEditMode()
    {
        if (isClosed)
        {
            foreach (GameObject s in borderspheres)
            {
                s.SetActive(true);
            }
            if (shader_greenScreen != null)
            {
                Shader.SetGlobalInt("_ZEDStencilComp", 0);
            }
            for (int i = 0; i < go.Count; i++)
            {
                if (go[i] == null) continue;
                go[i].GetComponent<MeshRenderer>().sharedMaterial.SetFloat("alpha", 0.5f);
                go[i].GetComponent<MeshRenderer>().sharedMaterial.renderQueue = QUEUE_TRANSPARENT_VALUE + 5;
            }
            isClosed = false;
        }
    }

    /// <summary>
    /// Removes the last sphere the user placed while defining the matte object's boundaries. 
    /// </summary>
    public void RemoveLastPoint()
    {
        //Prevent to remove and move a sphere at the same time
        if (numberSpheresSelected != -1) return;
        if (isClosed)
        {
            foreach (GameObject s in borderspheres)
            {
                s.SetActive(true);
            }
            if (shader_greenScreen != null)
            {
                Shader.SetGlobalInt("_ZEDStencilComp", 0);
            }
            for (int i = 0; i < go.Count; i++)
            {
                if (go[i] == null) continue;
                go[i].GetComponent<MeshRenderer>().sharedMaterial.SetFloat("alpha", 0.5f);
                go[i].GetComponent<MeshRenderer>().sharedMaterial.renderQueue = QUEUE_TRANSPARENT_VALUE + 5;
            }
            isClosed = false;
        }
        if (borderspheres.Count % 4 == 0 && currentPlaneIndex > 0)
        {
            GameObject.Destroy(go[currentPlaneIndex - 1]);
            go.RemoveAll(item => item == null);
            meshFilters.RemoveAll(item => item == null);
            meshFilters[currentPlaneIndex - 1].sharedMesh.Clear();

            currentPlaneIndex--;

        }

        if (borderspheres != null && borderspheres.Count > 0)
        {

            GameObject.DestroyImmediate(borderspheres[borderspheres.Count - 1]);
            borderspheres.RemoveAt(borderspheres.Count - 1);
            if (borderspheres.Count % 4 == 0 && borderspheres.Count > 0)
            {
                borderspheres[borderspheres.Count - 1].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.02f);
            }

        }

    }

    /// <summary>
    /// Clears the boundary points and triangles. Used before making a new plane, or when resetting all data. 
    /// </summary>
    private void ResetDataCurrentPlane()
    {
        points.Clear();
        triangles.Clear();
    }

    /// <summary>
    /// Removes existing sphere objects used to define bounds. 
    /// Used to ensure they're properly cleaned up when not being used to edit the garbage matte. 
    /// </summary>
	public void CleanSpheres()
	{
		GameObject[] remain_sphere2 = GameObject.FindGameObjectsWithTag ("HelpObject");
		if (remain_sphere2.Length > 0) {
			foreach (GameObject sph in remain_sphere2)
				GameObject.DestroyImmediate (sph);
		}
	}

    /// <summary>
    /// Destroys all planes and spheres used to edit the matte to start from scratch. 
    /// </summary>
    /// <param name="cleansphere"></param>
	public void ResetPoints(bool cleansphere)
    {
		if (cleansphere) {
			GameObject[] remain_sphere2 = GameObject.FindGameObjectsWithTag ("HelpObject");
			if (remain_sphere2.Length > 0) {
				foreach (GameObject sph in remain_sphere2)
					GameObject.Destroy (sph);
			}
		}

        Shader.SetGlobalInt("_ZEDStencilComp", 0);

        if (go == null) return;
        isClosed = false;
        
        currentPlaneIndex = 0;
        for (int i = 0; i < go.Count; i++)
        {
            GameObject.DestroyImmediate(go[i]);
        }
        go.Clear();
        meshFilters.Clear();
        ResetDataCurrentPlane();
        if (borderspheres != null)
        {
            foreach (GameObject s in borderspheres)
            {
                GameObject.DestroyImmediate(s);
            }
        }
        borderspheres.Clear();
        if (commandBuffer != null)
        {
            commandBuffer.Clear();
        }

		points.Clear ();

    }

    /// <summary>
    /// Helper function to determine a point's orientation along the plane. 
    /// Used by OrderPoints to sort vertices. 
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p"></param>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <returns>1 if it should be higher on the list, 0 if it should be lower. </returns>
    private static int Orientation(Vector3 p1, Vector3 p2, Vector3 p, Vector3 X, Vector3 Y)
    {
        return (Vector3.Dot(p2, X) - Vector3.Dot(p1, X)) * (Vector3.Dot(p, Y) - Vector3.Dot(p1, Y)) - (Vector3.Dot(p, X) - Vector3.Dot(p1, X)) * (Vector3.Dot(p2, Y) - Vector3.Dot(p1, Y)) > 0 ? 1 : 0;
    }

    /// <summary>
    /// Orders the points in the points list in an order proper for drawing a mesh.
    /// Points need to appear in the list in clockwise order within a triangle being drawn with them, around the plane's normal. 
    /// </summary>
    /// <returns></returns>
    private List<int> OrderPoints(List<Vector3> points)
    {
        Vector3 normal = Vector3.Cross((points[1] - points[0]), (points[2] - points[0]));
        normal.Normalize();
        Vector3 X = new Vector3(-normal.y, normal.x, 0);
        X.Normalize();
        Vector3 Y = Vector3.Cross(X, normal);
        Y.Normalize();

        List<int> orderedIndex = new List<int>();

        List<Vector3> convexHull = new List<Vector3>();
        float minX = Vector3.Dot(points[0], X);
        Vector3 p = points[0];

        for (int i = 0; i < points.Count; i++)
        {
            if (Vector3.Dot(points[i], X) < minX)
            {
                minX = Vector3.Dot(points[i], X);
                p = points[i];
            }
        }
        Vector3 currentTestPoint;

        for (int i = 0; i < 4; i++)
        {
            convexHull.Add(p);
            orderedIndex.Add(points.IndexOf(p));
            currentTestPoint = points[0];
            for (int j = 0; j < points.Count; j++)
            {
                if ((currentTestPoint == p) || (Orientation(p, currentTestPoint, points[j], X, Y) == 1))
                {
                    currentTestPoint = points[j];
                }
            }
            p = currentTestPoint;
        }
        return orderedIndex;
    }

    /// <summary>
    /// Finish off the last quad mesh. 
    /// </summary>
    public void CloseShape(List<int> triangles, List<Vector3> points, int currentPlaneIndex)
    {
        triangles.Clear();
        List<int> indexOrder = OrderPoints(points);

        triangles.Add(indexOrder[0]);
        triangles.Add(indexOrder[1]);
        triangles.Add(indexOrder[2]);
        triangles.Add(indexOrder[0]);
        triangles.Add(indexOrder[2]);
        triangles.Add(indexOrder[3]);

        if (go[currentPlaneIndex] == null)
        {
            go[currentPlaneIndex] = CreateGameObject();
            meshFilters[currentPlaneIndex] = go[currentPlaneIndex].GetComponent<MeshFilter>();
            meshFilters[currentPlaneIndex].sharedMesh = CreateMesh();
            meshFilters[currentPlaneIndex].sharedMesh.MarkDynamic();
        }
        go[currentPlaneIndex].GetComponent<MeshFilter>().sharedMesh.Clear();
        go[currentPlaneIndex].GetComponent<MeshFilter>().sharedMesh.vertices = points.ToArray();
        go[currentPlaneIndex].GetComponent<MeshFilter>().sharedMesh.triangles = triangles.ToArray();

        borderspheres[borderspheres.Count - 1].GetComponent<MeshRenderer>().material.SetFloat("_Outline", 0.00f);

    }

    /// <summary>
    /// Apply the garbage matte by rendering into the stencil buffer.
    /// </summary>
    public void ApplyGarbageMatte()
    {

        if (currentPlaneIndex <= 0)
        {
            editMode = false;
            ResetPoints(false);
            return;
        }
        if (shader_greenScreen != null)
        {
            isClosed = true;
            foreach (GameObject s in borderspheres)
            {
                s.SetActive(false);
            }
            Shader.SetGlobalInt("_ZEDStencilComp", 3);
            for (int i = 0; i < go.Count; i++)
            {
                if (go[i] == null) continue;
                go[i].GetComponent<MeshRenderer>().sharedMaterial.SetFloat("alpha", 0.0f);
                go[i].GetComponent<MeshRenderer>().sharedMaterial.renderQueue = QUEUE_TRANSPARENT_VALUE - 5;
            }
        }

        commandBuffer.Clear();
        for (int i = 0; i < go.Count; ++i)
        {
            if (go[i] != null)
            {
                commandBuffer.DrawMesh(go[i].GetComponent<MeshFilter>().mesh, go[i].transform.localToWorldMatrix, go[i].GetComponent<Renderer>().material);
            }
        }
        editMode = false;
    }
    private void OnApplicationQuit()
    {
        ResetPoints(true);
    }

    /// <summary>
    /// Create a hidden GameObject used to hold the editing components. 
    /// </summary>
    /// <returns></returns>
    private GameObject CreateGameObject()
    {
        GameObject plane = new GameObject("PlaneTest");
        plane.hideFlags = HideFlags.HideInHierarchy;
        meshFilters.Add((MeshFilter)plane.AddComponent(typeof(MeshFilter)));

        MeshRenderer renderer = plane.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.sharedMaterial = new Material(Resources.Load("Materials/Mat_ZED_Mask_Quad") as Material);
        renderer.sharedMaterial.SetFloat("alpha", 0.5f);
        return plane;
    }

    private Mesh CreateMesh()
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        return m;
    }

    /// <summary>
    /// Represents a single plane to be made into a mesh, then a matte. 
    /// </summary>
    [System.Serializable]
    public struct Plane
    {
        public int numberVertices;
        public List<Vector3> vertices;
    }

    /// <summary>
    /// Holds all planes to be turned into mattes and the total number of meshes. 
    /// </summary>
    [System.Serializable]
    public struct GarbageMatteData
    {
        public int numberMeshes;
        public List<Plane> planes;
    }

    /// <summary>
    /// Packages the current garbage matte into GarbageMatteData, which can be serialized/saved by GreenScreenEditor. 
    /// </summary>
    /// <returns>Data ready to be serialized. </returns>
    public GarbageMatteData RegisterData()
    {
        GarbageMatteData garbageMatteData = new GarbageMatteData();

        if (meshFilters == null) return garbageMatteData;
        garbageMatteData.numberMeshes = meshFilters.Count;
        garbageMatteData.planes = new List<Plane>();
        for (int i = 0; i < meshFilters.Count ; i++)
        {
            Vector3[] vertices = meshFilters[i].mesh.vertices;
            Plane p = new Plane();
            p.numberVertices = vertices.Length;
            p.vertices = new List<Vector3>(vertices);
            garbageMatteData.planes.Add(p);
            //garbageMatteData.plane.ad
        }
        return garbageMatteData;
    }

    /// <summary>
    /// Loads a serialized GarbageMatteData instance to be used/viewed/edited. 
    /// </summary>
    /// <param name="garbageMatteData"></param>
    /// <returns>True if there was actual data to load (at least one plane).</returns>
    public bool LoadData(GarbageMatteData garbageMatteData)
    {
       
        int nbMesh = garbageMatteData.numberMeshes;
        if (nbMesh < 0) return false;
        currentPlaneIndex = 0;
        ResetPoints(false);

        for (int i = 0; i < nbMesh; i++)
        {
            points.Clear();
            triangles.Clear();
            go.Add(CreateGameObject());
            go[currentPlaneIndex].GetComponent<MeshRenderer>().material.renderQueue = QUEUE_TRANSPARENT_VALUE + 5;
            meshFilters[currentPlaneIndex] = go[currentPlaneIndex].GetComponent<MeshFilter>();
            meshFilters[currentPlaneIndex].sharedMesh = CreateMesh();
            meshFilters[currentPlaneIndex].sharedMesh.MarkDynamic();
            Plane p = garbageMatteData.planes[i];
            for (int j = 0; j < p.numberVertices; j++)
            {
                points.Add(p.vertices[j]);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
				sphere.tag = "HelpObject";
                sphere.hideFlags = HideFlags.HideInHierarchy;
                outlineMaterial.SetFloat("_Outline", 0.00f);
                sphere.GetComponent<MeshRenderer>().material = outlineMaterial;

                sphere.transform.position = points[points.Count - 1];
                sphere.layer = sphereLayer;
                borderspheres.Add(sphere);
            }
            if (go.Count == 0) return false;

            CloseShape(triangles, points, currentPlaneIndex);
            EndPlane();
        }
        return true;
    }

    /// <summary>
    /// Save the points into a file.
    /// </summary>
    public void Save()
    {
        List<string> meshes = new List<string>();
        meshes.Add((meshFilters.Count - 1).ToString());

        for (int i = 0; i < meshFilters.Count - 1; i++)
        {
            Vector3[] vertices = meshFilters[i].mesh.vertices;
            int[] tri = meshFilters[i].mesh.triangles;
            meshes.Add("v#" + vertices.Length);
            for (int j = 0; j < vertices.Length; j++)
            {
                meshes.Add(vertices[j].x + " " + vertices[j].y + " " + vertices[j].z);
            }
        }
        System.IO.File.WriteAllLines(garbageMattePath, meshes.ToArray());
    }

    /// <summary>
    /// Load the current shape
    /// </summary>
    public bool Load()
    {
        if (!System.IO.File.Exists(garbageMattePath)) return false;
        string[] meshes = System.IO.File.ReadAllLines(garbageMattePath);
        if (meshes == null) return false;
        int nbMesh = int.Parse(meshes[0]);
        if (nbMesh < 0) return false;
        currentPlaneIndex = 0;
        ResetPoints(false);
        int lineCount = 1;
        string[] splittedLine;
        for (int i = 0; i < nbMesh; i++)
        {
            points.Clear();
            triangles.Clear();
            go.Add(CreateGameObject());
            go[currentPlaneIndex].GetComponent<MeshRenderer>().material.renderQueue = QUEUE_TRANSPARENT_VALUE + 5;
            meshFilters[currentPlaneIndex] = go[currentPlaneIndex].GetComponent<MeshFilter>();
            meshFilters[currentPlaneIndex].sharedMesh = CreateMesh();
            meshFilters[currentPlaneIndex].sharedMesh.MarkDynamic();
            splittedLine = meshes[lineCount].Split('#');
            lineCount++;
            int nbVertices = int.Parse(splittedLine[1]);
            for (int j = 0; j < nbVertices; j++)
            {
                splittedLine = meshes[lineCount].Split(' ');
                lineCount++;
                float x = float.Parse(splittedLine[0]), y = float.Parse(splittedLine[1]), z = float.Parse(splittedLine[2]);
                points.Add(new Vector3(x, y, z));
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                sphere.hideFlags = HideFlags.HideInHierarchy;
				sphere.tag = "HelpObject";
                sphere.transform.position = points[points.Count - 1];
                sphere.layer = sphereLayer;
                borderspheres.Add(sphere);
            }
            if (go.Count == 0) return false;

            CloseShape(triangles, points, currentPlaneIndex);
            EndPlane();
        }
        return true;
    }
}

