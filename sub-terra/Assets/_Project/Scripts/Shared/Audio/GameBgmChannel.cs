namespace SubTerra.Shared.Audio
{
    /// <summary>
    /// 예정 BGM 채널. 실제 클립 배치는 이후 단계에서 하고,
    /// 마스터 음량은 모든 채널에 공통으로 적용한다.
    /// </summary>
    public enum GameBgmChannel
    {
        /// <summary>타이틀 화면 BGM</summary>
        Title = 0,
        /// <summary>표면 기지(휴식·업그레이드) BGM</summary>
        SurfaceBase = 1,
        /// <summary>지하 탐사·채굴 BGM</summary>
        UndergroundExploration = 2,
        /// <summary>위험·지반 붕괴 긴장 BGM</summary>
        DangerCollapse = 3
    }

    /// <summary>BGM 채널 메타. 클립 연결 전에도 채널 구성을 문서화한다.</summary>
    public static class GameBgmChannelInfo
    {
        public static readonly GameBgmChannel[] All =
        {
            GameBgmChannel.Title,
            GameBgmChannel.SurfaceBase,
            GameBgmChannel.UndergroundExploration,
            GameBgmChannel.DangerCollapse
        };

        public static string GetDebugName(GameBgmChannel channel)
        {
            return channel switch
            {
                GameBgmChannel.Title => "Title",
                GameBgmChannel.SurfaceBase => "SurfaceBase",
                GameBgmChannel.UndergroundExploration => "UndergroundExploration",
                GameBgmChannel.DangerCollapse => "DangerCollapse",
                _ => channel.ToString()
            };
        }
    }
}
