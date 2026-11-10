using System;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.State;
using SubTerra.App.UI.Building;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB191FollowupTests
    {
        private const string MenuPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private GameObject menu;
        private BuildingMenuView view;
        private BuildingMenuBinder binder;
        private PlacementPort placement;
        private GameState state;

        [SetUp]
        public void SetUp()
        {
            menu = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath));
            menu.SetActive(true);
            view = menu.GetComponent<BuildingMenuView>();
            binder = menu.GetComponent<BuildingMenuBinder>();
            placement = new PlacementPort();
            state = GameState.CreateNew();
            binder.BindTo(null, null, state, placement);
            placement.CancelCount = 0;
            foreach (var entry in menu.GetComponentsInChildren<BuildingMenuEntryButton>(true))
                typeof(BuildingMenuEntryButton).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic).Invoke(entry, null);
            foreach (var row in menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true))
                typeof(BuildingMenuCatalogRowView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic).Invoke(row, null);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(menu);

        [Test]
        public void Rail_ReclickCancelsOnceAndMatchesPlacedScreen_OtherFacilitySwitches()
        {
            var ladder = Entry(DataIds.Buildings.LadderBasic);
            Click(ladder);
            Assert.That(Find("Card").activeSelf, Is.True);
            Assert.That(placement.PreviewId, Is.EqualTo(ladder.BuildingId));
            Click(ladder);
            Assert.That(placement.CancelCount, Is.EqualTo(1));
            AssertCleared();

            Click(ladder);
            placement.Place();
            AssertCleared();
            Click(ladder);
            Click(Entry(DataIds.Buildings.ChargerBasic));
            Assert.That(view.SelectedBuildingId, Is.EqualTo(DataIds.Buildings.ChargerBasic));
            Assert.That(placement.PreviewId, Is.EqualTo(DataIds.Buildings.ChargerBasic));
            Assert.That(Find("Card").activeSelf, Is.True);
        }

        [Test]
        public void Rail_ReclickInMoveModeUsesExistingCancelMoveWithSound()
        {
            var ladder = Entry(DataIds.Buildings.LadderBasic);
            Click(ladder);
            placement.IsMoveMode = true;
            Click(ladder);
            Assert.That(placement.MoveCancelCount, Is.EqualTo(1));
            Assert.That(placement.MoveCancelSound, Is.True);
            Assert.That(placement.CancelCount, Is.Zero);
            AssertCleared();
        }

        [Test]
        public void Catalog_ReclickStillSelectsAndOpensDetail()
        {
            var ladder = Entry(DataIds.Buildings.LadderBasic);
            Click(ladder);
            view.ToggleExpanded();
            var row = menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true)
                .Single(r => r.BuildingId == ladder.BuildingId);
            row.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(view.SelectedBuildingId, Is.EqualTo(ladder.BuildingId));
            Assert.That(view.Navigation.Page, Is.EqualTo(BuildingModePage.Detail));
            Assert.That(placement.BeginCount, Is.EqualTo(2));
        }

        [TestCase('ㄱ')]
        [TestCase('ㅅ')]
        [TestCase('ㅏ')]
        public void SearchFont_JamoHasActualAtlasPixels(char c)
        {
            var font = view.SearchField.fontAsset;
            Assert.That(font.HasCharacter(c, true), Is.True);
            var character = font.characterLookupTable[c];
            var rect = character.glyph.glyphRect;
            var pixels = font.atlasTextures[character.glyph.atlasIndex].GetPixels(rect.x, rect.y, rect.width, rect.height);
            Assert.That(pixels.Any(pixel => pixel.a > 0.2f), Is.True, "HasCharacter만으로 빈 아틀라스 칸을 검출할 수 없다.");
        }

        [TestCase("ㅅㄷ")]
        [TestCase("사ㄷ")]
        public void Search_DisplayedMixedQueryAndInitialResultsArePreserved(string query)
        {
            view.ToggleExpanded();
            view.SetSearchQuery(query);
            Assert.That(view.SearchField.text, Is.EqualTo(query));
            Assert.That(menu.GetComponentsInChildren<BuildingMenuCatalogRowView>(true)
                .Where(row => row.gameObject.activeSelf).Select(row => row.BuildingId),
                Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));
        }

        [Test]
        public void Card_UsesDedicatedSquareFrameAndKeepsRaycastsInsideCard()
        {
            var card = Find("Card");
            var frame = card.transform.Find("WindowFrame").GetComponent<UnityEngine.UI.Image>();
            var detailFrame = Find("CatalogWindow").transform.Find("WindowFrame").GetComponent<UnityEngine.UI.Image>();
            Assert.That(frame.sprite, Is.SameAs(detailFrame.sprite));
            Assert.That(AssetDatabase.GetAssetPath(frame.sprite), Is.EqualTo(
                "Assets/_Project/Art/UI/Gameplay/building-card-square.png"));
            Assert.That(frame.type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
            Assert.That(frame.raycastTarget, Is.False);
            Assert.That(frame.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout, Is.True);
            Assert.That(card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding.left, Is.EqualTo(16));
            Assert.That(Find("SidePane").GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(300f));
            Assert.That(card.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight, Is.EqualTo(300f));
            Assert.That(frame.rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(frame.rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(card.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Hover_ThreeTenthsUnscaledReversesAndKeepsSelection(bool reduceMotion)
        {
            var previousReduce = AccessibilityPreferences.ReduceMotion;
            var previousTimeScale = Time.timeScale;
            try
            {
                AccessibilityPreferences.ReduceMotion = reduceMotion;
                Time.timeScale = 0f;
                var ladder = Entry(DataIds.Buildings.LadderBasic);
                var relay = ladder.GetComponent<BuildingRailHoverRelay>();
                Click(ladder);
                relay.OnPointerEnter(null);
                var tag = view.RailHologram;
                Assert.That(ladder.HoverTransitionDurationSeconds, Is.EqualTo(0.3f));
                Assert.That(tag.HoverTransitionDurationSeconds, Is.EqualTo(0.3f));
                Advance(ladder, tag, 0.15f);
                var alpha = tag.GetComponent<CanvasGroup>().alpha;
                Assert.That(alpha, Is.EqualTo(0.5f).Within(0.001f));
                relay.OnPointerExit(null);
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.EqualTo(alpha));
                Advance(ladder, tag, 0.06f);
                Assert.That(ladder.HoverProgress, Is.EqualTo(0.3f).Within(0.001f));
                var closingAlpha = tag.GetComponent<CanvasGroup>().alpha;
                relay.OnPointerEnter(null);
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.EqualTo(closingAlpha));
                Advance(ladder, tag, 0.21f);
                Assert.That(ladder.HoverProgress, Is.EqualTo(1f).Within(0.001f));
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
                relay.OnPointerExit(null);
                Advance(ladder, tag, 0.3f);
                Assert.That(tag.IsShown, Is.False);
                Assert.That(ladder.HoverProgress, Is.Zero);
                Assert.That(ladder.IsSelected, Is.True);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                AccessibilityPreferences.ReduceMotion = previousReduce;
            }
        }

        [Test]
        public void Hover_FastSwitchFadesBothTagsAndExpandClearsEverything()
        {
            var ladder = Entry(DataIds.Buildings.LadderBasic);
            var charger = Entry(DataIds.Buildings.ChargerBasic);
            ladder.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
            var first = view.RailHologram;
            Advance(ladder, first, 0.15f);
            charger.GetComponent<BuildingRailHoverRelay>().OnPointerEnter(null);
            var second = view.RailHologram;
            Assert.That(second, Is.Not.SameAs(first));
            ladder.GetComponent<BuildingRailHoverRelay>().OnPointerExit(null);
            Advance(ladder, first, 0.1f);
            Advance(charger, second, 0.1f);
            Assert.That(first.HoverProgress, Is.LessThan(0.5f));
            Assert.That(second.HoverProgress, Is.GreaterThan(0f));
            Assert.That(second.IsClosing, Is.False, "이전 버튼의 늦은 Exit는 새 이름표를 끄지 않는다.");
            Assert.That(Find("HintChip").activeSelf, Is.False);
            view.ToggleExpanded();
            foreach (var tag in menu.GetComponentsInChildren<BuildingRailHologram>(true))
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(ladder.HoverProgress, Is.Zero);
            Assert.That(charger.HoverProgress, Is.Zero);
        }

        private static void Advance(BuildingMenuEntryVisual entry, BuildingRailHologram tag, float time)
        {
            entry.AdvanceHover(time);
            tag.Advance(time);
        }

        private void AssertCleared()
        {
            Assert.That(view.SelectedBuildingId, Is.Empty);
            Assert.That(binder.Presenter.SelectedBuildingId, Is.Empty);
            Assert.That(state.GetBuildingSelection().HasSelection, Is.False);
            Assert.That(placement.PreviewId, Is.Empty);
            Assert.That(Find("Card").activeSelf, Is.False);
            Assert.That(Find("HintChip").activeSelf, Is.True);
            Assert.That(Find("StatusChip").activeSelf, Is.False);
        }

        private BuildingMenuEntryVisual Entry(string id) => menu.GetComponentsInChildren<BuildingMenuEntryVisual>(true).Single(e => e.BuildingId == id);
        private static void Click(BuildingMenuEntryVisual entry) => entry.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        private GameObject Find(string name) => menu.GetComponentsInChildren<Transform>(true).First(t => t.name == name).gameObject;

        private sealed class PlacementPort : IBuildingPlacementPort, IBuildingFacilityMovePort
        {
            public event Action<BuildingPlacementResultDto> PlacementChanged;
            public event Action<string> MoveStatusChanged { add { } remove { } }
            public bool IsMoveMode { get; set; }
            public int BeginCount;
            public int CancelCount;
            public int MoveCancelCount;
            public bool MoveCancelSound;
            public string PreviewId = string.Empty;
            public bool BeginPreview(string id) { BeginCount++; PreviewId = id; return true; }
            public void CancelPreview() { CancelCount++; PreviewId = string.Empty; }
            public void BeginMoveSelection() => IsMoveMode = true;
            public void CancelMove(bool sound) { MoveCancelCount++; MoveCancelSound = sound; PreviewId = string.Empty; IsMoveMode = false; }
            public void Place()
            {
                var id = PreviewId;
                PreviewId = string.Empty;
                PlacementChanged?.Invoke(new BuildingPlacementResultDto { state = BuildingPlacementState.Placed, buildingId = id });
            }
        }
    }
}
