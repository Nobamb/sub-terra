using System;
using SubTerra.App.Inventory;
using SubTerra.App.State;

namespace SubTerra.App.Progression
{
    /// <summary>
    /// 업그레이드 효과를 B 소유 최대 전력/화물 상태에 연결한다.
    /// Gameplay 효과는 Shared Provider를 직접 읽고, HUD/인벤토리는 기존 State 이벤트를 통해 갱신한다.
    /// </summary>
    public sealed class ProgressionDerivedStateSynchronizer : IDisposable
    {
        private readonly GameState gameState;
        private readonly InventoryService inventory;
        private readonly int baseMaximumEnergy;
        private readonly float baseMaximumCargo;

        private ProgressionService service;

        public ProgressionDerivedStateSynchronizer(
            GameState gameState,
            InventoryService inventory,
            int baseMaximumEnergy = 100,
            float baseMaximumCargo = InventoryState.DefaultMaxCapacity)
        {
            this.gameState = gameState;
            this.inventory = inventory;
            this.baseMaximumEnergy = baseMaximumEnergy < 0 ? 0 : baseMaximumEnergy;
            this.baseMaximumCargo = baseMaximumCargo < 0f ? 0f : baseMaximumCargo;
        }

        public void Bind(ProgressionService progression)
        {
            Unbind();
            service = progression;
            if (service != null)
            {
                service.UpgradeChanged += OnUpgradeChanged;
            }

            Refresh();
        }

        public void Unbind()
        {
            if (service != null)
            {
                service.UpgradeChanged -= OnUpgradeChanged;
                service = null;
            }
        }

        public void Refresh()
        {
            if (service == null)
            {
                return;
            }

            if (gameState != null)
            {
                var maximum = service.Effects.GetMaximumEnergy(baseMaximumEnergy);
                gameState.SetEnergy(gameState.Player.Energy, maximum);
            }

            if (inventory != null)
            {
                var maximum = service.Effects.GetMaximumCargoWeight(baseMaximumCargo);
                inventory.SetMaximumCapacity(maximum);
            }
        }

        public void Dispose()
        {
            Unbind();
        }

        private void OnUpgradeChanged(UpgradeSnapshot _)
        {
            Refresh();
        }
    }
}
