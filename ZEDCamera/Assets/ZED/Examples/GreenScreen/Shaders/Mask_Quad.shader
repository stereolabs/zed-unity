Shader "Custom/Green Screen/Mask Quad"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent"
		"Queue" = "Transparent-2"

	}
		Cull Off
		Lighting On
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
	{
		//Write in the stencil
		Stencil{
		Ref 129
		Comp always
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
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float alpha;
	v2f vert(appdata v)
	{
		v2f o;
		//o.vertex = UnityObjectToClipPos(v.vertex);

		o.vertex = mul(mul(UNITY_MATRIX_P, UNITY_MATRIX_V), v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return fixed4(1,1,1,alpha);
	}
		ENDCG
	}
	}

}

