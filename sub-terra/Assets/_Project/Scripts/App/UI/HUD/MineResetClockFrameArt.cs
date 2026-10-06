using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    internal static class MineResetClockFrameArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite PlateFill() => GetOrCreate("plate", 32, 32, 12f, (x, y) => Inside(x, y, 0f));
        public static Sprite Rim() => GetOrCreate("rim", 32, 32, 12f,
            (x, y) => Inside(x, y, 0f) && !Inside(x, y, 1f));
        public static Sprite Inner() => GetOrCreate("inner", 32, 32, 12f,
            (x, y) => Inside(x, y, 3f) && !Inside(x, y, 4f));
        public static Sprite CornerPlate() => GetOrCreate("corner", 20, 14, 0f, CornerInside, CornerShade);

        private static bool CornerInside(float x, float y)
        {
            var diagonal = x + 14f - y;
            return x >= 1f && x <= 19f && y >= 1f && y <= 13f && diagonal >= 12f
                && (diagonal <= 16f || y >= 10f || x <= 4f);
        }

        private static float CornerShade(float x, float y)
        {
            // 금속 명암은 불투명 회색으로 표현해 아래 구간색이 리벳 홈에 비치지 않게 한다.
            var rivet = (x - 13.5f) * (x - 13.5f) + (y - 11.5f) * (y - 11.5f);
            if (rivet < 0.25f) return 1f;
            if (rivet < 1.7f) return 0.28f;
            if (!CornerInside(x, y + 1f) || !CornerInside(x - 0.7f, y + 0.7f)) return 1f;
            if (!CornerInside(x, y - 1f) || !CornerInside(x + 1f, y)) return 0.42f;
            return 0.82f;
        }

        private static bool Inside(float x, float y, float inset)
        {
            var edgeX = Mathf.Min(x, 32f - x);
            var edgeY = Mathf.Min(y, 32f - y);
            return edgeX >= inset && edgeY >= inset && edgeX + edgeY >= 10f + inset;
        }

        private static Sprite GetOrCreate(string key, int width, int height, float border, Func<float, float, bool> inside,
            Func<float, float, float> shade = null)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var samples = 0;
                var brightness = 0f;
                for (var sy = 0; sy < 3; sy++)
                for (var sx = 0; sx < 3; sx++)
                {
                    var sampleX = x + (sx + 0.5f) / 3f;
                    var sampleY = y + (sy + 0.5f) / 3f;
                    if (inside(sampleX, sampleY))
                    {
                        samples++;
                        brightness += shade == null ? 1f : shade(sampleX, sampleY);
                    }
                }
                var value = samples > 0 ? (byte)(255f * brightness / samples) : (byte)255;
                pixels[y * width + x] = new Color32(value, value, value, (byte)(255 * samples / 9));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "MineResetClockFrameArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
