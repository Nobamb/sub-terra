using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 보관함 팝업 연출 전용 절차 생성 그림(흰색, Image.color로 색을 입힌다). 새 이미지 파일을 만들지 않는다.
    /// 자원 아이콘이 카탈로그에 없을 때의 보석 대체 그림과 '꺼내기' 기호만 담는다.
    /// </summary>
    internal static class StorageBoxArt
    {
        private const int Samples = 3;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>깎은 보석(육각 윤곽 + 윗면 분할선). 48x48.</summary>
        public static Sprite Gem()
        {
            return Shape("gem", 48, 48, (x, y) =>
            {
                var u = x - 24f;
                var v = y - 24f;
                var outer = Mathf.Abs(u) <= 18f && Mathf.Abs(v) <= 20f && Mathf.Abs(u) * 0.62f + Mathf.Abs(v) <= 22f;
                var inner = Mathf.Abs(u) <= 14.5f && Mathf.Abs(v) <= 16.5f && Mathf.Abs(u) * 0.62f + Mathf.Abs(v) <= 18.5f;
                var facet = inner && Mathf.Abs(v - 6f) <= 1.2f;
                var fill = inner && v < 6f;
                return outer && !inner || facet || fill;
            });
        }

        /// <summary>'꺼내기' 기호: 받침에서 위로 나가는 화살표. 24x24.</summary>
        public static Sprite LiftUp()
        {
            return Shape("lift-up", 24, 24, (x, y) =>
            {
                var tray = y >= 2f && y <= 4f && x >= 4f && x <= 20f
                    || (x >= 4f && x <= 6f || x >= 18f && x <= 20f) && y >= 2f && y <= 8f;
                var stem = Mathf.Abs(x - 12f) <= 1.4f && y >= 7f && y <= 17f;
                var headY = 22f - y;
                var head = headY >= 0f && headY <= 6f && Mathf.Abs(x - 12f) <= headY + 0.5f && Mathf.Abs(x - 12f) >= headY - 2.6f;
                return tray || stem || head;
            });
        }

        private static Sprite Shape(string key, int width, int height, Func<float, float, bool> inside)
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
                    var hits = 0;
                    for (var sy = 0; sy < Samples; sy++)
                    {
                        for (var sx = 0; sx < Samples; sx++)
                        {
                            if (inside(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples))
                            {
                                hits++;
                            }
                        }
                    }

                    var a = (byte)Mathf.RoundToInt(hits / (float)(Samples * Samples) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "StorageBoxArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
