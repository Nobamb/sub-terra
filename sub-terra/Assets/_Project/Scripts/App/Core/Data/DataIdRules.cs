using System.Text.RegularExpressions;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 영구 ID 형식 규칙. 소문자·숫자·밑줄과 점 구분만 허용한다.
    /// 예: mineral.copper, building.outpost_core.basic
    /// </summary>
    public static class DataIdRules
    {
        private static readonly Regex IdPattern = new Regex(
            @"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsValidPermanentId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            if (id != id.Trim())
            {
                return false;
            }

            return IdPattern.IsMatch(id);
        }

        /// <summary>공백 트림 후 소문자화. 저장용 정규화이며 표시 이름에는 쓰지 않는다.</summary>
        public static string Normalize(string id)
        {
            if (id == null)
            {
                return string.Empty;
            }

            return id.Trim().ToLowerInvariant();
        }

        public static string DescribeInvalid(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "ID is empty.";
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return "ID is whitespace only.";
            }

            if (id != id.Trim())
            {
                return "ID has leading or trailing whitespace.";
            }

            if (id != id.ToLowerInvariant())
            {
                return "ID must be lowercase.";
            }

            if (!id.Contains("."))
            {
                return "ID must be dotted (e.g. mineral.copper).";
            }

            return "ID format is invalid (use lowercase letters, digits, underscore, dots).";
        }
    }
}
