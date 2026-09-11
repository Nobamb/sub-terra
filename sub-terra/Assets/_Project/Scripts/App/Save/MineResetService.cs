using System;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public interface IMineResetSeedSource
    {
        long NextSeed();
    }

    public sealed class UtcMineResetSeedSource : IMineResetSeedSource
    {
        private long lastSeed;

        public long NextSeed()
        {
            var seed = DateTime.UtcNow.Ticks;
            if (seed <= 0)
            {
                seed = 1;
            }

            if (seed <= lastSeed)
            {
                seed = lastSeed == long.MaxValue ? 1 : lastSeed + 1;
            }

            lastSeed = seed;
            return seed;
        }
    }

    public enum MineResetStatus
    {
        Success = 0,
        InvalidState = 1,
        InsufficientGold = 2,
        SeedFailed = 3
    }

    public readonly struct MineResetResult
    {
        public MineResetStatus Status { get; }
        public long PreviousSeed { get; }
        public long NewSeed { get; }
        public int RemainingGold { get; }
        public int FeeCharged { get; }
        public bool TimedReset { get; }

        public MineResetResult(
            MineResetStatus status,
            long previousSeed,
            long newSeed,
            int remainingGold,
            int feeCharged = 0,
            bool timedReset = false)
        {
            Status = status;
            PreviousSeed = previousSeed;
            NewSeed = newSeed;
            RemainingGold = remainingGold;
            FeeCharged = feeCharged;
            TimedReset = timedReset;
        }
    }

    /// <summary>
    /// 광산 초기화 규칙.
    /// 유료 초기화는 500G부터 시작해 성공할 때마다 2배, 3시간 자동 초기화는 무료이며 비용을 500G로 되돌린다.
    /// 두 경로 모두 3시간 타이머를 처음부터 다시 센다.
    /// </summary>
    public static class MineResetService
    {
        public const int BaseFeeGold = 500;
        public const int FeeGold = BaseFeeGold;
        public const double CycleDurationSeconds = 3d * 60d * 60d;
        private const int MaximumSeedAttempts = 8;
        private const int MaximumPaidDoubling = 22;

        public static int GetFeeGold(int paidResetCount)
        {
            var count = paidResetCount < 0 ? 0 : paidResetCount;
            if (count == 0)
            {
                return BaseFeeGold;
            }

            if (count >= MaximumPaidDoubling)
            {
                return int.MaxValue;
            }

            var fee = BaseFeeGold;
            for (var i = 0; i < count; i++)
            {
                if (fee > int.MaxValue / 2)
                {
                    return int.MaxValue;
                }

                fee *= 2;
            }

            return fee;
        }

        public static int GetFeeGold(GameState state)
        {
            return GetFeeGold(state?.MineResetCycle?.PaidResetCount ?? 0);
        }

        public static double GetRemainingSeconds(double elapsedSeconds)
        {
            var elapsed = elapsedSeconds < 0d || double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds)
                ? 0d
                : elapsedSeconds;
            var remaining = CycleDurationSeconds - elapsed;
            return remaining < 0d ? 0d : remaining;
        }

        public static double GetRemainingSeconds(GameState state)
        {
            return GetRemainingSeconds(state?.MineResetCycle?.ElapsedSeconds ?? 0d);
        }

        public static bool IsCycleExpired(double elapsedSeconds)
        {
            return GetRemainingSeconds(elapsedSeconds) <= 0d
                && elapsedSeconds >= CycleDurationSeconds;
        }

        public static bool IsCycleExpired(GameState state)
        {
            return IsCycleExpired(state?.MineResetCycle?.ElapsedSeconds ?? 0d);
        }

        public static string FormatClock(double remainingSeconds)
        {
            var total = (int)Math.Floor(Math.Max(0d, remainingSeconds) + 0.0000001d);
            var hours = total / 3600;
            var minutes = (total % 3600) / 60;
            var seconds = total % 60;
            return hours.ToString("00")
                + ":"
                + minutes.ToString("00")
                + ":"
                + seconds.ToString("00");
        }

        public static bool TryReset(
            GameState state,
            MineWorldCache cache,
            IMineResetSeedSource seeds,
            out MineResetResult result)
        {
            return TryCommit(state, cache, seeds, paid: true, out result);
        }

        public static bool TryTimedReset(
            GameState state,
            MineWorldCache cache,
            IMineResetSeedSource seeds,
            out MineResetResult result)
        {
            return TryCommit(state, cache, seeds, paid: false, out result);
        }

        private static bool TryCommit(
            GameState state,
            MineWorldCache cache,
            IMineResetSeedSource seeds,
            bool paid,
            out MineResetResult result)
        {
            if (!GameState.IsComplete(state) || cache == null || seeds == null)
            {
                result = new MineResetResult(MineResetStatus.InvalidState, 0, 0, 0);
                return false;
            }

            var gold = state.Player.Gold;
            var previous = cache.Peek();
            var previousSeed = previous?.worldSeed ?? 0;
            var fee = paid ? GetFeeGold(state) : 0;
            if (paid && gold < fee)
            {
                result = new MineResetResult(
                    MineResetStatus.InsufficientGold,
                    previousSeed,
                    0,
                    gold,
                    fee);
                return false;
            }

            var newSeed = TryCreateDifferentSeed(seeds, previousSeed);
            if (newSeed == 0)
            {
                result = new MineResetResult(
                    MineResetStatus.SeedFailed,
                    previousSeed,
                    0,
                    gold,
                    fee);
                return false;
            }

            var replacement = new WorldSnapshotDto
            {
                worldSeed = newSeed,
                generatorVersion = previous != null && previous.generatorVersion > 0
                    ? previous.generatorVersion
                    : 1
            };

            // 모든 실패 조건을 먼저 확인한 뒤 골드·월드·주기를 연속 커밋한다.
            if (paid)
            {
                state.SetGold(gold - fee);
                state.MineResetCycle.ApplyPaidReset();
            }
            else
            {
                state.MineResetCycle.ApplyTimedReset();
            }

            cache.ReplaceFromProvider(replacement);
            state.NotifyMineResetCycleChanged();
            result = new MineResetResult(
                MineResetStatus.Success,
                previousSeed,
                newSeed,
                state.Player.Gold,
                paid ? fee : 0,
                timedReset: !paid);
            return true;
        }

        private static long TryCreateDifferentSeed(
            IMineResetSeedSource seeds,
            long previousSeed)
        {
            for (var attempt = 0; attempt < MaximumSeedAttempts; attempt++)
            {
                long candidate;
                try
                {
                    candidate = seeds.NextSeed();
                }
                catch (Exception)
                {
                    continue;
                }

                if (candidate != 0 && candidate != previousSeed)
                {
                    return candidate;
                }
            }

            return 0;
        }
    }
}
