//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

/// <summary>
/// Process the mesh taken from the ZED
/// </summary>
public class ZEDPlaneGameObject : MonoBehaviour
{

    /// <summary>
    /// Plane Type (horizontal, vertical, unknown)
    /// </summary>
    public enum PLANE_TYPE
    {
        FLOOR,
        HIT_HORIZONTAL,
		HIT_VERTICAL,
		HIT_UNKNOWN
    };

    /// <summary>
    /// Structure that defines a plane
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneData
    {
        public sl.ERROR_CODE ErrorCode;
        public ZEDPlaneGameObject.PLANE_TYPE Type;
        public Vector3 PlaneNormal;
        public Vector3 PlaneCenter;
        public Vector3 PlaneTransformPosition;
        public Quaternion PlaneTransformOrientation;
        public Vector4 PlaneEquation;
        public Vector2 Extents;
        public int BoundsSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public Vector3[] Bounds; //max 256 points
    }

    public ZEDPlaneGameObject.PlaneData planeData;


    private bool isCreated = false;
    public bool IsCreated
    {
        get { return isCreated; }
    }
    public Vector3 worldNormal { get; private set; }

    public Vector3 worldCenter
    {
        get
        {
            return gameObject.transform.position;
        }
    }

    private void SetComponents(PlaneData plane, Vector3[] vertices, int[] triangles, Material rendermaterial)
    {
        //Create the mesh filter to render the mesh
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null)
            mf = gameObject.AddComponent<MeshFilter>();

        //Eliminate superfluous verts
        int highestvertindex = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] > highestvertindex) highestvertindex = triangles[i];
        }
        System.Array.Resize(ref vertices, highestvertindex + 1);


        //Calculate the UVs for the vertices based on world space so they line up with other planes
        Vector2[] uvs = new Vector2[vertices.Length];
        Quaternion rotatetobackward = Quaternion.FromToRotation(worldNormal, Vector3.back);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 upwardcoords = rotatetobackward * (vertices[i] + worldCenter);
            uvs[i] = new Vector2(upwardcoords.x, upwardcoords.y);
        }

        mf.mesh.Clear();
        mf.mesh.vertices = vertices;
        mf.mesh.triangles = triangles;
        mf.mesh.uv = uvs;
        mf.mesh.RecalculateNormals();
        mf.mesh.RecalculateBounds();

        // Get the mesh renderer and set properties
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = rendermaterial;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.enabled = true;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // Get the mesh collider and set properties
        MeshCollider mc = gameObject.GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();

        // Set the mesh for the collider.
        mc.sharedMesh = mf.mesh;

    }

    /// <summary>
    ///  Create the plane as a gameobject and fills the internal structure planeData for future access
    /// </summary>
    /// <param name="holder">Holder.</param>
    /// <param name="plane">PlaneData fills by findXXXPlane().</param>
    /// <param name="vertices">Vertices of the mesh</param>
    /// <param name="triangles">Triangles of the mesh</param>
    public void Create(PlaneData plane, Vector3[] vertices, int[] triangles, int opt_count)
    {
        Create(plane, vertices, triangles, opt_count, GetDefaultMaterial(plane.Type));
    }

    /// <summary>
    ///  Create the plane as a gameobject and fills the internal structure planeData for future access
    /// </summary>
    /// <param name="holder">Holder.</param>
    /// <param name="plane">PlaneData fills by findXXXPlane().</param>
    /// <param name="vertices">Vertices of the mesh</param>
    /// <param name="triangles">Triangles of the mesh</param>
    public void Create(PlaneData plane, Vector3[] vertices, int[] triangles, int opt_count, Material rendermaterial)
    {
        planeData.ErrorCode = plane.ErrorCode;
        planeData.Type = plane.Type;
        planeData.PlaneNormal = plane.PlaneNormal;
        planeData.PlaneCenter = plane.PlaneCenter;
        planeData.PlaneTransformPosition = plane.PlaneTransformPosition;
        planeData.PlaneTransformOrientation = plane.PlaneTransformOrientation;
        planeData.PlaneEquation = plane.PlaneEquation;
        planeData.Extents = plane.Extents;

        //Set normal in world space
        Camera leftCamera = ZEDManager.Instance.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        worldNormal = leftCamera.transform.TransformDirection(planeData.PlaneNormal);

        ///Create the MeshCollider
        gameObject.AddComponent<MeshCollider>().sharedMesh = null;

        if (plane.Type != PLANE_TYPE.FLOOR)
			gameObject.name = "Hit Plane " + opt_count;      
        else
			gameObject.name = "Floor Plane";


        gameObject.layer = sl.ZEDCamera.TagOneObject;

        SetComponents(plane, vertices, triangles, rendermaterial);

        isCreated = true;
    }


    /// <summary>
    /// Updates the floor plane using "force" mode
    /// </summary>
    /// <returns><c>true</c>, if floor plane was updated, <c>false</c> otherwise.</returns>
    /// <param name="force">If set to <c>true</c> force the update. Is set to false, update only if new plane/mesh is bigger or contains the old one</param>
    /// <param name="plane">Plane.</param>
    /// <param name="vertices">Vertices.</param>
    /// <param name="triangles">Triangles.</param>
    public bool UpdateFloorPlane(bool force, ZEDPlaneGameObject.PlaneData plane, Vector3[] vertices, int[] triangles, Material rendermaterial = null)
    {
        //Needs to be created
        if (gameObject == null)
            return false;

        bool need_update = false;
        //Check mesh
        if (!force)
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
        else
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
    /// Enable the Mesh collider of the game object
    /// </summary>
    /// <param name="c">If set to <c>true</c> c.</param>
    public void SetPhysics(bool c)
    {
        MeshCollider mc = gameObject.GetComponent<MeshCollider>();
        if (mc != null)
            mc.enabled = c;
    }

    /// <summary>
    /// Sets the Plane visible on the Scene
    /// </summary>
    /// <param name="c">If set to <c>true</c> c.</param>
    public void SetVisible(bool c)
    {
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = c;
    }

    /// <summary>
    /// Gets the size of the bounding rect that fits the plane
    /// </summary>
    /// <returns>The scale.</returns>
    public Vector2 GetScale()
    {
        return planeData.Extents;
    }


    public Bounds GetBounds()
    {
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf != null)
            return mf.mesh.bounds;
        else
            return new Bounds(gameObject.transform.position, Vector3.zero);
    }

    /// <summary>
    /// Destroy this instance.
    /// </summary>
    public void Destroy()
    {
        if (gameObject != null)
            GameObject.Destroy(gameObject);

    }

    /// <summary>
    /// Determines whether this floor plane is visible by any camera.
    /// </summary>
    /// <returns><c>true</c> if this instance is floor plane visible; otherwise, <c>false</c>.</returns>
    public bool IsFloorPlaneVisible()
    {
        return gameObject.GetComponent<MeshRenderer>().isVisible;
    }

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
			case PLANE_TYPE.HIT_HORIZONTAL :
			case PLANE_TYPE.HIT_VERTICAL :
		    case PLANE_TYPE.HIT_UNKNOWN :
			     // Hit planes are pink
                defaultmaterial.SetColor("_WireColor", new Color(221.0f / 255.0f, 20.0f / 255.0f, 149.0f / 255.0f, 174.0f / 255.0f));
                break;

            default:
                //Unknown planes are white
                defaultmaterial.SetColor("_WireColor", new Color(1, 1, 1, 174.0f / 255.0f));
                break;
        }

        return defaultmaterial;
    }
}
