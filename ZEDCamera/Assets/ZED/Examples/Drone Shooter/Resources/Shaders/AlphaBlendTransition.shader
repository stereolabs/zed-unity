Shader "Custom/AlphaBlendTransition" {
Properties {
	_Color ("Color", Color) = (1,1,1,1)
	_Blend ("Texture Blend", Range(0,1)) = 0.0

	_MainTex ("Albedo (RGB)", 2D) = "white" {}
	_MainTex2 ("Albedo 2 (RGB)", 2D) = "white" {}

	_BumpMap ("Bumpmap", 2D) = "bump" {}
	_BumpScale("Scale", Float) = 1.0

	_SpecGlossMap("Roughness Map", 2D) = "gloss" {}

    _MetallicGlossMap("Metallic", 2D) = "white" {}
}

SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
	 	#pragma shader_feature _NORMALMAP
        #pragma shader_feature _METALLICGLOSSMAP
	    #pragma shader_feature _SPECGLOSSMAP

		struct Input {
			float2 uv_MainTex;
			float2 uv_MainTex2;
			float2 uv_BumpMap;
		};

		sampler2D _MainTex;
		sampler2D _MainTex2;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;

		half _Blend;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = lerp (tex2D (_MainTex, IN.uv_MainTex), tex2D (_MainTex2, IN.uv_MainTex2), _Blend) * _Color;
			o.Albedo = c.rgb;
			fixed4 metal = tex2D (_MetallicGlossMap, IN.uv_MainTex);
			o.Metallic = metal.r;

			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
			o.Alpha = c.a;
		}
		ENDCG
		}
		FallBack "Diffuse"

}

