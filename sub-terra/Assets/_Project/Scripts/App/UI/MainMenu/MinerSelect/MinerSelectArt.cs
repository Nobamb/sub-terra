using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 선택 화면의 절차 생성 그림(받침대 타원·투사 빛기둥)과 홀로그램 셰이더.
    /// 새 이미지 파일 없이 코드로 만든다. 셰이더가 없으면 단색 틴트로 대체한다.
    /// </summary>
    public static class MinerSelectArt
    {
        public const string ShaderName = "SubTerra/MinerHologramUI";
        public const string ShaderResourceName = "MinerHologramUI";
        public static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static Shader cachedShader;
        private static bool shaderLookedUp;

        public static Shader HologramShader()
        {
            if (shaderLookedUp) return cachedShader;
            shaderLookedUp = true;
            try
            {
                cachedShader = Resources.Load<Shader>(ShaderResourceName);
                if (cachedShader == null) cachedShader = Shader.Find(ShaderName);
                if (cachedShader != null && !cachedShader.isSupported) cachedShader = null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SubTerra] " + ShaderName + " 셰이더를 읽지 못해 단색 틴트로 대체합니다. " + exception.Message);
                cachedShader = null;
            }

            return cachedShader;
        }

        /// <summary>받침대 윗면: 테두리가 밝은 납작한 타원.</summary>
        public static Sprite PedestalRing()
        {
            return Get("pedestal-ring", () =>
            {
                const int w = 256;
                const int h = 72;
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        var nx = (x + 0.5f) / w * 2f - 1f;
                        var ny = (y + 0.5f) / h * 2f - 1f;
                        var r = Mathf.Sqrt(nx * nx + ny * ny);
                        var ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.1f);
                        var inner = r < 0.9f ? 0.35f * (1f - r / 0.9f) + 0.12f : 0f;
                        var a = Mathf.Clamp01(ring * ring + inner);
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(w, h, pixels);
            });
        }

        /// <summary>받침대에서 위로 퍼지는 투사 빛. 아래가 밝고 위로 갈수록 사라진다.</summary>
        public static Sprite Beam()
        {
            return Get("beam", () =>
            {
                const int w = 128;
                const int h = 256;
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                {
                    var v = (y + 0.5f) / h;
                    // 위로 갈수록 조금 넓어지는 원뿔.
                    var halfWidth = Mathf.Lerp(0.62f, 0.95f, v);
                    var fade = Mathf.Pow(1f - v, 1.6f);
                    for (var x = 0; x < w; x++)
                    {
                        var nx = Mathf.Abs((x + 0.5f) / w * 2f - 1f) / halfWidth;
                        var side = Mathf.Clamp01(1f - nx);
                        side = side * side * (3f - 2f * side);
                        var a = Mathf.Clamp01(side * fade * 0.85f);
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(w, h, pixels);
            });
        }

        /// <summary>둥글게 번지는 빛(받침대 바닥 반사·선택 하이라이트).</summary>
        public static Sprite SoftDisc()
        {
            return Get("soft-disc", () =>
            {
                const int size = 64;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var nx = (x + 0.5f) / size * 2f - 1f;
                        var ny = (y + 0.5f) / size * 2f - 1f;
                        var t = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                        var a = t * t;
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(size, size, pixels);
            });
        }

        /// <summary>스프라이트가 텍스처 안에서 차지하는 UV 구역(셰이더의 아래→위 차오름 기준).</summary>
        public static Vector4 UvRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return new Vector4(0f, 0f, 1f, 1f);
            var rect = sprite.textureRect;
            var tw = Mathf.Max(1f, sprite.texture.width);
            var th = Mathf.Max(1f, sprite.texture.height);
            return new Vector4(rect.x / tw, rect.y / th, rect.width / tw, rect.height / th);
        }

        private static Sprite Get(string key, Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = create();
            sprite.name = "MinerSelect_" + key;
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Make(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
