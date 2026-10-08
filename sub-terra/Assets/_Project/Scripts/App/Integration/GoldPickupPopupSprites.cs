using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 팝업이 쓰는 절차 생성 스프라이트(글로우·라운드 사각형·구·원통·스캔라인). 외부 에셋 없이 한 번 만들어 공유한다.
    /// 모두 HideAndDontSave이고 도메인 리로드 때 다시 만들어진다.
    /// </summary>
    public static class GoldPickupPopupSprites
    {
        private const float Ppu = 100f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>가장자리가 부드럽게 사라지는 9-슬라이스 글로우.</summary>
        public static Sprite Glow()
        {
            return Get("glow", () =>
            {
                const int size = 32;
                const float fade = 12f;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        int edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                        float k = Mathf.Clamp01(edge / fade);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(k * k * (3f - 2f * k) * 255f));
                    }
                }

                return Make(size, size, pixels, new Vector4(12f, 12f, 12f, 12f), TextureWrapMode.Clamp);
            });
        }

        /// <summary>라운드 사각형. outline이 0보다 크면 그 두께의 테두리만 그린다. 9-슬라이스용.</summary>
        public static Sprite RoundRect(int radius, float outline)
        {
            return Get("rr" + radius + "_" + outline, () =>
            {
                int size = radius * 2 + 8;
                var pixels = new Color32[size * size];
                float half = size * 0.5f;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                        float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                        float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                        float d = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                        float alpha = Mathf.Clamp01(0.5f - d);
                        if (outline > 0f)
                        {
                            alpha *= Mathf.Clamp01(d + outline + 0.5f);
                        }

                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                float border = radius + 2;
                return Make(size, size, pixels, new Vector4(border, border, border, border), TextureWrapMode.Clamp);
            });
        }

        /// <summary>왼쪽 위에서 빛을 받은 금빛 구. 손잡이 공에 쓴다.</summary>
        public static Sprite Sphere()
        {
            return Get("sphere", () =>
            {
                const int size = 48;
                var pixels = new Color32[size * size];
                var light = new Vector3(-0.42f, 0.55f, 0.72f).normalized;
                Vector3 half = (light + Vector3.forward).normalized;
                var lit = new Color(1f, 0.82f, 0.28f);
                var dark = new Color(0.5f, 0.24f, 0.02f);
                float radius = size * 0.5f - 0.5f;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float nx = (x + 0.5f - size * 0.5f) / radius;
                        float ny = (y + 0.5f - size * 0.5f) / radius;
                        float r2 = nx * nx + ny * ny;
                        if (r2 >= 1.15f)
                        {
                            pixels[y * size + x] = new Color32(0, 0, 0, 0);
                            continue;
                        }

                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - r2));
                        var normal = new Vector3(nx, ny, nz);
                        float diffuse = Mathf.Max(0f, Vector3.Dot(normal, light));
                        float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, half)), 34f);
                        // 가장자리에 아래쪽 반사광을 살짝 넣어 금속 느낌을 낸다.
                        float rim = Mathf.Pow(1f - nz, 3f) * Mathf.Clamp01(-ny * 0.8f + 0.2f) * 0.35f;
                        Color color = Color.Lerp(dark, lit, 0.2f + 0.8f * diffuse);
                        color += new Color(1f, 0.85f, 0.45f) * rim + new Color(1f, 0.98f, 0.85f) * (spec * 0.9f);
                        float alpha = Mathf.Clamp01((Mathf.Sqrt(r2) - 1f) * -radius + 0.5f);
                        pixels[y * size + x] = new Color32(
                            ToByte(color.r), ToByte(color.g), ToByte(color.b), ToByte(alpha));
                    }
                }

                return Make(size, size, pixels, Vector4.zero, TextureWrapMode.Clamp);
            });
        }

        /// <summary>가로 방향으로 음영이 있는 놋쇠 원통. 세로로 늘려 막대에 쓴다.</summary>
        public static Sprite Cylinder()
        {
            return Get("cylinder", () =>
            {
                const int width = 16;
                const int height = 4;
                var pixels = new Color32[width * height];
                var light = new Vector3(-0.45f, 0f, 0.75f).normalized;
                Vector3 half = (light + Vector3.forward).normalized;
                var lit = new Color(1f, 0.84f, 0.38f);
                var dark = new Color(0.34f, 0.18f, 0.02f);
                for (var x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width * 2f - 1f;
                    var normal = new Vector3(u, 0f, Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
                    float diffuse = Mathf.Max(0f, Vector3.Dot(normal, light));
                    float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, half)), 22f);
                    Color color = Color.Lerp(dark, lit, 0.15f + 0.85f * diffuse) + new Color(1f, 0.95f, 0.75f) * (spec * 0.7f);
                    for (var y = 0; y < height; y++)
                    {
                        pixels[y * width + x] = new Color32(ToByte(color.r), ToByte(color.g), ToByte(color.b), 255);
                    }
                }

                return Make(width, height, pixels, Vector4.zero, TextureWrapMode.Clamp);
            });
        }

        /// <summary>중심이 진하고 바깥이 사라지는 타원 그림자.</summary>
        public static Sprite SoftDisc()
        {
            return Get("disc", () =>
            {
                const int size = 32;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float nx = (x + 0.5f) / size * 2f - 1f;
                        float ny = (y + 0.5f) / size * 2f - 1f;
                        float k = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                        pixels[y * size + x] = new Color32(255, 255, 255, ToByte(k * k * (3f - 2f * k)));
                    }
                }

                return Make(size, size, pixels, Vector4.zero, TextureWrapMode.Clamp);
            });
        }

        /// <summary>위가 진하고 아래로 사라지는 세로 그라디언트. 슬롯 창의 위·아래 어둠에 쓴다.</summary>
        public static Sprite VerticalFade()
        {
            return Get("vfade", () =>
            {
                const int width = 4;
                const int height = 16;
                var pixels = new Color32[width * height];
                for (var y = 0; y < height; y++)
                {
                    float k = y / (height - 1f);
                    byte alpha = ToByte(k * k);
                    for (var x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                return Make(width, height, pixels, Vector4.zero, TextureWrapMode.Clamp);
            });
        }

        /// <summary>타일로 반복해 깔 가는 가로 줄무늬. 홀로그램 스캔라인.</summary>
        public static Sprite Scanline()
        {
            return Get("scan", () =>
            {
                const int size = 4;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(y == 0 ? 255 : 0));
                    }
                }

                return Make(size, size, pixels, Vector4.zero, TextureWrapMode.Repeat);
            });
        }

        private static Sprite Get(string key, System.Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = create();
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Make(int width, int height, Color32[] pixels, Vector4 border, TextureWrapMode wrap)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = wrap,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }
    }
}
