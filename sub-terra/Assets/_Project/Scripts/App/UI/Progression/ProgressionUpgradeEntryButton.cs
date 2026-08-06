using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 업그레이드 목록의 한 행.
    /// Button.onClick 외에 IPointerClickHandler로도 선택해
    /// 하위 탭 클릭이 다른 UI/리스너 상태에 막히지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class ProgressionUpgradeEntryButton : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color NormalColor = new Color(0.14f, 0.28f, 0.34f, 1f);
        private static readonly Color SelectedColor = new Color(0.28f, 0.52f, 0.42f, 1f);

        [SerializeField] private string upgradeId;
        [SerializeField] private ProgressionPanelBinder binder;
        [SerializeField] private TMP_Text label;

        public string UpgradeId => upgradeId;

        private Button button;
        private Image background;
        private ProgressionPanelView cachedView;

        private void Awake()
        {
            button = GetComponent<Button>();
            background = GetComponent<Image>();
            ConfigureButton();
        }

        private void OnEnable()
        {
            EnsureInteractable();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void SetSnapshot(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            if (upgrades == null || label == null || string.IsNullOrEmpty(upgradeId))
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

        /// <summary>선택 중인 목록 행을 색상으로 하이라이트한다.</summary>
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

        /// <summary>EventSystem 포인터 클릭 — Button 리스너가 깨져도 동작한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            RequestSelect();
        }

        private void OnButtonClicked()
        {
            RequestSelect();
        }

        private void RequestSelect()
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return;
            }

            // 1) View 직접 경로 (presenter 연결이 가장 확실)
            if (cachedView == null)
            {
                cachedView = GetComponentInParent<ProgressionPanelView>(true);
            }

            if (cachedView != null)
            {
                cachedView.SelectUpgradeEntry(upgradeId);
                return;
            }

            // 2) Binder 경로 폴백
            var target = binder != null
                ? binder
                : GetComponentInParent<ProgressionPanelBinder>(true);
            if (target == null)
            {
                return;
            }

            binder = target;
            target.SelectUpgrade(upgradeId);
        }

        /// <summary>
        /// 필터로 다시 켜진 하위 탭이 항상 클릭·선택 가능하도록
        /// 버튼/그래픽/클릭 리스너를 재배선한다.
        /// </summary>
        public void EnsureInteractable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            ConfigureButton();

            if (button != null)
            {
                button.interactable = true;
                button.enabled = true;
                button.onClick.RemoveListener(OnButtonClicked);
                button.onClick.AddListener(OnButtonClicked);
            }

            if (background != null)
            {
                background.enabled = true;
                background.raycastTarget = true;
                var color = background.color;
                if (color.a < 0.9f)
                {
                    color.a = 1f;
                    background.color = color;
                }
            }

            if (label != null)
            {
                label.raycastTarget = false;
            }

            if (binder == null)
            {
                binder = GetComponentInParent<ProgressionPanelBinder>(true);
            }

            cachedView = GetComponentInParent<ProgressionPanelView>(true);
        }

        private void ConfigureButton()
        {
            if (button == null)
            {
                return;
            }

            if (background != null)
            {
                button.targetGraphic = background;
            }

            // 키보드 네비게이션이 포커스를 가로채지 않게 한다.
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.transition = Selectable.Transition.ColorTint;
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
