// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "KriptoFX/FPS_Pack/Decal"
{
    Properties
    {
        [HDR]_TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" { }
    }

    Category
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend DstColor Zero
        
        ZWrite Off
        Cull Front
        ZTest Always

        SubShader
        {

            Pass
            {

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag


                #pragma multi_compile_fog
                //#pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _TintColor;

                struct appdata_t
                {
                    float4 vertex : POSITION;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float fogFactor : TEXCOORD0;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_TRANSFER_INSTANCE_ID(v, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = TransformObjectToHClip(v.vertex.xyz);
                    o.fogFactor = ComputeFogFactor(o.vertex.z);

                    return o;
                }


                half4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(i);

                    float2 UV = i.vertex.xy / _ScaledScreenParams.xy;
                    
                    #if UNITY_REVERSED_Z
                        float depth = SampleSceneDepth(UV);
                    #else
                        float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                    #endif

                    float3 wpos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                    float3 opos = mul(unity_WorldToObject, float4(wpos, 1)).xyz;

                    float3 stepVal = saturate((0.5 - abs(opos.xyz)) * 10000);
                    float projClipFade = stepVal.x * stepVal.y * stepVal.z * (1 - abs(opos.y * 2));
                    projClipFade = pow(abs(projClipFade), 0.2);

                    float2 uvMain = (opos.xz + 0.5);
                    half4 tex = tex2D(_MainTex, uvMain);
                    
                    half4 res = tex * _TintColor;
                    res.rgb *= 2;
                    res.a = saturate(res.a * projClipFade);


                    res = lerp(1, res, saturate(res.a * 2));  
                    res.rgb = MixFogColor(res.rgb, float3(1,1,1), i.fogFactor.x);

                    return res;
                }
                ENDHLSL
            }
        }
    }
}