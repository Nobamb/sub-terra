using NUnit.Framework;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 38: 인벤토리 버튼 클릭이 I 키와 같이 패널을 열어야 한다.
    /// Scene 영속 onClick + 런타임 AddListener 중복 시 토글이 두 번 실행되어
    /// 창이 열렸다 바로 닫히는 회귀를 고정한다.
    /// </summary>
    public sealed class InventoryOpenButtonWiringTests
    {
        private GameObject host;
        private GameObject panelGo;
        private GameObject panelRootGo;
        private Button openButton;
        private HudPanelChromeController chrome;
        private InventoryPanelView panelView;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("ChromeHost");
            chrome = host.AddComponent<HudPanelChromeController>();

            panelGo = new GameObject("InventoryPanel");
            panelRootGo = new GameObject("PanelRoot");
            panelRootGo.transform.SetParent(panelGo.transform, false);
            panelView = panelGo.AddComponent<InventoryPanelView>();

            // PanelRoot 직렬화 필드를 리플렉션으로 연결.
            var rootField = typeof(InventoryPanelView).GetField(
                "panelRoot",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(rootField, Is.Not.Null);
            rootField.SetValue(panelView, panelRootGo);

            openButton = new GameObject("InventoryOpen").AddComponent<Button>();
            openButton.gameObject.transform.SetParent(host.transform, false);

            // 의도적으로 영속 리스너와 동일한 메서드를 미리 넣어 중복 상황을 재현한다.
            openButton.onClick.AddListener(chrome.ToggleInventoryPanel);

            var invViewField = typeof(HudPanelChromeController).GetField(
                "inventoryPanelView",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            var invRootField = typeof(HudPanelChromeController).GetField(
                "inventoryPanelRoot",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            var invOpenField = typeof(HudPanelChromeController).GetField(
                "inventoryOpenButton",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(invViewField, Is.Not.Null);
            Assert.That(invRootField, Is.Not.Null);
            Assert.That(invOpenField, Is.Not.Null);
            invViewField.SetValue(chrome, panelView);
            invRootField.SetValue(chrome, panelGo);
            invOpenField.SetValue(chrome, openButton);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            if (panelGo != null)
            {
                Object.DestroyImmediate(panelGo);
            }

            if (openButton != null)
            {
                Object.DestroyImmediate(openButton.gameObject);
            }
        }

        [Test]
        public void WireButtons_ReplacesListeners_SingleClickOpensPanel()
        {
            // Awake 경로와 동일하게 WireButtons를 호출한다(private → 리플렉션).
            var wire = typeof(HudPanelChromeController).GetMethod(
                "WireButtons",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(wire, Is.Not.Null);

            // 시작 시 닫힘.
            chrome.CloseInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(panelRootGo.activeSelf, Is.False);

            // Wire 전에 리스너가 이미 1개 있는 상태 → Wire 후 단일 리스너만 남아야 한다.
            openButton.onClick.AddListener(chrome.ToggleInventoryPanel);
            wire.Invoke(chrome, null);

            // 버튼 클릭 1회 = 토글 1회 → 열린다.
            openButton.onClick.Invoke();
            Assert.That(
                chrome.IsInventoryPanelOpen,
                Is.True,
                "인벤토리 버튼 1회 클릭 시 패널이 열려야 한다(이중 토글 금지).");
            Assert.That(panelRootGo.activeSelf, Is.True);

            // 한 번 더 클릭하면 닫힌다.
            openButton.onClick.Invoke();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(panelRootGo.activeSelf, Is.False);
        }

        [Test]
        public void ToggleInventoryPanel_MatchesIKeyBehavior()
        {
            chrome.CloseInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);

            // I 키 경로와 동일한 public API.
            chrome.ToggleInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            Assert.That(panelRootGo.activeSelf, Is.True);

            chrome.ToggleInventoryPanel();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
        }
    }
}
