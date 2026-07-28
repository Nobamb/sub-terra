using System;
using NUnit.Framework;
using SubTerra.App.UI.Hazards;

namespace SubTerra.App.Tests.UI
{
    public sealed class HazardHudPresenterTests
    {
        [Test]
        public void G_F05_ActualStatusesUpdateTextIconStateAndGasPriority()
        {
            var source = new FakeHazardSource();
            var view = new RecordingHazardView();
            var presenter = new HazardHudPresenter(view);
            presenter.Bind(source);

            var structural = new HazardStatusReadModel(
                HazardSeverity.Caution,
                "주의",
                "지지대 권장");
            var gas = new HazardStatusReadModel(
                HazardSeverity.Critical,
                "위험",
                "잔여 4.0초");
            var power = new PowerStatusReadModel(
                false,
                2,
                5,
                1,
                "케이블 미연결");

            source.EmitStructural(structural);
            source.EmitGas(gas);
            source.EmitPower(power);

            Assert.That(view.Structural.Severity, Is.EqualTo(HazardSeverity.Caution));
            Assert.That(view.Gas.ValueText, Is.EqualTo("잔여 4.0초"));
            Assert.That(view.Power.IsConnected, Is.False);
            Assert.That(view.Power.Reason, Does.Contain("미연결"));
            Assert.That(view.GasPriority, Is.True);

            presenter.Unbind();
            source.EmitGas(new HazardStatusReadModel(HazardSeverity.Safe, "안전", string.Empty));
            Assert.That(view.Gas.Severity, Is.EqualTo(HazardSeverity.Critical),
                "Scene 종료 후에는 파괴된 HUD 구독이 남지 않아야 합니다.");
        }

        private sealed class FakeHazardSource : IHazardStatusSource
        {
            public HazardStatusReadModel StructuralStatus { get; private set; } =
                new(HazardSeverity.Safe, "안전", string.Empty);
            public HazardStatusReadModel GasStatus { get; private set; } =
                new(HazardSeverity.Safe, "안전", string.Empty);
            public PowerStatusReadModel PowerStatus { get; private set; } =
                new(false, 0, 0, 0, string.Empty);

            public event Action<HazardStatusReadModel> StructuralStatusChanged;
            public event Action<HazardStatusReadModel> GasStatusChanged;
            public event Action<PowerStatusReadModel> PowerStatusChanged;

            public void EmitStructural(HazardStatusReadModel status)
            {
                StructuralStatus = status;
                StructuralStatusChanged?.Invoke(status);
            }

            public void EmitGas(HazardStatusReadModel status)
            {
                GasStatus = status;
                GasStatusChanged?.Invoke(status);
            }

            public void EmitPower(PowerStatusReadModel status)
            {
                PowerStatus = status;
                PowerStatusChanged?.Invoke(status);
            }
        }

        private sealed class RecordingHazardView : IHazardStatusView
        {
            public HazardStatusReadModel Structural;
            public HazardStatusReadModel Gas;
            public PowerStatusReadModel Power;
            public bool GasPriority;

            public void SetStructuralStatus(HazardStatusReadModel status)
            {
                Structural = status;
            }

            public void SetGasStatus(HazardStatusReadModel status)
            {
                Gas = status;
            }

            public void SetPowerStatus(PowerStatusReadModel status)
            {
                Power = status;
            }

            public void SetGasPriority(bool isPriority)
            {
                GasPriority = isPriority;
            }
        }
    }
}
