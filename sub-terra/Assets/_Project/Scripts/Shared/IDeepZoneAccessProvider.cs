namespace SubTerra.Shared
{
    /// <summary>Gameplay이 심층 신호 접근 권한을 읽는 최소 계약.</summary>
    public interface IDeepZoneAccessProvider
    {
        bool IsDeepZoneUnlocked { get; }
    }
}
