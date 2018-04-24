//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Set the depth/normals just after the depth texture is created
Shader "ZED/ZED Deferred"
{
Properties
	{
		[HideInInspector] _MainTex("Base (RGB) Trans (A)", 2D) = "" {}
		_DepthXYZTex("Depth texture", 2D) = "" {}
		_CameraTex("Texture from ZED", 2D) = "" {}
	}
	SubShader
	{
		
		Pass
		{
		// Stencil is 2^8 bits, Unity uses 4 first bits 
		// 1 0 0 0 0 0 0 0 enables all lights
		// 1 1 0 0 0 0 0 0 enables all except the light may be not rendered if too far way
		// 1 1 1 0 0 0 0 0 enables all lights



		Stencil {
                Ref 128 
                Comp always
                Pass replace
		
            }


			ZWrite On
			Cull Front

			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "../ZED_Utils.cginc"
			#include "Lighting.cginc"
			#include "UnityCG.cginc"
			 #pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile __ ZED_XYZ
			#pragma multi_compile __ NO_DEPTH_OCC
			struct v2f
			{
				float4 pos : POSITION;
				float4 screenUV : TEXCOORD0;
				float4 depthUV : TEXCOORD1;

			};
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4x4 _Model;
	float4x4 _Projection;
	sampler2D _DepthXYZTex;
	float4 _DepthXYZTex_TexelSize;
	float4 _DepthXYZTex_ST;

			v2f vert (v2f v)
			{
				v2f o;
#if SHADER_API_D3D11
				o.pos = float4(v.pos.x*2.0, v.pos.y*2.0, 0, 1);
#elif SHADER_API_GLCORE || SHADER_API_VULKAN
				o.pos = float4(v.pos.x*2.0, -v.pos.y*2.0, 0, 1);

#endif
				o.screenUV = float4(v.pos.x - 0.5, v.pos.y - 0.5, 0, 1);
				o.depthUV = float4(v.pos.x + 0.5f, v.pos.y + 0.5f, 0, 1);
				return o;

			}

			sampler2D _MaskTex;
			sampler2D _NormalsTex;
			float _Exposure;
			uniform int _ZEDReferenceMeasure;
			uniform int ZEDGreenScreenActivated;
			sampler2D ZEDMaskTexGreenScreen;
			void frag(v2f i, 
					  out half4 outColor : SV_Target0, 
					  out half4 outSpecRoughness : SV_Target1, 
					  out half4 outNormal : SV_Target2, 
					  out half4 outEmission : SV_Target3,
					  out float outDepth:DEPTH)
			{
				float2 uv = i.screenUV.xy / i.screenUV.w;
#if defined(ZED_XYZ)
				float4 dxyz = tex2D (_DepthXYZTex, uv).xyzw;
				float d = computeDepthXYZ(dxyz.xyz);
#else
				float4 dxyz = tex2D(_DepthXYZTex, i.depthUV).xxxx;
				float d = computeDepthXYZ(dxyz.x);
#endif



				outSpecRoughness = half4(0,0,0,0);

				
				float3 normals = tex2D(_NormalsTex, i.depthUV).rgb;
				outColor = saturate(tex2D (_MainTex, i.depthUV).bgra);

				#ifdef NO_DEPTH_OCC
					    outDepth = 0;
				#else
						outDepth = saturate(d);
				#endif

								

				if (ZEDGreenScreenActivated == 1) {
					float a = (tex2D(ZEDMaskTexGreenScreen, float2(i.depthUV.x, 1 - i.depthUV.y)).a);

					a = a <= 0.5 ? 0 : 1;

					outDepth *= a;
				}
				#if UNITY_HDR_ON
				outEmission = half4(0,0,0,0.5);
				#else
				outEmission = half4(1,1,1,0);

				#endif
				outColor.a = 0;
				
				//Normal to world pos
				normals.rgb = mul((float3x3)unity_CameraToWorld, float3(normals)).rgb;
				normals = normalize(normals);
				outNormal.rgb = normals*0.5 + 0.5;

				outNormal.w = 0.33; // Used as mask
			}
			ENDCG
		}
	}
}