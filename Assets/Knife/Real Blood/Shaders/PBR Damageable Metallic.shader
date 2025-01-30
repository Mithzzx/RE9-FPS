// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/PBR Damageable Metallic"
{
	Properties
	{
		_TessValue( "Max Tessellation", Range( 1, 32 ) ) = 15
		_TessMin( "Tess Min Distance", Float ) = 10
		_TessMax( "Tess Max Distance", Float ) = 25
		_Cutoff( "Mask Clip Value", Float ) = 0.01
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_NormalScale("NormalScale", Float) = 1
		_AmbientOcclusion("Ambient Occlusion", 2D) = "white" {}
		_MetallicSmoothness("MetallicSmoothness", 2D) = "white" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Metallic("Metallic", Range( 0 , 1)) = 1
		_AmbientOcclusionAmount("AmbientOcclusion Amount", Range( 0 , 1)) = 1
		_ExplosionMask("ExplosionMask", 2D) = "white" {}
		_ExplosionBlend("ExplosionBlend", Range( -0.5 , 1)) = 0
		_ExplosionUniformScale("ExplosionUniformScale", Float) = 0
		_ExplosionSphereRadius("ExplosionSphereRadius", Float) = 0
		_ExplosionCenter("ExplosionCenter", Vector) = (0,0,0,0)
		_ExplosionUniformNoise("ExplosionUniformNoise", 2D) = "white" {}
		_MaskTiling("MaskTiling", Float) = 0
		_BlendStart("BlendStart", Float) = 0
		_BlendEnd("BlendEnd", Float) = 0
		_DamageMask("DamageMask", 2D) = "black" {}
		_DamageAlbedo("DamageAlbedo", 2D) = "white" {}
		_DamageNormalMap("DamageNormalMap", 2D) = "bump" {}
		_DamageNormalBlendPower("DamageNormalBlendPower", Float) = 0
		_DamageSpecBlendPower("DamageSpecBlendPower", Float) = 0
		_DamageAmbientOcclusion("DamageAmbientOcclusion", 2D) = "white" {}
		_DamageMetallicSmoothness("DamageMetallicSmoothness", 2D) = "white" {}
		_DamageColor("DamageColor", Color) = (1,1,1,1)
		_DamageBorderColor("DamageBorderColor", Color) = (1,1,1,1)
		_DamageBorderLength("DamageBorderLength", Range( 0 , 1)) = 0
		_DamageBorderPower("DamageBorderPower", Range( 0 , 5)) = 0
		_DamageMaskPower("DamageMaskPower", Range( 0 , 5)) = 0
		_DamageNormalScale("DamageNormalScale", Float) = 1
		_DamageSmoothness("DamageSmoothness", Range( 0 , 1)) = 1
		_DamageMetallic("DamageMetallic", Range( 0 , 1)) = 1
		_DamageAmbientOcclusionAmount("DamageAmbientOcclusion Amount", Range( 0 , 1)) = 1
		_DamageDisplacement("DamageDisplacement", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "Tessellation.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.6
		#define ASE_TEXTURE_PARAMS(textureName) textureName

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
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform float _DamageDisplacement;
		uniform sampler2D _DamageMask;
		uniform float4 _DamageMask_ST;
		uniform float _DamageMaskPower;
		uniform float _ExplosionSphereRadius;
		uniform float3 _ExplosionCenter;
		uniform sampler2D _ExplosionUniformNoise;
		uniform float4 _ExplosionUniformNoise_ST;
		uniform float _ExplosionUniformScale;
		uniform float _BlendStart;
		uniform float _BlendEnd;
		uniform float _ExplosionBlend;
		uniform float _NormalScale;
		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform float _DamageNormalScale;
		uniform sampler2D _DamageNormalMap;
		uniform float4 _DamageNormalMap_ST;
		uniform float _DamageNormalBlendPower;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float4 _DamageColor;
		uniform float4 _DamageBorderColor;
		uniform float _DamageBorderLength;
		uniform float _DamageBorderPower;
		uniform sampler2D _DamageAlbedo;
		uniform float4 _DamageAlbedo_ST;
		uniform sampler2D _MetallicSmoothness;
		uniform float4 _MetallicSmoothness_ST;
		uniform float _Metallic;
		uniform sampler2D _DamageMetallicSmoothness;
		uniform float4 _DamageMetallicSmoothness_ST;
		uniform float _DamageMetallic;
		uniform float _DamageSpecBlendPower;
		uniform float _Smoothness;
		uniform float _DamageSmoothness;
		uniform sampler2D _AmbientOcclusion;
		uniform float4 _AmbientOcclusion_ST;
		uniform float _AmbientOcclusionAmount;
		uniform sampler2D _DamageAmbientOcclusion;
		uniform float4 _DamageAmbientOcclusion_ST;
		uniform float _DamageAmbientOcclusionAmount;
		uniform sampler2D _ExplosionMask;
		uniform float _MaskTiling;
		uniform float _Cutoff = 0.01;
		uniform float _TessValue;
		uniform float _TessMin;
		uniform float _TessMax;


		inline float4 TriplanarSamplingSF( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.zy * float2( nsign.x, 1.0 ) ) );
			yNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.xz * float2( nsign.y, 1.0 ) ) );
			zNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.xy * float2( -nsign.z, 1.0 ) ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			return UnityDistanceBasedTess( v0.vertex, v1.vertex, v2.vertex, _TessMin, _TessMax, _TessValue );
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 ase_vertexNormal = v.normal.xyz;
			float3 damageVertexPosition127 = ( ase_vertex3Pos - ( ase_vertexNormal * _DamageDisplacement ) );
			float2 uv_DamageMask = v.texcoord * _DamageMask_ST.xy + _DamageMask_ST.zw;
			float damageBlend111 = pow( tex2Dlod( _DamageMask, float4( uv_DamageMask, 0, 0.0) ).r , _DamageMaskPower );
			float3 lerpResult129 = lerp( ase_vertex3Pos , damageVertexPosition127 , damageBlend111);
			float3 normalizeResult33 = normalize( ( ase_vertex3Pos + _ExplosionCenter ) );
			float2 uv_ExplosionUniformNoise = v.texcoord * _ExplosionUniformNoise_ST.xy + _ExplosionUniformNoise_ST.zw;
			float4 explosion_vertex37 = ( float4( ( ( _ExplosionSphereRadius * normalizeResult33 ) - _ExplosionCenter ) , 0.0 ) + ( tex2Dlod( _ExplosionUniformNoise, float4( uv_ExplosionUniformNoise, 0, 0.0) ) * _ExplosionUniformScale * float4( ase_vertexNormal , 0.0 ) ) );
			float clampResult96 = clamp( _ExplosionBlend , 0.0 , 1.0 );
			float smoothstepResult100 = smoothstep( _BlendStart , _BlendEnd , clampResult96);
			float blendParam41 = smoothstepResult100;
			float4 lerpResult46 = lerp( float4( lerpResult129 , 0.0 ) , explosion_vertex37 , blendParam41);
			v.vertex.xyz = lerpResult46.rgb;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			float3 normal10 = UnpackScaleNormal( tex2D( _NormalMap, uv_NormalMap ), _NormalScale );
			float2 uv_DamageNormalMap = i.uv_texcoord * _DamageNormalMap_ST.xy + _DamageNormalMap_ST.zw;
			float3 damageNormalMap118 = UnpackScaleNormal( tex2D( _DamageNormalMap, uv_DamageNormalMap ), _DamageNormalScale );
			float2 uv_DamageMask = i.uv_texcoord * _DamageMask_ST.xy + _DamageMask_ST.zw;
			float damageBlend111 = pow( tex2D( _DamageMask, uv_DamageMask ).r , _DamageMaskPower );
			float3 lerpResult134 = lerp( normal10 , damageNormalMap118 , pow( damageBlend111 , _DamageNormalBlendPower ));
			o.Normal = lerpResult134;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 color9 = ( _Color * tex2D( _MainTex, uv_MainTex ) );
			float smoothstepResult144 = smoothstep( 0.0 , _DamageBorderLength , damageBlend111);
			float clampResult157 = clamp( pow( ( smoothstepResult144 - damageBlend111 ) , _DamageBorderPower ) , 0.0 , 1.0 );
			float damageBorder150 = clampResult157;
			float4 lerpResult151 = lerp( _DamageColor , _DamageBorderColor , damageBorder150);
			float2 uv_DamageAlbedo = i.uv_texcoord * _DamageAlbedo_ST.xy + _DamageAlbedo_ST.zw;
			float4 damageAlbedo119 = ( lerpResult151 * tex2D( _DamageAlbedo, uv_DamageAlbedo ) );
			float4 lerpResult131 = lerp( color9 , damageAlbedo119 , damageBlend111);
			o.Albedo = lerpResult131.rgb;
			float2 uv_MetallicSmoothness = i.uv_texcoord * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
			float4 tex2DNode3 = tex2D( _MetallicSmoothness, uv_MetallicSmoothness );
			float metallic12 = ( tex2DNode3.r * _Metallic );
			float2 uv_DamageMetallicSmoothness = i.uv_texcoord * _DamageMetallicSmoothness_ST.xy + _DamageMetallicSmoothness_ST.zw;
			float4 tex2DNode106 = tex2D( _DamageMetallicSmoothness, uv_DamageMetallicSmoothness );
			float damageMetallic117 = ( tex2DNode106.r * _DamageMetallic );
			float lerpResult135 = lerp( metallic12 , damageMetallic117 , pow( damageBlend111 , _DamageSpecBlendPower ));
			o.Metallic = lerpResult135;
			float smoothness11 = ( tex2DNode3.a * _Smoothness );
			float damageSmoothness116 = ( tex2DNode106.a * _DamageSmoothness );
			float lerpResult136 = lerp( smoothness11 , damageSmoothness116 , pow( damageBlend111 , _DamageSpecBlendPower ));
			o.Smoothness = lerpResult136;
			float2 uv_AmbientOcclusion = i.uv_texcoord * _AmbientOcclusion_ST.xy + _AmbientOcclusion_ST.zw;
			float AO166 = ( tex2D( _AmbientOcclusion, uv_AmbientOcclusion ).r * _AmbientOcclusionAmount );
			float2 uv_DamageAmbientOcclusion = i.uv_texcoord * _DamageAmbientOcclusion_ST.xy + _DamageAmbientOcclusion_ST.zw;
			float damageAO171 = ( tex2D( _DamageAmbientOcclusion, uv_DamageAmbientOcclusion ).r * _DamageAmbientOcclusionAmount );
			float lerpResult175 = lerp( AO166 , damageAO171 , damageBlend111);
			o.Occlusion = lerpResult175;
			o.Alpha = 1;
			float2 appendResult99 = (float2(_MaskTiling , _MaskTiling));
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar97 = TriplanarSamplingSF( _ExplosionMask, ase_worldPos, ase_worldNormal, 1.0, appendResult99, 1.0, 0 );
			float clampResult75 = clamp( ( (-1.0 + (triplanar97.x - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) + ( _ExplosionBlend * 3.0 ) ) , 0.0 , 1.0 );
			float opacity89 = clampResult75;
			clip( ( 1.0 - opacity89 ) - _Cutoff );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows vertex:vertexDataFunc tessellate:tessFunction 

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
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
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
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
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
-1868;1;1839;1010;4886.443;-2952.099;1;True;False
Node;AmplifyShaderEditor.SamplerNode;103;-3526.038,1993.056;Float;True;Property;_DamageMask;DamageMask;24;0;Create;True;0;0;False;0;None;37e6f91f3efb0954cbdce254638862ea;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;109;-3395.577,2252.142;Float;False;Property;_DamageMaskPower;DamageMaskPower;35;0;Create;True;0;0;False;0;0;1.3;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;110;-3132.577,1953.142;Float;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;111;-2832.577,1963.142;Float;False;damageBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;145;-2660.078,2216.369;Float;False;Property;_DamageBorderLength;DamageBorderLength;33;0;Create;True;0;0;False;0;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;144;-2449.078,2065.369;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;149;-2212.078,1999.369;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;156;-2312.485,2264.385;Float;False;Property;_DamageBorderPower;DamageBorderPower;34;0;Create;True;0;0;False;0;0;0.04;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;155;-2018.485,2078.385;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;63;-2755.261,1449.445;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;98;-3492.209,585.4632;Float;False;Property;_MaskTiling;MaskTiling;21;0;Create;True;0;0;False;0;0;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;66;-2672.203,1656.075;Float;False;Property;_ExplosionCenter;ExplosionCenter;19;0;Create;True;0;0;False;0;0,0,0;0,0,-1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;99;-3293.209,614.4632;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;157;-1865.507,2060.567;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;67;-2392.515,1494.816;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-2395.064,1271.666;Float;False;Property;_ExplosionSphereRadius;ExplosionSphereRadius;18;0;Create;True;0;0;False;0;0;1.26;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;150;-1708.078,2020.369;Float;False;damageBorder;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;121;-3074.42,3538.641;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;123;-3120.42,3697.641;Float;False;Property;_DamageDisplacement;DamageDisplacement;40;0;Create;True;0;0;False;0;0;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;97;-2973.839,488.6118;Float;True;Spherical;World;False;ExplosionMask;_ExplosionMask;white;15;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;ExplosionMask;False;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalizeNode;33;-2229.408,1427.971;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-2924.363,771.8979;Float;False;Property;_ExplosionBlend;ExplosionBlend;16;0;Create;True;0;0;False;0;0;0;-0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;82;-1912.347,1521.078;Float;True;Property;_ExplosionUniformNoise;ExplosionUniformNoise;20;0;Create;True;0;0;False;0;None;97318301147cec541bd42658de1bbd7f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-2522.101,815.6252;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;125;-2938.2,3373.623;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-1809.065,1382.666;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalVertexDataNode;78;-1658.342,1789.686;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;152;-4427.902,2643.264;Float;False;150;damageBorder;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-1884.973,1728.114;Float;False;Property;_ExplosionUniformScale;ExplosionUniformScale;17;0;Create;True;0;0;False;0;0;0.17;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-2835.42,3567.641;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;153;-4605.902,2466.264;Float;False;Property;_DamageBorderColor;DamageBorderColor;32;0;Create;True;0;0;False;0;1,1,1,1;0.2729999,0.05308325,0.001516671,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;72;-2459.355,478.378;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;107;-4527.371,2327.448;Float;False;Property;_DamageColor;DamageColor;31;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;115;-4154.318,3396.86;Float;False;Property;_DamageSmoothness;DamageSmoothness;37;0;Create;True;0;0;False;0;1;0.83;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;164;-1233.294,571.2;Float;True;Property;_AmbientOcclusion;Ambient Occlusion;10;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;7;-1293.861,-472.1436;Float;False;Property;_Color;Color;6;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4;-1521.861,24.85632;Float;False;Property;_NormalScale;NormalScale;9;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;151;-4147.902,2494.264;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-1301.861,-255.1437;Float;True;Property;_MainTex;MainTex;7;0;Create;True;0;0;False;0;None;342898649d9cf0f439c769c7cda4a1e1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;101;-2618.694,1011.609;Float;False;Property;_BlendStart;BlendStart;22;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-2046.103,688.6254;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;102;-2553.694,1144.609;Float;False;Property;_BlendEnd;BlendEnd;23;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;168;-4088.787,3686.689;Float;False;Property;_DamageAmbientOcclusionAmount;DamageAmbientOcclusion Amount;39;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;96;-2312.17,889.3149;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;167;-4149.959,3469.225;Float;True;Property;_DamageAmbientOcclusion;DamageAmbientOcclusion;29;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;179;-4156.939,3338.362;Float;False;Property;_DamageMetallic;DamageMetallic;38;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;104;-4187.385,2729.555;Float;True;Property;_DamageAlbedo;DamageAlbedo;25;0;Create;True;0;0;False;0;None;65df218ee1e84e24b8e06771f2c3dd3c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-1266.861,183.8563;Float;True;Property;_MetallicSmoothness;MetallicSmoothness;11;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;77;-1562.089,1396.811;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-4436.319,3016.86;Float;False;Property;_DamageNormalScale;DamageNormalScale;36;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;106;-4193.49,3112.396;Float;True;Property;_DamageMetallicSmoothness;DamageMetallicSmoothness;30;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-1474.41,1663.066;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1195.861,467.8564;Float;False;Property;_Smoothness;Smoothness;12;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;163;-1031.294,767.2001;Float;False;Property;_AmbientOcclusionAmount;AmbientOcclusion Amount;14;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;177;-1234.427,391.658;Float;False;Property;_Metallic;Metallic;13;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;126;-2653.2,3466.623;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SmoothstepOpNode;100;-2082.694,1088.609;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;112;-3871.318,2673.86;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;178;-947.427,247.658;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;-3761.787,3563.689;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;75;-1833.103,773.6254;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;127;-2475.1,3496.881;Float;False;damageVertexPosition;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;113;-3805.318,3206.86;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-943.861,-219.1437;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-3867.939,3122.362;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;105;-4166.597,2913.052;Float;True;Property;_DamageNormalMap;DamageNormalMap;26;0;Create;True;0;0;False;0;None;913c5d7e195c9de42beef6123fdfab54;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;-848.7939,608.2001;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-1317.136,1487.132;Float;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;-1301.861,-34.14367;Float;True;Property;_NormalMap;NormalMap;8;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-841.861,322.8564;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;41;-1801.519,1058.666;Float;False;blendParam;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;159;-485.4855,895.9299;Float;False;Property;_DamageNormalBlendPower;DamageNormalBlendPower;27;0;Create;True;0;0;False;0;0;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;9;-768.6943,-232.5186;Float;False;color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;12;-685.6943,167.4814;Float;False;metallic;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;119;-3688.318,2735.86;Float;False;damageAlbedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;138;-322.5519,1088.812;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;62;-165.4176,1524.434;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;89;-1253.653,828.5303;Float;False;opacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;11;-679.6943,352.4814;Float;False;smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;171;-3610.08,3503.073;Float;False;damageAO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;37;-1040.328,1419.034;Float;False;explosion_vertex;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;10;-743.6943,4.481445;Float;False;normal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;139;-138.5519,1333.812;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;117;-3681.318,3046.86;Float;False;damageMetallic;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;130;-116.2374,1804.874;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;160;-486.8852,1340.983;Float;False;Property;_DamageSpecBlendPower;DamageSpecBlendPower;28;0;Create;True;0;0;False;0;0;10.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;166;-710.7939,618.2001;Float;False;AO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;137;-441.4519,741.0123;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;128;-245.2374,1691.874;Float;False;127;damageVertexPosition;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;118;-3810.318,2942.86;Float;False;damageNormalMap;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;116;-3610.318,3137.86;Float;False;damageSmoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-142.3389,977.459;Float;False;117;damageMetallic;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;83;529.372,1204.697;Float;False;89;opacity;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-130.3389,1266.459;Float;False;116;damageSmoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-257.0519,656.8123;Float;False;118;damageNormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;173;-90.38311,325.7801;Float;False;166;AO;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;162;25.71478,1048.033;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;158;-112.2855,767.03;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;161;163.3148,1341.083;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;61;164.2664,1870.182;Float;False;37;explosion_vertex;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;132;-130.7519,163.9123;Float;False;119;damageAlbedo;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;172;-82.96022,396.2134;Float;False;171;damageAO;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;129;176.7626,1638.874;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;174;-70.9602,487.2134;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;48;-138.1748,93.47894;Float;False;9;color;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;55;209.52,1961.264;Float;False;41;blendParam;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;58;-91.7016,1186.648;Float;False;11;smoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;50;-286.4625,575.578;Float;False;10;normal;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;56;-74.565,909.7781;Float;False;12;metallic;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;133;-118.7519,254.9123;Float;False;111;damageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;175;212.0398,397.2134;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;46;650.5566,1689.07;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;131;164.2481,164.9123;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;135;261.4481,947.8123;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;154;894.6096,1303.548;Float;False;150;damageBorder;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;134;35.9481,582.8123;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;84;764.072,1181.397;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;136;200.4481,1217.812;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1069.76,818.4084;Float;False;True;6;Float;ASEMaterialInspector;0;0;Standard;Knife/PBR Damageable Metallic;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.01;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;True;0;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;5;-1;-1;0;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;110;0;103;1
WireConnection;110;1;109;0
WireConnection;111;0;110;0
WireConnection;144;0;111;0
WireConnection;144;2;145;0
WireConnection;149;0;144;0
WireConnection;149;1;111;0
WireConnection;155;0;149;0
WireConnection;155;1;156;0
WireConnection;99;0;98;0
WireConnection;99;1;98;0
WireConnection;157;0;155;0
WireConnection;67;0;63;0
WireConnection;67;1;66;0
WireConnection;150;0;157;0
WireConnection;97;3;99;0
WireConnection;33;0;67;0
WireConnection;74;0;29;0
WireConnection;34;0;31;0
WireConnection;34;1;33;0
WireConnection;122;0;121;0
WireConnection;122;1;123;0
WireConnection;72;0;97;1
WireConnection;151;0;107;0
WireConnection;151;1;153;0
WireConnection;151;2;152;0
WireConnection;73;0;72;0
WireConnection;73;1;74;0
WireConnection;96;0;29;0
WireConnection;77;0;34;0
WireConnection;77;1;66;0
WireConnection;80;0;82;0
WireConnection;80;1;81;0
WireConnection;80;2;78;0
WireConnection;126;0;125;0
WireConnection;126;1;122;0
WireConnection;100;0;96;0
WireConnection;100;1;101;0
WireConnection;100;2;102;0
WireConnection;112;0;151;0
WireConnection;112;1;104;0
WireConnection;178;0;3;1
WireConnection;178;1;177;0
WireConnection;169;0;167;1
WireConnection;169;1;168;0
WireConnection;75;0;73;0
WireConnection;127;0;126;0
WireConnection;113;0;106;4
WireConnection;113;1;115;0
WireConnection;8;0;7;0
WireConnection;8;1;1;0
WireConnection;180;0;106;1
WireConnection;180;1;179;0
WireConnection;105;5;114;0
WireConnection;165;0;164;1
WireConnection;165;1;163;0
WireConnection;79;0;77;0
WireConnection;79;1;80;0
WireConnection;2;5;4;0
WireConnection;6;0;3;4
WireConnection;6;1;5;0
WireConnection;41;0;100;0
WireConnection;9;0;8;0
WireConnection;12;0;178;0
WireConnection;119;0;112;0
WireConnection;89;0;75;0
WireConnection;11;0;6;0
WireConnection;171;0;169;0
WireConnection;37;0;79;0
WireConnection;10;0;2;0
WireConnection;117;0;180;0
WireConnection;166;0;165;0
WireConnection;118;0;105;0
WireConnection;116;0;113;0
WireConnection;162;0;138;0
WireConnection;162;1;160;0
WireConnection;158;0;137;0
WireConnection;158;1;159;0
WireConnection;161;0;139;0
WireConnection;161;1;160;0
WireConnection;129;0;62;0
WireConnection;129;1;128;0
WireConnection;129;2;130;0
WireConnection;175;0;173;0
WireConnection;175;1;172;0
WireConnection;175;2;174;0
WireConnection;46;0;129;0
WireConnection;46;1;61;0
WireConnection;46;2;55;0
WireConnection;131;0;48;0
WireConnection;131;1;132;0
WireConnection;131;2;133;0
WireConnection;135;0;56;0
WireConnection;135;1;142;0
WireConnection;135;2;162;0
WireConnection;134;0;50;0
WireConnection;134;1;140;0
WireConnection;134;2;158;0
WireConnection;84;0;83;0
WireConnection;136;0;58;0
WireConnection;136;1;141;0
WireConnection;136;2;161;0
WireConnection;0;0;131;0
WireConnection;0;1;134;0
WireConnection;0;3;135;0
WireConnection;0;4;136;0
WireConnection;0;5;175;0
WireConnection;0;10;84;0
WireConnection;0;11;46;0
ASEEND*/
//CHKSM=B2EF01B9D708D3449774BB634D047FFC85AE4864