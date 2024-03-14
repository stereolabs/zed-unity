Shader "ZED/Planetarium/Rings" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	
	}
		SubShader
		{
			Tags{ "RenderType" = "Transparent" "Queue"="Transparent" }

			Blend SrcAlpha OneMinusSrcAlpha

			Pass
		{
			Stencil{
			Ref 148
			Comp Always
			Pass replace
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
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float dist : TEXCOORD1;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;

		float4 _Color;
		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			o.dist = length(ObjSpaceViewDir(v.vertex));
			return o;
		}

		float4 frag(v2f i) : SV_Target
		{
			float4 color = _Color;
			//#if AWAYS
			color.a = 10 / (0.1 + i.dist*i.dist);
			color.a = _Color.a;
			//#endif
			return color;
		}
			ENDCG
		}
		}
			Fallback off
}
