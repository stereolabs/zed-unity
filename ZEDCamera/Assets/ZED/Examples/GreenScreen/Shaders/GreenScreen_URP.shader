//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Sets the transparency. Add the shadows and the lights
Shader "Custom/Green Screen/Green Screen URP" {
	Properties
	{
		[MaterialToggle] directionalLightEffect("The directional light affects the real", Int) = 0
		_CameraTex("CameraTex", 2D) = "defaulttexture" {}
	}

		SubShader
	{
	Tags{
		"RenderPipeline"="UniversalPipeline"
		"RenderType" = "Transparent"
		"Queue" = "Transparent-1"
		"LightMode" = "UniversalForward"
	}

	Pass
	{
		/*To use as a garbage matte*/
		Stencil{
		Ref 129
		Comp[_ZEDStencilComp]
		Pass keep
	}

		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha
		HLSLPROGRAM

		#pragma multi_compile FINAL FOREGROUND BACKGROUND ALPHA KEY
		#pragma multi_compile __ ZED_XYZ


		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d11_9x
		#pragma target 2.0


		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
		#pragma shader_feature _RECEIVE_SHADOWS_OFF
		#pragma multi_compile _ _SHADOWS_SOFT

		#pragma vertex vert
		#pragma fragment frag

		//#include "UnityCG.cginc"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"



		#include "../../../SDK/Helpers/Shaders/ZED_Utils.cginc"
		#define ZED_SPOT_LIGHT_DECLARATION
		#define ZED_POINT_LIGHT_DECLARATION
		#include "../../../SDK/Helpers/Shaders/Lighting/ZED_Lighting_URP.cginc"


				struct appdata
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
			};

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

			sampler2D _DepthXYZTex;
			sampler2D _CameraTex;
			float4 _DepthXYZTex_ST;
			float4 _CameraTex_ST;

			sampler2D _MaskTex;
			uniform float4x4 _ProjectionMatrix;
			float4 ZED_directionalLight[2];
			int directionalLightEffect;
			int _HasShadows;

			//Horizontal and vertical fields of view, assigned from ZEDRenderingPlane. 
			//Needs to be assigned, not derived from projection matrix, because otherwise goofy things happen because of the Scene view camera. 
			float _ZEDHFoVRad;
			float _ZEDVFoVRad;

			float _ZEDFactorAffectReal;
			float _MaxDepth;

			void vert(Attributes input, out Varyings output)
			{
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

				// TRANSFORM_TEX is the same as the old shader library.
				output.uv.xy = TRANSFORM_TEX(input.uv, _CameraTex);
				output.uv.y = 1 - output.uv.y;

				output.uv.zw = TRANSFORM_TEX(input.uv, _DepthXYZTex);
				output.uv.w = 1 - output.uv.w;

				output.positionWS = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;
			}



			uint _numberColors;
			float4 _CameraTex_TexelSize;

			int _erosion;
			uniform float4 _keyColor;
			uniform float _smoothness;
			uniform float _range;
			uniform float _spill;
			float _whiteClip;
			float _blackClip;


			sampler2D _DirectionalShadowMap;

			float4 _AmnbientLight;

			void frag(Varyings input, out half4 outColor: SV_Target, out float outDepth : SV_Depth)
			{
				/*outColor = half4(1, 0, 0, 1);
				outDepth = 1;
				return;*/

				//Get the depth in XYZ format
				float4 uv = input.uv;
				float zed_z = tex2D(_DepthXYZTex, uv.zw).x;

				//Compute the depth to work with Unity (1m real = 1m to Unity)
				float depthReal = computeDepthXYZ(zed_z);
				//Color from the camera
				float3 colorCamera = tex2D(_CameraTex, uv.xy).bgr;
				float3 normals = tex2D(_NormalsTex, uv.zw).rgb;
				float alpha = tex2D(_MaskTex, float2(uv.x, 1 - uv.y)).a;

				//fragOut o;

				outColor.rgb = colorCamera.rgb;
				outColor.a = 1;

				float a = alpha <= 0.0 ? 0 : 1;
				outDepth = depthReal * a;



		#ifndef FOREGROUND

				float4 color = tex2D(_MaskTex, float2(uv.x, 1 - uv.y)).rgba;
				half4 c = color;

				/*//Apply directional light
				if (directionalLightEffect == 1) {
					color *= ZED_directionalLight[1];
				}

				//Apply Shadows
				if (_HasShadows == 1) {
					float3 shadows = tex2D(_DirectionalShadowMap, half2(uv.z, 1 - uv.w)).rgb;
					c = color * (half4(saturate(shadows),1));
				}
				else {
					c = half4(color);
				}*/

				//Compute world normals.
				normals = float3(normals.x, 0 - normals.y, normals.z);
				float3 worldnormals = mul((float3x3)unity_ObjectToWorld, normals); //TODO: This erroneously applies object scale to the normals. The canvas object is scaled to fill the frame. Fix. 

				//Compute world position of the pixel. 
				float xfovpartial = (input.uv.x - 0.5) * _ZEDHFoVRad;
				float yfovpartial = (1 - input.uv.y - 0.5) * _ZEDVFoVRad;

				float xpos = tan(xfovpartial) * zed_z;
				float ypos = tan(yfovpartial) * zed_z;

				float3 camrelpose = float3(xpos, ypos, -zed_z);// +_WorldSpaceCameraPos;

				float3 worldPos = mul(UNITY_MATRIX_V, float4(camrelpose.xyz, 0)).xyz + _WorldSpaceCameraPos;

				// Apply lighting
				//c += saturate(computeLighting(color.rgb, normals, worldspace, 1, 1));
				c.rgb = computeLightingLWRP(colorCamera.rgb, worldnormals, worldPos, 1, _ZEDFactorAffectReal).rgb;

				outColor.a = alpha;
				outColor.rgb = c.rgb;
				//outColor.rgb = half3(1, 0, 0);
		#else

				outDepth = MAX_DEPTH;
				outColor.a = 1;
		#endif

		#ifdef ALPHA
				outColor.r = alpha;
				outColor.g = alpha;
				outColor.b = alpha;
				outColor.a = 1;
				outDepth = MAX_DEPTH;

		#endif


		#ifdef BACKGROUND
				outColor.a = 0;
		#endif
		#ifdef KEY
				outDepth = MAX_DEPTH;
				outColor.rgb = tex2D(_MaskTex, float2(uv.x, 1 - uv.y)).rgb;
				outColor.rgb *= alpha;
				outColor.rgb = clamp(c.rgb, float3(0.0, 0.0, 0.0), float3(1, 1, 1));

				outColor.a = 1;
		#endif
				//return o;
			
			}
			ENDHLSL
			}
	}
}