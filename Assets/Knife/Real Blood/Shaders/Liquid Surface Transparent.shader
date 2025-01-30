// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Liquid/Surface Transparent"
{
	Properties
	{
		_TessValue( "Max Tessellation", Range( 1, 32 ) ) = 15
		_TessMin( "Tess Min Distance", Float ) = 10
		_TessMax( "Tess Max Distance", Float ) = 25
		_SpecularColor("SpecularColor", Color) = (0.3962264,0.3962264,0.3962264,1)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.9764706
		_Color("Color", Color) = (1,1,1,1)
		_FoamDepth("FoamDepth", Float) = 0
		_FoamBorderDepth("FoamBorderDepth", Float) = 0
		_Displacement("Displacement", Float) = 0.1
		_FoamColor("FoamColor", Color) = (0,0,0,0)
		_Foam("Foam", 2D) = "white" {}
		_Depth("Depth", Float) = 0
		_FoamAmplitude("FoamAmplitude", Float) = 0
		_FoamFrequency("FoamFrequency", Float) = 1
		_DepthColor("Depth Color", Color) = (0,0,0,0)
		_Distortion("Distortion", Float) = 0
		_Normal1("Normal 1", 2D) = "bump" {}
		_Normal2("Normal 2", 2D) = "bump" {}
		_FoamBorderColor("FoamBorderColor", Color) = (0,0,0,0)
		_Mask("Mask", 2D) = "black" {}
		_NormalSpeed2("Normal Speed 2", Vector) = (0,0,0,0)
		_NormalSpeed1("Normal Speed 1", Vector) = (0,0,0,0)
		_NormalOffset("NormalOffset", Float) = 0.5
		_NormalStrength("NormalStrength", Float) = 2
		_NormalScale2("Normal Scale 2", Float) = 1
		_NormalScale1("Normal Scale 1", Float) = 1
		[Toggle(_STATICNORMAL_ON)] _StaticNormal("StaticNormal", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha
		
		GrabPass{ }
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "Tessellation.cginc"
		#pragma target 4.6
		#pragma shader_feature _STATICNORMAL_ON
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows vertex:vertexDataFunc tessellate:tessFunction 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float _Displacement;
		uniform float _NormalOffset;
		uniform float _NormalStrength;
		uniform float _NormalScale1;
		uniform sampler2D _Normal1;
		uniform float2 _NormalSpeed1;
		uniform float4 _Normal1_ST;
		uniform float _NormalScale2;
		uniform sampler2D _Normal2;
		uniform float2 _NormalSpeed2;
		uniform float4 _Normal2_ST;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform float _Distortion;
		uniform float4 _Color;
		uniform float4 _DepthColor;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _Depth;
		uniform sampler2D _Foam;
		uniform float4 _Foam_ST;
		uniform float _FoamFrequency;
		uniform float _FoamAmplitude;
		uniform float4 _FoamColor;
		uniform float4 _FoamBorderColor;
		uniform float _FoamBorderDepth;
		uniform float _FoamDepth;
		uniform float4 _SpecularColor;
		uniform float _Smoothness;
		uniform float _TessValue;
		uniform float _TessMin;
		uniform float _TessMax;

		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			return UnityDistanceBasedTess( v0.vertex, v1.vertex, v2.vertex, _TessMin, _TessMax, _TessValue );
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float2 uv0_Mask = v.texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
			float maskValue14 = tex2Dlod( _Mask, float4( uv0_Mask, 0, 0.0) ).r;
			float3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( maskValue14 * _Displacement * ase_vertexNormal );
		}

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv0_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float2 temp_output_2_0_g3 = uv0_Mask;
			float2 break6_g3 = temp_output_2_0_g3;
			float temp_output_25_0_g3 = ( pow( _NormalOffset , 3.0 ) * 0.1 );
			float2 appendResult8_g3 = (float2(( break6_g3.x + temp_output_25_0_g3 ) , break6_g3.y));
			float4 tex2DNode14_g3 = tex2D( _Mask, temp_output_2_0_g3 );
			float temp_output_4_0_g3 = _NormalStrength;
			float3 appendResult13_g3 = (float3(1.0 , 0.0 , ( ( tex2D( _Mask, appendResult8_g3 ).g - tex2DNode14_g3.g ) * temp_output_4_0_g3 )));
			float2 appendResult9_g3 = (float2(break6_g3.x , ( break6_g3.y + temp_output_25_0_g3 )));
			float3 appendResult16_g3 = (float3(0.0 , 1.0 , ( ( tex2D( _Mask, appendResult9_g3 ).g - tex2DNode14_g3.g ) * temp_output_4_0_g3 )));
			float3 normalizeResult22_g3 = normalize( cross( appendResult13_g3 , appendResult16_g3 ) );
			float3 temp_output_8_0 = normalizeResult22_g3;
			float2 uv0_Normal1 = i.uv_texcoord * _Normal1_ST.xy + _Normal1_ST.zw;
			float2 panner110 = ( 1.0 * _Time.y * _NormalSpeed1 + uv0_Normal1);
			float2 uv0_Normal2 = i.uv_texcoord * _Normal2_ST.xy + _Normal2_ST.zw;
			float2 panner111 = ( 1.0 * _Time.y * _NormalSpeed2 + uv0_Normal2);
			float3 normalizeResult119 = normalize( ( temp_output_8_0 + UnpackScaleNormal( tex2D( _Normal1, panner110 ), _NormalScale1 ) + UnpackScaleNormal( tex2D( _Normal2, panner111 ), _NormalScale2 ) ) );
			#ifdef _STATICNORMAL_ON
				float3 staticSwitch120 = normalizeResult119;
			#else
				float3 staticSwitch120 = temp_output_8_0;
			#endif
			float3 normal98 = staticSwitch120;
			o.Normal = normal98;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 screenColor93 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( ase_screenPosNorm + float4( ( (normal98).xy * _Distortion ), 0.0 , 0.0 ) ).xy);
			float screenDepth76 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth76 = abs( ( screenDepth76 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Depth ) );
			float clampResult79 = clamp( distanceDepth76 , 0.0 , 1.0 );
			float alpha104 = clampResult79;
			float4 lerpResult21 = lerp( ( screenColor93 * _Color ) , _DepthColor , alpha104);
			float2 uv0_Foam = i.uv_texcoord * _Foam_ST.xy + _Foam_ST.zw;
			float4 foamBorder85 = _FoamBorderColor;
			float screenDepth81 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth81 = abs( ( screenDepth81 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _FoamBorderDepth ) );
			float clampResult82 = clamp( distanceDepth81 , 0.0 , 1.0 );
			float foamBorderBlend90 = ( 1.0 - clampResult82 );
			float4 lerpResult84 = lerp( ( tex2D( _Foam, ( uv0_Foam + ( ( sin( ( ( 0.1 * _FoamFrequency ) * _Time.y ) ) * _FoamAmplitude ) + pow( ( cos( ( _Time.y * ( 0.2 * _FoamFrequency ) ) ) * _FoamAmplitude ) , 2.0 ) ) ) ) * _FoamColor ) , foamBorder85 , foamBorderBlend90);
			float3 foamCol32 = (lerpResult84).rgb;
			float screenDepth78 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth78 = abs( ( screenDepth78 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _FoamDepth ) );
			float clampResult35 = clamp( distanceDepth78 , 0.0 , 1.0 );
			float foamAlpha33 = ( (lerpResult84).a * ( 1.0 - clampResult35 ) );
			float4 lerpResult28 = lerp( lerpResult21 , float4( foamCol32 , 0.0 ) , foamAlpha33);
			o.Albedo = lerpResult28.rgb;
			o.Specular = _SpecularColor.rgb;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17000
7;1;1844;1050;2083.707;-271.8684;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;44;-2770.306,1927.32;Float;False;Constant;_Float1;Float 1;14;0;Create;True;0;0;False;0;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-2721.306,2069.32;Float;False;Property;_FoamFrequency;FoamFrequency;18;0;Create;True;0;0;False;0;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;42;-2836.306,1754.32;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-2761.306,1543.32;Float;False;Constant;_Float0;Float 0;14;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-2413.306,1940.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;108;-2617.233,1050.688;Float;False;0;106;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;109;-2652.233,1322.688;Float;False;0;107;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;112;-2446.233,1204.688;Float;False;Property;_NormalSpeed1;Normal Speed 1;26;0;Create;True;0;0;False;0;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;113;-2439.233,1460.688;Float;False;Property;_NormalSpeed2;Normal Speed 2;25;0;Create;True;0;0;False;0;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;12;-2259.977,529.7603;Float;False;Property;_NormalOffset;NormalOffset;27;0;Create;True;0;0;False;0;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-2242.977,612.7603;Float;False;Property;_NormalStrength;NormalStrength;28;0;Create;True;0;0;False;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;111;-2196.233,1297.688;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;110;-2194.233,1046.688;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;115;-2107.707,1191.868;Float;False;Property;_NormalScale1;Normal Scale 1;30;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2144.707,1442.868;Float;False;Property;_NormalScale2;Normal Scale 2;29;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;9;-3198.695,234.6759;Float;True;Property;_Mask;Mask;24;0;Create;True;0;0;False;0;None;None;False;black;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;11;-3336.587,459.0391;Float;False;0;9;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-2484.306,1628.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-2118.306,1758.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-2145.306,1608.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;41;-1895.306,1683.32;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-1864.306,1879.32;Float;False;Property;_FoamAmplitude;FoamAmplitude;17;0;Create;True;0;0;False;0;0;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;106;-1763.868,988.0892;Float;True;Property;_Normal1;Normal 1;21;0;Create;True;0;0;False;0;None;dd2fd2df93418444c8e280f1d34deeb5;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;107;-1798.233,1212.688;Float;True;Property;_Normal2;Normal 2;22;0;Create;True;0;0;False;0;None;dd2fd2df93418444c8e280f1d34deeb5;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;8;-1964.977,458.7602;Float;False;NormalCreate;7;;3;e12f7ae19d416b942820e3932b56220f;0;4;1;SAMPLER2D;;False;2;FLOAT2;0,0;False;3;FLOAT;0.5;False;4;FLOAT;2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;117;-1359.707,871.8684;Float;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-1585.306,1731.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-1331.213,2309.253;Float;False;Property;_FoamBorderDepth;FoamBorderDepth;12;0;Create;True;0;0;False;0;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;40;-1892.306,1565.32;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;81;-1001.213,2390.253;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;48;-1062.306,1717.32;Float;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-1610.306,1556.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;119;-1224.707,892.8684;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;120;-1092.707,805.8684;Float;False;Property;_StaticNormal;StaticNormal;31;0;Create;True;0;0;False;0;0;0;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;82;-673.213,2360.253;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;39;-1169.306,1405.32;Float;False;0;25;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;47;-973.3062,1583.32;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;86;-678.3617,2125.862;Float;False;Property;_FoamBorderColor;FoamBorderColor;23;0;Create;True;0;0;False;0;0,0,0,0;0.4509804,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;49;-763.3062,1460.32;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;98;-957.1523,697.1266;Float;False;normal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;83;-451.213,2361.253;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;85;-350.9547,2189.469;Float;False;foamBorder;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;25;-505.8284,1315.825;Float;True;Property;_Foam;Foam;15;0;Create;True;0;0;False;0;None;d01457b88b1c5174ea4235d140b5fab8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;90;-275.3617,2394.862;Float;False;foamBorderBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;26;-531.8284,1590.825;Float;False;Property;_FoamColor;FoamColor;14;0;Create;True;0;0;False;0;0,0,0,0;0.4528298,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;23;-592,1997;Float;False;Property;_FoamDepth;FoamDepth;11;0;Create;True;0;0;False;0;0;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;100;-944.4144,-504.118;Float;False;98;normal;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-724.1243,-398.5839;Float;False;Property;_Distortion;Distortion;20;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;78;-245.1793,1799.187;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;101;-754.4144,-490.118;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;91;-111.5414,1592.498;Float;False;90;foamBorderBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;92;-73.54138,1507.498;Float;False;85;foamBorder;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-145.8284,1374.825;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;-512.4146,-501.118;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;35;64.68231,1815.565;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;84;122.426,1240.327;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;94;-945.699,-772.1902;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;77;-819.6508,427.3455;Float;False;Property;_Depth;Depth;16;0;Create;True;0;0;False;0;0;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;38;243.6938,1813.32;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-397.699,-691.1901;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DepthFade;76;-564.5419,343.2762;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;29;275.8042,1482.403;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;10;-2704.695,682.6759;Float;True;Property;_MaskSample;MaskSample;7;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;93;-211.6991,-814.1902;Float;False;Global;_GrabScreen0;Grab Screen 0;19;0;Create;True;0;0;False;0;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;597.6823,1519.565;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;30;272.4531,1369.756;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;79;-260.8854,348.9812;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;3;-519.562,-207.2758;Float;False;Property;_Color;Color;10;0;Create;True;0;0;False;0;1,1,1,1;0.524,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-2050.977,762.7603;Float;False;maskValue;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;33;956.6823,1452.565;Float;False;foamAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;-67.96904,-330.2366;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;19;-506.9027,59.15601;Float;False;Property;_DepthColor;Depth Color;19;0;Create;True;0;0;False;0;0,0,0,0;0.3499998,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;32;692.0823,1383.565;Float;False;foamCol;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;104;-52.51949,320.5693;Float;False;alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;15;-492,736.5;Float;False;14;maskValue;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;21;68.47446,-54.22373;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;36;344.2783,266.1312;Float;False;33;foamAlpha;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-460,909.5;Float;False;Property;_Displacement;Displacement;13;0;Create;True;0;0;False;0;0.1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;37;307.2783,117.1312;Float;False;32;foamCol;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalVertexDataNode;7;-481,1024.5;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;2;962.8353,101.4688;Float;False;Property;_SpecularColor;SpecularColor;6;0;Create;True;0;0;False;0;0.3962264,0.3962264,0.3962264,1;0.3779987,0.3779987,0.3779987,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;99;712.4519,48.6545;Float;False;98;normal;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1;927.3516,303.1879;Float;False;Property;_Smoothness;Smoothness;9;0;Create;True;0;0;False;0;0.9764706;0.9764706;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;105;-755.506,-268.1294;Float;False;104;alpha;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;28;495.5745,-21.52371;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-204,791.5;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1320,-135.8;Float;False;True;6;Float;ASEMaterialInspector;0;0;StandardSpecular;Knife/Liquid/Surface Transparent;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;1;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;True;0;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;54;0;44;0
WireConnection;54;1;53;0
WireConnection;111;0;109;0
WireConnection;111;2;113;0
WireConnection;110;0;108;0
WireConnection;110;2;112;0
WireConnection;57;0;43;0
WireConnection;57;1;53;0
WireConnection;45;0;42;0
WireConnection;45;1;54;0
WireConnection;46;0;57;0
WireConnection;46;1;42;0
WireConnection;41;0;45;0
WireConnection;106;1;110;0
WireConnection;106;5;115;0
WireConnection;107;1;111;0
WireConnection;107;5;116;0
WireConnection;8;1;9;0
WireConnection;8;2;11;0
WireConnection;8;3;12;0
WireConnection;8;4;13;0
WireConnection;117;0;8;0
WireConnection;117;1;106;0
WireConnection;117;2;107;0
WireConnection;51;0;41;0
WireConnection;51;1;52;0
WireConnection;40;0;46;0
WireConnection;81;0;80;0
WireConnection;48;0;51;0
WireConnection;50;0;40;0
WireConnection;50;1;52;0
WireConnection;119;0;117;0
WireConnection;120;1;8;0
WireConnection;120;0;119;0
WireConnection;82;0;81;0
WireConnection;47;0;50;0
WireConnection;47;1;48;0
WireConnection;49;0;39;0
WireConnection;49;1;47;0
WireConnection;98;0;120;0
WireConnection;83;0;82;0
WireConnection;85;0;86;0
WireConnection;25;1;49;0
WireConnection;90;0;83;0
WireConnection;78;0;23;0
WireConnection;101;0;100;0
WireConnection;27;0;25;0
WireConnection;27;1;26;0
WireConnection;102;0;101;0
WireConnection;102;1;97;0
WireConnection;35;0;78;0
WireConnection;84;0;27;0
WireConnection;84;1;92;0
WireConnection;84;2;91;0
WireConnection;38;0;35;0
WireConnection;96;0;94;0
WireConnection;96;1;102;0
WireConnection;76;0;77;0
WireConnection;29;0;84;0
WireConnection;10;0;9;0
WireConnection;10;1;11;0
WireConnection;93;0;96;0
WireConnection;34;0;29;0
WireConnection;34;1;38;0
WireConnection;30;0;84;0
WireConnection;79;0;76;0
WireConnection;14;0;10;1
WireConnection;33;0;34;0
WireConnection;103;0;93;0
WireConnection;103;1;3;0
WireConnection;32;0;30;0
WireConnection;104;0;79;0
WireConnection;21;0;103;0
WireConnection;21;1;19;0
WireConnection;21;2;104;0
WireConnection;28;0;21;0
WireConnection;28;1;37;0
WireConnection;28;2;36;0
WireConnection;6;0;15;0
WireConnection;6;1;5;0
WireConnection;6;2;7;0
WireConnection;0;0;28;0
WireConnection;0;1;99;0
WireConnection;0;3;2;0
WireConnection;0;4;1;0
WireConnection;0;11;6;0
ASEEND*/
//CHKSM=08DDFA51C3CABFAE23D7DF29BE350B70519827F7