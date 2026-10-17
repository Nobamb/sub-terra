using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    public sealed class EmergencyEscapePortalPlayModeTests
    {
        [UnityTest]
        public IEnumerator PromptB47_2_Portal_OpensPanelWhenNetworkBoundEvenIfCapacityUnpowered()
        {
            var networkObject = new GameObject("PromptB47_2_PowerNetwork");
            var network = networkObject.AddComponent<PowerNetworkSystem>();
            // 전진기지급 공급 5만 두고 케이블 없이 수요 30 포탈을 등록한다.
            var sourceObject = new GameObject("PromptB47_2_Source");
            sourceObject.AddComponent<PowerNode>().Configure(
                network, true, 5, 0, PowerPriority.Critical);

            var portalObject = new GameObject("PromptB47_2_Portal");
            portalObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var portalPower = portalObject.AddComponent<PowerNode>();
            portalPower.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            network.Rebuild();

            // 용량 기반 PowerNode.IsPowered 는 false여야 한다(공급 5 < 수요 30 + 미연결).
            Assert.That(portalPower.IsPowered, Is.False);

            var player = new GameObject("PromptB47_2_Rider");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            // prompt-B 147: 캐릭터 판정 영역이 포탈 영역 안에 완전히 들어가야 포탈이 열린다.
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.9f);
            var movement = player.AddComponent<PlayerMovement>();
            var portObject = new GameObject("PromptB47_2_FakePort");
            var fakePort = portObject.AddComponent<FakeEscapePort>();
            SetField(portal, "rider", movement);
            SetField(portal, "escapePort", fakePort);

            Assert.That(portalPower.Demand, Is.EqualTo(30));
            Assert.That(portal.IsPowered, Is.True, "망 등록만으로 사용 가능으로 본다.");
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));
            yield return null;

            portalPower.SetNetwork(null);
            Assert.That(portal.IsPowered, Is.False);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));

            Object.Destroy(portObject);
            Object.Destroy(player);
            Object.Destroy(portalObject);
            Object.Destroy(sourceObject);
            Object.Destroy(networkObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB47_1_Portal_RequiresRiderAndPowerNetworkBeforeOpenPanel()
        {
            var networkObject = new GameObject("PromptB47_PowerNetwork");
            var network = networkObject.AddComponent<PowerNetworkSystem>();
            var sourceObject = new GameObject("PromptB47_PowerSource");
            var source = sourceObject.AddComponent<PowerNode>();
            source.Configure(network, true, 50, 0, PowerPriority.Critical);

            var portalObject = new GameObject("PromptB47_Portal");
            portalObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var portalPower = portalObject.AddComponent<PowerNode>();
            portalPower.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            var cableObject = new GameObject("PromptB47_Cable");
            var cable = cableObject.AddComponent<PowerCable>();
            cable.Configure(network, source, portalPower);
            network.RegisterCable(cable);
            network.Rebuild();

            var player = new GameObject("PromptB47_Rider");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            // prompt-B 147: 캐릭터 판정 영역이 포탈 영역 안에 완전히 들어가야 포탈이 열린다.
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.9f);
            var movement = player.AddComponent<PlayerMovement>();
            var portObject = new GameObject("PromptB47_FakePort");
            var fakePort = portObject.AddComponent<FakeEscapePort>();
            SetField(portal, "rider", movement);
            SetField(portal, "escapePort", fakePort);

            Assert.That(portalPower.Demand, Is.EqualTo(30));
            Assert.That(portal.IsPowered, Is.True);
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));
            yield return null;

            portalPower.SetNetwork(null);
            Assert.That(portal.IsPowered, Is.False);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(fakePort.OpenCount, Is.EqualTo(1));

            Object.Destroy(portObject);
            Object.Destroy(player);
            Object.Destroy(cableObject);
            Object.Destroy(portalObject);
            Object.Destroy(sourceObject);
            Object.Destroy(networkObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB47_1_EscapeTo_SelectedDestination_PaysOnceAndTeleports()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = new GameObject("PromptB47_Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var elevator = new GameObject("PromptB47_ElevatorCenter");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = new GameObject("PromptB47_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            var panelHost = new GameObject("PromptB47_Panel", typeof(RectTransform));
            var view = panelHost.AddComponent<EmergencyEscapePanelView>();
            var binder = panelHost.AddComponent<EmergencyEscapePanelBinder>();
            var confirm = panelHost.AddComponent<Button>();
            var close = new GameObject("Close").AddComponent<Button>();
            SetField(view, "panelRoot", panelHost);
            SetField(binder, "view", view);
            SetField(binder, "confirmButton", confirm);
            SetField(binder, "closeButton", close);
            bridge.BindPanel(binder);

            var options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(options[0].Kind, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(options[0].DisplayName, Is.EqualTo("엘리베이터"));

            Assert.That(bridge.TryOpenEscapePanel(out _), Is.True);
            Assert.That(
                bridge.TryEscapeTo(EmergencyEscapeDestination.Elevator, string.Empty, out _),
                Is.True);
            Assert.That(player.transform.position, Is.EqualTo(elevator.transform.position));
            Assert.That(state.Player.Gold, Is.EqualTo(200));
            Assert.That(state.Player.Energy, Is.EqualTo(90));

            var outpost = new GameObject("PromptB47_Outpost");
            outpost.transform.position = new Vector3(12f, -10f, 0f);
            outpost.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-0002",
                DataIds.Buildings.OutpostCoreBasic);

            options = bridge.GetDestinationOptions();
            Assert.That(options.Count, Is.EqualTo(2));
            Assert.That(options[1].Kind, Is.EqualTo(EmergencyEscapeDestination.OutpostCore));
            Assert.That(
                bridge.TryEscapeTo(
                    EmergencyEscapeDestination.OutpostCore,
                    "building.outpost_core.basic-0002",
                    out _),
                Is.True);
            Assert.That(
                player.transform.position,
                Is.EqualTo(outpost.transform.position + Vector3.up));
            Assert.That(state.Player.Gold, Is.EqualTo(100));
            Assert.That(state.Player.Energy, Is.EqualTo(80));

            Object.Destroy(panelHost);
            Object.Destroy(host);
            Object.Destroy(outpost);
            Object.Destroy(elevator);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB74_EscapeTo_CompletesQuestOnCurrentRuntimeState()
        {
            GameBootstrapper.ResetInstanceForTests();

            var staleState = GameState.CreateNew();
            staleState.SetGold(300);
            var currentState = GameState.CreateNew();
            currentState.SetGold(300);
            currentState.SetDemoProgress(
                DemoObjectiveIds.EmergencyEscapeReturn,
                DemoObjectiveIds.RequiredCount - 1,
                false);

            var bootstrapObject = new GameObject("PromptB74_Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            Assert.That(bootstrap.TryReplaceState(currentState), Is.True);

            var player = new GameObject("PromptB74_Player");
            var elevator = new GameObject("PromptB74_Elevator");
            elevator.transform.position = new Vector3(5f, -2f, 0f);
            var host = new GameObject("PromptB74_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(staleState, player.transform, elevator.transform);

            Assert.That(
                bridge.TryEscapeTo(EmergencyEscapeDestination.Elevator, string.Empty, out _),
                Is.True);
            Assert.That(currentState.Progress.IsDemoComplete, Is.True);
            Assert.That(
                currentState.Progress.CompletedObjectives,
                Is.EqualTo(DemoObjectiveIds.RequiredCount));
            Assert.That(currentState.Player.Gold, Is.EqualTo(200));
            Assert.That(staleState.Player.Gold, Is.EqualTo(300));

            var outpostState = GameState.CreateNew();
            outpostState.SetGold(300);
            outpostState.SetDemoProgress(
                DemoObjectiveIds.EmergencyEscapeReturn,
                DemoObjectiveIds.RequiredCount - 1,
                false);
            Assert.That(bootstrap.TryReplaceState(outpostState), Is.True);

            var outpost = new GameObject("PromptB74_Outpost");
            outpost.AddComponent<BuildingInstance>().Initialize(
                "building.outpost_core.basic-prompt-b74",
                DataIds.Buildings.OutpostCoreBasic);

            Assert.That(
                bridge.TryEscapeTo(
                    EmergencyEscapeDestination.OutpostCore,
                    "building.outpost_core.basic-prompt-b74",
                    out _),
                Is.True);
            Assert.That(outpostState.Progress.IsDemoComplete, Is.True);
            Assert.That(
                outpostState.Progress.CompletedObjectives,
                Is.EqualTo(DemoObjectiveIds.RequiredCount));
            Assert.That(outpostState.Player.Gold, Is.EqualTo(200));

            Object.Destroy(host);
            Object.Destroy(outpost);
            Object.Destroy(elevator);
            Object.Destroy(player);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator PromptB147_PanelView_OpensThroughIntro_SkipsAndClosesEvenWhenTimeIsPaused()
        {
            var canvasObject = new GameObject("PromptB147_Canvas", typeof(Canvas));
            var root = new GameObject("PromptB147_Panel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);
            var window = new GameObject("Window", typeof(RectTransform), typeof(CanvasGroup));
            window.transform.SetParent(panelRoot.transform, false);
            var wormhole = new GameObject("Wormhole", typeof(RectTransform), typeof(CanvasGroup));
            wormhole.transform.SetParent(panelRoot.transform, false);
            var view = root.AddComponent<EmergencyEscapePanelView>();
            SetField(view, "panelRoot", panelRoot);
            SetField(view, "windowRect", window.GetComponent<RectTransform>());
            SetField(view, "windowGroup", window.GetComponent<CanvasGroup>());
            SetField(view, "wormholeRect", wormhole.GetComponent<RectTransform>());
            SetField(view, "wormholeGroup", wormhole.GetComponent<CanvasGroup>());

            // 게임 시간이 멈춰도(창이 열려 있는 동안 timeScale 0) 연출은 unscaled 시간으로 끝까지 재생된다.
            var previousScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                view.SetVisible(true);
                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Opening));
                Assert.That(window.GetComponent<CanvasGroup>().blocksRaycasts, Is.False, "연출 중에는 클릭을 막는다.");

                var waited = 0f;
                while (view.State == EmergencyEscapePanelView.PlayState.Opening && waited < 3f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Open));
                Assert.That(waited, Is.LessThan(1.2f + 0.3f), "열기 연출은 1.2초 안에 끝난다(프레임 여유 포함).");
                Assert.That(window.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(window.transform.localScale.x, Is.EqualTo(1f).Within(0.001f));

                // 건너뛰기: 열기 도중 SkipOpen을 부르면 즉시 끝 상태다.
                view.HideImmediate();
                view.SetVisible(true);
                yield return null;
                view.SkipOpen();
                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Open));
                Assert.That(window.transform.localScale.x, Is.EqualTo(1f).Within(0.001f));

                // 닫기: 블랙홀이 나타나고 창이 빨려 들어가는 연출 뒤 숨는다.
                var closed = false;
                view.BeginClose(() => closed = true);
                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Closing));
                Assert.That(view.IsOpen, Is.False);
                waited = 0f;
                while (!closed && waited < 2f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(closed, Is.True);
                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Hidden));
                Assert.That(panelRoot.activeSelf, Is.False);
            }
            finally
            {
                Time.timeScale = previousScale;
                Object.Destroy(canvasObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB147_2_Close_KeepsGameTimePausedUntilTheCloseAnimationEnds()
        {
            var canvasObject = new GameObject("PromptB147_2_Canvas", typeof(Canvas));
            var root = new GameObject("PromptB147_2_Panel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);
            var window = new GameObject("Window", typeof(RectTransform), typeof(CanvasGroup));
            window.transform.SetParent(panelRoot.transform, false);
            var wormhole = new GameObject("Wormhole", typeof(RectTransform), typeof(CanvasGroup));
            wormhole.transform.SetParent(panelRoot.transform, false);
            var view = root.AddComponent<EmergencyEscapePanelView>();
            SetField(view, "panelRoot", panelRoot);
            SetField(view, "windowRect", window.GetComponent<RectTransform>());
            SetField(view, "windowGroup", window.GetComponent<CanvasGroup>());
            SetField(view, "wormholeRect", wormhole.GetComponent<RectTransform>());
            SetField(view, "wormholeGroup", wormhole.GetComponent<CanvasGroup>());
            var binder = root.AddComponent<EmergencyEscapePanelBinder>();
            SetField(binder, "view", view);
            yield return null;
            yield return null;
            yield return null;

            var previousScale = Time.timeScale;
            Time.timeScale = 1f;
            try
            {
                var options = new List<EmergencyEscapeDestinationOption>
                {
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
                };
                binder.Open(options, new SubTerra.App.Run.EmergencyEscapeCost(100, 10));
                Assert.That(Time.timeScale, Is.EqualTo(0f), "창이 열려 있는 동안 게임이 멈춘다.");
                view.SkipOpen();

                binder.Close();
                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Closing));
                Assert.That(Time.timeScale, Is.EqualTo(0f),
                    "닫는 연출 중에도 월드(플레이어·카메라)는 움직이지 않는다.");

                var waited = 0f;
                while (view.State != EmergencyEscapePanelView.PlayState.Hidden && waited < 3f)
                {
                    Assert.That(Time.timeScale, Is.EqualTo(0f), "연출이 끝나기 전에는 풀리지 않는다.");
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Hidden));
                Assert.That(Time.timeScale, Is.EqualTo(1f), "창이 완전히 사라진 뒤에 게임이 다시 흐른다.");
            }
            finally
            {
                Time.timeScale = previousScale;
                Object.Destroy(canvasObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB147_Prewarm_BuildsCctvOnce_ThenLeavesPanelHiddenAndRenderOff()
        {
            var canvasObject = new GameObject("PromptB147_PrewarmCanvas", typeof(Canvas));
            var root = new GameObject("PromptB147_PrewarmPanel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);
            var videoObject = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
            videoObject.transform.SetParent(panelRoot.transform, false);
            ((RectTransform)videoObject.transform).sizeDelta = new Vector2(320f, 180f);
            var view = root.AddComponent<EmergencyEscapePanelView>();
            SetField(view, "panelRoot", panelRoot);
            SetField(view, "video", videoObject.GetComponent<RawImage>());
            panelRoot.SetActive(false);

            yield return view.Prewarm();

            Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Hidden));
            Assert.That(panelRoot.activeSelf, Is.False, "프리워밍이 끝나면 창은 숨겨진다.");
            Assert.That(view.Rig, Is.Not.Null);
            Assert.That(view.Rig.IsCreated, Is.True, "카메라와 렌더 텍스처를 미리 만들어 둔다.");
            Assert.That(view.Rig.IsRendering, Is.False, "미리 그린 뒤에는 렌더링을 끈다.");

            // 이후 열면 같은 카메라를 재사용한다.
            var camera = view.Rig.Camera;
            view.SetVisible(true);
            view.ShowDestination(new Vector2(3f, 4f), "엘리베이터");
            Assert.That(view.Rig.Camera, Is.SameAs(camera));
            Assert.That(view.Rig.Position, Is.EqualTo(new Vector2(3f, 4f)), "열 때마다 이전 위치에서 쓸려 가지 않고 바로 비춘다.");

            view.HideImmediate();
            Object.Destroy(canvasObject);
            yield return null;
        }

        private sealed class FakeEscapePort : MonoBehaviour, IEmergencyEscapePortalPort
        {
            public int OpenCount { get; private set; }

            public bool TryOpenEscapePanel(out string reason)
            {
                OpenCount++;
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(
                        EmergencyEscapeDestination.Elevator,
                        string.Empty,
                        "엘리베이터")
                };
            }

            public bool TryEscapeTo(
                EmergencyEscapeDestination kind,
                string outpostInstanceId,
                out string reason)
            {
                reason = string.Empty;
                return true;
            }
        }
    }
}
