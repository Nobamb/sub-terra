namespace SubTerra.Shared
{
    /// <summary>
    /// 채굴 완료 보상을 인벤토리 경계로 한 번 전달한다.
    /// 구현체는 영구 광물 ID와 양수 수량을 기준으로 보상을 반영한다.
    /// </summary>
    public interface IMiningRewardReceiver
    {
        void AddMineral(string mineralId, int quantity);
    }
}
