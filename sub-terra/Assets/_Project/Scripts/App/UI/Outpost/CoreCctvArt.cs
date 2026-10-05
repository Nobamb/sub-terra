using System;
using System.Collections.Generic;
using SubTerra.App.UI.HUD;
using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 코어 CCTV 팝업의 절차 생성 그림: 스캔라인·노이즈·육각형 무늬·테두리·발광.
    /// 새 이미지 파일을 늘리지 않으려고 작은 텍스처를 한 번만 만들어 재사용한다.
    /// </summary>
    internal static class CoreCctvArt
    {
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>한 줄만 불투명한 1x4 무늬. uvRect를 세로로 반복해 가는 스캔라인을 만든다.</summary>
        public static Texture2D Scanline()
        {
            return GetTexture("scanline", () =>
            {
                var texture = NewTexture(1, 4, FilterMode.Point, TextureWrapMode.Repeat);
                var pixels = new Color32[4];
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, (byte)(i == 0 ? 255 : 0));
                }

                Apply(texture, pixels);
                return texture;
            });
        }

        /// <summary>흑백 정적 노이즈. 매번 같은 모양이 나오도록 시드를 고정한다.</summary>
        public static Texture2D Noise()
        {
            return GetTexture("noise", () =>
            {
                const int size = 128;
                var texture = NewTexture(size, size, FilterMode.Point, TextureWrapMode.Repeat);
                var random = new System.Random(20260105);
                var pixels = new Color32[size * size];
                for (var i = 0; i < pixels.Length; i++)
                {
                    var v = (byte)random.Next(0, 256);
                    pixels[i] = new Color32(v, v, v, 255);
                }

                Apply(texture, pixels);
                return texture;
            });
        }

        /// <summary>가로 방향으로 이어 붙는 육각형 격자선. 흰색이라 RawImage.color로 청록색을 입힌다.</summary>
        public static Texture2D Hex()
        {
            return GetTexture("hex", () =>
            {
                const float radius = 30f;
                var periodX = 3f * radius;
                var periodY = Mathf.Sqrt(3f) * radius;
                var width = Mathf.RoundToInt(periodX);
                var height = Mathf.RoundToInt(periodY);
                var texture = NewTexture(width, height, FilterMode.Bilinear, TextureWrapMode.Repeat);
                var pixels = new Color32[width * height];
                var inradius = Mathf.Sqrt(3f) * 0.5f * radius;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        // 텍스처 한 칸을 격자 한 주기에 정확히 맞춰 이어 붙였을 때 이음매가 보이지 않게 한다.
                        var px = (x + 0.5f) / width * periodX;
                        var py = (y + 0.5f) / height * periodY;
                        var edge = EdgeDistance(px, py, periodX, periodY, radius, inradius);
                        var alpha = Mathf.Clamp01(1.4f - edge / 0.75f);
                        pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                Apply(texture, pixels);
                return texture;
            });
        }

        /// <summary>안쪽이 비고 테두리만 있는 9분할 스프라이트.</summary>
        public static Sprite Border()
        {
            return GetSprite("border", () =>
            {
                const int size = 24;
                var texture = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                        var alpha = edge < 2 ? 1f : 0f;
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                Apply(texture, pixels);
                return MakeSprite(texture, new Vector4(4f, 4f, 4f, 4f));
            });
        }

        /// <summary>가장자리가 부드럽게 번지는 9분할 발광. 이미지를 항목보다 조금 크게 둔다.</summary>
        public static Sprite Glow()
        {
            return GetSprite("glow", () =>
            {
                const int size = 48;
                const float fade = 16f;
                var texture = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                        var t = Mathf.Clamp01(edge / fade);
                        var alpha = t * t * (3f - 2f * t);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                Apply(texture, pixels);
                return MakeSprite(texture, new Vector4(16f, 16f, 16f, 16f));
            });
        }

        /// <summary>가장자리로 갈수록 어두워지는 비네트. 검은색이라 색은 그대로 쓴다.</summary>
        public static Sprite Vignette()
        {
            return GetSprite("vignette", () =>
            {
                const int size = 64;
                var texture = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var nx = (x + 0.5f) / size * 2f - 1f;
                        var ny = (y + 0.5f) / size * 2f - 1f;
                        var r = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny)) * 0.55f
                            + Mathf.Sqrt(nx * nx + ny * ny) * 0.45f;
                        var t = Mathf.Clamp01((r - 0.55f) / 0.55f);
                        var alpha = t * t * (3f - 2f * t) * 0.7f;
                        pixels[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                Apply(texture, pixels);
                return MakeSprite(texture, Vector4.zero);
            });
        }

        public static Sprite Dot()
        {
            return MineResetPopupArt.Soft("core-cctv-dot", 0.72f);
        }

        public static Sprite Soft()
        {
            return MineResetPopupArt.Soft("core-cctv-soft", 0.05f);
        }

        // 한 칸 안의 두 격자(중심이 어긋난 두 줄)에서 가장 가까운 육각형의 가장자리까지 거리.
        private static float EdgeDistance(
            float px, float py, float periodX, float periodY, float radius, float inradius)
        {
            var best = float.MaxValue;
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    for (var half = 0; half < 2; half++)
                    {
                        var cx = periodX * i + (half == 0 ? 0f : 1.5f * radius);
                        var cy = periodY * (j + (half == 0 ? 0f : 0.5f));
                        var dx = Mathf.Abs(px - cx);
                        var dy = Mathf.Abs(py - cy);
                        var edge = inradius - Mathf.Max(dy, Mathf.Sqrt(3f) * 0.5f * dx + 0.5f * dy);
                        // 셀 안쪽(edge >= 0)에서만 비교한다.
                        if (edge >= 0f && edge < best)
                        {
                            best = edge;
                        }
                    }
                }
            }

            return best == float.MaxValue ? 0f : best;
        }

        private static Texture2D GetTexture(string key, Func<Texture2D> factory)
        {
            if (Textures.TryGetValue(key, out var texture) && texture != null)
            {
                return texture;
            }

            texture = factory();
            texture.name = "CoreCctvArt-" + key;
            Textures[key] = texture;
            return texture;
        }

        private static Sprite GetSprite(string key, Func<Sprite> factory)
        {
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = factory();
            sprite.name = "CoreCctvArt-" + key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height, FilterMode filter, TextureWrapMode wrap)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = wrap,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void Apply(Texture2D texture, Color32[] pixels)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
        }

        private static Sprite MakeSprite(Texture2D texture, Vector4 border)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
