using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Auga.Utilities;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class PauseMenu_Setup
    {
        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Update))]
        public static class TextDialog_Update_Patch
        {
            public static void TextDialogUpdate(TextsDialog instance)
            {
                instance.UpdateGamepadInput();
                if (instance.m_texts.Count <= 0)
                    return;

                if (instance.m_leftScrollbar == null)
                    return;
                
                if (instance.m_leftScrollRect == null)
                    return;
                
                instance.m_leftScrollbar.size = ((RectTransform)instance.m_leftScrollRect.transform).rect.height / instance.m_listRoot.rect.height;    
            }
            
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(TextDialog_Update_Patch), nameof(TextDialogUpdate))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.ShowText), new []{typeof(TextsDialog.TextInfo)})]
        public static class TextDialog_ShowText_Patch
        {
            public static void ShowText(TextsDialog instance, TextsDialog.TextInfo text)
            {
                if (text == null)
                    return;
                
                instance.m_textAreaTopic.text = Localization.instance.Localize(text.m_topic);
                instance.m_textArea.text = Localization.instance.Localize(text.m_text);
                foreach (TextsDialog.TextInfo text1 in instance.m_texts)
                    text1.m_selected.SetActive(false);
                text.m_selected.SetActive(true);
                if (instance.m_leftScrollRect != null)
                {
                    instance.StartCoroutine(instance.FocusOnCurrentLevel(instance.m_leftScrollRect, instance.m_listRoot, text.m_selected.transform as RectTransform));                    
                }
            }
            
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(TextDialog_ShowText_Patch), nameof(ShowText))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.UpdateNavigation))]
        public static class Menu_UpdateNavigation_Patch
        {
            public static void UpdateNavigation(Menu instance)
            {
                try
                {
                    Button component1;
                    Button component2;
                    Button component3;
                    Button component4;
                    Button component5;
                    
                    List<Button> buttonList = new List<Button>();

                    if (instance.name.StartsWith("Auga"))
                    {
                        component1 = instance.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
                        component2 = instance.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
                        component3 = instance.m_menuDialog.Find("MenuEntries/DividerMedium/CloseButton").GetComponent<Button>();
                        component4 = instance.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();
                        component5 = instance.m_menuDialog.Find("MenuEntries/Compendium").GetComponent<Button>();

                        instance.m_firstMenuButton = component3;
                        
                        //Settings
                        buttonList.Add(component4);
                        
                        //Compendium
                        buttonList.Add(component5);

                        //Save
                        if (instance.m_saveButton.interactable)
                            buttonList.Add(instance.m_saveButton);

                        //Logout
                        buttonList.Add(component1);

                        //Exit
                        if (component2.gameObject.activeSelf)
                            buttonList.Add(component2);

                        //Close Menu
                        buttonList.Add(component3);
                    }
                    else
                    {
                        component1 = instance.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
                        component2 = instance.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
                        component3 = instance.m_menuDialog.Find("MenuEntries/Continue").GetComponent<Button>();
                        component4 = instance.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();

                        instance.m_firstMenuButton = component3;
                        
                        buttonList.Add(component3);
                        
                        if (instance.m_saveButton.interactable)
                            buttonList.Add(instance.m_saveButton);

                        if (instance.m_playerListButton.gameObject.activeSelf)
                            buttonList.Add(instance.m_playerListButton);
                        
                        buttonList.Add(component4);

                        buttonList.Add(component1);
                        
                        if (component2.gameObject.activeSelf)
                            buttonList.Add(component2);
                    }
                    
                    for (int index = 0; index < buttonList.Count; ++index)
                    {
                        Navigation navigation = buttonList[index].navigation with
                        {
                            selectOnUp = index <= 0 ? buttonList[buttonList.Count - 1] : (Selectable) buttonList[index - 1],
                            selectOnDown = index >= buttonList.Count - 1 ? buttonList[0] : (Selectable) buttonList[index + 1]
                        };
                        buttonList[index].navigation = navigation;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Start Menu Navigation ({instance.name}) Error Caught {e.Message}");
                    Debug.LogWarning($"{e.StackTrace}");
                }
            }
            
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Menu_UpdateNavigation_Patch), nameof(UpdateNavigation))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

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
                var playerPrefab = __instance.CurrentPlayersPrefab;
                var newMenu = Object.Instantiate(Auga.Assets.MenuPrefab, parent, false).GetComponent<Menu>();
                newMenu.CurrentPlayersPrefab = playerPrefab;
                WireNewMenuFields(newMenu, __instance);
                Object.Destroy(__instance.gameObject);
            }

            /// <summary>
            /// The Auga menu prefab predates several Menu fields (m_continueButton, m_skipButton, m_settingsButton,
            /// m_logoutButton, m_quitButton, lastSaveText, menuEntriesParent, gamepad map, cloud warnings, ...).
            /// Point the ones Auga has its own buttons for at those, and adopt everything else from the vanilla menu.
            /// </summary>
            private static void WireNewMenuFields(Menu newMenu, Menu vanilla)
            {
                var dialog = newMenu.m_menuDialog;
                Button FindButton(string path) => dialog != null ? dialog.Find(path)?.GetComponent<Button>() : null;

                // The prefab's serialized references for these entries point at file ids that no longer exist in it;
                // in the built bundle they resolve to null or, worse, to arbitrary objects (touching those crashes
                // natively). Re-resolve every entry by path in the Auga prefab; anything it lacks is adopted from the
                // vanilla menu below. The settings prefab must stay vanilla as well (see MainMenu_Setup: the Auga
                // settings panel predates the tabbed settings screen).
                newMenu.m_settingsPrefab = vanilla.m_settingsPrefab;
                newMenu.m_continueButton = FindButton("MenuEntries/DividerMedium/CloseButton");
                newMenu.m_settingsButton = FindButton("MenuEntries/Settings");
                newMenu.m_logoutButton = FindButton("MenuEntries/Logout");
                newMenu.m_quitButton = FindButton("MenuEntries/Exit");
                newMenu.m_saveButton = FindButton("MenuEntries/Save");
                newMenu.m_playerListButton = FindButton("MenuEntries/CurrentPlayerList");
                newMenu.m_skipButton = FindButton("MenuEntries/SkipIntro");
                newMenu.m_inviteButton = null;
                newMenu.lastSaveText = dialog != null ? dialog.Find("MenuEntries/LastTimeSaved")?.GetComponent<TMP_Text>() : null;
                newMenu.menuEntriesParent = dialog != null ? dialog.Find("MenuEntries") as RectTransform : null;
                if (newMenu.m_skipButton != null)
                {
                    // the prefab's SkipIntro entry has no click handler serialized
                    newMenu.m_skipButton.onClick.RemoveAllListeners();
                    newMenu.m_skipButton.onClick.AddListener(newMenu.OnSkip);
                }

                // Vanilla keeps the gamepad map (with two full-screen darken images), the cloud-storage warnings and
                // the dialogs under Menu.m_root, which Hide() deactivates. Adopted objects must land under the Auga
                // menu's own root for the same reason; parking them next to the root leaves them visible while the
                // menu is closed (the gamepad map darkened the screen until the menu was first opened).
                var vanillaRoot = vanilla.m_root;
                var augaRoot = newMenu.m_root != null ? newMenu.m_root : newMenu.transform;
                SerializedFieldHelper.CopyMissingFields(newMenu, vanilla,
                    t => vanillaRoot != null && t.IsChildOf(vanillaRoot) ? augaRoot : newMenu.transform,
                    nameof(Menu.CurrentPlayersPrefab));

                // The vanilla menu is a root Canvas of its own now; the Auga prefab has none and would not render.
                SetupHelper.EnsureRootCanvas(newMenu.gameObject, vanilla.gameObject);

                // Entries adopted from the vanilla menu (save, player list, skip intro, invite, last-save label) are
                // stacked below Auga's own entries; the game toggles their visibility itself.
                var entries = dialog != null ? (dialog.Find("MenuEntries") as RectTransform ?? (RectTransform)dialog) : null;
                if (entries != null)
                {
                    var bottom = float.MaxValue;
                    foreach (RectTransform child in entries)
                    {
                        bottom = Mathf.Min(bottom, child.anchoredPosition.y - child.rect.height * (1f - child.pivot.y));
                    }
                    if (bottom == float.MaxValue) bottom = 0f;

                    foreach (var adopted in new Component[] { newMenu.m_saveButton, newMenu.lastSaveText, newMenu.m_playerListButton, newMenu.m_inviteButton, newMenu.m_skipButton })
                    {
                        if (adopted == null || adopted.transform.IsChildOf(entries))
                            continue;
                        var rect = (RectTransform)adopted.transform;
                        rect.SetParent(entries, false);
                        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        rect.anchoredPosition = new Vector2(0f, bottom - 6f);
                        bottom -= rect.rect.height + 6f;
                    }
                }
            }
        }

        /// <summary>
        /// The Auga menu entries sit at fixed positions. The top slot (player list in multiplayer, skip intro during
        /// the intro) only shows in some sessions and then touches the top divider while a large gap stays below the
        /// last entry. After Show() has toggled the entries, slide the whole block so the room above the first
        /// visible entry equals the room below the last one.
        /// </summary>
        [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
        public static class Menu_Show_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (!__instance.name.StartsWith("Auga"))
                    return;

                var entries = __instance.menuEntriesParent;
                if (entries == null && __instance.m_menuDialog != null)
                    entries = __instance.m_menuDialog.Find("MenuEntries") as RectTransform;
                if (entries == null)
                    return;
                var topDivider = entries.Find("DividerSmall") as RectTransform;
                var bottomDivider = entries.Find("DividerMedium") as RectTransform;
                if (topDivider == null || bottomDivider == null)
                    return;

                var block = new List<RectTransform>();
                float? first = null, last = null;
                foreach (RectTransform child in entries)
                {
                    if (child == topDivider || child == bottomDivider)
                        continue;
                    block.Add(child);
                    if (!child.gameObject.activeSelf || child.GetComponent<Button>() == null)
                        continue;
                    var y = child.localPosition.y;
                    first = first.HasValue ? Mathf.Max(first.Value, y) : y;
                    last = last.HasValue ? Mathf.Min(last.Value, y) : y;
                }
                if (!first.HasValue)
                    return;

                var roomAbove = topDivider.localPosition.y - first.Value;
                var roomBelow = last.Value - bottomDivider.localPosition.y;
                var shiftDown = (roomBelow - roomAbove) / 2f;
                if (Mathf.Abs(shiftDown) < 0.5f)
                    return;
                foreach (var child in block)
                {
                    child.anchoredPosition -= new Vector2(0f, shiftDown);
                }
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
                    if (filter == null || knownText.Key.Contains(filter.Filter))
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

