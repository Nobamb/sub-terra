using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-164: 효과음 D-1(이동·조작, 채굴·자원, 반복 소리 멈춤) 칸과 판정.</summary>
    public sealed class PromptB164SfxD1Tests
    {
        private static readonly (SfxId Id, int Number, string File, float Volume)[] Table =
        {
            (SfxId.Footstep, 51, "footstep", 0.65f),
            (SfxId.Jump, 52, "jump", 0.4f),
            (SfxId.Land, 53, "land", 0.45f),
            (SfxId.LandHeavy, 54, "land_heavy", 0.7f),
            (SfxId.LadderGrab, 55, "ladder_grab", 0.9f),
            (SfxId.LadderClimb, 56, "ladder_climb", 0.35f),
            (SfxId.LadderClimbB, 57, "ladder_climb_b", 0.35f),
            (SfxId.ElevatorDoor, 58, "elevator_door", 0.45f),
            (SfxId.ElevatorMoveLoop, 59, "elevator_move_loop", 0.15f),
            (SfxId.ElevatorArriveDing, 60, "elevator_arrive_ding", 0.45f),
            (SfxId.DrillLoop, 61, "drill_loop", 0.35f),
            (SfxId.BlockBreak, 62, "block_break", 0.3f),
            (SfxId.PickupCommon, 63, "pickup_common", 0.65f),
            (SfxId.PickupRare, 64, "pickup_rare", 0.5f),
            (SfxId.CargoHeavyWarning, 65, "cargo_heavy_warning", 0.2f),
        };

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        // ---- 칸 ----

        [Test]
        public void Defaults_D1SlotsNumberedFrom51_AllWorldBus()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(defaults.ContainsKey(pair.Entry.Id), Is.False, pair.Entry.Id.ToString());
                Assert.That(pair.FileName, Is.Not.EqualTo("drill_loop_fwdrev"), "비교용 드릴 루프는 칸을 만들지 않는다");
                defaults.Add(pair.Entry.Id, (pair.Entry, pair.FileName));
            }

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            var numbers = new HashSet<int>();
            foreach (var row in Table)
            {
                Assert.That((int)row.Id, Is.EqualTo(row.Number), row.Id.ToString());
                Assert.That(numbers.Add(row.Number), Is.True, row.Id.ToString());
                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, row.Id.ToString());
                Assert.That(actual.File, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.EqualTo(SfxBus.World), row.Id.ToString());
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume), row.Id.ToString());

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Clip, Is.Not.Null, row.Id.ToString());
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(asset.Bus, Is.EqualTo(SfxBus.World), row.Id.ToString());
            }

            Assert.That(SfxLibraryDefaults.IsLoopFile("elevator_move_loop"), Is.True);
            Assert.That(SfxLibraryDefaults.IsLoopFile("drill_loop"), Is.True);
            Assert.That(SfxLibraryDefaults.IsLoopFile("footstep"), Is.False);
            Assert.That((int)SfxId.PortalClose, Is.EqualTo(50), "기존 번호는 그대로");
            Assert.That(defaults.ContainsKey((SfxId)3), Is.False, "3번은 비워 둔다");
            Assert.That(SfxLibraryDefaults.Fallbacks.Count, Is.EqualTo(1), "대체 표는 그대로");
            Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>(
                SfxLibraryDefaults.ClipRoot + "/World/drill_loop_fwdrev.wav"), Is.Null);
        }

        [Test]
        public void Import_LoopFilesStayCompressed_OthersMono()
        {
            foreach (var row in Table)
            {
                var path = SfxLibraryDefaults.ClipRoot + "/World/" + row.File + ".wav";
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.forceToMono, Is.True, path);
                var expected = SfxLibraryDefaults.IsLoopFile(row.File)
                    ? AudioClipLoadType.CompressedInMemory
                    : AudioClipLoadType.DecompressOnLoad;
                Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(expected), path);
            }
        }

        // ---- 점프·착지 ----

        [Test]
        public void Landing_DamageHeavyOnly_NormalLand_SilentCases()
        {
            var now = 0f;
            var gate = new PlayerMoveSfxGate(() => now);
            Assert.That(gate.ResolveLanding(6f, true, false, true), Is.EqualTo(SfxId.LandHeavy), "피해면 LandHeavy만");

            now = 1f;
            Assert.That(gate.ResolveLanding(0.3f, false, false, true), Is.EqualTo(SfxId.Land), "0.3 이상이면 Land");

            now = 2f;
            Assert.That(gate.ResolveLanding(0.2f, false, false, true), Is.EqualTo(SfxId.None), "0.2 낙하는 무음");
            Assert.That(gate.ResolveLanding(3f, false, true, true), Is.EqualTo(SfxId.None), "사다리로 내려옴은 무음");
            Assert.That(gate.ResolveLanding(9f, true, false, false), Is.EqualTo(SfxId.None), "치명 낙하는 무음");
        }

        [Test]
        public void Jump_RightAfterLanding_IsSkipped()
        {
            var now = 0f;
            var gate = new PlayerMoveSfxGate(() => now);
            Assert.That(gate.ResolveLanding(1f, false, false, true), Is.EqualTo(SfxId.Land));

            now = 0.05f;
            Assert.That(gate.ResolveJump(), Is.EqualTo(SfxId.None), "착지 0.05초 뒤 점프는 무음");

            now = 0.2f;
            Assert.That(gate.ResolveJump(), Is.EqualTo(SfxId.Jump));

            now = 0.25f;
            Assert.That(gate.ResolveLanding(1f, false, false, true), Is.EqualTo(SfxId.None), "점프 직후 착지도 하나만");
        }

        // ---- 사다리 ----

        [Test]
        public void Ladder_WalkPastIsSilent_AirGrabAndRelease()
        {
            var gate = new LadderSfxGate(() => 0f);
            Assert.That(gate.OnEnter(true), Is.EqualTo(SfxId.None), "땅에서 들어오기만 하면 무음");
            Assert.That(gate.OnExit(false), Is.EqualTo(SfxId.None), "잡지 않았으면 놓음도 무음");

            Assert.That(gate.OnEnter(false), Is.EqualTo(SfxId.LadderGrab), "공중 잡기");
            Assert.That(gate.OnVerticalMove(), Is.EqualTo(SfxId.None), "이미 잡았으면 다시 안 냄");
            Assert.That(gate.OnExit(false), Is.EqualTo(SfxId.LadderGrab), "놓을 때 한 번");

            Assert.That(gate.OnEnter(true), Is.EqualTo(SfxId.None));
            Assert.That(gate.OnVerticalMove(), Is.EqualTo(SfxId.LadderGrab), "땅에서 켜진 뒤 처음 위아래로 움직임");
            Assert.That(gate.OnVerticalMove(), Is.EqualTo(SfxId.None));
            Assert.That(gate.OnExit(true), Is.EqualTo(SfxId.None), "OnDisable·위치 복원은 무음");
        }

        [Test]
        public void Ladder_StepsAlternate_AndRespectMinInterval()
        {
            var now = 0f;
            var gate = new LadderSfxGate(() => now);
            gate.OnEnter(false);
            var steps = new List<SfxId>();
            for (var i = 0; i < 4; i++)
            {
                now = i * 0.25f;
                steps.Add(gate.OnClimbStep());
            }

            Assert.That(steps, Is.EqualTo(new[]
            {
                SfxId.LadderClimb, SfxId.LadderClimbB, SfxId.LadderClimb, SfxId.LadderClimbB
            }));

            var tight = new LadderSfxGate(() => now);
            now = 5f;
            var played = 0;
            if (tight.OnClimbStep() != SfxId.None) played++;
            now = 5.1f;
            if (tight.OnClimbStep() != SfxId.None) played++;
            Assert.That(played, Is.EqualTo(1), "0.1초 간격 두 번이면 한 번");
        }

        // ---- 채굴 실패 ----

        [Test]
        public void MiningFail_SameReasonOnce_ReasonChangeOrNewPressPlaysAgain()
        {
            var gate = new MiningFailSfxGate();
            var count = 0;
            for (var i = 0; i < 3; i++)
            {
                if (gate.OnProgress(MiningPhase.Failed, MiningFailureReason.DrillLevelTooLow, false)) count++;
            }

            Assert.That(count, Is.EqualTo(1), "같은 이유 3번이면 1번");
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.InventoryFull, false), Is.True, "이유가 바뀌면 다시");
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.InventoryFull, false), Is.False);

            gate.OnMiningPressed();
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.InventoryFull, false), Is.True, "새로 누르면 다시");

            gate.OnProgress(MiningPhase.Mining, MiningFailureReason.None, false);
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.InventoryFull, false), Is.True, "다른 단계 뒤에는 다시");
        }

        [Test]
        public void MiningFail_InvalidTargetAndHiddenAreSilent()
        {
            var gate = new MiningFailSfxGate();
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.InvalidTarget, false), Is.False);
            gate.OnMiningPressed();
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.TargetChanged, false), Is.False);
            gate.OnMiningPressed();
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.DependencyMissing, false), Is.False);
            gate.OnMiningPressed();
            Assert.That(gate.OnProgress(MiningPhase.Failed, MiningFailureReason.NotMineable, true), Is.False, "설치 중 숨긴 문구");
            Assert.That(MiningProgressHud.FailureMessage(MiningFailureReason.NotMineable, true), Is.Empty);
        }

        // ---- 화물 ----

        [Test]
        public void CargoHeavy_PlaysOnCrossingOnly()
        {
            var first = new CargoHeavySfxGate();
            Assert.That(first.Observe(0.9f), Is.False, "첫 관측 0.9면 무음");

            var gate = new CargoHeavySfxGate();
            Assert.That(gate.Observe(0.79f), Is.False);
            Assert.That(gate.Observe(0.8f), Is.True, "0.79→0.8");
            Assert.That(gate.Observe(0.85f), Is.False);
            Assert.That(gate.Observe(0.9f), Is.False, "0.85→0.9");
            Assert.That(gate.Observe(0.7f), Is.False);
            Assert.That(gate.Observe(0.82f), Is.True, "내려갔다 다시 넘으면 또");
        }

        // ---- 픽업 ----

        [Test]
        public void Pickup_CommonRareAndGoldSilent()
        {
            Assert.That(MiningPickupSfx.Resolve("mineral.copper", 1, 0), Is.EqualTo(SfxId.PickupCommon));
            Assert.That(MiningPickupSfx.Resolve("item.rare.engine_fuel", 1, 0), Is.EqualTo(SfxId.PickupRare));
            Assert.That(MiningPickupSfx.Resolve("mineral.copper", 1, 30), Is.EqualTo(SfxId.None), "황금 지형은 골드 팝업이 맡음");
            Assert.That(MiningPickupSfx.Resolve(string.Empty, 1, 0), Is.EqualTo(SfxId.None));
            Assert.That(MiningPickupSfx.Resolve("mineral.copper", 0, 0), Is.EqualTo(SfxId.None));
        }

        // ---- 반복 소리 ----

        [Test]
        public void LoopGate_StopsOnPauseFailureAndDisable_ResumesAfterPause()
        {
            var gate = new WorldLoopGate();
            Assert.That(gate.Evaluate(true, false, false, true), Is.EqualTo(WorldLoopAction.Start));
            Assert.That(gate.Evaluate(true, false, false, true), Is.EqualTo(WorldLoopAction.Hold));
            Assert.That(gate.Evaluate(true, true, false, true), Is.EqualTo(WorldLoopAction.Stop), "일시정지");
            Assert.That(gate.Evaluate(true, false, false, true), Is.EqualTo(WorldLoopAction.Start), "풀리고 조건이 남으면 다시");
            Assert.That(gate.Evaluate(true, false, true, true), Is.EqualTo(WorldLoopAction.Stop), "실패 처리");
            Assert.That(gate.Evaluate(true, false, false, false), Is.EqualTo(WorldLoopAction.Hold), "체력 0이면 계속 멈춤");
            Assert.That(gate.Evaluate(true, false, false, true), Is.EqualTo(WorldLoopAction.Start));
            Assert.That(gate.Halt(), Is.EqualTo(WorldLoopAction.Stop), "OnDisable");
            Assert.That(gate.Evaluate(false, false, false, true), Is.EqualTo(WorldLoopAction.Hold));
            gate.Evaluate(true, false, false, true);
            Assert.That(gate.Evaluate(false, false, false, true), Is.EqualTo(WorldLoopAction.FadeOut), "조건이 꺼지면 줄이며 끝냄");
        }

        [Test]
        public void LoopAudio_DrillFadesAndStops_PauseAndDisable()
        {
            var host = new GameObject("B164Loop");
            created.Add(host);
            var wanted = true;
            var paused = false;
            var failing = false;
            var loop = WorldLoopAudio.Attach(host, SfxId.DrillLoop, () => wanted, () => failing, () => true);
            loop.ConfigureForTests(
                AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath),
                () => false,
                () => paused);
            Assert.That(WorldLoopAudio.Attach(host, SfxId.DrillLoop, () => wanted, () => failing, () => true),
                Is.SameAs(loop), "같은 칸은 하나");

            loop.Tick(0.02f);
            Assert.That(loop.IsPlaying, Is.True);
            Assert.That(loop.Source.loop, Is.True);
            Assert.That(loop.Fade, Is.InRange(0.01f, 0.99f), "0.05초 페이드인");
            loop.Tick(0.05f);
            Assert.That(loop.Fade, Is.EqualTo(1f));

            paused = true;
            loop.Tick(0.016f);
            Assert.That(loop.IsPlaying, Is.False, "일시정지면 바로 멈춤");
            paused = false;
            loop.Tick(0.016f);
            Assert.That(loop.IsPlaying, Is.True, "풀리면 다시");

            failing = true;
            loop.Tick(0.016f);
            Assert.That(loop.IsPlaying, Is.False, "실패 처리 중 멈춤");
            failing = false;
            loop.Tick(0.1f);

            wanted = false;
            loop.Tick(0.04f);
            Assert.That(loop.IsPlaying, Is.True, "0.08초 페이드아웃 중");
            loop.Tick(0.05f);
            Assert.That(loop.IsPlaying, Is.False, "페이드아웃 끝");

            wanted = true;
            loop.Tick(0.1f);
            Assert.That(loop.IsPlaying, Is.True);
            typeof(WorldLoopAudio).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(loop, null);
            Assert.That(loop.IsPlaying, Is.False, "OnDisable이면 멈춤");
        }

        [Test]
        public void PortalIdleLoop_StopsWhilePaused()
        {
            var host = new GameObject("B164Portal");
            created.Add(host);
            var paused = false;
            var loop = host.AddComponent<PortalIdleLoopAudio>();
            loop.ConfigureForTests(
                AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath),
                () => false,
                () => paused);
            var portals = new List<Vector2> { new Vector2(1f, 0f) };

            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.True);
            paused = true;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.False, "일시정지면 멈춤");
            paused = false;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.True);
        }
    }
}
