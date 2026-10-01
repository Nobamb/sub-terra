Shader "SubTerra/MineRoundedCorners"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _MaskTex ("Light mask", 2D) = "white" {}
        _NormalMap ("Normal map", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CornerMap ("Exposed corners", 2D) = "black" {}
        _CornerRadius ("Corner radius in cells", Range(0,0.15)) = 0.065
        _UseLighting ("Use 2D lights", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; float2 cell : TEXCOORD4; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            TEXTURE2D(_CornerMap);
            SAMPLER(sampler_CornerMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _CornerBounds;
                float4x4 _WorldToCell;
                float _CornerRadius;
                float _UseLighting;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.cell = mul(_WorldToCell, float4(TransformObjectToWorld(input.positionOS), 1)).xy;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 index = floor(input.cell);
                float2 uv = (index - _CornerBounds.xy + 0.5) / _CornerBounds.zw;
                half4 corners = SAMPLE_TEXTURE2D(_CornerMap, sampler_CornerMap, uv);
                if (any(uv < 0) || any(uv > 1)) corners = 0;
                float2 p = frac(input.cell);
                float2 edge = min(p, 1 - p);
                half enabled = p.y < 0.5 ? (p.x < 0.5 ? corners.r : corners.g)
                    : (p.x < 0.5 ? corners.b : corners.a);
                float coverage = 1;
                if (enabled > 0.5 && all(edge < _CornerRadius))
                {
                    float distanceToArc = length(edge - _CornerRadius);
                    coverage = saturate((_CornerRadius - distanceToArc) / max(fwidth(distanceToArc), 0.0001) + 0.5);
                    clip(coverage - 0.001);
                }
                half4 color = _UseLighting > 0.5 ? CommonLitFragment(input, input.color)
                    : input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color.a *= coverage;
                return color;
            }
            ENDHLSL
        }
    }
}
