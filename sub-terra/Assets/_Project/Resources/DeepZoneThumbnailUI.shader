Shader "SubTerra/DeepZoneThumbnailUI"
{
    // B-155 업그레이드 창 심층 구역 썸네일. UI-Default 기반에 흑백↔컬러와 원형 색 퍼짐만 더했다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0, 1)) = 0
        _GrayBrightness ("Gray Brightness", Range(0, 1.5)) = 0.6
        _RevealCenter ("Reveal Center (UV)", Vector) = (0.5, 0.6, 0, 0)
        _RevealRadius ("Reveal Radius", Float) = 0
        _RevealFeather ("Reveal Feather", Float) = 0.18
        _RevealAspect ("Reveal Aspect (w/h)", Float) = 3
        _RevealRimColor ("Reveal Rim Color", Color) = (0.25, 0.92, 0.96, 1)
        _RevealRim ("Reveal Rim Strength", Range(0, 1)) = 0

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
            Name "DeepZoneThumbnail"
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
            float _Saturation;
            float _GrayBrightness;
            float4 _RevealCenter;
            float _RevealRadius;
            float _RevealFeather;
            float _RevealAspect;
            fixed4 _RevealRimColor;
            float _RevealRim;

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
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // 가로로 긴 칸에서도 퍼짐이 원으로 보이도록 x를 칸 비율만큼 늘려 거리를 잰다.
                // 가장 먼 모서리까지를 1로 맞추므로 반지름 1 + 페더면 칸 전체가 컬러가 된다.
                float aspect = max(_RevealAspect, 0.0001);
                float2 delta = i.texcoord - _RevealCenter.xy;
                delta.x *= aspect;
                float2 farCorner = float2(max(_RevealCenter.x, 1.0 - _RevealCenter.x) * aspect,
                    max(_RevealCenter.y, 1.0 - _RevealCenter.y));
                float dist = length(delta) / max(length(farCorner), 0.0001);
                float feather = max(_RevealFeather, 0.0001);
                float inside = 1.0 - smoothstep(_RevealRadius - feather, _RevealRadius, dist);
                inside *= step(0.0001, _RevealRadius);
                float colorAmount = saturate(max(_Saturation, inside));

                half luma = dot(color.rgb, half3(0.299, 0.587, 0.114));
                half3 gray = luma.xxx * _GrayBrightness;
                half3 rgb = lerp(gray, color.rgb, colorAmount);

                // 퍼지는 경계에 얇은 청록 테를 둘러 #188 해금 톤과 맞춘다.
                float rimWidth = feather * 0.3;
                float rim = 1.0 - abs(dist - (_RevealRadius - feather * 0.5)) / rimWidth;
                rim = saturate(rim) * _RevealRim * step(0.0001, _RevealRadius);
                rgb += _RevealRimColor.rgb * rim * 0.4;

                color.rgb = rgb;

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
