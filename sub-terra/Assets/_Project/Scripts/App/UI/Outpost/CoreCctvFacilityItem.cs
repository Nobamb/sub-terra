using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>코어 CCTV 목록의 한 줄. 시설 아이콘과 표시 이름만 보여 주고, 누르면 관찰 대상만 바꾼다.</summary>
    public sealed class CoreCctvFacilityItem : MonoBehaviour
    {
        private static readonly Color BackgroundIdle = new Color(0.035f, 0.075f, 0.115f, 0.86f);
        private static readonly Color BackgroundSelected = new Color(0.05f, 0.17f, 0.22f, 0.96f);
        private static readonly Color BorderIdle = new Color(0.25f, 0.46f, 0.56f, 0.38f);
        private static readonly Color BorderSelected = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color GlowSelected = new Color(0.30f, 0.90f, 1f, 0.22f);
        private static readonly Color NameIdle = new Color(0.62f, 0.73f, 0.80f, 1f);
        private static readonly Color NameSelected = new Color(0.90f, 1f, 1f, 1f);

        [SerializeField] private RectTransform rect;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private Image glow;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;

        private bool selected;

        public event Action<string> Clicked;

        public string InstanceId { get; private set; } = string.Empty;
        public string BuildingId { get; private set; } = string.Empty;
        public bool IsSelected => selected;
        public RectTransform Rect => rect != null ? rect : (RectTransform)transform;
        public Image Icon => icon;
        public TMP_Text NameText => nameText;
        public Button Button => button;
        public float Alpha => group != null ? group.alpha : 1f;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
                UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }

        public void Bind(CoreCctvFacility facility, Sprite sprite)
        {
            InstanceId = facility.InstanceId;
            BuildingId = facility.BuildingId;
            gameObject.name = "Facility_" + facility.InstanceId;
            if (nameText != null)
            {
                nameText.text = facility.DisplayName;
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            SetSelected(false);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (background != null)
            {
                background.color = value ? BackgroundSelected : BackgroundIdle;
            }

            if (border != null)
            {
                border.color = value ? BorderSelected : BorderIdle;
            }

            if (glow != null)
            {
                glow.color = value ? GlowSelected : new Color(GlowSelected.r, GlowSelected.g, GlowSelected.b, 0f);
            }

            if (nameText != null)
            {
                nameText.color = value ? NameSelected : NameIdle;
                nameText.fontStyle = value ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        /// <summary>등장 중에는 보이는 정도에 맞춰 입력도 막는다.</summary>
        public void SetAlpha(float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.6f;
        }

        public void SetArt(Sprite borderSprite, Sprite glowSprite)
        {
            if (border != null && border.sprite == null)
            {
                border.sprite = borderSprite;
            }

            if (glow != null && glow.sprite == null)
            {
                glow.sprite = glowSprite;
            }
        }

        private void OnClick()
        {
            Clicked?.Invoke(InstanceId);
        }
    }
}
