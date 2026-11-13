using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 195-1 / 195-2: 포탈 탈출은 검은 전체화면 + 웜홀에 빨려 드는 연출, 비상 탈출(전력 고갈 구출)은 암전 + 간단 글리치.
    /// 두 흐름 모두 카메라를 움직이지 않고, 화면이 완전히 검은 동안 옮겨 켜질 때 이미 도착해 있다.
    /// </summary>
    public sealed class PromptB1951EscapeWormholeWarpTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<Object> assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            AccessibilityPreferences.ReduceMotion = false;
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
            for (var i = assets.Count - 1; i >= 0; i--)
            {
                if (assets[i] != null)
                {
                    Object.DestroyImmediate(assets[i]);
                }
            }

            assets.Clear();
            DestroyIfExists("EscapeWarpOverlay");
            DestroyIfExists("EscapeWormholeWarpOverlay");
        }

        // ---------------------------------------------------------------- 웜홀 시간표

        [Test]
        public void Wormhole_DefaultDurations_AndPhaseOrder_TeleportOnceInTheMiddleOfTheBlackout()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            Assert.That(timeline.SuckInDuration, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(timeline.VoidDuration, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(timeline.EmergeDuration, Is.EqualTo(0.65f).Within(0.0001f));
            Assert.That(timeline.TotalDuration, Is.EqualTo(1.65f).Within(0.0001f));

            var phases = new List<EscapeWormholePhase>();
            var teleports = 0;
            var teleportBlack = -1f;
            var teleportWormhole = -1f;
            var teleportPlayer = -1f;
            var teleportFlash = -1f;
            var teleportTime = -1f;
            while (!timeline.IsComplete)
            {
                var frame = timeline.Advance(1f / 60f);
                if (phases.Count == 0 || phases[phases.Count - 1] != frame.Phase)
                {
                    phases.Add(frame.Phase);
                }

                if (frame.ShouldTeleport)
                {
                    teleports++;
                    teleportBlack = frame.BlackoutAlpha;
                    teleportWormhole = frame.WormholeScale;
                    teleportPlayer = frame.PlayerScale;
                    teleportFlash = frame.FlashAlpha;
                    teleportTime = frame.ElapsedSeconds;
                }
            }

            Assert.That(phases, Is.EqualTo(new[]
            {
                EscapeWormholePhase.SuckIn,
                EscapeWormholePhase.Void,
                EscapeWormholePhase.Emerge,
                EscapeWormholePhase.Completed
            }), "카메라 이동 단계가 없다.");
            Assert.That(teleports, Is.EqualTo(1), "좌표 변경은 한 번만.");
            Assert.That(teleportTime, Is.EqualTo(timeline.TeleportTime).Within(0.02f));
            Assert.That(teleportBlack, Is.GreaterThanOrEqualTo(0.999f), "완전히 검은 화면에서 옮긴다.");
            Assert.That(teleportWormhole, Is.LessThan(0.05f), "웜홀이 한 점으로 모인 순간.");
            Assert.That(teleportPlayer, Is.EqualTo(0f).Within(0.0001f), "캐릭터는 보이지 않는다.");
            Assert.That(teleportFlash, Is.GreaterThan(0.9f), "번쩍이는 순간 옮긴다.");
            Assert.That(timeline.TeleportCount, Is.EqualTo(1));
            timeline.Advance(1f);
            Assert.That(timeline.TeleportCount, Is.EqualTo(1), "끝난 뒤에는 다시 옮기지 않는다.");
        }

        [Test]
        public void Wormhole_ScreenGoesBlackWhileSuckingIn_StaysBlackInTheVoid_AndClearsOnEmerge()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            var previous = 0f;
            for (var t = 0f; t < timeline.SuckInDuration; t += 0.01f)
            {
                var alpha = timeline.Evaluate(t).BlackoutAlpha;
                Assert.That(alpha, Is.GreaterThanOrEqualTo(previous - 0.0001f), "어두워지는 동안 밝아지지 않는다.");
                previous = alpha;
            }

            Assert.That(previous, Is.GreaterThan(0.95f), "빨려 드는 구간이 끝날 때 거의 검다.");
            for (var t = timeline.SuckInDuration + 0.001f; t < timeline.TeleportTime - 0.001f; t += 0.01f)
            {
                var frame = timeline.Evaluate(t);
                Assert.That(frame.Phase, Is.EqualTo(EscapeWormholePhase.Void));
                Assert.That(frame.BlackoutAlpha, Is.EqualTo(1f), "암전 구간은 완전히 검다.");
            }

            previous = 1f;
            for (var t = timeline.TeleportTime; t < timeline.TotalDuration; t += 0.01f)
            {
                var alpha = timeline.Evaluate(t).BlackoutAlpha;
                Assert.That(alpha, Is.LessThanOrEqualTo(previous + 0.0001f), "걷히는 동안 다시 어두워지지 않는다.");
                previous = alpha;
            }

            Assert.That(timeline.Evaluate(timeline.TotalDuration).BlackoutAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void Wormhole_PlayerShrinksAndSpinsIntoTheHole_ThenGrowsOutOfTheDestinationHole()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            var open = timeline.Evaluate(timeline.SuckInDuration * 0.1f);
            Assert.That(open.WormholeScale, Is.GreaterThan(0.3f), "웜홀이 먼저 열린다.");
            Assert.That(open.PlayerScale, Is.EqualTo(1f).Within(0.0001f), "아직 빨려 들기 전.");

            var middle = timeline.Evaluate(timeline.SuckInDuration * 0.6f);
            Assert.That(middle.PlayerScale, Is.InRange(0.01f, 0.99f), "줄어드는 중");
            Assert.That(middle.PlayerSpin, Is.GreaterThan(1f), "돌면서 빨려 든다.");

            var end = timeline.Evaluate(timeline.SuckInDuration - 0.001f);
            Assert.That(end.PlayerScale, Is.LessThan(0.05f), "빨려 드는 구간이 끝나면 거의 사라졌다.");
            for (var t = timeline.SuckInDuration; t < timeline.TeleportTime; t += 0.02f)
            {
                Assert.That(timeline.Evaluate(t).PlayerScale, Is.EqualTo(0f), "암전 중에는 캐릭터가 없다.");
            }

            var arrivalOpen = timeline.Evaluate(timeline.TeleportTime + (timeline.EmergeDuration * 0.3f));
            Assert.That(arrivalOpen.WormholeScale, Is.GreaterThan(0.5f), "목적지에서 웜홀이 열린다.");
            var arrivalMiddle = timeline.Evaluate(timeline.TeleportTime + (timeline.EmergeDuration * 0.6f));
            Assert.That(arrivalMiddle.PlayerScale, Is.InRange(0.01f, 0.99f), "커지며 나온다.");
            Assert.That(arrivalMiddle.PlayerSpin, Is.LessThan(-0.5f), "반대 방향에서 풀리며 나온다.");
            var done = timeline.Evaluate(timeline.TotalDuration);
            Assert.That(done.WormholeScale, Is.EqualTo(0f).Within(0.0001f), "웜홀은 닫힌다.");
            Assert.That(done.PlayerScale, Is.EqualTo(1f));
            Assert.That(done.PlayerSpin, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Wormhole_GrowsInTheVoidThenCollapsesToAPoint_WithSuctionAndAFlash()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            var peakScale = 0f;
            var peakSuction = 0f;
            var lastScale = -1f;
            for (var t = timeline.SuckInDuration; t < timeline.TeleportTime; t += 0.005f)
            {
                var frame = timeline.Evaluate(t);
                peakScale = Mathf.Max(peakScale, frame.WormholeScale);
                peakSuction = Mathf.Max(peakSuction, frame.SuctionStrength);
                lastScale = frame.WormholeScale;
            }

            Assert.That(peakScale, Is.GreaterThan(EscapeWormholeWarpTimeline.SuckInEndScale), "암전 동안 웜홀이 더 커진다.");
            Assert.That(peakSuction, Is.GreaterThan(0.9f), "빨아들이는 줄기가 가장 세진다.");
            Assert.That(lastScale, Is.LessThan(0.1f), "끝에서 한 점으로 모인다.");

            Assert.That(timeline.Evaluate(0f).SuctionStrength, Is.EqualTo(0f), "시작에는 줄기가 없다.");
            Assert.That(timeline.Evaluate(timeline.TotalDuration).SuctionStrength, Is.EqualTo(0f));
            Assert.That(timeline.Evaluate(timeline.TeleportTime + 0.3f).SuctionStrength, Is.EqualTo(0f), "나올 때는 줄기가 없다.");

            for (var t = 0f; t < timeline.TotalDuration; t += 0.01f)
            {
                var frame = timeline.Evaluate(t);
                if (frame.FlashAlpha > 0.001f)
                {
                    var nearTeleport = Mathf.Abs(t - timeline.TeleportTime);
                    Assert.That(nearTeleport, Is.LessThan(0.25f), "번쩍임은 옮기는 순간 근처에서만.");
                }
            }
        }

        [Test]
        public void Wormhole_SpinUsesThePortalWindowSpinSpeed_AndHonorsInspectorDurations()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            Assert.That(
                timeline.Evaluate(0.2f).WormholeAngle,
                Is.EqualTo(EscapePortalOpenTimeline.SpinSpeedMax * EscapeWormholeWarpTimeline.SpinMultiplier * 0.2f).Within(0.01f),
                "창 열기 웜홀의 회전 속도를 기준으로 한다.");

            Assert.That(timeline.Advance(10f).ShouldTeleport, Is.True);
            Assert.That(timeline.Advance(10f).ShouldTeleport, Is.False);

            var custom = new EscapeWormholeWarpTimeline(new EscapeWarpSettings
            {
                suckInDuration = 0.8f,
                voidDuration = 0.5f,
                emergeDuration = 0.6f
            });
            Assert.That(custom.TeleportTime, Is.EqualTo(1.3f).Within(0.0001f));
            Assert.That(custom.TotalDuration, Is.EqualTo(1.9f).Within(0.0001f));
        }

        // ---------------------------------------------------------------- 줄기 그래픽

        [Test]
        public void SuctionStreaks_FlyInwardOnly_FromTheOuterRingToTheHoleEdge()
        {
            const float outer = 800f;
            const float hole = 90f;
            Assert.That(EscapeWormholeSuctionGraphic.RadiusAt(0f, outer, hole), Is.EqualTo(outer).Within(0.01f));
            Assert.That(EscapeWormholeSuctionGraphic.RadiusAt(1f, outer, hole), Is.EqualTo(hole).Within(0.01f));
            var previous = float.MaxValue;
            for (var life = 0f; life <= 1f; life += 0.05f)
            {
                var radius = EscapeWormholeSuctionGraphic.RadiusAt(life, outer, hole);
                Assert.That(radius, Is.LessThanOrEqualTo(previous + 0.0001f), "바깥에서 안으로만 움직인다.");
                previous = radius;
            }
        }

        // ---------------------------------------------------------------- 웜홀 오버레이

        [Test]
        public void WormholeOverlay_BlackFullscreenWithWormholeSuctionPlayerAndFlash_ReusingTheWindowArt()
        {
            var timeline = new EscapeWormholeWarpTimeline();
            var overlay = new EscapeWormholeWarpOverlay();
            try
            {
                var sprite = NewSprite();
                overlay.SetPlayerProxy(new[]
                {
                    new EscapeWarpProxySprite { Sprite = sprite, Size = new Vector2(60f, 90f), Pivot = new Vector2(0.5f, 0.2f) }
                });
                Assert.That(overlay.ProxyCount, Is.EqualTo(1));

                overlay.Apply(timeline.Evaluate(0f), new Vector2(500f, 300f), 150f, 0.016f);
                Assert.That(overlay.BlackoutAlpha, Is.EqualTo(0f), "시작에는 투명.");
                Assert.That(overlay.PlayerVisible, Is.True, "시작부터 캐릭터 복제가 이어받는다.");
                Assert.That(overlay.PlayerScale, Is.EqualTo(1f));

                overlay.Apply(timeline.Evaluate(timeline.SuckInDuration * 0.7f), new Vector2(500f, 300f), 150f, 0.016f);
                Assert.That(overlay.BlackoutAlpha, Is.GreaterThan(0.4f), "화면이 어두워진다.");
                Assert.That(overlay.WormholeVisible, Is.True);
                Assert.That(overlay.SuctionActive, Is.True, "빨아들이는 줄기가 보인다.");
                Assert.That(overlay.PlayerScale, Is.InRange(0.01f, 0.99f), "캐릭터가 웜홀로 줄어든다.");

                overlay.Apply(timeline.Evaluate(timeline.SuckInDuration + 0.2f), new Vector2(500f, 300f), 150f, 0.016f);
                Assert.That(overlay.BlackoutAlpha, Is.EqualTo(1f), "암전 구간은 완전히 검다.");
                Assert.That(overlay.CurrentScale, Is.GreaterThan(1.5f), "웜홀이 커져 있다.");
                Assert.That(overlay.PlayerVisible, Is.False);

                overlay.Apply(timeline.Evaluate(timeline.TeleportTime), new Vector2(500f, 300f), 150f, 0.016f);
                Assert.That(overlay.FlashAlpha, Is.GreaterThan(0.5f), "한 점으로 모일 때 번쩍인다.");
                Assert.That(overlay.WormholeVisible, Is.False);

                overlay.Apply(timeline.Evaluate(timeline.TotalDuration), Vector2.zero, 150f, 0.016f);
                Assert.That(overlay.BlackoutAlpha, Is.EqualTo(0f));
                Assert.That(overlay.WormholeVisible, Is.False);
                Assert.That(overlay.SuctionActive, Is.False);

                var images = overlay.Root.GetComponentsInChildren<Image>(true);
                var sprites = new List<Sprite>();
                var blockers = 0;
                foreach (var image in images)
                {
                    sprites.Add(image.sprite);
                    if (image.raycastTarget)
                    {
                        blockers++;
                    }
                }

                Assert.That(blockers, Is.EqualTo(1), "클릭을 막는 이미지는 검은 전체화면 하나.");
                Assert.That(sprites, Does.Contain(EscapeWormholeArt.Swirl(0)));
                Assert.That(sprites, Does.Contain(EscapeWormholeArt.Swirl(1)));
                Assert.That(sprites, Does.Contain(EscapeWormholeArt.Core()));
                Assert.That(overlay.Root.GetComponentInChildren<EscapeWormholeParticleGraphic>(true), Is.Not.Null,
                    "창의 웜홀 입자 그래픽을 재사용한다.");
                Assert.That(overlay.Root.GetComponentInChildren<EscapeWormholeSuctionGraphic>(true), Is.Not.Null);
                Assert.That(overlay.Root.transform.Find("Glitch"), Is.Null, "글리치 레이어가 없다.");
            }
            finally
            {
                overlay.Destroy();
            }
        }

        // ---------------------------------------------------------------- 진행기: 스타일별 연출, 카메라 고정, 검은 화면에서 이동

        [Test]
        public void Runner_Wormhole_BlackFullscreenWormhole_NoGlitch_CameraStaysPut_ArrivesWhileBlack()
        {
            var env = NewEnvironment(out var follow, out var player, out var target);
            var observed = new Observed(follow, player);
            var runner = new EscapeWarpRunner(
                new EscapeWarpSettings(), EscapeWarpStyle.Wormhole, follow, player,
                observed.Arrive, reduceMotionOverride: false);

            Drive(runner, observed);

            Assert.That(observed.ArriveCount, Is.EqualTo(1), "좌표 변경은 한 번만.");
            Assert.That(observed.SawWormhole, Is.True, "웜홀이 나온다.");
            Assert.That(observed.SawSuction, Is.True, "빨려 드는 줄기가 나온다.");
            Assert.That(observed.SawProxyPlayer, Is.True, "캐릭터 복제가 빨려 든다.");
            Assert.That(observed.SawGlitchOverlay, Is.False, "포탈에는 글리치가 없다.");
            Assert.That(observed.BlackAtArrive, Is.GreaterThanOrEqualTo(0.999f), "완전히 검은 화면에서 옮긴다.");
            Assert.That(observed.CameraMovedBeforeArrive, Is.False, "도착 전에는 카메라가 움직이지 않는다.");
            Assert.That(observed.RealPlayerHiddenSeen, Is.True, "월드의 캐릭터는 복제로 대체돼 가려진다.");
            Assert.That(observed.PlayerAtArrive, Is.EqualTo(target));
            Assert.That(follow.transform.position.x, Is.EqualTo(follow.FollowOffset.x + target.x).Within(0.001f), "카메라는 즉시 도착지를 비춘다.");
            Assert.That(follow.transform.position.y, Is.EqualTo(follow.FollowOffset.y + target.y).Within(0.001f));
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(player.GetComponentInChildren<SpriteRenderer>().enabled, Is.True, "끝나면 캐릭터 모습이 복구된다.");
            Assert.That(GameObject.Find("EscapeWormholeWarpOverlay"), Is.Null, "오버레이는 끝나면 사라진다.");
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void Runner_Glitch_BlackoutAndGlitch_NoCameraMove_ArrivesWhileBlackAndLightsUpAtTheDestination()
        {
            var env = NewEnvironment(out var follow, out var player, out var target);
            var observed = new Observed(follow, player);
            var runner = new EscapeWarpRunner(
                new EscapeWarpSettings(), EscapeWarpStyle.Glitch, follow, player,
                observed.Arrive, reduceMotionOverride: false);

            Drive(runner, observed);

            Assert.That(observed.ArriveCount, Is.EqualTo(1));
            Assert.That(observed.SawGlitchOverlay, Is.True, "비상 탈출은 암전 + 글리치가 나온다.");
            Assert.That(observed.SawGlitchActive, Is.True);
            Assert.That(observed.SawWormhole, Is.False, "비상 탈출에는 웜홀이 없다.");
            Assert.That(observed.BlackAtArrive, Is.GreaterThanOrEqualTo(0.999f), "화면이 완전히 꺼졌을 때 옮긴다.");
            Assert.That(observed.CameraMovedBeforeArrive, Is.False, "카메라 이동이 없다.");
            Assert.That(observed.PlayerAtArrive, Is.EqualTo(target));
            Assert.That(follow.transform.position.x, Is.EqualTo(follow.FollowOffset.x + target.x).Within(0.001f),
                "켜질 때 카메라가 이미 엘리베이터를 비춘다(기존 위치를 비추다 이동하지 않는다).");
            Assert.That(observed.BlackAfterArrive, Is.GreaterThanOrEqualTo(0.9f), "도착한 직후에도 아직 검다.");
            Assert.That(observed.RealPlayerHiddenSeen, Is.False, "글리치 흐름은 캐릭터 모습을 건드리지 않는다.");
            Assert.That(GameObject.Find("EscapeWarpOverlay"), Is.Null);
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void Runner_ReduceMotion_BothStylesOnlyFadeAndSnap()
        {
            foreach (var style in new[] { EscapeWarpStyle.Glitch, EscapeWarpStyle.Wormhole })
            {
                var env = NewEnvironment(out var follow, out var player, out var target);
                var observed = new Observed(follow, player);
                var runner = new EscapeWarpRunner(
                    new EscapeWarpSettings(), style, follow, player,
                    observed.Arrive, reduceMotionOverride: true);

                Drive(runner, observed);

                Assert.That(observed.ArriveCount, Is.EqualTo(1), style + ": 한 번만 옮긴다.");
                Assert.That(observed.SawWormhole, Is.False, style + ": 웜홀 빨림 없음");
                Assert.That(observed.SawGlitchActive, Is.False, style + ": 글리치 없음");
                Assert.That(observed.CameraMovedBeforeArrive, Is.False, style + ": 카메라 이동 없음");
                Assert.That(observed.BlackAtArrive, Is.GreaterThanOrEqualTo(0.99f), style + ": 검은 화면에서 옮긴다.");
                Assert.That(observed.PlayerAtArrive, Is.EqualTo(target));
                Assert.That(env, Is.Not.Null);
                TearDown();
            }
        }

        [Test]
        public void Runner_Abort_ArrivesOnceAndCleansUp()
        {
            var env = NewEnvironment(out var follow, out var player, out _);
            var observed = new Observed(follow, player);
            var runner = new EscapeWarpRunner(
                new EscapeWarpSettings(), EscapeWarpStyle.Wormhole, follow, player,
                observed.Arrive, reduceMotionOverride: false);
            var run = runner.Run();
            for (var i = 0; i < 5; i++)
            {
                run.MoveNext();
            }

            Assert.That(GameObject.Find("EscapeWormholeWarpOverlay"), Is.Not.Null);
            Assert.That(player.GetComponentInChildren<SpriteRenderer>().enabled, Is.False, "연출 중에는 복제로 대체.");
            runner.Abort();
            runner.Abort();
            Assert.That(observed.ArriveCount, Is.EqualTo(1), "중단돼도 낸 비용만큼 도착 처리는 한 번.");
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(GameObject.Find("EscapeWormholeWarpOverlay"), Is.Null);
            Assert.That(player.GetComponentInChildren<SpriteRenderer>().enabled, Is.True, "캐릭터 모습이 복구된다.");
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void PlayerProxy_CapturesTheSpriteSizeAndPivotOnScreen_AndRestoresTheRenderer()
        {
            var cameraObject = Spawn("B1951_ProxyCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var player = Spawn("B1951_ProxyPlayer");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localScale = new Vector3(-1f, 1f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = NewSprite();
            renderer.color = new Color(1f, 0.5f, 0.5f, 1f);

            var proxy = new EscapeWarpPlayerProxy(player.transform, camera);
            Assert.That(proxy.Sprites.Count, Is.EqualTo(1));
            var captured = proxy.Sprites[0];
            Assert.That(captured.Sprite, Is.SameAs(renderer.sprite));
            Assert.That(captured.FlipX, Is.True, "PlayerFacing이 뒤집은 방향을 따른다.");
            Assert.That(captured.Tint.g, Is.EqualTo(0.5f).Within(0.001f));
            var expectedPixelsPerUnit = camera.WorldToScreenPoint(Vector3.right).x - camera.WorldToScreenPoint(Vector3.zero).x;
            var spriteUnits = renderer.sprite.rect.size / renderer.sprite.pixelsPerUnit;
            Assert.That(captured.Size.x, Is.EqualTo(spriteUnits.x * expectedPixelsPerUnit).Within(0.5f));
            Assert.That(captured.Size.y, Is.EqualTo(spriteUnits.y * expectedPixelsPerUnit).Within(0.5f));

            proxy.HideRenderers();
            Assert.That(renderer.enabled, Is.False);
            proxy.Restore();
            Assert.That(renderer.enabled, Is.True);
            proxy.Restore();
            Assert.That(renderer.enabled, Is.True);
        }

        // ---------------------------------------------------------------- 연결: 구출=글리치, 포탈=웜홀, 카메라 이동 없음

        [Test]
        public void Wiring_RescueUsesGlitchAfterPayment_PortalUsesWormholeWithoutGlitch()
        {
            var rescue = Read("Integration", "EmergencyRescueRuntimeController.cs");
            var tryRescue = rescue.Substring(rescue.IndexOf("private void TryRescue()", System.StringComparison.Ordinal));
            tryRescue = tryRescue.Substring(0, tryRescue.IndexOf("private void BeginRescueWarp()", System.StringComparison.Ordinal));
            Assert.That(rescue, Does.Contain("EscapeWarpStyle.Glitch"));
            Assert.That(rescue, Does.Not.Contain("EscapeWarpStyle.Wormhole"));
            Assert.That(tryRescue, Does.Contain("service.TryRescue("));
            Assert.That(tryRescue, Does.Not.Contain("MoveToResolvedElevator"), "이동은 연출 중 화면이 꺼졌을 때 한다.");
            Assert.That(tryRescue.IndexOf("service.TryRescue(", System.StringComparison.Ordinal),
                Is.LessThan(tryRescue.IndexOf("BeginRescueWarp();", System.StringComparison.Ordinal)), "결제가 먼저.");
            Assert.That(tryRescue.IndexOf("HideAll();", System.StringComparison.Ordinal),
                Is.LessThan(tryRescue.IndexOf("BeginRescueWarp();", System.StringComparison.Ordinal)),
                "팝업은 전환이 시작될 때 치운다.");
            Assert.That(rescue, Does.Not.Contain("RunFailureRuntimeController"), "사망 실패 흐름은 호출하지 않는다.");
            Assert.That(rescue, Does.Not.Contain("ResolveFailure"));
            Assert.That(CountOf(rescue, "service.TryRescue("), Is.EqualTo(1), "결제는 한 번.");

            var portal = Read("Integration", "EmergencyEscapePortalRuntimeBridge.cs");
            Assert.That(portal, Does.Contain("EscapeWarpStyle.Wormhole"));
            Assert.That(portal, Does.Not.Contain("EscapeWarpStyle.Glitch"));
            Assert.That(portal, Does.Not.Contain("EscapeWarpOverlay"), "포탈에는 글리치 오버레이가 없다.");
            Assert.That(CountOf(portal, "service.TrySpend("), Is.EqualTo(1), "결제는 한 번.");
            Assert.That(portal, Does.Not.Contain("ResolveFailure"));
        }

        [Test]
        public void Wiring_NeitherFlowMovesTheCamera_AndNoNewSfx()
        {
            foreach (var file in new[] { "EscapeWarpRunner.cs", "EmergencyEscapePortalRuntimeBridge.cs", "EmergencyRescueRuntimeController.cs" })
            {
                var source = Read("Integration", file);
                Assert.That(source, Does.Not.Contain("Cinematic"), file + ": 카메라 연출 이동 없음");
            }

            var runner = Read("Integration", "EscapeWarpRunner.cs");
            Assert.That(runner, Does.Not.Contain("PreviewWorldPosition"), "카메라를 목적지로 보내지 않는다.");
            Assert.That(runner, Does.Not.Contain("SfxPlayer"), "진행기는 소리를 내지 않는다. 기존 PortalWarp만 쓴다.");
            Assert.That(runner, Does.Contain("SnapToTarget()"), "도착 뒤 카메라를 즉시 맞춘다.");
            var cameraSource = File.ReadAllText(Path.Combine(
                Application.dataPath, "_Project", "Scripts", "Gameplay", "Player", "PlayerCameraFollow.cs"));
            Assert.That(cameraSource, Does.Not.Contain("BeginCinematic"), "쓰지 않는 카메라 연출 API는 두지 않는다.");

            var ui = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "EmergencyEscape");
            Assert.That(File.ReadAllText(Path.Combine(ui, "EscapeWormholeWarpOverlay.cs")), Does.Not.Contain("SfxPlayer"));
            Assert.That(File.ReadAllText(Path.Combine(ui, "EscapeWormholeSuctionGraphic.cs")), Does.Not.Contain("SfxPlayer"));
        }

        // ---------------------------------------------------------------- 도우미

        private sealed class Observed
        {
            private readonly PlayerCameraFollow follow;
            private readonly Transform player;
            public int ArriveCount;
            public bool SawWormhole;
            public bool SawSuction;
            public bool SawProxyPlayer;
            public bool SawGlitchOverlay;
            public bool SawGlitchActive;
            public bool RealPlayerHiddenSeen;
            public bool CameraMovedBeforeArrive;
            public float BlackAtArrive;
            public float BlackAfterArrive = -1f;
            public Vector3 PlayerAtArrive;
            public Vector3 CameraStart;

            public Observed(PlayerCameraFollow follow, Transform player)
            {
                this.follow = follow;
                this.player = player;
                CameraStart = follow.transform.position;
            }

            public void Arrive()
            {
                ArriveCount++;
                CameraMovedBeforeArrive |= (follow.transform.position - CameraStart).sqrMagnitude > 0.000001f;
                BlackAtArrive = CurrentBlack();
                var targetMarker = GameObject.Find("B1951_Target");
                player.position = targetMarker.transform.position;
                PlayerAtArrive = player.position;
            }

            public float CurrentBlack()
            {
                var wormhole = GameObject.Find("EscapeWormholeWarpOverlay");
                if (wormhole != null)
                {
                    return wormhole.transform.Find("Black").GetComponent<Image>().color.a;
                }

                var glitch = GameObject.Find("EscapeWarpOverlay");
                return glitch != null ? glitch.transform.Find("Black").GetComponent<Image>().color.a : 0f;
            }

            public void Observe()
            {
                var wormhole = GameObject.Find("EscapeWormholeWarpOverlay");
                if (wormhole != null)
                {
                    SawWormhole |= wormhole.transform.Find("Wormhole").gameObject.activeSelf;
                    SawSuction |= wormhole.transform.Find("Suction").gameObject.activeSelf;
                    SawProxyPlayer |= wormhole.transform.Find("Player").gameObject.activeSelf;
                }

                var glitch = GameObject.Find("EscapeWarpOverlay");
                if (glitch != null)
                {
                    SawGlitchOverlay = true;
                    var glitchLayer = glitch.transform.Find("Glitch");
                    SawGlitchActive |= glitchLayer != null && glitchLayer.gameObject.activeSelf;
                }

                var renderer = player.GetComponentInChildren<SpriteRenderer>();
                RealPlayerHiddenSeen |= renderer != null && !renderer.enabled;
                if (ArriveCount == 0)
                {
                    CameraMovedBeforeArrive |= (follow.transform.position - CameraStart).sqrMagnitude > 0.000001f;
                }
                else if (BlackAfterArrive < 0f)
                {
                    BlackAfterArrive = CurrentBlack();
                }
            }
        }

        private static void Drive(EscapeWarpRunner runner, Observed observed)
        {
            var run = runner.Run();
            var guard = 0;
            while (run.MoveNext())
            {
                guard++;
                Assert.That(guard, Is.LessThan(20000), "연출이 끝나지 않는다.");
                observed.Observe();
            }

            runner.Abort();
        }

        private GameObject NewEnvironment(out PlayerCameraFollow follow, out Transform player, out Vector3 target)
        {
            var cameraObject = Spawn("B1951_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            follow = cameraObject.AddComponent<PlayerCameraFollow>();

            var playerObject = Spawn("B1951_Player");
            playerObject.transform.position = new Vector3(2f, 1f, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(playerObject.transform, false);
            visual.AddComponent<SpriteRenderer>().sprite = NewSprite();
            player = playerObject.transform;
            follow.SetTarget(player, snapImmediately: true);

            var targetObject = Spawn("B1951_Target");
            targetObject.transform.position = new Vector3(40f, -12f, 0f);
            target = targetObject.transform.position;
            return playerObject;
        }

        private Sprite NewSprite()
        {
            var texture = new Texture2D(16, 24, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[16 * 24];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 24f), new Vector2(0.5f, 0.2f), 16f);
            assets.Add(texture);
            assets.Add(sprite);
            return sprite;
        }

        private GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        private static void DestroyIfExists(string name)
        {
            var found = GameObject.Find(name);
            while (found != null)
            {
                Object.DestroyImmediate(found);
                found = GameObject.Find(name);
            }
        }

        private static string Read(params string[] relative)
        {
            var parts = new List<string> { Application.dataPath, "_Project", "Scripts", "App" };
            parts.AddRange(relative);
            return File.ReadAllText(Path.Combine(parts.ToArray()));
        }

        private static int CountOf(string text, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }
    }
}
