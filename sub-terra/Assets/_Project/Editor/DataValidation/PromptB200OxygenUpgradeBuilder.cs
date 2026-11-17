using SubTerra.App.Core.Data;
using UnityEditor;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 200: 산소 업그레이드 2종 에셋을 만들고 카탈로그에만 등록한다.
    /// 다른 업그레이드·프리팹·씬은 건드리지 않는다. 트리 배치는 PromptB118UpgradeTreeBuilder가 맡는다.
    /// </summary>
    public static class PromptB200OxygenUpgradeBuilder
    {
        public const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/Data/Build Prompt-B 200 Oxygen Upgrades")]
        public static void BuildFromMenu() => UnityEngine.Debug.Log(Build());

        public static string Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                return "FAIL: catalog not found";
            }

            var added = 0;
            var upgrades = new[]
            {
                MvpDataAssetBuilder.BuildMaximumOxygenUpgrade(),
                MvpDataAssetBuilder.BuildOxygenEfficiencyUpgrade()
            };

            // 해금 규칙은 새 에셋 2개에만 쓴다. 기존 업그레이드의 해금 값은 건드리지 않는다.
            upgrades[0].EditorSetUnlock(DataIds.Upgrades.MaximumEnergy, 1, DataIds.Upgrades.MaximumEnergy);
            upgrades[1].EditorSetUnlock(DataIds.Upgrades.MaximumOxygen, 1, DataIds.Upgrades.MaximumOxygen);
            EditorUtility.SetDirty(upgrades[0]);
            EditorUtility.SetDirty(upgrades[1]);

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("upgrades");
            for (var u = 0; u < upgrades.Length; u++)
            {
                var found = false;
                for (var i = 0; i < entries.arraySize; i++)
                {
                    if (entries.GetArrayElementAtIndex(i).objectReferenceValue == upgrades[u])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    entries.arraySize++;
                    entries.GetArrayElementAtIndex(entries.arraySize - 1).objectReferenceValue = upgrades[u];
                    added++;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            for (var u = 0; u < upgrades.Length; u++)
            {
                AssetDatabase.SaveAssetIfDirty(upgrades[u]);
            }

            AssetDatabase.SaveAssetIfDirty(catalog);
            return "Prompt-B 200 oxygen upgrades ready. newly registered: " + added;
        }
    }
}
