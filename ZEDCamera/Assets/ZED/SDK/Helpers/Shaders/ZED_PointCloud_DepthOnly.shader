// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Displays point cloud though geometry
Shader "ZED/ZED PointCloud"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Size("Size", Range(0.1,2)) = 0.1
		_CXTest("CX Test", Range(-1.0, 1)) = 0.0
	}
	SubShader
	{


		Pass
		{
			Cull Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag


			#include "UnityCG.cginc"
			

			struct PS_INPUT
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;

			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			//sampler2D _XYZTex;
			sampler2D _DepthTex;
			sampler2D _ColorTex;
			float4 _XYZTex_TexelSize;
			float4 _DepthTex_TexelSize;
			float4x4 _Position;

			float _ZEDHFoVRad;
			float _ZEDVFoVRad;

			float _ZEDCX;
			float _ZEDCY;

			float _CXTest;

			float _Size;
			PS_INPUT vert (appdata_full v, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				PS_INPUT o;
				o.normal = v.normal;

				//Compute the UVS
				/*float2 uv = float2(
							clamp(fmod(instance_id, _XYZTex_TexelSize.z) * _XYZTex_TexelSize.x, _XYZTex_TexelSize.x, 1.0 - _XYZTex_TexelSize.x),
							clamp(((instance_id -fmod(instance_id, _XYZTex_TexelSize.z) * _XYZTex_TexelSize.x) / _XYZTex_TexelSize.z) * _XYZTex_TexelSize.y, _XYZTex_TexelSize.y, 1.0 - _XYZTex_TexelSize.y)
							);*/
				
				float2 uv = float2(
							clamp(fmod(instance_id, _DepthTex_TexelSize.z) * _DepthTex_TexelSize.x, _DepthTex_TexelSize.x, 1.0 - _DepthTex_TexelSize.x),
							clamp(((instance_id -fmod(instance_id, _DepthTex_TexelSize.z) * _DepthTex_TexelSize.x) / _DepthTex_TexelSize.z) * _DepthTex_TexelSize.y, _DepthTex_TexelSize.y, 1.0 - _DepthTex_TexelSize.y)
							);
				
				


				//Load the texture
				//float4 XYZPos = float4(tex2Dlod(_XYZTex, float4(uv, 0.0, 0.0)).rgb ,1.0f);

				//Extract position from depth. 
				float zed_z = tex2Dlod(_DepthTex, float4(uv, 0.0, 0.0)).x;

				float hfov = atan(1.0 / unity_CameraProjection[0, 0]) * 2.0;// *(180.0 / 3.14159265358979323846f);
				float vfov = atan(1.0 / unity_CameraProjection[1, 1]) * 2.0;// *(180.0 / 3.14159265358979323846f);

				float xfovpartial = (uv.x - 0.5 + _CXTest) * _ZEDHFoVRad;
				float yfovpartial = (1 - uv.y - 0.5) * _ZEDVFoVRad;

				float xpos = tan(xfovpartial) * zed_z;
				float ypos = tan(yfovpartial) * zed_z;

				float4 XYZPos = float4(xpos, ypos, zed_z, 1.0);



				//Set the World pos
				o.position = mul(mul(UNITY_MATRIX_VP, _Position ), XYZPos);
				//o.position = mul(UNITY_MATRIX_VP, XYZPos);

				o.color =  float4(tex2Dlod(_ColorTex, float4(uv, 0.0, 0.0)).bgr ,1.0f);

				return o;
			}

			struct gs_out {
				float4 position : SV_POSITION;
				float4 color : COLOR;
			};


			
			fixed4 frag (PS_INPUT i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}
