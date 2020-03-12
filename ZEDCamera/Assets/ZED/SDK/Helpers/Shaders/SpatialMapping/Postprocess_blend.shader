//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
///
/// Blend the mesh to the ZED video
///
Shader "Custom/Spatial Mapping/Postprocess Blend"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	_WireColor("Wire color", Color) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		ZWrite Off
		
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

			sampler2D _MainTex;
			sampler2D _ZEDMeshTex;
			float4 _ZEDMeshTex_ST;
			float4 _MainTex_ST;
			int _IsTextured;
			float4 _WireColor;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{

				float4 meshColor = tex2D(_ZEDMeshTex, i.uv);

				if (_IsTextured == 0) {
					
					if (length(meshColor) < 0.1) {
						return tex2D(_MainTex, i.uv);
					}
					float4 m =  clamp(tex2D(_MainTex, i.uv)*meshColor, float4(_WireColor.rgb - 0.05f,meshColor.a), float4(_WireColor.rgb + 0.05f, meshColor.a));
#if !AWAY
					m = tex2D(_MainTex, i.uv)*(1 - meshColor.a) + (meshColor.a)* float4(_WireColor.rgb - 0.05f, meshColor.a);
#endif
					return m;
				}



			if (meshColor.r != 0) {
				return meshColor;
			}
			return  tex2D(_MainTex, i.uv);

			}

			ENDCG
		}
	}
}
