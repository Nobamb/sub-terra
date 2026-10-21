using SubTerra.App.Core.Data;
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
    /// <summary>
    /// 프롬프트 B-153: 화물 버리기 창. Mine_Demo_Integration의 QuestRewardDumpPanel 아래만 고친다
    /// (CargoDiscardPopupView 부착, 줄마다 5개·10개·필요 버튼 추가, 버튼 문구, onClick 연결).
    /// 기존 오브젝트 이름과 onClick은 유지한다. 장식은 런타임에 CargoDiscardPopupView가 만든다.
    /// 화물 부족·버리기 창 문구 글자를 폰트 에셋에 미리 넣는다.
    /// </summary>
    public static class PromptB153CargoDiscardPopupBuilder
    {
        public const string IntegrationScenePath = PromptB95QuestRewardUiBuilder.IntegrationScenePath;
        public const string PanelName = "QuestRewardDumpPanel";

        [MenuItem("SubTerra/UI/Build Prompt-B 153 Cargo Discard Popup")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<CargoFullPopupSkin>(PromptB152CargoFullPopupBuilder.SkinAssetPath) == null)
            {
                PromptB152CargoFullPopupBuilder.BuildSkin();
            }

            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            var closeAfterBuild = !scene.IsValid() || !scene.isLoaded;
            if (closeAfterBuild)
            {
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var view = root != null ? root.GetComponent<DemoObjectiveView>() : null;
                if (view == null)
                {
                    throw new System.InvalidOperationException("Mine_Demo_Integration: DemoObjectiveRoot/DemoObjectiveView가 없습니다.");
                }

                var panel = root.transform.Find(PanelName);
                if (panel == null)
                {
                    throw new System.InvalidOperationException("DemoObjectiveRoot: " + PanelName + "이 없습니다.");
                }

                var title = Required<TMP_Text>(panel, CargoDiscardPopupView.TitleName);
                title.text = CargoDiscardText.Title;
                EditorUtility.SetDirty(title);
                var fontReport = KoreanFontAssetUtility.EnsureCharactersSaved(title.font, CargoDiscardText.FontCharacters);

                EnsureRow(panel, DataIds.Minerals.Copper, title.font,
                    view.OnDumpCopperOneClicked, view.OnDumpCopperFiveClicked, view.OnDumpCopperTenClicked,
                    view.OnDumpCopperNeededClicked, view.OnDumpCopperAllClicked);
                EnsureRow(panel, DataIds.Minerals.Iron, title.font,
                    view.OnDumpIronOneClicked, view.OnDumpIronFiveClicked, view.OnDumpIronTenClicked,
                    view.OnDumpIronNeededClicked, view.OnDumpIronAllClicked);
                EnsureRow(panel, DataIds.Minerals.Lithium, title.font,
                    view.OnDumpLithiumOneClicked, view.OnDumpLithiumFiveClicked, view.OnDumpLithiumTenClicked,
                    view.OnDumpLithiumNeededClicked, view.OnDumpLithiumAllClicked);

                var back = Required<Button>(panel, CargoDiscardPopupView.BackName);
                SetLabel(back, CargoDiscardText.BackTitle);
                EnsureWired(back, view.OnDumpClosedClicked);

                ((RectTransform)panel).sizeDelta = CargoFullPopupView.DefaultPanelSize;
                var popup = panel.GetComponent<CargoDiscardPopupView>();
                if (popup == null)
                {
                    popup = panel.gameObject.AddComponent<CargoDiscardPopupView>();
                }

                var popupSerialized = new SerializedObject(popup);
                popupSerialized.FindProperty("panelSize").vector2Value = CargoFullPopupView.DefaultPanelSize;
                popupSerialized.ApplyModifiedPropertiesWithoutUndo();

                var viewSerialized = new SerializedObject(view);
                var property = viewSerialized.FindProperty("dumpPopup");
                if (property == null)
                {
                    throw new System.InvalidOperationException("DemoObjectiveView 직렬화 필드 누락: dumpPopup");
                }

                property.objectReferenceValue = popup;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                panel.gameObject.SetActive(false);
                EditorUtility.SetDirty(popup);
                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new System.InvalidOperationException("Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 153 cargo discard popup built: " + IntegrationScenePath + " (font " + fontReport + ")";
            }
            finally
            {
                if (closeAfterBuild && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void EnsureRow(
            Transform panel,
            string mineralId,
            TMP_FontAsset font,
            UnityEngine.Events.UnityAction one,
            UnityEngine.Events.UnityAction five,
            UnityEngine.Events.UnityAction ten,
            UnityEngine.Events.UnityAction needed,
            UnityEngine.Events.UnityAction all)
        {
            var rowName = CargoDiscardPopupView.RowName(mineralId);
            var row = panel.Find(rowName);
            if (row == null)
            {
                throw new System.InvalidOperationException(PanelName + "/" + rowName + "가 없습니다.");
            }

            var actions = new[] { one, five, ten, needed, all };
            for (var k = 0; k < CargoDiscardText.Kinds.Length; k++)
            {
                var kind = CargoDiscardText.Kinds[k];
                var button = EnsureButton(row, CargoDiscardPopupView.ButtonName(kind), font);
                SetLabel(button, CargoDiscardText.ButtonLabel(kind));
                EnsureWired(button, actions[k]);
            }
        }

        private static Button EnsureButton(Transform row, string name, TMP_FontAsset font)
        {
            var existing = row.Find(name);
            if (existing != null)
            {
                var found = existing.GetComponent<Button>();
                if (found == null)
                {
                    throw new System.InvalidOperationException(row.name + "/" + name + ": Button이 없습니다.");
                }

                return found;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = row.gameObject.layer;
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(96f, 38f);
            go.GetComponent<Image>().color = Color.clear;

            var label = new GameObject("Label", typeof(RectTransform));
            label.layer = row.gameObject.layer;
            label.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = label.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static void SetLabel(Button button, string text)
        {
            var label = button.transform.Find("Label");
            var tmp = label != null ? label.GetComponent<TMP_Text>() : null;
            if (tmp == null)
            {
                throw new System.InvalidOperationException(button.name + ": Label TMP가 없습니다.");
            }

            tmp.text = text;
            EditorUtility.SetDirty(tmp);
        }

        private static void EnsureWired(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button.onClick.GetPersistentEventCount() == 1
                && button.onClick.GetPersistentMethodName(0) == action.Method.Name
                && button.onClick.GetPersistentTarget(0) == (Object)action.Target)
            {
                return;
            }

            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static T Required<T>(Transform parent, string name) where T : Component
        {
            var child = parent.Find(name);
            var component = child != null ? child.GetComponent<T>() : null;
            if (component == null)
            {
                throw new System.InvalidOperationException(parent.name + "/" + name + ": " + typeof(T).Name + "가 없습니다.");
            }

            return component;
        }

        private static GameObject Find(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == name)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }
    }
}
