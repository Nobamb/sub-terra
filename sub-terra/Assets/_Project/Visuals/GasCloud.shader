Shader "SubTerra/GasCloud"
{
    // 유독가스 구름. 스프라이트 UV(0~1)로 반지름 안쪽 감쇠를 계산하고, 월드 좌표 노이즈 2장을 서로 다른 속도로 흘린다.
    // 구름은 항상 반지름 1(= 판정 반지름) 안쪽에서만 보인다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseA ("Noise A", 2D) = "gray" {}
        _NoiseB ("Noise B", 2D) = "gray" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _NoiseScaleA ("Noise Scale A", Float) = 0.18
        _NoiseScaleB ("Noise Scale B", Float) = 0.11
        _ScrollA ("Scroll A (xy per second)", Vector) = (0.02, 0.006, 0, 0)
        _ScrollB ("Scroll B (xy per second)", Vector) = (-0.012, 0.004, 0, 0)
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.55
        _EdgeWarp ("Edge Warp", Range(0, 0.5)) = 0.16
        _Core ("Core", Range(0, 0.95)) = 0.2
        _FloorBias ("Floor Bias", Range(0, 1)) = 0.65
        _TopDensity ("Top Density", Range(0, 1)) = 0.3
        [PerRendererData] _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
            };

            sampler2D _NoiseA;
            sampler2D _NoiseB;
            float4 _Color;
            float _NoiseScaleA;
            float _NoiseScaleB;
            float4 _ScrollA;
            float4 _ScrollB;
            float _NoiseAmount;
            float _EdgeWarp;
            float _Core;
            float _FloorBias;
            float _TopDensity;
            float _Seed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float rGeo = length(p);

                float2 wa = i.world * _NoiseScaleA + _ScrollA.xy * _Time.y + _Seed;
                float2 wb = i.world * _NoiseScaleB + _ScrollB.xy * _Time.y + _Seed * 1.7;
                float n = tex2D(_NoiseA, wa).r * 0.55 + tex2D(_NoiseB, wb).r * 0.45;

                // 밀도 중심을 바닥 쪽으로 조금 내리고, 노이즈로 가장자리를 일렁이게 한다.
                float2 q = float2(p.x, p.y + 0.25 * _FloorBias);
                float r = length(q) * (1.0 + (n - 0.5) * _EdgeWarp * 2.0);
                float radial = 1.0 - smoothstep(_Core, 1.0, r);

                float heavy = 1.0 - smoothstep(0.25, 1.0, saturate(i.uv.y));
                float floorWeight = lerp(1.0, lerp(_TopDensity, 1.0, heavy), _FloorBias);
                float wisps = lerp(1.0 - _NoiseAmount, 1.0, n);

                // 판정 반지름(uv 반지름 1) 밖은 절대 보이지 않는다.
                float inside = 1.0 - smoothstep(0.88, 1.0, rGeo);
                float density = saturate(radial * floorWeight * wisps) * inside;

                float4 col = i.color;
                col.rgb *= lerp(0.82, 1.08, n);
                col.a *= density;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
