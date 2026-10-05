using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 판매 창 절차 생성 그림: 각진 버튼 면·테두리(9-slice), 절제된 육각형 무늬(타일), 금화, 발광, 작은 기호.
    /// 모두 흰색이라 Image.color로 색을 입힌다. 작은 텍스처를 한 번만 만들어 재사용한다(새 이미지 파일 없음).
    /// 기존 프레임 그림은 9-slice 경계가 없어서, 같은 텍스처를 가리키는 9-slice 스프라이트를 런타임에 한 번 만든다.
    /// </summary>
    internal static class ResourceSellArt
    {
        private const int Samples = 3;
        private const float Chamfer = 7f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        // 원본 그림마다 9-slice 경계 비율은 하나만 쓴다.
        private static readonly Dictionary<Sprite, Sprite> SlicedCache = new Dictionary<Sprite, Sprite>();

        public static Sprite Soft()
        {
            return Create("soft", 64, 64, Vector4.zero, TextureWrapMode.Clamp, (x, y) =>
            {
                const float c = 31.5f;
                var r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                var t = Mathf.Clamp01((r - 0.05f) / 0.95f);
                return 1f - t * t * (3f - 2f * t);
            });
        }

        /// <summary>9-slice 부드러운 발광 사각형(버튼·행 주변 번짐).</summary>
        public static Sprite SoftRect()
        {
            return Create("soft-rect", 48, 48, new Vector4(20f, 20f, 20f, 20f), TextureWrapMode.Clamp, (x, y) =>
            {
                var dx = Mathf.Max(0f, Mathf.Max(20f - x, x - 27f)) / 20f;
                var dy = Mathf.Max(0f, Mathf.Max(20f - y, y - 27f)) / 20f;
                var r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                return (1f - r) * (1f - r);
            });
        }

        /// <summary>모서리를 비스듬히 자른 면. 32x32, 9-slice.</summary>
        public static Sprite ChamferFill()
        {
            return Shape("chamfer-fill", 32, 32, new Vector4(10f, 10f, 10f, 10f), (x, y) => InsideChamfer(x, y, 32f, 0f));
        }

        /// <summary>각진 면의 얇은 테두리. 32x32, 9-slice.</summary>
        public static Sprite ChamferOutline()
        {
            return Shape("chamfer-outline", 32, 32, new Vector4(10f, 10f, 10f, 10f),
                (x, y) => InsideChamfer(x, y, 32f, 0f) && !InsideChamfer(x, y, 32f, 1.6f));
        }

        /// <summary>
        /// 이어 붙일 수 있는 육각형 선 무늬 한 장(80x46). 격자 중심의 보로노이 경계가 육각형이 된다.
        /// </summary>
        public static Sprite HexTile()
        {
            return Create("hex-tile", 80, 46, Vector4.zero, TextureWrapMode.Repeat, (x, y) =>
            {
                var best = float.MaxValue;
                var second = float.MaxValue;
                for (var gx = -1; gx <= 2; gx++)
                {
                    for (var gy = -1; gy <= 2; gy++)
                    {
                        Consider(x - gx * 80f, y - gy * 46f, ref best, ref second);
                        Consider(x - gx * 80f - 40f, y - gy * 46f - 23f, ref best, ref second);
                    }
                }

                var edge = second - best;
                return 1f - Mathf.Clamp01((edge - 0.6f) / 1.4f);
            });
        }

        /// <summary>금화 한 닢: 둥근 원판과 안쪽 홈. 64x64.</summary>
        public static Sprite Coin()
        {
            return Shape("coin", 64, 64, Vector4.zero, (x, y) =>
            {
                var r = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 32f) * (y - 32f));
                return r <= 29f && (r < 19f || r > 22.5f);
            });
        }

        /// <summary>
        /// 이모티콘처럼 도톰한 금화(색 포함, color는 흰색으로 둔다). 어두운 금테·밝은 면·안쪽 홈·하이라이트. 64x64.
        /// </summary>
        public static Sprite CoinEmoji()
        {
            return CreateColored("coin-emoji", 64, 64, (x, y) =>
            {
                var dx = x - 31.5f;
                var dy = y - 31.5f;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var edge = Mathf.Clamp01(30f - r);
                if (edge <= 0f)
                {
                    return Color.clear;
                }

                var rim = new Color(0.72f, 0.42f, 0.06f, 1f);
                // 왼쪽 위가 밝고 오른쪽 아래가 짙다(y는 위쪽이 +).
                var face = Color.Lerp(new Color(1f, 0.93f, 0.50f, 1f), new Color(0.98f, 0.68f, 0.16f, 1f),
                    Mathf.Clamp01((dx * 0.6f - dy * 0.8f) / 52f + 0.5f));
                var color = r > 26f ? rim : face;
                // 안쪽 홈: 면보다 조금 어두운 고리.
                if (r > 18f && r < 21.5f)
                {
                    color = Color.Lerp(face, new Color(0.80f, 0.52f, 0.08f, 1f), 0.8f);
                }

                // 왼쪽 위 하이라이트 호.
                var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (r > 22.5f && r < 25.5f && angle > 100f && angle < 170f)
                {
                    color = Color.Lerp(color, Color.white, 0.8f);
                }

                color.a = edge;
                return color;
            });
        }

        /// <summary>홀로그램 줄무늬 한 장(8x6). 위쪽 2줄만 켜진다. 흰색, 세로로 이어 붙인다.</summary>
        public static Sprite ScanTile()
        {
            return Create("scan-tile", 8, 6, Vector4.zero, TextureWrapMode.Repeat, (x, y) => y >= 4f ? 1f : 0f);
        }

        /// <summary>수량 조절 기호. plus=true면 +, 아니면 −. 24x24.</summary>
        public static Sprite Sign(bool plus)
        {
            return Shape(plus ? "plus" : "minus", 24, 24, Vector4.zero, (x, y) =>
            {
                var horizontal = Mathf.Abs(y - 12f) <= 1.6f && Mathf.Abs(x - 12f) <= 8f;
                var vertical = plus && Mathf.Abs(x - 12f) <= 1.6f && Mathf.Abs(y - 12f) <= 8f;
                return horizontal || vertical;
            });
        }

        /// <summary>닫기 X. 24x24.</summary>
        public static Sprite Cross()
        {
            return Shape("cross", 24, 24, Vector4.zero, (x, y) =>
            {
                var u = x - 12f;
                var v = y - 12f;
                var inBox = Mathf.Abs(u) <= 7.5f && Mathf.Abs(v) <= 7.5f;
                return inBox && (Mathf.Abs(u - v) <= 2.2f || Mathf.Abs(u + v) <= 2.2f);
            });
        }

        /// <summary>'최대 선택' 기호: 아래 화살표와 받침. 24x24.</summary>
        public static Sprite FillDown()
        {
            return Shape("fill-down", 24, 24, Vector4.zero, (x, y) =>
            {
                var tray = y >= 2f && y <= 4f && x >= 4f && x <= 20f
                    || (x >= 4f && x <= 6f || x >= 18f && x <= 20f) && y >= 2f && y <= 8f;
                var stem = Mathf.Abs(x - 12f) <= 1.4f && y >= 9f && y <= 21f;
                var headY = y - 7f;
                var head = headY >= 0f && headY <= 6f && Mathf.Abs(x - 12f) <= headY + 0.5f && Mathf.Abs(x - 12f) >= headY - 2.6f;
                return tray || stem || head;
            });
        }

        /// <summary>'선택 초기화' 기호: 끝에 화살촉이 달린 원호. 24x24.</summary>
        public static Sprite ResetArc()
        {
            return Shape("reset-arc", 24, 24, Vector4.zero, (x, y) =>
            {
                var dx = x - 12f;
                var dy = y - 12f;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                var ring = Mathf.Abs(r - 7.5f) <= 1.4f && !(angle > 20f && angle < 80f);
                // 화살촉: 원호 끝(약 80도) 근처 작은 삼각형.
                var hx = x - 13.5f;
                var hy = y - 19.5f;
                var head = hy <= 3.2f && hy >= -3.2f && hx >= -1f && hx <= 5f - Mathf.Abs(hy) * 1.4f;
                return ring || head;
            });
        }

        /// <summary>
        /// 9-slice 경계가 없는 기존 그림을 같은 텍스처의 9-slice 스프라이트로 감싼다(그림 파일은 그대로).
        /// border는 원본 픽셀 비율(가로·세로).
        /// </summary>
        public static Sprite Sliced(Sprite source, float borderX, float borderY)
        {
            if (source == null || source.texture == null)
            {
                return null;
            }

            if (SlicedCache.TryGetValue(source, out var cached) && cached != null)
            {
                return cached;
            }

            var rect = source.rect;
            var bx = Mathf.Round(rect.width * borderX);
            var by = Mathf.Round(rect.height * borderY);
            var sprite = Sprite.Create(
                source.texture,
                rect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(bx, by, bx, by));
            sprite.name = source.name + "-sliced";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SlicedCache[source] = sprite;
            return sprite;
        }

        private static void Consider(float dx, float dy, ref float best, ref float second)
        {
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < best)
            {
                second = best;
                best = d;
            }
            else if (d < second)
            {
                second = d;
            }
        }

        private static bool InsideChamfer(float x, float y, float size, float inset)
        {
            var lo = inset;
            var hi = size - inset;
            if (x < lo || x > hi || y < lo || y > hi)
            {
                return false;
            }

            var c = Chamfer - inset * 0.4f;
            // 왼쪽 위·오른쪽 아래만 깎은 비대칭 모서리(기존 팝업의 각진 금속 모서리 느낌).
            if ((x - lo) + (hi - y) < c)
            {
                return false;
            }

            return (hi - x) + (y - lo) >= c;
        }

        private static Sprite Shape(string key, int width, int height, Vector4 border, Func<float, float, bool> inside)
        {
            return Create(key, width, height, border, TextureWrapMode.Clamp, (x, y) =>
            {
                var hits = 0;
                for (var sy = 0; sy < Samples; sy++)
                {
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        if (inside(x - 0.5f + (sx + 0.5f) / Samples + 0.5f, y - 0.5f + (sy + 0.5f) / Samples + 0.5f))
                        {
                            hits++;
                        }
                    }
                }

                return hits / (float)(Samples * Samples);
            });
        }

        private static Sprite CreateColored(string key, int width, int height, Func<float, float, Color> color)
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
                    pixels[y * width + x] = color(x, y);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "ResourceSellArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Create(
            string key, int width, int height, Vector4 border, TextureWrapMode wrap, Func<float, float, float> alpha)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = wrap,
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
            sprite.name = "ResourceSellArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
