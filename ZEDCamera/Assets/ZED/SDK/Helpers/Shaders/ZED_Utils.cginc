//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

#if !defined(ZED_UTILS)
#define ZED_UTILS


#define MAX_DEPTH 0.9999f


#if UNITY_REVERSED_Z
#define NEAR_DEPTH MAX_DEPTH
#define FAR_DEPTH 1 - MAX_DEPTH
#else
#define NEAR_DEPTH 1 - MAX_DEPTH
#define FAR_DEPTH MAX_DEPTH
#endif


#define MAX_ZED_DEPTH 20
#define MIN_ZED_DEPTH 0.1f

#define ZED_CLAMP(name) name = clamp(name,MIN_ZED_DEPTH, MAX_ZED_DEPTH);

#if UNITY_REVERSED_Z
#define ZED_DEPTH_CLAMP(name) clamp(name,FAR_DEPTH, NEAR_DEPTH)
#else
#define ZED_DEPTH_CLAMP(name) clamp(name,NEAR_DEPTH, FAR_DEPTH)
#endif

#define ZED_PI 3.14159265359
//Compute the depth of ZED to the Unity scale
float computeDepthXYZ(float3 colorXYZ) {
	float d = FAR_DEPTH;
	//Unity BUG, (isinf(colorXYZ.z) && colorXYZ.z > 0 and isinf(colorXYZ.z) && colorXYZ.z < 0 pass together, and it's not a nan
	if (isinf(colorXYZ.z) && colorXYZ.z > 0) d = FAR_DEPTH;
	if (isinf(colorXYZ.z) && colorXYZ.z < 0) d += NEAR_DEPTH;
	if (d == FAR_DEPTH) return FAR_DEPTH;
	else if (d == (FAR_DEPTH + NEAR_DEPTH)) return NEAR_DEPTH;

	colorXYZ = clamp(colorXYZ, 0.01, 20);
	//reverse Y and Z axes
	colorXYZ.b = -colorXYZ.b;
#if SHADER_API_D3D11
	colorXYZ.g = -colorXYZ.g;
#elif SHADER_API_GLCORE
	colorXYZ.g = -colorXYZ.g * 2 + 1;
#endif

	float4 v = float4(colorXYZ, 1);
	//Project to unity's coordinate
	float4 depthXYZVector = mul(UNITY_MATRIX_P, v);

	if (depthXYZVector.w != depthXYZVector.w) return FAR_DEPTH;

	depthXYZVector.b /= (depthXYZVector.w);
	float depthReal = depthXYZVector.b;
	
	return ZED_DEPTH_CLAMP(depthReal);
}

float3 applyQuatToVec3(float4 q, float3 v)
{
	float3 t = 2 * cross(q.xyz, v);
	return v + q.w * t + cross(q.xyz, t);
}

//Compute the depth of ZED to the Unity scale
float computeDepthXYZ(float colorXYZ) {


	if (isinf(colorXYZ) && colorXYZ > 0) return FAR_DEPTH;
	if (isinf(colorXYZ) && colorXYZ < 0) return NEAR_DEPTH;

	if (colorXYZ != colorXYZ) return NEAR_DEPTH;
	colorXYZ = clamp(colorXYZ, 0.01, 20);

#if SHADER_API_D3D11
	colorXYZ = -colorXYZ;
#elif SHADER_API_GLCORE
	colorXYZ = -colorXYZ * 2 + 1;
#endif

	float4 v = float4(0,0, colorXYZ, 1);
	//Project to unity's coordinate
	float4 depthXYZVector = mul(UNITY_MATRIX_P, v);

	if (depthXYZVector.w != depthXYZVector.w) return FAR_DEPTH;

	depthXYZVector.b /= (depthXYZVector.w);
	float depthReal = depthXYZVector.b;

	return ZED_DEPTH_CLAMP(depthReal);
}

//Remove the optical center of the projection matrix for a specific object
float4 GetPosWithoutOpticalCenter(float4 vertex) {
    float4x4 copy_projection = UNITY_MATRIX_P;
		copy_projection[0][2] = 0;
		copy_projection[1][2] = 0;
  return mul(mul(mul(copy_projection, UNITY_MATRIX_V), UNITY_MATRIX_M), vertex);
}

//Converts RGB to YUV
float3 RGBtoYUV(float3 rgb)
{
	float4x4 RGB2YUV = { 0.182586,  0.614231,  0.062007, 0.062745,
		-0.100644, -0.338572,  0.439216, 0.501961,
		0.439216, -0.398942, -0.040274, 0.501961,
		0.000000,  0.000000,  0.000000, 1.000000 };

	return mul(RGB2YUV, float4(rgb,1)).rgb;
}

//Algorithm to compute the alpha of a frag depending of the similarity of a color.
//ColorCamera is the color from a texture given by the camera
float computeAlphaYUVFromYUV(float3 colorCamera, in float3 keyColor) {
	return distance(keyColor.yz, colorCamera.yz);
}

#endif
