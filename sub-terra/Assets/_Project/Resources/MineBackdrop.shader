Shader "SubTerra/MineBackdrop"
{
    Properties
    {
        _MainTex ("Background", 2D) = "black" {}
        _IronTex ("Iron", 2D) = "black" {}
        _LithiumTex ("Lithium", 2D) = "black" {}
        _LayerWeights ("Layer weights", Vector) = (1,0,0,0)
        _LithiumGlow ("Lithium glow", Float) = 0
        _UvRect ("Viewport", Vector) = (0,0,1,1)
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_IronTex); SAMPLER(sampler_IronTex);
            TEXTURE2D(_LithiumTex); SAMPLER(sampler_LithiumTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _UvRect;
                half4 _Tint;
                half4 _LayerWeights;
                half _LithiumGlow;
            CBUFFER_END
            struct A { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            V Vert(A i) { V o; o.position=TransformObjectToHClip(i.position.xyz); o.uv=_UvRect.xy+i.uv*_UvRect.zw; return o; }
            half4 Frag(V i) : SV_Target
            {
                half3 copper = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
                half3 iron = SAMPLE_TEXTURE2D(_IronTex,sampler_IronTex,i.uv).rgb;
                half3 lithium = SAMPLE_TEXTURE2D(_LithiumTex,sampler_LithiumTex,i.uv).rgb;
                half cyan = max(0, min(lithium.g, lithium.b) - lithium.r);
                lithium += half3(0, .85, 1) * cyan * _LithiumGlow;
                return half4((copper * _LayerWeights.x + iron * _LayerWeights.y + lithium * _LayerWeights.z) * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
