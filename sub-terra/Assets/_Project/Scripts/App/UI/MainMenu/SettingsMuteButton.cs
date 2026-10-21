using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>기존 슬라이더 왼쪽의 도트 확성기. 별도 이미지나 폰트 글리프를 요구하지 않는다.</summary>
    public sealed class SettingsMuteButton : UnityEngine.UI.MaskableGraphic
    {
        private UnityEngine.UI.Slider slider;
        public UnityEngine.UI.Button Button => GetComponent<UnityEngine.UI.Button>();
        public bool ShowsMuted => SettingsRuntimeApplier.IsMasterMuted || slider != null && slider.value <= 0f;

        public static SettingsMuteButton Ensure(UnityEngine.UI.Slider slider)
        {
            var existing = slider.transform.Find("MasterMuteButton");
            var icon = existing != null ? existing.GetComponent<SettingsMuteButton>() : null;
            if (icon == null)
            {
                var root = new GameObject("MasterMuteButton", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(SettingsMuteButton), typeof(UnityEngine.UI.Button));
                root.layer = slider.gameObject.layer;
                root.transform.SetParent(slider.transform, false);
                icon = root.GetComponent<SettingsMuteButton>();
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-28f, 0f);
                rect.sizeDelta = new Vector2(32f, 32f);
                icon.color = new Color(0.45f, 0.94f, 1f);
                icon.raycastTarget = true;
                icon.Button.targetGraphic = icon;
                icon.Button.onClick.AddListener(icon.ToggleMute);
            }
            if (icon.slider != null) icon.slider.onValueChanged.RemoveListener(icon.OnSliderChanged);
            icon.slider = slider;
            slider.onValueChanged.AddListener(icon.OnSliderChanged);
            icon.SetVerticesDirty();
            return icon;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SettingsRuntimeApplier.MasterMuteChanged -= RefreshIcon;
            SettingsRuntimeApplier.MasterMuteChanged += RefreshIcon;
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderChanged);
                slider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        protected override void OnDisable()
        {
            SettingsRuntimeApplier.MasterMuteChanged -= RefreshIcon;
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
            base.OnDestroy();
        }

        private void ToggleMute()
        {
            if (slider != null)
                SettingsRuntimeApplier.SetMasterMuted(!SettingsRuntimeApplier.IsMasterMuted, slider.value);
        }

        private void OnSliderChanged(float value)
        {
            SettingsRuntimeApplier.PreviewMasterVolume(value);
            RefreshIcon();
        }

        private void RefreshIcon() => SetVerticesDirty();

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            PixelRect(vh, 2, 6, 4, 4, color);
            PixelRect(vh, 6, 4, 2, 8, color);
            PixelRect(vh, 8, 2, 2, 12, color);
            if (ShowsMuted)
            {
                var slash = new Color(1f, 0.7f, 0.25f, color.a);
                for (var i = 1; i < 14; i++) PixelRect(vh, i, i, 2, 2, slash);
            }
            else
            {
                PixelRect(vh, 11, 5, 1, 6, color);
                PixelRect(vh, 13, 3, 1, 2, color);
                PixelRect(vh, 14, 5, 1, 6, color);
                PixelRect(vh, 13, 11, 1, 2, color);
            }
        }

        private void PixelRect(UnityEngine.UI.VertexHelper vh, float x, float y, float w, float h, Color tint)
        {
            var rect = GetPixelAdjustedRect();
            var min = rect.min + Vector2.Scale(new Vector2(x / 16f, y / 16f), rect.size);
            var max = min + Vector2.Scale(new Vector2(w / 16f, h / 16f), rect.size);
            var index = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y, 0f), tint, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y, 0f), tint, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y, 0f), tint, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y, 0f), tint, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
