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
        public long NextSeed()
        {
            var seed = DateTime.UtcNow.Ticks;
            return seed == 0 ? 1 : seed;
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

        public MineResetResult(
            MineResetStatus status,
            long previousSeed,
            long newSeed,
            int remainingGold)
        {
            Status = status;
            PreviousSeed = previousSeed;
            NewSeed = newSeed;
            RemainingGold = remainingGold;
        }
    }

    /// <summary>Surface Base 유료 광산 초기화의 상태 변경 규칙.</summary>
    public static class MineResetService
    {
        public const int FeeGold = 500;
        private const int MaximumSeedAttempts = 8;

        public static bool TryReset(
            GameState state,
            MineWorldCache cache,
            IMineResetSeedSource seeds,
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
            if (gold < FeeGold)
            {
                result = new MineResetResult(
                    MineResetStatus.InsufficientGold,
                    previousSeed,
                    0,
                    gold);
                return false;
            }

            var newSeed = TryCreateDifferentSeed(seeds, previousSeed);
            if (newSeed == 0)
            {
                result = new MineResetResult(
                    MineResetStatus.SeedFailed,
                    previousSeed,
                    0,
                    gold);
                return false;
            }

            var replacement = new WorldSnapshotDto
            {
                worldSeed = newSeed,
                generatorVersion = previous != null && previous.generatorVersion > 0
                    ? previous.generatorVersion
                    : 1
            };

            // 모든 실패 조건을 먼저 확인한 뒤 골드와 월드를 연속 커밋한다.
            state.SetGold(gold - FeeGold);
            cache.ReplaceFromProvider(replacement);
            result = new MineResetResult(
                MineResetStatus.Success,
                previousSeed,
                newSeed,
                state.Player.Gold);
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
