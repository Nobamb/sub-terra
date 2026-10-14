using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 기본 HUD View. 전달받은 값으로 텍스트/게이지만 표시하며 State를 읽거나 쓰지 않는다.
    /// </summary>
    public sealed class BasicHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI cargoText;
        [SerializeField] private TextMeshProUGUI unsettledValueText;
        [SerializeField] private TextMeshProUGUI buildingSelectionText;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private HudGaugeView energyGauge;
        [SerializeField] private HudGaugeView healthGauge;

        public TextMeshProUGUI EnergyText => energyText;
        public TextMeshProUGUI HealthText => healthText;
        public TextMeshProUGUI DepthText => depthText;
        public TextMeshProUGUI GoldText => goldText;
        public TextMeshProUGUI CargoText => cargoText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI BuildingSelectionText => buildingSelectionText;
        public TextMeshProUGUI InteractionPromptText => interactionPromptText;

        private void Awake()
        {
            AlignHealthRow();
            HideBuildingSelection();
            ApplyCompactLayout();
        }

        public void ApplyCompactLayout()
        {
            var root = transform as RectTransform;
            var frame = transform.Find("HudFrame") as RectTransform;
            if (root == null || frame == null) return;
            root.localScale = Vector3.one;
            PlaceCompact(root, 12f, 12f, 380f, 208f);
            PlaceCompact(frame, 0f, 0f, 380f, 208f);
            foreach (var gaugeName in new[] { "HealthGauge", "EnergyGauge" })
            {
                var gauge = transform.Find(gaugeName) as RectTransform;
                if (gauge == null) continue;
                PlaceCompact(gauge, 106f, gaugeName == "HealthGauge" ? 16f : 52f, 258f, 32f);
                foreach (var child in new[] { "Empty", "Fill", "Frame" })
                    PlaceCompact(gauge.Find(child) as RectTransform, 0f, 0f, 258f, 32f);
            }
            TextCompact(healthText, 110f, 16f, 246f, 30f, 22f);
            TextCompact(energyText, 110f, 52f, 246f, 30f, 22f);
            LabelCompact("HealthLabel", 52f, 17f, 50f, 28f, 20f);
            LabelCompact("EnergyLabel", 52f, 53f, 50f, 28f, 20f);
            TextCompact(depthText, 52f, 96f, 132f, 26f, 19f);
            TextCompact(goldText, 52f, 126f, 132f, 26f, 19f);
            TextCompact(cargoText, 232f, 96f, 134f, 26f, 19f);
            TextCompact(unsettledValueText, 232f, 126f, 134f, 26f, 18f);
            float[] xs = { 20f, 20f, 20f, 20f, 202f, 202f };
            float[] ys = { 18f, 54f, 97f, 127f, 97f, 127f };
            for (int i = 0; i < 6; i++) PlaceCompact(transform.Find("HudIcon" + i) as RectTransform, xs[i], ys[i], 24f, 24f);
            PlaceCompact(transform.Find("TopSeparator") as RectTransform, 16f, 89f, 348f, 1f);
            PlaceCompact(transform.Find("BottomSeparator") as RectTransform, 16f, 158f, 348f, 1f);
            PlaceCompact(transform.Find("ColumnSeparator") as RectTransform, 190f, 96f, 1f, 56f);
            var status = transform.Find("StructuralStatusRow") as RectTransform;
            if (status != null)
            {
                PlaceCompact(status, 16f, 164f, 348f, 28f);
                PlaceCompact(status.Find("Icon") as RectTransform, 4f, 2f, 24f, 24f);
                var label = status.Find("Label");
                if (label != null) TextCompact(label.GetComponent<TMP_Text>(), 36f, 0f, 306f, 28f, 20f);
            }
        }

        private void LabelCompact(string name, float x, float y, float w, float h, float size)
        {
            var label = transform.Find(name);
            if (label != null) TextCompact(label.GetComponent<TMP_Text>(), x, y, w, h, size);
        }

        private static void TextCompact(TMP_Text text, float x, float y, float w, float h, float size)
        {
            if (text == null) return;
            PlaceCompact(text.rectTransform, x, y, w, h);
            text.fontSize = size;
        }

        private static void PlaceCompact(RectTransform rect, float x, float y, float w, float h)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        public void AlignHealthRow()
        {
            if (healthGauge != null) return;
            if (healthText == null || energyText == null)
            {
                return;
            }

            RectTransform health = healthText.rectTransform;
            RectTransform energy = energyText.rectTransform;
            health.anchorMin = energy.anchorMin;
            health.anchorMax = energy.anchorMax;
            health.pivot = energy.pivot;
            health.sizeDelta = energy.sizeDelta;
            health.anchoredPosition = new Vector2(
                energy.anchoredPosition.x,
                energy.anchoredPosition.y + energy.rect.height);
        }

        public void SetEnergy(string text)
        {
            SetText(energyText, energyGauge != null ? text.Replace("전력 ", "") : text);
        }

        public void SetEnergyLevel(float current, int maximum)
        {
            if (energyGauge != null) energyGauge.SetValue(current, maximum);
        }

        public void SetHealthLevel(float current, int maximum)
        {
            if (healthGauge != null) healthGauge.SetValue(current, maximum);
        }

        public void SetHealth(string text)
        {
            SetText(healthText, healthGauge != null ? text.Replace("체력 ", "") : text);
        }

        public void SetDepth(string text)
        {
            SetText(depthText, text);
        }

        public void SetGold(string text)
        {
            SetText(goldText, text + "G");
        }

        public void SetCargo(string text)
        {
            SetText(cargoText, text);
        }

        public void SetUnsettledValue(string text)
        {
            SetText(unsettledValueText, text + "G");
        }

        public void SetBuildingSelection(string text)
        {
            // The construction panel owns this guidance; the HUD row overlaps the mission panel.
            HideBuildingSelection();
        }

        private void HideBuildingSelection()
        {
            if (buildingSelectionText == null) return;
            buildingSelectionText.text = string.Empty;
            buildingSelectionText.enabled = false;
            buildingSelectionText.raycastTarget = false;
        }

        public void SetInteractionPrompt(string text)
        {
            SetText(interactionPromptText, text);
        }

        public bool HasRequiredReferences()
        {
            return energyText != null
                && healthText != null
                && depthText != null
                && goldText != null
                && cargoText != null
                && unsettledValueText != null
                && buildingSelectionText != null
                && interactionPromptText != null;
        }

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}
