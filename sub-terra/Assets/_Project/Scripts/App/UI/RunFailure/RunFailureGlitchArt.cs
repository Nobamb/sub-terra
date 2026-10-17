using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.RunFailure
{
    /// <summary>
    /// 런 실패(체력 소진·가스 중독) 팝업과 화면 전환 글리치 연출에 사용하는 절차적 스프라이트 생성기.
    /// 모든 흰색 마스크 스프라이트는 Image.color로 원인별 색상(적색/녹색)을 입힌다.
    /// </summary>
    public static class RunFailureGlitchArt
    {
        private const int AntiAliasSamples = 3;
        private const int HaloSize = 64;
        private const int HaloBorder = 24;
        private const int VignetteSize = 64;
        private const float ChamferSize = 7f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static Sprite scanlineSprite;
        private static Sprite crtBeamSprite;
        private static Sprite fallbackMineralSprite;
        private static Sprite softGlowSprite;
        private static Sprite haloSprite;
        private static Sprite vignetteSprite;
        private static Sprite hexTileSprite;
        private static Sprite chamferFillSprite;
        private static Sprite chamferOutlineSprite;
        private static Sprite frameNeonBorderSprite;
        private static Sprite glitchBlockSprite;
        private static Sprite diamondSprite;

        public static Sprite GetScanlineSprite()
        {
            if (scanlineSprite != null)
            {
                return scanlineSprite;
            }

            const int width = 32;
            const int height = 32;
            var texture = CreateTexture("RunFailureScanlines", width, height, FilterMode.Point, TextureWrapMode.Repeat);
            for (var y = 0; y < height; y++)
            {
                var alpha = (y % 4 == 0) ? 0.58f : (y % 2 == 0 ? 0.20f : 0.03f);
                var rowColor = new Color(1f, 1f, 1f, alpha);
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, rowColor);
                }
            }

            texture.Apply(false, false);
            scanlineSprite = CreateSprite(texture, Vector4.zero);
            return scanlineSprite;
        }

        public static Sprite GetCrtBeamSprite()
        {
            if (crtBeamSprite != null)
            {
                return crtBeamSprite;
            }

            const int width = 96;
            const int height = 24;
            var texture = CreateTexture("RunFailureCrtBeam", width, height, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var centerY = (height - 1) * 0.5f;
            var centerX = (width - 1) * 0.5f;
            for (var y = 0; y < height; y++)
            {
                var dy = Mathf.Abs(y - centerY) / centerY;
                var core = Mathf.Pow(Mathf.Clamp01(1f - dy), 3.2f);
                var halo = Mathf.Pow(Mathf.Clamp01(1f - dy), 1.35f) * 0.45f;
                var vertical = Mathf.Clamp01(core + halo);
                for (var x = 0; x < width; x++)
                {
                    var dx = Mathf.Abs(x - centerX) / centerX;
                    var horizontalEdge = Mathf.Clamp01(1f - Mathf.Pow(dx, 3.6f));
                    var alpha = Mathf.Clamp01(vertical * horizontalEdge);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            crtBeamSprite = CreateSprite(texture, Vector4.zero);
            return crtBeamSprite;
        }

        public static Sprite GetFallbackMineralSprite()
        {
            if (fallbackMineralSprite != null)
            {
                return fallbackMineralSprite;
            }

            const int size = 24;
            var texture = CreateTexture("RunFailureFallbackMineral", size, size, FilterMode.Point, TextureWrapMode.Clamp);
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var diamond = (Mathf.Abs(x - center) + Mathf.Abs(y - center)) / (size * 0.42f);
                    if (diamond > 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var highlight = (x < center && y > center) ? 1f : 0.78f;
                    var border = diamond > 0.82f ? 0.55f : highlight;
                    texture.SetPixel(x, y, new Color(border, border * 0.92f, border * 0.85f, 1f));
                }
            }

            texture.Apply(false, false);
            fallbackMineralSprite = CreateSprite(texture, Vector4.zero);
            return fallbackMineralSprite;
        }

        /// <summary>중심이 진하고 가장자리로 부드럽게 퍼지는 원형 발광 스프라이트.</summary>
        public static Sprite GetSoftGlowSprite()
        {
            if (softGlowSprite != null)
            {
                return softGlowSprite;
            }

            const int size = 96;
            const float center = 47.5f;
            var texture = CreateTexture("RunFailureSoftGlow", size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x - center) / center;
                    var ny = (y - center) / center;
                    var r = Mathf.Sqrt((nx * nx) + (ny * ny));
                    var t = Mathf.Clamp01((r - 0.04f) / 0.96f);
                    var a = 1f - (t * t * (3f - (2f * t)));
                    pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            softGlowSprite = CreateSprite(texture, Vector4.zero);
            return softGlowSprite;
        }

        /// <summary>팝업 카드 바깥 둘레로 번지는 9-slice 후광.</summary>
        public static Sprite GetHaloSprite()
        {
            if (haloSprite != null)
            {
                return haloSprite;
            }

            var texture = CreateTexture("RunFailureHalo", HaloSize, HaloSize, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var pixels = new Color32[HaloSize * HaloSize];
            for (var y = 0; y < HaloSize; y++)
            {
                for (var x = 0; x < HaloSize; x++)
                {
                    var cx = x + 0.5f;
                    var cy = y + 0.5f;
                    var dx = Mathf.Max(HaloBorder - cx, cx - (HaloSize - HaloBorder), 0f);
                    var dy = Mathf.Max(HaloBorder - cy, cy - (HaloSize - HaloBorder), 0f);
                    var d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var a = Mathf.Clamp01(1f - (d / HaloBorder));
                    pixels[(y * HaloSize) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            haloSprite = CreateSprite(texture, new Vector4(HaloBorder, HaloBorder, HaloBorder, HaloBorder));
            return haloSprite;
        }

        /// <summary>가운데는 투명하고 가장자리로 갈수록 진해지는 비네트.</summary>
        public static Sprite GetVignetteSprite()
        {
            if (vignetteSprite != null)
            {
                return vignetteSprite;
            }

            var texture = CreateTexture("RunFailureVignette", VignetteSize, VignetteSize, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var pixels = new Color32[VignetteSize * VignetteSize];
            var center = (VignetteSize - 1) * 0.5f;
            for (var y = 0; y < VignetteSize; y++)
            {
                for (var x = 0; x < VignetteSize; x++)
                {
                    var nx = Mathf.Abs((x - center) / center);
                    var ny = Mathf.Abs((y - center) / center);
                    var d = Mathf.Pow((nx * nx * nx * nx) + (ny * ny * ny * ny), 0.25f);
                    var t = Mathf.Clamp01((d - 0.38f) / 0.62f);
                    var a = t * t * (3f - (2f * t));
                    pixels[(y * VignetteSize) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            vignetteSprite = CreateSprite(texture, Vector4.zero);
            return vignetteSprite;
        }

        /// <summary>배경에 은은하게 깔리는 타일형 육각형 그리드.</summary>
        public static Sprite GetHexTileSprite()
        {
            if (hexTileSprite != null)
            {
                return hexTileSprite;
            }

            const int width = 80;
            const int height = 46;
            var texture = CreateTexture("RunFailureHexTile", width, height, FilterMode.Bilinear, TextureWrapMode.Repeat);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var best = float.MaxValue;
                    var second = float.MaxValue;
                    for (var gx = -1; gx <= 2; gx++)
                    {
                        for (var gy = -1; gy <= 2; gy++)
                        {
                            ConsiderHexDistance(x - (gx * 80f), y - (gy * 46f), ref best, ref second);
                            ConsiderHexDistance(x - (gx * 80f) - 40f, y - (gy * 46f) - 23f, ref best, ref second);
                        }
                    }

                    var edge = second - best;
                    var a = 1f - Mathf.Clamp01((edge - 0.6f) / 1.4f);
                    pixels[(y * width) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            hexTileSprite = CreateSprite(texture, Vector4.zero);
            return hexTileSprite;
        }

        /// <summary>모서리가 비스듬히 깎인 9-slice 행/배지 배경 판.</summary>
        public static Sprite GetChamferFillSprite()
        {
            if (chamferFillSprite != null)
            {
                return chamferFillSprite;
            }

            chamferFillSprite = CreateShapeSprite(
                "RunFailureChamferFill",
                32,
                32,
                new Vector4(10f, 10f, 10f, 10f),
                (x, y) => InsideChamfer(x, y, 32f, 0f));
            return chamferFillSprite;
        }

        /// <summary>모서리가 비스듬히 깎인 9-slice 테두리.</summary>
        public static Sprite GetChamferOutlineSprite()
        {
            if (chamferOutlineSprite != null)
            {
                return chamferOutlineSprite;
            }

            chamferOutlineSprite = CreateShapeSprite(
                "RunFailureChamferOutline",
                32,
                32,
                new Vector4(10f, 10f, 10f, 10f),
                (x, y) => InsideChamfer(x, y, 32f, 0f) && !InsideChamfer(x, y, 32f, 1.8f));
            return chamferOutlineSprite;
        }

        /// <summary>
        /// 전력 고갈 팝업 프레임(mine-reset-frame)의 4면 대칭 모서리 각도에 맞춰
        /// 원인별 경고색(적색/녹색) 네온 테두리와 코너 브래킷을 입히는 9-slice 오버레이 스프라이트.
        /// </summary>
        public static Sprite GetFrameNeonBorderSprite()
        {
            if (frameNeonBorderSprite != null)
            {
                return frameNeonBorderSprite;
            }

            const int size = 96;
            const float border = 28f;
            const float cornerCut = 14f;
            var texture = CreateTexture("RunFailureFrameNeonBorder", size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var dx = Mathf.Min(px, size - px);
                    var dy = Mathf.Min(py, size - py);
                    var cornerSum = dx + dy;

                    if (cornerSum < cornerCut - 1.5f)
                    {
                        pixels[(y * size) + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var edgeDist = Mathf.Min(Mathf.Min(dx, dy) - 2.5f, (cornerSum - cornerCut) * 0.7071f);
                    if (edgeDist < -2f)
                    {
                        pixels[(y * size) + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // 외곽 네온 코어 라인 + 부드러운 안쪽 번짐
                    var lineCore = Mathf.Clamp01(1f - (Mathf.Abs(edgeDist - 1.2f) / 1.8f));
                    var innerGlow = edgeDist >= 0f ? Mathf.Pow(Mathf.Clamp01(1f - (edgeDist / 11f)), 2.2f) * 0.48f : 0f;

                    // 모서리 브래킷 강조
                    var inCornerZone = dx < 24f && dy < 24f;
                    var cornerAccent = inCornerZone
                        ? Mathf.Clamp01(1f - (Mathf.Abs(edgeDist - 4.2f) / 1.4f)) * 0.75f
                        : 0f;

                    var alpha = Mathf.Clamp01(lineCore + innerGlow + cornerAccent);
                    pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            frameNeonBorderSprite = CreateSprite(texture, new Vector4(border, border, border, border));
            return frameNeonBorderSprite;
        }

        /// <summary>
        /// 디지털 노이즈·신호 깨짐 패턴이 들어간 글리치 바 스프라이트.
        /// 단색 사각형 대신 실제 홀로그램 데이터 손상 띠 느낌을 낸다.
        /// </summary>
        public static Sprite GetGlitchBlockSprite()
        {
            if (glitchBlockSprite != null)
            {
                return glitchBlockSprite;
            }

            const int width = 128;
            const int height = 16;
            var texture = CreateTexture("RunFailureGlitchBlock", width, height, FilterMode.Point, TextureWrapMode.Repeat);
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            {
                var scanFactor = (y % 2 == 0) ? 1f : 0.72f;
                for (var x = 0; x < width; x++)
                {
                    var cellX = x / 8;
                    var cellY = y / 4;
                    var h = Hash01((cellX * 37) + (cellY * 131) + 19);
                    var subH = Hash01((x * 17) + (y * 53) + 7);
                    var edgeFade = 1f;
                    if (x < 6)
                    {
                        edgeFade = x / 6f;
                    }
                    else if (x > width - 7)
                    {
                        edgeFade = (width - 1 - x) / 6f;
                    }

                    var alpha = h < 0.14f
                        ? 0.18f
                        : (h > 0.78f ? 1f : 0.62f + (0.30f * subH));
                    alpha = Mathf.Clamp01(alpha * scanFactor * edgeFade);
                    pixels[(y * width) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            glitchBlockSprite = CreateSprite(texture, Vector4.zero);
            return glitchBlockSprite;
        }

        /// <summary>경고 다이아몬드(마름모) 아이콘 스프라이트.</summary>
        public static Sprite GetDiamondSprite()
        {
            if (diamondSprite != null)
            {
                return diamondSprite;
            }

            diamondSprite = CreateShapeSprite(
                "RunFailureDiamond",
                28,
                28,
                Vector4.zero,
                (x, y) =>
                {
                    const float c = 14f;
                    var manhattan = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                    return manhattan <= 11.5f;
                });
            return diamondSprite;
        }

        private static float Hash01(int seed)
        {
            unchecked
            {
                var h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static void ConsiderHexDistance(float dx, float dy, ref float best, ref float second)
        {
            var d = Mathf.Sqrt((dx * dx) + (dy * dy));
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

            var c = ChamferSize - (inset * 0.4f);
            if ((x - lo) + (hi - y) < c)
            {
                return false;
            }

            if ((hi - x) + (hi - y) < c)
            {
                return false;
            }

            if ((x - lo) + (y - lo) < c)
            {
                return false;
            }

            return (hi - x) + (y - lo) >= c;
        }

        private static Sprite CreateShapeSprite(
            string key,
            int width,
            int height,
            Vector4 border,
            Func<float, float, bool> inside)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = CreateTexture(key, width, height, FilterMode.Bilinear, TextureWrapMode.Clamp);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var hits = 0;
                    for (var sy = 0; sy < AntiAliasSamples; sy++)
                    {
                        for (var sx = 0; sx < AntiAliasSamples; sx++)
                        {
                            var px = x + ((sx + 0.5f) / AntiAliasSamples);
                            var py = y + ((sy + 0.5f) / AntiAliasSamples);
                            if (inside(px, py))
                            {
                                hits++;
                            }
                        }
                    }

                    var a = hits / (float)(AntiAliasSamples * AntiAliasSamples);
                    pixels[(y * width) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            var sprite = CreateSprite(texture, border);
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D CreateTexture(
            string name,
            int width,
            int height,
            FilterMode filterMode,
            TextureWrapMode wrapMode)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filterMode,
                wrapMode = wrapMode,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite CreateSprite(Texture2D texture, Vector4 border)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name + "_Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
