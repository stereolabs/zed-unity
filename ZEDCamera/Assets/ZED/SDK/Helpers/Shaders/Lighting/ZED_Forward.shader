//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
// Sets the ZED depth after the depth texture of Unity is created
Shader "ZED/ZED Forward"
{
	Properties
	{
		[HideInInspector] _MainTex("Base (RGB) Trans (A)", 2D) = "" {}
		_DepthXYZTex("Depth texture", 2D) = "" {}
		_CameraTex("Texture from ZED", 2D) = "" {}
	}

	SubShader
	{

		Stencil{
		Ref 129
		Comp[_ZEDStencilComp]
		Pass keep
	}



		Pass
		{
		
	Tags { "RenderType"="Opaque" "Queue"="Geometry" "LightMode" = "Always" }
			ZWrite On
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers nomrt
#pragma multi_compile __ ZED_XYZ
#pragma multi_compile __ DEPTH_ALPHA
#pragma multi_compile __ NO_DEPTH

			#include "UnityCG.cginc"
			#include "../ZED_Utils.cginc"
			#include "Lighting.cginc"
			#include "UnityCG.cginc"
	float4 _MaskTex_ST;
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenUV : TEXCOORD0;
				float4 depthUV : TEXCOORD1;
			};
			v2f vert (float3 v : POSITION)
			{
				v2f o;
				#if SHADER_API_D3D11
				o.pos = float4(v.x*2.0,v.y*2.0,0,1);
#elif SHADER_API_GLCORE
				o.pos = float4(v.x*2.0, -v.y*2.0, 0, 1);
#else
				o.pos = float4(v.x*2.0, v.y*2.0, 0, 1);
#endif
				o.screenUV = float4(v.x-0.5,v.y-0.5,0,1);
				o.depthUV = float4(v.x + 0.5, v.y + 0.5, 0, 1);
				return o;
			}

			sampler2D _MainTex;
			sampler2D _DepthXYZTex;
			uniform float4 _DepthXYZTex_TexelSize;
			sampler2D ZEDMaskTexGreenScreen;
			int ZEDGreenScreenActivated;
			int ZED_GreenScreen_BG;
			void frag(v2f i, 
					  out half4 outColor : SV_Target0, 
					  out float outDepth:SV_Depth)
			{
				float2 uv = i.screenUV.xy / i.screenUV.w;
				float2 depthUV = i.depthUV.xy / i.depthUV.w;
				float2 uvTex = float2(uv.x + 1.0f, uv.y + 1.0f);
				uvTex.y = 1 - uvTex.y;
				outColor = tex2D (_MainTex, uv).bgra;
#if defined(ZED_XYZ)
				float4 dxyz = tex2D(_DepthXYZTex, depthUV).xyzw;
				float realDepth = computeDepthXYZ(dxyz.rgb);
#else
				float4 dxyz = tex2D(_DepthXYZTex, depthUV).xxxx;
				float realDepth = computeDepthXYZ(dxyz.r);
#endif

				//For the green screen to apply a mask on depth
#if DEPTH_ALPHA
					float a = (tex2D(ZEDMaskTexGreenScreen, uvTex).a);
					
					a = a <= 0.5 ? 0 : 1;
					outDepth = realDepth*a;
#if NO_DEPTH
					outDepth = 0;

#endif
				
#else
					outDepth = realDepth;
#endif
				
				outColor = 0;
			}

			ENDCG
		}
	}

}
