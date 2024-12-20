Shader "KriptoFX/FPS_Pack/AlphaBlendedAnim" {
	Properties{
		[HDR]_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Particle Texture", 2D) = "white" {}
		_InvFade("Soft Particles Factor", Range(0.01,5)) = 1.0
	}

		Category{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "LightMode" = "ForwardBase"}
		Blend SrcAlpha OneMinusSrcAlpha

		Cull Off
		ZWrite Off

		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#pragma multi_compile_particles
#pragma multi_compile_fog
#define FORWARD_BASE_PASS
//#pragma multi_compile _ VERTEXLIGHT_ON

#include "UnityCG.cginc"
#include "Lighting.cginc"



		sampler2D _MainTex;
	fixed4 _TintColor;

	struct appdata_t {
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float3 normal : NORMAL;
		float4 texcoords : TEXCOORD0;
		float texcoordBlend : TEXCOORD1;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
		float2 texcoord2 : TEXCOORD1;
		fixed blend : TEXCOORD2;
		UNITY_FOG_COORDS(3)
#ifdef SOFTPARTICLES_ON
			float4 projPos : TEXCOORD4;
#endif
		UNITY_VERTEX_OUTPUT_STEREO
	};

	float4 _MainTex_ST;

	float3 ShadePointLights (
    float4 lightPosX, float4 lightPosY, float4 lightPosZ,
    float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
    float4 lightAttenSq,
    float3 pos)
{
    // to light vectors
    float4 toLightX = lightPosX - pos.x;
    float4 toLightY = lightPosY - pos.y;
    float4 toLightZ = lightPosZ - pos.z;
    // squared lengths
    float4 lengthSq = 0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;
    // don't produce NaNs if some vertex position overlaps with the light
    lengthSq = max(lengthSq, 0.000001);

    // attenuation
    float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
    float4 diff = 1 * atten;
    // final color
    float3 col = 0;
    col += lightColor0 * diff.x;
    col += lightColor1 * diff.y;
    col += lightColor2 * diff.z;
    col += lightColor3 * diff.w;
    return col;
}


	half3 ShadeTranslucentLights(float4 vertex)
	{
		float3 normal = float3(0, 1, 0);
		half3 otherLights = ShadeSH9(float4(normal, 1.0));
		
//#ifdef VERTEXLIGHT_ON
		float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
		otherLights += ShadePointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, worldPos);
		
//#endif

		return saturate(otherLights + _LightColor0.rgb);
	}

	v2f vert(appdata_t v)
	{
		v2f o;
	UNITY_SETUP_INSTANCE_ID(v); //Insert
		UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
		o.vertex = UnityObjectToClipPos(v.vertex);
#ifdef SOFTPARTICLES_ON
		o.projPos = ComputeScreenPos(o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif
		o.color = v.color * _TintColor;
		o.color.rgb *= ShadeTranslucentLights(v.vertex);

		o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
		o.texcoord2 = TRANSFORM_TEX(v.texcoords.zw, _MainTex);
		o.blend = v.texcoordBlend;
		UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}


	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
	float _InvFade;

	fixed4 frag(v2f i) : SV_Target
	{
		//return float4(i.color.xyz, 1);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
#ifdef SOFTPARTICLES_ON
		float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.projPos.xy / i.projPos.w));
		float partZ = i.projPos.z;
		float fade = saturate(_InvFade * (sceneZ - partZ));
		fade = _InvFade < 0.02f ? 1 : fade;
		i.color.a *= fade;
	#endif

		half4 colA = tex2D(_MainTex, i.texcoord);
		half4 colB = tex2D(_MainTex, i.texcoord2);
		half4 col = i.color * lerp(colA, colB, i.blend);
		UNITY_APPLY_FOG(i.fogCoord, col);
		col.a = saturate(col.a * 2);
		return col;
	}
		ENDCG
	}
	}
		}
}
