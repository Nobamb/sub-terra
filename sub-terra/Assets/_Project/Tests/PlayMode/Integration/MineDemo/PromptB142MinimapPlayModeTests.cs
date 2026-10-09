using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>
    /// B-142 광산 관측 전광판의 실제 프레임 계약: 키보드 입력 → 상태 전환, unscaled 연출, Canvas 마스크,
    /// 실제 BuildingPlacementSystem 건설/복원/초기화 이벤트, 타일 변경 이벤트, disable/destroy 정리.
    /// 순수 계산은 EditMode(PromptB142MinimapBoardTests / PromptB94MinimapTests)가 검증한다.
    /// </summary>
    public sealed class PromptB142MinimapPlayModeTests
    {
        private readonly List<Object> created = new();
        private Keyboard keyboard;
        private Tilemap terrain;
        private Transform player;
        private ExplorationMinimap map;
        private BuildingPlacementSystem placement;
        private Tile tile;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            keyboard = InputSystem.AddDevice<Keyboard>();

            var cameraObject = Track(new GameObject("MainCamera", typeof(Camera)));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var grid = Track(new GameObject("Grid", typeof(Grid)));
            var terrainObject = new GameObject("ForegroundTilemap", typeof(Tilemap), typeof(TilemapRenderer));
            terrainObject.transform.SetParent(grid.transform, false);
            terrain = terrainObject.GetComponent<Tilemap>();
            tile = ScriptableObject.CreateInstance<Tile>();
            Track(tile);
            for (int y = -3; y <= -1; y++)
                for (int x = -12; x <= 12; x++)
                    terrain.SetTile(new Vector3Int(x, y, 0), tile);

            var playerObject = Track(new GameObject("Player", typeof(BoxCollider2D)));
            var body = playerObject.GetComponent<BoxCollider2D>();
            body.size = new Vector2(0.8f, 1.8f);
            body.offset = new Vector2(0f, 0.9f);
            player = playerObject.transform;

            var host = Track(new GameObject("Placement"));
            host.SetActive(false);
            placement = host.AddComponent<BuildingPlacementSystem>();
            var wallet = host.AddComponent<BuildingTestResourceWallet>();
            SetField(wallet, "remainingPlacements", 10);
            SetField(placement, "terrainTilemap", terrain);
            SetField(placement, "resourceWalletBehaviour", wallet);
            SetField(placement, "restoreDefinitions", new[]
            {
                Definition("building.outpost_core.basic", new Vector2Int(2, 2)),
                Definition("building.clinic.basic", new Vector2Int(1, 2)),
                Definition("building.storage.basic", new Vector2Int(2, 1)),
                Definition("building.charger.basic", Vector2Int.one)
            });
            host.SetActive(true);

            var canvasObject = Track(new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler)));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var mapObject = new GameObject("ExplorationMinimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            mapObject.transform.SetParent(canvasObject.transform, false);
            map = mapObject.AddComponent<ExplorationMinimap>();
            yield return null;
            map.Bind(terrain, player, null);
            map.BindFacilities(placement);
            yield return UiTestWait.FocusGameView();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = previousTimeScale;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.Destroy(created[i]);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MKey_RealFrames_CycleOneStepPerPress_UnscaledEvenWhenTimeStopped()
        {
            Time.timeScale = 0f;
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed), "초기 표시는 닫힘");
            Assert.That(map.IsBoardRendered, Is.False);

            yield return Press();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide));
            // 길게 누른 채 여러 프레임: 추가 전환 없음.
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide));
            yield return Release();
            yield return new WaitForSecondsRealtime(MinimapBoardTimeline.OpenDuration + 0.1f);
            Assert.That(map.IsTransitioning, Is.False, "timeScale 0에서도 unscaled로 열린다");
            Assert.That(map.Pose.Open, Is.EqualTo(1f));
            Assert.That(map.FrameRect.rect.width / map.FrameRect.rect.height, Is.EqualTo(2f).Within(0.001f));
            Assert.That(map.IsScanLineVisible, Is.True);
            Assert.That(map.ViewportRect.GetComponent<RectMask2D>(), Is.Not.Null);
            AssertInsideScreen();

            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Square));
            yield return new WaitForSecondsRealtime(MinimapBoardTimeline.ShrinkDuration + 0.1f);
            Assert.That(map.FrameRect.rect.width / map.FrameRect.rect.height, Is.EqualTo(1f).Within(0.001f));
            Assert.That(map.HintText, Is.EqualTo(ExplorationMinimap.CloseHintLabel));
            AssertInsideScreen();

            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            yield return new WaitForSecondsRealtime(MinimapBoardTimeline.CloseDuration + 0.1f);
            Assert.That(map.IsBoardRendered, Is.False, "마지막 빛까지 사라진다");
            Assert.That(map.IsScanLineVisible, Is.False);
            Assert.That(Time.timeScale, Is.Zero, "미니맵은 게임 시간을 바꾸지 않는다");

            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide), "닫힌 뒤 다시 가로형");
        }

        [UnityTest]
        public IEnumerator RapidTaps_DuringTransition_GoToNextLogicalState_AndSettleClean()
        {
            yield return Tap();
            yield return null;
            Assert.That(map.IsTransitioning, Is.True);
            float openMid = map.Pose.Open;
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Square), "전환 중 재입력은 다음 단계");
            Assert.That(map.Pose.Open, Is.GreaterThanOrEqualTo(openMid - 0.0001f), "현재 개방에서 이어간다");
            // 연타: 정사각형 → 닫힘 → 가로형 → 정사각형 → 닫힘(입력 1회 = 정확히 1단계).
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide));
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Square));
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(map.IsBoardRendered, Is.False);
            Assert.That(map.Pose.Line, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Facilities_FollowRealPlacementRestoreAndResetEvents()
        {
            Assert.That(placement.TryRestoreBuilding(Snapshot("core-1", "building.outpost_core.basic", -4, 0, 2, 2)), Is.True);
            Assert.That(placement.TryRestoreBuilding(Snapshot("clinic-1", "building.clinic.basic", -1, 0, 0, 0)), Is.True);
            Assert.That(placement.TryRestoreBuilding(Snapshot("storage-1", "building.storage.basic", 2, 0, 0, 0)), Is.True);
            Assert.That(map.Facilities.Count, Is.EqualTo(3));
            AssertCells("core-1", -4, 0, 2, 2);
            AssertCells("clinic-1", -1, 0, 1, 2);
            AssertCells("storage-1", 2, 0, 2, 1);

            placement.Select(Definition("building.charger.basic", Vector2Int.one));
            var placed = placement.TryPlaceAt(new Vector3Int(5, 0, 0));
            Assert.That(placed.IsSuccess, Is.True, placed.Failure.ToString());
            Assert.That(map.Facilities.TryGet(placed.InstanceId, out var charger), Is.True);
            Assert.That(charger.BuiltAt, Is.GreaterThanOrEqualTo(0f), "실제 건설 이벤트에만 건설 연출");
            Assert.That(map.Facilities.TryGet("core-1", out var core) && core.BuiltAt < 0f, Is.True, "복원 시설은 연출 없음");

            yield return Tap();
            yield return new WaitForSecondsRealtime(0.3f);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.FacilityGraphic.TryGetDrawnFootprint("core-1", out var coreRect), Is.True);
            float px = map.Window.CellPixels;
            Assert.That(coreRect.width, Is.EqualTo(2f * px).Within(0.01f));
            Assert.That(coreRect.height, Is.EqualTo(2f * px).Within(0.01f));
            Assert.That(coreRect.yMin, Is.EqualTo((0 - map.Window.BaseCell.y) * px).Within(0.01f), "시설 바닥 = 블록 윗면");

            // 철거(인스턴스 파괴)는 다음 프레임에 지도에서 사라진다.
            var storage = FindInstance("storage-1");
            Assert.That(storage, Is.Not.Null);
            Object.Destroy(storage.gameObject);
            yield return null;
            yield return null;
            Assert.That(map.Facilities.TryGet("storage-1", out _), Is.False);

            // 광산 재생성·복원 준비: 이전 시설 잔상 없음.
            placement.PrepareForWorldRestore();
            Assert.That(map.Facilities.Count, Is.Zero);
            map.RestoreMining(new WorldSnapshotDto());
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.FacilityGraphic.DrawnCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Terrain_ReflectsTileChangesWithinFrames()
        {
            yield return Tap();
            yield return new WaitForSecondsRealtime(0.3f);
            Canvas.ForceUpdateCanvases();
            int before = map.TerrainGraphic.BlockCount;
            Assert.That(before, Is.GreaterThan(0));
            // 채굴 이벤트 없이 타일만 바뀌어도(붕괴·복원 등) Tilemap 변경 알림으로 갱신된다.
            terrain.SetTile(new Vector3Int(0, -1, 0), null);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.TerrainGraphic.BlockCount, Is.EqualTo(before - 1));
            terrain.SetTile(new Vector3Int(1, -1, 0), null);
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 1, y = -1 });
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.TerrainGraphic.BlockCount, Is.EqualTo(before - 2), "채굴 셀 제거를 지연하지 않는다");
            Assert.That(map.TerrainGraphic.FlashCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(MinimapMinedFlashes.Duration + 0.1f);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.TerrainGraphic.FlashCount, Is.Zero, "가장자리 점등은 짧게 끝난다");
        }

        [UnityTest]
        public IEnumerator Player_TracksRealMovementAndHeight()
        {
            yield return Tap();
            yield return new WaitForSecondsRealtime(0.3f);
            Vector2 standing = map.PlayerMarker.anchoredPosition;
            Assert.That(standing.y, Is.EqualTo(map.Window.CellToViewport(Vector2.zero).y).Within(0.5f), "발 = 블록 윗면");
            player.position = new Vector3(0.5f, 1.5f, 0f);
            yield return null;
            yield return null;
            Vector2 expected = map.Window.CellToViewport(new Vector2(0.5f, 1.5f));
            Assert.That(map.PlayerMarker.anchoredPosition.y, Is.EqualTo(expected.y).Within(0.5f), "떠 있는 높이 유지");
            Assert.That(map.PlayerMarker.anchoredPosition.x, Is.EqualTo(expected.x).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator DisableAndDestroy_LeaveNoBeamScanOrSubscriptions()
        {
            Assert.That(map.IsPlacementSubscribed, Is.True);
            Assert.That(map.IsTilemapSubscribed, Is.True);
            yield return Tap();
            yield return null;
            map.enabled = false;
            Assert.That(map.IsBoardRendered, Is.False, "disable 시 즉시 소등");
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(map.IsPlacementSubscribed, Is.False);
            Assert.That(map.IsTilemapSubscribed, Is.False);
            yield return null;
            map.enabled = true;
            Assert.That(map.IsPlacementSubscribed, Is.True);
            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide), "재활성화 후 처음부터 다시 연다");

            var destroyed = map;
            Object.Destroy(map.gameObject);
            yield return null;
            Assert.That(destroyed == null, Is.True);
            // 파괴 뒤 시설·타일 이벤트가 와도 예외가 나지 않는다(구독 해제).
            Assert.DoesNotThrow(() => placement.TryRestoreBuilding(Snapshot("late-1", "building.charger.basic", 8, 0, 1, 1)));
            terrain.SetTile(new Vector3Int(3, -1, 0), null);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MKey_IgnoredWhileModalPauseHeld_AndResumesAfterRelease()
        {
            const string owner = "prompt-b142-minimap-test";
            Assert.That(SubTerra.App.UI.UiPauseGate.Acquire(owner), Is.True);
            try
            {
                yield return Tap();
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed), "모달 일시정지 중에는 기존 정책대로 무시");
            }
            finally
            {
                SubTerra.App.UI.UiPauseGate.Release(owner);
            }

            yield return Tap();
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide));
        }

        /// <summary>실제 렌더 결과 확인용 캡처(Temp). 배치 모드에서는 그래픽 장치가 없어 건너뛴다.</summary>
        [UnityTest]
        public IEnumerator Visual_CapturesWideAndSquareBoards()
        {
            if (Application.isBatchMode) Assert.Ignore("Visual capture requires a non-batch Editor.");
            for (int x = -12; x <= 12; x++)
                if (x % 5 != 0) terrain.SetTile(new Vector3Int(x, 2, 0), tile);
            terrain.SetTile(new Vector3Int(-6, -1, 0), null);
            terrain.SetTile(new Vector3Int(-6, -2, 0), null);
            placement.TryRestoreBuilding(Snapshot("core-1", "building.outpost_core.basic", -4, 0, 2, 2));
            placement.TryRestoreBuilding(Snapshot("clinic-1", "building.clinic.basic", 3, 0, 0, 0));
            placement.TryRestoreBuilding(Snapshot("storage-1", "building.storage.basic", 5, 0, 0, 0));
            placement.Select(Definition("building.charger.basic", Vector2Int.one));
            Assert.That(placement.TryPlaceAt(new Vector3Int(8, 0, 0)).IsSuccess, Is.True);
            yield return Tap();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return UiTestWait.Capture("Temp/prompt-b142-minimap-wide.png");
            yield return Tap();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return UiTestWait.Capture("Temp/prompt-b142-minimap-square.png");
        }

        private IEnumerator Tap()
        {
            yield return Press();
            yield return Release();
        }

        private IEnumerator Press()
        {
            yield return UiTestWait.FocusGameView();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M));
            // 키가 실제로 눌린 프레임에 미니맵 Update가 이미 입력을 처리했다.
            yield return UiTestWait.Until(() => keyboard.mKey.isPressed, "minimap M down", 2f, "input");
        }

        private IEnumerator Release()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return UiTestWait.Until(() => !keyboard.mKey.isPressed, "minimap M up", 2f, "input");
        }

        private void AssertInsideScreen()
        {
            var corners = new Vector3[4];
            map.FrameRect.GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-0.5f));
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(-0.5f));
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width + 0.5f));
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.height + 0.5f));
            Assert.That(map.FrameRect.lossyScale.x, Is.EqualTo(map.FrameRect.lossyScale.y).Within(0.0001f), "균일 배율");
        }

        private void AssertCells(string id, int x, int y, int width, int height)
        {
            Assert.That(map.Facilities.TryGet(id, out var record), Is.True, id);
            Assert.That(record.Cells, Is.EqualTo(new Rect(x, y, width, height)), id);
        }

        private static BuildingInstance FindInstance(string id)
        {
            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude))
                if (instance.InstanceId == id) return instance;
            return null;
        }

        private BuildingPlacementDefinition Definition(string id, Vector2Int size)
        {
            // 런타임 프리팹 대용 템플릿. 활성 상태로 둬야 복제본도 활성 시설이 된다.
            var prefab = Track(new GameObject(id + ".template"));
            prefab.transform.position = new Vector3(0f, 500f, 0f);
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            definition.EditorSet(id, prefab, size, true);
            Track(definition);
            return definition;
        }

        private static BuildingSnapshotDto Snapshot(string id, string type, int x, int y, int width, int height) => new()
        {
            instanceId = id,
            buildingTypeId = type,
            x = x,
            y = y,
            footprintWidth = width,
            footprintHeight = height
        };

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
