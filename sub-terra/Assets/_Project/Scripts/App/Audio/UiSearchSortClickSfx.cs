using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Audio
{
    /// <summary>B-192: 검색 시작·정렬 펼침·항목 변경에만 기존 클릭음을 연결한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UiSearchSortClickSfx : MonoBehaviour, IPointerDownHandler
    {
        private TMP_InputField searchField;
        private TMP_Dropdown dropdown;
        private bool searchClickPending;

        public static void Attach(UnityEngine.UI.Selectable control)
        {
            if (control == null) return;
            var sound = control.GetComponent<UiSearchSortClickSfx>();
            if (sound == null) sound = control.gameObject.AddComponent<UiSearchSortClickSfx>();
            sound.OnEnable();
        }

        private void OnEnable()
        {
            searchField = GetComponent<TMP_InputField>();
            dropdown = GetComponent<TMP_Dropdown>();
            if (searchField != null)
            {
                searchField.onDeselect.RemoveListener(ResetSearchClick);
                searchField.onDeselect.AddListener(ResetSearchClick);
                searchField.onEndEdit.RemoveListener(ResetSearchClick);
                searchField.onEndEdit.AddListener(ResetSearchClick);
            }
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnSortChanged);
                dropdown.onValueChanged.AddListener(OnSortChanged);
            }
        }

        private void OnDisable()
        {
            if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnSortChanged);
            if (searchField != null)
            {
                searchField.onDeselect.RemoveListener(ResetSearchClick);
                searchField.onEndEdit.RemoveListener(ResetSearchClick);
            }
            searchClickPending = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (dropdown != null && dropdown.IsActive() && dropdown.IsInteractable() && !dropdown.IsExpanded)
            {
                SfxPlayer.Play(SfxId.UiClick);
            }

            // TMP는 PointerDown에서 선택하고 다음 LateUpdate에 입력을 활성화한다.
            // PointerClick까지 기다리면 마우스를 누른 사이 이미 포커스가 생겨 첫 클릭을 놓친다.
            if (searchField != null && searchField.IsActive() && searchField.IsInteractable()
                && !searchField.isFocused && !searchClickPending)
            {
                searchClickPending = true;
                SfxPlayer.Play(SfxId.UiClick);
            }
        }

        private void OnSortChanged(int _)
        {
            // TMP는 항목을 고를 때 값을 먼저 알리고 목록을 닫는다. 코드 초기화는 무음이다.
            if (dropdown != null && dropdown.IsActive() && dropdown.IsInteractable() && dropdown.IsExpanded)
            {
                SfxPlayer.Play(SfxId.UiClick);
            }
        }

        private void ResetSearchClick(string _)
        {
            searchClickPending = false;
        }
    }
}
