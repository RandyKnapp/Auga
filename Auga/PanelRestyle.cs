using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auga.Utilities;
using AugaUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>What <see cref="AugaPanelRestyler.Restyle"/> should touch, and which texts are headers.</summary>
    public class RestyleOptions
    {
        /// <summary>Text objects that become DividerLarge headers (the panel title). Empty: the largest header found.</summary>
        public HashSet<string> Titles = new HashSet<string>();
        /// <summary>Text objects that become DividerMedium headers (section headers).</summary>
        public HashSet<string> Headers = new HashSet<string>();
        /// <summary>Objects (and everything under them) that are left alone.</summary>
        public HashSet<string> Skip = new HashSet<string>();
        /// <summary>Also treat texts that are clearly larger than the body text (or named *Header/*Title/*Topic) as headers.</summary>
        public bool DetectHeaders = true;
        public bool ReplaceBackground = true;
        public bool ReplaceButtons = true;
        public bool ReplaceScrollbars = true;
        /// <summary>Vanilla sliders take the look of Auga's slider widget in place (they keep their scripts).</summary>
        public bool RestyleSliders = true;
        /// <summary>Vanilla toggles take the look of Auga's checkbox in place.</summary>
        public bool RestyleToggles = true;
        public bool RestyleTexts = true;
        /// <summary>How far the Auga background reaches past the panel's left and right edges (content margin).</summary>
        public float BackgroundOverhang = 40f;
        /// <summary>The panel title: the settings screen's title look (Norsebold, no divider).</summary>
        public float TitleFontSize = 45f;
        /// <summary>Section headers inside a DividerMedium.</summary>
        public float HeaderFontSize = 18f;
        /// <summary>Mouse wheel sensitivity for every ScrollRect in the panel (vanilla panels scroll far too slowly).</summary>
        public float ScrollSensitivity = 600f;
        /// <summary>Use this Auga button prefab for every replaced button instead of picking one by size.</summary>
        public GameObject ButtonPrefab;
    }

    /// <summary>
    /// Restyles an arbitrary vanilla UI panel in place with Auga's look: Auga fonts and text colours, Auga button
    /// prefabs (small / medium / fancy by size) with the vanilla click handlers and gamepad hints carried over,
    /// Auga scrollbars, DividerLarge / DividerMedium headers around the header texts and the Auga panel background.
    /// Every component in the panel's root canvas that referenced a replaced object (serialized fields, ScrollRect
    /// scrollbars, Selectable navigation) is re-pointed at the replacement, so the vanilla code keeps working.
    /// </summary>
    public static class AugaPanelRestyler
    {
        public static readonly Color Brown1 = Hex("#EAE1D9");
        public static readonly Color Brown2 = Hex("#D1C9C2");
        public static readonly Color Brown3 = Hex("#A39689");
        public static readonly Color Brown5 = Hex("#2E2620");
        public static readonly Color Brown7 = Hex("#181410");
        public static readonly Color BrightGold = Hex("#EAA800");
        /// <summary>Text links and emphasis.</summary>
        public static readonly Color LightBlue = Hex("#1AACEF");
        /// <summary>The selected row of a list (Auga.Colors.Blue).</summary>
        public static readonly Color SelectionBlue = Hex("#216388");
        public static readonly Color ScrollHandle = Hex("#8B7C6A");
        public const float ScrollbarWidth = 8f;

        private static TMP_FontAsset _bold;
        private static TMP_FontAsset _regular;

        /// <summary>Source Sans Pro Bold as the TextMeshPro asset the Auga buttons use.</summary>
        public static TMP_FontAsset BoldFont
        {
            get
            {
                if (_bold == null) _bold = FontOf(Auga.Assets.ButtonSmall);
                return _bold;
            }
        }

        /// <summary>Source Sans Pro Regular as the TextMeshPro asset the Auga widget labels use.</summary>
        public static TMP_FontAsset RegularFont
        {
            get
            {
                if (_regular == null) _regular = FontOf(Auga.Assets.LabeledCheckbox);
                return _regular;
            }
        }

        public static void Restyle(Transform panel, RestyleOptions options = null)
        {
            if (panel == null)
                return;
            options = options ?? new RestyleOptions();
            var scope = panel.root;
            var map = new Dictionary<Object, Object>();
            var doomed = new List<GameObject>();

            try
            {
                if (options.ReplaceBackground) RestyleBackground(panel, options);
                if (options.RestyleTexts) RestyleTexts(panel, options);
                ConvertHeaders(panel, options);
                foreach (var scrollRect in panel.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (!IsSkipped(scrollRect.transform, panel, options))
                        scrollRect.scrollSensitivity = options.ScrollSensitivity;
                }
                if (options.ReplaceScrollbars)
                {
                    foreach (var scrollbar in panel.GetComponentsInChildren<Scrollbar>(true).ToList())
                    {
                        if (IsSkipped(scrollbar.transform, panel, options)) continue;
                        ReplaceScrollbar(scrollbar, map, doomed);
                    }
                }
                if (options.RestyleSliders)
                {
                    foreach (var slider in panel.GetComponentsInChildren<Slider>(true))
                        if (!IsSkipped(slider.transform, panel, options)) RestyleSliderInPlace(slider);
                }
                if (options.RestyleToggles)
                {
                    foreach (var toggle in panel.GetComponentsInChildren<Toggle>(true))
                        if (!IsSkipped(toggle.transform, panel, options)) RestyleToggleInPlace(toggle);
                }
                if (options.ReplaceButtons)
                {
                    foreach (var button in panel.GetComponentsInChildren<Button>(true).ToList())
                    {
                        if (IsSkipped(button.transform, panel, options)) continue;
                        ReplaceButton(button, options, map, doomed);
                    }
                }
                Repoint(scope, map);
                // Auga's divider art is saved with Maskable off: anything the restyle put under a mask must clip
                foreach (var graphic in panel.GetComponentsInChildren<MaskableGraphic>(true))
                {
                    if (!graphic.maskable && (graphic.GetComponentInParent<Mask>() != null || graphic.GetComponentInParent<RectMask2D>() != null))
                        graphic.maskable = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auga] Restyling panel '{panel.name}' failed: {e}");
            }
            foreach (var go in doomed)
            {
                if (go == null) continue;
                go.SetActive(false);
                Object.Destroy(go);
            }
        }

        // ------------------------------------------------------------------ background

        private static void RestyleBackground(Transform panel, RestyleOptions options)
        {
            var panelRect = panel as RectTransform;
            if (panelRect == null)
                return;
            var size = panelRect.rect.size;
            foreach (Transform child in panel)
            {
                if (IsSkipped(child, panel, options)) continue;
                var image = child.GetComponent<Image>();
                var rect = child as RectTransform;
                if (image == null || rect == null || child.GetComponent<Button>() != null || child.GetComponent<Scrollbar>() != null)
                    continue;
                // a vanilla backdrop: an image covering (nearly) the whole panel
                if (rect.rect.width >= size.x * 0.8f && rect.rect.height >= size.y * 0.8f)
                    image.enabled = false;
            }
            if (Auga.Assets.PanelBase != null)
            {
                var background = Object.Instantiate(Auga.Assets.PanelBase, panel, false);
                background.name = "AugaPanelBackground";
                background.transform.SetAsFirstSibling();
                var backgroundRect = (RectTransform)background.transform;
                Stretch(backgroundRect);
                backgroundRect.offsetMin = new Vector2(-options.BackgroundOverhang, 0f);
                backgroundRect.offsetMax = new Vector2(options.BackgroundOverhang, 0f);
            }
            // flat (sprite-less) boxes inside the panel, e.g. list backgrounds: Auga's dark tone
            var augaBackground = panel.Find("AugaPanelBackground");
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == panel || image.sprite != null || !image.enabled || IsSkipped(image.transform, panel, options)) continue;
                if (augaBackground != null && image.transform.IsChildOf(augaBackground)) continue;
                if (image.GetComponentInParent<Selectable>() != null) continue;
                image.color = new Color(Brown7.r, Brown7.g, Brown7.b, Mathf.Min(image.color.a, 0.6f));
            }
        }

        // ------------------------------------------------------------------ texts

        private static void RestyleTexts(Transform panel, RestyleOptions options)
        {
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsSkipped(text.transform, panel, options)) continue;
                var bold = (text.fontStyle & FontStyles.Bold) != 0;
                var font = bold ? BoldFont : RegularFont;
                if (font != null)
                {
                    text.font = font;
                    if (bold) text.fontStyle &= ~FontStyles.Bold; // the bold face carries the weight now
                }
                var link = text.GetComponent<Button>() != null && text.GetComponent<Image>() == null;
                text.color = link ? WithAlpha(LightBlue, text.color.a) : MapColor(text.color);
            }
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                if (IsSkipped(text.transform, panel, options)) continue;
                var bold = text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic;
                var font = bold ? Auga.Assets.SourceSansProBold : Auga.Assets.SourceSansProRegular;
                if (font != null)
                {
                    text.font = font;
                    if (bold) text.fontStyle = text.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
                }
                text.color = MapColor(text.color);
            }
        }

        /// <summary>Whites and grays become Auga's browns, warm accent colours become Auga's gold; anything else stays.</summary>
        public static Color MapColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            Color target;
            if (s < 0.2f)
            {
                if (v > 0.8f) target = Brown1;
                else if (v > 0.55f) target = Brown2;
                else if (v > 0.3f) target = Brown3;
                else return color;
            }
            else if (h >= 0.07f && h <= 0.18f && v > 0.5f)
                target = BrightGold;
            else
                return color;
            return WithAlpha(target, color.a);
        }

        // ------------------------------------------------------------------ headers

        private static void ConvertHeaders(Transform panel, RestyleOptions options)
        {
            var texts = panel.GetComponentsInChildren<TMP_Text>(true)
                .Where(t => !IsSkipped(t.transform, panel, options) && t.GetComponentInParent<Selectable>() == null)
                .ToList();
            if (texts.Count == 0)
                return;
            var sizes = texts.Select(t => t.fontSize).OrderBy(x => x).ToList();
            var body = sizes[sizes.Count / 2];

            var candidates = new List<TMP_Text>();
            foreach (var text in texts)
            {
                if (options.Titles.Contains(text.name) || options.Headers.Contains(text.name))
                    candidates.Add(text);
                else if (options.DetectHeaders && LooksLikeHeader(text, body))
                    candidates.Add(text);
            }
            if (candidates.Count == 0)
                return;

            TMP_Text title = null;
            if (options.Titles.Count == 0)
            {
                // the panel title: the largest header that is not inside a scroll view
                title = candidates.Where(t => t.GetComponentInParent<ScrollRect>() == null).OrderByDescending(t => t.fontSize).FirstOrDefault();
            }
            foreach (var text in candidates)
            {
                if (options.Titles.Contains(text.name) || text == title)
                    StyleTitle(text, options);
                else
                    StyleSectionHeader(text, options);
            }
        }

        /// <summary>The settings screen's title look: Norsebold, large, light brown, centred, no divider.</summary>
        public static void StyleTitle(TMP_Text text, RestyleOptions options)
        {
            var reference = Auga.Assets.SettingsPrefab != null ? Auga.Assets.SettingsPrefab.transform.Find("panel/PlayerPanelTitle")?.GetComponent<TMP_Text>() : null;
            var font = reference != null ? reference.font : FontOf(Auga.Assets.ButtonFancy);
            if (font != null)
            {
                text.font = font;
                if (reference != null && reference.fontSharedMaterial != null) text.fontSharedMaterial = reference.fontSharedMaterial;
            }
            text.fontSize = options.TitleFontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = WithAlpha(Brown2, text.color.a);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
        }

        /// <summary>A section header: Source Sans Pro Bold, all caps, light, inside a DividerMedium.</summary>
        public static void StyleSectionHeader(TMP_Text text, RestyleOptions options)
        {
            WrapInDivider(text, false);
            if (BoldFont != null) text.font = BoldFont;
            text.fontSize = options.HeaderFontSize;
            text.fontStyle = FontStyles.UpperCase;
            text.color = WithAlpha(Brown1, text.color.a);
            text.enableAutoSizing = false;
        }

        private static bool LooksLikeHeader(TMP_Text text, float bodySize)
        {
            var value = text.text ?? string.Empty;
            if (value.Length > 80 || value.Contains("\n"))
                return false;
            var name = text.name.ToLowerInvariant();
            if (name.Contains("header") || name.Contains("title") || name.Contains("topic"))
                return true;
            return text.fontSize >= bodySize * 1.2f;
        }

        /// <summary>Puts a header text into the middle of a DividerLarge / DividerMedium that takes the text's place.</summary>
        public static Transform WrapInDivider(TMP_Text text, bool large)
        {
            var prefab = large ? Auga.Assets.DividerLarge : Auga.Assets.DividerMedium;
            if (prefab == null || text == null)
                return null;
            var textRect = text.rectTransform;
            var parent = textRect.parent;
            var index = textRect.GetSiblingIndex();

            var divider = Object.Instantiate(prefab, parent, false);
            divider.name = text.name + "Divider";
            divider.transform.SetSiblingIndex(index);
            var rect = (RectTransform)divider.transform;
            var height = rect.sizeDelta.y;
            rect.anchorMin = textRect.anchorMin;
            rect.anchorMax = textRect.anchorMax;
            rect.pivot = textRect.pivot;
            rect.anchoredPosition = textRect.anchoredPosition;
            var stretched = !Mathf.Approximately(textRect.anchorMin.x, textRect.anchorMax.x);
            rect.sizeDelta = new Vector2(stretched ? textRect.sizeDelta.x : Mathf.Max(textRect.rect.width, 200f), height);
            if (parent.GetComponent<LayoutGroup>() != null)
            {
                var element = divider.GetComponent<LayoutElement>() ?? divider.AddComponent<LayoutElement>();
                element.preferredHeight = height;
                element.minHeight = height;
                var old = text.GetComponent<LayoutElement>();
                if (old != null)
                {
                    element.flexibleWidth = old.flexibleWidth;
                    element.ignoreLayout = old.ignoreLayout;
                }
            }

            var content = divider.transform.Find("Content") as RectTransform;
            if (content == null)
                return divider.transform;
            foreach (var oldElement in text.GetComponents<LayoutElement>()) Object.DestroyImmediate(oldElement);
            foreach (var fitter in text.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(fitter);
            textRect.SetParent(content, false);
            Stretch(textRect);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = Vector4.zero;
            var header = divider.AddComponent<AugaDividerHeader>();
            header.Content = content;
            header.Text = text;
            return divider.transform;
        }

        // ------------------------------------------------------------------ scrollbars

        private static void ReplaceScrollbar(Scrollbar old, Dictionary<Object, Object> map, List<GameObject> doomed)
        {
            var oldRect = (RectTransform)old.transform;
            var vertical = old.direction == Scrollbar.Direction.BottomToTop || old.direction == Scrollbar.Direction.TopToBottom;
            if (Auga.Assets.ScrollBar == null)
            {
                RestyleScrollbarInPlace(old, vertical);
                return;
            }

            var go = Object.Instantiate(Auga.Assets.ScrollBar, oldRect.parent, false);
            go.name = old.name;
            go.transform.SetSiblingIndex(oldRect.GetSiblingIndex());
            var rect = (RectTransform)go.transform;
            CopyPlacement(oldRect, rect);
            rect.sizeDelta = vertical ? new Vector2(ScrollbarWidth, oldRect.sizeDelta.y) : new Vector2(oldRect.sizeDelta.x, ScrollbarWidth);
            if (vertical)
            {
                // a scrollbar inside its scroll view fills it; one placed beside the scroll view (a sibling)
                // follows the view's vertical extent instead of its parent's
                var owner = ScrollRectOf(old);
                if (owner != null && !oldRect.IsChildOf(owner.transform)) AlignVertically(rect, (RectTransform)owner.transform);
                else StretchVertically(rect);
            }
            var scrollbar = go.GetComponent<Scrollbar>();
            scrollbar.direction = old.direction;
            NormalizeHandle(scrollbar);
            scrollbar.numberOfSteps = old.numberOfSteps;
            scrollbar.size = old.size;
            scrollbar.SetValueWithoutNotify(old.value);
            scrollbar.interactable = old.interactable;
            scrollbar.onValueChanged = old.onValueChanged;
            go.SetActive(old.gameObject.activeSelf);

            map[old] = scrollbar;
            map[old.gameObject] = go;
            map[oldRect] = rect;
            doomed.Add(old.gameObject);
        }

        /// <summary>
        /// The ScrollBar prefab's handle is saved with zero-size anchors across the bar, which leaves it invisible;
        /// the Scrollbar component only drives the anchors along its axis, so the handle must fill the other one.
        /// </summary>
        public static void NormalizeHandle(Scrollbar scrollbar)
        {
            var handle = scrollbar != null ? scrollbar.handleRect : null;
            if (handle == null) return;
            var vertical = scrollbar.direction == Scrollbar.Direction.BottomToTop || scrollbar.direction == Scrollbar.Direction.TopToBottom;
            if (vertical)
            {
                handle.anchorMin = new Vector2(0f, handle.anchorMin.y);
                handle.anchorMax = new Vector2(1f, handle.anchorMax.y);
                handle.sizeDelta = new Vector2(0f, handle.sizeDelta.y);
                handle.anchoredPosition = new Vector2(0f, handle.anchoredPosition.y);
            }
            else
            {
                handle.anchorMin = new Vector2(handle.anchorMin.x, 0f);
                handle.anchorMax = new Vector2(handle.anchorMax.x, 1f);
                handle.sizeDelta = new Vector2(handle.sizeDelta.x, 0f);
                handle.anchoredPosition = new Vector2(handle.anchoredPosition.x, 0f);
            }
        }

        /// <summary>Auga's scrollbar look on a vanilla scrollbar (used when the bundle has no ScrollBar prefab).</summary>
        public static void RestyleScrollbarInPlace(Scrollbar scrollbar, bool vertical)
        {
            var rect = (RectTransform)scrollbar.transform;
            rect.sizeDelta = vertical ? new Vector2(ScrollbarWidth, rect.sizeDelta.y) : new Vector2(rect.sizeDelta.x, ScrollbarWidth);
            if (vertical) StretchVertically(rect);
            var background = scrollbar.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = Brown5;
            }
            var handle = scrollbar.handleRect != null ? scrollbar.handleRect.GetComponent<Image>() : null;
            if (handle != null)
            {
                handle.sprite = null;
                handle.type = Image.Type.Simple;
                handle.color = ScrollHandle;
            }
            scrollbar.transition = Selectable.Transition.None;
        }

        /// <summary>Runs a vertical scrollbar over the full height of its parent, keeping its horizontal placement.</summary>
        /// <summary>The ScrollRect driving <paramref name="scrollbar"/>: an ancestor, or a scroll view next to it that references it.</summary>
        private static ScrollRect ScrollRectOf(Scrollbar scrollbar)
        {
            var ancestor = scrollbar.GetComponentInParent<ScrollRect>();
            if (ancestor != null) return ancestor;
            var scope = scrollbar.transform.parent != null ? scrollbar.transform.parent : scrollbar.transform;
            return scope.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault(s => s.verticalScrollbar == scrollbar || s.horizontalScrollbar == scrollbar);
        }

        /// <summary><paramref name="rect"/> spans the vertical extent of <paramref name="target"/> (its horizontal placement stays).</summary>
        public static void AlignVertically(RectTransform rect, RectTransform target)
        {
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var bottom = parent.InverseTransformPoint(corners[0]).y - parent.rect.yMin;
            var top = parent.InverseTransformPoint(corners[1]).y - parent.rect.yMin;
            var left = rect.offsetMin.x;
            var right = rect.offsetMax.x;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 0f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        public static void StretchVertically(RectTransform rect)
        {
            var left = rect.offsetMin.x;
            var right = rect.offsetMax.x;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 0f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(right, 0f);
        }

        /// <summary>A vertical Auga scrollbar (the ScrollBar prefab, or one built to its look) under <paramref name="parent"/>.</summary>
        public static Scrollbar CreateScrollbar(Transform parent, string name)
        {
            GameObject go;
            if (Auga.Assets.ScrollBar != null)
            {
                go = Object.Instantiate(Auga.Assets.ScrollBar, parent, false);
                go.name = name;
                var prefabScrollbar = go.GetComponent<Scrollbar>();
                NormalizeHandle(prefabScrollbar);
                return prefabScrollbar;
            }
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            go.transform.SetParent(parent, false);
            var background = go.GetComponent<Image>();
            background.color = Brown5;
            var area = new GameObject("Sliding Area", typeof(RectTransform)).transform as RectTransform;
            area.SetParent(go.transform, false);
            Stretch(area);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).transform as RectTransform;
            handle.SetParent(area, false);
            Stretch(handle); // the Scrollbar drives the anchors along its axis; the handle fills the other axis
            handle.GetComponent<Image>().color = ScrollHandle;
            var scrollbar = go.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.None;
            ((RectTransform)go.transform).sizeDelta = new Vector2(ScrollbarWidth, 100f);
            return scrollbar;
        }

        // ------------------------------------------------------------------ sliders and toggles, in place

        /// <summary>
        /// A vanilla slider takes the look of Auga's slider widget (dark track, light fill, diamond handle) without
        /// being replaced: scripts such as KeySlider sit on the slider object and reach it through GetComponentInParent.
        /// </summary>
        public static void RestyleSliderInPlace(Slider slider)
        {
            var template = Auga.Assets.LabeledSliderWithValue != null ? Auga.Assets.LabeledSliderWithValue.GetComponentInChildren<Slider>(true) : null;
            if (slider == null || template == null)
                return;
            var handle = ImageOf(slider.handleRect);
            var templateHandle = ImageOf(template.handleRect);
            if (handle == null || templateHandle == null || handle.sprite == templateHandle.sprite)
                return;   // Auga's own
            CopyLook(TrackOf(slider), TrackOf(template));
            CopyLook(ImageOf(slider.fillRect), ImageOf(template.fillRect));
            CopyLook(handle, templateHandle);
            var handleRect = slider.handleRect;
            var stretched = !Mathf.Approximately(handleRect.anchorMin.y, handleRect.anchorMax.y);
            handleRect.sizeDelta = new Vector2(12f, stretched ? -8f : 14f);
        }

        /// <summary>A vanilla toggle takes the look of Auga's checkbox (diamond box and mark) in place.</summary>
        public static void RestyleToggleInPlace(Toggle toggle)
        {
            var template = Auga.Assets.LabeledCheckbox != null ? Auga.Assets.LabeledCheckbox.GetComponent<Toggle>() : null;
            if (toggle == null || template == null)
                return;
            var mark = toggle.graphic as Image;
            var templateMark = template.graphic as Image;
            if (mark == null || templateMark == null || mark.sprite == templateMark.sprite)
                return;   // Auga's own
            var box = mark.transform.parent != null ? mark.transform.parent.GetComponent<Image>() : null;
            var templateBox = templateMark.transform.parent != null ? templateMark.transform.parent.GetComponent<Image>() : null;
            CopyLook(box, templateBox);
            CopyLook(mark, templateMark);
            if (box != null && templateBox != null)
                ((RectTransform)box.transform).sizeDelta = ((RectTransform)templateBox.transform).sizeDelta;
            var markRect = (RectTransform)mark.transform;
            markRect.anchorMin = markRect.anchorMax = new Vector2(0.5f, 0.5f);
            markRect.anchoredPosition = Vector2.zero;
            markRect.sizeDelta = ((RectTransform)templateMark.transform).sizeDelta;
        }

        private static Image TrackOf(Slider slider)
        {
            var background = slider.transform.Find("Background");
            return background != null ? background.GetComponent<Image>() : slider.GetComponent<Image>();
        }

        private static Image ImageOf(RectTransform rect)
        {
            return rect != null ? rect.GetComponent<Image>() : null;
        }

        private static void CopyLook(Image to, Image from)
        {
            if (to == null || from == null) return;
            to.sprite = from.sprite;
            to.color = from.color;
            to.type = from.type;
            to.material = from.material;
        }

        // ------------------------------------------------------------------ buttons

        private static void ReplaceButton(Button old, RestyleOptions options, Dictionary<Object, Object> map, List<GameObject> doomed)
        {
            // text links (a Button on a bare text), Auga's own buttons and the parts of other controls stay
            if (old is ColorButtonText || old.GetComponent<Image>() == null) return;
            if (old.GetComponentInParent<Scrollbar>() != null || old.GetComponent<TMP_Dropdown>() != null || old.GetComponentInParent<TMP_Dropdown>() != null) return;

            var oldRect = (RectTransform)old.transform;
            var width = oldRect.rect.width > 0f ? oldRect.rect.width : oldRect.sizeDelta.x;
            var prefab = options.ButtonPrefab != null ? options.ButtonPrefab : width <= 95f ? Auga.Assets.ButtonSmall : width <= 200f ? Auga.Assets.ButtonMedium : Auga.Assets.ButtonFancy;
            if (prefab == null) return;

            var go = Object.Instantiate(prefab, oldRect.parent, false);
            go.name = old.name;
            go.transform.SetSiblingIndex(oldRect.GetSiblingIndex());
            var rect = (RectTransform)go.transform;
            var native = ((RectTransform)prefab.transform).sizeDelta;
            CopyPlacement(oldRect, rect);
            var stretched = !Mathf.Approximately(oldRect.anchorMin.x, oldRect.anchorMax.x);
            // the Auga button's own height, but never wider than the vanilla button's slot (side-by-side buttons)
            var buttonWidth = width > 0f ? Mathf.Min(native.x, width) : native.x;
            rect.sizeDelta = new Vector2(stretched ? oldRect.sizeDelta.x : buttonWidth, native.y);
            if (oldRect.parent.GetComponent<LayoutGroup>() != null)
            {
                var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                element.preferredWidth = buttonWidth;
                element.preferredHeight = native.y;
            }

            var label = go.transform.Find("Label")?.GetComponent<TMP_Text>() ?? go.GetComponentInChildren<TMP_Text>(true);
            var oldLabel = old.transform.Find("Text")?.GetComponent<TMP_Text>() ?? old.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.GetComponentInParent<UIGamePad>() == null || t.transform.parent == old.transform);
            if (label != null && oldLabel != null)
            {
                label.text = RawText(oldLabel);
                if (oldLabel.GetComponent<Localize>() != null && label.GetComponent<Localize>() == null)
                    label.gameObject.AddComponent<Localize>();
                // the Auga button is often narrower than the vanilla one: a long label shrinks instead of wrapping
                var size = label.fontSize;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMax = size;
                label.fontSizeMin = Mathf.Max(8f, size * 0.7f);
            }

            var button = go.GetComponent<Button>();
            button.onClick = old.onClick;
            button.interactable = old.interactable;
            button.navigation = old.navigation;
            var pad = old.GetComponent<UIGamePad>();
            if (pad != null)
            {
                var copy = go.AddComponent<UIGamePad>();
                SerializedFieldHelper.CopyMissingFields(copy, pad, go.transform);
            }
            go.SetActive(old.gameObject.activeSelf);

            map[old.gameObject] = go;
            map[old] = button;
            map[oldRect] = rect;
            doomed.Add(old.gameObject);
        }

        // ------------------------------------------------------------------ re-pointing

        /// <summary>
        /// Replaces every reference to a replaced object (serialized fields of any component, ScrollRect scrollbars,
        /// Selectable navigation) under <paramref name="scope"/> with the replacement.
        /// </summary>
        public static void Repoint(Transform scope, Dictionary<Object, Object> map)
        {
            if (map.Count == 0)
                return;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var component in scope.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                if (component is ScrollRect scrollRect)
                {
                    if (scrollRect.verticalScrollbar != null && map.TryGetValue(scrollRect.verticalScrollbar, out var v) && v is Scrollbar vs) scrollRect.verticalScrollbar = vs;
                    if (scrollRect.horizontalScrollbar != null && map.TryGetValue(scrollRect.horizontalScrollbar, out var h) && h is Scrollbar hs) scrollRect.horizontalScrollbar = hs;
                    continue;
                }
                if (component is Selectable selectable)
                {
                    var navigation = selectable.navigation;
                    var changed = false;
                    if (navigation.selectOnUp != null && map.TryGetValue(navigation.selectOnUp, out var up) && up is Selectable u) { navigation.selectOnUp = u; changed = true; }
                    if (navigation.selectOnDown != null && map.TryGetValue(navigation.selectOnDown, out var down) && down is Selectable d) { navigation.selectOnDown = d; changed = true; }
                    if (navigation.selectOnLeft != null && map.TryGetValue(navigation.selectOnLeft, out var left) && left is Selectable l) { navigation.selectOnLeft = l; changed = true; }
                    if (navigation.selectOnRight != null && map.TryGetValue(navigation.selectOnRight, out var right) && right is Selectable r) { navigation.selectOnRight = r; changed = true; }
                    if (changed) selectable.navigation = navigation;
                }
                if (!(component is MonoBehaviour))
                    continue;
                for (var type = component.GetType(); type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour); type = type.BaseType)
                {
                    foreach (var field in type.GetFields(flags | BindingFlags.DeclaredOnly))
                    {
                        if (field.IsStatic) continue;
                        if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null) continue;
                        var fieldType = field.FieldType;
                        if (typeof(Object).IsAssignableFrom(fieldType))
                        {
                            var value = field.GetValue(component) as Object;
                            if (value != null && map.TryGetValue(value, out var replacement) && replacement != null && fieldType.IsInstanceOfType(replacement))
                                field.SetValue(component, replacement);
                        }
                        else if (typeof(IList).IsAssignableFrom(fieldType))
                        {
                            if (!(field.GetValue(component) is IList list)) continue;
                            for (var i = 0; i < list.Count; i++)
                            {
                                if (list[i] is Object item && map.TryGetValue(item, out var replacement) && replacement != null)
                                {
                                    var elementType = fieldType.IsArray ? fieldType.GetElementType() : fieldType.IsGenericType ? fieldType.GetGenericArguments()[0] : null;
                                    if (elementType != null && elementType.IsInstanceOfType(replacement))
                                        list[i] = replacement;
                                }
                            }
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        private static bool IsSkipped(Transform t, Transform panel, RestyleOptions options)
        {
            if (options.Skip.Count == 0)
                return false;
            for (var current = t; current != null && current != panel; current = current.parent)
            {
                if (options.Skip.Contains(current.name))
                    return true;
            }
            return false;
        }

        private static TMP_FontAsset FontOf(GameObject prefab)
        {
            return prefab != null ? prefab.GetComponentInChildren<TMP_Text>(true)?.font : null;
        }

        public static string RawText(TMP_Text text)
        {
            if (text == null) return string.Empty;
            return Localization.instance != null && Localization.instance.textMeshStrings.TryGetValue(text, out var raw) ? raw : text.text;
        }

        public static void CopyPlacement(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.white;
        }
    }
}
