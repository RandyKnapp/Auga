using System;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class PauseMenu_Setup
    {
        [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
        public static class Menu_Start_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (__instance.name != "Menu")
                {
                    return;
                }

                var parent = __instance.transform.parent;
                Object.Instantiate(Auga.Assets.MenuPrefab, parent, false).GetComponent<Menu>();
                Object.Destroy(__instance.gameObject);
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.OnSettings))]
        public static class Menu_OnSettings_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (Player.m_localPlayer != null && !Player.m_localPlayer.InCutscene())
                    Game.Pause();
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.OnClose))]
        public static class Menu_OnClose_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                var compendium = __instance.GetComponent<AugaCompendiumController>();
                if (compendium != null)
                {
                    compendium.HideCompendium();
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
        public static class Menu_Show_Patch 
        {
            public static bool Prefix(Menu __instance)
            {
                Gogan.LogEvent("Screen", "Enter", nameof(Menu), 0L);
                __instance.m_root.gameObject.SetActive(true);
                __instance.m_menuDialog.gameObject.SetActive(true);
                __instance.m_logoutDialog.gameObject.SetActive(false);
                __instance.m_quitDialog.gameObject.SetActive(false);

                JoinCode.Show();

                return false;
            }

        }

        [HarmonyPatch(typeof(TextsDialog))]
        public static class TextsDialog_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddActiveEffects))]
            public static bool AddActiveEffects_Prefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddLog))]
            public static bool AddLog_Prefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.UpdateTextsList))]
            public static bool UpdateTextsList_Prefix(TextsDialog __instance)
            {
                __instance.m_texts.Clear();

                var filter = __instance.GetComponent<AugaTextsDialogFilter>();
                foreach (var knownText in Player.m_localPlayer.GetKnownTexts())
                {
                    if (filter == null || knownText.Key.StartsWith(filter.Filter))
                    {
                        var keyText = Localization.instance.Localize(knownText.Key);
                        var separatorIndex = keyText.IndexOf(": ", StringComparison.Ordinal);
                        keyText = separatorIndex >= 0 ? keyText.Substring(separatorIndex + 2) : keyText;
                        __instance.m_texts.Add(new TextsDialog.TextInfo(keyText, Localization.instance.Localize(knownText.Value)));
                    }
                }

                __instance.m_texts.Sort((a, b) => string.Compare(a.m_topic, b.m_topic, StringComparison.CurrentCulture));
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(TextsDialog.ShowText), typeof(TextsDialog.TextInfo))]
            public static void AddLog_Postfix(TextsDialog __instance)
            {
                __instance.m_textArea.text = __instance.m_textArea.text.Replace("color=yellow", $"color={Auga.Colors.Topic}");
            }
        }
    }
}

