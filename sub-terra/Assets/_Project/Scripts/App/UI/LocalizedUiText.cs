using System;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI
{
    /// <summary>Keeps labels created by runtime UI builders in the applied language.</summary>
    internal sealed class LocalizedUiText : MonoBehaviour
    {
        private TMP_Text label;
        private Func<string> value;

        public static void Bind(TMP_Text target, Func<string> getText)
        {
            if (target == null) return;
            var binding = target.gameObject.AddComponent<LocalizedUiText>();
            binding.label = target;
            binding.value = getText;
            binding.Refresh();
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (label != null && value != null) label.text = value();
        }
    }
}
