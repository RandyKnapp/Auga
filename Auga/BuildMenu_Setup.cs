using System;
using System.Collections.Generic;
using System.Linq;
using Auga.Utilities;
using AugaUnity;
using GUIFramework;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// The 1.x build menu (BuildUIV2, driven by BuildUi): the vanilla box is replaced by Auga's panel background
    /// with room around the widgets, the category tabs by the settings screen's tab bar, the search field by Auga's
    /// text input and the tab-switch key hints by Auga's key boxes. The layout stays vanilla's: the tab bar
    /// across the top with the left / right keys at its ends on its midline, the search field with its F key at
    /// the top of the tag list column. Everything vanilla holds a reference to is re-pointed, so BuildUi and its
    /// TabHandler keep driving the screen.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class BuildMenu_Setup
    {
        /// <summary>How far the Auga background reaches past the vanilla window; the widgets sit 18+ px inside it.</summary>
        private const float BackgroundMargin = 30f;
        /// <summary>The top reaches less far: the tab row already sits 39 px below the window's top edge.</summary>
        private const float BackgroundMarginTop = 10f;
        /// <summary>Distance from the tab container's edges to the first / last element of the row.</summary>
        private const float RowInset = 10f;
        private const float RowHeight = 38f;
        private const float KeyBoxWidth = 20f;
        private const float KeyBoxHeight = 20f;
        /// <summary>Between the F key and the search field; the tag list's clipping mask reaches 15 px past the column.</summary>
        private const float SmallGap = 4f;
        private const float Gap = 12f;
        /// <summary>Added to vanilla's 10 px between the tag list column and the piece grid.</summary>
        private const float ColumnGapExtra = 8f;
        /// <summary>The scrollbars move this far right of their scroll views' edges.</summary>
        private const float ScrollbarShift = 8f;
        /// <summary>The first row of the tag list column: the repair label and the 64 px repair / remove button.</summary>
        private const float RepairRowHeight = 64f;
        private const float RepairLabelSize = 18f;
        /// <summary>Between the repair label's text and the button.</summary>
        private const float RepairLabelPadding = 8f;
        public const string RepairRowName = "AugaRepairRow";

        public const string TabBarName = "AugaTabBar";

        [UsedImplicitly]
        public static void Postfix(Hud __instance)
        {
            AugaPanelRestyler.LinkGameFontFallbacks();
            try
            {
                SetupSelectedPiece(__instance);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auga] Build piece info setup failed: {e}");
            }

            var buildUi = __instance.m_buildUi;
            if (buildUi == null)
                return;
            var root = buildUi.transform;
            var window = root.Find("bar/SelectionWindow");
            var tabContainer = window != null ? window.Find("TabContainer") as RectTransform : null;
            if (window == null || tabContainer == null)
                return;

            try
            {
                SetupBackground(window);
                var map = new Dictionary<Object, Object>();
                var doomed = new List<GameObject>();

                // the tab row, left to right: left key, tab bar stretched between the keys, right key
                var barInset = RowInset + KeyBoxWidth + Gap;
                SetupSearch(buildUi, map, doomed);
                SetupTabs(buildUi, tabContainer, barInset, barInset, map, doomed);
                SetupHints(tabContainer, RowInset);

                AugaPanelRestyler.Repoint(root, map);
                FixHideWithTagList(buildUi, tabContainer);
                foreach (var go in doomed)
                {
                    if (go == null) continue;
                    go.SetActive(false);
                    Object.Destroy(go);
                }
                SetupScrollbars(window);
                SetupPieceButtons(buildUi);
                SetupTagButtons(buildUi);
                SetupDivider(buildUi);
                SetupFavoritesDropdown(buildUi);
                SetupSpacing(buildUi);
                Localization.instance.Localize(root);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auga] Build menu setup failed: {e}");
            }
        }

        // ------------------------------------------------------------------ selected piece info

        /// <summary>
        /// The piece info panel under the crosshair (vanilla's legacy build hud, which it still shows): the name and
        /// description in Auga's bold face and colours, and the six requirement slots replaced by Auga's
        /// BuildingRequirementElement in the vanilla slots' places. The element keeps vanilla's child names
        /// (res_icon, res_name, res_amount) and its tooltip, so InventoryGui.SetupRequirement and Auga's station
        /// slot keep filling it.
        /// </summary>
        private static void SetupSelectedPiece(Hud hud)
        {
            StyleText(hud.m_buildSelection, AugaPanelRestyler.Brown1);
            StyleText(hud.m_pieceDescription, AugaPanelRestyler.Brown3);

            var template = Auga.Assets.Hud != null ? Auga.Assets.Hud.transform.Find("hudroot/BuildHud/SelectedPiece/Requirements/BuildingRequirementElement") : null;
            var items = hud.m_requirementItems;
            if (template == null || items == null)
                return;
            for (var i = 0; i < items.Length; i++)
            {
                var old = items[i];
                if (old == null)
                    continue;
                var oldRect = (RectTransform)old.transform;
                var go = Object.Instantiate(template.gameObject, oldRect.parent, false);
                go.name = old.name;
                go.transform.SetSiblingIndex(oldRect.GetSiblingIndex());
                var rect = (RectTransform)go.transform;
                rect.anchorMin = oldRect.anchorMin;
                rect.anchorMax = oldRect.anchorMax;
                rect.pivot = oldRect.pivot;
                rect.anchoredPosition = oldRect.anchoredPosition;
                rect.sizeDelta = oldRect.sizeDelta;
                go.SetActive(old.activeSelf);
                items[i] = go;
                old.SetActive(false);
                Object.Destroy(old);
            }
        }

        private static void StyleText(TMP_Text text, Color color)
        {
            if (text == null)
                return;
            if (AugaPanelRestyler.BoldFont != null)
            {
                text.font = AugaPanelRestyler.BoldFont;
                text.fontStyle &= ~FontStyles.Bold;   // the bold face carries the weight
            }
            text.color = color;
        }

        // ------------------------------------------------------------------ background

        /// <summary>
        /// Auga's panel background over the whole window, reaching past its edges so every widget has 40+ px of
        /// room. The vanilla box stays as an invisible raycast target: without one, a click on the panel's empty
        /// parts would fall through to the close-on-click-outside catcher behind it.
        /// </summary>
        private static void SetupBackground(Transform window)
        {
            var vanilla = window.Find("Background");
            if (vanilla != null)
            {
                foreach (var image in vanilla.GetComponentsInChildren<Image>(true))
                    image.color = Color.clear;
            }
            if (Auga.Assets.PanelBase == null)
                return;
            var background = Object.Instantiate(Auga.Assets.PanelBase, window, false);
            background.name = "AugaPanelBackground";
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-BackgroundMargin, -BackgroundMargin);
            rect.offsetMax = new Vector2(BackgroundMargin, BackgroundMarginTop);
        }

        // ------------------------------------------------------------------ search

        /// <summary>
        /// The search field becomes Auga's text input (the field of the AugaTextInput prefab) with the vanilla
        /// input settings and placeholder, in its vanilla place at the top of the tag list column next to the
        /// repair / remove piece buttons. The F key hint vanilla hangs off the field's corner sits centred on the
        /// field, to its left.
        /// </summary>
        private static void SetupSearch(BuildUi buildUi, Dictionary<Object, Object> map, List<GameObject> doomed)
        {
            var old = buildUi.m_searchField;
            var template = Auga.Assets.TextInput != null ? Auga.Assets.TextInput.transform.Find("panel/TextField") : null;
            if (old == null || template == null)
                return;
            var row = old.transform.parent as RectTransform;
            if (row == null)
                return;

            var go = Object.Instantiate(template.gameObject, row, false);
            go.name = "SearchBar";
            go.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, RowHeight);   // the row's layout controls the width only
            var oldElement = old.GetComponent<LayoutElement>();
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.flexibleWidth = oldElement != null ? oldElement.flexibleWidth : 1f;
            element.minWidth = oldElement != null ? oldElement.minWidth : -1f;
            element.preferredWidth = oldElement != null ? oldElement.preferredWidth : -1f;
            element.preferredHeight = RowHeight;

            var field = go.GetComponent<GuiInputField>();
            if (field == null)
                return;
            CopyInputSettings(old, field);
            field.text = old.text;
            var oldPlaceholder = old.placeholder as TMP_Text;
            var placeholder = go.transform.Find("Placeholder")?.GetComponent<TMP_Text>();
            if (placeholder != null && oldPlaceholder != null)
                placeholder.text = AugaPanelRestyler.RawText(oldPlaceholder);
            // the prefab's submit helper re-focuses its field every frame; the game manages focus itself
            var submit = go.GetComponent<GuiInputFieldSubmit>();
            if (submit != null) Object.DestroyImmediate(submit);

            // the "F" hint vanilla shows beside the field, as an Auga key box to the left of it
            var hint = old.transform.Find("SearchBarHint");
            if (hint != null)
            {
                hint.SetParent(row, false);
                var hintRect = (RectTransform)hint;
                hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 0.5f);
                hintRect.pivot = new Vector2(1f, 0.5f);
                hintRect.anchoredPosition = new Vector2(-SmallGap, 0f);
                hintRect.sizeDelta = new Vector2(KeyBoxWidth, KeyBoxHeight);
                var hintElement = hint.GetComponent<LayoutElement>() ?? hint.gameObject.AddComponent<LayoutElement>();
                hintElement.ignoreLayout = true;
                var mouse = hint.Find("Mouse");
                if (mouse != null)
                {
                    var box = MakeKeyBox(hint, "Mouse", null, leftAligned: true);
                    if (box != null)
                    {
                        box.SetText("F");   // vanilla focuses the field on F regardless of bindings
                        var boxRect = (RectTransform)box.transform;
                        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0f, 0.5f);
                        boxRect.pivot = new Vector2(0f, 0.5f);
                        boxRect.anchoredPosition = Vector2.zero;
                        Repoint(hint, mouse.gameObject, box.gameObject);
                        mouse.gameObject.SetActive(false);
                        Object.Destroy(mouse.gameObject);
                    }
                }
            }

            map[old] = field;
            map[old.gameObject] = go;
            map[old.transform] = rect;
            doomed.Add(old.gameObject);

            LayoutColumnHead(buildUi, row);
        }

        /// <summary>
        /// The head of the tag list column: a "Repair" label with the repair / remove button, 64 px and flush with
        /// the column's right edge, on the first row; the search field across the whole column on the second; the
        /// tag list below both. The button's container is vanilla's, so BuildUi keeps showing and hiding it; the
        /// label's row follows it (see the BuildUi postfixes).
        /// </summary>
        private static void LayoutColumnHead(BuildUi buildUi, RectTransform searchRow)
        {
            var column = searchRow.parent as RectTransform;
            var special = buildUi.m_specialButtonsContainer;
            if (column == null || special == null)
                return;

            var repairRow = new GameObject(RepairRowName, typeof(RectTransform)).GetComponent<RectTransform>();
            repairRow.SetParent(column, false);
            repairRow.SetSiblingIndex(searchRow.GetSiblingIndex());
            repairRow.anchorMin = new Vector2(0f, 1f);
            repairRow.anchorMax = new Vector2(1f, 1f);
            repairRow.pivot = new Vector2(0.5f, 1f);
            repairRow.anchoredPosition = Vector2.zero;
            repairRow.sizeDelta = new Vector2(0f, RepairRowHeight);

            // the text component gets its font before it wakes up: TMP looks for its default font asset (which the
            // game does not ship) on Awake when none is set and warns about it
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.SetActive(false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            if (AugaPanelRestyler.BoldFont != null) label.font = AugaPanelRestyler.BoldFont;
            var labelRect = label.rectTransform;
            labelRect.SetParent(repairRow, false);
            labelObject.SetActive(true);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = new Vector2(-(RepairRowHeight + RepairLabelPadding), 0f);
            label.text = "$piece_repair";
            label.fontSize = RepairLabelSize;
            label.fontStyle = FontStyles.UpperCase;
            label.color = AugaPanelRestyler.Brown3;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            special.SetParent(repairRow, false);
            special.anchorMin = special.anchorMax = new Vector2(1f, 0.5f);
            special.pivot = new Vector2(1f, 0.5f);
            special.anchoredPosition = Vector2.zero;
            special.sizeDelta = new Vector2(RepairRowHeight, RepairRowHeight);
            var specialElement = special.GetComponent<LayoutElement>();
            if (specialElement != null) specialElement.preferredWidth = RepairRowHeight;
            var specialLayout = special.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (specialLayout != null)
            {
                specialLayout.padding = new RectOffset();
                specialLayout.childForceExpandWidth = true;
                specialLayout.childForceExpandHeight = true;
            }

            searchRow.anchorMin = new Vector2(0f, 1f);
            searchRow.anchorMax = new Vector2(1f, 1f);
            searchRow.pivot = new Vector2(0.5f, 1f);
            searchRow.anchoredPosition = new Vector2(0f, -(RepairRowHeight + SmallGap));
            searchRow.sizeDelta = new Vector2(0f, RowHeight);

            var scroll = buildUi.m_tagListScrollRect != null ? buildUi.m_tagListScrollRect.transform as RectTransform : null;
            if (scroll != null)
                scroll.offsetMax = new Vector2(scroll.offsetMax.x, -(RepairRowHeight + SmallGap + RowHeight + SmallGap));
        }

        /// <summary>The repair label's row shows exactly when vanilla shows the repair / remove button's container.</summary>
        public static void SyncRepairRow(BuildUi buildUi)
        {
            var special = buildUi.m_specialButtonsContainer;
            var row = special != null ? special.parent : null;
            if (row == null || row.name != RepairRowName)
                return;
            if (row.gameObject.activeSelf != special.gameObject.activeSelf)
                row.gameObject.SetActive(special.gameObject.activeSelf);
        }

        private static void CopyInputSettings(GuiInputField from, GuiInputField to)
        {
            to.characterLimit = from.characterLimit;
            to.characterValidation = from.characterValidation;
            to.contentType = from.contentType;
            to.inputType = from.inputType;
            to.keyboardType = from.keyboardType;
            to.lineType = from.lineType;
            to.onFocusSelectAll = from.onFocusSelectAll;
            to.resetOnDeActivation = from.resetOnDeActivation;
            to.restoreOriginalTextOnEscape = from.restoreOriginalTextOnEscape;
            to.richText = from.richText;
        }

        /// <summary>Points every serialized reference under <paramref name="scope"/> from one object to another.</summary>
        private static void Repoint(Transform scope, GameObject from, GameObject to)
        {
            var map = new Dictionary<Object, Object> { [from] = to, [from.transform] = to.transform };
            AugaPanelRestyler.Repoint(scope, map);
        }

        // ------------------------------------------------------------------ tabs

        /// <summary>
        /// The settings screen's tab bar (dividers left and right, "Selected" overlay per tab) takes the place of
        /// the vanilla category tabs on the row's midline, between the two key hints. Each vanilla tab button
        /// becomes a clone of the bar's tab template with the vanilla label token, click handlers and gamepad
        /// hint; the TabHandler's tabs, BuildUi's tab list and its tab container are re-pointed at the clones.
        /// </summary>
        private static void SetupTabs(BuildUi buildUi, RectTransform tabContainer, float leftInset, float rightInset, Dictionary<Object, Object> map, List<GameObject> doomed)
        {
            var tabHandler = buildUi.m_tabHandler;
            var oldTabs = buildUi.m_tabContainer;
            var template = Auga.Assets.SettingsPrefab != null ? Auga.Assets.SettingsPrefab.transform.Find("panel/TabButtons") : null;
            if (tabHandler == null || oldTabs == null || template == null || tabHandler.m_tabs.All(t => t.m_button == null))
                return;

            var bar = Object.Instantiate(template.gameObject, tabContainer, false);
            bar.name = TabBarName;
            var barHandler = bar.GetComponent<TabHandler>();
            if (barHandler != null) Object.DestroyImmediate(barHandler);   // BuildUIV2's own TabHandler drives the tabs
            var tabsParent = bar.transform.Find("Tabs") as RectTransform;
            var tabTemplate = tabsParent != null && tabsParent.childCount > 0 ? tabsParent.GetChild(0) : null;
            if (tabTemplate == null)
            {
                Object.DestroyImmediate(bar);
                return;
            }

            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(1f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(-(leftInset + rightInset), barRect.sizeDelta.y);
            barRect.anchoredPosition = new Vector2((leftInset - rightInset) * 0.5f, 0f);
            bar.transform.SetSiblingIndex(oldTabs.GetSiblingIndex());

            foreach (var tab in tabHandler.m_tabs)
            {
                var old = tab.m_button;
                if (old == null)
                    continue;
                var go = Object.Instantiate(tabTemplate.gameObject, tabsParent, false);
                go.name = old.name;
                var oldLabel = old.transform.Find("Text")?.GetComponent<TMP_Text>() ?? old.GetComponentInChildren<TMP_Text>(true);
                var token = oldLabel != null ? AugaPanelRestyler.RawText(oldLabel)?.Trim() : null;
                foreach (var path in new[] { "Text", "Selected/Text" })
                {
                    var label = go.transform.Find(path)?.GetComponent<TMP_Text>();
                    if (label != null && token != null)
                        label.text = token;
                }
                var button = go.GetComponent<Button>();
                button.onClick = old.onClick;   // BuildUi's list-selection listener rides along
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

            // BuildUi walks its tab container for the buttons (and hides it with the tag list, see FixHideWithTagList)
            map[oldTabs] = tabsParent;
            map[oldTabs.gameObject] = tabsParent.gameObject;
            buildUi.m_tabContainer = tabsParent;
            doomed.Add(oldTabs.gameObject);
            var border = tabContainer.Find("TabBorder");
            if (border != null) border.gameObject.SetActive(false);
        }

        /// <summary>Vanilla hides its tab row with the tag list; the whole Auga bar hides instead of just its tabs.</summary>
        private static void FixHideWithTagList(BuildUi buildUi, RectTransform tabContainer)
        {
            var list = buildUi.m_hideWithTagList;
            var bar = tabContainer.Find(TabBarName) as RectTransform;
            if (list == null || bar == null)
                return;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].IsChildOf(bar))
                    list[i] = bar;
            }
        }

        // ------------------------------------------------------------------ scrollbars

        /// <summary>The piece list's and the tag list's scrollbars become Auga's; the scroll views keep vanilla's wheel speed.</summary>
        private static void SetupScrollbars(Transform window)
        {
            AugaPanelRestyler.Restyle(window, new RestyleOptions
            {
                ReplaceBackground = false,
                RestyleTexts = false,
                DetectHeaders = false,
                ReplaceButtons = false,
                RestyleSliders = false,
                RestyleToggles = false,
                ReplaceScrollbars = true,
                ScrollSensitivity = 1000f,
            });
        }

        // ------------------------------------------------------------------ favourites dropdown

        /// <summary>The LabeledCheckbox prefab's colours: the box in Auga's black, the mark in Auga's gold.</summary>
        private static readonly Color CheckboxColor = AugaPanelRestyler.Hex("#181410");
        private static readonly Color CheckmarkColor = AugaPanelRestyler.Hex("#B98A12");
        private const float CheckmarkSize = 9f;

        /// <summary>
        /// The favourites dropdown on a piece (remove from favourites, a divider, one check row per category):
        /// Auga's bold face and text colours, the flat selection blue, Auga's small divider, and the category rows'
        /// check boxes in Auga's diamond style. The row prefab is restyled, so rows made later have the look, and so
        /// are any rows already there.
        /// </summary>
        private static void SetupFavoritesDropdown(BuildUi buildUi)
        {
            var dropdown = buildUi.m_favoritesDropdown;
            if (dropdown == null)
                return;
            var root = dropdown.transform;
            AugaPanelRestyler.Restyle(root, TextOnly);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name != "Selected") continue;
                image.sprite = null;
                image.color = AugaPanelRestyler.SelectionBlue;
            }

            var divider = root.Find("MaterialDivider") as RectTransform;
            if (divider != null && Auga.Assets.DividerSmall != null)
            {
                var go = Object.Instantiate(Auga.Assets.DividerSmall, root, false);
                go.name = divider.name;
                go.transform.SetSiblingIndex(divider.GetSiblingIndex());
                go.SetActive(divider.gameObject.activeSelf);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = divider.anchorMin;
                rect.anchorMax = divider.anchorMax;
                rect.pivot = divider.pivot;
                rect.anchoredPosition = divider.anchoredPosition;
                rect.sizeDelta = new Vector2(divider.sizeDelta.x, rect.sizeDelta.y);
                divider.gameObject.SetActive(false);
                Object.Destroy(divider.gameObject);
            }

            var prefab = dropdown.checkButtonPrefab;
            if (prefab != null)
                RestyleCheckButton(prefab.transform);
            foreach (var button in root.GetComponentsInChildren<BuildUiFavoriteCategoryCheckButton>(true))
                RestyleCheckButton(button.transform);
        }

        private static void RestyleCheckButton(Transform button)
        {
            AugaPanelRestyler.Restyle(button, TextOnly);
            var selected = button.Find("Selected")?.GetComponent<Image>();
            if (selected != null)
            {
                selected.sprite = null;
                selected.color = AugaPanelRestyler.SelectionBlue;
            }
            var diamond = Auga.Assets.ContainerDiamond;
            if (diamond == null)
                return;
            var box = button.Find("Panel")?.GetComponent<Image>();
            if (box != null)
            {
                box.sprite = diamond;
                box.type = Image.Type.Simple;
                box.preserveAspect = true;
                box.color = CheckboxColor;
            }
            var mark = button.Find("Panel/Image")?.GetComponent<Image>();
            if (mark != null)
            {
                mark.sprite = diamond;
                mark.type = Image.Type.Simple;
                mark.preserveAspect = true;
                mark.color = CheckmarkColor;
                var rect = (RectTransform)mark.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(CheckmarkSize, CheckmarkSize);
            }
        }

        // ------------------------------------------------------------------ spacing

        /// <summary>
        /// Room around the lists: the piece grid starts further from the tag list column (BuildUi re-applies the
        /// column's width when it shows the column, from the offset it read on Awake, so that is moved too), and
        /// both scrollbars sit a little right of their scroll views.
        /// </summary>
        private static void SetupSpacing(BuildUi buildUi)
        {
            var pieceView = buildUi.m_pieceView;
            if (pieceView != null)
            {
                pieceView.offsetMin = new Vector2(pieceView.offsetMin.x + ColumnGapExtra, pieceView.offsetMin.y);
                if (buildUi.m_tagListOpenOffset > 0f)
                    buildUi.m_tagListOpenOffset += ColumnGapExtra;
            }
            foreach (var scrollRect in new[] { buildUi.m_pieceScrollRect, buildUi.m_tagListScrollRect })
            {
                var bar = scrollRect != null ? scrollRect.verticalScrollbar : null;
                if (bar == null) continue;
                var rect = (RectTransform)bar.transform;
                rect.anchoredPosition += new Vector2(ScrollbarShift, 0f);
            }
        }

        // ------------------------------------------------------------------ divider

        /// <summary>
        /// The line the tag list draws between the major materials and the rest becomes Auga's small divider.
        /// BuildUi moves the divider between rows and shows or hides it by reference, so the reference moves over.
        /// </summary>
        private static void SetupDivider(BuildUi buildUi)
        {
            var old = buildUi.m_materialSeparator;
            var prefab = Auga.Assets.DividerSmall;
            if (old == null || prefab == null)
                return;
            var go = Object.Instantiate(prefab, old.parent, false);
            go.name = old.name;
            go.transform.SetSiblingIndex(old.GetSiblingIndex());
            go.SetActive(old.gameObject.activeSelf);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = old.anchorMin;
            rect.anchorMax = old.anchorMax;
            rect.pivot = old.pivot;
            rect.anchoredPosition = old.anchoredPosition;
            rect.sizeDelta = new Vector2(old.sizeDelta.x, rect.sizeDelta.y);   // the column's layout sets the width
            buildUi.m_materialSeparator = rect;
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        // ------------------------------------------------------------------ piece buttons

        /// <summary>
        /// The piece buttons come from Auga's BuildHudElement prefab, which carries the vanilla BuildUiPieceButton
        /// with its icon, upgrade arrow and favourite star, plus Auga's selected frame. Only the root catches the
        /// pointer, as on the vanilla button, so the frame and the icon never sit between the pointer and the button.
        /// </summary>
        private static void SetupPieceButtons(BuildUi buildUi)
        {
            var prefab = Auga.Assets.BuildHudElement;
            if (prefab == null || prefab.GetComponent<BuildUiPieceButton>() == null || prefab.GetComponent<UIInputHandler>() == null)
                return;
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
                image.raycastTarget = image.transform == prefab.transform;
            buildUi.m_pieceButtonPrefab = prefab;
        }

        // ------------------------------------------------------------------ tag buttons

        private static readonly RestyleOptions TextOnly = new RestyleOptions
        {
            ReplaceBackground = false,
            DetectHeaders = false,
            ReplaceButtons = false,
            ReplaceScrollbars = false,
            RestyleSliders = false,
            RestyleToggles = false,
        };

        /// <summary>
        /// The tag list's rows take Auga's fonts and colours and the flat selection blue. The change goes onto the
        /// prefab the list is built from, so every row made later has it, and onto the rows already in the scene:
        /// the "All" row and the add-favourite row BuildUi makes on Awake.
        /// </summary>
        private static void SetupTagButtons(BuildUi buildUi)
        {
            var prefab = buildUi.m_tagButtonPrefab;
            if (prefab != null)
                RestyleTagButton(prefab.transform);
            foreach (var button in buildUi.GetComponentsInChildren<BuildUiTagButton>(true))
                RestyleTagButton(button.transform);
        }

        private static void RestyleTagButton(Transform button)
        {
            AugaPanelRestyler.Restyle(button, TextOnly);
            var selected = button.Find("Selected")?.GetComponent<Image>();
            if (selected != null)
            {
                selected.sprite = null;
                selected.color = AugaPanelRestyler.SelectionBlue;
            }
        }

        // ------------------------------------------------------------------ hints

        /// <summary>
        /// The tab-switch key hints (Q / E by default) become Auga key boxes that follow the live bindings, on the
        /// row's midline: the left one after the search field, the right one at the row's end. The gamepad glyph
        /// hints keep their look and only move onto the midline. The vanilla input-hint switcher keeps toggling the
        /// mouse+keyboard and gamepad groups.
        /// </summary>
        private static void SetupHints(RectTransform tabContainer, float leftKeyX)
        {
            var help = tabContainer.Find("InputHelp") as RectTransform;
            if (help == null)
                return;
            help.anchoredPosition = new Vector2(help.anchoredPosition.x, 0f);
            var keyboard = help.Find("MK hints");
            if (keyboard != null)
            {
                ReplaceKeyHint(keyboard, "Left", "TabLeft", leftKeyX, leftSide: true);
                ReplaceKeyHint(keyboard, "Right", "TabRight", -RowInset, leftSide: false);
            }
            var gamepad = help.Find("Gamepad hints");
            if (gamepad != null)
            {
                PlaceOnMidline(gamepad.Find("gamepad_hint_L") as RectTransform, leftKeyX, leftSide: true);
                PlaceOnMidline(gamepad.Find("gamepad_hint_R") as RectTransform, -RowInset, leftSide: false);
            }
        }

        private static void ReplaceKeyHint(Transform group, string name, string keyName, float x, bool leftSide)
        {
            var old = group.Find(name);
            var box = MakeKeyBox(group, name, keyName, leftAligned: leftSide);
            if (box == null)
                return;
            PlaceOnMidline((RectTransform)box.transform, x, leftSide);
            if (old != null)
            {
                Repoint(group.root, old.gameObject, box.gameObject);
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }
        }

        private static void PlaceOnMidline(RectTransform rect, float x, bool leftSide)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(leftSide ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(leftSide ? 0f : 1f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
        }

        /// <summary>
        /// A single key box in Auga's key-hint style: a clone of the HUD prefab's build key hint row without its
        /// label, its binding display following <paramref name="keyName"/> (or set by hand when null). The visuals
        /// sit at the box's left or right edge, so long key names grow away from what the box is next to.
        /// </summary>
        private static AugaBindingDisplay MakeKeyBox(Transform parent, string name, string keyName, bool leftAligned)
        {
            var template = Auga.Assets.Hud != null ? Auga.Assets.Hud.transform.Find("hudroot/KeyHints/BuildHints/Keyboard/Place") : null;
            var display = template != null ? template.GetComponent<AugaBindingDisplay>() : null;
            if (display == null)
                return null;
            var go = Object.Instantiate(template.gameObject, parent, false);
            go.name = name;
            var clone = go.GetComponent<AugaBindingDisplay>();
            clone.AutomaticKeyName = keyName;
            var label = go.transform.Find("Label");
            if (label != null) Object.DestroyImmediate(label.gameObject);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(KeyBoxWidth, KeyBoxHeight);
            var slot = go.transform.Find("KeyBind") as RectTransform;
            if (slot != null)
            {
                slot.anchorMin = new Vector2(0f, 0.5f);
                slot.anchorMax = new Vector2(1f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = Vector2.zero;
                slot.sizeDelta = new Vector2(0f, KeyBoxHeight);
                foreach (RectTransform visual in slot)
                {
                    // the prefab right-aligns the short box inside a 64 px slot; anchor every visual to one edge
                    visual.anchorMin = new Vector2(leftAligned ? 0f : 1f, 0.5f);
                    visual.anchorMax = new Vector2(leftAligned ? 0f : 1f, 0.5f);
                    visual.pivot = new Vector2(leftAligned ? 0f : 1f, 0.5f);
                    visual.anchoredPosition = Vector2.zero;
                }
            }
            if (!string.IsNullOrEmpty(keyName))
                clone.SetBinding(keyName);
            return clone;
        }
    }

    /// <summary>BuildUi shows the repair / remove button's container when the list has such a piece; the label's row follows.</summary>
    [HarmonyPatch(typeof(BuildUi), "UpdatePieceButtons")]
    public static class BuildUi_UpdatePieceButtons_Patch
    {
        [UsedImplicitly]
        public static void Postfix(BuildUi __instance)
        {
            BuildMenu_Setup.SyncRepairRow(__instance);
        }
    }

    /// <summary>BuildUi hides the repair / remove button's container with the tag list column; the label's row follows.</summary>
    [HarmonyPatch(typeof(BuildUi), "SetTagListActive")]
    public static class BuildUi_SetTagListActive_Patch
    {
        [UsedImplicitly]
        public static void Postfix(BuildUi __instance)
        {
            BuildMenu_Setup.SyncRepairRow(__instance);
        }
    }

    /// <summary>
    /// Vanilla writes a requirement's amount in white (red while it is missing); on the build hud's Auga elements the
    /// white becomes Auga's text colour.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    public static class InventoryGui_SetupRequirement_BuildHud_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Transform elementRoot)
        {
            if (elementRoot == null || Hud.instance == null || !elementRoot.IsChildOf(Hud.instance.transform))
                return;
            var amount = elementRoot.Find("res_amount")?.GetComponent<TMP_Text>();
            if (amount != null && amount.color == Color.white)
                amount.color = AugaPanelRestyler.Brown2;
        }
    }

    /// <summary>
    /// Vanilla marks a piece the player cannot build by tinting its icon yellow at 75% alpha (its own icon material
    /// reads that as a cue; Auga's draws it as it is). Such icons are dimmed and faded instead.
    /// </summary>
    [HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.UpdateRequirements))]
    public static class BuildUiPieceButton_UpdateRequirements_Patch
    {
        private static readonly Color VanillaMissing = new Color(1f, 1f, 0f, 0.75f);
        public static readonly Color Missing = new Color(0.55f, 0.55f, 0.55f, 0.55f);

        [UsedImplicitly]
        public static void Postfix(BuildUiPieceButton __instance)
        {
            var icon = __instance.m_icon;
            if (icon != null && icon.color == VanillaMissing)
                icon.color = Missing;
        }
    }
}
