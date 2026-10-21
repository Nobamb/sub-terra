using SubTerra.App.Progression;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class Issue223DisplaySettingsBuilder
    {
        public const string AssetPath = "Assets/_Project/Resources/DeepZoneDisplaySettings.asset";

        [MenuItem("SubTerra/UI/Create Deep Zone Display Settings")]
        public static void EnsureAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<DeepZoneDisplaySettings>(AssetPath) != null) return;
            var settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            settings.EditorSet(new[]
            {
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "Lv.1 해금 · Demo 목표 13개 완료"),
                new DeepZoneDisplayLevel(76, 100, false, "Lv.2 해금 · 골드/희귀 자원 조건 준비 중")
            });
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        public static void BuildAndValidate()
        {
            EnsureAsset();
            TestValidationRunner.Batch();
        }
    }
}
