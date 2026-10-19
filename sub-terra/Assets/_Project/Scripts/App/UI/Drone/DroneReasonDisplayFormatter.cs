using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SubTerra.App.Drone;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// 드론 분석 창 문구. 점수는 표시하지 않고, 근거 줄 끝에 실제 수치·단위를 붙인다.
    /// 근거 문구에 같은 수치가 이미 단위째 들어 있거나 값이 의미 없는 근거는 문구만 둔다.
    /// </summary>
    public static class DroneReasonDisplayFormatter
    {
        public const string ActionPrefix = "AI 추천 // ";
        public const string EmptyReasons = "추가 위험 근거 없음";
        public const string ValueSeparator = " · ";

        public static string FormatAction(DroneAction action)
        {
            return ActionPrefix + DroneAnalysisService.FormatAction(action);
        }

        /// <summary>근거 줄 목록. 근거가 없으면 안내 한 줄.</summary>
        public static List<string> FormatReasonLines(IReadOnlyList<DroneReason> reasons)
        {
            var lines = new List<string>();
            if (reasons != null)
            {
                for (var i = 0; i < reasons.Count; i++)
                {
                    lines.Add(FormatReasonLine(i, reasons[i]));
                }
            }

            if (lines.Count == 0)
            {
                lines.Add(EmptyReasons);
            }

            return lines;
        }

        public static string JoinLines(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        /// <summary>앞에서 count줄이 차지하는 글자 수(줄바꿈 포함). TMP maxVisibleCharacters에 쓴다.</summary>
        public static int CharactersInFirstLines(IReadOnlyList<string> lines, int count)
        {
            var total = 0;
            var clamped = Math.Min(count, lines.Count);
            for (var i = 0; i < clamped; i++)
            {
                total += lines[i].Length + (i > 0 ? 1 : 0);
            }

            return total;
        }

        public static string FormatReasonLine(int index, DroneReason reason)
        {
            var line = "[" + (index + 1).ToString("00", CultureInfo.InvariantCulture) + "] "
                + (reason != null ? reason.Message : string.Empty);
            var value = FormatValue(reason);
            return value.Length > 0 ? line + ValueSeparator + value : line;
        }

        /// <summary>
        /// 근거 단위별 실제 수치 표기.
        /// risk·ratio(0~1) → 정수 %, gold → 정수 G, weight → 소수 한 자리 kg.
        /// energy·m·depth는 문구에 같은 값이 이미 있어 생략, detected·값 없음은 의미가 없어 생략.
        /// </summary>
        public static string FormatValue(DroneReason reason)
        {
            if (reason == null || double.IsNaN(reason.ActualValue) || double.IsInfinity(reason.ActualValue))
            {
                return string.Empty;
            }

            var value = reason.ActualValue;
            switch (reason.Unit)
            {
                case "risk":
                    return Percent(value);
                case "ratio":
                    return "무결성 " + Percent(value);
                case "gold":
                    return Math.Round(value, MidpointRounding.AwayFromZero)
                        .ToString("0", CultureInfo.InvariantCulture) + "G";
                case "weight":
                    return value.ToString("0.#", CultureInfo.InvariantCulture) + "kg";
                default:
                    return string.Empty;
            }
        }

        private static string Percent(double ratio)
        {
            var clamped = ratio < 0d ? 0d : ratio > 1d ? 1d : ratio;
            return Math.Round(clamped * 100d, MidpointRounding.AwayFromZero)
                .ToString("0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
