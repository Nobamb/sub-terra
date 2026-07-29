using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>Prefab에서 광물 선택 버튼과 영구 ID를 연결한다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class OutpostMineralSelectButton : MonoBehaviour
    {
        [SerializeField] private string mineralId;
        [SerializeField] private OutpostPanelBinder binder;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Select);
        }

        private void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(Select);
            }
        }

        public void EditorSet(string id, OutpostPanelBinder target)
        {
            mineralId = id ?? string.Empty;
            binder = target;
        }

        private void Select()
        {
            binder?.SelectMineral(mineralId);
        }
    }
}
