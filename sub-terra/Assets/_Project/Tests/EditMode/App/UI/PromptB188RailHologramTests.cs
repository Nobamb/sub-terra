using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Building;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-188 레일 호버: 아이콘 오른쪽 홀로그램 이름표(클릭 안 받음, 기본 레일에서만).</summary>
    public sealed class PromptB188RailHologramTests
    {
        private const string MenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [Test]
        public void Motion_UnfoldsFlickersTypesAndFades()
        {
            Assert.That(BuildingRailHologramMotion.ScaleX(0f), Is.EqualTo(BuildingRailHologramMotion.StartScaleX).Within(1e-4f));
            Assert.That(BuildingRailHologramMotion.ScaleX(BuildingRailHologramMotion.OpenDuration), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(BuildingRailHologramMotion.Alpha(0f), Is.EqualTo(0f).Within(1e-4f));

            // 펼치는 도중 한 번 어두워졌다가 다시 밝아진다.
            var open = BuildingRailHologramMotion.OpenDuration;
            var beforeFlicker = BuildingRailHologramMotion.Alpha(open * 0.3f);
            var inFlicker = BuildingRailHologramMotion.Alpha(open * 0.42f);
            var afterFlicker = BuildingRailHologramMotion.Alpha(open * 0.6f);
            Assert.That(inFlicker, Is.LessThan(beforeFlicker));
            Assert.That(afterFlicker, Is.GreaterThan(inFlicker));

            // 다 켜진 뒤 일렁임은 살짝만(0.94~1).
            for (var t = open; t < open + 2f; t += 0.05f)
            {
                var alpha = BuildingRailHologramMotion.Alpha(t);
                Assert.That(alpha, Is.InRange(1f - BuildingRailHologramMotion.ShimmerAmount - 1e-4f, 1f + 1e-4f));
            }

            Assert.That(BuildingRailHologramMotion.VisibleCharacters(0f, 5), Is.EqualTo(0));
            Assert.That(BuildingRailHologramMotion.VisibleCharacters(open, 5), Is.InRange(1, 4));
            Assert.That(BuildingRailHologramMotion.VisibleCharacters(open + BuildingRailHologramMotion.TypeDuration, 5), Is.EqualTo(5));
            Assert.That(BuildingRailHologramMotion.VisibleCharacters(10f, 0), Is.EqualTo(0));

            Assert.That(BuildingRailHologramMotion.ScanPosition(0f), Is.EqualTo(0f));
            Assert.That(BuildingRailHologramMotion.ScanPosition(BuildingRailHologramMotion.ScanPeriod * 0.5f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(BuildingRailHologramMotion.CloseAlpha(0f), Is.EqualTo(1f));
            Assert.That(BuildingRailHologramMotion.CloseAlpha(BuildingRailHologramMotion.CloseDuration), Is.EqualTo(0f));
        }

        [Test]
        public void Prefab_HologramIsTopmostAndNeverBlocksClicks()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var view = menu.GetComponent<BuildingMenuView>();
            var hologram = view.RailHologram;
            Assert.That(hologram, Is.Not.Null, "BuildingMenuView.railHologram");
            Assert.That(hologram.HasRequiredReferences(), Is.True);
            Assert.That(hologram.transform.parent.name, Is.EqualTo("PanelRoot"));
            Assert.That(hologram.transform.GetSiblingIndex(), Is.EqualTo(hologram.transform.parent.childCount - 1), "레일·카드 위에 그린다.");

            var group = hologram.GetComponent<CanvasGroup>();
            Assert.That(group.alpha, Is.EqualTo(0f), "기본은 보이지 않는다.");
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
            foreach (var graphic in hologram.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name + "가 클릭을 가로채면 안 된다.");
            }

            var entries = menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true);
            Assert.That(entries, Is.Not.Empty);
            foreach (var entry in entries)
            {
                var relay = entry.GetComponent<BuildingRailHoverRelay>();
                Assert.That(relay, Is.Not.Null, entry.name);
                Assert.That(new SerializedObject(relay).FindProperty("view").objectReferenceValue, Is.SameAs(view), entry.name);
            }
        }

        [Test]
        public void View_HoverShowsNameRightOfIconOnlyOnRail()
        {
            var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            try
            {
                var view = menu.GetComponent<BuildingMenuView>();
                var items = CatalogItems();
                view.SetBuildingList(items);
                view.ClearSelection();
                var hologram = view.RailHologram;
                var group = hologram.GetComponent<CanvasGroup>();
                var ladder = Entry(menu, DataIds.Buildings.LadderBasic);
                var charger = Entry(menu, DataIds.Buildings.ChargerBasic);
                var ladderName = items.First(i => i.BuildingId == DataIds.Buildings.LadderBasic).DisplayName;
                var chargerName = items.First(i => i.BuildingId == DataIds.Buildings.ChargerBasic).DisplayName;

                ladder.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
                Assert.That(hologram.IsShown, Is.True);
                Assert.That(hologram.CurrentName, Is.EqualTo(ladderName));
                Assert.That(group.blocksRaycasts, Is.False);
                var hint = menu.GetComponentsInChildren<Transform>(true).First(t => t.name == "HintChip").gameObject;
                Assert.That(hint.activeSelf, Is.False, "호버 중에는 힌트가 비켜 준다.");

                // 아이콘 오른쪽 가운데에 붙는다.
                var iconRect = (RectTransform)ladder.transform;
                var holoRect = (RectTransform)hologram.transform;
                var iconCorners = new Vector3[4];
                var holoCorners = new Vector3[4];
                iconRect.GetWorldCorners(iconCorners);
                hologram.Advance(1f);
                holoRect.GetWorldCorners(holoCorners);
                Assert.That(holoCorners[0].x, Is.GreaterThan(iconCorners[2].x), "이름표는 아이콘 오른쪽");
                var iconMidY = (iconCorners[0].y + iconCorners[1].y) * 0.5f;
                var holoMidY = (holoCorners[0].y + holoCorners[1].y) * 0.5f;
                Assert.That(holoMidY, Is.EqualTo(iconMidY).Within(0.5f), "아이콘과 같은 높이");
                Assert.That(group.alpha, Is.GreaterThan(0.9f));
                Assert.That(hologram.ScanVisible, Is.True, "다 켜지면 주사선이 흐른다.");

                // 다른 아이콘으로 옮긴 뒤 늦게 온 이전 아이콘의 나가기는 무시한다.
                charger.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
                ladder.GetComponent<BuildingRailHoverRelay>().OnPointerExit(null);
                Assert.That(hologram.IsShown && !hologram.IsClosing, Is.True);
                Assert.That(hologram.CurrentName, Is.EqualTo(chargerName));

                // 나가면 짧게 사라진다.
                charger.GetComponent<BuildingRailHoverRelay>().OnPointerExit(null);
                Assert.That(hologram.IsClosing, Is.True);
                hologram.Advance(1f);
                Assert.That(hologram.IsShown, Is.False);
                Assert.That(group.alpha, Is.EqualTo(0f));
                // B-191: 버튼을 떠난 직후에는 힌트를 바로 띄우지 않고 잠깐 기다린 뒤 돌려놓는다.
                Assert.That(hint.activeSelf, Is.False, "떠난 직후에는 아직 숨긴다.");
                view.AdvanceHintDelay(view.HintReshowDelaySeconds + 0.01f);
                Assert.That(hint.activeSelf, Is.True, "호버가 끝나고 대기가 지나면 힌트가 돌아온다.");

                // `>>` 큰 창에서는 목록이 이름을 보여 주므로 띄우지 않는다.
                ladder.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
                view.ToggleExpanded();
                Assert.That(hologram.IsShown, Is.False, "펼치면 바로 지운다.");
                ladder.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
                Assert.That(hologram.IsShown, Is.False);
                view.ToggleExpanded();

                // 창을 닫으면 남지 않는다.
                ladder.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
                view.SetVisible(false);
                Assert.That(hologram.IsShown, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menu);
            }
        }

        private static BuildingMenuEntryVisual Entry(GameObject menu, string buildingId)
        {
            return menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true).Single(e => e.BuildingId == buildingId);
        }

        private static List<BuildingMenuItemReadModel> CatalogItems()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            return catalog.Buildings
                .Where(b => b != null)
                .Select(b => new BuildingMenuItemReadModel(
                    b.Id, b.DisplayName, b.Description, b.Icon, b.PowerDraw,
                    b.BuildCosts.Select(c => new BuildingCostReadModel(c.ItemId, c.Quantity, 0)).ToList()))
                .ToList();
        }
    }
}
