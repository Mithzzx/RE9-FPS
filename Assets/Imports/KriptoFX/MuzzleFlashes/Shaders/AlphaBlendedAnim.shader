Shader "KriptoFX/FPS_Pack/AlphaBlendedAnim"
{
    Properties
    {
        [HDR] _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _MainTex ("Particle Texture", 2D) = "white" { }
        _InvFade ("Soft Particles Factor", Range(0.01, 5)) = 1.0
    }

    Category
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Cull Off
        ZWrite Off

        SubShader
        {
            Pass
            {
                Name "Forward"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"


                sampler2D _MainTex;
                half4 _TintColor;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    half4 color : COLOR;
                    float3 normal : NORMAL;
                    float4 texcoords : TEXCOORD0;
                    float texcoordBlend : TEXCOORD1;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    half4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float2 texcoord2 : TEXCOORD1;
                    half blend : TEXCOORD2;
                    float4 screenUV : TEXCOORD3;
                    float fogFactor : TEXCOORD4;
                    
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

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

                    o.texcoord = v.texcoords.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                    o.texcoord2 = v.texcoords.zw * _MainTex_ST.xy + _MainTex_ST.zw;
                    o.blend = v.texcoordBlend;

                    
                    float3 worldPos = TransformObjectToWorld(v.vertex);
                    o.color.rgb *= saturate(GetLighting(worldPos, float3(0, 1, 0)));


                    o.fogFactor = ComputeFogFactor(o.vertex.z);
                    return o;
                }

                float _InvFade;

                half4 frag(v2f i) : SV_Target
                {
                    
                    UNITY_SETUP_INSTANCE_ID(i);
                    
                    float depthZ = LinearEyeDepth(SampleSceneDepth(i.screenUV.xy / i.screenUV.w), _ZBufferParams);
                    float thisZ = LinearEyeDepth(i.screenUV.z / i.screenUV.w, _ZBufferParams);
                    float fade = saturate(_InvFade * (depthZ - thisZ));
                    fade = _InvFade < 0.02 ?   1 : fade;
                    i.color.a *= fade;
                    

                    half4 colA = tex2D(_MainTex, i.texcoord);
                    half4 colB = tex2D(_MainTex, i.texcoord2);
                    half4 col = _TintColor * i.color * lerp(colA, colB, i.blend);
                    col.rgb = MixFog(col.rgb, i.fogFactor);
                    
                    col.a = saturate(col.a * 2);
                    
                    return col;
                }
                ENDHLSL
            }
        }
    }
}