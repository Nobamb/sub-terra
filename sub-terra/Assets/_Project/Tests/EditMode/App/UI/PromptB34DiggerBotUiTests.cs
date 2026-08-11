using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 34: 드론 말풍선 + digger-bot 창 가시성·토글 정적 검증.</summary>
    public sealed class PromptB34DiggerBotUiTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public void BuildLayout()
        {
            PromptB34DiggerBotUiBuilder.Build();
        }

        [Test]
        public void DialoguePrefab_HasLargeDialogueTextAndXClose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab");
            Assert.That(prefab, Is.Not.Null);

            var view = prefab.GetComponent<DroneDialoguePanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(view.CloseButton, Is.Not.Null);

            var label = view.CloseButton.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo("×"));

            var dialogue = prefab.GetComponentsInChildren<TMP_Text>(true)
                .First(t => t.name == "DialogueText");
            Assert.That(
                dialogue.fontSize,
                Is.EqualTo(DroneDialoguePanelView.PanelDialogueFontSize).Within(0.5f));
        }

        [Test]
        public void IntegrationScene_DiggerBotWired_HostAlwaysOn_NoOpenButton()
        {
            var scene = OpenIntegration();
            var canvas = Find<Canvas>(scene, "HUDCanvas");
            Assert.That(canvas, Is.Not.Null);

            var digger = FindTransform(scene, "DroneDialoguePanel");
            Assert.That(digger, Is.Not.Null);
            // 시작 시 digger-bot 창은 닫힘. Tab/드론 클릭으로 연다.
            Assert.That(digger.gameObject.activeSelf, Is.False);

            var host = FindTransform(scene, "DiggerBotPanel");
            Assert.That(host, Is.Not.Null);
            Assert.That(host.gameObject.activeSelf, Is.True);

            var rect = digger as RectTransform ?? digger.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(24f).Within(0.5f));
            Assert.That(rect.sizeDelta.x, Is.EqualTo(760f).Within(0.5f));

            var view = digger.GetComponent<DroneDialoguePanelView>()
                ?? digger.GetComponentInChildren<DroneDialoguePanelView>(true);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            Assert.That(chrome.HasRequiredReferences(), Is.True);

            var so = new SerializedObject(chrome);
            Assert.That(so.FindProperty("diggerBotOpen").boolValue, Is.False);
            Assert.That(
                so.FindProperty("diggerBotWorldTarget").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                so.FindProperty("diggerCloseButton").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                so.FindProperty("diggerOpenButton").objectReferenceValue,
                Is.Null);

            Assert.That(canvas.transform.Find("OpenDiggerBotButton"), Is.Null);

            var socket = Find<DroneDialogueSocket>(scene, "ViewSocket");
            Assert.That(socket, Is.Not.Null);
            Assert.That(socket.HasRequiredReferences(), Is.True);

            var binder = Find<DroneUiBinder>(scene, null);
            Assert.That(binder, Is.Not.Null);
            Assert.That(binder.HasWorldDialogueSocket, Is.True);
            Assert.That(binder.gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void ChromeController_ToggleDiggerBot_ShowsPanelWithoutOpenButton()
        {
            var host = new GameObject("ChromeHost34");
            host.SetActive(false);
            var diggerHost = new GameObject("DiggerHost34");
            var diggerRoot = new GameObject("DiggerRoot34");
            diggerRoot.transform.SetParent(diggerHost.transform);
            try
            {
                var chrome = host.AddComponent<HudPanelChromeController>();
                var so = new SerializedObject(chrome);
                so.FindProperty("diggerBotRoot").objectReferenceValue = diggerRoot;
                so.FindProperty("diggerHostRoot").objectReferenceValue = diggerHost;
                so.FindProperty("diggerBotOpen").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                InvokePrivateAwake(chrome);

                Assert.That(chrome.IsDiggerBotOpen, Is.False);
                Assert.That(diggerRoot.activeSelf, Is.False);
                Assert.That(diggerHost.activeSelf, Is.True);

                chrome.ToggleDiggerBot();
                Assert.That(chrome.IsDiggerBotOpen, Is.True);
                Assert.That(diggerRoot.activeSelf, Is.True);
                Assert.That(diggerHost.activeSelf, Is.True);

                chrome.ToggleDiggerBot();
                Assert.That(chrome.IsDiggerBotOpen, Is.False);
                Assert.That(diggerRoot.activeSelf, Is.False);
                // 호스트(Binder)는 창을 닫아도 활성 유지.
                Assert.That(diggerHost.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(diggerHost);
            }
        }

        private static Scene OpenIntegration()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            return scene;
        }

        private static void InvokePrivateAwake(Component component)
        {
            var awake = component.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null, "테스트 대상의 Awake 초기화가 필요합니다.");
            awake.Invoke(component, null);
        }

        private static T Find<T>(Scene scene, string name)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault(item => name == null || item.name == name);
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}
