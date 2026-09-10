using System.Collections.Generic;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>현재 카메라 영역의 지형과 채굴 기록을 조명에 영향받지 않는 HUD로 표시한다.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ExplorationMinimap : MaskableGraphic
    {
        /// <summary>화면 면적 대비 미니맵 비율.</summary>
        public const float ScreenAreaRatio = 0.1f;
        /// <summary>기본 패널 투명도. 지상·지하 모두 항상 50%.</summary>
        public const float PanelOpacity = 0.5f;
        /// <summary>Ctrl+M을 누르고 있는 동안의 불투명도.</summary>
        public const float OpaqueOpacity = 1f;
        /// <summary>미니맵 하단 닫기 안내.</summary>
        public const string CloseHintLabel = "닫기: M";
        /// <summary>우측 최하단 구석 앵커·피벗.</summary>
        public static readonly Vector2 BottomRightCorner = new(1f, 0f);

        private const float HintBarHeight = 20f;
        private const string CloseHintName = "CloseHint";
        private const string DarknessOverlayName = "DepthDarknessOverlay";

        private readonly HashSet<Vector3Int> minedCells = new();
        private Tilemap terrain;
        private Transform player;
        private Camera worldCamera;
        private CanvasGroup fadeGroup;
        private TMP_Text closeHint;
        private bool visible = true;
        private bool wasMapKeyDown;
        private bool opaqueHold;

        public bool IsMapVisible => visible;
        public bool IsOpaqueHold => opaqueHold;
        public int MinedCellCount => minedCells.Count;
        public float DisplayOpacity => fadeGroup != null ? fadeGroup.alpha : 0f;
        public string CloseHintText => closeHint != null ? closeHint.text : string.Empty;

        public void Bind(Tilemap map, Transform target, WorldSnapshotDto snapshot)
        {
            terrain = map;
            player = target;
            worldCamera = Camera.main;
            raycastTarget = false;
            ApplyHudLayout();
            RestoreMining(snapshot);
        }

        /// <summary>우측 최하단 구석에 고정하고 투명도를 항상 50%로 맞춘다.</summary>
        public void ApplyHudLayout()
        {
            var rect = rectTransform;
            if (rect.anchorMin != BottomRightCorner) rect.anchorMin = BottomRightCorner;
            if (rect.anchorMax != BottomRightCorner) rect.anchorMax = BottomRightCorner;
            if (rect.pivot != BottomRightCorner) rect.pivot = BottomRightCorner;
            if (rect.anchoredPosition != Vector2.zero) rect.anchoredPosition = Vector2.zero;
            // Graphic.color 틴트는 지하 암전 오버레이와 겹치면 체감 투명도가 달라진다.
            // 정점색은 불투명으로 두고 CanvasGroup.alpha로 항상 50%를 맞춘다.
            if (color != Color.white) color = Color.white;
            EnsureFadeGroup();
            EnsureCloseHint();
            PlaceAboveDarknessOverlay();
            ApplyDisplayOpacity();
            if (rect.parent is RectTransform parent)
            {
                var size = parent.rect.size * Mathf.Sqrt(ScreenAreaRatio);
                if (size.x > 0f && size.y > 0f && rect.sizeDelta != size)
                    rect.sizeDelta = size;
            }
        }

        public void RestoreMining(WorldSnapshotDto snapshot)
        {
            minedCells.Clear();
            if (snapshot != null && snapshot.miningChanges != null)
                foreach (var change in snapshot.miningChanges)
                    if (change.isDestroyed) minedCells.Add(new Vector3Int(change.x, change.y, 0));
            SetVerticesDirty();
        }

        public void RecordMining(GameplayEventDto change)
        {
            if (change != null && change.type == GameplayEventType.TileMined)
                minedCells.Add(new Vector3Int(change.x, change.y, 0));
        }

        public void ToggleMap()
        {
            // 컴포넌트를 끄지 않아 숨긴 상태에서도 M 입력을 계속 받는다.
            visible = !visible;
            if (!visible) opaqueHold = false;
            ApplyDisplayOpacity();
            SetVerticesDirty();
        }

        private void Update()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool typing = selected != null && (selected.GetComponentInParent<TMP_InputField>() != null
                || selected.GetComponentInParent<InputField>() != null);
            var keyboard = Keyboard.current;
            bool mapKeyDown = keyboard != null && keyboard.mKey.isPressed;
            bool ctrlHeld = keyboard != null
                && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
            // Ctrl+M은 토글이 아니라 누르고 있는 동안 불투명 표시다.
            if (!typing && mapKeyDown && !wasMapKeyDown && !ctrlHeld)
                ToggleMap();
            opaqueHold = !typing && visible && ctrlHeld && mapKeyDown;
            wasMapKeyDown = mapKeyDown;
        }

        private void LateUpdate()
        {
            ApplyHudLayout();
            if (!visible) return;
            if (worldCamera == null) worldCamera = Camera.main;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!visible || terrain == null || player == null || worldCamera == null) return;
            Rect outer = rectTransform.rect;
            AddRect(mesh, outer, new Color32(83, 109, 123, 255));
            Rect area = Rect.MinMaxRect(outer.xMin + 3, outer.yMin + HintBarHeight, outer.xMax - 3, outer.yMax - 3);
            AddRect(mesh, area, new Color32(13, 22, 30, 255));
            Rect hintBar = Rect.MinMaxRect(outer.xMin + 3, outer.yMin + 3, outer.xMax - 3, outer.yMin + HintBarHeight);
            AddRect(mesh, hintBar, new Color32(8, 14, 20, 255));
            float distance = Mathf.Abs(worldCamera.transform.position.z - terrain.transform.position.z);
            Vector3 bottom = worldCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
            Vector3 top = worldCamera.ViewportToWorldPoint(new Vector3(1, 1, distance));
            Vector3Int first = terrain.WorldToCell(bottom);
            Vector3Int last = terrain.WorldToCell(top);
            // 전체 월드 스캔 대신 화면 안의 셀만 그린다. 이동·채굴·붕괴가 바로 반영된다.
            for (int y = first.y; y <= last.y; y++)
                for (int x = first.x; x <= last.x; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool occupied = terrain.HasTile(cell);
                    if (!occupied && !minedCells.Contains(cell)) continue;
                    Vector3 a = worldCamera.WorldToViewportPoint(terrain.CellToWorld(cell));
                    Vector3 b = worldCamera.WorldToViewportPoint(terrain.CellToWorld(cell + new Vector3Int(1, 1, 0)));
                    Vector2 center = Project(area, (a + b) * 0.5f);
                    float side = Mathf.Min(Mathf.Abs(b.x - a.x) * area.width,
                        Mathf.Abs(b.y - a.y) * area.height) * (occupied ? 0.94f : 0.6f);
                    Rect dot = Rect.MinMaxRect(Mathf.Max(area.xMin, center.x - side / 2),
                        Mathf.Max(area.yMin, center.y - side / 2), Mathf.Min(area.xMax, center.x + side / 2),
                        Mathf.Min(area.yMax, center.y + side / 2));
                    if (dot.width > 0 && dot.height > 0)
                        AddRect(mesh, dot, occupied ? new Color32(50, 65, 76, 255) : new Color32(165, 204, 213, 255));
                }

            Vector3 position = worldCamera.WorldToViewportPoint(player.position);
            if (position.z <= 0 || position.x < 0 || position.x > 1 || position.y < 0 || position.y > 1) return;
            Vector2 marker = Project(area, position);
            float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2) + 1) * 0.5f;
            // 가장자리에서도 원과 광원이 패널 밖으로 넘치지 않게 한다.
            marker.x = Mathf.Clamp(marker.x, area.xMin + 15, area.xMax - 15);
            marker.y = Mathf.Clamp(marker.y, area.yMin + 15, area.yMax - 15);
            AddCircle(mesh, marker, 10 + pulse * 5, new Color(1, 0.35f, 0.35f, 0.08f + pulse * 0.16f));
            AddCircle(mesh, marker, 7 + pulse * 2, new Color(1, 0.45f, 0.45f, 0.12f + pulse * 0.18f));
            AddCircle(mesh, marker, 4.5f, new Color32(255, 64, 70, 255));
        }

        private void EnsureFadeGroup()
        {
            if (fadeGroup == null)
                fadeGroup = GetComponent<CanvasGroup>();
            if (fadeGroup == null)
                fadeGroup = gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            fadeGroup.ignoreParentGroups = false;
        }

        private void EnsureCloseHint()
        {
            if (closeHint == null)
            {
                var existing = transform.Find(CloseHintName);
                if (existing != null)
                    closeHint = existing.GetComponent<TMP_Text>();
            }

            if (closeHint == null)
            {
                var root = new GameObject(CloseHintName, typeof(RectTransform), typeof(CanvasRenderer));
                root.transform.SetParent(transform, false);
                closeHint = root.AddComponent<TextMeshProUGUI>();
            }

            var hintRect = closeHint.rectTransform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(6f, 4f);
            hintRect.offsetMax = new Vector2(-6f, -4f);
            closeHint.raycastTarget = false;
            closeHint.fontSize = 14f;
            closeHint.fontStyle = FontStyles.Bold;
            closeHint.alignment = TextAlignmentOptions.Bottom;
            closeHint.textWrappingMode = TextWrappingModes.NoWrap;
            closeHint.overflowMode = TextOverflowModes.Overflow;
            closeHint.color = new Color(0.92f, 0.96f, 1f, 1f);
            if (closeHint.text != CloseHintLabel)
                closeHint.text = CloseHintLabel;
            var font = FacilityProximityLabelController.ResolveKoreanFont();
            if (font != null && closeHint.font != font)
                closeHint.font = font;
        }

        private void PlaceAboveDarknessOverlay()
        {
            var parent = rectTransform.parent;
            if (parent == null) return;
            int overlayIndex = -1;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == DarknessOverlayName)
                {
                    overlayIndex = i;
                    break;
                }
            }

            // 암전 오버레이 뒤에 있으면 지하에서만 추가로 어두워진다. 바로 위에 두어 항상 같은 투명도를 유지한다.
            int target = overlayIndex >= 0 ? overlayIndex + 1 : 0;
            if (rectTransform.GetSiblingIndex() != target)
                rectTransform.SetSiblingIndex(target);
        }

        private void ApplyDisplayOpacity()
        {
            if (fadeGroup == null) return;
            float target = !visible ? 0f : (opaqueHold ? OpaqueOpacity : PanelOpacity);
            if (!Mathf.Approximately(fadeGroup.alpha, target))
                fadeGroup.alpha = target;
        }

        private static Vector2 Project(Rect area, Vector3 viewport) =>
            new(area.xMin + viewport.x * area.width, area.yMin + viewport.y * area.height);

        private static void AddRect(VertexHelper mesh, Rect rect, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddCircle(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            const int segments = 24;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
