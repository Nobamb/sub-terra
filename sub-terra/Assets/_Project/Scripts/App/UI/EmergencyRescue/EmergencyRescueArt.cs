using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 구출 홀로그램 전용 절차 생성 그림: 사람 실루엣, 위쪽 화살표, 키캡, 코인, 부드러운 발광.
    /// 모두 흰색이라 Image.color로 색을 입히고, 각 부위가 따로 움직이도록 개별 스프라이트로 만든다.
    /// 새 이미지 파일을 늘리지 않으려고 작은 텍스처를 한 번만 만들어 재사용한다.
    /// </summary>
    internal static class EmergencyRescueArt
    {
        private const int Samples = 3;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>중심이 진하고 가장자리로 사라지는 원형 발광.</summary>
        public static Sprite Soft()
        {
            return Field("soft", 96, 96, Vector4.zero, (x, y) =>
            {
                const float center = 47.5f;
                var nx = (x - center) / center;
                var ny = (y - center) / center;
                var r = Mathf.Sqrt(nx * nx + ny * ny);
                var t = Mathf.Clamp01((r - 0.05f) / 0.95f);
                return 1f - t * t * (3f - 2f * t);
            });
        }

        /// <summary>사람 실루엣(머리·몸통·팔·다리). 64x64, 아래쪽이 발.</summary>
        public static Sprite Human()
        {
            return Shape("human", 64, 64, Vector4.zero, (x, y) =>
            {
                var d = Disc(x, y, 32f, 52f, 7.5f);
                d = Mathf.Min(d, Capsule(x, y, 32f, 40f, 32f, 27f, 8.5f));
                d = Mathf.Min(d, Capsule(x, y, 27.5f, 24f, 27.5f, 7f, 4.4f));
                d = Mathf.Min(d, Capsule(x, y, 36.5f, 24f, 36.5f, 7f, 4.4f));
                d = Mathf.Min(d, Capsule(x, y, 22f, 40f, 18.5f, 27f, 3.6f));
                d = Mathf.Min(d, Capsule(x, y, 42f, 40f, 45.5f, 27f, 3.6f));
                return d <= 0f;
            });
        }

        /// <summary>위쪽 화살표. 48x64, 끝이 위.</summary>
        public static Sprite Arrow()
        {
            return Shape("arrow", 48, 64, Vector4.zero, (x, y) =>
            {
                const float tipY = 62f;
                const float headBase = 30f;
                const float halfHead = 22f;
                if (y <= tipY && y >= headBase)
                {
                    var half = (tipY - y) / (tipY - headBase) * halfHead;
                    if (Mathf.Abs(x - 24f) <= half)
                    {
                        return true;
                    }
                }

                return y >= 2f && y < headBase && Mathf.Abs(x - 24f) <= 7f;
            });
        }

        /// <summary>코인. 바깥 원판과 안쪽 둥근 홈. 64x64.</summary>
        public static Sprite Coin()
        {
            return Shape("coin", 64, 64, Vector4.zero, (x, y) =>
            {
                var r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 32f) * (y - 32f));
                if (r > 29f)
                {
                    return false;
                }

                return r < 19f || r > 23f;
            });
        }

        /// <summary>9-slice 둥근 사각형. 키캡 윗면·옆면에 쓴다.</summary>
        public static Sprite RoundedRect()
        {
            return Shape("round", 32, 32, new Vector4(10f, 10f, 10f, 10f), (x, y) =>
            {
                const float radius = 9f;
                var cx = Mathf.Clamp(x, radius, 32f - radius);
                var cy = Mathf.Clamp(y, radius, 32f - radius);
                var dx = x - cx;
                var dy = y - cy;
                return dx * dx + dy * dy <= radius * radius;
            });
        }

        private static float Disc(float x, float y, float cx, float cy, float r)
        {
            return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
        }

        private static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            var pax = x - ax;
            var pay = y - ay;
            var bax = bx - ax;
            var bay = by - ay;
            var h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            var dx = pax - bax * h;
            var dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        private static Sprite Field(
            string key, int width, int height, Vector4 border, Func<float, float, float> alpha)
        {
            return Create(key, width, height, border, alpha);
        }

        private static Sprite Shape(
            string key, int width, int height, Vector4 border, Func<float, float, bool> inside)
        {
            return Create(key, width, height, border, (x, y) =>
            {
                // 하위 샘플 평균으로 가장자리를 부드럽게 만든다.
                var hits = 0;
                for (var sy = 0; sy < Samples; sy++)
                {
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var px = x - 0.5f + (sx + 0.5f) / Samples;
                        var py = y - 0.5f + (sy + 0.5f) / Samples;
                        if (inside(px, py))
                        {
                            hits++;
                        }
                    }
                }

                return hits / (float)(Samples * Samples);
            });
        }

        private static Sprite Create(
            string key, int width, int height, Vector4 border, Func<float, float, float> alpha)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = Mathf.Clamp01(alpha(x, y));
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = "EmergencyRescueArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
