using System;
using System.Collections;
using System.Collections.Generic;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 터미널 창: 웜홀 인트로 → 창 튀어나옴 → 접속 줄, CCTV 미리보기, 항상 펼쳐진 목적지 목록, 비용 줄.
    /// 연출 값은 EscapePortalOpenTimeline이 계산하고 이 View는 값을 적용만 한다.
    /// 게임 시간이 멈춰도 돌도록 unscaled 시간을 쓴다.
    /// </summary>
    public sealed class EmergencyEscapePanelView : MonoBehaviour, IEmergencyEscapePanelView
    {
        public const string TerminalLine1 = "> 긴급 탈출 포탈 접속 중...";
        public const string TerminalLine2 = "> 연결되었습니다";
        public const string CctvOfflineLabel = "CCTV 대기";
        /// <summary>다른 팝업(32,100~)·광산 초기화 시계(32,000)보다 앞, 설정 창(32,760)보다는 뒤.</summary>
        public const int SortingOrder = 32_600;
        private static readonly Color RecRed = new Color(1f, 0.22f, 0.2f, 1f);

        public enum PlayState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private RectTransform wormholeRect;
        [SerializeField] private CanvasGroup wormholeGroup;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private ScrollRect listScroll;
        [SerializeField] private EmergencyEscapeDestinationRowView rowTemplate;
        [SerializeField] private TMP_Text terminalLine1;
        [SerializeField] private TMP_Text terminalLine2;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text destinationNameText;
        [SerializeField] private TMP_Text cctvStatusText;
        [SerializeField] private RawImage video;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [Header("전진기지 코어 팝업과 같은 질감")]
        [SerializeField] private RawImage hexPattern;
        [SerializeField] private RawImage scanlines;
        [SerializeField] private Image vignette;
        [SerializeField] private Image[] borderPanels;
        [SerializeField] private Image wormholeGlow;
        [SerializeField] private CanvasGroup recGroup;
        [SerializeField] private Image recDot;

        private readonly List<EmergencyEscapeDestinationRowView> rows = new List<EmergencyEscapeDestinationRowView>();
        private EscapeDestinationListModel model = new EscapeDestinationListModel();
        private CoreCctvCameraRig rig;
        private PlayState state;
        private float clock;
        private float closeStartAngle;
        private Action closedCallback;
        private bool busy;
        private bool cctvRendering;
        private bool snapNextCctv = true;
        private bool prewarming;
        private bool artApplied;

        public event Action<int> DestinationSelected;

        public PlayState State => state;
        public float Clock => clock;
        public int SelectedDestinationIndex => model.SelectedIndex;
        public EscapeDestinationListModel Model => model;
        public int RowCount => rows.Count;
        public bool IsOpening => state == PlayState.Opening;
        public bool IsOpen => state != PlayState.Hidden && state != PlayState.Closing
            && (panelRoot != null ? panelRoot : gameObject).activeInHierarchy;
        public GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;
        public CoreCctvCameraRig Rig => rig;
        public string TerminalText1 => terminalLine1 != null ? terminalLine1.text : string.Empty;
        public string TerminalText2 => terminalLine2 != null ? terminalLine2.text : string.Empty;
        public string CostLine => costText != null ? costText.text : string.Empty;
        public string DestinationName => destinationNameText != null ? destinationNameText.text : string.Empty;
        public bool ConfirmInteractable => confirmButton != null && confirmButton.interactable;
        public EmergencyEscapeDestinationRowView RowAt(int index) =>
            index >= 0 && index < rows.Count ? rows[index] : null;

        private void Awake()
        {
            ApplyArt();
        }

        private void OnDisable()
        {
            StopCctv();
        }

        /// <summary>코어 CCTV 팝업과 같은 스캔라인·육각 무늬·비네트·테두리를 입힌다(런타임 생성 그림).</summary>
        public void ApplyArt()
        {
            if (artApplied)
            {
                return;
            }

            artApplied = true;
            SetTexture(hexPattern, CoreCctvArt.Hex());
            SetTexture(scanlines, CoreCctvArt.Scanline());
            SetSprite(vignette, CoreCctvArt.Vignette());
            SetSprite(recDot, CoreCctvArt.Dot());
            SetSprite(wormholeGlow, CoreCctvArt.Soft());
            if (borderPanels != null)
            {
                for (var i = 0; i < borderPanels.Length; i++)
                {
                    SetSprite(borderPanels[i], CoreCctvArt.Border());
                }
            }

            if (hexPattern != null)
            {
                var size = windowRect != null ? windowRect.rect.size : new Vector2(1000f, 600f);
                hexPattern.uvRect = new Rect(0f, 0f, size.x / 90f, size.y / 51.96f);
            }

            if (scanlines != null)
            {
                scanlines.uvRect = new Rect(0f, 0f, 1f, scanlines.rectTransform.rect.height / 4f);
            }
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null && image.sprite == null)
            {
                image.sprite = sprite;
            }
        }

        private static void SetTexture(RawImage image, Texture texture)
        {
            if (image != null && image.texture == null)
            {
                image.texture = texture;
            }
        }

        /// <summary>
        /// 처음 열 때 생기는 끊김(CCTV 카메라·렌더 텍스처 생성, 글자 아틀라스, 레이아웃)을 미리 치른다.
        /// 창을 투명하게 켜 두고 CCTV를 몇 프레임 그린 뒤 숨긴다.
        /// </summary>
        public IEnumerator Prewarm()
        {
            if (!Application.isPlaying || state != PlayState.Hidden || prewarming)
            {
                yield break;
            }

            prewarming = true;
            var root = panelRoot != null ? panelRoot : gameObject;
            ApplyArt();
            root.SetActive(true);
            ApplySorting();
            if (windowGroup != null)
            {
                windowGroup.alpha = 0f;
                windowGroup.blocksRaycasts = false;
            }

            if (wormholeGroup != null)
            {
                wormholeGroup.alpha = 0f;
            }

            ApplyTerminal(1f, 1f);
            yield return null;
            if (state == PlayState.Hidden && video != null)
            {
                if (rig == null)
                {
                    rig = new CoreCctvCameraRig();
                }

                rig.EnsureCreated();
                ResizeRig();
                rig.SnapTo(Vector2.zero);
                rig.SetRendering(true);
                yield return null;
                yield return null;
                if (rig != null && state == PlayState.Hidden)
                {
                    rig.SetRendering(false);
                }
            }

            if (state == PlayState.Hidden)
            {
                root.SetActive(false);
            }

            prewarming = false;
        }

        private void ApplySorting()
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
        }

        private void OnDestroy()
        {
            if (rig != null)
            {
                rig.Dispose();
                rig = null;
            }
        }

        private void Update()
        {
            if (state == PlayState.Hidden)
            {
                return;
            }

            var delta = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            if (rig != null)
            {
                rig.Tick(delta);
            }

            switch (state)
            {
                case PlayState.Opening:
                    clock += delta;
                    ApplyOpen(clock);
                    if (clock >= EscapePortalOpenTimeline.OpenDuration)
                    {
                        FinishOpen();
                    }

                    break;
                case PlayState.Open:
                    // 열림 상태에서도 웜홀은 창 뒤에서 최대 속도로 계속 돈다.
                    clock += delta;
                    ApplyWormholeSpin(clock);
                    ApplyRecBlink();
                    break;
                case PlayState.Closing:
                    clock += delta;
                    ApplyClose(clock);
                    if (clock >= EscapePortalOpenTimeline.CloseDuration)
                    {
                        CompleteClose();
                    }

                    break;
            }
        }

        public void SetVisible(bool visible)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (!visible)
            {
                HideImmediate();
                return;
            }

            prewarming = false;
            ApplyArt();
            root.SetActive(true);
            // 활성화된 뒤에야 중첩 Canvas의 정렬 값이 유지된다.
            ApplySorting();
            closedCallback = null;
            clock = 0f;
            snapNextCctv = true;
            if (video != null)
            {
                // 새 화면이 잡히기 전에는 이전 목적지의 마지막 프레임이 비치지 않게 검게 둔다.
                video.texture = null;
                video.color = Color.black;
            }

            if (Application.isPlaying)
            {
                state = PlayState.Opening;
                ApplyOpen(0f);
            }
            else
            {
                // 에디트 모드(테스트)에서는 연출 없이 끝 상태로 연다.
                state = PlayState.Open;
                ApplyOpenEnd();
            }
        }

        /// <summary>연출 중 키 입력으로 호출한다. 열기 연출을 건너뛰고 바로 끝 상태로 간다.</summary>
        public void SkipOpen()
        {
            if (state != PlayState.Opening)
            {
                return;
            }

            ApplyOpenEnd();
            FinishOpen();
        }

        /// <summary>창이 웜홀로 빨려 들어가는 닫기 연출을 재생한다. 연출이 끝나면 onClosed를 부른다.</summary>
        public void BeginClose(Action onClosed)
        {
            if (state == PlayState.Hidden)
            {
                onClosed?.Invoke();
                return;
            }

            if (!Application.isPlaying || !(panelRoot != null ? panelRoot : gameObject).activeInHierarchy)
            {
                HideImmediate();
                onClosed?.Invoke();
                return;
            }

            if (state == PlayState.Closing)
            {
                return;
            }

            closedCallback = onClosed;
            closeStartAngle = EscapePortalOpenTimeline.SpinAngle(clock);
            state = PlayState.Closing;
            clock = 0f;
            StopCctv();
            SetWindowInteractable(false);
            ApplyClose(0f);
        }

        public void HideImmediate()
        {
            state = PlayState.Hidden;
            clock = 0f;
            closedCallback = null;
            StopCctv();
            (panelRoot != null ? panelRoot : gameObject).SetActive(false);
        }

        public void SetDestinations(EscapeDestinationListModel next)
        {
            model = next ?? new EscapeDestinationListModel();
            // 열 때마다 이전 줄을 모두 지우고 현재 목록으로 다시 만든다.
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    DestroyRow(rows[i].gameObject);
                }
            }

            rows.Clear();
            if (listContent == null || rowTemplate == null)
            {
                return;
            }

            rowTemplate.gameObject.SetActive(false);
            for (var i = 0; i < model.Count; i++)
            {
                var row = Instantiate(rowTemplate, listContent);
                row.name = "Row" + i;
                row.gameObject.SetActive(true);
                var data = model.Rows[i];
                row.Bind(i, data.Option.DisplayName, data.Enabled, data.Reason, HandleRowClicked);
                rows.Add(row);
            }

            RefreshSelection();
        }

        public void SetSelectedDestinationIndex(int index)
        {
            model.Select(index);
            RefreshSelection();
        }

        public void SetCost(int gold, int energy)
        {
            if (costText != null)
            {
                costText.text = "비용: " + gold + "G + 전력 " + energy;
            }
        }

        public void SetResult(string message, bool isError)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.text = message ?? string.Empty;
            resultText.color = isError
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(0.45f, 1f, 0.65f);
        }

        public void SetBusy(bool isBusy)
        {
            busy = isBusy;
            RefreshConfirm();
        }

        /// <summary>선택한 목적지를 CCTV로 비춘다. 카메라·플레이어는 건드리지 않는다.</summary>
        public void ShowDestination(Vector2 worldPosition, string displayName)
        {
            if (destinationNameText != null)
            {
                destinationNameText.text = displayName ?? string.Empty;
            }

            if (!Application.isPlaying || video == null)
            {
                SetCctvStatus(CctvOfflineLabel);
                return;
            }

            if (rig == null)
            {
                rig = new CoreCctvCameraRig();
            }

            rig.EnsureCreated();
            ResizeRig();
            if (rig.Texture == null)
            {
                SetCctvStatus(CctvOfflineLabel);
                return;
            }

            if (snapNextCctv || !rig.HasPosition)
            {
                snapNextCctv = false;
                rig.SnapTo(worldPosition);
            }
            else
            {
                rig.MoveTo(worldPosition);
            }

            SetCctvStatus(string.Empty);
            cctvRendering = true;
            // 창이 정면으로 자리 잡은 뒤부터 렌더링해 인트로 중 불필요한 렌더를 피한다.
            rig.SetRendering(state == PlayState.Open || (state == PlayState.Opening && clock >= EscapePortalOpenTimeline.PopEnd));
        }

        public bool HasRequiredReferences()
        {
            return panelRoot != null
                && windowRect != null
                && windowGroup != null
                && wormholeRect != null
                && listContent != null
                && rowTemplate != null
                && rowTemplate.HasRequiredReferences()
                && costText != null
                && confirmButton != null
                && closeButton != null
                && video != null;
        }

        private void FinishOpen()
        {
            state = PlayState.Open;
            ApplyOpenEnd();
            if (rig != null && cctvRendering)
            {
                rig.SetRendering(true);
            }
        }

        private void ApplyOpenEnd()
        {
            ApplyPose(EscapePortalOpenTimeline.OpenEndPose);
            ApplyTerminal(1f, 1f);
        }

        /// <summary>확인용: 시각 t의 열기 연출 모습을 그대로 적용한다(캡처·테스트용).</summary>
        public void ApplyOpenPreview(float t)
        {
            ApplyOpen(t);
        }

        private void ApplyOpen(float t)
        {
            var pose = EscapePortalOpenTimeline.EvaluateOpen(t);
            ApplyPose(pose);
            ApplyTerminal(pose.Line1, pose.Line2);
            if (rig != null && cctvRendering && pose.Interactable)
            {
                rig.SetRendering(true);
            }
        }

        private void ApplyClose(float t)
        {
            ApplyPose(EscapePortalOpenTimeline.EvaluateClose(t, closeStartAngle));
        }

        private void ApplyRecBlink()
        {
            if (recGroup != null)
            {
                recGroup.alpha = 0.55f + 0.45f * Mathf.Sin(clock * Mathf.PI * 1.6f);
            }

            if (recDot != null)
            {
                recDot.color = RecRed;
            }
        }

        private void ApplyWormholeSpin(float t)
        {
            if (wormholeRect != null)
            {
                wormholeRect.localRotation = Quaternion.Euler(0f, 0f, EscapePortalOpenTimeline.SpinAngle(t));
            }
        }

        private void ApplyPose(EscapePortalOpenTimeline.Pose pose)
        {
            if (wormholeRect != null)
            {
                wormholeRect.localScale = new Vector3(pose.WormholeScale, pose.WormholeScale, 1f);
                wormholeRect.localRotation = Quaternion.Euler(0f, 0f, pose.WormholeAngle);
            }

            if (wormholeGroup != null)
            {
                wormholeGroup.alpha = pose.WormholeAlpha;
            }

            if (windowRect != null)
            {
                windowRect.localScale = new Vector3(pose.WindowScale, pose.WindowScale, 1f);
                windowRect.localRotation = Quaternion.Euler(pose.WindowTilt, 0f, 0f);
            }

            if (windowGroup != null)
            {
                windowGroup.alpha = pose.WindowAlpha;
            }

            SetWindowInteractable(pose.Interactable);
        }

        private void ApplyTerminal(float line1, float line2)
        {
            ApplyTypedLine(terminalLine1, TerminalLine1, line1);
            ApplyTypedLine(terminalLine2, TerminalLine2, line2);
        }

        private static void ApplyTypedLine(TMP_Text target, string full, float progress)
        {
            if (target == null)
            {
                return;
            }

            var count = Mathf.Clamp(Mathf.CeilToInt(full.Length * Mathf.Clamp01(progress)), 0, full.Length);
            target.text = full.Substring(0, count);
        }

        private void SetWindowInteractable(bool interactable)
        {
            if (windowGroup != null)
            {
                windowGroup.interactable = interactable;
                windowGroup.blocksRaycasts = interactable;
            }
        }

        private void CompleteClose()
        {
            var callback = closedCallback;
            HideImmediate();
            callback?.Invoke();
        }

        private void RefreshSelection()
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].SetSelected(i == model.SelectedIndex);
                }
            }

            RefreshConfirm();
            EnsureSelectedVisible();
        }

        private void RefreshConfirm()
        {
            if (confirmButton != null)
            {
                // 이동할 수 없는 항목이거나 처리 중이면 이동 버튼을 막는다.
                confirmButton.interactable = !busy && model.CanTravel;
            }
        }

        // 키로 선택을 옮겼을 때 선택한 줄이 보이는 영역 안에 오도록 스크롤한다.
        private void EnsureSelectedVisible()
        {
            if (listScroll == null || listContent == null || model.SelectedIndex >= rows.Count
                || rows[model.SelectedIndex] == null)
            {
                return;
            }

            var viewport = listScroll.viewport != null ? listScroll.viewport : listScroll.transform as RectTransform;
            var rowRect = rows[model.SelectedIndex].transform as RectTransform;
            if (viewport == null || rowRect == null)
            {
                return;
            }

            var rowHeight = rowRect.rect.height > 0f ? rowRect.rect.height : 44f;
            var spacing = 4f;
            var rowTop = model.SelectedIndex * (rowHeight + spacing);
            var rowBottom = rowTop + rowHeight;
            var position = listContent.anchoredPosition;
            var viewHeight = viewport.rect.height;
            if (rowTop < position.y)
            {
                position.y = rowTop;
            }
            else if (rowBottom > position.y + viewHeight)
            {
                position.y = rowBottom - viewHeight;
            }

            var maxScroll = Mathf.Max(0f, model.Count * (rowHeight + spacing) - spacing - viewHeight);
            position.y = Mathf.Clamp(position.y, 0f, maxScroll);
            listContent.anchoredPosition = position;
        }

        private void HandleRowClicked(int index)
        {
            SetSelectedDestinationIndex(index);
            DestinationSelected?.Invoke(index);
        }

        private void ResizeRig()
        {
            var canvas = video.canvas;
            var scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            var size = video.rectTransform.rect.size * scale;
            var width = Mathf.Max(32, Mathf.RoundToInt(size.x));
            var height = Mathf.Max(32, Mathf.RoundToInt(size.y));
            rig.Resize(width, height);
            video.texture = rig.Texture;
            video.color = Color.white;
        }

        private void StopCctv()
        {
            cctvRendering = false;
            if (rig != null)
            {
                rig.SetRendering(false);
            }
        }

        private void SetCctvStatus(string text)
        {
            if (cctvStatusText != null)
            {
                cctvStatusText.text = text;
            }
        }

        private static void DestroyRow(GameObject row)
        {
            row.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(row);
            }
            else
            {
                DestroyImmediate(row);
            }
        }
    }
}
