using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 97: Integration 업그레이드 창에 없는 카탈로그 엔트리만 기존 버튼을 복제해 추가한다.
    /// Surface Base·폰트·다른 Prefab은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB97YieldUpgradeLayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/UI/Build Prompt-B 97 Mining Yield Upgrade Entries")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open integration";
            }

            var upgrade = FindTransform(scene, "UpgradePanel");
            if (upgrade == null)
            {
                RestoreScene(previous, IntegrationScenePath);
                return "SKIP: UpgradePanel missing";
            }

            var added = AddMissingEntries(upgrade);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, IntegrationScenePath);
            return "Integration UpgradePanel added=" + added;
        }

        private static int AddMissingEntries(Transform upgrade)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            var view = upgrade.GetComponent<ProgressionPanelView>();
            var binder = upgrade.GetComponent<ProgressionPanelBinder>()
                ?? upgrade.GetComponentInParent<ProgressionPanelBinder>();
            if (catalog == null || catalog.Upgrades == null || view == null || binder == null)
            {
                return 0;
            }

            var existing = upgrade.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true);
            if (existing.Length == 0)
            {
                return 0;
            }

            var template = existing[0];
            var entries = new List<ProgressionUpgradeEntryButton>(existing);
            var added = 0;
            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var data = catalog.Upgrades[i];
                if (data == null || string.IsNullOrEmpty(data.Id) || Contains(entries, data.Id))
                {
                    continue;
                }

                var clone = Object.Instantiate(template, template.transform.parent);
                clone.name = "UpgradeEntry_" + data.Id.Replace('.', '_');
                clone.Configure(data.Id, binder, clone.GetComponentInChildren<TMP_Text>(true));
                var label = clone.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = ItemDisplayNames.PreferDisplay(data.Id, data.DisplayName);
                }

                clone.EnsureInteractable();
                entries.Add(clone);
                added++;
            }

            var so = new SerializedObject(view);
            var prop = so.FindProperty("upgradeButtons");
            if (prop != null)
            {
                prop.arraySize = entries.Count;
                for (var i = 0; i < entries.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(view);
            return added;
        }

        private static bool Contains(List<ProgressionUpgradeEntryButton> entries, string upgradeId)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].UpgradeId == upgradeId)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindChildRecursive(roots[i].transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void RestoreScene(string previous, string current)
        {
            if (!string.IsNullOrEmpty(previous)
                && previous != current
                && System.IO.File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}
