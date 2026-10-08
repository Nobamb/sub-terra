using System;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>재사용 대기 시간 표시 문자열. 1시간 미만은 MM:SS, 이상은 H:MM:SS.</summary>
    public static class FacilityCooldownFormat
    {
        public static string Format(int totalSeconds)
        {
            var total = Math.Max(0, totalSeconds);
            var hours = total / 3600;
            var minutes = (total % 3600) / 60;
            var seconds = total % 60;
            if (hours > 0)
            {
                return hours + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            }

            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }
}
