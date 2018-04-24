using UnityEngine;
/// <summary>
/// Functions support to help to retrieve depth and normals
/// </summary>
public class ZEDSupportFunctions
{

	/***********************************************************************************************
	 ********************             BASIC "GET" FUNCTIONS             ****************************
	 ***********************************************************************************************/


    /// <summary>
	/// Get the Normal vector at a given pixel (i,j). the Normal can be given regarding camera reference or in the world reference.
    /// </summary>
	/// <param name="pixel"> position of the pixel</param>
	/// <param name="reference_frame"> Reference frame given by the enum sl.REFERENCE_FRAME</param>
	/// <param name="cam"> Unity Camera (to access world to camera transform</param>
	/// <out> normal that will be filled </out>
    /// <returns> true if success, false otherwie</returns>
	public static bool GetNormalAtPixel(Vector2 pixel, sl.REFERENCE_FRAME reference_frame,Camera cam, out Vector3 normal)
    {
        Vector4 n;
		bool r = sl.ZEDCamera.GetInstance().GetNormalValue(new Vector3(pixel.x,pixel.y, 0), out n);

		switch (reference_frame) {
		case sl.REFERENCE_FRAME.CAMERA:
			normal = n;
			break;

		case sl.REFERENCE_FRAME.WORLD:
			normal = cam.transform.TransformDirection(n);
			break;
		default :
			normal = Vector3.zero;
			break;
		}

        return r;
    }

	/// <summary>
	/// Get the Normal vector at a world position (x,y,z). the Normal can be given regarding camera reference or in the world reference.
	/// </summary>
	/// <param name="position"> world position</param>
	/// <param name="reference_frame"> Reference frame given by the enum sl.REFERENCE_FRAME</param>
	/// <param name="cam"> Unity Camera (to access world to camera transform</param>
	/// <out> normal vector that will be filled </out>
	/// <returns> true if success, false otherwie</returns>
	public static bool GetNormalAtWorldLocation(Vector3 position, sl.REFERENCE_FRAME reference_frame,Camera cam, out Vector3 normal)
    {
 		Vector4 n;
		bool r = sl.ZEDCamera.GetInstance().GetNormalValue(cam.WorldToScreenPoint(position), out n);

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
	/// Get forward distance (ie depth) value at a given image pixel
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool GetForwardDistanceAtPixel(Vector2 pixel, out float depth)
    {
		float d = sl.ZEDCamera.GetInstance().GetDepthValue(new Vector3(pixel.x, pixel.y, 0));
        depth = d;

        if (d == -1) return false;
        return true;
    }


	/// <summary>
	/// Get forward distance (ie depth) at a world position (x,y,z).
	/// </summary>
	/// <param name="position"> (x,y,z) world location</param>
	/// <param name="Camera"> Camera object</param>
    /// <out> Depth value in float </out>
	/// <returns></returns>
	public static bool GetForwardDistanceAtWorldLocation(Vector3 position, Camera cam,out float depth)
	{
		Vector3 pixelPosition = cam.WorldToScreenPoint (position);

		float d = sl.ZEDCamera.GetInstance().GetDepthValue(new Vector3(pixelPosition.x, pixelPosition.y, 0));
		depth = d;

		if (d == -1) return false;
		return true;
	}



	/// <summary>
	/// Get euclidean distance value at a given image pixel to the left camera
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public static bool GetEuclideanDistanceAtPixel(Vector2 pixel, out float distance)
	{
		float d = sl.ZEDCamera.GetInstance().GetDistanceValue(new Vector3(pixel.x, pixel.y, 0));
		distance = d;

		if (d == -1) return false;
		return true;
	}


	/// <summary>
	/// Get the euclidean distance value from a point at a world position (x,y,z) to the left camera
	/// </summary>
	/// <param name="position"> (x,y,z) world location</param>
	/// <param name="Camera"> Camera object</param>
	/// <out> Depth value in float </out>
	/// <returns></returns>
	public static bool GetEuclideanDistanceAtWorldLocation(Vector3 position, Camera cam,out float distance)
	{
		Vector3 pixelPosition = cam.WorldToScreenPoint (position);

		float d = sl.ZEDCamera.GetInstance().GetDistanceValue(new Vector3(pixelPosition.x, pixelPosition.y, 0));
		distance = d;

		if (d == -1) return false;
		return true;
	}
    /// <summary>
    /// Get the world position of the given image pixel.
    /// </summary>
    /// <param name="pixelPos"></param>
    /// <param name="cam"></param>
    /// <returns>true if success, false otherwise</returns>
    public static bool GetWorldPositionAtPixel(Vector2 pixel, Camera cam, out Vector3 worldPos)
    {
 		float d;
		worldPos = Vector3.zero;
		if (!GetForwardDistanceAtPixel(pixel, out d)) return false;

		//convert regarding screen size
		float xp = pixel.x * sl.ZEDCamera.GetInstance().ImageWidth / Screen.width;
		float yp = pixel.y * sl.ZEDCamera.GetInstance().ImageHeight / Screen.height;
		//Extract world position using S2W
		worldPos = cam.ScreenToWorldPoint(new Vector3(xp, yp,d));
	    return true;
    }


    /// <summary>
	/// Checks if a world location is visible from the camera (true) or masked by a virtual object (with collider)
	/// This uses the raycast to check if we hit something or not
    /// </summary>
	/// <warning> This only occurs with the virtual objects that have a collider </warning>
	/// <param name="position"> (x,y,z) world location</param>
    /// <param name="cam"></param>
    /// <returns></returns>
    public static bool IsLocationVisible(Vector3 position, Camera cam)
    {
        RaycastHit hit;
		float d;
		GetForwardDistanceAtWorldLocation(position, cam,out d);
        if (Physics.Raycast(cam.transform.position, position - cam.transform.position, out hit))
        {
            if (hit.distance < d) return false;
        }
        return true;
    }


	/// <summary>
	/// Checks if an image pixel is visible from the camera (true) or masked by a virtual object (with collider)
	/// This uses the raycast to check if we hit something or not
	/// </summary>
	/// <warning> This only occurs with the virtual objects that have a collider </warning>
	/// <param name="pixel"></param>
	/// <param name="cam"></param>
	/// <returns></returns>
	public static bool IsPixelVisible(Vector2 pixel, Camera cam)
	{
		RaycastHit hit;
		float d;
		GetForwardDistanceAtPixel(pixel,out d);
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
	/// Static functions for checking collisions or 'hit' with the real world.
	/// In each function, "countinvalidascollision" specifies if off-screen pixels or missing depth values should count as collision.
	/// "realworldthickness" specifies how far back a point needs to be behind the real world before it's not considered a collision.
	/// </summary>



	/// <summary>
	/// Checks an individual point in world space to see if it's occluded by the real world.
	/// </summary>
	/// <param name="point">3D point in the world that belongs to a virtual object</param>
	/// <param name="camera">camera (usually left camera)</param>
	/// <returns>True if the test represents a valid hit test.</returns>
	public static bool HitTestAtPoint(Camera camera, Vector3 point, bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		//Transform the point into screen space
		Vector3 screenpoint = camera.WorldToScreenPoint(point);

		//Make sure it's within our view frustrum (except for clipping planes)
		if (!CheckScreenView(point,camera))
			return countinvalidascollision;

		//Compare distance in _virtual camera to corresponding point in distance map.
		float realdistance;
		ZEDSupportFunctions.GetEuclideanDistanceAtPixel(new Vector2(screenpoint.x, screenpoint.y), out realdistance);

		//If we pass bad parameters, or we don't have an accurate reading on the depth, we can't test.
		if(realdistance <= 0f)
		{
			return countinvalidascollision; //We can't read the depth from that pixel.
		}

		///Detection is the space
		if (realdistance <= Vector3.Distance(point, camera.transform.position) && Vector3.Distance(point, camera.transform.position) - realdistance <= realworldthickness)
		{
			return true; //The real pixel is closer or at the same depth as the virtual point. That's a collision.
		}
		else return false; //It's behind the virtual point.
	}

	/// <summary>
	/// Performs a "raycast" by checking for collisions/hit in a series of points on a ray.
	/// </summary>
	/// <param name="camera">camera (left camera usually)</param>
	/// <param name="startpos">starting position of the ray</param>
	/// <param name="rot">rotation of the ray from starting point</param>
	/// <param name="maxdistance">maximum distance of the ray</param>
	/// <param name="distbetweendots">distance between sample dots that define the ray</param>
	/// <param name="collisionpoint">out : collision point</param>
	/// <returns></returns>
	public static bool HitTestOnRay(Camera camera, Vector3 startpos, Quaternion rot, float maxdistance, float distbetweendots, out Vector3 collisionpoint,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		//We're gonna check for occlusion in a series of dots, spaced apart evenly.

		Vector3 lastvalidpoint = startpos;
		for (float i = 0; i < maxdistance; i += distbetweendots)
		{
			Vector3 pointtocheck = rot * new Vector3(0f, 0f, i);
			pointtocheck += startpos;

			bool hit = HitTestAtPoint(camera, pointtocheck,countinvalidascollision, realworldthickness);

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

		//This code will only be reached if there's no collision
		collisionpoint = lastvalidpoint;
		return false;

	}

	/// <summary>
	/// Checks if a spherical area is blocked above a given percentage. Useful for checking is a drone spawn point is valid.
	/// Works by checking random points around the sphere for occlusion, Monte Carlo-style, so more samples means greater accuracy.
	/// </summary>
	/// <param name="camera">camera (Left camera usually)</param>
	/// <param name="centerpoint">Center point of the sphere that belongs to the virtual objects</param>
	/// <param name="radius">radius of the sphere</param>
	/// <param name="numberofsamples">number of dots in the sphere (increasing this number will increase the processing time)</param>
	/// <param name="blockedpercentagethreshold"> percentage between 0 and 1 that defines a collision (if greater than)</param>
	/// <returns></returns>
	public static bool HitTestOnSphere(Camera camera, Vector3 centerpoint, float radius, int numberofsamples, float blockedpercentagethreshold,
		bool countinvalidascollision = true, float realworldthickness = Mathf.Infinity)
	{
		int occludedpoints = 0;

		for (int i = 0; i < numberofsamples; i++)
		{
			//Find a random point along the bounds of a sphere and check if it's occluded
			Vector3 randompoint = Random.onUnitSphere * radius + centerpoint;
			if(HitTestAtPoint(camera, randompoint, countinvalidascollision, realworldthickness))
			{
				occludedpoints++;
			}
		}

		//See if the percentage of occluded pixels exceeds the threshold
		float occludedpercent = occludedpoints / (float)numberofsamples;
		if (occludedpercent > blockedpercentagethreshold)
		{
			return true; //Occluded
		}
		else return false;
	}

	/// <summary>
	/// Checks for collisions with the vertices of a given mesh with a given transform.
	/// Expensive, and quality depends on density and distribution of the mesh's vertices.
	/// </summary>
	/// <param name="camera">camera (Left camera usually)</param>
	/// <param name="mesh">Mesh object</param>
	/// <param name="worldtransform">world transform</param>
	/// <param name="blockedpercentagethreshold">percentage between 0 and 1 that defines a collision (if greater than)</param>
	/// <param name="meshsamplepercent">percentage between 0 and 1 that samples the mesh (will skip vertices if less than 1)</param>
	/// <param name="countinvalidascollision">see HitTestAtPoint</param>
	/// <param name="realworldthickness">see HitTestAtPoint</param>
	/// <returns></returns>
	public static bool HitTestOnMeshVertices(Camera camera, Mesh mesh, Transform worldtransform, float blockedpercentagethreshold, float meshsamplepercent = 1,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		//Find how often we check samples, represented as an integer denominator
		int checkfrequency = Mathf.RoundToInt(1f / Mathf.Clamp01(meshsamplepercent));
		int totalchecks = Mathf.FloorToInt(mesh.vertices.Length / (float)checkfrequency);

		//Check the vertices in the mesh for a collision, skipping vertices to match the specified sample percentage.
		int intersections = 0;
		for(int i = 0; i < mesh.vertices.Length; i += checkfrequency)
		{
			if (HitTestAtPoint(camera, worldtransform.TransformPoint(mesh.vertices[i]),countinvalidascollision, realworldthickness))
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
	/// Checks for collisions with the vertices of the mesh in a meshfilter.
	/// Expensive, and quality depends on density and distribution of the mesh's vertices.
	/// </summary>
	/// <param name="camera">camera (Left camera usually)</param>
	/// <param name="meshfilter">Mesh filter object</param>
	/// <param name="blockedpercentagethreshold">percentage between 0 and 1 that defines a collision (if greater than)</param>
	/// <param name="meshsamplepercent">percentage between 0 and 1 that samples the mesh (will skip vertices if less than 1)</param>
	/// <param name="countinvalidascollision">see HitTestAtPoint</param>
	/// <param name="realworldthickness">see HitTestAtPoint</param>
	/// <returns></returns>
	public static bool HitTestOnMeshFilter(Camera camera, MeshFilter meshfilter, float blockedpercentagethreshold, float meshsamplepercent = 1,
		bool countinvalidascollision = false, float realworldthickness = Mathf.Infinity)
	{
		return HitTestOnMeshVertices(camera, meshfilter.mesh, meshfilter.transform, blockedpercentagethreshold, meshsamplepercent, countinvalidascollision, realworldthickness);
	}


	/// <summary>
	/// Simply checking if the object is within our view frustrum.
	/// </summary>
	/// <param name="point"></param>
	/// <param name="camera"></param>
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

	static public bool SaveImage(RenderTexture rt, string path = "Assets/image.png")
    {
        if (rt == null || path.Length == 0) return false;
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

        RenderTexture.active = currentActiveRT;
        return true;
    }


}
