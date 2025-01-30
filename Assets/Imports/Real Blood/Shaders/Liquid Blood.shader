// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Liquid/Blood"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[NoScaleOffset][Normal]_Normal("Normal", 2D) = "bump" {}
		[Gamma]_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smothness("Smothness", Range( 0 , 1)) = 0.9529412
		_NormalScale("NormalScale", Float) = 0
		_ReflectionMap("ReflectionMap", CUBE) = "black" {}
		_Specular("Specular", Range( 0 , 1)) = 0
		_Tint2("Tint 2", Color) = (0,0,0,0)
		_Tint("Tint", Color) = (0,0,0,0)
		_SpecularNormalMul("SpecularNormalMul", Float) = 0
		_AlphaRemapColorBlend("AlphaRemapColorBlend", Vector) = (0,1,0,1)
		_AlphaRemap("AlphaRemap", Vector) = (0,1,0,1)
		_FadeDistance("FadeDistance", Float) = 0.02
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 2
		_Alpha("Alpha", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Off
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float3 worldRefl;
			INTERNAL_DATA
			float4 screenPos;
		};

		uniform float _Cull;
		uniform float _NormalScale;
		uniform sampler2D _Normal;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float4 _Tint;
		uniform float4 _Tint2;
		uniform float4 _AlphaRemapColorBlend;
		uniform float _Specular;
		uniform samplerCUBE _ReflectionMap;
		uniform float _SpecularNormalMul;
		uniform float _Metallic;
		uniform float _Smothness;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _FadeDistance;
		uniform float4 _AlphaRemap;
		uniform float _Alpha;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv0_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 tex2DNode2 = UnpackScaleNormal( tex2D( _Normal, uv0_MainTex ), _NormalScale );
			o.Normal = tex2DNode2;
			float4 tex2DNode1 = tex2D( _MainTex, uv0_MainTex );
			float clampResult62 = clamp( (_AlphaRemapColorBlend.z + (tex2DNode1.r - _AlphaRemapColorBlend.x) * (_AlphaRemapColorBlend.w - _AlphaRemapColorBlend.z) / (_AlphaRemapColorBlend.y - _AlphaRemapColorBlend.x)) , 0.0 , 1.0 );
			float4 lerpResult55 = lerp( _Tint , _Tint2 , clampResult62);
			float4 temp_output_10_0 = ( i.vertexColor * lerpResult55 );
			float3 newWorldReflection16 = WorldReflectionVector( i , tex2DNode2 );
			float3 appendResult46 = (float3(( (newWorldReflection16).xy * _SpecularNormalMul ) , newWorldReflection16.z));
			float3 normalizeResult42 = normalize( appendResult46 );
			o.Albedo = ( temp_output_10_0 + ( _Specular * texCUBE( _ReflectionMap, normalizeResult42 ) ) ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smothness;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth53 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth53 = abs( ( screenDepth53 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _FadeDistance ) );
			float clampResult51 = clamp( distanceDepth53 , 0.0 , 1.0 );
			float clampResult60 = clamp( (_AlphaRemap.z + (tex2DNode1.r - _AlphaRemap.x) * (_AlphaRemap.w - _AlphaRemap.z) / (_AlphaRemap.y - _AlphaRemap.x)) , 0.0 , 1.0 );
			float FinalAlpha54 = ( clampResult51 * ( i.vertexColor.a * clampResult60 ) * (temp_output_10_0).a * _Alpha );
			o.Alpha = FinalAlpha54;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows nolightmap  

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
				float4 screenPos : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				half4 color : COLOR0;
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
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.screenPos = ComputeScreenPos( o.pos );
				o.color = v.color;
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
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldRefl = -worldViewDir;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				surfIN.screenPos = IN.screenPos;
				surfIN.vertexColor = IN.color;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
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
7;1;1844;1050;774.2118;617.8778;1.3;True;False
Node;AmplifyShaderEditor.RangedFloatNode;14;-986.1643,839.7092;Float;False;Property;_NormalScale;NormalScale;4;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-1699.24,296.1099;Float;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-704.9028,708.5743;Float;True;Property;_Normal;Normal;1;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;01d8b6aaad7a7a04f806d4da0f39a0f3;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldReflectionVector;16;-388.9095,742.3661;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;1;-1555.158,99.18211;Float;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;None;c263573e4c232f641a0107ba016cd37a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;63;-1341.611,345.0229;Float;False;Property;_AlphaRemapColorBlend;AlphaRemapColorBlend;10;0;Create;True;0;0;False;0;0,1,0,1;0,0.05,0,2.13;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;40;-287.364,944.7702;Float;False;Property;_SpecularNormalMul;SpecularNormalMul;9;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;44;-178.364,720.7702;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;61;-1111.03,203.7357;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;20;-871.3125,-607.9806;Float;False;Property;_Tint;Tint;8;0;Create;True;0;0;False;0;0,0,0,0;0.699,0,0,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;56;-934.3248,-307.1588;Float;False;Property;_Tint2;Tint 2;7;0;Create;True;0;0;False;0;0,0,0,0;0.28,0,0,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;62;-930.1828,235.384;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;44.63599,744.7702;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;59;-962.5824,402.6683;Float;False;Property;_AlphaRemap;AlphaRemap;11;0;Create;True;0;0;False;0;0,1,0,1;0,0.05,0,6.3;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;55;-556.8054,-358.0222;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;9;-856.4006,-24.19998;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;48;298.05,-64.4738;Float;False;Property;_FadeDistance;FadeDistance;12;0;Create;True;0;0;False;0;0.02;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;58;-732.0016,261.3811;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;46;174.636,767.7702;Float;False;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;42;366.636,868.7702;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-397.2849,-59.69388;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DepthFade;53;545.05,-132.4738;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;60;-551.1543,293.0294;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;67;-210.0119,-148.5778;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;37.80782,73.99613;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-48.30959,602.6661;Float;False;Property;_Specular;Specular;6;0;Create;True;0;0;False;0;0;0.039;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;51;716.05,-52.47379;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;15;496.2905,657.666;Float;True;Property;_ReflectionMap;ReflectionMap;5;0;Create;True;0;0;False;0;None;c6ba66ab77fde32449695eaf84e29471;True;0;False;black;LockedToCube;False;Object;-1;Auto;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;68;510.1882,206.3222;Float;False;Property;_Alpha;Alpha;14;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;905.05,48.52625;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;241.5905,484.3661;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;54;1022.536,155.2986;Float;False;FinalAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;17;241.5904,-216.3339;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-275.5,262;Float;False;Property;_Smothness;Smothness;3;0;Create;True;0;0;False;0;0.9529412;0.9529412;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;66;1126.646,327.2355;Float;False;Property;_Cull;Cull;13;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-370.5,174;Float;False;Property;_Metallic;Metallic;2;1;[Gamma];Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1483.933,-308.006;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Knife/Liquid/Blood;False;False;False;False;False;False;True;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;4;1;False;-1;1;False;-1;0;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;2;1;38;0
WireConnection;2;5;14;0
WireConnection;16;0;2;0
WireConnection;1;1;38;0
WireConnection;44;0;16;0
WireConnection;61;0;1;1
WireConnection;61;1;63;1
WireConnection;61;2;63;2
WireConnection;61;3;63;3
WireConnection;61;4;63;4
WireConnection;62;0;61;0
WireConnection;45;0;44;0
WireConnection;45;1;40;0
WireConnection;55;0;20;0
WireConnection;55;1;56;0
WireConnection;55;2;62;0
WireConnection;58;0;1;1
WireConnection;58;1;59;1
WireConnection;58;2;59;2
WireConnection;58;3;59;3
WireConnection;58;4;59;4
WireConnection;46;0;45;0
WireConnection;46;2;16;3
WireConnection;42;0;46;0
WireConnection;10;0;9;0
WireConnection;10;1;55;0
WireConnection;53;0;48;0
WireConnection;60;0;58;0
WireConnection;67;0;10;0
WireConnection;25;0;9;4
WireConnection;25;1;60;0
WireConnection;51;0;53;0
WireConnection;15;1;42;0
WireConnection;50;0;51;0
WireConnection;50;1;25;0
WireConnection;50;2;67;0
WireConnection;50;3;68;0
WireConnection;19;0;18;0
WireConnection;19;1;15;0
WireConnection;54;0;50;0
WireConnection;17;0;10;0
WireConnection;17;1;19;0
WireConnection;0;0;17;0
WireConnection;0;1;2;0
WireConnection;0;3;11;0
WireConnection;0;4;12;0
WireConnection;0;9;54;0
ASEEND*/
//CHKSM=2E522F4CA8B80D8F2D2FB546B376058DDC690C64