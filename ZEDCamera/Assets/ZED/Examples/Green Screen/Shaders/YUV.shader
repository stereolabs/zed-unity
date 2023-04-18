//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Transforms the RGBA texture into an YUV
Shader "Custom/Green Screen/ YUV"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }


		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
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
			int _isLinear;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
#ifndef UNITY_COLORSPACE_GAMMA

				return float4(RGBtoYUV(LinearToGammaSpace(tex2D(_MainTex, i.uv).bgr)), 1);
#else
				return float4(RGBtoYUV(tex2D(_MainTex, i.uv).bgr), 1);
#endif
			}
			ENDCG
		}
	}
}
