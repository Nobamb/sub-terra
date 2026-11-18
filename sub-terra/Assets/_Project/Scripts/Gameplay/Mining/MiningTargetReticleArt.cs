using System;
using UnityEngine;

namespace SubTerra.Gameplay.Mining
{
    /// <summary>
    /// 채굴 과녁 그림을 코드로 만든다(프리팹·아트 에셋 없음). 128px = 1칸.
    /// 각진(챔퍼) 꺾쇠 + 어두운 외곽선 + 청록 광채, 다이아몬드 링 조준선, 칸 안쪽 은은한 틴트.
    /// 청록 배경에서도 묻히지 않도록 밝은 심지 + 어두운 외곽선으로 대비를 만든다.
    /// </summary>
    public static class MiningTargetReticleArt
    {
        public const float PixelsPerUnit = 128f;

        /// <summary>꺾쇠 텍스처 한 변과, 칸 모서리가 놓이는 피벗 여백(광채가 칸 밖으로 번질 자리).</summary>
        public const int CornerSize = 56;
        public const int CornerMargin = 8;
        public const int CrosshairSize = 64;
        public const int FillSize = 64;

        private const int Supersample = 4;
        private const float ArmLength = 44f;
        private const float Thickness = 9f;
        private const float Chamfer = 9f;
        private const float EndSlant = 8f;

        private static readonly Color Cyan = new Color(0.30f, 0.95f, 0.92f, 1f);
        private static readonly Color Bright = new Color(0.88f, 1f, 0.98f, 1f);
        private static readonly Color Dark = new Color(0.01f, 0.08f, 0.11f, 1f);

        /// <summary>좌하 모서리 꺾쇠. 피벗은 칸의 좌하 모서리이고, 나머지 모서리는 뒤집어 쓴다.</summary>
        public static Sprite BuildCornerSprite()
        {
            float[] mask = Coverage(CornerSize, CornerSize, CornerMargin, CornerMargin, CornerShape);
            Texture2D texture = Compose(CornerSize, CornerSize, mask, "MiningTargetReticleCorner", 5f, 1.8f, 0.85f);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, CornerSize, CornerSize),
                new Vector2(CornerMargin / (float)CornerSize, CornerMargin / (float)CornerSize),
                PixelsPerUnit);
        }

        /// <summary>한가운데 조준선: 다이아몬드 대시 링 + 바깥 눈금 네 개 + 중심점.</summary>
        public static Sprite BuildCrosshairSprite()
        {
            float c = CrosshairSize * 0.5f;
            float[] mask = Coverage(CrosshairSize, CrosshairSize, c, c, CrosshairShape);
            Texture2D texture = Compose(CrosshairSize, CrosshairSize, mask, "MiningTargetReticleCrosshair", 3.5f, 1.4f, 0.8f);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, CrosshairSize, CrosshairSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }

        /// <summary>칸 전체에 깔리는 은은한 안쪽 광채 + 얇은 테두리. 1칸 = 1 월드 단위.</summary>
        public static Sprite BuildFillSprite()
        {
            var texture = NewTexture(FillSize, FillSize, "MiningTargetReticleFill");
            var pixels = new Color32[FillSize * FillSize];
            float half = FillSize * 0.5f;
            for (int y = 0; y < FillSize; y++)
            {
                for (int x = 0; x < FillSize; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x + 0.5f, FillSize - x - 0.5f), Mathf.Min(y + 0.5f, FillSize - y - 0.5f));
                    float inner = Mathf.Clamp01(1f - edge / half);
                    float alpha = 0.04f + 0.34f * Mathf.Pow(inner, 2.6f);
                    alpha += 0.28f * Mathf.Clamp01(1.6f - edge);
                    pixels[y * FillSize + x] = ToColor32(Cyan, Mathf.Clamp01(alpha));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, FillSize, FillSize), new Vector2(0.5f, 0.5f), FillSize);
        }

        // 원점은 칸 모서리, x·y는 칸 안쪽 방향. 바깥 모서리는 깎고, 팔 끝은 비스듬히 잘라 각진 느낌을 준다.
        private static bool CornerShape(float x, float y)
        {
            bool armX = x >= 0f && x <= ArmLength && y >= 0f && y <= Thickness;
            bool armY = y >= 0f && y <= ArmLength && x >= 0f && x <= Thickness;
            bool inside = armX || armY;
            if (inside && x + y < Chamfer) inside = false;
            if (inside && armX && !armY && (ArmLength - x) < (Thickness - y) * (EndSlant / Thickness)) inside = false;
            if (inside && armY && !armX && (ArmLength - y) < (Thickness - x) * (EndSlant / Thickness)) inside = false;

            // 안쪽에 작은 다이아 포인트.
            float dot = Thickness + 10f;
            if (Mathf.Abs(x - dot) + Mathf.Abs(y - dot) <= 4.2f) inside = true;
            return inside;
        }

        // 원점은 중심.
        private static bool CrosshairShape(float x, float y)
        {
            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            if (ax + ay <= 4.4f) return true;                       // 중심점
            float diamond = ax + ay;
            if (diamond >= 11f && diamond <= 14.8f && ax >= 4f && ay >= 4f) return true; // 모서리만 남긴 다이아 링
            bool tickX = ay <= 1.7f && ax >= 17.5f && ax <= 26f;     // 바깥 눈금
            bool tickY = ax <= 1.7f && ay >= 17.5f && ay <= 26f;
            return tickX || tickY;
        }

        private static float[] Coverage(int width, int height, float originX, float originY, Func<float, float, bool> inside)
        {
            var mask = new float[width * height];
            float step = 1f / Supersample;
            float total = Supersample * Supersample;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            float px = x + (sx + 0.5f) * step - originX;
                            float py = y + (sy + 0.5f) * step - originY;
                            if (inside(px, py)) hits++;
                        }
                    }

                    mask[y * width + x] = hits / total;
                }
            }

            return mask;
        }

        // 심지(밝은 청백) → 외곽선(어두운 청록) → 광채(청록) 순으로 위에 쌓는다.
        private static Texture2D Compose(int width, int height, float[] mask, string name, float glowRadius, float outlineRadius, float glowStrength)
        {
            float[] glow = Blur(Blur(mask, width, height, Mathf.RoundToInt(glowRadius)), width, height, Mathf.RoundToInt(glowRadius));
            float[] outline = Blur(Blur(mask, width, height, Mathf.Max(1, Mathf.RoundToInt(outlineRadius))), width, height, Mathf.Max(1, Mathf.RoundToInt(outlineRadius)));
            float[] inner = Blur(mask, width, height, 1);

            var texture = NewTexture(width, height, name);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                float coreA = mask[i];
                Color coreColor = Color.Lerp(Cyan, Bright, Mathf.Clamp01((inner[i] - 0.55f) * 2.4f));
                float outlineA = Mathf.Clamp01(outline[i] * 2.8f) * 0.82f;
                float glowA = Mathf.Clamp01(glow[i] * 1.5f) * glowStrength * 0.6f;

                // 프리멀티플라이드로 over 합성한 뒤 되돌린다.
                Vector3 premultiplied = new Vector3(Cyan.r, Cyan.g, Cyan.b) * glowA;
                float alpha = glowA;
                premultiplied = new Vector3(Dark.r, Dark.g, Dark.b) * outlineA + premultiplied * (1f - outlineA);
                alpha = outlineA + alpha * (1f - outlineA);
                premultiplied = new Vector3(coreColor.r, coreColor.g, coreColor.b) * coreA + premultiplied * (1f - coreA);
                alpha = coreA + alpha * (1f - coreA);

                // 투명 칸의 색은 청록으로 둬서 쌍선형 보간 때 검은 테두리가 번지지 않게 한다.
                Color result = alpha > 0.001f
                    ? new Color(premultiplied.x / alpha, premultiplied.y / alpha, premultiplied.z / alpha, alpha)
                    : new Color(Cyan.r, Cyan.g, Cyan.b, 0f);
                pixels[i] = result;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static float[] Blur(float[] source, int width, int height, int radius)
        {
            if (radius <= 0) return source;
            var horizontal = new float[source.Length];
            float norm = 1f / (radius * 2 + 1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++)
                        sum += source[y * width + Mathf.Clamp(x + k, 0, width - 1)];
                    horizontal[y * width + x] = sum * norm;
                }
            }

            var result = new float[source.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++)
                        sum += horizontal[Mathf.Clamp(y + k, 0, height - 1) * width + x];
                    result[y * width + x] = sum * norm;
                }
            }

            return result;
        }

        private static Texture2D NewTexture(int width, int height, string name) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

        private static Color32 ToColor32(Color color, float alpha) => new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
    }
}
