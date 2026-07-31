using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>MVP2 D 카메라 경계를 Surface Base와 Mine Scene에 연결합니다.</summary>
    public static class PhaseDCameraSetup
    {
        public const string MineScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string SurfaceScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";

        [MenuItem("SubTerra/MVP2/Build Phase D Camera Bounds")]
        public static string BuildAll()
        {
            ConfigureMineScene();
            ConfigureSurfaceScene();
            AssetDatabase.SaveAssets();
            return "Phase D camera bounds wired for Mine and Surface Base.";
        }

        private static void ConfigureMineScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MineScenePath, OpenSceneMode.Single);
            Camera camera = FindInScene<Camera>(scene);
            Tilemap tilemap = FindInScene<Tilemap>(scene, "ForegroundTilemap");
            PlayerCameraFollow follow = camera != null
                ? camera.GetComponent<PlayerCameraFollow>()
                : null;
            if (camera == null || tilemap == null || follow == null)
            {
                throw new System.InvalidOperationException(
                    "Mine Scene의 Camera, Follow 또는 ForegroundTilemap 참조가 없습니다.");
            }

            BoundsInt cells = tilemap.cellBounds;
            Vector3 min = tilemap.CellToWorld(cells.min);
            Vector3 max = tilemap.CellToWorld(cells.max);
            CameraBounds2D provider = camera.GetComponent<CameraBounds2D>()
                ?? camera.gameObject.AddComponent<CameraBounds2D>();
            provider.SetWorldBounds((min + max) * 0.5f, max - min);

            var serializedFollow = new SerializedObject(follow);
            serializedFollow.FindProperty("boundsProvider").objectReferenceValue = provider;
            serializedFollow.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(provider);
            EditorUtility.SetDirty(follow);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureSurfaceScene()
        {
            Scene scene = EditorSceneManager.OpenScene(SurfaceScenePath, OpenSceneMode.Single);
            Camera camera = FindInScene<Camera>(scene);
            if (camera == null)
            {
                throw new System.InvalidOperationException("Surface Base Camera가 없습니다.");
            }

            CameraBounds2D provider = camera.GetComponent<CameraBounds2D>()
                ?? camera.gameObject.AddComponent<CameraBounds2D>();
            provider.SetWorldBounds(Vector2.zero, new Vector2(36f, 20f));
            EditorUtility.SetDirty(provider);
            EditorSceneManager.SaveScene(scene);
        }

        private static T FindInScene<T>(Scene scene, string objectName = null)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (objectName == null || component.gameObject.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }
    }
}
