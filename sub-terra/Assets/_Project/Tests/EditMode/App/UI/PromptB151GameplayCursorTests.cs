using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.GameplayCursor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-151 드릴·집게 커서: 시트 슬라이스, 가속·감속, 시트 전환, 청록 팁, 클릭 비차단.</summary>
    public sealed class PromptB151GameplayCursorTests
    {
        private const float Step = 1f / 60f;
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            GameplayCursorState.Unregister(GameplayCursorState.Source);
            if (host != null) Object.DestroyImmediate(host);
        }

        private static GameplayCursorSkin LoadSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameplayCursorSkin>(PromptB151GameplayCursorBuilder.SkinAssetPath);
            Assert.That(skin, Is.Not.Null, "GameplayCursorSkin 에셋이 없습니다. B-151 빌더를 실행하세요.");
            return skin;
        }

        private static GameplayCursorModel NewModel()
        {
            return new GameplayCursorModel(new GameplayCursorTuning());
        }

        private static void Run(GameplayCursorModel model, float seconds, GameplayCursorInput input)
        {
            for (var t = 0f; t < seconds; t += Step) model.Tick(Step, input);
        }

        private GameplayCursorController NewController()
        {
            host = new GameObject("CursorHost");
            var controller = GameplayCursorController.AttachTo(host.transform);
            controller.SetSkin(LoadSkin());
            Assert.That(controller.EnsureBuilt(), Is.True);
            return controller;
        }

        [Test]
        public void Sheets_SliceIntoEightAndFour_WithTipPivotAtUpperLeft()
        {
            var skin = LoadSkin();
            Assert.That(skin.drillFrames, Has.Length.EqualTo(8));
            Assert.That(skin.clawFrames, Has.Length.EqualTo(4));
            foreach (var sprite in skin.drillFrames) AssertTipPivot(sprite);
            foreach (var sprite in skin.clawFrames) AssertTipPivot(sprite);
            Assert.That(Resources.Load<GameplayCursorSkin>(GameplayCursorSkin.ResourcePath), Is.SameAs(skin));
        }

        private static void AssertTipPivot(Sprite sprite)
        {
            Assert.That(sprite, Is.Not.Null);
            var pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            Assert.That(pivot.x, Is.LessThan(0.3f), sprite.name + " 핫스팟이 왼쪽 끝이 아닙니다.");
            Assert.That(pivot.y, Is.GreaterThan(0.75f), sprite.name + " 핫스팟이 위쪽 끝이 아닙니다.");
        }

        [Test]
        public void Idle_ShowsFirstDrillFrameOnly()
        {
            var model = NewModel();
            Run(model, 1f, default);
            Assert.That(model.Sheet, Is.EqualTo(GameplayCursorSheet.Drill));
            Assert.That(model.DrillFrameIndex, Is.EqualTo(0));
            Assert.That(model.JitterAmplitude, Is.EqualTo(0f));
        }

        [Test]
        public void LeftHold_StartsAroundThirdFrame_AndClimbsToTopSpeed()
        {
            var model = NewModel();
            var held = new GameplayCursorInput { LeftHeld = true };
            model.Tick(Step, held);
            Assert.That(model.DrillFrameIndex, Is.InRange(2, 3), "시작은 3~4번대");
            var start = model.Speed;
            Run(model, 3f, held);
            Assert.That(model.Speed, Is.GreaterThan(start));
            Assert.That(model.Speed, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(model.DrillFrameIndex, Is.InRange(6, 7), "최고속은 7~8번");
            Assert.That(model.JitterAmplitude, Is.GreaterThan(0f).And.LessThanOrEqualTo(2f));
        }

        [Test]
        public void Release_SlowsDownToFirstFrame()
        {
            var model = NewModel();
            Run(model, 3f, new GameplayCursorInput { LeftHeld = true });
            model.Tick(Step, default);
            var justReleased = model.Speed;
            Run(model, 0.5f, default);
            Assert.That(model.Speed, Is.LessThan(justReleased).And.GreaterThan(0f), "천천히 내려온다");
            Run(model, 5f, default);
            Assert.That(model.Speed, Is.EqualTo(0f));
            Assert.That(model.DrillFrameIndex, Is.EqualTo(0));
        }

        [Test]
        public void RepressWhileSlowing_ContinuesFromCurrentSpeed()
        {
            var model = NewModel();
            Run(model, 3f, new GameplayCursorInput { LeftHeld = true });
            Run(model, 0.4f, default);
            var slowing = model.Speed;
            Assert.That(slowing, Is.LessThan(1f).And.GreaterThan(new GameplayCursorTuning().startSpeed));
            model.Tick(Step, new GameplayCursorInput { LeftHeld = true });
            Assert.That(model.Speed, Is.GreaterThanOrEqualTo(slowing), "1번으로 리셋하지 않는다");
            Assert.That(model.DrillFrameIndex, Is.GreaterThan(0));
        }

        [Test]
        public void MoveMode_UsesClawSheet_AndReturnsToDrillWhenOff()
        {
            var model = NewModel();
            model.Tick(Step, default);
            model.Tick(Step, new GameplayCursorInput { MoveMode = true });
            Assert.That(model.Sheet, Is.EqualTo(GameplayCursorSheet.Claw));
            Assert.That(model.ClawFrameIndex, Is.InRange(0, 1), "기본은 1~2번");
            Assert.That(model.PulseScale, Is.GreaterThan(1f), "전환 순간 살짝 커진다");
            Assert.That(model.RingAlpha, Is.GreaterThan(0f));

            model.Tick(Step, new GameplayCursorInput { MoveMode = true, HoverGrabbable = true });
            Assert.That(model.ClawFrameIndex, Is.InRange(2, 3), "잡을 수 있는 시설 위에서 연다");
            model.Tick(Step, new GameplayCursorInput { MoveMode = true, Carrying = true });
            Assert.That(model.ClawFrameIndex, Is.EqualTo(3), "옮기는 중에는 잡기");

            Run(model, 0.3f, new GameplayCursorInput { MoveMode = true });
            Assert.That(model.PulseScale, Is.EqualTo(1f));
            model.Tick(Step, new GameplayCursorInput { LeftHeld = true });
            Assert.That(model.Sheet, Is.EqualTo(GameplayCursorSheet.Drill));
            Assert.That(model.DrillFrameIndex, Is.GreaterThan(0), "돌아올 때 좌클릭 상태에 맞춘다");
        }

        [Test]
        public void TipGlowAndParticles_OnlyWhileHoverGrabbable()
        {
            var controller = NewController();
            controller.Apply(default, Step, new Vector2(100f, 100f));
            Assert.That(controller.Glow.enabled, Is.False);
            Assert.That(controller.Model.ParticlesActive, Is.False);

            for (var i = 0; i < 30; i++)
                controller.Apply(new GameplayCursorInput { HoverGrabbable = true }, Step, new Vector2(100f, 100f));
            Assert.That(controller.Glow.enabled, Is.True);
            Assert.That(controller.Model.ParticlesActive, Is.True);
            Assert.That(controller.ActiveParticleCount, Is.GreaterThan(0));
            var color = controller.Glow.color;
            Assert.That(color.g, Is.GreaterThan(color.r), "청록색");

            for (var i = 0; i < 90; i++) controller.Apply(default, Step, new Vector2(100f, 100f));
            Assert.That(controller.Glow.enabled, Is.False);
            Assert.That(controller.Model.ParticlesActive, Is.False);
            Assert.That(controller.ActiveParticleCount, Is.EqualTo(0));
        }

        [Test]
        public void CursorImages_DoNotBlockRaycasts()
        {
            var controller = NewController();
            Assert.That(controller.Canvas, Is.Not.Null);
            Assert.That(controller.Canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(controller.Canvas.sortingOrder, Is.EqualTo(GameplayCursorController.SortingOrder));
            Assert.That(controller.Canvas.GetComponent<GraphicRaycaster>(), Is.Null);
            Assert.That(controller.Canvas.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            foreach (var graphic in controller.Canvas.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
        }

        [Test]
        public void Jitter_MovesArtOnly_HotspotStaysOnPointer()
        {
            var controller = NewController();
            var pointer = new Vector2(321f, 234f);
            var moved = false;
            for (var i = 0; i < 60; i++)
            {
                controller.Apply(new GameplayCursorInput { LeftHeld = true }, Step, pointer);
                Assert.That(Vector2.Distance(controller.Hotspot.position, pointer), Is.LessThan(0.01f));
                var offset = controller.Visual.rectTransform.anchoredPosition;
                Assert.That(offset.magnitude, Is.LessThanOrEqualTo(new GameplayCursorTuning().jitterPixels * 1.5f));
                moved |= offset.sqrMagnitude > 0f;
            }
            Assert.That(moved, Is.True);
        }

        [Test]
        public void Controller_FollowsMoveStateSource()
        {
            var controller = NewController();
            var source = new FakeSource { MoveMode = true, Carrying = true };
            GameplayCursorState.Register(source);
            controller.Apply(GameplayCursorState.Read(false), Step, Vector2.zero);
            Assert.That(controller.Model.Sheet, Is.EqualTo(GameplayCursorSheet.Claw));
            Assert.That(controller.Visual.sprite, Is.SameAs(LoadSkin().clawFrames[3]));

            source.MoveMode = false;
            source.Carrying = false;
            controller.Apply(GameplayCursorState.Read(false), Step, Vector2.zero);
            Assert.That(controller.Model.Sheet, Is.EqualTo(GameplayCursorSheet.Drill));
            Assert.That(controller.Visual.sprite, Is.SameAs(LoadSkin().drillFrames[0]));
        }

        [Test]
        public void CustomCursor_OnlyInGameplayScenes()
        {
            Assert.That(GameplayCursorController.IsGameplayScene("SurfaceBase"), Is.True);
            Assert.That(GameplayCursorController.IsGameplayScene("Mine_Demo_Integration"), Is.True);
            Assert.That(GameplayCursorController.IsGameplayScene("MainMenu"), Is.False);
            Assert.That(GameplayCursorController.IsGameplayScene("Bootstrap"), Is.False);
        }

        private sealed class FakeSource : IGameplayCursorStateSource
        {
            public bool MoveMode;
            public bool Carrying;
            public bool Hover;
            public bool CursorMoveMode => MoveMode;
            public bool CursorCarrying => Carrying;
            public bool CursorHoverGrabbable => Hover;
        }
    }
}
