// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Liquid/Blood Sphere Errosion"
{
	Properties
	{
		_TessValue( "Max Tessellation", Range( 1, 32 ) ) = 4
		_TessMin( "Tess Min Distance", Float ) = 2
		_TessMax( "Tess Max Distance", Float ) = 3
		_MainTex("MainTex", 2D) = "white" {}
		[NoScaleOffset][Normal]_Normal("Normal", 2D) = "bump" {}
		_Errosion("Errosion", Range( 0 , 1)) = 0
		_Scale("Scale", Range( 0 , 1)) = 0
		_Softness("Softness", Range( 0 , 1)) = 1
		_Smothness("Smothness", Range( 0 , 1)) = 0.9529412
		_NormalScale("NormalScale", Float) = 0
		_FadeDistance("FadeDistance", Float) = 0.02
		_ReflectionMap("ReflectionMap", CUBE) = "black" {}
		_Specular("Specular", Range( 0 , 1)) = 0
		_Tint("Tint", Color) = (0,0,0,0)
		_SpecularColor("SpecularColor", Color) = (0,0,0,0)
		[Toggle(_PARTICLESCALEAFFECT_ON)] _ParticleScaleAffect("ParticleScaleAffect", Float) = 0
		_SpecularNormalMul("SpecularNormalMul", Float) = 0
		_DisplacementNoise("DisplacementNoise", 2D) = "white" {}
		_MaxDisplacement("MaxDisplacement", Float) = 0.1
		_MaxScale("MaxScale", Float) = 1
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#include "Tessellation.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.6
		#pragma shader_feature _PARTICLESCALEAFFECT_ON
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
		};

		uniform float _MaxDisplacement;
		uniform sampler2D _DisplacementNoise;
		uniform float4 _DisplacementNoise_ST;
		uniform float _MaxScale;
		uniform float _Scale;
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
		uniform float _Softness;
		uniform float _TessValue;
		uniform float _TessMin;
		uniform float _TessMax;

		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			return UnityDistanceBasedTess( v0.vertex, v1.vertex, v2.vertex, _TessMin, _TessMax, _TessValue );
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float2 uv_DisplacementNoise = v.texcoord * _DisplacementNoise_ST.xy + _DisplacementNoise_ST.zw;
			float3 ase_vertexNormal = v.normal.xyz;
			float Scale69 = ( v.texcoord.w + _Scale );
			#ifdef _PARTICLESCALEAFFECT_ON
				float staticSwitch76 = v.texcoord1.xy.x;
			#else
				float staticSwitch76 = 1.0;
			#endif
			v.vertex.xyz += ( ( ( _MaxDisplacement * tex2Dlod( _DisplacementNoise, float4( uv_DisplacementNoise, 0, 0.0) ).r ) + _MaxScale ) * ase_vertexNormal * Scale69 * staticSwitch76 );
		}

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv0_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 tex2DNode2 = UnpackScaleNormal( tex2D( _Normal, uv0_MainTex ), _NormalScale );
			float switchResult70 = (((i.ASEVFace>0)?(tex2DNode2.b):(-tex2DNode2.b)));
			float3 appendResult71 = (float3(tex2DNode2.r , tex2DNode2.g , switchResult70));
			o.Normal = appendResult71;
			float3 newWorldReflection16 = WorldReflectionVector( i , appendResult71 );
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
			float temp_output_24_0 = ( _Errosion + i.uv_tex4coord.z );
			float FinalAlpha54 = ( clampResult51 * ( i.vertexColor.a * saturate( ( ( tex2D( _MainTex, uv0_MainTex ).r - temp_output_24_0 ) / _Softness ) ) ) );
			o.Alpha = FinalAlpha54;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardSpecular alpha:fade keepalpha fullforwardshadows nolightmap  vertex:vertexDataFunc tessellate:tessFunction 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.6
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
				float4 screenPos : TEXCOORD3;
				float4 tSpace0 : TEXCOORD4;
				float4 tSpace1 : TEXCOORD5;
				float4 tSpace2 : TEXCOORD6;
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
				vertexDataFunc( v );
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
7;7;1844;1044;-512.531;-816.7405;1;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-1867.268,96.96561;Float;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-1357.071,929.3242;Float;False;Property;_NormalScale;NormalScale;12;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1207.246,499.47;Float;True;Property;_Normal;Normal;6;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;fe70d782544d921468d272102ab61a6d;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NegateNode;73;-931.7946,738.5438;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwitchByFaceNode;70;-810.3787,617.7672;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;71;-598.7823,548.6122;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-2084.694,329.7405;Float;False;Property;_Errosion;Errosion;7;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector;16;-388.9095,742.3661;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;23;-2041.179,437.683;Float;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-1048.283,-120.5145;Float;True;Property;_MainTex;MainTex;5;0;Create;True;0;0;False;0;None;00cbdcd1118c7744683ca60b6cbfa64d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-1745.198,386.5111;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-287.364,944.7702;Float;False;Property;_SpecularNormalMul;SpecularNormalMul;19;0;Create;True;0;0;False;0;0;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;44;-178.364,720.7702;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;5;-658.2833,-0.5145569;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-778.9198,205.4213;Float;False;Property;_Softness;Softness;9;0;Create;True;0;0;False;0;1;0.15;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;44.63599,744.7702;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;48;258.8332,-162.5158;Float;False;Property;_FadeDistance;FadeDistance;13;0;Create;True;0;0;False;0;0.02;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;6;-483.2833,59.48547;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;46;174.636,767.7702;Float;False;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-2201.511,809.5727;Float;False;Property;_Scale;Scale;8;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;53;505.8331,-230.5158;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;9;-1023.184,-311.7146;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;8;-341.2833,91.48548;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;42;366.636,868.7702;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;59;911.2635,693.3096;Float;False;Property;_MaxDisplacement;MaxDisplacement;21;0;Create;True;0;0;False;0;0.1;0.04;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;58;808.5776,796.4774;Float;True;Property;_DisplacementNoise;DisplacementNoise;20;0;Create;True;0;0;False;0;None;97318301147cec541bd42658de1bbd7f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;68;-1813.512,719.5727;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;75;922.531,1448.74;Float;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-62.00797,90.96698;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;1186.264,642.3096;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;15;496.2905,657.666;Float;True;Property;_ReflectionMap;ReflectionMap;14;0;Create;True;0;0;False;0;None;c6ba66ab77fde32449695eaf84e29471;True;0;False;black;LockedToCube;False;Object;-1;Auto;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-48.30959,602.6661;Float;False;Property;_Specular;Specular;15;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;51;676.8332,-150.5158;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;69;-1641.512,762.5727;Float;False;Scale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;20;-1026.793,-490.8484;Float;False;Property;_Tint;Tint;16;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;77;934.531,1310.74;Float;False;Constant;_Float0;Float 0;19;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;63;1031.264,1032.309;Float;False;Property;_MaxScale;MaxScale;22;0;Create;True;0;0;False;0;1;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;66;1335.7,961.6089;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;56;1285.089,1169.926;Float;False;69;Scale;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;62;1451.264,824.3096;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-583.2833,-332.5146;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;241.5905,484.3661;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;865.8332,-49.51572;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;76;1262.531,1398.74;Float;False;Property;_ParticleScaleAffect;ParticleScaleAffect;18;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;54;983.3193,57.25665;Float;False;FinalAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;74;1180.702,-203.6042;Float;False;Property;_SpecularColor;SpecularColor;17;0;Create;True;0;0;False;0;0,0,0,0;0.4528298,0.4528298,0.4528298,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;12;-442.2833,-25.51455;Float;False;Property;_Smothness;Smothness;11;0;Create;True;0;0;False;0;0.9529412;0.923;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;17;202.3736,-314.3759;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;55;-1601.688,459.9107;Float;False;Errosion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;1602.264,885.3096;Float;False;4;4;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-537.2833,-113.5145;Float;False;Property;_Metallic;Metallic;10;1;[Gamma];Create;True;0;0;False;0;0;0.75;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1710.3,-367.4;Float;False;True;6;Float;ASEMaterialInspector;0;0;StandardSpecular;Knife/Liquid/Blood Sphere Errosion;False;False;False;False;False;False;True;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;True;0;4;2;3;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;0;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;2;1;38;0
WireConnection;2;5;14;0
WireConnection;73;0;2;3
WireConnection;70;0;2;3
WireConnection;70;1;73;0
WireConnection;71;0;2;1
WireConnection;71;1;2;2
WireConnection;71;2;70;0
WireConnection;16;0;71;0
WireConnection;1;1;38;0
WireConnection;24;0;4;0
WireConnection;24;1;23;3
WireConnection;44;0;16;0
WireConnection;5;0;1;1
WireConnection;5;1;24;0
WireConnection;45;0;44;0
WireConnection;45;1;40;0
WireConnection;6;0;5;0
WireConnection;6;1;7;0
WireConnection;46;0;45;0
WireConnection;46;2;16;3
WireConnection;53;0;48;0
WireConnection;8;0;6;0
WireConnection;42;0;46;0
WireConnection;68;0;23;4
WireConnection;68;1;67;0
WireConnection;25;0;9;4
WireConnection;25;1;8;0
WireConnection;60;0;59;0
WireConnection;60;1;58;1
WireConnection;15;1;42;0
WireConnection;51;0;53;0
WireConnection;69;0;68;0
WireConnection;62;0;60;0
WireConnection;62;1;63;0
WireConnection;10;0;9;0
WireConnection;10;1;20;0
WireConnection;19;0;18;0
WireConnection;19;1;15;0
WireConnection;50;0;51;0
WireConnection;50;1;25;0
WireConnection;76;1;77;0
WireConnection;76;0;75;1
WireConnection;54;0;50;0
WireConnection;17;0;10;0
WireConnection;17;1;19;0
WireConnection;55;0;24;0
WireConnection;65;0;62;0
WireConnection;65;1;66;0
WireConnection;65;2;56;0
WireConnection;65;3;76;0
WireConnection;0;0;17;0
WireConnection;0;1;71;0
WireConnection;0;3;74;0
WireConnection;0;4;12;0
WireConnection;0;9;54;0
WireConnection;0;11;65;0
ASEEND*/
//CHKSM=43D738BB5F8ABA12E2DCCB39E37E670628A6690A