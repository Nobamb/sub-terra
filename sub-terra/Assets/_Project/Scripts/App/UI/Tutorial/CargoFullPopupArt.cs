using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 부족 팝업(B-152) 절차 생성 도트 그림: 게이지 칸, 칸 테두리, 초과 칸 사선 줄무늬, 목록 아이콘.
    /// 모두 흰색이라 Image.color로 색을 입히고, 번짐이 없게 Point 필터를 쓴다(새 이미지 파일 없음).
    /// </summary>
    internal static class CargoFullPopupArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<(Sprite, float, float), Sprite> SlicedCache =
            new Dictionary<(Sprite, float, float), Sprite>();

        /// <summary>게이지 칸 하나. 위 1px은 밝고 아래 1px은 어두운 납작한 블록(9-slice).</summary>
        public static Sprite Cell()
        {
            return Create("cell", 8, 8, new Vector4(2f, 2f, 2f, 2f), TextureWrapMode.Clamp, (x, y) =>
            {
                if (y == 7)
                {
                    return new Color32(255, 255, 255, 255);
                }

                return y == 0
                    ? new Color32(150, 150, 150, 255)
                    : new Color32(210, 210, 210, 255);
            });
        }

        /// <summary>1px 테두리(9-slice). 빈 칸·초과 칸 묶음 테두리에 쓴다.</summary>
        public static Sprite Outline()
        {
            return Create("outline", 8, 8, new Vector4(2f, 2f, 2f, 2f), TextureWrapMode.Clamp, (x, y) =>
                x == 0 || y == 0 || x == 7 || y == 7
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0));
        }

        /// <summary>이어 붙는 사선 줄무늬 한 장(16x16). Image.Type.Tiled로 깐다.</summary>
        public static Sprite Stripes()
        {
            return Create("stripes", 16, 16, Vector4.zero, TextureWrapMode.Repeat, (x, y) =>
                ((x + y) & 7) < 4
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0));
        }

        /// <summary>도트 목록 아이콘(16x16): 각진 상자 안에 점과 줄 세 개.</summary>
        public static Sprite ListIcon()
        {
            return Create("list", 16, 16, Vector4.zero, TextureWrapMode.Clamp, (x, y) =>
            {
                var on = false;
                // 상자 테두리(모서리 1px 깎음).
                var edgeX = x == 1 || x == 14;
                var edgeY = y == 1 || y == 14;
                var inside = x >= 1 && x <= 14 && y >= 1 && y <= 14;
                var corner = (x == 1 || x == 14) && (y == 1 || y == 14);
                if (inside && (edgeX || edgeY) && !corner)
                {
                    on = true;
                }

                // 세 줄: 점(2x2) + 막대.
                for (var row = 0; row < 3; row++)
                {
                    var top = 11 - row * 4;
                    if (y == top || y == top - 1)
                    {
                        if (x == 4 || x == 5)
                        {
                            on = true;
                        }

                        if (x >= 7 && x <= 11)
                        {
                            on = true;
                        }
                    }
                }

                return on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            });
        }

        /// <summary>도트 되돌아가기 아이콘(16x16): 왼쪽을 향한 화살촉 + 위로 꺾인 꼬리.</summary>
        public static Sprite BackIcon()
        {
            return Create("back", 16, 16, Vector4.zero, TextureWrapMode.Clamp, (x, y) =>
            {
                // 화살촉: (2,7) 꼭짓점에서 오른쪽으로 벌어지는 2px 두께 선.
                var dx = x - 2;
                var arrow = dx >= 0 && dx <= 5 && (y == 7 - dx || y == 8 - dx || y == 7 + dx || y == 8 + dx);
                // 몸통: 가로 막대 후 오른쪽에서 위로 꺾임.
                var shaft = (y == 7 || y == 8) && x >= 3 && x <= 12;
                var tail = (x == 12 || x == 13) && y >= 7 && y <= 13;
                return arrow || shaft || tail ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            });
        }

        /// <summary>9-slice 경계가 없는 기존 그림을 같은 텍스처의 9-slice 스프라이트로 감싼다(그림 파일은 그대로).</summary>
        public static Sprite Sliced(Sprite source, float borderX, float borderY)
        {
            if (source == null || source.texture == null)
            {
                return null;
            }

            var key = (source, borderX, borderY);
            if (SlicedCache.TryGetValue(key, out var cached) && cached != null)
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
            sprite.name = source.name + "-cargo-sliced";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SlicedCache[key] = sprite;
            return sprite;
        }

        private static Sprite Create(
            string key, int width, int height, Vector4 border, TextureWrapMode wrap, Func<int, int, Color32> pixel)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = wrap,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel(x, y);
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
            sprite.name = "CargoFullPopupArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
