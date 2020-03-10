//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Displays the point cloud of the real world in front of the camera.
/// Can be attached to any GameObject in a scene, but requires a ZEDManager component to exist somewhere. 
/// </summary>
public class ZEDPointCloudManager : MonoBehaviour
{
    /// <summary>
    /// Set to a camera if you do not want that camera to see the point cloud. 
    /// </summary>
    [Tooltip("Set to a camera if you do not want that camera to see the point cloud. ")]
    public Camera hiddenObjectFromCamera;

    /// <summary>
    /// Number of points displayed. Usually equal to the width * height of the ZED's resolution (eg. 1280 * 720). 
    /// </summary>
    private int numberPoints = 0;

    /// <summary>
    /// Instance of the ZEDManager interface
    /// </summary>
    public ZEDManager zedManager = null;

	/// <summary>
	/// zed Camera controller by zedManager
	/// </summary>
	private sl.ZEDCamera zed = null;

    /// <summary>
    /// Texture that holds the 3D position of the points.
    /// </summary>
    private Texture2D XYZTexture;

    /// <summary>
    /// Texture that holds the colors of each point.
    /// </summary>
    private Texture2D colorTexture;

    /// <summary>
    /// Temporary copy/buffer of the XYZTexture to stop the point cloud in a defined moment.
    /// </summary>
    private RenderTexture XYZTextureCopy = null;

    /// <summary>
    /// Temporary copy/buffer of the ColorTexture to stop the point cloud in a defined moment.
    /// </summary>
    private RenderTexture ColorTextureCopy = null;

    /// <summary>
    /// Material used to display the point cloud. Usually Mat_ZED_PointCloud.
    /// </summary>
    public Material mat;

    /// <summary>
    /// Cached property index of _Position shader property, so we only look it up once. Do not use. 
    /// </summary>
    private static int? _positionid;
    /// <summary>
    /// Returns the property index of the _Position property, and looks it up if it hasn't been looked up yet. 
    /// </summary>
    private static int positionID
    {
        get
        {
            if (_positionid == null)
            {
                _positionid = Shader.PropertyToID("_Position");
            }
            return (int)_positionid;
        }
    }
    /// <summary>
    /// Cached property index of the _ColorTex shader property, so we only look it up once. Use colorTexID instead.
    /// </summary>
    private static int? _colortexid;
    /// <summary>
    /// Returns the property index of the _ColorTex property, which is the RGB color texture from the ZED.
    /// </summary>
    private static int colorTexID
    {
        get
        {
            if (_colortexid == null)
            {
                _colortexid = Shader.PropertyToID("_ColorTex");
            }
            return (int)_colortexid;
        }
    }
    /// <summary>
    /// Cached property index of _XYZTex shader property, so we only look it up once. Use xyzTexID instead.
    /// </summary>
    private static int? _xyztexid;
    /// <summary>
    /// Returns the property index of the _XYZTex property, which is the XYZ position of each pixel relative to the ZED. 
    /// </summary>
    private static int xyzTexID
    {
        get
        {
            if (_xyztexid == null)
            {
                _xyztexid = Shader.PropertyToID("_XYZTex");
            }
            return (int)_xyztexid;
        }
    }

    /// <summary>
    /// Whether the point cloud should be visible or not. 
    /// </summary>
    [Tooltip("Whether the point cloud should be visible or not. ")]
    public bool display = true;

    /// <summary>
    /// Whether to update the point cloud. 
    /// If false, the point cloud will display the content of the temp textures from the last update. 
    /// </summary>
    [Tooltip("Whether to update the point cloud. If false, the point cloud will display the content of the temp textures from the last update. ")]
    public bool update = true;

    /// <summary>
    /// Flag to check if the update has changed state.
    /// </summary>
    private bool previousUpdate = true;

    void Start()
    {
        if(zedManager == null)
        {
            zedManager = FindObjectOfType<ZEDManager>();
            if(ZEDManager.GetInstances().Count > 1) //We chose a ZED arbitrarily, but there are multiple cams present. Warn the user. 
            {
                Debug.Log("Warning: " + gameObject.name + "'s zedManager was not specified, so the first available ZEDManager instance was " +
                    "assigned. However, there are multiple ZEDManager's in the scene. It's recommended to specify which ZEDManager you want to " +
                    "use to display a point cloud.");
            }
        }

		if (zedManager != null)
			zed = zedManager.zedCamera;
    }

    // Update is called once per frame
    void Update()
    {
		if (zed.IsCameraReady) //Don't do anything unless the ZED has been initialized. 
        {
            if (numberPoints == 0)
            {
                //Create the textures. These will be updated automatically by the ZED.
                //We'll copy them each frame into XYZTextureCopy and ColorTextureCopy, which will be the ones actually displayed. 
                XYZTexture = zed.CreateTextureMeasureType(sl.MEASURE.XYZ);
                colorTexture = zed.CreateTextureImageType(sl.VIEW.LEFT);
                numberPoints = zed.ImageWidth * zed.ImageHeight;

                //Load and set the material properties.
                if (mat == null)
                {
                    mat = new Material(Resources.Load("Materials/PointCloud/Mat_ZED_PointCloud") as Material);
                }
                if (mat != null)
                {
                    //mat.SetTexture("_XYZTex", XYZTexture);
                    mat.SetTexture(xyzTexID, XYZTexture);
                    //mat.SetTexture("_ColorTex", colorTexture);
                    mat.SetTexture(colorTexID, colorTexture);

                }
            }

        //If stop updated, create new render texture and fill them with the textures from the ZED.
        // These textures will be displayed as they are not updated
        if (!update && previousUpdate != update)
        {
            if (XYZTextureCopy == null)
            {
                XYZTextureCopy = new RenderTexture(XYZTexture.width, XYZTexture.height, 0, RenderTextureFormat.ARGBFloat);
            }

            if (ColorTextureCopy == null)
            {
                ColorTextureCopy = new RenderTexture(colorTexture.width, colorTexture.height, 0, RenderTextureFormat.ARGB32);
            }

            //Copy the new textures into the buffers. 
            Graphics.Blit(XYZTexture, XYZTextureCopy);
            Graphics.Blit(colorTexture, ColorTextureCopy);

            if (mat != null)
            {
                //mat.SetTexture("_XYZTex", XYZTextureCopy);
                mat.SetTexture(xyzTexID, XYZTextureCopy);
                //mat.SetTexture("_ColorTex", ColorTextureCopy);
                mat.SetTexture(colorTexID, ColorTextureCopy);
            }
        }
        //Send the textures to the material/shader. 
        if (update && previousUpdate != update && mat != null)
        {
            //mat.SetTexture("_XYZTex", XYZTexture);
            mat.SetTexture(xyzTexID, XYZTexture);
            //mat.SetTexture("_ColorTex", colorTexture);
            mat.SetTexture(colorTexID, colorTexture);


        }
        previousUpdate = update;
        }
    }


    void OnApplicationQuit()
    {
        //Free up memory. 
        mat = null;
        if (XYZTextureCopy != null)
        {
            XYZTextureCopy.Release();
        }

        if (ColorTextureCopy != null)
        {
            ColorTextureCopy.Release();
        }
    }

    void OnRenderObject()
    {
        if (mat != null)
        {
            if (hiddenObjectFromCamera == Camera.current) return;
            
            if (!display) return; //Don't draw anything if the user doesn't want to. 

            //mat.SetMatrix("_Position", transform.localToWorldMatrix);
            mat.SetMatrix(positionID, transform.localToWorldMatrix);
            mat.SetPass(0);

            #if UNITY_2019_1_OR_NEWER
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, numberPoints);
            #else
            Graphics.DrawProcedural(MeshTopology.Points, 1, numberPoints);
            #endif
        }
    }

}
