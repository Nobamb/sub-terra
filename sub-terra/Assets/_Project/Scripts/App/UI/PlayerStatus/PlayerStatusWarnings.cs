namespace SubTerra.App.UI.PlayerStatus
{
    /// <summary>머리 위 경고 문구를 받는 곳. 시설 사용불가·건설 실패 등 여러 프레젠터가 같은 통로를 쓴다.</summary>
    public interface IPlayerStatusWarningSink
    {
        void ShowWarning(string key, string text);
    }

    /// <summary>
    /// 현재 활성화된 머리 위 스택을 찾는 통로. 프레젠터는 씬 배선 순서와 무관하게 호출 시점에 이 값을 읽는다.
    /// 스택이 켜질 때 등록하고 꺼질 때 해제한다.
    /// </summary>
    public static class PlayerStatusWarnings
    {
        public static IPlayerStatusWarningSink Current { get; set; }
    }
}
