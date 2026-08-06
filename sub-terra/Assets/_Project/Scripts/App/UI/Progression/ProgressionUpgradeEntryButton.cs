using System.Collections.Generic;
using SubTerra.App.Core.Data;
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
        private static readonly Color NormalColor = new Color(0.14f, 0.28f, 0.34f, 1f);
        private static readonly Color SelectedColor = new Color(0.28f, 0.52f, 0.42f, 1f);

        [SerializeField] private string upgradeId;
        [SerializeField] private ProgressionPanelBinder binder;
        [SerializeField] private TMP_Text label;

        public string UpgradeId => upgradeId;

        private Button button;
        private Image background;

        private void Awake()
        {
            button = GetComponent<Button>();
            background = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (background == null)
            {
                background = GetComponent<Image>();
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

                var name = ItemDisplayNames.PreferDisplay(upgrade.UpgradeId, upgrade.DisplayName);
                label.text = name
                    + "  Lv."
                    + upgrade.CurrentLevel
                    + "/"
                    + upgrade.MaximumLevel;
                return;
            }
        }

        /// <summary>prompt-B 33-1: 선택 중인 목록 행을 색상으로 하이라이트한다.</summary>
        public void SetSelected(bool selected)
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (background != null)
            {
                background.color = selected ? SelectedColor : NormalColor;
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
