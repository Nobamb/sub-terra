namespace SubTerra.Shared
{
    /// <summary>
    /// Gameplay(A)의 확정 결과를 App(B)의 상태·UI·자동 저장 경계로 전달한다.
    /// </summary>
    public interface IGameplayEventSink
    {
        void Publish(GameplayEventDto gameplayEvent);
    }
}
