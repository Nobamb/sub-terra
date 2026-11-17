using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>
    /// prompt-B 118-1: 업그레이드 창 통일(지상·지하), 설정창형 프레임, 정사각 X 버튼, 심층 구역 문구 제거,
    /// 노드 호버 빛·아이콘 연출, 스크롤 확대/축소.
    /// </summary>
    public sealed class PromptB1181UpgradeWindowTests
    {
        private const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string PanelPath = "SurfaceBaseContent/UpgradeModal/ProgressionPanel";

        // ------------------------------------------------------------------ 확대/축소 계산

        [Test]
        public void Zoom_ScrollUpZoomsIn_ScrollDownZoomsOut_AndClamps()
        {
            Assert.That(UpgradeTreeZoom.NextZoom(1f, 1f), Is.EqualTo(UpgradeTreeZoom.Step).Within(1e-4f));
            Assert.That(UpgradeTreeZoom.NextZoom(1f, -120f), Is.EqualTo(1f / UpgradeTreeZoom.Step).Within(1e-4f));
            Assert.That(UpgradeTreeZoom.NextZoom(1.3f, 0f), Is.EqualTo(1.3f).Within(1e-4f));

            var zoom = 1f;
            for (var i = 0; i < 30; i++)
            {
                zoom = UpgradeTreeZoom.NextZoom(zoom, 1f);
            }

            Assert.That(zoom, Is.EqualTo(UpgradeTreeZoom.MaxZoom).Within(1e-4f));
            for (var i = 0; i < 30; i++)
            {
                zoom = UpgradeTreeZoom.NextZoom(zoom, -1f);
            }

            Assert.That(zoom, Is.EqualTo(UpgradeTreeZoom.MinZoom).Within(1e-4f));
        }

        [Test]
        public void Zoom_KeepsTreePointUnderPointer()
        {
            var pan = new Vector2(30f, -12f);
            var pivot = new Vector2(180f, 60f);
            var before = (pivot - pan) / 0.9f;
            var moved = UpgradeTreeZoom.PanAround(pan, 0.9f, 1.35f, pivot);
            var after = (pivot - moved) / 1.35f;
            Assert.That(Vector2.Distance(before, after), Is.LessThan(0.001f));
        }

        [Test]
        public void Zoom_PanIsCenteredWhenTreeFits_AndLimitedToOverflowWhenLarger()
        {
            var content = new Vector2(1000f, 500f);
            var viewport = new Vector2(1000f, 600f);
            Assert.That(UpgradeTreeZoom.ClampPan(new Vector2(300f, 300f), content, 1f, viewport), Is.EqualTo(Vector2.zero));

            var clamped = UpgradeTreeZoom.ClampPan(new Vector2(900f, -900f), content, 2f, viewport);
            Assert.That(clamped.x, Is.EqualTo(500f).Within(0.01f));
            Assert.That(clamped.y, Is.EqualTo(-200f).Within(0.01f));
        }

        // ------------------------------------------------------------------ 아이콘 연출 곡선

        [Test]
        public void IconFx_EveryUpgradeHasItsOwnKind()
        {
            var ids = PromptB118UpgradeTreeBuilder.Nodes.Select(n => n.Id).ToArray();
            var kinds = ids.Select(UpgradeIconFx.KindFor).ToArray();
            Assert.That(kinds, Has.None.EqualTo(UpgradeIconFxKind.None));
            Assert.That(kinds.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(UpgradeIconFx.KindFor("upgrade.unknown"), Is.EqualTo(UpgradeIconFxKind.None));
        }

        [Test]
        public void IconFx_DrillArrowThrustsDownTwiceAndReturns()
        {
            var cycle = UpgradeIconFx.DrillCycle;
            Assert.That(UpgradeIconFx.DrillThrust(0f), Is.EqualTo(0f));
            Assert.That(UpgradeIconFx.DrillThrust(cycle * 0.4f), Is.EqualTo(1f).Within(0.02f));
            Assert.That(UpgradeIconFx.DrillThrust(cycle * 0.999f), Is.LessThan(0.05f));
            Assert.That(UpgradeIconFx.DrillThrust(cycle * 1.4f), Is.EqualTo(1f).Within(0.02f));
            Assert.That(UpgradeIconFx.DrillThrust(cycle * 2f + 0.01f), Is.EqualTo(0f));
        }

        [Test]
        public void IconFx_CargoLidOpensThenCloses_AndItemsFadeIntoBox()
        {
            Assert.That(UpgradeIconFx.LidOpen(0f), Is.EqualTo(0f));
            Assert.That(UpgradeIconFx.LidOpen(0.6f), Is.EqualTo(1f));
            Assert.That(UpgradeIconFx.LidOpen(UpgradeIconFx.LidCloseEnd), Is.EqualTo(0f));
            Assert.That(UpgradeIconFx.CargoItemStarts.Length, Is.EqualTo(3), "구리·철·리튬");
            Assert.That(UpgradeIconFx.CargoItemStarts[0], Is.GreaterThanOrEqualTo(UpgradeIconFx.LidOpenEnd));

            Assert.That(UpgradeIconFx.ItemAlpha(-1f), Is.EqualTo(0f));
            Assert.That(UpgradeIconFx.ItemAlpha(0.3f), Is.EqualTo(1f));
            Assert.That(UpgradeIconFx.ItemAlpha(0.75f), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(UpgradeIconFx.ItemAlpha(1f), Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(0f)]
        [TestCase(0.1f)]
        [TestCase(0.25f)]
        [TestCase(0.4f)]
        [TestCase(0.5f)]
        public void IconFx_RegenSpinMatchesMineResetButtonCurve(float seconds)
        {
            Assert.That(UpgradeIconFx.SpinTurn(seconds), Is.EqualTo(SurfaceBaseButtonFeedback.SpinTurn(seconds)).Within(1e-5f));
        }

        [Test]
        public void IconFx_BreathInhalesHoldsThenExhalesLonger_AndRepeats()
        {
            var cycle = UpgradeIconFx.OxygenBreathCycle;
            var inhale = cycle * UpgradeIconFx.BreathInhaleShare;
            var exhaleStart = UpgradeIconFx.BreathExhaleStart(cycle);
            Assert.That(UpgradeIconFx.Breath(0f, cycle), Is.EqualTo(0f));
            Assert.That(UpgradeIconFx.Breath(inhale * 0.5f, cycle), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(UpgradeIconFx.Breath((inhale + exhaleStart) * 0.5f, cycle), Is.EqualTo(1f), "가득 들이쉰 뒤 잠깐 멈춘다");
            Assert.That(UpgradeIconFx.Breath(cycle * 0.999f, cycle), Is.LessThan(0.01f));
            Assert.That(UpgradeIconFx.Breath(cycle + inhale * 0.5f, cycle), Is.EqualTo(0.5f).Within(0.01f), "반복");
            Assert.That(cycle - exhaleStart, Is.GreaterThan(inhale), "날숨이 들숨보다 길다");
            Assert.That(UpgradeIconFx.MinerBreathCycle, Is.GreaterThan(cycle), "광부는 O2보다 느리게 쉰다");

            // 광부 호흡 그림: 0→1→2(들숨) → 2(멈춤) → 3→0(날숨). 구간 경계에서 그림이 끊기지 않는다.
            var miner = UpgradeIconFx.MinerBreathCycle;
            var minerInhale = miner * UpgradeIconFx.BreathInhaleShare;
            UpgradeIconFx.MinerBreathFrame(0f, miner, out var from, out var to, out var mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 0, 0 }));
            UpgradeIconFx.MinerBreathFrame(minerInhale * 0.25f, miner, out from, out to, out mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(mix, Is.EqualTo(0.5f).Within(0.01f));
            UpgradeIconFx.MinerBreathFrame(minerInhale * 0.75f, miner, out from, out to, out mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 1, 2 }));
            UpgradeIconFx.MinerBreathFrame(minerInhale + 0.01f, miner, out from, out to, out mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 2, 2 }), "가득 들이쉰 채 멈춤");
            UpgradeIconFx.MinerBreathFrame(UpgradeIconFx.BreathExhaleStart(miner) + 0.01f, miner, out from, out to, out mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(mix, Is.LessThan(0.05f), "멈춤에서 이어진다");
            UpgradeIconFx.MinerBreathFrame(miner * 0.999f, miner, out from, out to, out mix);
            Assert.That(new[] { from, to }, Is.EqualTo(new[] { 3, 0 }));
            Assert.That(mix, Is.GreaterThan(0.99f), "다 내쉰 그림으로 끝나 다음 호흡과 이어진다");

            Assert.That(UpgradeIconFx.PassedCycleMark(0f, exhaleStart - 0.01f, cycle, exhaleStart), Is.False);
            Assert.That(UpgradeIconFx.PassedCycleMark(exhaleStart - 0.01f, exhaleStart + 0.01f, cycle, exhaleStart), Is.True);
            Assert.That(UpgradeIconFx.PassedCycleMark(exhaleStart + 0.01f, cycle, cycle, exhaleStart), Is.False);
            Assert.That(UpgradeIconFx.PassedCycleMark(cycle, cycle + exhaleStart + 0.01f, cycle, exhaleStart), Is.True);
        }

        // ------------------------------------------------------------------ 지상 기지 프리팹

        [Test]
        public void SurfaceWindow_UsesSettingsStyleFrame_AndKeepsSize()
        {
            var panel = SurfacePanel();
            var rect = (RectTransform)panel;
            Assert.That(rect.sizeDelta, Is.EqualTo(PromptB1181UpgradeWindowBuilder.WindowSize));
            var image = panel.GetComponent<Image>();
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.name, Is.EqualTo("upgrade-window-frame"));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.sprite.border.x, Is.GreaterThan(0f), "9-slice 경계가 있어야 모서리가 늘어나지 않는다");
        }

        [Test]
        public void SurfaceWindow_CloseButtonIsSquareSettingsX()
        {
            var close = (RectTransform)SurfacePanel().Find("CloseUpgradeButton");
            AssertSettingsClose(close);
        }

        [Test]
        public void Issue223_SurfaceWindowDeepZoneConditionsReserveSpaceAndDoNotDuplicateOnRefresh()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
                var initialBottom = tree.Viewport.offsetMin.y;
                tree.SetDeepZoneStatus(new ZoneAccessResult(false, false, "드론 스캔 범위 2레벨 필요"));
                var labels = instance.GetComponentsInChildren<TMP_Text>(true)
                    .Where(t => t.text != null && t.text.Contains("심층 구역 Lv.0/3")).ToArray();
                Assert.That(labels.Length, Is.EqualTo(1));
                // B-155: 조건 문구는 Lv.1 썸네일 아래 띠로 옮겼다.
                var lv1Condition = tree.DeepZoneThumbnails.GetCellState(0).Condition;
                Assert.That(lv1Condition, Does.Contain("드릴 속도 Lv.2"));
                Assert.That(lv1Condition, Does.Contain("드론 스캔 범위 Lv.2"));
                Assert.That(lv1Condition, Does.Contain("가스 저항 Lv.1"));
                Assert.That(labels[0].raycastTarget, Is.False);
                var labelPanel = (RectTransform)labels[0].transform.parent;
                Assert.That(tree.Viewport.offsetMin.y, Is.GreaterThan(initialBottom));
                Assert.That(labelPanel.offsetMax.y, Is.LessThan(tree.Viewport.offsetMin.y));
                var reservedBottom = tree.Viewport.offsetMin.y;
                tree.SetDeepZoneStatus(new ZoneAccessResult(true, true, string.Empty));
                Assert.That(tree.Viewport.offsetMin.y, Is.EqualTo(reservedBottom));
                Assert.That(labels[0].text, Does.Contain("심층 구역 Lv.1/3"));
                Assert.That(instance.GetComponentsInChildren<Transform>(true).Count(t => t.name == "DeepZoneStatus"), Is.EqualTo(1));
                Assert.That(tree.DeepZoneThumbnails.CellCount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void SurfaceWindow_EveryNodeHasHoverFx_AndOnlyNodeRootReceivesInput()
        {
            var panel = SurfacePanel();
            var tree = panel.GetComponentInChildren<UpgradeTreeView>(true);
            Assert.That(tree.Viewport.GetComponent<RectMask2D>(), Is.Not.Null, "확대 시 트리가 상세 패널 위로 넘치지 않아야 한다");
            Assert.That(panel.parent.GetComponent<UpgradeTreeScrollForwarder>(), Is.Not.Null);

            foreach (var node in tree.Nodes)
            {
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                Assert.That(fx, Is.Not.Null, node.UpgradeId);
                Assert.That(fx.Kind, Is.EqualTo(UpgradeIconFx.KindFor(node.UpgradeId)), node.UpgradeId);
                Assert.That(fx.HoverGlow, Is.Not.Null, node.UpgradeId);
                Assert.That(fx.HoverGlow.color.a, Is.EqualTo(0f), "평소에는 빛이 보이지 않는다");

                var targets = node.GetComponentsInChildren<Graphic>(true).Where(g => g.raycastTarget).ToArray();
                Assert.That(targets.Length, Is.EqualTo(1), node.UpgradeId);
                Assert.That(targets[0].gameObject, Is.EqualTo(node.gameObject), node.UpgradeId);
            }
        }

        [Test]
        public void SurfaceWindow_GasResistanceUsesCharacterInGasIcon()
        {
            var tree = SurfacePanel().GetComponentInChildren<UpgradeTreeView>(true);
            var gas = tree.FindNode(DataIds.Upgrades.GasResistance);
            Assert.That(gas.IconSprite, Is.Not.Null);
            Assert.That(gas.IconSprite.name, Is.EqualTo("upgrade-icon-gas"));
            Assert.That(gas.transform.Find("Body/FxFront/GasHand"), Is.Not.Null, "호버 시 입을 막는 손");
        }

        // ------------------------------------------------------------------ 호버 동작

        [Test]
        public void Hover_CargoBoxOpensWithItems_ThenRestoresClosedIcon()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var tree = panel.GetComponentInChildren<UpgradeTreeView>(true);
                var node = tree.FindNode(DataIds.Upgrades.MaximumCargo);
                node.Apply(Snapshot(DataIds.Upgrades.MaximumCargo, true));
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                var icon = node.transform.Find("Body/Icon").GetComponent<Image>();
                var lid = node.transform.Find("Body/FxFront/BoxLid").GetComponent<Image>();
                var restPosition = icon.rectTransform.anchoredPosition;

                fx.BeginHover();
                Step(fx, 0.5f);
                Assert.That(fx.HoverGlow.color.a, Is.GreaterThan(0.2f), "가운데 청록 빛");
                Assert.That(lid.enabled, Is.True);
                Assert.That(lid.rectTransform.anchoredPosition.y, Is.GreaterThan(5f), "뚜껑이 열린다");
                Assert.That(icon.canvasRenderer.GetAlpha(), Is.EqualTo(0f), "연출 중에는 닫힌 원본 아이콘을 가린다");
                Assert.That(node.transform.Find("Body/FxFront").GetComponentsInChildren<Image>(true)
                    .Any(i => i.name.StartsWith("Item") && i.enabled), Is.True, "화물이 상자로 들어간다");

                fx.EndHover();
                Step(fx, 2f);
                Assert.That(fx.IsPlaying, Is.False);
                Assert.That(fx.HoverGlow.color.a, Is.EqualTo(0f).Within(0.001f));
                Assert.That(lid.enabled, Is.False);
                Assert.That(icon.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
                Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(restPosition));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hover_DrillArrowMovesDownAndComesBack()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var node = panel.GetComponentInChildren<UpgradeTreeView>(true).FindNode(DataIds.Upgrades.DrillSpeed);
                node.Apply(Snapshot(DataIds.Upgrades.DrillSpeed, true));
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                var icon = node.transform.Find("Body/Icon").GetComponent<RectTransform>();
                var rest = icon.anchoredPosition;

                fx.BeginHover();
                Step(fx, UpgradeIconFx.DrillCycle * 0.4f);
                Assert.That(icon.anchoredPosition.y, Is.LessThan(rest.y - 5f));
                Step(fx, UpgradeIconFx.DrillCycle * 2f);
                Assert.That(icon.anchoredPosition.y, Is.EqualTo(rest.y).Within(0.01f));
                fx.EndHover();
                Step(fx, 1f);
                Assert.That(icon.anchoredPosition, Is.EqualTo(rest));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hover_LockedNodeShowsOnlyGlow()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var node = panel.GetComponentInChildren<UpgradeTreeView>(true).FindNode(DataIds.Upgrades.GasResistance);
                node.Apply(Snapshot(DataIds.Upgrades.GasResistance, false));
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                var hand = node.transform.Find("Body/FxFront/GasHand").GetComponent<Image>();

                fx.BeginHover();
                Step(fx, 0.4f);
                var blindGlow = node.transform.Find("Body/Blind/BlindHoverGlow").GetComponent<Image>();
                Assert.That(blindGlow.color.a, Is.GreaterThan(0.1f), "잠긴 노드도 가운데 빛으로 호버를 알린다");
                Assert.That(hand.enabled, Is.False, "잠긴 노드는 아이콘 연출을 하지 않는다");
                Assert.That(fx.IsPlaying, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hover_MaximumOxygenBreathesWithWhiteGlowBehind_ThenReturnsToRest()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var node = panel.GetComponentInChildren<UpgradeTreeView>(true).FindNode(DataIds.Upgrades.MaximumOxygen);
                node.Apply(Snapshot(DataIds.Upgrades.MaximumOxygen, true));
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                var icon = node.transform.Find("Body/Icon").GetComponent<RectTransform>();
                var rest = icon.anchoredPosition;
                var cycle = UpgradeIconFx.OxygenBreathCycle;

                fx.BeginHover();
                Step(fx, cycle * 0.44f);
                Assert.That(icon.localScale.x, Is.GreaterThan(1.18f), "가득 들이쉬면 O2가 부푼다");
                var glow = node.transform.Find("Body/FxBack/TintGlow").GetComponent<Image>();
                Assert.That(glow.enabled, Is.True);
                Assert.That(glow.color.r, Is.GreaterThan(0.95f), "뒤쪽 광채는 흰색");
                Assert.That(glow.color.b, Is.GreaterThan(0.95f));
                Assert.That(glow.color.a, Is.GreaterThan(0.5f));
                Assert.That(glow.transform.parent.GetSiblingIndex(), Is.LessThan(icon.GetSiblingIndex()), "광채는 아이콘 뒤");
                var peakGlow = glow.rectTransform.sizeDelta.x;

                Step(fx, cycle * 0.3f);
                Assert.That(icon.localScale.x, Is.InRange(1.02f, 1.18f), "천천히 줄어드는 중");
                Assert.That(node.transform.Find("Body/FxFront").GetComponentsInChildren<Image>(false)
                    .Any(i => i.name.StartsWith("Fx")), Is.True, "내쉴 때 기포가 떠오른다");

                Step(fx, cycle * 0.25f);
                Assert.That(icon.localScale.x, Is.LessThan(1.02f), "다 내쉬면 원래 크기");
                Assert.That(glow.rectTransform.sizeDelta.x, Is.LessThan(peakGlow));

                Step(fx, cycle * 0.45f);
                Assert.That(icon.localScale.x, Is.GreaterThan(1.18f), "호버 동안 계속 숨을 쉰다");

                fx.EndHover();
                Step(fx, 1f);
                Assert.That(fx.IsPlaying, Is.False);
                Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
                Assert.That(icon.anchoredPosition, Is.EqualTo(rest));
                Assert.That(glow.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hover_OxygenEfficiencyShowsSlowlyBreathingMiner_ThenRestoresIcon()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var node = panel.GetComponentInChildren<UpgradeTreeView>(true).FindNode(DataIds.Upgrades.OxygenEfficiency);
                node.Apply(Snapshot(DataIds.Upgrades.OxygenEfficiency, true));
                var fx = node.GetComponent<UpgradeNodeHoverFx>();
                var icon = node.transform.Find("Body/Icon").GetComponent<Image>();
                var now = node.transform.Find("Body/FxFront/MinerBreathA").GetComponent<Image>();
                var next = node.transform.Find("Body/FxFront/MinerBreathB").GetComponent<Image>();
                Assert.That(icon.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-01"), "썸네일부터 광부 상반신");
                Assert.That(now.enabled, Is.False, "평소에는 썸네일 한 장만 보인다");
                Assert.That(now.rectTransform.pivot.y, Is.EqualTo(0f), "아래쪽을 기준으로 부풀어 밑단이 뜨지 않는다");
                var cycle = UpgradeIconFx.MinerBreathCycle;
                var inhale = cycle * UpgradeIconFx.BreathInhaleShare;

                fx.BeginHover();
                Step(fx, inhale * 0.25f);
                Assert.That(now.enabled, Is.True);
                Assert.That(icon.canvasRenderer.GetAlpha(), Is.EqualTo(0f).Within(0.01f), "썸네일 대신 호흡 그림을 그린다");
                Assert.That(now.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-01"));
                Assert.That(next.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-02"), "들이쉬는 그림이 겹쳐 섞인다");
                Assert.That(next.color.a, Is.InRange(0.2f, 0.8f));
                var early = now.rectTransform.localScale;
                Assert.That(early.y, Is.GreaterThan(1.01f), "섞는 동안 앞 그림은 다음 그림 높이로 올라간다");
                Assert.That(early.y * UpgradeIconFx.MinerFrameRise[0],
                    Is.EqualTo(next.rectTransform.localScale.y * UpgradeIconFx.MinerFrameRise[1]).Within(1e-3f),
                    "겹친 두 그림의 머리 높이가 같아 둘로 보이지 않는다");

                Step(fx, cycle * 0.44f - inhale * 0.25f);
                Assert.That(now.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-03"), "가득 들이쉰 그림에서 잠깐 멈춘다");
                var full = now.rectTransform.localScale;
                Assert.That(full.y, Is.EqualTo(1f).Within(1e-4f), "멈춘 그림은 늘이지 않는다");
                Assert.That(full.x, Is.GreaterThan(early.x + 0.02f), "들이쉬면 가슴이 옆으로 부푼다");
                Assert.That(next.rectTransform.localScale, Is.EqualTo(full), "겹친 두 그림은 같이 움직인다");

                var exhaleStart = UpgradeIconFx.BreathExhaleStart(cycle);
                var blow = exhaleStart + (cycle - exhaleStart) * UpgradeIconFx.MinerBlowShare;
                Step(fx, blow + 0.05f - cycle * 0.44f);
                Assert.That(now.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-04"), "입으로 내쉬는 그림");
                Assert.That(next.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-01"), "다 내쉰 그림으로 돌아간다");
                Assert.That(now.rectTransform.localScale.y, Is.LessThan(full.y), "내쉬면 내려온다");
                Assert.That(node.transform.Find("Body/FxFront").GetComponentsInChildren<Image>(true)
                    .Any(i => i.name.StartsWith("Fx")), Is.True, "입가의 숨결");

                Step(fx, cycle - blow + inhale);
                Assert.That(now.sprite.name, Is.EqualTo("upgrade-icon-miner-breath-03"), "호버 동안 계속 숨을 쉰다");

                fx.EndHover();
                Step(fx, 1f);
                Assert.That(fx.IsPlaying, Is.False);
                Assert.That(now.enabled, Is.False);
                Assert.That(next.enabled, Is.False);
                Assert.That(now.rectTransform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(next.rectTransform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(icon.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ------------------------------------------------------------------ 스크롤 확대/축소

        [Test]
        public void Scroll_OnWindowZoomsTree_UpInDownOut()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var panel = OpenModal(instance);
                var view = panel.GetComponent<ProgressionPanelView>();
                var tree = view.TreeView;
                tree.SnapLayout();
                var start = tree.ZoomTarget;

                view.OnScroll(new PointerEventData(null) { scrollDelta = new Vector2(0f, 1f), position = new Vector2(-9999f, -9999f) });
                Assert.That(tree.ZoomTarget, Is.GreaterThan(start));

                var forwarder = panel.parent.GetComponent<UpgradeTreeScrollForwarder>();
                for (var i = 0; i < 20; i++)
                {
                    forwarder.OnScroll(new PointerEventData(null) { scrollDelta = new Vector2(0f, -1f), position = new Vector2(-9999f, -9999f) });
                }

                Assert.That(tree.ZoomTarget, Is.EqualTo(UpgradeTreeZoom.MinZoom).Within(1e-4f));
                Assert.That(tree.PanTarget, Is.EqualTo(Vector2.zero), "축소 상태에서는 트리가 가운데에 있다");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ------------------------------------------------------------------ 지하(통합 씬) 창

        [Test]
        public void MineWindow_MatchesSurfaceWindow()
        {
            var scene = SceneManager.GetSceneByPath(PromptB118UpgradeTreeBuilder.IntegrationScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(PromptB118UpgradeTreeBuilder.IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var panel = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<ProgressionPanelView>(true))
                    .Single(v => v.name == "UpgradePanel");
                var rect = (RectTransform)panel.transform;
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.sizeDelta, Is.EqualTo(PromptB1181UpgradeWindowBuilder.WindowSize));
                Assert.That(panel.GetComponent<Image>().sprite.name, Is.EqualTo("upgrade-window-frame"));
                AssertSettingsClose((RectTransform)panel.transform.Find("CloseButton"));

                var purchase = panel.transform.Find("PurchaseButton");
                Assert.That(purchase.GetComponent<SurfaceBaseButtonFeedback>(), Is.Not.Null, "지상 기지와 같은 구매 버튼");
                Assert.That(purchase.Find("Frame"), Is.Not.Null);

                Assert.That(panel.IsTreeMode, Is.True);
                Assert.That(panel.GetComponentsInChildren<UpgradeNodeHoverFx>(true).Length, Is.EqualTo(13));
                Assert.That(panel.GetComponentsInChildren<Transform>(true).Any(t => t.name == "DeepZoneText"), Is.False);
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        // ------------------------------------------------------------------ 보조

        private static Transform SurfacePanel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath);
            var panel = prefab.transform.Find(PanelPath);
            Assert.That(panel, Is.Not.Null);
            return panel;
        }

        private static Transform OpenModal(GameObject instance)
        {
            var panel = instance.transform.Find(PanelPath);
            panel.parent.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            return panel;
        }

        private static void AssertSettingsClose(RectTransform close)
        {
            Assert.That(close, Is.Not.Null);
            Assert.That(close.sizeDelta.x, Is.EqualTo(PromptB1181UpgradeWindowBuilder.CloseSize));
            Assert.That(close.sizeDelta.y, Is.EqualTo(PromptB1181UpgradeWindowBuilder.CloseSize), "기존 세로 길이 기준 정사각형");
            Assert.That(close.GetComponent<Image>().sprite.name, Is.EqualTo("setting-close-normal"));
            var hover = close.Find("HoverOverlay");
            Assert.That(hover, Is.Not.Null);
            Assert.That(hover.GetComponent<Image>().sprite.name, Is.EqualTo("setting-close-hover"));
            Assert.That(hover.GetComponent<Image>().raycastTarget, Is.False);
            var button = close.GetComponent<Button>();
            Assert.That(button.targetGraphic, Is.EqualTo(hover.GetComponent<Image>()));
            Assert.That(close.GetComponentInChildren<TMP_Text>(true), Is.Null, "글자 X 대신 설정창 X 그림을 쓴다");
            Assert.That(close.GetComponent<SurfaceBaseButtonFeedback>(), Is.Null);
        }

        private static void Step(UpgradeNodeHoverFx fx, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f)
            {
                fx.Tick(1f / 60f);
            }
        }

        private static UpgradeSnapshot Snapshot(string id, bool unlocked)
        {
            return new UpgradeSnapshot(
                id,
                id,
                1,
                3,
                1f,
                2f,
                new[] { new ItemCostDto(DataIds.Minerals.Copper, 1) },
                true,
                null,
                null,
                unlocked,
                unlocked ? null : "드릴 속도 Lv.2 필요",
                null,
                null,
                DataIds.Upgrades.DrillSpeed,
                2);
        }
    }
}
