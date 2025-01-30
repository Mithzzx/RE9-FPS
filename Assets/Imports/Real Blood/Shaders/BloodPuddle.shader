// Upgrade NOTE: upgraded instancing buffer 'KnifeBloodPuddle' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Blood/Puddle"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		[NoScaleOffset]_Normals("Normals", 2D) = "bump" {}
		[NoScaleOffset]_Specular("Specular", 2D) = "black" {}
		_Noise("Noise", 2D) = "white" {}
		_Power("Power", Float) = 1
		_MaxDistance("MaxDistance", Float) = 1
		_ShowFraction("ShowFraction", Range( 0 , 1)) = 0
		_Tint("Tint", Color) = (1,1,1,1)
		_NormalScale("NormalScale", Float) = 1
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Power2("Power2", Float) = 1
		_Power3("Power3", Float) = 1
		_Mul("Mul", Float) = 1
		_ColumnsRows("ColumnsRows", Vector) = (2,2,0,0)
		_FrameNoise("FrameNoise", Float) = 1
		_SpecularTint("SpecularTint", Color) = (0,0,0,0)
		_Alpha("Alpha", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		struct Input
		{
			half2 uv_texcoord;
			float3 worldPos;
		};

		uniform half _NormalScale;
		uniform sampler2D _Normals;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform half2 _ColumnsRows;
		uniform half _FrameNoise;
		uniform half4 _Tint;
		uniform sampler2D _Specular;
		uniform half4 _SpecularTint;
		uniform half _Smoothness;
		uniform sampler2D _Noise;
		uniform half _ShowFraction;
		uniform half _Power2;
		uniform half _MaxDistance;
		uniform half _Power;
		uniform half _Mul;
		uniform half _Power3;

		UNITY_INSTANCING_BUFFER_START(KnifeBloodPuddle)
			UNITY_DEFINE_INSTANCED_PROP(half4, _Noise_ST)
#define _Noise_ST_arr KnifeBloodPuddle
			UNITY_DEFINE_INSTANCED_PROP(half, _Alpha)
#define _Alpha_arr KnifeBloodPuddle
		UNITY_INSTANCING_BUFFER_END(KnifeBloodPuddle)

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv0_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 transform154 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
			// *** BEGIN Flipbook UV Animation vars ***
			// Total tiles of Flipbook Texture
			float fbtotaltiles152 = _ColumnsRows.x * _ColumnsRows.y;
			// Offsets for cols and rows of Flipbook Texture
			float fbcolsoffset152 = 1.0f / _ColumnsRows.x;
			float fbrowsoffset152 = 1.0f / _ColumnsRows.y;
			// Speed of animation
			float fbspeed152 = _Time[ 1 ] * 0.0;
			// UV Tiling (col and row offset)
			float2 fbtiling152 = float2(fbcolsoffset152, fbrowsoffset152);
			// UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
			// Calculate current tile linear index
			float fbcurrenttileindex152 = round( fmod( fbspeed152 + ( ( transform154.x + transform154.z ) * _FrameNoise ), fbtotaltiles152) );
			fbcurrenttileindex152 += ( fbcurrenttileindex152 < 0) ? fbtotaltiles152 : 0;
			// Obtain Offset X coordinate from current tile linear index
			float fblinearindextox152 = round ( fmod ( fbcurrenttileindex152, _ColumnsRows.x ) );
			// Multiply Offset X by coloffset
			float fboffsetx152 = fblinearindextox152 * fbcolsoffset152;
			// Obtain Offset Y coordinate from current tile linear index
			float fblinearindextoy152 = round( fmod( ( fbcurrenttileindex152 - fblinearindextox152 ) / _ColumnsRows.x, _ColumnsRows.y ) );
			// Reverse Y to get tiles from Top to Bottom
			fblinearindextoy152 = (int)(_ColumnsRows.y-1) - fblinearindextoy152;
			// Multiply Offset Y by rowoffset
			float fboffsety152 = fblinearindextoy152 * fbrowsoffset152;
			// UV Offset
			float2 fboffset152 = float2(fboffsetx152, fboffsety152);
			// Flipbook UV
			half2 fbuv152 = uv0_Albedo * fbtiling152 + fboffset152;
			// *** END Flipbook UV Animation vars ***
			o.Normal = UnpackScaleNormal( tex2D( _Normals, fbuv152 ), _NormalScale );
			float4 temp_output_83_0 = ( _Tint * tex2D( _Albedo, fbuv152 ) );
			o.Albedo = (temp_output_83_0).rgb;
			float4 temp_output_159_0 = ( tex2D( _Specular, fbuv152 ) + _SpecularTint );
			o.Specular = temp_output_159_0.rgb;
			o.Smoothness = ( (temp_output_159_0).a * _Smoothness );
			float4 _Noise_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_Noise_ST_arr, _Noise_ST);
			float2 uv_Noise = i.uv_texcoord * _Noise_ST_Instance.xy + _Noise_ST_Instance.zw;
			float temp_output_151_0 = (0.4 + (_ShowFraction - 0.0) * (0.8 - 0.4) / (1.0 - 0.0));
			float clampResult13 = clamp( temp_output_151_0 , 0.0 , 100000.0 );
			float clampResult72 = clamp( ( tex2D( _Noise, uv_Noise ).r + (-1.0 + (clampResult13 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) ) , 0.0 , 1.0 );
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float clampResult16 = clamp( ( 1.0 - ( ( distance( float3( 0,0,0 ) , ase_vertex3Pos ) - (-_MaxDistance + (temp_output_151_0 - 0.0) * (_MaxDistance - -_MaxDistance) / (1.0 - 0.0)) ) / _MaxDistance ) ) , 0.0 , 1.0 );
			float temp_output_67_0 = pow( clampResult16 , _Power );
			float clampResult109 = clamp( ( ( ( pow( clampResult72 , _Power2 ) * temp_output_67_0 ) * _Mul ) + pow( temp_output_67_0 , _Power3 ) ) , 0.0 , 1.0 );
			float _Alpha_Instance = UNITY_ACCESS_INSTANCED_PROP(_Alpha_arr, _Alpha);
			o.Alpha = ( (temp_output_83_0).a * clampResult109 * _Alpha_Instance );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardSpecular alpha:fade keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				SurfaceOutputStandardSpecular o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandardSpecular, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17000
7;1;1844;1050;943.7;351.5022;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;30;-3964.635,1976.671;Half;False;Property;_ShowFraction;ShowFraction;6;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-4309.897,1886.485;Half;False;Property;_MaxDistance;MaxDistance;5;0;Create;True;0;0;False;0;1;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;42;-3880.094,2077.613;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;66;-3911.791,2232.45;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;151;-3693.122,1972.312;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.4;False;4;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;25;-3527.109,1724.137;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;24;-3100.695,1717.788;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;39;-3468.517,1986.374;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;38;-2888.853,1739.337;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;13;-3264.475,1487.537;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;100000;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;14;-3030.475,1480.537;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-2555.753,1148.81;Float;True;Property;_Noise;Noise;3;0;Create;True;0;0;False;0;None;e28dc97a9541e3642a48c0e3886688c5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;26;-2631.116,1829.243;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;28;-2427.119,1683.099;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectToWorldTransfNode;154;-2038.201,502.2243;Float;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;7;-2234.999,1256.576;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-2233.621,1620.768;Half;False;Property;_Power;Power;4;0;Create;True;0;0;False;0;1;39.38;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-1760.201,685.2243;Half;False;Property;_FrameNoise;FrameNoise;14;0;Create;True;0;0;False;0;1;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-2045.012,1348.652;Half;False;Property;_Power2;Power2;10;0;Create;True;0;0;False;0;1;3.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;16;-2224.119,1468.099;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;72;-2009.053,1238.626;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;155;-1726.201,532.2243;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;95;-1821.431,1255.493;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;67;-1830.986,1535.271;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;153;-1567.201,309.2243;Half;False;Property;_ColumnsRows;ColumnsRows;13;0;Create;True;0;0;False;0;2,2;2,2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;81;-1794.391,14.44153;Float;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;156;-1565.201,567.2243;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;147;-1477.142,1553.453;Half;False;Property;_Power3;Power3;11;0;Create;True;0;0;False;0;1;2.65;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;144;-1124.102,1230.022;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;152;-1151.201,315.2243;Float;False;0;0;6;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;149;-1162.142,1348.453;Half;False;Property;_Mul;Mul;12;0;Create;True;0;0;False;0;1;25.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;-952.1416,1211.453;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;158;-642.2706,553.1838;Half;False;Property;_SpecularTint;SpecularTint;15;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;82;-706.3907,-369.5585;Half;False;Property;_Tint;Tint;7;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;146;-1293.142,1418.453;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-863,-193;Float;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;False;0;None;88fe64047f18469449d4f995c074ce24;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-732,263;Float;True;Property;_Specular;Specular;2;1;[NoScaleOffset];Create;True;0;0;False;0;None;0469071655e1aa946a6277ed7b6bff43;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;-394.3907,-217.5585;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;145;-820.6133,1254.756;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;159;-410.2706,282.1838;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;161;-267.271,347.1838;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;85;-245.3907,-131.5585;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;162;73.30005,520.4978;Float;False;InstancedProperty;_Alpha;Alpha;16;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-382.3907,482.4415;Half;False;Property;_Smoothness;Smoothness;9;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-1080.391,178.4415;Half;False;Property;_NormalScale;NormalScale;8;0;Create;True;0;0;False;0;1;0.56;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;109;-307.3147,864.9542;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;319,287;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;84;-259.3907,-232.5585;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-23.39069,391.4415;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-739,51;Float;True;Property;_Normals;Normals;1;1;[NoScaleOffset];Create;True;0;0;False;0;None;ca9b718a0a24735459bb19022f4fd5a3;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;608.5332,-55.73052;Half;False;True;2;Half;ASEMaterialInspector;0;0;StandardSpecular;Knife/Blood/Puddle;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;42;0;27;0
WireConnection;66;0;27;0
WireConnection;151;0;30;0
WireConnection;24;1;25;0
WireConnection;39;0;151;0
WireConnection;39;3;42;0
WireConnection;39;4;66;0
WireConnection;38;0;24;0
WireConnection;38;1;39;0
WireConnection;13;0;151;0
WireConnection;14;0;13;0
WireConnection;26;0;38;0
WireConnection;26;1;27;0
WireConnection;28;0;26;0
WireConnection;7;0;4;1
WireConnection;7;1;14;0
WireConnection;16;0;28;0
WireConnection;72;0;7;0
WireConnection;155;0;154;1
WireConnection;155;1;154;3
WireConnection;95;0;72;0
WireConnection;95;1;96;0
WireConnection;67;0;16;0
WireConnection;67;1;11;0
WireConnection;156;0;155;0
WireConnection;156;1;157;0
WireConnection;144;0;95;0
WireConnection;144;1;67;0
WireConnection;152;0;81;0
WireConnection;152;1;153;1
WireConnection;152;2;153;2
WireConnection;152;4;156;0
WireConnection;148;0;144;0
WireConnection;148;1;149;0
WireConnection;146;0;67;0
WireConnection;146;1;147;0
WireConnection;1;1;152;0
WireConnection;3;1;152;0
WireConnection;83;0;82;0
WireConnection;83;1;1;0
WireConnection;145;0;148;0
WireConnection;145;1;146;0
WireConnection;159;0;3;0
WireConnection;159;1;158;0
WireConnection;161;0;159;0
WireConnection;85;0;83;0
WireConnection;109;0;145;0
WireConnection;8;0;85;0
WireConnection;8;1;109;0
WireConnection;8;2;162;0
WireConnection;84;0;83;0
WireConnection;88;0;161;0
WireConnection;88;1;87;0
WireConnection;2;1;152;0
WireConnection;2;5;86;0
WireConnection;0;0;84;0
WireConnection;0;1;2;0
WireConnection;0;3;159;0
WireConnection;0;4;88;0
WireConnection;0;9;8;0
ASEEND*/
//CHKSM=C2BEC4EBA145DD18C3451BCD77E57DC89E6C1E15