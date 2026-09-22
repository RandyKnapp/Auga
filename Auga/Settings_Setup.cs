using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Auga
{
    // Valheim's settings menu is split into tabs (Valheim.SettingsGui.*); Settings_Builder.cs rebuilds it with Auga
    // widgets. The key binding rows carry an AugaBindingDisplay that this patch keeps in sync.
    [HarmonyPatch]
    public static class Settings_Setup
    {
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
