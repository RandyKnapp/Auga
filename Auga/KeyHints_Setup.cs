using System.Collections.Generic;
using AugaUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Rebuilds the vanilla key hint strip out of Auga's LabeledKeybind_HUD rows and stacks them vertically in the
    /// bottom-right corner of the HUD.
    ///
    /// The vanilla KeyHints object is kept (the game toggles its hint groups and single entries by reference and
    /// the Auga key-hint prefab predates the current bindings). Every keyboard/mouse entry - a row like
    /// "Dodge [Mouse-2] + [Space]" made of a label, key boxes, "+" texts and mouse icons - is converted in place:
    /// the entry GameObject survives (so KeyHints.m_primaryAttackKB & co. stay valid), its vanilla children are
    /// replaced by one LabeledKeybind_HUD row whose AugaBindingDisplay follows the live binding ($KEY_ tokens) or
    /// shows the fixed key text ($button_ tokens, "Esc"). Gamepad hints are single texts with glyph sprites; they
    /// are left as they are and only re-stacked.
    /// </summary>
    public static class AugaKeyHints
    {
        private const float KeySlotWidth = 64f;   // width of a key box (LongBackground) in LabeledKeybind_HUD
        private const float KeySlotGap = 12f;     // room for the "+" between two keys
        private const float LabelOffset = 75f;    // Label.anchoredPosition.x in LabeledKeybind_HUD
        private const float RowHeight = 20f;
        private const float RowSpacing = 8f;      // spacing of the Auga HUD prefab's own key hint stack

        private sealed class KeyUnit
        {
            public string Binding;      // ZInput button name (from "$KEY_<name>")
            public string StaticText;   // fixed, already localized key text ("$button_lshift", "Esc")
            public int MouseButton = -1;
            public Image Icon;          // vanilla icon (mouse wheel, rotate) reused as the key visual
            public string Separator;    // text shown before this key ("+", "/")
        }

        public static void Convert(Transform keyHints)
        {
            if (keyHints == null || Auga.Assets.Hud == null)
                return;

            var template = FindTemplate();
            if (template == null)
            {
                Auga.LogWarning("Auga HUD prefab has no LabeledKeybind_HUD row under hudroot/KeyHints; keeping the vanilla key hints.");
                return;
            }

            // the strip only anchors the hint groups now; give it the footprint the Auga HUD prefab used
            var keyHintsRect = keyHints as RectTransform;
            if (keyHintsRect != null)
            {
                keyHintsRect.sizeDelta = new Vector2(300f, 140f);
            }

            var rows = 0;
            foreach (var group in keyHints.GetComponentsInChildren<UIInputHint>(true))
            {
                if (group.transform.parent != keyHints)
                    continue;

                foreach (Transform container in group.transform)
                {
                    if (container.GetComponent<HorizontalLayoutGroup>() == null)
                        continue;

                    var keyboardStyle = false;
                    var overhang = 0f;
                    foreach (Transform entry in container)
                    {
                        // keyboard/mouse entries are small horizontal groups; gamepad hints are bare texts
                        if (entry.GetComponent<HorizontalLayoutGroup>() == null)
                            continue;
                        keyboardStyle = true;
                        if (ConvertEntry(entry, template, out var entryOverhang))
                        {
                            rows++;
                            overhang = Mathf.Max(overhang, entryOverhang);
                        }
                    }
                    MakeVertical(container, keyboardStyle ? TextAnchor.LowerLeft : TextAnchor.LowerRight, overhang);
                }
            }
            Auga.Log($"Key hints rebuilt with {rows} LabeledKeybind_HUD rows.");
        }

        /// <summary>
        /// A single-key LabeledKeybind_HUD row from the Auga HUD prefab's own key hints: the binding display on the
        /// row root, a "KeyBind" child with the key visuals and a "Label" text. The crosshair carries binding
        /// displays too (bare key containers without a label), so the search is limited to hudroot/KeyHints.
        /// </summary>
        private static AugaBindingDisplay FindTemplate()
        {
            var root = Auga.Assets.Hud.transform.Find("hudroot/KeyHints") ?? Auga.Assets.Hud.transform;
            foreach (var display in root.GetComponentsInChildren<AugaBindingDisplay>(true))
            {
                if (display.LongKeybindBox == null || display.KeybindBox == null || display.LongKeybindBox.transform.parent == display.transform)
                    continue;
                if (display.GetComponents<AugaBindingDisplay>().Length != 1)
                    continue;
                if (display.transform.Find("Label")?.GetComponent<TMP_Text>() == null)
                    continue;
                return display;
            }
            return null;
        }

        /// <param name="overhang">How far the row's extra keys reach to the left of the key column (0 for single keys).</param>
        private static bool ConvertEntry(Transform entry, AugaBindingDisplay template, out float overhang)
        {
            overhang = 0f;
            var labels = new List<string>();
            var units = new List<KeyUnit>();
            var vanillaChildren = new List<GameObject>();
            string pendingSeparator = null;

            foreach (Transform child in entry)
            {
                vanillaChildren.Add(child.gameObject);
                if (!child.gameObject.activeSelf)
                    continue; // alternative-layout keys and unused icons

                var text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    var raw = OriginalText(text);
                    var shown = (text.text ?? string.Empty).Trim();
                    if (shown == "+" || shown == "/")
                    {
                        pendingSeparator = shown;
                    }
                    else if (!string.IsNullOrEmpty(raw))
                    {
                        labels.Add(raw);
                    }
                    continue;
                }

                var keyText = child.GetComponentInChildren<TMP_Text>(true);
                if (keyText != null)
                {
                    // key_bkg/Key
                    var raw = OriginalText(keyText);
                    var unit = new KeyUnit { Separator = pendingSeparator };
                    pendingSeparator = null;
                    if (raw.StartsWith("$KEY_"))
                    {
                        unit.Binding = raw.Substring(5).Trim();
                    }
                    else
                    {
                        unit.StaticText = string.IsNullOrEmpty(raw) ? keyText.text : Localization.instance.Localize(raw);
                        unit.MouseButton = MouseButtonOf(raw);
                    }
                    units.Add(unit);
                    continue;
                }

                var icon = child.GetComponent<Image>();
                if (icon != null)
                {
                    // vanilla's mouse sprites map onto the prefab's own mouse icons (the wheel is the middle button);
                    // anything else (the rotate arrow) is shown as it is
                    var mouseButton = MouseButtonOfSprite(icon.sprite != null ? icon.sprite.name : string.Empty);
                    units.Add(mouseButton >= 0
                        ? new KeyUnit { StaticText = Localization.instance.Localize("$button_mouse" + mouseButton), MouseButton = mouseButton, Separator = pendingSeparator }
                        : new KeyUnit { Icon = icon, Separator = pendingSeparator });
                    pendingSeparator = null;
                }
            }

            if (labels.Count == 0 && units.Count == 0)
                return false;

            var row = Object.Instantiate(template.gameObject, entry, false);
            row.name = "LabeledKeybind_HUD";
            row.SetActive(true);
            Stretch((RectTransform)row.transform);

            var display = row.GetComponent<AugaBindingDisplay>();
            display.AutomaticKeyName = string.Empty;
            var slot = display.LongKeybindBox.transform.parent;
            var label = row.transform.Find("Label")?.GetComponent<TMP_Text>() ?? row.GetComponentInChildren<TMP_Text>(true);

            if (units.Count == 0)
            {
                slot.gameObject.SetActive(false);
            }
            else
            {
                // The last key sits in the prefab's slot, whose visuals are right-aligned at the key column edge
                // (x = 64) so every row's key ends where a single key ends. Earlier keys and their "+" are laid
                // out leftwards from there; the container reserves the largest overhang as left padding.
                var last = units.Count - 1;
                var extraSlots = new List<(Transform slot, AugaBindingDisplay display)>();
                for (var i = 0; i < last; i++)
                {
                    // clone the pristine slot before the last unit changes it
                    var clone = Object.Instantiate(slot.gameObject, row.transform, false);
                    clone.name = slot.name + i;
                    var slotDisplay = row.AddComponent<AugaBindingDisplay>();
                    Mirror(display, slot, slotDisplay, clone.transform);
                    LeftAlignSlot(clone.transform);
                    extraSlots.Add((clone.transform, slotDisplay));
                }

                ApplyUnit(display, slot, units[last], leftAligned: false);
                var leftmost = KeySlotWidth - VisibleWidth(display, units[last]);
                for (var i = last - 1; i >= 0; i--)
                {
                    var separatorX = leftmost - KeySlotGap;
                    AddSeparator(row.transform, label, units[i + 1].Separator ?? "+", separatorX, KeySlotGap);
                    var (unitSlot, slotDisplay) = extraSlots[i];
                    ApplyUnit(slotDisplay, unitSlot, units[i], leftAligned: true);
                    var keyWidth = VisibleWidth(slotDisplay, units[i]);
                    var slotRect = (RectTransform)unitSlot;
                    slotRect.anchoredPosition = new Vector2(separatorX - keyWidth, slotRect.anchoredPosition.y);
                    leftmost = separatorX - keyWidth;
                }
                overhang = Mathf.Max(0f, -leftmost);
            }

            var labelOffset = units.Count == 0 ? 0f : LabelOffset;
            var width = labelOffset;
            if (label != null)
            {
                var labelRect = label.rectTransform;
                labelRect.anchoredPosition = new Vector2(labelOffset, labelRect.anchoredPosition.y);
                labelRect.sizeDelta = new Vector2(-labelOffset, labelRect.sizeDelta.y);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.text = string.Join(" ", labels);
                Localization.instance.Localize(label.transform); // caches the tokens for language changes
                // the prefab's AlwaysUpper component upper-cases the label later; measure what will be shown
                label.text = label.text.ToUpperInvariant();
                width += label.preferredWidth + 8f;
            }

            foreach (var go in vanillaChildren)
            {
                if (go != null && go.transform.parent == entry)
                {
                    Object.Destroy(go);
                }
            }
            var entryLayout = entry.GetComponent<HorizontalLayoutGroup>();
            if (entryLayout != null) Object.DestroyImmediate(entryLayout);
            var entryFitter = entry.GetComponent<ContentSizeFitter>();
            if (entryFitter != null) Object.DestroyImmediate(entryFitter);
            var element = entry.GetComponent<LayoutElement>() ?? entry.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = false;
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;
            element.preferredWidth = width;
            return true;
        }

        private static void ApplyUnit(AugaBindingDisplay display, Transform slot, KeyUnit unit, bool leftAligned = false)
        {
            if (unit.Icon != null)
            {
                display.AutomaticKeyName = string.Empty;
                SetKeyVisualsActive(display, false);
                var icon = unit.Icon.rectTransform;
                icon.SetParent(slot, false);
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 1f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                var sprite = unit.Icon.sprite;
                var aspect = sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
                var height = RowHeight * 1.2f;
                icon.sizeDelta = new Vector2(Mathf.Min(KeySlotWidth, height * aspect), height);
                icon.anchoredPosition = new Vector2(leftAligned ? icon.sizeDelta.x / 2f : KeySlotWidth - icon.sizeDelta.x / 2f, -RowHeight / 2f);
                unit.Icon.preserveAspect = true;
                unit.Icon.gameObject.SetActive(true);
                return;
            }

            if (!string.IsNullOrEmpty(unit.Binding) && ZInput.instance != null && ZInput.instance.m_buttons.ContainsKey(unit.Binding))
            {
                // AugaBindingDisplay re-reads the binding every frame, so rebinding in the settings is reflected
                display.AutomaticKeyName = unit.Binding;
                display.SetBinding(unit.Binding);
                return;
            }

            display.AutomaticKeyName = string.Empty;
            var text = unit.StaticText ?? unit.Binding ?? string.Empty;
            display.SetText(text, unit.MouseButton);
        }

        /// <summary>Moves every key visual of a slot to the slot's left edge (the prefab right-aligns the short ones).</summary>
        private static void LeftAlignSlot(Transform slot)
        {
            foreach (RectTransform child in slot)
            {
                child.anchoredPosition = new Vector2(0f, child.anchoredPosition.y);
            }
        }

        /// <summary>Width of what a slot currently shows: the long box, the small box, a mouse icon or a vanilla icon.</summary>
        private static float VisibleWidth(AugaBindingDisplay display, KeyUnit unit)
        {
            if (unit.Icon != null)
                return unit.Icon.rectTransform.sizeDelta.x;
            foreach (var go in new[] { display.LongKeybindBox, display.KeybindBox, display.Mouse1, display.Mouse2, display.Mouse3, display.MouseX })
            {
                if (go != null && go.activeSelf)
                    return ((RectTransform)go.transform).sizeDelta.x;
            }
            return KeySlotWidth;
        }

        private static void SetKeyVisualsActive(AugaBindingDisplay display, bool active)
        {
            foreach (var go in new[] { display.KeybindBox, display.LongKeybindBox, display.Mouse1, display.Mouse2, display.Mouse3, display.MouseX })
            {
                if (go != null) go.SetActive(active);
            }
        }

        /// <summary>Points <paramref name="target"/> at the same objects inside the cloned key slot as <paramref name="source"/> uses in the original.</summary>
        private static void Mirror(AugaBindingDisplay source, Transform sourceSlot, AugaBindingDisplay target, Transform targetSlot)
        {
            target.AutomaticKeyName = string.Empty;
            target.KeybindBox = MirrorObject(source.KeybindBox, sourceSlot, targetSlot);
            target.KeybindText = MirrorComponent(source.KeybindText, sourceSlot, targetSlot);
            target.LongKeybindBox = MirrorObject(source.LongKeybindBox, sourceSlot, targetSlot);
            target.LongKeybindText = MirrorComponent(source.LongKeybindText, sourceSlot, targetSlot);
            target.Mouse1 = MirrorObject(source.Mouse1, sourceSlot, targetSlot);
            target.Mouse2 = MirrorObject(source.Mouse2, sourceSlot, targetSlot);
            target.Mouse3 = MirrorObject(source.Mouse3, sourceSlot, targetSlot);
            target.MouseX = MirrorObject(source.MouseX, sourceSlot, targetSlot);
            target.MouseXText = MirrorComponent(source.MouseXText, sourceSlot, targetSlot);
        }

        private static GameObject MirrorObject(GameObject original, Transform from, Transform to)
        {
            if (original == null) return null;
            var found = to.Find(RelativePath(original.transform, from));
            return found != null ? found.gameObject : original;
        }

        private static T MirrorComponent<T>(T original, Transform from, Transform to) where T : Component
        {
            if (original == null) return null;
            var found = to.Find(RelativePath(original.transform, from));
            var component = found != null ? found.GetComponent<T>() : null;
            return component != null ? component : original;
        }

        private static string RelativePath(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t != null && t != root)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return t == root ? string.Join("/", parts) : string.Empty;
        }

        /// <summary>A "+" between two keys, styled like the row's label (a copy of it keeps font, outline and shadow).</summary>
        private static void AddSeparator(Transform row, TMP_Text style, string text, float x, float width)
        {
            var go = style != null
                ? Object.Instantiate(style.gameObject, row, false)
                : new GameObject("Separator", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.name = "Separator";
            go.SetActive(true);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, RowHeight);
            var separator = go.GetComponent<TMP_Text>();
            separator.alignment = TextAlignmentOptions.Center;
            separator.textWrappingMode = TextWrappingModes.NoWrap;
            separator.overflowMode = TextOverflowModes.Overflow;
            separator.raycastTarget = false;
            separator.text = text;
        }

        private static void MakeVertical(Transform container, TextAnchor alignment, float leftPadding = 0f)
        {
            var horizontal = container.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) Object.DestroyImmediate(horizontal);

            var vertical = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(Mathf.CeilToInt(leftPadding), 0, 0, 0);
            vertical.spacing = RowSpacing;
            vertical.childAlignment = alignment;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = alignment == TextAnchor.LowerLeft;
            vertical.childForceExpandHeight = false;

            var fitter = container.GetComponent<ContentSizeFitter>() ?? container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // hug the content and grow up/left from the bottom-right corner of the hint group
            var rect = (RectTransform)container;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300f, RowHeight);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// The text with its localization tokens, as authored in the prefab. Hud.Awake runs before the IngameGui
        /// Localize component localizes the HUD, so the live text usually still is the token; once localized,
        /// Localization keeps the original in its cache.
        /// </summary>
        private static string OriginalText(TMP_Text text)
        {
            if (Localization.instance != null && Localization.instance.textMeshStrings.TryGetValue(text, out var raw))
                return raw ?? string.Empty;
            return (text.text ?? string.Empty).Trim();
        }

        private static int MouseButtonOfSprite(string spriteName)
        {
            switch (spriteName)
            {
                case "mouse1_icon": return 0;
                case "mouse2_icon": return 1;
                case "mouse3_icon":
                case "mousew_icon": return 2;
                default: return -1;
            }
        }

        private static int MouseButtonOf(string token)
        {
            switch (token)
            {
                case "$button_mouse0": return 0;
                case "$button_mouse1": return 1;
                case "$button_mouse2": return 2;
                default: return -1;
            }
        }
    }
}
