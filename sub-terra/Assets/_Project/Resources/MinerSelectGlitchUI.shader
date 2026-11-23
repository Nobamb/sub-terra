Shader "SubTerra/MinerSelectGlitchUI"
{
    // B-209 광부 선택 화면이 닫히는 글리치. 닫히기 직전 화면을 캡처한 텍스처(RawImage)에 쓴다.
    // 가로 띠 찢김 + RGB 분리 + 블록 밀림 + 노이즈 + 세로 흔들림 → TV 꺼지듯 가로선으로 접힘 → 검정.
    Properties
    {
        [PerRendererData] _MainTex ("Captured Screen", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
        _Collapse ("TV Off Collapse", Range(0, 1)) = 0
        _LineWidth ("TV Off Line Width", Range(0, 1)) = 1
        _Flash ("Flash", Range(0, 1)) = 0
        _Black ("Black", Range(0, 1)) = 0
        _HoloColor ("Accent Color", Color) = (0.42, 0.94, 1, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
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
            Name "MinerSelectGlitch"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float _Intensity;
            float _Seed;
            float _Collapse;
            float _LineWidth;
            float _Flash;
            float _Black;
            fixed4 _HoloColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half3 Screen(float2 uv)
            {
                // 화면 밖은 검정(찢긴 띠가 비는 자리).
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return tex2D(_MainTex, saturate(uv)).rgb * inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float I = saturate(_Intensity);
                float seed = _Seed;

                // TV 꺼짐: 세로로 납작해져 가운데 가로선이 되고, 마지막에 선이 양옆에서 줄어든다.
                float squash = max(1.0 - _Collapse * 0.996, 0.004);
                float y = 0.5 + (uv.y - 0.5) / squash;
                float lineHalf = 0.5 * saturate(_LineWidth);
                float insideX = step(0.5 - lineHalf, uv.x) * step(uv.x, 0.5 + lineHalf);
                float insideY = step(0.0, y) * step(y, 1.0);
                float2 suv = float2(uv.x, y);

                // 세로 흔들림(롤).
                suv.y += (Hash(float2(seed, 3.1)) - 0.5) * 0.04 * I;

                // 가로 띠 찢김: 띠 높이도 프레임마다 바뀐다.
                float bands = lerp(18.0, 64.0, Hash(float2(seed, 9.7)));
                float band = floor(suv.y * bands);
                float bandPick = Hash(float2(band, seed));
                float tear = step(1.0 - I * 0.65, bandPick) * (Hash(float2(band * 1.3, seed + 1.0)) - 0.5) * 0.22 * I;
                suv.x += tear;

                // 블록 밀림.
                float2 cell = floor(suv * float2(16.0, 9.0));
                float blockPick = Hash(cell + seed * 1.7);
                float block = step(1.0 - I * 0.1, blockPick);
                suv += block * (float2(Hash(cell + 2.0 + seed), Hash(cell + 5.0 + seed)) - 0.5) * 0.08;

                // RGB 분리.
                float split = (0.004 + 0.018 * I) * (0.6 + 0.8 * Hash(float2(band, seed + 4.0)));
                half3 col;
                col.r = Screen(suv + float2(split, 0)).r;
                col.g = Screen(suv).g;
                col.b = Screen(suv - float2(split, 0)).b;

                // 밀린 블록 일부는 청록으로 물든다(원래 화면이 비치게 반만).
                float tint = block * step(0.55, Hash(cell + seed * 3.3));
                col = lerp(col, col * 0.4 + _HoloColor.rgb * 0.7, tint * 0.6);

                // 찢긴 띠는 살짝 밝게, 옅은 노이즈와 가는 흰 가로줄.
                col *= 1.0 + step(1.0 - I * 0.65, bandPick) * 0.25 * I;
                float grain = Hash(floor(uv * float2(480.0, 270.0)) + seed * 13.0);
                col += (grain - 0.5) * 0.06 * I;
                float streak = step(0.992 - I * 0.01, Hash(float2(floor(suv.y * 270.0), seed + 7.0)));
                col = lerp(col, half3(0.85, 1, 1), streak * 0.5 * I);

                // 스캔라인.
                col *= 0.85 + 0.15 * sin(uv.y * 1080.0 * 1.5708);

                // 접히는 동안 밝아지며 가로선만 남는다.
                col = lerp(col, half3(1, 1, 1) * 1.2, saturate(_Collapse * _Collapse) * 0.85);
                col += _Flash;

                float visible = insideY * insideX;
                col *= visible * (1.0 - saturate(_Black));
                return half4(col, 1.0) * i.color;
            }
            ENDCG
        }
    }

    Fallback Off
}
