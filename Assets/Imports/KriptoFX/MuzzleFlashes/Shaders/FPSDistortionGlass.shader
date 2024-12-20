// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "KriptoFX/FPS_Pack/Glass" {
Properties {
        [HDR]_TintColor ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "black" {}
        _DuDvMap ("DuDv Map", 2D) = "black" {}
		_BumpAmt ("Distortion", Float) = 10
}

SubShader{
			GrabPass{
			"_GrabTexture"
			}

			Tags{ "Queue" = "Transparent+2" "IgnoreProjector" = "True" "RenderType" = "Transparent"  "LightMode" = "ForwardBase"}
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			ZWrite On

			Pass{


CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#define FORWARD_BASE_PASS
#pragma multi_compile _ VERTEXLIGHT_ON

#include "UnityCG.cginc"
#include "Lighting.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float2 texcoord: TEXCOORD0;
	float4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
	float4 vertex : POSITION;
	float4 uvgrab : TEXCOORD0;
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
UNITY_DECLARE_SCREENSPACE_TEXTURE (_GrabTexture);


float4 _TintColor;

float4 _DuDvMap_ST;
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

v2f vert (appdata_t v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v); //Insert
	UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
	o.vertex = UnityObjectToClipPos(v.vertex);


	o.color = v.color;
	o.color.rgb *= ShadeTranslucentLights(v.vertex);

	o.uvgrab = ComputeGrabScreenPos(o.vertex);
	o.uvbump = TRANSFORM_TEX( v.texcoord, _DuDvMap );
	o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );

	return o;
}

half4 frag( v2f i ) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
	half3 bump = UnpackNormal(tex2D(_DuDvMap, i.uvbump));
	half alphaBump = saturate((0.94 - pow(bump.z, 127)) * 5);
	i.uvgrab.xy = bump.rg * i.color.a * alphaBump * _BumpAmt + i.uvgrab.xy;

	half4 grab = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, i.uvgrab.xy / i.uvgrab.w);
	fixed4 tex = tex2D(_MainTex, i.uvmain) * i.color;

	fixed4 res = grab + tex * _TintColor * i.color.a;
    res.a = saturate(res.a);
	return res;
}
ENDCG
		}
	}

	FallBack "Diffuse"



}

