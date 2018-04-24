using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidsManager : MonoBehaviour
{

    public GameObject asteroidsType1;
    public GameObject asteroidsType2;
    public Transform center;
    public static int amount = 50;
    public float radius = 1;
    public float offset = 0.0f;
    Matrix4x4[] listPositionsOrigin = new Matrix4x4[amount];
    Matrix4x4[] listPositions = new Matrix4x4[amount];

    Matrix4x4[] listPositionsOrigin2 = new Matrix4x4[amount];
    Matrix4x4[] listPositions2 = new Matrix4x4[amount];
    public ZEDManager manager;
    private Camera leftCamera = null;
    private Camera rightCamera = null;
    void Start()
    {
        CreateAsteroids(listPositionsOrigin, listPositions, amount, radius, offset);
        CreateAsteroids(listPositionsOrigin2, listPositions2, amount, radius, offset);

    }

    private void CreateAsteroids(Matrix4x4[] listPositionsOrigin, Matrix4x4[] listPositions, int amount, float radius, float offset)
    {
       // Matrix4x4 m2 = Matrix4x4.TRS(center.position, Quaternion.identity, Vector3.one);
       //
       // listPositionsOrigin.Add(m2);
        for (int i = 0; i < amount; ++i)
        {
            // 1. translation: displace along circle with 'radius' in range [-offset, offset]
            float angle = (float)i / (float)amount * 360.0f;
            float displacement = (Random.Range(0, 30) % (int)(2 * offset * 100)) / 100.0f - offset;
            float x = Mathf.Sin(angle) * radius + displacement;
            displacement = (Random.Range(0, 30) % (int)(2 * offset * 100)) / 100.0f - offset;
            float y = displacement * 2.0f; // keep height of asteroid field smaller compared to width of x and z
            displacement = (Random.Range(0, 30) % (int)(2 * offset * 100)) / 100.0f - offset;
            float z = Mathf.Cos(angle) * radius + displacement;
            Vector3 position = new Vector3(x, y, z);
            //position = center.TransformPoint(position);
            //// 2. scale: Scale between 0.05 and 0.25f
            float scale = Random.Range(0.2f, 0.6f);
            //model = glm::scale(model, glm::vec3(scale));

            // 3. rotation: add random rotation around a (semi)randomly picked rotation axis vector
            float rotAngle = (Random.Range(0, 100000) % 360);
            

            //Matrix4x4 m = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(scale, scale, scale));
            Matrix4x4 m = Matrix4x4.TRS(position, Quaternion.Euler(rotAngle, rotAngle, rotAngle), new Vector3(scale, scale, scale));

            listPositionsOrigin[i] = m;
        }
       
    }


    void UpdatePosition(Matrix4x4[] listPositionsOrigin, Matrix4x4[] listPositions)
    {
        for (int i = 0; i < listPositionsOrigin.Length; ++i)
        {

            listPositionsOrigin[i] = listPositionsOrigin[i] * Matrix4x4.TRS(Vector3.zero,
                 Quaternion.Euler(Time.deltaTime * Random.Range(0, 100), Time.deltaTime * Random.Range(0, 100), Time.deltaTime * Random.Range(0, 100)),
                Vector3.one); 
            listPositions[i] = transform.localToWorldMatrix* ( listPositionsOrigin[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(leftCamera == null)
        {
			if (manager) leftCamera = manager.GetLeftCameraTransform().GetComponent<Camera>();
        }
        if(rightCamera == null && ZEDManager.IsStereoRig)
        {
			if (manager) rightCamera = manager.GetRightCameraTransform().GetComponent<Camera>();
        }
        //Update positions and draw asteroids of type 1
        UpdatePosition(listPositionsOrigin, listPositions);
        Graphics.DrawMeshInstanced(asteroidsType1.GetComponent<MeshFilter>().sharedMesh, 
                                    0, asteroidsType1.GetComponent<MeshRenderer>().sharedMaterial,
                                    listPositions,
                                    listPositions.Length,
                                    null, 
                                    UnityEngine.Rendering.ShadowCastingMode.Off, 
                                    false, 
                                    8,
                                    leftCamera);
        if (ZEDManager.IsStereoRig)
        {
            Graphics.DrawMeshInstanced(asteroidsType1.GetComponent<MeshFilter>().sharedMesh,
                                        0,
                                        asteroidsType1.GetComponent<MeshRenderer>().sharedMaterial,
                                        listPositions,
                                        listPositions.Length,
                                        null,
                                        UnityEngine.Rendering.ShadowCastingMode.Off,
                                        false,
                                        10,
                                        rightCamera);
        }
        //Update positions and draw asteroids of type 2
        UpdatePosition(listPositionsOrigin2, listPositions2);
        Graphics.DrawMeshInstanced(asteroidsType2.GetComponent<MeshFilter>().sharedMesh, 
                                    0, 
                                    asteroidsType2.GetComponent<MeshRenderer>().sharedMaterial, 
                                    listPositions2,
                                    listPositions2.Length, 
                                    null, 
                                    UnityEngine.Rendering.ShadowCastingMode.Off, 
                                    false, 
                                    8,
                                    leftCamera);
        if (ZEDManager.IsStereoRig)
        {
            Graphics.DrawMeshInstanced(asteroidsType2.GetComponent<MeshFilter>().sharedMesh,
                                    0,
                                    asteroidsType2.GetComponent<MeshRenderer>().sharedMaterial,
                                    listPositions2,
                                    listPositions2.Length,
                                    null,
                                    UnityEngine.Rendering.ShadowCastingMode.Off,
                                    false,
                                    10,
                                    rightCamera);
        }
    }
}
