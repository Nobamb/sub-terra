using System;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 팝업 효과음(B-160). 레버는 팝업 이벤트에 한 번, 숫자 착지는 기본·추가 높이를 다르게,
    /// 금화 분출(착지·합치기)은 0.25초 간격으로 묶어 연속 채굴에도 쌓이지 않게 한다. 퇴장·HUD 입자는 무음.
    /// </summary>
    public sealed class GoldPickupSfx
    {
        public const float CoinInterval = 0.25f;
        public const float ReelInterval = 0.12f;
        public const float BonusReelPitch = 1.1f;

        private readonly SfxThrottle coin;
        private readonly SfxThrottle reel;

        public GoldPickupSfx(Func<float> clock = null)
        {
            coin = new SfxThrottle(CoinInterval, clock);
            reel = new SfxThrottle(ReelInterval, clock);
        }

        public void Handle(GoldPickupPopupEvents events)
        {
            if ((events & GoldPickupPopupEvents.LeverPulled) != 0)
            {
                SfxPlayer.Play(SfxId.GoldLever);
            }

            var baseLanded = (events & GoldPickupPopupEvents.BaseLanded) != 0;
            var bonusLanded = (events & GoldPickupPopupEvents.BonusLanded) != 0;
            if (!baseLanded && !bonusLanded)
            {
                return;
            }

            // 같은 프레임에 둘 다 착지하면 기본 높이로 한 번만 낸다.
            if (reel.TryPass())
            {
                SfxPlayer.Play(SfxId.GoldReelStop, baseLanded ? 1f : BonusReelPitch);
            }

            PlayCoins();
        }

        public void OnMerged(GoldPickupMergeResult result)
        {
            if (result.Changed)
            {
                PlayCoins();
            }
        }

        private void PlayCoins()
        {
            if (coin.TryPass())
            {
                SfxPlayer.Play(SfxId.GoldCoinPop);
            }
        }
    }
}
