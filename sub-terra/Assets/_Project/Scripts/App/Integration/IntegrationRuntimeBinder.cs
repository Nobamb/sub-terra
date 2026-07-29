using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Connects scene-owned gameplay components to the persistent app runtime.
    /// </summary>
    public sealed class IntegrationRuntimeBinder :
        MonoBehaviour,
        IMiningRewardReceiver,
        IGameplayEventSink
    {
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private HudBinder hudBinder;

        private SaveRuntimeController runtime;

        private void Start()
        {
            runtime = SaveRuntimeController.Instance;
            var bootstrap = GameBootstrapper.Instance;
            if (runtime == null || bootstrap == null)
            {
                Debug.LogWarning(
                    "[SubTerra] Integration scene opened without the Bootstrap runtime.");
                return;
            }

            runtime.EnsureGameplayServices();
            runtime.InventoryService?.BindGameState(bootstrap.State);
            buildingPlacementSystem?.SetResourceWallet(runtime.Economy);
            hudBinder?.BindTo(bootstrap.State);
        }

        public void AddMineral(string mineralId, int quantity)
        {
            runtime ??= SaveRuntimeController.Instance;
            runtime?.InventoryService?.AddMineral(mineralId, quantity);
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            var state = GameBootstrapper.Instance?.State;
            if (state == null || gameplayEvent == null)
            {
                return;
            }

            if (gameplayEvent.type == GameplayEventType.StructuralRiskChanged)
            {
                state.SetStructuralRisk(ToStructuralRisk(gameplayEvent.structuralIntegrity));
            }
            else if (gameplayEvent.type == GameplayEventType.GasTriggered)
            {
                state.SetGasExposure(ToGasRisk(gameplayEvent.gasRisk));
            }
        }

        private static StructuralRiskLevel ToStructuralRisk(float integrity)
        {
            if (integrity <= 0.25f)
            {
                return StructuralRiskLevel.Critical;
            }

            return integrity <= 0.75f
                ? StructuralRiskLevel.Caution
                : StructuralRiskLevel.Safe;
        }

        private static GasRiskLevel ToGasRisk(float risk)
        {
            if (risk >= 0.7f)
            {
                return GasRiskLevel.Hazard;
            }

            return risk >= 0.3f
                ? GasRiskLevel.Elevated
                : GasRiskLevel.Safe;
        }
    }
}
