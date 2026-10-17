using System;
using System.Linq;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>데스크탑 시각 검수의 명시된 HUD·미션·시설창·레일만 갱신한다.</summary>
    public static class DesktopVisualPolishBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string HudPath = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        private const string ElevatorPath = "Assets/_Project/Prefabs/Gameplay/Traversal/StartElevator.prefab";

        [MenuItem("SubTerra/UI/Apply Desktop Visual Review (Integration HUD and Elevator)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.isLoaded && existing.isDirty) throw new InvalidOperationException("Integration scene has unsaved changes.");
            PromptB110BuildingMenuBuilder.Build();
            PromptB107QuestUiBuilder.Build();
            ModifyPrefab(HudPath, root => LayoutHud(root.GetComponent<BasicHudView>()));
            ModifyPrefab(ElevatorPath, root => root.GetComponent<ElevatorDoorVisual>().RefreshFixedShaftGeometry());
            var previous = SceneManager.GetActiveScene();
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                var roots = scene.GetRootGameObjects();
                // 과거 Play 상태로 직렬화된 스캔은 소유 View가 없어 수명 종료가 호출되지 않는다.
                // 런타임이 새 효과를 생성하므로 명시된 생성 루트만 제거한다.
                int removedScans = 0;
                foreach (var root in roots.Where(r => r.name == "DroneScanPulseVisuals" && r.GetComponent<MonoBehaviour>() == null))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    removedScans++;
                }
                roots = scene.GetRootGameObjects();
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<BasicHudView>(true)).Single();
                LayoutHud(hud);
                var building = roots.SelectMany(r => r.GetComponentsInChildren<BuildingMenuView>(true)).Single();
                Place((RectTransform)building.transform, 16f, 360f, PromptB110BuildingMenuBuilder.PanelWidth, 560f);
                var chrome = roots.SelectMany(r => r.GetComponentsInChildren<HudPanelChromeController>(true)).Single();
                SetBool(chrome, "buildingMenuOpen", false);
                var menu = roots.SelectMany(r => r.GetComponentsInChildren<GameplaySideMenuController>(true)).Single();
                menu.transform.Find("OpenMenuRoot").gameObject.SetActive(false);
                menu.transform.Find("ClosedMenuRoot").gameObject.SetActive(true);
                var cracks = roots.SelectMany(r => r.GetComponentsInChildren<StructuralCrackOverlay>(true)).Single();
                var so = new SerializedObject(cracks);
                so.FindProperty("cautionColor").colorValue = new Color(0.8f, 0.72f, 0.38f, 0.3f);
                so.FindProperty("dangerColor").colorValue = new Color(0.85f, 0.48f, 0.22f, 0.48f);
                so.FindProperty("imminentColor").colorValue = new Color(0.9f, 0.24f, 0.14f, 0.66f);
                so.FindProperty("telegraphFlashSeconds").floatValue = 0.6f;
                so.FindProperty("unsupportedShakeDistance").floatValue = 0.025f;
                so.FindProperty("unsupportedShakeInterval").floatValue = 0.12f;
                so.FindProperty("impactShakeDistance").floatValue = 0.06f;
                so.FindProperty("impactShakeInterval").floatValue = 0.08f;
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var lift in roots.SelectMany(r => r.GetComponentsInChildren<ElevatorDoorVisual>(true)))
                    lift.RefreshFixedShaftGeometry();
                // 프리팹 인스턴스의 현재 레이아웃을 명시적으로 보존한다.
                foreach (var root in new[] { hud.gameObject, building.gameObject, menu.gameObject, cracks.gameObject })
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                        if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save Integration.");
                Debug.Log("[SubTerra] Desktop visual review applied to explicit HUD, mission, building and elevator targets.");
                Debug.Log("[SubTerra] Removed serialized transient scan roots: " + removedScans);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static void ModifyPrefab(string path, Action<GameObject> modify)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { modify(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetBool(UnityEngine.Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LayoutHud(BasicHudView hud)
        {
            var root = (RectTransform)hud.transform;
            Place(root, 12f, 12f, 380f, 208f);
            Place(Required(root, "HudFrame"), 0f, 0f, 380f, 208f);
            foreach (var gauge in new[] { "HealthGauge", "EnergyGauge" })
            {
                var rect = Required(root, gauge);
                Place(rect, 106f, gauge == "HealthGauge" ? 16f : 52f, 258f, 32f);
                foreach (var child in new[] { "Empty", "Fill", "Frame" })
                    Place(Required(rect, child), 0f, 0f, 258f, 32f);
            }
            Text(hud.HealthText, 110f, 16f, 246f, 30f, 22f);
            Text(hud.EnergyText, 110f, 52f, 246f, 30f, 22f);
            Text(Required(root, "HealthLabel").GetComponent<TMP_Text>(), 52f, 17f, 50f, 28f, 20f);
            Text(Required(root, "EnergyLabel").GetComponent<TMP_Text>(), 52f, 53f, 50f, 28f, 20f);
            Text(hud.DepthText, 52f, 96f, 132f, 26f, 19f);
            Text(hud.GoldText, 52f, 126f, 132f, 26f, 19f);
            Text(hud.CargoText, 232f, 96f, 134f, 26f, 19f);
            Text(hud.UnsettledValueText, 232f, 126f, 134f, 26f, 18f);
            float[] xs = { 20f, 20f, 20f, 20f, 202f, 202f };
            float[] ys = { 18f, 54f, 97f, 127f, 97f, 127f };
            for (int i = 0; i < 6; i++) Place(Required(root, "HudIcon" + i), xs[i], ys[i], 24f, 24f);
            Place(Required(root, "TopSeparator"), 16f, 89f, 348f, 1f);
            Place(Required(root, "BottomSeparator"), 16f, 158f, 348f, 1f);
            Place(Required(root, "ColumnSeparator"), 190f, 96f, 1f, 56f);
            var row = Required(root, "StructuralStatusRow");
            Place(row, 16f, 164f, 348f, 28f);
            Place(Required(row, "Icon"), 4f, 2f, 24f, 24f);
            Text(Required(row, "Label").GetComponent<TMP_Text>(), 36f, 0f, 306f, 28f, 20f);
        }

        private static RectTransform Required(Transform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            return child != null ? child : throw new InvalidOperationException("Missing layout target: " + name);
        }
        private static void Text(TMP_Text text, float x, float y, float w, float h, float size)
        {
            Place(text.rectTransform, x, y, w, h);
            text.fontSize = size;
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}
