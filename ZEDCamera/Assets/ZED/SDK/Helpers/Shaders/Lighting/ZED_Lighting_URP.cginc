//Copied from LitInput.hlsl then modified. 

#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED
#endif

#ifndef unity_ColorSpaceDielectricSpec
#define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
//#include "UnityStandardUtils.cginc"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _OcclusionStrength;
CBUFFER_END

TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

/******************************************************************/
/************** Point lights information **************************/
/******************************************************************/
struct ZED_PointLight {
	float4 color;
	float range;
	float3 position;
};


/******************************************************************/
/************** Spot lights information **************************/
/******************************************************************/
struct ZED_SpotLight {
	float4 color;
	float3 position;
	float4 direction;
	float4 params;// angle, intensity, 1/range, cone interior
};

///From ZED_Lighting.cginc
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

bool Unity_IsNan_float3(float3 In)
{
	bool Out = (In < 0.0 || In > 0.0 || In == 0.0) ? 0 : 1;
	return Out;
}

//Compute the light for all light
#if defined(ZED_SPOT_LIGHT_DECLARATION) || defined(ZED_POINT_LIGHT_DECLARATION)
half4 computeLightingLWRP(float3 albedo, float3 normals, float3 worldPos, float alpha, float zedfactoraffectreal)
{
	half3 worldViewDir = normalize(worldPos - _WorldSpaceCameraPos.xyz);
#ifdef UNITY_COMPILER_HLSL

	SurfaceData o = (SurfaceData)0;

#else

	SurfaceData o;

#endif

	o.albedo.rgb = albedo;
	o.specular = 1.0;
	o.metallic = _Metallic;
	o.smoothness = 1.0;
	o.normalTS = normals;
	o.emission = 0.0;
	o.occlusion = 0.0;
	o.alpha = 1;


	float3 specularTint = lerp(unity_ColorSpaceDielectricSpec.rgb, o.albedo, o.metallic); //Don't think this is needed. 
	float oneMinusReflectivity = unity_ColorSpaceDielectricSpec.a - o.metallic * unity_ColorSpaceDielectricSpec.a;
	o.albedo.rgb *= oneMinusReflectivity;


	BRDFData brdfDataRaw;
	InitializeBRDFData(o.albedo, o.metallic, o.specular, o.smoothness, o.alpha, brdfDataRaw);

	float4 c = float4(albedo.rgb * zedfactoraffectreal, 1);

#ifndef _RECEIVE_SHADOWS_OFF

#ifdef _MAIN_LIGHT_SHADOWS

#if SHADOWS_SCREEN

	float3 modws = float3(worldPos.x, worldPos.y, worldPos.z);
	float4 newshadcoords = TransformWorldToShadowCoord(modws);

	Light mainLight = GetMainLight();

	ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
	half shadowStrength = GetMainLightShadowStrength();
	mainLight.shadowAttenuation = SampleShadowmap(newshadcoords, TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowSamplingData, shadowStrength, false);

#else
	float3 modws = float3(worldPos.x, worldPos.y, worldPos.z);
	float4 newshadcoords = TransformWorldToShadowCoord(modws);
  
	Light mainLight = GetMainLight(newshadcoords);

#endif

#else
	Light mainLight = GetMainLight();
#endif

	//Only apply directional shadows if it's not opposite of the normal, so that we don't draw shadows from objects onto walls that they're behind. 
	float dirlightnormdot = dot(normals, mainLight.direction);
	if (!Unity_IsNan_float3(normals)) c.rgb = lerp(c.rgb, c.rgb *=mainLight.shadowAttenuation, step(0, dirlightnormdot));
	//c.rgb *= clamp(dirlightnormdot, 0, 1);
	/*if (dirlightnormdot > 0)
	{
		c.rgb *= mainLight.shadowAttenuation;
	}*/

#endif


	int indexPointLights = 0;
	//For the point light
#if defined(ZED_POINT_LIGHT_DECLARATION)
	UNITY_LOOP
		for (indexPointLights = 0; indexPointLights < numberPointLights; indexPointLights++) {
			float3 lightVec = pointLights[indexPointLights].position - worldPos;

			if (pointLights[indexPointLights].range - length(lightVec) < 0) {
				continue;
			}
			float distanceSqr = max(dot(lightVec, lightVec), HALF_MIN);
			//distanceSqr *= length(lightVec) / pointLights[indexPointLights].range;
			distanceSqr /= pointLights[indexPointLights].range;
			distanceSqr = saturate(distanceSqr);
			float att = DistanceAttenuation(distanceSqr, pointLights[indexPointLights].color);

			if (dot(lightVec, float3(normals.x, normals.y, normals.z)) <= 0.0) {
				continue;
			}

			Light p;
			p.direction = lightVec;
			p.distanceAttenuation = att;
			p.color = pointLights[indexPointLights].color*att*alpha;
			p.shadowAttenuation = 1.0;

			c.rgb += LightingPhysicallyBased(brdfDataRaw, p, normals, worldViewDir);
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

			float3 dirSpotToWorld = lightVec;
			float dotDirectionWorld = dot(normalize(dirSpotToWorld), normalize(spotLights[indexSpotLights].direction.xyz));
			float angleWorld = degrees(acos(-dotDirectionWorld));
			float angleMax = spotLights[indexSpotLights].params.x / 2.0;

			//float distanceSqr = max(dotDirectionWorld, HALF_MIN);

			float distanceSqr = length(lightVec) / (1 / spotLights[indexSpotLights].params.z);
			//float distanceSqr = max(dot(lightVec, lightVec), HALF_MIN);
			//float att = DistanceAttenuation(distanceSqr, spotLights[indexSpotLights].color);
			float att = DistanceAttenuation(distanceSqr, half2(1, 1));
			//float att = DistanceAttenuation(.0001, spotLights[indexSpotLights].color);

			att = saturate(att);
			//att = dotDirectionWorld;


			UNITY_BRANCH

				if (dotDirectionWorld > 0 || dotDirectionWorld > -spotLights[indexSpotLights].direction.w) {
					continue;
				}
				else {
					float angleP = angleMax * (1 - spotLights[indexSpotLights].params.w);
					if (angleP < angleWorld && angleWorld < angleMax)
					{
						att *= (angleMax - angleWorld) / (angleMax - angleP);

					}
				}

			Light p;
			p.direction = -spotLights[indexSpotLights].direction.xyz;
			p.distanceAttenuation = att;
			p.color = spotLights[indexSpotLights].color.xyz*att*alpha;
			p.shadowAttenuation = 1.0;

			c.rgb += LightingPhysicallyBased(brdfDataRaw, p, normals, worldViewDir);
			c.a = 1.0;
		}
#endif

	return c;
}

#endif
