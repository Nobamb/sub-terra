#if UNITY_EDITOR
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Creates the six concrete Phase I facility prefabs and points BuildingData at them.</summary>
    public static class PhaseIFacilityAssetBuilder
    {
        private const string PowerPrefabFolder = "Assets/_Project/Prefabs/Gameplay/Power";
        private const string BuildingDataFolder = "Assets/_Project/Data/Buildings";

        [MenuItem("Tools/SubTerra/Phase I/Build Facility Runtime Prefabs")]
        public static void BuildFromMenu()
        {
            MvpDataAssetBuilder.BuildAll();
            PhaseFSupportBuilder.Build();
            EnsureFolder("Assets/_Project/Prefabs/Gameplay", "Power");

            GameObject light = CreateFacility("LightFacility", 1, PowerPriority.High, new Color(1f, 0.86f, 0.25f));
            GameObject charger = CreateFacility("ChargerFacility", 3, PowerPriority.Normal, new Color(0.25f, 0.95f, 0.45f));
            GameObject storage = CreateFacility("StorageFacility", 1, PowerPriority.Normal, new Color(0.38f, 0.66f, 1f));
            GameObject settlement = CreateFacility("SettlementFacility", 1, PowerPriority.Low, new Color(0.88f, 0.45f, 0.95f));
            GameObject core = CreateCore();

            UpdateBuildingData("Building_Light_Basic.asset", light);
            UpdateBuildingData("Building_Charger_Basic.asset", charger);
            UpdateBuildingData("Building_Storage_Basic.asset", storage);
            UpdateBuildingData("Building_Settlement_Basic.asset", settlement);
            UpdateBuildingData("Building_OutpostCore_Basic.asset", core);
            RestoreMineralVisualIcons();
            FacilityPurchaseFlowBuilder.Build();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SubTerra] Phase I facility prefabs and BuildingData references are ready.");
        }

        private static void RestoreMineralVisualIcons()
        {
            var pairs = new[]
            {
                ("Assets/_Project/Data/Minerals/Mineral_Copper.asset", "Assets/_Project/Visuals/Graybox/Terrain/CopperVisual.asset"),
                ("Assets/_Project/Data/Minerals/Mineral_Iron.asset", "Assets/_Project/Visuals/Graybox/Terrain/IronVisual.asset"),
                ("Assets/_Project/Data/Minerals/Mineral_Lithium.asset", "Assets/_Project/Visuals/Graybox/Terrain/LithiumVisual.asset")
            };

            for (var i = 0; i < pairs.Length; i++)
            {
                var mineral = AssetDatabase.LoadAssetAtPath<MineralData>(pairs[i].Item1);
                var assets = AssetDatabase.LoadAllAssetsAtPath(pairs[i].Item2);
                Sprite icon = null;
                for (var j = 0; j < assets.Length; j++)
                {
                    icon = assets[j] as Sprite;
                    if (icon != null) break;
                }

                if (mineral == null || icon == null)
                {
                    throw new System.InvalidOperationException("Missing mineral visual data: " + pairs[i].Item1);
                }

                mineral.EditorSet(mineral.Id, mineral.DisplayName, mineral.UnitWeight, mineral.UnitPrice, icon);
                EditorUtility.SetDirty(mineral);
            }
        }

        private static GameObject CreateFacility(
            string name,
            int demand,
            PowerPriority priority,
            Color color)
        {
            var root = new GameObject(name);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = color;
            root.AddComponent<BuildingInstance>();
            var node = root.AddComponent<PowerNode>();
            node.Configure(null, false, 0, demand, priority);

            var visualRoot = new GameObject("PoweredVisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localScale = Vector3.one * 0.58f;
            var visual = visualRoot.AddComponent<SpriteRenderer>();
            visual.sprite = renderer.sprite;
            visual.color = Color.white;
            visualRoot.SetActive(false);

            var facility = root.AddComponent<PowerFacility>();
            var serialized = new SerializedObject(facility);
            serialized.FindProperty("powerNode").objectReferenceValue = node;
            serialized.FindProperty("poweredVisuals").arraySize = 1;
            serialized.FindProperty("poweredVisuals").GetArrayElementAtIndex(0).objectReferenceValue = visualRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // VisualRoot is deliberately present on every consumer. Light and charger use it immediately,
            // while storage and settlement can later replace it with authored interaction visuals.
            var prefab = SavePrefab(root, name);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCore()
        {
            const string name = "OutpostCore";
            var root = new GameObject(name);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.18f, 0.72f, 1f);
            root.AddComponent<BuildingInstance>();
            root.AddComponent<PowerNode>().Configure(null, true, 5, 0, PowerPriority.Critical);
            var prefab = SavePrefab(root, name);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            return PrefabUtility.SaveAsPrefabAsset(root, PowerPrefabFolder + "/" + name + ".prefab");
        }

        private static void UpdateBuildingData(string fileName, GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(BuildingDataFolder + "/" + fileName);
            if (data == null)
            {
                Debug.LogError("[SubTerra] Missing BuildingData: " + fileName);
                return;
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("runtimePrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
