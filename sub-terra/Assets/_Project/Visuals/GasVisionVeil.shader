Shader "SubTerra/GasVisionVeil"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Dark Color", Color) = (0.04, 0.05, 0.05, 0.95)
        _LightColor ("Light Color", Color) = (1, 0.12, 0.08, 0.05)
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _LightColor;
            float4 _GasLights[32];
            float _GasLightCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float texAlpha = tex2D(_MainTex, i.uv).a;
                int count = (int)_GasLightCount;
                float cover = 0.0;
                for (int n = 0; n < 32; n++)
                {
                    if (n >= count)
                    {
                        break;
                    }

                    float radius = max(_GasLights[n].w, 0.0001);
                    float distance = length(i.worldPos - _GasLights[n].xy);
                    cover = max(cover, step(distance, radius));
                }

                float4 color = lerp(_Color, _LightColor, cover);
                color.a *= texAlpha;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
