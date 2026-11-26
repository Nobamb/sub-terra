using System;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Run;
using SubTerra.App.UI;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using SubTerra.Shared.Localization;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-213 산소 부족 드론 경고. 산소가 위험(Critical, 구출 표식과 같은 임계값) 아래로 처음 떨어질 때만
    /// 빨간 말풍선과 TAB 드론 터미널을 한 번 띄운다. 조작·시간은 막지 않는다.
    /// 구출 가능 여부(EmergencyRescueRuntimeController)와는 따로 OxygenRiskChanged를 구독한다.
    /// </summary>
    public sealed class LowOxygenDroneWarningController : MonoBehaviour
    {
        [Tooltip("경고로 연(또는 경고로 바꾼) 드론 터미널을 자동으로 닫기까지의 실제 초. TAB으로 먼저 닫아도 된다.")]
        [SerializeField, Min(0.5f)] private float autoCloseSeconds = 6f;

        private readonly LowOxygenWarningPolicy policy = new LowOxygenWarningPolicy();
        private PlayerSurvivalController survival;
        private bool restoring;
        private bool pending;
        private bool autoCloseArmed;
        private float autoCloseAt;
        private HudPanelChromeController chrome;

        /// <summary>발사 판정이 난 횟수(테스트·확인용).</summary>
        public int RaisedCount { get; private set; }
        /// <summary>정지 중이라 아직 화면에 띄우지 못한 경고가 있음.</summary>
        public bool IsPending => pending;
        public bool IsArmed => policy.IsArmed;
        public float AutoCloseSeconds => autoCloseSeconds;
        public event Action WarningRaised;

        public void Bind(PlayerSurvivalController survivalController)
        {
            Unbind();
            survival = survivalController;
            if (survival == null)
            {
                return;
            }

            survival.OxygenRiskChanged += OnOxygenRiskChanged;
            // 바인딩 시점(씬 로드·이어하기)에 이미 위험이면 떨어지는 순간이 아니다.
            policy.Reset(IsCritical());
        }

        public void Unbind()
        {
            if (survival != null)
            {
                survival.OxygenRiskChanged -= OnOxygenRiskChanged;
            }

            survival = null;
            restoring = false;
            pending = false;
            autoCloseArmed = false;
            policy.Clear();
        }

        /// <summary>저장 산소 복원 시작. 복원으로 바뀐 위험 단계는 하강으로 보지 않는다.</summary>
        public void BeginRestore()
        {
            restoring = true;
        }

        /// <summary>저장 산소 복원 끝. 복원된 값을 새 기준점으로 삼는다.</summary>
        public void EndRestore()
        {
            restoring = false;
            pending = false;
            policy.Reset(IsCritical());
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            if (pending)
            {
                TryShowPending();
            }

            TickAutoClose();
        }

        private void OnOxygenRiskChanged(OxygenRiskLevel risk)
        {
            if (restoring)
            {
                return;
            }

            var critical = risk == OxygenRiskLevel.Critical;
            if (!critical)
            {
                // 띄우기 전에 회복했으면 늦게 띄우지 않는다.
                pending = false;
            }

            if (!policy.Observe(critical))
            {
                return;
            }

            RaisedCount++;
            pending = true;
            WarningRaised?.Invoke();
            TryShowPending();
        }

        private bool IsCritical()
        {
            return survival != null && survival.OxygenRisk == OxygenRiskLevel.Critical;
        }

        private void TryShowPending()
        {
            // 디버그 콘솔·설정 등으로 정지 중이면 소리·자동 닫힘이 묻히므로 풀린 뒤에 띄운다.
            if (!Application.isPlaying || UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen)
            {
                return;
            }

            pending = false;
            try
            {
                ShowWarning();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SubTerra] 산소 부족 드론 경고 표시 실패: " + exception);
            }
        }

        private void ShowWarning()
        {
            var dialogue = new DroneDialogueResult(
                DataIds.Dialogue.DroneOxygenWarning,
                ResolveWarningText(),
                false,
                false,
                true);

            // 말풍선이 먼저 심각한 긴급음을 낸다. 터미널 열기의 기존 소리는 그대로 둔다.
            var socket = FindAnyObjectByType<DroneDialogueSocket>(FindObjectsInactive.Exclude);
            if (socket != null)
            {
                socket.ShowOxygenWarning(dialogue);
            }

            var panel = FindAnyObjectByType<DroneDialoguePanelView>(FindObjectsInactive.Include);
            if (panel != null)
            {
                panel.ShowOxygenWarning(dialogue, autoCloseSeconds);
            }

            if (chrome == null)
            {
                chrome = FindAnyObjectByType<HudPanelChromeController>(FindObjectsInactive.Exclude);
            }

            if (chrome == null)
            {
                return;
            }

            chrome.OpenDiggerBot();
            autoCloseArmed = chrome.IsDiggerBotOpen;
            autoCloseAt = Time.unscaledTime + autoCloseSeconds;
        }

        private void TickAutoClose()
        {
            if (!autoCloseArmed)
            {
                return;
            }

            if (chrome == null || !chrome.IsDiggerBotOpen)
            {
                // TAB·X로 먼저 닫았으면 다시 연 창에는 자동 닫힘을 걸지 않는다.
                autoCloseArmed = false;
                return;
            }

            if (Time.unscaledTime < autoCloseAt)
            {
                return;
            }

            autoCloseArmed = false;
            chrome.CloseDiggerBot();
        }

        private static string ResolveWarningText()
        {
            if (LocalizationService.Current == GameLanguage.English)
            {
                return LocalizationService.Get(DataIds.Dialogue.DroneOxygenWarning, DroneOxygenWarningFx.DefaultText);
            }

            var bootstrap = GameBootstrapper.Instance;
            var catalog = bootstrap != null ? bootstrap.AssignedCatalog as GameDataCatalog : null;
            if (catalog != null
                && catalog.TryGetDialogue(DataIds.Dialogue.DroneOxygenWarning, out var data)
                && data != null
                && !string.IsNullOrWhiteSpace(data.Template))
            {
                return data.Template;
            }

            // 카탈로그에 템플릿이 없어도 경고는 항상 나온다.
            return DroneOxygenWarningFx.DefaultText;
        }
    }
}
