using System.IO;
using System.Text;
using SubTerra.App.UI.Hazards;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase N 목표/안내 UI를 Integration Scene HUD 아래에 생성·연결한다.</summary>
    public static class PhaseNTutorialUiBuilder
    {
        private const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Phase N Tutorial UI")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "phase-n-tutorial-ui-build.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Phase N Tutorial UI build");

            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: could not open " + IntegrationScenePath;
            }

            var canvas = GameObject.Find("HUDCanvas");
            if (canvas == null)
            {
                var existingCanvas = Object.FindFirstObjectByType<Canvas>();
                canvas = existingCanvas != null ? existingCanvas.gameObject : null;
            }

            if (canvas == null)
            {
                var canvasGo = new GameObject(
                    "HUDCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                var c = canvasGo.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;
                canvas = canvasGo;
                sb.AppendLine("Created HUDCanvas");
            }

            var root = canvas.transform.Find("DemoObjectiveRoot");
            GameObject rootGo;
            if (root == null)
            {
                rootGo = new GameObject("DemoObjectiveRoot", typeof(RectTransform));
                rootGo.transform.SetParent(canvas.transform, false);
                Stretch(rootGo.GetComponent<RectTransform>());
                sb.AppendLine("Created DemoObjectiveRoot");
            }
            else
            {
                rootGo = root.gameObject;
                sb.AppendLine("Reusing DemoObjectiveRoot");
            }

            var view = rootGo.GetComponent<DemoObjectiveView>();
            if (view == null)
            {
                view = rootGo.AddComponent<DemoObjectiveView>();
            }

            var title = EnsureTmp(
                rootGo.transform,
                "ObjectiveTitle",
                new Vector2(0.02f, 0.92f),
                new Vector2(0.55f, 0.98f),
                22,
                "목표");
            var body = EnsureTmp(
                rootGo.transform,
                "ObjectiveBody",
                new Vector2(0.02f, 0.84f),
                new Vector2(0.55f, 0.92f),
                16,
                "설명");
            var next = EnsureTmp(
                rootGo.transform,
                "NextAction",
                new Vector2(0.02f, 0.78f),
                new Vector2(0.55f, 0.84f),
                15,
                "다음 행동");
            var count = EnsureTmp(
                rootGo.transform,
                "ProgressCount",
                new Vector2(0.56f, 0.92f),
                new Vector2(0.75f, 0.98f),
                16,
                "0 / 13");

            var guidance = rootGo.transform.Find("GuidancePanel");
            GameObject guidanceGo;
            if (guidance == null)
            {
                guidanceGo = new GameObject(
                    "GuidancePanel",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(CanvasGroup));
                guidanceGo.transform.SetParent(rootGo.transform, false);
                SetAnchor(
                    guidanceGo.GetComponent<RectTransform>(),
                    new Vector2(0.25f, 0.35f),
                    new Vector2(0.75f, 0.65f));
                guidanceGo.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.92f);
                sb.AppendLine("Created GuidancePanel");
            }
            else
            {
                guidanceGo = guidance.gameObject;
            }

            var gTitle = EnsureTmp(
                guidanceGo.transform,
                "GuidanceTitle",
                new Vector2(0.05f, 0.7f),
                new Vector2(0.95f, 0.95f),
                20,
                "안내");
            var gBody = EnsureTmp(
                guidanceGo.transform,
                "GuidanceBody",
                new Vector2(0.05f, 0.25f),
                new Vector2(0.95f, 0.7f),
                16,
                "본문");
            var dismissBtn = EnsureButton(
                guidanceGo.transform,
                "DismissButton",
                new Vector2(0.3f, 0.05f),
                new Vector2(0.7f, 0.22f),
                "닫기");

            var complete = rootGo.transform.Find("DemoCompletePanel");
            GameObject completeGo;
            if (complete == null)
            {
                completeGo = new GameObject(
                    "DemoCompletePanel",
                    typeof(RectTransform),
                    typeof(Image));
                completeGo.transform.SetParent(rootGo.transform, false);
                SetAnchor(
                    completeGo.GetComponent<RectTransform>(),
                    new Vector2(0.2f, 0.3f),
                    new Vector2(0.8f, 0.7f));
                completeGo.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.08f, 0.95f);
                completeGo.SetActive(false);
                sb.AppendLine("Created DemoCompletePanel");
            }
            else
            {
                completeGo = complete.gameObject;
            }

            var completeText = EnsureTmp(
                completeGo.transform,
                "CompleteText",
                new Vector2(0.05f, 0.2f),
                new Vector2(0.95f, 0.8f),
                18,
                "데모 종료");

            var so = new SerializedObject(view);
            Assign(so, "objectiveTitleText", title);
            Assign(so, "objectiveBodyText", body);
            Assign(so, "nextActionText", next);
            Assign(so, "progressCountText", count);
            Assign(so, "guidanceRoot", guidanceGo);
            Assign(so, "guidanceTitleText", gTitle);
            Assign(so, "guidanceBodyText", gBody);
            Assign(so, "demoCompleteRoot", completeGo);
            Assign(so, "demoCompleteText", completeText);
            Assign(so, "tutorialCanvas", canvas.GetComponent<Canvas>());
            Assign(so, "guidanceCanvasGroup", guidanceGo.GetComponent<CanvasGroup>());
            so.ApplyModifiedPropertiesWithoutUndo();

            var binder = rootGo.GetComponent<TutorialDirectorBinder>();
            if (binder == null)
            {
                binder = rootGo.AddComponent<TutorialDirectorBinder>();
            }

            var binderSo = new SerializedObject(binder);
            Assign(binderSo, "objectiveView", view);
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            var btn = dismissBtn.GetComponent<Button>();
            if (btn != null)
            {
                while (btn.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                }

                UnityEventTools.AddPersistentListener(btn.onClick, view.OnDismissClicked);
            }

            if (rootGo.GetComponent<DemoObjectiveDebugTools>() == null)
            {
                var debug = rootGo.AddComponent<DemoObjectiveDebugTools>();
                var debugSo = new SerializedObject(debug);
                Assign(debugSo, "tutorialBinder", binder);
                debugSo.ApplyModifiedPropertiesWithoutUndo();
                sb.AppendLine("Added Development debug tools");
            }

            // IntegrationRuntimeBinder is in GameplayIntegration asmdef — find by name/type string.
            MonoBehaviour integration = null;
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && behaviours[i].GetType().Name == "IntegrationRuntimeBinder")
                {
                    integration = behaviours[i];
                    break;
                }
            }

            if (integration != null)
            {
                var irb = new SerializedObject(integration);
                var prop = irb.FindProperty("tutorialDirector");
                if (prop != null)
                {
                    prop.objectReferenceValue = binder;
                    irb.ApplyModifiedPropertiesWithoutUndo();
                    sb.AppendLine("Wired IntegrationRuntimeBinder.tutorialDirector");
                }
                else
                {
                    sb.AppendLine("tutorialDirector property missing on IntegrationRuntimeBinder");
                }
            }
            else
            {
                sb.AppendLine(
                    "IntegrationRuntimeBinder not found (runtime FindFirstObjectByType fallback)");
            }

            var hazardView = Object.FindFirstObjectByType<HazardHudView>();
            if (hazardView != null)
            {
                var hso = new SerializedObject(hazardView);
                var canvasProp = hso.FindProperty("hazardCanvas");
                if (canvasProp != null && canvasProp.objectReferenceValue == null)
                {
                    canvasProp.objectReferenceValue = hazardView.GetComponentInParent<Canvas>();
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                sb.AppendLine("HazardHudView present");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            sb.AppendLine("Scene saved: " + IntegrationScenePath);
            sb.AppendLine("view=" + (view != null) + " binder=" + (binder != null));
            return sb.ToString();
        }

        private static void Assign(SerializedObject so, string name, Object value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static TMP_Text EnsureTmp(
            Transform parent,
            string name,
            Vector2 min,
            Vector2 max,
            int size,
            string text)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = size;
                tmp.text = text;
                tmp.color = Color.white;
                SetAnchor(go.GetComponent<RectTransform>(), min, max);
                return tmp;
            }

            SetAnchor(existing.GetComponent<RectTransform>(), min, max);
            var t = existing.GetComponent<TextMeshProUGUI>();
            if (t == null)
            {
                t = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }

            return t;
        }

        private static GameObject EnsureButton(
            Transform parent,
            string name,
            Vector2 min,
            Vector2 max,
            string label)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetAnchor(go.GetComponent<RectTransform>(), min, max);
            go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.75f, 1f);
            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
