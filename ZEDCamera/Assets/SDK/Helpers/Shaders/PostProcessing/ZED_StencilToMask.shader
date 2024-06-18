//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
// Sets the stencil to a texture to be used as a mask
Shader "ZED/ZED StencilToMask"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }


		Pass
		{
		Stencil{
		Ref 148

		Comp Equal
	}
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
				float4 vertex : SV_POSITION;
			};

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			return float4(1,0,0,1);
			}
			ENDCG
		}
	}
}
