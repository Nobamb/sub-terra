using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.MainMenu;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    /// <summary>B-208: 여자 모션 세트·성별 저장·gender 명령·짧은 세트 안전 처리·가이드 성별.</summary>
    public sealed class PromptB208FemalePlayerTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string SkinPath = "Assets/_Project/Resources/UI/GameGuideSkin.asset";
        private readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp() => PlayerGenderPresentation.ResetForTests();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            PlayerGenderPresentation.ResetForTests();
        }

        // ------------------------------------------------------------------ 저장

        [Test]
        public void NewGame_IsMale_AndStartNewGameTakesGenderDefaultingToMale()
        {
            Assert.That(GameState.CreateNew().Player.Gender, Is.EqualTo(PlayerGender.Male));
            var method = typeof(SaveRuntimeController).GetMethod("StartNewGame");
            var parameter = method.GetParameters().Single(p => p.ParameterType == typeof(PlayerGender));
            Assert.That(parameter.HasDefaultValue, Is.True);
            Assert.That(parameter.DefaultValue, Is.EqualTo(PlayerGender.Male));
        }

        [TestCase("")]
        [TestCase(",\"gender\": 7")]
        public void OldSaveJson_WithoutOrUnknownGender_RestoresMale(string replacement)
        {
            var json = Serialize(GameState.CreateNew());
            var field = new Regex(@",\s*""gender"":\s*0");
            Assert.That(field.IsMatch(json), Is.True);
            var restored = Restore(field.Replace(json, replacement, 1));
            Assert.That(restored.Player.Gender, Is.EqualTo(PlayerGender.Male));
        }

        [Test]
        public void SaveRoundTrip_KeepsFemale()
        {
            var state = GameState.CreateNew();
            state.SetPlayerGender(PlayerGender.Female);
            var json = Serialize(state);
            Assert.That(Regex.IsMatch(json, @"""gender"":\s*1"), Is.True);
            Assert.That(Restore(json).Player.Gender, Is.EqualTo(PlayerGender.Female));
        }

        [Test]
        public void SetPlayerGender_RaisesOnlyOnChange()
        {
            var state = GameState.CreateNew();
            var raised = new List<PlayerGender>();
            state.PlayerGenderChanged += raised.Add;
            state.SetPlayerGender(PlayerGender.Male);
            state.SetPlayerGender(PlayerGender.Female);
            state.SetPlayerGender(PlayerGender.Female);
            state.SetPlayerGender((PlayerGender)9);
            Assert.That(raised, Is.EqualTo(new[] { PlayerGender.Female, PlayerGender.Male }));
        }

        // ------------------------------------------------------------------ 세트·장수

        [Test]
        public void FemaleSet_CountsMatchTable_FemaleArtOnly_NoDuplicatedFrames()
        {
            Assert.That(PromptB208PlayerAnimationSetBuilder.CollectErrors(), Is.Empty);
            var female = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PromptB208PlayerAnimationSetBuilder.FemaleSetPath);
            var expected = new Dictionary<string, int>
            {
                { "idleFrames", 1 }, { "walkFrames", 39 }, { "miningFrames", 23 }, { "ladderMiningFrames", 23 },
                { "jumpReadyFrames", 7 }, { "jumpRiseFrames", 23 }, { "jumpFallFrames", 23 },
                { "knockoutFrames", 47 }, { "ladderFrames", 5 }, { "damageFrames", 8 }
            };
            foreach (var pair in expected)
            {
                var frames = PromptB208PlayerAnimationSetBuilder.Frames(female, pair.Key);
                Assert.That(frames.Length, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(frames.Distinct().Count(), Is.EqualTo(pair.Value), pair.Key + " 복제 장 없음");
            }

            var root = PromptB208PlayerAnimationSetBuilder.FemaleFramesRoot;
            Assert.That(Directory.GetFiles(root, "*.png", SearchOption.AllDirectories).Length,
                Is.EqualTo(expected.Values.Sum()), "여자 폴더 PNG 수 = 표 합계");
            Assert.That(Directory.GetFiles(root, "*.webm", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(root, "*.mov", SearchOption.AllDirectories)), Is.Empty);
        }

        [Test]
        public void FemaleImports_MatchMaleImportSettings()
        {
            foreach (var path in Directory.GetFiles(PromptB208PlayerAnimationSetBuilder.FemaleFramesRoot, "*.png",
                         SearchOption.AllDirectories))
            {
                var assetPath = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                var ladder = assetPath.Contains("/LadderBack/");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), assetPath);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(ladder
                    ? PromptB208PlayerAnimationSetBuilder.LadderPixelsPerUnit : 256f).Within(0.001f), assetPath);
                Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)), assetPath);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), assetPath);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), assetPath);
                Assert.That(importer.mipmapEnabled, Is.False, assetPath);
            }
        }

        [Test]
        public void MaleSet_IsExactlyThePrefabFrames()
        {
            var animation = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab)
                .GetComponentInChildren<PlayerAnimationController>(true);
            var serialized = new SerializedObject(animation);
            var male = animation.MaleAnimationSet;
            Assert.That(male, Is.Not.Null);
            Assert.That(animation.FemaleAnimationSet, Is.Not.Null);
            foreach (var field in new[] { "idleFrames", "walkFrames", "jumpReadyFrames", "jumpRiseFrames", "jumpFallFrames",
                         "ladderFrames", "miningFrames", "ladderMiningFrames", "damageFrames", "knockoutFrames" })
            {
                var property = serialized.FindProperty(field);
                var frames = PromptB208PlayerAnimationSetBuilder.Frames(male, field);
                Assert.That(frames.Length, Is.EqualTo(property.arraySize), field);
                for (var i = 0; i < frames.Length; i++)
                    Assert.That(frames[i], Is.EqualTo(property.GetArrayElementAtIndex(i).objectReferenceValue), field + i);
            }

            var footsteps = serialized.FindProperty("footstepFrames");
            Assert.That(footsteps.arraySize, Is.EqualTo(2));
            Assert.That(footsteps.GetArrayElementAtIndex(0).intValue, Is.EqualTo(1));
            Assert.That(footsteps.GetArrayElementAtIndex(1).intValue, Is.EqualTo(21));
        }

        // ------------------------------------------------------------------ 남자 결과 불변

        [Test]
        public void MaleCounts_GiveTheSameMiningLadderAndJumpOrderAsBefore()
        {
            Assert.That(MiningTrace(24), Is.EqualTo(MiningTrace(0)));
            Assert.That(LadderMiningTrace(24), Is.EqualTo(LadderMiningTrace(0)));
            Assert.That(JumpTrace(8, 24, 24), Is.EqualTo(JumpTrace(0, 0, 0)));
            // 바꾸기 전 고정값: 채굴 마무리 끝 24, 사다리 채굴 끝 24, 상승 끝 24, 낙하 루프 18~24, 착지 8부터.
            Assert.That(MiningTrace(24).Max(), Is.EqualTo(23));
            Assert.That(LadderMiningTrace(24).Max(), Is.EqualTo(23));
            var jump = JumpTrace(8, 24, 24);
            Assert.That(jump.Where(s => s.StartsWith("Rise")).Max(Index), Is.EqualTo(23));
            Assert.That(FallLoop(jump), Is.EqualTo(Enumerable.Range(17, 7)));
            Assert.That(jump.First(s => s.StartsWith("Land")), Is.EqualTo("Land:Ready:7"));
        }

        [Test]
        public void FemaleCounts_StayInRange_FallLoopIs18To23_LandStartsAt7()
        {
            var mining = MiningTrace(23);
            Assert.That(mining.Max(), Is.EqualTo(22), "채굴 마무리 끝 = 23번");
            var ladder = LadderMiningTrace(23);
            Assert.That(ladder.Max(), Is.EqualTo(22), "사다리 채굴 마무리 끝 = 23번");
            var jump = JumpTrace(7, 23, 23);
            Assert.That(jump.Where(s => s.Contains(":Rise:")).Max(Index), Is.EqualTo(22), "상승 끝 = 23번");
            Assert.That(jump.Where(s => s.Contains(":Fall:")).Max(Index), Is.LessThanOrEqualTo(22));
            Assert.That(jump.Where(s => s.Contains(":Ready:")).Max(Index), Is.LessThanOrEqualTo(6));
            Assert.That(FallLoop(jump), Is.EqualTo(Enumerable.Range(17, 6)), "낙하 루프 18~23");
            Assert.That(jump.First(s => s.StartsWith("Land")), Is.EqualTo("Land:Ready:6"), "착지 7→1");
        }

        [TestCase(PlayerGender.Male, 48)]
        [TestCase(PlayerGender.Female, 47)]
        public void Knockout_GasBreathingLoopsFrom37ToLastFrame(PlayerGender gender, int count)
        {
            var rig = CreatePlayer();
            Assert.That(rig.Animation.SetGender(gender), Is.True);
            var frames = (Sprite[])Field(rig.Animation, "knockoutFrames");
            Assert.That(frames.Length, Is.EqualTo(count));
            var survival = rig.Root.GetComponent<PlayerSurvivalController>();
            if (survival == null) survival = rig.Root.AddComponent<PlayerSurvivalController>();
            Call(survival, "Awake");
            rig.Animation.BindSurvival(survival);
            survival.State.TryApplyDamage(RunFailureCause.GasExposure, 999, Time.unscaledTime, 0f, true, out _);
            Call(rig.Animation, "HandleFailureRequested", new RunFailureInputDto { cause = RunFailureCause.GasExposure });
            var end = count / 24f;
            var seen = new HashSet<int>();
            for (var step = 0; step < 60; step++)
            {
                var sample = end + 0.02f + step / 12f;
                SetField(rig.Animation, "stateStartedAt", Time.unscaledTime - sample);
                Call(rig.Animation, "Play", "Knockout");
                var index = System.Array.IndexOf(frames, rig.Renderer.sprite);
                Assert.That(index, Is.EqualTo(36 + Mathf.FloorToInt((sample - end) * 12f) % (count - 36)), "t " + sample);
                seen.Add(index);
            }

            Assert.That(seen.OrderBy(i => i), Is.EqualTo(Enumerable.Range(36, count - 36)), "숨쉬기 37~마지막");
        }

        // ------------------------------------------------------------------ 세트 교체

        [Test]
        public void SwappingSets_KeepsMiningStateAndProgress_ClampsToLastFrame()
        {
            var rig = CreatePlayer();
            rig.Animation.SetMining(true);
            Call(rig.Animation, "LateUpdate");
            rig.Animation.NotifyMiningCompleted();
            var sequence = Field(rig.Animation, "miningSequence");
            typeof(PlayerMiningAnimationSequence).GetField("<FrameIndex>k__BackingField", Private).SetValue(sequence, 23);
            Assert.That(rig.Animation.SetGender(PlayerGender.Female), Is.True);
            Assert.That(rig.Animation.Gender, Is.EqualTo(PlayerGender.Female));
            Assert.That(Field(rig.Animation, "miningSequence"), Is.SameAs(sequence));
            Assert.That(((PlayerMiningAnimationSequence)sequence).IsActive, Is.True);
            Assert.That(((PlayerMiningAnimationSequence)sequence).FrameIndex, Is.EqualTo(23), "진행은 리셋하지 않는다");
            Call(rig.Animation, "Play", "Mining");
            var female = rig.Animation.FemaleAnimationSet.miningFrames;
            Assert.That(rig.Renderer.sprite, Is.EqualTo(female[22]), "넘는 번호는 마지막 장");
            Assert.That(Field(rig.Animation, "currentState"), Is.EqualTo("Mining"));

            Assert.That(rig.Animation.SetGender(PlayerGender.Male), Is.True);
            Call(rig.Animation, "Play", "Mining");
            Assert.That(rig.Renderer.sprite, Is.EqualTo(rig.Animation.MaleAnimationSet.miningFrames[23]));
        }

        [Test]
        public void FemaleWalk_Loops39Frames()
        {
            var rig = CreatePlayer();
            rig.Animation.SetGender(PlayerGender.Female);
            var walk = rig.Animation.FemaleAnimationSet.walkFrames;
            rig.Movement.SetMoveInput(1f);
            Call(rig.Animation, "LateUpdate");
            foreach (var sample in new[] { 0.9625f, 0.9875f, 1.0125f, 1.9625f })
            {
                SetField(rig.Animation, "stateStartedAt", Time.unscaledTime - sample);
                Call(rig.Animation, "Play", "Walk");
                Assert.That(rig.Renderer.sprite, Is.EqualTo(walk[Mathf.FloorToInt(sample * 40f) % 39]), "t " + sample);
            }
        }

        [Test]
        public void PresentationChange_AppliesToPlayerThroughBridge()
        {
            var rig = CreatePlayer();
            var bridge = PlayerGenderVisualBridge.Attach(rig.Animation);
            created.Add(bridge);
            Assert.That(rig.Animation.Gender, Is.EqualTo(PlayerGender.Male));
            PlayerGenderPresentation.Set(PlayerGender.Female);
            Assert.That(rig.Animation.Gender, Is.EqualTo(PlayerGender.Female));
            Assert.That(((Sprite[])Field(rig.Animation, "walkFrames")).Length, Is.EqualTo(39));
            PlayerGenderPresentation.Set(PlayerGender.Male);
            Assert.That(((Sprite[])Field(rig.Animation, "walkFrames")).Length, Is.EqualTo(40));
        }

        // ------------------------------------------------------------------ gender 명령

        [TestCase("male", true, PlayerGender.Male)]
        [TestCase("FEMALE", true, PlayerGender.Female)]
        [TestCase("남", true, PlayerGender.Male)]
        [TestCase("여", true, PlayerGender.Female)]
        [TestCase("여자", true, PlayerGender.Female)]
        [TestCase("abc", false, PlayerGender.Male)]
        [TestCase("", false, PlayerGender.Male)]
        public void GenderParse(string token, bool ok, PlayerGender gender)
        {
            Assert.That(DeveloperDebugGenderCommands.TryParse(token, out var parsed), Is.EqualTo(ok));
            if (ok) Assert.That(parsed, Is.EqualTo(gender));
        }

        [Test]
        public void GenderCommand_SetsStateAndPresentation_UsageOnBadInput()
        {
            var registry = DeveloperDebugCommandRegistry.CreateIsolated();
            var state = GameState.CreateNew();
            var context = new DeveloperDebugCommandContext(state, null, null, null);
            Assert.That(registry.Execute("gender female", context), Does.Contain("-> 여자"));
            Assert.That(state.Player.Gender, Is.EqualTo(PlayerGender.Female));
            Assert.That(PlayerGenderPresentation.Current, Is.EqualTo(PlayerGender.Female));
            foreach (var line in new[] { "gender", "gender abc", "성별 여 남" })
            {
                var output = registry.Execute(line, context);
                Assert.That(output, Does.Contain("현재 성별: 여자"), line);
                Assert.That(output, Does.Contain(DeveloperDebugGenderCommands.Usage), line);
            }

            Assert.That(registry.Execute("성별 남", context), Does.Contain("-> 남자"));
            Assert.That(state.Player.Gender, Is.EqualTo(PlayerGender.Male));
            Assert.That(PlayerGenderPresentation.Current, Is.EqualTo(PlayerGender.Male));
        }

        // ------------------------------------------------------------------ 가이드

        [Test]
        public void GuideSkin_HasFemaleKeysWithFemaleCounts()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(SkinPath);
            skin.ResetLookup();
            var female = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PromptB208PlayerAnimationSetBuilder.FemaleSetPath);
            for (var i = 1; i <= 39; i++) Assert.That(skin.Get("player.female.walk." + i), Is.EqualTo(female.walkFrames[i - 1]));
            for (var i = 1; i <= 23; i++) Assert.That(skin.Get("player.female.mine." + i), Is.EqualTo(female.miningFrames[i - 1]));
            for (var i = 1; i <= 5; i++) Assert.That(skin.Get("player.female.ladder." + i), Is.EqualTo(female.ladderFrames[i - 1]));
            Assert.That(skin.Get("player.female.walk.40"), Is.Null);
            Assert.That(skin.Get("player.female.mine.24"), Is.Null);
            Assert.That(skin.Get("player.female.idle"), Is.EqualTo(female.idleFrames[0]));
            Assert.That(skin.Get("player.female.knockout.1"), Is.EqualTo(female.knockoutFrames[46]), "기절 대표 = knockout_47");
            Assert.That(skin.Get("player.female.damage.1"), Is.EqualTo(female.damageFrames[0]));
            Assert.That(skin.Get("player.female.jump.4"), Is.EqualTo(female.jumpRiseFrames[11]));
        }

        [Test]
        public void FemaleGuideClips_UseOnlyFemaleKeysWithFemaleCounts()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(SkinPath);
            skin.ResetLookup();
            var clips = GuideDemoLibrary.All.ToList();
            foreach (ControlKeyFunction function in System.Enum.GetValues(typeof(ControlKeyFunction)))
            {
                var clip = ControlTerminalDemoClips.Get(function);
                if (clip != null) clips.Add(clip);
            }

            var walkLayers = 0;
            var mineLayers = 0;
            foreach (var clip in clips)
            {
                Assert.That(GuidePlayerArt.ForGender(clip, PlayerGender.Male), Is.SameAs(clip));
                var female = GuidePlayerArt.ForGender(clip, PlayerGender.Female);
                foreach (var key in female.UsedSpriteKeys().Where(k => k.StartsWith("player.")))
                {
                    Assert.That(key, Does.StartWith("player.female."), clip.Id);
                    Assert.That(skin.Get(key), Is.Not.Null, clip.Id + " " + key);
                }

                foreach (var layer in female.Layers.Where(l => l.FrameKeys != null && l.AnimPrefix.StartsWith("player.")))
                {
                    if (layer.AnimPrefix == "player.female.walk")
                    {
                        walkLayers++;
                        Assert.That(layer.FrameKeys.Length, Is.EqualTo(39), clip.Id);
                    }

                    if (layer.MiningStart >= 0f)
                    {
                        mineLayers++;
                        Assert.That(layer.FrameKeys.Length, Is.EqualTo(23), clip.Id);
                        for (var t = 0f; t < clip.Duration; t += 1f / 120f)
                            Assert.That(layer.FrameIndex.Eval(t), Is.LessThanOrEqualTo(22f), clip.Id + " t " + t);
                    }
                }
            }

            Assert.That(walkLayers, Is.GreaterThan(0));
            Assert.That(mineLayers, Is.GreaterThan(0));
        }

        [Test]
        public void MaleMiningClip_KeepsTheSameFinishTrack()
        {
            var layer = new DemoLayer().AnimateMining(0.3f, 1.333f);
            Assert.That(layer.FrameKeys.Length, Is.EqualTo(24));
            Assert.That(layer.FrameIndex.Eval(1.333f + 9f / 60f), Is.EqualTo(23f));
            Assert.That(layer.FrameActive.Eval(1.333f + 9.5f / 60f), Is.EqualTo(1f));
            Assert.That(layer.FrameActive.Eval(1.333f + 10.5f / 60f), Is.EqualTo(0f));
        }

        [Test]
        public void ControlTerminalStage_FollowsGender_MainMenuLockStaysMale()
        {
            var root = new GameObject("B208 Controls", typeof(RectTransform), typeof(Canvas));
            created.Add(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            var panel = ControlSchemePanel.Attach(root);
            var stage = panel.Terminal.Stage;
            stage.Bind(ControlTerminalDemoClips.Get(ControlKeyFunction.MoveRight), false);
            Assert.That(stage.BoundGender, Is.EqualTo(PlayerGender.Male));

            PlayerGenderPresentation.Set(PlayerGender.Female);
            stage.RefreshGender();
            Assert.That(stage.BoundGender, Is.EqualTo(PlayerGender.Female));
            Assert.That(stage.Clip.UsedSpriteKeys().Where(k => k.StartsWith("player.")),
                Is.All.StartWith("player.female."));

            // 화면이 꺼진 동안 성별이 바뀌어도 다시 켤 때(OnEnable) 맞춘다.
            PlayerGenderPresentation.Set(PlayerGender.Male);
            Call(stage, "OnEnable");
            Assert.That(stage.BoundGender, Is.EqualTo(PlayerGender.Male));
            PlayerGenderPresentation.Set(PlayerGender.Female);

            panel.LockGuideGender(PlayerGender.Male);
            Assert.That(stage.BoundGender, Is.EqualTo(PlayerGender.Male));
            Assert.That(stage.Clip, Is.SameAs(ControlTerminalDemoClips.Get(ControlKeyFunction.MoveRight)));
        }

        // ------------------------------------------------------------------ 도우미

        private static List<int> MiningTrace(int count)
        {
            var sequence = new PlayerMiningAnimationSequence(new PlayerMiningAnimationSettings());
            if (count > 0) sequence.SetFrameCount(count);
            var trace = new List<int>();
            sequence.Start();
            for (var i = 0; i < 80; i++) { sequence.Tick(1f / 30f); trace.Add(sequence.FrameIndex); }
            sequence.Complete();
            for (var i = 0; i < 20 && sequence.IsActive; i++) { sequence.Tick(1f / 60f); trace.Add(sequence.FrameIndex); }
            Assert.That(sequence.IsActive, Is.False);
            sequence.Start();
            for (var i = 0; i < 10; i++) { sequence.Tick(1f / 30f); trace.Add(sequence.FrameIndex); }
            sequence.Stop();
            for (var i = 0; i < 30 && sequence.IsActive; i++) { sequence.Tick(1f / 60f); trace.Add(sequence.FrameIndex); }
            return trace;
        }

        private static List<int> LadderMiningTrace(int count)
        {
            var sequence = new PlayerLadderMiningAnimationSequence(new PlayerLadderMiningAnimationSettings());
            if (count > 0) sequence.SetFrameCount(count);
            var trace = new List<int>();
            sequence.Start(-1);
            for (var i = 0; i < 60; i++) { sequence.Tick(1f / 30f); trace.Add(sequence.FrameIndex); }
            sequence.Complete();
            for (var i = 0; i < 20 && sequence.IsActive; i++) { sequence.Tick(1f / 60f); trace.Add(sequence.FrameIndex); }
            Assert.That(sequence.IsActive, Is.False);
            return trace;
        }

        // 상승 → 정점 → 긴 낙하 → 착지까지의 (단계:그림 묶음:번호) 기록.
        private static List<string> JumpTrace(int ready, int rise, int fall)
        {
            var sequence = new PlayerJumpAnimationSequence(new PlayerJumpAnimationSettings());
            if (ready > 0) sequence.SetFrameCounts(ready, rise, fall);
            var trace = new List<string>();
            sequence.StartJump(10f);
            var speed = 10f;
            for (var i = 0; i < 150; i++)
            {
                speed -= 0.4f;
                sequence.Tick(1f / 60f, true, speed, false);
                trace.Add(sequence.Phase + ":" + sequence.Clip + ":" + sequence.FrameIndex);
            }

            for (var i = 0; i < 40 && sequence.IsActive; i++)
            {
                sequence.Tick(1f / 60f, false, 0f, false);
                trace.Add(sequence.Phase + ":" + sequence.Clip + ":" + sequence.FrameIndex);
            }

            return trace;
        }

        private static int Index(string entry) => int.Parse(entry.Substring(entry.LastIndexOf(':') + 1));

        private static IEnumerable<int> FallLoop(List<string> trace) =>
            trace.Where(s => s.StartsWith("FallLoop")).Select(Index).Distinct().OrderBy(i => i);

        private static string Serialize(GameState state)
        {
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var data = mapper.Capture(new SaveCaptureContext(state, new InventoryState(), new UpgradeState(), null, null,
                SceneNames.SurfaceBase, "b208-test"));
            Assert.That(data, Is.Not.Null);
            return new SaveJsonCodec(new SaveMigrationService()).Serialize(data);
        }

        private static GameState Restore(string json)
        {
            var status = new SaveJsonCodec(new SaveMigrationService()).TryDeserialize(json, out var data);
            Assert.That(status, Is.EqualTo(SaveMigrationStatus.Current).Or.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(new SaveDataMapper(new SystemSaveClock()).TryRestore(data, out var restored), Is.True);
            return restored.GameState;
        }

        private sealed class Rig
        {
            public GameObject Root;
            public PlayerMovement Movement;
            public PlayerAnimationController Animation;
            public SpriteRenderer Renderer;
        }

        private Rig CreatePlayer()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab));
            created.Add(root);
            var rig = new Rig
            {
                Root = root,
                Movement = root.GetComponent<PlayerMovement>(),
                Animation = root.GetComponentInChildren<PlayerAnimationController>()
            };
            rig.Renderer = rig.Animation.GetComponent<SpriteRenderer>();
            Call(rig.Movement, "Awake");
            Call(rig.Animation, "Awake");
            typeof(PlayerMovement).GetField("<IsGrounded>k__BackingField", Private).SetValue(rig.Movement, true);
            return rig;
        }

        private static object Field(object target, string name) =>
            target.GetType().GetField(name, Private).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        private static void Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}
