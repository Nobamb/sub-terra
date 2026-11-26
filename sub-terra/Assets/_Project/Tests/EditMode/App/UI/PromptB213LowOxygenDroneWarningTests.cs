using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Integration;
using SubTerra.App.Run;
using SubTerra.App.UI.Drone;
using SubTerra.Gameplay.Player;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-213 산소 부족 드론 경고: 발사·재무장 규칙, 임계값, 말풍선 유지, 가장자리 연출.</summary>
    public sealed class PromptB213LowOxygenDroneWarningTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<(SfxId id, float pitch)> played = new List<(SfxId, float)>();
        private float now;

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add((id, pitch));

        [Test]
        public void Policy_FirstDropFiresOnce_AndDoesNotRepeatWhileCritical()
        {
            var policy = new LowOxygenWarningPolicy();
            policy.Reset(false);
            Assert.That(policy.Observe(false), Is.False);
            Assert.That(policy.Observe(true), Is.True, "Safe/Caution → Critical 하강 순간");
            Assert.That(policy.Observe(true), Is.False, "Critical 유지 중 반복 없음");
            Assert.That(policy.Observe(true), Is.False);
        }

        [Test]
        public void Policy_RecoveryRearms_AndNextDropFiresAgain()
        {
            var policy = new LowOxygenWarningPolicy();
            policy.Reset(false);
            Assert.That(policy.Observe(true), Is.True);
            Assert.That(policy.Observe(false), Is.False);
            Assert.That(policy.IsArmed, Is.True, "Critical 위로 회복되면 다시 무장");
            Assert.That(policy.Observe(true), Is.True, "또 떨어지면 한 번 더");
        }

        [Test]
        public void Policy_AlreadyCriticalOnLoad_DoesNotFireUntilRecovered()
        {
            var policy = new LowOxygenWarningPolicy();
            policy.Reset(true);
            Assert.That(policy.IsArmed, Is.False);
            Assert.That(policy.Observe(true), Is.False);
            Assert.That(policy.Observe(false), Is.False);
            Assert.That(policy.Observe(true), Is.True);

            var fresh = new LowOxygenWarningPolicy();
            Assert.That(fresh.Observe(true), Is.False, "첫 관찰이 이미 위험이면 기준점일 뿐 하강이 아니다");
        }

        [Test]
        public void Controller_DebugOxygen_FiresOnFirstDropOnly_AndRearmsAfterRecovery()
        {
            var (survival, controller) = CreateBoundPair(100);

            Assert.That(survival.TrySetDebugOxygenPercent(50), Is.True);
            Assert.That(controller.RaisedCount, Is.EqualTo(0), "Caution(35%)에서는 띄우지 않는다");

            survival.TrySetDebugOxygenPercent(14);
            Assert.That(survival.OxygenRisk, Is.EqualTo(OxygenRiskLevel.Critical));
            Assert.That(controller.RaisedCount, Is.EqualTo(1));

            survival.TrySetDebugOxygenPercent(8);
            survival.TrySetDebugOxygenPercent(3);
            Assert.That(controller.RaisedCount, Is.EqualTo(1), "위험에 머무는 동안 반복 없음");

            survival.TrySetDebugOxygenPercent(100);
            Assert.That(controller.IsArmed, Is.True);
            survival.TrySetDebugOxygenPercent(16);
            Assert.That(controller.RaisedCount, Is.EqualTo(1), "16%는 임계값 위");

            survival.TrySetDebugOxygenPercent(15);
            Assert.That(survival.OxygenRisk, Is.EqualTo(OxygenRiskLevel.Critical), "15% 정확히도 Critical(<=)");
            Assert.That(controller.RaisedCount, Is.EqualTo(2), "회복 뒤 다시 떨어지면 한 번 더");
        }

        [Test]
        public void Controller_OxygenAboveThreshold_NeverFires_EvenWhileOtherRescueCausesExist()
        {
            // 전력 0은 이 컨트롤러의 입력이 아니다. 산소가 임계값 위면 어떤 경우에도 발사하지 않는다.
            var (survival, controller) = CreateBoundPair(100);
            survival.TrySetDebugOxygenPercent(100);
            survival.TrySetDebugOxygenPercent(40);
            survival.TrySetDebugOxygenPercent(16);
            Assert.That(controller.RaisedCount, Is.EqualTo(0));
            Assert.That(controller.IsPending, Is.False);
        }

        [Test]
        public void Controller_BoundWhileAlreadyCritical_DoesNotFireUntilRecoveredAndDroppedAgain()
        {
            var host = NewHost("B213_Load");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.BindOxygenDepth(() => 40f);
            survival.TrySetDebugOxygenPercent(10);
            var controller = host.AddComponent<LowOxygenDroneWarningController>();
            controller.Bind(survival);

            Assert.That(controller.IsArmed, Is.False);
            survival.TrySetDebugOxygenPercent(5);
            Assert.That(controller.RaisedCount, Is.EqualTo(0), "이어하기 때 이미 위험이면 바로 띄우지 않는다");

            survival.TrySetDebugOxygenPercent(100);
            survival.TrySetDebugOxygenPercent(14);
            Assert.That(controller.RaisedCount, Is.EqualTo(1));
        }

        [Test]
        public void Controller_SaveRestoreIntoCritical_IsNotTreatedAsDrop()
        {
            var (survival, controller) = CreateBoundPair(100);
            controller.BeginRestore();
            survival.RestoreOxygen(survival.OxygenMaximum * 0.05f);
            controller.EndRestore();

            Assert.That(survival.OxygenRisk, Is.EqualTo(OxygenRiskLevel.Critical));
            Assert.That(controller.RaisedCount, Is.EqualTo(0));
            Assert.That(controller.IsArmed, Is.False);
        }

        [Test]
        public void Controller_AutoCloseDefaultsToUrgentBubbleSeconds()
        {
            var host = NewHost("B213_Defaults");
            var controller = host.AddComponent<LowOxygenDroneWarningController>();
            Assert.That(controller.AutoCloseSeconds, Is.EqualTo(6f));
        }

        [Test]
        public void OxygenWarningTemplate_IsSeriousUrgent()
        {
            var line = new DroneDialogueResult(DataIds.Dialogue.DroneOxygenWarning, "산소", false, false, true);
            Assert.That(DroneUrgentBubbleRule.IsSerious(line), Is.True);
            Assert.That(DroneOxygenWarningFx.TemplateId, Is.EqualTo("dialogue.drone.oxygen.warning"));
        }

        [Test]
        public void Socket_OxygenWarning_PlaysSeriousOnce_AndHoldsAgainstAnalysisDialogue()
        {
            var socket = Socket(out var label);
            var warning = new DroneDialogueResult(
                DataIds.Dialogue.DroneOxygenWarning, DroneOxygenWarningFx.DefaultText, false, false, true);

            socket.ShowOxygenWarning(warning);
            Assert.That(socket.IsOxygenWarningActive, Is.True);
            Assert.That(label.text, Does.Contain("산소가 부족합니다"));
            Assert.That(played.Select(p => p.id), Is.EqualTo(new[] { SfxId.DroneBubbleUrgentSerious }),
                "기존 심각한 긴급음 하나만");

            played.Clear();
            now += 2f;
            socket.SetDialogue(new DroneDialogueResult(DataIds.Dialogue.DroneExplore, "탐사 계속", false, false));
            Assert.That(label.text, Does.Contain("산소가 부족합니다"), "분석 루프가 경고 문구를 덮어쓰지 않는다");
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void Fx_HaloBlinks_AndSliversStayOnPanelEdges()
        {
            var samples = Enumerable.Range(0, 20).Select(i => DroneOxygenWarningFx.HaloAlpha(i * 0.05f)).ToArray();
            Assert.That(samples.All(a => a >= 0.25f - 0.0001f && a <= 1.0001f), Is.True);
            Assert.That(samples.Max() - samples.Min(), Is.GreaterThan(0.5f), "후광이 깜빡인다");

            for (var step = 0; step < 40; step++)
            {
                var t = step * DroneOxygenWarningFx.JitterStep;
                for (var i = 0; i < DroneOxygenWarningFx.SliverCount; i++)
                {
                    var a = DroneOxygenWarningFx.Sliver(t, i);
                    var b = DroneOxygenWarningFx.Sliver(t, i);
                    Assert.That(a.OffsetX, Is.EqualTo(b.OffsetX), "같은 시간이면 같은 모양");
                    Assert.That(a.RightSide, Is.EqualTo(i % 2 == 1));
                    Assert.That(a.CenterY, Is.InRange(0f, 1f));
                    Assert.That(a.Width, Is.GreaterThan(a.Height * 2.5f), "가로로 길고 세로로 얇다");
                    Assert.That(a.Height, Is.InRange(DroneOxygenWarningFx.SliverMinHeight, DroneOxygenWarningFx.SliverMaxHeight));
                    // 중심이 바깥쪽으로 폭의 15~45% → 조각의 65~95%가 패널 밖.
                    var outsideRatio = 0.5f + Mathf.Abs(a.OffsetX) / a.Width;
                    Assert.That(outsideRatio, Is.InRange(0.64f, 0.96f), "대부분 패널 밖으로 빠져나온다");
                    Assert.That(Mathf.Sign(a.OffsetX), Is.EqualTo(a.RightSide ? 1f : -1f));
                }
            }
        }

        [Test]
        public void Warp_StripOffsets_AreBoundedDeterministic_AndZeroInExcludedHeader()
        {
            var anyShift = false;
            for (var step = 0; step < 60; step++)
            {
                var t = step * DroneOxygenWarningFx.JitterStep;
                var shifted = 0;
                for (var s = 0; s < DroneOxygenEdgeWarp.StripCount; s++)
                {
                    var offset = DroneOxygenWarningFx.StripOffset(t, s, DroneOxygenEdgeWarp.StripCount, false);
                    Assert.That(offset, Is.EqualTo(DroneOxygenWarningFx.StripOffset(t, s, DroneOxygenEdgeWarp.StripCount, false)));
                    Assert.That(Mathf.Abs(offset), Is.LessThanOrEqualTo(DroneOxygenEdgeWarp.MaxShift), "살짝만 왜곡");
                    Assert.That(DroneOxygenWarningFx.StripOffset(t, s, DroneOxygenEdgeWarp.StripCount, true), Is.EqualTo(0f));
                    if (Mathf.Abs(offset) > 0.001f) shifted++;
                }

                Assert.That(shifted, Is.LessThanOrEqualTo(12), "일부 띠만 밀린다");
                anyShift |= shifted > 0;
            }

            Assert.That(anyShift, Is.True);
        }

        [Test]
        public void Warp_SplitsBackgroundQuadIntoStrips_AndKeepsHeaderStripsInPlace()
        {
            var rect = new Rect(-200f, -80f, 400f, 160f);
            var quad = new List<UIVertex>
            {
                V(-200f, -80f, 0f, 0f), V(-200f, 80f, 0f, 1f), V(200f, 80f, 1f, 1f),
                V(200f, 80f, 1f, 1f), V(200f, -80f, 1f, 0f), V(-200f, -80f, 0f, 0f)
            };
            const float exclusion = DroneOxygenWarningFx.TerminalHeaderExclusion;
            for (var step = 0; step < 30; step++)
            {
                var output = new List<UIVertex>();
                DroneOxygenEdgeWarp.WarpStream(quad, output, rect, exclusion, step * DroneOxygenWarningFx.JitterStep);
                Assert.That(output.Count, Is.EqualTo(DroneOxygenEdgeWarp.StripCount * 6), "가로 띠로 잘린다");
                foreach (var v in output)
                {
                    Assert.That(Mathf.Abs(Mathf.Abs(v.position.x) - 200f), Is.LessThanOrEqualTo(DroneOxygenEdgeWarp.MaxShift + 0.01f));
                    Assert.That(v.uv0.y, Is.InRange(0f, 1f));
                    if (v.position.y > rect.yMax - exclusion + 0.01f)
                    {
                        Assert.That(Mathf.Abs(v.position.x), Is.EqualTo(200f).Within(0.01f), "X 버튼 헤더 영역은 그대로");
                    }
                }
            }
        }

        [Test]
        public void Fx_AddsWarpOnlyToPanelBackground_AndClearsOnHide()
        {
            var panelGo = NewHost("B213_PanelBg");
            var panel = panelGo.AddComponent<RectTransform>();
            panel.sizeDelta = new Vector2(400f, 160f);
            panelGo.AddComponent<Image>();
            var fx = new DroneOxygenWarningFx(panel, true, DroneOxygenWarningFx.TerminalHeaderExclusion);
            var warp = panelGo.GetComponent<DroneOxygenEdgeWarp>();
            Assert.That(fx.HasWarp, Is.True);
            Assert.That(warp, Is.Not.Null);
            var haloWarp = panel.Find("OxygenWarningHalo").GetComponent<DroneOxygenEdgeWarp>();
            Assert.That(haloWarp, Is.Not.Null, "후광도 같은 띠로 찢겨 곧은 선이 틀을 덮지 않는다");
            Assert.That(panel.GetComponentsInChildren<DroneOxygenEdgeWarp>(true).Length, Is.EqualTo(2), "배경·후광에만 붙고 글자에는 없다");

            fx.Apply(0.2f);
            Assert.That(warp.IsWarping, Is.True);
            Assert.That(haloWarp.IsWarping, Is.True);
            fx.Hide();
            Assert.That(warp.IsWarping, Is.False);
            Assert.That(haloWarp.IsWarping, Is.False);
        }

        [Test]
        public void Warp_PiecesOutsideReferenceRect_StayInPlace()
        {
            // 후광 위아래 줄처럼 기준 패널 밖(2px 넘게)에 있는 조각은 밀지 않는다.
            var rect = new Rect(-200f, -80f, 400f, 160f);
            var below = new List<UIVertex>
            {
                V(-216f, -96f, 0f, 0f), V(-216f, -80f, 0f, 1f), V(216f, -80f, 1f, 1f),
                V(216f, -80f, 1f, 1f), V(216f, -96f, 1f, 0f), V(-216f, -96f, 0f, 0f)
            };
            for (var step = 0; step < 30; step++)
            {
                var output = new List<UIVertex>();
                DroneOxygenEdgeWarp.WarpStream(below, output, rect, 0f, step * DroneOxygenWarningFx.JitterStep);
                Assert.That(output.All(v => Mathf.Abs(Mathf.Abs(v.position.x) - 216f) < 0.01f), Is.True);
            }
        }

        private static UIVertex V(float x, float y, float u, float v)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, 0f);
            vertex.uv0 = new Vector4(u, v, 0f, 0f);
            vertex.color = Color.white;
            return vertex;
        }

        [Test]
        public void Fx_CreatesHaloAndEdgeSliversInsidePanelOnly()
        {
            var panelGo = NewHost("B213_Panel");
            var panel = panelGo.AddComponent<RectTransform>();
            panel.sizeDelta = new Vector2(400f, 160f);
            var fx = new DroneOxygenWarningFx(panel, true);
            fx.Apply(0.3f);

            var halo = panel.Find("OxygenWarningHalo").GetComponent<Image>();
            Assert.That(halo.gameObject.activeSelf, Is.True);
            Assert.That(halo.fillCenter, Is.False, "가운데는 비워 둘레에만 번진다");
            Assert.That(halo.raycastTarget, Is.False);
            Assert.That(halo.rectTransform.offsetMin.x, Is.EqualTo(-DroneOxygenWarningFx.HaloPadding));
            var images = panel.GetComponentsInChildren<Image>(true);
            Assert.That(images.Length, Is.EqualTo(DroneOxygenWarningFx.SliverCount + 1));
            Assert.That(images.All(image => !image.raycastTarget), Is.True, "입력을 막지 않는다");

            fx.Hide();
            Assert.That(images.All(image => !image.gameObject.activeSelf), Is.True);
        }

        private (PlayerSurvivalController survival, LowOxygenDroneWarningController controller) CreateBoundPair(int percent)
        {
            var host = NewHost("B213_Pair");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.BindOxygenDepth(() => 40f);
            survival.TrySetDebugOxygenPercent(percent);
            var controller = host.AddComponent<LowOxygenDroneWarningController>();
            controller.Bind(survival);
            return (survival, controller);
        }

        private DroneDialogueSocket Socket(out TMP_Text label)
        {
            var go = NewHost("B213_Socket");
            go.AddComponent<RectTransform>();
            var group = go.AddComponent<CanvasGroup>();
            var text = new GameObject("Text", typeof(RectTransform));
            created.Add(text);
            text.transform.SetParent(go.transform, false);
            label = text.AddComponent<TextMeshProUGUI>();
            var socket = go.AddComponent<DroneDialogueSocket>();
            Set(socket, "dialogueText", label);
            Set(socket, "canvasGroup", group);
            socket.ConfigureSfxForTests(() => now, id => float.PositiveInfinity, () => false);
            socket.SetVisible(true);
            socket.SetDialogue(new DroneDialogueResult("initial", "초기화", false, false));
            now += 1.1f;
            played.Clear();
            return socket;
        }

        private GameObject NewHost(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
