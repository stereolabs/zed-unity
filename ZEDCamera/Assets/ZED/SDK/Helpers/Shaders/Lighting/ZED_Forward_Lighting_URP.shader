//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
 // Computes lighting and shadows and apply them to the real
Shader "ZED/ZED Forward Lighting URP"
{

	Properties{
		[MaterialToggle] directionalLightEffect("Directional light affects image", Int) = 0
		_MaxDepth("Max Depth Range", Range(1,40)) = 40
	}

		SubShader //URP-only shader.
	{
		Tags{"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"}

		Pass
		{
		Name "StandardLit"

		Tags{"LightMode" = "UniversalForward"}

		HLSLPROGRAM

#ifndef ZED_URP

		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d11_9x
		#pragma target 2.0

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
		#pragma shader_feature _RECEIVE_SHADOWS_OFF
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma multi_compile __ NO_DEPTH

		#pragma vertex vert
		#pragma fragment frag

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		//For ZED CG functions, namely depth conversion.
		#include "../ZED_Utils.cginc"
		#define ZED_SPOT_LIGHT_DECLARATION
		#define ZED_POINT_LIGHT_DECLARATION

		#include "ZED_Lighting_URP.cginc"

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

		float _cx;
		float _cy;

		//Varyings LitPassVertex(Attributes input, out float outDepth : SV_Depth)
		void vert(Attributes input, out Varyings output)
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

		void frag(Varyings input, out float4 outColor: SV_Target, out float outDepth : SV_Depth)
		{

			float zed_z = tex2D(_DepthXYZTex, input.uv.zw).x;
			//Filter out depth values beyond the max value.
			if (_MaxDepth < 20.0) //Avoid clipping out FAR values when not using feature.
			{
				if (zed_z > _MaxDepth) discard;
			}
			//ZED Depth
			#ifdef NO_DEPTH
					outDepth = 0;
			#else
					outDepth = computeDepthXYZ(zed_z);
			#endif


			//ZED Color - for now ignoring everything above.
			half4 c;
			float4 color = tex2D(_MainTex, input.uv.xy).bgra;
			float4 normals = float4(tex2D(_NormalsTex, input.uv.zw).bgr,0);

			//Apply directional light
			color *= _ZEDFactorAffectReal;

			c = color;

			//Compute world normals.
			//normals = float4(normals.x, 0 - normals.y, normals.z, 0);
			float4 worldnormals = mul(unity_ObjectToWorld, normals); //TODO: This erroneously applies object scale to the normals. The canvas object is scaled to fill the frame. Fix.

			//Compute world position of the pixel.
			float xfovpartial = (input.uv.x - _cx) * _ZEDHFoVRad;
			float yfovpartial = (1 - input.uv.y - _cy) * _ZEDVFoVRad;

			float xpos = tan(xfovpartial) * zed_z;
			float ypos = tan(yfovpartial) * zed_z;

			float3 camrelpose = float3(xpos, ypos, -zed_z);// +_WorldSpaceCameraPos;

			float3 worldPos = mul(UNITY_MATRIX_I_V, float4(camrelpose.xyz, 0)).xyz + _WorldSpaceCameraPos;

			//c.rgb = saturate(computeLighting(color.rgb, worldnormals, worldPos, 1));
			c.rgb = computeLightingLWRP(color.rgb, worldnormals.xyz, worldPos, 1, _ZEDFactorAffectReal).rgb;
			c.a = 0;

			outColor = c;
		}
		#endif
						ENDHLSL
					}
		// Used for rendering shadowmaps
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"

							// Used for depth prepass
							// If shadows cascade are enabled we need to perform a depth prepass.
							// We also need to use a depth prepass in some cases camera require depth texture
							// (e.g, MSAA is enabled and we can't resolve with Texture2DMS
							UsePass "Universal Render Pipeline/Lit/DepthOnly"

							// Used for Baking GI. This pass is stripped from build.
							UsePass "Universal Render Pipeline/Lit/Meta"
	}

		Fallback Off

}
