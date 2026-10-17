#if UNITY_EDITOR || SUBTERRA_BUILD_QA
using System;
using System.Collections.Generic;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace SubTerra.App.UI.HUD
{
    /// <summary>격리 QA 실행에서만 마우스 버튼을 일반 Input System 키 상태로 전달한다.</summary>
    public sealed class QaPointerInputOverlay : MonoBehaviour
    {
        private Keyboard keyboard;
        private readonly Dictionary<Key, float> releases = new();
        private TMP_Text status;
        private ControlScheme previousScheme;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-subterra-qa-pointer-input") < 0
                || Array.IndexOf(args, "-subterra-save-root") < 0) return;
            if (FindAnyObjectByType<QaPointerInputOverlay>() != null) return;
            var root = new GameObject("QaPointerInputOverlay");
            DontDestroyOnLoad(root);
            root.AddComponent<QaPointerInputOverlay>();
        }

        private void Awake()
        {
            keyboard = InputSystem.AddDevice<Keyboard>("QaPointerKeyboard");
            previousScheme = ControlPreferences.Scheme;
            // 방향키 이동/WASD 채굴은 기존 설정에서 제공하는 조작 방식이다.
            ControlPreferences.Scheme = ControlScheme.ArrowsMove;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildUi();
        }

        public void Pulse(Key key, float seconds)
        {
            if (keyboard == null || ControlPreferences.IsSettingsOpen) return;
            ReleaseAll();
            releases[key] = Time.unscaledTime + Mathf.Clamp(seconds, 0.1f, 2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            if (status != null) status.text = "QA pointer input: " + key;
        }

        public void ReleaseAll()
        {
            releases.Clear();
            if (keyboard != null && keyboard.added)
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        private void Update()
        {
            foreach (var release in releases)
                if (Time.unscaledTime >= release.Value || ControlPreferences.IsSettingsOpen)
                {
                    ReleaseAll();
                    if (status != null) status.text = "QA pointer input: idle";
                    break;
                }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ReleaseAll();
            ControlPreferences.Scheme = ControlScheme.ArrowsMove;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
        private void OnDisable() => ReleaseAll();
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ReleaseAll();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            ControlPreferences.Scheme = previousScheme;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("QaInputCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("QaInputPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-12, 12);
            rect.sizeDelta = new Vector2(620, 112);
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.015f, 0.04f, 0.05f, 0.96f);
            status = Label(panel.transform, "QA pointer input: idle", new Vector2(6, -2), new Vector2(608, 24));
            string[] labels = { "Left", "Right", "Up", "Down", "Jump", "E / Use", "Mine L", "Mine R", "Mine Up", "Mine D", "C / Build", "Stop" };
            Key[] keys = { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.Space, Key.E,
                Key.A, Key.D, Key.W, Key.S, Key.C, Key.None };
            for (int i = 0; i < labels.Length; i++)
            {
                var buttonObject = new GameObject(labels[i], typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(panel.transform, false);
                var buttonRect = (RectTransform)buttonObject.transform;
                buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0, 1);
                buttonRect.anchoredPosition = new Vector2(6 + i % 6 * 102, -28 - i / 6 * 40);
                buttonRect.sizeDelta = new Vector2(98, 36);
                buttonObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.21f, 0.25f);
                var label = Label(buttonObject.transform, labels[i], Vector2.zero, buttonRect.sizeDelta);
                label.alignment = TextAlignmentOptions.Center;
                var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                Key key = keys[i];
                float duration = i >= 6 && i <= 9 ? 2f : i < 4 ? 0.45f : 0.15f;
                button.onClick.AddListener(() => { if (key == Key.None) ReleaseAll(); else Pulse(key, duration); });
            }
        }

        private static TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size)
        {
            var root = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = root.GetComponent<TextMeshProUGUI>();
            label.fontSize = 16;
            label.color = new Color(0.8f, 0.96f, 1);
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
    }
}
#endif
