Shader "Unlit/Sun"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	_SecondaryTex("Secondary texture", 2D) = "white" {}

	_NormalTex("Normals", 2D) = "white" {}
	_ColorEmission("Colors emission", Color) = (1,1,1,1)
		_FactorEmission("Factor emission", Range(0.1,10)) = 1
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque""LightMode" = "Deferred" }

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
#pragma target 4.0

#pragma multi_compile ___ UNITY_HDR_ON

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;

		float4 vertex : SV_POSITION;
	};

	// 2D Random
	float random(in float2 st) {
		return frac(sin(dot(st.xy,
			float2(12.9898, 78.233)))
			* 43758.5453123);
	}

	// 2D Noise based on Morgan McGuire @morgan3d
	// https://www.shadertoy.com/view/4dS3Wd
	float noise(in float2 st) {
		float2 i = floor(st);
		float2 f = frac(st);

		// Four corners in 2D of a tile
		float a = random(i);
		float b = random(i + float2(1.0, 0.0));
		float c = random(i + float2(0.0, 1.0));
		float d = random(i + float2(1.0, 1.0));

		// Smooth Interpolation

		// Cubic Hermine Curve.  Same as SmoothStep()
		float2 u = f*f*(3.0 - 2.0*f);
		// u = smoothstep(0.,1.,f);

		// Mix 4 coorners porcentages
		return lerp(a, b, u.x) +
			(c - a)* u.y * (1.0 - u.x) +
			(d - b) * u.x * u.y;
	}

	sampler2D _MainTex;
	sampler2D _NormalTex;
	sampler2D _SecondaryTex;
	float4 _MainTex_ST;
	float4 _SecondaryTex_ST;
	float4 _ColorEmission;
	float _FactorEmission;
	v2f vert(appdata v)
	{
		v2f o;
		float4 ve = v.vertex;
		ve.xyz += noise(v.vertex.xy*float2(_Time.y * 5, _Time.y * 5)) / 100.0f;
		o.vertex = UnityObjectToClipPos(ve);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.uv2 = TRANSFORM_TEX(v.uv2, _SecondaryTex);

		return o;
	}



	void frag(v2f i,
		out half4 outColor : SV_Target0,
		out half4 outNormal : SV_Target2,
		out half4 outEmission : SV_Target3
	)
	{
	    UNITY_INITIALIZE_OUTPUT(half4,outColor);
		UNITY_INITIALIZE_OUTPUT(half4,outNormal);
		UNITY_INITIALIZE_OUTPUT(half4,outEmission);
		float2 uv = i.uv;
		// sample the texture
		float4 col = tex2D(_MainTex, uv + noise(uv*float2(_Time.y, _Time.y)) / 30.0f);
		float4 col2 = tex2D(_SecondaryTex, i.uv - noise(uv*float2(_Time.y, _Time.y)) / 30.0f);

		outColor = normalize(col2*col2*0.6);
#if UNITY_HDR_ON
		outEmission = _ColorEmission*outColor*_FactorEmission;
#else 
		outEmission = float4(1, 1, 1, 0);
#endif
		outNormal.a = 0.33;
	}
	ENDCG
	}
	}

		SubShader
	{
		Tags{ "RenderType" = "Opaque""LightMode" = "ForwardBase" }

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

#pragma multi_compile ___ UNITY_HDR_ON
#pragma target 4.0
#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;

		float4 vertex : SV_POSITION;
	};

	// 2D Random
	float random(in float2 st) {
		return frac(sin(dot(st.xy,
			float2(12.9898, 78.233)))
			* 43758.5453123);
	}

	// 2D Noise based on Morgan McGuire @morgan3d
	// https://www.shadertoy.com/view/4dS3Wd
	float noise(in float2 st) {
		float2 i = floor(st);
		float2 f = frac(st);

		// Four corners in 2D of a tile
		float a = random(i);
		float b = random(i + float2(1.0, 0.0));
		float c = random(i + float2(0.0, 1.0));
		float d = random(i + float2(1.0, 1.0));

		// Smooth Interpolation

		// Cubic Hermine Curve.  Same as SmoothStep()
		float2 u = f*f*(3.0 - 2.0*f);
		// u = smoothstep(0.,1.,f);

		// Mix 4 coorners porcentages
		return lerp(a, b, u.x) +
			(c - a)* u.y * (1.0 - u.x) +
			(d - b) * u.x * u.y;
	}

	sampler2D _MainTex;
	sampler2D _NormalTex;
	sampler2D _SecondaryTex;
	float4 _MainTex_ST;
	float4 _SecondaryTex_ST;
	float4 _ColorEmission;
	float _FactorEmission;
	v2f vert(appdata v)
	{
		v2f o;
		float4 ve = v.vertex;
		//ve.xyz += noise(v.vertex.xy*float2(_Time.y * 5, _Time.y * 5)) / 200.0f;
		o.vertex = UnityObjectToClipPos(ve);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.uv2 = TRANSFORM_TEX(v.uv2, _SecondaryTex);

		return o;
	}



	void frag(v2f i,
		out half4 outColor : SV_Target0

	)
	{
		float2 uv = i.uv;
		// sample the texture
		float4 col = tex2D(_MainTex, uv + noise(uv*float2(_Time.y, _Time.y)) / 30.0f);
		float4 col2 = tex2D(_SecondaryTex, i.uv - noise(uv*float2(_Time.y, _Time.y)) / 30.0f);

		outColor = normalize(col*col2);
		outColor = _ColorEmission*outColor*_FactorEmission;

	}
	ENDCG
	}
	}
}
