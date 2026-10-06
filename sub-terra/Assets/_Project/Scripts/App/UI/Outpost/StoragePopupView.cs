using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 전진기지 보관함 팝업(B-138). 판매창과 같은 껍데기·버튼 규칙에 보관/꺼내기 조작을 담는다.
    /// 표시·입력 위임·연출만 한다. 이동 판정(수량 클램프·무게 검사)은 OutpostService, 선택 상태는 Presenter가 맡는다.
    /// 등장·종료는 StorageBoxTimeline의 Showbox(수납 상자 개봉) 연출이며 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class StoragePopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "보관함";
        public const string HintLabel = "보관하거나 꺼낼 자원과 수량을 고르세요.";
        public const string SelectPrompt = "자원을 선택하세요.";
        public const string PreviewPrompt = "자원을 선택하면\n옮길 수량을 표시합니다.";
        public const string DepositLabel = "선택 수량 보관";
        public const string WithdrawLabel = "선택 수량 꺼내기";
        public const string PlayerEmptyLabel = "화물이 비어 있습니다";
        public const string StorageEmptyLabel = "보관된 자원이 없습니다";
        public static readonly Vector2 CardSize = new Vector2(1400f, 840f);
        public static readonly int[] QuickQuantities = { 1, 5, 10 };

        // 좌우 여백은 판매창과 같다(프레임 모서리 장식과 겹치지 않는 값).
        private const float Pad = 72f;
        private const float ColumnGap = 24f;
        private const float ColumnW = (1400f - Pad * 2f - ColumnGap) * 0.5f;
        private const float RightX = Pad + ColumnW + ColumnGap;
        private const float TitleY = 48f;
        private const float CloseSize = 48f;
        private const float HeaderRowY = 56f;
        private const float SectionY = 188f;
        private const float StripY = 236f;
        private const float ListY = 276f;
        private const float RowPitch = StorageCargoRowView.Columns.RowHeight + StorageCargoRowView.Columns.RowGap;
        private const float ListH = 4f * RowPitch - StorageCargoRowView.Columns.RowGap;
        private const float PickerY = 522f;
        private const float ControlH = 52f;
        private const float StatusY = 582f;
        private const float FooterY = 620f;
        private const float FooterH = 136f;
        private const float ActionW = 250f;
        private const float ActionH = 88f;
        private const float OptionsH = 220f;
        private const float BoxSize = 170f;
        // 튀어나온 광물의 최대 크기는 상자(뚜껑을 연 모습 포함)의 30%.
        private const float BoxVisualSize = BoxSize * 1.6f;
        private const float PopIconSize = BoxVisualSize * 0.3f;
        private const float MaxStep = 0.05f;
        private const float BackdropAlpha = 0.62f;

        private static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);

        private sealed class CargoList
        {
            public RectTransform Content;
            public ScrollRect Scroll;
            public Scrollbar Scrollbar;
            public TMP_Text Empty;
            public TMP_Text Totals;
            public readonly List<StorageCargoRowView> Rows = new List<StorageCargoRowView>();
        }

        private readonly CargoList playerList = new CargoList();
        private readonly CargoList storageList = new CargoList();
        private readonly List<string> popIds = new List<string>();

        private TMP_FontAsset font;
        private Sprite cargoSprite;
        private Canvas canvas;
        private Image backdrop;
        private RectTransform card;
        private CanvasGroup cardGroup;
        private Image cardBlocker;
        private CanvasGroup contentGroup;
        private CanvasGroup headerGroup;
        private CanvasGroup listGroup;
        private CanvasGroup controlsGroup;

        private ResourceSellButton closeButton;
        private TMP_Text cargoNumbers;
        private RectTransform cargoFill;
        private OutpostMineralPickerView picker;
        private RectTransform optionsContent;
        private Image selectionIcon;
        private TMP_Text selectionName;
        private TMP_Text selectionCounts;
        private TMP_Text statusText;
        private TMP_InputField quantityInput;
        private ResourceSellButton[] quickButtons;
        private ResourceSellButton minusButton;
        private TMP_Text previewText;
        private ResourceSellButton depositButton;
        private ResourceSellButton withdrawButton;

        private RectTransform boxRoot;
        private StorageBoxGraphic box;
        private Image mouthGlow;
        private RectTransform popRoot;
        private Image[] pops;

        private Func<string, Sprite> iconResolver;
        private InventorySnapshot playerCargo;
        private InventorySnapshot storage;
        private string selectedId = string.Empty;
        private int owned;
        private int stored;
        private int quantity = 1;
        private bool busy;

        private PopupState state;
        private float clock;
        private float closeFrom;
        private int popCount;
        private int poppedAtClose;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsOpen => state == PopupState.Opening || state == PopupState.Open;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsInteractive => contentGroup != null && contentGroup.interactable;
        public float Clock => clock;
        public Canvas Canvas => canvas;
        public RectTransform CardRect => card;
        public float CardScale => card != null ? card.localScale.x : 0f;
        public float CardAlpha => cardGroup != null ? cardGroup.alpha : 0f;
        public OutpostMineralPickerView Picker => picker;
        public TMP_InputField QuantityInput => quantityInput;
        public ResourceSellButton CloseButton => closeButton;
        public ResourceSellButton DepositButton => depositButton;
        public ResourceSellButton WithdrawButton => withdrawButton;
        public IReadOnlyList<ResourceSellButton> QuickButtons => quickButtons;
        public ResourceSellButton MinusButton => minusButton;
        public IReadOnlyList<StorageCargoRowView> PlayerRows => playerList.Rows;
        public IReadOnlyList<StorageCargoRowView> StorageRows => storageList.Rows;
        public ScrollRect PlayerScroll => playerList.Scroll;
        public ScrollRect StorageScroll => storageList.Scroll;
        public int Quantity => quantity;
        public string SelectedMineralId => selectedId;
        public int PopCount => popCount;
        public float BoxY => boxRoot != null ? boxRoot.anchoredPosition.y : 0f;
        public float BoxLid => box != null ? box.Lid : 0f;
        public string PlayerTotalsString => playerList.Totals != null ? playerList.Totals.text : string.Empty;
        public string StorageTotalsString => storageList.Totals != null ? storageList.Totals.text : string.Empty;
        public bool PlayerEmptyVisible => playerList.Empty != null && playerList.Empty.gameObject.activeSelf;
        public bool StorageEmptyVisible => storageList.Empty != null && storageList.Empty.gameObject.activeSelf;
        public string SelectionString => (selectionName != null ? selectionName.text : string.Empty)
            + (selectionCounts != null && selectionCounts.text.Length > 0 ? " | " + selectionCounts.text : string.Empty);
        public string PreviewString => previewText != null ? previewText.text : string.Empty;
        public string StatusString => statusText != null ? statusText.text : string.Empty;
        public string CargoString => cargoNumbers != null ? cargoNumbers.text : string.Empty;

        public bool AnyEffectVisible
        {
            get
            {
                if (box != null && box.enabled || mouthGlow != null && mouthGlow.enabled)
                {
                    return true;
                }

                for (var i = 0; pops != null && i < pops.Length; i++)
                {
                    if (pops[i].enabled)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>X 버튼. 실제 닫기 경로는 Binder.ClosePanel(DismissInteractionPanel)이다.</summary>
        public event Action CloseRequested;
        /// <summary>목록 행 선택. 검색·드롭다운 선택은 Picker 이벤트를 그대로 쓴다.</summary>
        public event Action<string> MineralSelected;
        public event Action<int> QuantityChanged;
        public event Action<int> DepositRequested;
        public event Action<int> WithdrawRequested;

        /// <summary>확인용: true면 Update가 시간을 진행하지 않고 테스트가 Tick으로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        /// <summary>canvasRoot 아래에 전체 화면 루트(자체 Canvas)를 만든다. 월드 클릭은 막지 않고 창 영역만 입력을 받는다.</summary>
        public static StoragePopupView Create(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var root = new GameObject(
                "StoragePopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            root.layer = canvasRoot.gameObject.layer;
            root.transform.SetParent(canvasRoot, false);
            ResourceSellUi.Stretch((RectTransform)root.transform);
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 32_100;
            root.GetComponent<Image>().raycastTarget = false;

            var view = root.AddComponent<StoragePopupView>();
            view.Build();
            return view;
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
            RenderLists();
            RenderSelection();
        }

        // ---------------- 상태 ----------------

        /// <summary>
        /// 연다. 숨김·닫는 중이면 등장 연출을 처음부터 재생한다.
        /// 이미 열려 있으면 아무것도 하지 않는다(Presenter가 스냅샷마다 불러도 안전).
        /// </summary>
        public bool Show()
        {
            if (state == PopupState.Opening || state == PopupState.Open)
            {
                return true;
            }

            gameObject.SetActive(true);
            state = PopupState.Opening;
            clock = 0f;
            ResetScroll(playerList);
            ResetScroll(storageList);
            RefreshPopIcons();
            transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(canvas);
            ApplyOpen(0f);
            return true;
        }

        /// <summary>역순 Showbox 종료 연출 뒤 숨긴다. 등장 도중이면 그 상태에서 바로 역순으로 이어진다.</summary>
        public bool BeginClose()
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            closeFrom = state == PopupState.Open ? StorageBoxTimeline.OpenDuration : clock;
            poppedAtClose = StorageBoxTimeline.PoppedCount(closeFrom, popCount);
            state = PopupState.Closing;
            clock = StorageBoxTimeline.CloseStartOffset(closeFrom, popCount);
            if (picker != null)
            {
                picker.ClearSearch();
            }

            if (!isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            ApplyClose(clock);
            return true;
        }

        public void HideImmediate()
        {
            if (state == PopupState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            FinishClose();
        }

        private void Update()
        {
            if (ManualTick || state == PopupState.Hidden)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            switch (state)
            {
                case PopupState.Opening:
                    clock += dt;
                    if (clock >= StorageBoxTimeline.OpenDuration)
                    {
                        state = PopupState.Open;
                        clock = 0f;
                        Settle();
                    }
                    else
                    {
                        ApplyOpen(clock);
                    }

                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= StorageBoxTimeline.CloseDuration)
                    {
                        FinishClose();
                        return;
                    }

                    ApplyClose(clock);
                    break;
            }
        }

        private void OnDisable()
        {
            // 씬 전환·부모 비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 되돌린다.
            state = PopupState.Hidden;
            HideEffects();
            PopupWindowSorting.Remove(canvas);
        }

        private void FinishClose()
        {
            state = PopupState.Hidden;
            clock = 0f;
            HideEffects();
            if (contentGroup != null)
            {
                contentGroup.interactable = false;
                contentGroup.blocksRaycasts = false;
            }

            if (cardBlocker != null)
            {
                cardBlocker.raycastTarget = false;
            }

            PopupWindowSorting.Remove(canvas);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        // ---------------- 표시 데이터 ----------------

        public void SetCargo(InventorySnapshot player, InventorySnapshot storageSnapshot)
        {
            playerCargo = player;
            storage = storageSnapshot;
            RenderLists();
            RenderCargoGauge();
            // 상자가 열리기 전이면 튀어나올 광물도 최신 화물 기준으로 맞춘다.
            if (state == PopupState.Hidden || state == PopupState.Opening && clock < StorageBoxTimeline.PopStart)
            {
                RefreshPopIcons();
            }
        }

        /// <summary>Presenter의 선택 상태. 빈 ID면 미선택이다.</summary>
        public void SetSelection(string mineralId, string displayName, int ownedQuantity, int storedQuantity, int requested)
        {
            selectedId = mineralId ?? string.Empty;
            owned = Mathf.Max(0, ownedQuantity);
            stored = Mathf.Max(0, storedQuantity);
            quantity = HasSelection
                ? StorageTransferPreview.ClampInput(requested, owned, stored)
                : Mathf.Max(0, requested);
            if (quantityInput != null && !(quantityInput.isFocused && quantityInput.text.Length == 0 && quantity == 0))
            {
                var text = quantity.ToString();
                if (quantityInput.text != text)
                {
                    quantityInput.SetTextWithoutNotify(text);
                }
            }

            if (selectionName != null)
            {
                selectionName.text = HasSelection ? displayName ?? selectedId : SelectPrompt;
                selectionName.color = HasSelection ? ResourceSellUi.TextMain : ResourceSellUi.TextMuted;
                selectionName.fontStyle = HasSelection ? FontStyles.Bold : FontStyles.Normal;
                selectionCounts.text = HasSelection ? "보유 " + owned + " · 보관 " + stored : string.Empty;
                var sprite = HasSelection ? ResolveIcon(selectedId) : null;
                selectionIcon.sprite = sprite != null ? sprite : StorageBoxArt.Gem();
                selectionIcon.color = sprite != null ? Color.white : ResourceSellUi.Teal;
                selectionIcon.enabled = HasSelection;
            }

            MarkSelectedRows();
            RenderSelection();
        }

        public void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId)
        {
            if (picker == null)
            {
                return;
            }

            picker.SetOptions(options, selectedMineralId);
            // 드롭다운에서도 지금 선택한 자원을 왼쪽 표시줄로 알려 준다.
            for (var i = 0; optionsContent != null && i < optionsContent.childCount; i++)
            {
                var child = optionsContent.GetChild(i);
                var mark = child.Find("SelectedMark");
                if (mark != null)
                {
                    mark.gameObject.SetActive(!string.IsNullOrEmpty(selectedMineralId)
                        && child.name == "Option_" + selectedMineralId);
                }
            }
        }

        public void ClearSearch()
        {
            if (picker != null)
            {
                picker.ClearSearch();
            }
        }

        public void SetStatus(string message, bool isError)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message ?? string.Empty;
            statusText.color = isError ? ResourceSellUi.Error : ResourceSellUi.Ok;
        }

        public void SetBusy(bool value)
        {
            busy = value;
            RenderSelection();
        }

        private bool HasSelection => !string.IsNullOrEmpty(selectedId);

        private Sprite ResolveIcon(string mineralId)
        {
            return iconResolver != null && !string.IsNullOrEmpty(mineralId) ? iconResolver(mineralId) : null;
        }

        private void RenderLists()
        {
            if (card == null)
            {
                return;
            }

            RenderList(playerList, playerCargo);
            RenderList(storageList, storage);
        }

        private void RenderList(CargoList list, InventorySnapshot snapshot)
        {
            var lines = StorageTransferPreview.Lines(snapshot);
            while (list.Rows.Count < lines.Count)
            {
                var row = StorageCargoRowView.Create(list.Content, font);
                row.Clicked += OnRowClicked;
                list.Rows.Add(row);
            }

            for (var i = 0; i < list.Rows.Count; i++)
            {
                var row = list.Rows[i];
                var visible = i < lines.Count;
                if (row.gameObject.activeSelf != visible)
                {
                    row.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                var line = lines[i];
                row.Bind(line, ResolveIcon(line.MineralId), line.MineralId == selectedId);
                ((RectTransform)row.transform).anchoredPosition = new Vector2(
                    StorageCargoRowView.Columns.RowWidth * 0.5f,
                    -(i * RowPitch + StorageCargoRowView.Columns.RowHeight * 0.5f));
            }

            var height = lines.Count * RowPitch - StorageCargoRowView.Columns.RowGap;
            list.Content.sizeDelta = new Vector2(list.Content.sizeDelta.x, Mathf.Max(0f, height));
            var overflow = height > ListH + 0.5f;
            list.Scroll.vertical = overflow;
            if (list.Scrollbar.gameObject.activeSelf != overflow)
            {
                list.Scrollbar.gameObject.SetActive(overflow);
            }

            if (!overflow)
            {
                list.Content.anchoredPosition = Vector2.zero;
            }

            list.Empty.gameObject.SetActive(lines.Count == 0);
            list.Totals.text = StorageTransferPreview.TotalsText(StorageTransferPreview.Totals(snapshot));
        }

        private void MarkSelectedRows()
        {
            for (var i = 0; i < playerList.Rows.Count; i++)
            {
                playerList.Rows[i].SetSelected(HasSelection && playerList.Rows[i].MineralId == selectedId);
            }

            for (var i = 0; i < storageList.Rows.Count; i++)
            {
                storageList.Rows[i].SetSelected(HasSelection && storageList.Rows[i].MineralId == selectedId);
            }
        }

        private void RenderCargoGauge()
        {
            if (cargoNumbers == null)
            {
                return;
            }

            var current = playerCargo != null ? playerCargo.CurrentWeight : 0f;
            var capacity = playerCargo != null ? playerCargo.MaxCapacity : 0f;
            var fraction = capacity > 0f && capacity < float.MaxValue ? Mathf.Clamp01(current / capacity) : 0f;
            cargoFill.anchorMin = Vector2.zero;
            cargoFill.anchorMax = new Vector2(fraction, 1f);
            cargoFill.offsetMin = Vector2.zero;
            cargoFill.offsetMax = Vector2.zero;
            cargoNumbers.text = ResourceSellUi.Cargo(current) + " / " + ResourceSellUi.Cargo(capacity) + "kg";
        }

        /// <summary>미리보기·버튼 상태. 실제 이동량은 Service가 같은 규칙(ClampToAvailable)으로 다시 판정한다.</summary>
        private void RenderSelection()
        {
            if (depositButton == null || previewText == null)
            {
                return;
            }

            var deposit = HasSelection ? StorageTransferPreview.DepositAmount(quantity, owned) : 0;
            var withdraw = HasSelection ? StorageTransferPreview.WithdrawAmount(quantity, stored) : 0;
            if (!HasSelection)
            {
                previewText.text = "<color=#9EC2D1>" + PreviewPrompt + "</color>";
            }
            else
            {
                previewText.text = "보관  <b>" + deposit + "개</b>" + AllNote(quantity, owned)
                    + "\n꺼내기  <b>" + withdraw + "개</b>" + AllNote(quantity, stored);
            }

            depositButton.Button.interactable = !busy && deposit > 0;
            withdrawButton.Button.interactable = !busy && withdraw > 0;
            for (var i = 0; i < quickButtons.Length; i++)
            {
                quickButtons[i].Button.interactable = !busy && HasSelection && (owned > 0 || stored > 0);
            }

            minusButton.Button.interactable = !busy && HasSelection && quantity > 0;
            quantityInput.interactable = !busy && HasSelection;
        }

        private static string AllNote(int requested, int available)
        {
            return requested > available && available > 0 ? "  <color=#9EC2D1>(전부)</color>" : string.Empty;
        }

        // ---------------- 입력 ----------------

        private bool CanAcceptInput()
        {
            return IsInteractive && (state == PopupState.Open || state == PopupState.Opening);
        }

        private void OnRowClicked(string mineralId)
        {
            if (!CanAcceptInput() || string.IsNullOrEmpty(mineralId))
            {
                return;
            }

            MineralSelected?.Invoke(mineralId);
        }

        // 빠른 수량 버튼은 값을 지정하지 않고 현재 수량에 더한다(범위는 ClampInput이 맞춘다).
        private void OnQuickQuantity(int value)
        {
            if (!CanAcceptInput() || !HasSelection)
            {
                return;
            }

            ApplyQuantity(StorageTransferPreview.ClampInput(quantity + value, owned, stored), true);
        }

        private void OnMinusQuantity()
        {
            if (!CanAcceptInput() || !HasSelection)
            {
                return;
            }

            ApplyQuantity(StorageTransferPreview.ClampInput(quantity - 1, owned, stored), true);
        }

        private void OnQuantityTyped(string text)
        {
            if (!CanAcceptInput())
            {
                return;
            }

            // 빈 칸은 지우는 중이므로 그대로 두고 0으로 본다.
            if (string.IsNullOrEmpty(text))
            {
                ApplyQuantity(0, false);
                return;
            }

            int.TryParse(text, out var parsed);
            var clamped = HasSelection ? StorageTransferPreview.ClampInput(parsed, owned, stored) : Mathf.Max(0, parsed);
            ApplyQuantity(clamped, clamped != parsed);
        }

        private void ApplyQuantity(int value, bool writeText)
        {
            quantity = Mathf.Max(0, value);
            if (writeText)
            {
                quantityInput.SetTextWithoutNotify(quantity.ToString());
            }

            RenderSelection();
            QuantityChanged?.Invoke(quantity);
        }

        /// <summary>보관 버튼. 즉시 요청하며 연출을 기다리지 않는다.</summary>
        public void RequestDeposit()
        {
            if (CanAcceptInput() && HasSelection && !busy)
            {
                DepositRequested?.Invoke(quantity);
            }
        }

        public void RequestWithdraw()
        {
            if (CanAcceptInput() && HasSelection && !busy)
            {
                WithdrawRequested?.Invoke(quantity);
            }
        }

        // ---------------- 연출 ----------------

        private void RefreshPopIcons()
        {
            popIds.Clear();
            popIds.AddRange(StorageTransferPreview.PopIds(playerCargo, storage, StorageBoxTimeline.MaxPops));
            popCount = StorageBoxTimeline.PopCount(popIds.Count);
            for (var i = 0; pops != null && i < pops.Length; i++)
            {
                var sprite = i < popCount ? ResolveIcon(popIds[i]) : null;
                pops[i].sprite = sprite != null ? sprite : StorageBoxArt.Gem();
                pops[i].color = sprite != null ? Color.white : ResourceSellUi.Teal;
            }
        }

        /// <summary>
        /// 광물 i가 안착할 목록 아이콘의 위치(팝업 좌표)와 그 크기에 해당하는 배율. 보유 목록을 먼저, 없으면 보관 목록에서 찾는다.
        /// 패널이 커지는 중이어도 아이콘의 현재 실제 위치를 따라가므로 도착 시점에 정확히 겹친다.
        /// 스크롤 밖의 행이면 목록 창 안쪽 가장자리로 모은다.
        /// </summary>
        private bool TryGetLanding(int index, out Vector2 position, out float scale)
        {
            position = Vector2.zero;
            scale = 1f;
            if (popRoot == null || index >= popIds.Count)
            {
                return false;
            }

            var list = FindListWithRow(popIds[index], out var row);
            if (list == null || row == null || row.IconRect == null)
            {
                return false;
            }

            var center = popRoot.InverseTransformPoint(row.IconRect.TransformPoint(row.IconRect.rect.center));
            position = new Vector2(center.x, center.y);
            if (list.Scroll != null && list.Scroll.viewport != null)
            {
                var viewport = list.Scroll.viewport;
                var half = StorageCargoRowView.Columns.RowHeight * 0.5f * card.localScale.y;
                var top = popRoot.InverseTransformPoint(viewport.TransformPoint(new Vector3(0f, viewport.rect.yMax, 0f))).y;
                var bottom = popRoot.InverseTransformPoint(viewport.TransformPoint(new Vector3(0f, viewport.rect.yMin, 0f))).y;
                position.y = Mathf.Clamp(position.y, Mathf.Min(bottom + half, top), Mathf.Max(top - half, bottom));
            }

            scale = StorageCargoRowView.Columns.IconSize * card.localScale.x / PopIconSize;
            return true;
        }

        private CargoList FindListWithRow(string mineralId, out StorageCargoRowView found)
        {
            found = FindRow(playerList, mineralId);
            if (found != null)
            {
                return playerList;
            }

            found = FindRow(storageList, mineralId);
            return found != null ? storageList : null;
        }

        private static StorageCargoRowView FindRow(CargoList list, string mineralId)
        {
            for (var i = 0; i < list.Rows.Count; i++)
            {
                var row = list.Rows[i];
                if (row.gameObject.activeSelf && row.MineralId == mineralId)
                {
                    return row;
                }
            }

            return null;
        }

        private void ApplyOpen(float t)
        {
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha * StorageBoxTimeline.Backdrop(t));

            var boxY = StorageBoxTimeline.BoxY(StorageBoxTimeline.BoxRise(t));
            boxRoot.anchoredPosition = new Vector2(0f, boxY);
            var boxScale = StorageBoxTimeline.OpenBoxScale(t);
            boxRoot.localScale = new Vector3(boxScale, boxScale, 1f);
            SetBox(StorageBoxTimeline.OpenBoxAlpha(t), StorageBoxTimeline.OpenLid(t), StorageBoxTimeline.MouthGlow(t));

            var mouth = new Vector2(0f, boxY) + StorageBoxGraphic.Mouth(BoxSize);
            for (var i = 0; i < pops.Length; i++)
            {
                var alpha = i < popCount ? StorageBoxTimeline.PopAlpha(t, i) : 0f;
                var progress = StorageBoxTimeline.PopProgress(t, i);
                var position = mouth + StorageBoxTimeline.PopPosition(progress, StorageBoxTimeline.PopApex(i, popCount));
                var landScale = 1f;
                if (i < popCount && TryGetLanding(i, out var landing, out var iconScale))
                {
                    // 정점에 닿은 광물이 목록의 자원 아이콘 위치·크기로 모인다.
                    position = Vector2.Lerp(position, landing, StorageBoxTimeline.FlyProgress(t, i));
                    landScale = iconScale;
                }

                SetPop(i, position, StorageBoxTimeline.PopScale(t, i, landScale), alpha);
            }

            var presence = StorageBoxTimeline.PanelPresence(t);
            ApplyCard(presence, mouth);
            SetGroup(headerGroup, StorageBoxTimeline.Reveal(t, StorageBoxTimeline.HeaderReveal));
            SetGroup(listGroup, StorageBoxTimeline.Reveal(t, StorageBoxTimeline.ListReveal));
            SetGroup(controlsGroup, StorageBoxTimeline.Reveal(t, StorageBoxTimeline.FooterReveal));
            contentGroup.alpha = 1f;
            contentGroup.interactable = StorageBoxTimeline.IsInteractive(t);
            contentGroup.blocksRaycasts = presence > 0f;
            cardBlocker.raycastTarget = presence > 0f;
        }

        private void ApplyClose(float t)
        {
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha * StorageBoxTimeline.CloseBackdrop(t));
            var boxY = StorageBoxTimeline.CloseBoxY(t, closeFrom);
            boxRoot.anchoredPosition = new Vector2(0f, boxY);
            boxRoot.localScale = Vector3.one;
            SetBox(StorageBoxTimeline.CloseBoxAlpha(t, closeFrom), StorageBoxTimeline.CloseLid(t, closeFrom), 0f);

            var mouth = new Vector2(0f, boxY) + StorageBoxGraphic.Mouth(BoxSize);
            for (var i = 0; i < pops.Length; i++)
            {
                var alpha = StorageBoxTimeline.ReturnAlpha(t, i, poppedAtClose);
                var fall = StorageBoxTimeline.ReturnProgress(t, i, poppedAtClose);
                var apex = StorageBoxTimeline.PopApex(i, popCount);
                SetPop(i, mouth + StorageBoxTimeline.PopPosition(1f - fall, apex), Mathf.Lerp(1f, StorageBoxTimeline.PopStartScale, fall), alpha);
            }

            ApplyCard(StorageBoxTimeline.ClosePanel(t, closeFrom), mouth);
            contentGroup.alpha = StorageBoxTimeline.CloseContentAlpha(t);
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            cardBlocker.raycastTarget = false;
        }

        /// <summary>패널은 상자 입구에서 커지며 가운데로 올라와 정착한다(종료는 반대).</summary>
        private void ApplyCard(float presence, Vector2 mouth)
        {
            var scale = StorageBoxTimeline.PanelScale(presence);
            card.localScale = new Vector3(scale, scale, 1f);
            card.anchoredPosition = Vector2.Lerp(mouth, Vector2.zero, presence);
            cardGroup.alpha = StorageBoxTimeline.PanelAlpha(presence);
            var visible = presence > 0.0001f;
            if (card.gameObject.activeSelf != visible)
            {
                card.gameObject.SetActive(visible);
            }
        }

        private void Settle()
        {
            card.gameObject.SetActive(true);
            card.localScale = Vector3.one;
            card.anchoredPosition = Vector2.zero;
            cardGroup.alpha = 1f;
            SetGroup(headerGroup, 1f);
            SetGroup(listGroup, 1f);
            SetGroup(controlsGroup, 1f);
            contentGroup.alpha = 1f;
            contentGroup.interactable = true;
            contentGroup.blocksRaycasts = true;
            cardBlocker.raycastTarget = true;
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, BackdropAlpha);
            HideEffects();
        }

        private void SetBox(float alpha, float lid, float glow)
        {
            box.color = ResourceSellUi.WithAlpha(Color.white, alpha);
            box.Lid = lid;
            box.enabled = alpha > 0.001f;
            mouthGlow.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.55f * glow * alpha);
            mouthGlow.enabled = glow * alpha > 0.001f;
        }

        private void SetPop(int index, Vector2 position, float scale, float alpha)
        {
            var image = pops[index];
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.localScale = new Vector3(scale, scale, 1f);
            var color = image.color;
            color.a = alpha;
            image.color = color;
            image.enabled = alpha > 0.001f;
        }

        private void HideEffects()
        {
            if (box != null)
            {
                box.enabled = false;
            }

            if (mouthGlow != null)
            {
                mouthGlow.enabled = false;
            }

            for (var i = 0; pops != null && i < pops.Length; i++)
            {
                pops[i].enabled = false;
            }
        }

        private static void SetGroup(CanvasGroup group, float reveal)
        {
            group.alpha = reveal;
            ((RectTransform)group.transform).anchoredPosition = new Vector2(0f, -10f * (1f - reveal));
        }

        private static void ResetScroll(CargoList list)
        {
            if (list.Scroll != null)
            {
                list.Scroll.verticalNormalizedPosition = 1f;
            }
        }

        // ---------------- 생성 ----------------

        private void Build()
        {
            var popupSkin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            var iconSkin = Resources.Load<ResourceSellSkin>(ResourceSellSkin.ResourcePath);
            font = popupSkin != null ? popupSkin.font : null;
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            cargoSprite = iconSkin != null ? iconSkin.cargoIcon : null;
            canvas = GetComponent<Canvas>();
            backdrop = GetComponent<Image>();
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, 0f);
            backdrop.canvasRenderer.cullTransparentMesh = false;

            BuildBox();
            BuildPops();
            card = ResourceSellUi.Centered(transform, "Card", Vector2.zero, CardSize);
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardBlocker = ResourceSellUi.Image(card, "InputBlocker", null, Color.clear);
            var shell = ResourceSellUi.Centered(card, "Shell", Vector2.zero, CardSize);
            ResourceSellPopupView.BuildPanelLayers(shell, popupSkin, CardSize);
            BuildContent(popupSkin);
            popRoot.SetAsLastSibling();

            card.gameObject.SetActive(false);
            HideEffects();
            gameObject.SetActive(false);
        }

        private void BuildBox()
        {
            boxRoot = ResourceSellUi.Centered(transform, "Showbox", new Vector2(0f, StorageBoxTimeline.BoxStartY), Vector2.zero);
            mouthGlow = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(boxRoot, "MouthGlow", StorageBoxGraphic.Mouth(BoxSize), new Vector2(380f, 300f)),
                ResourceSellArt.Soft(), Color.clear);
            var graphicRect = ResourceSellUi.Centered(boxRoot, "Box", Vector2.zero, new Vector2(BoxSize * 1.4f, BoxSize * 1.4f));
            box = graphicRect.gameObject.AddComponent<StorageBoxGraphic>();
            box.Size = BoxSize;
            box.raycastTarget = false;
        }

        private void BuildPops()
        {
            // 목록 아이콘으로 날아가는 광물이 패널 위에 보이도록 BuildContent 뒤에 맨 앞으로 올린다(Build 참고).
            // 위치·회전 값은 고정이라 같은 상태에서 늘 같은 모양이다.
            popRoot = ResourceSellUi.Centered(transform, "Pops", Vector2.zero, Vector2.zero);
            pops = new Image[StorageBoxTimeline.MaxPops];
            for (var i = 0; i < pops.Length; i++)
            {
                pops[i] = ResourceSellUi.AddImage(
                    ResourceSellUi.Centered(popRoot, "Pop" + i, Vector2.zero, new Vector2(PopIconSize, PopIconSize)),
                    StorageBoxArt.Gem(), ResourceSellUi.Teal);
                pops[i].preserveAspect = true;
                pops[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (6f + i * 3f));
            }
        }

        private void BuildContent(MineResetTimedPopupSkin skin)
        {
            var contentRoot = ResourceSellUi.Centered(card, "Content", Vector2.zero, CardSize);
            contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            headerGroup = Group(contentRoot, "Header");
            listGroup = Group(contentRoot, "Lists");
            controlsGroup = Group(contentRoot, "Controls");
            BuildHeader(headerGroup.transform, skin);
            BuildList(listGroup.transform, playerList, Pad, "보유 자원", "화물", PlayerEmptyLabel, "PlayerCargo");
            BuildList(listGroup.transform, storageList, RightX, "보관 자원", "보관함", StorageEmptyLabel, "StorageCargo");
            BuildControls(controlsGroup.transform);
        }

        private static CanvasGroup Group(Transform parent, string name)
        {
            var rect = ResourceSellUi.Centered(parent, name, Vector2.zero, CardSize);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        private void BuildHeader(Transform header, MineResetTimedPopupSkin skin)
        {
            ResourceSellUi.AddImage(ResourceSellUi.Place(header, "TitleAccent", Pad, TitleY + 14f, 4f, 36f), null,
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.9f));
            var title = ResourceSellUi.Label(header, "Title", Pad + 18f, TitleY, 420f, 64f, font, 40f, FontStyles.Bold,
                TitleColor, TextAlignmentOptions.Left);
            title.text = Title;
            title.characterSpacing = 3f;
            if (skin != null && skin.titleDivider != null)
            {
                var divider = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "TitleDivider", Pad, TitleY + 68f, 560f, 14f),
                    skin.titleDivider, ResourceSellUi.WithAlpha(Color.white, 0.75f));
                divider.preserveAspect = false;
            }

            var hint = ResourceSellUi.Label(header, "Hint", Pad + 18f, TitleY + 90f, 620f, 30f, font, 20f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            hint.text = HintLabel;

            closeButton = ResourceSellButton.Create(header, "CloseButton", CardSize.x - Pad - CloseSize, HeaderRowY, CloseSize,
                CloseSize, font, null, 20f, ResourceSellButton.Variant.Close, ResourceSellArt.Cross(), 22f);
            closeButton.Button.onClick.AddListener(() => CloseRequested?.Invoke());

            // 꺼내기는 화물 무게 한도를 넘을 수 없으므로 현재 적재량을 항상 보여 준다.
            const float x = 740f;
            const float y = 124f;
            var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(header, "CargoIcon", x, y + 2f, 50f, 50f), cargoSprite,
                Color.white);
            icon.preserveAspect = true;
            icon.enabled = cargoSprite != null;
            var label = ResourceSellUi.Label(header, "CargoLabel", x + 62f, y - 2f, 200f, 26f, font, 17f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            label.text = "화물 적재량";
            cargoNumbers = ResourceSellUi.Label(header, "CargoNumbers", x + 230f, y - 2f, CardSize.x - Pad - x - 230f, 26f,
                font, 17f, FontStyles.Normal, ResourceSellUi.TextMain, TextAlignmentOptions.Right);
            var bar = ResourceSellUi.Place(header, "CargoGauge", x + 62f, y + 30f, CardSize.x - Pad - x - 62f, 18f);
            ResourceSellUi.Image(bar, "Back", null, new Color(0.01f, 0.03f, 0.045f, 1f));
            cargoFill = ResourceSellUi.Image(bar, "Fill", null, ResourceSellUi.WithAlpha(ResourceSellUi.TealDeep, 0.95f)).rectTransform;
            var outline = ResourceSellUi.Image(bar, "Outline", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.35f));
            outline.type = Image.Type.Sliced;
            outline.pixelsPerUnitMultiplier = 2f;
        }

        private void BuildList(Transform parent, CargoList list, float x, string title, string subtitle, string empty, string name)
        {
            var section = ResourceSellUi.Place(parent, name, x, SectionY, ColumnW, ListY + ListH - SectionY);
            const float localStrip = StripY - SectionY;
            const float localList = ListY - SectionY;

            var heading = ResourceSellUi.Label(section, "Title", 4f, 0f, 160f, 40f, font, 24f, FontStyles.Bold,
                ResourceSellUi.TextMain, TextAlignmentOptions.Left);
            heading.text = title;
            var sub = ResourceSellUi.Label(section, "Subtitle", 4f + heading.GetPreferredValues(title).x + 12f, 4f, 120f, 36f,
                font, 17f, FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            sub.text = subtitle;
            list.Totals = ResourceSellUi.Label(section, "Totals", ColumnW - 340f, 4f, 336f, 36f, font, 18f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Right);

            var strip = ResourceSellUi.Place(section, "ColumnHeader", 0f, localStrip, StorageCargoRowView.Columns.RowWidth, 34f);
            ResourceSellUi.Image(strip, "Strip", ResourceSellArt.ChamferFill(), ResourceSellUi.Strip);
            ResourceSellUi.Image(strip, "StripLine", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.18f));
            ColumnLabel(strip, "자원", StorageCargoRowView.Columns.IconX + 4f, 200f, TextAlignmentOptions.Left);
            ColumnLabel(strip, "수량", StorageCargoRowView.Columns.QuantityX, StorageCargoRowView.Columns.QuantityW,
                TextAlignmentOptions.Right);
            ColumnLabel(strip, "무게", StorageCargoRowView.Columns.WeightX, StorageCargoRowView.Columns.WeightW,
                TextAlignmentOptions.Right);

            var view = ResourceSellUi.Place(section, "ListView", 0f, localList, ColumnW, ListH);
            list.Scroll = view.gameObject.AddComponent<ScrollRect>();
            var viewport = ResourceSellUi.Place(view, "Viewport", 0f, 0f, StorageCargoRowView.Columns.RowWidth + 6f, ListH);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            catcher.raycastTarget = true;
            list.Content = ResourceSellUi.Place(viewport, "Content", 0f, 0f, StorageCargoRowView.Columns.RowWidth, 0f);
            list.Content.pivot = new Vector2(0f, 1f);
            list.Content.anchoredPosition = Vector2.zero;

            var barRect = ResourceSellUi.Place(view, "Scrollbar", ColumnW - 9f, 0f, 8f, ListH);
            ResourceSellUi.Image(barRect, "Track", null, ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.08f));
            var area = ResourceSellUi.Centered(barRect, "SlidingArea", Vector2.zero, Vector2.zero);
            ResourceSellUi.Stretch(area);
            var handle = ResourceSellUi.Image(area, "Handle", null, ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.75f));
            handle.raycastTarget = true;
            list.Scrollbar = barRect.gameObject.AddComponent<Scrollbar>();
            list.Scrollbar.direction = Scrollbar.Direction.BottomToTop;
            list.Scrollbar.handleRect = handle.rectTransform;
            list.Scrollbar.targetGraphic = handle;
            var navigation = list.Scrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            list.Scrollbar.navigation = navigation;

            list.Scroll.viewport = viewport;
            list.Scroll.content = list.Content;
            list.Scroll.horizontal = false;
            list.Scroll.vertical = true;
            list.Scroll.movementType = ScrollRect.MovementType.Clamped;
            list.Scroll.scrollSensitivity = 30f;
            list.Scroll.inertia = false;
            list.Scroll.verticalScrollbar = list.Scrollbar;
            list.Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            list.Scrollbar.gameObject.SetActive(false);

            list.Empty = ResourceSellUi.Label(section, "Empty", 0f, localList, ColumnW, ListH, font, 21f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Center);
            list.Empty.text = empty;
            list.Empty.gameObject.SetActive(false);
        }

        private void ColumnLabel(Transform parent, string text, float x, float w, TextAlignmentOptions alignment)
        {
            var label = ResourceSellUi.Label(parent, "Col_" + text, x, 0f, w, 34f, font, 16f, FontStyles.Normal,
                ResourceSellUi.TextMuted, alignment);
            label.text = text;
        }

        private void BuildControls(Transform controls)
        {
            // 선택 카드(오른쪽) — 판매창 행처럼 아이콘·이름·보유/보관 수량.
            var selection = ResourceSellUi.Place(controls, "Selection", RightX, PickerY, ColumnW, ControlH);
            ResourceSellUi.Image(selection, "Back", ResourceSellArt.ChamferFill(), ResourceSellUi.Strip);
            ResourceSellUi.Image(selection, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.3f));
            selectionIcon = ResourceSellUi.AddImage(ResourceSellUi.Place(selection, "Icon", 14f, 8f, 36f, 36f), null, Color.white);
            selectionIcon.preserveAspect = true;
            selectionIcon.enabled = false;
            selectionName = ResourceSellUi.Label(selection, "Name", 62f, 0f, 260f, ControlH, font, 21f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            selectionName.text = SelectPrompt;
            selectionCounts = ResourceSellUi.Label(selection, "Counts", ColumnW - 300f, 0f, 282f, ControlH, font, 19f,
                FontStyles.Normal, ResourceSellUi.TextMain, TextAlignmentOptions.Right);

            statusText = ResourceSellUi.Label(controls, "Status", Pad, StatusY, CardSize.x - Pad * 2f, 28f, font, 18f,
                FontStyles.Normal, ResourceSellUi.Ok, TextAlignmentOptions.Right);

            BuildFooter(controls);
            // 드롭다운은 열릴 때 위쪽(목록 위)으로 펼쳐지므로 Controls의 마지막에 둔다.
            BuildPicker(controls);
        }

        private void BuildFooter(Transform controls)
        {
            var barWidth = CardSize.x - Pad * 2f;
            var bar = ResourceSellUi.Place(controls, "Footer", Pad, FooterY, barWidth, FooterH);
            ResourceSellUi.Image(bar, "Back", ResourceSellArt.ChamferFill(), new Color(0.012f, 0.028f, 0.042f, 0.97f));
            ResourceSellUi.Image(bar, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.38f));

            var x = Pad + 32f;
            Caption(controls, "QuantityCaption", "수량", x, FooterY + 16f);
            const float inputY = FooterY + 52f;
            const float inputH = 56f;
            const float minusW = 52f;
            minusButton = ResourceSellButton.Create(controls, "QuantityMinusButton", x, inputY, minusW, inputH, font, "-", 28f,
                ResourceSellButton.Variant.Normal);
            minusButton.Button.onClick.AddListener(OnMinusQuantity);
            var inputX = x + minusW + 8f;
            quantityInput = BuildInput(controls, "QuantityInput", inputX, inputY, 120f, inputH, "0", 28f, TextAlignmentOptions.Center, true);
            quantityInput.onValueChanged.AddListener(OnQuantityTyped);

            quickButtons = new ResourceSellButton[QuickQuantities.Length];
            var qx = inputX + 120f + 10f;
            for (var i = 0; i < QuickQuantities.Length; i++)
            {
                var value = QuickQuantities[i];
                // 1개짜리는 "+" 한 글자, 나머지는 "+5"처럼 더하는 양을 보여준다.
                quickButtons[i] = ResourceSellButton.Create(controls, "Quantity" + value + "Button", qx, inputY, 76f, inputH, font,
                    value == 1 ? "+" : "+" + value, value == 1 ? 28f : 21f, ResourceSellButton.Variant.Normal);
                quickButtons[i].Button.onClick.AddListener(() => OnQuickQuantity(value));
                qx += 76f + 8f;
            }

            var withdrawX = Pad + barWidth - 28f - ActionW;
            var depositX = withdrawX - 16f - ActionW;
            var previewX = qx + 20f;
            Caption(controls, "PreviewCaption", "옮길 수량", previewX, FooterY + 16f);
            previewText = ResourceSellUi.Label(controls, "Preview", previewX, inputY - 4f, depositX - 28f - previewX, 64f, font, 20f,
                FontStyles.Normal, ResourceSellUi.TextMain, TextAlignmentOptions.TopLeft);
            previewText.richText = true;
            previewText.lineSpacing = 6f;
            previewText.textWrappingMode = TextWrappingModes.Normal;

            var actionY = FooterY + (FooterH - ActionH) * 0.5f;
            depositButton = ResourceSellButton.Create(controls, "DepositButton", depositX, actionY, ActionW, ActionH, font,
                DepositLabel, 23f, ResourceSellButton.Variant.Primary, ResourceSellArt.FillDown(), 26f);
            withdrawButton = ResourceSellButton.Create(controls, "WithdrawButton", withdrawX, actionY, ActionW, ActionH, font,
                WithdrawLabel, 23f, ResourceSellButton.Variant.Primary, StorageBoxArt.LiftUp(), 26f);
            FitLabel(depositButton);
            FitLabel(withdrawButton);
            depositButton.Button.onClick.AddListener(RequestDeposit);
            withdrawButton.Button.onClick.AddListener(RequestWithdraw);
        }

        private static void FitLabel(ResourceSellButton button)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 23f;
            }
        }

        private void BuildPicker(Transform controls)
        {
            var rect = ResourceSellUi.Place(controls, "MineralPicker", Pad, PickerY, ColumnW, ControlH);
            var pickerObject = rect.gameObject;
            // Awake 전에 참조를 넣어야 기존 Picker 초기화(리스너 연결)가 그대로 돈다.
            pickerObject.SetActive(false);
            var search = BuildInput(rect, "SearchInput", 0f, 0f, 300f, ControlH, "자원 이름 검색", 19f, TextAlignmentOptions.Left, false);
            var caption = ResourceSellButton.Create(rect, "CaptionButton", 316f, 0f, ColumnW - 316f, ControlH, font, "자원 선택", 20f,
                ResourceSellButton.Variant.Normal);
            var captionLabel = caption.GetComponentInChildren<TMP_Text>(true);

            var panel = ResourceSellUi.Place(rect, "OptionsPanel", 0f, -(OptionsH + 8f), ColumnW, OptionsH);
            var panelBack = ResourceSellUi.AddImage(panel, ResourceSellArt.ChamferFill(), new Color(0.02f, 0.05f, 0.075f, 0.98f));
            panelBack.raycastTarget = true;
            ResourceSellUi.Image(panel, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.5f));
            var viewport = ResourceSellUi.Centered(panel, "Viewport", Vector2.zero, Vector2.zero);
            ResourceSellUi.Stretch(viewport);
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewport.gameObject.AddComponent<RectMask2D>();
            optionsContent = ResourceSellUi.Centered(viewport, "Content", Vector2.zero, Vector2.zero);
            optionsContent.anchorMin = new Vector2(0f, 1f);
            optionsContent.anchorMax = new Vector2(1f, 1f);
            optionsContent.pivot = new Vector2(0.5f, 1f);
            optionsContent.anchoredPosition = Vector2.zero;
            optionsContent.sizeDelta = Vector2.zero;
            var layout = optionsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 4f;
            var fitter = optionsContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var empty = ResourceSellUi.Text(ResourceSellUi.Centered(optionsContent, "EmptyLabel", Vector2.zero, new Vector2(0f, 44f)),
                font, 18f, FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Center);
            empty.text = "일치하는 자원이 없습니다.";
            var emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
            emptyLayout.minHeight = 44f;
            emptyLayout.preferredHeight = 44f;

            var template = ResourceSellButton.Create(optionsContent, "OptionTemplate", 0f, 0f, ColumnW - 16f, 44f, font, "자원", 19f,
                ResourceSellButton.Variant.Normal);
            var templateLayout = template.gameObject.AddComponent<LayoutElement>();
            templateLayout.minHeight = 44f;
            templateLayout.preferredHeight = 44f;
            var templateLabel = template.GetComponentInChildren<TMP_Text>(true);
            if (templateLabel != null)
            {
                templateLabel.alignment = TextAlignmentOptions.Left;
                templateLabel.margin = new Vector4(18f, 0f, 12f, 0f);
            }

            var mark = ResourceSellUi.AddImage(ResourceSellUi.Place(template.transform, "SelectedMark", 3f, 8f, 4f, 28f), null,
                ResourceSellUi.Teal);
            mark.gameObject.SetActive(false);

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = optionsContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = false;

            picker = pickerObject.AddComponent<OutpostMineralPickerView>();
            picker.Configure(search, caption.Button, captionLabel, panel.gameObject, optionsContent, template.Button, empty);
            pickerObject.SetActive(true);
        }

        private TMP_InputField BuildInput(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            string placeholderText,
            float size,
            TextAlignmentOptions alignment,
            bool numeric)
        {
            var rect = ResourceSellUi.Place(parent, name, x, y, w, h);
            var wasActive = rect.gameObject.activeSelf;
            rect.gameObject.SetActive(false);
            var face = ResourceSellUi.AddImage(rect, ResourceSellArt.ChamferFill(), new Color(0.02f, 0.06f, 0.09f, 0.98f));
            face.raycastTarget = true;
            ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.55f));
            var area = ResourceSellUi.Centered(rect, "Text Area", Vector2.zero, Vector2.zero);
            ResourceSellUi.Stretch(area);
            area.offsetMin = new Vector2(14f, 4f);
            area.offsetMax = new Vector2(-14f, -4f);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = ResourceSellUi.Text(ResourceSellUi.Centered(area, "Placeholder", Vector2.zero, Vector2.zero), font, size,
                FontStyles.Normal, ResourceSellUi.TextDim, alignment);
            ResourceSellUi.Stretch(placeholder.rectTransform);
            placeholder.text = placeholderText;
            var text = ResourceSellUi.Text(ResourceSellUi.Centered(area, "Text", Vector2.zero, Vector2.zero), font, size,
                numeric ? FontStyles.Bold : FontStyles.Normal, Color.white, alignment);
            ResourceSellUi.Stretch(text.rectTransform);

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = face;
            input.transition = Selectable.Transition.None;
            input.fontAsset = font;
            input.pointSize = size;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = numeric ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
            input.characterLimit = numeric ? 4 : 24;
            input.caretColor = ResourceSellUi.Teal;
            input.customCaretColor = true;
            input.selectionColor = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.35f);
            var navigation = input.navigation;
            navigation.mode = Navigation.Mode.None;
            input.navigation = navigation;
            rect.gameObject.SetActive(wasActive);
            return input;
        }

        private void Caption(Transform parent, string name, string text, float x, float y)
        {
            var label = ResourceSellUi.Label(parent, name, x, y, 220f, 26f, font, 17f, FontStyles.Normal,
                ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            label.text = text;
        }
    }
}
