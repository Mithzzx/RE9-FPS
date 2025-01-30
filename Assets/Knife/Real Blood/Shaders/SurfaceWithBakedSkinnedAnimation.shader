// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/SurfaceWithBakedSkinnedAnimation"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal (XYZW)", 2D) = "bump" {}
        _Specular ("Specular", 2D) = "black" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
		_AnimationPos("Baked Position Animation Texture", 2D) = "black" {}
		_AnimationNm("Baked Normal Animation Texture", 2D) = "black" {}
		_Speed("Animation Speed", float) = 60
		_Length("Animation Length", float) = 300
		_ManualFrame("Animation Frame", float) = 300
		[Toggle(MANUAL)] _UseManual("Manual", Float) = 0

		_NoiseTiling("Offset noise tiling", Vector) = (1,1,1,1)

		_SpeedNoiseTiling("Speed noise tiling", Vector) = (1,1,1,1)
		_SpeedMinMax("Speed min max", Vector) = (1,1,1,1)

		_ScaleNoiseTiling("Scale noise tiling", Vector) = (1,1,1,1)
		_ScaleMinMax("Scale min max", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows
		#pragma vertex vert
		#pragma multi_compile ___ MANUAL

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _LocalPosition)
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _InstancedColor)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
		
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _Specular;
        sampler2D _AnimationPos;
        sampler2D _AnimationNm;
		float4 _AnimationPos_TexelSize;

		float4 _NoiseTiling;
		
		float4 _SpeedNoiseTiling;
		float4 _SpeedMinMax;

		float4 _ScaleNoiseTiling;
		float4 _ScaleMinMax;

		float _Speed;
		float _Length;
		float _ManualFrame;
			
        struct Input
        {
            float2 uv_MainTex;
        };

		struct appdata 
		{
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
			uint id : SV_VertexID;
			UNITY_VERTEX_INPUT_INSTANCE_ID
        };
		
		float remap(float s, float a1, float a2, float b1, float b2)
		{
			return b1 + (s-a1)*(b2-b1)/(a2-a1);
		}

        void vert (inout appdata v)
        {
			float framesCount = _AnimationPos_TexelSize.z;
			float verticesCount = _AnimationPos_TexelSize.w;

			float3 randomOffset = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _LocalPosition).xyz);
			
#if MANUAL
			float _frame = _ManualFrame;
#else
			float3 speedOffset = (randomOffset * _SpeedNoiseTiling.xyz);

			float speedRandomOffset = (speedOffset.x + speedOffset.y + speedOffset.z) % (_SpeedMinMax.y - _SpeedMinMax.x);
			speedRandomOffset += _SpeedMinMax.x;

			float _frame = (_Speed * speedRandomOffset) * _Time.y;

			float3 frameOffset = (randomOffset * _NoiseTiling.xyz);
			_frame += frameOffset.x + frameOffset.y + frameOffset.z;

			_frame = _frame % _Length;
#endif
			
			float3 scaleOffset = randomOffset * _ScaleNoiseTiling.xyz;
			float scaleRandomOffset = (scaleOffset.x + scaleOffset.y + scaleOffset.z) % (_ScaleMinMax.y - _ScaleMinMax.x);
			scaleRandomOffset += _ScaleMinMax.x;

			float _vertexId = (float)v.id;

			float3 offset = tex2Dlod(_AnimationPos, float4(_frame / framesCount, (_vertexId + 0.5) / verticesCount, 0, 0));
			float3 normal = tex2Dlod(_AnimationNm, float4(_frame / framesCount, (_vertexId + 0.5) / verticesCount, 0, 0));
			
			v.vertex.xyz = offset * scaleRandomOffset;
			v.normal.xyz = normal;
        }

        half _Smoothness;
        fixed4 _Color;


        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * UNITY_ACCESS_INSTANCED_PROP(Props, _InstancedColor);
            fixed3 nm = UnpackNormal(tex2D (_NormalMap, IN.uv_MainTex));
            fixed4 ss = tex2D (_Specular, IN.uv_MainTex);

            o.Albedo = c.rgb;
			o.Normal = nm;
            o.Specular = ss.rgb;
            o.Smoothness = _Smoothness * ss.a;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
