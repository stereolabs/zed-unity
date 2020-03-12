//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
 // Computes lighting and shadows and apply them to the real
	Shader "ZED/ZED Forward Lighting"
{

	Properties{
		[MaterialToggle] directionalLightEffect("Directional light affects image", Int) = 0
		_MaxDepth("Max Depth Range", Range(1,40)) = 40
	}
		SubShader
		{
			Tags{"RenderPipeline" = "" }
			ZWrite On
			Pass
			{
				Name "FORWARD"
				Tags{ "LightMode" = "Always" }

				Cull Off
				CGPROGRAM

				#define ZEDStandard
						// compile directives
				#pragma target 4.0

				#pragma vertex vert_surf
				#pragma fragment frag_surf


				#pragma multi_compile_fwdbase
				#pragma multi_compile_fwdadd_fullshadows

				#pragma multi_compile __ NO_DEPTH_OCC

				#include "HLSLSupport.cginc"
				#include "UnityShaderVariables.cginc"

				#define UNITY_PASS_FORWARDBASE
				#include "UnityCG.cginc"

				#include "AutoLight.cginc"
				#include "../ZED_Utils.cginc"
				#define ZED_SPOT_LIGHT_DECLARATION
				#define ZED_POINT_LIGHT_DECLARATION
				#include "ZED_Lighting.cginc"


				sampler2D _MainTex;

				struct Input {
					float2 uv_MainTex;
				};

				float4x4 _CameraMatrix;
				sampler2D _DirectionalShadowMap;


				struct v2f_surf {
					float4 pos : SV_POSITION;
					float4 pack0 : TEXCOORD0;
					SHADOW_COORDS(4)
						ZED_WORLD_DIR(1)

				};



				float4 _MainTex_ST;
				sampler2D _DepthXYZTex;
				float4 _DepthXYZTex_ST;
				sampler2D _MaskTex;
				int _HasShadows;
				float4 ZED_directionalLight[2];
				int directionalLightEffect;
				float _ZEDFactorAffectReal;
				float _MaxDepth;

				// vertex shader
				v2f_surf vert_surf(appdata_full v) 
				{

					v2f_surf o;
					UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
					o.pos = UnityObjectToClipPos(v.vertex);

					ZED_TRANSFER_WORLD_DIR(o)

						o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.pack0.zw = TRANSFORM_TEX(v.texcoord, _DepthXYZTex);

					o.pack0.y = 1 - o.pack0.y;
					o.pack0.w = 1 - o.pack0.w;

					TRANSFER_SHADOW(o);

					return o;
				}

				// fragment shader
				void frag_surf(v2f_surf IN, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth) 
				{
					UNITY_INITIALIZE_OUTPUT(fixed4,outColor);
					float4 uv = IN.pack0;

					float3 zed_xyz = tex2D(_DepthXYZTex, uv.zw).xxx;

					//Filter out depth values beyond the max value. 
					if (_MaxDepth < 40.0) //Avoid clipping out FAR values when not using feature. 
					{
						if (zed_xyz.z > _MaxDepth) discard;
					}

				#ifdef NO_DEPTH_OCC
					outDepth = 0;
				#else
					outDepth = computeDepthXYZ(zed_xyz.z);
				#endif


					fixed4 c = 0;
					float4 color = tex2D(_MainTex, uv.xy).bgra;
					float3 normals = tex2D(_NormalsTex, uv.zw).rgb;


					//Apply directional light
					color *=  _ZEDFactorAffectReal;

					//Compute world space
					float3 worldspace;
					GET_XYZ(IN, zed_xyz.x, worldspace)

					//Apply shadows
					// e(1) == 2.71828182846
					if (_HasShadows == 1) {
						//Depends on the ambient lighting
						float atten = saturate(tex2D(_DirectionalShadowMap, fixed2(uv.z, 1 - uv.w)) + log(1 + 1.72*length(UNITY_LIGHTMODEL_AMBIENT.rgb)/4.0));

						c = half4(color*atten);
					}
					else {
						c = half4(color);
					}


					//Add light
					c += saturate(computeLighting(color.rgb, normals, worldspace, 1));


					c.a = 0;
					outColor.rgb = c;

				}

	ENDCG

			}
		}


		SubShader //LWRP/URP-only shader. 
		{
			Tags{"RenderPipeline" = "LightweightPipeline" "RenderType" = "Opaque"}
			LOD 300

			Pass
			{

				Name "StandardLit"
				Tags{"LightMode" = "LightweightForward"}

				HLSLPROGRAM

				#ifdef ZED_LWRP

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 2.0

				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma shader_feature _RECEIVE_SHADOWS_OFF
				#pragma multi_compile _ _SHADOWS_SOFT

				#pragma vertex LitPassVertex
				#pragma fragment LitPassFragment

				#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

				//For ZED CG functions, namely depth conversion. 
				#include "../ZED_Utils.cginc"
				#define ZED_SPOT_LIGHT_DECLARATION
				#define ZED_POINT_LIGHT_DECLARATION

				//#include "ZED_Lighting.cginc"
				#include "ZED_Lighting_LWRP.cginc" //Special version that handles lighting for LWRP/URP. 

				struct Attributes
				{
					float4 positionOS   : POSITION;
					float3 normalOS     : NORMAL;
					float4 tangentOS    : TANGENT;
					float2 uv           : TEXCOORD0;
					float2 uvLM         : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct Varyings
				{
					float4 uv                       : TEXCOORD0;
					float3 positionWS				: TEXCOORD1;
					float4 positionCS               : SV_POSITION;
					};


				//ZED textures. 
				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _DepthXYZTex;
				float4 _DepthXYZTex_ST;

				//Horizontal and vertical fields of view, assigned from ZEDRenderingPlane. 
				//Needs to be assigned, not derived from projection matrix, because otherwise goofy things happen because of the Scene view camera. 
				float _ZEDHFoVRad;
				float _ZEDVFoVRad;

				float _ZEDFactorAffectReal;
				float _MaxDepth;


				//Varyings LitPassVertex(Attributes input, out float outDepth : SV_Depth)
				void LitPassVertex(Attributes input, out Varyings output)
				{
					VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
					VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

					// Computes fog factor per-vertex.
					float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

					// TRANSFORM_TEX is the same as the old shader library.
					output.uv.xy = TRANSFORM_TEX(input.uv, _MainTex);
					output.uv.y = 1 - output.uv.y;

					output.uv.zw = TRANSFORM_TEX(input.uv, _DepthXYZTex);
					output.uv.w = 1 - output.uv.w;

					output.positionWS = vertexInput.positionWS;
					output.positionCS = vertexInput.positionCS;

					}
				void LitPassFragment(Varyings input, out half4 outColor: SV_Target, out float outDepth : SV_Depth)
				{
				#ifdef NO_DEPTH_OCC
					outDepth = 0;
				#else
					float zed_z = tex2D(_DepthXYZTex, input.uv.zw).x;

					//Filter out depth values beyond the max value. 
					if (_MaxDepth < 20.0) //Avoid clipping out FAR values when not using feature. 
					{
						if (zed_z > _MaxDepth) discard;
					}

					outDepth = computeDepthXYZ(zed_z);

					//ZED Color - for now ignoring everything above. 
					half4 c;
					float4 color = tex2D(_MainTex, input.uv.xy).bgra;
					float3 normals = tex2D(_NormalsTex, input.uv.zw).rgb;

					//Apply directional light
					color *= _ZEDFactorAffectReal;

					c = color;

					//Compute world normals.
					normals = float3(normals.x, 0 - normals.y, normals.z);
					float3 worldnormals = mul((float3x3)unity_ObjectToWorld, normals); //TODO: This erroneously applies object scale to the normals. The canvas object is scaled to fill the frame. Fix. 

					//Compute world position of the pixel. 
					float xfovpartial = (input.uv.x - 0.5) * _ZEDHFoVRad;
					float yfovpartial = (1 - input.uv.y - 0.5) * _ZEDVFoVRad;

					float xpos = tan(xfovpartial) * zed_z;
					float ypos = tan(yfovpartial) * zed_z;

					float3 camrelpose = float3(xpos, ypos, -zed_z);// +_WorldSpaceCameraPos;

					float3 worldPos = mul(UNITY_MATRIX_V, float4(camrelpose.xyz, 0)) + _WorldSpaceCameraPos;

					//c.rgb = saturate(computeLighting(color.rgb, worldnormals, worldPos, 1));
					c.rgb = computeLighting(color.rgb, worldnormals, worldPos, 1, _ZEDFactorAffectReal);
					c.a = 0;
					outColor = c;

					#endif
				}

				#else
					void LitPassVertex() {}
					void LitPassFragment() {}
				#endif
				ENDHLSL
			}

			// Used for rendering shadowmaps
			UsePass "Lightweight Render Pipeline/Lit/ShadowCaster"

										// Used for depth prepass
										// If shadows cascade are enabled we need to perform a depth prepass. 
										// We also need to use a depth prepass in some cases camera require depth texture
										// (e.g, MSAA is enabled and we can't resolve with Texture2DMS
										UsePass "Lightweight Render Pipeline/Lit/DepthOnly"

										// Used for Baking GI. This pass is stripped from build.
										UsePass "Lightweight Render Pipeline/Lit/Meta"
		}
		Fallback Off
}
