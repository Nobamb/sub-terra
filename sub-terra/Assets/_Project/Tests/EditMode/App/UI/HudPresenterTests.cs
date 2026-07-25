using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.Tests.UI
{
    public sealed class HudPresenterTests
    {
        [Test]
        public void C_F01_GoldOnlyChange_UpdatesGoldViewOnce()
        {
            var state = GameState.CreateNew();
            state.SetGold(5);
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);
            view.ResetCounts();

            state.AddGold(10);

            Assert.That(view.GoldCount, Is.EqualTo(1));
            Assert.That(view.Gold, Is.EqualTo("15"));
            Assert.That(view.EnergyCount, Is.Zero);
            Assert.That(view.DepthCount, Is.Zero);
            Assert.That(view.CargoCount, Is.Zero);
            Assert.That(view.UnsettledValueCount, Is.Zero);
            Assert.That(view.StructuralCount, Is.Zero);
            Assert.That(view.GasRiskCount, Is.Zero);
            Assert.That(view.BuildingCount, Is.Zero);
            Assert.That(view.InteractionCount, Is.Zero);
        }

        [Test]
        public void C_F02_InitialRender_MatchesNonZeroStateWithoutNewEvents()
        {
            var state = GameState.CreateNew();
            state.SetEnergy(40, 120);
            state.SetGold(77);
            state.SetDepth(12);
            state.SetInventory(3.5f, 42f);
            state.SetStructuralRisk(StructuralRiskLevel.Caution);
            state.SetGasExposure(GasRiskLevel.Elevated);
            state.SetBuildingSelection("building.light.basic", "기본 조명");
            state.SetInteractionPrompt("F: 상호작용");

            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);

            Assert.That(view.Energy, Is.EqualTo(HudFormatter.FormatEnergy(40, 120)));
            Assert.That(view.Gold, Is.EqualTo("77"));
            Assert.That(view.Depth, Is.EqualTo("12"));
            Assert.That(view.Cargo, Is.EqualTo(HudFormatter.FormatCargo(3.5f)));
            Assert.That(view.UnsettledValue, Is.EqualTo("42"));
            Assert.That(view.Structural, Is.EqualTo(HudFormatter.LabelCaution));
            Assert.That(view.GasRisk, Is.EqualTo(HudFormatter.LabelGasElevated));
            Assert.That(view.GasVisible, Is.True);
            Assert.That(view.Building, Is.EqualTo("기본 조명"));
            Assert.That(view.Interaction, Is.EqualTo("F: 상호작용"));

            // 최초 렌더는 이벤트 없이 State 스냅샷만 반영한다.
            Assert.That(view.EnergyCount, Is.EqualTo(1));
            Assert.That(view.GoldCount, Is.EqualTo(1));
        }

        [Test]
        public void SameValueSet_DoesNotRaiseEventsOrUpdateView()
        {
            var state = GameState.CreateNew();
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);
            view.ResetCounts();

            var energyEvents = 0;
            var creditEvents = 0;
            var depthEvents = 0;
            state.EnergyChanged += _ => energyEvents++;
            state.CreditsChanged += _ => creditEvents++;
            state.DepthChanged += _ => depthEvents++;

            state.SetEnergy(100, 100);
            state.SetGold(0);
            state.AddGold(0);
            state.SetDepth(0);
            state.SetCargoWeight(0f);
            state.SetUnsettledValue(0f);
            state.SetStructuralRisk(StructuralRiskLevel.Safe);
            state.SetGasExposure(GasRiskLevel.Safe);
            state.SetBuildingSelection(string.Empty, string.Empty);
            state.SetInteractionPrompt(string.Empty);

            Assert.That(energyEvents, Is.Zero);
            Assert.That(creditEvents, Is.Zero);
            Assert.That(depthEvents, Is.Zero);
            Assert.That(view.GoldCount, Is.Zero);
            Assert.That(view.EnergyCount, Is.Zero);
            Assert.That(view.DepthCount, Is.Zero);
        }

        [Test]
        public void Unbind_StopsFurtherViewUpdates()
        {
            var state = GameState.CreateNew();
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);
            presenter.Unbind();
            view.ResetCounts();

            state.AddGold(5);
            state.SetDepth(3);
            state.SetCurrentEnergy(50);

            Assert.That(view.GoldCount, Is.Zero);
            Assert.That(view.DepthCount, Is.Zero);
            Assert.That(view.EnergyCount, Is.Zero);
            Assert.That(presenter.IsBound, Is.False);
        }

        [Test]
        public void Rebind_FullRenderThenSelectiveUpdates()
        {
            var state = GameState.CreateNew();
            state.SetGold(9);
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);
            presenter.Unbind();
            view.ResetCounts();

            presenter.Bind(state);
            Assert.That(view.Gold, Is.EqualTo("9"));
            Assert.That(view.GoldCount, Is.EqualTo(1));

            view.ResetCounts();
            state.AddGold(1);
            Assert.That(view.GoldCount, Is.EqualTo(1));
            Assert.That(view.Gold, Is.EqualTo("10"));
            Assert.That(view.EnergyCount, Is.Zero);
        }

        [Test]
        public void InventoryChange_UpdatesCargoAndValueOnly()
        {
            var state = GameState.CreateNew();
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);
            view.ResetCounts();

            state.SetInventory(2f, 15f);

            Assert.That(view.CargoCount, Is.EqualTo(1));
            Assert.That(view.UnsettledValueCount, Is.EqualTo(1));
            Assert.That(view.GoldCount, Is.Zero);
            Assert.That(view.Cargo, Is.EqualTo("2"));
            Assert.That(view.UnsettledValue, Is.EqualTo("15"));
        }

        [Test]
        public void UiLayer_HasNoStateWriteSurfaceOnViewContract()
        {
            // IHudView는 표시 설정만 노출한다. GameState 파라미터나 의도 변경 API가 없어야 한다.
            foreach (var method in typeof(IHudView).GetMethods())
            {
                Assert.That(method.Name, Is.Not.EqualTo("AddGold"));
                Assert.That(method.Name, Is.Not.EqualTo("SetCurrentEnergy"));
                Assert.That(method.Name, Is.Not.EqualTo("SetCargoWeight"));
                foreach (var param in method.GetParameters())
                {
                    Assert.That(param.ParameterType, Is.Not.EqualTo(typeof(GameState)));
                    Assert.That(param.ParameterType.Namespace, Is.Not.EqualTo("SubTerra.App.State"));
                }
            }

            foreach (var method in typeof(HudPresenter).GetMethods())
            {
                Assert.That(method.Name, Is.Not.EqualTo("AddGold"));
                Assert.That(method.Name, Is.Not.EqualTo("SetDepth"));
                Assert.That(method.Name, Is.Not.EqualTo("SetGold"));
            }
        }
    }
}
