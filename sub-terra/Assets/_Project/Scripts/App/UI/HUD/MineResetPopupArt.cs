using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 시계 계기판·만료 알림용 절차 생성 그림: 세그먼트·브래킷, 시계 테두리·시침과 부드러운 발광.
    /// 새 이미지 파일을 늘리지 않으려고 작은 텍스처를 한 번만 만들어 재사용한다.
    /// </summary>
    internal static class MineResetPopupArt
    {
        private const int ClockSize = 256;
        private const int SoftSize = 128;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Segment(bool vertical)
        {
            return GetOrCreate(vertical ? "segment-v" : "segment-h", () =>
            {
                var texture = Build(vertical ? 4 : 20, vertical ? 20 : 4, (x, y) =>
                {
                    var along = vertical ? y : x;
                    var across = vertical ? x : y;
                    var end = Mathf.Min(along + 0.5f, 19.5f - along);
                    return Mathf.Clamp01(Mathf.Min(2f, end) - Mathf.Abs(across - 1.5f) + 0.5f);
                });
                texture.filterMode = FilterMode.Point;
                return texture;
            });
        }

        public static Sprite Bracket()
        {
            return GetOrCreate("bracket", () =>
            {
                var texture = Build(12, 12, (x, y) => x < 3 || y >= 9 ? 1f : 0f);
                texture.filterMode = FilterMode.Point;
                return texture;
            });
        }

        /// <summary>숫자·눈금 없는 원형 테두리. 흰색이라 Image.color로 청록색을 입힌다.</summary>
        public static Sprite ClockRing()
        {
            return GetOrCreate("ring", () =>
            {
                const float center = (ClockSize - 1) * 0.5f;
                const float outer = 124f;
                const float thickness = 14f;
                return Build(ClockSize, ClockSize, (x, y) =>
                {
                    var r = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var d = Mathf.Abs(r - (outer - thickness * 0.5f));
                    return Mathf.Clamp01(thickness * 0.5f - d + 0.5f);
                });
            });
        }

        /// <summary>
        /// 시침 하나. 텍스처 정중앙이 회전축(허브)이고 바늘은 12시 방향으로 뻗는다.
        /// 이미지 피벗이 정중앙이라 회전해도 시계 중심에서 벗어나지 않는다.
        /// </summary>
        public static Sprite ClockHand()
        {
            return GetOrCreate("hand", () =>
            {
                const float center = (ClockSize - 1) * 0.5f;
                const float length = 84f;
                const float halfWidth = 6.5f;
                const float hub = 15f;
                return Build(ClockSize, ClockSize, (x, y) =>
                {
                    var dx = x - center;
                    var dy = y - center;
                    var along = Mathf.Clamp(dy, 0f, length);
                    var segment = Mathf.Sqrt(dx * dx + (dy - along) * (dy - along));
                    var needle = Mathf.Clamp01(halfWidth - segment + 0.5f);
                    var disc = Mathf.Clamp01(hub - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    return Mathf.Max(needle, disc);
                });
            });
        }

        /// <summary>중심이 진하고 가장자리로 부드럽게 사라지는 원형 그라데이션. inner는 완전 불투명 반경(0~1).</summary>
        public static Sprite Soft(string name, float inner)
        {
            return GetOrCreate("soft-" + name, () =>
            {
                const float center = (SoftSize - 1) * 0.5f;
                return Build(SoftSize, SoftSize, (x, y) =>
                {
                    var nx = (x - center) / center;
                    var ny = (y - center) / center;
                    var r = Mathf.Sqrt(nx * nx + ny * ny);
                    var t = Mathf.Clamp01((r - inner) / (1f - inner));
                    return 1f - t * t * (3f - 2f * t);
                });
            });
        }

        private static Sprite GetOrCreate(string key, System.Func<Texture2D> factory)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            var texture = factory();
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = "MineResetPopupArt-" + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D Build(int width, int height, System.Func<int, int, float> alpha)
        {
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
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha(x, y) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
