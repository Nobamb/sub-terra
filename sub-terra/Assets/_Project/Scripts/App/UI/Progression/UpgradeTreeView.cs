using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 업그레이드 트리(왼쪽 주 영역)와 선택 항목 상세 패널(오른쪽)을 그린다.
    /// ProgressionPanelView가 서비스 결과를 넘겨 주면 표시만 갱신하고,
    /// 비용 차감·레벨·해금 판정은 ProgressionService에 둔다.
    /// 미해금 노드는 조건 문구만 노출하며 이름·아이콘·효과는 상세 패널에도 내보내지 않는다.
    /// </summary>
    public sealed class UpgradeTreeView : MonoBehaviour
    {
        private const float UnlockFlowDuration = 0.22f;
        private const float UnlockStagger = 0.08f;
        private const float ShortageFlashDuration = 0.35f;

        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private Vector2 contentSize = new Vector2(1100f, 560f);
        [SerializeField] private UpgradeTreeNodeView[] nodes = Array.Empty<UpgradeTreeNodeView>();
        [SerializeField] private UpgradeTreeConnector[] connectors = Array.Empty<UpgradeTreeConnector>();

        [Header("Detail")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailQuestion;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailLevel;
        [SerializeField] private TMP_Text detailDescription;
        [SerializeField] private GameObject effectGroup;
        [SerializeField] private TMP_Text effectName;
        [SerializeField] private TMP_Text effectCurrent;
        [SerializeField] private TMP_Text effectNext;
        [SerializeField] private GameObject effectArrow;
        [SerializeField] private GameObject effectNextCaption;
        [SerializeField] private GameObject costGroup;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseLabel;
        [SerializeField] private CanvasGroup purchaseGroup;
        [SerializeField] private GameObject maxBadge;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text deepZoneText;

        private readonly List<UpgradeSnapshot> snapshots = new List<UpgradeSnapshot>();
        private string selectedId = string.Empty;
        private bool busy;
        private Vector2 lastViewportSize;
        private Coroutine flashRoutine;
        private float shortageFlash;
        private float fitScale = 1f;
        private float zoom = 1f;
        private float zoomTarget = 1f;
        private Vector2 pan;
        private Vector2 panTarget;

        private const float ZoomSharpness = 14f;

        public string SelectedId => selectedId;
        public float ZoomTarget => zoomTarget;
        public Vector2 PanTarget => panTarget;
        public RectTransform Viewport => viewport;
        public RectTransform Content => content;
        public IReadOnlyList<UpgradeTreeNodeView> Nodes => nodes;
        public IReadOnlyList<UpgradeTreeConnector> Connectors => connectors;
        public bool PurchaseButtonVisible => purchaseButton != null && purchaseButton.gameObject.activeSelf;

        private void OnEnable()
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].Clicked -= OnNodeClicked;
                    nodes[i].Clicked += OnNodeClicked;
                }
            }

            lastViewportSize = Vector2.zero;
            // 다시 열 때는 기본 배율·가운데 정렬로 시작한다.
            zoom = zoomTarget = 1f;
            pan = panTarget = Vector2.zero;
            StopFlash();
            FitContent();
            ApplyZoom();
        }

        private void OnDisable()
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].Clicked -= OnNodeClicked;
                }
            }

            StopFlash();
        }

        private void LateUpdate()
        {
            FitContent();
            if (!Mathf.Approximately(zoom, zoomTarget) || pan != panTarget)
            {
                var k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * ZoomSharpness);
                zoom = Mathf.Abs(zoomTarget - zoom) < 0.001f ? zoomTarget : Mathf.Lerp(zoom, zoomTarget, k);
                pan = (panTarget - pan).sqrMagnitude < 0.01f ? panTarget : Vector2.Lerp(pan, panTarget, k);
                ApplyZoom();
            }
        }

        // 패널 크기가 달라도 트리가 잘리지 않고 읽히도록 균일 비율로 맞춘다.
        private void FitContent()
        {
            if (viewport == null || content == null || contentSize.x <= 0f || contentSize.y <= 0f)
            {
                return;
            }

            var size = viewport.rect.size;
            if (size == lastViewportSize || size.x <= 1f || size.y <= 1f)
            {
                return;
            }

            lastViewportSize = size;
            fitScale = Mathf.Clamp(Mathf.Min(size.x / contentSize.x, size.y / contentSize.y), 0.4f, 1.3f);
            panTarget = UpgradeTreeZoom.ClampPan(panTarget, contentSize, fitScale * zoomTarget, size);
            pan = UpgradeTreeZoom.ClampPan(pan, contentSize, fitScale * zoom, size);
            ApplyZoom();
        }

        /// <summary>
        /// prompt-B 118-1: 업그레이드 창 위에서 위로 스크롤하면 확대, 아래로 스크롤하면 축소한다.
        /// 포인터가 트리 영역 안이면 그 지점을 기준으로, 밖이면 현재 화면 가운데를 기준으로 배율을 바꾼다.
        /// </summary>
        public void HandleScroll(Vector2 screenPosition, float scrollY, Camera eventCamera)
        {
            if (viewport == null || content == null || Mathf.Approximately(scrollY, 0f))
            {
                return;
            }

            FitContent();
            var next = UpgradeTreeZoom.NextZoom(zoomTarget, scrollY);
            if (Mathf.Approximately(next, zoomTarget))
            {
                return;
            }

            var pivot = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPosition, eventCamera, out var local)
                && viewport.rect.Contains(local))
            {
                pivot = local - viewport.rect.center;
            }

            var moved = UpgradeTreeZoom.PanAround(panTarget, fitScale * zoomTarget, fitScale * next, pivot);
            zoomTarget = next;
            panTarget = UpgradeTreeZoom.ClampPan(moved, contentSize, fitScale * zoomTarget, viewport.rect.size);
        }

        /// <summary>보간 없이 현재 목표 배율·위치로 즉시 맞춘다(창 크기 변경 직후, 테스트용).</summary>
        public void SnapLayout()
        {
            lastViewportSize = Vector2.zero;
            FitContent();
            zoom = zoomTarget;
            pan = panTarget;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (content == null)
            {
                return;
            }

            var scale = fitScale * zoom;
            content.localScale = new Vector3(scale, scale, 1f);
            content.anchoredPosition = pan;
        }

        public UpgradeTreeNodeView FindNode(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return null;
            }

            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && nodes[i].UpgradeId == upgradeId)
                {
                    return nodes[i];
                }
            }

            return null;
        }

        public UpgradeTreeConnector FindConnectorToChild(string childId)
        {
            for (var i = 0; i < connectors.Length; i++)
            {
                if (connectors[i] != null && connectors[i].ChildId == childId)
                {
                    return connectors[i];
                }
            }

            return null;
        }

        public void Render(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            snapshots.Clear();
            if (upgrades != null)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    snapshots.Add(upgrades[i]);
                }
            }

            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                var found = false;
                for (var s = 0; s < snapshots.Count; s++)
                {
                    if (snapshots[s].UpgradeId == node.UpgradeId)
                    {
                        node.Apply(snapshots[s]);
                        found = true;
                        break;
                    }
                }

                node.gameObject.SetActive(found);
            }

            for (var i = 0; i < connectors.Length; i++)
            {
                var connector = connectors[i];
                if (connector == null)
                {
                    continue;
                }

                var child = FindSnapshot(connector.ChildId);
                connector.SetChildUnlocked(child.HasValue && child.Value.IsUnlocked);
            }
        }

        private UpgradeSnapshot? FindSnapshot(string upgradeId)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].UpgradeId == upgradeId)
                {
                    return snapshots[i];
                }
            }

            return null;
        }

        public void SetBusy(bool value)
        {
            busy = value;
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public void SetDeepZoneStatus(ZoneAccessResult access)
        {
            if (deepZoneText == null)
            {
                return;
            }

            deepZoneText.text = access.IsUnlocked
                ? "심층 구역 · 해금됨"
                : "심층 구역 · 잠금" + (string.IsNullOrEmpty(access.Reason) ? string.Empty : " — " + access.Reason);
        }

        // ---------- 상세 패널 ----------

        public void ShowDetail(UpgradeSnapshot upgrade)
        {
            if (selectedId != upgrade.UpgradeId)
            {
                SetMessage(string.Empty);
            }

            selectedId = upgrade.UpgradeId;
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].SetSelected(nodes[i].UpgradeId == selectedId);
                }
            }

            StopFlash();
            if (!upgrade.IsUnlocked)
            {
                ShowLockedDetail(upgrade);
            }
            else
            {
                ShowUnlockedDetail(upgrade);
            }
        }

        // 미해금: 이름·아이콘·효과·설명을 모두 숨기고 해금 조건만 안내한다.
        private void ShowLockedDetail(UpgradeSnapshot upgrade)
        {
            SetActive(detailIcon, false);
            SetActive(detailQuestion, true);
            SetText(detailName, "???");
            SetText(detailLevel, string.Empty);
            SetText(detailDescription, "아직 해금되지 않은 업그레이드입니다.");
            SetActive(effectGroup, false);
            SetActive(costGroup, false);
            SetActive(maxBadge, false);
            SetText(statusText, "잠금");
            SetColor(statusText, UpgradeTreeTween.TextDim);
            SetText(hintText, upgrade.LockedReason);
            SetPurchaseVisible(false);
        }

        private void ShowUnlockedDetail(UpgradeSnapshot upgrade)
        {
            var node = FindNode(upgrade.UpgradeId);
            var sprite = node != null ? node.IconSprite : null;
            if (detailIcon != null)
            {
                // 잠금 상세가 오브젝트를 꺼 둔 채 남지 않도록 다시 켠다.
                detailIcon.gameObject.SetActive(true);
                detailIcon.sprite = sprite;
                detailIcon.enabled = sprite != null;
            }

            SetActive(detailQuestion, false);
            SetText(detailName, ItemDisplayNames.PreferDisplay(upgrade.UpgradeId, upgrade.DisplayName));
            SetText(detailLevel, "Lv." + upgrade.CurrentLevel + " / " + upgrade.MaximumLevel);

            var description = ItemDisplayNames.UpgradeDescription(upgrade.UpgradeId);
            var unlockNote = ItemDisplayNames.UpgradeUnlockDescription(upgrade.UpgradeId, upgrade.CurrentLevel);
            SetText(detailDescription, string.IsNullOrEmpty(unlockNote) ? description : description + "\n" + unlockNote);

            BuildEffect(upgrade);
            BuildCost(upgrade);

            var maxed = upgrade.IsMaximumLevel;
            SetActive(maxBadge, maxed);
            SetActive(costGroup, !maxed);
            if (maxed)
            {
                SetText(statusText, string.Empty);
                SetText(hintText, string.Empty);
                SetPurchaseVisible(false);
                return;
            }

            if (upgrade.CanAffordNextLevel)
            {
                SetText(statusText, "구매 가능");
                SetColor(statusText, UpgradeTreeTween.Cyan);
            }
            else
            {
                SetText(statusText, "재료 부족");
                SetColor(statusText, UpgradeTreeTween.Warning);
            }

            SetText(hintText, BuildHint(upgrade));
            SetPurchaseVisible(true);
            if (purchaseLabel != null)
            {
                purchaseLabel.text = upgrade.CanAffordNextLevel ? "업그레이드" : "재료 부족";
            }

            if (purchaseGroup != null)
            {
                // 부족 상태도 눌러서 실패 피드백을 받을 수 있게 interactable은 유지하고 시각만 낮춘다.
                purchaseGroup.alpha = upgrade.CanAffordNextLevel ? 1f : 0.55f;
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = !busy;
            }
        }

        private string BuildHint(UpgradeSnapshot upgrade)
        {
            var builder = new StringBuilder();
            if (upgrade.NextCostShortages.Count > 0)
            {
                for (var i = 0; i < upgrade.NextCostShortages.Count; i++)
                {
                    var shortage = upgrade.NextCostShortages[i];
                    if (builder.Length > 0)
                    {
                        builder.Append(" · ");
                    }

                    builder.Append(ItemName(shortage.ItemId)).Append(' ').Append(shortage.Quantity).Append(" 부족");
                }
            }

            // 드릴 속도: 다음 레벨에서 몇 개가 열리는지만 알려 준다(이름은 공개하지 않는다).
            var opens = CountOpensAtNextLevel(upgrade);
            if (opens > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("Lv.").Append(upgrade.CurrentLevel + 1)
                    .Append(" 달성 시 주변 업그레이드 ").Append(opens).Append("개 해금");
            }

            return builder.ToString();
        }

        private int CountOpensAtNextLevel(UpgradeSnapshot upgrade)
        {
            var count = 0;
            for (var i = 0; i < snapshots.Count; i++)
            {
                var other = snapshots[i];
                if (!other.IsUnlocked
                    && other.UnlockRequirementUpgradeId == upgrade.UpgradeId
                    && other.UnlockRequiredLevel == upgrade.CurrentLevel + 1)
                {
                    count++;
                }
            }

            return count;
        }

        private void BuildEffect(UpgradeSnapshot upgrade)
        {
            SetActive(effectGroup, true);
            var maxed = upgrade.IsMaximumLevel;
            if (upgrade.UpgradeId == DataIds.Upgrades.CargoYield)
            {
                SetText(effectName, "채굴 추가 획득");
                SetText(effectCurrent, FormatYield(upgrade.CurrentMiningYieldBonuses));
                SetText(effectNext, maxed ? string.Empty : FormatYield(upgrade.NextMiningYieldBonuses));
            }
            else
            {
                SetText(effectName, UpgradeEffectFormatter.EffectName(upgrade.UpgradeId));
                SetText(
                    effectCurrent,
                    UpgradeEffectFormatter.FormatValue(upgrade.UpgradeId, upgrade.CurrentEffectValue));
                SetText(
                    effectNext,
                    maxed ? string.Empty : UpgradeEffectFormatter.FormatValue(upgrade.UpgradeId, upgrade.NextEffectValue));
            }

            SetActive(effectArrow, !maxed);
            SetActive(effectNextCaption, !maxed);
            SetActive(effectNext, !maxed);
        }

        private static string FormatYield(IReadOnlyList<MineralBonusEntry> bonuses)
        {
            var builder = new StringBuilder();
            if (bonuses != null)
            {
                for (var i = 0; i < bonuses.Count; i++)
                {
                    var entry = bonuses[i];
                    if (entry == null || entry.Quantity <= 0)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(ItemDisplayNames.Mineral(entry.MineralId)).Append(" +").Append(entry.Quantity);
                }
            }

            return builder.Length > 0 ? builder.ToString() : "없음";
        }

        private void BuildCost(UpgradeSnapshot upgrade)
        {
            if (costText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < upgrade.NextCosts.Count; i++)
            {
                var cost = upgrade.NextCosts[i];
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                var missing = MissingOf(upgrade, cost.ItemId);
                if (missing > 0)
                {
                    builder.Append("<color=#FF6B52>")
                        .Append(ItemName(cost.ItemId)).Append(" ×").Append(cost.Quantity)
                        .Append("  (").Append(missing).Append(" 부족)</color>");
                }
                else
                {
                    builder.Append(ItemName(cost.ItemId)).Append(" ×").Append(cost.Quantity);
                }
            }

            costText.text = builder.Length > 0 ? builder.ToString() : "없음";
            costText.color = UpgradeTreeTween.TextMain;
        }

        private static int MissingOf(UpgradeSnapshot upgrade, string itemId)
        {
            for (var i = 0; i < upgrade.NextCostShortages.Count; i++)
            {
                if (upgrade.NextCostShortages[i].ItemId == itemId)
                {
                    return upgrade.NextCostShortages[i].Quantity;
                }
            }

            return 0;
        }

        private static string ItemName(string itemId)
        {
            return itemId == DataIds.Currency.Gold ? "골드" : ItemDisplayNames.Mineral(itemId);
        }

        private void SetPurchaseVisible(bool visible)
        {
            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(visible);
            }
        }

        // ---------- 구매 결과 연출 ----------

        /// <summary>서비스가 확정한 결과만 연출한다. 실패는 흔들림, 성공은 레벨업/해금.</summary>
        public void PlayPurchaseFeedback(ProgressionPurchaseResult result)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var node = FindNode(result.UpgradeId);
            if (result.IsSuccess)
            {
                if (node != null)
                {
                    node.PlayLevelUp();
                }

                var unlocked = result.NewlyUnlockedUpgradeIds;
                for (var i = 0; i < unlocked.Count; i++)
                {
                    var target = FindNode(unlocked[i]);
                    if (target == null)
                    {
                        continue;
                    }

                    // Refresh가 노드를 갱신하기 전에 블라인드 유지를 먼저 걸어 둔다.
                    target.HoldBlindForReveal();
                    var connector = FindConnectorToChild(unlocked[i]);
                    if (connector != null)
                    {
                        connector.PlayFlow(UnlockFlowDuration);
                    }

                    target.PlayUnlock(UnlockFlowDuration + UnlockStagger * i);
                }

                return;
            }

            if (result.Status == ProgressionPurchaseStatus.InsufficientResources
                || result.Status == ProgressionPurchaseStatus.Locked)
            {
                if (node != null)
                {
                    node.PlayShake();
                }

                if (result.Status == ProgressionPurchaseStatus.InsufficientResources)
                {
                    FlashShortage();
                }
            }
        }

        private void FlashShortage()
        {
            if (costText == null && statusText == null)
            {
                return;
            }

            StopFlash();
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            yield return UpgradeTreeTween.Run(ShortageFlashDuration, t =>
            {
                shortageFlash = 1f - t;
                if (costText != null)
                {
                    costText.color = Color.Lerp(UpgradeTreeTween.TextMain, UpgradeTreeTween.Warning, shortageFlash);
                }

                if (statusText != null)
                {
                    var pop = 1f + 0.12f * shortageFlash;
                    statusText.rectTransform.localScale = new Vector3(pop, pop, 1f);
                }
            });
            ResetFlash();
            flashRoutine = null;
        }

        private void StopFlash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            ResetFlash();
        }

        private void ResetFlash()
        {
            shortageFlash = 0f;
            if (costText != null)
            {
                costText.color = UpgradeTreeTween.TextMain;
            }

            if (statusText != null)
            {
                statusText.rectTransform.localScale = Vector3.one;
            }
        }

        // ---------- 보조 ----------

        private void OnNodeClicked(string upgradeId)
        {
            var owner = GetComponentInParent<ProgressionPanelView>(true);
            if (owner != null)
            {
                owner.SelectUpgradeEntry(upgradeId);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        private static void SetColor(TMP_Text label, Color color)
        {
            if (label != null)
            {
                label.color = color;
            }
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
