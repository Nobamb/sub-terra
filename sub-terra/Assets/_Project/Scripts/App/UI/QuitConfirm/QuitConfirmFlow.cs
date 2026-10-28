using System;
using SubTerra.App.Audio;
using SubTerra.App.Save;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.QuitConfirm
{
    /// <summary>
    /// B-170 공용 종료 진입점. 메인 화면·지상 기지·광산의 종료 버튼과 O키가 모두 Request를 부른다.
    /// QuitPolicy 판단 → 종료 확인 창(UiPauseGate로 게임 멈춤) → "종료"면 먼저 저장 → 종료 연출 → 끄기.
    /// ESC·X는 다른 처리보다 먼저(-400) 여기서 받고, 창이 떠 있는 동안 IsBlockingInput으로 다른 단축키를 막는다.
    /// Enter·Space 등 키보드로는 종료하지 않는다(입력 처리는 HandleInput 한 곳).
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class QuitConfirmFlow : MonoBehaviour
    {
        public const string PauseOwner = "quit.confirm";
        private const float MaxStep = 0.1f;

        private static QuitConfirmFlow instance;
        private static int consumedFrame = -1;

        /// <summary>엘리베이터 탑승 중이면 true. App이 Gameplay를 모르므로 Integration이 등록한다.</summary>
        public static Func<bool> RideCheck;
        /// <summary>테스트용 가짜 저장·종료 대상. null이면 SaveRuntimeController를 쓴다.</summary>
        public static IQuitTarget TargetOverride;

        private QuitSequence sequence;
        private QuitConfirmPopupView popup;
        private QuitOutroOverlay overlay;
        private IQuitOutroView menuOutro;
        private bool inGame;
        private int confirmFrame = -1;

        public static QuitConfirmFlow Instance => instance;
        public QuitSequence Sequence => sequence;
        public QuitConfirmPopupView Popup => popup;
        public QuitOutroOverlay Overlay => overlay;
        /// <summary>확인용: true면 Update가 입력·시간을 진행하지 않는다.</summary>
        public bool ManualTick { get; set; }

        /// <summary>창이 떠 있거나 종료 연출 중이거나, 이번 프레임 ESC·X를 여기서 썼다.</summary>
        public static bool IsBlockingInput =>
            Time.frameCount <= consumedFrame
            || (instance != null && instance.Busy);

        private bool Busy => (sequence != null && sequence.Current != QuitSequence.Phase.Idle)
            || (popup != null && popup.IsVisible);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            consumedFrame = -1;
            RideCheck = null;
            TargetOverride = null;
        }

        /// <summary>
        /// 종료 요청. inGame이면 자동 저장 안내 줄을 보이고 지상·광산 덮개로 접는다.
        /// menuOutro가 있으면(메인 화면) 그 화면을 거꾸로 접는다. callerBlocked는 설정 창이 열린 경우 등.
        /// </summary>
        public static QuitRequestResult Request(bool inGame, IQuitOutroView menuOutro = null, bool callerBlocked = false)
        {
            try
            {
                var blocked = callerBlocked || UiPauseGate.IsHeld || (RideCheck != null && RideCheck());
                var flow = instance != null ? instance : Create();
                return flow.RequestInternal(inGame, menuOutro, blocked);
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuitConfirmFlow] Request failed: " + ex);
                return QuitRequestResult.Blocked;
            }
        }

        private static QuitConfirmFlow Create()
        {
            var host = new GameObject(
                "QuitConfirmFlow",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = QuitConfirmPopupView.SortOrder;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var flow = host.AddComponent<QuitConfirmFlow>();
            flow.Build(TMP_Settings.defaultFontAsset);
            return flow;
        }

        private void Build(TMP_FontAsset font)
        {
            instance = this;
            sequence = new QuitSequence(new TargetProxy(), () => WindowTvSfx.Play(SfxId.WindowTvClose));
            popup = QuitConfirmPopupView.Create(transform, font);
            popup.ConfirmClicked += OnConfirm;
            popup.CancelClicked += Close;
            overlay = QuitOutroOverlay.Create(transform);
        }

        private QuitRequestResult RequestInternal(bool requestInGame, IQuitOutroView outro, bool blocked)
        {
            var result = sequence.Request(blocked);
            if (result != QuitRequestResult.Shown) return result;
            inGame = requestInGame;
            menuOutro = outro;
            UiPauseGate.Acquire(PauseOwner);
            popup.Show(requestInGame, AccessibilityPreferences.ReduceMotion);
            return result;
        }

        private void OnDestroy()
        {
            if (popup != null)
            {
                popup.ConfirmClicked -= OnConfirm;
                popup.CancelClicked -= Close;
            }

            UiPauseGate.Release(PauseOwner);
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (ManualTick) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var escape = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame);
            var anyInput = (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));
            HandleInput(escape, anyInput, Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>입력 처리는 여기 한 곳. escape = ESC·X, anyInput = 아무 키·클릭(연출 중 바로 종료).</summary>
        public void HandleInput(bool escape, bool anyInput, float deltaSeconds)
        {
            var phase = sequence.Current;
            if (phase == QuitSequence.Phase.Confirming)
            {
                if (escape)
                {
                    consumedFrame = Time.frameCount;
                    Close();
                }

                return;
            }

            if (phase == QuitSequence.Phase.Outro || phase == QuitSequence.Phase.WaitingSave)
            {
                sequence.Tick(deltaSeconds, anyInput && Time.frameCount > confirmFrame);
            }
        }

        /// <summary>"취소"·ESC·X: 닫힘 글리치 뒤 게임을 이어 간다.</summary>
        public void Close()
        {
            if (sequence.Current != QuitSequence.Phase.Confirming || popup.IsClosing) return;
            popup.BeginClose(() =>
            {
                sequence.Cancel();
                UiPauseGate.Release(PauseOwner);
            });
        }

        /// <summary>"종료": 확인음(버튼) → 창 바로 숨김 → 먼저 저장 → 종료 연출.</summary>
        private void OnConfirm()
        {
            if (sequence.Current != QuitSequence.Phase.Confirming || !popup.IsStable) return;
            confirmFrame = Time.frameCount;
            popup.HideImmediate();
            var view = menuOutro != null ? menuOutro : overlay;
            sequence.Confirm(view, AccessibilityPreferences.ReduceMotion);
        }

        /// <summary>테스트용: 버튼 클릭과 같은 경로.</summary>
        public void ConfirmForTests() => OnConfirm();

        /// <summary>테스트가 만든 흐름을 지운다.</summary>
        public static void DestroyForTests()
        {
            if (instance != null)
            {
                var go = instance.gameObject;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            instance = null;
            consumedFrame = -1;
            UiPauseGate.Release(PauseOwner);
        }

        /// <summary>요청 시점의 SaveRuntimeController(또는 테스트 대상)에 넘긴다.</summary>
        private sealed class TargetProxy : IQuitTarget
        {
            public bool IsDirty
            {
                get
                {
                    if (TargetOverride != null) return TargetOverride.IsDirty;
                    var runtime = SaveRuntimeController.Instance;
                    return runtime != null && runtime.IsDirty;
                }
            }

            public bool IsSaveInProgress
            {
                get
                {
                    if (TargetOverride != null) return TargetOverride.IsSaveInProgress;
                    var runtime = SaveRuntimeController.Instance;
                    return runtime != null && runtime.IsSaveInProgress;
                }
            }

            public void SaveBeforeQuit()
            {
                if (TargetOverride != null)
                {
                    TargetOverride.SaveBeforeQuit();
                    return;
                }

                var runtime = SaveRuntimeController.Instance;
                if (runtime != null) runtime.SaveBeforeQuit();
            }

            public void Shutdown()
            {
                if (TargetOverride != null)
                {
                    TargetOverride.Shutdown();
                    return;
                }

                var runtime = SaveRuntimeController.Instance;
                if (runtime != null) runtime.ShutdownApplication();
                else Application.Quit();
            }
        }
    }
}
