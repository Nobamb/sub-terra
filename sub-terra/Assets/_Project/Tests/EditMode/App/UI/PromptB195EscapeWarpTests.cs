using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.RunFailure;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 195 / 195-1 / 195-2: 비상 탈출(전력 고갈 구출)의 암전 + 간단 글리치 시간표(카메라 이동 없음, 검은 화면에서 이동),
    /// 브리지 결제·이동 순서, 포탈 창 안쪽 불투명 채움.
    /// </summary>
    public sealed class PromptB195EscapeWarpTests
    {
        private const string PanelPrefabPath = "Assets/_Project/Prefabs/UI/EmergencyEscapePanel.prefab";
        private const string FramePngPath = "Assets/_Project/Art/UI/SurfaceBase/MineReset/mine-reset-frame.png";
        private readonly List<GameObject> spawned = new List<GameObject>();

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
        }

        // ---------------------------------------------------------------- 시간표

        [Test]
        public void Timeline_DefaultDurations_AreShorterThanTheDeathTransition()
        {
            var warp = new EscapeWarpTimeline();
            var death = new RunFailureTransitionTimeline();
            Assert.That(warp.BlackoutDuration, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(warp.GlitchDuration, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(warp.HoldDuration, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(warp.ScreenOnDuration, Is.EqualTo(0.30f).Within(0.0001f));
            Assert.That(warp.TotalDuration, Is.LessThan(death.TotalDuration), "사망 기본(0.4+0.6+0.4)보다 짧다.");
            Assert.That(warp.GlitchDuration, Is.LessThan(death.HeavyGlitchDuration), "사망의 강한 글리치보다 짧다.");
            Assert.That(EscapeWarpTimeline.MaxGlitchIntensity, Is.LessThan(1f), "사망 글리치(최대 1)보다 약하다.");
        }

        [Test]
        public void Timeline_RunsBlackoutThenScreenOn_AndTeleportsOnceWhileFullyBlack()
        {
            var timeline = new EscapeWarpTimeline();
            var phases = new List<EscapeWarpPhase>();
            var teleports = 0;
            var teleportAlpha = -1f;
            var teleportTime = -1f;
            var sawBlack = false;
            var sawGlitch = false;

            while (!timeline.IsComplete)
            {
                var frame = timeline.Advance(1f / 60f);
                if (phases.Count == 0 || phases[phases.Count - 1] != frame.Phase)
                {
                    phases.Add(frame.Phase);
                }

                sawBlack |= frame.BlackoutAlpha >= 0.999f;
                sawGlitch |= frame.GlitchIntensity > 0.01f;
                if (frame.ShouldTeleport)
                {
                    teleports++;
                    teleportAlpha = frame.BlackoutAlpha;
                    teleportTime = frame.ElapsedSeconds;
                }
            }

            Assert.That(phases, Is.EqualTo(new[]
            {
                EscapeWarpPhase.Blackout,
                EscapeWarpPhase.ScreenOn,
                EscapeWarpPhase.Completed
            }), "카메라 이동 단계가 없다.");
            Assert.That(sawBlack, Is.True, "화면이 완전히 검어진다.");
            Assert.That(sawGlitch, Is.True, "간단 글리치가 난다.");
            Assert.That(teleports, Is.EqualTo(1), "좌표 변경은 한 번만.");
            Assert.That(teleportAlpha, Is.GreaterThanOrEqualTo(0.999f), "화면이 완전히 꺼진 채 옮긴다.");
            Assert.That(teleportTime, Is.EqualTo(timeline.TeleportTime).Within(0.02f));
            Assert.That(timeline.TeleportTime, Is.EqualTo(timeline.BlackoutPhaseDuration).Within(0.0001f),
                "켜지기 시작하는 순간 이미 도착해 있다.");
            Assert.That(timeline.TotalDuration, Is.EqualTo(timeline.TeleportTime + timeline.ScreenOnDuration).Within(0.0001f));
            timeline.Advance(1f);
            Assert.That(timeline.TeleportCount, Is.EqualTo(1), "끝난 뒤에는 다시 옮기지 않는다.");
        }

        [Test]
        public void Timeline_LargeStep_StillTeleportsOnce()
        {
            var timeline = new EscapeWarpTimeline();
            var frame = timeline.Advance(10f);
            Assert.That(frame.ShouldTeleport, Is.True);
            Assert.That(frame.IsComplete, Is.True);
            Assert.That(timeline.Advance(10f).ShouldTeleport, Is.False);
            Assert.That(timeline.TeleportCount, Is.EqualTo(1));
        }

        [Test]
        public void Timeline_GlitchRunsWhileDarkeningAndFadesOutOnArrival_InTheColdColor()
        {
            var settings = new EscapeWarpSettings();
            var warp = new EscapeWarpTimeline(settings);
            var arrivalGlitchSeen = false;
            for (var t = 0f; t < warp.TotalDuration; t += 0.01f)
            {
                var frame = warp.Evaluate(t);
                if (frame.GlitchIntensity <= 0f)
                {
                    continue;
                }

                if (frame.Phase == EscapeWarpPhase.Blackout)
                {
                    Assert.That(t, Is.LessThan(warp.GlitchDuration), "어두워지는 동안만 센 글리치.");
                }
                else
                {
                    Assert.That(frame.Phase, Is.EqualTo(EscapeWarpPhase.ScreenOn), "끝난 뒤에는 글리치가 없다.");
                    Assert.That(frame.GlitchIntensity,
                        Is.LessThanOrEqualTo(EscapeWarpTimeline.MaxGlitchIntensity * EscapeWarpTimeline.ArrivalGlitchRatio + 0.0001f),
                        "도착 글리치는 약하게.");
                    arrivalGlitchSeen = true;
                }
            }

            Assert.That(arrivalGlitchSeen, Is.True, "켜지는 순간 잠깐 남는 글리치가 있다.");
            Assert.That(warp.Evaluate(warp.TotalDuration).GlitchIntensity, Is.EqualTo(0f));
            var death = RunFailureRevealTimeline.ResolveGlitchColor(RunFailureCause.Unknown);
            Assert.That(settings.glitchColor.b, Is.GreaterThan(settings.glitchColor.r), "청록 계열");
            Assert.That(settings.glitchColor.r, Is.LessThan(death.r), "사망용 적색과 다르다.");
        }

        [Test]
        public void Timeline_ReduceMotion_FadesWithoutGlitch_AndTeleportsWhileBlack()
        {
            var timeline = new EscapeWarpTimeline(null, reduceMotion: true);
            Assert.That(timeline.GlitchDuration, Is.EqualTo(0f));

            var teleports = 0;
            var teleportAlpha = -1f;
            var teleportTime = -1f;
            while (!timeline.IsComplete)
            {
                var frame = timeline.Advance(1f / 60f);
                Assert.That(frame.GlitchIntensity, Is.EqualTo(0f));
                if (frame.ShouldTeleport)
                {
                    teleports++;
                    teleportAlpha = frame.BlackoutAlpha;
                    teleportTime = frame.ElapsedSeconds;
                }
            }

            Assert.That(teleports, Is.EqualTo(1));
            Assert.That(teleportAlpha, Is.GreaterThanOrEqualTo(0.99f), "화면이 완전히 꺼진 채 옮긴다.");
            Assert.That(teleportTime, Is.EqualTo(timeline.BlackoutPhaseDuration).Within(0.02f));
            Assert.That(timeline.Evaluate(timeline.TotalDuration).BlackoutAlpha, Is.EqualTo(0f), "페이드 인으로 끝난다.");
        }

        [Test]
        public void Timeline_HonorsInspectorDurations()
        {
            var settings = new EscapeWarpSettings
            {
                blackoutDuration = 0.5f,
                glitchDuration = 0.1f,
                blackHoldDuration = 0.2f,
                screenOnDuration = 0.4f
            };
            var timeline = new EscapeWarpTimeline(settings);
            Assert.That(timeline.BlackoutPhaseDuration, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(timeline.TotalDuration, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(timeline.Evaluate(0.3f).Phase, Is.EqualTo(EscapeWarpPhase.Blackout));
            Assert.That(timeline.Evaluate(0.3f).GlitchIntensity, Is.EqualTo(0f), "글리치는 0.1초 뒤에 끝난다.");
        }

        // ---------------------------------------------------------------- 브리지: 결제 즉시, 워프는 연출 뒤

        [Test]
        public void Bridge_EditMode_PaysOnceAndWarpsImmediatelyWithoutLeavingAPause()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = Spawn("B195_BridgePlayer");
            var elevator = Spawn("B195_BridgeElevator");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = Spawn("B195_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            Assert.That(bridge.TryEscapeTo(EmergencyEscapeDestination.Elevator, string.Empty, out _), Is.True);
            Assert.That(player.transform.position, Is.EqualTo(elevator.transform.position));
            Assert.That(state.Player.Gold, Is.EqualTo(200), "결제는 한 번만.");
            Assert.That(state.Player.Energy, Is.EqualTo(90));
            Assert.That(bridge.IsWarping, Is.False, "에디트 모드(테스트)에서는 연출 없이 끝 상태.");
            Assert.That(UiPauseGateProbe.IsWarpPauseHeld, Is.False);
        }

        [Test]
        public void Bridge_FailureOrMissingDestination_DoesNotStartAWarp()
        {
            var state = GameState.CreateNew();
            state.SetGold(0);
            var player = Spawn("B195_FailPlayer");
            var elevator = Spawn("B195_FailElevator");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = Spawn("B195_FailBridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);
            var before = player.transform.position;

            Assert.That(bridge.TryEscapeTo(EmergencyEscapeDestination.Elevator, string.Empty, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(player.transform.position, Is.EqualTo(before));
            Assert.That(bridge.IsWarping, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(0));

            Assert.That(
                bridge.TryEscapeTo(EmergencyEscapeDestination.OutpostCore, "missing", out _),
                Is.False);
            Assert.That(bridge.IsWarping, Is.False);
        }

        // ---------------------------------------------------------------- 포탈 창 안쪽 불투명

        [Test]
        public void FrameSilhouette_FillCoversTheWholeInsideAndNothingOutside()
        {
            var alpha = LoadFrameAlpha(out var width, out var height);
            var inside = EscapeWindowFillArt.ComputeInside(alpha, width, height);

            Assert.That(inside[(height / 2 * width) + (width / 2)], Is.True, "창 가운데(구멍)는 채운다.");
            AssertInsideAt(inside, width, height, 0.5f, 0.5f);
            AssertInsideAt(inside, width, height, 0.30f, 0.30f);
            AssertInsideAt(inside, width, height, 0.70f, 0.70f);
            AssertInsideAt(inside, width, height, 0.20f, 0.85f);
            AssertInsideAt(inside, width, height, 0.80f, 0.20f);
            AssertInsideAt(inside, width, height, 0.06f, 0.5f);

            // 모서리가 깎인 바깥(프레임 밖)은 채우지 않는다 — 예전 검은 네모가 비치던 자리.
            Assert.That(inside[0], Is.False, "왼쪽 위 바깥 모서리");
            Assert.That(inside[width - 1], Is.False, "오른쪽 위 바깥 모서리");
            Assert.That(inside[(height - 1) * width], Is.False, "왼쪽 아래 바깥 모서리");
            Assert.That(inside[(height * width) - 1], Is.False, "오른쪽 아래 바깥 모서리");

            var count = 0;
            for (var i = 0; i < inside.Length; i++)
            {
                if (inside[i])
                {
                    count++;
                }
            }

            var fraction = (float)count / inside.Length;
            Assert.That(fraction, Is.InRange(0.9f, 0.995f), "프레임 실루엣 안쪽 대부분을 채우고 바깥 모서리는 남긴다.");

            // 채운 모든 칸은 프레임 그림 안쪽이다(바깥에서 닿는 빈 칸은 하나도 채우지 않는다).
            var outsideTouchingEdge = 0;
            for (var x = 0; x < width; x++)
            {
                if (alpha[x] < EscapeWindowFillArt.EmptyAlpha && inside[x])
                {
                    outsideTouchingEdge++;
                }
            }

            Assert.That(outsideTouchingEdge, Is.Zero, "가장자리의 빈 칸은 채우지 않는다.");
        }

        [Test]
        public void FillArt_FromAlpha_IsOpaqueInsideAndTransparentOutside()
        {
            // 8x8: 가장자리 한 줄은 비어 있고, 그 안쪽은 테두리(알파 1) 한 줄 + 속이 빈 구멍.
            const int size = 8;
            var alpha = new float[size * size];
            for (var y = 1; y < size - 1; y++)
            {
                for (var x = 1; x < size - 1; x++)
                {
                    var border = x == 1 || y == 1 || x == size - 2 || y == size - 2;
                    alpha[(y * size) + x] = border ? 1f : 0f;
                }
            }

            var inside = EscapeWindowFillArt.ComputeInside(alpha, size, size);
            Assert.That(inside[(4 * size) + 4], Is.True, "구멍 안쪽");
            Assert.That(inside[(1 * size) + 1], Is.True, "테두리 자리");
            Assert.That(inside[0], Is.False);
            Assert.That(inside[(0 * size) + 4], Is.False);

            var sprite = EscapeWindowFillArt.FromAlpha(alpha, size, size);
            try
            {
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.texture.width, Is.EqualTo(size));
                Assert.That(EscapeWindowFillArt.FillColor.a, Is.EqualTo(1f), "채움 색은 불투명.");
            }
            finally
            {
                Object.DestroyImmediate(sprite.texture);
                Object.DestroyImmediate(sprite);
            }
        }

        [Test]
        public void PanelPrefab_WindowFillIsOpaqueStretchedAndWired_ButKeepsShieldFrameAndWormhole()
        {
            var contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            try
            {
                AssertWindowLayers(contents);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [Test]
        public void Builder_ReapplyKeepsTheOpaqueFillAndTheShield()
        {
            // 저장하지 않고 빌더를 한 번 더 적용해, 다시 돌려도 채움·차단막이 유지되는지 본다.
            var contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            try
            {
                PromptB147EscapePortalTerminalBuilder.ApplyTo(contents);
                AssertWindowLayers(contents);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [Test]
        public void ViewApplyArt_TurnsOnTheFrameShapedOpaqueFill()
        {
            var contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            try
            {
                var view = contents.GetComponent<EmergencyEscapePanelView>();
                Assert.That(view.WindowFill, Is.Not.Null);
                view.ApplyArt();
                var fill = view.WindowFill;
                Assert.That(fill.enabled, Is.True, "채움이 켜진다.");
                Assert.That(fill.color.a, Is.EqualTo(1f), "불투명");
                Assert.That(fill.sprite, Is.Not.Null, "프레임 실루엣 모양 그림");
                Assert.That(fill.sprite.name, Is.EqualTo("EscapeWindowFill"));
                Assert.That(fill.rectTransform.offsetMin, Is.EqualTo(Vector2.zero), "프레임과 같은 크기.");
                Assert.That(fill.rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(fill.raycastTarget, Is.False, "클릭 차단은 InputShield가 맡는다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void AssertWindowLayers(GameObject root)
        {
            var window = FindDeep(root.transform, "Window");
            Assert.That(window, Is.Not.Null);

            var panel = window.Find("Panel");
            Assert.That(panel, Is.Not.Null, "창 안쪽 채움(Panel)");
            var panelImage = panel.GetComponent<Image>();
            Assert.That(panelImage, Is.Not.Null);
            Assert.That(panelImage.color.a, Is.EqualTo(1f), "알파 1 불투명 면.");
            Assert.That(panelImage.sprite, Is.Null, "공유 mine-reset-panel.png를 쓰지 않는다.");
            var rect = (RectTransform)panel;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero), "프레임과 같은 크기(실루엣 그림이 안쪽만 칠한다).");
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(panel.GetSiblingIndex(), Is.LessThan(window.Find("Frame").GetSiblingIndex()), "프레임 뒤에 깔린다.");

            var view = root.GetComponent<EmergencyEscapePanelView>();
            var so = new SerializedObject(view);
            Assert.That(so.FindProperty("windowFill").objectReferenceValue, Is.SameAs(panelImage));
            Assert.That(so.FindProperty("frameImage").objectReferenceValue, Is.SameAs(window.Find("Frame").GetComponent<Image>()));

            var shield = window.Find("InputShield");
            Assert.That(shield, Is.Not.Null, "InputShield는 지우지 않는다.");
            var shieldImage = shield.GetComponent<Image>();
            Assert.That(shieldImage, Is.Not.Null);
            Assert.That(shieldImage.color.a, Is.EqualTo(0f), "투명해야 한다.");
            Assert.That(shieldImage.raycastTarget, Is.True, "클릭 차단은 유지한다.");
            Assert.That(shieldImage.enabled, Is.True);

            var frame = window.Find("Frame");
            var frameImage = frame.GetComponent<Image>();
            Assert.That(frameImage.enabled && frameImage.sprite != null, Is.True, "프레임은 그대로.");
            Assert.That(window.Find("HexPattern"), Is.Not.Null);
            Assert.That(window.Find("ListPanel"), Is.Not.Null);
            Assert.That(FindDeep(root.transform, "Wormhole"), Is.Not.Null, "웜홀은 그대로.");
        }

        private static float[] LoadFrameAlpha(out int width, out int height)
        {
            var full = Path.Combine(Application.dataPath, "..", FramePngPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(full)), Is.True);
                // 실제 런타임처럼 절반으로 줄여 알파를 읽는다.
                const int step = 2;
                width = texture.width / step;
                height = texture.height / step;
                var pixels = texture.GetPixels32();
                var alpha = new float[width * height];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        alpha[(y * width) + x] = pixels[(y * step * texture.width) + (x * step)].a / 255f;
                    }
                }

                return alpha;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertInsideAt(bool[] inside, int width, int height, float nx, float ny)
        {
            var x = Mathf.Clamp(Mathf.RoundToInt(nx * width), 0, width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(ny * height), 0, height - 1);
            Assert.That(inside[(y * width) + x], Is.True, "프레임 안쪽 (" + nx + "," + ny + ")");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                {
                    return all[i];
                }
            }

            return null;
        }

        private GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        private static class UiPauseGateProbe
        {
            public static bool IsWarpPauseHeld =>
                SubTerra.App.UI.UiPauseGate.IsHeldBy(EmergencyEscapePortalRuntimeBridge.WarpPauseOwner);
        }
    }
}
