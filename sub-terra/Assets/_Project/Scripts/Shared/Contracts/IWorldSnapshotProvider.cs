namespace SubTerra.Shared
{
    /// <summary>
    /// 개발자 A(Gameplay)와 개발자 B(SaveService)를 연결하는 월드 스냅샷 계약 인터페이스
    /// </summary>
    public interface IWorldSnapshotProvider
    {
        /// <summary>
        /// (개발자 A 구현) 현재 월드의 변경점 스냅샷을 캡처하여 DTO로 반환합니다.
        /// </summary>
        WorldSnapshotDto CaptureSnapshot();

        /// <summary>
        /// (개발자 A 구현) 전달받은 스냅샷 DTO를 기반으로 월드 상태를 복원합니다.
        /// </summary>
        void RestoreSnapshot(WorldSnapshotDto snapshot);
    }
}
