Shader "KriptoFX/FPS_Pack/Distortion"
{
    Properties
    {
        [HDR] _TintColor ("Tint Color", Color) = (0, 0, 0, 1)
        _BaseTex ("Base (RGB) Gloss (A)", 2D) = "black" { }
        [HDR]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _MainTex ("Normalmap & CutOut", 2D) = "black" { }
        _BumpAmt ("Distortion", Float) = 1
        _InvFade ("Soft Particles Factor", Float) = 0.5
    }



    SubShader
    {


        Tags { "Queue" = "Transparent-10" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float2 texcoord : TEXCOORD0;
                half4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : POSITION;
                float2 uvMain : TEXCOORD1;
                half4 color : COLOR;
                float4 screenUV : TEXCOORD3;
                float fogFactor : TEXCOORD4;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _BaseTex;
            half4 _TintColor;
            half4 _MainColor;
            float _BumpAmt;

            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.screenUV = ComputeScreenPos(o.vertex);
                
                o.uvMain = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = v.color;

                //o.fogFactor = ComputeFogFactor(o.vertex.z);
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

                half3 bump = UnpackNormal(tex2D(_MainTex, i.uvMain));
                half alphaBump = saturate((0.94 - pow(bump.z, 127)) * 5);
                i.screenUV.xy += bump.rg * i.color.a * alphaBump * _BumpAmt;

                half3 grabColor = SampleSceneColor(i.screenUV.xy / i.screenUV.w);
                half4 result = _MainColor;
                result.rgb *= grabColor;

                result.a = saturate(result.a * alphaBump);
                if (result.a < 0.01) discard;

                //result.rgb = MixFog(result.rgb, i.fogFactor);
                return result;
            }
            ENDHLSL
        }
    }
}