using SubTerra.Shared.Localization;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154) 문구. 레벨 자리는 #223 레벨제용으로 미리 둔다(지금은 넘기지 않는다).
    /// </summary>
    public static class DeepZoneUnlockPopupText
    {
        private const string KoreanDetail = "리튬 광맥이 잠든 깊은 지층이 열렸습니다";
        private const string EnglishDetail = "A deep layer rich in lithium is now open.";

        public static string CloseLabel => LocalizationService.Text("닫기", "Close");

        /// <summary>B-154-1: 심층 띠는 리튬만 나온다(MineLayerDistribution 심층 띠).</summary>
        public static string Detail => LocalizationService.Text(KoreanDetail, EnglishDetail);

        /// <summary>정적 폰트 아틀라스에 넣어 둘 글자. 숫자는 깊이·레벨 값이 바뀌어도 그릴 수 있게 모두 넣는다.</summary>
        public const string FontCharacters = "심층 구역 해금 진입 가능 닫기 0123456789~ML.v·" + KoreanDetail;

        public static string Header(int? level = null) =>
            level.HasValue
                ? LocalizationService.Text($"심층 구역 Lv.{level.Value} 해금", $"Deep Zone Lv.{level.Value} Unlocked")
                : LocalizationService.Text("심층 구역 해금", "Deep Zone Unlocked");

        public static string Range(int minDepth, int maxDepth, int? level = null)
        {
            var range = minDepth + "~" + maxDepth + "M";
            return level.HasValue
                ? LocalizationService.Text($"심층 구역 Lv.{level.Value} · {range}", $"Deep Zone Lv.{level.Value} · {range}")
                : LocalizationService.Text(range + " 진입 가능", "Depths open: " + range);
        }
    }
}
