//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Applies the despill factor, the white clip and black clip on the ZED texture
Shader "Custom/Green Screen/Preprocess"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	_MaskTex("Texture from ZED", 2D) = "" {}

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

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

			sampler2D _MaskTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			float _erosion;
			uniform float4 _keyColor;
			uniform float _smoothness;
			uniform float _range;
			uniform float _spill;
			float _whiteClip;
			float _blackClip;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				//Get the depth in XYZ format
				float2 uv = i.uv;
				//Color from the camera
				float3 colorCamera = tex2D(_MainTex, float2(uv.x, 1 - uv.y)).bgr;

				float alpha = tex2D(_MaskTex, uv).r;

				fixed4 o;

				o.rgb = colorCamera.rgb;
				o.a = 1;

				// sample the texture
				float fullMask = pow(saturate(alpha / _smoothness), 1.5);
				o.a = fullMask;
				//To use the despill
				float spillVal = pow(saturate(alpha / _spill), 1.5);
				float desat = (colorCamera.r * 0.2126 + colorCamera.g * 0.7152 + colorCamera.b * 0.0722);
				o.rgb = float3(desat, desat, desat) * (1. - spillVal) + colorCamera.rgb * (spillVal);

				float2 uv1 = clamp(uv + float2(-_MainTex_TexelSize.x*_erosion, 0), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
				float2 uv3 = clamp(uv + float2(0, -_MainTex_TexelSize.y*_erosion), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
				float2 uv5 = clamp(uv + float2(_MainTex_TexelSize.x*_erosion, 0), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));
				float2 uv7 = clamp(uv + float2(0, _MainTex_TexelSize.y*_erosion), fixed2(_MainTex_TexelSize.x, _MainTex_TexelSize.y), fixed2(1 - _MainTex_TexelSize.x, 1 - _MainTex_TexelSize.y));

				if (_erosion >= 1) {
				
					//Erosion with one pass not optimized, prefer erosion with multi pass
					//0 | X | 0
					//X | 0 | X
					//0 | X | 0
					//X are the sampling done
					float a1 = pow(saturate(tex2D(_MaskTex, uv1).r / _smoothness), 1.5);
					float a2 = pow(saturate(tex2D(_MaskTex, uv3).r / _smoothness), 1.5);
					float a3 = pow(saturate(tex2D(_MaskTex, uv5).r / _smoothness), 1.5);
					float a4 = pow(saturate(tex2D(_MaskTex, uv7).r / _smoothness), 1.5);
				
					o.a = min(min(min(min(o.a, a1), a2), a3), a4);
				}
				else {
					o.a = fullMask;
				}

				if (o.a > _whiteClip) o.a = 1;
				else if (o.a < _blackClip) o.a = 0;

				return o;
			}
			ENDCG
		}
	}
}
