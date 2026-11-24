using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// B-208: 성별별 플레이어 그림 묶음. 재생 속도·채굴/점프 규칙은 PlayerAnimationController 설정을 함께 쓰고
    /// 이 에셋은 그림만 바꾼다. 장수가 짧은 세트도 컨트롤러가 실제 장수로 잘라 재생한다.
    /// </summary>
    [CreateAssetMenu(menuName = "SubTerra/Player Animation Set", fileName = "PlayerAnimationSet")]
    public sealed class PlayerAnimationSet : ScriptableObject
    {
        public PlayerGender gender;
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite[] jumpReadyFrames;
        public Sprite[] jumpRiseFrames;
        public Sprite[] jumpFallFrames;
        public Sprite[] ladderFrames;
        public Sprite[] miningFrames;
        public Sprite[] ladderMiningFrames;
        public Sprite[] damageFrames;
        public Sprite[] knockoutFrames;

        // B-210: 성별 공통 이름. 비어 있으면 컨트롤러가 지금 그림으로 대신한다
        // (가스 피해 → damageFrames, 사다리 피격 → damageFrames, 발소리 → 장수 1/4·3/4 지점, 사다리 채굴 구간 → 컨트롤러 설정).
        public Sprite[] gasDamageFrames;
        public Sprite[] ladderHitFrames;
        [Tooltip("사다리 오르기 한 바퀴 중 발소리를 내는 그림 번호(1부터). 6장 이상 세트에서만 쓴다.")]
        public int[] ladderStepFrames;
        public PlayerLadderMiningFrameRanges ladderMiningRanges;
    }
}
