Shader "SubTerra/MineBackdrop"
{
    Properties
    {
        _MainTex ("Background", 2D) = "black" {}
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
            CBUFFER_START(UnityPerMaterial)
                float4 _UvRect;
                half4 _Tint;
            CBUFFER_END
            struct A { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            V Vert(A i) { V o; o.position=TransformObjectToHClip(i.position.xyz); o.uv=_UvRect.xy+i.uv*_UvRect.zw; return o; }
            half4 Frag(V i) : SV_Target { return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb * _Tint.rgb,1); }
            ENDHLSL
        }
    }
}
