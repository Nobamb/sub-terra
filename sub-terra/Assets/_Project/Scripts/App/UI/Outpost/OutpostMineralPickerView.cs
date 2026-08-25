using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>보관함 자원 검색창과 드롭다운 목록. State는 변경하지 않는다.</summary>
    public sealed class OutpostMineralPickerView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button captionButton;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Transform optionsContent;
        [SerializeField] private Button optionTemplate;
        [SerializeField] private TMP_Text emptyLabel;

        private readonly List<GameObject> spawnedOptions = new List<GameObject>();
        private string selectedMineralId = string.Empty;
        private int originalSiblingIndex;

        public event Action<string> SearchChanged;
        public event Action<string> MineralSelected;

        public bool HasRequiredReferences()
        {
            return searchInput != null
                && captionButton != null
                && captionText != null
                && optionsPanel != null
                && optionsContent != null
                && optionTemplate != null;
        }

        private void Awake()
        {
            originalSiblingIndex = transform.GetSiblingIndex();
            if (optionTemplate != null)
            {
                optionTemplate.gameObject.SetActive(false);
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchChanged);
                searchInput.onSelect.AddListener(OnSearchSelected);
            }

            if (captionButton != null)
            {
                captionButton.onClick.AddListener(ToggleOptions);
            }

            if (captionText != null && string.IsNullOrEmpty(captionText.text))
            {
                captionText.text = "자원 선택";
            }
        }

        private void OnDestroy()
        {
            if (searchInput != null)
            {
                searchInput.onValueChanged.RemoveListener(OnSearchChanged);
                searchInput.onSelect.RemoveListener(OnSearchSelected);
            }

            if (captionButton != null)
            {
                captionButton.onClick.RemoveListener(ToggleOptions);
            }
        }

        public void SetOptions(IReadOnlyList<OutpostMineralOption> options, string selectedId)
        {
            selectedMineralId = selectedId ?? string.Empty;
            UpdateCaption(options);
            RebuildOptions(options);
        }

        public void ClearSearch()
        {
            if (searchInput != null && searchInput.text.Length > 0)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }

            SetOptionsOpen(false);
        }

        public string CurrentSearch => searchInput != null ? searchInput.text : string.Empty;

        private void OnSearchChanged(string value)
        {
            SetOptionsOpen(true);
            SearchChanged?.Invoke(value ?? string.Empty);
        }

        private void OnSearchSelected(string _)
        {
            SetOptionsOpen(true);
        }

        private void ToggleOptions()
        {
            SetOptionsOpen(optionsPanel == null || !optionsPanel.activeSelf);
        }

        private void NotifySelected(string mineralId)
        {
            selectedMineralId = mineralId ?? string.Empty;
            SetOptionsOpen(false);
            if (searchInput != null)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }

            MineralSelected?.Invoke(selectedMineralId);
        }

        private void SetOptionsOpen(bool open)
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(open);
            }

            if (open)
            {
                transform.SetAsLastSibling();
            }
            else
            {
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }

        private void UpdateCaption(IReadOnlyList<OutpostMineralOption> options)
        {
            if (captionText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedMineralId))
            {
                captionText.text = "자원 선택";
                return;
            }

            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    if (options[i].MineralId == selectedMineralId)
                    {
                        captionText.text = options[i].DisplayName;
                        return;
                    }
                }
            }

            captionText.text = ItemDisplayNames.Mineral(selectedMineralId);
        }

        private void RebuildOptions(IReadOnlyList<OutpostMineralOption> options)
        {
            for (var i = 0; i < spawnedOptions.Count; i++)
            {
                if (spawnedOptions[i] != null)
                {
                    Destroy(spawnedOptions[i]);
                }
            }

            spawnedOptions.Clear();
            var count = options != null ? options.Count : 0;
            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(count == 0);
                emptyLabel.text = "일치하는 자원이 없습니다.";
            }

            if (count == 0 || optionTemplate == null || optionsContent == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var option = options[i];
                var instance = Instantiate(optionTemplate, optionsContent);
                instance.gameObject.SetActive(true);
                instance.name = "Option_" + option.MineralId;
                var label = instance.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = option.Label;
                }

                var image = instance.GetComponent<Image>();
                if (image != null && option.MineralId == selectedMineralId)
                {
                    image.color = new Color(0.22f, 0.42f, 0.55f, 1f);
                }

                var mineralId = option.MineralId;
                instance.onClick.AddListener(() => NotifySelected(mineralId));
                spawnedOptions.Add(instance.gameObject);
            }
        }
    }
}
