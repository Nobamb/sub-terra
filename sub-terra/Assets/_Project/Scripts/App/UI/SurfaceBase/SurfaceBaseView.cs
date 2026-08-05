using System;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// Surface Base 표시. 경제 수치는 Economy/Progression 패널이 담당한다.
    /// prompt-B 31-1: 새로고침 대신 설정·종료 버튼을 제공한다.
    /// </summary>
    public sealed class SurfaceBaseView : MonoBehaviour, ISurfaceBaseView
    {
        [SerializeField] private TMP_Text goalsText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text deepZoneText;
        [SerializeField] private TMP_Text recentRunText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button exploreButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private TMP_Text resolutionLabel;
        [SerializeField] private Button settingsApplyButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button settingsDefaultsButton;

        // 구 버전 Prefab 호환(비활성 유지). 더 이상 사용하지 않는다.
        [SerializeField] private Button refreshButton;

        public event Action ExploreClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;
        public event Action SettingsApplyClicked;
        public event Action SettingsCancelClicked;
        public event Action SettingsDefaultsClicked;

        private void OnEnable()
        {
            exploreButton?.onClick.AddListener(OnExplore);
            settingsButton?.onClick.AddListener(OnSettings);
            quitButton?.onClick.AddListener(OnQuit);
            settingsApplyButton?.onClick.AddListener(OnSettingsApply);
            settingsCancelButton?.onClick.AddListener(OnSettingsCancel);
            settingsDefaultsButton?.onClick.AddListener(OnSettingsDefaults);

            // 남아 있는 새로고침 버튼은 숨긴다.
            if (refreshButton != null)
            {
                refreshButton.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            exploreButton?.onClick.RemoveListener(OnExplore);
            settingsButton?.onClick.RemoveListener(OnSettings);
            quitButton?.onClick.RemoveListener(OnQuit);
            settingsApplyButton?.onClick.RemoveListener(OnSettingsApply);
            settingsCancelButton?.onClick.RemoveListener(OnSettingsCancel);
            settingsDefaultsButton?.onClick.RemoveListener(OnSettingsDefaults);
        }

        public void SetGoals(int completedObjectives, string summary)
        {
            if (goalsText != null)
            {
                goalsText.text = summary ?? ("목표 " + completedObjectives);
            }
        }

        public void SetDeepZoneLock(bool unlocked, string reason)
        {
            if (deepZoneText != null)
            {
                deepZoneText.text = unlocked
                    ? "심층 구역: 해금"
                    : "심층 잠금: " + (reason ?? string.Empty);
            }
        }

        public void SetEnergy(int current, int max, int explorationCost)
        {
            if (energyText == null)
            {
                return;
            }

            var safeCurrent = Mathf.Max(0, current);
            var safeMax = Mathf.Max(0, max);
            var safeCost = Mathf.Max(0, explorationCost);
            var afterDeparture = Mathf.Max(0, safeCurrent - safeCost);
            energyText.text = "전력 " + safeCurrent + " / " + safeMax
                + "  ·  지하행 " + safeCost + " 소모"
                + "  ·  도착 예상 " + afterDeparture;
        }

        public void SetRecentRun(int depth, bool isSafe, string structural, string gas)
        {
            if (recentRunText != null)
            {
                recentRunText.text =
                    "최근 탐사 깊이 " + depth
                    + " / " + (isSafe ? "안전" : "위험")
                    + " / 구조 " + structural
                    + " / 가스 " + gas;
            }
        }

        public void SetReturnResult(SurfaceRunResultReadModel result)
        {
            if (recentRunText == null || !result.HasCompletedReturn)
            {
                return;
            }

            recentRunText.text = "귀환 결과 · 최고 깊이 " + result.MaximumDepth
                + " / 화물 " + result.CargoWeight.ToString("0.##")
                + " / 판매 예상 " + result.UnsettledValue.ToString("0.##")
                + " / " + (result.IsSafe ? "안전" : "위험")
                + " / 구조 " + result.StructuralRisk
                + " / 가스 " + result.GasExposure;
        }

        public void SetExplorationBusy(bool busy)
        {
            if (exploreButton != null)
            {
                exploreButton.interactable = !busy;
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public void SetSettingsVisible(bool visible)
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(visible);
            }
        }

        public void SetSettingsDraft(SettingsValues values)
        {
            if (values == null)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);
            }

            if (reduceMotionToggle != null)
            {
                reduceMotionToggle.SetIsOnWithoutNotify(values.ReduceMotion);
            }

            if (resolutionLabel != null)
            {
                resolutionLabel.text =
                    values.ResolutionWidth + " x " + values.ResolutionHeight;
            }
        }

        public SettingsValues ReadSettingsDraft(SettingsValues fallback)
        {
            var result = fallback != null ? fallback.Clone() : SettingsValues.CreateDefaults();
            if (masterVolumeSlider != null)
            {
                result.MasterVolume = masterVolumeSlider.value;
            }

            if (reduceMotionToggle != null)
            {
                result.ReduceMotion = reduceMotionToggle.isOn;
            }

            return result;
        }

        public bool HasRequiredReferences()
        {
            return goalsText != null
                && energyText != null
                && deepZoneText != null
                && recentRunText != null
                && messageText != null
                && exploreButton != null
                && settingsButton != null
                && quitButton != null;
        }

        private void OnExplore() => ExploreClicked?.Invoke();
        private void OnSettings() => SettingsClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
        private void OnSettingsApply() => SettingsApplyClicked?.Invoke();
        private void OnSettingsCancel() => SettingsCancelClicked?.Invoke();
        private void OnSettingsDefaults() => SettingsDefaultsClicked?.Invoke();
    }
}
