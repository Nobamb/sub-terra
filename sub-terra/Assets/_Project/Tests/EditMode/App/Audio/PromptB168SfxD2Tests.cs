using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Progression;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-168: 효과음 D-2(위험·생존, 시설 설치·업그레이드, 실패음 입구) 칸과 판정.</summary>
    public sealed class PromptB168SfxD2Tests
    {
        private static readonly (SfxId Id, int Number, string File, SfxBus Bus, float Volume)[] Table =
        {
            (SfxId.GasPulseLoop, 66, "gas_pulse_loop", SfxBus.World, 0.38f),
            (SfxId.PlayerHurt, 67, "player_hurt", SfxBus.World, 0.6f),
            (SfxId.LowHealthWarning, 68, "low_health_warning", SfxBus.World, 0.5f),
            (SfxId.CeilingCollapse, 69, "ceiling_collapse", SfxBus.World, 0.7f),
            (SfxId.RescueHoloOpen, 70, "rescue_holo_open", SfxBus.World, 0.7f),
            (SfxId.BuildPlaceSmall, 71, "build_place_small", SfxBus.World, 0.9f),
            (SfxId.BuildPlaceHeavy, 72, "build_place_heavy", SfxBus.World, 1.0f),
            (SfxId.BuildFailResource, 73, "build_fail_resource", SfxBus.UI, 0.5f),
            (SfxId.BuildFailBlocked, 74, "build_fail_blocked", SfxBus.UI, 0.55f),
            (SfxId.UpgradeLevelUp, 75, "upgrade_levelup", SfxBus.UI, 0.55f),
            (SfxId.UnlockRattle, 76, "unlock_rattle", SfxBus.UI, 0.5f),
            (SfxId.UnlockClick, 77, "unlock_click", SfxBus.UI, 0.7f),
            (SfxId.UnlockGlow, 78, "unlock_glow", SfxBus.UI, 0.45f),
            (SfxId.WindowTvOpen, 79, "window_tv_open", SfxBus.UI, 0.4f),
            (SfxId.WindowTvClose, 80, "window_tv_close", SfxBus.UI, 0.5f),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> requests = new List<SfxId>();
        private float now;
        private int frame;

        [SetUp]
        public void SetUp()
        {
            requests.Clear();
            now = 0f;
            frame = 1;
            SfxRequests.PlayRequested += Record;
            FailureFeedback.ResetForTests(() => now, () => frame);
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            FailureFeedback.ResetForTests(null, null);
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            requests.Clear();
        }

        private void Record(SfxId id, float pitch) => requests.Add(id);

        // ---- 칸 ----

        [Test]
        public void Defaults_D2SlotsNumberedFrom66_MatchTableAndAsset()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(defaults.ContainsKey(pair.Entry.Id), Is.False, pair.Entry.Id.ToString());
                defaults.Add(pair.Entry.Id, (pair.Entry, pair.FileName));
            }

            Assert.That(defaults.Count, Is.EqualTo(79));
            Assert.That(defaults.ContainsKey((SfxId)3), Is.False, "3번은 비워 둔다");
            Assert.That((int)SfxId.CargoHeavyWarning, Is.EqualTo(65));

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            var numbers = new HashSet<int>();
            foreach (var row in Table)
            {
                Assert.That((int)row.Id, Is.EqualTo(row.Number), row.Id.ToString());
                Assert.That(numbers.Add(row.Number), Is.True, row.Id.ToString());

                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, row.Id.ToString());
                Assert.That(actual.File, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume).Within(0.0001f), row.Id.ToString());

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Clip, Is.Not.Null, row.Id.ToString());
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(asset.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(asset.Volume, Is.EqualTo(actual.Entry.Volume).Within(0.0001f), row.Id.ToString());
                Assert.That(asset.StartTime, Is.EqualTo(actual.Entry.StartTime).Within(0.0001f), row.Id.ToString());
                Assert.That(asset.MaxDuration, Is.EqualTo(actual.Entry.MaxDuration).Within(0.0001f), row.Id.ToString());
                Assert.That(asset.FadeOut, Is.EqualTo(actual.Entry.FadeOut).Within(0.0001f), row.Id.ToString());

                var path = SfxLibraryDefaults.ClipRoot + "/" + SfxLibraryDefaults.FolderOf(row.Bus) + "/" + row.File + ".wav";
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.forceToMono, Is.True, path);
            }

            Assert.That(defaults[SfxId.UnlockRattle].Entry.MaxDuration, Is.EqualTo(0.3f));
            Assert.That(defaults[SfxId.UnlockRattle].Entry.FadeOut, Is.EqualTo(0.1f));
            Assert.That(SfxLibraryDefaults.IsLoopFile("gas_pulse_loop"), Is.False, "반복 파일이 아니다");

            foreach (var pair in SfxLibraryDefaults.Create())
            {
                var file = pair.FileName;
                Assert.That(file.StartsWith("upgrade_lock"), Is.False, "잠금 연출은 B-169");
                Assert.That(file, Is.Not.EqualTo("upgrade_window_suck_in"), "잠금 연출은 B-169");
            }
        }

        // ---- 실패음 입구 ----

        [Test]
        public void FailureFeedback_SameFrameOnce_SameKeyWithin03sIgnored()
        {
            frame = 1;
            now = 0f;
            Assert.That(FailureFeedback.Play(SfxId.BuildFailBlocked, "a"), Is.True);
            Assert.That(FailureFeedback.Play(SfxId.BuildFailResource, "b"), Is.False, "같은 프레임은 한 번");
            Assert.That(requests.Count, Is.EqualTo(1));

            frame = 2;
            now = 0.2f;
            Assert.That(FailureFeedback.Play(SfxId.BuildFailBlocked, "a"), Is.False, "같은 key 0.3초 안");
            Assert.That(requests.Count, Is.EqualTo(1));

            frame = 3;
            now = 0.35f;
            Assert.That(FailureFeedback.Play(SfxId.BuildFailBlocked, "a"), Is.True);
            Assert.That(requests.Count, Is.EqualTo(2));

            frame = 4;
            now = 0.36f;
            Assert.That(FailureFeedback.Play(SfxId.BuildFailResource, "b"), Is.True, "앞 프레임에 버려진 b는 기록이 없다");
            Assert.That(requests.Count, Is.EqualTo(3));
        }

        [Test]
        public void PlacementFailure_ResourceVsBlocked_EveryRetry()
        {
            Assert.That(BuildingPlacementSfx.ResolveFailure(true, "occupied"), Is.EqualTo(SfxId.BuildFailResource));
            Assert.That(BuildingPlacementSfx.ResolveFailure(false, "cannot_afford"), Is.EqualTo(SfxId.BuildFailResource));
            Assert.That(BuildingPlacementSfx.ResolveFailure(false, "spend_failed"), Is.EqualTo(SfxId.BuildFailResource));
            foreach (var reason in new[] { "occupied", "missing_ground", "elevator_space", "out_of_range", "outside_allowed_area" })
            {
                Assert.That(BuildingPlacementSfx.ResolveFailure(false, reason), Is.EqualTo(SfxId.BuildFailBlocked), reason);
            }

            var times = new[] { 0f, 0.5f, 1.0f };
            for (var i = 0; i < times.Length; i++)
            {
                frame = i + 1;
                now = times[i];
                FailureFeedback.Play(BuildingPlacementSfx.ResolveFailure(false, "occupied"), BuildingPlacementSfx.PlaceFailureKey);
            }

            Assert.That(requests.Count, Is.EqualTo(3));
            Assert.That(requests, Is.All.EqualTo(SfxId.BuildFailBlocked));

            Assert.That(BuildingPlacementSfx.ResolvePlaced("building.ladder.basic"), Is.EqualTo(SfxId.BuildPlaceSmall));
            Assert.That(BuildingPlacementSfx.ResolvePlaced("building.support.basic"), Is.EqualTo(SfxId.BuildPlaceSmall));
            Assert.That(BuildingPlacementSfx.ResolvePlaced("building.light.basic"), Is.EqualTo(SfxId.BuildPlaceSmall));
            Assert.That(BuildingPlacementSfx.ResolvePlaced("building.outpost_core.basic"), Is.EqualTo(SfxId.BuildPlaceHeavy));
            Assert.That(BuildingPlacementSfx.ResolvePlaced(string.Empty), Is.EqualTo(SfxId.None));
            Assert.That(BuildingPlacementSfx.ResolvePlaced(null), Is.EqualTo(SfxId.None));
        }

        // ---- 업그레이드 ----

        [Test]
        public void UpgradeFailure_ResourceAndLockedTones()
        {
            Assert.That(UpgradeTreeView.ResolvePurchaseFailureSfx(ProgressionPurchaseStatus.InsufficientResources),
                Is.EqualTo(SfxId.BuildFailResource));
            Assert.That(UpgradeTreeView.ResolvePurchaseFailureSfx(ProgressionPurchaseStatus.Locked),
                Is.EqualTo(SfxId.BuildFailBlocked));
            Assert.That(UpgradeTreeView.ResolvePurchaseFailureSfx(ProgressionPurchaseStatus.MaximumLevel),
                Is.EqualTo(SfxId.None));
            Assert.That(UpgradeTreeView.ResolvePurchaseFailureSfx(ProgressionPurchaseStatus.Busy),
                Is.EqualTo(SfxId.None));
        }

        // ---- 위험·생존 ----

        [Test]
        public void Hurt_GasAndCollapseOnce_FallAndFinalBlowSilent()
        {
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.StructuralCollapse, 10, false),
                Is.EqualTo(SfxId.PlayerHurt));
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.GasExposure, 5, false),
                Is.EqualTo(SfxId.PlayerHurt));
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.Fall, 20, false),
                Is.EqualTo(SfxId.None), "낙하는 착지음이 맡는다");
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.StructuralCollapse, 30, true),
                Is.EqualTo(SfxId.None), "마지막 한 방은 실패 창에 양보");
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.GasExposure, 0, false),
                Is.EqualTo(SfxId.None));
            Assert.That(PlayerSurvivalController.ResolveHurtSfx(RunFailureCause.PowerDepleted, 10, false),
                Is.EqualTo(SfxId.None));

            Assert.That(StructuralRiskFeedback.ShouldPlayCollapseSfx(null), Is.False);
            var empty = new StructuralCollapseEventDto();
            Assert.That(StructuralRiskFeedback.ShouldPlayCollapseSfx(empty), Is.False);
            empty.cells.Add(new CollapseCellDto { x = 1, y = 2 });
            Assert.That(StructuralRiskFeedback.ShouldPlayCollapseSfx(empty), Is.True);
        }

        [Test]
        public void LowHealth_FirstDropBelow30Once_RecoverRearms_FirstObservationSilent()
        {
            var gate = new LowHealthSfxGate();
            var plays = 0;
            gate.Observe(new PlayerHealthReadModel(100, 100), 0f);
            gate.Observe(new PlayerHealthReadModel(50, 100), 1f);
            Assert.That(gate.Tick(1.3f, false), Is.False);
            gate.Observe(new PlayerHealthReadModel(25, 100), 2f);
            Assert.That(gate.Tick(2.1f, false), Is.False, "0.25초 전");
            if (gate.Tick(2.26f, false)) plays++;
            Assert.That(gate.Tick(2.5f, false), Is.False);
            gate.Observe(new PlayerHealthReadModel(20, 100), 3f);
            Assert.That(gate.Tick(3.5f, false), Is.False, "같은 저체력 구간에서는 반복하지 않는다");
            gate.Observe(new PlayerHealthReadModel(50, 100), 4f);
            gate.Observe(new PlayerHealthReadModel(28, 100), 5f);
            if (gate.Tick(5.3f, false)) plays++;
            Assert.That(plays, Is.EqualTo(2), "회복 뒤 다시 떨어지면 한 번 더");

            var first = new LowHealthSfxGate();
            first.Observe(new PlayerHealthReadModel(20, 100), 0f);
            Assert.That(first.Tick(1f, false), Is.False, "첫 관측은 무음");
            first.Observe(new PlayerHealthReadModel(15, 100), 1f);
            Assert.That(first.Tick(2f, false), Is.False);

            var death = new LowHealthSfxGate();
            death.Observe(new PlayerHealthReadModel(100, 100), 0f);
            death.Observe(new PlayerHealthReadModel(25, 100), 1f);
            death.Observe(new PlayerHealthReadModel(0, 100), 1.1f);
            Assert.That(death.Tick(1.3f, false), Is.False, "사망하면 취소");

            var cancelled = new LowHealthSfxGate();
            cancelled.Observe(new PlayerHealthReadModel(100, 100), 0f);
            cancelled.Observe(new PlayerHealthReadModel(25, 100), 1f);
            Assert.That(cancelled.Tick(1.3f, true), Is.False, "기다리는 동안 실패 처리");
            Assert.That(cancelled.Tick(1.5f, false), Is.False);

            var maxOnly = new LowHealthSfxGate();
            maxOnly.Observe(new PlayerHealthReadModel(30, 100), 0f);
            maxOnly.Observe(new PlayerHealthReadModel(30, 120), 1f);
            Assert.That(maxOnly.Tick(2f, false), Is.False, "최대 체력만 바뀌면 무음");
        }

        [Test]
        public void GasPulse_GateCrossesPeakOnce_CriticalPitch()
        {
            var gate = new GasPulseGate();
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.1f), Is.False, "첫 표본");
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.4f), Is.False);
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.55f), Is.True);
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.9f), Is.False);
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.2f), Is.False, "0.5를 넘지 않은 감김");
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.45f), Is.False);
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.95f), Is.True, "한 번만");
            Assert.That(gate.Observe(false, GasRiskLevel.Elevated, 0.6f), Is.True, "1.0을 넘어 0.5를 지나는 감김");

            var blocked = new GasPulseGate();
            Assert.That(blocked.Observe(false, GasRiskLevel.Elevated, 0.3f), Is.False);
            Assert.That(blocked.Observe(true, GasRiskLevel.Elevated, 0.6f), Is.False, "막히면 무음");
            Assert.That(blocked.Observe(false, GasRiskLevel.Elevated, 0.7f), Is.False, "풀리면 기준을 다시 잡는다");
            Assert.That(blocked.Observe(false, GasRiskLevel.Elevated, 0.4f), Is.False);
            Assert.That(blocked.Observe(false, GasRiskLevel.Elevated, 0.52f), Is.True, "다음 맥박부터");

            var changed = new GasPulseGate();
            Assert.That(changed.Observe(false, GasRiskLevel.Elevated, 0.4f), Is.False);
            Assert.That(changed.Observe(false, GasRiskLevel.Hazard, 0.6f), Is.False, "단계가 바뀌면 기준 재설정");

            var safe = new GasPulseGate();
            Assert.That(safe.Observe(false, GasRiskLevel.Safe, 0.6f), Is.False);

            Assert.That(GasPulseGate.PitchFor(GasRiskLevel.Hazard), Is.EqualTo(1.08f).Within(0.0001f));
            Assert.That(GasPulseGate.PitchFor(GasRiskLevel.Elevated), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GasPulseAudio_PulsesOnVignettePeak_StopsWhenPausedRidingOrFailing()
        {
            var go = new GameObject("GasOverlayTest");
            created.Add(go);
            var view = go.AddComponent<GasPoisonOverlayView>();
            view.ResetImmediate();
            view.Timeline.SetLevel(GasRiskLevel.Elevated);

            var paused = false;
            var riding = false;
            var failing = false;
            var canAct = true;
            var pulse = GasPulseAudio.Attach(view, () => failing, () => canAct, () => riding);
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            pulse.ConfigureForTests(library, () => false, () => paused, () => false);

            Run(view, pulse, 2.5f);
            Assert.That(pulse.PulseCount, Is.EqualTo(1), "주기 2초의 0.5 지점에서 한 번");
            Assert.That(library.TryGetEntry(SfxId.GasPulseLoop, out var entry), Is.True);
            Assert.That(pulse.Source.pitch, Is.EqualTo(entry.Pitch * GasPulseGate.CautionPitch).Within(0.001f));

            var count = pulse.PulseCount;
            paused = true;
            Run(view, pulse, 2.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count), "일시정지");

            paused = false;
            riding = true;
            Run(view, pulse, 2.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count), "엘리베이터 탑승");

            riding = false;
            failing = true;
            Run(view, pulse, 2.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count), "실패 처리");

            failing = false;
            canAct = false;
            Run(view, pulse, 2.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count), "조작 불가");

            canAct = true;
            Run(view, pulse, 2.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count + 1), "풀리면 다음 맥박");

            view.Timeline.SetLevel(GasRiskLevel.Hazard);
            count = pulse.PulseCount;
            Run(view, pulse, 3.0f);
            Assert.That(pulse.PulseCount, Is.GreaterThanOrEqualTo(count + 2), "위험 단계 주기 1.2초");
            Assert.That(pulse.Source.pitch, Is.EqualTo(entry.Pitch * GasPulseGate.CriticalPitch).Within(0.001f));

            view.Timeline.SetLevel(GasRiskLevel.Safe);
            count = pulse.PulseCount;
            Run(view, pulse, 3.0f);
            Assert.That(pulse.PulseCount, Is.EqualTo(count), "안전 지대에서는 무음");
        }

        // 0.05초 단위로 타임라인과 맥박 판정을 함께 흘린다(View의 Update와 LateUpdate 순서와 같다).
        private static void Run(GasPoisonOverlayView view, GasPulseAudio pulse, float seconds)
        {
            const float step = 0.05f;
            var steps = Mathf.RoundToInt(seconds / step);
            for (var i = 0; i < steps; i++)
            {
                view.Timeline.Advance(step);
                pulse.Tick(step);
            }
        }
    }
}
