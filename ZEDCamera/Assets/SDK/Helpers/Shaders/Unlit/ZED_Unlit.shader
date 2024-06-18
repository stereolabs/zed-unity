Shader "ZED/ZED_Unlit"
{
	Properties
	{
		_MainTexLeft("Texture", 2D) = "white" {}
		_MainTexRight("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uvLeft : TEXCOORD0;
				float2 uvRight : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uvLeft : TEXCOORD0;
				float2 uvRight : TEXCOORD1;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTexLeft;
			float4 _MainTexLeft_ST;
			sampler2D _MainTexRight;
			float4 _MainTexRight_ST;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvLeft = TRANSFORM_TEX(v.uvLeft, _MainTexLeft);
				o.uvRight = TRANSFORM_TEX(v.uvRight, _MainTexRight);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			if (unity_StereoEyeIndex == 0) {
				return tex2D(_MainTexLeft, i.uvLeft);
			}
			else return tex2D(_MainTexRight, i.uvRight);

			}
			ENDCG
		}
	}
}
