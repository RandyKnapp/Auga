using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Auga
{
    // Valheim's settings menu is now split into tabs (Valheim.SettingsGui.*). The resolution picker is a native
    // dropdown there (GraphicsSettings.m_resolutionDropdown), so only the language dropdown and the key binding
    // display are still customised here.
    [HarmonyPatch]
    public static class Settings_Setup
    {
        public static Dropdown LanguageDropdown;
        public static TMP_Text LanguageSelectionText;

        private static GameplaySettings _gameplaySettings;

        [HarmonyPatch(typeof(GameplaySettings), nameof(GameplaySettings.Initialize))]
        [HarmonyPostfix]
        public static void GameplaySettings_Initialize_Postfix(GameplaySettings __instance)
        {
            _gameplaySettings = __instance;

            LanguageDropdown = __instance.m_language != null ? __instance.m_language.transform.parent.GetComponent<Dropdown>() : null;
            if (LanguageDropdown == null)
            {
                return;
            }

            LanguageSelectionText = LanguageDropdown.gameObject.GetComponentInChildren<TMP_Text>();
            LanguageDropdown.onValueChanged.RemoveListener(OnLanguageValueChanged);
            LanguageDropdown.onValueChanged.AddListener(OnLanguageValueChanged);
            SetupLanguageOptions();
        }

        private static void SetupLanguageOptions()
        {
            LanguageDropdown.ClearOptions();
            var optionList = new List<string>();
            optionList.Add("No Change");
            optionList.AddRange(Localization.instance.GetLanguages().Select(x => Localization.instance.Localize($"$language_{x.ToLower()}")).ToList());
            LanguageDropdown.AddOptions(optionList);
        }

        private static void OnLanguageValueChanged(int index)
        {
            if (index == 0 || _gameplaySettings == null)
                return;

            _gameplaySettings.m_languageKey = Localization.instance.GetLanguages()[index - 1];
            if (LanguageSelectionText != null)
            {
                LanguageSelectionText.text = LanguageDropdown.options[index].text;
            }
        }

        [HarmonyPatch(typeof(KeyboardMouseSettings), nameof(KeyboardMouseSettings.UpdateBindings))]
        [HarmonyPrefix]
        public static bool UpdateBindings_Prefix(KeyboardMouseSettings __instance)
        {
            foreach (var key in __instance.m_keys)
            {
                // Auga's settings prefab renders bindings with AugaBindingDisplay; on the vanilla prefab there is
                // none, so fall through and set the button text exactly like the game does.
                var bindingDisplay = key.m_keyTransform.GetComponent<AugaBindingDisplay>();
                if (bindingDisplay)
                {
                    bindingDisplay.SetBinding(key.m_keyName);
                }

                var keyButton = key.m_keyTransform.GetComponentInChildren<Button>();
                if (keyButton != null)
                {
                    var textComponent = keyButton.GetComponentInChildren<TMP_Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = Localization.instance.GetBoundKeyString(key.m_keyName, true);
                    }
                }
            }

            return false;
        }
    }
}
