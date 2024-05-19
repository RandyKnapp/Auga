using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch]
    public static class Settings_Setup
    {
        public static Dropdown ResolutionDropdown;
        public static TMP_Text ResolutionSelectionText;
        public static Dropdown LanguageDropdown;
        public static TMP_Text LanguageSelectionText;
        
        
        [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Settings __instance)
        {
            ResolutionDropdown = __instance.m_resButtonText.transform.parent.GetComponent<Dropdown>();
            ResolutionSelectionText = ResolutionDropdown.gameObject.GetComponentInChildren<TMP_Text>();
            ResolutionDropdown.onValueChanged.AddListener(OnResolutionValueChanged);
            SetupResolutionOptions(__instance);

            LanguageDropdown = __instance.m_language.transform.parent.GetComponent<Dropdown>();
            LanguageSelectionText = LanguageDropdown.gameObject.GetComponentInChildren<TMP_Text>();
            LanguageDropdown.onValueChanged.AddListener(OnLanguageValueChanged);
            SetupLanguageOptions();
        }

        public static void SetupResolutionOptions(Settings __instance)
        {
            __instance.UpdateValidResolutions();
            ResolutionDropdown.ClearOptions();
            var optionList = new List<string>();
            optionList.Add("No Change");
            optionList.AddRange(__instance.m_resolutions.Select(x => $"{x.width}x{x.height} {x.refreshRateRatio}hz").ToList());
            ResolutionDropdown.AddOptions(optionList);
        }

        private static void OnResolutionValueChanged(int index)
        {
            if (index == 0)
                return;
            
            Settings.instance.m_selectedRes = Settings.instance.m_resolutions[index-1];
            ResolutionSelectionText.text = ResolutionDropdown.options[index].text;
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
            if (index == 0)
                return;
            
            Settings.instance.m_languageKey = Localization.instance.GetLanguages()[index-1];
            LanguageSelectionText.text = LanguageDropdown.options[index].text;
        }

        /*[HarmonyPatch(typeof(Settings), nameof(Settings.SetQualityText))]
        [HarmonyPrefix]
        public static bool SetQualityText_Prefix(Text text, int level)
        {
            var locIds = new[] { "$settings_low", "$settings_medium", "$settings_high", "$settings_veryhigh" };
            text.text = Localization.instance.Localize(locIds[Mathf.Clamp(level, 0, locIds.Length - 1)]);

            return false;
        }*/

        [HarmonyPatch(typeof(Settings), nameof(Settings.UpdateBindings))]
        [HarmonyPrefix]
        public static bool UpdateBindings_Prefix(Settings __instance)
        {
            foreach (var key in __instance.m_keys)
            {
                var bindingDisplay = key.m_keyTransform.GetComponent<AugaBindingDisplay>();
                if (!bindingDisplay)
                {
                    Auga.LogWarning($"Could not set binding for {key.m_keyTransform.name}");
                    continue;
                }
                bindingDisplay.SetBinding(key.m_keyName);

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
            
            Settings.UpdateGamepadMap(__instance.m_gamepadRoot, __instance.m_alternativeGlyphs.isOn, ZInput.InputLayout, true);
            return false;
        }
    }
}
