using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>B-199 / #349: 버팀목 → 완충판 이름, 높이 조절, 착지 피해 10%, 저장, 안내 홀로그램.</summary>
    public sealed class PromptB199CushionPadTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string OldName = "버팀목";
        private const string NewName = "완충판";

        // ───────── 이름 ─────────

        [Test]
        public void Names_DisplayNameDataAndMenuLabelUseCushionPad()
        {
            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.SupportBasic), Is.EqualTo(NewName));
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(PhaseFSupportBuilder.SupportDataPath);
            Assert.That(data.Id, Is.EqualTo(DataIds.Buildings.SupportBasic), "내부 ID는 그대로");
            Assert.That(data.DisplayName, Is.EqualTo(NewName));
            Assert.That(data.Description, Does.Not.Contain(OldName));
            Assert.That(data.Description, Does.Contain("10%"));
            Assert.That(data.BuildCosts.Single().Quantity, Is.EqualTo(1), "비용(구리 1개)은 바꾸지 않는다");

            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB199CushionPadBuilder.BuildingMenuPrefabPath);
            var labels = menu.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text).ToArray();
            Assert.That(labels, Does.Not.Contain(OldName));
            Assert.That(labels, Does.Contain(NewName));
        }

        [Test]
        public void Names_GuideAndObjectiveTextsNeverShowOldName()
        {
            foreach (GuideTabKind tab in Enum.GetValues(typeof(GuideTabKind)))
            {
                var text = GameGuideCatalog.PlainText(tab);
                Assert.That(text, Does.Not.Contain(OldName), tab + " 가이드");
            }

            Assert.That(GameGuideCatalog.Get("fac.support").Title, Is.EqualTo(NewName));
            Assert.That(GameGuideCatalog.PlainText(GuideTabKind.Resources), Does.Contain(NewName));
            Assert.That(GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get("mech.structure"), null)),
                Does.Contain("약 10%").And.Not.Contain("추가 피해를 막"));
            Assert.That(GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get("fac.support"), null)),
                Does.Contain("위/아래 방향키"));

            var objective = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.PlaceSupportInDanger);
            Assert.That(objective.Description + objective.NextActionHint, Does.Not.Contain(OldName));
            Assert.That(objective.Description, Does.Contain(NewName));
            Assert.That(objective.Description, Does.Not.Contain("추가 피해를 막"));
        }

        // ───────── 높이 규칙 ─────────

        [Test]
        public void Height_MinimumIsBelowFullLoadJumpAndMaximumIsOldHeight()
        {
            var jumpHeight = FullLoadJumpHeight();
            Assert.That(jumpHeight, Is.EqualTo(1.16f).Within(0.05f), "이론 만적 점프 높이");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhaseFSupportBuilder.SupportPrefabPath);
            var pad = prefab.GetComponent<CushionPad>();
            Assert.That(pad.MinHeight, Is.LessThan(jumpHeight - 0.25f), "만적으로 여유 있게 올라갈 수 있다");
            Assert.That(pad.MaxHeight, Is.EqualTo(1.8f).Within(0.0001f));
            Assert.That(pad.MaxHeight, Is.GreaterThan(jumpHeight), "최대 높이는 만적 점프로는 못 올라간다");
            Assert.That(pad.StepHeight, Is.GreaterThan(0.1f).And.LessThanOrEqualTo(0.5f));
            Assert.That(CushionPadRules.StepCount(pad.MinHeight, pad.MaxHeight, pad.StepHeight), Is.EqualTo(4));
        }

        [Test]
        public void Height_StepsClampAndNormalizeBrokenValues()
        {
            const float min = 0.8f, max = 1.8f, step = 0.25f;
            var h = min;
            var visited = new System.Collections.Generic.List<float> { h };
            for (var i = 0; i < 10; i++)
            {
                var next = CushionPadRules.StepHeight(h, 1, min, max, step);
                if (Mathf.Approximately(next, h)) break;
                h = next;
                visited.Add(h);
            }

            Assert.That(visited.Last(), Is.EqualTo(max).Within(0.0001f));
            Assert.That(visited.Count, Is.EqualTo(5));
            Assert.That(CushionPadRules.StepHeight(max, 1, min, max, step), Is.EqualTo(max));
            Assert.That(CushionPadRules.StepHeight(min, -1, min, max, step), Is.EqualTo(min));
            Assert.That(CushionPadRules.StepHeight(1.7f, 1, min, max, step), Is.EqualTo(max), "마지막 단계는 최대로 자른다");
            Assert.That(CushionPadRules.NormalizeHeight(0f, min, max), Is.EqualTo(min));
            Assert.That(CushionPadRules.NormalizeHeight(-3f, min, max), Is.EqualTo(min));
            Assert.That(CushionPadRules.NormalizeHeight(float.NaN, min, max), Is.EqualTo(min));
            Assert.That(CushionPadRules.NormalizeHeight(float.PositiveInfinity, min, max), Is.EqualTo(min));
            Assert.That(CushionPadRules.NormalizeHeight(50f, min, max), Is.EqualTo(max));
            Assert.That(CushionPadRules.NormalizeHeight(1.3f, min, max), Is.EqualTo(1.3f));
        }

        [Test]
        public void Prefab_StartsAtMinimumAndSurfaceFollowsHeight()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhaseFSupportBuilder.SupportPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var pad = instance.GetComponent<CushionPad>();
                var collider = instance.GetComponent<BoxCollider2D>();
                Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MinHeight));
                Assert.That(instance.GetComponents<BoxCollider2D>().Length, Is.EqualTo(1));
                Assert.That(collider.usedByEffector, Is.True);
                Assert.That(instance.GetComponent<PlatformEffector2D>().useOneWay, Is.True);
                Assert.That(instance.GetComponent<StructuralSupport>(), Is.Not.Null);
                Assert.That(instance.TryGetComponent<IAdjustableLandingPlatform>(out _), Is.True);
                Assert.That(instance.TryGetComponent<IStableLandingPlatform>(out _), Is.True);
                AssertSurface(instance, pad, collider, pad.MinHeight);

                var events = 0;
                pad.HeightChanged += _ => events++;
                var expected = pad.MinHeight;
                while (pad.TryStepHeight(1, out var delta))
                {
                    expected += pad.StepHeight;
                    Assert.That(delta, Is.EqualTo(pad.StepHeight).Within(0.0001f));
                    AssertSurface(instance, pad, collider, Mathf.Min(expected, pad.MaxHeight));
                }

                Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MaxHeight).Within(0.0001f));
                AssertSurface(instance, pad, collider, pad.MaxHeight);
                Assert.That(pad.TryStepHeight(1, out var none), Is.False);
                Assert.That(none, Is.Zero);
                Assert.That(events, Is.EqualTo(4));
                while (pad.TryStepHeight(-1, out _)) { }
                Assert.That(pad.CurrentHeight, Is.EqualTo(pad.MinHeight).Within(0.0001f));
                AssertSurface(instance, pad, collider, pad.MinHeight);
                Assert.That(pad.TryStepHeight(-1, out _), Is.False);

                // 윗면 그림(Cap)은 윗변 피벗이라 콜라이더 윗면과 같은 높이에 놓이고, 기둥은 Tiled로 늘어난다.
                var post = pad.PostRenderer;
                Assert.That(post.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(pad.TopRenderer.sprite.pivot.y, Is.EqualTo(pad.TopRenderer.sprite.rect.height).Within(0.01f));
                pad.SetHeight(pad.MaxHeight);
                var tall = post.size.y;
                pad.SetHeight(pad.MinHeight);
                Assert.That(post.size.y, Is.LessThan(tall));
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void AssertSurface(GameObject root, CushionPad pad, BoxCollider2D collider, float height)
        {
            // 지면 = 루트 y - 0.5. 윗면 = 콜라이더 윗변.
            var top = collider.offset.y + collider.size.y * 0.5f;
            Assert.That(top, Is.EqualTo(height - 0.5f).Within(0.001f), "콜라이더 윗면");
            Assert.That(pad.TopRenderer.transform.localPosition.y, Is.EqualTo(height - 0.5f).Within(0.001f), "윗면 그림");
            Assert.That(pad.CurrentHeight, Is.EqualTo(height).Within(0.001f));
        }

        // ───────── 착지 피해 ─────────

        [Test]
        public void Landing_CushionDamageIsTenPercentOfNormalAndGroundUnchanged()
        {
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            try
            {
                Assert.That(settings.CushionLandingDamageRatio, Is.EqualTo(0.1f).Within(0.0001f));
                var ratio = settings.CushionLandingDamageRatio;
                foreach (var distance in new[] { 10f, 25f, 60f, 110f })
                {
                    var baseDamage = PlayerFallDamageRules.CalculateDamage(distance, false);
                    foreach (var cargo in new[] { 1f, 1.25f, 1.5f })
                    {
                        var normal = PlayerFallDamageRules.ScaleDamage(baseDamage, cargo);
                        var cushion = PlayerFallDamageRules.ScaleDamage(baseDamage, cargo, true, ratio);
                        Assert.That(cushion, Is.EqualTo((int)Math.Round(normal * 0.1, MidpointRounding.AwayFromZero)),
                            $"낙하 {distance} 짐 {cargo}");
                        Assert.That(cushion, Is.LessThan(normal));
                        Assert.That(PlayerFallDamageRules.ScaleDamage(baseDamage, cargo, false, ratio), Is.EqualTo(normal),
                            "옆 땅·일반 블록은 기존 규칙");
                    }
                }

                Assert.That(PlayerFallDamageRules.ScaleDamage(10, 1f, true, 0f), Is.Zero, "비율 0이면 피해 없음");
                Assert.That(PlayerFallDamageRules.ScaleDamage(100, 1f, true, 0.1f), Is.EqualTo(10));
                Assert.That(PlayerFallDamageRules.ScaleDamage(100, 1.5f, true, 0.1f), Is.EqualTo(15));
                Assert.That(PlayerFallDamageRules.ScaleDamage(100, 1f, true, 5f), Is.EqualTo(100), "비율은 1을 넘지 않는다");
                Assert.That(PlayerFallDamageRules.ScaleDamage(0, 1.5f, true, 0.1f), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        // ───────── 저장 ─────────

        [Test]
        public void Save_HeightFieldRoundTripsAndOldSaveWithoutFieldLoadsAsZero()
        {
            var dto = new BuildingSnapshotDto
            {
                instanceId = "building.support.basic-0001",
                buildingTypeId = DataIds.Buildings.SupportBasic,
                cushionHeight = 1.3f
            };
            var json = JsonUtility.ToJson(dto);
            Assert.That(JsonUtility.FromJson<BuildingSnapshotDto>(json).cushionHeight, Is.EqualTo(1.3f));

            const string oldSave = "{\"instanceId\":\"building.support.basic-0001\",\"buildingTypeId\":\"building.support.basic\","
                + "\"x\":2,\"y\":-4,\"rotation\":0,\"level\":1,\"health\":1}";
            var loaded = JsonUtility.FromJson<BuildingSnapshotDto>(oldSave);
            Assert.That(loaded.cushionHeight, Is.Zero);
            Assert.That(CushionPadRules.NormalizeHeight(loaded.cushionHeight, 0.8f, 1.8f), Is.EqualTo(0.8f));
        }

        // ───────── 입력 ─────────

        [Test]
        public void Input_OnPadArrowVerticalIsReservedForHeightButWasdAndHorizontalKeepWorking()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var originalActive = PlayerKeyboardControls.AdjustablePadActive;
            try
            {
                PlayerKeyboardControls.AdjustablePadActive = false;
                Press(keyboard, Key.UpArrow);
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.ArrowsMove), Is.EqualTo(Vector2.up));
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.Classic), Is.EqualTo(Vector2.up));

                PlayerKeyboardControls.AdjustablePadActive = true;
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.ArrowsMove), Is.EqualTo(Vector2.zero));
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.Classic), Is.EqualTo(Vector2.zero));
                Press(keyboard, Key.DownArrow);
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.ArrowsMove), Is.EqualTo(Vector2.zero));

                Press(keyboard, Key.LeftArrow);
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.ArrowsMove), Is.EqualTo(Vector2.left));
                Press(keyboard, Key.W);
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.Classic), Is.EqualTo(Vector2.up),
                    "Classic의 W는 판 위에서도 이동");
                Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, ControlScheme.WasdMove), Is.EqualTo(Vector2.up),
                    "WASD 스킴의 W는 기존 이동 유지");

                // WASD 스킴에서 위/아래 화살표는 채굴 키다. 판 위에서는 높이 조절만 하고 위·아래 채굴은 하지 않는다.
                Press(keyboard, Key.UpArrow);
                Assert.That(PlayerKeyboardControls.ReadMiningDirection(keyboard, ControlScheme.WasdMove), Is.EqualTo(Vector2.zero));
                Press(keyboard, Key.RightArrow);
                Assert.That(PlayerKeyboardControls.ReadMiningDirection(keyboard, ControlScheme.WasdMove), Is.EqualTo(Vector2.right));
                PlayerKeyboardControls.AdjustablePadActive = false;
                Press(keyboard, Key.UpArrow);
                Assert.That(PlayerKeyboardControls.ReadMiningDirection(keyboard, ControlScheme.WasdMove), Is.EqualTo(Vector2.up));
            }
            finally
            {
                PlayerKeyboardControls.AdjustablePadActive = originalActive;
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void Input_PadStepUsesOnlyUpOrDownArrowPress()
        {
            Assert.That(PlayerKeyboardControls.ReadPadStep(true, false), Is.EqualTo(1));
            Assert.That(PlayerKeyboardControls.ReadPadStep(false, true), Is.EqualTo(-1));
            Assert.That(PlayerKeyboardControls.ReadPadStep(true, true), Is.Zero, "동시 입력은 상쇄");
            Assert.That(PlayerKeyboardControls.ReadPadStep(false, false), Is.Zero);
            Assert.That(PlayerKeyboardControls.ReadPadStep((Keyboard)null), Is.Zero);
        }

        // ───────── 힌트 홀로그램 ─────────

        [Test]
        public void Hint_ShowsOncePerEntryHidesAfterAdjustAndResetsOnNewEntry()
        {
            CushionPadHintSession.Reset();
            try
            {
                var logic = new CushionPadHintLogic();
                Assert.That(logic.Tick(false, 1f), Is.False);
                Assert.That(logic.Tick(true, CushionPadHintLogic.StandDelaySeconds * 0.5f), Is.False, "잠깐 스친 것은 제외");
                Assert.That(logic.Visible, Is.False);
                Assert.That(logic.Tick(true, CushionPadHintLogic.StandDelaySeconds), Is.True);
                Assert.That(logic.Visible, Is.True);
                Assert.That(logic.Tick(true, 1f), Is.False, "이미 보이는 중이면 다시 알리지 않는다");

                logic.NotifyAdjusted();
                Assert.That(logic.Visible, Is.False, "높이를 바꾸면 스스로 사라진다");
                Assert.That(logic.Tick(true, 5f), Is.False);
                Assert.That(logic.Tick(false, 0.1f), Is.False);
                Assert.That(logic.Tick(true, 5f), Is.False, "같은 입장에서 다시 올라가도 안 나온다");
                Assert.That(logic.Visible, Is.False);

                // 판에서 내려도 안 바꾼 채 내려가면 사라진다. 새 입장(Reset)에서는 다시 한 번 보인다.
                CushionPadHintSession.Reset();
                var next = new CushionPadHintLogic();
                Assert.That(next.Tick(true, 1f), Is.True);
                Assert.That(next.Tick(false, 0.1f), Is.False);
                Assert.That(next.Visible, Is.False);
                Assert.That(new CushionPadHintLogic().Tick(true, 1f), Is.False, "같은 입장의 다른 인스턴스도 다시 안 보인다");
            }
            finally { CushionPadHintSession.Reset(); }
        }

        // ───────── 설치 미리보기·SFX ─────────

        [Test]
        public void Preview_UsesTopArtFirstAndNoNewSfxIds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhaseFSupportBuilder.SupportPrefabPath);
            var visual = prefab.transform.Find("VisualRoot");
            Assert.That(visual.GetChild(0).name, Is.EqualTo("Cap"), "미리보기는 첫 렌더러(윗면)를 대표로 쓴다");
            Assert.That(visual.GetChild(0).GetComponent<SpriteRenderer>().enabled, Is.True);

            // 새 SfxId를 추가하지 않았다: 높이 조절=KeycapPop, 힌트=KeysHoloOpen 모두 기존 값.
            Assert.That(Enum.IsDefined(typeof(SubTerra.Shared.Audio.SfxId), "KeycapPop"), Is.True);
            Assert.That(Enum.IsDefined(typeof(SubTerra.Shared.Audio.SfxId), "KeysHoloOpen"), Is.True);
        }

        // ───────── helpers ─────────

        private static float FullLoadJumpHeight()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var movement = new SerializedObject(player.GetComponent<PlayerMovement>());
            var body = player.GetComponent<Rigidbody2D>();
            var impulse = movement.FindProperty("jumpImpulse").floatValue;
            var speed = impulse * CargoLoadEffectPolicy.FullLoadJumpMultiplier / Mathf.Max(0.0001f, body.mass);
            var gravity = Mathf.Abs(Physics2D.gravity.y) * body.gravityScale;
            return speed * speed / (2f * gravity);
        }

        private static void Press(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private static void SetGrounded(PlayerMovement movement, bool grounded, IAdjustableLandingPlatform platform)
        {
            var type = typeof(PlayerMovement);
            type.GetProperty("IsGrounded").GetSetMethod(true).Invoke(movement, new object[] { grounded });
            type.GetField("standingPlatform", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, platform);
        }
    }
}
