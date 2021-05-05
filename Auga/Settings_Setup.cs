using System.Linq;
using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch]
    public static class Settings_Setup
    {
        public static Dropdown ResolutionDropdown;
        public static Dropdown LanguageDropdown;

        [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Settings __instance)
        {
            ResolutionDropdown = __instance.m_resButtonText.transform.parent.GetComponent<Dropdown>();
            ResolutionDropdown.onValueChanged.AddListener(OnResolutionValueChanged);
            SetupResolutionOptions(__instance);

            LanguageDropdown = __instance.m_language.transform.parent.GetComponent<Dropdown>();
            LanguageDropdown.onValueChanged.AddListener(OnLanguageValueChanged);
            SetupLanguageOptions();
        }

        public static void SetupResolutionOptions(Settings __instance)
        {
            __instance.UpdateValidResolutions();
            ResolutionDropdown.ClearOptions();
            ResolutionDropdown.AddOptions(__instance.m_resolutions.Select(x => $"{x.width}x{x.height} {x.refreshRate}hz").ToList());
            var resolutionIndex = __instance.m_resolutions.IndexOf(__instance.m_selectedRes);
            ResolutionDropdown.value = resolutionIndex;
        }

        private static void OnResolutionValueChanged(int index)
        {
            Settings.instance.m_selectedRes = Settings.instance.m_resolutions[index];
        }

        private static void SetupLanguageOptions()
        {
            LanguageDropdown.ClearOptions();
            LanguageDropdown.AddOptions(Localization.instance.GetLanguages().Select(x => Localization.instance.Localize($"$language_{x.ToLower()}")).ToList());
        }

        private static void OnLanguageValueChanged(int index)
        {
            Settings.instance.m_languageKey = Localization.instance.GetLanguages()[index];
        }

        [HarmonyPatch(typeof(Settings), nameof(Settings.SetQualityText))]
        [HarmonyPrefix]
        public static bool SetQualityText_Prefix(Text text, int level)
        {
            var locIds = new[] { "$settings_low", "$settings_medium", "$settings_high", "$settings_veryhigh" };
            text.text = Localization.instance.Localize(locIds[Mathf.Clamp(level, 0, locIds.Length - 1)]);

            return false;
        }

        [HarmonyPatch(typeof(Settings), nameof(Settings.UpdateBindings))]
        [HarmonyPrefix]
        public static bool UpdateBindings_Prefix(Settings __instance)
        {
            foreach (Settings.KeySetting key in __instance.m_keys)
            {
                var bindingDisplay = key.m_keyTransform.GetComponent<AugaBindingDisplay>();
                bindingDisplay.SetBinding(key.m_keyName);
                //key.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = Localization.instance.GetBoundKeyString(key.m_keyName);
            }

            return false;
        }
    }
}
