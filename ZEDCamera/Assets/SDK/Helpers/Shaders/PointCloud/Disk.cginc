//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
#include "UnityCG.cginc"
#include "../ZED_Utils.cginc"
// Uniforms
half4 _Tint;
half _PointSize;
float4x4 _Transform;

StructuredBuffer<float4> _PointBuffer;

// Vertex input attributes
struct Attributes
{
    uint vertexID : SV_VertexID;
};

// Fragment varyings
struct Varyings
{
    float4 position : SV_POSITION;
    half3 color : COLOR;
    UNITY_FOG_COORDS(0)

};

// Vertex phase
Varyings Vertex(Attributes input)
{
    // Retrieve vertex attributes.
    float4 pt = _PointBuffer[input.vertexID];
    float4 pos = mul(_Transform, float4(pt.xyz, 1));
    float4 col = PCDecodeColor(asuint(pt.w));

    // Set vertex output.
    Varyings o;
    o.position = UnityObjectToClipPos(pos);
    o.color = col;
    UNITY_TRANSFER_FOG(o, o.position);
    return o;
}

// Geometry phase
[maxvertexcount(36)]
void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream)
{
    float4 origin = input[0].position;
    float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);

    // Copy the basic information.
    Varyings o = input[0];

    // Determine the number of slices based on the radius of the
    // point on the screen.
    float radius = extent.y / origin.w * _ScreenParams.y;
    uint slices = min((radius + 1) / 5, 4) + 2;

    // Slightly enlarge quad points to compensate area reduction.
    if (slices == 2) extent *= 1.2;

    // Top vertex
    o.position.y = origin.y + extent.y;
    o.position.xzw = origin.xzw;
    outStream.Append(o);

    UNITY_LOOP for (uint i = 1; i < slices; i++)
    {
        float sn, cs;
        sincos(UNITY_PI / slices * i, sn, cs);

        // Right side vertex
        o.position.xy = origin.xy + extent * float2(sn, cs);
        outStream.Append(o);

        // Left side vertex
        o.position.x = origin.x - extent.x * sn;
        outStream.Append(o);
    }

    // Bottom vertex
    o.position.x = origin.x;
    o.position.y = origin.y - extent.y;
    outStream.Append(o);

    outStream.RestartStrip();
}

half4 Fragment(Varyings input) : SV_Target
{
    half4 c = half4(input.color, _Tint.a);
    UNITY_APPLY_FOG(input.fogCoord, c);
    return c;

}

