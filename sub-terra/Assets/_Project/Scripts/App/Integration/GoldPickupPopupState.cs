using System;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 팝업 하나의 상태(순수 C#, prompt-B 142).
    /// 확정된 기본·추가 합계, 롤업 표시값, 추가 줄 확장, 퇴장 예약·취소를 맡는다.
    /// 실제 골드 지급과 무관하며, 받은 확정값의 합만 표시한다.
    /// </summary>
    public sealed class GoldPickupPopupState
    {
        private readonly Row baseRow = new Row();
        private readonly Row bonusRow = new Row();
        private float bonusStart;
        private float bonusFallStart;
        private bool baseLandingTaken;
        private bool bonusLandingTaken;
        private bool restoring;
        private float restoreFrom;
        private float restoreClock;
        private float exitClock;

        public float Age { get; private set; }
        public int MergeCount { get; private set; }
        public float LastMergeAge { get; private set; }
        public bool HasBonus { get; private set; }
        public bool Exiting { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>퇴장 진행 0~1. 합치기로 복원되는 동안은 현재 값에서 0으로 부드럽게 줄어든다.</summary>
        public float ExitAmount { get; private set; }
        /// <summary>HUD 비행은 팝업당 한 번만 시작한다. 퇴장이 취소돼도 되돌리지 않는다.</summary>
        public bool HudSequenceStarted { get; private set; }
        public float HudClock { get; private set; }

        public int BaseTotal => baseRow.Target;
        public int BonusTotal => bonusRow.Target;
        public int BaseDisplay => baseRow.Display(Age);
        public int BonusDisplay => bonusRow.Display(Age);
        public float BaseFallTime => Age - GoldPickupPopupTimeline.BaseFallStart;
        public float BonusFallTime => Age - bonusFallStart;
        public float BonusStart => bonusStart;
        public float FrameHeight => GoldPickupPopupTimeline.FrameHeight(HasBonus, Age - bonusStart);
        public float LeverAngle => GoldPickupPopupTimeline.LeverAngle(Age);

        public GoldPickupPopupState(int baseGold, int bonusGold)
        {
            baseRow.Set(Math.Max(0, baseGold));
            int bonus = Math.Max(0, bonusGold);
            bonusRow.Set(bonus);
            if (bonus > 0)
            {
                HasBonus = true;
                bonusStart = 0f;
                bonusFallStart = GoldPickupPopupTimeline.BonusFallStart;
            }
        }

        /// <summary>추가 줄의 낙하가 끝나는 시각. 추가가 없으면 기본 줄 기준이다.</summary>
        public float LandingEnd
        {
            get
            {
                float landed = GoldPickupPopupTimeline.BaseFallEnd;
                if (HasBonus)
                {
                    landed = Math.Max(landed, bonusFallStart + GoldPickupPopupTimeline.FallDuration);
                }

                return landed;
            }
        }

        /// <summary>퇴장 시작 시각. 마지막 합치기 후 최소 대기와 숫자 정착 이후로 미룬다.</summary>
        public float ExitStartAge
        {
            get
            {
                float start = LandingEnd + GoldPickupPopupTimeline.ExitLead;
                if (MergeCount > 0)
                {
                    start = Math.Max(start, LastMergeAge + GoldPickupPopupTimeline.MergeHold);
                }

                return start;
            }
        }

        /// <summary>
        /// 확정값을 현재 팝업에 합산하고 유효 증가분(기본+추가)을 돌려준다.
        /// 합산은 int 상한에서 멈추며, 증가분이 0이면 아무것도 바꾸지 않는다.
        /// </summary>
        public int Merge(int baseGold, int bonusGold)
        {
            if (Finished)
            {
                return 0;
            }

            int addBase = Math.Max(0, baseGold);
            int addBonus = Math.Max(0, bonusGold);
            int newBase = SaturatingAdd(baseRow.Target, addBase);
            int newBonus = SaturatingAdd(bonusRow.Target, addBonus);
            int incBase = newBase - baseRow.Target;
            int incBonus = newBonus - bonusRow.Target;
            if (incBase <= 0 && incBonus <= 0)
            {
                return 0;
            }

            if (incBase > 0)
            {
                baseRow.Roll(Age, newBase);
            }

            if (incBonus > 0)
            {
                if (!HasBonus)
                {
                    // 추가 최초 발생: 높이 확장과 추가 줄 낙하는 한 번만 수행한다.
                    HasBonus = true;
                    bonusStart = Age;
                    bonusFallStart = Age + GoldPickupPopupTimeline.LateBonusFallDelay;
                    bonusLandingTaken = false;
                    bonusRow.Set(newBonus);
                }
                else
                {
                    bonusRow.Roll(Age, newBonus);
                }
            }

            MergeCount++;
            LastMergeAge = Age;
            CancelExit();
            return SaturatingAdd(incBase, incBonus);
        }

        public bool TakeBaseLanding()
        {
            if (baseLandingTaken || Age < GoldPickupPopupTimeline.BaseFallEnd)
            {
                return false;
            }

            baseLandingTaken = true;
            return true;
        }

        public bool TakeBonusLanding()
        {
            if (!HasBonus || bonusLandingTaken
                || Age < bonusFallStart + GoldPickupPopupTimeline.FallDuration)
            {
                return false;
            }

            bonusLandingTaken = true;
            return true;
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Finished)
            {
                return;
            }

            float previous = Age;
            bool hudWasStarted = HudSequenceStarted;
            Age += deltaSeconds;

            if (restoring)
            {
                restoreClock += deltaSeconds;
                float k = restoreClock / GoldPickupPopupTimeline.MergeRestoreDuration;
                if (k >= 1f)
                {
                    restoring = false;
                    ExitAmount = 0f;
                }
                else
                {
                    ExitAmount = restoreFrom * (1f - GoldPickupPopupTimeline.EaseOutCubic(k));
                }
            }
            else if (!Exiting && Age >= ExitStartAge)
            {
                Exiting = true;
                exitClock = Age - Math.Max(previous, ExitStartAge);
                if (!HudSequenceStarted)
                {
                    HudSequenceStarted = true;
                    HudClock = exitClock;
                }
            }
            else if (Exiting)
            {
                exitClock += deltaSeconds;
            }

            if (Exiting)
            {
                ExitAmount = Math.Min(1f, exitClock / GoldPickupPopupTimeline.ExitDuration);
                if (exitClock >= GoldPickupPopupTimeline.ExitDuration)
                {
                    Finished = true;
                }
            }

            // HUD 시퀀스는 처음 시작된 뒤 퇴장 취소·재개와 무관하게 계속 흐른다.
            if (hudWasStarted)
            {
                HudClock += deltaSeconds;
            }
        }

        private void CancelExit()
        {
            if (ExitAmount > 0f)
            {
                restoring = true;
                restoreFrom = ExitAmount;
                restoreClock = 0f;
            }

            Exiting = false;
            exitClock = 0f;
        }

        private static int SaturatingAdd(int a, int b)
        {
            long sum = (long)a + b;
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        private sealed class Row
        {
            public int From;
            public int Target;
            public float RollStart;

            public void Set(int value)
            {
                From = value;
                Target = value;
                RollStart = 0f;
            }

            public void Roll(float age, int newTarget)
            {
                // 진행 중인 롤업도 현재 표시값에서 이어 간다.
                From = Display(age);
                Target = newTarget;
                RollStart = age;
            }

            public int Display(float age)
            {
                if (Target <= From)
                {
                    return Target;
                }

                float k = (age - RollStart) / GoldPickupPopupTimeline.MergeRollDuration;
                if (k >= 1f)
                {
                    return Target;
                }

                if (k <= 0f)
                {
                    return From;
                }

                double eased = GoldPickupPopupTimeline.EaseOutCubic(k);
                long value = From + (long)Math.Floor((Target - (double)From) * eased);
                return value >= Target ? Target : (int)value;
            }
        }
    }
}
