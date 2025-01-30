// Upgrade NOTE: upgraded instancing buffer 'KnifeDistortion' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Knife/Distortion"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[NoScaleOffset]_NormalMap("NormalMap", 2D) = "bump" {}
		_Color("Color", Color) = (1,1,1,1)
		_NormalScale("NormalScale", Float) = 1
		_Distortion("Distortion", Range( 0 , 0.2)) = 0.1
		_Alpha("Alpha", Range( 0 , 1)) = 1
		_Frame("Frame", Int) = 0
		_Columns("Columns", Int) = 1
		_Rows("Rows", Int) = 1
		[Toggle(_SUBSTRACTALPHAFADING_ON)] _SubstractAlphaFading("SubstractAlphaFading", Float) = 0
		_DistrotionAlphaPower("DistrotionAlphaPower", Float) = 1
		_SpecularNormalMul("SpecularNormalMul", Float) = 0
		_ReflectionMap("ReflectionMap", CUBE) = "black" {}
		_Specular("Specular", Range( 0 , 1)) = 1
		_AlphaPower("AlphaPower", Float) = 0
	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		Cull Back
		ColorMask RGBA
		ZWrite Off
		ZTest Always
		
		
		GrabPass{ }

		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
			#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
			#else
			#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
			#endif


			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#pragma shader_feature _SUBSTRACTALPHAFADING_ON


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_tangent : TANGENT;
				float3 ase_normal : NORMAL;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
			};

			uniform float4 _Color;
			uniform sampler2D _MainTex;
			uniform int _Columns;
			uniform int _Rows;
			uniform float _Alpha;
			uniform float _AlphaPower;
			uniform float _Specular;
			uniform samplerCUBE _ReflectionMap;
			uniform float _NormalScale;
			uniform sampler2D _NormalMap;
			uniform float _SpecularNormalMul;
			ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
			uniform float _Distortion;
			uniform float _DistrotionAlphaPower;
			UNITY_INSTANCING_BUFFER_START(KnifeDistortion)
				UNITY_DEFINE_INSTANCED_PROP(int, _Frame)
#define _Frame_arr KnifeDistortion
			UNITY_INSTANCING_BUFFER_END(KnifeDistortion)
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 ase_worldTangent = UnityObjectToWorldDir(v.ase_tangent);
				o.ase_texcoord1.xyz = ase_worldTangent;
				float3 ase_worldNormal = UnityObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord2.xyz = ase_worldNormal;
				float ase_vertexTangentSign = v.ase_tangent.w * unity_WorldTransformParams.w;
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				o.ase_texcoord3.xyz = ase_worldBitangent;
				float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.ase_texcoord4.xyz = ase_worldPos;
				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;
				
				o.ase_texcoord.xyz = v.ase_texcoord.xyz;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.w = 0;
				o.ase_texcoord1.w = 0;
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord4.w = 0;
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
				float2 uv019 = i.ase_texcoord.xyz * float2( 1,1 ) + float2( 0,0 );
				int _Frame_Instance = UNITY_ACCESS_INSTANCED_PROP(_Frame_arr, _Frame);
				// *** BEGIN Flipbook UV Animation vars ***
				// Total tiles of Flipbook Texture
				float fbtotaltiles46 = (float)_Columns * (float)_Rows;
				// Offsets for cols and rows of Flipbook Texture
				float fbcolsoffset46 = 1.0f / (float)_Columns;
				float fbrowsoffset46 = 1.0f / (float)_Rows;
				// Speed of animation
				float fbspeed46 = _Time[ 1 ] * 0.0;
				// UV Tiling (col and row offset)
				float2 fbtiling46 = float2(fbcolsoffset46, fbrowsoffset46);
				// UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
				// Calculate current tile linear index
				float fbcurrenttileindex46 = round( fmod( fbspeed46 + (float)_Frame_Instance, fbtotaltiles46) );
				fbcurrenttileindex46 += ( fbcurrenttileindex46 < 0) ? fbtotaltiles46 : 0;
				// Obtain Offset X coordinate from current tile linear index
				float fblinearindextox46 = round ( fmod ( fbcurrenttileindex46, (float)_Columns ) );
				// Multiply Offset X by coloffset
				float fboffsetx46 = fblinearindextox46 * fbcolsoffset46;
				// Obtain Offset Y coordinate from current tile linear index
				float fblinearindextoy46 = round( fmod( ( fbcurrenttileindex46 - fblinearindextox46 ) / (float)_Columns, (float)_Rows ) );
				// Reverse Y to get tiles from Top to Bottom
				fblinearindextoy46 = (int)((float)_Rows-1) - fblinearindextoy46;
				// Multiply Offset Y by rowoffset
				float fboffsety46 = fblinearindextoy46 * fbrowsoffset46;
				// UV Offset
				float2 fboffset46 = float2(fboffsetx46, fboffsety46);
				// Flipbook UV
				half2 fbuv46 = uv019 * fbtiling46 + fboffset46;
				// *** END Flipbook UV Animation vars ***
				float4 temp_output_7_0 = ( _Color * tex2D( _MainTex, fbuv46 ) );
				float temp_output_8_0 = (temp_output_7_0).a;
				float clampResult55 = clamp( ( temp_output_8_0 - ( 1.0 - _Alpha ) ) , 0.0 , 1.0 );
				#ifdef _SUBSTRACTALPHAFADING_ON
				float staticSwitch53 = pow( clampResult55 , _AlphaPower );
				#else
				float staticSwitch53 = ( temp_output_8_0 * _Alpha );
				#endif
				clip( staticSwitch53 );
				float3 tex2DNode3 = UnpackScaleNormal( tex2D( _NormalMap, fbuv46 ), _NormalScale );
				float3 ase_worldTangent = i.ase_texcoord1.xyz;
				float3 ase_worldNormal = i.ase_texcoord2.xyz;
				float3 ase_worldBitangent = i.ase_texcoord3.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 ase_worldPos = i.ase_texcoord4.xyz;
				float3 ase_worldViewDir = UnityWorldSpaceViewDir(ase_worldPos);
				ase_worldViewDir = normalize(ase_worldViewDir);
				float3 worldRefl59 = reflect( -ase_worldViewDir, float3( dot( tanToWorld0, tex2DNode3 ), dot( tanToWorld1, tex2DNode3 ), dot( tanToWorld2, tex2DNode3 ) ) );
				float3 appendResult63 = (float3(( (worldRefl59).xy * _SpecularNormalMul ) , worldRefl59.z));
				float3 normalizeResult64 = normalize( appendResult63 );
				float4 screenPos = i.ase_texcoord5;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float4 screenColor9 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( ( (tex2DNode3).xy * _Distortion * pow( staticSwitch53 , _DistrotionAlphaPower ) ) + (ase_screenPosNorm).xy ));
				
				
				finalColor = ( ( ( temp_output_7_0 * staticSwitch53 ) + ( staticSwitch53 * _Specular * texCUBE( _ReflectionMap, normalizeResult64 ) ) ) + ( ( 1.0 - staticSwitch53 ) * screenColor9 ) );
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=17000
7;1;1844;1050;-707.1406;520.7149;1;True;False
Node;AmplifyShaderEditor.IntNode;49;-3558.161,222.7962;Float;False;Property;_Rows;Rows;8;0;Create;True;0;0;False;0;1;4;0;1;INT;0
Node;AmplifyShaderEditor.IntNode;48;-3555.161,109.7962;Float;False;Property;_Columns;Columns;7;0;Create;True;0;0;False;0;1;4;0;1;INT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-3574.229,-36.49412;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;47;-3546.161,375.7962;Float;False;InstancedProperty;_Frame;Frame;6;0;Create;True;0;0;False;0;0;0;0;1;INT;0
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;46;-3152.004,2.727998;Float;False;0;0;6;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;2;-2119.624,-812.2261;Float;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;None;5e511c12facd3474ebbd24d2b202ffa3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;4;-2066.624,-1021.226;Float;False;Property;_Color;Color;2;0;Create;True;0;0;False;0;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-1829.624,-867.2261;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1871.541,-343.353;Float;False;Property;_Alpha;Alpha;5;0;Create;True;0;0;False;0;1;0.963;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;8;-1627.554,-687.3397;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;56;-1602.881,-317.3805;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;54;-1377.881,-419.3805;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1371.311,752.9272;Float;False;Property;_NormalScale;NormalScale;3;0;Create;True;0;0;False;0;1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-1475.454,32.6517;Float;False;Property;_AlphaPower;AlphaPower;14;0;Create;True;0;0;False;0;0;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;55;-1284.11,-130.3975;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1042.311,638.9273;Float;True;Property;_NormalMap;NormalMap;1;1;[NoScaleOffset];Create;True;0;0;False;0;None;3bec9ecfa3d3f254981fbc5038211f52;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1340.843,-584.9871;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector;59;-764.1644,401.8087;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PowerNode;70;-1102.454,-49.3483;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;60;-546.1644,414.8088;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-617.1644,553.8087;Float;False;Property;_SpecularNormalMul;SpecularNormalMul;11;0;Create;True;0;0;False;0;0;4.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;53;-918.2336,-147.8945;Float;False;Property;_SubstractAlphaFading;SubstractAlphaFading;9;0;Create;True;0;0;False;0;0;0;1;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-283.1644,411.8088;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;58;-823.9517,278.1173;Float;False;Property;_DistrotionAlphaPower;DistrotionAlphaPower;10;0;Create;True;0;0;False;0;1;2.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClipNode;69;-411.8724,-375.7164;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-761.3112,809.9272;Float;False;Property;_Distortion;Distortion;4;0;Create;True;0;0;False;0;0.1;0.1;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;-148.1644,456.8087;Float;False;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;6;-766.3112,945.9272;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;57;-492.9517,208.1173;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;12;-715.3112,689.9272;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-336.4112,641.9272;Float;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;14;-516.3112,909.9272;Float;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NormalizeNode;64;-18.71783,457.4708;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;65;155.2822,391.4708;Float;True;Property;_ReflectionMap;ReflectionMap;12;0;Create;True;0;0;False;0;None;c6ba66ab77fde32449695eaf84e29471;True;0;False;black;LockedToCube;False;Object;-1;Auto;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;10;-213.3112,769.9272;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;67;119.1365,293.132;Float;False;Property;_Specular;Specular;13;0;Create;True;0;0;False;0;1;0.078;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;9;-48.31115,788.9272;Float;False;Global;_GrabScreen0;Grab Screen 0;4;0;Create;True;0;0;False;0;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;479.1365,266.132;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;17;1177.714,13.78046;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;908.7137,-217.2195;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;1406.714,69.78048;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;68;1064.136,-168.868;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;1524.714,-89.21949;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;77;2134.714,-117.7195;Float;False;True;2;Float;ASEMaterialInspector;0;1;Knife/Distortion;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;True;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;7;False;-1;True;False;0;False;-1;0;False;-1;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;46;0;19;0
WireConnection;46;1;48;0
WireConnection;46;2;49;0
WireConnection;46;4;47;0
WireConnection;2;1;46;0
WireConnection;7;0;4;0
WireConnection;7;1;2;0
WireConnection;8;0;7;0
WireConnection;56;0;21;0
WireConnection;54;0;8;0
WireConnection;54;1;56;0
WireConnection;55;0;54;0
WireConnection;3;1;46;0
WireConnection;3;5;5;0
WireConnection;20;0;8;0
WireConnection;20;1;21;0
WireConnection;59;0;3;0
WireConnection;70;0;55;0
WireConnection;70;1;71;0
WireConnection;60;0;59;0
WireConnection;53;1;20;0
WireConnection;53;0;70;0
WireConnection;61;0;60;0
WireConnection;61;1;62;0
WireConnection;69;0;53;0
WireConnection;69;1;53;0
WireConnection;63;0;61;0
WireConnection;63;2;59;3
WireConnection;57;0;69;0
WireConnection;57;1;58;0
WireConnection;12;0;3;0
WireConnection;13;0;12;0
WireConnection;13;1;11;0
WireConnection;13;2;57;0
WireConnection;14;0;6;0
WireConnection;64;0;63;0
WireConnection;65;1;64;0
WireConnection;10;0;13;0
WireConnection;10;1;14;0
WireConnection;9;0;10;0
WireConnection;66;0;69;0
WireConnection;66;1;67;0
WireConnection;66;2;65;0
WireConnection;17;0;69;0
WireConnection;16;0;7;0
WireConnection;16;1;69;0
WireConnection;18;0;17;0
WireConnection;18;1;9;0
WireConnection;68;0;16;0
WireConnection;68;1;66;0
WireConnection;15;0;68;0
WireConnection;15;1;18;0
WireConnection;77;0;15;0
ASEEND*/
//CHKSM=2667946D808D076EBDF39E67D62E7B0085405AEA