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
    }
}
