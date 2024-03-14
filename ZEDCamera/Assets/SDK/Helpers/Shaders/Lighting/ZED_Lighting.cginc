//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============


#if !defined(ZED_LIGHTING)
#define ZED_LIGHTING

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"

#include "AutoLight.cginc"
/******************************************************************/
/************** Point lights information **************************/
/******************************************************************/
struct ZED_PointLight{
	float4 color;
	float range;
	float3 position;
};


/******************************************************************/
/************** Spot lights information **************************/
/******************************************************************/
struct ZED_SpotLight{
	float4 color;
	float3 position;
	float4 direction;
	float4 params;// angle, intensity, 1/range, cone interior
};

sampler2D _NormalsTex;
uniform float4 _CameraRotationQuat;

#if defined(ZED_SPOT_LIGHT_DECLARATION)
StructuredBuffer<ZED_SpotLight> spotLights;
int  numberSpotLights;
#endif

#if defined(ZED_POINT_LIGHT_DECLARATION)
StructuredBuffer<ZED_PointLight> pointLights;
int  numberPointLights;
#endif
//FallOff the light
float FallOff(float dist, float inverseRange, float coeff) {
	return lerp( 1.0, ( 1.0 - pow( dist * inverseRange * inverseRange, coeff ) ), 1 );
}

#define ZED_WORLD_DIR(index) float3 worldDirection : TEXCOORD##index;
#define ZED_TRANSFER_WORLD_DIR(o) o.worldDirection = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
#define GET_XYZ(o, z, world) world = (o.worldDirection/ o.pos.w) * z + _WorldSpaceCameraPos;

float _Metallic;

// Create a point light or spot light to be used per Unity
UnityLight CreateLight (float3 pos, float4 color, float3 worldPos, float3 normal) {

	UnityLight light;
	light.dir = pos;
	light.color = color;
	light.ndotl = DotClamped(normal, light.dir);

	return light;
}

//Compute the light for all light
#if defined(ZED_SPOT_LIGHT_DECLARATION) || defined(ZED_POINT_LIGHT_DECLARATION)
half4 computeLighting(float3 albedo, float3 normals,  float3 worldPos, float alpha) {
	fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
	SurfaceOutputStandard o;
#endif
	o.Albedo.rgb = albedo;
	o.Emission = 0.0;
	o.Alpha = 1.0;
	o.Occlusion = 1.0;
	o.Metallic = _Metallic;
	o.Normal.rgb = mul((float3x3)unity_CameraToWorld, normals);

	float3 specularTint;
	float oneMinusReflectivity;
	o.Albedo.rgb = DiffuseAndSpecularFromMetallic(
		albedo, o.Metallic, specularTint, oneMinusReflectivity
	);
	float4 c = 0;
	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;

	int indexPointLights = 0;
	//For the point light
#if defined(ZED_POINT_LIGHT_DECLARATION)
	UNITY_LOOP
		for (indexPointLights = 0; indexPointLights < numberPointLights; indexPointLights++) {
			float3 lightVec = pointLights[indexPointLights].position - worldPos;


			if (pointLights[indexPointLights].range - length(lightVec) < 0) {
				continue;
			}

			float att = FallOff(dot(lightVec, lightVec), 1 / pointLights[indexPointLights].range, 0.2);
			float v = dot(lightVec, float3(o.Normal.x, o.Normal.y, o.Normal.z));


			//Remove light from backward
			//UNITY_BRANCH
			if (dot(lightVec, float3(o.Normal.x, o.Normal.y, o.Normal.z)) <= 0.0) {
				continue;
			}

			UnityLight p = CreateLight(lightVec, pointLights[indexPointLights].color*att*alpha, worldPos, o.Normal.rgb);

			gi.light = p;
			c += UNITY_BRDF_PBS(o.Albedo.rgb, specularTint, oneMinusReflectivity, 0, o.Normal.rgb, normalize(_WorldSpaceCameraPos - worldPos), p, gi.indirect);
			c.a = 1.0;
		}
#endif
	c.a = 1.0;

	//For the spot light
#if defined(ZED_SPOT_LIGHT_DECLARATION)
	int indexSpotLights = 0;
	UNITY_LOOP
		for (indexSpotLights = 0; indexSpotLights < numberSpotLights; indexSpotLights++) {
			float3 lightVec = spotLights[indexSpotLights].position - worldPos;
			float att = FallOff(dot(lightVec, lightVec), spotLights[indexSpotLights].params.z, 0.8);


			float3 dirSpotToWorld = -lightVec;
			float dotDirectionWorld = dot(normalize(dirSpotToWorld), spotLights[indexSpotLights].direction.xyz);
			float angleWorld = degrees(acos(dotDirectionWorld));
			float angleMax = spotLights[indexSpotLights].params.x / 2.0;

			UNITY_BRANCH
				if (dotDirectionWorld < 0 || dotDirectionWorld < spotLights[indexSpotLights].direction.w) {
					continue;

				}
				else {
					float angleP = angleMax*(1 - spotLights[indexSpotLights].params.w);
					UNITY_BRANCH
						if (angleP < angleWorld && angleWorld < angleMax) {
							att *= (angleMax - angleWorld) / (angleMax - angleP);
						}

				}
				att = saturate(att);
				UnityLight p = CreateLight(lightVec, (spotLights[indexSpotLights].color)*att*alpha, worldPos, o.Normal.rgb);

				gi.light = p;
				c += UNITY_BRDF_PBS(o.Albedo.rgb, specularTint, oneMinusReflectivity, 1, o.Normal.rgb, normalize(_WorldSpaceCameraPos - worldPos), p, gi.indirect);
				c.a = 1.0;
		}
#endif
	return c;
}
#endif

#endif