namespace SubTerra.Shared
{
    /// <summary>저장된 Seed와 생성기 버전으로 변경 전 기본 월드를 다시 만드는 경계입니다.</summary>
    public interface IWorldBaseGenerator
    {
        bool Regenerate(long worldSeed, int generatorVersion);
    }
}
