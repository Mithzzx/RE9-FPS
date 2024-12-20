// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "KriptoFX/FPS_Pack/WaterParticles" {
Properties {
       [HDR] _TintColor ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture (R) CutOut (G)", 2D) = "white" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
		_BumpAmt ("Distortion", Float) = 10
}

Category {

	Tags { "Queue"="Transparent+1"  "IgnoreProjector"="True"  "RenderType"="Transparent"   "LightMode" = "ForwardBase"}
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off
	ZWrite Off


	SubShader {
		GrabPass {
			"_GrabTexture"
 		}
		Pass {


CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#pragma multi_compile_particles
#define FORWARD_BASE_PASS

#include "UnityCG.cginc"
#include "Lighting.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float4 texcoord: TEXCOORD0;
	fixed4 color : COLOR;
	float texcoordBlend : TEXCOORD1;
UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
	float4 vertex : POSITION;
	float4 uvgrab : TEXCOORD0;
	float4 uvbump : TEXCOORD1;
	fixed4 color : COLOR;

	#ifdef SOFTPARTICLES_ON
		float4 projPos : TEXCOORD4;
	#endif
		fixed blend : TEXCOORD6;
		UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO

};

sampler2D _MainTex;
sampler2D _BumpMap;

float _BumpAmt;
float _ColorStrength;
UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture);
float4 _GrabTexture_TexelSize;
fixed4 _TintColor;

float4 _BumpMap_ST;
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

	return saturate(otherLights * 1.5 + _LightColor0.rgb);
}


v2f vert (appdata_t v)
{
	v2f o;
		UNITY_SETUP_INSTANCE_ID(v); //Insert
		UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
	o.vertex = UnityObjectToClipPos(v.vertex);

	#ifdef SOFTPARTICLES_ON
		o.projPos = ComputeScreenPos (o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
	#endif
	o.color = v.color;

	o.color.rgb *= ShadeTranslucentLights(v.vertex);

	o.uvgrab = ComputeGrabScreenPos (o.vertex);
	o.uvbump.xy = TRANSFORM_TEX(v.texcoord.xy, _BumpMap);
	o.uvbump.zw = TRANSFORM_TEX(v.texcoord.zw, _BumpMap);
	o.blend = v.texcoordBlend;

	return o;
}

sampler2D _CameraDepthTexture;
float _InvFade;

half4 frag( v2f i ) : SV_Target
{
		UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
	fixed4 bumpTex1 = tex2D(_BumpMap, i.uvbump.xy);
	fixed4 bumpTex2 = tex2D(_BumpMap, i.uvbump.zw);
	half3 bump = UnpackNormal(lerp(bumpTex1, bumpTex2, i.blend));
	half alphaBump = saturate((0.94 - pow(bump.z, 127)) * 5);

	if (alphaBump < 0.1) discard;


	fixed4 tex = tex2D(_MainTex, i.uvbump.xy);
	fixed4 tex2 = tex2D(_MainTex, i.uvbump.zw);
	tex = lerp(tex, tex2, i.blend);


	float2 offset = bump * _BumpAmt  * i.color.a * alphaBump;
	i.uvgrab.xy = offset  + i.uvgrab.xy;

	half4 grab = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, i.uvgrab.xy / i.uvgrab.w);


	//fixed4 cut = tex2D(_CutOut, i.uvcutout) * i.color;
	//fixed4 emission = col * i.color + tex.r * _ColorStrength * _TintColor * _LightColor0 * i.color * i.color.a;
	fixed4 emission = grab + tex.a * _TintColor * i.color * i.color.a;
    emission.a = _TintColor.a * alphaBump ;

	return saturate(emission);
}
ENDCG
		}
	}


}

}
