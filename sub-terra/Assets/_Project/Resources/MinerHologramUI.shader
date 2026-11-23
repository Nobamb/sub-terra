Shader "SubTerra/MinerHologramUI"
{
    // B-209 광부 선택 카드. UI-Default 기반에 청록 실루엣, 윤곽 또렷하게(_Detail), 아래에서 위로 원래 색 차오름(_Fill)만 더했다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HoloColor ("Hologram Color", Color) = (0.42, 0.94, 1, 1)
        _Detail ("Detail", Range(0, 1)) = 0
        _Fill ("Color Fill", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0, 2)) = 1
        _ScanAlpha ("Scanline Strength", Range(0, 1)) = 0.22
        _ScanDensity ("Scanline Count", Float) = 90
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
            #pragma target 2.0
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
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _HoloColor;
            float _Detail;
            float _Fill;
            float _Brightness;
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

            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;
                half luma = dot(tex.rgb, half3(0.299, 0.587, 0.114));

                // 기본은 그림 모양만 보이는 단색 청록. _Detail이 오르면 원본 밝기가 섞여 얼굴·옷 경계가 드러난다.
                half flat = 0.72 + 0.1 * luma;
                half detailed = saturate(0.18 + luma * 1.45);
                half shade = lerp(flat, detailed, saturate(_Detail));
                half3 holo = _HoloColor.rgb * shade * _Brightness;

                float localY = saturate((i.texcoord.y - _UvRect.y) / max(_UvRect.w, 0.0001));
                float scan = 0.5 + 0.5 * sin((localY * _ScanDensity - _Time.y * 1.6) * 6.2831853);
                holo *= 1.0 - _ScanAlpha * scan;

                // 확정 때 아래에서 위로 원래 색이 차오르고, 경계에 밝은 스캔 줄이 지나간다.
                float hasFill = step(0.0001, _Fill);
                float filled = (1.0 - smoothstep(_Fill - 0.035, _Fill, localY)) * hasFill;
                half3 rgb = lerp(holo, tex.rgb, filled);
                float front = saturate(1.0 - abs(localY - _Fill) / 0.02) * hasFill * step(_Fill, 0.999);
                rgb += _HoloColor.rgb * front * 0.9 * step(0.02, tex.a);

                half alpha = tex.a * lerp(0.86, 1.0, filled);
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
