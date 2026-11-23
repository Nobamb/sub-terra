using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 선택 화면 그림. 카드 테두리·받침대·버튼·화면 테두리는 Codex로 그린 PNG(Resources/UI/MinerSelect),
    /// 빛기둥·스캔 줄·그라데이션·입자 점은 절차 생성, 홀로그램·글리치는 셰이더다.
    /// 그림이나 셰이더가 없으면 단색·절차 그림으로 대체한다.
    /// </summary>
    public static class MinerSelectArt
    {
        public const string ShaderName = "SubTerra/MinerHologramUI";
        public const string ShaderResourceName = "MinerHologramUI";
        public const string GlitchShaderName = "SubTerra/MinerSelectGlitchUI";
        public const string GlitchShaderResourceName = "MinerSelectGlitchUI";
        public const string ArtRoot = "UI/MinerSelect/";
        /// <summary>9분할 그림의 테두리 px(빌더와 같은 값).</summary>
        public const float ButtonBorderPixels = 72f;
        public const float ButtonArtHeight = 124f;
        public const float EdgeGlowOutset = 26f;
        public static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static Shader cachedShader;
        private static bool shaderLookedUp;
        private static Shader cachedGlitchShader;
        private static bool glitchShaderLookedUp;

        public static Sprite CardFrame() => LoadArt("miner-card-frame");
        public static Sprite PedestalArt() => LoadArt("miner-pedestal");
        public static Sprite ButtonFrame() => LoadArt("miner-button-frame");
        public static Sprite ButtonFrameOff() => LoadArt("miner-button-frame-off");
        public static Sprite ScreenFrame() => LoadArt("miner-screen-frame");

        private static Sprite LoadArt(string name)
        {
            try
            {
                return Resources.Load<Sprite>(ArtRoot + name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Shader GlitchShader()
        {
            if (glitchShaderLookedUp) return cachedGlitchShader;
            glitchShaderLookedUp = true;
            try
            {
                cachedGlitchShader = Resources.Load<Shader>(GlitchShaderResourceName);
                if (cachedGlitchShader == null) cachedGlitchShader = Shader.Find(GlitchShaderName);
                if (cachedGlitchShader != null && !cachedGlitchShader.isSupported) cachedGlitchShader = null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SubTerra] " + GlitchShaderName + " 셰이더를 읽지 못해 막대 글리치로 대체합니다. " + exception.Message);
                cachedGlitchShader = null;
            }

            return cachedGlitchShader;
        }

        /// <summary>스캔 줄: 가운데가 가장 밝고 위아래로 번지는 가로 띠(양 끝도 흐려짐).</summary>
        public static Sprite ScanGlow()
        {
            return Get("scan-glow", () =>
            {
                const int w = 128;
                const int h = 32;
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                {
                    var ny = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    var core = Mathf.Exp(-ny * ny * 60f);
                    var halo = Mathf.Exp(-ny * ny * 5f) * 0.45f;
                    for (var x = 0; x < w; x++)
                    {
                        var nx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                        var ends = Mathf.Clamp01((1f - nx) / 0.25f);
                        var a = Mathf.Clamp01((core + halo) * ends);
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(w, h, pixels);
            });
        }

        /// <summary>
        /// 카드 바깥으로만 번지는 빛(9분할). 그림 가장자리에서 EdgeGlowOutset px 안쪽이 카드 테두리 자리이고,
        /// 그 안쪽은 금방 투명해져 카드 바탕을 밝히지 않는다.
        /// </summary>
        public static Sprite EdgeGlow()
        {
            return Get("edge-glow", () =>
            {
                const int size = 96;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        float d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                        float a;
                        if (d < EdgeGlowOutset)
                        {
                            var k = d / EdgeGlowOutset;
                            a = k * k * 0.9f;
                        }
                        else
                        {
                            a = 0.9f * Mathf.Exp(-(d - EdgeGlowOutset) / 4f);
                        }

                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                    }
                }

                return Make(size, size, pixels, new Vector4(40f, 40f, 40f, 40f));
            });
        }

        /// <summary>카드 안쪽 바탕: 아래가 조금 진하고 위로 옅어지는 세로 그라데이션.</summary>
        public static Sprite VerticalFade()
        {
            return Get("vertical-fade", () =>
            {
                const int w = 4;
                const int h = 128;
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                {
                    var v = (y + 0.5f) / h;
                    var a = Mathf.Lerp(1f, 0.55f, v);
                    for (var x = 0; x < w; x++)
                    {
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(w, h, pixels);
            });
        }

        /// <summary>버튼 위를 지나가는 비스듬한 빛줄기.</summary>
        public static Sprite Shine()
        {
            return Get("shine", () =>
            {
                const int w = 64;
                const int h = 64;
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        var nx = (x + 0.5f) / w * 2f - 1f;
                        var ny = (y + 0.5f) / h * 2f - 1f;
                        var d = Mathf.Abs(nx - ny * 0.45f);
                        var a = Mathf.Clamp01(1f - d / 0.5f);
                        a = a * a * (1f - Mathf.Abs(ny) * 0.3f);
                        pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                return Make(w, h, pixels);
            });
        }

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
            return Make(width, height, pixels, Vector4.zero);
        }

        private static Sprite Make(int width, int height, Color32[] pixels, Vector4 border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
