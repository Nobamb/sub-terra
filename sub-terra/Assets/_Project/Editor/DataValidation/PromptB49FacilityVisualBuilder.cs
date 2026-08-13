#if UNITY_EDITOR
using System;
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 49: 조명·충전기·보관함·정산 콘솔·전진기지 코어를 1칸 블록 형태로 구분한다.
    /// 대상: Power 시설 Prefab 5개와 Integration 씬의 OutpostCore_Demo.
    /// </summary>
    public static class PromptB49FacilityVisualBuilder
    {
        public const string LightPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/LightFacility.prefab";
        public const string ChargerPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/ChargerFacility.prefab";
        public const string StoragePrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/StorageFacility.prefab";
        public const string SettlementPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/SettlementFacility.prefab";
        public const string OutpostPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/OutpostCore.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public const string VisualRootName = "VisualRoot";
        public const string PoweredVisualRootName = "PoweredVisualRoot";

        [MenuItem("SubTerra/UI/Build Prompt-B 49 Facility 1-Tile Visuals")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            ApplyAllPrefabs();
            ApplyDemoOutpostInIntegration();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 49 facility 1-tile visuals applied.";
        }

        public static void ApplyAllPrefabs()
        {
            ApplyPrefab(LightPrefabPath, FacilityVisualKind.Light, keepPoweredVisual: true);
            ApplyPrefab(ChargerPrefabPath, FacilityVisualKind.Charger, keepPoweredVisual: true);
            ApplyPrefab(StoragePrefabPath, FacilityVisualKind.Storage, keepPoweredVisual: true);
            ApplyPrefab(SettlementPrefabPath, FacilityVisualKind.Settlement, keepPoweredVisual: true);
            ApplyPrefab(OutpostPrefabPath, FacilityVisualKind.OutpostCore, keepPoweredVisual: false);
        }

        public static void ApplyVisual(GameObject root, FacilityVisualKind kind, bool keepPoweredVisual)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (box == null || knob == null)
            {
                throw new InvalidOperationException("Built-in UI sprites are required for facility visuals.");
            }

            var rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                // 루트 점 스프라이트는 끄고 VisualRoot가 1칸 본체를 담당한다.
                rootRenderer.enabled = false;
            }

            var visualRoot = EnsureChild(root.transform, VisualRootName);
            ClearChildren(visualRoot);
            BuildKind(visualRoot, kind, box, knob);

            if (keepPoweredVisual)
            {
                var powered = EnsureChild(root.transform, PoweredVisualRootName);
                ClearChildren(powered);
                CreatePart(
                    powered,
                    "PowerGlow",
                    box,
                    Vector3.zero,
                    new Vector2(1.08f, 1.08f),
                    new Color(1f, 1f, 1f, 0.28f),
                    3);
                powered.gameObject.SetActive(false);

                var facility = root.GetComponent<SubTerra.Gameplay.Power.PowerFacility>();
                if (facility != null)
                {
                    var so = new SerializedObject(facility);
                    var visuals = so.FindProperty("poweredVisuals");
                    visuals.arraySize = 1;
                    visuals.GetArrayElementAtIndex(0).objectReferenceValue = powered.gameObject;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ApplyPrefab(string path, FacilityVisualKind kind, bool keepPoweredVisual)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing facility prefab: " + path);
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyVisual(root, kind, keepPoweredVisual);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyDemoOutpostInIntegration()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject demo = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    demo = FindChildByName(root.transform, "OutpostCore_Demo");
                    if (demo != null)
                    {
                        break;
                    }
                }

                if (demo == null)
                {
                    return;
                }

                ApplyVisual(demo, FacilityVisualKind.OutpostCore, keepPoweredVisual: false);
                var instance = demo.GetComponent<BuildingInstance>();
                if (instance == null)
                {
                    instance = demo.AddComponent<BuildingInstance>();
                }

                if (string.IsNullOrWhiteSpace(instance.BuildingId))
                {
                    instance.Initialize("outpost.demo", DataIds.Buildings.OutpostCoreBasic);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded
                    && SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void BuildKind(
            Transform visualRoot,
            FacilityVisualKind kind,
            Sprite box,
            Sprite knob)
        {
            switch (kind)
            {
                case FacilityVisualKind.Light:
                    CreatePart(visualRoot, "LampGlow", knob, Vector3.zero, new Vector2(1f, 1f), new Color(1f, 0.93f, 0.45f, 0.32f), 3);
                    CreatePart(visualRoot, "LampHead", knob, Vector3.zero, new Vector2(0.78f, 0.78f), new Color(1f, 0.86f, 0.25f, 1f), 5);
                    CreatePart(visualRoot, "LampStem", box, new Vector3(0f, -0.28f, 0f), new Vector2(0.16f, 0.36f), new Color(0.42f, 0.34f, 0.12f, 1f), 4);
                    break;
                case FacilityVisualKind.Charger:
                    CreatePart(visualRoot, "Body", box, Vector3.zero, new Vector2(1f, 1f), new Color(0.18f, 0.72f, 0.36f, 1f), 4);
                    CreatePart(visualRoot, "PlusV", box, Vector3.zero, new Vector2(0.18f, 0.62f), new Color(0.08f, 0.22f, 0.12f, 1f), 6);
                    CreatePart(visualRoot, "PlusH", box, Vector3.zero, new Vector2(0.62f, 0.18f), new Color(0.08f, 0.22f, 0.12f, 1f), 6);
                    break;
                case FacilityVisualKind.Storage:
                    CreatePart(visualRoot, "Chest", box, new Vector3(0f, -0.08f, 0f), new Vector2(1f, 0.76f), new Color(0.28f, 0.52f, 0.92f, 1f), 4);
                    CreatePart(visualRoot, "Lid", box, new Vector3(0f, 0.34f, 0f), new Vector2(1f, 0.22f), new Color(0.16f, 0.32f, 0.68f, 1f), 5);
                    CreatePart(visualRoot, "Handle", knob, new Vector3(0f, 0.16f, 0f), new Vector2(0.2f, 0.12f), new Color(0.92f, 0.78f, 0.28f, 1f), 6);
                    break;
                case FacilityVisualKind.Settlement:
                    CreatePart(visualRoot, "Stand", box, new Vector3(0f, -0.4f, 0f), new Vector2(0.32f, 0.2f), new Color(0.38f, 0.16f, 0.46f, 1f), 4);
                    CreatePart(visualRoot, "Console", box, new Vector3(0f, 0.08f, 0f), new Vector2(1f, 0.72f), new Color(0.78f, 0.38f, 0.88f, 1f), 5);
                    CreatePart(visualRoot, "Screen", box, new Vector3(0f, 0.14f, 0f), new Vector2(0.72f, 0.32f), new Color(0.18f, 0.08f, 0.26f, 1f), 6);
                    break;
                case FacilityVisualKind.OutpostCore:
                    var diamond = CreatePart(
                        visualRoot,
                        "Diamond",
                        box,
                        Vector3.zero,
                        new Vector2(0.7f, 0.7f),
                        new Color(0.18f, 0.72f, 1f, 1f),
                        4);
                    diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    CreatePart(visualRoot, "Core", knob, Vector3.zero, new Vector2(0.34f, 0.34f), new Color(0.85f, 0.97f, 1f, 1f), 6);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static Transform CreatePart(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;
            if (sprite != null)
            {
                var bounds = sprite.bounds.size;
                var scaleX = bounds.x > 0.0001f ? size.x / bounds.x : size.x;
                var scaleY = bounds.y > 0.0001f ? size.y / bounds.y : size.y;
                go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            return go.transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static GameObject FindChildByName(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildByName(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }

    public enum FacilityVisualKind
    {
        Light = 0,
        Charger = 1,
        Storage = 2,
        Settlement = 3,
        OutpostCore = 4
    }
}
#endif
