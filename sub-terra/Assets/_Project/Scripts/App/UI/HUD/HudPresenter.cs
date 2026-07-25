using SubTerra.App.State;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// State 이벤트 → View 선택 갱신. OnEnable 바인드 시 최초 전체 렌더 1회,
    /// 이후 변경 이벤트 항목만 View를 갱신한다. Update 폴링 없음.
    /// </summary>
    public sealed class HudPresenter
    {
        private readonly IHudView view;
        private GameState boundState;

        public HudPresenter(IHudView view)
        {
            this.view = view;
        }

        public bool IsBound => boundState != null;

        /// <summary>
        /// State 구독 + 최초 전체 렌더. 이전 구독은 먼저 해제한다.
        /// state가 null이면 기본값 표시만 하고 구독하지 않는다.
        /// </summary>
        public void Bind(GameState state)
        {
            Unbind();
            boundState = state;
            if (boundState == null)
            {
                RenderDefaults();
                return;
            }

            // 구독 수명: Bind/Unbind 대칭. Scene 왕복·재활성 시 중복 구독을 막는다.
            boundState.EnergyChanged += OnEnergyChanged;
            boundState.CreditsChanged += OnCreditsChanged;
            boundState.InventoryChanged += OnInventoryChanged;
            boundState.DepthChanged += OnDepthChanged;
            boundState.StructuralRiskChanged += OnStructuralRiskChanged;
            boundState.GasExposureChanged += OnGasExposureChanged;
            boundState.BuildingSelectionChanged += OnBuildingSelectionChanged;
            boundState.InteractionPromptChanged += OnInteractionPromptChanged;

            RenderAll();
        }

        public void Unbind()
        {
            if (boundState == null)
            {
                return;
            }

            boundState.EnergyChanged -= OnEnergyChanged;
            boundState.CreditsChanged -= OnCreditsChanged;
            boundState.InventoryChanged -= OnInventoryChanged;
            boundState.DepthChanged -= OnDepthChanged;
            boundState.StructuralRiskChanged -= OnStructuralRiskChanged;
            boundState.GasExposureChanged -= OnGasExposureChanged;
            boundState.BuildingSelectionChanged -= OnBuildingSelectionChanged;
            boundState.InteractionPromptChanged -= OnInteractionPromptChanged;
            boundState = null;
        }

        private void RenderAll()
        {
            var energy = boundState.GetEnergy();
            view.SetEnergy(HudFormatter.FormatEnergy(energy));
            view.SetDepth(HudFormatter.FormatDepth(boundState.Run.Depth));
            view.SetGold(HudFormatter.FormatGold(boundState.Player.Gold));
            var inv = boundState.GetInventory();
            view.SetCargo(HudFormatter.FormatCargo(inv.CargoWeight));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(inv.UnsettledValue));
            view.SetStructuralRisk(HudFormatter.FormatStructuralRisk(boundState.Run.StructuralRisk));
            var gas = boundState.Run.GasExposure;
            view.SetGasRisk(HudFormatter.FormatGasRisk(gas));
            view.SetGasWarningVisible(HudFormatter.ShouldShowGasWarning(gas));
            view.SetBuildingSelection(HudFormatter.FormatBuildingSelection(boundState.GetBuildingSelection()));
            view.SetInteractionPrompt(HudFormatter.FormatInteractionPrompt(boundState.InteractionPrompt));
        }

        private void RenderDefaults()
        {
            view.SetEnergy(HudFormatter.FormatEnergy(0, 0));
            view.SetDepth(HudFormatter.FormatDepth(0));
            view.SetGold(HudFormatter.FormatGold(0));
            view.SetCargo(HudFormatter.FormatCargo(0f));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(0f));
            view.SetStructuralRisk(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Safe));
            view.SetGasRisk(HudFormatter.FormatGasRisk(GasRiskLevel.Safe));
            view.SetGasWarningVisible(false);
            view.SetBuildingSelection(HudFormatter.DefaultBuildingNone);
            view.SetInteractionPrompt(HudFormatter.DefaultInteractionEmpty);
        }

        private void OnEnergyChanged(EnergyReadModel model)
        {
            view.SetEnergy(HudFormatter.FormatEnergy(model));
        }

        private void OnCreditsChanged(int gold)
        {
            view.SetGold(HudFormatter.FormatGold(gold));
        }

        private void OnInventoryChanged(InventoryReadModel model)
        {
            view.SetCargo(HudFormatter.FormatCargo(model.CargoWeight));
            view.SetUnsettledValue(HudFormatter.FormatUnsettledValue(model.UnsettledValue));
        }

        private void OnDepthChanged(int depth)
        {
            view.SetDepth(HudFormatter.FormatDepth(depth));
        }

        private void OnStructuralRiskChanged(StructuralRiskLevel level)
        {
            view.SetStructuralRisk(HudFormatter.FormatStructuralRisk(level));
        }

        private void OnGasExposureChanged(GasRiskLevel level)
        {
            view.SetGasRisk(HudFormatter.FormatGasRisk(level));
            view.SetGasWarningVisible(HudFormatter.ShouldShowGasWarning(level));
        }

        private void OnBuildingSelectionChanged(BuildingSelectionReadModel selection)
        {
            view.SetBuildingSelection(HudFormatter.FormatBuildingSelection(selection));
        }

        private void OnInteractionPromptChanged(string prompt)
        {
            view.SetInteractionPrompt(HudFormatter.FormatInteractionPrompt(prompt));
        }
    }
}
