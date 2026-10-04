using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Outpost
{
    /// <summary>보건소·충전기 서비스 팝업: 시간표, 실제 값 연결, 표시 상태 전환, 잔여 상태 방지.</summary>
    public sealed class FacilityServicePopupTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";

        private GameObject instance;

        [TearDown]
        public void TearDown()
        {
            if (instance != null)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = null;
            }
        }

        // ---- 시간표 ----

        [TestCase(OutpostPanelMode.Clinic)]
        [TestCase(OutpostPanelMode.Charger)]
        public void Timeline_InfoAppearsWithinSixToEightTenthsSeconds(OutpostPanelMode mode)
        {
            var info = FacilityServicePopupTimeline.InfoTime(mode);

            Assert.That(info, Is.InRange(0.6f, 0.8f));
            Assert.That(FacilityServicePopupTimeline.Evaluate(mode, info).TextAlpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(FacilityServicePopupTimeline.Evaluate(mode, info * 0.5f).TextAlpha, Is.LessThan(0.2f));
        }

        [TestCase(OutpostPanelMode.Clinic)]
        [TestCase(OutpostPanelMode.Charger)]
        public void Timeline_StagesOverlap_PanelOpensBeforeFacilityEffectEnds(OutpostPanelMode mode)
        {
            var overlapped = false;
            for (var t = 0f; t < 1f; t += 0.005f)
            {
                var frame = FacilityServicePopupTimeline.Evaluate(mode, t);
                var effect = mode == OutpostPanelMode.Clinic
                    ? frame.LineAlpha * (frame.LineProgress > 0f ? 1f : 0f)
                    : frame.ArcAlpha + frame.Flash;
                if (frame.PanelOpen > 0.05f && effect > 0.05f)
                {
                    overlapped = true;
                    break;
                }
            }

            Assert.That(overlapped, Is.True, "아이콘→효과→펼침이 순서대로 끊기지 않고 겹쳐야 한다.");
        }

        [TestCase(OutpostPanelMode.Clinic)]
        [TestCase(OutpostPanelMode.Charger)]
        public void Timeline_AfterCompletion_StrongEffectsAreOffAndLayoutIsStable(OutpostPanelMode mode)
        {
            var frame = FacilityServicePopupTimeline.Evaluate(mode, FacilityServicePopupTimeline.TotalDuration(mode) + 0.5f);

            Assert.That(frame.LineAlpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(frame.ArcAlpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(frame.Flash, Is.EqualTo(0f).Within(0.001f));
            Assert.That(frame.Pulse, Is.LessThan(0.01f));
            Assert.That(frame.PanelGlow, Is.LessThan(0.01f));
            Assert.That(frame.PanelOpen, Is.EqualTo(1f));
            Assert.That(frame.IconMove, Is.EqualTo(1f));
            Assert.That(frame.IconScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(frame.TextAlpha, Is.EqualTo(1f));
        }

        [TestCase(OutpostPanelMode.Clinic)]
        [TestCase(OutpostPanelMode.Charger)]
        public void Timeline_PanelAndTextOnlyGrow_AndTextFollowsPanelOpening(OutpostPanelMode mode)
        {
            var previousOpen = 0f;
            var previousText = 0f;
            for (var t = 0f; t <= 1.2f; t += 0.01f)
            {
                var frame = FacilityServicePopupTimeline.Evaluate(mode, t);
                Assert.That(frame.PanelOpen, Is.GreaterThanOrEqualTo(previousOpen - 0.0001f));
                Assert.That(frame.TextAlpha, Is.GreaterThanOrEqualTo(previousText - 0.0001f));
                if (frame.TextAlpha > 0.05f)
                {
                    Assert.That(frame.PanelOpen, Is.GreaterThan(0.7f), "글자는 패널이 거의 펼쳐진 후반부에 나타난다.");
                }

                previousOpen = frame.PanelOpen;
                previousText = frame.TextAlpha;
            }
        }

        [Test]
        public void Timeline_ChargerFlashPlaysOnce()
        {
            var bursts = 0;
            var inside = false;
            for (var t = 0f; t <= 1f; t += 0.002f)
            {
                var bright = FacilityServicePopupTimeline.Evaluate(OutpostPanelMode.Charger, t).Flash > 0.5f;
                if (bright && !inside)
                {
                    bursts++;
                }

                inside = bright;
            }

            Assert.That(bursts, Is.EqualTo(1));
        }

        [Test]
        public void Timeline_ClinicHeartPulsesWhenEcgReachesCenter()
        {
            var peakTime = 0f;
            var peak = 0f;
            for (var t = 0f; t <= 0.7f; t += 0.002f)
            {
                var frame = FacilityServicePopupTimeline.Evaluate(OutpostPanelMode.Clinic, t);
                if (frame.Pulse > peak)
                {
                    peak = frame.Pulse;
                    peakTime = t;
                }
            }

            var atPeak = FacilityServicePopupTimeline.Evaluate(OutpostPanelMode.Clinic, peakTime);
            Assert.That(peak, Is.GreaterThan(0.95f));
            Assert.That(atPeak.LineProgress, Is.InRange(0.4f, 0.6f), "선이 중앙 부근을 지나는 순간 하트가 맥동한다.");
        }

        [Test]
        public void Ecg_IsDrawnProgressively_NotTranslated()
        {
            var early = new List<Vector2>();
            var late = new List<Vector2>();
            FacilityServicePopupTimeline.BuildEcg(250f, 80f, 0.6f, early);
            FacilityServicePopupTimeline.BuildEcg(250f, 80f, 0.95f, late);

            Assert.That(early.Count, Is.GreaterThan(2));
            for (var i = 0; i < early.Count - 1; i++)
            {
                Assert.That(late[i], Is.EqualTo(early[i]), "이미 그려진 부분은 움직이지 않는다.");
            }

            Assert.That(early[early.Count - 1].x, Is.EqualTo(-250f + 500f * 0.6f).Within(0.01f));
        }

        [Test]
        public void Ecg_RisesOnceNearCenter_ThenReturnsFlat()
        {
            var points = new List<Vector2>();
            FacilityServicePopupTimeline.BuildEcg(250f, 80f, 1f, points);

            var spikes = 0;
            for (var i = 1; i < points.Count - 1; i++)
            {
                if (points[i].y > 70f)
                {
                    spikes++;
                    Assert.That(Mathf.Abs(points[i].x), Is.LessThan(60f));
                }
            }

            Assert.That(spikes, Is.EqualTo(1));
            Assert.That(points[0].y, Is.EqualTo(0f));
            Assert.That(points[points.Count - 1].y, Is.EqualTo(0f));
            Assert.That(points[points.Count - 1].x, Is.EqualTo(250f).Within(0.01f));
        }

        [Test]
        public void Ecg_NothingDrawnBeforeStart()
        {
            var points = new List<Vector2> { Vector2.one };
            FacilityServicePopupTimeline.BuildEcg(250f, 80f, 0f, points);
            Assert.That(points, Is.Empty);
        }

        [Test]
        public void Hash_IsDeterministicAndInRange()
        {
            for (var seed = -50; seed < 500; seed++)
            {
                var value = FacilityServicePopupTimeline.Hash01(seed);
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                Assert.That(FacilityServicePopupTimeline.Hash01(seed), Is.EqualTo(value));
            }
        }

        // ---- 실제 값 연결 (Presenter) ----

        [Test]
        public void Charger_PassesRealBeforeAfterEnergyToView_AndChargesWithoutWaitingForAnimation()
        {
            var system = CreateSystem(null);
            system.State.SetEnergy(25, 100);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(FacilityStatus(DataIds.Buildings.ChargerBasic, true));

            presenter.ToggleInteractionPanel();

            Assert.That(system.State.Player.Energy, Is.EqualTo(100), "연출 완료를 기다리지 않고 즉시 충전된다.");
            Assert.That(view.VitalKind, Is.EqualTo(OutpostOperationKind.Charge));
            Assert.That(view.VitalBefore, Is.EqualTo(25f));
            Assert.That(view.VitalAfter, Is.EqualTo(100f));
            Assert.That(view.VitalMax, Is.EqualTo(100f));
            Assert.That(view.LastResult, Is.EqualTo("충전이 완료되었습니다."));
            presenter.Unbind();
        }

        [Test]
        public void Charger_WhenAlreadyFull_ReportsFullMessageWithFlatGauge()
        {
            var system = CreateSystem(null);
            system.State.SetEnergy(100, 100);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(FacilityStatus(DataIds.Buildings.ChargerBasic, true));

            presenter.ToggleInteractionPanel();

            Assert.That(view.LastResult, Is.EqualTo("이미 완전히 충전되었습니다."));
            Assert.That(view.VitalBefore, Is.EqualTo(100f));
            Assert.That(view.VitalAfter, Is.EqualTo(100f));
            presenter.Unbind();
        }

        [Test]
        public void Clinic_PassesRealBeforeAfterHealthToView()
        {
            var vitals = new FakeVitals { Current = 40f, Max = 100 };
            var system = CreateSystem(vitals);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(FacilityStatus(DataIds.Buildings.ClinicBasic, true));

            presenter.ToggleInteractionPanel();

            Assert.That(vitals.Current, Is.EqualTo(100f));
            Assert.That(view.VitalKind, Is.EqualTo(OutpostOperationKind.Heal));
            Assert.That(view.VitalBefore, Is.EqualTo(40f));
            Assert.That(view.VitalAfter, Is.EqualTo(100f));
            Assert.That(view.LastResult, Is.EqualTo("체력 회복이 완료되었습니다."));
            presenter.Unbind();
        }

        [Test]
        public void Clinic_WhenAlreadyFull_ReportsFullMessage()
        {
            var vitals = new FakeVitals { Current = 100f, Max = 100 };
            var system = CreateSystem(vitals);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(FacilityStatus(DataIds.Buildings.ClinicBasic, true));

            presenter.ToggleInteractionPanel();

            Assert.That(view.LastResult, Is.EqualTo("이미 체력이 최대입니다."));
            Assert.That(view.VitalBefore, Is.EqualTo(100f));
            Assert.That(view.VitalAfter, Is.EqualTo(100f));
            presenter.Unbind();
        }

        [Test]
        public void Clinic_WhenUnavailable_ShowsErrorWithCurrentValueAndDoesNotHeal()
        {
            var vitals = new FakeVitals { Current = 40f, Max = 100 };
            var system = CreateSystem(vitals);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(FacilityStatus(DataIds.Buildings.ClinicBasic, true));
            // 패널을 연 뒤 시설이 범위를 벗어난 상태에서의 직접 요청: 기존 판정 그대로 실패해야 한다.
            presenter.ToggleInteractionPanel();
            vitals.Current = 40f;
            system.Service.ClearRuntimeStatus();
            view.LastResult = null;

            var result = presenter.RequestHeal();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(vitals.Current, Is.EqualTo(40f));
            Assert.That(view.LastResultIsError, Is.True);
            Assert.That(view.VitalBefore, Is.EqualTo(40f));
            Assert.That(view.VitalAfter, Is.EqualTo(40f));
            presenter.Unbind();
        }

        [Test]
        public void ReadOnlyVital_DoesNotChangeState()
        {
            var vitals = new FakeVitals { Current = 40f, Max = 100 };
            var system = CreateSystem(vitals);
            system.State.SetEnergy(25, 100);

            Assert.That(system.Service.TryGetPlayerVital(OutpostOperationKind.Charge, out var energy, out var energyMax), Is.True);
            Assert.That(system.Service.TryGetPlayerVital(OutpostOperationKind.Heal, out var health, out var healthMax), Is.True);
            Assert.That(system.Service.TryGetPlayerVital(OutpostOperationKind.Deposit, out _, out _), Is.False);

            Assert.That(energy, Is.EqualTo(25f));
            Assert.That(energyMax, Is.EqualTo(100f));
            Assert.That(health, Is.EqualTo(40f));
            Assert.That(healthMax, Is.EqualTo(100f));
            Assert.That(system.State.Player.Energy, Is.EqualTo(25));
            Assert.That(vitals.Current, Is.EqualTo(40f));
        }

        // ---- 프리팹과 View ----

        [Test]
        public void Prefab_HasServicePopupWired_AndKeepsExistingPanel()
        {
            var view = Spawn();
            var popup = view.ServicePopup;

            Assert.That(popup, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(view.ServiceCloseButton, Is.Not.Null);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.transform.parent, Is.EqualTo(view.transform));
            Assert.That(view.PanelRoot.transform.Find("CloseButton"), Is.Not.Null, "기존 패널의 닫기 버튼은 그대로여야 한다.");
        }

        [TestCase(OutpostPanelMode.Clinic)]
        [TestCase(OutpostPanelMode.Charger)]
        public void ServiceMode_UsesPopup_NotLegacyPanel(OutpostPanelMode mode)
        {
            var view = Spawn();

            view.SetMode(mode);
            view.SetVisible(true);

            Assert.That(view.ServicePopup.IsShown, Is.True);
            Assert.That(view.ServicePopup.Mode, Is.EqualTo(mode));
            Assert.That(view.PanelRoot.activeSelf, Is.False);
            Assert.That(view.ActiveWindowRoot, Is.EqualTo(view.ServicePopup.gameObject));
            Assert.That(view.transform.Find("PanelRoot/ChargerRoot").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void CoreMode_UsesLegacyPanel_AndPopupStaysHidden()
        {
            var view = Spawn();

            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);

            Assert.That(view.PanelRoot.activeSelf, Is.True);
            Assert.That(view.ServicePopup.State, Is.EqualTo(FacilityServicePopupView.PlayState.Hidden));
            Assert.That(view.ActiveWindowRoot, Is.EqualTo(view.PanelRoot));
        }

        [Test]
        public void RepeatedRenderCalls_DoNotRestartPlayback()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Charger);
            view.SetVisible(true);
            view.ServicePopup.Tick(0.2f);
            var clock = view.ServicePopup.Clock;

            // Presenter는 스냅샷마다 SetVisible/SetMode를 다시 호출한다.
            view.SetVisible(true);
            view.SetMode(OutpostPanelMode.Charger);
            view.SetVisible(true);

            Assert.That(view.ServicePopup.Clock, Is.EqualTo(clock));
        }

        [Test]
        public void Playback_SettlesWithGaugeAtRealAfterValue()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Clinic);
            view.SetVisible(true);
            view.SetServiceVital(OutpostOperationKind.Heal, 40f, 100f, 100f);
            view.SetResult("체력 회복이 완료되었습니다.", false);

            Assert.That(view.ServicePopup.DisplayedGaugeFraction, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(view.ServicePopup.ValueMessage, Is.EqualTo("체력 40 / 100"));

            for (var i = 0; i < 120; i++)
            {
                view.ServicePopup.Tick(0.01f);
            }

            var popup = view.ServicePopup;
            Assert.That(popup.State, Is.EqualTo(FacilityServicePopupView.PlayState.Settled));
            Assert.That(popup.DisplayedGaugeFraction, Is.EqualTo(1f).Within(0.001f));
            Assert.That(popup.ValueMessage, Is.EqualTo("체력 100 / 100"));
            Assert.That(popup.ResultMessage, Is.EqualTo("체력 회복이 완료되었습니다."));
            Assert.That(popup.ContentGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Chargers_GaugeUsesEnergyValuesAndFormat()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Charger);
            view.SetVisible(true);
            view.SetServiceVital(OutpostOperationKind.Charge, 25f, 100f, 100f);

            Assert.That(view.ServicePopup.ValueMessage, Is.EqualTo("전력 25 / 100"));
            for (var i = 0; i < 120; i++)
            {
                view.ServicePopup.Tick(0.01f);
            }

            Assert.That(view.ServicePopup.ValueMessage, Is.EqualTo("전력 100 / 100"));
        }

        [Test]
        public void Text_IsNeverScaledWhilePanelBackdropUnfolds()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Clinic);
            view.SetVisible(true);
            var content = view.ServicePopup.ContentGroup.transform;
            var backdrop = view.ServicePopup.transform.Find("PanelBackdrop");
            var sawSmallBackdrop = false;

            for (var i = 0; i < 80; i++)
            {
                view.ServicePopup.Tick(0.01f);
                Assert.That(content.localScale, Is.EqualTo(Vector3.one));
                sawSmallBackdrop |= backdrop.localScale.y < 0.9f;
            }

            Assert.That(sawSmallBackdrop, Is.True);
            Assert.That(backdrop.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Close_PlaysShortExit_ThenDeactivatesWithoutResidualEffects()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Charger);
            view.SetVisible(true);
            view.ServicePopup.Tick(0.15f);

            view.SetMode(OutpostPanelMode.None);
            view.SetVisible(false);

            var popup = view.ServicePopup;
            Assert.That(popup.IsShown, Is.False);
            for (var i = 0; i < 40 && popup.gameObject.activeSelf; i++)
            {
                popup.Tick(0.01f);
            }

            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.State, Is.EqualTo(FacilityServicePopupView.PlayState.Hidden));
            Assert.That(view.PanelRoot.activeSelf, Is.False, "닫는 중 예전 큰 패널이 켜지면 안 된다.");
        }

        [Test]
        public void RapidCloseAndReopen_RestartsCleanly()
        {
            var view = Spawn();
            var popup = view.ServicePopup;
            view.SetMode(OutpostPanelMode.Clinic);
            view.SetVisible(true);
            popup.Tick(0.3f);

            view.SetMode(OutpostPanelMode.None);
            view.SetVisible(false);
            popup.Tick(0.05f);
            view.SetMode(OutpostPanelMode.Charger);
            view.SetVisible(true);

            Assert.That(popup.State, Is.EqualTo(FacilityServicePopupView.PlayState.Playing));
            Assert.That(popup.Mode, Is.EqualTo(OutpostPanelMode.Charger));
            Assert.That(popup.Clock, Is.EqualTo(0f));
            Assert.That(popup.gameObject.activeSelf, Is.True);
            Assert.That(popup.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Assert.That(popup.ResultMessage, Is.Empty, "이전 결과 문구가 남지 않는다.");
        }

        [Test]
        public void Popup_BlocksInputBehindItDuringPlayback()
        {
            var view = Spawn();
            view.SetMode(OutpostPanelMode.Clinic);
            view.SetVisible(true);

            var shield = view.ServicePopup.transform.Find("InputShield").GetComponent<UnityEngine.UI.Image>();
            Assert.That(shield.raycastTarget, Is.True);
            Assert.That(view.ServicePopup.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            var rect = (RectTransform)view.ServicePopup.transform;
            Assert.That(((RectTransform)shield.transform).rect.size, Is.EqualTo(rect.rect.size));
        }

        // ---- helpers ----

        private OutpostPanelView Spawn()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance.GetComponent<OutpostPanelView>();
        }

        private static (OutpostService Service, GameState State) CreateSystem(FakeVitals vitals)
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            return (new OutpostService(inventory, catalog, state, healthCommand: vitals), state);
        }

        private static OutpostStatusDto FacilityStatus(string buildingId, bool active)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = "facility.1",
                interactionFacilityBuildingId = buildingId,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "facility.1",
                        buildingId = buildingId,
                        isActive = active,
                        inactiveReasonId = active ? string.Empty : "power_disconnected"
                    }
                }
            };
        }

#pragma warning disable CS0067
        private sealed class FakeVitals : IPlayerHealthCommand, IPlayerHealthSource
        {
            public float Current;
            public int Max = 100;
            public event Action<PlayerHealthReadModel> HealthChanged;

            public PlayerHealthReadModel GetHealth() => new PlayerHealthReadModel(Current, Max);

            public bool RestoreFull()
            {
                if (Current >= Max)
                {
                    return false;
                }

                Current = Max;
                return true;
            }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
            public void SetHealthAbsolute(int health)
            {
                Current = health;
            }
#endif
        }
#pragma warning restore CS0067

        private sealed class RecordingView : IOutpostPanelView
        {
            public OutpostOperationKind VitalKind;
            public float VitalBefore = -1f;
            public float VitalAfter = -1f;
            public float VitalMax;
            public string LastResult;
            public bool LastResultIsError;

            public void SetVisible(bool visible) { }
            public void SetMode(OutpostPanelMode mode) { }
            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId) { }
            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetSettlementCargo(string cargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public void SetSelectedMineral(string summary) { }
            public void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId) { }
            public void ClearMineralSearch() { }

            public void SetResult(string message, bool isError)
            {
                LastResult = message;
                LastResultIsError = isError;
            }

            public void ShowTemporaryMessage(string message, float durationSeconds) { }
            public void SetTutorialVisible(bool visible) { }
            public void SetBusy(bool busy) { }

            public void SetServiceVital(OutpostOperationKind kind, float before, float after, float maximum)
            {
                VitalKind = kind;
                VitalBefore = before;
                VitalAfter = after;
                VitalMax = maximum;
            }
        }
    }
}
