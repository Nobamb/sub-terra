using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    // B-210: 새 모션 4종(가스 피해·사다리 오르기·사다리 피격·사다리 채굴)과 피격 경직.
    public sealed class PromptB210PlayerMotionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const float Stun = PlayerSurvivalSettings.DefaultHitStunSeconds;

        private GameObject player;
        private GameObject survivalObject;
        private PlayerMovement movement;
        private PlayerAnimationController animation;
        private PlayerFacing facing;
        private SpriteRenderer renderer;
        private PlayerSurvivalController survival;
        private Rigidbody2D body;

        [SetUp]
        public void SetUp()
        {
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab));
            movement = player.GetComponent<PlayerMovement>();
            animation = player.GetComponentInChildren<PlayerAnimationController>();
            facing = player.GetComponent<PlayerFacing>();
            renderer = animation.GetComponent<SpriteRenderer>();
            body = player.GetComponent<Rigidbody2D>();
            Invoke(movement, "Awake");
            Invoke(animation, "Awake");
            Invoke(facing, "Awake");
            // 통합 씬처럼 생존 컨트롤러는 따로 있고 BindMovement로 이동에 경직을 알린다.
            survivalObject = new GameObject("B210 survival");
            survival = survivalObject.AddComponent<PlayerSurvivalController>();
            Invoke(survival, "Awake");
            survival.BindTarget(player.transform);
            survival.BindMovement(movement);
            animation.BindSurvival(survival);
            Set(animation, "previousHealth", survival.State.Health);
            Set(movement, "<IsGrounded>k__BackingField", true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(survivalObject);
        }

        // ------------------------------------------------------------------ 경직 규칙(순수 상태)

        [TestCase(RunFailureCause.GasExposure, true)]
        [TestCase(RunFailureCause.Fall, true)]
        [TestCase(RunFailureCause.StructuralCollapse, true)]
        [TestCase(RunFailureCause.OxygenDepleted, false)]
        public void HitStun_OnlyGasFallCollapse(RunFailureCause cause, bool stuns)
        {
            var state = new PlayerSurvivalState(100);
            Assert.That(state.TryApplyDamage(cause, 5f, 10f, 0.75f, false, out _), Is.True);
            Assert.That(state.TryStartHitStun(cause, 10f, Stun), Is.EqualTo(stuns));
            Assert.That(state.IsHitStunned(10.39f), Is.EqualTo(stuns));
            Assert.That(state.IsHitStunned(10.4f), Is.False);
        }

        [Test]
        public void HitStun_ReDamageDoesNotExtendOrRestart()
        {
            var state = new PlayerSurvivalState(100);
            Assert.That(state.TryStartHitStun(RunFailureCause.GasExposure, 0f, Stun), Is.True);
            Assert.That(state.TryStartHitStun(RunFailureCause.Fall, 0.3f, Stun), Is.False);
            Assert.That(state.HitStunEndsAt, Is.EqualTo(Stun).Within(1e-5f), "끝 시각은 처음 경직 그대로");
            Assert.That(state.IsHitStunned(0.41f), Is.False);
        }

        [Test]
        public void GasTicksEverySecond_LeaveTimeToMoveBetweenTicks()
        {
            var state = new PlayerSurvivalState(100);
            float free = 0f;
            for (var tick = 0; tick < 5; tick++)
            {
                float at = tick * 1f;
                Assert.That(state.TryApplyDamage(RunFailureCause.GasExposure, 2f, at, 0.75f, false, out _), Is.True);
                Assert.That(state.TryStartHitStun(RunFailureCause.GasExposure, at, Stun), Is.True);
                for (var t = at; t < at + 1f; t += 0.01f)
                    if (!state.IsHitStunned(t)) free += 0.01f;
            }

            Assert.That(free / 5f, Is.GreaterThanOrEqualTo(0.59f), "틱 사이 최소 0.6초는 움직일 수 있다");
            Assert.That(PlayerSurvivalSettings.IsHitStunLongerThanInvulnerability(Stun, 0.75f), Is.False);
            Assert.That(PlayerSurvivalSettings.IsHitStunLongerThanInvulnerability(0.8f, 0.75f), Is.True);
        }

        [Test]
        public void Knockout_ClearsHitStun_RescueResets()
        {
            survival.ApplyCollapseImpactWithoutCameraShake(100f);
            Assert.That(survival.State.CanAct, Is.False);
            Assert.That(survival.IsHitStunned, Is.False, "기절이 경직보다 우선");
            survival.RestoreAfterRescue();
            Assert.That(survival.IsHitStunned, Is.False);
        }

        // ------------------------------------------------------------------ 실제 피해 경로

        [Test]
        public void Gas_Fall_Collapse_StartStunForHitStunSeconds_OxygenDoesNot()
        {
            Assert.That(Gas(), Is.True);
            Assert.That(survival.IsHitStunned, Is.True);
            Assert.That(survival.HitStunEndsAt - Time.unscaledTime, Is.EqualTo(Stun).Within(0.02f));

            Reset();
            Assert.That(survival.SimulateFall(30f), Is.True);
            Assert.That(survival.IsHitStunned, Is.True);

            Reset();
            Assert.That(survival.ApplyCollapseImpactWithoutCameraShake(), Is.True);
            Assert.That(survival.IsHitStunned, Is.True);

            Reset();
            Assert.That(Oxygen(), Is.True);
            Assert.That(survival.IsHitStunned, Is.False, "산소 0 틱은 경직 없음");
        }

        // ------------------------------------------------------------------ 입력 무시

        [Test]
        public void Stunned_IgnoresMoveJumpAndPlatform_GroundStopsInPlace_ThenRecovers()
        {
            movement.SetMoveInput(1f);
            Assert.That(movement.IsMovementRequested, Is.True);
            Gas();
            Assert.That(movement.IsHitStunned, Is.True);
            Assert.That(movement.IsMovementRequested, Is.False, "경직 중 이동 입력 무시");
            body.linearVelocity = new Vector2(5f, 0f);
            Invoke(movement, "ApplyHorizontalMovement");
            Assert.That(body.linearVelocityX, Is.Zero, "땅에서는 그 자리에 선다");
            movement.RequestJump();
            Assert.That(Invoke(movement, "TryApplyJump"), Is.EqualTo(false), "점프 무시");
            Assert.That(movement.TryAdjustStandingPlatform(1), Is.False);
            movement.SetMoveInput(-1f);
            Assert.That(movement.FacingDirection, Is.EqualTo(1f), "경직 중 방향도 바뀌지 않는다");

            survival.State.ClearHitStun();
            Assert.That(movement.IsMovementRequested, Is.True, "끝나면 누르고 있던 입력이 바로 이어진다");
            Invoke(movement, "ApplyHorizontalMovement");
            Assert.That(body.linearVelocityX, Is.LessThan(5f));
        }

        [Test]
        public void Airborne_Stunned_NoHorizontalControl_StillFallsByGravity()
        {
            Set(movement, "<IsGrounded>k__BackingField", false);
            body.linearVelocity = new Vector2(0f, -3f);
            Gas();
            movement.SetMoveInput(1f);
            Invoke(movement, "ApplyHorizontalMovement");
            Assert.That(body.linearVelocityX, Is.Zero, "좌우 조작 없음");
            Assert.That(body.linearVelocityY, Is.EqualTo(-3f), "세로 속도는 그대로(중력)");
            Assert.That(body.gravityScale, Is.GreaterThan(0f));
        }

        [Test]
        public void Ladder_Stunned_HangsWithoutFalling_NoClimbOrJumpOff()
        {
            movement.EnterLadder();
            Assert.That(movement.IsClimbing, Is.True);
            movement.SetVerticalMoveInput(1f);
            Assert.That(survival.SimulateFall(30f), Is.True);
            Assert.That(movement.IsHitStunned, Is.True);
            body.linearVelocity = new Vector2(0f, 3f);
            Invoke(movement, "ApplyVerticalMovement");
            Assert.That(body.linearVelocityY, Is.Zero, "매달린 채 멈춤");
            Assert.That(movement.IsClimbing, Is.True);
            Assert.That(body.gravityScale, Is.Zero, "사다리 상태 유지(떨어지지 않음)");
            Assert.That(movement.IsMovingOnLadder, Is.False);
            movement.RequestJump();
            Assert.That(Invoke(movement, "TryApplyJump"), Is.EqualTo(false), "뛰어내리기 무시");
            Assert.That(movement.IsClimbing, Is.True);

            survival.State.ClearHitStun();
            Invoke(movement, "ApplyVerticalMovement");
            Assert.That(body.linearVelocityY, Is.GreaterThan(0f), "끝나면 다시 오른다");
        }

        // ------------------------------------------------------------------ 피격 그림

        [Test]
        public void GroundGas_ShowsGasDamageCompressedIntoHitStun()
        {
            var gas = (Sprite[])Get(animation, "gasDamageFrames");
            Assert.That(gas.Length, Is.EqualTo(34));
            Gas();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.GasDamage));
            Assert.That(animation.DamageVisualDuration, Is.EqualTo(Stun).Within(0.02f));
            foreach (var (elapsed, frame) in new[] { (0.005f, 0), (0.2f, 17), (0.3f, 25), (0.399f, 33) })
            {
                At(elapsed);
                Assert.That(renderer.sprite, Is.SameAs(gas[frame]), "elapsed " + elapsed);
            }

            At(0.401f);
            Assert.That(State, Is.EqualTo("Idle"));
        }

        [TestCase("fall")]
        [TestCase("collapse")]
        public void GroundFallOrCollapse_ShowsExistingDamageFrames(string cause)
        {
            if (cause == "fall") survival.SimulateFall(30f);
            else survival.ApplyCollapseImpactWithoutCameraShake();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.Damage));
            var damage = (Sprite[])Get(animation, "damageFrames");
            Assert.That(damage, Does.Contain(renderer.sprite));
            Assert.That(animation.DamageVisualDuration, Is.EqualTo(Stun).Within(0.02f));
        }

        [TestCase("gas")]
        [TestCase("fall")]
        [TestCase("collapse")]
        public void OnLadder_AnyCause_ShowsLadderHitCompressed_NotStandingGas(string cause)
        {
            movement.EnterLadder();
            Tick();
            if (cause == "gas") Gas();
            else if (cause == "fall") survival.SimulateFall(30f);
            else survival.ApplyCollapseImpactWithoutCameraShake();
            Tick();
            var hit = (Sprite[])Get(animation, "ladderHitFrames");
            Assert.That(hit.Length, Is.EqualTo(25));
            Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.LadderHit));
            Assert.That(animation.IsShowingLadderDamageVisual, Is.True);
            At(0.2f);
            Assert.That(renderer.sprite, Is.SameAs(hit[12]));
            At(0.399f);
            Assert.That(renderer.sprite, Is.SameAs(hit[24]));
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f), "LadderDamageDirection 기본 오른쪽");
            At(0.401f);
            Assert.That(animation.IsShowingLadderVisual, Is.True);
        }

        [Test]
        public void Oxygen_KeepsOldDamageVisual_WithoutStun()
        {
            Oxygen();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(animation.IsShowingHitStunVisual, Is.False);
            Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.Damage));
            Assert.That((Sprite[])Get(animation, "damageFrames"), Does.Contain(renderer.sprite));
        }

        [Test]
        public void ReDamageDuringStunVisual_DoesNotRestartOrExtend()
        {
            Gas();
            Tick();
            float until = (float)Get(animation, "damageUntil");
            // 무적이 막지 않는 재피해(디버그 체력 조정 등)도 그림을 다시 시작하지 않는다.
            survival.SetHealthAbsolute((int)survival.State.Health - 3);
            Assert.That((float)Get(animation, "damageUntil"), Is.EqualTo(until));
            Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.GasDamage));
        }

        // ------------------------------------------------------------------ 사다리 오르기

        [TestCase(1f)]
        [TestCase(-1f)]
        public void LadderClimb_CyclesWholeSet_UpForwardDownReverse_IdleFirst_NoFlip(float direction)
        {
            var frames = (Sprite[])Get(animation, "ladderFrames");
            Assert.That(frames.Length, Is.EqualTo(32));
            float step = (float)Get(animation, "ladderCycleDistance") / frames.Length;
            Assert.That(step, Is.EqualTo(2f / 32f).Within(1e-5f));
            movement.EnterLadder();
            body.simulated = false;
            var start = body.position;
            movement.SetVerticalMoveInput(direction);
            for (var i = 0; i <= frames.Length; i++)
            {
                body.position = start + Vector2.up * (direction * i * (step + 0.00001f));
                Tick();
                int expected = direction > 0f ? i % frames.Length : (frames.Length - i) % frames.Length;
                Assert.That(renderer.sprite, Is.SameAs(frames[expected]), "step " + i);
                Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f), "사다리 그림은 뒤집지 않는다");
            }

            movement.SetVerticalMoveInput(0f);
            body.position += Vector2.up * direction * step * 3.5f;
            Tick();
            Assert.That(renderer.sprite, Is.SameAs(frames[0]), "가만히 있으면 1번");
        }

        [Test]
        public void LadderClimb_FootstepOnConfiguredFrames()
        {
            var steps = (int[])Get(animation, "ladderStepFrames");
            CollectionAssert.AreEqual(PromptB210PlayerMotionsBuilder.MaleLadderStepFrames, steps);
            var frames = (Sprite[])Get(animation, "ladderFrames");
            float step = 2f / frames.Length;
            var played = new List<int>();
            void Record(SfxId id, float pitch)
            {
                if (id == SfxId.LadderClimb || id == SfxId.LadderClimbB)
                    played.Add((int)Get(animation, "ladderSequenceIndex") + 1);
            }

            movement.EnterLadder();
            body.simulated = false;
            var start = body.position;
            movement.SetVerticalMoveInput(1f);
            SfxRequests.PlayRequested += Record;
            try
            {
                for (var i = 0; i <= frames.Length; i++)
                {
                    // 발소리 최소 간격(0.18초)을 넘기도록 판정기 시계를 보지 않고 한 장씩 따로 검사한다.
                    Set(movement.LadderSfx, "stepPlayed", false);
                    body.position = start + Vector2.up * (i * (step + 0.00001f));
                    Tick();
                }
            }
            finally
            {
                SfxRequests.PlayRequested -= Record;
            }

            CollectionAssert.AreEqual(new[] { 9, 25 }, played);
        }

        // ------------------------------------------------------------------ 사다리 채굴 구간·세트

        [Test]
        public void LadderMining_NewRanges_StartWorkPingPongFinish()
        {
            var sequence = new PlayerLadderMiningAnimationSequence(new PlayerLadderMiningAnimationSettings());
            sequence.SetFrameCount(25);
            sequence.SetFrameRanges(PromptB210PlayerMotionsBuilder.LadderMiningRanges);
            sequence.Start(1);
            var seen = new List<int>();
            for (var i = 0; i < 80; i++)
            {
                sequence.Tick(1f / 24f);
                seen.Add(sequence.FrameIndex);
            }

            Assert.That(seen.Take(7), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }), "시작 1–8");
            Assert.That(seen[7], Is.EqualTo(8), "8번 다음 작업 9번");
            Assert.That(seen.Skip(7).Min(), Is.EqualTo(8), "작업 9번부터");
            Assert.That(seen.Skip(7).Max(), Is.EqualTo(17), "작업 18번까지 핑퐁");
            sequence.Complete();
            Assert.That(sequence.FrameIndex, Is.EqualTo(18));
            sequence.Tick(1f);
            Assert.That(sequence.IsActive, Is.False);
            sequence.ResumeAfterDamage();
            Assert.That(sequence.FrameIndex, Is.EqualTo(8));
        }

        [Test]
        public void EmptyRanges_FallBackToSettings_OldTwentyFourSequenceUnchanged()
        {
            var sequence = new PlayerLadderMiningAnimationSequence(new PlayerLadderMiningAnimationSettings());
            sequence.SetFrameRanges(default);
            Assert.That(sequence.StartLastFrame, Is.EqualTo(12));
            Assert.That(sequence.WorkFirstFrame, Is.EqualTo(13));
            Assert.That(sequence.WorkLastFrame, Is.EqualTo(16));
            Assert.That(sequence.FinishFirstFrame, Is.EqualTo(17));
            Assert.That(sequence.FinishLastFrame, Is.EqualTo(24));
        }

        [Test]
        public void Sets_MaleAndFemaleHaveNewMotions_FemaleArtOnly()
        {
            Assert.That(PromptB210PlayerMotionsBuilder.CollectErrors(), Is.Empty);
            var male = animation.MaleAnimationSet;
            var female = animation.FemaleAnimationSet;
            Assert.That(male.gasDamageFrames.Length, Is.EqualTo(34));
            Assert.That(male.ladderFrames.Length, Is.EqualTo(32));
            Assert.That(male.ladderHitFrames.Length, Is.EqualTo(25));
            Assert.That(male.ladderMiningFrames.Length, Is.EqualTo(25));
            Assert.That(female.gasDamageFrames.Length, Is.EqualTo(34));
            Assert.That(female.ladderFrames.Length, Is.EqualTo(25));
            Assert.That(female.ladderHitFrames.Length, Is.EqualTo(25));
            Assert.That(female.ladderMiningFrames.Length, Is.EqualTo(25));
            CollectionAssert.AreEqual(PromptB210PlayerMotionsBuilder.FemaleLadderStepFrames, female.ladderStepFrames);
            foreach (var frames in new[] { female.gasDamageFrames, female.ladderFrames, female.ladderHitFrames, female.ladderMiningFrames })
            foreach (var sprite in frames)
                Assert.That(AssetDatabase.GetAssetPath(sprite), Does.StartWith(PromptB208PlayerAnimationSetBuilder.FemaleFramesRoot + "/"));
            foreach (var frames in new[] { male.gasDamageFrames, male.ladderFrames, male.ladderHitFrames, male.ladderMiningFrames })
            foreach (var sprite in frames)
            {
                Assert.That(AssetDatabase.GetAssetPath(sprite), Does.StartWith(PromptB210PlayerMotionsBuilder.MaleFramesRoot + "/"));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(256f));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128, 128)));
            }
        }

        [Test]
        public void FemaleSet_AppliesItsLadderCountStepsAndRanges()
        {
            animation.SetGender(PlayerGender.Female);
            var frames = (Sprite[])Get(animation, "ladderFrames");
            Assert.That(frames.Length, Is.EqualTo(25));
            var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
            Assert.That(sequence.WorkFirstFrame, Is.EqualTo(9));
            Assert.That(sequence.FinishLastFrame, Is.EqualTo(25));
            movement.EnterLadder();
            body.simulated = false;
            var start = body.position;
            movement.SetVerticalMoveInput(-1f);
            float step = 2f / 25f;
            body.position = start;
            Tick();
            body.position = start + Vector2.down * (step + 0.00001f);
            Tick();
            Assert.That(renderer.sprite, Is.SameAs(frames[24]), "여자도 내려가면 N→1");
        }

        [Test]
        public void SetWithoutNewFields_FallsBackToCurrentDrawings()
        {
            var set = ScriptableObject.Instantiate(animation.FemaleAnimationSet);
            try
            {
                set.gasDamageFrames = null;
                set.ladderHitFrames = null;
                set.ladderStepFrames = null;
                set.ladderMiningRanges = default;
                animation.ApplyAnimationSet(set);
                var damage = (Sprite[])Get(animation, "damageFrames");
                Gas();
                Tick();
                Assert.That(animation.DamageVisual, Is.EqualTo(PlayerDamageVisual.GasDamage));
                Assert.That(damage, Does.Contain(renderer.sprite), "가스 그림이 없으면 damageFrames");
                var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
                Assert.That(sequence.WorkFirstFrame, Is.EqualTo(13), "구간이 비면 프리팹 설정값");
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }

        // ------------------------------------------------------------------ 채굴 중 경직

        [Test]
        public void MiningDuringStun_PausesProgress_ResumesWorkFramesWhenHeld()
        {
            using var mining = new MiningFixture(player, movement);
            movement.ExitLadder();
            Assert.That(mining.StartRight(), Is.True);
            mining.System.TickMining(0.2f, movement.Position, 2f);
            float progress = mining.System.Progress;
            Assert.That(progress, Is.GreaterThan(0f));
            animation.SetMining(true);
            Gas();
            Invoke(mining.Controller, "HandleHitStun");
            Assert.That(mining.System.IsMining, Is.True, "경직 중에는 멈추기만 한다");
            Assert.That(mining.System.Progress, Is.EqualTo(progress));
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            // 경직 끝: 버튼을 누르고 있지 않았으면 그 블록 채굴을 끝낸다.
            survival.State.ClearHitStun();
            Set(animation, "damageUntil", 0f);
            Invoke(mining.Controller, "HandleHitStun");
            Assert.That(mining.System.IsMining, Is.False);
        }

        // ------------------------------------------------------------------ 가이드

        [Test]
        public void Guide_MaleLadderUsesNew32Frames_FemaleUses25_NoMixing()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(PromptB140GameGuideSkinBuilder.SkinAssetPath);
            skin.ResetLookup();
            var male = animation.MaleAnimationSet;
            for (var i = 1; i <= 32; i++) Assert.That(skin.Get("player.ladder." + i), Is.SameAs(male.ladderFrames[i - 1]));
            Assert.That(skin.Get("player.ladder.33"), Is.Null);

            var clips = GuideDemoLibrary.All.ToList();
            foreach (ControlKeyFunction function in System.Enum.GetValues(typeof(ControlKeyFunction)))
            {
                var clip = ControlTerminalDemoClips.Get(function);
                if (clip != null && !clips.Contains(clip)) clips.Add(clip);
            }

            var ladderLayers = 0;
            foreach (var clip in clips)
            foreach (var layer in clip.Layers.Where(l => l.AnimPrefix == "player.ladder"))
            {
                ladderLayers++;
                Assert.That(layer.FrameKeys, Is.EqualTo(Enumerable.Range(1, 32).Select(i => "player.ladder." + i).ToArray()), clip.Id);
                Assert.That(layer.Fps, Is.EqualTo(32f), clip.Id + " 한 바퀴 1초");
                var female = GuidePlayerArt.ForGender(clip, PlayerGender.Female).Layers.First(l => l.AnimPrefix == "player.female.ladder");
                Assert.That(female.FrameKeys.Length, Is.EqualTo(25), clip.Id);
                Assert.That(female.Fps, Is.EqualTo(25f), clip.Id);
                Assert.That(female.FrameKeys.All(k => k.StartsWith("player.female.ladder.")), Is.True, clip.Id);
            }

            Assert.That(ladderLayers, Is.GreaterThanOrEqualTo(3), "가이드 2장면 + 조작 단말 장면");
        }

        // ------------------------------------------------------------------ 도움

        private sealed class MiningFixture : System.IDisposable
        {
            private readonly GameObject world;
            private readonly Tile tile;
            private readonly PlayerMovement movement;
            public MiningSystem System { get; }
            public PlayerMiningController Controller { get; }

            public MiningFixture(GameObject player, PlayerMovement movement)
            {
                this.movement = movement;
                world = new GameObject("B210 mining world", typeof(Grid));
                var tiles = new GameObject("Tiles", typeof(Tilemap));
                tiles.transform.SetParent(world.transform);
                var map = tiles.GetComponent<Tilemap>();
                var resolver = world.AddComponent<MiningTileResolver>();
                System = world.AddComponent<MiningSystem>();
                Set(System, "foregroundTilemap", map);
                Set(System, "tileResolver", resolver);
                tile = ScriptableObject.CreateInstance<Tile>();
                resolver.RegisterRuntime(tile, new MiningTileDto("tile.b210", "", 0, true, 1f, 5f, 0f, false));
                map.SetTile(Vector3Int.right, tile);
                player.transform.position = new Vector3(.5f, .5f, 0f);
                player.GetComponent<Rigidbody2D>().position = new Vector2(.5f, .5f);
                Controller = player.GetComponent<PlayerMiningController>();
                if (Controller == null) Controller = player.AddComponent<PlayerMiningController>();
                Invoke(Controller, "Awake");
                Set(Controller, "miningSystem", System);
                Invoke(Controller, "BindAnimationEvents");
            }

            public bool StartRight()
            {
                System.TryStartMiningInDirection(movement.Position, Vector2.right, 2f);
                return System.IsMining;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(world);
                Object.DestroyImmediate(tile);
            }
        }

        private bool Gas() => survival.ApplyGasFailure(new GasExposureFailureInputDto
        {
            gasZoneId = "b210",
            severity = GasExposureFailureSeverity.Damage,
            damage = 3,
            cumulativeExposureSeconds = Random.Range(1f, 1000f)
        });

        private bool Oxygen() => (bool)typeof(PlayerSurvivalController).GetMethod("ApplyDamage", Private)
            .Invoke(survival, new object[] { RunFailureCause.OxygenDepleted, 5f, false, "oxygen:test", "oxygen" });

        // 체력·무적·경직·그림을 처음 상태로 돌린다.
        private void Reset()
        {
            survival.RestoreFull();
            Set(animation, "previousHealth", survival.State.Health);
            Set(animation, "damageUntil", 0f);
            Set(animation, "damageIsHitStun", false);
        }

        private string State => (string)Get(animation, "currentState");

        // 다침 시작 뒤 elapsed초가 지난 것처럼 끝 시각을 당긴 뒤 한 틱 돈다.
        private void At(float elapsed)
        {
            float duration = animation.DamageVisualDuration;
            float start = Time.unscaledTime - elapsed;
            Set(animation, "damageUntil", start + duration);
            if (State == "Damage") Set(animation, "stateStartedAt", start);
            if (elapsed >= duration) survival.State.ClearHitStun();
            Tick();
        }

        private void Tick()
        {
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
        }

        private static void Set(object target, string name, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(name, Private | BindingFlags.Public);
                type = type.BaseType;
            }

            field.SetValue(target, value);
        }

        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);

        private static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Private | BindingFlags.Public).Invoke(target, args);
    }
}
