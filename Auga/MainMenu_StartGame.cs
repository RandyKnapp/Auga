using System;
using System.Collections.Generic;
using System.Linq;
using Auga.Utilities;
using AugaUnity;
using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Auga's StartGame prefab (world list, server list, new world / remove world / add server dialogs) replaces the
    /// vanilla start game panel. FejdStartup and a fresh ServerListGui keep driving it: their fields are re-pointed
    /// at the Auga objects, the vanilla click handlers move over, and the pieces the Auga prefab does not carry (the
    /// server options dialog, the PlayStation-only toggles, the tooltip anchors, the favourite / move up / move down
    /// buttons of the server list) are adopted from vanilla or built from Auga widgets.
    /// </summary>
    public static class MainMenuStartGame
    {
        private const float RowHeight = 30f;
        private const float SideGap = 30f;   // tooltip anchors sit this far right of the panel

        public static void Setup(FejdStartup startup)
        {
            var vanilla = startup.m_startGamePanel;
            var template = Auga.Assets.StartGamePrefab != null ? Auga.Assets.StartGamePrefab.transform
                : Auga.Assets.MainMenuPrefab != null ? Auga.Assets.MainMenuPrefab.transform.Find("StartGame") : null;
            if (vanilla == null || template == null)
            {
                Auga.LogWarning("The bundle has no StartGame prefab; the start game screen stays vanilla.");
                return;
            }

            var root = Object.Instantiate(template.gameObject, vanilla.transform.parent, false);
            root.name = vanilla.name;
            root.transform.SetSiblingIndex(vanilla.transform.GetSiblingIndex());
            root.SetActive(vanilla.activeSelf);
            var v = vanilla.transform;
            var a = root.transform;
            var missing = new List<string>();
            Button Wire(string augaPath, string vanillaPath, string what) =>
                MainMenuExtras.WireButton(a, augaPath, v.Find(vanillaPath)?.GetComponent<Button>(), what, missing);

            SetupTabs(v, a, missing);
            SetupWorldPanel(startup, v, a, Wire, missing);
            SetupDialogs(startup, v, a, Wire, missing);
            SetupServerList(startup, v, a, Wire, missing);

            // the server options dialog has no Auga counterpart yet: the vanilla one moves over. It goes after Panel,
            // which FejdStartup expects as the first child (the tab panel).
            var serverOptions = v.Find("StartGui_ServerOptions");
            if (serverOptions != null)
            {
                serverOptions.SetParent(a, false);
                RestyleServerOptions(serverOptions, missing);
            }
            else missing.Add("StartGui_ServerOptions (the vanilla server options dialog)");

            startup.m_startGamePanel = root;
            vanilla.SetActive(false);
            Object.Destroy(vanilla);
            if (missing.Count > 0)
                Debug.LogWarning("[Auga] Start game: " + string.Join("; ", missing));
        }

        // ------------------------------------------------------------------ world modifiers dialog

        private const float ModifiersWidth = 340f;
        private const float DialogWidth = 620f;

        /// <summary>
        /// The vanilla world modifiers dialog rebuilt with Auga widgets: the panel restyler for the title, the section
        /// headers, the background and the preset buttons (their KeyButton data moves along), Auga slider and checkbox
        /// rows for the modifiers (their KeySlider / KeyToggle scripts move along), a settings button for "reset to
        /// normal", and a vertical layout with generous margins in place of the vanilla fixed positions.
        /// </summary>
        private static void RestyleServerOptions(Transform serverOptions, List<string> missing)
        {
            var panel = serverOptions.Find("panel");
            var presets = panel != null ? panel.Find("Presets") : null;
            var modifiers = panel != null ? panel.Find("Modifiers") : null;
            if (panel == null || presets == null || modifiers == null)
            {
                missing.Add("world modifiers: panel / Presets / Modifiers");
                return;
            }

            // the section headers leave their groups: they become rows of the dialog
            var presetsHeader = presets.Find("Presets Text");
            var customizeHeader = modifiers.Find("Customize Text");
            if (presetsHeader != null) presetsHeader.SetParent(panel, false);
            if (customizeHeader != null) customizeHeader.SetParent(panel, false);

            // preset key data: the restyler swaps the buttons, KeyButton rides on the vanilla ones
            var keys = presets.GetComponentsInChildren<KeyButton>(true)
                .Where(k => k.gameObject.name != "Default")
                .Select(k => (k.gameObject.name, k.m_toolTipLabel, k.m_toolTip, k.m_preset, k.m_keys)).ToList();
            var sliders = modifiers.GetComponentsInChildren<KeySlider>(true).ToList();
            var toggles = modifiers.GetComponentsInChildren<KeyToggle>(true).ToList();

            AugaPanelRestyler.Restyle(panel, new RestyleOptions
            {
                Titles = { "topic" },
                Headers = { "Presets Text", "Customize Text" },
                DetectHeaders = false,
                Skip = { "OLD", "Modifiers", "Default" },
                RestyleSliders = false,
                RestyleToggles = false,
                BackgroundOverhang = 0f,
            });
            foreach (var (name, label, tip, preset, list) in keys)
            {
                var button = presets.Find(name);
                if (button == null || button.GetComponent<KeyButton>() != null)
                    continue;
                var key = button.gameObject.AddComponent<KeyButton>();
                key.m_toolTipLabel = label;
                key.m_toolTip = tip;
                key.m_preset = preset;
                key.m_keys = list;
            }

            var defaultButton = presets.Find("Default");
            var reset = defaultButton != null ? ReplaceWithButton(defaultButton.gameObject, Auga.Assets.ButtonSettings) : null;
            if (reset == null) missing.Add("world modifiers: Presets/Default (or the ButtonSettings prefab)");

            BuildModifierRows(modifiers, sliders, toggles, missing);
            LayoutServerOptions(panel, presets, modifiers, reset, presetsHeader, customizeHeader);
        }

        /// <summary>An Auga button in place of a vanilla one, keeping click handlers, label and KeyButton data.</summary>
        private static Button ReplaceWithButton(GameObject old, GameObject prefab)
        {
            if (prefab == null)
                return null;
            var go = Object.Instantiate(prefab, old.transform.parent, false);
            go.name = old.name;
            go.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            var button = go.GetComponent<Button>();
            var oldButton = old.GetComponent<Button>();
            if (oldButton != null)
            {
                button.onClick = oldButton.onClick;
                button.interactable = oldButton.interactable;
            }
            MainMenuExtras.CopyLabel(old.transform.Find("Text")?.GetComponent<TMP_Text>(), go.transform.Find("Label")?.GetComponent<TMP_Text>());
            var key = old.GetComponent<KeyButton>();
            if (key != null)
            {
                var copy = go.AddComponent<KeyButton>();
                copy.m_toolTipLabel = key.m_toolTipLabel;
                copy.m_toolTip = key.m_toolTip;
                copy.m_preset = key.m_preset;
                copy.m_keys = key.m_keys;
            }
            old.SetActive(false);
            Object.Destroy(old);
            return button;
        }

        /// <summary>
        /// The modifier rows as Auga widgets, in a single column: one LabeledSliderWithValue per vanilla slider group
        /// (label, value label, slider) and one LabeledCheckbox per vanilla toggle. The KeySlider / KeyToggle scripts
        /// that ServerOptionsGUI reads are re-created on the Auga widgets with the vanilla data.
        /// </summary>
        private static void BuildModifierRows(Transform modifiers, List<KeySlider> sliders, List<KeyToggle> toggles, List<string> missing)
        {
            if (Auga.Assets.LabeledSliderWithValue == null || Auga.Assets.LabeledCheckbox == null)
            {
                missing.Add("world modifiers: LabeledSliderWithValue / LabeledCheckbox prefabs");
                return;
            }
            foreach (var slider in sliders)
            {
                var vanillaRow = slider.transform.parent;
                var go = Object.Instantiate(Auga.Assets.LabeledSliderWithValue, modifiers, false);
                go.name = vanillaRow.name;
                var newSlider = go.GetComponentInChildren<Slider>(true);
                var value = go.transform.Find("TMP ValueLabel")?.GetComponent<TMP_Text>();
                MainMenuExtras.CopyLabel(vanillaRow.Find("label")?.GetComponent<TMP_Text>(), go.transform.Find("TMP Label")?.GetComponent<TMP_Text>());
                if (value != null) value.text = string.Empty;
                var key = newSlider.gameObject.AddComponent<KeySlider>();
                key.m_nameLabel = value;             // KeySlider names the selected setting there
                key.m_toolTipLabel = slider.m_toolTipLabel;
                key.m_toolTip = slider.m_toolTip;
                key.m_defaultIndex = slider.m_defaultIndex;
                key.m_modifier = slider.m_modifier;
                key.m_settings = slider.m_settings;
                var sfx = slider.GetComponent<SliderSfx>();
                if (sfx != null)
                    SerializedFieldHelper.CopyMissingFields(newSlider.gameObject.AddComponent<SliderSfx>(), sfx, newSlider.transform);
                go.SetActive(vanillaRow.gameObject.activeSelf);
                vanillaRow.gameObject.SetActive(false);
                Object.Destroy(vanillaRow.gameObject);
            }
            foreach (var toggle in toggles)
            {
                var vanillaToggle = toggle.transform;
                var go = Object.Instantiate(Auga.Assets.LabeledCheckbox, modifiers, false);
                go.name = vanillaToggle.name;
                MainMenuExtras.CopyLabel(vanillaToggle.Find("Label")?.GetComponent<TMP_Text>(), go.transform.Find("TMP Label")?.GetComponent<TMP_Text>());
                var key = go.AddComponent<KeyToggle>();
                key.m_toolTipLabel = toggle.m_toolTipLabel;
                key.m_toolTip = toggle.m_toolTip;
                key.m_defaultOn = toggle.m_defaultOn;
                key.m_enabledKey = toggle.m_enabledKey;
                go.SetActive(vanillaToggle.gameObject.activeSelf);
                vanillaToggle.gameObject.SetActive(false);
                Object.Destroy(vanillaToggle.gameObject);
            }

            var rect = (RectTransform)modifiers;
            rect.sizeDelta = new Vector2(ModifiersWidth, rect.sizeDelta.y);
            var column = modifiers.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 10f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = false;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            var element = modifiers.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ModifiersWidth;
            element.minWidth = ModifiersWidth;
            element.flexibleWidth = 0f;   // the rows inside expand to the column, the column itself must not
        }

        /// <summary>
        /// The dialog as a vertical layout with wide margins: title, presets header, the preset grid with the reset
        /// button beneath, customize header, the modifier column, then Cancel / Done. Backgrounds and the tooltip box
        /// stay out of the layout; the tooltip box moves to the right of the dialog.
        /// </summary>
        private static void LayoutServerOptions(Transform panel, Transform presets, Transform modifiers, Button reset, Transform presetsHeader, Transform customizeHeader)
        {
            var rect = (RectTransform)panel;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(DialogWidth, rect.sizeDelta.y);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 64, 64);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;     // rows take their preferred sizes: texts, layout groups, layout elements
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // the preset grid, three per row, the reset button under it
            var grid = new GameObject("Grid", typeof(RectTransform)).transform;
            grid.SetParent(presets, false);
            foreach (var button in presets.GetComponentsInChildren<Button>(true))
            {
                if (button.transform.parent == presets && button.gameObject.activeSelf && (reset == null || button != reset))
                    button.transform.SetParent(grid, false);
            }
            var cells = grid.gameObject.AddComponent<GridLayoutGroup>();
            cells.cellSize = new Vector2(140f, 35f);
            cells.spacing = new Vector2(14f, 12f);
            cells.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cells.constraintCount = 3;
            cells.childAlignment = TextAnchor.MiddleCenter;
            var presetsLayout = presets.gameObject.AddComponent<VerticalLayoutGroup>();
            presetsLayout.spacing = 16f;
            presetsLayout.childAlignment = TextAnchor.UpperCenter;
            presetsLayout.childControlWidth = true;
            presetsLayout.childControlHeight = true;
            presetsLayout.childForceExpandWidth = false;
            presetsLayout.childForceExpandHeight = false;
            grid.SetAsFirstSibling();
            if (reset != null)
            {
                reset.transform.SetAsLastSibling();
                KeepSize(reset.transform);
            }

            // Cancel / Done side by side
            var buttons = new GameObject("Buttons", typeof(RectTransform)).transform;
            buttons.SetParent(panel, false);
            foreach (var name in new[] { "Cancel", "Done" })
            {
                var button = panel.Find(name);
                if (button == null) continue;
                button.SetParent(buttons, false);
                KeepSize(button);
            }
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 40f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            // headers: a fixed width, room above and below
            foreach (var header in new[] { presetsHeader, customizeHeader })
            {
                if (header == null) continue;
                var top = TopLevel(header, panel);
                var element = top.GetComponent<LayoutElement>() ?? top.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = DialogWidth - 160f;
                element.preferredHeight = 30f;
            }

            // out of the layout: the backgrounds (drawn first, so under the rows), the tooltip box (to the right of
            // the dialog) and the disabled leftovers
            foreach (Transform child in panel.Cast<Transform>().ToList())
            {
                if (child.name != "bkg" && child.name != "AugaPanelBackground" && child.name != "Tooltips" && child.name != "OLD")
                    continue;
                (child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>()).ignoreLayout = true;
                if (child.name == "Tooltips" || child.name == "OLD") child.SetAsLastSibling();
                else child.SetAsFirstSibling();
            }
            var tooltips = panel.Find("Tooltips") as RectTransform;
            if (tooltips != null)
            {
                tooltips.anchorMin = tooltips.anchorMax = new Vector2(1f, 0.5f);
                tooltips.pivot = new Vector2(0f, 0.5f);
                tooltips.anchoredPosition = new Vector2(30f, 0f);
            }

            var topic = panel.Find("topic");
            var order = new List<Transform>
            {
                topic, Spacer(panel, 16f),
                presetsHeader != null ? TopLevel(presetsHeader, panel) : null, Spacer(panel, 16f),
                presets, Spacer(panel, 16f),
                customizeHeader != null ? TopLevel(customizeHeader, panel) : null, Spacer(panel, 16f),
                modifiers, Spacer(panel, 20f),
                buttons,
            };
            var index = panel.Cast<Transform>().Count(c => c.name == "bkg" || c.name == "AugaPanelBackground");   // after the backgrounds
            foreach (var item in order)
            {
                if (item != null) item.SetSiblingIndex(index++);
            }
        }

        /// <summary>A widget with no layout data of its own keeps its current size inside a layout group.</summary>
        private static void KeepSize(Transform t)
        {
            var rect = (RectTransform)t;
            var element = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = rect.sizeDelta.x;
            element.preferredHeight = rect.sizeDelta.y;
        }

        private static Transform Spacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return go.transform;
        }

        private static Transform TopLevel(Transform t, Transform panel)
        {
            while (t.parent != null && t.parent != panel)
                t = t.parent;
            return t;
        }

        // ------------------------------------------------------------------ host / join tabs

        private static void SetupTabs(Transform v, Transform a, List<string> missing)
        {
            var tabs = a.Find("Panel")?.GetComponent<TabHandler>();
            var vanillaTabs = v.Find("Panel")?.GetComponent<TabHandler>();
            if (tabs == null || vanillaTabs == null)
            {
                missing.Add("Panel TabHandler (the Host / Join tabs)");
                return;
            }
            CopyTabSettings(vanillaTabs, tabs);
            // OnSelectWorldTab / OnServerListTab ride along, wherever vanilla keeps them (tab entry or button)
            for (var i = 0; i < tabs.m_tabs.Count && i < vanillaTabs.m_tabs.Count; i++)
            {
                tabs.m_tabs[i].m_onClick = vanillaTabs.m_tabs[i].m_onClick;
                if (tabs.m_tabs[i].m_button != null && vanillaTabs.m_tabs[i].m_button != null)
                    tabs.m_tabs[i].m_button.onClick = vanillaTabs.m_tabs[i].m_button.onClick;
            }
            // only the default page starts active, however the prefab was saved (an active join panel would run
            // the server list before Steam matchmaking exists)
            foreach (var tab in tabs.m_tabs)
            {
                if (tab.m_page != null) tab.m_page.gameObject.SetActive(tab.m_default);
            }
        }

        private static void CopyTabSettings(TabHandler from, TabHandler to)
        {
            to.m_cycling = from.m_cycling;
            to.m_tabKeyInput = from.m_tabKeyInput;
            to.m_keybaordInput = from.m_keybaordInput;
            to.m_keyboardNavigateLeft = from.m_keyboardNavigateLeft;
            to.m_keyboardNavigateRight = from.m_keyboardNavigateRight;
            to.m_gamepadInput = from.m_gamepadInput;
            to.m_gamepadNavigateLeft = from.m_gamepadNavigateLeft;
            to.m_gamepadNavigateRight = from.m_gamepadNavigateRight;
        }

        // ------------------------------------------------------------------ world panel

        private static void SetupWorldPanel(FejdStartup startup, Transform v, Transform a, Func<string, string, string, Button> wire, List<string> missing)
        {
            var world = a.Find("Panel/WorldPanel");
            var vanillaWorld = v.Find("Panel/WorldPanel");
            if (world == null)
            {
                missing.Add("Panel/WorldPanel");
                return;
            }
            startup.m_worldListPanel = world.gameObject;

            var list = world.Find("ScrollRect/ItemList") as RectTransform;
            if (list != null) startup.m_worldListRoot = list;
            else missing.Add("WorldPanel/ScrollRect/ItemList");
            var ensure = world.Find("ScrollRect")?.GetComponent<ScrollRectEnsureVisible>();
            if (ensure != null) startup.m_worldListEnsureVisible = ensure;
            else missing.Add("WorldPanel/ScrollRect (ScrollRectEnsureVisible)");
            if (Auga.Assets.WorldListElement != null)
            {
                EnsureRowTooltip(Auga.Assets.WorldListElement);
                startup.m_worldListElement = Auga.Assets.WorldListElement;
                startup.m_worldListElementStep = RowHeight;
            }
            else missing.Add("WorldListElement prefab");

            var start = wire("Panel/WorldPanel/Start", "Panel/WorldPanel/Start", "Start");
            var remove = wire("Panel/WorldPanel/RemoveButton", "Panel/WorldPanel/Remove", "Remove world");
            wire("Panel/WorldPanel/NewButton", "Panel/WorldPanel/New world", "New world");
            wire("Panel/WorldPanel/Back", "Panel/WorldPanel/Back", "Back");
            wire("Panel/WorldPanel/ManageSaves", "Panel/WorldPanel/ManageSaves", "Manage saves");
            var options = wire("Panel/WorldPanel/Server Options", "Panel/WorldPanel/Server Options", "Server options");
            if (start != null) startup.m_worldStart = start;
            if (remove != null) startup.m_worldRemove = remove;
            if (options != null) startup.m_serverOptionsButton = options;

            var open = WireToggle(a, "Panel/WorldPanel/CheckboxRow/StartServerToggle", startup.m_openServerToggle, "Start server toggle", missing);
            var community = WireToggle(a, "Panel/WorldPanel/CheckboxRow/StartPublicGameToggle", startup.m_publicServerToggle, "Community server toggle", missing);
            var crossplay = WireToggle(a, "Panel/WorldPanel/CheckboxRow/CrossplayToggle", startup.m_crossplayServerToggle, "Crossplay toggle", missing);
            if (open != null) startup.m_openServerToggle = open;
            if (community != null) startup.m_publicServerToggle = community;
            if (crossplay != null) startup.m_crossplayServerToggle = crossplay;
            startup.m_toggleColor = AugaPanelRestyler.Brown1;   // FejdStartup recolours those two labels every frame (dimmed while off)
            // PlayStation only and inactive, but FejdStartup writes to it: the vanilla toggle moves over
            var samePlatform = Adopt(vanillaWorld, "PlayStation_SamePlatformOnlyToggle", world)?.GetComponent<Toggle>();
            if (samePlatform != null) startup.m_samePlatformOnlyToggleWorldPanel = samePlatform;
            else missing.Add("vanilla WorldPanel/PlayStation_SamePlatformOnlyToggle");

            var password = a.Find("Panel/WorldPanel/ServerPassword")?.GetComponent<GuiInputField>();
            if (password != null)
            {
                CopyInputSettings(startup.m_serverPassword, password);
                startup.m_serverPassword = password;
                SubmitTo(password, () => startup.m_worldStart);
            }
            else missing.Add("WorldPanel/ServerPassword (GuiInputField)");
            var error = a.Find("Panel/WorldPanel/ServerPassword/Tooltip/ErrorText")?.GetComponent<TMP_Text>();
            if (error != null)
            {
                startup.m_passwordError = error;
                // vanilla only ever writes the text; the bubble around it shows while there is one
                error.transform.parent.gameObject.AddComponent<ShowWhileTextNotEmpty>().Text = error;
            }
            else missing.Add("WorldPanel/ServerPassword/Tooltip/ErrorText");

            var help = world.Find("Server help");
            var helpText = help != null ? help.Find("Text")?.GetComponent<TextMeshProUGUI>() : null;
            if (help != null && helpText != null)
            {
                startup.m_worldSourceInfoPanel = help.gameObject;
                startup.m_worldSourceInfo = helpText;
            }
            else missing.Add("WorldPanel/Server help/Text");

            var anchor = Adopt(vanillaWorld, "TooltipAnchor", world) as RectTransform;
            var secondary = Adopt(vanillaWorld, "SecondaryTooltipAnchor", world) as RectTransform;
            if (anchor != null) { PlaceBesidePanel(anchor, -230f); startup.m_tooltipAnchor = anchor; }
            else missing.Add("vanilla WorldPanel/TooltipAnchor");
            if (secondary != null) { PlaceBesidePanel(secondary, -700f); startup.m_tooltipSecondaryAnchor = secondary; }
            else missing.Add("vanilla WorldPanel/SecondaryTooltipAnchor");
        }

        // ------------------------------------------------------------------ new world / remove world

        private static void SetupDialogs(FejdStartup startup, Transform v, Transform a, Func<string, string, string, Button> wire, List<string> missing)
        {
            var create = a.Find("NewWorldDialog");
            if (create != null)
            {
                startup.m_createWorldPanel = create.gameObject;
                var name = create.Find("WorldName")?.GetComponent<GuiInputField>();
                var seed = create.Find("WorldSeed")?.GetComponent<GuiInputField>();
                if (name != null)
                {
                    CopyInputSettings(startup.m_newWorldName, name);
                    startup.m_newWorldName = name;
                    SubmitTo(name, () => startup.m_newWorldDone);
                }
                else missing.Add("NewWorldDialog/WorldName (GuiInputField)");
                if (seed != null)
                {
                    CopyInputSettings(startup.m_newWorldSeed, seed);
                    startup.m_newWorldSeed = seed;
                    SubmitTo(seed, () => startup.m_newWorldDone);
                }
                else missing.Add("NewWorldDialog/WorldSeed (GuiInputField)");
                var done = wire("NewWorldDialog/Done", "newWorld/panel/Done", "New world: done");
                wire("NewWorldDialog/Cancel", "newWorld/panel/Cancel", "New world: cancel");
                if (done != null) startup.m_newWorldDone = done;
            }
            else missing.Add("NewWorldDialog");

            var removeDialog = a.Find("RemoveWorldDialog");
            if (removeDialog != null)
            {
                startup.m_removeWorldDialog = removeDialog.gameObject;
                var name = removeDialog.Find("Text")?.GetComponent<TMP_Text>();
                if (name != null) startup.m_removeWorldName = name;
                else missing.Add("RemoveWorldDialog/Text");
                wire("RemoveWorldDialog/ButtonYes", "RemoveWorldDialog/Dialog/ButtonYes", "Remove world: yes");
                wire("RemoveWorldDialog/ButtonNo", "RemoveWorldDialog/Dialog/ButtonNo", "Remove world: no");
            }
            else missing.Add("RemoveWorldDialog");
        }

        // ------------------------------------------------------------------ join panel / server list

        private static void SetupServerList(FejdStartup startup, Transform v, Transform a, Func<string, string, string, Button> wire, List<string> missing)
        {
            var join = a.Find("Panel/JoinPanel");
            var vanillaJoin = v.Find("Panel/JoinPanel");
            var old = vanillaJoin != null ? vanillaJoin.GetComponent<ServerListGui>() : null;
            if (join == null || old == null)
            {
                missing.Add("Panel/JoinPanel, or the vanilla ServerListGui to take the settings from");
                return;
            }
            startup.m_serverListPanel = join.gameObject;

            // the prefab points the join list's scroll view at the world list's scrollbar
            var scroll = join.Find("ScrollRect")?.GetComponent<ScrollRect>();
            var bar = join.Find("ScrollBar")?.GetComponent<Scrollbar>();
            if (scroll != null && bar != null) scroll.verticalScrollbar = bar;

            // ServerListGui clones one tab per server list under its own transform, so it lives on the tab strip;
            // the strip's first placeholder tab is the template, the others go
            var strip = join.Find("TabButtons/Tabs");
            var tabTemplate = strip != null && strip.childCount > 0 ? strip.GetChild(0).gameObject : null;
            if (tabTemplate == null)
            {
                missing.Add("JoinPanel/TabButtons/Tabs with a tab to clone");
                return;
            }
            for (var i = strip.childCount - 1; i >= 1; i--)
                Object.DestroyImmediate(strip.GetChild(i).gameObject);
            tabTemplate.name = "ServerListTab";
            tabTemplate.SetActive(false);
            if (tabTemplate.GetComponent<UIGamePad>() == null)
                tabTemplate.AddComponent<UIGamePad>();   // SetHint expects one on every tab
            foreach (var hint in new[] { "gamepad_hint_left", "gamepad_hint_right" })
            {
                var go = new GameObject(hint, typeof(RectTransform));
                go.transform.SetParent(tabTemplate.transform, false);
                go.SetActive(false);
            }

            if (strip.GetComponent<UIGamePad>() == null)
                strip.gameObject.AddComponent<UIGamePad>();      // the list's own input gate
            var tabHandler = strip.GetComponent<TabHandler>() ?? strip.gameObject.AddComponent<TabHandler>();
            var vanillaTabHandler = vanillaJoin.GetComponent<TabHandler>();
            if (vanillaTabHandler != null) CopyTabSettings(vanillaTabHandler, tabHandler);
            var gui = strip.gameObject.AddComponent<ServerListGui>();   // inactive tree: Awake waits for the first show
            gui.m_serverListTabHandler = tabHandler;
            gui.m_serverListTab = tabTemplate;
            gui.m_startup = startup;
            gui.m_serverListElementStep = RowHeight;
            gui.m_serverListRoot = join.Find("ScrollRect/ItemList") as RectTransform;
            gui.m_serverListEnsureVisible = join.Find("ScrollRect")?.GetComponent<ScrollRectEnsureVisible>();
            gui.m_serverCount = join.Find("ServerCount")?.GetComponent<TextMeshProUGUI>();
            gui.m_filterInputField = join.Find("Filter")?.GetComponent<GuiInputField>();
            if (gui.m_serverListRoot == null) missing.Add("JoinPanel/ScrollRect/ItemList");
            if (gui.m_serverListEnsureVisible == null) missing.Add("JoinPanel/ScrollRect (ScrollRectEnsureVisible)");
            if (gui.m_serverCount == null) missing.Add("JoinPanel/ServerCount");
            if (gui.m_filterInputField != null)
            {
                CopyInputSettings(old.m_filterInputField, gui.m_filterInputField);
                StripSubmit(gui.m_filterInputField);
            }
            else missing.Add("JoinPanel/Filter (GuiInputField)");
            if (Auga.Assets.ServerListElement != null)
            {
                FixServerRow(Auga.Assets.ServerListElement, missing);
                gui.m_serverListElement = Auga.Assets.ServerListElement;
            }
            else missing.Add("ServerListElement prefab");

            // vanilla's refresh / remove / favourite buttons are icons whose hidden texts are stray, so those get fixed tokens
            gui.m_serverRefreshButton = Hook(join, "RefreshButton", null, "$menu_refresh", gui.OnRefreshButton, missing);
            gui.m_addServerButton = Hook(join, "AddServer", vanillaJoin.Find("Add server"), "$menu_addserver", gui.OnAddServerOpen, missing);
            gui.m_removeButton = Hook(join, "RemoveButton", null, "$menu_remove", null, missing);   // ServerListGui adds its own handler
            gui.m_joinGameButton = wire("Panel/JoinPanel/Connect", "Panel/JoinPanel/Join", "Join");
            wire("Panel/JoinPanel/Back", "Panel/JoinPanel/Back", "Join: back");

            // the prefab has no favourite button: a clone of the remove button stands in. The two are never shown
            // together (favourites list: remove; other lists: favourite), so they share the slot.
            if (gui.m_removeButton != null)
            {
                var favorite = Object.Instantiate(gui.m_removeButton.gameObject, gui.m_removeButton.transform.parent, false);
                favorite.name = "FavoriteButton";
                favorite.transform.SetSiblingIndex(gui.m_removeButton.transform.GetSiblingIndex() + 1);
                var favoriteButton = favorite.GetComponent<Button>();
                favoriteButton.onClick = new Button.ButtonClickedEvent();
                SetLabel(favorite.transform, "$menu_favorite");
                gui.m_favoriteButton = favoriteButton;
            }
            // the move up / move down buttons stay hidden (vanilla never shows them either; reordering is by keyboard),
            // but ServerListGui needs them: the vanilla ones move over, inactive
            gui.m_upButton = Adopt(vanillaJoin, "MoveUpButton", join)?.GetComponent<Button>();
            gui.m_downButton = Adopt(vanillaJoin, "MoveDownButton", join)?.GetComponent<Button>();
            if (gui.m_upButton == null || gui.m_downButton == null) missing.Add("vanilla JoinPanel/MoveUpButton or MoveDownButton");

            var anchor = Adopt(vanillaJoin, "TooltipAnchor", join) as RectTransform;
            if (anchor != null) { PlaceBesidePanel(anchor, -700f); gui.m_tooltipAnchor = anchor; }
            else missing.Add("vanilla JoinPanel/TooltipAnchor");
            var samePlatform = Adopt(vanillaJoin, "PlayStation_SamePlatformOnlyToggle", join)?.GetComponent<Toggle>();
            if (samePlatform != null) startup.m_samePlatformOnlyToggleJoinPanel = samePlatform;
            else missing.Add("vanilla JoinPanel/PlayStation_SamePlatformOnlyToggle");

            // the add server dialog: the prefab's JoinIP
            var addServer = a.Find("JoinIP");
            var vanillaAddServer = startup.transform.Find("AddServer/panel");
            if (addServer != null)
            {
                gui.m_addServerPanel = addServer.gameObject;
                var address = addServer.Find("Address")?.GetComponent<GuiInputField>();
                if (address != null)
                {
                    CopyInputSettings(old.m_addServerTextInput, address);
                    gui.m_addServerTextInput = address;
                }
                else missing.Add("JoinIP/Address (GuiInputField)");
                gui.m_addServerConfirmButton = Hook(addServer, "Connect", vanillaAddServer?.Find("Add"), "$menu_addserver", gui.OnAddServer, missing);
                gui.m_addServerCancelButton = Hook(addServer, "Cancel", vanillaAddServer?.Find("Cancel"), "$menu_cancel", gui.OnAddServerClose, missing);
                if (address != null) SubmitTo(address, () => gui.m_addServerConfirmButton);
            }
            else missing.Add("JoinIP (the add server dialog)");

            gui.m_connectIcons = ConnectIconsFor(old.m_connectIcons);
            SetupRemoveServerDialog(a, v, gui, missing);
        }

        private static GameObject s_removeServerDialog;
        private static TMP_Text s_removeServerName;

        /// <summary>The prefab's RemoveServerDialog stands in for the vanilla yes / no popup (see the ServerListGui patch).</summary>
        private static void SetupRemoveServerDialog(Transform a, Transform v, ServerListGui gui, List<string> missing)
        {
            var dialog = a.Find("RemoveServerDialog");
            s_removeServerDialog = dialog != null ? dialog.gameObject : null;
            s_removeServerName = dialog != null ? dialog.Find("Text")?.GetComponent<TMP_Text>() : null;
            if (dialog == null)
            {
                missing.Add("RemoveServerDialog");
                return;
            }
            dialog.gameObject.SetActive(false);
            if (s_removeServerName == null) missing.Add("RemoveServerDialog/Text");
            // yes / no as on the remove world dialog; the prefab's stray click handlers go
            Hook(dialog, "ButtonYes", v.Find("RemoveWorldDialog/Dialog/ButtonYes"), "$menu_yes", () =>
            {
                RemoveSelectedFavorite(gui);
                dialog.gameObject.SetActive(false);
            }, missing);
            Hook(dialog, "ButtonNo", v.Find("RemoveWorldDialog/Dialog/ButtonNo"), "$menu_no", () => dialog.gameObject.SetActive(false), missing);
        }

        /// <summary>Opens Auga's remove server dialog for the selected server; false when there is nothing to open.</summary>
        internal static bool ShowRemoveServerDialog(ServerListGui gui)
        {
            if (s_removeServerDialog == null || gui == null)
                return false;
            var index = gui.GetSelectedServer();
            var list = gui.CurrentServerListFiltered;
            if (index < 0 || index >= list.Count)
                return false;
            if (s_removeServerName != null)
                s_removeServerName.text = list[index].m_serverName;   // vanilla runs it through the console UGC filter, which is a pass-through on PC
            s_removeServerDialog.SetActive(true);
            return true;
        }

        /// <summary>Removes the selected favourite: vanilla's OnRemoveServerConfirm without its popup bookkeeping.</summary>
        private static void RemoveSelectedFavorite(ServerListGui gui)
        {
            if (gui.m_serverLists[gui.m_currentServerList] != gui.m_favoriteServersList)
            {
                ZLog.LogError("Can't remove server from invalid list!");
                return;
            }
            var selected = gui.GetSelectedServer();
            var list = gui.CurrentServerListFiltered;
            if (selected < 0 || selected >= list.Count)
                return;
            var joinData = list[selected].m_joinData;
            if (!gui.m_favoriteServersList.TryGetIndexOf(joinData, out var index))
            {
                ZLog.LogError("Selected server was not in the favorites list!");
                return;
            }
            gui.m_favoriteServersList.Remove(gui.m_favoriteServersList[index]);
            gui.m_filteredListOutdated = true;
            if (gui.CurrentServerListFiltered.Count <= 0 && gui.m_filterInputField.text != "")
            {
                gui.m_filterInputField.text = "";
                gui.OnServerFilterChanged();
                gui.m_startup.SetServerToJoin(ServerJoinData.None);
            }
            else
            {
                gui.UpdateLocalServerListSelection();
                gui.SetSelectedServer(selected, true);
            }
        }

        /// <summary>Auga's status sprites for the server rows, the vanilla ones where a sprite is not in the bundle.</summary>
        private static ConnectIcons ConnectIconsFor(ConnectIcons vanilla)
        {
            var icons = vanilla;
            if (Auga.Assets.ServerStatusUnknown != null) icons.m_unknown = Auga.Assets.ServerStatusUnknown;
            if (Auga.Assets.ServerStatusRefresh != null) icons.m_trying = Auga.Assets.ServerStatusRefresh;
            if (Auga.Assets.ServerStatusOnline != null) icons.m_success = Auga.Assets.ServerStatusOnline;
            if (Auga.Assets.ServerStatusOffline != null) icons.m_failed = Auga.Assets.ServerStatusOffline;
            return icons;
        }

        /// <summary>The ServerListElement prefab carries a stray second child named "status" (a copy of the key icon); ServerListElement takes the first match.</summary>
        private static void FixServerRow(GameObject prefab, List<string> missing)
        {
            var statuses = prefab.transform.Cast<Transform>().Where(t => t.name == "status").ToList();
            if (statuses.Count <= 1)
                return;
            var keep = statuses.FirstOrDefault(t =>
            {
                var image = t.GetComponent<Image>();
                return image != null && image.sprite != null && image.sprite.name.StartsWith("status");
            }) ?? statuses[0];
            foreach (var stray in statuses.Where(t => t != keep))
            {
                stray.name = "status_stray";
                stray.gameObject.SetActive(false);
            }
            missing.Add("ServerListElement prefab: two children named 'status' (the extra one is a copy of the key icon; disabled at runtime)");
        }

        /// <summary>FejdStartup puts the world modifier tooltip on the row's own UITooltip; the Auga row only has them on the source icons.</summary>
        private static void EnsureRowTooltip(GameObject prefab)
        {
            if (prefab.GetComponent<UITooltip>() != null)
                return;
            var child = prefab.GetComponentInChildren<UITooltip>(true);
            var tooltip = prefab.AddComponent<UITooltip>();
            if (child != null) tooltip.m_tooltipPrefab = child.m_tooltipPrefab;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The Auga button takes a label and a fresh click handler (the prefab carries stray ones).</summary>
        private static Button Hook(Transform parent, string name, Transform vanillaButton, string fallbackToken, UnityAction action, List<string> missing)
        {
            var t = parent.Find(name);
            var button = t != null ? t.GetComponent<Button>() : null;
            if (button == null)
            {
                missing.Add(parent.name + "/" + name);
                return null;
            }
            button.onClick = new Button.ButtonClickedEvent();
            if (action != null) button.onClick.AddListener(action);
            SetLabel(t, LabelToken(vanillaButton, fallbackToken));
            return button;
        }

        private static Toggle WireToggle(Transform a, string augaPath, Toggle source, string what, List<string> missing)
        {
            var toggle = a.Find(augaPath)?.GetComponent<Toggle>();
            if (toggle == null)
            {
                missing.Add(what + ": the Auga prefab has no " + augaPath);
                return null;
            }
            if (source == null)
            {
                missing.Add(what + ": no vanilla toggle to take the handlers from");
                return toggle;
            }
            toggle.SetIsOnWithoutNotify(source.isOn);
            toggle.interactable = source.interactable;
            toggle.onValueChanged = source.onValueChanged;
            MainMenuExtras.CopyLabel(source.transform.Find("Label")?.GetComponent<TMP_Text>(), toggle.transform.Find("Label")?.GetComponent<TMP_Text>());
            return toggle;
        }

        /// <summary>The vanilla button's label token, or a fallback when it has none (icon-only buttons).</summary>
        private static string LabelToken(Transform vanillaButton, string fallback)
        {
            var text = vanillaButton != null ? vanillaButton.Find("Text")?.GetComponent<TMP_Text>() : null;
            var raw = text != null ? AugaPanelRestyler.RawText(text) : null;
            return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
        }

        private static void SetLabel(Transform button, string token)
        {
            var label = button.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null) label.text = token;
        }

        /// <summary>Moves a vanilla object (kept as it is) under an Auga parent.</summary>
        private static Transform Adopt(Transform vanillaParent, string name, Transform newParent)
        {
            var t = vanillaParent != null ? vanillaParent.Find(name) : null;
            if (t != null) t.SetParent(newParent, false);
            return t;
        }

        /// <summary>A tooltip anchor just right of the panel, <paramref name="y"/> below its top edge.</summary>
        private static void PlaceBesidePanel(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(SideGap, y);
        }

        private static void CopyInputSettings(GuiInputField from, GuiInputField to)
        {
            if (from == null || to == null)
                return;
            to.characterLimit = from.characterLimit;
            to.characterValidation = from.characterValidation;
            to.contentType = from.contentType;
            to.inputType = from.inputType;
            to.keyboardType = from.keyboardType;
            to.asteriskChar = from.asteriskChar;
            to.lineType = from.lineType;
        }

        /// <summary>
        /// Enter in the field presses the button. The prefab's GuiInputFieldSubmit goes: it re-focuses its field every
        /// frame, which two fields in one dialog cannot share and which blocks navigating away with a gamepad.
        /// </summary>
        private static void SubmitTo(GuiInputField field, Func<Button> target)
        {
            StripSubmit(field);
            field.onSubmit.AddListener(_ =>
            {
                var button = target();
                if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                    button.onClick.Invoke();
            });
        }

        private static void StripSubmit(GuiInputField field)
        {
            var submit = field != null ? field.GetComponent<GuiInputFieldSubmit>() : null;
            if (submit != null) Object.DestroyImmediate(submit);
        }
    }

    /// <summary>The remove server confirmation is Auga's RemoveServerDialog instead of the vanilla popup.</summary>
    [HarmonyPatch(typeof(ServerListGui), nameof(ServerListGui.OnRemoveServerButton))]
    public static class ServerListGui_OnRemoveServerButton_Patch
    {
        public static bool Prefix(ServerListGui __instance)
        {
            return !MainMenuStartGame.ShowRemoveServerDialog(__instance);
        }
    }

    /// <summary>Shows a bubble's graphics only while its text has content (vanilla only ever writes the text).</summary>
    public class ShowWhileTextNotEmpty : MonoBehaviour
    {
        public TMP_Text Text;
        private Graphic[] _graphics;

        private void Awake()
        {
            _graphics = GetComponentsInChildren<Graphic>(true).Where(g => g != Text).ToArray();
        }

        private void LateUpdate()
        {
            var show = Text != null && !string.IsNullOrEmpty(Text.text);
            foreach (var graphic in _graphics)
            {
                if (graphic != null && graphic.enabled != show)
                    graphic.enabled = show;
            }
        }
    }
}
