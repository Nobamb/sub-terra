#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural.Editor
{
    /// <summary>MVP2 G의 데이터·Overlay·경고음을 Test/Integration Scene에 연결한다.</summary>
    public static class PhaseGStructuralSceneSetup
    {
        private const string SettingsPath =
            "Assets/_Project/Data/Structural/StructuralRiskSettings.asset";
        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Structural_Test.unity",
            "Assets/_Project/Scenes/Test/Gameplay/Gameplay_DemoWorld_Test.unity",
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity"
        };

        [MenuItem("Tools/SubTerra/MVP2/Apply Phase G Structural Hazards")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("구조 위험 Scene 연결은 Play Mode 밖에서 실행해야 합니다.");

            EnsureSettings();
            AssetDatabase.SaveAssets();
            foreach (string path in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ApplyToOpenScene(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MVP2 Phase G structural hazards were applied to test and Integration scenes.");
        }

        public static void ApplyToOpenScene(Scene scene)
        {
            StructuralIntegritySystem structural = FindInScene<StructuralIntegritySystem>(scene);
            Tilemap foreground = FindInScene<Tilemap>(scene, "ForegroundTilemap");
            if (structural == null || foreground == null)
                throw new InvalidOperationException($"{scene.path}: 구조 시스템 또는 Foreground Tilemap이 없습니다.");

            StructuralRiskSettings settings = EnsureSettings();
            Tilemap overlay = EnsureOverlayTilemap(foreground);
            StructuralCrackOverlay crackOverlay =
                structural.GetComponent<StructuralCrackOverlay>();
            if (crackOverlay == null)
                crackOverlay = structural.gameObject.AddComponent<StructuralCrackOverlay>();
            AudioSource audioSource = structural.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = structural.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            StructuralRiskFeedback feedback =
                structural.GetComponent<StructuralRiskFeedback>();
            if (feedback == null)
                feedback = structural.gameObject.AddComponent<StructuralRiskFeedback>();

            var structuralSerialized = new SerializedObject(structural);
            SetObject(structuralSerialized, "foregroundTilemap", foreground);
            SetObject(structuralSerialized, "crackOverlay", crackOverlay);
            SetObject(structuralSerialized, "riskSettings", settings);
            SetProtectedTiles(structuralSerialized, FindBoundaryTile(foreground));
            structuralSerialized.ApplyModifiedPropertiesWithoutUndo();

            var overlaySerialized = new SerializedObject(crackOverlay);
            SetObject(overlaySerialized, "overlayTilemap", overlay);
            overlaySerialized.ApplyModifiedPropertiesWithoutUndo();

            var feedbackSerialized = new SerializedObject(feedback);
            SetObject(feedbackSerialized, "structuralSystem", structural);
            SetObject(feedbackSerialized, "audioSource", audioSource);
            feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("OutpostCore", StringComparison.OrdinalIgnoreCase))
                    structural.RegisterProtectedCell(foreground.WorldToCell(child.position));
            }

            EditorUtility.SetDirty(structural);
            EditorUtility.SetDirty(crackOverlay);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(overlay);
        }

        private static StructuralRiskSettings EnsureSettings()
        {
            EnsureFolder("Assets/_Project/Data/Structural");
            StructuralRiskSettings settings =
                AssetDatabase.LoadAssetAtPath<StructuralRiskSettings>(SettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static Tilemap EnsureOverlayTilemap(Tilemap foreground)
        {
            Transform parent = foreground.transform.parent;
            Transform existing = parent.Find("StructuralCrackOverlay");
            GameObject overlayObject;
            if (existing != null)
            {
                overlayObject = existing.gameObject;
            }
            else
            {
                overlayObject = new GameObject("StructuralCrackOverlay");
                overlayObject.transform.SetParent(parent, false);
            }

            Tilemap overlay = overlayObject.GetComponent<Tilemap>();
            if (overlay == null) overlay = overlayObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = overlayObject.GetComponent<TilemapRenderer>();
            if (renderer == null) renderer = overlayObject.AddComponent<TilemapRenderer>();
            if (renderer == null)
                throw new InvalidOperationException("StructuralCrackOverlay TilemapRenderer 생성에 실패했습니다.");
            TilemapRenderer foregroundRenderer = foreground.GetComponent<TilemapRenderer>();
            renderer.sortingLayerID = foregroundRenderer != null
                ? foregroundRenderer.sortingLayerID
                : 0;
            renderer.sortingOrder = foregroundRenderer != null
                ? foregroundRenderer.sortingOrder + 10
                : 10;
            return overlay;
        }

        private static TileBase FindBoundaryTile(Tilemap foreground)
        {
            foreach (Vector3Int cell in foreground.cellBounds.allPositionsWithin)
            {
                TileBase tile = foreground.GetTile(cell);
                if (tile != null && tile.name.Contains("Boundary", StringComparison.OrdinalIgnoreCase))
                    return tile;
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene, string objectName = null)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (string.IsNullOrEmpty(objectName) || component.gameObject.name == objectName)
                    return component;
            }

            return null;
        }

        private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Serialized property is missing: {name}");
            property.objectReferenceValue = value;
        }

        private static void SetProtectedTiles(SerializedObject serialized, TileBase boundary)
        {
            SerializedProperty property = serialized.FindProperty("protectedTiles");
            property.arraySize = boundary != null ? 1 : 0;
            if (boundary != null) property.GetArrayElementAtIndex(0).objectReferenceValue = boundary;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
#endif
