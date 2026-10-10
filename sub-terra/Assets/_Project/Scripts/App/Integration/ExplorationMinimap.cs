using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 광산 관측 전광판(미니맵). 카메라에 보이는 범위 안에서 실제 블록·빈 공간·플레이어·시설을 관측해 표시만 한다.
    /// M키 한 번에 닫힘 → 가로형(2:1) → 정사각형(1:1) → 닫힘으로 한 단계씩 전환한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ExplorationMinimap : MonoBehaviour, IMinimapTerrainSource
    {
        public const string TitleLabel = "광산 관측";
        public const string ShrinkHintLabel = "[M] 축소";
        public const string CloseHintLabel = "[M] 닫기";
        /// <summary>바깥 화면 쪽 이벤트를 놓쳐도 표시 중 이 주기로 지형을 다시 읽는다.</summary>
        public const float TerrainSafetyRefreshSeconds = 1f;
        public static readonly Vector2 BottomRightCorner = new(1f, 0f);
        /// <summary>하단 범례는 이름 없이 아이콘만 둔다. 설명은 게임 가이드 시설 항목에 있다.</summary>
        public static readonly MinimapFacilityKind[] LegendKinds =
        {
            MinimapFacilityKind.Core, MinimapFacilityKind.Charger, MinimapFacilityKind.Clinic, MinimapFacilityKind.Elevator,
            MinimapFacilityKind.Light, MinimapFacilityKind.Storage, MinimapFacilityKind.Settlement, MinimapFacilityKind.Portal
        };

        public const string ApertureName = "Aperture";
        public const string BoardName = "Board";
        public const string ViewportName = "Viewport";
        public const string ScanLineName = "ScanLine";
        public const string PlayerMarkerName = "PlayerMarker";

        private const string DarknessOverlayName = "DepthDarknessOverlay";
        private const float LegendLeft = 10f;
        private const float LegendIconSize = 14f;
        private const float HintPadding = 10f;
        /// <summary>범례의 긴급 탈출 포탈 아이콘이 한 바퀴 도는 시간(초). 지도 속 포탈과 같은 속도다.</summary>
        private const float LegendSpinSeconds = MinimapFacilityGraphic.PortalSpinSeconds;
        private static readonly Vector2 FallbackPlayerCells = new(0.7f, 1.6f);

        private readonly HashSet<Vector3Int> minedCells = new();
        private readonly MinimapBoardTimeline timeline = new();
        private readonly MinimapFacilityRegistry facilities = new();
        private readonly MinimapMinedFlashes minedFlashes = new();
        private readonly Dictionary<string, BuildingInstance> facilityInstances = new();
        private readonly Dictionary<string, PowerNode> facilityPower = new();
        private readonly List<string> scratchIds = new();
        private readonly MinimapOpacity opacity = new();
        private readonly MinimapGlyphGraphic[] legendIcons = new MinimapGlyphGraphic[LegendKinds.Length];
        private readonly float[] legendWidths = new float[LegendKinds.Length];
        private readonly float[] legendX = new float[LegendKinds.Length];

        private Tilemap terrain;
        private Transform player;
        private Vector2 playerFeetOffset;
        private Vector2 playerSize;
        private bool hasPlayerShape;
        private Camera worldCamera;
        private BuildingPlacementSystem placement;
        private ElevatorController[] elevators = Array.Empty<ElevatorController>();
        private bool placementSubscribed;
        private bool tilemapSubscribed;

        private RectTransform root;
        private RectTransform aperture;
        private RectTransform board;
        private RectTransform viewport;
        private RectTransform content;
        private RectTransform playerMarker;
        private RectTransform ringRect;
        private RectTransform scanLine;
        private RectTransform beamTop;
        private RectTransform beamBottom;
        private RectTransform hintRect;
        private CanvasGroup rootGroup;
        private CanvasGroup chromeGroup;
        private CanvasGroup terrainGroup;
        private CanvasGroup facilityGroup;
        private CanvasGroup playerGroup;
        private MinimapFrameGraphic frameGraphic;
        private MinimapTerrainGraphic terrainGraphic;
        private MinimapFacilityGraphic facilityGraphic;
        private MinimapGlyphGraphic beacon;
        private RectTransform beaconRect;
        private MinimapGlyphGraphic ring;
        private Image scanImage;
        private Image beamTopImage;
        private Image beamBottomImage;
        private TMP_Text title;
        private TMP_Text hint;
        private Transform darknessOverlay;
        private bool hierarchyReady;

        private bool wasMapKeyDown;
        private bool terrainDirty = true;
        private bool terrainFlashing;
        private bool facilityFlashing;
        private int drawnFacilityVersion = -1;
        private Vector2Int drawnBase;
        private Vector2Int drawnCount;
        private float drawnCellPixels;
        private float appliedFrameWidth = -1f;
        private float terrainRefreshTimer;
        private float hintBlockWidth;
        private MinimapWindow window;

        public MinimapBoardMode Mode => timeline.Mode;
        public bool IsMapVisible => timeline.Mode != MinimapBoardMode.Closed;
        public MinimapBoardPose Pose => timeline.Pose;
        public bool IsTransitioning => timeline.IsTransitioning;
        public int MinedCellCount => minedCells.Count;
        public MinimapFacilityRegistry Facilities => facilities;
        /// <summary>true면 불투명(Ctrl+M), false면 기본 반투명(50%).</summary>
        public bool IsOpaque => opacity.IsOpaque;
        public float CurrentAlpha => rootGroup != null ? rootGroup.alpha : opacity.Alpha;
        public RectTransform BeaconRect => beaconRect;
        public int LegendIconCount => legendIcons.Length;
        public MinimapWindow Window => window;
        public float CurrentFrameWidth => MinimapBoardLayout.FrameWidth(timeline.Pose.Width);
        public string HintText => hint != null ? hint.text : string.Empty;
        public string TitleText => title != null ? title.text : string.Empty;
        public RectTransform FrameRect => board;
        public RectTransform ViewportRect => viewport;
        public RectTransform ContentRect => content;
        public RectTransform PlayerMarker => playerMarker;
        public RectTransform ScanLine => scanLine;
        public MinimapTerrainGraphic TerrainGraphic => terrainGraphic;
        public MinimapFacilityGraphic FacilityGraphic => facilityGraphic;
        public bool IsBoardRendered => aperture != null && aperture.gameObject.activeSelf;
        public bool IsScanLineVisible => IsBoardRendered && scanLine != null && scanLine.gameObject.activeSelf;
        /// <summary>프레임 좌측 기준 범례 끝·M키 안내 구분선 x(테스트·레이아웃 검증용).</summary>
        public float LegendRightEdge { get; private set; }
        public float HintLeftEdge { get; private set; }
        public bool IsPlacementSubscribed => placementSubscribed;
        public bool IsTilemapSubscribed => tilemapSubscribed;

        public void Bind(Tilemap map, Transform target, WorldSnapshotDto snapshot)
        {
            terrain = map;
            player = target;
            worldCamera = Camera.main;
            MeasurePlayerShape();
            EnsureHierarchy();
            if (isActiveAndEnabled) SubscribeTilemap();
            ApplyHudLayout();
            RestoreMining(snapshot);
            RefreshElevators();
        }

        /// <summary>시설은 BuildingPlacementSystem 이벤트와 엘리베이터 승강로 점유 범위로만 읽는다.</summary>
        public void BindFacilities(BuildingPlacementSystem system, params ElevatorController[] shafts)
        {
            UnsubscribePlacement();
            placement = system;
            elevators = shafts ?? Array.Empty<ElevatorController>();
            if (isActiveAndEnabled) SubscribePlacement();
            RefreshElevators();
        }

        /// <summary>세이브 복원·월드 재생성 뒤 채굴 기록과 시설 목록을 실제 스냅샷으로 다시 맞춘다.</summary>
        public void RestoreMining(WorldSnapshotDto snapshot)
        {
            minedCells.Clear();
            minedFlashes.Clear();
            if (snapshot != null && snapshot.miningChanges != null)
                foreach (var change in snapshot.miningChanges)
                    if (change.isDestroyed) minedCells.Add(new Vector3Int(change.x, change.y, 0));
            if (snapshot != null) SeedFacilities(snapshot);
            terrainDirty = true;
        }

        /// <summary>채굴 완료는 즉시 빈 공간으로 반영한다. 가장자리 점등만 짧게 남긴다.</summary>
        public void RecordMining(GameplayEventDto change)
        {
            if (change == null || change.type != GameplayEventType.TileMined) return;
            minedCells.Add(new Vector3Int(change.x, change.y, 0));
            minedFlashes.Add(new Vector2Int(change.x, change.y), Time.unscaledTime);
            terrainDirty = true;
        }

        /// <summary>M 한 번 = 한 단계. 입력 경로는 Update 한 곳뿐이며 테스트도 이 진입점을 쓴다.</summary>
        public MinimapBoardMode PressMapKey()
        {
            EnsureHierarchy();
            MinimapBoardMode mode = timeline.Press();
            UpdateHint();
            if (mode != MinimapBoardMode.Closed)
            {
                // 재열기 시 숨김 동안의 변화를 최신 데이터로 맞춘다.
                terrainDirty = true;
                drawnFacilityVersion = -1;
                SetBoardRendered(true);
            }

            return mode;
        }

        /// <summary>Ctrl+M 한 번 = 반투명 ↔ 불투명 전환(0.3초). 입력 경로는 Update 한 곳뿐이며 테스트도 이 진입점을 쓴다.</summary>
        public bool PressOpacityKey()
        {
            EnsureHierarchy();
            return opacity.Toggle();
        }

        /// <summary>우하단 고정·화면 맞춤 배율·암전 오버레이 위 정렬을 적용한다.</summary>
        public void ApplyHudLayout()
        {
            EnsureHierarchy();
            if (root.anchorMin != BottomRightCorner) root.anchorMin = BottomRightCorner;
            if (root.anchorMax != BottomRightCorner) root.anchorMax = BottomRightCorner;
            if (root.pivot != BottomRightCorner) root.pivot = BottomRightCorner;
            float edge = MinimapBoardLayout.ScreenMargin + MinimapBoardLayout.GlowMargin;
            var position = new Vector2(-edge, edge);
            if (root.anchoredPosition != position) root.anchoredPosition = position;
            float scale = root.parent is RectTransform parent ? MinimapBoardLayout.FitScale(parent.rect.size) : 1f;
            var scaled = new Vector3(scale, scale, 1f);
            if (root.localScale != scaled) root.localScale = scaled;
            ApplyFrameWidth(CurrentFrameWidth);
            PlaceAboveDarknessOverlay();
        }

        /// <summary>unscaled 시간으로 연출을 진행하고 표시 중이면 지도 데이터를 갱신한다.</summary>
        public void Advance(float unscaledDeltaTime)
        {
            EnsureHierarchy();
            timeline.Advance(unscaledDeltaTime);
            opacity.Advance(unscaledDeltaTime);
            float alpha = opacity.Alpha;
            if (!Mathf.Approximately(rootGroup.alpha, alpha)) rootGroup.alpha = alpha;
            ApplyHudLayout();
            bool shown = timeline.IsVisible;
            SetBoardRendered(shown);
            if (!shown) return;
            MinimapBoardPose pose = timeline.Pose;
            ApplyPose(pose);
            UpdateWorld(Mathf.Max(0f, unscaledDeltaTime));
            UpdateDecor(pose);
        }

        public MinimapCellKind Sample(int x, int y)
        {
            if (terrain == null) return MinimapCellKind.Void;
            var cell = new Vector3Int(x, y, 0);
            BoundsInt bounds = terrain.cellBounds;
            bool inside = x >= bounds.xMin && x < bounds.xMax && y >= bounds.yMin && y < bounds.yMax;
            return MinimapTerrainClassifier.Classify(inside, inside && terrain.HasTile(cell));
        }

        /// <summary>타일맵 로컬 좌표를 셀 좌표(1칸 = 1)로 바꾼다. 셀 (x, y)는 [x, x+1) 구간이다.</summary>
        public Vector2 WorldToCellSpace(Vector3 world)
        {
            if (terrain == null) return world;
            Vector3 local = terrain.transform.InverseTransformPoint(world);
            Vector3 stride = CellStride();
            return new Vector2(local.x / stride.x, local.y / stride.y);
        }

        private void OnEnable()
        {
            SubscribeTilemap();
            SubscribePlacement();
        }

        private void OnDisable()
        {
            UnsubscribeTilemap();
            UnsubscribePlacement();
            // 비활성화 시 연출 잔상(광선·스캔선) 없이 즉시 닫는다.
            timeline.ForceClosed();
            wasMapKeyDown = false;
            if (hierarchyReady)
            {
                SetBoardRendered(false);
                UpdateHint();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeTilemap();
            UnsubscribePlacement();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            bool mapKeyDown = keyboard != null && keyboard.mKey.isPressed;
            // 누르고 있는 동안은 첫 프레임에만 한 번 처리한다. Ctrl+M은 투명도, M만은 단계 전환.
            if (mapKeyDown && !wasMapKeyDown && IsInputAvailable(keyboard))
            {
                bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                if (ctrl) PressOpacityKey();
                else PressMapKey();
            }

            wasMapKeyDown = mapKeyDown;
        }

        private void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private static bool IsInputAvailable(Keyboard keyboard)
        {
            if (SubTerra.App.UI.UiPauseGate.IsHeld) return false;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected == null || (selected.GetComponentInParent<TMP_InputField>() == null
                && selected.GetComponentInParent<InputField>() == null);
        }

        private void ApplyPose(MinimapBoardPose pose)
        {
            float height = MinimapBoardLayout.FrameHeight + MinimapBoardLayout.GlowMargin * 2f;
            // 지도를 늘이지 않고 마스크 높이만 연다.
            float closed = height * (1f - Mathf.Clamp01(pose.Open)) * 0.5f;
            var min = new Vector2(-MinimapBoardLayout.GlowMargin, -MinimapBoardLayout.GlowMargin + closed);
            var max = new Vector2(MinimapBoardLayout.GlowMargin, MinimapBoardLayout.GlowMargin - closed);
            if (aperture.offsetMin != min) aperture.offsetMin = min;
            if (aperture.offsetMax != max) aperture.offsetMax = max;
            chromeGroup.alpha = pose.Frame;
            terrainGroup.alpha = pose.Terrain;
            facilityGroup.alpha = pose.Markers;
            playerGroup.alpha = pose.Markers;

            bool beam = pose.Line > 0.001f;
            float offset = MinimapBoardLayout.FrameHeight * Mathf.Clamp01(pose.Open) * 0.5f;
            SetActive(beamTop, beam);
            SetActive(beamBottom, beam);
            if (beam)
            {
                beamTop.anchoredPosition = new Vector2(0f, offset);
                beamBottom.anchoredPosition = new Vector2(0f, -offset);
                beamTopImage.canvasRenderer.SetAlpha(pose.Line);
                beamBottomImage.canvasRenderer.SetAlpha(pose.Line);
            }
        }

        private void UpdateWorld(float delta)
        {
            if (terrain == null || player == null)
            {
                SetActive(playerMarker, false);
                return;
            }

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null)
            {
                SetActive(playerMarker, false);
                return;
            }

            Rect cameraCells = CameraCellRect();
            float cellPixels = MinimapBoardLayout.CellPixels(MinimapBoardLayout.WideViewportSize, cameraCells.size);
            Vector2 viewportSize = MinimapBoardLayout.ViewportRect(CurrentFrameWidth).size;
            GetPlayerCells(out Vector2 feet, out Vector2 bodyCells);
            Vector2 focus = feet + new Vector2(0f, bodyCells.y * 0.5f);
            window = MinimapBoardLayout.ComputeWindow(viewportSize, cellPixels, focus, cameraCells);
            if (!window.IsValid)
            {
                SetActive(playerMarker, false);
                return;
            }

            content.anchoredPosition = window.ContentOffset;
            Vector2Int baseCell = window.BaseCell;
            Vector2Int count = window.CellCount;
            content.sizeDelta = new Vector2(count.x, count.y) * window.CellPixels;
            float now = Time.unscaledTime;
            bool geometryChanged = baseCell != drawnBase || count != drawnCount || !Mathf.Approximately(window.CellPixels, drawnCellPixels);

            terrainRefreshTimer += delta;
            if (terrainRefreshTimer >= TerrainSafetyRefreshSeconds)
            {
                terrainRefreshTimer = 0f;
                terrainDirty = true;
            }

            bool flashing = minedFlashes.AnyActive(now);
            if (terrainDirty || geometryChanged || flashing || terrainFlashing)
            {
                terrainGraphic.Configure(this, minedFlashes, baseCell, count, window.CellPixels, now);
                terrainDirty = false;
            }

            terrainFlashing = flashing;
            PollFacilityStates();
            bool facilityFlash = facilities.AnyFlashActive(now);
            // 포탈 내부 소용돌이는 천천히 계속 돌기 때문에 보이는 동안 다시 그린다.
            bool facilityAnimated = facilities.AnyAnimatedVisible(window);
            if (geometryChanged || facilities.Version != drawnFacilityVersion || facilityFlash || facilityFlashing || facilityAnimated)
            {
                facilityGraphic.Configure(facilities, window, now);
                drawnFacilityVersion = facilities.Version;
            }

            facilityFlashing = facilityFlash;
            drawnBase = baseCell;
            drawnCount = count;
            drawnCellPixels = window.CellPixels;
            PlacePlayer(feet, bodyCells);
        }

        private void PlacePlayer(Vector2 feet, Vector2 bodyCells)
        {
            SetActive(playerMarker, true);
            float px = window.CellPixels;
            // 실제 몸 크기로 그리되 너무 작아지지 않게만 보정한다. 발 = 실제 높이(떠 있으면 떠 있게).
            var size = new Vector2(Mathf.Max(6f, bodyCells.x * px), Mathf.Max(10f, bodyCells.y * px));
            playerMarker.anchoredPosition = window.CellToViewport(feet);
            if (playerMarker.sizeDelta != size) playerMarker.sizeDelta = size;
            // 깜빡이는 붉은 표시등은 몸 중앙에 고정 크기로 둔다. 사람 모양은 쓰지 않는다.
            float lightSize = Mathf.Clamp(px * 1.5f, 12f, 22f);
            var lightDelta = new Vector2(lightSize, lightSize);
            if (beaconRect.sizeDelta != lightDelta) beaconRect.sizeDelta = lightDelta;
            var center = new Vector2(0f, size.y * 0.5f);
            beaconRect.anchoredPosition = center;
            float ringSize = lightSize * 1.9f;
            var ringDelta = new Vector2(ringSize, ringSize);
            if (ringRect.sizeDelta != ringDelta) ringRect.sizeDelta = ringDelta;
            ringRect.anchoredPosition = center;
        }

        private void UpdateDecor(MinimapBoardPose pose)
        {
            bool scanning = pose.Terrain > 0.01f && timeline.Mode != MinimapBoardMode.Closed;
            SetActive(scanLine, scanning);
            if (scanning)
            {
                float height = MinimapBoardLayout.ViewportRect(CurrentFrameWidth).height;
                scanLine.anchoredPosition = new Vector2(0f, -timeline.ScanPhase * height);
                scanImage.canvasRenderer.SetAlpha(pose.Terrain);
            }

            // 붉은 표시등이 깜빡이고, 바깥 링은 꺼질 때 살짝 퍼지며 사라진다.
            float blink = timeline.BeaconBlink;
            beacon.canvasRenderer.SetAlpha(blink);
            float scale = 1f + 0.3f * (1f - blink);
            ringRect.localScale = new Vector3(scale, scale, 1f);
            ring.canvasRenderer.SetAlpha(0.7f * blink);
            // 범례의 포탈 아이콘도 지도 속 포탈처럼 천천히 돈다.
            int portal = Array.IndexOf(LegendKinds, MinimapFacilityKind.Portal);
            if (portal >= 0 && legendIcons[portal] != null)
                legendIcons[portal].rectTransform.localRotation = Quaternion.Euler(0f, 0f, timeline.DisplayClock * 360f / LegendSpinSeconds);
        }

        private void ApplyFrameWidth(float frameWidth)
        {
            if (Mathf.Approximately(appliedFrameWidth, frameWidth)) return;
            appliedFrameWidth = frameWidth;
            var size = new Vector2(frameWidth, MinimapBoardLayout.FrameHeight);
            root.sizeDelta = size;
            board.sizeDelta = size;
            LayoutFooter(frameWidth);
        }

        private void LayoutFooter(float frameWidth)
        {
            float divider = hintBlockWidth + 4f;
            frameGraphic.SetDivider(divider);
            hintRect.sizeDelta = new Vector2(hintBlockWidth, MinimapBoardLayout.FooterHeight);
            float available = frameWidth - divider - LegendLeft - 8f;
            // 이름 없이 아이콘만: 정사각형에서 넘치면 아이콘을 같은 비율로 줄인다.
            float icon = LegendIconSize;
            if (!MinimapBoardLayout.LayoutLegend(available, legendWidths, icon, 0f, 6f, 22f, legendX))
            {
                float scale = MinimapBoardLayout.LegendScale(available, legendWidths, LegendIconSize, 0f, 4f);
                icon = LegendIconSize * scale;
                MinimapBoardLayout.LayoutLegend(available, legendWidths, icon, 0f, 4f * scale, 22f, legendX);
            }

            float y = MinimapBoardLayout.FooterHeight * 0.5f;
            for (int i = 0; i < legendIcons.Length; i++)
            {
                var iconRect = legendIcons[i].rectTransform;
                // 중심 피벗: 포탈 아이콘이 제자리에서 돌도록 한다.
                iconRect.anchoredPosition = new Vector2(LegendLeft + legendX[i] + icon * 0.5f, y);
                iconRect.sizeDelta = new Vector2(icon, icon);
            }

            int last = legendIcons.Length - 1;
            LegendRightEdge = LegendLeft + legendX[last] + icon;
            HintLeftEdge = frameWidth - divider;
        }

        private void UpdateHint()
        {
            if (hint == null) return;
            string next = timeline.Mode == MinimapBoardMode.Square ? CloseHintLabel
                : timeline.Mode == MinimapBoardMode.Wide ? ShrinkHintLabel
                : hint.text;
            if (string.IsNullOrEmpty(next)) next = ShrinkHintLabel;
            if (hint.text != next) hint.text = next;
        }

        private void SetBoardRendered(bool shown)
        {
            if (aperture == null) return;
            SetActive(aperture, shown);
            if (!shown)
            {
                SetActive(beamTop, false);
                SetActive(beamBottom, false);
            }
        }

        private void MeasurePlayerShape()
        {
            hasPlayerShape = false;
            if (player == null) return;
            var collider = player.GetComponent<Collider2D>();
            if (collider == null) return;
            Bounds bounds = collider.bounds;
            if (bounds.size.y <= 0f || bounds.size.x <= 0f) return;
            // 발 기준점 = 콜라이더 하단 중앙. 이후에는 실제 Transform 위치에 이 오프셋만 더한다.
            playerFeetOffset = new Vector2(bounds.center.x - player.position.x, bounds.min.y - player.position.y);
            playerSize = new Vector2(bounds.size.x, bounds.size.y);
            hasPlayerShape = true;
        }

        private void GetPlayerCells(out Vector2 feet, out Vector2 bodyCells)
        {
            Vector3 stride = CellStride();
            if (hasPlayerShape)
            {
                feet = WorldToCellSpace(player.position + (Vector3)playerFeetOffset);
                bodyCells = new Vector2(playerSize.x / stride.x, playerSize.y / stride.y);
                return;
            }

            feet = WorldToCellSpace(player.position);
            bodyCells = FallbackPlayerCells;
        }

        private Rect CameraCellRect()
        {
            float distance = Mathf.Abs(worldCamera.transform.position.z - terrain.transform.position.z);
            Vector2 a = WorldToCellSpace(worldCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distance)));
            Vector2 b = WorldToCellSpace(worldCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distance)));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private Vector3 CellStride()
        {
            Vector3 stride = Vector3.one;
            if (terrain != null && terrain.layoutGrid != null)
                stride = terrain.layoutGrid.cellSize + terrain.layoutGrid.cellGap;
            return new Vector3(Mathf.Max(0.0001f, stride.x), Mathf.Max(0.0001f, stride.y), 1f);
        }

        private void SeedFacilities(WorldSnapshotDto snapshot)
        {
            facilities.ClearBuildings();
            facilityInstances.Clear();
            facilityPower.Clear();
            if (snapshot.buildings == null || snapshot.buildings.Count == 0) return;
            // 복원 이벤트는 바인딩 전에 지나가므로 스냅샷과 실제 시설 위치로 한 번만 맞춘다.
            var live = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude);
            foreach (BuildingSnapshotDto building in snapshot.buildings)
            {
                if (string.IsNullOrEmpty(building.instanceId)) continue;
                BuildingInstance instance = null;
                for (int i = 0; i < live.Length; i++)
                {
                    if (live[i] != null && live[i].InstanceId == building.instanceId)
                    {
                        instance = live[i];
                        break;
                    }
                }

                var size = building.footprintWidth > 0 && building.footprintHeight > 0
                    ? new Vector2Int(building.footprintWidth, building.footprintHeight)
                    : Vector2Int.one;
                var origin = new Vector2Int(building.x, building.y);
                // 저장값보다 실제 설치된 위치(점유 중심)에서 역산한 크기를 우선한다.
                if (instance != null && terrain != null
                    && MinimapFacilityRegistry.TryDeriveFootprint(WorldToCellSpace(instance.transform.position), origin, out var derived))
                    size = derived;
                Track(building.instanceId, building.buildingTypeId,
                    MinimapFacilityRegistry.FootprintCells(origin.x, origin.y, size.x, size.y), false, instance);
            }
        }

        private void Track(string id, string buildingId, Rect cells, bool newlyBuilt, BuildingInstance instance)
        {
            facilities.Upsert(id, buildingId, cells, newlyBuilt, Time.unscaledTime);
            if (instance == null) return;
            facilityInstances[id] = instance;
            var node = instance.GetComponent<PowerNode>();
            if (node == null) return;
            facilityPower[id] = node;
            facilities.SetActive(id, IsNodeActive(node));
        }

        /// <summary>네트워크에 속한 전력 노드가 실제로 전력을 못 받을 때만 비활성으로 본다.</summary>
        private static bool IsNodeActive(PowerNode node) => node.Network == null || node.IsPowered;

        private void PollFacilityStates()
        {
            scratchIds.Clear();
            foreach (var pair in facilityInstances)
            {
                // 철거·파괴되어 사라진 시설은 실제 위치에서 제거한다.
                if (pair.Value == null) scratchIds.Add(pair.Key);
            }

            for (int i = 0; i < scratchIds.Count; i++)
            {
                facilityInstances.Remove(scratchIds[i]);
                facilityPower.Remove(scratchIds[i]);
                facilities.Remove(scratchIds[i]);
            }

            foreach (var pair in facilityPower)
            {
                if (pair.Value != null) facilities.SetActive(pair.Key, IsNodeActive(pair.Value));
            }
        }

        private void RefreshElevators()
        {
            scratchIds.Clear();
            var records = facilities.Records;
            for (int i = 0; i < records.Count; i++)
                if (records[i].Kind == MinimapFacilityKind.Elevator) scratchIds.Add(records[i].Id);
            for (int i = 0; i < scratchIds.Count; i++) facilities.Remove(scratchIds[i]);
            if (terrain == null || elevators == null) return;
            for (int i = 0; i < elevators.Length; i++)
            {
                var elevator = elevators[i];
                if (elevator == null) continue;
                try
                {
                    // 승강로 점유 범위(시설 배치 금지 영역)를 그대로 셀 좌표로 옮긴다.
                    Bounds bounds = elevator.GetPlacementExclusionBounds();
                    Vector2 min = WorldToCellSpace(bounds.min);
                    Vector2 max = WorldToCellSpace(bounds.max);
                    facilities.Upsert("elevator:" + i, "elevator", Rect.MinMaxRect(
                        Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)),
                        false, Time.unscaledTime);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[SubTerra] Minimap elevator bounds unavailable: " + exception.GetType().Name);
                }
            }
        }

        private void OnBuildingPlaced(BuildingPlacementResult result) => TrackResult(result, true);

        private void OnBuildingRestored(BuildingPlacementResult result) => TrackResult(result, false);

        private void TrackResult(BuildingPlacementResult result, bool newlyBuilt)
        {
            if (!result.IsSuccess || string.IsNullOrEmpty(result.InstanceId)) return;
            Vector2Int size = result.Footprint;
            Track(result.InstanceId, result.BuildingId,
                MinimapFacilityRegistry.FootprintCells(result.Cell.x, result.Cell.y, size.x, size.y),
                newlyBuilt, FindInstance(result.InstanceId));
        }

        private static BuildingInstance FindInstance(string id)
        {
            var live = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude);
            for (int i = 0; i < live.Length; i++)
                if (live[i] != null && live[i].InstanceId == id) return live[i];
            return null;
        }

        private void OnWorldRestorePreparing()
        {
            // 광산 재생성·복원 직전: 이전 시설·점등 잔상을 비운다. 지형은 다음 갱신에서 실제 타일을 다시 읽는다.
            facilities.ClearBuildings();
            facilityInstances.Clear();
            facilityPower.Clear();
            minedFlashes.Clear();
            terrainDirty = true;
        }

        private void OnTilemapChanged(Tilemap map, Tilemap.SyncTile[] _)
        {
            if (map == terrain) terrainDirty = true;
        }

        private void SubscribePlacement()
        {
            if (placementSubscribed || placement == null) return;
            placement.BuildingPlaced += OnBuildingPlaced;
            placement.BuildingRestored += OnBuildingRestored;
            placement.WorldRestorePreparing += OnWorldRestorePreparing;
            placementSubscribed = true;
        }

        private void UnsubscribePlacement()
        {
            if (!placementSubscribed) return;
            if (placement != null)
            {
                placement.BuildingPlaced -= OnBuildingPlaced;
                placement.BuildingRestored -= OnBuildingRestored;
                placement.WorldRestorePreparing -= OnWorldRestorePreparing;
            }

            placementSubscribed = false;
        }

        private void SubscribeTilemap()
        {
            if (tilemapSubscribed) return;
            Tilemap.tilemapTileChanged += OnTilemapChanged;
            tilemapSubscribed = true;
        }

        private void UnsubscribeTilemap()
        {
            if (!tilemapSubscribed) return;
            Tilemap.tilemapTileChanged -= OnTilemapChanged;
            tilemapSubscribed = false;
        }

        private void PlaceAboveDarknessOverlay()
        {
            var parent = root.parent;
            if (parent == null) return;
            if (darknessOverlay == null || darknessOverlay.parent != parent)
            {
                darknessOverlay = null;
                for (int i = 0; i < parent.childCount; i++)
                {
                    if (parent.GetChild(i).name == DarknessOverlayName)
                    {
                        darknessOverlay = parent.GetChild(i);
                        break;
                    }
                }
            }

            // 암전 오버레이 바로 위에 두어 지하에서 추가로 어두워지지 않게 한다.
            int target = darknessOverlay != null ? darknessOverlay.GetSiblingIndex() + 1 : 0;
            if (target >= parent.childCount) target = parent.childCount - 1;
            if (root.GetSiblingIndex() != target) root.SetSiblingIndex(target);
        }

        private void EnsureHierarchy()
        {
            if (hierarchyReady) return;
            root = (RectTransform)transform;
            rootGroup = GetComponent<CanvasGroup>();
            if (rootGroup == null) rootGroup = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = opacity.Alpha;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
            rootGroup.ignoreParentGroups = false;

            aperture = Child(root, ApertureName, typeof(RectMask2D));
            aperture.anchorMin = Vector2.zero;
            aperture.anchorMax = Vector2.one;
            aperture.pivot = new Vector2(0.5f, 0.5f);

            board = Child(aperture, BoardName);
            board.anchorMin = board.anchorMax = new Vector2(0.5f, 0.5f);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.anchoredPosition = Vector2.zero;

            var chrome = Child(board, "Chrome", typeof(CanvasGroup));
            Stretch(chrome);
            chromeGroup = Group(chrome);
            frameGraphic = Graphic<MinimapFrameGraphic>(chrome, "Frame");
            Stretch(frameGraphic.rectTransform);

            var font = FacilityProximityLabelController.ResolveKoreanFont();
            title = Text(chrome, "Title", TitleLabel, 14f, MinimapPalette.Title, TextAlignmentOptions.MidlineLeft, font);
            title.fontStyle = FontStyles.Bold;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(30f, -MinimapBoardLayout.HeaderHeight * 0.5f);
            titleRect.sizeDelta = new Vector2(220f, MinimapBoardLayout.HeaderHeight);

            var footer = Child(chrome, "Footer");
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = Vector2.zero;
            footer.anchoredPosition = Vector2.zero;
            footer.sizeDelta = new Vector2(0f, MinimapBoardLayout.FooterHeight);
            for (int i = 0; i < LegendKinds.Length; i++)
            {
                var icon = Graphic<MinimapGlyphGraphic>(footer, "LegendIcon" + i);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = Vector2.zero;
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.Set(MinimapPalette.GlyphForKind(LegendKinds[i]), MinimapPalette.ForKind(LegendKinds[i]));
                legendIcons[i] = icon;
            }

            hint = Text(footer, "Hint", ShrinkHintLabel, 13f, MinimapPalette.Title, TextAlignmentOptions.Center, font);
            hintRect = hint.rectTransform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = Vector2.zero;
            hintBlockWidth = Mathf.Max(Measure(hint, 13f, ShrinkHintLabel), Measure(hint, 13f, CloseHintLabel)) + HintPadding * 2f;

            viewport = Child(board, ViewportName, typeof(RectMask2D));
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = Vector2.zero;
            viewport.offsetMin = new Vector2(MinimapBoardLayout.ViewportSideInset, MinimapBoardLayout.FooterHeight + MinimapBoardLayout.ViewportGap);
            viewport.offsetMax = new Vector2(-MinimapBoardLayout.ViewportSideInset, -(MinimapBoardLayout.HeaderHeight + MinimapBoardLayout.ViewportGap));

            content = Child(viewport, "Content");
            content.anchorMin = content.anchorMax = Vector2.zero;
            content.pivot = Vector2.zero;
            terrainGraphic = Graphic<MinimapTerrainGraphic>(content, "Terrain", typeof(CanvasGroup));
            Stretch(terrainGraphic.rectTransform);
            terrainGraphic.rectTransform.pivot = Vector2.zero;
            terrainGroup = Group(terrainGraphic.rectTransform);
            facilityGraphic = Graphic<MinimapFacilityGraphic>(content, "Facilities", typeof(CanvasGroup));
            Stretch(facilityGraphic.rectTransform);
            facilityGraphic.rectTransform.pivot = Vector2.zero;
            facilityGroup = Group(facilityGraphic.rectTransform);

            playerMarker = Child(viewport, PlayerMarkerName, typeof(CanvasGroup));
            playerMarker.anchorMin = playerMarker.anchorMax = Vector2.zero;
            playerMarker.pivot = new Vector2(0.5f, 0f);
            playerGroup = Group(playerMarker);
            ring = Graphic<MinimapGlyphGraphic>(playerMarker, "Ring");
            ringRect = ring.rectTransform;
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ring.Set(MinimapGlyph.Ring, MinimapPalette.Beacon);
            beacon = Graphic<MinimapGlyphGraphic>(playerMarker, "Beacon");
            beaconRect = beacon.rectTransform;
            beaconRect.anchorMin = beaconRect.anchorMax = new Vector2(0.5f, 0f);
            beaconRect.pivot = new Vector2(0.5f, 0.5f);
            beacon.Set(MinimapGlyph.Beacon, MinimapPalette.Beacon);

            scanLine = Child(viewport, ScanLineName, typeof(CanvasRenderer), typeof(Image));
            scanLine.anchorMin = new Vector2(0f, 1f);
            scanLine.anchorMax = new Vector2(1f, 1f);
            scanLine.pivot = new Vector2(0.5f, 0.5f);
            scanLine.sizeDelta = new Vector2(0f, 2f);
            scanImage = Img(scanLine, MinimapPalette.Scan);

            beamTop = Child(root, "BeamTop", typeof(CanvasRenderer), typeof(Image));
            beamBottom = Child(root, "BeamBottom", typeof(CanvasRenderer), typeof(Image));
            foreach (var beam in new[] { beamTop, beamBottom })
            {
                beam.anchorMin = new Vector2(0f, 0.5f);
                beam.anchorMax = new Vector2(1f, 0.5f);
                beam.pivot = new Vector2(0.5f, 0.5f);
                beam.sizeDelta = new Vector2(0f, 2f);
            }

            beamTopImage = Img(beamTop, MinimapPalette.Beam);
            beamBottomImage = Img(beamBottom, MinimapPalette.Beam);
            hierarchyReady = true;
            appliedFrameWidth = -1f;
            ApplyFrameWidth(CurrentFrameWidth);
            SetBoardRendered(timeline.IsVisible);
        }

        private static float Measure(TMP_Text text, float size, string value = null)
        {
            string sample = value ?? text.text;
            float previous = text.fontSize;
            text.fontSize = size;
            float measured = text.font != null ? text.GetPreferredValues(sample, 1000f, 100f).x : 0f;
            text.fontSize = previous;
            // 폰트가 없는 환경(테스트)에서도 넘치지 않도록 글자 수 기반 상한을 쓴다.
            return measured > 0f ? measured : sample.Length * size;
        }

        private static RectTransform Child(Transform parent, string name, params Type[] components)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var created = new GameObject(name, types);
            created.layer = parent.gameObject.layer;
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        private static T Graphic<T>(Transform parent, string name, params Type[] extra) where T : MaskableGraphic
        {
            // CanvasRenderer를 먼저 붙여 마스크 등록·해제 시점에도 항상 존재하게 한다.
            var types = new Type[extra.Length + 1];
            types[0] = typeof(CanvasRenderer);
            Array.Copy(extra, 0, types, 1, extra.Length);
            var rect = Child(parent, name, types);
            var graphic = rect.GetComponent<T>();
            if (graphic == null) graphic = rect.gameObject.AddComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private static CanvasGroup Group(Component target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static Image Img(RectTransform rect, Color color)
        {
            var image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        private static TMP_Text Text(Transform parent, string name, string value, float size, Color color,
            TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var rect = Child(parent, name, typeof(CanvasRenderer));
            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null) text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null && target.gameObject.activeSelf != active) target.gameObject.SetActive(active);
        }
    }
}
