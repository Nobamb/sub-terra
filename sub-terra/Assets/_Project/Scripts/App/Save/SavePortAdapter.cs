using SubTerra.App.Core;
using SubTerra.App.State;

namespace SubTerra.App.Save
{
    /// <summary>기존 Bootstrap의 최소 ISavePort 계약에 슬롯 로드를 연결한다.</summary>
    public sealed class SavePortAdapter : ISavePort
    {
        private readonly LoadService loader;
        private readonly int slotId;

        public SavePortAdapter(LoadService loadService, int saveSlotId)
        {
            loader = loadService;
            slotId = saveSlotId;
        }

        public bool HasSave => loader?.GetSlotMetadata(slotId).HasSave == true;

        public GameState Load()
        {
            var result = loader?.Load(slotId);
            return result != null && result.IsSuccess ? result.State?.GameState : null;
        }
    }
}
