using System;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// BasicHUD 프리팹의 전력 게이지 아래에 산소 줄(라벨·게이지·수치)을 만든다.
    /// 전력 줄을 복제해 채움 스프라이트(회색 oxygen-bar)와 색만 바꾼다. 좌표는 BasicHudView.ApplyCompactLayout이 소유한다.
    /// </summary>
    public static class PromptB198HudOxygenGaugeBuilder
    {
        public const string Prefab = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        public const string FillSprite = "Assets/_Project/Art/UI/Gameplay/HUD/oxygen-bar.png";
        /// <summary>게이지 왼쪽 「O2」 글자 아이콘(탁한 흰색 + 어두운 외곽선). 색은 OxygenIcon의 Image Color로 조절한다.</summary>
        public const string IconSprite = "Assets/_Project/Art/UI/Gameplay/HUD/oxygen-icon.png";

        /// <summary>탁한 회색빛 반투명 흰색(알파 90%). 최종 값은 프리팹 OxygenGauge/Fill 검사기에서 조절한다.</summary>
        public static readonly Color OxygenFillColor = new Color(0.86f, 0.88f, 0.90f, 0.9f);

        [MenuItem("SubTerra/UI/Build Prompt-B 198 HUD Oxygen Gauge")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.ImportAsset(FillSprite);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FillSprite);
            if (sprite == null) throw new InvalidOperationException("Missing sprite: " + FillSprite);

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var view = root.GetComponent<BasicHudView>();
                var rootRect = (RectTransform)root.transform;
                var energyGauge = rootRect.Find("EnergyGauge");
                if (view == null || energyGauge == null || view.EnergyText == null)
                    throw new InvalidOperationException("BasicHUD prefab is missing EnergyGauge/EnergyText.");

                if (rootRect.Find("OxygenGauge") == null)
                {
                    var gauge = UnityEngine.Object.Instantiate(energyGauge.gameObject, rootRect, false);
                    gauge.name = "OxygenGauge";
                    gauge.transform.SetSiblingIndex(energyGauge.GetSiblingIndex() + 1);
                    var fill = gauge.transform.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                    fill.sprite = sprite;
                    fill.color = OxygenFillColor;
                    fill.fillAmount = 1f;
                    var gaugeView = gauge.GetComponent<HudGaugeView>();
                    Set(gaugeView, "fill", fill);
                    Set(view, "oxygenGauge", gaugeView);
                }

                if (rootRect.Find("OxygenIcon") == null)
                {
                    var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconSprite);
                    if (iconSprite == null) throw new InvalidOperationException("Missing sprite: " + IconSprite);
                    var icon = new GameObject("OxygenIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    icon.transform.SetParent(rootRect, false);
                    var iconImage = icon.GetComponent<UnityEngine.UI.Image>();
                    iconImage.sprite = iconSprite;
                    iconImage.color = OxygenFillColor;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    var energyIcon = rootRect.Find("HudIcon1");
                    icon.transform.SetSiblingIndex(energyIcon != null ? energyIcon.GetSiblingIndex() + 1 : rootRect.childCount - 1);
                }

                if (rootRect.Find("OxygenText") == null)
                {
                    var text = UnityEngine.Object.Instantiate(view.EnergyText.gameObject, rootRect, false);
                    text.name = "OxygenText";
                    text.transform.SetSiblingIndex(view.EnergyText.transform.GetSiblingIndex() + 1);
                    text.GetComponent<TextMeshProUGUI>().text = "100 / 100";
                    Set(view, "oxygenText", text.GetComponent<TextMeshProUGUI>());
                }

                if (rootRect.Find("OxygenLabel") == null)
                {
                    var energyLabel = rootRect.Find("EnergyLabel");
                    if (energyLabel == null) throw new InvalidOperationException("BasicHUD prefab is missing EnergyLabel.");
                    var label = UnityEngine.Object.Instantiate(energyLabel.gameObject, rootRect, false);
                    label.name = "OxygenLabel";
                    label.transform.SetSiblingIndex(energyLabel.GetSiblingIndex() + 1);
                    label.GetComponent<TextMeshProUGUI>().text = "산소";
                }

                view.ApplyCompactLayout();
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
