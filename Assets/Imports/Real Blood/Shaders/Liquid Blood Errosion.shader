// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Liquid/Blood Errosion"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[NoScaleOffset][Normal]_Normal("Normal", 2D) = "bump" {}
		_Errosion("Errosion", Range( 0 , 1)) = 0
		_VelocityErrosion("VelocityErrosion", Range( 0 , 1)) = 0
		_Softness("Softness", Float) = 1
		_Smothness("Smothness", Range( 0 , 1)) = 0.9529412
		_NormalScale("NormalScale", Float) = 0
		_ReflectionMap("ReflectionMap", CUBE) = "black" {}
		_Specular("Specular", Range( 0 , 1)) = 0
		_Tint("Tint", Color) = (0,0,0,0)
		_SpecularNormalMul("SpecularNormalMul", Float) = 0
		_FadeDistance("FadeDistance", Float) = 0.02
		[Enum(UnityEngine.Rendering.CullMode)]_FaceCull("Face Cull", Range( 0 , 2)) = 2
		_SpecularColor("SpecularColor", Color) = (0.4528301,0.4528301,0.4528301,1)
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull [_FaceCull]
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
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			half ASEVFace : VFACE;
			float4 vertexColor : COLOR;
			float3 worldRefl;
			INTERNAL_DATA
			float4 screenPos;
			float4 uv_tex4coord;
			float4 uv2_tex4coord2;
		};

		uniform float _FaceCull;
		uniform float _NormalScale;
		uniform sampler2D _Normal;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float4 _Tint;
		uniform float _Specular;
		uniform samplerCUBE _ReflectionMap;
		uniform float _SpecularNormalMul;
		uniform float4 _SpecularColor;
		uniform float _Smothness;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _FadeDistance;
		uniform float _Errosion;
		uniform float _VelocityErrosion;
		uniform float _Softness;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv0_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 tex2DNode2 = UnpackScaleNormal( tex2D( _Normal, uv0_MainTex ), _NormalScale );
			float switchResult58 = (((i.ASEVFace>0)?(tex2DNode2.b):(-tex2DNode2.b)));
			float3 appendResult57 = (float3(tex2DNode2.r , tex2DNode2.g , switchResult58));
			o.Normal = appendResult57;
			float3 newWorldReflection16 = WorldReflectionVector( i , appendResult57 );
			float3 appendResult46 = (float3(( (newWorldReflection16).xy * _SpecularNormalMul ) , newWorldReflection16.z));
			float3 normalizeResult42 = normalize( appendResult46 );
			o.Albedo = ( ( i.vertexColor * _Tint ) + ( _Specular * texCUBE( _ReflectionMap, normalizeResult42 ) ) ).rgb;
			o.Specular = _SpecularColor.rgb;
			o.Smoothness = _Smothness;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth53 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth53 = abs( ( screenDepth53 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _FadeDistance ) );
			float clampResult51 = clamp( distanceDepth53 , 0.0 , 1.0 );
			float clampResult27 = clamp( ( -i.uv2_tex4coord2.y / i.uv_tex4coord.w ) , 0.0 , 1.0 );
			float FinalAlpha54 = ( clampResult51 * ( i.vertexColor.a * saturate( ( ( tex2D( _MainTex, uv0_MainTex ).r - ( _Errosion + i.uv_tex4coord.z + ( clampResult27 * _VelocityErrosion ) ) ) / _Softness ) ) ) );
			o.Alpha = FinalAlpha54;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardSpecular alpha:fade keepalpha fullforwardshadows nolightmap  

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
				float4 customPack2 : TEXCOORD2;
				float4 customPack3 : TEXCOORD3;
				float4 screenPos : TEXCOORD4;
				float4 tSpace0 : TEXCOORD5;
				float4 tSpace1 : TEXCOORD6;
				float4 tSpace2 : TEXCOORD7;
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
				o.customPack2.xyzw = customInputData.uv_tex4coord;
				o.customPack2.xyzw = v.texcoord;
				o.customPack3.xyzw = customInputData.uv2_tex4coord2;
				o.customPack3.xyzw = v.texcoord1;
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
				surfIN.uv_tex4coord = IN.customPack2.xyzw;
				surfIN.uv2_tex4coord2 = IN.customPack3.xyzw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldRefl = -worldViewDir;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				surfIN.screenPos = IN.screenPos;
				surfIN.vertexColor = IN.color;
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
7;7;1844;1044;987.8463;541.6884;1;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-2141.24,270.1099;Float;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-1591.864,871.0092;Float;False;Property;_NormalScale;NormalScale;7;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;-2607.527,727.0146;Float;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1310.603,739.8743;Float;True;Property;_Normal;Normal;1;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;6162beaabeb290f489de01b990e40cd8;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NegateNode;56;-974.8422,840.0896;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;23;-2520.879,477.983;Float;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NegateNode;31;-2332.548,796.236;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwitchByFaceNode;58;-832.8423,795.0895;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;28;-2117.367,773.5263;Float;False;2;0;FLOAT;0;False;1;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;27;-1938.476,729.3999;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-2004.128,917.1488;Float;False;Property;_VelocityErrosion;VelocityErrosion;3;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;57;-675.8423,727.0895;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-1633.128,846.1488;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1741.222,378.5788;Float;False;Property;_Errosion;Errosion;2;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector;16;-327.8095,1245.466;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;44;-117.264,1223.87;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-226.264,1447.87;Float;False;Property;_SpecularNormalMul;SpecularNormalMul;11;0;Create;True;0;0;False;0;0;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-1431.612,397.3494;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-1323.5,141;Float;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;None;9ccd0dba2f82e7846974ab3e300958d6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;105.736,1247.87;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-527.5,478;Float;False;Property;_Softness;Softness;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;5;-491.5,287;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;6;-316.5,347;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;298.05,-64.4738;Float;False;Property;_FadeDistance;FadeDistance;12;0;Create;True;0;0;False;0;0.02;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;46;235.736,1270.87;Float;False;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;42;427.736,1371.87;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;8;-174.5,379;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;53;545.05,-132.4738;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;9;-856.4006,-24.19998;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;81.69041,589.6661;Float;False;Property;_Specular;Specular;9;0;Create;True;0;0;False;0;0;0.08;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;124.6898,219.1661;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;51;716.05,-52.47379;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;20;-860.0095,-203.3339;Float;False;Property;_Tint;Tint;10;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;15;557.3904,1160.766;Float;True;Property;_ReflectionMap;ReflectionMap;8;0;Create;True;0;0;False;0;None;c6ba66ab77fde32449695eaf84e29471;True;0;False;black;LockedToCube;False;Object;-1;Auto;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-416.5,-45;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;905.05,48.52625;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;371.5905,471.3661;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-275.5,262;Float;False;Property;_Smothness;Smothness;6;0;Create;True;0;0;False;0;0.9529412;0.89;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-370.5,174;Float;False;Property;_Metallic;Metallic;5;1;[Gamma];Create;True;0;0;False;0;0;0.75;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;1064.685,289.8003;Float;False;Property;_FaceCull;Face Cull;13;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;2;2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;54;1022.536,155.2986;Float;False;FinalAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;17;241.5904,-216.3339;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;59;888.5712,-151.4629;Float;False;Property;_SpecularColor;SpecularColor;14;0;Create;True;0;0;False;0;0.4528301,0.4528301,0.4528301,1;0.133,0.133,0.133,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1322.3,-285.4;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;Knife/Liquid/Blood Errosion;False;False;False;False;False;False;True;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;True;55;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;2;1;38;0
WireConnection;2;5;14;0
WireConnection;56;0;2;3
WireConnection;31;0;26;2
WireConnection;58;0;2;3
WireConnection;58;1;56;0
WireConnection;28;0;31;0
WireConnection;28;1;23;4
WireConnection;27;0;28;0
WireConnection;57;0;2;1
WireConnection;57;1;2;2
WireConnection;57;2;58;0
WireConnection;32;0;27;0
WireConnection;32;1;33;0
WireConnection;16;0;57;0
WireConnection;44;0;16;0
WireConnection;24;0;4;0
WireConnection;24;1;23;3
WireConnection;24;2;32;0
WireConnection;1;1;38;0
WireConnection;45;0;44;0
WireConnection;45;1;40;0
WireConnection;5;0;1;1
WireConnection;5;1;24;0
WireConnection;6;0;5;0
WireConnection;6;1;7;0
WireConnection;46;0;45;0
WireConnection;46;2;16;3
WireConnection;42;0;46;0
WireConnection;8;0;6;0
WireConnection;53;0;48;0
WireConnection;25;0;9;4
WireConnection;25;1;8;0
WireConnection;51;0;53;0
WireConnection;15;1;42;0
WireConnection;10;0;9;0
WireConnection;10;1;20;0
WireConnection;50;0;51;0
WireConnection;50;1;25;0
WireConnection;19;0;18;0
WireConnection;19;1;15;0
WireConnection;54;0;50;0
WireConnection;17;0;10;0
WireConnection;17;1;19;0
WireConnection;0;0;17;0
WireConnection;0;1;57;0
WireConnection;0;3;59;0
WireConnection;0;4;12;0
WireConnection;0;9;54;0
ASEEND*/
//CHKSM=33AA3A386C31D9446D9015D6A4AE0E0093EA7B4B