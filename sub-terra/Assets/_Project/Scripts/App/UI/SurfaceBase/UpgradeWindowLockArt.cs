using System;
using System.Collections.Generic;
using SubTerra.App.UI.Progression;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// B-167 업그레이드 창 자물쇠의 절차 생성 픽셀 그림. 노드·썸네일 자물쇠와 같은 4조각 비율(30x34 단위)을
    /// 1단위 1픽셀 텍스처로 만들고 point 필터로 확대해 픽셀감을 낸다. 새 이미지 파일 없이 한 번만 만들어 재사용한다.
    /// </summary>
    internal static class UpgradeWindowLockArt
    {
        public const int CrackWidth = 52;
        public const int CrackHeight = 36;

        private static readonly Color Outline = new Color(0.05f, 0.1f, 0.13f, 1f);
        private static readonly Color Dark = new Color(0.22f, 0.3f, 0.35f, 1f);
        // 심층 썸네일 자물쇠 LockColor와 같은 금속빛.
        private static readonly Color Metal = new Color(0.5f, 0.62f, 0.68f, 1f);
        private static readonly Color Light = new Color(0.68f, 0.8f, 0.85f, 1f);
        private static readonly Color Keyhole = new Color(0.02f, 0.05f, 0.07f, 1f);

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static List<CrackPixel> crackPixels;

        public struct CrackPixel
        {
            public int X;
            public int Y;
            public float Threshold;
        }

        /// <summary>몸통 26x18. 위·왼쪽 안쪽 가장자리는 청록 빛, 가운데 열쇠 구멍.</summary>
        public static Sprite Body()
        {
            return Shape("body", 26, 18, (x, y) =>
            {
                var edgeX = x == 0 || x == 25;
                var edgeY = y == 0 || y == 17;
                if (edgeX && edgeY) return Color.clear;
                if (edgeX || edgeY) return Outline;

                var kx = x + 0.5f - 13f;
                var ky = y + 0.5f - 10.5f;
                if (kx * kx + ky * ky <= 4.8f || (x >= 12 && x <= 13 && y >= 5 && y <= 9)) return Keyhole;
                if (kx * kx + ky * ky <= 9f) return Dark;
                if (y == 16 || x == 1) return UpgradeTreeTween.Cyan;
                if (y == 1 || x == 24) return UpgradeTreeTween.CyanDim;
                if ((x == 3 || x == 22) && (y == 3 || y == 14)) return Light;
                if (y == 12) return Light;
                return Color.Lerp(Dark, Metal, (y - 1f) / 15f);
            });
        }

        /// <summary>고리 다리 5x16(좌·우 공용).</summary>
        public static Sprite ShackleLeg()
        {
            return Shape("leg", 5, 16, (x, y) =>
            {
                if (x == 0 || x == 4) return Outline;
                if (x == 1) return UpgradeTreeTween.Cyan;
                return x == 2 ? Light : Dark;
            });
        }

        /// <summary>고리 윗부분 21x5. 다리와 맞닿는 양 끝 아래는 이음새 없이 이어진다.</summary>
        public static Sprite ShackleTop()
        {
            return Shape("top", 21, 5, (x, y) =>
            {
                var edgeX = x == 0 || x == 20;
                if (edgeX && y == 4) return Color.clear;
                if (edgeX || y == 4) return Outline;
                if (y == 0) return x >= 5 && x <= 15 ? Outline : Dark;
                if (y == 3) return UpgradeTreeTween.Cyan;
                return y == 2 ? Light : Dark;
            });
        }

        /// <summary>조각 8x8. 조각마다 다른 고정 다각형이고 바깥 가장자리에 청록 빛이 남는다.</summary>
        public static Sprite Shard(int index)
        {
            var polygon = ShardPolygon(index);
            return Shape("shard-" + index, 8, 8, (x, y) =>
            {
                if (!Inside(polygon, x + 0.5f, y + 0.5f)) return Color.clear;
                var top = !Inside(polygon, x + 0.5f, y + 1.5f) || !Inside(polygon, x - 0.5f, y + 0.5f);
                var bottom = !Inside(polygon, x + 0.5f, y - 0.5f) || !Inside(polygon, x + 1.5f, y + 0.5f);
                if (top) return UpgradeTreeTween.Cyan;
                if (bottom) return Outline;
                return Color.Lerp(Dark, Metal, y / 7f);
            });
        }

        /// <summary>몸통 가운데(열쇠 구멍)에서 갈라지는 지그재그 금 4줄 + 갈래 1줄. 고정 시드라 매번 같은 모양이다.</summary>
        public static IReadOnlyList<CrackPixel> CrackPixels()
        {
            if (crackPixels != null) return crackPixels;

            var random = new System.Random(167);
            var seen = new Dictionary<int, int>();
            var pixels = new List<CrackPixel>();
            var origin = new Vector2(26f, 21f);
            float[] angles = { 28f, 148f, 212f, 332f };
            for (var k = 0; k < angles.Length; k++)
            {
                var angle = angles[k] + (float)(random.NextDouble() * 24.0 - 12.0);
                var delay = k * 0.07f;
                var path = Walk(origin, angle, 40, random);
                for (var i = 0; i < path.Count; i++)
                {
                    Add(pixels, seen, path[i], delay + (1f - delay) * i / Mathf.Max(1, path.Count - 1));
                }

                if (k == 0 && path.Count > 8)
                {
                    var branch = Walk(path[path.Count / 2], angle + 48f, 9, random);
                    for (var i = 0; i < branch.Count; i++)
                    {
                        Add(pixels, seen, branch[i], 0.5f + 0.45f * i / Mathf.Max(1, branch.Count - 1));
                    }
                }
            }

            crackPixels = pixels;
            return crackPixels;
        }

        public static Texture2D NewCrackTexture()
        {
            return new Texture2D(CrackWidth, CrackHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        /// <summary>진행값까지 보이는 금 픽셀만 칠한다. 칠한 픽셀 수를 돌려준다.</summary>
        public static int PaintCrack(Texture2D texture, float progress, Color32[] buffer)
        {
            Array.Clear(buffer, 0, buffer.Length);
            var count = 0;
            if (progress > 0f)
            {
                var list = CrackPixels();
                // 옅은 테두리를 먼저 깔고 가운데 선을 밝게 칠해 빛이 새는 틈처럼 보이게 한다.
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Threshold > progress) continue;
                    Halo(buffer, list[i].X + 1, list[i].Y);
                    Halo(buffer, list[i].X - 1, list[i].Y);
                    Halo(buffer, list[i].X, list[i].Y + 1);
                    Halo(buffer, list[i].X, list[i].Y - 1);
                }

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Threshold > progress) continue;
                    buffer[list[i].Y * CrackWidth + list[i].X] = new Color32(255, 255, 255, 255);
                    count++;
                }
            }

            texture.SetPixels32(buffer);
            texture.Apply(false, false);
            return count;
        }

        private static void Halo(Color32[] buffer, int x, int y)
        {
            if (x < 0 || y < 0 || x >= CrackWidth || y >= CrackHeight) return;
            var index = y * CrackWidth + x;
            if (buffer[index].a < 110) buffer[index] = new Color32(255, 255, 255, 110);
        }

        public static int VisibleCrackCount(float progress)
        {
            if (progress <= 0f) return 0;
            var list = CrackPixels();
            var count = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Threshold <= progress) count++;
            }

            return count;
        }

        private static List<Vector2Int> Walk(Vector2 from, float angle, int maxSteps, System.Random random)
        {
            var result = new List<Vector2Int>();
            var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var side = new Vector2(-direction.y, direction.x);
            var point = from + direction * 4.5f;
            var zig = 1f;
            for (var step = 0; step < maxSteps; step++)
            {
                if (step % 3 == 0) zig = -zig;
                point += direction + side * (zig * (0.45f + (float)random.NextDouble() * 0.5f));
                var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (cell.x < 2 || cell.y < 2 || cell.x > CrackWidth - 3 || cell.y > CrackHeight - 3) break;
                result.Add(cell);
            }

            return result;
        }

        private static void Add(List<CrackPixel> pixels, Dictionary<int, int> seen, Vector2Int cell, float threshold)
        {
            var key = cell.y * CrackWidth + cell.x;
            if (seen.TryGetValue(key, out var existing))
            {
                var pixel = pixels[existing];
                pixel.Threshold = Mathf.Min(pixel.Threshold, threshold);
                pixels[existing] = pixel;
                return;
            }

            seen[key] = pixels.Count;
            pixels.Add(new CrackPixel { X = cell.x, Y = cell.y, Threshold = Mathf.Clamp01(threshold) });
        }

        private static Vector2[] ShardPolygon(int index)
        {
            var random = new System.Random(167 + index * 31);
            var count = 5;
            var points = new Vector2[count];
            var start = (float)(random.NextDouble() * Math.PI * 2.0);
            for (var i = 0; i < count; i++)
            {
                var angle = start + i * Mathf.PI * 2f / count + (float)(random.NextDouble() * 0.6 - 0.3);
                var radius = 2.6f + (float)random.NextDouble() * 1.4f;
                points[i] = new Vector2(4f + Mathf.Cos(angle) * radius, 4f + Mathf.Sin(angle) * radius);
            }

            return points;
        }

        private static bool Inside(Vector2[] polygon, float x, float y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Sprite Shape(string key, int width, int height, Func<int, int, Color> paint)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = paint(x, y);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "UpgradeWindowLock-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
