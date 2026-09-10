Shader "SubTerra/DepthDarknessOverlayUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DarkColor ("Dark Color", Color) = (0,0,0,0.95)
        _PlayerRadius ("Player Visible Radius", Float) = 0.11
        _Feather ("Edge Feather", Float) = 0.04
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width Cells", Float) = 0.07
        _BlockDarkAlpha ("Occupied Block Dark Alpha", Float) = 1
        _Fade ("Boundary Fade", Float) = 0
        _OccupancyTex ("Occupancy", 2D) = "black" {}
        _WorldMin ("World Min", Vector) = (0,0,0,0)
        _WorldMax ("World Max", Vector) = (1,1,0,0)
        _OccWorldMin ("Occupancy World Min", Vector) = (0,0,0,0)
        _CellSize ("Cell Size", Vector) = (1,1,0,0)
        _OccTexSize ("Occupancy Tex Size", Vector) = (0,0,0,0)
        _PlayerViewport ("Player Viewport", Vector) = (0.5,0.5,1,0)
        _ScanGlowRadius ("Scan Glow Radius Cells", Float) = 1.05
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
            sampler2D _OccupancyTex;
            float4 _Color;
            float4 _DarkColor;
            float4 _PlayerViewport;
            float4 _WorldMin;
            float4 _WorldMax;
            float4 _OccWorldMin;
            float4 _CellSize;
            float4 _OccTexSize;
            float4 _OutlineColor;
            float _PlayerRadius;
            float _Feather;
            float _OutlineWidth;
            float _BlockDarkAlpha;
            float _Fade;
            float _ScanGlowRadius;
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
                float darkMask = smoothstep(
                    _PlayerRadius,
                    _PlayerRadius + max(_Feather, 0.0001),
                    length(delta));

                float occupied = 0;
                float outline = 0;
                float scanGlow = 0;
                if (_OccTexSize.x > 0.5 && _CellSize.x > 0.0001)
                {
                    float2 world = lerp(_WorldMin.xy, _WorldMax.xy, i.texcoord.xy);
                    float2 cellFloat = (world - _OccWorldMin.xy) / max(_CellSize.xy, 0.0001);
                    float2 cellIndex = floor(cellFloat);
                    float2 cellFrac = cellFloat - cellIndex;
                    float2 occUV = (cellIndex + 0.5) / max(_OccTexSize.xy, 0.0001);
                    if (occUV.x >= 0.0 && occUV.x <= 1.0 && occUV.y >= 0.0 && occUV.y <= 1.0)
                    {
                        occupied = tex2D(_OccupancyTex, occUV).r;
                    }

                    [unroll]
                    for (int scanY = -1; scanY <= 1; scanY++)
                    {
                        [unroll]
                        for (int scanX = -1; scanX <= 1; scanX++)
                        {
                            float2 scanCell = cellIndex + float2(scanX, scanY);
                            float2 scanUV = (scanCell + 0.5) / max(_OccTexSize.xy, 0.0001);
                            if (scanUV.x >= 0.0 && scanUV.x <= 1.0
                                && scanUV.y >= 0.0 && scanUV.y <= 1.0)
                            {
                                float target = tex2D(_OccupancyTex, scanUV).g;
                                float distanceFromTarget = length(cellFloat - (scanCell + 0.5));
                                float glow = 1.0 - smoothstep(0.42, max(0.43, _ScanGlowRadius), distanceFromTarget);
                                scanGlow = max(scanGlow, target * glow);
                            }
                        }
                    }

                    float edge = min(min(cellFrac.x, 1.0 - cellFrac.x), min(cellFrac.y, 1.0 - cellFrac.y));
                    outline = occupied * step(edge, _OutlineWidth) * darkMask;
                }

                float fade = saturate(_Fade);
                float4 tinted = tex2D(_MainTex, i.texcoord) * i.color;
                float screenAlpha = _DarkColor.a * tinted.a * fade;
                float blockAlpha = max(_DarkColor.a, _BlockDarkAlpha) * tinted.a * fade;
                float4 darkColor = float4(_DarkColor.rgb, lerp(screenAlpha, blockAlpha, occupied));
                darkColor.a *= darkMask;

                // 화면 암전을 테두리 위에 덮어 깊이에 따라 테두리 밝기가 달라지게 한다.
                // 테두리도 10m 진입 페이드에 맞춰 알파가 올라가야 한 번에 나타나지 않는다.
                float remain = saturate(1.0 - _DarkColor.a);
                float4 veiledOutline = float4(_OutlineColor.rgb * remain, fade);
                float4 color = lerp(darkColor, veiledOutline, outline);
                color.a *= 1.0 - saturate(scanGlow);

                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
