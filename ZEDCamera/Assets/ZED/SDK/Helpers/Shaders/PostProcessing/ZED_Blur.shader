//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Blurs with predefined kernel and weights
Shader "ZED/ZED Blur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		ZWrite Off
		ZTest Always
		Cull Off


		//0 - Blur based on alpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.0
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			float4 _MainTex_TexelSize;
			uniform int horizontal;

			uniform float weights[5];
			uniform float offset[5];
			float4 frag(v2f vi) : SV_Target
			{



			float2 uv = vi.uv;
			float result = tex2D(_MainTex, uv).a * weights[0]; // current fragment's contribution
			int i = 0;
			if (horizontal == 1)
			{

					result += tex2D(_MainTex, uv + float2(offset[1] * _MainTex_TexelSize.x, 0.0)).a * weights[1];
					result += tex2D(_MainTex, uv - float2(offset[1] * _MainTex_TexelSize.x, 0.0)).a * weights[1];

					result += tex2D(_MainTex, uv + float2(offset[2] * _MainTex_TexelSize.x, 0.0)).a * weights[2];
					result += tex2D(_MainTex, uv - float2(offset[2] * _MainTex_TexelSize.x, 0.0)).a * weights[2];

				
			}
			else
			{

					result += tex2D(_MainTex, uv + float2(0.0, offset[1] * _MainTex_TexelSize.y)).a * weights[1];
					result += tex2D(_MainTex, uv - float2(0.0, offset[1] * _MainTex_TexelSize.y)).a * weights[1];

					result += tex2D(_MainTex, uv + float2(0.0, offset[2] * _MainTex_TexelSize.y)).a * weights[2];
					result += tex2D(_MainTex, uv - float2(0.0, offset[2] * _MainTex_TexelSize.y)).a * weights[2];
			}

			return float4(tex2D(_MainTex, uv).rgb, result);
			}
			ENDCG
		}
		// 1 - Blur based on RGB, R unchanged, GB blured
		Pass
			{
				CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			float4 _MainTex_TexelSize;
			uniform int horizontal;

			uniform float weights2[5];
			uniform float offset2[5];
			float4 frag(v2f vi) : SV_Target
			{



				float2 uv = vi.uv;
				float3 result = tex2D(_MainTex, uv).rgb * weights2[0]; // current fragment's contribution
				result.r = tex2D(_MainTex, uv).r;
				int i = 0;
				if (horizontal == 1)
				{

						result.gb += tex2D(_MainTex, uv + float2(offset2[1] * _MainTex_TexelSize.x, 0.0)).gb * weights2[1];
						result.gb += tex2D(_MainTex, uv - float2(offset2[1] * _MainTex_TexelSize.x, 0.0)).gb * weights2[1];

						result.gb += tex2D(_MainTex, uv + float2(offset2[2] * _MainTex_TexelSize.x, 0.0)).gb * weights2[2];
						result.gb += tex2D(_MainTex, uv - float2(offset2[2] * _MainTex_TexelSize.x, 0.0)).gb * weights2[2];
					
				}
				else
				{

						result.gb += tex2D(_MainTex, uv + float2(0.0, offset2[1] * _MainTex_TexelSize.y)).gb * weights2[1];
						result.gb += tex2D(_MainTex, uv - float2(0.0, offset2[1] * _MainTex_TexelSize.y)).gb * weights2[1];

						result.gb += tex2D(_MainTex, uv + float2(0.0, offset2[2] * _MainTex_TexelSize.y)).gb * weights2[2];
						result.gb += tex2D(_MainTex, uv - float2(0.0, offset2[2] * _MainTex_TexelSize.y)).gb * weights2[2];
					
				}

				return float4(result.x, result.y, result.z, 1);
			}
				ENDCG
			}

			// 2 - Blur based with mask
				Pass
			{
				CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
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
			sampler2D _Mask;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			float4 _MainTex_TexelSize;
			uniform int horizontal;

			uniform float weights[5];
			uniform float offset[5];
			float4 frag(v2f vi) : SV_Target
			{


				//TODO issue : The blur will shrink the virtual due to the mask
				float2 uv = vi.uv;
				float3 result = tex2D(_MainTex, uv).rgb;
				if (tex2D(_Mask, uv).r != 0) {
					result *= weights[0]; // current fragment's contribution
					int i = 0;
					if (horizontal == 1)
					{

							result += tex2D(_MainTex, uv + float2(offset[1] * _MainTex_TexelSize.x, 0.0)).rgb * weights[1];
							result += tex2D(_MainTex, uv - float2(offset[1] * _MainTex_TexelSize.x, 0.0)).rgb * weights[1];

							result += tex2D(_MainTex, uv + float2(offset[2] * _MainTex_TexelSize.x, 0.0)).rgb * weights[2];
							result += tex2D(_MainTex, uv - float2(offset[2] * _MainTex_TexelSize.x, 0.0)).rgb * weights[2];
						
					}
					else
					{

							result += tex2D(_MainTex, uv + float2(0.0, offset[1] * _MainTex_TexelSize.y)).rgb * weights[1];
							result += tex2D(_MainTex, uv - float2(0.0, offset[1] * _MainTex_TexelSize.y)).rgb * weights[1];

							result += tex2D(_MainTex, uv + float2(0.0, offset[2] * _MainTex_TexelSize.y)).rgb * weights[2];
							result += tex2D(_MainTex, uv - float2(0.0, offset[2] * _MainTex_TexelSize.y)).rgb * weights[2];
						
					}
				}

				return float4(result, 1);
			}
				ENDCG
			}

					//3 - Blur based on R
				Pass
			{
				CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			float4 _MainTex_TexelSize;
			uniform int horizontal;

			uniform float weights[5];
			uniform float offset[5];
			float4 frag(v2f vi) : SV_Target
			{



				float2 uv = vi.uv;
				float result = tex2D(_MainTex, uv).r * weights[0]; // current fragment's contribution
				int i = 0;
				if (horizontal == 1)
				{

						result += tex2D(_MainTex, uv + float2(offset[1] * _MainTex_TexelSize.x, 0.0)).r * weights[1];
						result += tex2D(_MainTex, uv - float2(offset[1] * _MainTex_TexelSize.x, 0.0)).r * weights[1];

						result += tex2D(_MainTex, uv + float2(offset[2] * _MainTex_TexelSize.x, 0.0)).r * weights[2];
						result += tex2D(_MainTex, uv - float2(offset[2] * _MainTex_TexelSize.x, 0.0)).r * weights[2];
					
				}
				else
				{

						result += tex2D(_MainTex, uv + float2(0.0, offset[1] * _MainTex_TexelSize.y)).r * weights[1];
						result += tex2D(_MainTex, uv - float2(0.0, offset[1] * _MainTex_TexelSize.y)).r * weights[1];

						result += tex2D(_MainTex, uv + float2(0.0, offset[2] * _MainTex_TexelSize.y)).r * weights[2];
						result += tex2D(_MainTex, uv - float2(0.0, offset[2] * _MainTex_TexelSize.y)).r * weights[2];
					
				}

				return float4(result, result, result, result);
			}
				ENDCG
			}
	}
}
