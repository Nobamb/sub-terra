using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 가스 중독 화면용 절차 생성 텍스처(알파만 사용, 색은 Image가 정한다).
    /// 비네팅: 가장자리가 노이즈로 일그러진 얼룩 경계 + 안쪽 반점. 줄무늬: 중심에서 바깥으로 퍼지는 가는 방사선.
    /// 같은 입력이면 항상 같은 결과다(해시 기반 노이즈).
    /// </summary>
    public static class GasPoisonVignetteTexture
    {
        public const int Width = 512;
        public const int Height = 288;

        /// <summary>기본 프로필(노이즈 전): 이 거리 안쪽은 투명.</summary>
        public const float InnerRadius = 0.88f;
        public const float OuterRadius = 0.96f;

        /// <summary>경계를 일그러뜨리는 최대 거리(정규화).</summary>
        public const float EdgeNoiseAmount = 0.1f;

        /// <summary>일그러진 경계가 화면 가장자리 중앙에서도 비지 않도록 거리를 밀어주는 값.</summary>
        public const float EdgeBias = 0.06f;

        /// <summary>화면 모서리까지 고르게 얇은 테두리가 되도록 둥근 사각형(p=4) 거리를 쓴다.</summary>
        public static float BoxDistance(float u, float v)
        {
            var a = u * u;
            var b = v * v;
            return Mathf.Sqrt(Mathf.Sqrt(a * a + b * b));
        }

        /// <summary>중심에서 정규화 거리(가장자리 중앙=1)에 대한 기본 프로필 알파(0~1).</summary>
        public static float AlphaAt(float distance)
        {
            var x = Mathf.Clamp01((distance - InnerRadius) / (OuterRadius - InnerRadius));
            return x * x * (3f - 2f * x);
        }

        /// <summary>노이즈로 일그러진 경계 알파. px·py는 텍스처 픽셀 좌표.</summary>
        public static float RaggedAlphaAt(float distance, float px, float py)
        {
            // 큰 얼룩 + 중간 요철 + 잔 톱니를 섞어 핏자국처럼 울퉁불퉁한 경계를 만든다.
            var blob = Fbm(px / 40f, py / 40f) - 0.5f;
            var mid = Fbm(px / 16f + 23f, py / 16f + 5f) - 0.5f;
            var jag = Fbm(px / 6f + 40f, py / 6f + 17f) - 0.5f;
            var warped = distance + EdgeBias + blob * EdgeNoiseAmount * 2f + mid * EdgeNoiseAmount * 1.2f + jag * EdgeNoiseAmount * 0.6f;

            // 또렷한 얼룩 경계 + 안쪽으로 옅게 번지는 기운
            var sharp = AlphaAt(warped);
            var soft = Mathf.Clamp01((warped - 0.5f) / 0.4f);
            var edge = Mathf.Max(sharp, soft * soft * 0.35f);

            // 경계 안쪽에 흩어진 반점
            var speckZone = Mathf.Clamp01((warped - (InnerRadius - 0.12f)) / 0.2f);
            var speck = Mathf.Clamp01((Noise(px / 5f + 91f, py / 5f + 63f) - 0.8f) / 0.1f) * speckZone * 0.85f;
            return Mathf.Clamp01(Mathf.Max(edge, speck));
        }

        /// <summary>
        /// 방사 줄무늬 알파. 각도마다 선 유무·굵기·시작 반경이 달라 들쭉날쭉하고, 중심 쪽은 투명하게 사라진다.
        /// u·v는 -1~1 정규화 좌표.
        /// </summary>
        public static float StreakAlphaAt(float u, float v)
        {
            const int bins = 140;
            var distance = Mathf.Sqrt(u * u + v * v);
            var angle01 = (Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 1f) % 1f * bins;
            var bin = Mathf.FloorToInt(angle01);
            var frac = angle01 - bin;

            var present = Hash(bin, 7);
            if (present < 0.5f)
            {
                return 0f;
            }

            var strength = Mathf.Lerp(0.45f, 1f, Hash(bin, 19));
            var halfWidth = Mathf.Lerp(0.04f, 0.16f, Hash(bin, 31));
            var line = 1f - Mathf.Clamp01((Mathf.Abs(frac - 0.5f) - halfWidth * 0.5f) / (halfWidth * 0.5f + 0.0001f));
            var start = Mathf.Lerp(0.3f, 0.65f, Hash(bin, 43));
            var radial = Mathf.Clamp01((distance - start) / (1.05f - start));
            return line * strength * radial * radial;
        }

        public static Texture2D Create()
        {
            return Build("GasPoisonVignette", (x, y, u, v) =>
                RaggedAlphaAt(BoxDistance(u, v), x, y));
        }

        public static Texture2D CreateStreaks()
        {
            return Build("GasPoisonStreaks", (x, y, u, v) => StreakAlphaAt(u, v));
        }

        private delegate float AlphaFunction(float x, float y, float u, float v);

        private static Texture2D Build(string name, AlphaFunction alphaAt)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Width * Height];
            var halfX = (Width - 1) * 0.5f;
            var halfY = (Height - 1) * 0.5f;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var u = (x - halfX) / halfX;
                    var v = (y - halfY) / halfY;
                    var alpha = Mathf.Clamp01(alphaAt(x, y, u, v));
                    pixels[y * Width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // ---------- 해시 노이즈 ----------

        private static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>값 노이즈(0~1). 격자점 해시를 부드럽게 보간한다.</summary>
        private static float Noise(float x, float y)
        {
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var fx = x - ix;
            var fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            var a = Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx);
            var b = Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float x, float y)
        {
            return Noise(x, y) * 0.6f + Noise(x * 2.1f + 5f, y * 2.1f + 9f) * 0.3f + Noise(x * 4.3f + 11f, y * 4.3f + 3f) * 0.1f;
        }
    }
}
