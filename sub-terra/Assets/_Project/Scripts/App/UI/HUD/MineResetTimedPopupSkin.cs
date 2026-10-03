using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 3시간 만료 알림(TimedResetPopup)이 런타임에 쓰는 기존 그림 참조 모음.
    /// 팝업이 프리팹이 아니라 코드로 만들어지므로, 빌드에서도 스프라이트를 찾을 수 있게 Resources에 둔다.
    /// 새 스프라이트는 없고 '새 광산 구역' 확인창(B-123-2)이 쓰는 그림을 그대로 가리킨다.
    /// </summary>
    public sealed class MineResetTimedPopupSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/MineResetTimedPopupSkin";

        public Sprite frame;
        public Sprite frameGlow;
        public Sprite panel;
        public Sprite cave;
        public Sprite[] caveGlows;
        public Sprite hexMine;
        public Sprite hexBorder;
        public Sprite hexBorderGlow;
        public Sprite hexCrystalGlow;
        public Sprite hexTunnelGlow;
        public Sprite hexRings;
        public Sprite coreGlow;
        public Sprite scanLine;
        public Sprite titleDivider;
        public Sprite timerPlate;
        public Sprite clockIcon;
        public Sprite buttonConfirm;
        public Sprite buttonConfirmHover;
        public Sprite closeButton;
        public Sprite mote;
        public Material titleGlowMaterial;
        public TMP_FontAsset font;
    }
}
