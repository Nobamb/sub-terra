using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 조작 단말의 이미지 에셋(프레임·버튼)과 절차 생성 꺾쇠. Resources/UI/ControlTerminal 아래 스프라이트를 읽는다.
    /// 에셋이 없으면 null을 돌려주고 호출한 쪽이 기존 절차 그림으로 대신 그린다.
    /// </summary>
    internal static class ControlTerminalArt
    {
        private const string Root = "UI/ControlTerminal/";

        /// <summary>프레임 9-slice 모서리가 화면에서 차지할 디자인 px.</summary>
        public const float FrameCornerPixels = 54f;
        public const float ButtonCornerPixels = 18f;

        private static Sprite buttonNormal;
        private static Sprite buttonHover;
        private static Sprite arrowNormal;
        private static Sprite arrowHover;
        private static Sprite chevronLeft;
        private static Sprite chevronRight;

        public static Sprite Frame() => Load("control-frame");
        public static Sprite CloseIcon() => Load("control-close");
        public static Sprite ButtonNormal() => Crop(ref buttonNormal, "control-button-normal", new Rect(5f, 55f, 755f, 85f), new Vector4(32f, 24f, 32f, 24f));
        public static Sprite ButtonHover() => Crop(ref buttonHover, "control-button-hover", new Rect(5f, 55f, 755f, 85f), new Vector4(32f, 24f, 32f, 24f));
        public static Sprite ArrowNormal() => Crop(ref arrowNormal, "control-arrow-normal", new Rect(28f, 34f, 228f, 222f), new Vector4(28f, 28f, 28f, 28f));
        public static Sprite ArrowHover() => Crop(ref arrowHover, "control-arrow-hover", new Rect(28f, 34f, 228f, 222f), new Vector4(28f, 28f, 28f, 28f));

        // 원본의 투명 여백을 제외해 버튼 면과 중앙 글자/아이콘의 기준을 맞춘다.
        private static Sprite Crop(ref Sprite cached, string name, Rect rect, Vector4 border)
        {
            if (cached != null) return cached;
            var source = Load(name);
            if (source == null) return null;
            cached = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            cached.name = name + "-aligned";
            return cached;
        }

        public static Sprite Chevron(bool right)
        {
            var cached = right ? chevronRight : chevronLeft;
            if (cached != null) return cached;
            const int width = 64, height = 96, samples = 4;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = right ? "ControlChevronRight" : "ControlChevronLeft";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var color = Color.clear;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    var px = x + (sx + 0.5f) / samples;
                    if (right) px = width - px;
                    var py = y + (sy + 0.5f) / samples;
                    var dy = Mathf.Abs(py - height * 0.5f);
                    if (dy > 44f || px < 4f + dy || px > 28f + dy || px + dy > 92f) continue;
                    var fill = Color.Lerp(new Color(0.06f, 0.58f, 0.62f), new Color(0.25f, 0.95f, 0.87f), py / height);
                    if (Mathf.Min(px - 4f - dy, 28f + dy - px, 92f - px - dy) < 1.5f)
                        fill = new Color(0.64f, 1f, 0.96f);
                    color += fill / (samples * samples);
                }
                if (color.a > 0f) { color.r /= color.a; color.g /= color.a; color.b /= color.a; }
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            cached = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            cached.name = texture.name;
            if (right) chevronRight = cached; else chevronLeft = cached;
            return cached;
        }

        /// <summary>스프라이트 9-slice 경계가 목표 디자인 px로 보이도록 하는 배율.</summary>
        public static float SliceMultiplier(Sprite sprite, float cornerPixels)
        {
            if (sprite == null) return 1f;
            var border = Mathf.Max(sprite.border.x, sprite.border.y, sprite.border.z, sprite.border.w);
            if (border <= 0f || cornerPixels <= 0f) return 1f;
            return Mathf.Max(0.01f, border / cornerPixels);
        }

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>(Root + name);
        }
    }
}
