using SubTerra.Shared;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Integration Scene에 연결된 Shared 5경계 실체를 한곳에 모아 검증한다.
    /// 하드코딩 기대값이 아니라 실제 주입 인스턴스 유무로 연결 여부를 판정한다.
    /// </summary>
    public sealed class IntegrationContractRegistry
    {
        public IMiningRewardReceiver MiningRewardReceiver { get; private set; }
        public IResourceWallet ResourceWallet { get; private set; }
        public IGameplayEventSink GameplayEventSink { get; private set; }
        public IWorldSnapshotProvider WorldSnapshotProvider { get; private set; }
        public IDroneContextProvider DroneContextProvider { get; private set; }

        public int ConnectedBoundaryCount
        {
            get
            {
                var count = 0;
                if (MiningRewardReceiver != null) count++;
                if (ResourceWallet != null) count++;
                if (GameplayEventSink != null) count++;
                if (WorldSnapshotProvider != null) count++;
                if (DroneContextProvider != null) count++;
                return count;
            }
        }

        public bool AreAllConnected => ConnectedBoundaryCount == 5;

        public void Bind(
            IMiningRewardReceiver miningRewardReceiver,
            IResourceWallet resourceWallet,
            IGameplayEventSink gameplayEventSink,
            IWorldSnapshotProvider worldSnapshotProvider,
            IDroneContextProvider droneContextProvider)
        {
            MiningRewardReceiver = miningRewardReceiver;
            ResourceWallet = resourceWallet;
            GameplayEventSink = gameplayEventSink;
            WorldSnapshotProvider = worldSnapshotProvider;
            DroneContextProvider = droneContextProvider;
        }
    }
}
