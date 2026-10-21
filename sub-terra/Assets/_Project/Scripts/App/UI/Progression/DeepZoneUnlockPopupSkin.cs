using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154)이 런타임에 쓰는 시작 브리핑 그림·폰트와 심층 깊이.
    /// PromptB154DeepZoneUnlockPopupBuilder가 채운다. 깊이는 MineLayerDistribution의 심층 띠 값이다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/DeepZoneUnlockPopupSkin";

        [Header("시작 브리핑 재사용 자원")]
        public Sprite frame;
        public Sprite buttonOff;
        public Sprite buttonOn;
        public Sprite buttonSweep;
        public Sprite buttonParticle;
        public TMP_FontAsset font;

        [Header("심층 띠 (MineLayerDistribution)")]
        public int minDepth;
        public int maxDepth;

        // 프레임 그림은 위아래 투명 여백이 커서, 시작 브리핑(1700x950)과 같은 약 1.8:1로 늘려 머리·본문·버튼 띠를 확보한다.
        [Header("레이아웃 (1920x1080 기준, 프레임 띠 중심에 맞춘 값)")]
        public Vector2 windowSize = new Vector2(560f, 310f);
        public float titleY = 80f;
        public Vector2 titleSize = new Vector2(440f, 44f);
        public float titleFontSize = 30f;
        public float bodyY = 4f;
        public Vector2 bodySize = new Vector2(460f, 44f);
        public float bodyFontSize = 22f;
        public float buttonY = -77f;
        public Vector2 buttonSize = new Vector2(184f, 44f);
        public float buttonFontSize = 22f;
        [Tooltip("등장 연출 배속. 1.6이면 시작 브리핑 1.28초를 0.8초로 줄인다.")]
        public float introSpeed = 1.6f;

        public bool IsComplete => frame != null && buttonOff != null && font != null && maxDepth >= minDepth && minDepth > 0;

        public static DeepZoneUnlockPopupSkin Load() => Resources.Load<DeepZoneUnlockPopupSkin>(ResourcePath);
    }
}
