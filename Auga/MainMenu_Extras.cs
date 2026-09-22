using AugaUnity;
using System.Collections.Generic;
using System.Linq;
using Auga.Utilities;
using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Auga's look for the vanilla version label: Source Sans Pro Bold, brown, bottom right, no outline or shadow.
    /// Applied in Start, after the text initialized (restyling it earlier throws inside TextMeshPro).
    /// </summary>
    public class AugaVersionLabelStyle : MonoBehaviour
    {
        public const float FontSize = 16f;
        public const float Height = 30f;

        private void Start()
        {
            var text = GetComponent<TMP_Text>();
            if (text == null)
                return;
            try
            {
                if (AugaPanelRestyler.BoldFont != null)
                {
                    text.font = AugaPanelRestyler.BoldFont;
                    text.fontStyle &= ~FontStyles.Bold;
                }
                text.enableAutoSizing = false; // vanilla auto-sizes into its box; Auga's label is a fixed size
                text.fontSize = FontSize;
                text.color = AugaPanelRestyler.Brown3;
                text.alignment = TextAlignmentOptions.BottomRight;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Auga] Styling the version label failed: " + e.Message);
            }
            Destroy(this);
        }
    }

    /// <summary>
    /// Vanilla activates the select panel before it loads the profiles; Auga's list reads them in OnEnable, so they
    /// are loaded first. The portrait photo booth is created on the first show (it needs the profiles too).
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), "ShowCharacterSelection")]
    public static class FejdStartup_ShowCharacterSelection_Patch
    {
        public static void Prefix(FejdStartup __instance)
        {
            if (__instance.m_profiles == null)
                __instance.m_profiles = SaveSystem.GetAllPlayerProfiles();
            if (Auga.Assets.MainMenuPrefab == null || __instance.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true) != null)
                return;
            if (__instance.m_selectCharacterPanel == null || __instance.m_selectCharacterPanel.GetComponentInChildren<AugaCharacterSelect>(true) == null)
                return;
            var template = Auga.Assets.MainMenuPrefab.transform.Find("CharacterSelectPhotoBooth");
            if (template == null)
                return;
            var booth = Object.Instantiate(template.gameObject, __instance.transform, false);
            booth.name = "CharacterSelectPhotoBooth";
        }
    }

    /// <summary>The Auga portrait list follows every vanilla refresh of the character list.</summary>
    [HarmonyPatch(typeof(FejdStartup), "UpdateCharacterList")]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var select = __instance.m_selectCharacterPanel != null ? __instance.m_selectCharacterPanel.GetComponentInChildren<AugaCharacterSelect>(true) : null;
            if (select != null && select.gameObject.activeInHierarchy)
                select.UpdateCharacterList();
        }
    }

    /// <summary>A new character gets its portrait taken right away.</summary>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
    public static class FejdStartup_OnNewCharacterDone_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var booth = __instance.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true);
            if (booth != null)
                booth.StartCoroutine(TakePhoto(__instance, booth));
        }

        private static System.Collections.IEnumerator TakePhoto(FejdStartup startup, AugaCharacterSelectPhotoBooth booth)
        {
            if (startup.m_profileIndex < 0 || startup.m_profiles == null || startup.m_profileIndex >= startup.m_profiles.Count)
                yield break;
            yield return booth.TakePhoto(startup.m_profileIndex);
            startup.UpdateCharacterList();
        }
    }

    /// <summary>While the booth photographs the profiles the preview swap must not play the change effect.</summary>
    [HarmonyPatch(typeof(FejdStartup), "ClearCharacterPreview")]
    public static class FejdStartup_ClearCharacterPreview_Patch
    {
        public static bool Prefix(FejdStartup __instance)
        {
            if (!AugaCharacterSelectPhotoBooth.TakingPhotos)
                return true;
            if (__instance.m_playerInstance != null)
            {
                Object.Destroy(__instance.m_playerInstance);
                __instance.m_playerInstance = null;
            }
            return false;
        }
    }

    /// <summary>The merch store button and the "modded" notice are not part of the Auga main menu.</summary>
    [HarmonyPatch(typeof(FejdStartup), "SetupGui")]
    public static class FejdStartup_SetupGui_Patch
    {
        public static void Prefix(FejdStartup __instance)
        {
            // SetupGui looks the merch button up and points the menu's right navigation at it: gone before that
            if (__instance.m_merchStoreButtonParent == null)
                return;
            foreach (var button in __instance.m_merchStoreButtonParent.GetComponentsInChildren<Button>(true))
                Object.DestroyImmediate(button);
            __instance.m_merchStoreButtonParent.SetActive(false);
        }

        public static void Postfix(FejdStartup __instance)
        {
            if (__instance.m_moddedText != null)
                __instance.m_moddedText.SetActive(false);
        }
    }

    /// <summary>
    /// The rest of the Auga main menu: version label, small logo, the bottom-left MenuButtonSmall entries, the Auga
    /// changelog and the restyled user agreement window. Runs before FejdStartup.Awake (see MainMenu_Setup), so
    /// everything takes part in its localization and setup.
    /// </summary>
    public static class MainMenuExtras
    {
        private const float Margin = 40f;

        public static void Setup(FejdStartup startup)
        {
            var menu = startup.m_mainMenu != null ? startup.m_mainMenu.transform : startup.transform.Find("Menu");
            if (menu == null)
                return;
            // one broken step must not take FejdStartup.Awake (and the whole menu) down with it
            var versionHeight = Guard("version label", () => SetupVersionLabel(startup, menu));
            Guard("logo", () => SetupLogo(menu, versionHeight));
            Guard("bottom-left buttons", () => SetupBottomLeftButtons(startup, menu));
            Guard("changelog", () => SetupChangeLog(startup));
            Guard("user agreement", () => SetupEula(startup));
            Guard("credits", () => SetupCredits(startup));
            Guard("character select", () => SetupCharacterSelect(startup));
            Guard("new character", () => SetupNewCharacter(startup));
            Guard("loading screen", () => SetupLoading(startup));
            Guard("start game", () => MainMenuStartGame.Setup(startup));
            Guard("manage saves", () => SetupManageSaves(startup));
        }

        private static void Guard(string what, System.Action step)
        {
            try { step(); }
            catch (System.Exception e) { Debug.LogError($"[Auga] Main menu {what} setup failed: {e}"); }
        }

        private static T Guard<T>(string what, System.Func<T> step)
        {
            try { return step(); }
            catch (System.Exception e) { Debug.LogError($"[Auga] Main menu {what} setup failed: {e}"); return default; }
        }

        /// <summary>
        /// The vanilla version label, bottom right at (-40, 40); AugaVersionLabelStyle gives it Auga's look once the
        /// text has initialized. Returns its height (the logo sits above it).
        /// </summary>
        private static float SetupVersionLabel(FejdStartup startup, Transform menu)
        {
            var text = startup.m_versionLabel;
            if (text == null)
                return 0f;
            var height = AugaVersionLabelStyle.Height;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-Margin, Margin);
            rect.sizeDelta = new Vector2(300f, height);
            if (text.GetComponent<AugaVersionLabelStyle>() == null)
                text.gameObject.AddComponent<AugaVersionLabelStyle>();
            return height;
        }

        /// <summary>The small Auga logo, bottom right, right above the version label. Decoration only.</summary>
        private static void SetupLogo(Transform menu, float versionHeight)
        {
            if (Auga.Assets.AugaLogoSmall == null)
            {
                Auga.LogWarning("AugaLogoSmall prefab is not in the asset bundle; the main menu logo is skipped.");
                return;
            }
            var go = Object.Instantiate(Auga.Assets.AugaLogoSmall, menu, false);
            go.name = "AugaLogoSmall";
            foreach (var component in new Component[] { go.GetComponent<UIGamePad>(), go.GetComponent<EventTrigger>(), go.GetComponent<Toggle>() })
            {
                if (component != null) Object.DestroyImmediate(component);
            }
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-Margin, Margin + versionHeight + 6f);
        }

        /// <summary>Changelog / user agreement / player log entries as MenuButtonSmall buttons.</summary>
        private static void SetupBottomLeftButtons(FejdStartup startup, Transform menu)
        {
            var list = menu.Find("BottomLeftButtons");
            if (list == null)
                return;
            if (Auga.Assets.MenuButtonSmall == null)
            {
                Auga.LogWarning("MenuButtonSmall prefab is not in the asset bundle; the bottom-left menu entries stay vanilla.");
                return;
            }
            FejdStartup_Awake_Patch.ConvertMenuList(startup, list, Auga.Assets.MenuButtonSmall.transform);
        }

        /// <summary>
        /// Auga's changelog on the right side of the menu (a DividerMedium title and the log text in a scroll view),
        /// driven by the vanilla ChangeLog component and toggled by FejdStartup like the vanilla one.
        /// </summary>
        private static void SetupChangeLog(FejdStartup startup)
        {
            var vanilla = startup.m_changeLog;
            if (vanilla == null)
                return;
            if (Auga.Assets.ChangeLogPrefab == null)
            {
                Auga.LogWarning("ChangeLog prefab is not in the asset bundle; the changelog stays vanilla.");
                return;
            }
            var vanillaLog = vanilla.GetComponent<ChangeLog>();
            var canvas = vanilla.transform.parent;

            // the left-hand strip between the top of the screen and the bottom-left menu entries
            var container = (RectTransform)new GameObject("AugaChangeLog", typeof(RectTransform)).transform;
            container.SetParent(canvas, false);
            container.anchorMin = new Vector2(0f, 0f);
            container.anchorMax = new Vector2(0f, 1f);
            container.pivot = new Vector2(0f, 1f);
            container.anchoredPosition = new Vector2(Margin, -170f);
            container.sizeDelta = new Vector2(400f, -(170f + 190f));

            var log = Object.Instantiate(Auga.Assets.ChangeLogPrefab, container, false);
            log.name = "ChangeLog";
            var changeLog = log.GetComponent<ChangeLog>() ?? log.AddComponent<ChangeLog>();
            if (vanillaLog != null)
            {
                changeLog.m_changeLog = vanillaLog.m_changeLog;
                changeLog.m_xboxChangeLog = vanillaLog.m_xboxChangeLog;
                changeLog.m_playstationChangeLog = vanillaLog.m_playstationChangeLog;
                changeLog.m_switchChangeLog = vanillaLog.m_switchChangeLog;
            }
            changeLog.m_showPlayerLog = null;
            // the prefab wires its own scroll view and scrollbar; only the vanilla component's references are filled in
            if (changeLog.m_textField == null)
                changeLog.m_textField = log.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.GetComponentInParent<HorizontalDividerFitter>() == null);
            if (changeLog.m_scrollbar == null)
            {
                var scrollRect = log.GetComponentInChildren<ScrollRect>(true);
                changeLog.m_scrollbar = scrollRect != null ? scrollRect.verticalScrollbar : null;
            }

            log.SetActive(false);
            startup.m_changeLog = log;
            vanilla.SetActive(false);
            Object.Destroy(vanilla);
        }

        /// <summary>
        /// Auga's character selection (a portrait list instead of the vanilla left/right browser) replaces the vanilla
        /// SelectCharacter panel. FejdStartup keeps driving it: its button fields are re-pointed at the Auga buttons
        /// (which receive the vanilla click handlers), the single-character labels it writes go to an inactive dummy
        /// and the list itself is refreshed after every vanilla UpdateCharacterList.
        /// </summary>
        private static void SetupCharacterSelect(FejdStartup startup)
        {
            var vanilla = startup.m_selectCharacterPanel;
            if (vanilla == null)
                return;
            var template = Auga.Assets.SelectCharacterPrefab != null ? Auga.Assets.SelectCharacterPrefab.transform
                : Auga.Assets.MainMenuPrefab != null ? Auga.Assets.MainMenuPrefab.transform.Find("CharacterSelection/SelectCharacter") : null;
            if (template == null)
            {
                Auga.LogWarning("The bundle has no SelectCharacter prefab; the character selection stays vanilla.");
                return;
            }

            var panel = Object.Instantiate(template.gameObject, vanilla.transform.parent, false);
            panel.name = vanilla.name;
            panel.transform.SetSiblingIndex(vanilla.transform.GetSiblingIndex());
            panel.SetActive(vanilla.activeSelf);
            var v = vanilla.transform;
            var a = panel.transform;
            var missing = new List<string>();

            Button VanillaButton(string path) => v.Find(path)?.GetComponent<Button>();
            Button Wire(string augaPath, Button source, string what) => WireButton(a, augaPath, source, what, missing);

            var start = Wire("Panel/Start", startup.m_csStartButton, "Start");
            var newButton = Wire("Panel/Inset/NewButton", startup.m_csNewButton, "New");
            var newBig = Wire("Panel/Inset/NewButtonBig", startup.m_csNewBigButton, "New (empty list)");
            var remove = Wire("Panel/Inset/RemoveButton", startup.m_csRemoveButton, "Remove");
            Wire("Panel/Back", VanillaButton("BottomWindow/Back"), "Back");
            Wire("Panel/ManageSaves", VanillaButton("BottomWindow/ManageSaves"), "Manage saves");
            Wire("RemoveCharacterDialog/ButtonYes", VanillaButton("RemoveCharacterDialog/Dialog/ButtonYes"), "Remove dialog: yes");
            Wire("RemoveCharacterDialog/ButtonNo", VanillaButton("RemoveCharacterDialog/Dialog/ButtonNo"), "Remove dialog: no");
            if (start != null) startup.m_csStartButton = start;
            if (newButton != null) startup.m_csNewButton = newButton;
            if (newBig != null) startup.m_csNewBigButton = newBig;
            if (remove != null) startup.m_csRemoveButton = remove;

            // vanilla browses one character at a time with arrows and a name / save-source label; the Auga list
            // shows all of them, so those fields point at an inactive dummy that absorbs the writes
            var dummy = a.Find("Panel/DummyObjects/Dummy");
            var dummyButton = dummy != null ? dummy.GetComponent<Button>() : null;
            var dummyText = dummy != null ? dummy.GetComponent<TMP_Text>() : null;
            if (dummyButton != null) { startup.m_csLeftButton = dummyButton; startup.m_csRightButton = dummyButton; }
            else missing.Add("dummy button for the left/right arrows");
            if (dummyText != null) { startup.m_csName = dummyText; startup.m_csFileSource = dummyText; }
            else missing.Add("dummy text for the character name / save source");
            var sourceInfo = a.Find("Panel/SourceInfo/Text")?.GetComponent<TMP_Text>();
            if (sourceInfo != null) startup.m_csSourceInfo = sourceInfo;
            else missing.Add("SourceInfo/Text");

            var dialog = a.Find("RemoveCharacterDialog");
            if (dialog != null)
            {
                startup.m_removeCharacterDialog = dialog.gameObject;
                CopyLabel(v.Find("RemoveCharacterDialog/Dialog/topic")?.GetComponent<TMP_Text>(), dialog.Find("Topic")?.GetComponent<TMP_Text>());
                var name = dialog.Find("Text")?.GetComponent<TMP_Text>();
                if (name != null) startup.m_removeCharacterName = name;
                else missing.Add("RemoveCharacterDialog/Text");
            }
            else missing.Add("RemoveCharacterDialog");

            var select = panel.GetComponentInChildren<AugaCharacterSelect>(true);
            if (select != null)
            {
                if (select.SourceInfoContent == null) select.SourceInfoContent = sourceInfo;   // not set in the prefab
                if (select.SourceInfoPanel == null) select.SourceInfoPanel = a.Find("Panel/SourceInfo")?.gameObject;
                if (select.CharacterPortraitPrefab == null) missing.Add("AugaCharacterSelect.CharacterPortraitPrefab");
                if (select.RenderTexture == null) missing.Add("AugaCharacterSelect.RenderTexture");
                if (select.ScrollBar == null) missing.Add("AugaCharacterSelect.ScrollBar");
            }
            else missing.Add("AugaCharacterSelect component");

            vanilla.SetActive(false);
            Object.Destroy(vanilla);
            startup.m_selectCharacterPanel = panel;
            if (missing.Count > 0)
                Debug.LogWarning("[Auga] Character select: " + string.Join("; ", missing));
        }

        /// <summary>
        /// The manage saves dialog: the generic restyle for its window, the vanilla World / Character tab buttons
        /// replaced by a clone of the settings screen's tab bar (the vanilla TabHandler keeps driving them), and
        /// Auga fonts and colours for the save rows, which vanilla instantiates from two inactive templates in the list.
        /// </summary>
        private static void SetupManageSaves(FejdStartup startup)
        {
            var panel = startup.m_manageSavesMenu != null ? startup.m_manageSavesMenu.transform.Find("Panel") : null;
            if (panel == null)
                return;
            ReplaceManageSavesTabs(panel);
            AugaPanelRestyler.Restyle(panel, new RestyleOptions
            {
                Titles = { "topic" },
                DetectHeaders = false,
                Skip = { "AugaTabButtons", "SaveElement", "BackupSaveElement", "ListRoot" },
            });
            var rowOptions = new RestyleOptions { ReplaceBackground = false, ReplaceButtons = false, ReplaceScrollbars = false, DetectHeaders = false };
            foreach (var path in new[] { "SaveList/SaveElement", "SaveList/BackupSaveElement" })
            {
                var template = panel.Find(path);
                if (template == null)
                    continue;
                AugaPanelRestyler.Restyle(template, rowOptions);
                // the selected row: Auga's flat blue highlight (as in the character list) instead of vanilla's orange
                foreach (var image in template.GetComponentsInChildren<Image>(true))
                {
                    if (image.name != "selected") continue;
                    image.sprite = null;
                    image.color = AugaPanelRestyler.LightBlue;
                }
            }
            // vanilla underlines its tabs with a thin strip; the Auga tab bar brings its own dividers
            foreach (RectTransform child in panel)
            {
                var image = child.GetComponent<Image>();
                if (image != null && child.name.StartsWith("bkg") && child.rect.height <= 6f)
                    image.enabled = false;
            }
            // the "please wait" sign shown while the save list loads (a wooden plank elsewhere under the start GUI)
            var pleaseWait = startup.m_manageSavesMenu.pleaseWait != null ? startup.m_manageSavesMenu.pleaseWait.transform.Find("panel") : null;
            if (pleaseWait != null)
            {
                var plank = pleaseWait.GetComponent<Image>();
                if (plank != null && plank.sprite != null) plank.enabled = false;
                AugaPanelRestyler.Restyle(pleaseWait, new RestyleOptions { DetectHeaders = false, BackgroundOverhang = 0f });
            }
        }

        /// <summary>
        /// The tab buttons of a vanilla TabHandler become Auga settings-style tabs: a clone of the settings tab bar
        /// (dividers left and right, "Selected" overlay per tab) placed at the vanilla tabs' height, each vanilla
        /// button replaced by a clone of the bar's tab template carrying the vanilla label, click handler and gamepad
        /// hint. The TabHandler's tab list is re-pointed, so it keeps working unchanged.
        /// </summary>
        private static void ReplaceManageSavesTabs(Transform panel)
        {
            var tabHandler = panel.GetComponent<TabHandler>();
            var template = Auga.Assets.SettingsPrefab != null ? Auga.Assets.SettingsPrefab.transform.Find("panel/TabButtons") : null;
            if (tabHandler == null || template == null || tabHandler.m_tabs.All(t => t.m_button == null))
                return;

            var bar = Object.Instantiate(template.gameObject, panel, false);
            bar.name = "AugaTabButtons";
            Object.DestroyImmediate(bar.GetComponent<TabHandler>());   // the panel's own TabHandler drives the tabs
            var tabsParent = bar.transform.Find("Tabs");
            var tabTemplate = tabsParent != null && tabsParent.childCount > 0 ? tabsParent.GetChild(0) : null;
            if (tabTemplate == null)
            {
                Object.DestroyImmediate(bar);
                return;
            }

            // the bar sits at the vanilla tabs' height (their vertical centre, measured in the panel's space)
            var panelRect = (RectTransform)panel;
            var barRect = (RectTransform)bar.transform;
            var first = (RectTransform)tabHandler.m_tabs.First(t => t.m_button != null).m_button.transform;
            var corners = new Vector3[4];
            first.GetWorldCorners(corners);
            var centre = panel.InverseTransformPoint((corners[0] + corners[2]) * 0.5f);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(0f, centre.y - panelRect.rect.yMax);
            bar.transform.SetSiblingIndex(first.GetSiblingIndex());

            var map = new Dictionary<Object, Object>();
            var doomed = new List<GameObject>();
            foreach (var tab in tabHandler.m_tabs)
            {
                var old = tab.m_button;
                if (old == null)
                    continue;
                var go = Object.Instantiate(tabTemplate.gameObject, tabsParent, false);
                go.name = old.name;
                var oldLabel = old.transform.Find("Text")?.GetComponent<TMP_Text>() ?? old.GetComponentInChildren<TMP_Text>(true);
                var token = AugaPanelRestyler.RawText(oldLabel);
                foreach (var path in new[] { "Text", "Selected/Text" })
                {
                    var label = go.transform.Find(path)?.GetComponent<TMP_Text>();
                    if (label == null || token == null)
                        continue;
                    label.text = token;
                    if (oldLabel.GetComponent<Localize>() != null && label.GetComponent<Localize>() == null)
                        label.gameObject.AddComponent<Localize>();
                }
                var button = go.GetComponent<Button>();
                button.onClick = old.onClick;
                button.interactable = old.interactable;
                var pad = old.GetComponent<UIGamePad>();
                if (pad != null)
                    SerializedFieldHelper.CopyMissingFields(go.AddComponent<UIGamePad>(), pad, go.transform);
                go.SetActive(true);
                tab.m_button = button;
                map[old.gameObject] = go;
                map[old] = button;
                map[old.transform] = go.transform;
                doomed.Add(old.gameObject);
            }
            Object.DestroyImmediate(tabTemplate.gameObject);
            AugaPanelRestyler.Repoint(panel.root, map);
            // gone right away: the restyle that follows must not see (and replace) the vanilla tab buttons
            foreach (var go in doomed)
                Object.DestroyImmediate(go);
        }

        /// <summary>
        /// The Auga button at <paramref name="augaPath"/> takes over a vanilla button: its click handlers, interactable
        /// state and label token (FejdStartup localizes the screen after Awake).
        /// </summary>
        internal static Button WireButton(Transform auga, string augaPath, Button source, string what, List<string> missing)
        {
            var button = auga.Find(augaPath)?.GetComponent<Button>();
            if (button == null) { missing.Add(what + ": the Auga prefab has no " + augaPath); return null; }
            if (source == null) { missing.Add(what + ": no vanilla button to take the click handler from"); return button; }
            button.onClick = source.onClick;
            button.interactable = source.interactable;
            CopyLabel(source.transform.Find("Text")?.GetComponent<TMP_Text>(), button.transform.Find("Label")?.GetComponent<TMP_Text>());
            return button;
        }

        internal static void CopyLabel(TMP_Text source, TMP_Text target)
        {
            if (source == null || target == null) return;
            target.text = AugaPanelRestyler.RawText(source);   // the token; FejdStartup localizes the screen after Awake
        }

        /// <summary>
        /// Auga's new character panel (name, sex toggles, colour sliders and a portrait grid for hair and beard)
        /// replaces the vanilla one. FejdStartup keeps driving it: the name field, Done / Cancel and the "name exists"
        /// label are re-pointed and the vanilla click handlers move over. Its PlayerCustomizaton gets what only the
        /// vanilla scene holds (the "no hair" / "no beard" items) and a dummy for the labels Auga does not show.
        /// </summary>
        private static void SetupNewCharacter(FejdStartup startup)
        {
            var vanilla = startup.m_newCharacterPanel;
            if (vanilla == null)
                return;
            var template = Auga.Assets.NewCharacterPanelPrefab != null ? Auga.Assets.NewCharacterPanelPrefab.transform
                : Auga.Assets.MainMenuPrefab != null ? Auga.Assets.MainMenuPrefab.transform.Find("CharacterSelection/NewCharacterPanel") : null;
            if (template == null)
            {
                Auga.LogWarning("The bundle has no NewCharacterPanel prefab; the new character screen stays vanilla.");
                return;
            }

            var panel = Object.Instantiate(template.gameObject, vanilla.transform.parent, false);
            panel.name = vanilla.name;
            panel.transform.SetSiblingIndex(vanilla.transform.GetSiblingIndex());
            panel.SetActive(vanilla.activeSelf);
            var a = panel.transform;
            var missing = new List<string>();

            var done = WireButton(a, "Panel/Done", startup.m_csNewCharacterDone, "Done", missing);
            var cancel = WireButton(a, "Panel/Cancel", startup.m_csNewCharacterCancel, "Cancel", missing);
            if (done != null) startup.m_csNewCharacterDone = done;
            if (cancel != null) startup.m_csNewCharacterCancel = cancel;

            var name = a.Find("Panel/Content/CharacterName")?.GetComponent<GuiInputField>();
            if (name != null)
            {
                var old = startup.m_csNewCharacterName;
                if (old != null)
                {
                    name.characterLimit = old.characterLimit;
                    name.characterValidation = old.characterValidation;
                    name.contentType = old.contentType;
                }
                // Auga's field submits on Enter (GuiInputFieldSubmit); that is Done, once the name is long enough
                var submit = name.GetComponent<GuiInputFieldSubmit>();
                if (submit != null)
                {
                    submit.m_onSubmit = _ =>
                    {
                        var button = startup.m_csNewCharacterDone;
                        if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                            button.onClick.Invoke();
                    };
                }
                startup.m_csNewCharacterName = name;
            }
            else missing.Add("Panel/Content/CharacterName (GuiInputField)");

            var error = a.Find("Panel/Content/NameExistsWarning");
            if (error != null)
            {
                error.gameObject.SetActive(false);
                startup.m_newCharacterError = error.gameObject;
            }
            else missing.Add("Panel/Content/NameExistsWarning");

            var custom = panel.GetComponent<PlayerCustomizaton>();
            var vanillaCustom = vanilla.GetComponent<PlayerCustomizaton>();
            if (custom != null)
            {
                if (vanillaCustom != null)
                {
                    custom.m_noHair = vanillaCustom.m_noHair;
                    custom.m_noBeard = vanillaCustom.m_noBeard;
                    custom.m_hairToolTier = vanillaCustom.m_hairToolTier;
                }
                else missing.Add("vanilla PlayerCustomizaton (the no-hair / no-beard items)");
                // vanilla names the current hair and beard in labels and toggles a beard panel; Auga's portrait grid
                // shows the selection itself, so those writes go to the inactive dummy
                var dummy = a.Find("Panel/DummyObjects/Dummy") as RectTransform;
                var dummyText = dummy != null ? dummy.GetComponent<TMP_Text>() : null;
                if (custom.m_selectedHair == null) custom.m_selectedHair = dummyText;
                if (custom.m_selectedBeard == null) custom.m_selectedBeard = dummyText;
                if (custom.m_beardPanel == null) custom.m_beardPanel = dummy;
                if (custom.m_selectedHair == null || custom.m_selectedBeard == null || custom.m_beardPanel == null)
                    missing.Add("a dummy for PlayerCustomizaton's hair / beard labels");
                if (custom.m_skinHue == null) missing.Add("PlayerCustomizaton.m_skinHue");
                if (custom.m_hairTone == null) missing.Add("PlayerCustomizaton.m_hairTone");
                if (custom.m_hairLevel == null) missing.Add("PlayerCustomizaton.m_hairLevel");
                if (custom.m_maleToggle == null || custom.m_femaleToggle == null) missing.Add("PlayerCustomizaton sex toggles");
            }
            else missing.Add("PlayerCustomizaton component");

            var portraits = panel.GetComponentInChildren<CharacterPortraitsController>(true);
            if (portraits == null) missing.Add("CharacterPortraitsController");
            else
            {
                if (portraits.PortraitPrefab == null) missing.Add("CharacterPortraitsController.PortraitPrefab");
                if (portraits.RenderTexture == null) missing.Add("CharacterPortraitsController.RenderTexture");
            }

            vanilla.SetActive(false);
            Object.Destroy(vanilla);
            startup.m_newCharacterPanel = panel;
            if (missing.Count > 0)
                Debug.LogWarning("[Auga] New character: " + string.Join("; ", missing));
        }

        /// <summary>
        /// The loading screen FejdStartup shows while the world scene loads. The menu Animator fades the vanilla
        /// Loading object's CanvasGroup in and out, so that object stays; only its content is swapped for the
        /// MainMenu prefab's Loading child (a black screen with a divider and Auga's own "Loading" token), and the
        /// animator rebinds in case its clips also reach the children.
        /// </summary>
        private static void SetupLoading(FejdStartup startup)
        {
            var loading = startup.m_loading;
            var template = Auga.Assets.MainMenuPrefab != null ? Auga.Assets.MainMenuPrefab.transform.Find("Loading") : null;
            if (loading == null || template == null)
                return;
            foreach (var child in loading.transform.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject);   // gone before the rebind: bindings resolve by name
            var content = Object.Instantiate(template.gameObject, loading.transform, false);
            foreach (var child in content.transform.Cast<Transform>().ToList())
                child.SetParent(loading.transform, false);
            Object.DestroyImmediate(content);
            if (startup.m_menuAnimator != null)
                startup.m_menuAnimator.Rebind();
        }

        /// <summary>The credits: Auga fonts and colours, a medium Auga button for Back; layout and background stay vanilla.</summary>
        private static void SetupCredits(FejdStartup startup)
        {
            if (startup.m_creditsPanel == null)
                return;
            var panel = startup.m_creditsPanel.transform;
            AugaPanelRestyler.Restyle(panel, new RestyleOptions
            {
                ReplaceBackground = false,
                DetectHeaders = false,
                ButtonPrefab = Auga.Assets.ButtonMedium,
            });
            // vanilla colours every credit line the same warm yellow; Auga's palette: gold section titles (the
            // top-level texts), light names (their child texts)
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<Selectable>() != null) continue;
                var isTitle = text.transform.parent == null || text.transform.parent.GetComponent<TMP_Text>() == null;
                var font = isTitle ? AugaPanelRestyler.BoldFont : AugaPanelRestyler.RegularFont;
                if (font != null) text.font = font;
                text.fontStyle &= ~FontStyles.Bold;
                text.color = AugaPanelRestyler.WithAlpha(isTitle ? AugaPanelRestyler.BrightGold : AugaPanelRestyler.Brown1, text.color.a);
            }
        }

        /// <summary>The user agreement window, restyled with the generic panel restyler.</summary>
        private static void SetupEula(FejdStartup startup)
        {
            var window = startup.m_eulaWindow != null ? startup.m_eulaWindow.m_window : null;
            if (window == null)
                return;
            AugaPanelRestyler.Restyle(window.transform, new RestyleOptions
            {
                Titles = { "HeaderText" },
                Headers = { "codeOfConductHeader", "ProhibitedBehavior", "ReportingViolations", "EnforcementAndBans", "BanAppeals" },
            });
        }
    }
}
