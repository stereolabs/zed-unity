using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates lines and meshes to display the frustum of the specified camera. 
/// Used to draw the frustum in the MR Calibration scene. 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FrustumDisplay : MonoBehaviour
{
    /// <summary>
    /// ZEDManager instance whose OnZEDReady event we subscribe to to know when to draw the frustum. 
    /// </summary>
    [Tooltip("ZEDManager instance whose OnZEDReady event we subscribe to to know when to draw the frustum. ")]
    public ZEDManager zedManager;
    /// <summary>
    /// Camera whose frustum to draw. Normally the left camera in a ZED_Rig_Mono or ZED_Rig_Stereo prefab. 
    /// </summary>
    [Tooltip("Camera whose frustum to draw. Normally the left camera in a ZED_Rig_Mono or ZED_Rig_Stereo prefab. ")]
    public Camera cam;

    /// <summary>
    /// How far away to draw the camera's near plane. We don't use the real one as it's sometimes not visually helpful.
    /// </summary>
    [Tooltip("How far away to draw the camera's near plane. We don't use the real one as it's sometimes not visually helpful.")]
    public float nearPlaneRendDist = 0.1f;
    /// <summary>
    /// How far away to draw the camera's far plane. We don't use the real one as it'll usually make the frustum way too big to be helpful.
    /// </summary>
    [Tooltip("How far away to draw the camera's far plane. We don't use the real one as it'll usually make the frustum way too big to be helpful.")]
    public float farPlaneRendDist = 0.75f;

    /// <summary>
    /// Line renderer object used to draw the near plane. Will also be used as a basis for the edges connecting the near and far plane corners.
    /// </summary>
    [Tooltip("Line renderer object used to draw the near plane. Will also be used as a basis for the edges connecting the near and far plane corners.")]
    public LineRenderer nearPlaneLineRend;

    /// <summary>
    /// Material used to draw the lines on the sides of the frustum. 
    /// </summary>
    [Tooltip("Material used to draw the lines on the sides of the frustum. ")]
    public Material edgeLineMat;
    /// <summary>
    /// Material used to draw the planes on the sides of the frustum. 
    /// </summary>
    [Tooltip("Material used to draw the planes on the sides of the frustum. ")]
    public Material sidePlaneMat;

    /// <summary>
    /// Color (with alpha) of the edge lines on the sides of the frustum, nearest to the camera. 
    /// </summary>
    [Tooltip("Color (with alpha) of the edge lines on the sides of the frustum, nearest to the camera. ")]
    [Space(5)]
    public Color edgeLineStartColor = Color.cyan;
    /// <summary>
    /// Color (with alpha) of the edge lines on the sides of the frustum, furthest to the camera. Used to create fade-out effect.
    /// </summary>
    [Tooltip("Color (with alpha) of the edge lines on the sides of the frustum, furthest to the camera. Used to create fade-out effect.")]
    public Color edgeLineEndColor = new Color(0, 1, 1, 0.2f);

    private LineRenderer topLeftLineRend;
    private LineRenderer topRightLineRend;
    private LineRenderer bottomLeftLineRend;
    private LineRenderer bottomRightLineRend;

    private MeshFilter topPlaneMF;
    private MeshFilter bottomPlaneMF;
    private MeshFilter leftPlaneMF;
    private MeshFilter rightPlaneMF;

    private List<MeshRenderer> planeRenderers = new List<MeshRenderer>();

    // Use this for initialization
    void Awake ()
    {
        if (!zedManager) ZEDManager.GetInstance(sl.ZED_CAMERA_ID.CAMERA_ID_01);
        if(!cam) cam = zedManager.GetLeftCamera();

        zedManager.OnZEDReady += DrawFrustum;

        if(!nearPlaneLineRend) nearPlaneLineRend = GetComponent<LineRenderer>();

        CreateEdgeLinesAndPlanes();
	}
	
    /// <summary>
    /// Use the camera's frustum to draw a visualization of it, using line renderers and mesh objects mostly created
    /// earlier by CreateEdgeLinesAndPlanes().
    /// </summary>
    private void DrawFrustum()
    {
        //Calculate the near plane.
        Vector3[] nearplane = new Vector3[4]; 
        //cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearplane);
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), nearPlaneRendDist, Camera.MonoOrStereoscopicEye.Mono, nearplane);

        nearPlaneLineRend.positionCount = 4;
        nearPlaneLineRend.SetPositions(nearplane);
        nearPlaneLineRend.loop = true; //Important to have Loop enabled or else one side will be open. 

        if (edgeLineMat != null) nearPlaneLineRend.material = edgeLineMat;

        //Calculate the far plane that the ends extend to. 
        Vector3[] farplane = new Vector3[4];  
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), farPlaneRendDist, Camera.MonoOrStereoscopicEye.Mono, farplane);

        bottomLeftLineRend.SetPositions(new Vector3[2] { nearplane[0], farplane[0] });
        topLeftLineRend.SetPositions(new Vector3[2] { nearplane[1], farplane[1] });
        topRightLineRend.SetPositions(new Vector3[2] { nearplane[2], farplane[2] });
        bottomRightLineRend.SetPositions(new Vector3[2] { nearplane[3], farplane[3] });

        //Set colors on edge lines. 
        bottomLeftLineRend.startColor = edgeLineStartColor;
        topLeftLineRend.startColor = edgeLineStartColor;
        topRightLineRend.startColor = edgeLineStartColor;
        bottomRightLineRend.startColor = edgeLineStartColor;

        bottomLeftLineRend.endColor = edgeLineEndColor;
        topLeftLineRend.endColor = edgeLineEndColor;
        topRightLineRend.endColor = edgeLineEndColor;
        bottomRightLineRend.endColor = edgeLineEndColor;

        //Force the near plane box to have the same colors, but in this case without the fade that comes from the end color. 
        nearPlaneLineRend.startColor = edgeLineStartColor;
        nearPlaneLineRend.endColor = edgeLineStartColor; //Not a typo. We don't want the fade. 

        //Create the plane meshes on the sides and assign them. 
        topPlaneMF.mesh = CreatePlaneMesh(nearplane[1], nearplane[2], farplane[1], farplane[2]);
        bottomPlaneMF.mesh = CreatePlaneMesh(nearplane[0], nearplane[3], farplane[0], farplane[3]);
        leftPlaneMF.mesh = CreatePlaneMesh(nearplane[0], nearplane[1], farplane[0], farplane[1]);
        rightPlaneMF.mesh = CreatePlaneMesh(nearplane[2], nearplane[3], farplane[2], farplane[3]);
    }

    /// <summary>
    /// Creates necessary objects for drawing the edge lines and planes, and applies basic settings. 
    /// </summary>
    private void CreateEdgeLinesAndPlanes()
    {
        GameObject blgo = new GameObject("Bottom Left Edge");
        GameObject tlgo = new GameObject("Top Left Edge");
        GameObject trgo = new GameObject("Top Right Edge");
        GameObject brgo = new GameObject("Bottom Right Edge");

        blgo.transform.parent = transform;
        tlgo.transform.parent = transform;
        trgo.transform.parent = transform;
        brgo.transform.parent = transform;

        blgo.layer = gameObject.layer;
        tlgo.layer = gameObject.layer;
        trgo.layer = gameObject.layer;
        brgo.layer = gameObject.layer;

        blgo.transform.localPosition = Vector3.zero;
        tlgo.transform.localPosition = Vector3.zero;
        trgo.transform.localPosition = Vector3.zero;
        brgo.transform.localPosition = Vector3.zero;

        blgo.transform.localRotation = Quaternion.identity;
        tlgo.transform.localRotation = Quaternion.identity;
        trgo.transform.localRotation = Quaternion.identity;
        brgo.transform.localRotation = Quaternion.identity;

        bottomLeftLineRend = blgo.AddComponent<LineRenderer>();
        topLeftLineRend = tlgo.AddComponent<LineRenderer>();
        topRightLineRend = trgo.AddComponent<LineRenderer>();
        bottomRightLineRend = brgo.AddComponent<LineRenderer>();

        bottomLeftLineRend.widthMultiplier = nearPlaneLineRend.widthMultiplier;
        topLeftLineRend.widthMultiplier = nearPlaneLineRend.widthMultiplier;
        topRightLineRend.widthMultiplier = nearPlaneLineRend.widthMultiplier;
        bottomRightLineRend.widthMultiplier = nearPlaneLineRend.widthMultiplier;

        bottomLeftLineRend.useWorldSpace = false;
        topLeftLineRend.useWorldSpace = false;
        topRightLineRend.useWorldSpace = false;
        bottomRightLineRend.useWorldSpace = false;


        if(edgeLineMat != null)
        {
            bottomLeftLineRend.material = edgeLineMat;
            topLeftLineRend.material = edgeLineMat;
            topRightLineRend.material = edgeLineMat;
            bottomRightLineRend.material = edgeLineMat;
        }

        //Create MeshFilters and cache them. 
        //We add them to the corner GameObjects such that it's clockwise one place of each corner, though it doesn't technically matter. 
        topPlaneMF = tlgo.AddComponent<MeshFilter>();
        bottomPlaneMF = brgo.AddComponent<MeshFilter>();
        leftPlaneMF = blgo.AddComponent<MeshFilter>();
        rightPlaneMF = trgo.AddComponent<MeshFilter>();

        //Create MeshRenderers, but they'll all have the same mat so their position doesn't MATter (lol) either.
        planeRenderers.Clear(); //In case this got called twice somehow. 
        planeRenderers.Add(tlgo.AddComponent<MeshRenderer>());
        planeRenderers.Add(brgo.AddComponent<MeshRenderer>());
        planeRenderers.Add(blgo.AddComponent<MeshRenderer>());
        planeRenderers.Add(trgo.AddComponent<MeshRenderer>());

        //Assign the plane materials. 
        if (sidePlaneMat != null)
        {
            foreach (MeshRenderer rend in planeRenderers)
            {
                rend.material = sidePlaneMat;
            }
        }

    }

    /// <summary>
    /// Creates a single plane mesh with the specified corners.
    /// </summary>
    private Mesh CreatePlaneMesh(Vector3 botleft, Vector3 botright, Vector3 topleft, Vector3 topright)
    {
        Vector3[] verts = new Vector3[] { botleft, botright, topleft, topright };

        int[] tris = new int[] { 0, 2, 1, 2, 3, 1 };

        //Vector2[] uvs = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 1) };
        float width = botright.x - botleft.x;

        float ydif = topleft.y - botleft.y;
        float zdif = topleft.z - botleft.z;
        //float height = topleft.z - botleft.z; 
        float height = Mathf.Sqrt((ydif * ydif) + (zdif * zdif));

        Vector2[] uvs = new Vector2[] { new Vector2(0, 0), new Vector2(width, 0), new Vector2(0, height), new Vector2(width, height) };

        Mesh returnmesh = new Mesh();
        returnmesh.vertices = verts;
        returnmesh.triangles = tris;
        returnmesh.uv = uvs;

        return returnmesh;
    }
}
