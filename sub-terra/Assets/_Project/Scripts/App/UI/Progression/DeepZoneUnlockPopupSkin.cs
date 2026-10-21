using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154, B-154-1)이 런타임에 쓰는 그림·폰트와 심층 깊이.
    /// 창 글리치·버튼·폰트는 시작 브리핑, 카드 프레임·바탕은 광산 초기화 창 그림을 재사용하고
    /// 가운데 그림(심층 발견)과 발광 마스크는 이 창 전용이다. PromptB154DeepZoneUnlockPopupBuilder가 채운다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/DeepZoneUnlockPopupSkin";

        [Header("창 (광산 초기화 카드 프레임 재사용)")]
        public Sprite frame;
        public Sprite frameGlow;
        public Sprite panel;
        public Sprite titleDivider;
        public Color panelColor = new Color(0.02f, 0.035f, 0.05f, 0.98f);

        [Header("버튼·폰트 (시작 브리핑 재사용)")]
        public Sprite buttonOff;
        public Sprite buttonOn;
        public Sprite buttonSweep;
        public Sprite buttonParticle;
        public TMP_FontAsset font;

        [Header("심층 발견 그림 (모두 같은 크기, 발광은 따로 호흡한다)")]
        public Sprite emblem;
        public Sprite emblemBorder;
        public Sprite emblemBorderGlow;
        public Sprite[] emblemCrystalGlows;
        public Sprite emblemCoreGlow;
        public Sprite emblemRing;
        public Sprite scanLine;
        public Sprite mote;

        [Header("심층 띠 (MineLayerDistribution)")]
        public int minDepth;
        public int maxDepth;

        // 광산 초기화 알림 카드(960x736)와 같은 비율의 프레임을 조금 작게 쓴다.
        [Header("레이아웃 (1920x1080 기준)")]
        public Vector2 windowSize = new Vector2(900f, 690f);
        [Tooltip("프레임 그림에서 보이는 외곽선의 반폭·반높이 비율. 가장자리 간섭 막대 위치에 쓴다.")]
        public Vector2 frameExtentRatio = new Vector2(0.481f, 0.474f);
        public float titleY = 290f;
        public Vector2 titleSize = new Vector2(600f, 56f);
        public float titleFontSize = 40f;
        public float dividerY = 256f;
        public Vector2 dividerSize = new Vector2(560f, 15.3f);
        [Tooltip("그림 캔버스(1376x856, 2배 해상도) 비율을 유지한 표시 크기. 바깥 24px(1배 기준)은 테두리 발광 여백이다.")]
        public float emblemY = 50f;
        public Vector2 emblemSize = new Vector2(672f, 418f);
        public float bodyY = -172f;
        public Vector2 bodySize = new Vector2(700f, 46f);
        public float bodyFontSize = 32f;
        public float detailY = -211f;
        public Vector2 detailSize = new Vector2(720f, 32f);
        public float detailFontSize = 20f;
        public float buttonY = -274f;
        public Vector2 buttonSize = new Vector2(260f, 56f);
        public float buttonFontSize = 24f;
        [Tooltip("창 등장 연출 배속. 1.25면 시작 브리핑 1.28초를 약 1초로 줄인다.")]
        public float introSpeed = 1.25f;

        public bool IsComplete => frame != null && buttonOff != null && font != null && maxDepth >= minDepth && minDepth > 0;
        public bool HasArt => emblem != null && emblemBorder != null;

        public static DeepZoneUnlockPopupSkin Load() => Resources.Load<DeepZoneUnlockPopupSkin>(ResourcePath);
    }
}
