using UnityEngine;

namespace SubTerra.App.UI.PlayerStatus
{
    /// <summary>
    /// 홀로그램 느낌의 노란 세모 느낌표 아이콘. 프로젝트에 재사용할 스프라이트가 없어 한 번만 코드로 그린다(에셋·패키지 추가 없음).
    /// 반투명한 면 + 밝은 테두리 + 바깥 글로우 + 가로 주사선으로 시설 이름표의 홀로그램 프레임과 어울리게 한다.
    /// 느낌표는 ASCII라 폰트 글리프 보완은 필요 없다.
    /// </summary>
    public static class PlayerStatusWarningIcon
    {
        private const int Size = 64;
        private const float StrokeHalf = 2.0f;
        private const float GlowRadius = 6.5f;
        private const float InnerFillAlpha = 0.20f;
        private const int ScanlinePeriod = 4;
        private const float ScanlineDim = 0.68f;

        // 세모 꼭짓점. 글로우가 텍스처 밖으로 잘리지 않도록 가장자리에서 띄운다.
        private static readonly Vector2 Apex = new Vector2(32f, 55f);
        private static readonly Vector2 BaseLeft = new Vector2(8f, 11f);
        private static readonly Vector2 BaseRight = new Vector2(56f, 11f);

        private static readonly Color Edge = new Color(1f, 0.88f, 0.36f, 1f);
        private static readonly Color Core = new Color(1f, 0.97f, 0.80f, 1f);
        private static readonly Color Glow = new Color(1f, 0.72f, 0.12f, 1f);
        private static readonly Color Fill = new Color(1f, 0.80f, 0.20f, 1f);

        private static Sprite cached;

        public static Sprite Icon
        {
            get
            {
                if (cached == null)
                {
                    cached = Build();
                }

                return cached;
            }
        }

        private static Sprite Build()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "PlayerStatusWarningIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                var scan = y % ScanlinePeriod == 0 ? ScanlineDim : 1f;
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var color = Shade(p);
                    color.a *= scan;
                    pixels[y * Size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static Color Shade(Vector2 p)
        {
            var signed = SignedDistance(p);
            var result = new Color(0f, 0f, 0f, 0f);

            // 바깥 글로우: 테두리에서 멀어질수록 급격히 옅어진다.
            if (signed > StrokeHalf && signed < StrokeHalf + GlowRadius)
            {
                var t = 1f - (signed - StrokeHalf) / GlowRadius;
                result = Over(result, WithAlpha(Glow, 0.50f * t * t));
            }

            // 안쪽 면: 아래쪽이 조금 더 진한 반투명 필름.
            if (signed < 0f)
            {
                var height = Mathf.InverseLerp(BaseLeft.y, Apex.y, p.y);
                var inner = InnerFillAlpha * Mathf.Lerp(1.25f, 0.6f, height);
                // 안쪽으로 번지는 가장자리 빛.
                var rim = Mathf.Clamp01(1f - (-signed - StrokeHalf) / 5f) * 0.28f;
                result = Over(result, WithAlpha(Fill, inner + rim));
            }

            // 테두리: 겉은 노랑, 중심선은 백열.
            var strokeDistance = Mathf.Abs(signed);
            if (strokeDistance <= StrokeHalf)
            {
                var soft = Mathf.Clamp01((StrokeHalf - strokeDistance) / 0.9f);
                result = Over(result, WithAlpha(Edge, soft));
                if (strokeDistance <= StrokeHalf * 0.45f)
                {
                    result = Over(result, WithAlpha(Core, 0.9f));
                }
            }

            // 느낌표.
            var mark = MarkCoverage(p);
            if (mark > 0f)
            {
                result = Over(result, WithAlpha(Glow, 0.35f * mark));
                result = Over(result, WithAlpha(mark >= 0.999f ? Core : Edge, 0.95f * mark));
            }

            return result;
        }

        // 음수: 세모 안쪽. 양수: 바깥. 값의 크기는 가장 가까운 변까지의 거리.
        private static float SignedDistance(Vector2 p)
        {
            var d = Mathf.Min(
                DistanceToSegment(p, Apex, BaseLeft),
                Mathf.Min(DistanceToSegment(p, BaseLeft, BaseRight), DistanceToSegment(p, BaseRight, Apex)));
            return Inside(p) ? -d : d;
        }

        private static bool Inside(Vector2 p)
        {
            var d1 = Cross(p, Apex, BaseLeft);
            var d2 = Cross(p, BaseLeft, BaseRight);
            var d3 = Cross(p, BaseRight, Apex);
            var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // 막대 + 점. 가장자리 1px는 부드럽게(0~1), 안쪽은 1.
        private static float MarkCoverage(Vector2 p)
        {
            var bar = Box(p, 32f, 31f, 2.6f, 8.5f);
            var dot = Box(p, 32f, 18.5f, 2.8f, 2.8f);
            return Mathf.Max(bar, dot);
        }

        private static float Box(Vector2 p, float cx, float cy, float halfW, float halfH)
        {
            var dx = halfW - Mathf.Abs(p.x - cx);
            var dy = halfH - Mathf.Abs(p.y - cy);
            return Mathf.Clamp01(Mathf.Min(dx, dy) + 0.5f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        // top을 bottom 위에 straight-alpha로 합성한다.
        private static Color Over(Color bottom, Color top)
        {
            var a = top.a + bottom.a * (1f - top.a);
            if (a <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            var rgb = (new Vector3(top.r, top.g, top.b) * top.a
                + new Vector3(bottom.r, bottom.g, bottom.b) * bottom.a * (1f - top.a)) / a;
            return new Color(rgb.x, rgb.y, rgb.z, a);
        }
    }
}
