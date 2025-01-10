// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "KriptoFX/FPS_Pack/Glass" {
Properties {
        [HDR]_TintColor ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "black" {}
        _DuDvMap ("DuDv Map", 2D) = "black" {}
		_BumpAmt ("Distortion", Float) = 10
}

SubShader{
			

			Tags{ "Queue" = "Transparent-9" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			ZWrite On

Pass{

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord: TEXCOORD0;
				float4 color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : POSITION;
				float4 screenUV : TEXCOORD3;
				float2 uvbump : TEXCOORD1;
				float2 uvmain : TEXCOORD2;
				float4 color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _DuDvMap;

			float _BumpAmt;
			float _ColorStrength;

			float4 _TintColor;

			float4 _DuDvMap_ST;
			float4 _MainTex_ST;

			half3 GetLighting(float3 positionWS, half3 normalWS)
			{
				Light mainLight = GetMainLight();
				float atten = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
				half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * atten);

				half3 vertexLightColor = half3(0.0, 0.0, 0.0);

				uint lightsCount = GetAdditionalLightsCount();
				for (uint lightIndex = 0u; lightIndex < lightsCount; ++lightIndex)
				{
					Light light = GetAdditionalLight(lightIndex, positionWS);
					half3 lightColor = light.color * light.distanceAttenuation;
					vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
				}

				half3 ambient =  SampleSH(normalWS);

				return attenuatedLightColor + vertexLightColor + ambient;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.screenUV = ComputeScreenPos(o.vertex);
				o.color = v.color;

				float3 worldPos = TransformObjectToWorld(v.vertex);
				o.color.rgb *= saturate(GetLighting(worldPos, float3(0, 1, 0)));

				o.uvbump = v.texcoord * _DuDvMap_ST.xy + _DuDvMap_ST.zw;
				o.uvmain = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;

				return o;
			}

			half4 frag( v2f i ): SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				half3 bump = UnpackNormal(tex2D(_DuDvMap, i.uvbump));
				half alphaBump = saturate((0.94 - pow(bump.z, 127)) * 5);
				i.screenUV.xy = bump.rg * i.color.a * alphaBump * _BumpAmt + i.screenUV.xy;

				half3 grabColor = SampleSceneColor(i.screenUV.xy / i.screenUV.w);
				half4 tex = tex2D(_MainTex, i.uvmain);
				half4 result = 1;
				result.rgb = grabColor + tex.xyz * _TintColor.xyz * i.color.xyz * i.color.a;
				
				return result;
			}
			ENDHLSL
		}
	}

}

