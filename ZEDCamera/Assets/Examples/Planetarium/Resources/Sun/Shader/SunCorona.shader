Shader "Unlit/SunCorona"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,0,0,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 viewDir : TEXCOORD1;
				float3 normal : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				//v.vertex.xyz += clamp((v.normal)*noise(v.vertex.xy*float2(_Time.z, _Time.z))/ 20.0f,0,0.2f);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				//o.vertex.xyz += o.vertex.xyz*UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz);
				return o;
			}
			float4 _Color;
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 color = tex2D(_MainTex, i.uv)*_Color;
				float alpha = clamp((1 - dot(i.normal, i.viewDir))*3 - 1.2,0,1);
				color *= alpha;
				//color.a = 1;
				return color;
			}
			ENDCG
		}
	}
}
