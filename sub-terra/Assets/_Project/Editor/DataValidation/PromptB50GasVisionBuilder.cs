#if UNITY_EDITOR
using System;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 50: 가스 생성/접근 연출과 조명 시야 구멍을 프리팹·Integration에 적용한다.
    /// 대상: GasZone.prefab, LightFacility.prefab, Mine_Demo_Integration.unity.
    /// </summary>
    public static class PromptB50GasVisionBuilder
    {
        public const string GasZonePrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Hazards/GasZone.prefab";
        public const string LightPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Power/LightFacility.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public const string GasCloudName = GasVisualRules.GasCloudChildName;
        public const string LightClearanceName = GasVisualRules.LightClearanceChildName;
        public const string PoweredVisualRootName = "PoweredVisualRoot";

        [MenuItem("SubTerra/UI/Build Prompt-B 50 Gas Vision")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            ApplyGasZonePrefab();
            ApplyLightPrefab();
            ApplyIntegrationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 50-1 gas vision applied.";
        }

        public static void ApplyGasZonePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GasZonePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing GasZone prefab: " + GasZonePrefabPath);
            }

            var root = PrefabUtility.LoadPrefabContents(GasZonePrefabPath);
            try
            {
                ApplyGasZoneVisual(root);
                PrefabUtility.SaveAsPrefabAsset(root, GasZonePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ApplyLightPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing LightFacility prefab: " + LightPrefabPath);
            }

            var root = PrefabUtility.LoadPrefabContents(LightPrefabPath);
            try
            {
                var powered = EnsureChild(root.transform, PoweredVisualRootName);
                ApplyLightClearance(powered);
                PrefabUtility.SaveAsPrefabAsset(root, LightPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ApplyGasZoneVisual(GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            root.transform.localScale = Vector3.one;
            var zone = root.GetComponent<GasZone>();
            if (zone == null)
            {
                zone = root.AddComponent<GasZone>();
            }

            var zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("radius").floatValue = GasVisualRules.GasRadiusBlocks;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            var trigger = root.GetComponent<CircleCollider2D>();
            if (trigger == null)
            {
                trigger = root.AddComponent<CircleCollider2D>();
            }

            trigger.isTrigger = true;
            trigger.radius = GasVisualRules.GasRadiusBlocks;

            var visual = root.GetComponent<GasZoneVisual>();
            if (visual == null)
            {
                visual = root.AddComponent<GasZoneVisual>();
            }

            var rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            var cloud = EnsureChild(root.transform, GasCloudName);
            var renderer = cloud.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = cloud.gameObject.AddComponent<SpriteRenderer>();
            }

            var knob = LoadKnob();
            renderer.sprite = knob;
            renderer.color = new Color(0.22f, 0.82f, 0.34f, 0f);
            renderer.sortingOrder = 20;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            GasZoneVisual.FitSpriteToWorldDiameter(renderer, 0.01f);

            var visualSo = new SerializedObject(visual);
            visualSo.FindProperty("cloudRenderer").objectReferenceValue = renderer;
            visualSo.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ApplyLightClearance(Transform poweredRoot)
        {
            if (poweredRoot == null)
            {
                throw new ArgumentNullException(nameof(poweredRoot));
            }

            var existing = poweredRoot.Find(LightClearanceName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject(LightClearanceName);
            go.transform.SetParent(poweredRoot, false);
            go.transform.localPosition = Vector3.zero;

            var knob = LoadKnob();
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = knob;
            renderer.color = new Color(1f, 0.12f, 0.08f, GasVisualRules.LightClearRedOpacity);
            renderer.sortingOrder = 22;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            GasZoneVisual.FitSpriteToWorldDiameter(
                renderer,
                GasVisualRules.LightClearRadiusBlocks * 2f);

            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = knob;
            mask.isCustomRangeActive = true;
            mask.backSortingOrder = 19;
            mask.frontSortingOrder = GasVisionWorldVeil.SortingOrder;

            var source = go.AddComponent<GasVisionClearanceSource>();
            source.SetRadius(GasVisualRules.LightClearRadiusBlocks);
        }

        private static void ApplyIntegrationScene()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                var gasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GasZonePrefabPath);
                var gasZone = gasPrefab != null ? gasPrefab.GetComponent<GasZone>() : null;
                GasHazardSystem hazard = null;
                GasExposureEffectController effect = null;
                CanvasGroup overlay = null;
                Camera mainCamera = null;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (hazard == null)
                    {
                        hazard = root.GetComponentInChildren<GasHazardSystem>(true);
                    }

                    if (effect == null)
                    {
                        effect = root.GetComponentInChildren<GasExposureEffectController>(true);
                    }

                    if (overlay == null)
                    {
                        overlay = FindOverlay(root.transform);
                    }

                    if (mainCamera == null)
                    {
                        mainCamera = root.GetComponentInChildren<Camera>(true);
                    }
                }

                if (hazard != null)
                {
                    var so = new SerializedObject(hazard);
                    so.FindProperty("defaultRadius").floatValue = GasVisualRules.GasRadiusBlocks;
                    so.FindProperty("gasZonePrefab").objectReferenceValue = gasZone;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (effect != null)
                {
                    var so = new SerializedObject(effect);
                    var settings = so.FindProperty("settings");
                    settings.FindPropertyRelative("maximumVisionObscuration").floatValue =
                        GasVisualRules.FullApproachOpacity;
                    settings.FindPropertyRelative("initialVisionObscuration").floatValue =
                        GasVisualRules.InitialApproachOpacity;
                    settings.FindPropertyRelative("approachFadeSeconds").floatValue =
                        GasVisualRules.ApproachFadeSeconds;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (overlay != null)
                {
                    var image = overlay.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(0.04f, 0.05f, 0.05f, 1f);
                    }
                }

                var veil = EnsureWorldVeil(scene, mainCamera);
                if (effect != null && veil != null)
                {
                    var so = new SerializedObject(effect);
                    so.FindProperty("worldVeil").objectReferenceValue = veil;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GasVisionWorldVeil EnsureWorldVeil(Scene scene, Camera camera)
        {
            GasVisionWorldVeil existing = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                existing = root.GetComponentInChildren<GasVisionWorldVeil>(true);
                if (existing != null)
                {
                    break;
                }
            }

            if (existing == null)
            {
                var go = new GameObject(GasVisionWorldVeil.ObjectName);
                SceneManager.MoveGameObjectToScene(go, scene);
                if (camera != null)
                {
                    go.transform.SetParent(camera.transform, false);
                    go.transform.localPosition = Vector3.zero;
                }

                existing = go.AddComponent<GasVisionWorldVeil>();
            }

            var renderer = existing.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                renderer.sortingOrder = GasVisionWorldVeil.SortingOrder;
                renderer.color = new Color(0.04f, 0.05f, 0.05f, 0f);
            }

            return existing;
        }

        private static CanvasGroup FindOverlay(Transform parent)
        {
            if (parent.name == "GasVisionOverlay")
            {
                return parent.GetComponent<CanvasGroup>();
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindOverlay(parent.GetChild(i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private static Sprite LoadKnob()
        {
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (knob == null)
            {
                throw new InvalidOperationException("Built-in Knob sprite is required for gas vision.");
            }

            return knob;
        }
    }
}
#endif
