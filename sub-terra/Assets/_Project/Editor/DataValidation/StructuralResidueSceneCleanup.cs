using System;
using System.Linq;
using SubTerra.Gameplay.Structural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    public static class StructuralResidueSceneCleanup
    {
        private const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/Validation/Clean Saved Structural Residue")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("Integration scene has unsaved changes.");
            bool closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var overlays = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<StructuralCrackOverlay>(true)).ToArray();
                if (overlays.Length != 1 || overlays[0].OverlayTilemap == null)
                    throw new InvalidOperationException("Expected one bound structural overlay.");
                var overlay = overlays[0];
                int beforeChildren = overlay.transform.childCount;
                int beforeTiles = overlay.OverlayTilemap.GetUsedTilesCount();
                overlay.ClearAll();
                int removedChildren = beforeChildren - overlay.transform.childCount;
                if (removedChildren == 0 && beforeTiles == 0) return;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
                Debug.Log($"[Issue245] Removed visual objects={removedChildren}, overlay tile types={beforeTiles}.");
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
