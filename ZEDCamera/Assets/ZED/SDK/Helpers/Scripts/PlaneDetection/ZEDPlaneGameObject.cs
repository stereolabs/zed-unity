//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

#if ZED_LWRP || ZED_HDRP
using UnityEngine.Rendering;
#endif

/// <summary>
/// Represents an individual plane that was detected by ZEDPlaneDetectionManager.
/// When created, it converts plane data from the ZED SDK into a mesh with proper world position/rotation. 
/// This is necessary as the ZED SDK provides plane data relative to the camera. 
/// It's also used to enable/disable collisions and visibility. 
/// </summary>
public class ZEDPlaneGameObject : MonoBehaviour
{
    /// <summary>
    /// Type of the plane, determined by its orientation and whether detected by ZEDPlaneDetectionManager's
    /// DetectFloorPlane() or DetectPlaneAtHit().
    /// </summary>
    public enum PLANE_TYPE
    {
        /// <summary>
        /// Floor plane of a scene. Retrieved by ZEDPlaneDetectionManager.DetectFloorPlane().
        /// </summary>
        FLOOR,
        /// <summary>
        /// Horizontal plane, such as a tabletop, floor, etc. Detected with DetectPlaneAtHit() using screen-space coordinates. 
        /// </summary>
        HIT_HORIZONTAL,
        /// <summary>
        /// Vertical plane, such as a wall. Detected with DetectPlaneAtHit() using screen-space coordinates. 
        /// </summary>
		HIT_VERTICAL,
        /// <summary>
        /// Plane at an angle neither parallel nor perpendicular to the floor. Detected with DetectPlaneAtHit() using screen-space coordinates. 
        /// </summary>
		HIT_UNKNOWN
    };

    /// <summary>
    /// Structure that defines a new plane, holding information directly from the ZED SDK.
    /// Data within is relative to the camera; use ZEDPlaneGameObject's public fields for world-space values. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneData
    {
        /// <summary>
        /// Error code returned by the ZED SDK when the plane detection was attempted. 
        /// </summary>
        public sl.ERROR_CODE ErrorCode;
        /// <summary>
        /// Type of the plane (floor, hit_vertical, etc.) 
        /// </summary>
        public ZEDPlaneGameObject.PLANE_TYPE Type;
        /// <summary>
        /// Normalized vector of the direction the plane is facing. 
        /// </summary>
        public Vector3 PlaneNormal;
        /// <summary>
        /// Camera-space position of the center of the plane. 
        /// </summary>
        public Vector3 PlaneCenter;
        /// <summary>
        /// Camera-space position of the center of the plane. 
        /// </summary>
        public Vector3 PlaneTransformPosition;
        /// <summary>
        /// Camera-space rotation/orientation of the plane. 
        /// </summary>
        public Quaternion PlaneTransformOrientation;
        /// <summary>
        /// The mathematical Vector4 equation of the plane. 
        /// </summary>
        public Vector4 PlaneEquation;
        /// <summary>
        /// How wide and long/tall the plane is in meters. 
        /// </summary>
        public Vector2 Extents;
        /// <summary>
        /// How many points make up the plane's bounds, eg. the array length of Bounds. 
        /// </summary>
        public int BoundsSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        ///Positions of the points that make up the edges of the plane's mesh. 
        public Vector3[] Bounds; //max 256 points
    }

    /// <summary>
    /// Copy of the PlaneData structure provided by the ZED SDK when the plane was first detected. 
    /// Position, orientation, normal and equation are relative to the ZED camera at time of detection, not the world. 
    /// </summary>
    public ZEDPlaneGameObject.PlaneData planeData;


    /// <summary>
    /// Whether the plane has had Create() called yet to build its mesh. 
    /// </summary>
    private bool isCreated = false;
    /// <summary>
    /// Public accessor for isCreated, which is hether the plane has had Create() called yet to build its mesh. 
    /// </summary>
    public bool IsCreated
    {
        get { return isCreated; }
    }
    /// <summary>
    /// Normalized vector representing the direction the plane is facing in world space. 
    /// </summary>
    public Vector3 worldNormal { get; private set; }

    /// <summary>
    /// Position of the plane's center in world space. 
    /// </summary>
    public Vector3 worldCenter
    {
        get
        {
            return gameObject.transform.position;
        }
    }

    /// <summary>
    /// The MeshFilter used to display the plane. 
    /// </summary>
    MeshFilter mfilter;

    /// <summary>
    /// The MeshRenderer attached to this object. 
    /// </summary>
    MeshRenderer rend;

    /// <summary>
    /// Enabled state of the attached Renderer prior to Unity's rendering stage. 
    /// <para>Used so that manually disabling the object's MeshRenderer won't be undone by this script.</para>
    /// </summary>
    private bool lastRenderState = true;

    /// <summary>
    /// Creates a mesh from given plane data and assigns it to new MeshFilter, MeshRenderer and MeshCollider components. 
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="vertices"></param>
    /// <param name="triangles"></param>
    /// <param name="rendermaterial"></param>
    private void SetComponents(PlaneData plane, Vector3[] vertices, int[] triangles, Material rendermaterial)
    {
        //Create the MeshFilter to render the mesh
        mfilter = gameObject.GetComponent<MeshFilter>();
        if (mfilter == null)
            mfilter = gameObject.AddComponent<MeshFilter>();

        //Eliminate superfluous vertices.
        int highestvertindex = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] > highestvertindex) highestvertindex = triangles[i];
        }
        System.Array.Resize(ref vertices, highestvertindex + 1);


        //Calculate the UVs for the vertices based on world space, so they line up with other planes.
        Vector2[] uvs = new Vector2[vertices.Length];
        Quaternion rotatetobackward = Quaternion.FromToRotation(worldNormal, Vector3.back);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 upwardcoords = rotatetobackward * (vertices[i] + worldCenter);
            uvs[i] = new Vector2(upwardcoords.x, upwardcoords.y);
        }

        //Apply the new data to the MeshFilter's mesh and update it. 
        mfilter.mesh.Clear();
        mfilter.mesh.vertices = vertices;
        mfilter.mesh.triangles = triangles;
        mfilter.mesh.uv = uvs;
        mfilter.mesh.RecalculateNormals();
        mfilter.mesh.RecalculateBounds();

        //Get the MeshRenderer and set properties.
        rend = gameObject.GetComponent<MeshRenderer>();
        if (rend == null)
            rend = gameObject.AddComponent<MeshRenderer>();
        rend.material = rendermaterial;

        //Turn off light and shadow effects, as the planes are meant to highlight a real-world object, not be a distinct object. 
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.enabled = true;
        rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        //Get the MeshCollider and apply the new mesh to it. 
        MeshCollider mc = gameObject.GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();

        // Set the mesh for the collider.
        mc.sharedMesh = mfilter.mesh;

        lastRenderState = true;

    }

    /// <summary>
    ///  Create the plane as a GameObject, and fills the internal PlaneData structure for future access.
    /// </summary>
    /// <param name="holder">Scene's holder object to which all planes are parented.</param>
    /// <param name="plane">PlaneData filled by the ZED SDK.</param>
    /// <param name="vertices">Vertices of the mesh.</param>
    /// <param name="triangles">Triangles of the mesh.</param>
    /// <param name="opt_count">If a hit plane, the total number of hit planes detected prior to and including this one.</param>
    public void Create(Camera cam, PlaneData plane, Vector3[] vertices, int[] triangles, int opt_count)
    {
        Create(cam, plane, vertices, triangles, opt_count, GetDefaultMaterial(plane.Type));
    }

    /// <summary>
    ///  Create the plane as a GameObject with a custom material, and fills the internal PlaneData structure for future access.
    /// </summary>
    /// <param name="holder">Scene's holder object to which all planes are parented.</param>
    /// <param name="plane">PlaneData filled by the ZED SDK.</param>
    /// <param name="vertices">Vertices of the mesh.</param>
    /// <param name="triangles">Triangles of the mesh.</param>
    /// <param name="opt_count">If a hit plane, the total number of hit planes detected prior to and including this one.</param>
    /// <param name="rendermaterial">Material to replace the default wireframe plane material.</param>
	public void Create(Camera cam, PlaneData plane, Vector3[] vertices, int[] triangles, int opt_count, Material rendermaterial)
    {
        //Copy the supplied PlaneData into this component's own PlaneData, for accessing later. 
        planeData.ErrorCode = plane.ErrorCode;
        planeData.Type = plane.Type;
        planeData.PlaneNormal = plane.PlaneNormal;
        planeData.PlaneCenter = plane.PlaneCenter;
        planeData.PlaneTransformPosition = plane.PlaneTransformPosition;
        planeData.PlaneTransformOrientation = plane.PlaneTransformOrientation;
        planeData.PlaneEquation = plane.PlaneEquation;
        planeData.Extents = plane.Extents;
        planeData.BoundsSize = plane.BoundsSize;
        planeData.Bounds = new Vector3[plane.BoundsSize];
        System.Array.Copy(plane.Bounds, planeData.Bounds, plane.BoundsSize);

        //Calculate the world space normal. 
        Camera leftCamera = cam;
        worldNormal = cam.transform.TransformDirection(planeData.PlaneNormal);

        //Create the MeshCollider.
        gameObject.AddComponent<MeshCollider>().sharedMesh = null;

        if (plane.Type != PLANE_TYPE.FLOOR) //Give it a name. 
            gameObject.name = "Hit Plane " + opt_count;
        else
            gameObject.name = "Floor Plane";


        gameObject.layer = 12;//sl.ZEDCamera.TagOneObject;

        SetComponents(plane, vertices, triangles, rendermaterial);

        isCreated = true;

        //Subscribe to events that let you govern visibility in the scene and game. 
#if !ZED_LWRP && !ZED_HDRP
        Camera.onPreCull += PreCull;
        Camera.onPostRender += PostRender;
#else
        RenderPipelineManager.beginFrameRendering += SRPFrameBegin;
#endif
    }

    /// <summary>
    /// Updates the floor plane with new plane data, if 
    /// </summary>
    /// <returns><c>true</c>, if floor plane was updated, <c>false</c> otherwise.</returns>
    /// <param name="force">If set to <c>true</c> force the update. Is set to false, update only if new plane/mesh is bigger or contains the old one</param>
    /// <param name="plane">PlaneData returned from the ZED SDK.</param>
    /// <param name="vertices">Vertices of the new plane mesh.</param>
    /// <param name="triangles">Triangles of the new plane mesh.</param>
    public bool UpdateFloorPlane(bool force, ZEDPlaneGameObject.PlaneData plane, Vector3[] vertices, int[] triangles, Material rendermaterial = null)
    {
        bool need_update = false;

        if (!force) //Not force mode. Check if the new mesh contains or is larger than the old one. 
        {
            if (!gameObject.GetComponent<MeshRenderer>().isVisible)
                need_update = true;
            else
            {
                Mesh tmp = new Mesh();
                tmp.vertices = vertices;
                tmp.SetTriangles(triangles, 0, false);
                tmp.RecalculateBounds();

                Bounds tmpNBnds = tmp.bounds;
                MeshFilter mf = gameObject.GetComponent<MeshFilter>();

                if ((tmpNBnds.Contains(mf.mesh.bounds.min) && tmpNBnds.Contains(mf.mesh.bounds.max)) || (tmpNBnds.size.x * tmpNBnds.size.y > 1.1 * mf.mesh.bounds.size.x * mf.mesh.bounds.size.y))
                    need_update = true;

                tmp.Clear();
            }

        }
        else //Force mode. Update the mesh regardless of the existing mesh. 
            need_update = true;

        if (need_update)
            SetComponents(plane, vertices, triangles, gameObject.GetComponent<MeshRenderer>().material);


        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (rendermaterial != null)
            {
                mr.material = rendermaterial;
            }
            else
            {
                mr.material = GetDefaultMaterial(plane.Type);
            }
        }

        return need_update;

    }



    /// <summary>
    /// Enable/disable the MeshCollider component to turn on/off collisions. 
    /// </summary>
    /// <param name="c">If set to <c>true</c>, collisions will be enabled.</param>
    public void SetPhysics(bool c)
    {
        MeshCollider mc = gameObject.GetComponent<MeshCollider>();
        if (mc != null)
            mc.enabled = c;
    }

    /// <summary>
    /// Enable/disable the MeshRenderer component to turn on/off visibility.
    /// </summary>
    /// <param name="c">If set to <c>true</c> c.</param>
    public void SetVisible(bool c)
    {
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = c;
    }

    /// <summary>
    /// Gets the size of the bounding rect that fits the plane (aka 'extents').
    /// </summary>
    /// <returns>The scale.</returns>
    public Vector2 GetScale()
    {
        return planeData.Extents;
    }

    /// <summary>
    /// Returns the bounds of the plane from the MeshFilter. 
    /// </summary>
    /// <returns></returns>
    public Bounds GetBounds()
    {
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf != null)
            return mf.mesh.bounds;
        else
            return new Bounds(gameObject.transform.position, Vector3.zero);
    }

    /// <summary>
    /// Gets the minimum distance to plane boundaries of a given 3D point (in world space)
    /// </summary>
    /// <returns>The minimum distance to boundaries.</returns>
    /// <param name="worldPosition">World position.</param>
    public float getMinimumDistanceToBoundaries(Camera cam, Vector3 worldPosition, out Vector3 minimumBoundsPosition)
    {

        Camera leftCamera = cam;
        float minimal_distance = ZEDSupportFunctions.DistancePointLine(worldPosition, leftCamera.transform.TransformPoint(planeData.Bounds[0]), leftCamera.transform.TransformPoint(planeData.Bounds[1]));

        Vector3 BestFoundPoint = new Vector3(0.0f, 0.0f, 0.0f);
        if (planeData.BoundsSize > 2)
        {
            for (int i = 1; i < planeData.BoundsSize - 1; i++)
            {
                float currentDistance = ZEDSupportFunctions.DistancePointLine(worldPosition, leftCamera.transform.TransformPoint(planeData.Bounds[i]), leftCamera.transform.TransformPoint(planeData.Bounds[i + 1]));
                if (currentDistance < minimal_distance)
                {
                    minimal_distance = currentDistance;
                    BestFoundPoint = ZEDSupportFunctions.ProjectPointLine(worldPosition, leftCamera.transform.TransformPoint(planeData.Bounds[i]), leftCamera.transform.TransformPoint(planeData.Bounds[i + 1]));
                }
            }
        }

        minimumBoundsPosition = BestFoundPoint;
        return minimal_distance;
    }


    /// <summary>
    /// Determines whether this floor plane is visible by any camera.
    /// </summary>
    /// <returns><c>true</c> if this instance is floor plane visible; otherwise, <c>false</c>.</returns>
    public bool IsFloorPlaneVisible()
    {
        return gameObject.GetComponent<MeshRenderer>().isVisible;
    }

    /// <summary>
    /// Loads the default material for a plane, given its plane type. 
    /// Blue wireframe for floor planes and pink wireframe for hit planes. 
    /// </summary>
    /// <param name="type">Type of plane based on its orientation and if it's the scene's floor plane.</param>
    /// <returns></returns>
    private Material GetDefaultMaterial(PLANE_TYPE type)
    {
        //Find the default material for the plane type
        Material defaultmaterial = new Material(Resources.Load("Materials/PlaneDetection/Mat_ZED_Geometry_WirePlane") as Material);
        switch (type)
        {
            case PLANE_TYPE.FLOOR:
                //Floor planes are blue
                defaultmaterial.SetColor("_WireColor", new Color(44.0f / 255.0f, 157.0f / 255.0f, 222.0f / 255.0f, 174.0f / 255.0f));
                break;
            case PLANE_TYPE.HIT_HORIZONTAL:
            case PLANE_TYPE.HIT_VERTICAL:
            case PLANE_TYPE.HIT_UNKNOWN:
                // Hit planes are pink
                defaultmaterial.SetColor("_WireColor", new Color(221.0f / 255.0f, 20.0f / 255.0f, 149.0f / 255.0f, 174.0f / 255.0f));
                break;

            default:
                //Misc. planes are white
                defaultmaterial.SetColor("_WireColor", new Color(1, 1, 1, 174.0f / 255.0f));
                break;
        }

        return defaultmaterial;
    }

#if! ZED_LWRP && !ZED_HDRP
    /// <summary>
    /// Disables the MeshRenderer object for rendering a single camera, depending on display settings in ZEDPlaneDetectionManager. 
    /// </summary>
    /// <param name="currentcam"></param>
    private void PreCull(Camera currentcam)
    {
        lastRenderState = rend.enabled;
        if (!rend.enabled) return; //We weren't going to render this object anyway, so skip the rest of the logic. 

        if (currentcam.name.ToLower().Contains("scenecamera"))
        {
            rend.enabled = ZEDPlaneDetectionManager.isSceneDisplay;

        }
        else
        {
            rend.enabled = ZEDPlaneDetectionManager.isGameDisplay;
        }

    }

    /// <summary>
    /// Re-enables the MeshRenderer after PreCull may disable it each time a camera renders. 
    /// </summary>
    /// <param name="currentcam"></param>
    private void PostRender(Camera currentcam)
    {
        rend.enabled = lastRenderState;
    }
#else
    private void SRPFrameBegin(ScriptableRenderContext context, Camera[] rendercams)
    {
        rend.enabled = false; //We'll only draw for certain cameras. 
        foreach (Camera rendcam in rendercams)
        {
            if (rendcam.name.ToLower().Contains("scenecamera"))
            {
                if (ZEDPlaneDetectionManager.isSceneDisplay) DrawPlane(rendcam);
            }
            else if (ZEDPlaneDetectionManager.isGameDisplay) DrawPlane(rendcam);
        }
    }

    private void DrawPlane(Camera drawcam)
    {
        Matrix4x4 canvastrs = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        Graphics.DrawMesh(mfilter.mesh, canvastrs, rend.material, 0, drawcam);
    }
#endif


    private void OnDestroy()
    {
#if !ZED_LWRP && !ZED_HDRP
        Camera.onPreCull -= PreCull;
        Camera.onPostRender -= PostRender;
#else
        RenderPipelineManager.beginFrameRendering -= SRPFrameBegin;
#endif
    }
}
