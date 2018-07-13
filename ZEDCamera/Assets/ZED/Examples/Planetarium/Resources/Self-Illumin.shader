Shader "Self-Illumin/Transparent/Cutout/Diffuse" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
		_Illum("Illumin (A)", 2D) = "white" {}
	_EmissionLM("Emission (Lightmapper)", Float) = 0
	}

		SubShader{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		CGPROGRAM
#pragma surface surf Lambert

		sampler2D _MainTex;
	sampler2D _Illum;
	fixed4 _Color;

	struct Input {
		float2 uv_MainTex;
		float2 uv_Illum;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
		o.Alpha = c.a;
	}
	ENDCG
	}

		Fallback "Transparent/Cutout/VertexLit"
}
