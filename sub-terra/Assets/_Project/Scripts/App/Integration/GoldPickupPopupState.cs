using System;
using SubTerra.App.UI.Sell;
using UnityEngine;

namespace SubTerra.App.Integration
{
    [Flags]
    public enum GoldPickupPopupEvents
    {
        None = 0,
        BaseLanded = 1,
        BonusLanded = 2,
        ExitStarted = 4,
        Finished = 8
    }

    /// <summary>합치기 한 번의 결과. 합계 포화로 실제로 늘지 않은 값은 0으로 돌아온다.</summary>
    public readonly struct GoldPickupMergeResult
    {
        public int EffectiveBase { get; }
        public int EffectiveBonus { get; }
        public bool BonusRowOpened { get; }
        public bool ExitCancelled { get; }
        public long EffectiveTotal => (long)EffectiveBase + EffectiveBonus;
        public bool Changed => EffectiveBase > 0 || EffectiveBonus > 0;

        public GoldPickupMergeResult(int effectiveBase, int effectiveBonus, bool bonusRowOpened, bool exitCancelled)
        {
            EffectiveBase = effectiveBase;
            EffectiveBonus = effectiveBonus;
            BonusRowOpened = bonusRowOpened;
            ExitCancelled = exitCancelled;
        }
    }

    /// <summary>
    /// 골드 팝업 하나의 상태(순수 C#). 확정된 기본·추가 합계, 표시값 롤업, 합치기, 퇴장 예약과 취소,
    /// 추가 줄 열림을 맡는다. 시간은 unscaled 초를 Advance로 받는다. 지급·저장과는 무관하다.
    /// </summary>
    public sealed class GoldPickupPopupState
    {
        private int baseFrom;
        private float baseRollStart;
        private int bonusFrom;
        private float bonusRollStart;
        private bool baseLandFired;
        private bool bonusLandFired;
        private bool exiting;
        private float exitStartedAt;
        private bool restoring;
        private float restoreElapsed;
        private float restoreFrom;

        public float Elapsed { get; private set; }
        public int TotalBase { get; private set; }
        public int TotalBonus { get; private set; }
        public int DisplayBase { get; private set; }
        public int DisplayBonus { get; private set; }
        public bool HasBonusRow { get; private set; }
        /// <summary>처음 생성될 때부터 추가가 있었는가. 그렇지 않으면 합치기로 열린 줄이다.</summary>
        public bool BonusFromStart { get; private set; }
        public float BonusOpenedAt { get; private set; }
        public float LastMergeAt { get; private set; } = -1f;
        public int MergeCount { get; private set; }
        public bool HudLaunched { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsExiting => exiting;
        public float ExitElapsed => exiting ? Elapsed - exitStartedAt : -1f;

        public GoldPickupPopupState(int baseGold, int bonusGold)
        {
            TotalBase = Math.Max(0, baseGold);
            TotalBonus = Math.Max(0, bonusGold);
            DisplayBase = baseFrom = TotalBase;
            DisplayBonus = bonusFrom = TotalBonus;
            HasBonusRow = TotalBonus > 0;
            BonusFromStart = HasBonusRow;
        }

        public float BaseFallStart => GoldPickupPopupTimeline.BaseFallStart;

        public float BonusFallStart => BonusFromStart
            ? GoldPickupPopupTimeline.BonusFallStart
            : BonusOpenedAt + GoldPickupPopupTimeline.MergeBonusFallDelay;

        public float ExpandProgress => GoldPickupPopupTimeline.ExpandProgress(
            Elapsed, HasBonusRow, BonusFromStart, BonusOpenedAt);

        /// <summary>퇴장이 시작되는 시각. 기준 시간, 마지막 합치기 후 0.45초, 숫자 정착 중 가장 늦은 값.</summary>
        public float ExitStartTime
        {
            get
            {
                float start = GoldPickupPopupTimeline.ExitFloor(BonusFromStart);
                if (LastMergeAt >= 0f)
                {
                    start = Math.Max(start, LastMergeAt + GoldPickupPopupTimeline.MergeHoldSeconds);
                }

                return Math.Max(start, SettleTime);
            }
        }

        private float SettleTime
        {
            get
            {
                float settle = GoldPickupPopupTimeline.SettledTime(BaseFallStart);
                if (HasBonusRow)
                {
                    settle = Math.Max(settle, GoldPickupPopupTimeline.SettledTime(BonusFallStart));
                }

                if (baseFrom < TotalBase)
                {
                    settle = Math.Max(settle, baseRollStart + GoldPickupPopupTimeline.RollupSeconds);
                }

                if (bonusFrom < TotalBonus)
                {
                    settle = Math.Max(settle, bonusRollStart + GoldPickupPopupTimeline.RollupSeconds);
                }

                return settle;
            }
        }

        /// <summary>
        /// 퇴장 진행 0~1(0=완전히 떠 있음). 합치기로 퇴장이 취소되면 현재 값에서 부드럽게 0으로 돌아가
        /// 글자가 다시 내려오고 프레임이 다시 펼쳐진다.
        /// </summary>
        public float ExitProgress
        {
            get
            {
                if (restoring)
                {
                    return Mathf.Lerp(restoreFrom, 0f, RestoreK());
                }

                return exiting ? Mathf.Clamp01(ExitElapsed / GoldPickupPopupTimeline.ExitSeconds) : 0f;
            }
        }

        public void MarkHudLaunched()
        {
            HudLaunched = true;
        }

        public GoldPickupMergeResult Merge(int addBase, int addBonus)
        {
            if (IsFinished)
            {
                return default;
            }

            int newBase = Saturate(TotalBase, addBase);
            int newBonus = Saturate(TotalBonus, addBonus);
            int effectiveBase = newBase - TotalBase;
            int effectiveBonus = newBonus - TotalBonus;
            if (effectiveBase <= 0 && effectiveBonus <= 0)
            {
                return default;
            }

            if (effectiveBase > 0)
            {
                baseFrom = DisplayBase;
                baseRollStart = Elapsed;
                TotalBase = newBase;
            }

            bool opened = false;
            if (effectiveBonus > 0)
            {
                if (!HasBonusRow)
                {
                    // 추가 줄은 목표값으로 바로 떨어지고, 이후 합치기부터 롤업한다.
                    HasBonusRow = true;
                    BonusOpenedAt = Elapsed;
                    DisplayBonus = bonusFrom = newBonus;
                    opened = true;
                }
                else
                {
                    bonusFrom = DisplayBonus;
                }

                bonusRollStart = Elapsed;
                TotalBonus = newBonus;
            }

            LastMergeAt = Elapsed;
            MergeCount++;

            bool cancelled = false;
            if (exiting || restoring)
            {
                restoreFrom = ExitProgress;
                restoreElapsed = 0f;
                restoring = true;
                cancelled = exiting;
                exiting = false;
            }

            return new GoldPickupMergeResult(effectiveBase, effectiveBonus, opened, cancelled);
        }

        public GoldPickupPopupEvents Advance(float deltaSeconds)
        {
            if (IsFinished)
            {
                return GoldPickupPopupEvents.None;
            }

            Elapsed += Math.Max(0f, deltaSeconds);
            if (restoring)
            {
                restoreElapsed += Math.Max(0f, deltaSeconds);
                if (restoreElapsed >= GoldPickupPopupTimeline.RestoreSeconds)
                {
                    restoring = false;
                }
            }

            DisplayBase = GoldPickupPopupTimeline.RollValue(baseFrom, TotalBase, Elapsed - baseRollStart);
            DisplayBonus = GoldPickupPopupTimeline.RollValue(bonusFrom, TotalBonus, Elapsed - bonusRollStart);

            var events = GoldPickupPopupEvents.None;
            if (!baseLandFired && Elapsed >= GoldPickupPopupTimeline.LandTime(BaseFallStart))
            {
                baseLandFired = true;
                events |= GoldPickupPopupEvents.BaseLanded;
            }

            if (HasBonusRow && !bonusLandFired && Elapsed >= GoldPickupPopupTimeline.LandTime(BonusFallStart))
            {
                bonusLandFired = true;
                events |= GoldPickupPopupEvents.BonusLanded;
            }

            if (!exiting && Elapsed >= ExitStartTime)
            {
                exiting = true;
                exitStartedAt = ExitStartTime;
                events |= GoldPickupPopupEvents.ExitStarted;
            }

            if (exiting && Elapsed - exitStartedAt >= GoldPickupPopupTimeline.ExitSeconds)
            {
                IsFinished = true;
                events |= GoldPickupPopupEvents.Finished;
            }

            return events;
        }

        private float RestoreK()
        {
            return ResourceSellTimeline.EaseOutCubic(restoreElapsed / GoldPickupPopupTimeline.RestoreSeconds);
        }

        private static int Saturate(int current, int add)
        {
            if (add <= 0)
            {
                return current;
            }

            long sum = (long)current + add;
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }
    }
}
