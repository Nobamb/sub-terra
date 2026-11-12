using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>자원 아이콘 홀로그램이 쓰는 절차 생성 스프라이트. 외부 에셋 없이 한 번 만들어 공유한다(HideAndDontSave).</summary>
    public static class ResourceHologramSprites
    {
        private static Sprite pixel;
        private static Sprite radialGlow;

        /// <summary>한 변이 1 단위인 흰 사각형. 덮개·스캔 줄·반짝임을 배율로 늘려 쓴다.</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    var texture = NewTexture(4, 4);
                    var colors = new Color32[16];
                    for (var i = 0; i < colors.Length; i++) colors[i] = new Color32(255, 255, 255, 255);
                    texture.SetPixels32(colors);
                    texture.Apply(false, true);
                    pixel = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                    pixel.hideFlags = HideFlags.HideAndDontSave;
                }

                return pixel;
            }
        }

        /// <summary>가운데가 밝고 가장자리로 부드럽게 사라지는 원형 후광. 한 변이 1 단위.</summary>
        public static Sprite RadialGlow
        {
            get
            {
                if (radialGlow == null)
                {
                    const int size = 32;
                    var texture = NewTexture(size, size);
                    var colors = new Color32[size * size];
                    var half = size * 0.5f;
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var d = Mathf.Clamp01(new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half);
                            var a = (1f - d) * (1f - d);
                            colors[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                        }
                    }

                    texture.SetPixels32(colors);
                    texture.Apply(false, true);
                    radialGlow = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                    radialGlow.hideFlags = HideFlags.HideAndDontSave;
                }

                return radialGlow;
            }
        }

        private static Sprite beam;
        private static Sprite ring;

        /// <summary>
        /// 프로젝터 빔. 아래 끝은 좁고 위로 갈수록 넓어지며, 가운데가 밝고 옆이 흐려지고 가로 줄무늬가 있다.
        /// 피벗은 아래 중앙, 가로 0.5 × 세로 1 단위.
        /// </summary>
        public static Sprite Beam
        {
            get
            {
                if (beam == null)
                {
                    const int w = 32, h = 64;
                    var texture = NewTexture(w, h);
                    var colors = new Color32[w * h];
                    for (var y = 0; y < h; y++)
                    {
                        var t = y / (float)(h - 1);
                        var half = Mathf.Lerp(0.16f, 0.5f, t) * w;
                        var stripe = (y / 2) % 2 == 0 ? 1f : 0.72f;
                        for (var x = 0; x < w; x++)
                        {
                            var d = Mathf.Abs(x + 0.5f - w * 0.5f) / half;
                            var side = Mathf.Clamp01(1f - d);
                            var a = side * side * (1f - t * 0.35f) * stripe;
                            colors[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                        }
                    }

                    texture.SetPixels32(colors);
                    texture.Apply(false, true);
                    beam = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0f), 64f);
                    beam.hideFlags = HideFlags.HideAndDontSave;
                }

                return beam;
            }
        }

        /// <summary>얇고 부드러운 원형 고리. 한 변이 1 단위. 가로로 눌러 타원 바닥 링으로, 키워 충격파로 쓴다.</summary>
        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    const int size = 64;
                    var texture = NewTexture(size, size);
                    var colors = new Color32[size * size];
                    var half = size * 0.5f;
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                            var a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.1f);
                            colors[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
                        }
                    }

                    texture.SetPixels32(colors);
                    texture.Apply(false, true);
                    ring = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                    ring.hideFlags = HideFlags.HideAndDontSave;
                }

                return ring;
            }
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}
