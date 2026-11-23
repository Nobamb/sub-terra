Shader "SubTerra/MinerHologramUI"
{
    // B-209 광부 선택 카드의 광부 그림. UI-Default 기반.
    // 기본: 윤곽 테두리만 빛나는 청록 실루엣. _Reveal: 위→아래 스캔 줄이 지나간 곳부터 얼굴·옷 선이 드러남
    // (청록 단색 계조 + 밝기 선명화 + 또렷한 가장자리, 스캔라인은 옅게).
    // _Fill: 위→아래 스캔 줄이 지나간 곳부터 원래 색. _Glitch: 가로 띠 어긋남.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HoloColor ("Hologram Color", Color) = (0.42, 0.94, 1, 1)
        _Reveal ("Detail Scan (top to bottom)", Range(0, 1)) = 0
        _Fill ("Color Scan (top to bottom)", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Glitch ("Band Glitch", Range(0, 1)) = 0.15
        _ScanAlpha ("Scanline Strength", Range(0, 1)) = 0.22
        _ScanDensity ("Scanline Count", Float) = 110
        _UvRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MinerHologram"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _HoloColor;
            float _Reveal;
            float _Fill;
            float _Brightness;
            float _Glitch;
            float _ScanAlpha;
            float _ScanDensity;
            float4 _UvRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Hash(float n)
            {
                return frac(sin(n * 12.9898) * 43758.5453);
            }

            half4 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv) + _TextureSampleAdd;
            }

            // 스캔 줄 위치(1=위, 0=아래). 진행 0이면 그림 위쪽 밖, 1이면 아래쪽 밖.
            float ScanLine(float progress)
            {
                return lerp(1.04, -0.04, saturate(progress));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float localY = saturate((i.texcoord.y - _UvRect.y) / max(_UvRect.w, 0.0001));

                // 가로 띠 어긋남: 시간마다 바뀌는 띠 몇 개만 옆으로 밀린다.
                float t = floor(_Time.y * 14.0);
                float band = floor(localY * 46.0);
                float pick = Hash(band * 1.7 + t * 7.3);
                float shift = step(1.0 - _Glitch * 0.18, pick) * (Hash(band + t * 3.1) - 0.5) * 0.05 * _UvRect.z;
                float2 uv = i.texcoord + float2(shift, 0.0);

                half4 tex = Sample(uv);
                float2 px = _MainTex_TexelSize.xy * 1.6;
                half aL = Sample(uv - float2(px.x, 0)).a;
                half aR = Sample(uv + float2(px.x, 0)).a;
                half aD = Sample(uv - float2(0, px.y)).a;
                half aU = Sample(uv + float2(0, px.y)).a;
                half lL = dot(Sample(uv - float2(px.x, 0)).rgb, half3(0.299, 0.587, 0.114));
                half lR = dot(Sample(uv + float2(px.x, 0)).rgb, half3(0.299, 0.587, 0.114));
                half lD = dot(Sample(uv - float2(0, px.y)).rgb, half3(0.299, 0.587, 0.114));
                half lU = dot(Sample(uv + float2(0, px.y)).rgb, half3(0.299, 0.587, 0.114));
                half luma = dot(tex.rgb, half3(0.299, 0.587, 0.114));

                // 실루엣 가장자리(바깥 윤곽)와 안쪽 선(밝기 경계).
                half rim = saturate((tex.a - min(min(aL, aR), min(aD, aU))) * 2.2);
                half lines = saturate((abs(lR - lL) + abs(lU - lD)) * 2.4) * tex.a;

                // 기본 실루엣: 위가 조금 밝은 단색 청록 + 빛나는 테두리.
                half3 cyan = _HoloColor.rgb;
                half body = 0.32 + 0.2 * localY;
                half3 silhouette = cyan * body + cyan * rim * 1.5 + cyan * lines * 0.12;

                // 드러난 홀로그램: 1텍셀 이웃으로 밝기를 선명하게 한 뒤 청록 계조로 칠한다(어두운 선은 어둡게 남아 생김새가 또렷하다).
                float2 p1 = _MainTex_TexelSize.xy;
                half4 n0 = Sample(uv - float2(p1.x, 0));
                half4 n1 = Sample(uv + float2(p1.x, 0));
                half4 n2 = Sample(uv - float2(0, p1.y));
                half4 n3 = Sample(uv + float2(0, p1.y));
                half3 lw = half3(0.299, 0.587, 0.114);
                half nA = n0.a + n1.a + n2.a + n3.a;
                half nLuma = (dot(n0.rgb, lw) * n0.a + dot(n1.rgb, lw) * n1.a + dot(n2.rgb, lw) * n2.a + dot(n3.rgb, lw) * n3.a)
                    / max(nA, 0.001);
                half sharp = saturate(luma + (luma - nLuma) * 0.85);
                half3 shade = lerp(cyan * 0.1, cyan * 0.9, smoothstep(0.0, 0.6, sharp));
                shade = lerp(shade, lerp(cyan, half3(1, 1, 1), 0.35), smoothstep(0.6, 1.0, sharp));
                half rimThin = saturate((tex.a - min(min(n0.a, n1.a), min(n2.a, n3.a))) * 1.6);
                half3 detailed = shade * 1.1 + cyan * rimThin * 0.35;
                // 확대된 그림의 번진 알파 가장자리를 화면 1px 안쪽으로 모은다.
                half edgeW = max(fwidth(tex.a), 0.0001);
                half crispA = saturate((tex.a - 0.5) / edgeW + 0.5);
                half detailAlpha = lerp(tex.a, crispA, 0.75);

                float revealLine = ScanLine(_Reveal);
                float revealed = smoothstep(revealLine - 0.012, revealLine + 0.012, localY);
                half3 holo = lerp(silhouette, detailed, revealed) * _Brightness;

                float scan = 0.5 + 0.5 * sin((localY * _ScanDensity - _Time.y * 2.2) * 6.2831853);
                holo *= 1.0 - _ScanAlpha * scan * lerp(1.0, 0.3, revealed);

                float fillLine = ScanLine(_Fill);
                float filled = smoothstep(fillLine - 0.01, fillLine + 0.01, localY) * step(0.0001, _Fill);
                half3 rgb = lerp(holo, tex.rgb, filled);

                // 진행 중인 스캔 줄: 줄 바로 위는 밝은 띠, 그 위로 잠깐 남는 잔광.
                float active = step(0.0001, _Reveal) * step(_Reveal, 0.9999);
                float dR = localY - revealLine;
                float frontR = exp(-abs(dR) * 140.0) + saturate(1.0 - dR * 9.0) * step(0.0, dR) * 0.35;
                rgb += cyan * frontR * active * 1.2 * step(0.02, tex.a);

                float activeF = step(0.0001, _Fill) * step(_Fill, 0.9999);
                float dF = localY - fillLine;
                float frontF = exp(-abs(dF) * 140.0) + saturate(1.0 - dF * 10.0) * step(0.0, dF) * 0.3;
                rgb += lerp(cyan, half3(1, 1, 1), 0.5) * frontF * activeF * 1.3 * step(0.02, tex.a);

                half holoAlpha = lerp(tex.a * 0.62 + rim * 0.3, detailAlpha * 0.96, revealed);
                half alpha = saturate(lerp(holoAlpha, tex.a, filled));
                half4 color = half4(rgb, alpha) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
