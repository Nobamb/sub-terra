using System;
using UnityEngine;

namespace SubTerra.App.UI.GameplayCursor
{
    /// <summary>드릴 가속·감속·떨림·호버 빛 세기. 인스펙터에서 조절한다.</summary>
    [Serializable]
    public sealed class GameplayCursorTuning
    {
        [Tooltip("좌클릭 시작 속도(0~1). 0.3이면 3~4번 칸을 오가며 시작한다.")]
        [Range(0f, 1f)] public float startSpeed = 0.3f;
        [Tooltip("누르고 있을 때 초당 속도 증가량. 1/값 초 만에 최고속(8번)에 닿는다.")]
        public float spinUpPerSecond = 0.45f;
        [Tooltip("뗐을 때 초당 속도 감소량. 작을수록 천천히 1번으로 내려온다.")]
        public float spinDownPerSecond = 0.6f;
        [Tooltip("최저·최고 속도에서의 회전 프레임 전환 빈도(초당).")]
        public float minFlickerFps = 9f;
        public float maxFlickerFps = 30f;
        [Tooltip("누르고 있을 때 그림 떨림 최대 폭(px). 핫스팟은 흔들리지 않는다.")]
        [Range(0f, 3f)] public float jitterPixels = 1.2f;
        public float jitterFrequency = 38f;
        [Tooltip("드릴↔집게 전환 펄스 길이와 크기.")]
        public float swapPulseSeconds = 0.12f;
        public float swapPulseScale = 1.14f;
        [Tooltip("잡기 가능 호버 시 끝 빛이 켜지고 꺼지는 속도(초당).")]
        public float glowFadePerSecond = 8f;
        public Color glowColor = new Color(0.247f, 0.878f, 0.816f, 1f);
        [Range(0f, 1f)] public float glowAlpha = 0.85f;
        public float glowSize = 22f;
        [Tooltip("호버 파티클 초당 개수와 크기(px).")]
        public float particlesPerSecond = 9f;
        public Vector2 particleSize = new Vector2(2.5f, 4.5f);
    }

    /// <summary>
    /// 인게임 커서 스프라이트(드릴 8칸·집게 4칸)와 표시 크기·연출 값.
    /// 커서는 코드로 만들어지므로 빌드에서도 찾을 수 있게 Resources에 둔다(B-151).
    /// </summary>
    public sealed class GameplayCursorSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/GameplayCursorSkin";
        public const int DrillFrameCount = 8;
        public const int ClawFrameCount = 4;

        public Sprite[] drillFrames = new Sprite[DrillFrameCount];
        public Sprite[] clawFrames = new Sprite[ClawFrameCount];

        [Tooltip("시트 한 칸을 그리는 크기(px, 1920x1080 기준). 칸 여백을 빼면 드릴 그림은 약 41px.")]
        public float drillCellSize = 48f;
        [Tooltip("집게 칸은 여백이 더 커서 드릴과 비슷한 크기로 보이게 조금 크게 둔다.")]
        public float clawCellSize = 55f;

        public GameplayCursorTuning tuning = new GameplayCursorTuning();

        public static GameplayCursorSkin Load()
        {
            return Resources.Load<GameplayCursorSkin>(ResourcePath);
        }
    }
}
