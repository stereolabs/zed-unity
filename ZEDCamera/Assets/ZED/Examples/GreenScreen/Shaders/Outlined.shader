//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
// Outlines the markers of the garbage matte
Shader "Custom/Green Screen/ Outlined" {

	Properties{
		_Color("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", Range(0.0, 0.03)) = .005
		_MainTex("Base (RGB)", 2D) = "white" { }
	}

		CGINCLUDE
#pragma target 4.0
#include "UnityCG.cginc"

		struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : POSITION;
		float4 color : COLOR;
	};

	uniform float _Outline;
	uniform float4 _OutlineColor;
	float4 _Color;

	v2f vert(appdata v) {
		// just make a copy of incoming vertex data but scaled according to normal direction
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		float3 norm = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
		float2 offset = TransformViewToProjection(norm.xy);

		o.pos.xy += offset * o.pos.z * _Outline;
		o.color = _OutlineColor;
		return o;
	}
	ENDCG

		SubShader{
		Tags{ "Queue" = "Transparent+2" }

		// note that a vertex shader is specified here but its using the one above
		Pass{
		Name "OUTLINE"
		Tags{ "LightMode" = "Always" }
		Cull Off
		ZWrite Off
		//ZTest Always
		ColorMask RGB // alpha not used

		Blend SrcAlpha OneMinusSrcAlpha // Normal


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
		half4 frag(v2f i) :COLOR{
		return i.color;
	}
		ENDCG
	}

		Pass
	{
		CGPROGRAM
#pragma vertex vert2
#pragma fragment frag2
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"


	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4x4 _projection;

	v2f vert2(appdata v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.color = _Color;
		return o;
	}

	float4 frag2(v2f i) : SV_Target
	{
		return _Color;
	}
		ENDCG
	}

	}
		//Fallback "Diffuse"
}
