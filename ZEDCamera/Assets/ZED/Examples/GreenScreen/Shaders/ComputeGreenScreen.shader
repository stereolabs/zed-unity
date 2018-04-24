//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
// Compares the YUV texture (input) and a YUV color. Set the comparison into the red canal 
Shader "Custom/Green Screen/Compute GreenScreen" {
	Properties
	{
		_MainTex("Texture from ZED", 2D) = "" {}
	}

		SubShader
	{
		Pass
	{
		Cull Off
		ZWrite Off
		Lighting Off
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
#include "UnityCG.cginc"
#include "../../../SDK/Helpers/Shaders/ZED_Utils.cginc"
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

	sampler2D _MainTex;
	float4 _MainTex_ST;


	v2f vert(appdata v)
	{
		v2f o;

		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}



	uint _numberColors;
	float4 _MainTex_TexelSize;

	int _erosion;
	uniform float4 _keyColor;
	uniform float _range;


	float4 frag(v2f i) : SV_Target
	{
		//Get the depth in XYZ format
		float2 uv = i.uv;
		uv.y = 1.0 - uv.y;
		//Color from the camera
		float3 colorCamera = RGBtoYUV(tex2D(_MainTex, uv).bgr);
		float alpha = 1;



		uint index = 0;
		//Compute the 4 UVs they need to be clamped. If these values are changed lines may appear on the screen
		float2 uv2 = clamp(uv + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv4 = clamp(uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv6 = clamp(uv + float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv8 = clamp(uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));

		float2 uv1 = clamp(uv + float2(-_MainTex_TexelSize.x, 0), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv3 = clamp(uv + float2(_MainTex_TexelSize.x, 0), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv5 = clamp(uv + float2(0, _MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
		float2 uv7 = clamp(uv + float2(0, -_MainTex_TexelSize.y), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));

		//X | 0 | X
		//0 | X | 0
		//X | 0 | X
		//X are the sampling done
		float a = computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv2).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv4).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv8).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv6).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv1).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv3).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv5).bgr), _keyColor.rgb)
			+ computeAlphaYUVFromYUV(RGBtoYUV(tex2D(_MainTex, uv7).bgr), _keyColor.rgb)

			+ computeAlphaYUVFromYUV(colorCamera.rgb, _keyColor);
		a /= 9.0;

		alpha = a;

		//return in the red canal, because this shader is used with a rendertexture RFloat
		return saturate(float4(alpha - _range, 1,1,1));
	}
		ENDCG
	}



	}
}