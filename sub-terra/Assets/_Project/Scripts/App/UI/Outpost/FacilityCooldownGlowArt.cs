using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 재사용 팝업의 빛 연출용 절차 스프라이트. 모두 흰색 + 알파라서 Image.color로 색을 입힌다.
    /// 한 번 만든 스프라이트는 캐시하고 파괴됐으면 다시 만든다.
    /// </summary>
    public static class FacilityCooldownGlowArt
    {
        private const int HaloSize = 64;
        private const int HaloBorder = 24;
        private const int VignetteSize = 64;
        private const int EdgeBandSize = 64;

        /// <summary>안쪽 띠 스프라이트의 9-slice 테두리(원본 픽셀). 화면 두께는 이 값을 배율로 나눈 값이다.</summary>
        public const float EdgeBandBorder = 24f;

        private static Sprite halo;
        private static Sprite vignette;
        private static Sprite edgeBand;

        /// <summary>
        /// 프레임 바깥으로 번지는 후광. 9-slice이며 가운데는 비운다(fillCenter 해제).
        /// 사각형 둘레에 가까울수록 밝고 바깥으로 갈수록 부드럽게 사라진다.
        /// </summary>
        public static Sprite Halo()
        {
            if (halo != null)
            {
                return halo;
            }

            var pixels = new Color32[HaloSize * HaloSize];
            for (var y = 0; y < HaloSize; y++)
            {
                for (var x = 0; x < HaloSize; x++)
                {
                    var cx = x + 0.5f;
                    var cy = y + 0.5f;
                    var dx = Mathf.Max(HaloBorder - cx, cx - (HaloSize - HaloBorder), 0f);
                    var dy = Mathf.Max(HaloBorder - cy, cy - (HaloSize - HaloBorder), 0f);
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(1f - d / HaloBorder);
                    pixels[y * HaloSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
                }
            }

            halo = CreateSprite(
                "FacilityCooldownGlowArt-halo",
                HaloSize,
                pixels,
                new Vector4(HaloBorder, HaloBorder, HaloBorder, HaloBorder));
            return halo;
        }

        /// <summary>
        /// 가운데는 투명하고 가장자리로 갈수록 밝은 안쪽 빛. 창 크기에 맞춰 늘려 쓴다.
        /// 모서리까지 이어지도록 사각에 가까운 거리(4제곱 노름)를 쓴다.
        /// </summary>
        public static Sprite Vignette()
        {
            if (vignette != null)
            {
                return vignette;
            }

            var pixels = new Color32[VignetteSize * VignetteSize];
            var center = (VignetteSize - 1) * 0.5f;
            for (var y = 0; y < VignetteSize; y++)
            {
                for (var x = 0; x < VignetteSize; x++)
                {
                    var nx = Mathf.Abs((x - center) / center);
                    var ny = Mathf.Abs((y - center) / center);
                    var d = Mathf.Pow(nx * nx * nx * nx + ny * ny * ny * ny, 0.25f);
                    var t = Mathf.Clamp01((d - 0.4f) / 0.6f);
                    var a = t * t * (3f - 2f * t);
                    pixels[y * VignetteSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            vignette = CreateSprite("FacilityCooldownGlowArt-vignette", VignetteSize, pixels, Vector4.zero);
            return vignette;
        }

        /// <summary>
        /// 창 안쪽 가장 바깥 띠. 9-slice이며 가운데는 비운다(fillCenter 해제).
        /// 가장자리에서 가장 밝고 안쪽으로 갈수록 부드럽게 사라지며, 띠 두께는 pixelsPerUnitMultiplier로 조절한다.
        /// </summary>
        public static Sprite EdgeBand()
        {
            if (edgeBand != null)
            {
                return edgeBand;
            }

            var pixels = new Color32[EdgeBandSize * EdgeBandSize];
            for (var y = 0; y < EdgeBandSize; y++)
            {
                for (var x = 0; x < EdgeBandSize; x++)
                {
                    var cx = x + 0.5f;
                    var cy = y + 0.5f;
                    var d = Mathf.Min(Mathf.Min(cx, EdgeBandSize - cx), Mathf.Min(cy, EdgeBandSize - cy));
                    var a = Mathf.Clamp01(1f - d / EdgeBandBorder);
                    pixels[y * EdgeBandSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
                }
            }

            edgeBand = CreateSprite(
                "FacilityCooldownGlowArt-edge",
                EdgeBandSize,
                pixels,
                new Vector4(EdgeBandBorder, EdgeBandBorder, EdgeBandBorder, EdgeBandBorder));
            return edgeBand;
        }

        private static Sprite CreateSprite(string name, int size, Color32[] pixels, Vector4 border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
