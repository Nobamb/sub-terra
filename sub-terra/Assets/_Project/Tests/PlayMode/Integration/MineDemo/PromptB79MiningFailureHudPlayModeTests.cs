using System.Collections;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.PlayerStatus;
using SubTerra.Gameplay.Mining;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>채굴 실패는 플레이어 머리 위 노란 경고 문구로 뜨고, 메인 화면 레이어에 그려져 CCTV에는 찍히지 않는다.</summary>
    public sealed class PromptB79MiningFailureHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator Failure_ShowsWarningTagOnMainScreenLayer()
        {
            var systemObject = new GameObject("MiningSystem");
            var system = systemObject.AddComponent<MiningSystem>();
            var stackObject = new GameObject("PlayerHeadMessageStack");
            var stack = stackObject.AddComponent<PlayerHeadMessageStack>();
            var hudObject = new GameObject("MiningProgressHud");
            var hud = hudObject.AddComponent<MiningProgressHud>();
            hud.SetMessageStack(stack);
            hud.BindTo(system);

            Assert.That(system.TryStartMining(Vector3Int.zero), Is.False);
            yield return null;

            var entry = stack.Model.Entries.FirstOrDefault(e => e.Key == "mining.failure");
            Assert.That(entry, Is.Not.Null, "실패 문구 항목이 생겨야 한다");
            Assert.That(entry.Style, Is.EqualTo(PlayerStatusStyle.Warning));
            Assert.That(entry.Wanted, Is.True);

            var canvas = stackObject.GetComponentsInChildren<Canvas>(false).FirstOrDefault();
            Assert.That(canvas, Is.Not.Null, "머리 위 월드 캔버스가 있어야 한다");
            Assert.That(canvas.gameObject.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
            Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(canvas.gameObject), Is.True);

            Object.Destroy(systemObject);
            Object.Destroy(hudObject);
            Object.Destroy(stackObject);
        }

        [UnityTest]
        public IEnumerator NotMineableWhilePlacing_ShowsPlacementMessage_InIntegrationScene()
        {
            using (var env = new UiTestEnvironment())
            {
                yield return env.LoadIntegration();
                var bridge = Object.FindAnyObjectByType<GameplayBuildingPlacementBridge>();
                var placement = Object.FindAnyObjectByType<SubTerra.Gameplay.Building.BuildingPlacementSystem>();
                var mining = Object.FindAnyObjectByType<MiningSystem>();
                var stack = Object.FindAnyObjectByType<PlayerHeadMessageStack>();
                Assert.That(bridge, Is.Not.Null);
                Assert.That(placement, Is.Not.Null);
                Assert.That(mining, Is.Not.Null);
                Assert.That(stack, Is.Not.Null);

                Assert.That(bridge.BeginPreview(SubTerra.App.Core.Data.DataIds.Buildings.ChargerBasic), Is.True);
                Assert.That(placement.Selection, Is.Not.Null, "시설을 고르면 설치 준비(Selection)가 있어야 한다");

                mining.SetCellProtectionPredicate(_ => true);
                Assert.That(mining.TryStartMining(Vector3Int.zero), Is.False);
                yield return null;

                var entry = stack.Model.Find("mining.failure");
                Assert.That(entry, Is.Not.Null);
                Assert.That(entry.Text, Is.EqualTo(MiningProgressHud.PlacementBlockedMessage));

                // 타일이 없는 빈 칸(시설이 겹친 자리)을 눌러도 채굴 문구가 아니라 설치 문구여야 한다.
                mining.SetCellProtectionPredicate(null);
                Assert.That(mining.TryStartMining(new Vector3Int(9999, 9999, 0)), Is.False);
                yield return null;
                Assert.That(mining.LastFailure, Is.EqualTo(MiningFailureReason.InvalidTarget));
                Assert.That(stack.Model.Find("mining.failure").Text, Is.EqualTo(MiningProgressHud.PlacementBlockedMessage));

                // 설치 준비를 끝내면 같은 실패가 다시 채굴 문구로 돌아온다.
                bridge.CancelPreview();
                Assert.That(mining.TryStartMining(new Vector3Int(9999, 9999, 0)), Is.False);
                yield return null;
                Assert.That(stack.Model.Find("mining.failure").Text, Is.Not.EqualTo(MiningProgressHud.PlacementBlockedMessage));
            }
        }

        [UnityTest]
        public IEnumerator RealClick_OnExistingFacilityWhilePlacing_NeverShowsMiningMessage()
        {
            using (var env = new UiTestEnvironment())
            {
                yield return env.LoadIntegration();
                var bridge = Object.FindAnyObjectByType<GameplayBuildingPlacementBridge>();
                var placement = Object.FindAnyObjectByType<SubTerra.Gameplay.Building.BuildingPlacementSystem>();
                var stack = Object.FindAnyObjectByType<PlayerHeadMessageStack>();
                var player = Object.FindAnyObjectByType<SubTerra.Gameplay.Player.PlayerMovement>();
                var mining = Object.FindAnyObjectByType<MiningSystem>();
                var target = player.transform.position + Vector3.right * 1.2f;
                var protectedCell = placement.WorldToCell(target);
                mining.SetCellProtectionPredicate(cell => cell == protectedCell);

                var body = player.GetComponent<Rigidbody2D>();
                var spot = player.transform.position;
                player.transform.position = spot;
                if (body != null) body.position = spot;
                yield return null;
                yield return null;

                Assert.That(bridge.BeginPreview(SubTerra.App.Core.Data.DataIds.Buildings.ChargerBasic), Is.True);
                yield return null;

                var screen = (Vector2)Camera.main.WorldToScreenPoint(target);
                env.Mouse.MakeCurrent();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(env.Mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = screen });
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(env.Mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = screen, buttons = 1 });
                var log = new System.Text.StringBuilder();
                for (var i = 0; i < 6; i++)
                {
                    yield return null;
                    log.Append("f").Append(i).Append(" sel=").Append(placement.Selection != null).Append(" [");
                    foreach (var e in stack.Model.Entries) log.Append(e.Key).Append('=').Append(e.Text).Append(e.Wanted ? "" : "(leaving)").Append("; ");
                    log.Append("] ");
                }

                UnityEngine.InputSystem.InputSystem.QueueStateEvent(env.Mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = screen });
                yield return null;
                var texts = string.Join("|", stack.Model.Entries.Select(e => e.Text));
                var drawn = string.Join("|", stack.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text));
                Assert.That(texts, Does.Not.Contain("채굴할 수 없는 지형입니다."), log.ToString());
                Assert.That(drawn, Does.Not.Contain("채굴할 수 없는 지형입니다."), "화면에 그려진 글자: " + drawn);
                Assert.That(drawn, Does.Contain(MiningProgressHud.PlacementBlockedMessage), "화면에 그려진 글자: " + drawn);
            }
        }

        [UnityTest]
        public IEnumerator Failure_LeavesAfterHoldTime()
        {
            var systemObject = new GameObject("MiningSystem");
            var system = systemObject.AddComponent<MiningSystem>();
            var stackObject = new GameObject("PlayerHeadMessageStack");
            var stack = stackObject.AddComponent<PlayerHeadMessageStack>();
            var hudObject = new GameObject("MiningProgressHud");
            var hud = hudObject.AddComponent<MiningProgressHud>();
            hud.SetMessageStack(stack);
            hud.BindTo(system);

            Assert.That(system.TryStartMining(Vector3Int.zero), Is.False);
            yield return null;
            Assert.That(stack.Model.Entries.Any(e => e.Key == "mining.failure" && e.Wanted), Is.True);

            // 2.5초 표시 + 퇴장 연출이 끝날 만큼 실시간으로 기다린다.
            yield return new WaitForSecondsRealtime(PlayerStatusTimeline.HoldSeconds + 0.6f);

            Assert.That(stack.Model.Entries.Any(e => e.Key == "mining.failure"), Is.False);
            Object.Destroy(systemObject);
            Object.Destroy(hudObject);
            Object.Destroy(stackObject);
        }
    }
}
