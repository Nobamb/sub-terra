using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Inventory;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB192SearchSortClickSfxTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<SfxId> requests = new List<SfxId>();
        private SfxPlayer player;
        private float now;
        private bool muted;

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            muted = false;
            player = SfxPlayer.CreateForTests(AssetDatabase.LoadAssetAtPath<SfxLibrary>(
                "Assets/_Project/Resources/SfxLibrary.asset"), () => now, () => muted);
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            for (var i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            requests.Clear();
            if (player != null) player.ReleaseForTests();
        }

        private void Record(SfxId id, float _) => requests.Add(id);

        private GameObject Create(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            created.Add(go);
            return go;
        }

        private GameObject Spawn(bool building)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/" + (building ? "BuildingMenu" : "InventoryPanel") + ".prefab"));
            created.Add(root);
            var view = building ? (Component)root.GetComponent<BuildingMenuView>() : root.GetComponent<InventoryPanelView>();
            var method = view.GetType().GetMethod(building ? "WireControls" : "WireManifestControls", Private);
            method.Invoke(view, null);
            method.Invoke(view, null);
            return root;
        }

        private static T Control<T>(GameObject root) where T : Component
        {
            var control = root.GetComponentInChildren<T>(true);
            for (var parent = control.transform; parent != null; parent = parent.parent)
                parent.gameObject.SetActive(true);
            return control;
        }

        private void Press(GameObject go, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            now += SfxPlayer.MinRepeatInterval + 0.01f;
            go.GetComponent<UiSearchSortClickSfx>().OnPointerDown(
                new PointerEventData(EventSystem.current) { button = button });
        }

        private GameObject Expand(TMP_Dropdown dropdown)
        {
            // EditMode에서 TMP의 지연 파괴 코루틴 대신 펼침 상태만 만든다.
            var list = Create("Dropdown List");
            typeof(TMP_Dropdown).GetField("m_Dropdown", Private).SetValue(dropdown, list);
            return list;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Dropdown_OpenCloseAndChange_RequestExactlyOnceAfterRepeatedWiring(bool building)
        {
            var dropdown = Control<TMP_Dropdown>(Spawn(building));
            Assert.That(dropdown.GetComponents<UiSearchSortClickSfx>().Length, Is.EqualTo(1));
            requests.Clear();
            var before = player.StartedCount;
            Press(dropdown.gameObject);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick }));
            Assert.That(player.StartedCount, Is.EqualTo(before + 1));
            Expand(dropdown);
            Press(dropdown.gameObject);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiClick }));
            Assert.That(player.StartedCount, Is.EqualTo(before + 2));
            now += 0.1f;
            dropdown.value = (dropdown.value + 1) % dropdown.options.Count;
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiClick, SfxId.UiClick }));
            Assert.That(player.StartedCount, Is.EqualTo(before + 3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Dropdown_HoverRightClickDisabledAndCodeReset_AreSilent(bool building)
        {
            var dropdown = Control<TMP_Dropdown>(Spawn(building));
            requests.Clear();
            ExecuteEvents.Execute(dropdown.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Press(dropdown.gameObject, PointerEventData.InputButton.Right);
            dropdown.interactable = false;
            Press(dropdown.gameObject);
            dropdown.interactable = true;
            dropdown.value = (dropdown.value + 1) % dropdown.options.Count;
            Expand(dropdown);
            dropdown.SetValueWithoutNotify((dropdown.value + 1) % dropdown.options.Count);
            Assert.That(requests, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Search_FirstMouseFocusOnly_TypingAndRepeatedPressAreSilent(bool building)
        {
            var input = Control<TMP_InputField>(Spawn(building));
            requests.Clear();
            Press(input.gameObject);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick }));
            Press(input.gameObject); // TMP가 포커스를 활성화하기 전 같은 프레임의 재입력도 중복 금지.
            // 실제 TMP 포커스 전환은 PlayMode에서 확인한다. EditMode는 활성 입력 상태를 주입한다.
            typeof(TMP_InputField).GetField("m_AllowInput", Private).SetValue(input, true);
            Assert.That(input.isFocused, Is.True);
            Press(input.gameObject);
            input.text = "사ㄷ";
            input.SetTextWithoutNotify(string.Empty);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick }));
            input.DeactivateInputField();
            input.onDeselect.Invoke(input.text);
            Press(input.gameObject);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiClick }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Search_ProgrammaticSelectionAndFocusAndDisabledPress_AreSilent(bool building)
        {
            var input = Control<TMP_InputField>(Spawn(building));
            requests.Clear();
            input.onSelect.Invoke(input.text);
            input.ActivateInputField();
            Assert.That(requests, Is.Empty);
            typeof(TMP_InputField).GetField("m_AllowInput", Private).SetValue(input, true);
            Press(input.gameObject);
            input.DeactivateInputField();
            input.onDeselect.Invoke(input.text);
            input.interactable = false;
            Press(input.gameObject);
            Assert.That(requests, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Muted_ClickRequestsDoNotStartPlayback(bool building)
        {
            var root = Spawn(building);
            var input = Control<TMP_InputField>(root);
            var dropdown = Control<TMP_Dropdown>(root);
            muted = true;
            requests.Clear();
            var before = player.StartedCount;
            Press(input.gameObject);
            Press(dropdown.gameObject);
            Expand(dropdown);
            now += 0.1f;
            dropdown.value = (dropdown.value + 1) % dropdown.options.Count;
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick, SfxId.UiClick, SfxId.UiClick }));
            Assert.That(player.StartedCount, Is.EqualTo(before));
        }
    }
}
