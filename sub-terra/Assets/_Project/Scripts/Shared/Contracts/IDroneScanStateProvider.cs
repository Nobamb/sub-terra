namespace SubTerra.Shared
{
    /// <summary>드론 스캔 펄스가 현재 월드에 표시되는지 제공한다.</summary>
    public interface IDroneScanStateProvider
    {
        bool IsScanPulseActive { get; }
    }
}
