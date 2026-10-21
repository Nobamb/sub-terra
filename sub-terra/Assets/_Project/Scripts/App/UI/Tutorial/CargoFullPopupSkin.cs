using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 공간 부족 팝업(B-152)이 쓰는 기존 아이콘과 색. 창은 코드로 만들어지므로 Resources에 둔다.
    /// 프레임·패널·구분선·폰트는 판매창과 같은 MineResetTimedPopupSkin을 읽기만 한다.
    /// </summary>
    public sealed class CargoFullPopupSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/CargoFullPopupSkin";

        [Header("아이콘 (모두 기존 도트 그림)")]
        public Sprite warningIcon;
        public Sprite cargoIcon;
        public Sprite trashIcon;
        public Sprite copperIcon;
        public Sprite ironIcon;
        public Sprite lithiumIcon;
        public Sprite goldIcon;

        [Header("색")]
        public Color teal = new Color(0.42f, 0.94f, 1f, 1f);
        public Color amber = new Color(1f, 0.74f, 0.2f, 1f);
        public Color danger = new Color(0.95f, 0.3f, 0.3f, 1f);
        public Color textMain = new Color(0.9f, 0.97f, 1f, 1f);
        public Color textMuted = new Color(0.62f, 0.76f, 0.82f, 1f);
        public Color plate = new Color(0.025f, 0.05f, 0.07f, 0.94f);
        public Color cellEmpty = new Color(0.04f, 0.08f, 0.11f, 1f);
        public Color backdrop = new Color(0.01f, 0.015f, 0.025f, 0.55f);
    }
}
