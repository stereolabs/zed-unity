//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
//Sets the transparency. Add the shadows and the lights
Shader "Custom/Green Screen/Green Screen" {
	Properties
	{
		[MaterialToggle] directionalLightEffect("The directional light affects the real", Int) = 0

	}

		SubShader
	{
	Tags{
		"RenderType" = "Transparent"
		"Queue" = "Transparent-1"
		"LightMode" = "Always"
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
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile FINAL FOREGROUND BACKGROUND ALPHA KEY
#pragma multi_compile __ ZED_XYZ

#pragma target 4.0
#include "UnityCG.cginc"
#include "../../../SDK/Helpers/Shaders/ZED_Utils.cginc"
#define ZED_SPOT_LIGHT_DECLARATION
#define ZED_POINT_LIGHT_DECLARATION
#include "../../../SDK/Helpers/Shaders/Lighting/ZED_Lighting.cginc"
		struct appdata
	{
		float4 vertex : POSITION;
		float4 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 uv : TEXCOORD0;
		float4 pos : SV_POSITION;
		ZED_WORLD_DIR(1)

	};
	struct fragOut {
		float depth : SV_Depth;
		fixed4 color : SV_Target;
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
	v2f vert(appdata_full v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		ZED_TRANSFER_WORLD_DIR(o)

		o.uv.xy = TRANSFORM_TEX(v.texcoord, _CameraTex);
		o.uv.zw = TRANSFORM_TEX(v.texcoord, _DepthXYZTex);

		o.uv.y = 1 - o.uv.y;
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


	sampler2D _DirectionalShadowMap;

	float4 _AmnbientLight;

	fragOut frag(v2f i)
	{
		//Get the depth in XYZ format
		float4 uv = i.uv;

		float3 colorXYZ = tex2D(_DepthXYZTex,  uv.zw).xxx;
		//Compute the depth to work with Unity (1m real = 1m to Unity)
		float depthReal = computeDepthXYZ(colorXYZ.r);
		//Color from the camera
		float3 colorCamera = tex2D(_CameraTex, uv.xy).bgr;
		float3 normals = tex2D(_NormalsTex, uv.zw).rgb;
		float alpha = tex2D(_MaskTex, float2(uv.x, 1 - uv.y)).a;

		fragOut o;

		o.color.rgb = colorCamera.rgb;
		o.color.a = 1;

		float a = alpha <= 0.0 ? 0 : 1;
		o.depth = depthReal*a;



#ifndef FOREGROUND

		float4 color = tex2D(_MaskTex, float2(uv.x, 1 - uv.y)).rgba;
		half4 c = color;

		//Apply directional light
		if (directionalLightEffect == 1) {
			color *= ZED_directionalLight[1];
		}

		//Apply Shadows
		if (_HasShadows == 1) {
			float3 shadows = tex2D(_DirectionalShadowMap, fixed2(uv.z, 1 - uv.w)).rgb;
			c = color*(half4(saturate(shadows),1));
		}
		else {
			c = half4(color);
		}

		//Compute world space
		float3 worldspace;
		GET_XYZ(i, colorXYZ.x, worldspace)

		// Apply lighting
		c += saturate(computeLighting(color.rgb, normals, worldspace, 1));

		o.color.a = alpha;
		o.color.rgb = c.rgb;
		
#else

		o.depth = MAX_DEPTH;
		o.color.a = 1;
#endif

#ifdef ALPHA
		o.color.r = o.color.a;
		o.color.g = o.color.a;
		o.color.b = o.color.a;
		o.color.a = 1;
		o.depth = MAX_DEPTH;

#endif


#ifdef BACKGROUND
		o.color.a = 0;
#endif
#ifdef KEY
		o.depth = MAX_DEPTH;
		o.color.rgb =  tex2D (_MaskTex, float2(uv.x, 1 - uv.y)).rgb;
		o.color.rgb *= alpha;
		o.color.rgb = clamp(o.color.rgb, float3(0., 0., 0.), float3(1, 1, 1));
		
		o.color.a = 1;
#endif

		return o;
	}
	ENDCG
	}

	
	}
	
}