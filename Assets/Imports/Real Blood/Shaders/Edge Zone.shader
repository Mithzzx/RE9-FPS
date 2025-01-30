// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Edge Zone"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (0.1933962,0.6679831,1,1)
		[HDR]_EdgeColor("EdgeColor", Color) = (1,1,1,1)
		_Depth("Depth", Range( 0 , 5)) = 0.4470588
		_DepthMaskedDistance("DepthMaskedDistance", Range( 0 , 5)) = 0.4470588
		_NoiseScale("NoiseScale", Float) = 0
		_Speed2("Speed 2", Vector) = (0,0,0,0)
		_FadeDistance("FadeDistance", Float) = 1
		_FadeEdgeWidth("FadeEdgeWidth", Float) = 1
		_Softness("Softness", Range( 0 , 1)) = 0
		_EdgeSoftness("EdgeSoftness", Range( 0 , 1)) = 0
		_MainTex("MainTex", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Int) = 2
		_NoiseRemap("NoiseRemap", Vector) = (0,1,0,1)
		_DepthMaskedTexture("DepthMaskedTexture", 2D) = "white" {}
		[HDR]_DepthMaskedColor("DepthMaskedColor", Color) = (0,0,0,0)
		_DepthMaskedTextureTiling("DepthMaskedTextureTiling", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		Cull [_Cull]
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 ase_texcoord : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
			};

			uniform int _Cull;
			uniform float4 _DepthMaskedColor;
			uniform sampler2D _DepthMaskedTexture;
			uniform float4 _DepthMaskedTexture_ST;
			uniform float _DepthMaskedTextureTiling;
			UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
			uniform float4 _CameraDepthTexture_TexelSize;
			uniform float _DepthMaskedDistance;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float4 _Color;
			uniform float4 _EdgeColor;
			uniform float _Depth;
			uniform float _EdgeSoftness;
			uniform float _FadeDistance;
			uniform float _FadeEdgeWidth;
			uniform float _Softness;
			uniform float2 _Speed2;
			uniform float _NoiseScale;
			uniform float4 _NoiseRemap;
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord1 = screenPos;
				float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.ase_texcoord2.xyz = ase_worldPos;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				o.ase_texcoord2.w = 0;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				float2 uv0_DepthMaskedTexture = i.ase_texcoord.xy * _DepthMaskedTexture_ST.xy + _DepthMaskedTexture_ST.zw;
				float4 screenPos = i.ase_texcoord1;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth93 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
				float distanceDepth93 = abs( ( screenDepth93 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _DepthMaskedDistance ) );
				float clampResult94 = clamp( distanceDepth93 , 0.0 , 1.0 );
				float2 uv_MainTex = i.ase_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float screenDepth20 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
				float distanceDepth20 = abs( ( screenDepth20 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Depth ) );
				float clampResult22 = clamp( distanceDepth20 , 0.0 , 1.0 );
				float3 ase_worldPos = i.ase_texcoord2.xyz;
				float temp_output_66_0 = distance( ase_worldPos , _WorldSpaceCameraPos );
				float clampResult76 = clamp( ( temp_output_66_0 / ( _FadeDistance + _FadeEdgeWidth ) ) , 0.0 , 1.0 );
				float smoothstepResult77 = smoothstep( _EdgeSoftness , 1.0 , clampResult76);
				float4 lerpResult49 = lerp( ( tex2D( _MainTex, uv_MainTex ) * _Color ) , _EdgeColor , ( ( 1.0 - clampResult22 ) + ( 1.0 - smoothstepResult77 ) ));
				float4 temp_output_102_0 = ( ( _DepthMaskedColor * tex2D( _DepthMaskedTexture, ( uv0_DepthMaskedTexture * _DepthMaskedTextureTiling ) ) * ( 1.0 - clampResult94 ) ) + lerpResult49 );
				float clampResult70 = clamp( ( temp_output_66_0 / _FadeDistance ) , 0.0 , 1.0 );
				float smoothstepResult71 = smoothstep( _Softness , 1.0 , clampResult70);
				float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
				float2 uv024 = i.ase_texcoord.xy * (ase_objectScale).xy + float2( 0,0 );
				float2 panner44 = ( 1.0 * _Time.y * _Speed2 + uv024);
				float simplePerlin2D37 = snoise( panner44*_NoiseScale );
				simplePerlin2D37 = simplePerlin2D37*0.5 + 0.5;
				float clampResult48 = clamp( ( (temp_output_102_0).a * smoothstepResult71 * (_NoiseRemap.z + (simplePerlin2D37 - _NoiseRemap.x) * (_NoiseRemap.w - _NoiseRemap.z) / (_NoiseRemap.y - _NoiseRemap.x)) ) , 0.0 , 1.0 );
				float4 appendResult45 = (float4((temp_output_102_0).rgb , clampResult48));
				
				
				finalColor = appendResult45;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=17000
7;1;1844;1050;-212.0029;474.053;1;True;False
Node;AmplifyShaderEditor.WorldSpaceCameraPos;63;-1333.031,675.1846;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;64;-1241.031,529.1846;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;74;-1153.387,932.0414;Float;False;Property;_FadeEdgeWidth;FadeEdgeWidth;9;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-1029.131,781.1846;Float;False;Property;_FadeDistance;FadeDistance;8;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;66;-972.0306,622.1846;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-889.3002,888.2213;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;75;-750.0432,838.2983;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1473.526,306.4968;Float;False;Property;_Depth;Depth;2;0;Create;True;0;0;False;0;0.4470588;0.07;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;73;-649.4993,977.3551;Float;False;Property;_EdgeSoftness;EdgeSoftness;11;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;76;-568.0432,823.9984;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;20;-1080.526,199.6968;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;92;-1502.291,-805.2317;Float;False;Property;_DepthMaskedDistance;DepthMaskedDistance;3;0;Create;True;0;0;False;0;0.4470588;5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;103;-1957.351,-1199.886;Float;False;0;90;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;77;-362.6432,886.3983;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;104;-1918.351,-1041.886;Float;False;Property;_DepthMaskedTextureTiling;DepthMaskedTextureTiling;17;0;Create;True;0;0;False;0;1;0.64;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;22;-806.3262,244.4968;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;93;-1109.291,-912.0317;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectScaleNode;41;-2052.439,-501.66;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;43;-1823.439,-475.66;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;94;-835.0907,-867.2317;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;23;-621.326,231.4968;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;80;-930.4456,-283.4798;Float;True;Property;_MainTex;MainTex;12;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;78;-205.343,821.3983;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1596.351,-1134.886;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;17;-915.1262,-78.40321;Float;False;Property;_Color;Color;0;1;[HDR];Create;True;0;0;False;0;0.1933962,0.6679831,1,1;1,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;29;-1325.826,-260.3032;Float;False;Property;_Speed2;Speed 2;5;0;Create;True;0;0;False;0;0,0;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ColorNode;100;-521.2403,-1299.154;Float;False;Property;_DepthMaskedColor;DepthMaskedColor;16;1;[HDR];Create;True;0;0;False;0;0,0,0,0;1.403122,0.3673093,0.3673093,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;56;-430.15,264.3039;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;24;-1513.526,-609.8031;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;81;-544.3456,-144.3798;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;91;-687.0906,-898.2317;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;90;-1139.954,-1163.393;Float;True;Property;_DepthMaskedTexture;DepthMaskedTexture;15;0;Create;True;0;0;False;0;None;b505eb209ef73844ca8d7dcd89b6f295;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;13;-690.575,46.12621;Float;False;Property;_EdgeColor;EdgeColor;1;1;[HDR];Create;True;0;0;False;0;1,1,1,1;73.51668,36.53779,36.53779,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;-147.4403,-1038.654;Float;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;49;-263.3774,-88.66192;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-1241.396,-96.19199;Float;False;Property;_NoiseScale;NoiseScale;4;0;Create;True;0;0;False;0;0;500;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;44;-1182.439,-491.66;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;67;-796.0305,671.1846;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;37;-841.426,-601.3032;Float;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;102;-94.41858,-214.1597;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector4Node;89;-799.1168,-472.8275;Float;False;Property;_NoiseRemap;NoiseRemap;14;0;Create;True;0;0;False;0;0,1,0,1;0,1,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;72;-497.843,701.7983;Float;False;Property;_Softness;Softness;10;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;70;-644.743,639.3984;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;71;-193.643,444.3984;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;86;-496.1168,-543.8275;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;47;78.51089,-34.27272;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;261.3573,120.6983;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;48;365.0109,15.02728;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;46;162.0109,-163.9727;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CameraDepthFade;57;-1079.652,1392.312;Float;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;45;451.0109,-129.9727;Float;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CameraDepthFade;50;-1098.27,1108.739;Float;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-1557.87,1560.439;Float;True;Property;_CameraEdgeWidth;CameraEdgeWidth;7;0;Create;True;0;0;False;0;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;54;-641.2701,1118.739;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-1614.07,1127.839;Float;True;Property;_CameraEdgeLength;CameraEdgeLength;6;0;Create;True;0;0;False;0;0;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;55;-822.2703,1128.739;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;60;-1271.318,1417.312;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;59;-610.6518,1336.312;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;58;-803.6519,1412.312;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;83;972.754,222.2202;Float;False;Property;_Cull;Cull;13;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;2;0;0;1;INT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;109;1339.275,-73.45132;Float;False;True;2;Float;ASEMaterialInspector;0;1;Knife/Edge Zone;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;True;False;True;0;True;83;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;False;0;False;-1;0;False;-1;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;66;0;64;0
WireConnection;66;1;63;0
WireConnection;79;0;68;0
WireConnection;79;1;74;0
WireConnection;75;0;66;0
WireConnection;75;1;79;0
WireConnection;76;0;75;0
WireConnection;20;0;21;0
WireConnection;77;0;76;0
WireConnection;77;1;73;0
WireConnection;22;0;20;0
WireConnection;93;0;92;0
WireConnection;43;0;41;0
WireConnection;94;0;93;0
WireConnection;23;0;22;0
WireConnection;78;0;77;0
WireConnection;105;0;103;0
WireConnection;105;1;104;0
WireConnection;56;0;23;0
WireConnection;56;1;78;0
WireConnection;24;0;43;0
WireConnection;81;0;80;0
WireConnection;81;1;17;0
WireConnection;91;0;94;0
WireConnection;90;1;105;0
WireConnection;101;0;100;0
WireConnection;101;1;90;0
WireConnection;101;2;91;0
WireConnection;49;0;81;0
WireConnection;49;1;13;0
WireConnection;49;2;56;0
WireConnection;44;0;24;0
WireConnection;44;2;29;0
WireConnection;67;0;66;0
WireConnection;67;1;68;0
WireConnection;37;0;44;0
WireConnection;37;1;38;0
WireConnection;102;0;101;0
WireConnection;102;1;49;0
WireConnection;70;0;67;0
WireConnection;71;0;70;0
WireConnection;71;1;72;0
WireConnection;86;0;37;0
WireConnection;86;1;89;1
WireConnection;86;2;89;2
WireConnection;86;3;89;3
WireConnection;86;4;89;4
WireConnection;47;0;102;0
WireConnection;69;0;47;0
WireConnection;69;1;71;0
WireConnection;69;2;86;0
WireConnection;48;0;69;0
WireConnection;46;0;102;0
WireConnection;57;0;60;0
WireConnection;45;0;46;0
WireConnection;45;3;48;0
WireConnection;50;0;51;0
WireConnection;54;0;55;0
WireConnection;55;0;50;0
WireConnection;60;0;51;0
WireConnection;60;1;52;0
WireConnection;59;0;58;0
WireConnection;58;0;57;0
WireConnection;109;0;45;0
ASEEND*/
//CHKSM=6E4E4C5F5A96499ACD838DF5753EF0EBD3ADE8C8