Shader "Custom/InnerGlow" {
	Properties{
		_MainTex("Diffuse(RGB) Spec(A)", 2D) = "white" {}
	_BumpMap("Bumpmap", 2D) = "bump" {}
	_RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower("Rim Power", Range(0.5,8.0)) = 3.0
		_SpecColor("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Shininess("Shininess", Range(0.01, 1)) = 0.078125
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM

#pragma surface surf NoLighting

		float _Shininess;

	half4 LightingSimpleSpecular(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
		half3 h = normalize(lightDir + viewDir);
		half diff = max(0, dot(s.Normal, lightDir));
		float nh = max(0, dot(s.Normal, h));
		float spec = pow(nh, 48.0);
		half4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec * s.Alpha * _Shininess * _SpecColor) * (atten * 2);
		c.a = s.Alpha;
		return c;
	}

	struct Input {
		float2 uv_MainTex;
		float2 uv_BumpMap;
		float3 viewDir;
	};
	sampler2D _MainTex;
	sampler2D _BumpMap;
	float4 _RimColor;
	float _RimPower;

	void surf(Input IN, inout SurfaceOutput o) {
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
		o.Emission = _RimColor.rgb * pow(rim, _RimPower);
		o.Alpha = tex2D(_MainTex, IN.uv_MainTex).a;
	}

	fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
	{
		fixed4 c;
		c.rgb = s.Albedo;
		c.a = s.Alpha;
		return c;
	}

	ENDCG
	}
		Fallback "Diffuse"
}
