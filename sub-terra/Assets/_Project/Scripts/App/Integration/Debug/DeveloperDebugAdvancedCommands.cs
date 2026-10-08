#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using System.Globalization;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 125-3 명령. 125-1 API와 기존 TryResetMine만 호출하고 코어 API는 추가하지 않는다.
    /// 경과 시간과 요금 오버라이드는 GameState, 적용은 SaveRuntimeController가 맡는다.
    /// </summary>
    public static class DeveloperDebugAdvancedCommands
    {
        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "upgrade",
                new[] { "upgrade", "업그레이드" },
                "[n|full|fullall|clear|clearall]",
                "upgrade 1",
                HandleUpgrade));
            registry.Register(new DeveloperDebugCommandSpec(
                "deepunlock",
                new[] { "deepunlock", "심층지역해금" },
                string.Empty,
                "deepunlock",
                HandleDeepUnlock));
            registry.Register(new DeveloperDebugCommandSpec(
                "initmine",
                new[] { "initmine", "광산초기화" },
                string.Empty,
                "initmine",
                HandleInitMine));
            registry.Register(new DeveloperDebugCommandSpec(
                "timer",
                new[] { "timer", "타이머" },
                "<n> [sec|min|hour] | end | reuse [hp|energy] [<n>s|m|h]",
                "timer -10",
                HandleTimer));
            registry.Register(new DeveloperDebugCommandSpec(
                "minecost",
                new[] { "minecost", "광산비용" },
                "<n|free|공짜>",
                "minecost free",
                HandleMineCost));
            registry.Register(new DeveloperDebugCommandSpec(
                "minepayment",
                new[] { "minepayment", "광산지불" },
                "[N] [costfree]",
                "minepayment 2 costfree",
                HandleMinePayment));
        }

        private static string HandleUpgrade(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryUpgradeMode(tokens, out var mode, out var delta))
            {
                return "upgrade 인자 오류. 예: upgrade 1";
            }

            if (context == null)
            {
                return "진행도 없음";
            }

            var progression = context.FindProgression();
            if (progression == null)
            {
                return "진행도 없음";
            }

            if (mode == UpgradeMode.FullAll || mode == UpgradeMode.ClearAll)
            {
                var snapshots = progression.GetSnapshots();
                if (snapshots == null || snapshots.Count == 0)
                {
                    return "업그레이드 없음";
                }

                var changed = 0;
                for (var i = 0; i < snapshots.Count; i++)
                {
                    var snapshot = snapshots[i];
                    var target = mode == UpgradeMode.ClearAll ? 0 : snapshot.MaximumLevel;
                    if (!progression.SetUpgradeLevelAbsolute(snapshot.UpgradeId, target))
                    {
                        return "업그레이드 실패: " + snapshot.UpgradeId;
                    }

                    changed++;
                }

                var label = mode == UpgradeMode.ClearAll ? "clearall " : "fullall ";
                return label + changed.ToString(CultureInfo.InvariantCulture);
            }

            var selected = context.FindSelectedUpgradeId();
            if (string.IsNullOrEmpty(selected))
            {
                return "업그레이드 창에서 항목을 선택";
            }

            if (!progression.TryGetSnapshot(selected, out var current))
            {
                return "업그레이드 실패: " + selected;
            }

            var level = current.CurrentLevel;
            if (mode == UpgradeMode.Full)
            {
                level = current.MaximumLevel;
            }
            else if (mode == UpgradeMode.Clear)
            {
                level = 0;
            }
            else
            {
                var sum = (long)current.CurrentLevel + delta;
                if (sum > int.MaxValue)
                {
                    sum = int.MaxValue;
                }

                level = (int)sum;
            }

            if (!progression.SetUpgradeLevelAbsolute(selected, level))
            {
                return "업그레이드 실패: " + selected;
            }

            if (!progression.TryGetSnapshot(selected, out var after))
            {
                return "업그레이드 실패: " + selected;
            }

            return "upgrade " + selected + " "
                + current.CurrentLevel.ToString(CultureInfo.InvariantCulture)
                + " -> "
                + after.CurrentLevel.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleDeepUnlock(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (tokens == null || tokens.Length != 1)
            {
                return "deepunlock 인자 오류. 예: deepunlock";
            }

            var progression = context != null ? context.FindProgression() : null;
            if (progression == null)
            {
                return "진행도 없음";
            }

            progression.ForceUnlockDeepZone();
            if (progression.State == null
                || !progression.State.IsZoneUnlocked(DataIds.Zones.Deep))
            {
                return "심층 지역 해금 실패";
            }

            return "심층 지역 해금";
        }

        private static string HandleInitMine(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (tokens == null || tokens.Length != 1)
            {
                return "initmine 인자 오류. 예: initmine";
            }

            var runtime = context != null ? context.FindRuntime() : null;
            if (runtime == null)
            {
                return "세이브 런타임 없음";
            }

            runtime.ResetMineWithoutPopup();
            return "광산 초기화";
        }

        private static string HandleTimerReuse(string[] tokens, DeveloperDebugCommandContext context)
        {
            const string usage = "timer reuse 인자 오류. 예: timer reuse hp 10s";
            if (!TryTimerReuse(tokens, out var target, out var seconds))
            {
                return usage;
            }

            if (context == null || context.State == null || context.State.Outpost == null)
            {
                return "GameState 없음";
            }

            var label = target == ReuseTarget.Clinic ? "보건소" : target == ReuseTarget.Charger ? "충전기" : "보건소·충전기";
            var changed = context.State.Outpost.ReduceFacilityCooldowns(id => MatchesReuseTarget(id, target), seconds);
            var action = double.IsInfinity(seconds)
                ? "초기화"
                : "-" + seconds.ToString("0.###", CultureInfo.InvariantCulture) + "초";
            if (changed == 0)
            {
                return "timer reuse " + label + " 재사용 대기 중인 시설 없음";
            }

            return "timer reuse " + label + " " + action + " " + changed.ToString(CultureInfo.InvariantCulture) + "곳";
        }

        // 시설 인스턴스 ID는 "{buildingId}-{순번}" 형태라 접두어로 종류를 가른다.
        private static bool MatchesReuseTarget(string instanceId, ReuseTarget target)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            var clinic = instanceId.StartsWith(DataIds.Buildings.ClinicBasic + "-", StringComparison.Ordinal);
            var charger = instanceId.StartsWith(DataIds.Buildings.ChargerBasic + "-", StringComparison.Ordinal);
            switch (target)
            {
                case ReuseTarget.Clinic:
                    return clinic;
                case ReuseTarget.Charger:
                    return charger;
                default:
                    return clinic || charger;
            }
        }

        private static string HandleTimer(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (tokens != null && tokens.Length >= 2 && IsReuse(tokens[1]))
            {
                return HandleTimerReuse(tokens, context);
            }

            if (!TryTimer(tokens, out var deltaSeconds, out var end))
            {
                return "timer 인자 오류. 예: timer -10";
            }

            if (context == null || context.State == null || context.State.MineResetCycle == null)
            {
                return "GameState 없음";
            }

            var runtime = context.FindRuntime();
            if (runtime == null)
            {
                return "세이브 런타임 없음";
            }

            var before = context.State.MineResetCycle.ElapsedSeconds;
            var next = end
                ? MineResetService.CycleDurationSeconds
                : before + deltaSeconds;
            runtime.SetMineResetElapsedSeconds(next);
            var after = context.State.MineResetCycle.ElapsedSeconds;
            var remaining = MineResetService.GetRemainingSeconds(context.State);
            return "timer "
                + before.ToString("0.###", CultureInfo.InvariantCulture)
                + " -> "
                + after.ToString("0.###", CultureInfo.InvariantCulture)
                + " remaining "
                + MineResetService.FormatClock(remaining);
        }

        private static string HandleMineCost(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryMineCost(tokens, out var fee))
            {
                return "minecost 인자 오류. 예: minecost free";
            }

            if (context == null || context.State == null)
            {
                return "GameState 없음";
            }

            MineResetService.SetNextPaidResetFeeOverride(context.State, fee);
            var applied = fee < 0 ? 0 : fee;
            return "minecost " + applied.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleMinePayment(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryMinePayment(tokens, out var count, out var costFree))
            {
                return "minepayment 인자 오류. 예: minepayment 2 costfree";
            }

            if (SceneManager.GetActiveScene().name != SceneNames.SurfaceBase)
            {
                return "지상 기지에서만 광산을 지불할 수 있습니다.";
            }

            if (context == null || context.State == null)
            {
                return "GameState 없음";
            }

            var runtime = context.FindRuntime();
            if (runtime == null)
            {
                return "세이브 런타임 없음";
            }

            var done = 0;
            for (var i = 0; i < count; i++)
            {
                if (costFree)
                {
                    MineResetService.SetNextPaidResetFeeOverride(context.State, 0);
                }

                if (!runtime.TryResetMine(out var reason))
                {
                    if (reason == "mine_reset.fail.gold")
                    {
                        return "골드가 부족합니다. "
                            + done.ToString(CultureInfo.InvariantCulture)
                            + "회 완료";
                    }

                    if (reason == "mine_reset.fail.surface")
                    {
                        return "지상 기지에서만 광산을 지불할 수 있습니다.";
                    }

                    return "광산 지불 실패. "
                        + done.ToString(CultureInfo.InvariantCulture)
                        + "회 완료";
                }

                done++;
            }

            return "광산 지불 " + done.ToString(CultureInfo.InvariantCulture) + "회";
        }

        private static bool TryUpgradeMode(string[] tokens, out UpgradeMode mode, out int delta)
        {
            mode = UpgradeMode.Delta;
            delta = 1;
            if (tokens == null || tokens.Length < 1 || tokens.Length > 2)
            {
                return false;
            }

            if (tokens.Length == 1)
            {
                return true;
            }

            if (IsWord(tokens[1], "fullall"))
            {
                mode = UpgradeMode.FullAll;
                return true;
            }

            if (IsWord(tokens[1], "clearall"))
            {
                mode = UpgradeMode.ClearAll;
                return true;
            }

            if (IsWord(tokens[1], "full"))
            {
                mode = UpgradeMode.Full;
                return true;
            }

            if (IsWord(tokens[1], "clear"))
            {
                mode = UpgradeMode.Clear;
                return true;
            }

            if (!TryParseInt(tokens[1], out delta) || delta <= 0)
            {
                return false;
            }

            mode = UpgradeMode.Delta;
            return true;
        }

        private static bool TryTimer(string[] tokens, out double deltaSeconds, out bool end)
        {
            deltaSeconds = 0d;
            end = false;
            if (tokens == null || tokens.Length < 2 || tokens.Length > 3)
            {
                return false;
            }

            if (IsEnd(tokens[1]))
            {
                if (tokens.Length != 2)
                {
                    return false;
                }

                end = true;
                return true;
            }

            string unit = null;
            int amount;
            if (tokens.Length == 2)
            {
                if (!TryParseAmountUnit(tokens[1], out amount, out unit))
                {
                    return false;
                }
            }
            else
            {
                if (!TryParseInt(tokens[1], out amount))
                {
                    return false;
                }

                unit = tokens[2];
            }

            if (!TryUnitSeconds(unit, out var unitSeconds))
            {
                return false;
            }

            deltaSeconds = amount * unitSeconds;
            return true;
        }

        /// <summary>
        /// timer reuse [hp|energy] [양]. 양이 없으면 초기화(seconds = 무한대),
        /// 있으면 그만큼 감소. 양은 "10s"처럼 붙이거나 "10 s"처럼 띄우며 단위가 없으면 분이다.
        /// hp/energy는 양의 앞뒤 어디에 와도 된다.
        /// </summary>
        private static bool TryTimerReuse(string[] tokens, out ReuseTarget target, out double seconds)
        {
            target = ReuseTarget.Both;
            seconds = double.PositiveInfinity;
            if (tokens == null || tokens.Length < 2 || tokens.Length > 5)
            {
                return false;
            }

            var rest = new System.Collections.Generic.List<string>();
            var targetSeen = false;
            for (var i = 2; i < tokens.Length; i++)
            {
                if (TryReuseTarget(tokens[i], out var parsed))
                {
                    if (targetSeen)
                    {
                        return false;
                    }

                    target = parsed;
                    targetSeen = true;
                    continue;
                }

                rest.Add(tokens[i]);
            }

            if (rest.Count == 0)
            {
                return true;
            }

            if (rest.Count > 2)
            {
                return false;
            }

            string unit = null;
            int amount;
            if (rest.Count == 1)
            {
                if (!TryParseAmountUnit(rest[0], out amount, out unit))
                {
                    return false;
                }
            }
            else
            {
                if (!TryParseInt(rest[0], out amount))
                {
                    return false;
                }

                unit = rest[1];
            }

            if (amount <= 0 || !TryUnitSeconds(unit, out var unitSeconds))
            {
                return false;
            }

            seconds = amount * unitSeconds;
            return true;
        }

        private static bool TryReuseTarget(string token, out ReuseTarget target)
        {
            target = ReuseTarget.Both;
            if (IsWord(token, "hp") || IsWord(token, "clinic")
                || string.Equals(token, "체력", StringComparison.Ordinal)
                || string.Equals(token, "보건소", StringComparison.Ordinal))
            {
                target = ReuseTarget.Clinic;
                return true;
            }

            if (IsWord(token, "energy") || IsWord(token, "charger")
                || string.Equals(token, "전력", StringComparison.Ordinal)
                || string.Equals(token, "충전기", StringComparison.Ordinal))
            {
                target = ReuseTarget.Charger;
                return true;
            }

            return false;
        }

        private static bool IsReuse(string token)
        {
            return IsWord(token, "reuse") || string.Equals(token, "재사용", StringComparison.Ordinal);
        }

        private static bool TryMineCost(string[] tokens, out int fee)
        {
            fee = 0;
            if (tokens == null || tokens.Length != 2)
            {
                return false;
            }

            if (IsWord(tokens[1], "free") || string.Equals(tokens[1], "공짜", StringComparison.Ordinal))
            {
                return true;
            }

            return TryParseInt(tokens[1], out fee);
        }

        private static bool TryMinePayment(string[] tokens, out int count, out bool costFree)
        {
            count = 1;
            costFree = false;
            if (tokens == null || tokens.Length < 1 || tokens.Length > 3)
            {
                return false;
            }

            if (tokens.Length == 1)
            {
                return true;
            }

            if (tokens.Length == 2)
            {
                if (IsWord(tokens[1], "costfree"))
                {
                    costFree = true;
                    return true;
                }

                return TryPaymentCount(tokens[1], out count);
            }

            if (!TryPaymentCount(tokens[1], out count) || !IsWord(tokens[2], "costfree"))
            {
                return false;
            }

            costFree = true;
            return true;
        }

        private static bool TryPaymentCount(string token, out int count)
        {
            count = 1;
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            if (value < 1d || Math.Abs(value - Math.Floor(value)) > 0.0000001d)
            {
                count = 1;
                return true;
            }

            if (value > int.MaxValue)
            {
                count = int.MaxValue;
                return true;
            }

            count = (int)value;
            return true;
        }

        private static bool TryParseAmountUnit(string token, out int amount, out string unit)
        {
            amount = 0;
            unit = null;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var suffixes = new[] { "hour", "min", "sec", "시간", "분", "초", "h", "m", "s" };
            for (var i = 0; i < suffixes.Length; i++)
            {
                var suffix = suffixes[i];
                if (token.Length <= suffix.Length
                    || !token.EndsWith(suffix, UnitComparison(suffix)))
                {
                    continue;
                }

                var head = token.Substring(0, token.Length - suffix.Length);
                if (!TryParseInt(head, out amount))
                {
                    return false;
                }

                unit = suffix;
                return true;
            }

            return TryParseInt(token, out amount);
        }

        private static bool TryUnitSeconds(string unit, out double seconds)
        {
            seconds = 60d;
            if (string.IsNullOrEmpty(unit))
            {
                return true;
            }

            if (IsUnit(unit, "sec", "s", "초"))
            {
                seconds = 1d;
                return true;
            }

            if (IsUnit(unit, "min", "m", "분"))
            {
                seconds = 60d;
                return true;
            }

            if (IsUnit(unit, "hour", "h", "시간"))
            {
                seconds = 3600d;
                return true;
            }

            return false;
        }

        private static bool IsUnit(string token, string english, string shortEnglish, string korean)
        {
            return IsWord(token, english)
                || IsWord(token, shortEnglish)
                || string.Equals(token, korean, StringComparison.Ordinal);
        }

        private static bool IsEnd(string token)
        {
            return IsWord(token, "end") || string.Equals(token, "종료", StringComparison.Ordinal);
        }

        private static bool IsWord(string token, string word)
        {
            return string.Equals(token, word, StringComparison.OrdinalIgnoreCase);
        }

        private static StringComparison UnitComparison(string suffix)
        {
            for (var i = 0; i < suffix.Length; i++)
            {
                if (suffix[i] > 127)
                {
                    return StringComparison.Ordinal;
                }
            }

            return StringComparison.OrdinalIgnoreCase;
        }

        private static bool TryParseInt(string token, out int amount)
        {
            return int.TryParse(
                token,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private enum ReuseTarget
        {
            Both = 0,
            Clinic = 1,
            Charger = 2
        }

        private enum UpgradeMode
        {
            Delta = 0,
            Full = 1,
            FullAll = 2,
            Clear = 3,
            ClearAll = 4
        }
    }
}
#endif
