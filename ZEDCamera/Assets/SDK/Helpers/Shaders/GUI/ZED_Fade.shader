//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Fade system depending on the current alpha
Shader "ZED/ZED Fade"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	_FadeColor("Wire color", Color) = (1.0, 1.0, 1.0, 1.0)

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _FadeColor;
			uniform float _Alpha;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				return col*(1 - _Alpha) + (_Alpha)*_FadeColor;
			}
			ENDCG
		}
	}
}
