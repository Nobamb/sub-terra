using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.Gameplay.Building;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>홀로그램 시설 이름표: 시간표, 비주얼 구성, 일반 화면 표시 규칙.</summary>
    public sealed class FacilityNameTagTests
    {
        [Test]
        public void Timeline_Durations_AreShortAndMatchTheRequest()
        {
            Assert.That(FacilityNameTagTimeline.ShowDuration, Is.InRange(0.2f, 0.3f));
            Assert.That(FacilityNameTagTimeline.HideDuration, Is.InRange(0.15f, 0.2f));
        }

        [Test]
        public void Timeline_Show_GrowsFromSmallSquareThenSpreadsThenRevealsText()
        {
            var start = FacilityNameTagTimeline.Evaluate(0f, true);
            Assert.That(start.FrameAlpha, Is.Zero);
            Assert.That(start.Reveal, Is.Zero);

            // 작은 네모 모서리 빛: 프레임은 아직 펼쳐지지 않았고 글자도 없다.
            var corner = FacilityNameTagTimeline.Evaluate(0.2f, true);
            Assert.That(corner.FrameAlpha, Is.EqualTo(1f));
            Assert.That(corner.Spread, Is.LessThan(0.2f));
            Assert.That(corner.Flare, Is.GreaterThan(FacilityNameTagTimeline.SteadyFlare));
            Assert.That(corner.TextAlpha, Is.Zero);

            // 프레임이 좌우로 펼쳐지는 동안에도 글자는 나오지 않는다.
            var spreading = FacilityNameTagTimeline.Evaluate(0.4f, true);
            Assert.That(spreading.Spread, Is.InRange(0.3f, 1f));
            Assert.That(spreading.Reveal, Is.Zero);

            // 스캔선이 지나가며 글자가 왼쪽부터 드러난다.
            var scan = FacilityNameTagTimeline.Evaluate(0.8f, true);
            Assert.That(scan.Spread, Is.EqualTo(1f).Within(0.001f));
            Assert.That(scan.Reveal, Is.InRange(0.3f, 0.9f));
            Assert.That(scan.ScanAlpha, Is.GreaterThan(0.5f));
            Assert.That(scan.ScanPosition, Is.EqualTo(scan.Reveal).Within(0.001f));

            var done = FacilityNameTagTimeline.Evaluate(1f, true);
            Assert.That(done.Reveal, Is.EqualTo(1f));
            Assert.That(done.TextAlpha, Is.EqualTo(1f));
            Assert.That(done.ScanAlpha, Is.Zero, "표시 중에는 스캔선이 남지 않는다.");
            Assert.That(done.Flare, Is.EqualTo(FacilityNameTagTimeline.SteadyFlare).Within(0.001f));
        }

        [Test]
        public void Timeline_Show_NeverMovesBackwardsAsLevelRises()
        {
            var previous = FacilityNameTagTimeline.Evaluate(0f, true);
            for (var i = 1; i <= 100; i++)
            {
                var frame = FacilityNameTagTimeline.Evaluate(i / 100f, true);
                Assert.That(frame.Spread, Is.GreaterThanOrEqualTo(previous.Spread - 1e-5f), "펼침 " + i);
                Assert.That(frame.Reveal, Is.GreaterThanOrEqualTo(previous.Reveal - 1e-5f), "드러남 " + i);
                Assert.That(frame.FrameAlpha, Is.GreaterThanOrEqualTo(previous.FrameAlpha - 1e-5f), "프레임 " + i);
                previous = frame;
            }
        }

        [Test]
        public void Timeline_Hide_FadesTextFirstThenFoldsFrameToCenter()
        {
            var full = FacilityNameTagTimeline.Evaluate(1f, false);
            Assert.That(full.TextAlpha, Is.EqualTo(1f));
            Assert.That(full.Spread, Is.EqualTo(1f).Within(0.001f));

            // 글자가 거의 사라진 시점에도 프레임은 아직 남아 접히는 중이다.
            var folding = FacilityNameTagTimeline.Evaluate(0.4f, false);
            Assert.That(folding.TextAlpha, Is.LessThan(0.2f));
            Assert.That(folding.Spread, Is.InRange(0.2f, 1f));
            Assert.That(folding.Reveal, Is.EqualTo(1f), "퇴장에서는 글자를 잘라내지 않고 흐려지기만 한다.");
            Assert.That(folding.ScanAlpha, Is.Zero);

            var gone = FacilityNameTagTimeline.Evaluate(0f, false);
            Assert.That(gone.FrameAlpha, Is.Zero);
            Assert.That(gone.TextAlpha, Is.Zero);
        }

        [Test]
        public void Timeline_Advance_MovesTowardsWantedAndClamps()
        {
            var level = 0f;
            for (var i = 0; i < 100; i++)
            {
                level = FacilityNameTagTimeline.Advance(level, true, 1f / 60f);
            }

            Assert.That(level, Is.EqualTo(1f));
            Assert.That(FacilityNameTagTimeline.Advance(0.5f, true, 0.026f), Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(FacilityNameTagTimeline.Advance(0.5f, false, 0.017f), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(FacilityNameTagTimeline.Advance(0.05f, false, 1f), Is.Zero);
        }

        [Test]
        public void Timeline_SteadyPulse_IsGentleAndNeverFlickers()
        {
            var previous = FacilityNameTagTimeline.Pulse(0f);
            var min = previous;
            var max = previous;
            for (var i = 1; i < 600; i++)
            {
                var value = FacilityNameTagTimeline.Pulse(i / 60f);
                Assert.That(Mathf.Abs(value - previous), Is.LessThan(0.01f), "한 프레임 변화가 작다.");
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
                previous = value;
            }

            Assert.That(min, Is.GreaterThan(0.8f));
            Assert.That(max, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Visual_RepeatedWanted_DoesNotRestartTheShowAnimation()
        {
            var root = new GameObject("TagRoot", typeof(RectTransform));
            try
            {
                var visual = FacilityNameTagVisual.CreateOverlay((RectTransform)root.transform, null, "Tag");
                visual.SetLabel("충전기");
                visual.SetWanted(true);
                for (var i = 0; i < 6; i++)
                {
                    visual.Tick(1f / 60f);
                }

                var level = visual.Level;
                Assert.That(level, Is.InRange(0.2f, 0.9f));
                for (var i = 0; i < 20; i++)
                {
                    visual.SetWanted(true);
                }

                Assert.That(visual.Level, Is.EqualTo(level), "같은 방향 요청은 진행을 되돌리지 않는다.");

                // 접는 도중에 다시 요청해도 현재 모습에서 이어서 펼쳐진다.
                visual.SetWanted(false);
                visual.Tick(1f / 60f);
                var folding = visual.Level;
                visual.SetWanted(true);
                visual.Tick(1f / 60f);
                Assert.That(visual.Level, Is.GreaterThan(folding));

                for (var i = 0; i < 60; i++)
                {
                    visual.Tick(1f / 60f);
                }

                Assert.That(visual.IsSettled, Is.True);
                visual.SetWanted(false);
                for (var i = 0; i < 30; i++)
                {
                    visual.Tick(1f / 60f);
                }

                Assert.That(visual.IsVisible, Is.False);
                Assert.That(visual.Root.activeSelf, Is.False, "사라지면 오브젝트도 꺼진다.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Visual_DoesNotBlockInput_AndShowsNameOnly()
        {
            var parent = new GameObject("TagParent");
            try
            {
                var visual = FacilityNameTagVisual.CreateWorld(
                    parent.transform, null, FacilityNameTagLayers.MainScreen, "Tag");
                visual.SetLabel("정산 콘솔");
                var graphics = visual.Root.GetComponentsInChildren<Graphic>(true);
                Assert.That(graphics.Length, Is.GreaterThan(0));
                for (var i = 0; i < graphics.Length; i++)
                {
                    Assert.That(graphics[i].raycastTarget, Is.False, graphics[i].name);
                }

                Assert.That(visual.Root.GetComponent<GraphicRaycaster>(), Is.Null);
                var texts = visual.Root.GetComponentsInChildren<TMPro.TMP_Text>(true);
                Assert.That(texts.Length, Is.EqualTo(1), "시설 이름 외에 다른 글자가 없다.");
                Assert.That(texts[0].text, Is.EqualTo("정산 콘솔"));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Visual_World_UsesMainOnlyLayer_ThatTheCctvCameraDoesNotRender()
        {
            var parent = new GameObject("TagParent");
            try
            {
                var visual = FacilityNameTagVisual.CreateWorld(
                    parent.transform, null, FacilityNameTagLayers.MainScreen, "Tag");
                var transforms = visual.Root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    Assert.That(transforms[i].gameObject.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen), transforms[i].name);
                }

                Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(visual.Root), Is.True);
                var normal = new GameObject("Normal");
                try
                {
                    Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(normal), Is.False, "일반 월드 오브젝트는 CCTV에 그대로 보인다.");
                }
                finally
                {
                    Object.DestroyImmediate(normal);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Visual_LongNames_GetWiderFrameWithPaddingUpToMax()
        {
            var root = new GameObject("TagRoot", typeof(RectTransform));
            try
            {
                var visual = FacilityNameTagVisual.CreateOverlay((RectTransform)root.transform, null, "Tag");
                visual.SetLabel("조");
                Assert.That(visual.Width, Is.EqualTo(FacilityNameTagVisual.MinWidth));
                visual.SetLabel(new string('W', 60));
                Assert.That(visual.Width, Is.EqualTo(FacilityNameTagVisual.MaxWidth));
                Assert.That(visual.TextComponent.overflowMode, Is.EqualTo(TMPro.TextOverflowModes.Overflow));
                Assert.That(visual.TextComponent.alignment, Is.EqualTo(TMPro.TextAlignmentOptions.Center));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProximityLabels_RapidApproachAndLeave_ReusesOneTagWithoutDuplicates()
        {
            var host = new GameObject("TagHost");
            var player = new GameObject("TagPlayer");
            var light = CreateBuilding("Light", DataIds.Buildings.LightBasic, Vector3.zero);
            try
            {
                var controller = host.AddComponent<FacilityProximityLabelController>();
                controller.SetPlayer(player.transform);
                for (var i = 0; i < 20; i++)
                {
                    player.transform.position = i % 2 == 0 ? Vector3.zero : new Vector3(30f, 0f, 0f);
                    controller.Refresh();
                }

                var root = host.transform.Find("FacilityNameBubbles");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.childCount, Is.EqualTo(1), "접근·이탈을 반복해도 이름표는 하나만 쓴다.");
            }
            finally
            {
                Object.DestroyImmediate(light);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ProximityLabels_OverlappingFacilities_KeepOnlyTheNearestReadable()
        {
            var host = new GameObject("TagHost");
            var player = new GameObject("TagPlayer");
            var near = CreateBuilding("Near", DataIds.Buildings.LightBasic, new Vector3(0.2f, 0f, 0f));
            var far = CreateBuilding("Far", DataIds.Buildings.ChargerBasic, new Vector3(0.7f, 0f, 0f));
            try
            {
                var controller = host.AddComponent<FacilityProximityLabelController>();
                controller.SetPlayer(player.transform);
                player.transform.position = Vector3.zero;
                controller.Refresh();

                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(1), "겹치는 이름표는 가까운 시설 것만 남긴다.");
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.LightBasic, out _), Is.True);
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.ChargerBasic, out _), Is.False);

                // 멀리 떨어져 겹치지 않으면 둘 다 표시된다.
                far.transform.position = new Vector3(1.8f, 0f, 0f);
                controller.Refresh();
                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(far);
                Object.DestroyImmediate(near);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ProximityLabels_RemovedFacility_LeavesNoTagBehind()
        {
            var host = new GameObject("TagHost");
            var player = new GameObject("TagPlayer");
            var light = CreateBuilding("Light", DataIds.Buildings.LightBasic, Vector3.zero);
            try
            {
                var controller = host.AddComponent<FacilityProximityLabelController>();
                controller.SetPlayer(player.transform);
                controller.Refresh();
                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(1));

                Object.DestroyImmediate(light);
                light = null;
                controller.Refresh();
                Assert.That(controller.VisibleBubbleCount, Is.Zero);
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.LightBasic, out _), Is.False);
            }
            finally
            {
                if (light != null)
                {
                    Object.DestroyImmediate(light);
                }

                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        private static GameObject CreateBuilding(string name, string buildingId, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<BuildingInstance>().Initialize(name + "-id", buildingId);
            return go;
        }
    }
}
