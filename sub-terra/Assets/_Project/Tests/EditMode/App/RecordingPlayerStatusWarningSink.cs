using SubTerra.App.UI.PlayerStatus;

namespace SubTerra.App.Tests
{
    /// <summary>머리 위 경고 통로를 기록만 하는 테스트용 싱크. 마지막 키·문구를 남긴다.</summary>
    public sealed class RecordingPlayerStatusWarningSink : IPlayerStatusWarningSink
    {
        public string Key;
        public string Text;
        public int Count;

        public void ShowWarning(string key, string text)
        {
            Key = key;
            Text = text;
            Count++;
        }
    }
}
