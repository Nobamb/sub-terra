namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154) 문구. 레벨 자리는 #223 레벨제용으로 미리 둔다(지금은 넘기지 않는다).
    /// </summary>
    public static class DeepZoneUnlockPopupText
    {
        public const string CloseLabel = "닫기";

        /// <summary>정적 폰트 아틀라스에 넣어 둘 글자. 숫자는 깊이·레벨 값이 바뀌어도 그릴 수 있게 모두 넣는다.</summary>
        public const string FontCharacters = "심층 구역 해금 진입 가능 닫기 0123456789~ML.v·";

        public static string Header(int? level = null) =>
            level.HasValue ? "심층 구역 Lv." + level.Value + " 해금" : "심층 구역 해금";

        public static string Range(int minDepth, int maxDepth, int? level = null)
        {
            var range = minDepth + "~" + maxDepth + "M";
            return level.HasValue
                ? "심층 구역 Lv." + level.Value + " · " + range
                : range + " 진입 가능";
        }
    }
}
