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
    /// 프롬프트 B-152: 화물 공간 부족 팝업. 기존 그림만 가리키는 CargoFullPopupSkin을 만들고,
    /// Mine_Demo_Integration의 QuestRewardCapacityPanel 아래만 고친다(CargoFullPopupView 부착, 버튼 문구).
    /// 패널·버튼 이름과 onClick 연결은 유지한다. 장식은 런타임에 CargoFullPopupView가 만든다.
    /// </summary>
    public static class PromptB152CargoFullPopupBuilder
    {
        public const string IntegrationScenePath = PromptB95QuestRewardUiBuilder.IntegrationScenePath;
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/CargoFullPopupSkin.asset";
        public const string WarningIconPath = "Assets/_Project/Art/UI/Gameplay/HUD/structural-caution.png";
        public const string CargoIconPath = "Assets/_Project/Art/UI/Upgrade/Icons/upgrade-icon-box.png";
        public const string TrashIconPath = "Assets/_Project/Art/UI/MainMenu/B150/icon-trash-white.png";
        public const string CopperIconPath = "Assets/_Project/Art/Icons/icon_copper.png";
        public const string IronIconPath = "Assets/_Project/Art/Icons/icon_iron.png";
        public const string LithiumIconPath = "Assets/_Project/Art/Icons/icon_lithium.png";
        public const string GoldIconPath = "Assets/_Project/Art/UI/Gameplay/Quest/quest-icon-gold.png";

        [MenuItem("SubTerra/UI/Build Prompt-B 152 Cargo Full Popup")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            BuildSkin();
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

                var panel = root.transform.Find("QuestRewardCapacityPanel");
                if (panel == null)
                {
                    throw new System.InvalidOperationException("DemoObjectiveRoot: QuestRewardCapacityPanel이 없습니다.");
                }

                var title = Required<TMP_Text>(panel, "CapacityTitle");
                var body = Required<TMP_Text>(panel, "CapacityBody");
                var dump = Required<Button>(panel, "CapacityDumpButton");
                var forfeit = Required<Button>(panel, "CapacityForfeitButton");

                title.text = CargoFullPopupText.Title;
                body.text = CargoFullPopupText.Guide;
                SetLabel(dump, CargoFullPopupText.DumpTitle);
                SetLabel(forfeit, CargoFullPopupText.ForfeitTitle);
                EnsureWired(dump, view.OnCapacityDumpClicked, nameof(DemoObjectiveView.OnCapacityDumpClicked));
                EnsureWired(forfeit, view.OnCapacityForfeitClicked, nameof(DemoObjectiveView.OnCapacityForfeitClicked));

                var rect = (RectTransform)panel;
                rect.sizeDelta = CargoFullPopupView.DefaultPanelSize;

                var popup = panel.GetComponent<CargoFullPopupView>();
                if (popup == null)
                {
                    popup = panel.gameObject.AddComponent<CargoFullPopupView>();
                }

                var popupSerialized = new SerializedObject(popup);
                popupSerialized.FindProperty("titleText").objectReferenceValue = title;
                popupSerialized.FindProperty("guideText").objectReferenceValue = body;
                popupSerialized.FindProperty("dumpButton").objectReferenceValue = dump;
                popupSerialized.FindProperty("forfeitButton").objectReferenceValue = forfeit;
                popupSerialized.FindProperty("panelSize").vector2Value = CargoFullPopupView.DefaultPanelSize;
                popupSerialized.ApplyModifiedPropertiesWithoutUndo();

                var viewSerialized = new SerializedObject(view);
                var property = viewSerialized.FindProperty("capacityPopup");
                if (property == null)
                {
                    throw new System.InvalidOperationException("DemoObjectiveView 직렬화 필드 누락: capacityPopup");
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

                return "Prompt-B 152 cargo full popup built: " + IntegrationScenePath;
            }
            finally
            {
                if (closeAfterBuild && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        public static CargoFullPopupSkin BuildSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<CargoFullPopupSkin>(SkinAssetPath);
            if (skin == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/UI"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");
                }

                skin = ScriptableObject.CreateInstance<CargoFullPopupSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            skin.warningIcon = Load(WarningIconPath);
            skin.cargoIcon = Load(CargoIconPath);
            skin.trashIcon = Load(TrashIconPath);
            skin.copperIcon = Load(CopperIconPath);
            skin.ironIcon = Load(IronIconPath);
            skin.lithiumIcon = Load(LithiumIconPath);
            skin.goldIcon = Load(GoldIconPath);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
            return skin;
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

        private static void EnsureWired(Button button, UnityEngine.Events.UnityAction action, string methodName)
        {
            if (button.onClick.GetPersistentEventCount() == 1
                && button.onClick.GetPersistentMethodName(0) == methodName)
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

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[SubTerra] Cargo full popup sprite missing: " + path);
            }

            return sprite;
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
