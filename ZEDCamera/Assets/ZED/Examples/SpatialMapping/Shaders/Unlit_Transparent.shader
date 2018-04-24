//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

//Displays the navMesh above the current depth
Shader "Custom/Unlit Transparent"
{
	Properties
	{
		_Color("color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Offset("Offset", Range(-200,-20)) = -20
	}
		SubShader
	{

		Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector" = "True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Offset [_Offset],-2
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
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{				
				return _Color;
			}
			ENDCG
		}
	}
}
