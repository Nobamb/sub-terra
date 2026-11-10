using System.Text;

namespace SubTerra.App.UI
{
    /// <summary>
    /// B-191: 한글 이름 검색 판정(순수 함수). 검색어의 자음은 이름의 초성과, 완성 글자는 그 글자와 이어서 맞춘다.
    /// 조합 중인 마지막 글자(받침 전·받침이 다음 초성으로 넘어가기 전)도 걸러지게 한다.
    /// </summary>
    public static class KoreanSearch
    {
        private const char FirstSyllable = '가';
        private const char LastSyllable = '힣';
        private const int SyllablesPerInitial = 588;
        private const int FinalCount = 28;
        private const string InitialConsonants = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

        // 받침이 다음 글자로 넘어갈 때: 남는 받침 번호 / 다음 글자의 초성 번호. (예: ㄺ → ㄹ 받침 + ㄱ 초성)
        private static readonly int[] FinalRemainder =
        {
            0, 0, 0, 1, 0, 4, 4, 0, 0, 8, 8, 8, 8, 8, 8, 8, 0, 0, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        private static readonly int[] FinalAsInitial =
        {
            -1, 0, 1, 9, 2, 12, 18, 3, 5, 0, 6, 7, 9, 16, 17, 18, 6, 7, 9, 9, 10, 11, 12, 14, 15, 16, 17, 18
        };

        /// <summary>공백을 빼고 소문자로 맞춘다.</summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    builder.Append(char.ToLowerInvariant(value[i]));
                }
            }

            return builder.ToString();
        }

        /// <summary>이름 안 어딘가에 검색어가 이어서 맞으면 true. 빈 검색어는 전체 통과.</summary>
        public static bool Matches(string name, string query)
        {
            var needle = Normalize(query);
            if (needle.Length == 0)
            {
                return true;
            }

            var text = Normalize(name);
            for (var start = 0; start < text.Length; start++)
            {
                if (MatchesAt(text, start, needle))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesAt(string text, int start, string needle)
        {
            for (var i = 0; i < needle.Length; i++)
            {
                var index = start + i;
                if (index >= text.Length)
                {
                    return false;
                }

                var last = i == needle.Length - 1;
                if (MatchesCharacter(text[index], needle[i], last))
                {
                    continue;
                }

                // 조합 중: `삳`은 `사` + 다음 글자 초성 ㄷ으로도 본다(사다리).
                return last && MatchesCarriedFinal(text, index, needle[i]);
            }

            return true;
        }

        private static bool MatchesCharacter(char name, char query, bool last)
        {
            if (name == query)
            {
                return true;
            }

            if (!IsSyllable(name))
            {
                return false;
            }

            var initial = InitialIndex(query);
            if (initial >= 0)
            {
                return (name - FirstSyllable) / SyllablesPerInitial == initial;
            }

            // 마지막 글자에 아직 받침을 치지 않았으면 같은 초성·중성 글자를 모두 후보로 둔다.
            return last && IsSyllable(query) && (query - FirstSyllable) % FinalCount == 0
                && (name - FirstSyllable) / FinalCount == (query - FirstSyllable) / FinalCount;
        }

        private static bool MatchesCarriedFinal(string text, int index, char query)
        {
            if (!IsSyllable(query) || index + 1 >= text.Length)
            {
                return false;
            }

            var final = (query - FirstSyllable) % FinalCount;
            if (final == 0)
            {
                return false;
            }

            var kept = (char)(query - final + FinalRemainder[final]);
            var next = text[index + 1];
            return text[index] == kept
                && IsSyllable(next)
                && (next - FirstSyllable) / SyllablesPerInitial == FinalAsInitial[final];
        }

        private static int InitialIndex(char value)
        {
            // 한글 자모 영역(U+1100~U+1112)과 호환 자모(ㄱ~ㅎ) 모두 받는다.
            return value >= 'ᄀ' && value <= 'ᄒ'
                ? value - 'ᄀ'
                : InitialConsonants.IndexOf(value);
        }

        private static bool IsSyllable(char value)
        {
            return value >= FirstSyllable && value <= LastSyllable;
        }
    }
}
