Shader "SubTerra/MineBackdrop"
{
    Properties
    {
        _MainTex ("Background", 2D) = "black" {}
        _IronTex ("Iron", 2D) = "black" {}
        _LithiumTex ("Lithium", 2D) = "black" {}
        _LayerWeights ("Layer weights", Vector) = (1,0,0,0)
        _LithiumGlow ("Lithium glow", Float) = 0
        _LithiumPulse ("Lithium pulse", Float) = 0
        _CopperPulse ("Copper pulse", Float) = 0
        _IronPulse ("Iron pulse", Float) = 0
        _PulseSeed ("Pulse seed", Float) = 0
        _GlowClock ("Glow clock", Float) = 0
        _LithiumShimmer ("Lithium shimmer", Float) = 0
        _CopperShimmer ("Copper shimmer", Float) = 0
        _IronShimmer ("Iron shimmer", Float) = 0
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
                half _LithiumPulse;
                half _CopperPulse;
                half _IronPulse;
                half _PulseSeed;
                float _GlowClock;
                half _LithiumShimmer;
                half _CopperShimmer;
                half _IronShimmer;
            CBUFFER_END
            struct A { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            V Vert(A i) { V o; o.position=TransformObjectToHClip(i.position.xyz); o.uv=_UvRect.xy+i.uv*_UvRect.zw; return o; }
            half4 Frag(V i) : SV_Target
            {
                // 반짝마다 위치가 바뀌는 드문 얼룩 마스크: 한 시점에 몇 군데만 켜진다.
                float2 p = i.uv * float2(7.0, 4.5) + _PulseSeed;
                half spot = smoothstep(.35, .9, sin(p.x) * sin(p.y * 1.3 + 1.7));
                half3 copper = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
                half3 iron = SAMPLE_TEXTURE2D(_IronTex,sampler_IronTex,i.uv).rgb;
                half3 lithium = SAMPLE_TEXTURE2D(_LithiumTex,sampler_LithiumTex,i.uv).rgb;
                // 위치마다 위상이 달라 광물 픽셀이 제각각 천천히 숨 쉰다. 화면 전체가 한꺼번에 깜빡이지 않는다.
                half breath = .5 + .5 * sin(_GlowClock * 1.4 + i.uv.x * 11.0 + i.uv.y * 7.0);
                breath *= breath;
                half warm = saturate((copper.r - copper.b) * 2.5) * smoothstep(.4, .75, copper.r);
                half bright = smoothstep(.45, .75, min(iron.r, min(iron.g, iron.b)));
                half cyan = max(0, min(lithium.g, lithium.b) - lithium.r);
                half crystal = smoothstep(.7, .95, min(lithium.g, lithium.b)) * saturate((lithium.b - lithium.r) * 3.0);
                half3 glowCopper = half3(1, .55, .2) * warm * (_CopperPulse * spot + _CopperShimmer * breath);
                half3 glowIron = half3(.9, .88, .8) * bright * (_IronPulse * spot + _IronShimmer * breath);
                half3 glowLithium = half3(0, .85, 1) * cyan * (_LithiumGlow + _LithiumPulse * spot)
                    + half3(.35, 1, 1) * crystal * _LithiumShimmer * breath;
                half3 baseColor = (copper * _LayerWeights.x + iron * _LayerWeights.y + lithium * _LayerWeights.z) * _Tint.rgb;
                // 빛은 깊이 어두워짐을 절반만 받아, 어두운 깊은 층에서도 광물이 또렷하다.
                half3 glow = (glowCopper * _LayerWeights.x + glowIron * _LayerWeights.y + glowLithium * _LayerWeights.z)
                    * lerp(half3(1, 1, 1), _Tint.rgb, .5);
                return half4(baseColor + glow, 1);
            }
            ENDHLSL
        }
    }
}
