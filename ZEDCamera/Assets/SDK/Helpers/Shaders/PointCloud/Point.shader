//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============


Shader "Point Cloud/Point"
{
    Properties
    {
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile_fog
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _DISTANCE_ON
            #pragma multi_compile _ _COMPUTE_BUFFER

            #include "UnityCG.cginc"
		    #include "../ZED_Utils.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                half3 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_Position;
                half3 color : COLOR;
                half psize : PSIZE;
                UNITY_FOG_COORDS(0)
            };

            half4 _Tint;
            float4x4 _Transform;
            half _PointSize;

            StructuredBuffer<float4> _PointBuffer;
       

            Varyings Vertex(uint vid : SV_VertexID)
            {
          
                float4 pt = _PointBuffer[vid];
                float4 pos = mul(_Transform, float4(pt.xyz, 1));
				float4 col = PCDecodeColor(asuint(pt.w));


                Varyings o;
                o.position = UnityObjectToClipPos(pos);
                o.color = col;
                o.psize = _PointSize;

                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 c = half4(input.color, _Tint.a);
                UNITY_APPLY_FOG(input.fogCoord, c);
                return c;
            }

            ENDCG
        }
    }
}
