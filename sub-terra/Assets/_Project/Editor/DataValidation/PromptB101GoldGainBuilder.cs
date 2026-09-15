using SubTerra.App.Core.Data;
using UnityEditor;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>골드 업그레이드 에셋·카탈로그와 Integration UpgradePanel만 갱신한다.</summary>
    public static class PromptB101GoldGainBuilder
    {
        public const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        public const string UpgradePath = "Assets/_Project/Data/Upgrades/Upgrade_Cargo_Gold.asset";

        [MenuItem("SubTerra/UI/Build Prompt-B 101 Gold Gain Upgrade")]
        public static void Build()
        {
            var upgrade = MvpDataAssetBuilder.BuildGoldGainUpgrade();
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("upgrades");
            var found = false;
            for (var i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).objectReferenceValue == upgrade) found = true;
            if (!found)
            {
                entries.arraySize++;
                entries.GetArrayElementAtIndex(entries.arraySize - 1).objectReferenceValue = upgrade;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssetIfDirty(upgrade);
            AssetDatabase.SaveAssetIfDirty(catalog);
            PromptB97YieldUpgradeLayoutBuilder.Build();
        }
    }
}
