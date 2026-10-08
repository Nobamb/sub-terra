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
    /// 전진기지는 원래의 넓은 원본 그림과 2x2 설치 규격을 쓴다.
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
        public const string OutpostPlacementPath =
            "Assets/_Project/Data/Buildings/Placement/outpost_core_basicPlacement.asset";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public const string VisualRootName = "VisualRoot";
        public const string PoweredVisualRootName = "PoweredVisualRoot";

        [MenuItem("SubTerra/UI/Build Facility Grounded Visuals")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        [MenuItem("SubTerra/MVP2/Refresh Elevator And Outpost Polish")]
        public static void RefreshElevatorAndOutpostPolish()
        {
            SubTerra.App.Editor.ElevatorVisualPrefabSetup.RefreshExistingPrefab();
            RefreshOutpostVisual();
            Debug.Log("[SubTerra] Elevator lift motion and grounded outpost visual refreshed.");
        }

        public static void RefreshOutpostVisual()
        {
            ApplyPrefab(OutpostPrefabPath, FacilityVisualKind.OutpostCore, keepPoweredVisual: false);
            ApplyOutpostFootprint();
            ApplyDemoOutpostInIntegration();
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(OutpostPlacementPath));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<BuildingData>(GetBuildingDataPath(FacilityVisualKind.OutpostCore)));
            AssetDatabase.Refresh();
        }

        [MenuItem("SubTerra/Facilities/Restore Outpost Two By Two And Remove Demo Core")]
        public static void RestoreOutpostTwoByTwo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing outpost assets.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard pending scene edits before applying the outpost change.");

            var root = PrefabUtility.LoadPrefabContents(OutpostPrefabPath);
            try
            {
                Transform artwork = root.transform.Find("VisualRoot/Artwork") ?? root.transform.Find("VisualRoot/Diamond");
                if (artwork == null) throw new InvalidOperationException("Outpost artwork is missing.");
                artwork.GetComponent<SpriteRenderer>().sprite = GetArtworkSprite(FacilityVisualKind.OutpostCore);
                FacilityGroundedVisual.Apply(root.transform, "building.outpost_core.basic", new Vector2Int(2, 2));
                PrefabUtility.SaveAsPrefabAsset(root, OutpostPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            ApplyOutpostFootprint();
            UpdateBuildingDataIcon(FacilityVisualKind.OutpostCore);
            ApplyDemoOutpostInIntegration();
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(OutpostPlacementPath));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<BuildingData>(GetBuildingDataPath(FacilityVisualKind.OutpostCore)));
        }

        [MenuItem("SubTerra/Facilities/Refresh Charger Storage And Outpost")]
        public static void RefreshChargerStorageAndOutpost()
        {
            RefreshGeometryOnly(ChargerPrefabPath, "building.charger.basic", new Vector2Int(2, 2));
            RefreshGeometryOnly(StoragePrefabPath, "building.storage.basic", Vector2Int.one);
            RefreshGeometryOnly(OutpostPrefabPath, "building.outpost_core.basic", new Vector2Int(2, 2));
            UpdateBuildingDataIcon(FacilityVisualKind.Charger);
            UpdateBuildingDataIcon(FacilityVisualKind.Storage);
            UpdateBuildingDataIcon(FacilityVisualKind.OutpostCore);
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                "Assets/_Project/Data/Buildings/Placement/charger_basicPlacement.asset");
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("footprint").vector2IntValue = new Vector2Int(2, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SubTerra/Facilities/Refresh Grounded Building Menu Icons")]
        public static void RefreshGroundedBuildingMenuIcons()
        {
            foreach (var kind in new[] { FacilityVisualKind.Light, FacilityVisualKind.Charger, FacilityVisualKind.Storage,
                FacilityVisualKind.Settlement, FacilityVisualKind.OutpostCore, FacilityVisualKind.Clinic })
            {
                UpdateBuildingDataIcon(kind);
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<BuildingData>(GetBuildingDataPath(kind)));
            }
        }

        [MenuItem("SubTerra/Facilities/Sync Menu And Quest Facility Artwork")]
        public static void SyncMenuAndQuestArtwork()
        {
            RefreshLightAndSettlement();
            RefreshGroundedBuildingMenuIcons();
            PromptB1072QuestThumbnailBuilder.Apply();
            Debug.Log("[SubTerra] Facility prefab, building-menu and quest artwork references synchronized.");
        }

        private static void RefreshGeometryOnly(string path, string id, Vector2Int footprint)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                FacilityGroundedVisual.Apply(root.transform, id, footprint);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("SubTerra/Facilities/Refresh Light And Settlement One By Two")]
        public static void RefreshLightAndSettlement()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before refreshing facility assets.");
            foreach (string kind in new[] { "light", "settlement" })
            {
                string path = kind == "light" ? LightPrefabPath : SettlementPrefabPath;
                RefreshGeometryOnly(path, "building." + kind + ".basic", new Vector2Int(1, 2));
                var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                    "Assets/_Project/Data/Buildings/Placement/" + kind + "_basicPlacement.asset");
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("footprint").vector2IntValue = new Vector2Int(1, 2);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }

        public static string Build()
        {
            ApplyAllPrefabs();
            ApplyOutpostFootprint();
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
            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (wasLoaded && scene.isDirty)
                throw new InvalidOperationException("Save pending integration scene edits before removing the demo core.");
            bool additive = !string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
            if (!wasLoaded)
            {
                if (!additive && SceneManager.GetActiveScene().isDirty)
                    throw new InvalidOperationException("Save or discard the untitled scene before opening the integration scene.");
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
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

                // The old core also supplies the starting grid and is the drone's base-distance anchor.
                // Retain that fixed backend node and its fileIDs/references, but remove the facility,
                // its label/interaction identity and every visible child. No invisible BuildingInstance remains.
                var source = demo.GetComponent<SubTerra.Gameplay.Power.PowerNode>();
                if (source == null || !source.IsPowerSource)
                    throw new InvalidOperationException("Expected the demo core's starting power supply.");
                for (int i = demo.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(demo.transform.GetChild(i).gameObject);
                foreach (var component in demo.GetComponents<Component>())
                    if (!(component is Transform) && !(component is SubTerra.Gameplay.Power.PowerNode))
                        UnityEngine.Object.DestroyImmediate(component);
                demo.name = "SurfaceBasePowerSupply";

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasLoaded && additive && scene.IsValid() && scene.isLoaded)
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
            Transform artwork = CreatePart(
                visualRoot,
                "Artwork",
                sprite,
                Vector3.zero,
                sprite.bounds.size,
                Color.white,
                4);
            ApplyGroundedArtworkGeometry(artwork, sprite, kind);
            return artwork;
        }

        private static void ApplyOutpostFootprint()
        {
            BuildingPlacementDefinition definition =
                AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(OutpostPlacementPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Missing outpost placement definition: " + OutpostPlacementPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("footprint").vector2IntValue = new Vector2Int(2, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void ApplyGroundedArtworkGeometry(
            Transform artwork, Sprite sprite, FacilityVisualKind kind)
        {
            string id = kind == FacilityVisualKind.Charger ? "building.charger.basic"
                : kind == FacilityVisualKind.Storage ? "building.storage.basic"
                : kind == FacilityVisualKind.OutpostCore ? "building.outpost_core.basic"
                : kind == FacilityVisualKind.Light ? "building.light.basic"
                : kind == FacilityVisualKind.Settlement ? "building.settlement.basic" : null;
            Vector2Int footprint = kind == FacilityVisualKind.Charger ? new Vector2Int(2, 2)
                : kind == FacilityVisualKind.OutpostCore ? new Vector2Int(2, 2)
                : kind == FacilityVisualKind.Light || kind == FacilityVisualKind.Settlement ? new Vector2Int(1, 2) : Vector2Int.one;
            if (FacilityGroundedVisual.TryGetGeometry(sprite, id, footprint, out var position, out var size))
            {
                artwork.localPosition = position;
                artwork.localScale = size;
                return;
            }
            bool isLargeFacility = kind == FacilityVisualKind.Clinic
                || kind == FacilityVisualKind.OutpostCore;
            float maxWidth = kind == FacilityVisualKind.OutpostCore
                ? 1.8f
                : isLargeFacility ? 1.8f : 0.96f;
            float maxHeight = kind == FacilityVisualKind.OutpostCore
                ? 1.7f
                : isLargeFacility ? 1.6f : 0.96f;
            float groundY = isLargeFacility ? -1.2f : -0.68f;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Min(
                maxWidth / Mathf.Max(spriteSize.x, 0.0001f),
                maxHeight / Mathf.Max(spriteSize.y, 0.0001f));
            float height = spriteSize.y * scale;
            artwork.localPosition = new Vector3(0f, groundY + height * 0.5f, 0f);
            artwork.localScale = new Vector3(scale, scale, 1f);
        }

        private static string GetArtworkPath(FacilityVisualKind kind)
        {
            return kind switch
            {
                FacilityVisualKind.Light => "Assets/_Project/Art/Facilities/MVP/light_basic_cartoon_v3.png",
                FacilityVisualKind.Charger => "Assets/_Project/Resources/Facilities/ChargerGrounded.png",
                FacilityVisualKind.Storage => "Assets/_Project/Resources/Facilities/StorageGrounded.png",
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
