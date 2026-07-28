namespace SubTerra.Shared
{
    /// <summary>
    /// Gameplay(A)이 수집한 실제 월드 값으로 드론 분석 Context를 제공한다.
    /// </summary>
    public interface IDroneContextProvider
    {
        DroneContextDto CreateContext();
    }
}
