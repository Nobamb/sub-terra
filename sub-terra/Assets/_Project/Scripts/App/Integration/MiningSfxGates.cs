using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared.Audio;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-164: 채굴 막힘 실패음 판정. 같은 이유가 이어지면 다시 내지 않고,
    /// 이유가 바뀌거나 채굴 키를 새로 누르거나 Failed가 아닌 단계가 오면 다시 낼 수 있다.
    /// </summary>
    public sealed class MiningFailSfxGate
    {
        private bool hasLast;
        private MiningFailureReason lastReason;

        /// <param name="messageHidden">설치 중이라 실패 문구를 숨긴 경우</param>
        public bool OnProgress(MiningPhase phase, MiningFailureReason reason, bool messageHidden)
        {
            if (phase != MiningPhase.Failed)
            {
                hasLast = false;
                return false;
            }

            var repeated = hasLast && lastReason == reason;
            hasLast = true;
            lastReason = reason;
            return !repeated && !messageHidden && IsAudible(reason);
        }

        public void OnMiningPressed()
        {
            hasLast = false;
        }

        /// <summary>"채굴할 수 없습니다"(빈 칸)·대상 바뀜·내부 오류는 무음이다.</summary>
        public static bool IsAudible(MiningFailureReason reason)
        {
            switch (reason)
            {
                case MiningFailureReason.NotMineable:
                case MiningFailureReason.DrillLevelTooLow:
                case MiningFailureReason.InsufficientEnergy:
                case MiningFailureReason.InventoryFull:
                case MiningFailureReason.OutOfRange:
                case MiningFailureReason.DeepZoneLocked:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>B-164: 블록을 캐서 자원이 화물칸에 들어올 때의 획득음. 황금 지형은 골드 팝업 소리가 맡는다.</summary>
    public static class MiningPickupSfx
    {
        public static SfxId Resolve(string mineralId, int quantity, int goldDrop)
        {
            if (string.IsNullOrEmpty(mineralId) || quantity <= 0 || goldDrop != 0)
            {
                return SfxId.None;
            }

            return DataIds.RareItems.IsRare(mineralId) ? SfxId.PickupRare : SfxId.PickupCommon;
        }
    }

    /// <summary>
    /// B-164: 화물 80% 경고음 판정. 80% 미만에서 이상으로 넘을 때 한 번, 다시 내려가면 다시 낼 수 있다.
    /// 씬 진입·불러오기의 첫 관측은 무음이다(가득 참은 채굴 실패음이 맡는다).
    /// </summary>
    public sealed class CargoHeavySfxGate
    {
        public const float Threshold = 0.8f;
        private const float Epsilon = 0.0001f;

        private bool observed;
        private bool heavy;

        public bool Observe(float loadRatio)
        {
            var nowHeavy = loadRatio >= Threshold - Epsilon;
            if (!observed)
            {
                observed = true;
                heavy = nowHeavy;
                return false;
            }

            var crossed = !heavy && nowHeavy;
            heavy = nowHeavy;
            return crossed;
        }
    }
}
