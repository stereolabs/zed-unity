// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unity5/Particle Softrim Dissolve/Alpha Blend"
{
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_Opacity ("Opacity", Range (0,1)) = 1
	_SoftRim ("Rim", Range(0,1.5)) = 1
	_EdgeColor ("Edge Color", Color) = (1,0,0)
    _EdgeWidth ("Edge Width", Range(0,1)) = 0.1
    _EdgeRamp ("Edge Ramp", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	//Blend One One
	//Blend SrcAlpha One
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off Lighting Off ZWrite Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma multi_compile SOFTDISSOLVE_ON SOFTDISSOLVE_OFF
			#pragma multi_compile OPACITYSLIDER_ON OPACITYSLIDER_OFF
			#pragma multi_compile STRETCH_ON STRETCH_OFF
			#pragma multi_compile SOFTRIM_ON SOFTRIM_OFF
			#pragma multi_compile EDGE_ON EDGE_OFF

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			#if EDGE_ON
			sampler2D _EdgeRamp;
			#endif
			
			struct appdata_t {
				fixed4 vertex : POSITION;
				fixed4 color : COLOR;
				fixed2 texcoord : TEXCOORD0;
				#if SOFTRIM_ON
				fixed4 normal : NORMAL;
				#endif
			};

			struct v2f {
				fixed4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				fixed2 texcoord : TEXCOORD0;
			};
			
			fixed4 _MainTex_ST;
			#if OPACITYSLIDER_ON
			fixed _Opacity;
			#endif
			#if SOFTRIM_ON
			fixed _SoftRim;
			#endif
			#if EDGE_ON
			fixed3 _EdgeColor;
			fixed _EdgeWidth;
			fixed4 _EdgeRamp_ST;
			#endif

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				#if SOFTRIM_ON
				fixed3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
					#if STRETCH_ON
					//viewDir.x = -abs(viewDir.x);
					//viewDir.y = abs(viewDir.y);
					//viewDir.z = abs(viewDir.z);
					viewDir.x = 1;
					viewDir.y = 1;
					viewDir.z = 1;				
					
					//v.normal.x = -abs(v.normal.x);
					//v.normal.y = -abs(v.normal.y);
					//v.normal.z = abs(v.normal.z);
					v.normal.x = 0;
					v.normal.y = -abs(v.normal.y);
					v.normal.z = 1;
					#endif
				fixed dotProduct = dot(v.normal, viewDir);
				o.color.rgb = v.color.rgb;
				o.color.a = smoothstep(1 - _SoftRim, 1.0, dotProduct)*v.color.a;
				#else
				o.color = v.color;
				#endif
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				
				#if SOFTDISSOLVE_ON
				fixed sf = smoothstep(0,(1-tex.a),i.color.a);
				#else
				fixed sf = step(1-i.color.a,tex.a);
				#endif
				
				#if EDGE_ON
				
					#if SOFTDISSOLVE_ON
					 if (sf < _EdgeWidth && sf > i.color.a)
					 {
					 	//sf=1;
					 	//tex.rgb = _EdgeColor;
					 	fixed4 ramp = tex2D (_EdgeRamp, fixed2(sf,1));
					 	tex.rgb = ramp.rgb*_EdgeColor;
					 }
					 #else
					 if (tex.a*i.color.a<_EdgeWidth && sf > i.color.a)
					 {
					 	sf=tex.a;
					 	fixed4 ramp = tex2D (_EdgeRamp, fixed2(tex.a*i.color.a,1));
					 	//tex.rgb = _EdgeColor;
					 	tex.rgb = ramp.rgb*_EdgeColor;
					 }
					 #endif
				#endif
				
				
				
				#if OPACITYSLIDER_ON
				clip(sf*tex.a*_Opacity);
				return fixed4(tex.rgb*i.color.rgb,sf*tex.a*_Opacity);
				#else
				clip(sf*tex.a);
				return fixed4(tex.rgb*i.color.rgb,sf*tex.a);
				#endif

			}
			ENDCG 
		}
	}CustomEditor "Shader_softrim_dissolve"		
}
}
