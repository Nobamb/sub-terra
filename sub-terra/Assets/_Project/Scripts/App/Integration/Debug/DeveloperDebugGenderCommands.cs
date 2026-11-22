#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-208: `gender male|female` (별칭 `성별 남|여`). 현재 게임 성별을 바꿔 다음 저장에 남기고
    /// 플레이어·가이드 그림에 바로 적용한다. 성별 선택 화면은 B-209 범위다.
    /// </summary>
    public static class DeveloperDebugGenderCommands
    {
        public const string Usage = "사용법: gender male|female (성별 남|여)";

        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "gender",
                new[] { "gender", "성별" },
                "<male|female>",
                "gender female",
                HandleGender));
        }

        /// <summary>male/m/남/남자 → 남자, female/f/여/여자 → 여자. 그 밖은 false.</summary>
        public static bool TryParse(string token, out PlayerGender gender)
        {
            gender = PlayerGender.Male;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var value = token.Trim();
            if (Is(value, "male") || Is(value, "m") || value == "남" || value == "남자")
            {
                gender = PlayerGender.Male;
                return true;
            }

            if (Is(value, "female") || Is(value, "f") || value == "여" || value == "여자")
            {
                gender = PlayerGender.Female;
                return true;
            }

            return false;
        }

        public static string Label(PlayerGender gender)
        {
            return gender == PlayerGender.Female ? "여자(female)" : "남자(male)";
        }

        private static string HandleGender(string[] tokens, DeveloperDebugCommandContext context)
        {
            var state = context != null ? context.State : null;
            if (state == null || state.Player == null)
            {
                return "GameState 없음";
            }

            var before = state.Player.Gender;
            if (tokens == null || tokens.Length != 2 || !TryParse(tokens[1], out var next))
            {
                return "현재 성별: " + Label(before) + "\n" + Usage;
            }

            state.SetPlayerGender(next);
            // 런타임에 묶이지 않은 상태(테스트 등)에서도 그림이 바로 따라오게 한다.
            PlayerGenderPresentation.Set(state.Player.Gender);
            return "gender " + Label(before) + " -> " + Label(state.Player.Gender);
        }

        private static bool Is(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
