Shader "Knife/ProjectionShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
 
        Pass
        {
            ZWrite Off
            Cull Off
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
           
            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc"
 
            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 targetUV : TEXCOORD1;
                float facingFactor : TEXCOORD2;
            };
 
            struct Output
            {
                float4 target1 : SV_Target;
            };
           
            sampler2D _Input1;
 
            float4x4 _ProjMat;
            float4 _HitNormal;
 
            v2f vert (
                float4 vertex : POSITION, // vertex position input
                float2 uv : TEXCOORD0, // texture coordinate input
                float4 normal : NORMAL, // normal input
                out float4 outpos : SV_POSITION // clip space position output
                )
            {
                v2f o;
                o.uv = float4(uv, 0, 0);
                float4 wpos = mul(unity_ObjectToWorld, float4(vertex.xyz, 1));
                o.targetUV = mul(_ProjMat, wpos);
                o.targetUV.xy /= o.targetUV.w;
                o.targetUV.xy = o.targetUV.xy * 0.5 + 0.5;
                outpos = mul(UNITY_MATRIX_VP, float4(uv-0.5, 0, 1));

				normal.xyz = normalize(normal.xyz);

                o.facingFactor = (dot(_HitNormal.xyz, mul(unity_ObjectToWorld, float4(normal.xyz, 0))) * 2 - 1);
                return o;
            }
           
            Output frag (v2f i)
            {
				Output output;

                output.target1 = 0;

                float4 brushPixel1 = tex2D(_Input1, i.targetUV);
                
                float normalBlend = i.facingFactor;

                if(normalBlend < -1 || i.targetUV.x < 0 || i.targetUV.y < 0 || i.targetUV.x > 1 || i.targetUV.y > 1)
                {
                    return output;
                }
				
				output.target1 = brushPixel1;
				//output.target1 = 1;

				return output;
            }
            ENDCG
        }
    }
}