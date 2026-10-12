using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 조작 단말의 이미지 에셋(프레임·버튼). Resources/UI/ControlTerminal 아래 스프라이트를 읽는다.
    /// 에셋이 없으면 null을 돌려주고 호출한 쪽이 기존 절차 그림으로 대신 그린다.
    /// </summary>
    internal static class ControlTerminalArt
    {
        private const string Root = "UI/ControlTerminal/";

        /// <summary>프레임 9-slice 모서리가 화면에서 차지할 디자인 px.</summary>
        public const float FrameCornerPixels = 54f;
        public const float ButtonCornerPixels = 18f;

        public static Sprite Frame() => Load("control-frame");
        public static Sprite ButtonNormal() => Load("control-button-normal");
        public static Sprite ButtonHover() => Load("control-button-hover");
        public static Sprite ArrowNormal() => Load("control-arrow-normal");
        public static Sprite ArrowHover() => Load("control-arrow-hover");

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
