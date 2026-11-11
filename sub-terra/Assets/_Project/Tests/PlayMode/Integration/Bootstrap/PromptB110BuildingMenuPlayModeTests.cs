using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 110 → B-188: 실제 Integration Scene에서 아이콘 레일 건설 모드의
    /// 선택·카드·비용 갱신·`>>` 목록·검색·상세·뒤로가기·B 닫기·해상도별 배치와 클릭 우선순위를 확인한다.
    /// </summary>
    public sealed class PromptB110BuildingMenuPlayModeTests
    {
        private UiTestEnvironment environment;
        private Keyboard keyboard;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildingMenu_SelectionCostsAvailabilityAndLayout()
        {
            yield return VerifyStatesAndInput(false);
        }

        [UnityTest, Category("Visual")]
        public IEnumerator BuildingMenu_StatesAndLayout_AtThreeResolutions()
        {
            yield return VerifyStatesAndInput(true);
        }

        /// <summary>B-191-3: 목록을 펼친 채 레일에서 아래쪽 시설을 누르면 목록 스크롤이 그 행을 가운데(끝 쪽은 맨 아래)로 옮긴다.</summary>
        [UnityTest]
        public IEnumerator BuildingCatalog_RailSelectionScrollsRowIntoView()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            Object.FindAnyObjectByType<HudPanelChromeController>().OpenBuildingMenu();
            yield return new WaitForSecondsRealtime(BuildingModeSlideMotion.OpenDuration + 0.05f);
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var root = view.transform;
            yield return ClickCenter(environment.Mouse, view.ExpandButton);
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            var scroll = view.CatalogScroll;
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(1e-3f), "처음엔 맨 위");
            var evidence = Path.GetFullPath("Temp/visual/prompt-b191-3");
            Directory.CreateDirectory(evidence);

            foreach (var id in new[] { DataIds.Buildings.StorageBasic, DataIds.Buildings.SettlementBasic, DataIds.Buildings.EmergencyEscapePortal })
            {
                yield return ClickCenter(environment.Mouse, Entry(root, id).GetComponent<Button>());
                yield return null;
                Assert.That(view.SelectedBuildingId, Is.EqualTo(id));
                var row = ScreenRect((RectTransform)Find(root, "Row_" + id));
                var viewport = ScreenRect(scroll.viewport);
                Assert.That(row.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f), id + " 아래가 잘리지 않는다.");
                Assert.That(row.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f), id + " 위가 잘리지 않는다.");
                if (id == DataIds.Buildings.StorageBasic)
                    Assert.That(row.center.y, Is.EqualTo(viewport.center.y).Within(1.5f), "보관함은 가운데");
                else
                    Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0f).Within(1e-3f), id + "는 맨 아래");
                yield return UiTestWait.Capture(Path.Combine(evidence, "catalog-focus-" + id + ".png"));
            }
        }

        [UnityTest]
        public IEnumerator BuildingSearch_CommittedKeyboardTextAndImeCompositionStayInSync()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            Object.FindAnyObjectByType<HudPanelChromeController>().OpenBuildingMenu();
            yield return new WaitForSecondsRealtime(0.6f);
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            view.ToggleExpanded();
            yield return new WaitForSecondsRealtime(0.5f);
            EventSystem.current.SetSelectedGameObject(view.SearchField.gameObject);
            view.SearchField.ActivateInputField();
            yield return null;

            foreach (var character in "사다리") InputSystem.QueueTextEvent(environment.Keyboard, character);
            yield return null;
            yield return null;
            Assert.That(view.SearchField.text, Is.EqualTo("사다리"), "확정된 키보드 문자를 TMP 입력 문자열에 반영한다.");
            Assert.That(view.Navigation.Query, Is.EqualTo("사다리"));
            Assert.That(view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false).Select(row => row.BuildingId),
                Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));

            // 두 입력 경로가 같은 문자를 주는 환경에서도 한 번만 입력한다.
            InputSystem.QueueTextEvent(environment.Keyboard, 'a');
            InputSystem.Update();
            view.SearchField.ProcessEvent(new Event { type = EventType.KeyDown, character = 'a' });
            yield return null;
            Assert.That(view.SearchField.text, Is.EqualTo("사다리a"));
            view.SearchField.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Backspace });
            Assert.That(view.SearchField.text, Is.EqualTo("사다리"));

            view.SetSearchQuery(string.Empty);
            var module = EventSystem.current.currentInputModule;
            var previousInput = module.inputOverride;
            var composition = view.gameObject.AddComponent<InventorySearchCompositionInput>();
            module.inputOverride = composition;
            try
            {
                foreach (var partial in new[] { "ㅅ", "사" })
                {
                    composition.Composition = partial;
                    view.SearchField.ForceLabelUpdate();
                    yield return null;
                    yield return null;
                    Assert.That(view.SearchField.text, Is.Empty);
                    Assert.That(view.Navigation.Query, Is.EqualTo(partial));
                }
                var committed = new[] { '사', '다', '리' };
                var composing = new[] { "다", "리", "" };
                var expected = new[] { "사", "사다", "사다리" };
                for (var i = 0; i < committed.Length; i++)
                {
                    composition.Composition = composing[i];
                    InputSystem.QueueTextEvent(environment.Keyboard, committed[i]);
                    yield return null;
                    yield return null;
                    Assert.That(view.SearchField.text, Is.EqualTo(expected[i]));
                    Assert.That(view.Navigation.Query, Is.EqualTo(expected[i] + composing[i]));
                }
            }
            finally
            {
                composition.Composition = string.Empty;
                module.inputOverride = previousInput;
                Object.DestroyImmediate(composition);
            }

            InputSystem.QueueTextEvent(environment.Keyboard, 'x');
            InputSystem.Update();
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            EventSystem.current.SetSelectedGameObject(view.SearchField.gameObject);
            view.SearchField.ActivateInputField();
            yield return null;
            Assert.That(view.SearchField.text, Is.EqualTo("사다리"), "포커스를 잃은 큐는 다음 입력에 섞이지 않는다.");
        }

        [UnityTest]
        public IEnumerator BuildingMode_FollowupReclickCompositionAndUnscaledHover()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            Object.FindAnyObjectByType<HudPanelChromeController>().OpenBuildingMenu();
            yield return new WaitForSecondsRealtime(0.5f);
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            var ladder = view.GetComponentsInChildren<BuildingMenuEntryVisual>(true)
                .Single(e => e.BuildingId == DataIds.Buildings.LadderBasic);
            var card = Find(view.transform, "Card");
            var evidence = "Builds/B191-1/evidence";
            Directory.CreateDirectory(evidence);
            yield return ClickCenter(environment.Mouse, ladder.GetComponent<Button>());
            Assert.That(card.gameObject.activeInHierarchy, Is.True);
            // B-191-3: 카드는 TV 켜짐으로 나타나므로 다 켜진 뒤 찍는다.
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            yield return UiTestWait.Capture(Path.Combine(evidence, "small-card.png"));
            yield return ClickCenter(environment.Mouse, ladder.GetComponent<Button>());
            Assert.That(view.SelectedBuildingId, Is.Empty);
            Assert.That(Object.FindAnyObjectByType<BuildingPlacementSystem>().Selection, Is.Null);
            Assert.That(view.IsCardShown, Is.False);
            // 재클릭 해제는 TV 꺼짐으로 사라진다.
            yield return null;
            var cardMotion = card.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(cardMotion != null && cardMotion.IsClosing, Is.True, "카드 TV 꺼짐");
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.CloseDuration + 0.05f);
            Assert.That(card.gameObject.activeSelf, Is.False);
            yield return UiTestWait.Capture(Path.Combine(evidence, "reclick-cleared.png"));

            // Update가 실제로 unscaled 시간을 사용해 정지 중에도 양방향 끝까지 진행한다.
            InputState.Change(environment.Mouse.position, new Vector2(Screen.width - 4f, Screen.height - 4f));
            yield return null;
            var relay = ladder.GetComponent<BuildingRailHoverRelay>();
            relay.OnPointerExit(null);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(view.RailHologram.IsShown, Is.False);
            var scale = Time.timeScale;
            var reduced = SubTerra.Shared.AccessibilityPreferences.ReduceMotion;
            try
            {
                Time.timeScale = 0f;
                SubTerra.Shared.AccessibilityPreferences.ReduceMotion = true;
                relay.OnPointerEnter(null);
                var tag = view.RailHologram;
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.InRange(0.05f, 0.99f));
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(tag.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(ladder.HoverProgress, Is.EqualTo(1f));
                relay.OnPointerExit(null);
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(tag.IsShown, Is.False);
                Assert.That(ladder.HoverProgress, Is.Zero);
            }
            finally
            {
                Time.timeScale = scale;
                SubTerra.Shared.AccessibilityPreferences.ReduceMotion = reduced;
            }

            view.ToggleExpanded();
            yield return new WaitForSecondsRealtime(0.5f);
            EventSystem.current.SetSelectedGameObject(view.SearchField.gameObject);
            view.SearchField.ActivateInputField();
            yield return null;
            var module = EventSystem.current.currentInputModule;
            var previousInput = module.inputOverride;
            var composition = view.gameObject.AddComponent<InventorySearchCompositionInput>();
            module.inputOverride = composition;
            try
            {
                foreach (var query in new[] { "ㅅ", "ㅅㄷ", "사ㄷ" })
                {
                    composition.Composition = query;
                    view.SearchField.ForceLabelUpdate();
                    yield return null;
                    yield return null;
                    Assert.That(view.SearchField.text, Is.Empty, "IME 미확정 문자열을 입력 필드에 다시 쓰지 않는다.");
                    Assert.That(view.SearchField.textComponent.text.TrimEnd('\u200B'), Is.EqualTo(query));
                    Assert.That(view.Navigation.Query, Is.EqualTo(query));
                    Assert.That(view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false)
                        .Any(r => r.BuildingId == DataIds.Buildings.LadderBasic), Is.True);
                    yield return UiTestWait.Capture(Path.Combine(evidence, "composition-" + query + ".png"));
                }
            }
            finally
            {
                composition.Composition = string.Empty;
                module.inputOverride = previousInput;
                Object.DestroyImmediate(composition);
            }
            Assert.That(binder.IsBound, Is.True);
        }

        [UnityTest]
        public IEnumerator BuildingMenu_ConstructingPoweredFacilityDoesNotSpendPlayerPower()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var state = GameBootstrapper.Instance.State;
            var inventory = environment.Save.InventoryService;
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper, 10).DidChange, Is.True);
            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            chrome.OpenBuildingMenu();
            yield return null;
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            Assert.That(binder.SelectBuilding(DataIds.Buildings.ChargerBasic), Is.True);
            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            Assert.That(placement.TryFindBestPlacementCell(1f, out var cell, out var failure), Is.True, failure.ToString());
            var energyBefore = state.Player.Energy;
            var copperBefore = inventory.State.GetQuantity(DataIds.Minerals.Copper);
            var copperCost = placement.Selection.Costs[0].Quantity;
            var result = placement.TryPlaceAt(cell);

            Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
            Assert.That(state.Player.Energy, Is.EqualTo(energyBefore), "건설 자체는 플레이어 전력을 차감하지 않는다.");
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(copperBefore - copperCost));
            // B-191-3: 설치 완료 뒤 카드는 TV 꺼짐이 끝나고 나서 내용을 비운다.
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.CloseDuration + 0.1f);
            Assert.That(Find(view.transform, "Card").gameObject.activeSelf, Is.False);
            Assert.That(Find(view.transform, "DetailOperatingCondition").gameObject.activeSelf, Is.False);
            Assert.That(environment.Save.ActiveSlot, Is.Zero);
        }

        /// <summary>B-188 E: 건설 대상을 고른 채로 지형 블록을 누르면 채굴, 빈 땅을 누르면 설치.</summary>
        [UnityTest]
        public IEnumerator BuildingMode_ClickOnTerrainMinesAndEmptyGroundPlaces()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var inventory = environment.Save.InventoryService;
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper, 10).DidChange, Is.True);
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Iron, 10).DidChange, Is.True);
            yield return UiTestWait.Press(environment.Keyboard, Key.B);
            yield return new WaitForSecondsRealtime(BuildingModeSlideMotion.OpenDuration + 0.05f);
            yield return null;
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var bridge = Object.FindAnyObjectByType<GameplayBuildingPlacementBridge>();
            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            var mining = Object.FindAnyObjectByType<MiningSystem>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.That(bridge != null && placement != null && mining != null && player != null, Is.True);
            var railButton = Entry(view.transform, DataIds.Buildings.SupportBasic).GetComponent<Button>();
            yield return ClickCenter(environment.Mouse, railButton);
            Assert.That(placement.Selection, Is.Not.Null);
            yield return new WaitForSecondsRealtime(0.4f);
            var card = (RectTransform)Find(view.transform, "Card");
            var status = (RectTransform)Find(view.transform, "StatusChip");
            view.SetStatusMessage("검증용 설치 상태");
            Canvas.ForceUpdateCanvases();
            Assert.That(card.gameObject.activeInHierarchy && status.gameObject.activeInHierarchy, Is.True);
            Assert.That(card.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(status.GetComponent<Image>().raycastTarget, Is.False);
            var camera = Camera.main;
            var cameraPosition = camera.transform.position;
            var cameraFollow = camera.GetComponent<PlayerCameraFollow>();
            bool wasFollowing = cameraFollow != null && cameraFollow.enabled;

            // 플레이어 옆·아래의 지형을 정보 카드 아래에 오도록 카메라를 맞춘다.
            var terrain = mining.ForegroundTilemap;
            var playerCell = terrain.WorldToCell(player.Position);
            var playerMining = player.GetComponent<PlayerMiningController>();
            var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
            Assert.That(resolver.TryFindTileById("tile.rock.normal", out var rock), Is.True);
            var candidates = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up }
                .Select(offset => playerCell + offset);
            Vector3Int? blockCell = null;
            foreach (var candidate in candidates)
            {
                // 입구 바닥은 보호되어 있으므로, 빈 인접 칸에 테스트용 일반 암석을 둔다.
                bool wasEmpty = !terrain.HasTile(candidate);
                if (wasEmpty) terrain.SetTile(candidate, rock);
                if (playerMining.CanPrioritizePointerMining(terrain.GetCellCenterWorld(candidate)))
                {
                    blockCell = candidate;
                    break;
                }

                if (wasEmpty) terrain.SetTile(candidate, null);
            }
            Assert.That(blockCell.HasValue, Is.True, "플레이어 옆에 실제로 캘 수 있는 블록이 없습니다.");
            var rejected = 0;
            var placed = 0;
            void OnRejected(BuildingPlacementResult _) => rejected++;
            void OnPlaced(BuildingPlacementResult _) => placed++;
            var miningAttempts = 0;
            void OnMining(MiningProgressState progress)
            {
                if (progress.Phase == MiningPhase.Mining) miningAttempts++;
            }

            IEnumerator ClickControlWithoutWorldInput(RectTransform control, Mouse mouse)
            {
                var screen = AlignWorldToPanel(terrain.GetCellCenterWorld(blockCell.Value), control);
                Assert.That(IsOverUi(screen), Is.True, control.name + " has no UI raycast");
                int oldMining = miningAttempts;
                int oldPlaced = placed;
                int oldRejected = rejected;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 1 });
                yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "mouse down " + control.name, 2f, "input");
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
                yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "mouse up " + control.name, 2f, "input");
                yield return null;
                Assert.That(miningAttempts, Is.EqualTo(oldMining), control.name + " started mining");
                Assert.That(placed, Is.EqualTo(oldPlaced), control.name + " placed a facility");
                Assert.That(rejected, Is.EqualTo(oldRejected), control.name + " started a placement attempt");
            }

            placement.PlacementRejected += OnRejected;
            placement.BuildingPlaced += OnPlaced;
            mining.ProgressChanged += OnMining;
            try
            {
                var mouse = environment.Mouse;
                yield return UiTestWait.FocusGameView();
                if (!mouse.enabled) InputSystem.EnableDevice(mouse);
                mouse.MakeCurrent();
                if (cameraFollow != null) cameraFollow.enabled = false;
                var blockScreen = AlignWorldToPanel(terrain.GetCellCenterWorld(blockCell.Value), card);
                Assert.That(IsOverUi(blockScreen), Is.False, "시설 카드가 채굴 클릭을 받았습니다.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = blockScreen });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = blockScreen, buttons = 1 });
                yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "mouse down terrain", 2f, "input");
                Assert.That(bridge.OwnsPointerInput, Is.False, "지형 블록 위 누르기는 설치가 아니라 채굴이 가져간다.");
                // 보호 칸의 실패 알림과 구분해 누르고 있는 동안 실제 채굴이 진행되는지 확인한다.
                yield return UiTestWait.Until(() => miningAttempts > 0, "mining attempt on terrain", 3f);
                Assert.That(miningAttempts, Is.GreaterThan(0), "terrain click did not reach mining: cell=" + blockCell
                    + " failure=" + mining.LastFailure + " owns=" + bridge.OwnsPointerInput);
                Assert.That(rejected, Is.Zero, "지형 클릭이 설치 실패(겹침)로 처리되면 안 된다.");
                Assert.That(placement.Selection, Is.Not.Null, "채굴해도 건설 대상은 유지된다.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = blockScreen });
                yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "mouse up terrain", 2f, "input");
                yield return null;

                // 설치 가능한 빈 땅: 같은 좌클릭이 설치로 간다.
                Assert.That(placement.TryFindBestPlacementCell(1f, out var cell, out var failure), Is.True, failure.ToString());
                view.SetStatusMessage("검증용 설치 상태");
                Canvas.ForceUpdateCanvases();
                var groundScreen = AlignWorldToPanel(terrain.GetCellCenterWorld(cell), status);
                Assert.That(IsOverUi(groundScreen), Is.False, "설치 칸이 UI에 가려졌습니다.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = groundScreen });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = groundScreen, buttons = 1 });
                yield return UiTestWait.Until(() => placed > 0 || rejected > 0, "placement on empty ground", 2f);
                Assert.That(placed, Is.EqualTo(1), "빈 땅 클릭은 기존 설치 판정으로 설치한다.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = groundScreen });
                yield return null;
                if (!terrain.HasTile(blockCell.Value)) terrain.SetTile(blockCell.Value, rock);
                yield return ClickControlWithoutWorldInput((RectTransform)railButton.transform, mouse);
                yield return ClickControlWithoutWorldInput((RectTransform)view.ExpandButton.transform, mouse);
                yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
                yield return ClickControlWithoutWorldInput((RectTransform)view.SearchField.transform, mouse);
                Assert.That(view.SearchField.isFocused, Is.True);
                var row = view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false).First();
                yield return ClickControlWithoutWorldInput((RectTransform)row.transform, mouse);
            }
            finally
            {
                camera.transform.position = cameraPosition;
                if (cameraFollow != null) cameraFollow.enabled = wasFollowing;
                placement.PlacementRejected -= OnRejected;
                placement.BuildingPlaced -= OnPlaced;
                mining.ProgressChanged -= OnMining;
            }
        }

        private static Vector2 AlignWorldToPanel(Vector3 world, RectTransform panel)
        {
            var camera = Camera.main;
            var target = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center));
            var projected = camera.WorldToScreenPoint(world);
            var targetWorld = camera.ScreenToWorldPoint(new Vector3(target.x, target.y, projected.z));
            camera.transform.position += world - targetWorld;
            return target;
        }

        private IEnumerator VerifyStatesAndInput(bool visual)
        {
            environment = new UiTestEnvironment();
            keyboard = environment.Keyboard;
            yield return environment.LoadIntegration();
            var save = environment.Save;

            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            chrome.OpenBuildingMenu();
            yield return new WaitForSecondsRealtime(BuildingModeSlideMotion.OpenDuration + 0.05f);
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            Assert.That(view, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(view.HasStructuredReferences(), Is.True);
            Assert.That(view.HasBuildingModeReferences(), Is.True);

            var root = view.transform;
            var card = Find(root, "Card").gameObject;
            var catalogWindow = Find(root, "CatalogWindow").gameObject;
            var detailName = Find(root, "DetailName").GetComponent<TMP_Text>();
            var availability = Find(root, "AvailabilityText").GetComponent<TMP_Text>();
            var costSection = Find(root, "CostSection").gameObject;
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.False);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "HintChip"), Is.False,
                "B-191-3: 미선택이면 안내 문구 없이 레일만");
            Assert.That(card.activeSelf, Is.False);
            Assert.That(catalogWindow.activeSelf, Is.False, "기본은 레일+카드");
            AssertNoControlGuide(root);

            var evidence = Path.GetFullPath("Temp/visual/prompt-b188");
            if (visual) Directory.CreateDirectory(evidence);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-rail-idle-1920x1080.png"));

            // 레일 호버: 아이콘 오른쪽에 홀로그램 이름표. 클릭은 막지 않는다(아래 ClickCenter가 raycast 첫 대상을 확인).
            var support = Entry(root, DataIds.Buildings.SupportBasic);
            var hologram = view.RailHologram;
            Assert.That(hologram, Is.Not.Null);
            yield return MoveMouseTo(environment.Mouse, (RectTransform)support);
            yield return new WaitForSecondsRealtime(view.RailHologram.HoverTransitionDurationSeconds + 0.05f);
            Assert.That(hologram.IsShown && !hologram.IsClosing, Is.True, "호버 이름표");
            Assert.That(hologram.CurrentName, Is.Not.Empty);
            Assert.That(hologram.CurrentName, Is.EqualTo(Find(Find(root, "Row_" + DataIds.Buildings.SupportBasic), "Name").GetComponent<TMP_Text>().text),
                "목록·카드와 같은 시설명");
            var hologramRect = ScreenRect((RectTransform)hologram.transform);
            var supportRect = ScreenRect((RectTransform)support);
            Assert.That(hologramRect.xMin, Is.GreaterThan(supportRect.xMax), "아이콘 오른쪽");
            Assert.That(hologramRect.center.y, Is.EqualTo(supportRect.center.y).Within(1f));
            Assert.That(hologramRect.xMax, Is.LessThanOrEqualTo(Screen.width));
            var holoText = Find(hologram.transform, "NameText").GetComponent<TMP_Text>();
            holoText.ForceMeshUpdate();
            Assert.That(holoText.isTextTruncated, Is.False);
            Assert.That(holoText.maxVisibleCharacters, Is.GreaterThanOrEqualTo(holoText.text.Length), "글자가 다 찍혔다.");
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-rail-hover-1920x1080.png"));

            // B-191: 건설 UI 전용 Canvas가 드론 말풍선 위에 있고, HUDCanvas 순서는 그대로다.
            Assert.That(view.ModeCanvas, Is.Not.Null);
            Assert.That(view.ModeCanvas.overrideSorting, Is.True);
            Assert.That(view.ModeCanvas.sortingOrder, Is.EqualTo(BuildingMenuView.CanvasSortingOrder));
            Assert.That(view.ModeCanvas.sortingOrder, Is.GreaterThan(DroneDialogueSocket.OverlaySortingOrder));
            Assert.That(view.ModeCanvas.rootCanvas, Is.Not.SameAs(view.ModeCanvas), "HUDCanvas 아래에 그대로 있다.");
            Assert.That(view.ModeCanvas.rootCanvas.sortingOrder, Is.LessThan(DroneDialogueSocket.OverlaySortingOrder),
                "HUDCanvas 전체 순서는 말풍선 위로 올리지 않는다.");
            Assert.That(card.GetComponentInParent<Canvas>(), Is.SameAs(view.ModeCanvas), "카드도 같은 Canvas");

            // 버팀목 선택: 초기 인벤토리는 비어 있어 자원 부족이 문구·기호와 함께 보여야 한다.
            yield return ClickCenter(environment.Mouse, support.GetComponent<Button>());
            Assert.That(binder.Presenter.SelectedBuildingId, Is.EqualTo(DataIds.Buildings.SupportBasic));
            Assert.That(support.GetComponent<BuildingMenuEntryVisual>().IsSelected, Is.True);
            AssertOnlySelected(root, DataIds.Buildings.SupportBasic);
            Assert.That(card.activeSelf, Is.True);
            // B-191-3: 카드도 큰 창과 같은 TV 켜짐으로 나타난다.
            var cardMotion = card.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(cardMotion, Is.Not.Null, "카드 TV 켜짐");
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            Assert.That(cardMotion.IsPlaying, Is.False);
            Assert.That(card.transform.localScale.y, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(detailName.text, Is.Not.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.True);
            Assert.That(availability.text, Does.Contain("자원 부족"));
            Assert.That(Find(root, "AvailabilityGlyph").GetComponent<TMP_Text>().text,
                Is.EqualTo(BuildingMenuDisplayFormatter.BlockedGlyph));
            AssertNoControlGuide(root);
            AssertInsideScreenAndClearOfHud(view);
            AssertNotTruncated(root);
            if (visual)
            {
                // 카드가 열린 상태에서 다른 아이콘 호버(이름표가 카드 위에 겹치는 경우) 모습.
                yield return MoveMouseTo(environment.Mouse, (RectTransform)Entry(root, DataIds.Buildings.LadderBasic));
                yield return new WaitForSecondsRealtime(view.RailHologram.HoverTransitionDurationSeconds + 0.05f);
                Assert.That(view.RailHologram.IsShown, Is.True);
                Assert.That(card.activeSelf, Is.True, "카드는 호버로 숨지 않는다.");
                yield return UiTestWait.Capture(Path.Combine(evidence, "building-rail-hover-card-1920x1080.png"));
                yield return MoveMouseTo(environment.Mouse, (RectTransform)support);
            }

            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-card-need-resources-1920x1080.png"));

            // 자원을 얻으면 비용 행·상태가 즉시 바뀌어야 한다.
            var added = save.InventoryService.TryAddMineralExact(DataIds.Minerals.Copper, 6);
            Assert.That(added.DidChange, Is.True, "copper add");
            yield return null;
            var row0 = Find(root, "CostRow_0").Find("State").GetComponent<TMP_Text>();
            Assert.That(row0.text, Is.EqualTo("충분"));
            Assert.That(Find(root, "CostRow_0").Find("Amount").GetComponent<TMP_Text>().text, Does.Contain(" / "));
            Assert.That(availability.text, Does.Not.Contain("자원 부족"));
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-card-affordable-1920x1080.png"));

            // 비용 2종 중 일부만 부족한 시설: 부족한 광물만 경고 표시.
            var core = Entry(root, DataIds.Buildings.OutpostCoreBasic);
            core.GetComponent<Button>().onClick.Invoke();
            yield return null;
            AssertOnlySelected(root, DataIds.Buildings.OutpostCoreBasic);
            Assert.That(Find(root, "CostRow_0").gameObject.activeSelf, Is.True);
            Assert.That(Find(root, "CostRow_1").gameObject.activeSelf, Is.True);
            Assert.That(Find(root, "CostRow_2").gameObject.activeSelf, Is.False);
            Assert.That(Find(root, "CostRow_1").Find("State").GetComponent<TMP_Text>().text, Does.EndWith("부족"));
            Assert.That(availability.text, Does.Contain("자원 부족"));
            AssertNotTruncated(root);

            var condition = Find(root, "DetailOperatingCondition").GetComponent<TMP_Text>();
            foreach (var id in new[]
            {
                DataIds.Buildings.ChargerBasic, DataIds.Buildings.ClinicBasic, DataIds.Buildings.SettlementBasic
            })
            {
                Entry(root, id).GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(condition.gameObject.activeSelf, Is.True, id);
                Assert.That(condition.text, Is.EqualTo(ItemDisplayNames.OutpostName + " 범위 내"), id);
                AssertNotTruncated(root);
            }

            if (visual)
            foreach (var resolution in new[] { new Vector2Int(2560, 1440), new Vector2Int(1366, 768), new Vector2Int(1920, 1080) })
            {
                yield return environment.Resolution.Set(resolution.x, resolution.y);
                Assert.That(Screen.width, Is.EqualTo(resolution.x));
                AssertInsideScreenAndClearOfHud(view);
                AssertNotTruncated(root);
                yield return UiTestWait.Capture(Path.Combine(evidence, $"building-card-{resolution.x}x{resolution.y}.png"));
            }

            // `>>`: 레일은 그대로, 옆에 큰 목록과 검색창.
            yield return ClickCenter(environment.Mouse, view.ExpandButton);
            Assert.That(catalogWindow.activeSelf, Is.True);
            Assert.That(Find(root, "ListPage").gameObject.activeSelf, Is.True);
            Assert.That(Find(root, "Rail").gameObject.activeInHierarchy, Is.True);
            Assert.That(card.activeSelf, Is.False);
            Assert.That(Find(view.ExpandButton.transform, "Label").GetComponent<TMP_Text>().text, Is.EqualTo("<<"));
            Assert.That(hologram.IsShown, Is.False, "큰 창을 펼치면 호버 이름표는 없다.");
            // B-191: 큰 창은 TV 켜짐으로 나타난다. 연출 중에도 입력은 막지 않는다.
            var windowMotion = catalogWindow.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(windowMotion, Is.Not.Null, "큰 창 TV 켜짐");
            Assert.That(windowMotion.IsPlaying && !windowMotion.IsClosing, Is.True);
            Assert.That(catalogWindow.GetComponent<CanvasGroup>().interactable, Is.True);
            if (visual)
            {
                yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration * 0.5f);
                yield return UiTestWait.Capture(Path.Combine(evidence, "building-catalog-opening-1920x1080.png"));
            }

            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            Assert.That(windowMotion.IsPlaying, Is.False);
            Assert.That(catalogWindow.transform.localScale, Is.EqualTo(Vector3.one));
            AssertInsideScreenAndClearOfHud(view, (RectTransform)catalogWindow.transform);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-catalog-1920x1080.png"));

            // B-191: 정렬 드롭다운. 펼친 목록은 큰 창 위에 그려지고, 펼친 동안 B·C·이동 키가 월드로 새지 않는다.
            var railOrder = root.GetComponentsInChildren<BuildingMenuEntryVisual>(true).Select(v => v.BuildingId).ToList();
            var defaultRows = view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false).Select(r => r.BuildingId).ToList();
            view.SortDropdown.Show();
            yield return null;
            yield return null;
            Assert.That(view.SortDropdown.IsExpanded, Is.True);
            var sortList = view.SortDropdown.transform.Find("Dropdown List");
            Assert.That(sortList, Is.Not.Null, "펼친 정렬 목록");
            Assert.That(sortList.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(view.ModeCanvas.sortingOrder));
            Assert.That(sortList.GetComponentsInChildren<Toggle>(false).Length, Is.EqualTo(BuildingCatalogQuery.SortCount));
            Assert.That(UiPauseGate.IsHeldBy(BuildingMenuView.SearchPauseOwner), Is.True, "정렬 목록을 펼친 동안 입력 차단");
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.True, "정렬 목록을 펼친 동안 B는 건설 모드를 닫지 않는다.");
            if (visual)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                yield return UiTestWait.Capture(Path.Combine(evidence, "building-catalog-sort-open-1920x1080.png"));
            }

            view.SortDropdown.Hide();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(UiPauseGate.IsHeldBy(BuildingMenuView.SearchPauseOwner), Is.False);
            view.SortDropdown.value = (int)BuildingCatalogSort.NameAscending;
            yield return null;
            Assert.That(view.Navigation.Sort, Is.EqualTo(BuildingCatalogSort.NameAscending));
            var sortedNames = view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false)
                .Select(r => Find(r.transform, "Name").GetComponent<TMP_Text>().text).ToList();
            Assert.That(sortedNames, Is.Ordered.Using<string>(string.CompareOrdinal), "가나다순");
            Assert.That(root.GetComponentsInChildren<BuildingMenuEntryVisual>(true).Select(v => v.BuildingId), Is.EqualTo(railOrder),
                "레일 순서는 정렬과 무관");
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-catalog-sort-name-1920x1080.png"));
            view.SortDropdown.value = (int)BuildingCatalogSort.Default;
            yield return null;
            Assert.That(view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false).Select(r => r.BuildingId), Is.EqualTo(defaultRows));

            // B-191: 초성만 쳐도 걸러진다.
            view.SetSearchQuery("ㅅㄷ");
            yield return null;
            Assert.That(view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false).Select(r => r.BuildingId),
                Is.EqualTo(new[] { DataIds.Buildings.LadderBasic }));

            // 검색: 시설명 일부만 남기고, 지우면 전체.
            view.SetSearchQuery("충전");
            yield return null;
            var visibleRows = view.GetComponentsInChildren<BuildingMenuCatalogRowView>(false);
            Assert.That(visibleRows.Select(r => r.BuildingId), Is.EqualTo(new[] { DataIds.Buildings.ChargerBasic }));
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-catalog-search-1920x1080.png"));

            // 목록에서 충전기 → 상세 + 레일 선택·설치 대상 동기화.
            yield return ClickCenter(environment.Mouse, visibleRows[0].GetComponent<Button>());
            Assert.That(Find(root, "DetailPage").gameObject.activeSelf, Is.True);
            // B-191: 상세 페이지에만 짧게 TV 켜짐. 큰 창은 다시 켜지 않는다.
            Assert.That(Find(root, "DetailPage").GetComponent<TerminalWindowOpenMotion>(), Is.Not.Null);
            Assert.That(windowMotion.IsPlaying, Is.False);
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration / BuildingMenuView.DetailMotionSpeed + 0.08f);
            Assert.That(Find(root, "DetailPage").GetComponent<TerminalWindowOpenMotion>().IsPlaying, Is.False);
            Assert.That(binder.Presenter.SelectedBuildingId, Is.EqualTo(DataIds.Buildings.ChargerBasic));
            AssertOnlySelected(root, DataIds.Buildings.ChargerBasic);
            Assert.That(Find(root, "DetailThumb").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(Find(root, "DetailPageCondition").GetComponent<TMP_Text>().text, Does.Contain("범위 내"));
            Assert.That(Find(root, "DetailPageStatus").GetComponent<TMP_Text>().text, Is.Not.Empty);
            AssertNotTruncated(root);
            if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, "building-detail-1920x1080.png"));

            // 뒤로가기: 목록으로만, 검색어 유지.
            yield return ClickCenter(environment.Mouse, view.BackButton);
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Assert.That(Find(root, "ListPage").gameObject.activeSelf, Is.True);
            Assert.That(windowMotion.IsPlaying, Is.False, "뒤로가기는 연출 없음");
            Assert.That(view.SearchField.text, Is.EqualTo("충전"));

            // 상세가 열린 채로 B: 레일·카드·큰 창 모두 닫힘, 선택 취소.
            view.OpenDetail(DataIds.Buildings.ChargerBasic);
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(binder.Presenter.SelectedBuildingId, Is.Empty);
            yield return new WaitForSecondsRealtime(BuildingModeSlideMotion.CloseDuration + 0.05f);
            Assert.That(view.gameObject.activeSelf, Is.False);

            // 다시 열면 레일+미선택(목록 접힘, 검색어 비움)에서 시작한다.
            yield return PressB();
            // 레일이 다 들어온 뒤에 누른다(슬라이드 중에는 아직 화면 왼쪽 밖에 있다).
            yield return new WaitForSecondsRealtime(BuildingModeSlideMotion.OpenDuration + 0.05f);
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            Assert.That(catalogWindow.activeSelf, Is.False);
            Assert.That(view.SearchField.text, Is.Empty);
            Assert.That(condition.gameObject.activeSelf, Is.False);
            AssertOnlySelected(root, string.Empty);

            // `<<`로 접기.
            view.ToggleExpanded();
            yield return null;
            Assert.That(catalogWindow.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            yield return ClickCenter(environment.Mouse, view.ExpandButton);
            // B-191: 거꾸로 접힌 뒤 꺼진다.
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.CloseDuration + 0.05f);
            Assert.That(catalogWindow.activeSelf, Is.False);
            Assert.That(catalogWindow.transform.localScale, Is.EqualTo(Vector3.one));

            // B-191: `>>`/`<<` 연타 뒤에도 창이 반쯤 남지 않는다.
            for (var i = 0; i < 5; i++)
            {
                view.ToggleExpanded();
                yield return null;
            }

            Assert.That(view.Navigation.IsExpanded, Is.True);
            yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.05f);
            Assert.That(catalogWindow.activeSelf, Is.True);
            Assert.That(catalogWindow.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(catalogWindow.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

            chrome.CloseBuildingMenu();
            yield return null;
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(save.ActiveSlot, Is.Zero);
        }

        private IEnumerator PressB() => UiTestWait.Press(keyboard, Key.B);

        /// <summary>교훈(#269): pivot이 아닌 실제 RectTransform 중앙을 화면 좌표로 눌러 마스크 경계에 걸리지 않게 한다.</summary>
        private static IEnumerator ClickCenter(Mouse mouse, Button button)
        {
            yield return UiTestWait.FocusGameView();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var position = (Vector2)rect.TransformPoint(rect.rect.center);
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var group = button.GetComponentInParent<CanvasGroup>();
            var diag = " pos=" + position + " screen=" + Screen.width + "x" + Screen.height
                + " corners=" + corners[0] + ".." + corners[2] + " active=" + button.gameObject.activeInHierarchy
                + " group=" + (group != null ? group.alpha + "/" + group.blocksRaycasts + "/" + group.interactable : "none");
            Assert.That(hits, Is.Not.Empty, button.name + " has no raycast hit" + diag);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name + " is obstructed by " + hits[0].gameObject.name + diag);
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "mouse down " + button.name, 2f, "input");
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "mouse up " + button.name, 2f, "input");
            yield return null;
            yield return null;
        }

        private static IEnumerator MoveMouseTo(Mouse mouse, RectTransform rect)
        {
            yield return UiTestWait.FocusGameView();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            Canvas.ForceUpdateCanvases();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = (Vector2)rect.TransformPoint(rect.rect.center) });
            yield return null;
            yield return null;
        }

        private static bool IsOverUi(Vector2 screen)
        {
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
            return hits.Count > 0;
        }

        private static Transform Find(Transform root, string name)
        {
            var found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            Assert.That(found, Is.Not.Null, name);
            return found;
        }

        private static Transform Entry(Transform root, string buildingId) => Find(root, "Select_" + buildingId);

        private static void AssertOnlySelected(Transform root, string buildingId)
        {
            foreach (var visual in root.GetComponentsInChildren<BuildingMenuEntryVisual>(true))
            {
                Assert.That(visual.IsSelected, Is.EqualTo(visual.BuildingId == buildingId), visual.name);
            }

            foreach (var row in root.GetComponentsInChildren<BuildingMenuCatalogRowView>(true))
            {
                Assert.That(row.IsSelected, Is.EqualTo(row.BuildingId == buildingId), row.name);
            }
        }

        private static void AssertNoControlGuide(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                Assert.That(text.text, Does.Not.Contain("좌클릭"), text.name);
                Assert.That(text.text, Does.Not.Contain("B 닫기"), text.name);
            }
        }

        private static void AssertNotTruncated(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextTruncated, Is.False, text.name + " truncated: " + text.text);
            }
        }

        private static void AssertInsideScreenAndClearOfHud(BuildingMenuView view, RectTransform extra = null)
        {
            var targets = new System.Collections.Generic.List<RectTransform>
            {
                (RectTransform)view.transform.Find("PanelRoot/Rail")
            };
            var side = view.transform.Find("PanelRoot/SidePane") as RectTransform;
            if (side != null && side.gameObject.activeInHierarchy) targets.Add(side);
            if (extra != null) targets.Add(extra);
            foreach (var rect in targets)
            {
                var menu = ScreenRect(rect);
                Assert.That(menu.xMin, Is.GreaterThanOrEqualTo(0f), rect.name);
                Assert.That(menu.yMin, Is.GreaterThanOrEqualTo(0f), rect.name + " 하단이 화면 밖입니다.");
                Assert.That(menu.xMax, Is.LessThanOrEqualTo(Screen.width), rect.name);
                Assert.That(menu.yMax, Is.LessThanOrEqualTo(Screen.height), rect.name);

                foreach (var hudName in new[] { "QuestSummaryButton", "BasicHUD" })
                {
                    var hud = GameObject.Find(hudName);
                    if (hud != null && hud.activeInHierarchy)
                    {
                        Assert.That(menu.Overlaps(ScreenRect((RectTransform)hud.transform)), Is.False, rect.name + " ↔ " + hudName);
                    }
                }

                var sideMenu = Object.FindAnyObjectByType<GameplaySideMenuController>();
                var open = sideMenu != null ? sideMenu.transform.Find("OpenMenuRoot") as RectTransform : null;
                if (open != null && open.gameObject.activeInHierarchy)
                {
                    Assert.That(menu.Overlaps(ScreenRect(open)), Is.False, rect.name + " ↔ 우측 메뉴");
                }
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
