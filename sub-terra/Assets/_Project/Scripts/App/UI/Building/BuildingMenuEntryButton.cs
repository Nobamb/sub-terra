using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>Prefab의 시설 버튼을 영구 ID 기반 선택 요청에 연결한다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class BuildingMenuEntryButton : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private BuildingMenuBinder binder;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(Select);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(Select);
        }

        private void Select()
        {
            binder?.SelectBuilding(buildingId);
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
