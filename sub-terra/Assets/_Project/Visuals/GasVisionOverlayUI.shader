Shader "SubTerra/GasVisionOverlayUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DarkColor ("Dark Color", Color) = (0.04, 0.05, 0.05, 0.95)
        _LightColor ("Light Color", Color) = (0.92, 0.30, 0.24, 0.05)
        _LightInner ("Light Hole Inner Fraction", Range(0, 0.99)) = 0.35
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GasVisionOverlay"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _DarkColor;
            float4 _LightColor;
            float _LightInner;
            float4 _ClipRect;
            float4 _GasLights[32];
            float _GasLightCount;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                int count = (int)_GasLightCount;
                float cover = 0.0;
                for (int n = 0; n < 32; n++)
                {
                    if (n >= count)
                    {
                        break;
                    }

                    float radius = max(_GasLights[n].z, 0.0001);
                    float2 delta = uv - _GasLights[n].xy;
                    float aspect = max(_GasLights[n].w, 0.0001);
                    delta.x *= aspect;
                    // 반지름 안쪽에서만 감쇠한다. 경계(radius)에서 0이라 판정 범위보다 커지지 않는다.
                    cover = max(cover, 1.0 - smoothstep(_LightInner * radius, radius, length(delta)));
                }

                // 프리멀티플라이드 보간: 반쯤 열린 경계 띠가 붉게 튀지 않고 어둠에서 붉은 빛으로 자연스럽게 이어진다.
                float a = lerp(_DarkColor.a, _LightColor.a, cover);
                float3 pm = lerp(_DarkColor.rgb * _DarkColor.a, _LightColor.rgb * _LightColor.a, cover);
                float4 color = float4(pm / max(a, 0.0001), a);
                color *= tex2D(_MainTex, uv);
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
