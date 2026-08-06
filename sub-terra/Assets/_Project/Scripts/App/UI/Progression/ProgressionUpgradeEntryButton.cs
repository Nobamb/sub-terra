using System.Collections.Generic;
using SubTerra.App.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>업그레이드 목록의 한 행을 영구 ID 기반 선택 요청에 연결한다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ProgressionUpgradeEntryButton : MonoBehaviour
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private ProgressionPanelBinder binder;
        [SerializeField] private TMP_Text label;

        public string UpgradeId => upgradeId;

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

        public void SetSnapshot(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            if (upgrades == null || label == null)
            {
                return;
            }

            for (var i = 0; i < upgrades.Count; i++)
            {
                var upgrade = upgrades[i];
                if (upgrade.UpgradeId != upgradeId)
                {
                    continue;
                }

                label.text = upgrade.DisplayName
                    + "  Lv."
                    + upgrade.CurrentLevel
                    + "/"
                    + upgrade.MaximumLevel;
                return;
            }
        }

        private void Select()
        {
            binder?.SelectUpgrade(upgradeId);
        }

#if UNITY_EDITOR
        public void EditorSet(string permanentId, ProgressionPanelBinder target, TMP_Text targetLabel)
        {
            upgradeId = permanentId ?? string.Empty;
            binder = target;
            label = targetLabel;
        }
#endif
    }
}
