using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.App.Run;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>긴급 탈출 터미널 창 입력과 Runtime Bridge를 연결한다.</summary>
    public sealed class EmergencyEscapePanelBinder : MonoBehaviour
    {
        public const string PauseOwner = "emergency-escape-panel";

        [SerializeField] private EmergencyEscapePanelView view;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button closeCornerButton;

        private IEmergencyEscapePortalPort port;
        private IEmergencyEscapeDestinationPreview preview;
        private IReadOnlyList<EmergencyEscapeDestinationOption> options =
            System.Array.Empty<EmergencyEscapeDestinationOption>();
        private bool busy;
        private int openFrame = -1;
        private int previewedIndex = -1;
        private readonly SfxThrottle switchSfx = new SfxThrottle(SwitchSfxInterval);

        public const float SwitchSfxInterval = 0.08f;

        /// <summary>목적지 변경음 간격 제한. 테스트는 Clock을 바꿔 시간을 주입한다.</summary>
        public SfxThrottle SwitchSfx => switchSfx;

        public bool IsOpen
        {
            get
            {
                if (view == null)
                {
                    return false;
                }

                return view.IsOpen;
            }
        }
        public bool IsTopWindow(Canvas canvas) => view != null
            && PopupWindowSorting.Contains(canvas, view.PanelRoot);

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<EmergencyEscapePanelView>();
            }

            WireButtons();
            view?.HideImmediate();
        }

        private void Start()
        {
            // 첫 열기에서 끊기지 않도록 CCTV·글자·레이아웃 준비를 미리 해 둔다.
            if (view != null && Application.isPlaying)
            {
                StartCoroutine(view.Prewarm());
            }
        }

        private void OnEnable()
        {
            WireButtons();
            WireDestinationEvents();
        }

        private void OnDisable()
        {
            UnwireButtons();
            UnwireDestinationEvents();
            ReleasePause();
        }

        private void Update()
        {
            if (view == null || !view.IsOpen)
            {
                return;
            }

            // 창을 연 E 입력과 같은 프레임의 키는 건너뛰기로 읽지 않는다.
            if (Time.frameCount <= openFrame)
            {
                return;
            }

            if (view.IsOpening)
            {
                if (AnyKeyOrClickPressed())
                {
                    view.SkipOpen();
                }

                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                StepSelection(-1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                StepSelection(1);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                ConfirmSelectedDestination();
            }
        }

        // 게임 시간이 멈춘 동안 UndergroundMenuController가 Esc를 막으므로 창이 직접 받는다.
        // 같은 프레임에 설정 창이 함께 열리지 않도록 모든 Update가 끝난 LateUpdate에서 닫는다.
        private void LateUpdate()
        {
            if (view == null || !view.IsOpen)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && Time.frameCount > openFrame)
            {
                Close();
            }
        }

        public void BindTo(IEmergencyEscapePortalPort escapePort)
        {
            port = escapePort;
            preview = escapePort as IEmergencyEscapeDestinationPreview;
        }

        /// <summary>
        /// 현재 목적지 목록과 비용을 새로 채운 뒤 창을 연다. 엘리베이터를 기본 선택한다.
        /// failure가 None이 아니면 모든 항목을 흐리게 하고 이유를 보여 준다.
        /// </summary>
        public void Open(
            IReadOnlyList<EmergencyEscapeDestinationOption> destinationOptions,
            EmergencyEscapeCost cost,
            EmergencyEscapePaymentFailure failure = EmergencyEscapePaymentFailure.None)
        {
            options = destinationOptions
                ?? System.Array.Empty<EmergencyEscapeDestinationOption>();
            if (view == null)
            {
                return;
            }

            var model = EscapeDestinationListModel.Build(options, failure);
            model.Select(0);
            view.SetDestinations(model);
            view.SetCost(cost.Gold, cost.Energy);
            view.SetResult(string.Empty, false);
            view.SetBusy(false);
            busy = false;
            openFrame = Time.frameCount;
            UiKeyboardSubmitGuard.ClearSelection();
            AcquirePause();
            view.SetVisible(true);
            // 웜홀 회전·창 튀어나옴과 같이 간다. 기본 선택 미리보기는 무음.
            SfxPlayer.Play(SfxId.PortalActivate);
            // 기본 선택(엘리베이터)부터 CCTV로 보여 준다.
            PreviewDestinationAt(view.SelectedDestinationIndex);
        }

        public void Close()
        {
            // 이동 성공으로 닫힐 때(처리 중)는 워프음만 낸다.
            if (!busy && view != null && view.IsOpen)
            {
                SfxPlayer.Play(SfxId.PortalClose);
            }

            busy = false;
            view?.SetBusy(false);
            if (view == null)
            {
                ReleasePause();
                return;
            }

            // 닫기 연출이 끝날 때까지 게임 진행을 멈춘 채로 둔다. 먼저 풀면 연출 중 플레이어·카메라가 움직인다.
            // 에디트 모드에서는 즉시, 플레이 중에는 블랙홀이 창을 빨아들이는 연출 뒤에 풀린다.
            view.BeginClose(ReleasePause);
        }

        /// <summary>목적지 선택이 바뀔 때 해당 목적지를 CCTV로 비춘다.</summary>
        public void PreviewDestinationAt(int index)
        {
            if (view == null || options == null || options.Count == 0)
            {
                return;
            }

            if (index < 0 || index >= options.Count)
            {
                index = 0;
            }

            var selected = options[index];
            previewedIndex = index;
            view.SetSelectedDestinationIndex(index);
            if (preview == null)
            {
                return;
            }

            if (preview.TryGetDestinationPosition(selected.Kind, selected.InstanceId, out var position))
            {
                view.ShowDestination(position, selected.DisplayName);
            }
        }

        public void StepSelection(int delta)
        {
            if (view == null)
            {
                return;
            }

            if (view.Model.Step(delta))
            {
                PlaySwitchSfx();
                PreviewDestinationAt(view.Model.SelectedIndex);
            }
        }

        private void PlaySwitchSfx()
        {
            if (switchSfx.TryPass())
            {
                SfxPlayer.Play(SfxId.PortalSwitch);
            }
        }

        public void ConfirmSelectedDestination()
        {
            if (busy)
            {
                return;
            }

            if (port == null)
            {
                view?.SetResult("긴급 탈출 경로가 준비되지 않았습니다.", true);
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }

            if (options == null || options.Count == 0)
            {
                view?.SetResult("이동할 목적지가 없습니다.", true);
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }

            var index = view != null ? view.SelectedDestinationIndex : 0;
            if (index < 0 || index >= options.Count)
            {
                index = 0;
            }

            // 이동할 수 없는 항목(골드·전력 부족)은 이유만 보여 주고 비용 처리 경로를 타지 않는다.
            if (view != null && view.Model.TryGetSelected(out var row) && !row.Enabled)
            {
                view.SetResult(row.Reason, true);
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }

            var selected = options[index];
            busy = true;
            view?.SetBusy(true);
            try
            {
                var success = port.TryEscapeTo(
                    selected.Kind,
                    selected.InstanceId,
                    out var reason);
                view?.SetResult(reason, !success);
                if (success)
                {
                    // B-165: 도착음(EscapeArrive)은 전원이 꺼지는 소리라 실패처럼 들려 내지 않는다. 칸·파일은 남긴다.
                    SfxPlayer.Play(SfxId.PortalWarp);
                    Close();
                }
                else
                {
                    SfxPlayer.Play(SfxId.UiCancelFail);
                }
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        private static bool AnyKeyOrClickPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        // 선택하는 동안 방향키가 캐릭터를 움직이지 않도록 게임 진행을 잠시 멈춘다(연출은 unscaled 시간).
        private static void AcquirePause()
        {
            if (Application.isPlaying)
            {
                UiPauseGate.Acquire(PauseOwner);
            }
        }

        private static void ReleasePause()
        {
            UiPauseGate.Release(PauseOwner);
        }

        private void WireButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelectedDestination);
                confirmButton.onClick.AddListener(ConfirmSelectedDestination);
                // 흐린 목적지라 막힌 이동 버튼을 눌러도 Enter와 같은 실패음이 난다.
                UiSfx.Attach(confirmButton, SfxId.None);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (closeCornerButton != null)
            {
                closeCornerButton.onClick.RemoveListener(Close);
                closeCornerButton.onClick.AddListener(Close);
            }
        }

        private void UnwireButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelectedDestination);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (closeCornerButton != null)
            {
                closeCornerButton.onClick.RemoveListener(Close);
            }
        }

        private void WireDestinationEvents()
        {
            if (view == null)
            {
                return;
            }

            view.DestinationSelected -= HandleDestinationSelected;
            view.DestinationSelected += HandleDestinationSelected;
        }

        private void UnwireDestinationEvents()
        {
            if (view == null)
            {
                return;
            }

            view.DestinationSelected -= HandleDestinationSelected;
        }

        // 행 클릭은 이벤트 전에 선택을 바꾸므로 마지막으로 비춘 목적지와 비교한다.
        private void HandleDestinationSelected(int index)
        {
            if (index != previewedIndex)
            {
                PlaySwitchSfx();
            }

            PreviewDestinationAt(index);
        }
    }
}
