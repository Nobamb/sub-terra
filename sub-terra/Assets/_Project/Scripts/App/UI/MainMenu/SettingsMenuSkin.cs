using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>설정 값은 기존 View가 관리하고, 이 컴포넌트는 표시와 닫기 버튼만 연결한다.</summary>
    public sealed class SettingsMenuSkin : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;
        [SerializeField] private UnityEngine.UI.Toggle reduceMotion;
        [SerializeField] private RectTransform switchHandle;
        [SerializeField] private UnityEngine.UI.Image switchTrack;
        [SerializeField] private TMP_Text volumeCaption;
        [SerializeField] private TMP_Text[] sectionTitles;
        private bool lastSwitchState;

        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Cancel);
            RefreshSwitch();
            RefreshScale();
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Cancel);
        }

        private void Cancel()
        {
            if (cancelButton != null) cancelButton.onClick.Invoke();
        }

        private void LateUpdate()
        {
            // 기존 View의 SetIsOnWithoutNotify로 복원한 초안도 스위치 외형에 반영한다.
            if (reduceMotion != null && reduceMotion.isOn != lastSwitchState) RefreshSwitch();
        }

        private void OnRectTransformDimensionsChange() => RefreshScale();

        private void RefreshScale()
        {
            if (card == null) return;
            var bounds = ((RectTransform)transform).rect;
            float scale = Mathf.Min(1f, bounds.width / 1120f, bounds.height / 1000f);
            card.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            card.anchoredPosition = new Vector2(0, -50f * scale);
        }

        private void RefreshSwitch()
        {
            if (reduceMotion == null || switchHandle == null || switchTrack == null) return;
            lastSwitchState = reduceMotion.isOn;
            switchHandle.anchoredPosition = new Vector2(lastSwitchState ? 18f : -18f, 0);
            switchTrack.color = lastSwitchState
                ? new Color(0.18f, 0.78f, 0.86f, 1f)
                : new Color(0.05f, 0.18f, 0.22f, 1f);
            var handleImage = switchHandle.GetComponent<UnityEngine.UI.Image>();
            if (handleImage != null)
            {
                handleImage.color = lastSwitchState
                    ? new Color(0.92f, 0.99f, 1f)
                    : new Color(0.55f, 0.82f, 0.88f);
            }
        }

        public static string FormatVolume(GameObject root, float volume)
        {
            var skin = root != null ? root.GetComponent<SettingsMenuSkin>() : null;
            if (skin == null) return LocalizationService.FormatMasterVolume(volume);
            if (skin.volumeCaption != null)
                skin.volumeCaption.text = LocalizationService.Get("settings.master_volume", "마스터 음량");
            bool english = LocalizationService.Current == GameLanguage.English;
            string[] titles = english
                ? new[] { "Audio", "Display", "System", "Controls" }
                : new[] { "오디오", "디스플레이", "시스템", "조작" };
            for (int i = 0; i < skin.sectionTitles.Length && i < titles.Length; i++)
                if (skin.sectionTitles[i] != null) skin.sectionTitles[i].text = titles[i];
            return Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f) + "%";
        }
    }
}
