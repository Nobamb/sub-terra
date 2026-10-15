using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 웜홀(블랙홀)의 절차 생성 그림. 새 이미지 파일을 늘리지 않으려고 한 번만 만들어 재사용한다.
    /// 소용돌이는 뚜렷한 날개 대신 로그 나선 띠가 부드럽게 번지는 가스 원반이라 돌려도 선풍기처럼 보이지 않는다.
    /// </summary>
    public static class EscapeWormholeArt
    {
        public const int SwirlSize = 384;
        public const int CoreSize = 192;
        /// <summary>코어 스프라이트에서 밝은 고리(광자 고리)가 놓이는 반지름 비율.</summary>
        public const float CoreRingRadius = 0.88f;

        private static readonly Color Teal = new Color(0.04f, 0.5f, 0.66f, 1f);
        private static readonly Color PaleCyan = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color RingCyan = new Color(0.62f, 1f, 0.96f, 1f);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private struct SwirlSpec
        {
            public int Arms;
            public float Twist;
            public float FadeInStart;
            public float FadeInEnd;
            public float FadeOutStart;
            public float Floor;
            public float Sharpness;
            public float Boost;
        }

        // 0: 넓고 옅은 바깥 가스, 1: 구멍 가까이를 도는 촘촘하고 밝은 안쪽 원반.
        private static readonly SwirlSpec[] Specs =
        {
            new SwirlSpec { Arms = 2, Twist = 5.2f, FadeInStart = 0.26f, FadeInEnd = 0.44f, FadeOutStart = 0.62f, Floor = 0.2f, Sharpness = 1.7f, Boost = 0.55f },
            new SwirlSpec { Arms = 3, Twist = 3.6f, FadeInStart = 0.44f, FadeInEnd = 0.58f, FadeOutStart = 0.68f, Floor = 0.3f, Sharpness = 1.35f, Boost = 0.8f }
        };

        public static int LayerCount => Specs.Length;

        /// <summary>청록색 소용돌이 원반. layer 0은 바깥 가스, 1은 안쪽 밝은 원반.</summary>
        public static Sprite Swirl(int layer)
        {
            layer = Mathf.Clamp(layer, 0, Specs.Length - 1);
            return GetSprite("swirl-" + layer, () => BuildSwirl(Specs[layer]));
        }

        /// <summary>검은 사건의 지평선과 그 둘레의 밝은 청록 고리.</summary>
        public static Sprite Core()
        {
            return GetSprite("core", BuildCore);
        }

        /// <summary>한 픽셀의 소용돌이 색. 테스트에서 모양(날개 없음·중심 비어 있음)을 검사하려고 공개한다.</summary>
        public static Color SwirlPixel(int layer, float nx, float ny)
        {
            var spec = Specs[Mathf.Clamp(layer, 0, Specs.Length - 1)];
            return SwirlColor(spec, nx, ny);
        }

        private static Color SwirlColor(SwirlSpec spec, float nx, float ny)
        {
            var r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 1f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            var theta = Mathf.Atan2(ny, nx);
            var lr = Mathf.Log(Mathf.Max(r, 0.06f));
            // 로그 나선: 위상이 일정한 띠가 안쪽으로 감겨 들어간다. 양의 각도로 돌리면 띠가 안으로 빨려 든다.
            var band1 = 0.5f + 0.5f * Mathf.Cos(spec.Arms * theta + spec.Twist * lr);
            var band2 = 0.5f + 0.5f * Mathf.Cos((spec.Arms * 2 + 1) * theta + spec.Twist * 1.7f * lr + 1.3f);
            var streak = Mathf.Pow(band1, spec.Sharpness) * 0.72f + band2 * band2 * 0.28f;

            var fadeIn = Smooth(spec.FadeInStart, spec.FadeInEnd, r);
            var fadeOut = 1f - Smooth(spec.FadeOutStart, 1f, r);
            // 구멍에 가까울수록 밝다.
            var boost = 1f + (1f - r) * spec.Boost;
            var alpha = Mathf.Clamp01((spec.Floor + (1f - spec.Floor) * streak) * fadeIn * fadeOut * boost);
            var color = Color.Lerp(Teal, PaleCyan, Mathf.Clamp01(streak * (0.55f + (1f - r) * 0.7f)));
            color.a = alpha;
            return color;
        }

        private static Sprite BuildSwirl(SwirlSpec spec)
        {
            var size = SwirlSize;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var half = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = SwirlColor(spec, (x - half) / half, (y - half) / half);
                }
            }

            return Finish(texture, pixels);
        }

        /// <summary>코어 한 픽셀의 색(정규화 좌표 -1~1). 테스트에서 검은 중심과 밝은 고리를 검사하려고 공개한다.</summary>
        public static Color CorePixel(float nx, float ny)
        {
            var r = Mathf.Sqrt(nx * nx + ny * ny);
            // 검은 원판은 고리 안쪽까지 완전히 불투명하고 바깥으로 부드럽게 사라진다.
            var disc = 1f - Smooth(CoreRingRadius - 0.02f, CoreRingRadius + 0.04f, r);
            var ringDistance = (r - CoreRingRadius) / 0.045f;
            var ring = Mathf.Exp(-ringDistance * ringDistance);
            var haloDistance = Mathf.Max(0f, r - CoreRingRadius) / 0.16f;
            var halo = r > CoreRingRadius ? Mathf.Exp(-haloDistance * haloDistance) * 0.5f : 0f;
            var light = Mathf.Clamp01(ring + halo);
            var alpha = Mathf.Max(disc, light);
            var weight = alpha > 0.0001f ? Mathf.Clamp01(light / alpha) : 0f;
            var color = Color.Lerp(Color.black, RingCyan, weight);
            color.a = r >= 1f ? 0f : alpha;
            return color;
        }

        /// <summary>
        /// 이동 버튼 호버용 안쪽 빛. 가운데가 가장 밝고 가장자리는 완전히 투명해, 버튼 그림 위에 얹어도
        /// 테두리와 크기는 그대로이고 안쪽만 청록빛이 돈다.
        /// </summary>
        public static Sprite ButtonInnerGlow()
        {
            return GetSprite("button-inner-glow", BuildButtonInnerGlow);
        }

        /// <summary>안쪽 빛 한 픽셀의 색(정규화 좌표 -1~1). 가장자리 알파가 0인지 테스트에서 검사한다.</summary>
        public static Color ButtonInnerGlowPixel(float nx, float ny)
        {
            // 네 모서리가 둥근 사각형 거리: 긴 버튼에서도 가운데 띠가 은은하게 퍼진다.
            var ax = Mathf.Abs(nx);
            var ay = Mathf.Abs(ny);
            var d = Mathf.Pow(ax * ax * ax * ax + ay * ay * ay * ay, 0.25f);
            var alpha = 1f - Smooth(0.15f, 1f, d);
            return new Color(RingCyan.r * 0.55f, RingCyan.g, RingCyan.b, alpha * alpha);
        }

        private static Sprite BuildButtonInnerGlow()
        {
            const int width = 128;
            const int height = 32;
            var texture = NewTexture(width, height);
            var pixels = new Color32[width * height];
            var halfX = (width - 1) * 0.5f;
            var halfY = (height - 1) * 0.5f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = ButtonInnerGlowPixel((x - halfX) / halfX, (y - halfY) / halfY);
                }
            }

            return Finish(texture, pixels);
        }

        private static Sprite BuildCore()
        {
            var size = CoreSize;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            var half = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = CorePixel((x - half) / half, (y - half) / half);
                }
            }

            return Finish(texture, pixels);
        }

        private static float Smooth(float from, float to, float value)
        {
            var t = Mathf.Clamp01((value - from) / Mathf.Max(0.0001f, to - from));
            return t * t * (3f - 2f * t);
        }

        private static Sprite GetSprite(string key, Func<Sprite> factory)
        {
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = factory();
            sprite.name = "EscapeWormholeArt-" + key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite Finish(Texture2D texture, Color32[] pixels)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
