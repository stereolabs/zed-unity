using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used in the ZED planetarium sample to draw asteroids in the solar system's asteroid belt and rotate them. 
/// The asteroids are not instantiated as GameObjects, but drawn each frame with DrawMeshInstanced. 
/// Given there are many asteroids to draw, this reduces draw calls substantially along with some CPU overhead. 
/// </summary>
public class AsteroidsManager : MonoBehaviour
{
    /// <summary>
    /// Prefab containing the first asteroid mesh type.
    /// </summary>
    [Tooltip("Prefab containing the first asteroid mesh type.")]
    public GameObject asteroidsType1;

    /// <summary>
    /// Prefab containing the first asteroid mesh type.
    /// </summary>
    [Tooltip("Prefab containing the first asteroid mesh type.")]
    public GameObject asteroidsType2;

    /// <summary>
    /// How many asteroids of each type to draw. (There will be twice this number of asteroids in total). 
    /// </summary>
    [Tooltip("How many asteroids of each type to draw. (There will be twice this number of asteroids in total).")]
    public static int amount = 100;

    /// <summary>
    /// The radius of the asteroid belt. This is meters when the localscale of this object is 1,1,1. 
    /// </summary>
    [Tooltip("The radius of the asteroid belt. This is meters when the localscale of this object is 1,1,1. ")]
    public float radius = 1;

    /// <summary>
    /// How wide the asteroids can be spaced apart. Values too low or high may cause asteroids to intersect Mars or Jupiter. 
    /// </summary>
    [Tooltip("How wide the asteroids can be spaced apart. Values too low or high may cause asteroids to intersect Mars or Jupiter. ")]
    public float offset = 0.05f;

    /// <summary>
    /// Holds the transform of where the asteroid started when it was created (for asteroid type 1). 
    /// </summary>
    Matrix4x4[] listPositionsOrigin = new Matrix4x4[amount];

    /// <summary>
    /// Holds the current transforms of the type 1 asteroids, updated each frame based on the solar system's location and its orbit.
    /// </summary>
    Matrix4x4[] listPositions = new Matrix4x4[amount];

    /// <summary>
    /// Holds the transform of where the asteroid started when it was created (for asteroid type 1). 
    /// </summary>
    Matrix4x4[] listPositionsOrigin2 = new Matrix4x4[amount];

    /// <summary>
    /// Holds the current transforms of the type 2 asteroids, updated each frame based on the solar system's location and its orbit.
    /// </summary>
    Matrix4x4[] listPositions2 = new Matrix4x4[amount];


    void Start()
    {
        CreateAsteroids(listPositionsOrigin, amount, radius, offset); //Create all type 1 asteroids.
        CreateAsteroids(listPositionsOrigin2, amount, radius, offset); //Create all type 2 asteroids. 

    }

    /// <summary>
    /// Fills the first Matrix array with the origin positions and scales of randomly positioned asteroids.
    /// </summary>
    /// <param name="listPositionsOrigin">Array to be populated with matrixes of each asteroid's starting transform.</param>
    /// <param name="amount">How many asteroids to make.</param>
    /// <param name="radius">Radius of the asteroid belt/</param>
    /// <param name="offset">How far each asteroid can randomly deviate from the radius.</param>
    private void CreateAsteroids(Matrix4x4[] listPositionsOrigin, int amount, float radius, float offset)
    {
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
            float scale = Random.Range(0.1f, 0.3f);
            //model = glm::scale(model, glm::vec3(scale));

            // 3. rotation: add random rotation around a (semi)randomly picked rotation axis vector
            float rotAngle = (Random.Range(0, 100000) % 360);
            
            //Matrix4x4 m = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(scale, scale, scale));
            Matrix4x4 m = Matrix4x4.TRS(position, Quaternion.Euler(rotAngle, rotAngle, rotAngle), new Vector3(scale, scale, scale));

            listPositionsOrigin[i] = m;
        }
       
    }

    /// <summary>
    /// Called each frame to move, rotate and scale the asteroids based on this object's transform and their orbit. 
    /// </summary>
    /// <param name="listPositionsOrigin">All asteroids' initial matrixes.</param>
    /// <param name="listPositions">All asteroids' current matrixes.</param>
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
        foreach(ZEDManager manager in ZEDManager.GetInstances())
        {
            DrawAsteroids(manager);
        }

        
    }

    private void DrawAsteroids(ZEDManager manager)
    {
        Camera leftcamera = manager.GetLeftCamera();
        Camera rightcamera = manager.GetRightCamera();

        //Update positions and draw asteroids of type 1
        UpdatePosition(listPositionsOrigin, listPositions);
        Graphics.DrawMeshInstanced(asteroidsType1.GetComponent<MeshFilter>().sharedMesh,
                                    0, asteroidsType1.GetComponent<MeshRenderer>().sharedMaterial,
                                    listPositions,
                                    listPositions.Length,
                                    null,
                                    UnityEngine.Rendering.ShadowCastingMode.Off,
                                    false,
                                    gameObject.layer,
                                    leftcamera);
        if (manager.IsStereoRig)
        {
            Graphics.DrawMeshInstanced(asteroidsType1.GetComponent<MeshFilter>().sharedMesh,
                                        0,
                                        asteroidsType1.GetComponent<MeshRenderer>().sharedMaterial,
                                        listPositions,
                                        listPositions.Length,
                                        null,
                                        UnityEngine.Rendering.ShadowCastingMode.Off,
                                        false,
                                        gameObject.layer,
                                        rightcamera);
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
                                    gameObject.layer,
                                    leftcamera);
        if (manager.IsStereoRig)
        {
            Graphics.DrawMeshInstanced(asteroidsType2.GetComponent<MeshFilter>().sharedMesh,
                                    0,
                                    asteroidsType2.GetComponent<MeshRenderer>().sharedMaterial,
                                    listPositions2,
                                    listPositions2.Length,
                                    null,
                                    UnityEngine.Rendering.ShadowCastingMode.Off,
                                    false,
                                    gameObject.layer,
                                    rightcamera);
        }
    }

}
