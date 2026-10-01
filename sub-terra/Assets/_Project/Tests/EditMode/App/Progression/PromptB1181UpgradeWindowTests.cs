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
        public void SurfaceWindow_HasNoDeepZoneStatusLine()
        {
            var panel = SurfacePanel();
            Assert.That(panel.GetComponentsInChildren<Transform>(true).Any(t => t.name == "DeepZoneText"), Is.False);
            var texts = panel.Find(PromptB118UpgradeTreeBuilder.TreeRootName).GetComponentsInChildren<TMP_Text>(true);
            Assert.That(texts.Any(t => t.text != null && t.text.Contains("심층 구역")), Is.False);
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
                Assert.That(panel.GetComponentsInChildren<UpgradeNodeHoverFx>(true).Length, Is.EqualTo(11));
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
