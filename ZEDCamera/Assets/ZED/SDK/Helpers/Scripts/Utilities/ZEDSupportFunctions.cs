using UnityEngine;
using System.Text;
using System.IO;

/// <summary>
/// Holds numerous static functions for getting info about the real world in 
/// specific places, to compare to the virtual world in the same place. 
/// <para>Examples include knowing where a real-world point you click on is in Unity world space, 
/// knowing what direction a real-world surface is facing, checking for collisions with the real world.</para>
/// </summary><remarks>
/// Functions that take a Vector2 for screen space (usually named "pixel" or something similar) are great for
/// when you want to click on the screen to test the real-world 'thing' you click on. To do this, use Input.mousePosition
/// and make a Vector2 out of the X and Y of the Vector3 it returns. 
/// Most functions take a Camera as a parameter. Use the one providing the image on the screen -
/// usually the left camera in the ZED rig, which can be easily retrieved using ZEDManager.GetLeftCameraTransform().
/// </remarks>
public class ZEDSupportFunctions
{

    /***********************************************************************************************
	 ********************             BASIC "GET" FUNCTIONS             ****************************
	 ***********************************************************************************************/
	public static bool IsVector3NaN(Vector3 input)
	{
		if (float.IsNaN (input.x) || float.IsNaN (input.y) || float.IsNaN (input.z))
			return true;
		else
			return false;
	}

    /// <summary>
    /// Gets the normal vector (the direction a surface is pointing) at a given screen-space pixel (i,j).  
    /// The normal can be given relative to the camera or the world. Returns false if outside camera's view frustum. 
    /// </summary>
    /// <param name="pixel">Pixel coordinates.</param>
    /// <param name="reference_frame">Reference frame given by the enum sl.REFERENCE_FRAME.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion.</param>
    /// <out>Normal to be filled.</out>
    /// <returns>True if successful, false otherwise.</returns>
	public static bool GetNormalAtPixel(sl.ZEDCamera zedCam,Vector2 pixel, sl.REFERENCE_FRAME reference_frame, Camera cam, out Vector3 normal)
    {
		normal = Vector3.zero;

		if (zedCam == null)
			return false;
		
        Vector4 n;
		bool r = zedCam.GetNormalValue(new Vector3(pixel.x,pixel.y, 0), out n);

		switch (reference_frame) {
		case sl.REFERENCE_FRAME.CAMERA: //Relative to the provided camera. 
			normal = n;
			break;

		case sl.REFERENCE_FRAME.WORLD: //Relative to the world. 
			normal = cam.transform.TransformDirection(n);
			break;
		default :
			normal = Vector3.zero;
			break;
		}

        return r;
    }

    /// <summary>
    /// Gets the normal vector (the direction a surface is pointing) at a world position (x,y,z). 
    /// The normal can be given relative to the camera or the world.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <param name="reference_frame"> Reference frame given by the enum sl.REFERENCE_FRAME.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <out>Normal vector to be filled.</out>
    /// <returns>True if successful, false otherwise.</returns>
	public static bool GetNormalAtWorldLocation(sl.ZEDCamera zedCam,Vector3 position, sl.REFERENCE_FRAME reference_frame,Camera cam, out Vector3 normal)
    {
		normal = Vector3.zero;

		if (zedCam == null)
			return false;

 		Vector4 n;
		bool r = zedCam.GetNormalValue(cam.WorldToScreenPoint(position), out n);

		switch (reference_frame) {
		case sl.REFERENCE_FRAME.CAMERA:
			normal = n;
			break;

		case sl.REFERENCE_FRAME.WORLD :
			normal = cam.transform.TransformDirection(n);
			break;

		default :
			normal = Vector3.zero;
			break;
		}

		return r;

    }

    /// <summary>
	/// Gets forward distance (i.e. depth) value at a given image pixel. 
    /// </summary><remarks>
    /// Forward distance/depth is distinct from Euclidean distance in that it only measures
    /// distance on the Z axis; the pixel's left/right or up/down position relative to the camera
    /// makes no difference to the depth value. 
    /// </remarks>
    /// <param name="pixel">Pixel coordinates in screen space.</param>
    /// <param name="depth">Forward distance/depth to given pixel.</param>
    /// <returns></returns>
	public static bool GetForwardDistanceAtPixel(sl.ZEDCamera zedCam,Vector2 pixel, out float depth)
    {
		depth = 0.0f;

		if (zedCam == null)
			return false;
		
		float d = zedCam.GetDepthValue(new Vector3(pixel.x, pixel.y, 0));
        depth = d;

        if (d == -1) return false;
        return true;
    }

    /// <summary>
    /// Gets forward distance (i.e. depth) at a given world position (x,y,z).
    /// </summary><remarks>
    /// Forward distance/depth is distinct from Euclidean distance in that it only measures
    /// distance on the Z axis; the pixel's left/right or up/down position relative to the camera
    /// makes no difference to the depth value. 
    /// </remarks>
    /// <param name="position">World position to measure.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="depth">Forward distance/depth to given position.</out>
    /// <returns></returns>
	public static bool GetForwardDistanceAtWorldLocation(sl.ZEDCamera zedCam,Vector3 position, Camera cam, out float depth)
	{
		depth = 0.0f;

		if (zedCam == null)
			return false;
		
		Vector3 pixelPosition = cam.WorldToScreenPoint (position);

		float d = zedCam.GetDepthValue(new Vector3(pixelPosition.x, pixelPosition.y, 0));
		depth = d;

		if (d == -1) return false;
		return true;
	}



    /// <summary>
    /// Gets the Euclidean distance from the world position of a given image pixel. 
    /// </summary><remarks>
    /// Euclidean distance is distinct from forward distance/depth in that it takes into account the point's X and Y position 
    /// relative to the camera. It's the actual distance between the camera and the point in world space. 
    /// </remarks>
    /// <param name="pixel">Pixel coordinates in screen space.</param>
    /// <param name="distance">Euclidean distance to given pixel.</param>
    /// <returns></returns>
	public static bool GetEuclideanDistanceAtPixel(sl.ZEDCamera zedCam,Vector2 pixel, out float distance)
	{
		distance = 0.0f;

		if (zedCam == null)
			return false;
		
		float d = zedCam.GetDistanceValue(new Vector3(pixel.x, pixel.y, 0));
		distance = d;

		if (d == -1) return false;
		return true;
	}


    /// <summary>
    /// Gets the Euclidean distance from the given caera to a point in the world (x,y,z).
    /// </summary><remarks>
    /// Euclidean distance is distinct from forward distance/depth in that it takes into account the point's X and Y position 
    /// relative to the camera. It's the actual distance between the camera and the point in world space. 
    /// </remarks>
    /// <param name="position">World position to measure.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="distance">Euclidean distance to given position.</out>
    /// <returns></returns>
	public static bool GetEuclideanDistanceAtWorldLocation(sl.ZEDCamera zedCam,Vector3 position, Camera cam, out float distance)
	{
		distance = 0.0f;

		if (zedCam == null)
			return false;
		
		Vector3 pixelPosition = cam.WorldToScreenPoint (position);

		float d = zedCam.GetDistanceValue(new Vector3(pixelPosition.x, pixelPosition.y, 0));
		distance = d;

		if (d == -1) return false;
		return true;
	}
    /// <summary>
    /// Gets the world position of the given image pixel.
    /// </summary>
    /// <param name="pixel">Pixel coordinates in screen space.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="worldPos">Filled with the world position of the specified pixel.</param>
    /// <returns>True if it found a value, false otherwise (such as if it's outside the camera's view frustum)</returns>
	public static bool GetWorldPositionAtPixel(sl.ZEDCamera zedCam,Vector2 pixel, Camera cam, out Vector3 worldPos)
    {
		worldPos = Vector3.zero;

		if (zedCam == null)
			return false;
		
 		float d;
		worldPos = Vector3.zero;
		if (!GetForwardDistanceAtPixel(zedCam,pixel, out d)) return false;

		//Adjust for difference between screen size and ZED's image resolution.
		float xp = pixel.x * zedCam.ImageWidth / Screen.width;
		float yp = pixel.y * zedCam.ImageHeight / Screen.height;

		//Extract world position using screen-to-world transform.
		worldPos = cam.ScreenToWorldPoint(new Vector3(xp, yp,d));
	    return true;
    }


    /// <summary>
	/// Checks if a real-world location is visible from the camera (true) or masked by a virtual object (with a collider).
    /// </summary>
	/// <warning>The virtual object must have a collider for this to work as it uses a collision test.</warning>
	/// <param name="position">Position to check in world space. Must be in camera's view to check against the real world.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <returns>True if visible, false if obscurred.</returns>
	public static bool IsLocationVisible(sl.ZEDCamera zedCam,Vector3 position, Camera cam)
    {
		if (zedCam == null)
			return false;
		
        RaycastHit hit;
		float d;
		GetForwardDistanceAtWorldLocation(zedCam,position, cam,out d);
        if (Physics.Raycast(cam.transform.position, position - cam.transform.position, out hit))
        {
            if (hit.distance < d) return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the real world at an image pixel is visible from the camera (true) or masked by a virtual object (with a collider).
    /// </summary>
	/// <warning>The virtual object must have a collider for this to work as it uses a collision test.</warning>
    /// <param name="pixel">Screen space coordinates of the real-world pixel.</param>
    /// <param name="cam">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <returns>True if visible, false if obscurred.</returns>
	public static bool IsPixelVisible(sl.ZEDCamera zedCam, Vector2 pixel, Camera cam)
	{
		if (zedCam == null)
			return false;
		
		RaycastHit hit;
		float d;
		GetForwardDistanceAtPixel(zedCam, pixel,out d);
		Vector3 position = cam.ScreenToWorldPoint(new Vector3(pixel.x, pixel.y, d));
		if (Physics.Raycast(cam.transform.position, position - cam.transform.position, out hit))
		{
			if (hit.distance < d) return false;
		}
		return true;
	}



    /***********************************************************************************************
	 ********************             HIT TEST  FUNCTIONS             ******************************
	 ***********************************************************************************************/

    /// <summary>
    /// Static functions for checking collisions or 'hits' with the real world. This does not require 
    /// scanning/spatial mapping or plane detection as it used the live depth map.
    /// Each is based on the premise that if a point is behind the real world, it has intersected with it (except when
    /// using realworldthickness). This is especially when checked each frame on a moving object, like a projectile. 
    /// In each function, "countinvalidascollision" specifies if off-screen pixels or missing depth values should count as collisions.
    /// "realworldthickness" specifies how far back a point needs to be behind the real world before it's not considered a collision.
    /// </summary>



    /// <summary>
    /// Checks an individual point in world space to see if it's occluded by the real world.
    /// </summary>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera).</param>
    /// <param name="point">3D point in the world that belongs to a virtual object.</param>
    /// <param name="countinvalidascollision">Whether a collision that can't be tested (such as when it's off-screen)
    /// is counted as hitting something.</param>
    /// <param name="realworldthickness">Sets the assumed thickness of the real world. Points further away than the world by
    /// more than this amount won't return true, considered "behind" the real world instead of inside it.</param>
    /// <returns>True if the test represents a valid hit test.</returns>
	public static bool HitTestAtPoint(sl.ZEDCamera zedCam, Camera camera, Vector3 point, bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		if (zedCam == null)
			return false;

		//Transform the point into screen space.
		Vector3 screenpoint = camera.WorldToScreenPoint(point);

		//Make sure it's within our view frustrum (excluding clipping planes).
		if (!CheckScreenView (point, camera)) {
			return countinvalidascollision;
		}

		//Compare distance in virtual camera to corresponding point in distance map.
		float realdistance;
		GetEuclideanDistanceAtPixel(zedCam, new Vector2(screenpoint.x, screenpoint.y), out realdistance);

		//If we pass bad parameters, or we don't have an accurate reading on the depth, we can't test.
		if(realdistance <= 0f)
		{
			return countinvalidascollision; //We can't read the depth from that pixel.
		}

		if (realdistance <= Vector3.Distance(point, camera.transform.position) && Vector3.Distance(point, camera.transform.position) - realdistance <= realworldthickness)
		{
			return true; //The real pixel is closer or at the same depth as the virtual point. That's a collision (unless closer by more than realworldthickness).
		}
		else return false; //The real pixel is behind the virtual point.
	}

    /// <summary>
    /// Performs a "raycast" by checking for collisions/hit in a series of points on a ray.
    /// Calls HitTestAtPoint at each point on the ray, spaced apart by distbetweendots.
    /// </summary>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="startpos">Starting position of the ray</param>
    /// <param name="rot">Direction of the ray.</param>
    /// <param name="maxdistance">Maximum distance of the ray</param>
    /// <param name="distbetweendots">Distance between sample dots. 1cm (0.01f) is recommended for most casses, but
    /// increase to improve performance at the cost of accuracy.</param>
    /// <param name="collisionpoint">Fills the point where the collision occurred, if any.</param>
    /// <param name="countinvalidascollision">Whether a collision that can't be tested (such as when it's off-screen)
    /// is counted as hitting something.</param>
    /// <param name="realworldthickness">Sets the assumed thickness of the real world. Points further away than the world by
    /// more than this amount won't return true, considered "behind" the real world instead of inside it.</param>
    /// <returns></returns>
	public static bool HitTestOnRay(sl.ZEDCamera zedCam, Camera camera, Vector3 startpos, Quaternion rot, float maxdistance, float distbetweendots, out Vector3 collisionpoint,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		collisionpoint = Vector3.zero;

		if (zedCam == null)
			return false;

		//Check for occlusion in a series of dots, spaced apart evenly.
		Vector3 lastvalidpoint = startpos;
		for (float i = 0; i < maxdistance; i += distbetweendots)
		{
			Vector3 pointtocheck = rot * new Vector3(0f, 0f, i);
			pointtocheck += startpos;

			bool hit = HitTestAtPoint(zedCam, camera, pointtocheck,countinvalidascollision, realworldthickness);

			if (hit)
			{
				//Return the last valid place before the collision.
				collisionpoint = lastvalidpoint;
				return true;
			}
			else
			{
				lastvalidpoint = pointtocheck;
			}
		}

		//There was no collision at any of the points checked. 
		collisionpoint = lastvalidpoint;
		return false;

	}

    /// <summary>
    /// Checks if a spherical area is blocked above a given percentage. Useful for checking if a drone spawn point is valid.
    /// Works by checking random points around the sphere for occlusion, Monte Carlo-style, so more samples means greater accuracy.
    /// </summary><remarks>
    /// Unlike HitTestOnRay, you can allow some individual points to collide without calling the whole thing a collision. This is useful
    /// to account for noise, or to allow objects to "graze" the real world. Adjust this with blockedpercentagethreshold.
    /// See the Drone or DroneSpawner class for examples.</remarks>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="centerpoint">Center point of the sphere.</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="numberofsamples">Number of dots in the sphere. Increase to improve accuracy at the cost of performance.</param>
    /// <param name="blockedpercentagethreshold">Percentage (0 - 1) that the number of hits must exceed for a collision.</param>
    /// <param name="countinvalidascollision">Whether a collision that can't be tested (such as when it's off-screen)
    /// is counted as hitting something.</param>
    /// <param name="realworldthickness">Sets the assumed thickness of the real world. Points further away than the world by
    /// more than this amount won't return true, considered "behind" the real world instead of inside it.</param>
    /// <returns>Whether the sphere is colliding with the real world.</returns>
	public static bool HitTestOnSphere(sl.ZEDCamera zedCam, Camera camera, Vector3 centerpoint, float radius, int numberofsamples, float blockedpercentagethreshold = 0.2f,
		bool countinvalidascollision = true, float realworldthickness = Mathf.Infinity)
	{
		int occludedpoints = 0;

		for (int i = 0; i < numberofsamples; i++)
		{
			//Find a random point along the bounds of a sphere and check if it's occluded.
			Vector3 randompoint = Random.onUnitSphere * radius + centerpoint;
			if(HitTestAtPoint(zedCam, camera, randompoint, countinvalidascollision, realworldthickness))
			{
				occludedpoints++;
			}
		}

		//See if the percentage of occluded pixels exceeds the threshold.
		float occludedpercent = occludedpoints / (float)numberofsamples;
		if (occludedpercent > blockedpercentagethreshold)
		{
			return true; //Occluded.
		}
		else return false;
	}

    /// <summary>
    /// Checks for collisions at each vertex of a given mesh with a given transform.
    /// Expensive on large meshes, and quality depends on density and distribution of the mesh's vertices. 
    /// </summary><remarks>
    /// As a mesh's vertices are not typically designed to be tested in this way, it is almost always better
    /// to use a sphere or a raycast; areas inside large faces of the mesh won't register as colliding, and
    /// dense parts of the mesh will do more checks than is necessary. To make proper use of this feature, make a 
    /// custom mesh with vertices spaced evenly, and use that in place of the mesh being used for rendering. 
    /// </remarks>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="mesh">Mesh to supply the vertices.</param>
    /// <param name="worldtransform">World position, rotation and scale of the mesh.</param>
    /// <param name="blockedpercentagethreshold">Percentage (0 - 1) that the number of hits must exceed for a collision.</param>
    /// <param name="meshsamplepercent">Percentage of the mesh's vertices to check for hits. Lower to improve performance
    /// at the cost of accuracy.</param>
    /// <param name="countinvalidascollision">Whether a collision that can't be tested (such as when it's off-screen)
    /// is counted as hitting something.</param>
    /// <param name="realworldthickness">Sets the assumed thickness of the real world. Points further away than the world by
    /// more than this amount won't return true, considered "behind" the real world instead of inside it.</param>
    /// <returns>True if the mesh collided with the real world.</returns>
	public static bool HitTestOnMesh(sl.ZEDCamera zedCam, Camera camera, Mesh mesh, Transform worldtransform, float blockedpercentagethreshold, float meshsamplepercent = 1,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		//Find how often we check samples, represented as an integer denominator.
        //For example, if meshamplepercent is 0.2, then we'll check every five vertices. 
		int checkfrequency = Mathf.RoundToInt(1f / Mathf.Clamp01(meshsamplepercent));
		int totalchecks = Mathf.FloorToInt(mesh.vertices.Length / (float)checkfrequency);

		//Check the vertices in the mesh for a collision, skipping vertices to match the specified sample percentage.
		int intersections = 0;
		for(int i = 0; i < mesh.vertices.Length; i += checkfrequency)
		{
			if (HitTestAtPoint(zedCam, camera, worldtransform.TransformPoint(mesh.vertices[i]),countinvalidascollision, realworldthickness))
			{
				intersections++;
			}
		}

		//See if our total collisions exceeds the threshold to call it a collision.
		float blockedpercentage = (float)intersections / totalchecks;
		if(blockedpercentage > blockedpercentagethreshold)
		{
			return true;
		}

		return false;
	}

    /// <summary>
    /// Checks for collisions at each vertex of a given mesh with a given transform.
    /// Expensive on large meshes, and quality depends on density and distribution of the mesh's vertices. 
    /// </summary><remarks>
    /// As a mesh's vertices are not typically designed to be tested in this way, it is almost always better
    /// to use a sphere or a raycast; areas inside large faces of the mesh won't register as colliding, and
    /// dense parts of the mesh will do more checks than is necessary. To make proper use of this feature, make a 
    /// custom mesh with vertices spaced evenly, and use that in place of the mesh being used for rendering. 
    /// </remarks>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <param name="meshfilter">MeshFilter whose mesh value will supply the vertices.</param>
    /// <param name="worldtransform">World position, rotation and scale of the mesh.</param>
    /// <param name="blockedpercentagethreshold">Percentage (0 - 1) that the number of hits must exceed for a collision.</param>
    /// <param name="meshsamplepercent">Percentage of the mesh's vertices to check for hits. Lower to improve performance
    /// at the cost of accuracy.</param>
    /// <param name="countinvalidascollision">Whether a collision that can't be tested (such as when it's off-screen)
    /// is counted as hitting something.</param>
    /// <param name="realworldthickness">Sets the assumed thickness of the real world. Points further away than the world by
    /// more than this amount won't return true, considered "behind" the real world instead of inside it.</param>
    /// <returns>True if the mesh collided with the real world.</returns>
	public static bool HitTestOnMesh(sl.ZEDCamera zedCam, Camera camera, MeshFilter meshfilter, float blockedpercentagethreshold, float meshsamplepercent = 1,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		return HitTestOnMesh(zedCam, camera, meshfilter.mesh, meshfilter.transform, blockedpercentagethreshold, meshsamplepercent, countinvalidascollision, realworldthickness);
	}

    /// <summary>
    /// Checks if a world space point is within our view frustum. 
    /// Excludes near/far planes but returns false if the point is behind the camera.
    /// </summary>
    /// <param name="point">World space point to check.</param>
    /// <param name="camera">Unity Camera used for world-camera space conversion (usually left camera)</param>
    /// <returns></returns>
    public static bool CheckScreenView(Vector3 point, Camera camera)
	{
		//Transform the point into screen space
		Vector3 screenpoint = camera.WorldToScreenPoint(point);


		//Make sure it's within our view frustrum (except for clipping planes)
		if (screenpoint.z <= 0f)
		{
			return false; //No collision if it's behind us.
		}


		if (screenpoint.x < 0f ||  //Too far to the left
			screenpoint.y < 0f || //Too far to the bottom
			screenpoint.x >= camera.pixelWidth || //Too far to the right
			screenpoint.y >= camera.pixelHeight) //Too far to the top
		{
			return false;
		}

		return true;
	}


    /***********************************************************************************************
	 ******************************        IMAGE UTILS            **********************************
	 ***********************************************************************************************/

    /// <summary>
    /// Saves a RenderTexture to a .png in the given relative path. Saved to Assets/image.png by default.
    /// Use this to take a picture of the ZED's final output.
    /// </summary><remarks>
    /// If in pass-through AR mode, you can pass ZEDRenderingPlane.target to this from the ZEDRenderingPlane
    /// components in the ZED's left eye. If not using AR, you can create your own RenderTexture, and use 
    /// Graphics.Blit to copy to it in an OnRenderImage function of a component you attach to the camera.
    /// </remarks>
    /// <param name="rt">Source RenderTexture to be saved.</param>
    /// <param name="path">Path and filename to save the file.</param>
    /// <returns></returns>
    static public bool SaveImage(RenderTexture rt, string path = "Assets/image.png")
    {
        if (rt == null || path.Length == 0) return false;
        RenderTexture currentActiveRT = RenderTexture.active; //Cache the currently active RenderTexture to avoid interference.
        RenderTexture.active = rt; //Switch the source RenderTexture to the active one.

        Texture2D tex = new Texture2D(rt.width, rt.height); //Make a Texture2D copy of it and save it. 
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

        RenderTexture.active = currentActiveRT; //Restore the old active RenderTexture.
        return true;
    }



    /***********************************************************************************************
	 ******************************        MESH UTILS            **********************************
	 ***********************************************************************************************/
    public static string MeshToString(MeshFilter mf)
    {
        Mesh m = mf.mesh;
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

        StringBuilder sb = new StringBuilder();

        sb.Append("g ").Append(mf.name).Append("\n");
        foreach (Vector3 v in m.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
        }
        return sb.ToString();
    }

    public static void MeshToFile(MeshFilter mf, string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            sw.Write(MeshToString(mf));
        }
    }

    /***********************************************************************************************
	 ******************************        MATH UTILS            **********************************
	 ***********************************************************************************************/

    public static float DistancePointLine(Vector3 point, Vector3 lineStartPoint, Vector3 lineEndPoint)
	{
		return Vector3.Magnitude(ProjectPointLine(point, lineStartPoint, lineEndPoint) - point);
	}

	public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
	{
		Vector3 rhs = point - lineStart;
		Vector3 vector2 = lineEnd - lineStart;
		float magnitude = vector2.magnitude;
		Vector3 lhs = vector2;
		if (magnitude > 1E-06f)
		{
			lhs = (Vector3)(lhs / magnitude);
		}
		float num2 = Mathf.Clamp(Vector3.Dot(lhs, rhs), 0f, magnitude);
		return (lineStart + ((Vector3)(lhs * num2)));
	}



}
