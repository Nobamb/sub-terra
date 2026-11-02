using SubTerra.App.UI.MainMenu;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// B-170: 메인 화면 프리팹의 부팅 시간 값(bootSettings)을 코드 기본값(약 2초)과 맞춘다.
    /// 다른 값은 건드리지 않는다. 배치 모드: -executeMethod SubTerra.App.Editor.DataValidation.PromptB170BootTimingBuilder.Apply
    /// </summary>
    public static class PromptB170BootTimingBuilder
    {
        public const string PrefabPath = PromptB150MainMenuBuilder.PrefabPath;

        [MenuItem("SubTerra/MVP2/B-170 Apply Main Menu Boot Timing")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var director = root.GetComponentInChildren<MainMenuIntroDirector>(true);
                if (director == null)
                {
                    Debug.LogError("[B-170] MainMenuIntroDirector not found in " + PrefabPath);
                    return;
                }

                var so = new SerializedObject(director);
                var defaults = new MainMenuBootSettings();
                var boot = so.FindProperty("bootSettings");
                foreach (var field in typeof(MainMenuBootSettings).GetFields())
                {
                    var property = boot.FindPropertyRelative(field.Name);
                    if (property == null || property.propertyType != SerializedPropertyType.Float) continue;
                    property.floatValue = (float)field.GetValue(defaults);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[B-170] Boot timing applied to " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
