Shader "ZED/ZED_GreenScreen_Unlit"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "white" {}
		_MaxDepth("Max Depth Range", Range(1,20)) = 20
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }

			Pass
			{
				/*To use as a garbage matte*/
				Stencil{
					Ref 129
					Comp[_ZEDStencilComp]
					Pass keep
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "../../../SDK/Helpers/Shaders/ZED_Utils.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D _CameraTex;
				float4 _CameraTex_ST;

				sampler2D _DepthXYZTex;
				float4 _DepthXYZTex_ST;

				sampler2D _NormalsTex;
				float4 _NormalsTex_ST;

				float _MaxDepth;

				sampler2D _MaskTex;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					o.uv.xy = TRANSFORM_TEX(v.uv, _CameraTex);
					o.uv.y = 1 - o.uv.y;

					o.uv.zw = TRANSFORM_TEX(v.uv, _DepthXYZTex);
					o.uv.w = 1 - o.uv.w;

					return o;
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

				void frag(v2f i, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
				{
					//outColor = tex2D(_MainTex, i.uv.xy).bgra;

					float3 colorXYZ = tex2D(_DepthXYZTex, i.uv.zw).xxx;

					//Color from the camera
					float3 colorCamera = tex2D(_CameraTex, i.uv.xy).bgr;
					float3 normals = tex2D(_NormalsTex, i.uv.zw).rgb;
					float alpha = tex2D(_MaskTex, float2(i.uv.x, 1 - i.uv.y)).a;

					outColor.rgb = colorCamera.rgb;
					outColor.a = 1;

					if (alpha == 0.0) discard;

	#ifdef NO_DEPTH_OCC
					outDepth = 0;
	#else
					float zed_z = tex2D(_DepthXYZTex, i.uv.zw).x;

					//Filter out depth values beyond the max value. 
					if (_MaxDepth < 20.0) //Avoid clipping out FAR values when not using feature. 
					{
						if (zed_z > _MaxDepth) discard;
					}

					outDepth = computeDepthXYZ(zed_z);
	#endif
				}
				ENDCG
			}
		}
}
