using System;
using System.Linq;
using System.Reflection;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Unity Pipeline run_script용 검증 도구. Assets 밖에 두어 게임 빌드에 포함하지 않는다.
public static class Settings104Review
{
    public static void Resolution(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { GameViewSizeGroupType.Standalone });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
        if (!SessionState.GetBool("Settings104ResolutionStored", false))
            SessionState.SetInt("Settings104CustomCount", (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null));
        var fixedSize = Activator.CreateInstance(sizeType, Enum.ToObject(modeType, 1), width, height, "104 Review");
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { fixedSize });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        var index = window.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (!SessionState.GetBool("Settings104ResolutionStored", false))
        {
            SessionState.SetInt("Settings104Resolution", (int)index.GetValue(window));
            SessionState.SetBool("Settings104ResolutionStored", true);
        }
        index.SetValue(window, count - 1);
        window.Repaint();
    }

    public static void RestoreResolution()
    {
        var window = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(window, SessionState.GetInt("Settings104Resolution", 0));
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { GameViewSizeGroupType.Standalone });
        int count = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        // 현재 검증 세션에서 추가한 해상도 항목만 정리한다.
        if (SessionState.GetInt("Settings104CustomCount", -1) >= 0)
            while (count > SessionState.GetInt("Settings104CustomCount", -1))
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { --count });
        SessionState.EraseBool("Settings104ResolutionStored");
    }

    public static void Open()
    {
        UnityEngine.Object.FindAnyObjectByType<MainMenuBinder>().Presenter.OpenSettings();
    }

    public static string Inspect()
    {
        Canvas.ForceUpdateCanvases();
        var skin = UnityEngine.Object.FindAnyObjectByType<SettingsMenuSkin>();
        var card = (RectTransform)skin.transform.Find("SettingsCard");
        var corners = new Vector3[4];
        card.GetWorldCorners(corners);
        Require(corners.All(p => p.x >= 0 && p.y >= 0 && p.x <= Screen.width && p.y <= Screen.height), "Card clipped");
        var text = card.GetComponentsInChildren<TMP_Text>().Where(t => t.gameObject.activeInHierarchy).ToArray();
        foreach (var label in text) { label.ForceMeshUpdate(); Require(!label.isTextOverflowing, "Text overflow: " + label.name); }
        foreach (var button in card.GetComponentsInChildren<UnityEngine.UI.Selectable>())
        {
            var center = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = center }, hits);
            Require(hits.Any(h => h.gameObject.transform.IsChildOf(button.transform) || h.gameObject == button.gameObject), "Raycast missing: " + button.name);
        }
        return Screen.width + "x" + Screen.height + ": card in bounds; " + text.Length + " labels without overflow; controls raycastable";
    }

    public static string TextMetrics()
    {
        var skin = UnityEngine.Object.FindAnyObjectByType<SettingsMenuSkin>();
        return string.Join("\n", skin.GetComponentsInChildren<TMP_Text>().Select(t =>
            t.name + " rect=" + t.rectTransform.rect.size + " preferred=" + t.preferredWidth + "," + t.preferredHeight + " overflow=" + t.isTextOverflowing));
    }

    public static string Exercise()
    {
        var binder = UnityEngine.Object.FindAnyObjectByType<MainMenuBinder>();
        var view = binder.GetComponent<MainMenuView>();
        var presenter = binder.Presenter;
        var original = presenter.Settings.Applied.Clone();
        var card = view.transform.Find("SettingsPanel/SettingsCard");
        var slider = card.GetComponentInChildren<UnityEngine.UI.Slider>();
        slider.value = 0.37f;
        Require(Mathf.Abs(AudioListener.volume - 0.37f) < 0.001f, "Volume preview");
        Require(card.Find("MasterVolumeLabel").GetComponent<TMP_Text>().text == "37%", "Volume display");
        card.GetComponentInChildren<UnityEngine.UI.Toggle>().isOn = !original.ReduceMotion;
        var resolution = card.Find("ResolutionDropdown").GetComponent<TMP_Dropdown>();
        resolution.value = (resolution.value + 1) % resolution.options.Count;
        card.Find("FrameRateDropdown").GetComponent<TMP_Dropdown>().value = 1;
        card.Find("LanguageDropdown").GetComponent<TMP_Dropdown>().value = 1;
        Require(card.Find("AudioTitle").GetComponent<TMP_Text>().text == "Audio", "Language preview");
        var draft = view.ReadSettingsDraft(original);
        Require(draft.ReduceMotion != original.ReduceMotion && draft.LanguageCode == "en", "Draft controls");
        card.Find("SettingsDefaults").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Require(Mathf.Abs(slider.value - 0.5f) < 0.001f, "Defaults");
        card.Find("SettingsClose").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Require(!presenter.Settings.IsOpen, "Close/Cancel");
        Require(Mathf.Abs(AudioListener.volume - original.MasterVolume) < 0.001f, "Cancel restores volume");
        presenter.OpenSettings();
        Require(Mathf.Abs(slider.value - original.MasterVolume) < 0.001f, "Reopen restores draft");
        card.Find("ChangeControls").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Require(view.TryCloseControlSchemePanel(), "Existing controls popup");
        return "PASS: volume preview/value, toggle, resolution/frame/language draft, defaults, X/cancel rollback, reopen, existing controls popup";
    }

    public static void Dropdown()
    {
        UnityEngine.Object.FindAnyObjectByType<SettingsMenuSkin>().transform.Find("SettingsCard/ResolutionDropdown")
            .GetComponent<TMP_Dropdown>().Show();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
