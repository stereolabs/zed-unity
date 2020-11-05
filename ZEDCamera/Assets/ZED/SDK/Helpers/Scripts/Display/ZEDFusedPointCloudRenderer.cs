//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;

/// <summary>
/// Displays the point cloud of the real world in front of the camera.
/// Can be attached to any GameObject in a scene, but requires a ZEDManager component to exist somewhere. 
/// </summary>
/// 
namespace sl
{
    public class ZEDFusedPointCloudRenderer : MonoBehaviour
    {
        /// <summary>
        /// Instance of the ZEDManager interface
        /// </summary>
        public ZEDManager zedManager = null;

        /// <summary>
        /// Point size of the Fused Point cloud. If > 0, then disk shader will be used to generate spheres around points
        /// </summary>
        public float _pointSize = 0.001f;

        /// <summary>
        /// Fused point cloud resolution (sent to spatial mapping parameters)
        /// </summary>
        public float resolution = 0.5f;

        /// <summary>
        /// Fused point cloud range (sent to spatial mapping parameters)
        /// </summary>
        public float range = 5.0f;

        /// <summary>
        /// zed Camera controller by zedManager.
        /// </summary>
        private sl.ZEDCamera zed = null;

        /// <summary>
        /// Point Material
        /// </summary>
        private Material _pointMaterial;

        /// <summary>
        /// Disk material (sphere around point)
        /// </summary>
        private Material _diskMaterial;

         
        /// <summary>
        /// Array of points as point buffer (compute buffer). Send to shaders
        /// </summary>
        private ComputeBuffer _pointBuffer;

               
        /// <summary>
        /// Size of Vector4
        /// </summary>
        private const int elementSize = sizeof(float) * 4;

        /// <summary>
        /// Array of vertex points
        /// </summary>
        private Vector4[] vertPoints;

        
        private bool notStarted = true;
        private bool canUpdate = false;
        private float updateTime = 0;

        void Start()
        {
            if (zedManager == null)
            {
                zedManager = FindObjectOfType<ZEDManager>();
                if (ZEDManager.GetInstances().Count > 1) //We chose a ZED arbitrarily, but there are multiple cams present. Warn the user. 
                {
                    Debug.Log("Warning: " + gameObject.name + "'s zedManager was not specified, so the first available ZEDManager instance was " +
                        "assigned. However, there are multiple ZEDManager's in the scene. It's recommended to specify which ZEDManager you want to " +
                        "use to display a point cloud.");
                }
            }

            if (zedManager != null)
                zed = zedManager.zedCamera;

            if (_pointMaterial == null)
            {
                _pointMaterial = new Material(Resources.Load("Materials/PointCloud/Mat_ZED_FusedPC_Point") as Material);
            }

            if (_diskMaterial == null)
            {
                _diskMaterial = new Material(Resources.Load("Materials/PointCloud/Mat_ZED_FusedPC_Disk") as Material);
            }
 
            _diskMaterial.hideFlags = HideFlags.DontSave;
            _pointMaterial.hideFlags = HideFlags.DontSave;
            zedManager.OnGrab += startMap;

        }

        /// <summary>
        /// Start Map linked to grab callback. Wait for one frame to start point cloud fusion
        /// </summary>
        private void startMap()
        {
            if (zed != null && notStarted)
            {
                zed.EnableSpatialMapping(sl.SPATIAL_MAP_TYPE.FUSED_POINT_CLOUD, resolution, range);
                notStarted = false;
                canUpdate = true;
            }
               
        }

        void OnDestroy()
        {
            canUpdate = false;
           
            if (zed != null)
                zed.DisableSpatialMapping();

            if (_pointBuffer != null)
            {
                _pointBuffer.Release();
                _pointBuffer = null;
            }
            
            if (_pointMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_pointMaterial);
                    Destroy(_diskMaterial);

                }
                else
                {
                    DestroyImmediate(_pointMaterial);
                    DestroyImmediate(_diskMaterial);

                }
            }
        }

        /// <summary>
        /// Update to request Map every 1 second
        /// </summary>
        void Update()
        {
            if (zed.IsCameraReady && canUpdate) //Don't do anything unless the ZED has been initialized. 
            {
                updateTime += Time.deltaTime;
                if (updateTime >= 1)
                {
                    zed.RequestMesh();
                    updateTime = 0;
                }               
            }
        }

        /// <summary>
        /// On Render Fct
        /// </summary>
        void OnRenderObject()
        {
            if (zed != null)
            {
                int nbPoints = 0;
                zed.UpdateFusedPointCloud(ref nbPoints);
                if (nbPoints > 0)
                {
                    vertPoints = new Vector4[nbPoints];
                    zed.RetrieveFusedPointCloud(vertPoints);
                    
                    if (_pointBuffer != null)
                    {
                        _pointBuffer.Release();
                        _pointBuffer = null;
                    }

                    _pointBuffer = new ComputeBuffer(nbPoints, elementSize);
                    _pointBuffer.SetData(vertPoints);
                }
            }

            if (_pointBuffer != null)
            {
                //Draw with Point shader
                if (_pointSize == 0)
                {
                    _pointMaterial.SetPass(0);
                    _pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                    _pointMaterial.SetBuffer("_PointBuffer", _pointBuffer);
#if UNITY_2019_1_OR_NEWER
                    Graphics.DrawProceduralNow(MeshTopology.Points, _pointBuffer.count, 1);
#else
                    Graphics.DrawProcedural(MeshTopology.Points, _pointBuffer.count, 1);

#endif
                }
                //Draw with Disk shader
                else
                {
                    _diskMaterial.SetPass(0);

                    _diskMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);

                    _diskMaterial.SetBuffer("_PointBuffer", _pointBuffer);
                    _diskMaterial.SetFloat("_PointSize", _pointSize);
#if UNITY_2019_1_OR_NEWER
                    Graphics.DrawProceduralNow(MeshTopology.Points, _pointBuffer.count, 1);
#else
                    Graphics.DrawProcedural(MeshTopology.Points, _pointBuffer.count, 1);
#endif
                }
            }
        }

    }
}
