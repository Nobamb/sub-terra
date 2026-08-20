Shader "SubTerra/DepthDarknessOverlayUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DarkColor ("Dark Color", Color) = (0,0,0,0.95)
        _PlayerRadius ("Player Visible Radius", Float) = 0.11
        _Feather ("Edge Feather", Float) = 0.04
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
            Name "DepthDarknessOverlay"
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
            float4 _PlayerViewport;
            float _PlayerRadius;
            float _Feather;
            float4 _ClipRect;

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
                float2 delta = i.texcoord - _PlayerViewport.xy;
                delta.x *= max(_PlayerViewport.z, 0.0001);
                float visibilityMask = smoothstep(
                    _PlayerRadius,
                    _PlayerRadius + max(_Feather, 0.0001),
                    length(delta));

                float4 color = _DarkColor * tex2D(_MainTex, i.texcoord) * i.color;
                color.a *= visibilityMask;
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
