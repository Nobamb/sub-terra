using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 가이드가 쓰는 절차 생성 기호(마우스·화살표·정보·경고·책 등). 모두 흰색이라 Image.color로 색을 입힌다.
    /// 새 이미지 파일 없이 작은 텍스처를 한 번만 만들어 재사용한다.
    /// </summary>
    internal static class GameGuideArt
    {
        private const int Samples = 3;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<Sprite, Sprite> SubCache = new Dictionary<Sprite, Sprite>();

        /// <summary>마우스: 몸통 윤곽 + 왼쪽 버튼 면. 24x32.</summary>
        public static Sprite Mouse()
        {
            return Shape("mouse", 24, 32, (x, y) =>
            {
                var cx = x - 12f;
                var cy = y - 16f;
                var body = Mathf.Abs(cx) <= 9f && Mathf.Abs(cy) <= 14.5f
                    && (Mathf.Abs(cx) <= 9f - Mathf.Max(0f, Mathf.Abs(cy) - 8f) * 0f);
                var outer = RoundedBox(cx, cy, 9.5f, 14.5f, 8f);
                var inner = RoundedBox(cx, cy, 7.6f, 12.6f, 6.4f);
                var outline = outer && !inner;
                var divider = cy >= 3f && cy <= 14f && Mathf.Abs(cx) <= 0.9f;
                var split = cy >= 3f && cy <= 4.2f && Mathf.Abs(cx) <= 8f;
                var leftButton = inner && cx < -0.9f && cy > 4.2f;
                return outline || divider || split || leftButton && body;
            });
        }

        public static Sprite ArrowRight()
        {
            return Shape("arrow-right", 24, 24, (x, y) =>
            {
                var shaft = x >= 3f && x <= 17f && Mathf.Abs(y - 12f) <= 1.5f;
                var head = x >= 12f && x <= 20f && Mathf.Abs(y - 12f) <= (20f - x) * 0.95f && Mathf.Abs(y - 12f) >= (20f - x) * 0.95f - 3.2f;
                return shaft || head;
            });
        }

        public static Sprite ChevronDown()
        {
            return Shape("chevron-down", 24, 24, (x, y) =>
            {
                var d = Mathf.Abs(Mathf.Abs(x - 12f) - (17f - y));
                return y >= 7f && y <= 17f && Mathf.Abs(x - 12f) <= 10f && d <= 1.7f;
            });
        }

        public static Sprite ChevronRight()
        {
            return Shape("chevron-right", 24, 24, (x, y) =>
            {
                var d = Mathf.Abs(Mathf.Abs(y - 12f) - (17f - x));
                return x >= 7f && x <= 17f && Mathf.Abs(y - 12f) <= 10f && d <= 1.7f;
            });
        }

        /// <summary>동그란 윤곽 안의 i.</summary>
        public static Sprite Info()
        {
            return Shape("info", 32, 32, (x, y) =>
            {
                var r = Mathf.Sqrt((x - 16f) * (x - 16f) + (y - 16f) * (y - 16f));
                var ring = r <= 14.2f && r >= 12f;
                var dot = Mathf.Abs(x - 16f) <= 1.8f && y >= 21f && y <= 24.5f;
                var bar = Mathf.Abs(x - 16f) <= 1.8f && y >= 8f && y <= 18.5f;
                return ring || dot || bar;
            });
        }

        /// <summary>삼각형 윤곽 안의 느낌표.</summary>
        public static Sprite Warning()
        {
            return Shape("warning", 32, 32, (x, y) =>
            {
                var halfWidth = (28f - y) * 0.58f;
                var inside = y >= 4f && y <= 28f && Mathf.Abs(x - 16f) <= halfWidth;
                var innerHalf = (28f - (y - 2.5f)) * 0.58f - 2.4f;
                var hollow = y >= 6.5f && Mathf.Abs(x - 16f) <= innerHalf;
                var bar = Mathf.Abs(x - 16f) <= 1.7f && y >= 11f && y <= 20f;
                var dot = Mathf.Abs(x - 16f) <= 1.7f && y >= 7.5f && y <= 10f;
                return inside && !hollow || bar || dot;
            });
        }

        /// <summary>펼친 책 기호(관련 안내 링크용). 32x24.</summary>
        public static Sprite BookGlyph()
        {
            return Shape("book-glyph", 32, 24, (x, y) =>
            {
                var left = x >= 3f && x <= 15.5f && y >= 4f && y <= 20f;
                var right = x >= 16.5f && x <= 29f && y >= 4f && y <= 20f;
                var leftHollow = x >= 5f && x <= 14.5f && y >= 6f && y <= 18f;
                var rightHollow = x >= 17.5f && x <= 27f && y >= 6f && y <= 18f;
                var lines = (Mathf.Abs(y - 9f) <= 0.8f || Mathf.Abs(y - 13f) <= 0.8f)
                    && (x >= 6.5f && x <= 12.5f || x >= 19.5f && x <= 25.5f);
                return left && !leftHollow || right && !rightHollow || lines;
            });
        }

        /// <summary>
        /// 긴급 탈출 포탈. 게임에는 전용 아이콘이 없고 월드 프리팹이 청록 외곽 사각형(OuterFrame)과 남색 포털 면(PortalField)으로만
        /// 그려지므로 같은 구성·색을 절차로 옮긴다. 색 포함 64x64.
        /// </summary>
        public static Sprite Portal()
        {
            return Colored("portal", 64, 64, (x, y) =>
            {
                var outer = RoundedBox(x - 32f, y - 32f, 30f, 30f, 5f);
                if (!outer)
                {
                    return Color.clear;
                }

                var inner = RoundedBox(x - 32f, y - 32f, 24f, 24f, 3f);
                return inner ? new Color(0.08f, 0.16f, 0.35f, 0.95f) : new Color(0.10f, 0.80f, 0.95f, 0.95f);
            });
        }

        /// <summary>가는 원 윤곽(전력 공급 범위 표시). 128x128.</summary>
        public static Sprite Ring()
        {
            return Shape("ring", 128, 128, (x, y) =>
            {
                var r = Mathf.Sqrt((x - 64f) * (x - 64f) + (y - 64f) * (y - 64f));
                return r <= 62f && r >= 59.5f;
            });
        }

        /// <summary>마름모. 24x24.</summary>
        public static Sprite Diamond()
        {
            return Shape("diamond", 24, 24, (x, y) => Mathf.Abs(x - 12f) + Mathf.Abs(y - 12f) <= 10f);
        }

        /// <summary>작은 위·아래 화살표(시연의 방향 표시). 24x24.</summary>
        public static Sprite ArrowUp()
        {
            return Shape("arrow-up", 24, 24, (x, y) =>
            {
                var shaft = Mathf.Abs(x - 12f) <= 1.6f && y >= 3f && y <= 16f;
                var head = y >= 12f && y <= 21f && Mathf.Abs(x - 12f) <= (21f - y) * 0.95f
                    && Mathf.Abs(x - 12f) >= (21f - y) * 0.95f - 3.4f;
                return shaft || head;
            });
        }

        /// <summary>책 겉모습(닫힌 책 표지의 가는 선 무늬). 흰색 줄무늬 한 장. 가로로 이어 붙인다.</summary>
        public static Sprite Line()
        {
            return Shape("line", 16, 4, (x, y) => y >= 1f && y <= 3f);
        }

        /// <summary>원본 스프라이트의 일부를 새 스프라이트로 잘라 낸다(hud-icons 한 장짜리 시트용).</summary>
        public static Sprite Sub(Sprite source, Rect fractionRect)
        {
            if (source == null || source.texture == null)
            {
                return null;
            }

            var key = source;
            var rect = source.rect;
            var cut = new Rect(
                rect.x + rect.width * fractionRect.x,
                rect.y + rect.height * fractionRect.y,
                rect.width * fractionRect.width,
                rect.height * fractionRect.height);
            var cacheKey = source.name + fractionRect;
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = Sprite.Create(source.texture, cut, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0,
                SpriteMeshType.FullRect);
            sprite.name = source.name + "-sub";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[cacheKey] = sprite;
            SubCache[key] = sprite;
            return sprite;
        }

        private static bool RoundedBox(float cx, float cy, float halfW, float halfH, float radius)
        {
            var dx = Mathf.Abs(cx) - (halfW - radius);
            var dy = Mathf.Abs(cy) - (halfH - radius);
            if (dx <= 0f || dy <= 0f)
            {
                return Mathf.Abs(cx) <= halfW && Mathf.Abs(cy) <= halfH;
            }

            return dx * dx + dy * dy <= radius * radius;
        }

        private static Sprite Colored(string key, int width, int height, Func<float, float, Color> color)
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
                    pixels[y * width + x] = color(x + 0.5f, y + 0.5f);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "GameGuideArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Shape(string key, int width, int height, Func<float, float, bool> inside)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = key == "line" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
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

                    var alpha = (byte)Mathf.RoundToInt(255f * hits / (Samples * Samples));
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "GameGuideArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
