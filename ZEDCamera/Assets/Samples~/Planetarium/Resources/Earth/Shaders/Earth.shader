// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "ZED/Planetarium/FresnelShader" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_FresnelFactor("Fresnel Factor", Range(0,1)) = 0.04
		_FresnelExponent("Fresnel Exponent", Range(0,10)) = 5
	}
		SubShader{
		Tags{ "RenderType" = "Opaque""LightMode"="Deferred" }
		Stencil{
		Ref 148
		Comp Always
		Pass replace
	}


		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf SimpleSpecular


		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
	};

	half _Glossiness;
	half _Metallic;
	half _FresnelFactor;
	half _FresnelExponent;
	fixed4 _Color;



		half4 LightingSimpleSpecular(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
		half NdotL = dot(s.Normal, lightDir);
		half4 c;
		half fresnel = _FresnelFactor + (1 - _FresnelFactor)*pow(1 - dot(s.Normal, viewDir), _FresnelExponent);
		half4 fresnelColor = 3.0*half4(0.382, 0.664, 1.0, 1.0)*fresnel;
		c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten) + fresnelColor;
		c.a = s.Alpha;
		return c;
	}


	void surf(Input IN, inout SurfaceOutput o) {
		// Albedo comes from a texture tinted by color
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;

		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}
