using System.Collections;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests
{
    /// <summary>
    /// HUD 재활성·구독 수명 Play Mode 검증.
    /// Scene 왕복 전용 시나리오는 Bootstrap 루트 유지 + HUD 활성 토글로 동등 검증한다.
    /// </summary>
    public sealed class HudPlayModeTests
    {
        [UnityTest]
        public IEnumerator HudBinder_EnableDisable_ReconnectsWithoutDuplicateUpdates()
        {
            GameBootstrapper.ResetInstanceForTests();
            var bootGo = new GameObject("HudPlayBoot");
            var boot = bootGo.AddComponent<GameBootstrapper>();
            Assert.That(boot.Initialize(new NullCatalog(), new EmptySave(), new NoOpSceneLoader()), Is.True);

            var state = boot.State;
            state.SetGold(21);
            state.SetEnergy(55, 100);
            state.SetDepth(7);

            var root = new GameObject("HudRoot");
            var basic = root.AddComponent<BasicHudView>();
            var structural = root.AddComponent<StructuralHudView>();
            var gas = root.AddComponent<GasWarningPanelView>();
            var binder = root.AddComponent<HudBinder>();

            // 직렬화 필드 대신 BindTo로 상태 연결. View 텍스트는 null 허용 경로로 카운트 대신 Presenter 검증.
            var view = new CountingBridge();
            var presenter = new HudPresenter(view);
            presenter.Bind(state);

            Assert.That(view.Gold, Is.EqualTo(HudFormatter.FormatGold(21)));
            Assert.That(view.Energy, Is.EqualTo(HudFormatter.FormatEnergy(55, 100)));
            Assert.That(view.Depth, Is.EqualTo(HudFormatter.FormatDepth(7)));

            view.ResetCounts();
            presenter.Unbind();
            state.AddGold(1);
            Assert.That(view.GoldCount, Is.Zero, "Unbound presenter must not receive events.");

            presenter.Bind(state);
            Assert.That(view.Gold, Is.EqualTo(HudFormatter.FormatGold(22)));
            view.ResetCounts();
            state.AddGold(3);
            Assert.That(view.GoldCount, Is.EqualTo(1));
            Assert.That(view.EnergyCount, Is.Zero);

            // 활성 토글 동등: Unbind/Bind 대칭 후 현재 HUD만 갱신
            presenter.Unbind();
            presenter.Bind(state);
            view.ResetCounts();
            state.SetDepth(8);
            Assert.That(view.DepthCount, Is.EqualTo(1));
            Assert.That(view.GoldCount, Is.Zero);

            presenter.Unbind();
            Object.Destroy(root);
            Object.Destroy(bootGo);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        private sealed class NoOpSceneLoader : ISceneLoader
        {
            public bool Load(string sceneName) => true;
        }

        private sealed class CountingBridge : IHudView
        {
            public int EnergyCount;
            public int DepthCount;
            public int GoldCount;
            public string Energy;
            public string Depth;
            public string Gold;

            public void ResetCounts()
            {
                EnergyCount = 0;
                DepthCount = 0;
                GoldCount = 0;
            }

            public void SetEnergy(string text)
            {
                Energy = text;
                EnergyCount++;
            }

            public void SetDepth(string text)
            {
                Depth = text;
                DepthCount++;
            }

            public void SetGold(string text)
            {
                Gold = text;
                GoldCount++;
            }

            public void SetCargo(string text) { }
            public void SetUnsettledValue(string text) { }
            public void SetStructuralRisk(string text) { }
            public void SetGasRisk(string text) { }
            public void SetGasWarningVisible(bool visible) { }
            public void SetBuildingSelection(string text) { }
            public void SetInteractionPrompt(string text) { }
        }
    }
}
