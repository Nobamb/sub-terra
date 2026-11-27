#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System.Globalization;
using System.Text;
using SubTerra.Gameplay.Building;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-214: `idlefx` 시설 주기 연출 디버그 명령.
    /// `idlefx` 시설별 남은 시간 / `idlefx now [종류]` 화면의 켜진 시설이 바로 한 번 재생(엇갈림 무시) /
    /// `idlefx period &lt;초|reset&gt;` 이번 실행 동안만 주기 변경(최소 2초, 저장 안 함).
    /// </summary>
    public static class DeveloperDebugFacilityIdleCommands
    {
        public const string Usage =
            "사용법: idlefx | idlefx now [light|charger|clinic|storage|settlement|outpost] | idlefx period <초|reset>";

        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "idlefx",
                new[] { "idlefx", "시설연출" },
                "[now [종류] | period <초|reset>]",
                "idlefx now clinic",
                HandleIdleFx));
        }

        private static string HandleIdleFx(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (tokens == null || tokens.Length <= 1)
            {
                return BuildList();
            }

            var verb = tokens[1].Trim().ToLowerInvariant();
            if (verb == "now" || verb == "지금")
            {
                return PlayNow(tokens.Length > 2 ? tokens[2] : null, Camera.main);
            }

            if (verb == "period" || verb == "주기")
            {
                return tokens.Length == 3 ? SetPeriod(tokens[2]) : Usage;
            }

            return Usage;
        }

        /// <summary>시설별 남은 시간 목록. 시설이 없으면 안내 문구.</summary>
        public static string BuildList()
        {
            var builder = new StringBuilder();
            var count = 0;
            builder.Append("주기 ")
                .Append(FacilityIdleAnimation.PeriodOverride > 0f
                    ? FacilityIdleAnimation.PeriodOverride.ToString("0.#", CultureInfo.InvariantCulture) + "초(디버그)"
                    : FacilityIdleAnimation.DefaultPeriodSeconds.ToString("0.#", CultureInfo.InvariantCulture) + "초(기본)");
            foreach (var entry in FacilityIdleAnimation.All)
            {
                if (entry == null || entry.Kind == FacilityIdleKind.None)
                {
                    continue;
                }

                count++;
                builder.Append("\n  ").Append(entry.Kind.ToString().ToLowerInvariant()).Append(' ')
                    .Append(entry.name).Append(": ");
                if (entry.IsPlaying)
                {
                    builder.Append("재생 중");
                }
                else
                {
                    builder.Append(entry.SecondsUntilNext.ToString("0.0", CultureInfo.InvariantCulture)).Append("초 뒤");
                    builder.Append(entry.CanPlay() ? string.Empty : " (꺼짐: 건너뜀)");
                }
            }

            if (count == 0)
            {
                builder.Append("\n  연출 대상 시설이 없습니다.");
            }

            return builder.ToString();
        }

        /// <summary>화면에 있는 켜진 시설을 바로 한 번 재생한다. 종류를 주면 그 종류만.</summary>
        public static string PlayNow(string kindToken, Camera camera)
        {
            var filter = FacilityIdleKind.None;
            if (!string.IsNullOrWhiteSpace(kindToken) && !FacilityIdleAnchors.TryParseKind(kindToken, out filter))
            {
                return Usage;
            }

            var played = 0;
            var skipped = 0;
            foreach (var entry in FacilityIdleAnimation.All)
            {
                if (entry == null || entry.Kind == FacilityIdleKind.None)
                {
                    continue;
                }

                if (filter != FacilityIdleKind.None && entry.Kind != filter)
                {
                    continue;
                }

                if (!IsOnScreen(camera, entry.transform.position))
                {
                    continue;
                }

                if (entry.PlayNow())
                {
                    played++;
                }
                else
                {
                    skipped++;
                }
            }

            return "idlefx now: " + played + "개 재생" + (skipped > 0 ? ", " + skipped + "개 건너뜀(꺼짐·건설 중·재생 중)" : string.Empty);
        }

        private static string SetPeriod(string token)
        {
            if (token != null && (token.Trim().ToLowerInvariant() == "reset" || token.Trim() == "초기화"))
            {
                FacilityIdleAnimation.SetPeriodOverride(0f);
                return "idlefx period: 기본값(" + FacilityIdleAnimation.DefaultPeriodSeconds.ToString("0.#", CultureInfo.InvariantCulture) + "초)으로 되돌렸습니다.";
            }

            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || float.IsNaN(seconds))
            {
                return Usage;
            }

            var applied = FacilityIdleAnimation.SetPeriodOverride(seconds);
            if (applied <= 0f)
            {
                return Usage;
            }

            // 이미 잡힌 대기 시간도 새 주기에 맞춰 다시 잡아 바로 확인할 수 있게 한다(저장되지 않는다).
            foreach (var entry in FacilityIdleAnimation.All)
            {
                if (entry != null && !entry.IsPlaying && entry.SecondsUntilNext > applied)
                {
                    entry.ScheduleAfter(Random.Range(0.5f, applied));
                }
            }

            return "idlefx period: " + applied.ToString("0.#", CultureInfo.InvariantCulture)
                + "초로 바꿨습니다(이번 실행 동안만, 최소 " + FacilityIdleAnimation.MinDebugPeriodSeconds.ToString("0", CultureInfo.InvariantCulture) + "초).";
        }

        private static bool IsOnScreen(Camera camera, Vector3 world)
        {
            if (camera == null)
            {
                return true;
            }

            var view = camera.WorldToViewportPoint(world);
            return view.z >= 0f && view.x > -0.05f && view.x < 1.05f && view.y > -0.05f && view.y < 1.05f;
        }
    }
}
#endif
