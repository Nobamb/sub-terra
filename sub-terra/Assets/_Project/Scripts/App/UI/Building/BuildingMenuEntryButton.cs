using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// Prefab의 시설 버튼을 영구 ID 기반 선택 요청에 연결한다.
    /// prompt-B 35-3: 클릭 후 EventSystem 선택을 남겨 두지 않아
    /// Enter가 시설 버튼/단축키 Submit으로 재실행되지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BuildingMenuEntryButton : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private BuildingMenuBinder binder;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(Select);
        }

        private void Select()
        {
            binder?.SelectBuilding(buildingId);
            // 선택 직후에도 키보드 Submit 잔여 대상을 남기지 않는다.
            UiKeyboardSubmitGuard.ClearSelection();
        }

#if UNITY_EDITOR
        public void EditorSet(string permanentId, BuildingMenuBinder target)
        {
            buildingId = permanentId ?? string.Empty;
            binder = target;
        }
#endif
    }
}
