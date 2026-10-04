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
    /// 시설 6종의 원본 아트를 유지하면서 지형 상단에 얕게 겹치도록 배치한다.
    /// 보건소는 2x2 점유 영역을 쓰고 나머지 시설은 기존 1x1 점유 영역을 유지한다.
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
        public const string ClinicPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/ClinicFacility.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public const string VisualRootName = "VisualRoot";
        public const string PoweredVisualRootName = "PoweredVisualRoot";

        [MenuItem("SubTerra/UI/Build Facility Grounded Visuals")]
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
            return "Facility grounded visuals applied.";
        }

        public static void ApplyAllPrefabs()
        {
            ApplyPrefab(LightPrefabPath, FacilityVisualKind.Light, keepPoweredVisual: true);
            ApplyPrefab(ChargerPrefabPath, FacilityVisualKind.Charger, keepPoweredVisual: true);
            ApplyPrefab(StoragePrefabPath, FacilityVisualKind.Storage, keepPoweredVisual: true);
            ApplyPrefab(SettlementPrefabPath, FacilityVisualKind.Settlement, keepPoweredVisual: true);
            ApplyPrefab(OutpostPrefabPath, FacilityVisualKind.OutpostCore, keepPoweredVisual: false);
            ApplyPrefab(ClinicPrefabPath, FacilityVisualKind.Clinic, keepPoweredVisual: true);
        }

        public static void ApplyVisual(GameObject root, FacilityVisualKind kind, bool keepPoweredVisual)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Sprite box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite artwork = GetArtworkSprite(kind);
            if (box == null || artwork == null)
            {
                throw new InvalidOperationException("Facility artwork or preview sprite is missing: " + kind);
            }

            var rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                // 루트 점 스프라이트는 끄고 원본 시설 아트만 표시한다.
                rootRenderer.enabled = false;
            }

            var visualRoot = EnsureChild(root.transform, VisualRootName);
            ClearChildren(visualRoot);
            Transform artworkTransform = CreateGroundedArtwork(visualRoot, artwork, kind);

            if (keepPoweredVisual)
            {
                var powered = EnsureChild(root.transform, PoweredVisualRootName);
                ClearChildren(powered);
                powered.localPosition = artworkTransform.localPosition;
                powered.localRotation = Quaternion.identity;
                powered.localScale = Vector3.one;
                CreatePart(
                    powered,
                    "PowerGlow",
                    box,
                    Vector3.zero,
                    new Vector2(1.02f, 1.02f),
                    new Color(1f, 1f, 1f, 0.28f),
                    3);
                if (kind == FacilityVisualKind.Light)
                {
                    PromptB50GasVisionBuilder.ApplyLightClearance(root.transform);
                }

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

        public static Sprite GetArtworkSprite(FacilityVisualKind kind)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(GetArtworkPath(kind));
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
                UpdateBuildingDataIcon(kind);
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

                // 기존 씬 오브젝트의 자식/fileID를 보존한다. 과거 빌더가 붙인
                // Diamond 아트의 바닥만 새 규격으로 내리고, 없는 경우에만 재구성한다.
                Transform existingArt = demo.transform.Find("VisualRoot/Diamond");
                if (existingArt != null && existingArt.GetComponent<SpriteRenderer>() != null)
                {
                    Vector3 local = existingArt.localPosition;
                    existingArt.localPosition = new Vector3(local.x, -0.227492f, local.z);
                }
                else
                {
                    ApplyVisual(demo, FacilityVisualKind.OutpostCore, keepPoweredVisual: false);
                }
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

        private static Transform CreateGroundedArtwork(
            Transform visualRoot, Sprite sprite, FacilityVisualKind kind)
        {
            // 점유 영역 아래 암석의 윗면보다 약 0.2칸 내려 놓는다.
            // SpriteRenderer가 지형보다 앞에 그려져 시설 실루엣은 가리지 않는다.
            bool isClinic = kind == FacilityVisualKind.Clinic;
            float maxWidth = isClinic ? 1.8f : 0.96f;
            float maxHeight = isClinic ? 1.6f : 0.96f;
            float groundY = isClinic ? -1.2f : -0.68f;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Min(
                maxWidth / Mathf.Max(spriteSize.x, 0.0001f),
                maxHeight / Mathf.Max(spriteSize.y, 0.0001f));
            float height = spriteSize.y * scale;
            var position = new Vector3(0f, groundY + height * 0.5f, 0f);
            return CreatePart(
                visualRoot,
                "Artwork",
                sprite,
                position,
                spriteSize * scale,
                Color.white,
                4);
        }

        private static string GetArtworkPath(FacilityVisualKind kind)
        {
            return kind switch
            {
                FacilityVisualKind.Light => "Assets/_Project/Art/Facilities/MVP/light_basic_cartoon_v3.png",
                FacilityVisualKind.Charger => "Assets/_Project/Art/Facilities/MVP/charger_basic_cartoon_v2.png",
                FacilityVisualKind.Storage => "Assets/_Project/Art/Facilities/MVP/storage_basic_cartoon_v2.png",
                FacilityVisualKind.Settlement => "Assets/_Project/Art/Facilities/MVP/settlement_console_cartoon_v3.png",
                FacilityVisualKind.OutpostCore => "Assets/_Project/Art/Facilities/MVP/outpost_core_cartoon_v3.png",
                FacilityVisualKind.Clinic => "Assets/_Project/Art/Facilities/MVP/clinic_basic_cartoon_v3.png",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static string GetBuildingDataPath(FacilityVisualKind kind)
        {
            return kind switch
            {
                FacilityVisualKind.Light => "Assets/_Project/Data/Buildings/Building_Light_Basic.asset",
                FacilityVisualKind.Charger => "Assets/_Project/Data/Buildings/Building_Charger_Basic.asset",
                FacilityVisualKind.Storage => "Assets/_Project/Data/Buildings/Building_Storage_Basic.asset",
                FacilityVisualKind.Settlement => "Assets/_Project/Data/Buildings/Building_Settlement_Basic.asset",
                FacilityVisualKind.OutpostCore => "Assets/_Project/Data/Buildings/Building_OutpostCore_Basic.asset",
                FacilityVisualKind.Clinic => "Assets/_Project/Data/Buildings/Building_Clinic_Basic.asset",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static void UpdateBuildingDataIcon(FacilityVisualKind kind)
        {
            BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(GetBuildingDataPath(kind));
            if (data == null) return;

            var serialized = new SerializedObject(data);
            serialized.FindProperty("icon").objectReferenceValue = GetArtworkSprite(kind);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
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
        OutpostCore = 4,
        Clinic = 5
    }
}
#endif
