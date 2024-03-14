Shader "ZED/ZED_Unlit_RawInput"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MaxDepth("Max Depth Range", Range(1,20)) = 20
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "../ZED_Utils.cginc"

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

				sampler2D _MainTex;
				float4 _MainTex_ST;

				sampler2D _DepthXYZTex;
				float4 _DepthXYZTex_ST;

				float _MaxDepth;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
					o.uv.y = 1 - o.uv.y;

					o.uv.zw = TRANSFORM_TEX(v.uv, _DepthXYZTex);
					o.uv.w = 1 - o.uv.w;

					return o;
				}

				void frag(v2f i, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
				{
					outColor = tex2D(_MainTex, i.uv.xy).bgra;

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
