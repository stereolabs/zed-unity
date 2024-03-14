//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Set noises and min black
Shader "ZED/ZED Post-Processing" {
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MinBlack("Min black threshold", Range(0,1)) = 0.01
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		ZWrite Off
		ZTest Always
		Cull Off
		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#pragma multi_compile ___ UNITY_HDR_ON

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};


	uniform sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	sampler2D ZEDMaskPostProcess;

	uniform float _gamma;
	uniform float _MinBlack;
	uniform int _NoiseSize;


	float rand(float2 co) {
		return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
	}

	//Vertex Shader
	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		return o;
	}

		//Fragment Shader
	float4 frag(v2f i) : SV_Target{
		float2 invertUV = i.uv;
		invertUV.y = 1 - i.uv.y;
		float mask = tex2D(ZEDMaskPostProcess, i.uv);

		float4 zed = tex2D(_MainTex, i.uv);

		if (mask > 0.9f)
		{
		
			//zed = (zed * 0.187) / (1.035 - zed);
			float SqrtPixel = sqrt(zed.r * zed.r + zed.g * zed.g + zed.b * zed.b);
			
			float3 NoiseFactors = 2;
			float2 random = _Time.x*_NoiseSize*floor(i.uv / _MainTex_TexelSize.xy / _NoiseSize) / 3.;
			float3 NoiseValue = float3(rand(random), 
									   rand(random), 
									   rand(random));

			float4 res = pow(zed, _gamma);

		
			res.r += (NoiseFactors.r * NoiseValue.r - NoiseFactors.r * 0.5) / 255;
			res.g += (NoiseFactors.g * NoiseValue.g - NoiseFactors.g * 0.5) / 255;
			res.b += (NoiseFactors.b * NoiseValue.b - NoiseFactors.b * 0.5) / 255;
			
			//res = res / (res + 0.187) * 1.035;
			res.a = 1.0f;
			//res *= mask;
#if UNITY_COLORSPACE_GAMMA
			return clamp(res, _MinBlack, 1.0f);
#else
			return clamp(res, GammaToLinearSpaceExact(_MinBlack), 1.0f);
#endif
		
		}
		return zed;
		
	}
		ENDCG
	}
	}
}
