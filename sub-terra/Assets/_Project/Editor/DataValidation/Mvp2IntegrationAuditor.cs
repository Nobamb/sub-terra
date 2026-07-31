using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Readiness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Integration Scene/Prefab/BuildingData를 읽기 전용으로 감사한다.
    /// SaveScene/SetDirty/SaveAssets를 호출하지 않으며 사용자 세이브에 접근하지 않는다.
    /// </summary>
    public static class Mvp2IntegrationAuditor
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public const string DefaultCatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        private static readonly string[] RequiredRootNames =
        {
            "GameplayRoot",
            "ApplicationRoot",
            "HUDCanvas"
        };

        private static readonly string[] RequiredNamedObjects =
        {
            "Grid",
            "BackgroundTilemap",
            "ForegroundTilemap",
            "HazardTilemap",
            "BuildingTilemap",
            "RuntimeBuildings"
        };

        private static readonly string[] RequiredBinderFields =
        {
            "buildingPlacementSystem",
            "hudBinder",
            "worldSnapshotProviderBehaviour",
            "buildingUiBinder"
        };

        /// <summary>읽기 전용 전체 감사. 에셋을 저장하지 않는다.</summary>
        public static List<IntegrationAuditFinding> AuditAll(bool includeCatalogPlaceholders = true)
        {
            var findings = new List<IntegrationAuditFinding>();
            AuditIntegrationScene(findings);
            if (includeCatalogPlaceholders)
            {
                AuditBuildingRuntimePrefabs(findings);
            }

            return findings;
        }

        public static void AuditIntegrationScene(List<IntegrationAuditFinding> findings)
        {
            if (findings == null)
            {
                return;
            }

            if (!System.IO.File.Exists(
                    System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(Application.dataPath, "..", IntegrationScenePath))))
            {
                // Unity 프로젝트 상대 경로는 AssetDatabase로 확인한다.
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath);
            if (sceneAsset == null)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.RequiredStructure,
                    IntegrationScenePath,
                    "scene",
                    "Mine_Demo_Integration scene asset is missing."));
                return;
            }

            // Additive open + close without save keeps the audit read-only.
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                CollectMissingScripts(scene, findings);
                CollectRequiredStructure(scene, findings);
                CollectDuplicates(scene, findings);
                CollectBinderMissingReferences(scene, findings);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void AuditBuildingRuntimePrefabs(List<IntegrationAuditFinding> findings)
        {
            if (findings == null)
            {
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(DefaultCatalogPath);
            if (catalog == null)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.MissingReference,
                    DefaultCatalogPath,
                    "catalog",
                    "GameDataCatalog asset is missing."));
                return;
            }

            var buildings = catalog.Buildings;
            if (buildings == null)
            {
                return;
            }

            for (var i = 0; i < buildings.Count; i++)
            {
                var data = buildings[i];
                if (data == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.MissingReference,
                        DefaultCatalogPath,
                        "buildings[" + i + "]",
                        "Null BuildingData entry."));
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(data);
                if (data.RuntimePrefab == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.MissingReference,
                        assetPath,
                        "runtimePrefab",
                        "Required runtime prefab is missing for building id '" + data.Id + "'."));
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(data.RuntimePrefab);
                var label = PlaceholderRuntimeClassifier.ClassifyLabel(
                    data.RuntimePrefab.name,
                    prefabPath,
                    false);
                if (label == "placeholder")
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.PlaceholderRuntime,
                        assetPath,
                        "runtimePrefab",
                        "Building '" + data.Id + "' uses temporary placeholder prefab '" +
                        data.RuntimePrefab.name + "' (" + prefabPath + ")."));
                }
            }
        }

        /// <summary>
        /// 인메모리/테스트 시 누락 참조 검출용. CatalogValidator 결과 메시지를 감사 형식으로 변환한다.
        /// </summary>
        public static List<IntegrationAuditFinding> FindingsFromCatalogValidation(
            CatalogValidationResult validation)
        {
            var list = new List<IntegrationAuditFinding>();
            if (validation == null)
            {
                return list;
            }

            for (var i = 0; i < validation.Issues.Count; i++)
            {
                var issue = validation.Issues[i];
                if (issue.Severity != CatalogIssueSeverity.Error)
                {
                    continue;
                }

                list.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.MissingReference,
                    issue.AssetPath,
                    issue.FieldName,
                    issue.Message));
            }

            return list;
        }

        private static void CollectMissingScripts(Scene scene, List<IntegrationAuditFinding> findings)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var components = transform.GetComponents<Component>();
                    for (var i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            findings.Add(new IntegrationAuditFinding(
                                IntegrationFindingKind.MissingScript,
                                IntegrationScenePath + ":" + GetHierarchyPath(transform),
                                "component[" + i + "]",
                                "Missing script on GameObject '" + transform.name + "'."));
                        }
                    }
                }
            }
        }

        private static void CollectRequiredStructure(Scene scene, List<IntegrationAuditFinding> findings)
        {
            for (var i = 0; i < RequiredRootNames.Length; i++)
            {
                if (FindRoot(scene, RequiredRootNames[i]) == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.RequiredStructure,
                        IntegrationScenePath,
                        RequiredRootNames[i],
                        "Required root GameObject is missing."));
                }
            }

            if (FindInScene<EventSystem>(scene) == null)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.RequiredStructure,
                    IntegrationScenePath,
                    "EventSystem",
                    "Required EventSystem is missing."));
            }

            for (var i = 0; i < RequiredNamedObjects.Length; i++)
            {
                if (FindByName(scene, RequiredNamedObjects[i]) == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.RequiredStructure,
                        IntegrationScenePath,
                        RequiredNamedObjects[i],
                        "Required named object is missing."));
                }
            }

            if (FindInScene<IntegrationRuntimeBinder>(scene) == null)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.RequiredStructure,
                    IntegrationScenePath,
                    "IntegrationRuntimeBinder",
                    "Required IntegrationRuntimeBinder is missing."));
            }
        }

        private static void CollectDuplicates(Scene scene, List<IntegrationAuditFinding> findings)
        {
            CountAndReportDuplicates<EventSystem>(scene, findings, "EventSystem");
            CountAndReportDuplicatesByName(scene, findings, "ApplicationRoot");
            CountAndReportDuplicatesByName(scene, findings, "GameplayRoot");
            CountAndReportDuplicatesByName(scene, findings, "HUDCanvas");
            CountAndReportDuplicates<IntegrationRuntimeBinder>(scene, findings, "IntegrationRuntimeBinder");
        }

        private static void CollectBinderMissingReferences(
            Scene scene,
            List<IntegrationAuditFinding> findings)
        {
            var binder = FindInScene<IntegrationRuntimeBinder>(scene);
            if (binder == null)
            {
                return;
            }

            var so = new SerializedObject(binder);
            for (var i = 0; i < RequiredBinderFields.Length; i++)
            {
                var field = RequiredBinderFields[i];
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.MissingReference,
                        IntegrationScenePath + ":IntegrationRuntimeBinder",
                        field,
                        "Serialized field not found on IntegrationRuntimeBinder."));
                    continue;
                }

                if (prop.propertyType == SerializedPropertyType.ObjectReference
                    && prop.objectReferenceValue == null)
                {
                    findings.Add(new IntegrationAuditFinding(
                        IntegrationFindingKind.MissingReference,
                        IntegrationScenePath + ":IntegrationRuntimeBinder",
                        field,
                        "Required binder reference is null."));
                }
            }

            // Drone 경로는 둘 중 하나면 충분하다.
            var droneSensor = so.FindProperty("droneSensor");
            var droneAdapter = so.FindProperty("droneContextAdapter");
            var droneOk =
                (droneSensor != null && droneSensor.objectReferenceValue != null)
                || (droneAdapter != null && droneAdapter.objectReferenceValue != null);
            if (!droneOk)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.MissingReference,
                    IntegrationScenePath + ":IntegrationRuntimeBinder",
                    "droneSensor|droneContextAdapter",
                    "Required drone context provider reference is null."));
            }
        }

        private static void CountAndReportDuplicates<T>(
            Scene scene,
            List<IntegrationAuditFinding> findings,
            string label) where T : Component
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                count += root.GetComponentsInChildren<T>(true).Length;
            }

            if (count > 1)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.DuplicateSystem,
                    IntegrationScenePath,
                    label,
                    "Duplicate " + label + " count=" + count + "."));
            }
        }

        private static void CountAndReportDuplicatesByName(
            Scene scene,
            List<IntegrationAuditFinding> findings,
            string objectName)
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                    {
                        count++;
                    }
                }
            }

            // Root 전용 이름은 루트만 집계해 false positive를 줄인다.
            if (objectName == "ApplicationRoot"
                || objectName == "GameplayRoot"
                || objectName == "HUDCanvas")
            {
                count = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == objectName)
                    {
                        count++;
                    }
                }
            }

            if (count > 1)
            {
                findings.Add(new IntegrationAuditFinding(
                    IntegrationFindingKind.DuplicateSystem,
                    IntegrationScenePath,
                    objectName,
                    "Duplicate root/object '" + objectName + "' count=" + count + "."));
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t.gameObject;
                    }
                }
            }

            return null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var stack = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }
    }
}
