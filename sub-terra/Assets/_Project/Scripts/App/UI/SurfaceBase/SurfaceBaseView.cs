using System;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// Surface Base 표시. 경제 수치는 Economy/Progression 패널이 담당한다.
    /// prompt-B 31-1/31-3: 설정(음량·해상도·진동 억제·언어)과 종료를 제공한다.
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
        [SerializeField] private TMP_Text masterVolumeLabel;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private TMP_Text reduceMotionLabel;
        [SerializeField] private TMP_Text resolutionLabel;
        [SerializeField] private Button resolutionPrevButton;
        [SerializeField] private Button resolutionNextButton;
        // 해상도 드롭다운(프리셋). 있으면 prev/next 버튼을 대체한다.
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Text languageLabel;
        [SerializeField] private Button languageCycleButton;
        // prompt-B 33-1: 언어 드롭다운(한국어 기본, 영어 선택).
        [SerializeField] private TMP_Dropdown languageDropdown;
        // prompt-B 33-2: 프레임 설정 드롭다운.
        [SerializeField] private TMP_Text frameRateLabel;
        [SerializeField] private TMP_Dropdown frameRateDropdown;
        [SerializeField] private TMP_Text bgmHintLabel;
        [SerializeField] private Button settingsApplyButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button settingsDefaultsButton;

        // 구 버전 Prefab 호환(비활성 유지). 더 이상 사용하지 않는다.
        [SerializeField] private Button refreshButton;

        private int draftResolutionWidth = 1920;
        private int draftResolutionHeight = 1080;
        private string draftLanguageCode = GameLanguageCodes.Korean;
        private FrameRateMode draftFrameRate = FrameRateMode.Auto;

        public event Action ExploreClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;
        public event Action SettingsApplyClicked;
        public event Action SettingsCancelClicked;
        public event Action SettingsDefaultsClicked;
        public event Action<float> MasterVolumePreviewChanged;

        private void OnEnable()
        {
            exploreButton?.onClick.AddListener(OnExplore);
            settingsButton?.onClick.AddListener(OnSettings);
            quitButton?.onClick.AddListener(OnQuit);
            settingsApplyButton?.onClick.AddListener(OnSettingsApply);
            settingsCancelButton?.onClick.AddListener(OnSettingsCancel);
            settingsDefaultsButton?.onClick.AddListener(OnSettingsDefaults);
            resolutionPrevButton?.onClick.AddListener(OnResolutionPrev);
            resolutionNextButton?.onClick.AddListener(OnResolutionNext);
            languageCycleButton?.onClick.AddListener(OnLanguageCycle);
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);
                // 드롭다운이 있으면 구 prev/next 사이클 버튼 숨김.
                if (resolutionPrevButton != null)
                {
                    resolutionPrevButton.gameObject.SetActive(false);
                }

                if (resolutionNextButton != null)
                {
                    resolutionNextButton.gameObject.SetActive(false);
                }
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.AddListener(OnFrameRateDropdownChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

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
            resolutionPrevButton?.onClick.RemoveListener(OnResolutionPrev);
            resolutionNextButton?.onClick.RemoveListener(OnResolutionNext);
            languageCycleButton?.onClick.RemoveListener(OnLanguageCycle);
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionDropdownChanged);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.RemoveListener(OnFrameRateDropdownChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }
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

            // prompt-B 33-4: 탐사 시작 busy 중에는 버튼 뒤/겹침 메시지가 보이지 않게 한다.
            if (busy && messageText != null)
            {
                messageText.text = string.Empty;
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

            draftResolutionWidth = values.ResolutionWidth > 0 ? values.ResolutionWidth : 1920;
            draftResolutionHeight = values.ResolutionHeight > 0 ? values.ResolutionHeight : 1080;
            draftLanguageCode = string.IsNullOrEmpty(values.LanguageCode)
                ? GameLanguageCodes.Korean
                : values.LanguageCode;
            draftFrameRate = values.FrameRate;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);
            }

            if (reduceMotionToggle != null)
            {
                reduceMotionToggle.SetIsOnWithoutNotify(values.ReduceMotion);
            }

            SyncResolutionDropdown();
            SyncLanguageDropdown();
            SyncFrameRateDropdown();
            RefreshSettingsLabels(values.MasterVolume);
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

            if (resolutionDropdown != null)
            {
                var preset = ResolutionPresets.Get(resolutionDropdown.value);
                draftResolutionWidth = preset.width;
                draftResolutionHeight = preset.height;
            }

            if (languageDropdown != null)
            {
                draftLanguageCode = languageDropdown.value == 1
                    ? GameLanguageCodes.English
                    : GameLanguageCodes.Korean;
            }

            if (frameRateDropdown != null)
            {
                draftFrameRate = FrameRatePresets.FromIndex(frameRateDropdown.value);
            }

            result.ResolutionWidth = draftResolutionWidth;
            result.ResolutionHeight = draftResolutionHeight;
            result.LanguageCode = draftLanguageCode;
            result.FrameRate = draftFrameRate;
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

        private void RefreshSettingsLabels(float volume)
        {
            if (masterVolumeLabel != null)
            {
                masterVolumeLabel.text = LocalizationService.FormatMasterVolume(volume);
            }

            if (resolutionLabel != null)
            {
                if (resolutionDropdown != null)
                {
                    resolutionLabel.text = LocalizationService.Get("settings.resolution", "해상도");
                }
                else
                {
                    resolutionLabel.text = LocalizationService.FormatResolution(
                        draftResolutionWidth,
                        draftResolutionHeight);
                }
            }

            if (reduceMotionLabel != null)
            {
                reduceMotionLabel.text = LocalizationService.Get(
                    "settings.reduce_motion",
                    "화면 진동 억제");
            }

            if (languageLabel != null)
            {
                if (languageDropdown != null)
                {
                    languageLabel.text = LocalizationService.Get("settings.language", "언어");
                }
                else
                {
                    var language = GameLanguageCodes.FromCode(draftLanguageCode);
                    languageLabel.text = LocalizationService.Get("settings.language", "언어")
                        + ": "
                        + LocalizationService.FormatLanguage(language);
                }
            }

            if (frameRateLabel != null)
            {
                frameRateLabel.text = LocalizationService.Get("settings.frame_rate", "프레임");
            }

            if (bgmHintLabel != null)
            {
                bgmHintLabel.text = LocalizationService.Get(
                    "settings.bgm_hint",
                    "BGM 4종(타이틀/기지/탐사/위험)은 마스터 음량으로 조절됩니다.");
            }
        }

        private void SyncResolutionDropdown()
        {
            if (resolutionDropdown == null)
            {
                return;
            }

            EnsureResolutionDropdownOptions();
            resolutionDropdown.SetValueWithoutNotify(
                ResolutionPresets.FindIndex(draftResolutionWidth, draftResolutionHeight));
        }

        private void SyncLanguageDropdown()
        {
            if (languageDropdown == null)
            {
                return;
            }

            EnsureLanguageDropdownOptions();
            var index = draftLanguageCode == GameLanguageCodes.English ? 1 : 0;
            languageDropdown.SetValueWithoutNotify(index);
        }

        private void SyncFrameRateDropdown()
        {
            if (frameRateDropdown == null)
            {
                return;
            }

            EnsureFrameRateDropdownOptions();
            frameRateDropdown.SetValueWithoutNotify(FrameRatePresets.ToIndex(draftFrameRate));
        }

        private void EnsureResolutionDropdownOptions()
        {
            if (resolutionDropdown == null)
            {
                return;
            }

            var expected = ResolutionPresets.All.Count;
            if (resolutionDropdown.options == null || resolutionDropdown.options.Count != expected)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(ResolutionPresets.BuildOptionLabels());
            }
        }

        private void EnsureLanguageDropdownOptions()
        {
            if (languageDropdown == null)
            {
                return;
            }

            if (languageDropdown.options == null || languageDropdown.options.Count < 2)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    LocalizationService.FormatLanguage(GameLanguage.Korean),
                    LocalizationService.FormatLanguage(GameLanguage.English)
                });
            }
        }

        private void EnsureFrameRateDropdownOptions()
        {
            if (frameRateDropdown == null)
            {
                return;
            }

            if (frameRateDropdown.options == null || frameRateDropdown.options.Count < 6)
            {
                frameRateDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>(6);
                for (var i = 0; i < 6; i++)
                {
                    options.Add(LocalizationService.FormatFrameRateOption(i));
                }

                frameRateDropdown.AddOptions(options);
            }
        }

        private void OnResolutionDropdownChanged(int index)
        {
            var preset = ResolutionPresets.Get(index);
            draftResolutionWidth = preset.width;
            draftResolutionHeight = preset.height;
            if (resolutionLabel != null && resolutionDropdown == null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }
        }

        private void OnLanguageDropdownChanged(int index)
        {
            draftLanguageCode = index == 1
                ? GameLanguageCodes.English
                : GameLanguageCodes.Korean;
            var previous = LocalizationService.Current;
            LocalizationService.SetLanguageCode(draftLanguageCode);
            float volume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
            RefreshSettingsLabels(volume);
            LocalizationService.SetLanguage(previous);
        }

        private void OnFrameRateDropdownChanged(int index)
        {
            draftFrameRate = FrameRatePresets.FromIndex(index);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (masterVolumeLabel != null)
            {
                masterVolumeLabel.text = LocalizationService.FormatMasterVolume(value);
            }

            MasterVolumePreviewChanged?.Invoke(value);
        }

        private void OnResolutionPrev()
        {
            var next = ResolutionPresets.Cycle(draftResolutionWidth, draftResolutionHeight, -1);
            draftResolutionWidth = next.width;
            draftResolutionHeight = next.height;
            if (resolutionLabel != null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }
        }

        private void OnResolutionNext()
        {
            var next = ResolutionPresets.Cycle(draftResolutionWidth, draftResolutionHeight, 1);
            draftResolutionWidth = next.width;
            draftResolutionHeight = next.height;
            if (resolutionLabel != null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }
        }

        private void OnLanguageCycle()
        {
            draftLanguageCode = draftLanguageCode == GameLanguageCodes.English
                ? GameLanguageCodes.Korean
                : GameLanguageCodes.English;
            var previous = LocalizationService.Current;
            LocalizationService.SetLanguageCode(draftLanguageCode);
            float volume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
            RefreshSettingsLabels(volume);
            LocalizationService.SetLanguage(previous);
        }

        private void OnExplore() => ExploreClicked?.Invoke();
        private void OnSettings() => SettingsClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
        private void OnSettingsApply() => SettingsApplyClicked?.Invoke();
        private void OnSettingsCancel() => SettingsCancelClicked?.Invoke();
        private void OnSettingsDefaults() => SettingsDefaultsClicked?.Invoke();
    }
}
