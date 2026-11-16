#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using System.Globalization;
using SubTerra.App.Core;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 가스 도트와 낙사 착지 확인용 명령. 실제 게임과 같은 피해 경로를 탄다.
    /// 광산 씬에서만 동작하고, 새 효과음은 만들지 않는다.
    /// </summary>
    public static class DeveloperDebugCollapseCommands
    {
        private const int MaximumGasTicks = 30;

        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "hitgas",
                new[] { "hitgas", "가스피해" },
                "[횟수]",
                "hitgas 3",
                HandleHitGas));
            registry.Register(new DeveloperDebugCommandSpec(
                "falling",
                new[] { "falling", "낙하" },
                "[높이m]",
                "falling 30m",
                HandleFalling));
        }

        private static string HandleHitGas(string[] tokens, DeveloperDebugCommandContext context)
        {
            const string usage = "hitgas 인자 오류. 예: hitgas 3";
            var count = 1;
            if (tokens != null && tokens.Length > 2)
            {
                return usage;
            }

            if (tokens != null && tokens.Length == 2
                && (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
                    || count < 1
                    || count > MaximumGasTicks))
            {
                return usage;
            }

            var error = ResolveMine(context, out var world);
            if (error != null)
            {
                return error;
            }

            if (world.GasEffect == null)
            {
                return "가스 효과 컨트롤러 없음";
            }

            world.GasEffect.DebugRaiseDamageTicks(count);
            return "hitgas " + count.ToString(CultureInfo.InvariantCulture) + "회 (틱당 "
                + world.GasEffect.DebugHealthDamagePerTick.ToString(CultureInfo.InvariantCulture)
                + ", 간격 1초)";
        }

        private static string HandleFalling(string[] tokens, DeveloperDebugCommandContext context)
        {
            const string usage = "falling 인자 오류. 예: falling 30m";
            float requested = 0f;
            if (tokens != null && tokens.Length > 2)
            {
                return usage;
            }

            if (tokens != null && tokens.Length == 2 && !TryReadMeters(tokens[1], out requested))
            {
                return usage;
            }

            var error = ResolveMine(context, out _);
            if (error != null)
            {
                return error;
            }

            var survival = context.FindPlayer();
            if (survival == null)
            {
                return "플레이어 없음";
            }

            var height = Mathf.Max(requested, survival.MinimumFallHeight);
            var healthBefore = survival.State != null ? survival.State.Health : 0f;
            // 실제 낙사 착지와 같은 ApplyFall + 착지음 경로를 탄다.
            if (!survival.SimulateFall(height))
            {
                return "피해 없음(무적 시간 중이거나 행동불능)";
            }

            return "falling " + Format(height) + "m -" + Format(healthBefore - survival.State.Health);
        }

        private static string ResolveMine(DeveloperDebugCommandContext context, out DeveloperDebugSpawnWorld world)
        {
            world = null;
            if (context == null)
            {
                return "광산 정보 없음";
            }

            world = context.FindSpawnWorld();
            if (world == null)
            {
                return "광산 정보 없음";
            }

            if (!string.Equals(world.SceneName, SceneNames.Integration, StringComparison.Ordinal))
            {
                return "광산 씬에서만 사용할 수 있습니다";
            }

            return null;
        }

        private static bool TryReadMeters(string token, out float meters)
        {
            meters = 0f;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var text = token.Trim();
            if (text.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 1);
            }

            // 0이나 음수, 최소 높이 미만은 최소 낙사 높이부터 적용한다(HandleFalling).
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out meters)
                && !float.IsNaN(meters)
                && !float.IsInfinity(meters);
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
#endif
