using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Economy
{
    /// <summary>판매 목록 한 행. 표시만 하며 클릭 시 mineralId를 위임한다.</summary>
    public sealed class EconomySellRowView : MonoBehaviour
    {
        [SerializeField] private string mineralId;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text ownedText;
        [SerializeField] private TMP_Text unitPriceText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Graphic selectedChrome;

        public string MineralId => mineralId;
        public event Action<string> Selected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnClicked);
            }
        }

        public void Bind(SellMineralRowReadModel row)
        {
            mineralId = row.MineralId;

            if (iconImage != null)
            {
                iconImage.sprite = row.Icon;
                iconImage.enabled = row.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(row.DisplayName) ? row.MineralId : row.DisplayName;
            }

            if (ownedText != null)
            {
                ownedText.text = "보유 " + row.OwnedQuantity;
            }

            if (unitPriceText != null)
            {
                unitPriceText.text = row.UnitPrice + "G";
            }

            if (selectedChrome != null)
            {
                selectedChrome.enabled = row.IsSelected;
            }
        }

        private void OnClicked()
        {
            if (!string.IsNullOrEmpty(mineralId))
            {
                Selected?.Invoke(mineralId);
            }
        }

        /// <summary>런타임/빌더에서 참조를 주입한다.</summary>
        public void BindReferences(
            string id,
            Image icon,
            TMP_Text name,
            TMP_Text owned,
            TMP_Text unitPrice,
            Button select,
            Graphic chrome)
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnClicked);
            }

            mineralId = id;
            iconImage = icon;
            nameText = name;
            ownedText = owned;
            unitPriceText = unitPrice;
            selectButton = select;
            selectedChrome = chrome;

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClicked);
            }
        }

#if UNITY_EDITOR
        public void EditorBind(
            string id,
            Image icon,
            TMP_Text name,
            TMP_Text owned,
            TMP_Text unitPrice,
            Button select,
            Graphic chrome)
        {
            BindReferences(id, icon, name, owned, unitPrice, select, chrome);
        }
#endif
    }
}
