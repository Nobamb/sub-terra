using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>기존 설정 패널 안에 생성되는 공통 키보드 조작 안내/선택 창.</summary>
    public sealed class ControlSchemePanel : MonoBehaviour
    {
        private GameObject overlay;
        private TMP_FontAsset font;
        private TMP_Text title;
        private TMP_Text description;
        private readonly System.Collections.Generic.List<TMP_Text> keyLabels = new();
        public ControlScheme Selected { get; private set; }
        public bool IsOpen => overlay != null && overlay.activeSelf;

        public static ControlSchemePanel Attach(GameObject settingsRoot)
        {
            if (settingsRoot == null) return null;
            var panel = settingsRoot.GetComponent<ControlSchemePanel>();
            if (panel != null) return panel;
            panel = settingsRoot.AddComponent<ControlSchemePanel>();
            try
            {
                panel.Build();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, settingsRoot);
            }
            return panel;
        }

        private void OnEnable() => ControlPreferences.IsSettingsOpen = true;
        private void OnDisable()
        {
            ControlPreferences.IsSettingsOpen = false;
            Close();
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void SetDraft(ControlScheme scheme)
        {
            Selected = ControlPreferences.FromIndex((int)scheme);
            Refresh();
        }

        public void Cycle(int delta)
        {
            Selected = ControlPreferences.Cycle(Selected, delta);
            Refresh();
            SettingsRuntimeApplier.SaveControlScheme(Selected);
        }

        public void Open()
        {
            if (overlay == null) return;
            Refresh();
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public bool Close()
        {
            if (!IsOpen) return false;
            overlay.SetActive(false);
            SettingsRuntimeApplier.SaveControlScheme(Selected);
            UiKeyboardSubmitGuard.ClearSelection();
            return true;
        }

        public static ControlScheme PeekSelected(GameObject settingsRoot, ControlScheme fallback)
        {
            if (settingsRoot == null) return fallback;
            var panel = settingsRoot.GetComponent<ControlSchemePanel>();
            if (panel == null) panel = settingsRoot.GetComponentInChildren<ControlSchemePanel>(true);
            return panel != null ? panel.Selected : fallback;
        }

        private void Build()
        {
            var sample = GetComponentInChildren<TMP_Text>(true);
            font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
            Button(transform, "ChangeControls", "키 조작 변경", 0, -366, 280, 40, Open);
            var backdrop = Rect(transform, "ControlSchemeOverlay", 0, 0, 0, 0);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.012f, 0.02f, 0.96f);
            overlay = backdrop.gameObject;
            Selected = ControlPreferences.FromIndex((int)ControlPreferences.Scheme);
            var card = Rect(backdrop, "KeyboardCard", 0, 0, 960, 650);
            card.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.07f, 0.105f);
            title = Label(card, "SchemeTitle", "", 0, 272, 700, 45, 30);
            Button(card, "PreviousScheme", "<", -420, 0, 64, 80, () => Cycle(-1));
            Button(card, "NextScheme", ">", 420, 0, 64, 80, () => Cycle(1));
            Label(card, "KeyboardCaption", "키보드 · 이동 / 채굴", 0, 217, 650, 32, 22);

            // 모든 방식에서 같은 키 배열을 유지하고 역할·색상·설명만 갱신한다.
            Row(card, new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }, -328, 146);
            Row(card, new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" }, -310, 80);
            Row(card, new[] { "Z", "X", "C", "V", "B", "N", "M" }, -280, 14);
            Key(card, "↑", 250, 14);
            Key(card, "←", 184, -52);
            Key(card, "↓", 250, -52);
            Key(card, "→", 316, -52);
            Key(card, "Space", -175, -65, 220);
            Key(card, "Enter", 15, -65, 130);
            Key(card, "마우스 클릭", -80, -136, 250);
            description = Label(card, "SchemeDescription", "", 0, -217, 760, 100, 22);
            Button(card, "CloseControls", "선택 후 돌아가기", 0, -291, 300, 42, () => Close());
            overlay.SetActive(false);
            Refresh();
        }

        private void Row(Transform parent, string[] keys, float x, float y)
        {
            for (int i = 0; i < keys.Length; i++) Key(parent, keys[i], x + i * 66, y);
        }

        private void Key(Transform parent, string key, float x, float y, float width = 60)
        {
            var rect = Rect(parent, key, x, y, width, 58);
            rect.gameObject.AddComponent<Image>();
            var label = Label(rect, "Role", key, 0, 0, width - 4, 54, 16);
            keyLabels.Add(label);
        }

        private void Refresh()
        {
            if (title == null) return;
            title.text = "키 조작 방식 " + ((int)Selected + 1) + " / 3";
            foreach (var label in keyLabels)
            {
                var key = label.transform.parent.name;
                bool wasd = key == "W" || key == "A" || key == "S" || key == "D";
                bool arrows = key == "↑" || key == "←" || key == "↓" || key == "→";
                bool move = wasd && Selected != ControlScheme.ArrowsMove
                    || arrows && Selected != ControlScheme.WasdMove;
                bool mine = wasd && Selected == ControlScheme.ArrowsMove
                    || arrows && Selected == ControlScheme.WasdMove || key == "Enter" || key == "마우스 클릭";
                string role = move ? "이동" : mine ? "채굴" : key == "Space" ? "점프" : "";
                if (wasd || arrows)
                {
                    string direction = key == "W" || key == "↑" ? "↑" : key == "S" || key == "↓" ? "↓"
                        : key == "A" || key == "←" ? "←" : "→";
                    role = direction + " " + role;
                }
                label.text = key + (role.Length > 0 ? "\n" + role : "");
                label.fontSize = wasd || arrows ? 13 : 16;
                label.transform.parent.GetComponent<Image>().color = move ? new Color(0.08f, 0.38f, 0.48f)
                    : mine ? new Color(0.55f, 0.31f, 0.10f) : new Color(0.12f, 0.17f, 0.22f);
            }
            description.text = Selected == ControlScheme.Classic
                ? "WASD / 방향키: 이동 (위·아래는 사다리)\n마우스 클릭: 선택 블록 채굴 · Enter: 바라보는 방향 채굴"
                : (Selected == ControlScheme.WasdMove ? "WASD: 이동 · 방향키: 상하좌우 채굴"
                    : "방향키: 이동 · WASD: 상하좌우 채굴")
                    + "\n캐릭터 위치 기준 인접 블록 채굴 · 마우스 / Enter도 사용 가능";
            description.text += "\n< >로 바꾸면 바로 저장됩니다. 돌아가기 후에도 유지됩니다.";
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private TMP_Text Label(Transform parent, string name, string text, float x, float y, float width, float height, float size)
        {
            var label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private void Button(Transform parent, string name, string text, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.10f, 0.28f, 0.38f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            Label(rect, "Label", text, 0, 0, width - 8, height - 4, 22);
        }
    }
}
