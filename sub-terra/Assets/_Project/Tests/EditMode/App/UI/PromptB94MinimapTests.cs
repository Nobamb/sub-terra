using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// 미니맵 View 계약(B-94 → B-142 광산 관측 전광판으로 갱신).
    /// 실제 Tilemap·Camera·Canvas 위에서 지형/플레이어/시설 좌표, M키 순환, 레이아웃을 검증한다.
    /// </summary>
    public sealed class PromptB94MinimapTests
    {
        private readonly List<Object> objects = new();
        private ExplorationMinimap map;
        private Camera camera;
        private Tilemap terrain;
        private Transform player;
        private Tile tileAsset;

        [SetUp]
        public void SetUp()
        {
            var canvas = Create("Canvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var root = Create("ExplorationMinimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            map = root.AddComponent<ExplorationMinimap>();
            camera = Create("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 16f / 9f;
            camera.transform.position = new Vector3(0, 0, -10);
            var grid = Create("Grid", typeof(Grid));
            terrain = Create("Terrain", typeof(Tilemap)).GetComponent<Tilemap>();
            terrain.transform.SetParent(grid.transform, false);
            tileAsset = ScriptableObject.CreateInstance<Tile>();
            objects.Add(tileAsset);
            // 바닥: y = -3..-1 세 층, x = -12..12. 플레이어는 y = 0(맨 윗층 윗면)에 선다.
            for (int y = -3; y <= -1; y++)
                for (int x = -12; x <= 12; x++)
                    PlaceTile(x, y);
            PlaceTile(-12, 6);
            player = Create("Player").transform;
            map.Bind(terrain, player, null);
            UseTestCamera();
            map.Advance(0f);
        }

        private void UseTestCamera() => typeof(ExplorationMinimap)
            .GetField("worldCamera", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(map, camera);

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Initial_IsClosed_RendersNothing_AndNeverBlocksInput()
        {
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(map.IsMapVisible, Is.False);
            Assert.That(map.IsBoardRendered, Is.False);
            Assert.That(map.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Open(MinimapBoardMode.Wide);
            foreach (var graphic in map.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name + "은 표시 전용");
            Assert.That(map.GetComponentInChildren<GraphicRaycaster>(true), Is.Null);
        }

        [Test]
        public void Layout_BottomRightCorner_AboveDarknessOverlay()
        {
            var overlay = Create("DepthDarknessOverlay", typeof(RectTransform));
            overlay.transform.SetParent(map.transform.parent, false);
            overlay.transform.SetAsFirstSibling();
            map.transform.SetAsFirstSibling();
            var rect = (RectTransform)map.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one;
            map.Advance(0f);
            Assert.That(rect.anchorMin, Is.EqualTo(ExplorationMinimap.BottomRightCorner));
            Assert.That(rect.pivot, Is.EqualTo(ExplorationMinimap.BottomRightCorner), "우하단 고정: 정사각형은 오른쪽 가장자리 기준으로 줄어든다");
            Assert.That(map.transform.GetSiblingIndex(), Is.EqualTo(overlay.transform.GetSiblingIndex() + 1));
        }

        [Test]
        public void Frame_WideTwoToOne_SquareOneToOne_SameCellSizeAndHeight()
        {
            Open(MinimapBoardMode.Wide);
            Vector2 wide = map.FrameRect.rect.size;
            float wideCell = map.Window.CellPixels;
            float wideViewportHeight = map.ViewportRect.rect.height;
            Vector2 wideWindow = map.Window.Size;
            Assert.That(wide.x / wide.y, Is.EqualTo(2f).Within(0.001f));
            Assert.That(map.HintText, Is.EqualTo(ExplorationMinimap.ShrinkHintLabel));

            Open(MinimapBoardMode.Square);
            Vector2 square = map.FrameRect.rect.size;
            Assert.That(square.x / square.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(square.y, Is.EqualTo(wide.y));
            Assert.That(map.Window.CellPixels, Is.EqualTo(wideCell), "셀 크기·시설 배율은 두 모드가 같다");
            Assert.That(map.ViewportRect.rect.height, Is.EqualTo(wideViewportHeight));
            Assert.That(map.Window.Size.y, Is.EqualTo(wideWindow.y).Within(0.001f));
            Assert.That(map.Window.Size.x, Is.LessThan(wideWindow.x));
            Assert.That(map.HintText, Is.EqualTo(ExplorationMinimap.CloseHintLabel));
            Assert.That(map.TitleText, Is.EqualTo(ExplorationMinimap.TitleLabel));
            Assert.That(map.FrameRect.localScale, Is.EqualTo(Vector3.one), "지도 자체를 늘이거나 압축하지 않는다");
            Assert.That(map.ContentRect.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Legend_FitsBeforeHint_InBothModes()
        {
            Open(MinimapBoardMode.Wide);
            Assert.That(map.LegendRightEdge, Is.LessThanOrEqualTo(map.HintLeftEdge));
            Open(MinimapBoardMode.Square);
            Assert.That(map.LegendRightEdge, Is.LessThanOrEqualTo(map.HintLeftEdge + 0.5f), "정사각형에서도 범례가 안내와 겹치지 않는다");
        }

        [Test]
        public void Legend_ShowsIconsOnly_WithoutFacilityNames()
        {
            Open(MinimapBoardMode.Wide);
            Assert.That(map.LegendIconCount, Is.EqualTo(ExplorationMinimap.LegendKinds.Length));
            foreach (var name in new[] { "코어", "충전기", "보건소", "엘리베이터", "조명", "보관함", "정산 콘솔", "긴급 탈출 포탈" })
            {
                foreach (var text in map.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    Assert.That(text.text, Does.Not.Contain(name), "하단에 시설 이름을 쓰지 않는다");
            }
        }

        [Test]
        public void PlayerMarker_IsBlinkingRedLight_NotPersonSilhouette()
        {
            Open(MinimapBoardMode.Wide);
            Assert.That(map.BeaconRect, Is.Not.Null);
            var glyphs = map.PlayerMarker.GetComponentsInChildren<MinimapGlyphGraphic>(true);
            Assert.That(glyphs.Length, Is.GreaterThan(0));
            foreach (var glyph in glyphs) Assert.That(glyph.Glyph, Is.Not.EqualTo(MinimapGlyph.Person), "사람 모양은 쓰지 않는다");
            var beacon = map.BeaconRect.GetComponent<MinimapGlyphGraphic>();
            Assert.That(beacon.Glyph, Is.EqualTo(MinimapGlyph.Beacon));
            Assert.That(beacon.color, Is.EqualTo(Color.white), "색은 정점색(붉은색)으로 그린다");
            Assert.That(MinimapPalette.Beacon.r, Is.GreaterThan(0.9f));
            Assert.That(MinimapPalette.Beacon.g, Is.LessThan(0.3f));
            Assert.That(MinimapPalette.Beacon.b, Is.LessThan(0.3f));
        }

        [Test]
        public void Opacity_DefaultsToHalf_AndCtrlMTogglesOpaqueInPointThreeSeconds()
        {
            Assert.That(map.IsOpaque, Is.False);
            Assert.That(map.CurrentAlpha, Is.EqualTo(0.5f).Within(0.001f), "기본 50% 투명도");
            map.Advance(1f);
            Assert.That(map.CurrentAlpha, Is.EqualTo(0.5f).Within(0.001f), "시간이 지나도 기본값 유지");

            Assert.That(map.PressOpacityKey(), Is.True);
            map.Advance(0.1f);
            Assert.That(map.CurrentAlpha, Is.InRange(0.51f, 0.99f), "전환 중");
            map.Advance(0.2f);
            Assert.That(map.CurrentAlpha, Is.EqualTo(1f).Within(0.001f), "0.3초 뒤 불투명");
            Assert.That(map.IsOpaque, Is.True);

            Assert.That(map.PressOpacityKey(), Is.False);
            map.Advance(0.3f);
            Assert.That(map.CurrentAlpha, Is.EqualTo(0.5f).Within(0.001f), "다시 누르면 반투명");
            Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed), "투명도 전환은 지도 단계를 바꾸지 않는다");
        }

        [Test]
        public void Window_StaysInsideCameraView_SoHiddenAreaIsNotRevealed()
        {
            Open(MinimapBoardMode.Wide);
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            var window = map.Window;
            Assert.That(window.Min.x, Is.GreaterThanOrEqualTo(-halfWidth - 0.001f));
            Assert.That(window.Max.x, Is.LessThanOrEqualTo(halfWidth + 0.001f));
            Assert.That(window.Min.y, Is.GreaterThanOrEqualTo(-halfHeight - 0.001f));
            Assert.That(window.Max.y, Is.LessThanOrEqualTo(halfHeight + 0.001f));
            // 카메라 밖 타일(-12, 6)은 셀 범위에 들어오지 않는다.
            Assert.That(window.BaseCell.x, Is.GreaterThan(-12));
        }

        [Test]
        public void Terrain_SeparatesBlocksEmptyAndUnobserved_AndMiningIsImmediate()
        {
            Open(MinimapBoardMode.Wide);
            int blocks = Terrain(out int empty, out int unobserved);
            Assert.That(blocks, Is.GreaterThan(0));
            Assert.That(empty, Is.GreaterThan(0), "블록 위 공기 = 셀 없는 빈 공간");
            Assert.That(map.Sample(-12, 6), Is.EqualTo(MinimapCellKind.Block));
            Assert.That(map.Sample(0, 0), Is.EqualTo(MinimapCellKind.Empty));
            Assert.That(map.Sample(100, 0), Is.EqualTo(MinimapCellKind.Void), "월드 밖 = 미관측");
            Assert.That(unobserved, Is.GreaterThanOrEqualTo(0));

            terrain.SetTile(new Vector3Int(0, -1, 0), null);
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 0, y = -1 });
            map.Advance(0f);
            Assert.That(Terrain(out _, out _), Is.EqualTo(blocks - 1), "채굴 셀은 지연 없이 빈 공간");
            Assert.That(map.TerrainGraphic.FlashCount, Is.EqualTo(1), "가장자리 점등만 짧게");
            Assert.That(map.Sample(0, -1), Is.EqualTo(MinimapCellKind.Empty));
            Assert.That(map.MinedCellCount, Is.EqualTo(1));

            map.RecordMining(new GameplayEventDto { type = GameplayEventType.GasTriggered, x = 3, y = -1 });
            Assert.That(map.MinedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetAndRestore_ClearsPreviousMiningAndFacilities()
        {
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 1, y = -1 });
            map.Facilities.Upsert("old-1", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(2, 0, 1, 1), false, 0f);
            map.RestoreMining(new WorldSnapshotDto
            {
                miningChanges = new List<MiningSnapshotDto> { new() { x = 4, y = -1, isDestroyed = true }, new() { x = 5, y = -1 } },
                buildings = new List<BuildingSnapshotDto>
                {
                    new() { instanceId = "core-9", buildingTypeId = "building.outpost_core.basic", x = -3, y = 0, footprintWidth = 2, footprintHeight = 2 }
                }
            });
            Assert.That(map.MinedCellCount, Is.EqualTo(1));
            Assert.That(map.Facilities.Count, Is.EqualTo(1), "이전 시설 잔상 없음");
            Assert.That(map.Facilities.TryGet("core-9", out var core), Is.True);
            Assert.That(core.Cells, Is.EqualTo(new Rect(-3f, 0f, 2f, 2f)));
            map.RestoreMining(new WorldSnapshotDto());
            Assert.That(map.MinedCellCount, Is.Zero);
            Assert.That(map.Facilities.Count, Is.Zero);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void Facility_DrawsActualFootprint_StandingOnBlockTop(int width, int height)
        {
            map.Facilities.Upsert("f-1", "building.clinic.basic", MinimapFacilityRegistry.FootprintCells(-2, 0, width, height), false, 0f);
            Open(MinimapBoardMode.Wide);
            var window = map.Window;
            FillFacilities();
            Assert.That(map.FacilityGraphic.TryGetDrawnFootprint("f-1", out var drawn), Is.True);
            Assert.That(drawn.width, Is.EqualTo(width * window.CellPixels).Within(0.01f));
            Assert.That(drawn.height, Is.EqualTo(height * window.CellPixels).Within(0.01f));
            // 시설 하단 = y 0 = 아래 블록(y -1) 윗면.
            float blockTop = (0 - window.BaseCell.y) * window.CellPixels;
            Assert.That(drawn.yMin, Is.EqualTo(blockTop).Within(0.01f));
            Assert.That(map.FacilityGraphic.DrawnCount, Is.EqualTo(1), "점유 셀마다 중복 아이콘을 만들지 않는다");
        }

        [Test]
        public void Facility_OutsideWindowIsNotDrawn_EdgeFacilityIsDrawnAndMasked()
        {
            map.Facilities.Upsert("far", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(40, 0, 1, 1), false, 0f);
            Open(MinimapBoardMode.Wide);
            var window = map.Window;
            int edgeX = Mathf.CeilToInt(window.Max.x) - 1;
            map.Facilities.Upsert("edge", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(edgeX, 0, 2, 1), false, 0f);
            map.Advance(0f);
            FillFacilities();
            Assert.That(map.FacilityGraphic.TryGetDrawnFootprint("far", out _), Is.False);
            Assert.That(map.FacilityGraphic.TryGetDrawnFootprint("edge", out _), Is.True);
            Assert.That(map.ViewportRect.GetComponent<RectMask2D>(), Is.Not.Null, "경계 시설은 실제 위치에서 마스크로 잘린다");
        }

        [Test]
        public void Player_FeetFollowRealHeight_NotSnappedToGround()
        {
            var body = player.gameObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(0.8f, 1.8f);
            body.offset = new Vector2(0f, 0.9f);
            Physics2D.SyncTransforms();
            map.Bind(terrain, player, null);
            UseTestCamera();
            Open(MinimapBoardMode.Wide);
            var window = map.Window;
            float groundY = window.CellToViewport(new Vector2(0f, 0f)).y;
            Assert.That(map.PlayerMarker.anchoredPosition.y, Is.EqualTo(groundY).Within(0.01f), "발이 블록 윗면에 닿는다");
            Assert.That(map.PlayerMarker.sizeDelta.y, Is.EqualTo(1.8f * window.CellPixels).Within(0.01f));

            player.position = new Vector3(0.3f, 1.25f, 0f);
            map.Advance(0f);
            window = map.Window;
            Vector2 expected = window.CellToViewport(new Vector2(0.3f, 1.25f));
            Assert.That(map.PlayerMarker.anchoredPosition.x, Is.EqualTo(expected.x).Within(0.01f));
            Assert.That(map.PlayerMarker.anchoredPosition.y, Is.EqualTo(expected.y).Within(0.01f), "떠 있는 높이·이동을 그대로 반영");
        }

        [Test]
        public void Opening_UsesMaskAperture_NotScale()
        {
            map.PressMapKey();
            map.Advance(MinimapBoardTimeline.OpenDuration * 0.25f);
            Assert.That(map.Pose.Open, Is.InRange(0.01f, 0.99f));
            var aperture = (RectTransform)map.transform.Find(ExplorationMinimap.ApertureName);
            Assert.That(aperture.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(aperture.rect.height, Is.LessThan(map.FrameRect.rect.height));
            Assert.That(map.FrameRect.rect.size, Is.EqualTo(new Vector2(MinimapBoardLayout.WideWidth, MinimapBoardLayout.FrameHeight)),
                "프레임·지도는 처음부터 원래 크기, 마스크만 열린다");
            Assert.That(map.FrameRect.lossyScale.y, Is.EqualTo(map.FrameRect.lossyScale.x));
        }

        [Test]
        public void ScanLine_OnlyWhileShown_AndMovesTopToBottom()
        {
            Assert.That(map.IsScanLineVisible, Is.False);
            Open(MinimapBoardMode.Wide);
            Assert.That(map.IsScanLineVisible, Is.True);
            float first = map.ScanLine.anchoredPosition.y;
            map.Advance(0.5f);
            Assert.That(map.ScanLine.anchoredPosition.y, Is.LessThan(first), "위 → 아래");
            map.PressMapKey();
            map.PressMapKey();
            map.Advance(0.001f);
            Assert.That(map.IsScanLineVisible, Is.False, "닫히는 순간 스캔선 제거");
            map.Advance(1f);
            Assert.That(map.IsBoardRendered, Is.False);
        }

        [Test]
        public void MKey_OneStepPerPress_HoldDoesNotRepeat()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Hold(keyboard, Key.M);
                Tick("Update");
                Tick("Update");
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide), "길게 눌러도 한 단계");
                Release(keyboard);
                Hold(keyboard, Key.M);
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Square));
                Release(keyboard);
                Hold(keyboard, Key.M);
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed));
                Release(keyboard);
                Hold(keyboard, Key.LeftCtrl, Key.M);
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed), "Ctrl 조합은 무시");
                Release(keyboard);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void MKey_IgnoredWhileTyping()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var events = Create("Events", typeof(EventSystem)).GetComponent<EventSystem>();
            try
            {
                typeof(EventSystem).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(events, null);
                var input = Create("Search", typeof(RectTransform), typeof(TMP_InputField));
                input.GetComponent<TMP_InputField>().enabled = false;
                EventSystem.current = events;
                events.SetSelectedGameObject(input);
                Hold(keyboard, Key.M);
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Closed), "텍스트 입력 중 무시");
                Release(keyboard);
                events.SetSelectedGameObject(null);
                Hold(keyboard, Key.M);
                Assert.That(map.Mode, Is.EqualTo(MinimapBoardMode.Wide), "선택 해제 후 정상 동작");
            }
            finally
            {
                typeof(EventSystem).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(events, null);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void MinimapNeverTouchesTimeScaleOrTiles()
        {
            float scale = Time.timeScale;
            int tiles = terrain.GetUsedTilesCount();
            Open(MinimapBoardMode.Wide);
            Open(MinimapBoardMode.Square);
            map.PressMapKey();
            map.Advance(1f);
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(terrain.GetUsedTilesCount(), Is.EqualTo(tiles));
        }

        private void Open(MinimapBoardMode target)
        {
            for (int i = 0; i < 3 && map.Mode != target; i++) map.PressMapKey();
            map.Advance(1f);
            map.Advance(0f);
            Assert.That(map.Mode, Is.EqualTo(target));
        }

        private int Terrain(out int empty, out int unobserved)
        {
            using var mesh = new VertexHelper();
            map.TerrainGraphic.Fill(mesh);
            empty = map.TerrainGraphic.EmptyCount;
            unobserved = map.TerrainGraphic.VoidCount;
            return map.TerrainGraphic.BlockCount;
        }

        private void FillFacilities()
        {
            using var mesh = new VertexHelper();
            map.FacilityGraphic.Fill(mesh);
            Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
        }

        private void Hold(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            keyboard.MakeCurrent();
            Tick("Update");
        }

        private void Release(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Tick("Update");
        }

        private void PlaceTile(int x, int y) => terrain.SetTile(new Vector3Int(x, y, 0), tileAsset);

        private void Tick(string method) => typeof(ExplorationMinimap)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(map, null);

        private GameObject Create(string name, params System.Type[] components)
        {
            var value = new GameObject(name, components);
            objects.Add(value);
            return value;
        }
    }
}
