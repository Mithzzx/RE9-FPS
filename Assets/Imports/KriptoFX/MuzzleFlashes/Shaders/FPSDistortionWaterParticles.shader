// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "KriptoFX/FPS_Pack/WaterParticles"
{
    Properties
    {
        [HDR] _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture (R) CutOut (G)", 2D) = "white" { }
        _BumpMap ("Normalmap", 2D) = "bump" { }
        _BumpAmt ("Distortion", Float) = 10
    }

    Category
    {

        Tags { "Queue" = "Transparent-9" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off


        SubShader
        {
            
            Pass
            {


                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile_instancing
                #pragma multi_compile_fog

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float4 texcoord : TEXCOORD0;
                    half4 color : COLOR;
                    float texcoordBlend : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : POSITION;
                    float4 uvbump : TEXCOORD0;
                    half4 color : COLOR;
                    float4 screenUV : TEXCOORD2;
                    half blend : TEXCOORD3;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                sampler2D _BumpMap;

                float _BumpAmt;
                float _ColorStrength;
                float4 _GrabTexture_TexelSize;
                half4 _TintColor;

                float4 _BumpMap_ST;
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

                    half3 ambient = SampleSH(normalWS);

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

                    float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                    o.color.rgb *= saturate(GetLighting(worldPos, float3(0, 1, 0)));

                    o.uvbump.xy = v.texcoord.xy * _BumpMap_ST.xy + _BumpMap_ST.zw;
                    o.uvbump.zw = v.texcoord.zw * _BumpMap_ST.xy + _BumpMap_ST.zw;
                    o.blend = v.texcoordBlend;

                    return o;
                }

                float _InvFade;

                half4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(i);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
                    half4 bumpTex1 = tex2D(_BumpMap, i.uvbump.xy);
                    half4 bumpTex2 = tex2D(_BumpMap, i.uvbump.zw);
                    half3 bump = UnpackNormal(lerp(bumpTex1, bumpTex2, i.blend));
                    half alphaBump = saturate((0.94 - pow(bump.z, 127)) * 5);

                    if (alphaBump < 0.1) discard;

                    half4 tex = tex2D(_MainTex, i.uvbump.xy);
                    half4 tex2 = tex2D(_MainTex, i.uvbump.zw);
                    tex = lerp(tex, tex2, i.blend);

                    float2 offset = bump * _BumpAmt * i.color.a * alphaBump;
                    i.screenUV.xy = offset +i.screenUV.xy;

                    half3 grabColor = SampleSceneColor(i.screenUV.xy / i.screenUV.w);

                    half4 emission = float4(grabColor, 1) + tex.a * _TintColor * i.color * i.color.a;
                    emission.a = saturate(_TintColor.a * alphaBump);

                    return emission;
                }
                ENDHLSL
            }
        }
    }
}