using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]

public class VertHandler : MonoBehaviour
{
    Mesh mesh;
    public Vector3[] verts;
    Vector3 vertPos;
    public GameObject[] handles;
    public List<GameObject> holders = new List<GameObject>();

    bool canMove;
    bool canRotate;
    private Vector3 screenPoint;
    private Vector3 offset;
    private Camera LeftCamera;
    private ZEDManager zedManager;
    [HideInInspector]
    public Transform draggingObject;
    public bool handlersReady = false;

    void OnEnable()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        mesh.Clear();

        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }


        float length = 1.5f;
        float width = 1.5f;
        float height = 1.5f;

        #region Vertices
        Vector3 p0 = new Vector3(-length * .5f, -width * .5f, height * .5f);
        Vector3 p1 = new Vector3(length * .5f, -width * .5f, height * .5f);
        Vector3 p2 = new Vector3(length * .5f, -width * .5f, -height * .5f);
        Vector3 p3 = new Vector3(-length * .5f, -width * .5f, -height * .5f);

        Vector3 p4 = new Vector3(-length * .5f, width * .5f, height * .5f);
        Vector3 p5 = new Vector3(length * .5f, width * .5f, height * .5f);
        Vector3 p6 = new Vector3(length * .5f, width * .5f, -height * .5f);
        Vector3 p7 = new Vector3(-length * .5f, width * .5f, -height * .5f);

        Vector3[] vertices = new Vector3[]
        {
	// Bottom
	p0, p1, p2, p3,
 
	// Left
	p7, p4, p0, p3,
 
	// Front
	p4, p5, p1, p0,
 
	// Back
	p6, p7, p3, p2,
 
	// Right
	p5, p6, p2, p1,
 
	// Top

        };
        #endregion

        #region Normales
        Vector3 up = Vector3.up;
        Vector3 down = Vector3.down;
        Vector3 front = Vector3.forward;
        Vector3 back = Vector3.back;
        Vector3 left = Vector3.left;
        Vector3 right = Vector3.right;

        Vector3[] normales = new Vector3[]
        {
	// Bottom
	down, down, down, down,
 
	// Left
	left, left, left, left,
 
	// Front
	front, front, front, front,
 
	// Back
	back, back, back, back,
 
	// Right
	right, right, right, right,
 
	// Top
        };
        #endregion

        #region UVs
        Vector2 _00 = new Vector2(0f, 0f);
        Vector2 _10 = new Vector2(1f, 0f);
        Vector2 _01 = new Vector2(0f, 1f);
        Vector2 _11 = new Vector2(1f, 1f);

        Vector2[] uvs = new Vector2[]
        {
	// Bottom
	_11, _01, _00, _10,
 
	// Left
	_11, _01, _00, _10,
 
	// Front
	_11, _01, _00, _10,
 
	// Back
	_11, _01, _00, _10,
 
	// Right
	_11, _01, _00, _10,
 
	// Top

        };
        #endregion

        #region Triangles
        int[] triangles = new int[]
        {
	// Bottom
			
 
	// Left
	3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
    3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
 
	// Front
	3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
    3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
 
	// Back
	3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
    3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
 
	// Right
	3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
    3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
 
	// Top


        };
        #endregion

        mesh.vertices = vertices;
        mesh.normals = normales;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        /*
        var vertices = mesh.vertices;
        var uv = mesh.uv;
        var normals = mesh.normals;
        var szV = vertices.Length;
        var newVerts = new Vector3[szV * 2];
        var newUv = new Vector2[szV * 2];
        var newNorms = new Vector3[szV * 2];
        for (var j = 0; j < szV; j++)
        {
            // duplicate vertices and uvs:
            newVerts[j] = newVerts[j + szV] = vertices[j];
            newUv[j] = newUv[j + szV] = uv[j];
            // copy the original normals...
            newNorms[j] = normals[j];
            // and revert the new ones
            newNorms[j + szV] = -normals[j];
        }
        var triangles = mesh.triangles;
        var szT = triangles.Length;
        var newTris = new int[szT * 2]; // double the triangles
        for (var i = 0; i < szT; i += 3)
        {
            // copy the original triangle
            newTris[i] = triangles[i];
            newTris[i + 1] = triangles[i + 1];
            newTris[i + 2] = triangles[i + 2];
            // save the new reversed triangle
            var j = i + szT;
            newTris[j] = triangles[i] + szV;
            newTris[j + 2] = triangles[i + 1] + szV;
            newTris[j + 1] = triangles[i + 2] + szV;
        }
        mesh.vertices = newVerts;
        mesh.uv = newUv;
        mesh.normals = newNorms;
        mesh.triangles = newTris; // assign triangles last!
        */

        //Making a copy of the mesh but inside Out
        //Taking the vertices from the mesh, creating a holder for each corner, and assigning them inside.
        verts = mesh.vertices;
        int corner = 0;
        bool reverse = false; ;
        foreach (Vector3 vert in verts)
        {
            vertPos = vert;
            GameObject handle = new GameObject("handle");
            handle.transform.parent = transform;
            handle.transform.position = vertPos;
            handle.tag = "handle";

            bool holder = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name.Contains(vertPos.ToString()))
                {
                    handle.transform.parent = transform.GetChild(i);
                    handle.transform.localPosition = Vector3.zero;
                    holder = true;
                    break;
                }
                else
                {
                    holder = false;
                }
            }

            if(holder == false)
            {
                GameObject handleHolder = new GameObject("handleHolder " + vertPos.ToString() +" Side" + corner.ToString());
                handleHolder.transform.parent = transform;
                handleHolder.transform.localPosition = vertPos;
                handleHolder.AddComponent<VertController>();
                handleHolder.AddComponent<SphereCollider>();
                handleHolder.GetComponent<SphereCollider>().radius = 0.1f;
                handle.transform.parent = handleHolder.transform;
                handle.transform.localPosition = Vector3.zero;

                if (corner == 3 && reverse)
                {
                    corner = -1;
                    reverse = false;
                }
                if (corner < 4 && !reverse)
                    corner += 1;
                if (corner == 4 && !reverse)
                {
                    corner = 3;
                    reverse = true;
                }
            }
        }

        handles = GameObject.FindGameObjectsWithTag("handle");
        for (int i = 0; i < transform.childCount; i++)
            holders.Add(transform.GetChild(i).gameObject);

        if (zedManager.IsZEDReady)
            LeftCamera = zedManager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        if(LeftCamera == null)
            LeftCamera = Camera.main;

        handlersReady = true;

        transform.localPosition = new Vector3(10f, 10f, 10f);
    }

    GameObject newPivot = null;
    void Update()
    {
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = handles[i].transform.parent.localPosition;
        }
        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = LeftCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.name.Contains("handleHolder"))
                {
                    draggingObject = hit.transform;
                    canMove = true;
                    Cursor.visible = false;
                }
                if (hit.transform.name.Contains("Zone"))
                {
                    GameObject pivot = new GameObject("pivot ");
                    pivot.transform.position = hit.point;
                    draggingObject = pivot.transform;
                    transform.parent = draggingObject;
                    canMove = true;
                    Cursor.visible = false;
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = LeftCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.name.Contains("Zone"))
                {
                    draggingObject = hit.transform;
                    var tmp = (holders[0].transform.position + holders[1].transform.position + holders[2].transform.position + holders[3].transform.position) / 4;
                    GameObject pivot = new GameObject("pivot ");
                    pivot.transform.position = tmp;
                    newPivot = pivot;
                    transform.parent = newPivot.transform;
                    canRotate = true;
                    Cursor.visible = false;
                }
            }
        }

        if(canRotate)
        {
            newPivot.transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * 100);
        }

        if (canMove)
        {
            float planeY = 0;

            planeY = draggingObject.position.y;
            Plane plane = new Plane(Vector3.up, Vector3.up * planeY); // ground plane

            Ray ray = LeftCamera.ScreenPointToRay(Input.mousePosition);

            float distance; // the distance from the ray origin to the ray intersection of the plane
            if (plane.Raycast(ray, out distance))
            {
                draggingObject.position = ray.GetPoint(distance); // distance along the ray
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            draggingObject = null;
            canMove = false;
            Destroy(gameObject.GetComponent<BoxCollider>());
            gameObject.AddComponent<BoxCollider>();
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            draggingObject = null;
            canRotate = false;
            transform.parent = null;
            Destroy(newPivot);
            newPivot = null;
            Cursor.visible = true;
        }
    }
}
