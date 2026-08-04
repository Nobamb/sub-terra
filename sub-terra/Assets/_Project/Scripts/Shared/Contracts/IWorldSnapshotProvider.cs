namespace SubTerra.Shared
{
    /// <summary>
    /// 개발자 A(Gameplay)와 개발자 B(SaveService)를 연결하는 월드 스냅샷 계약 인터페이스.
    /// JSON에는 Unity Object를 넣지 않고, Seed+변경점만 왕복한다.
    /// </summary>
    public interface IWorldSnapshotProvider
    {
        /// <summary>
        /// (개발자 A 구현) 현재 월드의 변경점 스냅샷을 캡처하여 DTO로 반환합니다.
        /// </summary>
        WorldSnapshotDto CaptureSnapshot();

        /// <summary>
        /// (개발자 A 구현) 스냅샷 DTO로 월드를 복원합니다.
        /// generatorVersion 불일치 등 기본 월드 재생성이 불가능하면 false를 반환합니다.
        /// null 스냅샷은 무해한 no-op으로 true를 반환합니다.
        /// </summary>
        bool RestoreSnapshot(WorldSnapshotDto snapshot);
    }
}
