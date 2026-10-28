using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AugaUnity;
using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valheim.SettingsGui;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>Marks a settings page whose controls were rebuilt from Auga widgets.</summary>
    public class AugaSettingsPage : MonoBehaviour
    {
    }

    /// <summary>
    /// Shrinks a label's font (down to MinScale of its size) when the localized text is wider than its box, e.g.
    /// "Controller layout" next to a dropdown. Runs a frame after the object is enabled, once localization is done.
    /// </summary>
    public class AugaFitLabel : MonoBehaviour
    {
        public float MinScale = 0.6f;
        /// <summary>A sibling drawn over the right part of the label (the dropdown box); the label must end before it.</summary>
        public RectTransform Obstacle;
        private float _baseSize;
        private string _fittedText;
        private TMP_Text _text;

        private void OnEnable()
        {
            _fittedText = null;
        }

        private void LateUpdate()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null || _text.text == _fittedText)
                return;
            Fit();
        }

        private void Fit()
        {
            if (_baseSize <= 0f) _baseSize = _text.fontSize;
            _text.fontSize = _baseSize;
            _fittedText = _text.text;
            var width = _text.rectTransform.rect.width;
            if (Obstacle != null && _text.rectTransform.parent is RectTransform parent)
                width = Mathf.Min(width, parent.rect.width - Obstacle.rect.width - 6f);
            if (width <= 0f || string.IsNullOrEmpty(_text.text))
                return;
            var preferred = _text.GetPreferredValues(_text.text, Mathf.Infinity, Mathf.Infinity).x;
            if (preferred > width)
                _text.fontSize = Mathf.Max(_baseSize * MinScale, _baseSize * width / preferred * 0.97f);
        }
    }

    /// <summary>
    /// Runtime glue for the controls vanilla drives through stepper buttons (language, controller layout, graphics
    /// preset), which Auga shows as dropdowns, plus the language tooltip. Lives on the settings root; Start() runs
    /// after Settings.Awake has initialized every tab.
    /// </summary>
    public class AugaSettingsGlue : MonoBehaviour
    {
        public GuiDropdown LanguageDropdown;
        public GameplaySettings Gameplay;
        public GameObject LanguageTooltip;
        public GuiDropdown PresetDropdown;
        public GraphicsSettings Graphics;
        public GuiDropdown LayoutDropdown;
        public GamepadSettings Gamepad;

        private static readonly MethodInfo UpdateLanguageText = AccessTools.Method(typeof(GameplaySettings), "UpdateLanguageText");
        private static readonly MethodInfo OnLayoutChanged = AccessTools.Method(typeof(GamepadSettings), "OnLayoutChanged");

        private void Start()
        {
            if (LanguageDropdown != null && Gameplay != null) SetupLanguage();
            if (PresetDropdown != null && Graphics != null) SetupPreset();
            if (LayoutDropdown != null && Gamepad != null) SetupLayout();
        }

        private void SetupLanguage()
        {
            var languages = Localization.instance.GetLanguages();
            LanguageDropdown.ClearOptions();
            LanguageDropdown.AddOptions(languages.Select(l => Localization.instance.Localize("$language_" + l.ToLower())).ToList());
            var current = languages.IndexOf(Gameplay.m_languageKey);
            LanguageDropdown.SetValueWithoutNotify(Mathf.Max(0, current));
            UpdateLanguageText?.Invoke(Gameplay, null);
            LanguageDropdown.onValueChanged.AddListener(index =>
            {
                if (index < 0 || index >= languages.Count) return;
                Gameplay.m_languageKey = languages[index];
                UpdateLanguageText?.Invoke(Gameplay, null);
            });

            if (LanguageTooltip != null)
            {
                LanguageTooltip.SetActive(false);
                var trigger = LanguageDropdown.gameObject.GetComponent<EventTrigger>() ?? LanguageDropdown.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => LanguageTooltip.SetActive(true));
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => LanguageTooltip.SetActive(false));
                trigger.triggers.Add(enter);
                trigger.triggers.Add(exit);
            }
        }

        private void SetupPreset()
        {
            var config = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
            if (config == null) return;
            var offset = config.HasCustomPreset ? 1 : 0;
            var options = new List<string>();
            if (config.HasCustomPreset) options.Add(Localization.instance.Localize("$settings_quality_mode_custom"));
            foreach (var preset in config.Presets)
                options.Add(Localization.instance.Localize(preset.m_type.NameTextId));
            PresetDropdown.ClearOptions();
            PresetDropdown.AddOptions(options);
            PresetDropdown.SetValueWithoutNotify(Mathf.Clamp(config.GetPresetIndexByID(Graphics.m_currentPresetID) + offset, 0, options.Count - 1));
            PresetDropdown.onValueChanged.AddListener(index =>
            {
                var current = config.GetPresetIndexByID(Graphics.m_currentPresetID);
                Graphics.ChangePreset(index - offset - current);
            });
        }

        private void SetupLayout()
        {
            var layouts = new[] { InputLayout.Default, InputLayout.Alternative1, InputLayout.Alternative2 };
            LayoutDropdown.ClearOptions();
            LayoutDropdown.AddOptions(layouts.Select(l => Localization.instance.Localize(GamepadMapController.GetLayoutStringId(l))).ToList());
            LayoutDropdown.SetValueWithoutNotify(Mathf.Clamp((int)Gamepad.m_currentControllerLayout, 0, layouts.Length - 1));
            LayoutDropdown.onValueChanged.AddListener(index =>
            {
                Gamepad.m_currentControllerLayout = layouts[Mathf.Clamp(index, 0, layouts.Length - 1)];
                OnLayoutChanged?.Invoke(Gamepad, null);
            });
        }
    }

    /// <summary>Keeps a DividerMedium's Content as wide as the header text inside it (the fitter sizes the lines around it).</summary>
    public class AugaDividerHeader : MonoBehaviour
    {
        public RectTransform Content;
        public TMP_Text Text;
        public float Padding = 14f;

        private string _fittedText;

        private void LateUpdate()
        {
            if (Content == null || Text == null || Text.text == _fittedText)
                return;
            _fittedText = Text.text;
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Text.preferredWidth + Padding * 2f);
        }
    }

    /// <summary>
    /// Restyles a vanilla controller diagram (GamepadMap) for the Auga settings screen: Auga font and text colours,
    /// Auga diamond dots and line colour, and the vanilla scale the artwork was drawn for. Applied once per map
    /// instance by the GamepadMapController.Show postfix; the marker component records that.
    /// </summary>
    public class AugaGamepadMapStyle : MonoBehaviour
    {
        public static readonly Color LineColor = new Color32(0xEA, 0xA8, 0x00, 0xFF);
        public static readonly Color LightText = new Color32(0xEA, 0xE1, 0xD9, 0xFF);
        public static readonly Color GrayText = new Color32(0xA3, 0x96, 0x89, 0xFF);
        public const string VanillaGrayTag = "<color=#AAAAAA>";
        public const string GrayTag = "<color=#A39689>";

        public static void Apply(GamepadMap map)
        {
            if (map == null || map.GetComponent<AugaGamepadMapStyle>() != null)
                return;
            map.gameObject.AddComponent<AugaGamepadMapStyle>();
            map.transform.localScale = AugaSettingsBuilder.VanillaScale;

            var font = AugaSettingsBuilder.SemiBoldFont;
            var texts = 0;
            foreach (var text in map.GetComponentsInChildren<TMP_Text>(true))
            {
                if (font != null) text.font = font;
                text.color = Recolor(text.color);
                texts++;
            }

            // the dots: the small images that anchor the lines, on the labels' side and on the controller itself.
            // Identified by the sprite of a label-side dot (an image with a label directly under it).
            Sprite dotSprite = null;
            foreach (var label in map.GetComponentsInChildren<GamepadMapLabel>(true))
            {
                var image = label.transform.parent != null ? label.transform.parent.GetComponent<Image>() : null;
                if (image != null && image.sprite != null && image.transform != map.transform) { dotSprite = image.sprite; break; }
            }
            var dots = 0;
            foreach (var image in map.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == map.transform) continue;
                var isDot = dotSprite != null ? image.sprite == dotSprite : IsSmall(image.rectTransform);
                if (!isDot) continue;
                if (Auga.Assets.ContainerDiamond != null) image.sprite = Auga.Assets.ContainerDiamond;
                image.color = LineColor;
                dots++;
            }
            foreach (var graphic in map.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetType().Name.IndexOf("LineRenderer", StringComparison.Ordinal) >= 0)
                    graphic.color = LineColor;
            }
            Debug.Log($"[Auga] Controller diagram '{map.name}' restyled: {texts} texts, {dots} dots (dot sprite {(dotSprite != null ? dotSprite.name : "by size")}).");
        }

        /// <summary>Vanilla marks secondary functions with a gray colour tag; swap it for Auga's gray.</summary>
        public static void RecolorText(GamepadMap map)
        {
            foreach (var text in map.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text != null && text.text.IndexOf(VanillaGrayTag, StringComparison.OrdinalIgnoreCase) >= 0)
                    text.text = text.text.Replace(VanillaGrayTag, GrayTag).Replace(VanillaGrayTag.ToLowerInvariant(), GrayTag);
            }
        }

        private static bool IsSmall(RectTransform rect)
        {
            var size = rect.rect.size;
            return size.x > 0f && size.x <= 40f && size.y > 0f && size.y <= 40f;
        }

        private static Color Recolor(Color color)
        {
            var max = Mathf.Max(color.r, color.g, color.b);
            var min = Mathf.Min(color.r, color.g, color.b);
            if (max - min > 0.08f)
                return color; // a real colour (button glyph tints), not white or gray
            var target = max > 0.9f ? LightText : GrayText;
            return new Color(target.r, target.g, target.b, color.a);
        }
    }

    [HarmonyPatch(typeof(GamepadMapController), nameof(GamepadMapController.Show))]
    public static class GamepadMapController_Show_Patch
    {
        public static void Postfix(GamepadMapController __instance)
        {
            if (__instance.GetComponentInParent<AugaSettingsPage>(true) == null)
                return;
            foreach (var map in __instance.GetComponentsInChildren<GamepadMap>(true))
                AugaGamepadMapStyle.Apply(map);
        }
    }

    [HarmonyPatch(typeof(GamepadMap), nameof(GamepadMap.UpdateMap))]
    public static class GamepadMap_UpdateMap_Patch
    {
        public static void Postfix(GamepadMap __instance)
        {
            if (__instance.GetComponentInParent<AugaSettingsPage>(true) == null)
                return;
            AugaGamepadMapStyle.RecolorText(__instance);
        }
    }

    /// <summary>
    /// Builds the Auga settings screen from the vanilla settings prefab: the vanilla root (Settings component,
    /// canvas) and every tab page component with its serialized event wiring are kept, the visuals come from the
    /// AugaSettings frame, and every control is rebuilt option for option from Auga's widget prefabs
    /// (LabeledCheckbox, LabeledSliderWithValue, LabeledDropdown, ButtonSettings, LabeledKeybind). The page
    /// component fields are re-pointed at the new widgets, so the vanilla tab code keeps driving them.
    /// The result is kept as an inactive template and handed to Menu/FejdStartup as their settings prefab.
    /// </summary>
    public static class AugaSettingsBuilder
    {
        private static readonly Dictionary<int, GameObject> Built = new Dictionary<int, GameObject>();
        private static GameObject _holder;
        private static Vector3 _vanillaScale = Vector3.one;
        private static Transform _headerTemplate;
        private static TMP_FontAsset _semiBold;
        private static bool _semiBoldTried;

        /// <summary>Scale of the vanilla settings root, which the kept vanilla artwork (controller diagram) is drawn for.</summary>
        public static Vector3 VanillaScale => _vanillaScale;

        /// <summary>
        /// Source Sans Pro SemiBold as a TextMeshPro font: the bundle only ships it as a classic Font, so a dynamic
        /// SDF font asset is created from it once (falling back to the widget font if that is not possible).
        /// </summary>
        public static TMP_FontAsset SemiBoldFont
        {
            get
            {
                if (_semiBoldTried)
                    return _semiBold;
                _semiBoldTried = true;
                var widgetFont = Auga.Assets.LabeledCheckbox != null ? Auga.Assets.LabeledCheckbox.GetComponentInChildren<TMP_Text>(true)?.font : null;
                try
                {
                    var font = Auga.Assets.SourceSansProSemiBold;
                    if (font != null)
                    {
                        _semiBold = TMP_FontAsset.CreateFontAsset(font);
                        if (_semiBold != null)
                        {
                            _semiBold.name = "SourceSansPro-SemiBold SDF (runtime)";
                            if (widgetFont != null)
                                _semiBold.fallbackFontAssetTable = new List<TMP_FontAsset> { widgetFont };
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Auga] Could not create the Source Sans Pro SemiBold text font: " + e.Message);
                }
                if (_semiBold == null)
                    _semiBold = widgetFont;
                return _semiBold;
            }
        }

        // vanilla objects that are kept as they are (moved along, hidden or shown by the vanilla code)
        private static readonly HashSet<string> KeepNames = new HashSet<string>
        {
            "SettingsTooltip", "BindDialog", "ResolutionDialog", "ResolutionSwitchDialog", "GamepadMap",
            "SubtabsGroup", "ControllerSettings", "SwitchMouseSettings", "MotionSensorXAxisSens", "MotionSensorYAxisSens",
            "ControllerSpeakerVolume", "QualitySlider", "QualityToggles", "Bindings",
            "DevBuildSettingsText", "DevBuildSettingFrame", "DevGraphicsModeValuesText", "DevPlayerPrefsValuesText",
        };

        private static readonly HashSet<string> KeepAtPageRoot = new HashSet<string> { "SettingsTooltip", "BindDialog", "ResolutionDialog", "ResolutionSwitchDialog" };

        private enum RowKind { Toggle, Slider, Dropdown, Button, Stepper }

        private sealed class VanillaRow
        {
            public RowKind Kind;
            public Transform Row;          // object that is replaced
            public Component Control;      // Toggle / Slider / TMP_Dropdown / Button / GuiStepper
            public TMP_Text Label;
            public bool Active;
            public MonoBehaviour Page;     // page component whose fields reference this row
        }

        private sealed class AugaRow
        {
            public VanillaRow Source;
            public Transform Root;
            public Toggle Toggle;
            public Slider Slider;
            public GuiDropdown Dropdown;
            public Button Button;
            public TMP_Text Label;
            public TMP_Text Value;
            public TMP_Text Caption;
            public Button LeftDummy;
            public Button RightDummy;
        }

        public static GameObject GetPrefab(GameObject vanillaPrefab)
        {
            if (vanillaPrefab == null || Auga.Assets.SettingsPrefab == null)
                return vanillaPrefab;
            var id = vanillaPrefab.GetInstanceID();
            if (Built.TryGetValue(id, out var existing) && existing != null)
                return existing;
            try
            {
                var built = Build(vanillaPrefab);
                Built[id] = built;
                Debug.Log("[Auga] Settings screen built from the vanilla settings prefab.");
                return built;
            }
            catch (Exception e)
            {
                // Unity's log, not Auga's (which is off by default): a broken settings screen must be visible
                Debug.LogError("[Auga] Building the settings screen failed, keeping the vanilla one: " + e);
                return vanillaPrefab;
            }
        }

        private static GameObject Build(GameObject vanillaPrefab)
        {
            if (_holder == null)
            {
                _holder = new GameObject("AugaSettingsPrefabs");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }

            var root = Object.Instantiate(vanillaPrefab, _holder.transform, false);
            root.name = "AugaSettings";
            // The vanilla settings root is scaled up (1.3 at the time of writing) and its Panel is laid out for that.
            // The Auga frame is designed 1:1 for the 1920x1080 reference resolution, so the root is reset to scale 1
            // and only the vanilla pieces kept as they are (the controller diagram) get the vanilla scale back.
            var vanillaScale = root.transform.localScale;
            root.transform.localScale = Vector3.one;
            _vanillaScale = new Vector3(vanillaScale.x, vanillaScale.y, 1f);
            var frame = Object.Instantiate(Auga.Assets.SettingsPrefab, _holder.transform, false);
            RemoveComponent<Settings>(frame);

            var settings = root.GetComponent<Settings>();
            var vanillaPanel = root.transform.Find("Panel");
            // the main tab bar; the gamepad page carries a second TabHandler for its Controller/Mouse sub tabs
            var vanillaTabHandler = vanillaPanel != null ? vanillaPanel.Find("TabButtons")?.GetComponent<TabHandler>() : null;
            if (vanillaTabHandler == null)
                vanillaTabHandler = root.GetComponentInChildren<TabHandler>();
            var panel = frame.transform.Find("panel");
            if (settings == null || vanillaPanel == null || vanillaTabHandler == null || panel == null)
                throw new InvalidOperationException("unexpected settings prefab layout");

            var augaTabHandler = panel.Find("TabButtons")?.GetComponent<TabHandler>() ?? panel.GetComponentInChildren<TabHandler>(true);
            var tabButtonsParent = augaTabHandler.transform.Find("Tabs");
            var tabButtonTemplate = tabButtonsParent.GetChild(0);
            _headerTemplate = tabButtonTemplate.Find("Selected");
            var pagesParent = panel.Find("Tabs");
            var templates = new Dictionary<string, RectTransform>();
            foreach (Transform child in pagesParent)
                templates[child.name] = (RectTransform)child;

            CopyImage(frame.GetComponent<Image>(), root.GetComponent<Image>());
            panel.SetParent(root.transform, false);
            panel.SetSiblingIndex(vanillaPanel.GetSiblingIndex());
            var bindDialog = frame.transform.Find("BindDialog");
            if (bindDialog != null)
                bindDialog.SetParent(root.transform, false);

            var augaBack = panel.Find("Back").GetComponent<Button>();
            var augaOk = panel.Find("Ok").GetComponent<Button>();
            var vanillaBack = vanillaPanel.Find("Back")?.GetComponent<Button>();
            var vanillaOk = vanillaPanel.Find("Ok")?.GetComponent<Button>();
            if (vanillaBack != null) augaBack.onClick = vanillaBack.onClick;
            if (vanillaOk != null) augaOk.onClick = vanillaOk.onClick;
            settings.m_settingsPanel = panel.gameObject;
            settings.m_tabHandler = augaTabHandler;
            settings.m_backButton = augaBack;
            settings.m_okButton = augaOk;
            settings.m_tabKeyHints = new GameObject[0];
            augaTabHandler.m_cycling = vanillaTabHandler.m_cycling;
            augaTabHandler.m_tabKeyInput = vanillaTabHandler.m_tabKeyInput;
            augaTabHandler.m_keybaordInput = vanillaTabHandler.m_keybaordInput;
            augaTabHandler.m_gamepadInput = vanillaTabHandler.m_gamepadInput;
            augaTabHandler.m_keyboardNavigateLeft = vanillaTabHandler.m_keyboardNavigateLeft;
            augaTabHandler.m_keyboardNavigateRight = vanillaTabHandler.m_keyboardNavigateRight;
            augaTabHandler.m_gamepadNavigateLeft = vanillaTabHandler.m_gamepadNavigateLeft;
            augaTabHandler.m_gamepadNavigateRight = vanillaTabHandler.m_gamepadNavigateRight;

            var glue = root.AddComponent<AugaSettingsGlue>();

            // every page vanilla knows about, in tab order; pages without a tab entry (if any) are appended
            var vanillaTabs = vanillaTabHandler.m_tabs.Where(t => t.m_page != null).ToList();
            var tabContent = vanillaPanel.Find("TabContent");
            var pages = vanillaTabs.Select(t => t.m_page).ToList();
            if (tabContent != null)
            {
                foreach (Transform child in tabContent)
                {
                    if (!pages.Contains(child) && child.GetComponent<ISettingsTab>() != null)
                    {
                        pages.Add((RectTransform)child);
                        vanillaTabs.Add(new TabHandler.Tab { m_page = (RectTransform)child, m_button = null, m_default = false, m_onClick = null });
                    }
                }
            }

            // the radial page has no tab of its own: its options join the gameplay page
            var radialPage = pages.FirstOrDefault(p => p.GetComponent<RadialSettings>() != null);
            var gameplayPage = pages.FirstOrDefault(p => p.GetComponent<GameplaySettings>() != null);

            var tabs = new List<TabHandler.Tab>();
            foreach (var tab in vanillaTabs)
            {
                var page = tab.m_page;
                var pageComponent = page.GetComponent<ISettingsTab>() as MonoBehaviour;
                page.SetParent(pagesParent, false);

                var button = Object.Instantiate(tabButtonTemplate.gameObject, tabButtonsParent, false);
                button.name = page.name;
                var token = tab.m_button != null ? RawText(tab.m_button.GetComponentInChildren<TMP_Text>(true)) : null;
                if (string.IsNullOrEmpty(token) && tab.m_button != null)
                    token = RawText(tab.m_button.transform.Find("Selected")?.GetComponentInChildren<TMP_Text>(true));
                if (!string.IsNullOrEmpty(token))
                {
                    SetText(button.transform, "Text", token);
                    SetText(button.transform, "Selected/Text", token);
                }
                var hidden = tab.m_button == null || page == radialPage;
                if (hidden)
                    button.SetActive(false);
                tabs.Add(new TabHandler.Tab { m_button = button.GetComponent<Button>(), m_page = page, m_default = tab.m_default && !hidden, m_onClick = tab.m_onClick });

                if (page == radialPage)
                    continue;
                var extra = page == gameplayPage && radialPage != null ? new List<RectTransform> { radialPage } : null;
                BuildPage(page, pageComponent, templates, extra, bindDialog, glue);
            }
            if (radialPage != null && gameplayPage == null)
            {
                BuildPage(radialPage, radialPage.GetComponent<RadialSettings>(), templates, null, bindDialog, glue);
            }
            if (!tabs.Any(t => t.m_default) && tabs.Count > 0)
                tabs.First(t => t.m_button.gameObject.activeSelf).m_default = true;

            Object.DestroyImmediate(tabButtonTemplate.gameObject);
            augaTabHandler.m_tabs = tabs;
            foreach (var template in templates.Values)
                Object.DestroyImmediate(template.gameObject);

            Object.DestroyImmediate(vanillaPanel.gameObject);
            Object.DestroyImmediate(frame);
            root.SetActive(true); // active itself, but under the inactive holder: no Awake until instantiated
            return root;
        }

        // ------------------------------------------------------------------ pages

        private static void BuildPage(RectTransform page, MonoBehaviour pageComponent, Dictionary<string, RectTransform> templates, List<RectTransform> extraPages, Transform bindDialog, AugaSettingsGlue glue)
        {
            var templateName = pageComponent is KeyboardMouseSettings ? "Controls"
                : pageComponent is Valheim.SettingsGui.AudioSettings || pageComponent is Valheim.SettingsGui.AccessibilitySettings ? "Audio"
                : "Gameplay";
            if (!templates.TryGetValue(templateName, out var template))
                template = templates.Values.First();

            CopyRect(template, page);
            var content = Object.Instantiate(template.gameObject, page, false);
            content.name = "AugaContent";
            content.SetActive(true);
            RemoveComponent<UIGroupHandler>(content);
            RemoveComponent<CanvasGroup>(content);
            content.AddComponent<AugaSettingsPage>();
            Stretch((RectTransform)content.transform);

            var first = content.transform.Find("FirstColumnWidgets");
            var second = content.transform.Find("SecondColumnWidgets");
            var keybinds = content.transform.Find("ItemListBkg/Mask/ItemList/KeyBindings");
            ClearChildren(first);
            ClearChildren(second);
            ClearChildren(keybinds);
            var extras = new GameObject("VanillaExtras", typeof(RectTransform)).transform;
            extras.SetParent(content.transform, false);
            Stretch((RectTransform)extras);

            // collect the vanilla rows of this page (and of pages merged into it)
            var rows = new List<VanillaRow>();
            CollectRows(page, content.transform, pageComponent, rows);
            var mergedPages = new List<(RectTransform page, MonoBehaviour component)>();
            if (extraPages != null)
            {
                foreach (var extraPage in extraPages)
                {
                    var extraComponent = extraPage.GetComponent<ISettingsTab>() as MonoBehaviour;
                    CollectRows(extraPage, null, extraComponent, rows);
                    mergedPages.Add((extraPage, extraComponent));
                }
            }

            // create the Auga widgets, first half left, second half right. Gameplay keeps its own options left and
            // the merged radial options right; the controller page puts the layout selector above the diagram at
            // the bottom of the page.
            var isGameplay = pageComponent is GameplaySettings;
            var bottom = pageComponent is GamepadSettings ? CreateBottomArea(content.transform) : null;
            var augaRows = new List<AugaRow>();
            var visible = rows.Count(r => r.Active && !(bottom != null && r.Kind == RowKind.Stepper));
            var half = (visible + 1) / 2;
            var placed = 0;
            foreach (var row in rows)
            {
                Transform column;
                if (isGameplay)
                    column = second == null || row.Page == pageComponent ? first : second;
                else if (bottom != null && row.Kind == RowKind.Stepper)
                    column = bottom;
                else
                {
                    column = second == null || placed < half ? first : second;
                    if (row.Active) placed++;
                }
                var augaRow = CreateWidget(row, column);
                if (augaRow == null) continue;
                augaRows.Add(augaRow);
            }

            if (isGameplay)
            {
                // the account button closes the first column; the radial options get a small header
                var playfab = augaRows.FirstOrDefault(r => r.Source.Page == pageComponent && r.Source.Kind == RowKind.Button
                                                           && r.Root.name.IndexOf("playfab", StringComparison.OrdinalIgnoreCase) >= 0);
                playfab?.Root.SetAsLastSibling();
                if (second != null && mergedPages.Count > 0)
                    CreateHeader(second, RadialHeaderToken(pageComponent)).SetAsFirstSibling();
            }

            // graphics: templates for the sliders/toggles the game instantiates per quality setting
            if (pageComponent is GraphicsSettings graphics)
            {
                BuildGraphicsTemplates(graphics, page, first, second ?? first, content.transform);
            }

            // the controller diagram is kept as it is. The diagram was drawn for the scaled vanilla root; the map
            // instances the controller creates get that scale back (AugaGamepadMapStyle), so its box is sized for
            // the scaled artwork: full page width at the bottom of the page, the layout selector centred above it.
            var gamepadMap = FindDescendant(page, "GamepadMap", content.transform);
            if (gamepadMap != null)
            {
                var rect = (RectTransform)gamepadMap;
                var size = rect.rect.size;
                if (bottom != null)
                {
                    // the box may reach below the page area (the page ends well above the Back/Apply divider)
                    const float bottomPadding = -22f, gap = 6f, margin = 6f, sideInset = 8f;
                    var boxHeight = Mathf.Max(size.y * _vanillaScale.y + margin * 2f, 100f);
                    // the page (panel/Tabs/page) may be wider than the panel: keep the box inside the panel
                    var panelRect = page.parent != null ? page.parent.parent as RectTransform : null;
                    var overflow = panelRect != null ? Mathf.Max(0f, page.rect.width - panelRect.rect.width) : 0f;
                    gamepadMap.SetParent(bottom, false);
                    rect.localScale = Vector3.one;
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.sizeDelta = new Vector2(-(overflow + sideInset * 2f), boxHeight);
                    rect.anchoredPosition = new Vector2(0f, bottomPadding);
                    var y = bottomPadding + boxHeight + gap;
                    foreach (var row in augaRows)
                    {
                        if (row.Root.parent != bottom) continue;
                        var rowRect = (RectTransform)row.Root;
                        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0f);
                        rowRect.pivot = new Vector2(0.5f, 0f);
                        rowRect.sizeDelta = new Vector2(320f, rowRect.sizeDelta.y);
                        rowRect.anchoredPosition = new Vector2(0f, y);
                        y += rowRect.sizeDelta.y + gap;
                    }
                }
                else
                {
                    gamepadMap.SetParent(second ?? first, false);
                    rect.localScale = Vector3.one;
                    var element = gamepadMap.GetComponent<LayoutElement>() ?? gamepadMap.gameObject.AddComponent<LayoutElement>();
                    element.ignoreLayout = false;
                    element.preferredWidth = Mathf.Max(size.x * _vanillaScale.x, 100f);
                    element.preferredHeight = Mathf.Max(size.y * _vanillaScale.y, 100f);
                }
            }

            if (pageComponent is KeyboardMouseSettings keyboard)
            {
                BuildKeyBindings(keyboard, keybinds ?? first);
            }

            MapFields(pageComponent, page, augaRows, glue, bindDialog, content.transform, extras);
            foreach (var (mergedPage, mergedComponent) in mergedPages)
            {
                MapFields(mergedComponent, mergedPage, augaRows, glue, bindDialog, content.transform, extras);
            }

            // the vanilla rows are replaced now; drop them so no container that is kept can still show them
            foreach (var row in augaRows)
            {
                if (row.Source.Row != null)
                    Object.DestroyImmediate(row.Source.Row.gameObject);
            }
            foreach (var (mergedPage, _) in mergedPages)
                CleanupPage(mergedPage, null);
            CleanupPage(page, content.transform);
        }

        /// <summary>A full-width, zero-height anchor line along the bottom of the page; its children are placed by hand.</summary>
        private static RectTransform CreateBottomArea(Transform content)
        {
            var bottom = (RectTransform)new GameObject("Bottom", typeof(RectTransform)).transform;
            bottom.SetParent(content, false);
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = Vector2.zero;
            return bottom;
        }

        /// <summary>A DividerMedium with the tab bar's selected-tab text in its middle, used as a section header.</summary>
        private static Transform CreateHeader(Transform column, string token)
        {
            var divider = Object.Instantiate(Auga.Assets.DividerMedium, column, false);
            divider.name = "RadialHeader";
            var element = divider.GetComponent<LayoutElement>() ?? divider.AddComponent<LayoutElement>();
            element.preferredHeight = ((RectTransform)divider.transform).sizeDelta.y;
            var content = divider.transform.Find("Content") as RectTransform;
            if (content != null && _headerTemplate != null)
            {
                var header = Object.Instantiate(_headerTemplate.gameObject, content, false);
                header.name = "Header";
                header.SetActive(true);
                Stretch((RectTransform)header.transform);
                var text = header.GetComponentInChildren<TMP_Text>(true);
                if (text != null)
                {
                    text.text = token;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                    // the tab text is left-aligned (the tab's layout sizes it); a header sits centred between the lines
                    text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    text.margin = Vector4.zero;
                    var fit = divider.AddComponent<AugaDividerHeader>();
                    fit.Content = content;
                    fit.Text = text;
                }
            }
            return divider.transform;
        }

        /// <summary>The label token of the vanilla "Radial Menu" settings button (the page that was merged into gameplay).</summary>
        private static string RadialHeaderToken(MonoBehaviour page)
        {
            var field = AccessTools.Field(page.GetType(), "m_radialSettingsButton");
            var button = field?.GetValue(page) as Button;
            var token = button != null ? RawText(FindLabel(button.transform, null)) : null;
            return string.IsNullOrEmpty(token) ? "$settings_radial" : token;
        }

        /// <summary>Destroys the vanilla content containers of a page, keeping the Auga content and the page-level dialogs.</summary>
        private static void CleanupPage(RectTransform page, Transform content)
        {
            var doomed = new List<GameObject>();
            foreach (Transform child in page)
            {
                if (child == content || KeepAtPageRoot.Contains(child.name))
                    continue;
                doomed.Add(child.gameObject);
            }
            foreach (var go in doomed)
                Object.DestroyImmediate(go);
        }

        private static void CollectRows(Transform parent, Transform skip, MonoBehaviour page, List<VanillaRow> rows)
        {
            if (page == null)
                throw new InvalidOperationException($"page component missing under {parent.name}");
            foreach (Transform child in parent)
            {
                if (child == skip || KeepNames.Contains(child.name))
                    continue;
                try
                {
                    CollectRow(child, skip, page, rows);
                }
                catch (Exception e) when (!(e is InvalidOperationException))
                {
                    throw new InvalidOperationException($"collecting settings row '{parent.name}/{child.name}' failed", e);
                }
            }
        }

        private static void CollectRow(Transform child, Transform skip, MonoBehaviour page, List<VanillaRow> rows)
        {
            {

                var stepper = child.GetComponent<GuiStepper>();
                if (stepper != null)
                {
                    rows.Add(new VanillaRow { Kind = RowKind.Stepper, Row = child.parent, Control = stepper, Label = FindLabel(child.parent, null), Active = IsActiveUpTo(child.parent, page.transform), Page = page });
                    return;
                }
                var dropdown = child.GetComponent<TMP_Dropdown>();
                if (dropdown != null)
                {
                    var rowLabel = FindLabel(child.parent, dropdown.captionText);
                    var row = rowLabel != null ? child.parent : child;
                    var label = rowLabel ?? FindLabel(child, dropdown.captionText);
                    rows.Add(new VanillaRow { Kind = RowKind.Dropdown, Row = row, Control = dropdown, Label = label, Active = IsActiveUpTo(child, page.transform), Page = page });
                    return;
                }
                var slider = child.GetComponent<Slider>();
                if (slider != null)
                {
                    rows.Add(new VanillaRow { Kind = RowKind.Slider, Row = child, Control = slider, Label = FindLabel(child, null), Active = IsActiveUpTo(child, page.transform), Page = page });
                    return;
                }
                var toggle = child.GetComponent<Toggle>();
                if (toggle != null)
                {
                    rows.Add(new VanillaRow { Kind = RowKind.Toggle, Row = child, Control = toggle, Label = FindLabel(child, null), Active = IsActiveUpTo(child, page.transform), Page = page });
                    return;
                }
                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    rows.Add(new VanillaRow { Kind = RowKind.Button, Row = child, Control = button, Label = FindLabel(child, null), Active = IsActiveUpTo(child, page.transform), Page = page });
                    return;
                }
                CollectRows(child, skip, page, rows);
            }
        }

        // ------------------------------------------------------------------ widgets

        private static AugaRow CreateWidget(VanillaRow row, Transform column)
        {
            var token = RawText(row.Label);
            AugaRow result;
            switch (row.Kind)
            {
                case RowKind.Toggle:
                {
                    var vanilla = (Toggle)row.Control;
                    var go = Object.Instantiate(Auga.Assets.LabeledCheckbox, column, false);
                    var toggle = go.GetComponent<Toggle>();
                    toggle.isOn = vanilla.isOn;
                    toggle.interactable = vanilla.interactable;
                    toggle.onValueChanged = vanilla.onValueChanged;
                    result = new AugaRow { Root = go.transform, Toggle = toggle, Label = FindText(go.transform, "TMP Label") };
                    break;
                }
                case RowKind.Slider:
                {
                    var vanilla = (Slider)row.Control;
                    var go = Object.Instantiate(Auga.Assets.LabeledSliderWithValue, column, false);
                    var slider = go.GetComponentInChildren<Slider>(true);
                    slider.minValue = vanilla.minValue;
                    slider.maxValue = vanilla.maxValue;
                    slider.wholeNumbers = vanilla.wholeNumbers;
                    slider.SetValueWithoutNotify(vanilla.value);
                    slider.interactable = vanilla.interactable;
                    slider.onValueChanged = vanilla.onValueChanged;
                    var value = FindText(go.transform, "TMP ValueLabel");
                    if (value != null) value.text = string.Empty;
                    result = new AugaRow { Root = go.transform, Slider = slider, Label = FindText(go.transform, "TMP Label"), Value = value };
                    break;
                }
                case RowKind.Dropdown:
                {
                    var vanilla = (TMP_Dropdown)row.Control;
                    var go = Object.Instantiate(Auga.Assets.LabeledDropdown, column, false);
                    var dropdown = UpgradeDropdown(go.transform.Find("Dropdown").gameObject);
                    dropdown.options = new List<TMP_Dropdown.OptionData>(vanilla.options);
                    dropdown.SetValueWithoutNotify(vanilla.value);
                    dropdown.interactable = vanilla.interactable;
                    dropdown.onValueChanged = vanilla.onValueChanged;
                    result = new AugaRow { Root = go.transform, Dropdown = dropdown, Label = FindText(go.transform, "TMP Label"), Caption = dropdown.captionText };
                    break;
                }
                case RowKind.Stepper:
                {
                    var go = Object.Instantiate(Auga.Assets.LabeledDropdown, column, false);
                    var dropdown = UpgradeDropdown(go.transform.Find("Dropdown").gameObject);
                    dropdown.ClearOptions();
                    result = new AugaRow { Root = go.transform, Dropdown = dropdown, Label = FindText(go.transform, "TMP Label"), Caption = dropdown.captionText };
                    result.LeftDummy = CreateDummyButton(go.transform, "LeftDummy");
                    result.RightDummy = CreateDummyButton(go.transform, "RightDummy");
                    break;
                }
                case RowKind.Button:
                {
                    var vanilla = (Button)row.Control;
                    var go = Object.Instantiate(Auga.Assets.ButtonSettings, column, false);
                    var button = go.GetComponent<Button>();
                    button.interactable = vanilla.interactable;
                    button.onClick = vanilla.onClick;
                    result = new AugaRow { Root = go.transform, Button = button, Label = FindText(go.transform, "Label") };
                    break;
                }
                default:
                    return null;
            }

            result.Source = row;
            result.Root.name = row.Row.name;
            if (result.Label != null && !string.IsNullOrEmpty(token))
                result.Label.text = token;
            if (result.Label != null && (row.Kind == RowKind.Dropdown || row.Kind == RowKind.Stepper))
            {
                // long labels ("Controller layout") must not be cut off by the dropdown next to them
                result.Label.textWrappingMode = TextWrappingModes.NoWrap;
                result.Label.overflowMode = TextOverflowModes.Overflow;
                var fit = result.Label.gameObject.AddComponent<AugaFitLabel>();
                fit.Obstacle = result.Dropdown != null ? (RectTransform)result.Dropdown.transform : null;
            }
            if (result.Value != null)
            {
                result.Value.textWrappingMode = TextWrappingModes.NoWrap;
                result.Value.overflowMode = TextOverflowModes.Overflow;
                result.Value.gameObject.AddComponent<AugaFitLabel>();
            }
            result.Root.gameObject.SetActive(row.Active);
            return result;
        }

        private static Button CreateDummyButton(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Button));
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        /// <summary>Replaces the legacy Dropdown of a LabeledDropdown with a GuiDropdown (a TMP_Dropdown) the vanilla tabs can drive.</summary>
        private static GuiDropdown UpgradeDropdown(GameObject dropdownObject)
        {
            var existing = dropdownObject.GetComponent<GuiDropdown>();
            if (existing != null)
                return existing;

            var legacy = dropdownObject.GetComponent<Dropdown>();
            var caption = dropdownObject.transform.Find("TMP Label")?.GetComponent<TMP_Text>() ?? dropdownObject.GetComponentInChildren<TMP_Text>(true);
            var template = legacy != null && legacy.template != null ? legacy.template : dropdownObject.transform.Find("Template") as RectTransform;
            var itemLabel = template.Find("Viewport/Content/Item/Item Label");
            var itemText = itemLabel != null ? itemLabel.GetComponent<TMP_Text>() : null;
            if (itemLabel != null && itemText == null)
            {
                var legacyText = itemLabel.GetComponent<Text>();
                var fontSize = legacyText != null ? legacyText.fontSize : (caption != null ? (int)caption.fontSize : 16);
                var color = legacyText != null ? legacyText.color : (caption != null ? caption.color : Color.white);
                if (legacyText != null) Object.DestroyImmediate(legacyText);
                var tmp = itemLabel.gameObject.AddComponent<TextMeshProUGUI>();
                if (caption != null)
                {
                    tmp.font = caption.font;
                    tmp.fontSharedMaterial = caption.fontSharedMaterial;
                    tmp.alignment = caption.alignment;
                }
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.text = "Option";
                itemText = tmp;
            }

            Graphic targetGraphic = null;
            var colors = ColorBlock.defaultColorBlock;
            var transition = Selectable.Transition.ColorTint;
            var navigation = Navigation.defaultNavigation;
            var interactable = true;
            if (legacy != null)
            {
                targetGraphic = legacy.targetGraphic;
                colors = legacy.colors;
                transition = legacy.transition;
                navigation = legacy.navigation;
                interactable = legacy.interactable;
                Object.DestroyImmediate(legacy);
            }
            var dropdown = dropdownObject.AddComponent<GuiDropdown>();
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.targetGraphic = targetGraphic ?? dropdownObject.GetComponent<Image>();
            dropdown.colors = colors;
            dropdown.transition = transition;
            dropdown.navigation = navigation;
            dropdown.interactable = interactable;
            return dropdown;
        }

        /// <summary>Swaps a Selectable for the gui_framework subclass the vanilla code looks up by type.</summary>
        private static T SwapSelectable<T>(Selectable old) where T : Selectable
        {
            if (old is T already)
                return already;
            var go = old.gameObject;
            var targetGraphic = old.targetGraphic;
            var colors = old.colors;
            var transition = old.transition;
            var navigation = old.navigation;
            var spriteState = old.spriteState;
            var interactable = old.interactable;
            var toggleState = old as Toggle;
            var graphic = toggleState != null ? toggleState.graphic : null;
            var isOn = toggleState != null && toggleState.isOn;
            var onToggle = toggleState != null ? toggleState.onValueChanged : null;
            var onClick = old is Button oldButton ? oldButton.onClick : null;
            Object.DestroyImmediate(old);
            var replacement = go.AddComponent<T>();
            replacement.targetGraphic = targetGraphic;
            replacement.colors = colors;
            replacement.transition = transition;
            replacement.navigation = navigation;
            replacement.spriteState = spriteState;
            replacement.interactable = interactable;
            if (replacement is Toggle newToggle)
            {
                newToggle.graphic = graphic;
                newToggle.isOn = isOn;
                if (onToggle != null) newToggle.onValueChanged = onToggle;
            }
            if (replacement is Button newButton && onClick != null)
                newButton.onClick = onClick;
            return replacement;
        }

        // ------------------------------------------------------------------ field mapping

        private static void MapFields(MonoBehaviour page, RectTransform pageRoot, List<AugaRow> augaRows, AugaSettingsGlue glue, Transform bindDialog, Transform content, Transform extras)
        {
            if (page == null)
                return;
            var byControl = new Dictionary<Transform, AugaRow>();
            foreach (var row in augaRows)
            {
                if (row.Source.Page != page) continue;
                byControl[row.Source.Row] = row;
                byControl[row.Source.Control.transform] = row;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in page.GetType().GetFields(flags))
            {
                if (field.IsStatic || !typeof(Object).IsAssignableFrom(field.FieldType))
                    continue;
                var value = field.GetValue(page) as Object;
                if (value == null)
                    continue;
                var t = TransformOf(value);
                if (t == null || !t.IsChildOf(pageRoot))
                    continue;

                if (field.Name == "m_bindDialog" && bindDialog != null)
                {
                    field.SetValue(page, bindDialog.gameObject);
                    continue;
                }
                if (field.Name == "m_listRoot")
                {
                    var dummy = new GameObject("ListRootDummy", typeof(RectTransform));
                    dummy.transform.SetParent(content, false);
                    dummy.SetActive(false);
                    field.SetValue(page, dummy.transform);
                    continue;
                }

                var row = FindRow(t, pageRoot, byControl);
                if (row != null)
                {
                    var mapped = MapToRow(field, value, t, row);
                    if (mapped != null)
                    {
                        field.SetValue(page, mapped);
                        if (field.Name == "m_back" || field.Name == "m_radialSettingsButton")
                            row.Root.gameObject.SetActive(false);
                    }
                    continue;
                }

                if (t.IsChildOf(content) || KeepAtPageRoot.Contains(TopLevelUnder(t, pageRoot).name))
                    continue;
                // referenced vanilla object that sits in a container about to be destroyed: keep it (together
                // with the kept group it belongs to, e.g. the Switch mouse settings), never a whole container
                var carry = KeptAncestor(t, pageRoot) ?? t;
                if (carry != null && carry != pageRoot && carry.parent != extras)
                    carry.SetParent(extras, false);
            }

            RegisterSteppers(page, augaRows, glue);
        }

        private static Object MapToRow(FieldInfo field, Object value, Transform valueTransform, AugaRow row)
        {
            var type = field.FieldType;
            if (typeof(Toggle).IsAssignableFrom(type))
                return row.Toggle != null ? (Object)(type == typeof(GuiToggle) ? SwapSelectable<GuiToggle>(row.Toggle) : row.Toggle) : null;
            if (typeof(Slider).IsAssignableFrom(type))
                return row.Slider;
            if (typeof(TMP_Dropdown).IsAssignableFrom(type))
                return row.Dropdown;
            if (type == typeof(GuiButton))
            {
                if (row.Button == null) return null;
                row.Button = SwapSelectable<GuiButton>(row.Button);
                return row.Button;
            }
            if (typeof(Button).IsAssignableFrom(type))
            {
                if (row.Button != null) return row.Button;
                if (row.Source.Kind == RowKind.Stepper)
                {
                    var name = field.Name.ToLowerInvariant();
                    return name.Contains("left") ? row.LeftDummy : row.RightDummy;
                }
                return null;
            }
            if (typeof(TMP_Text).IsAssignableFrom(type))
            {
                if (row.Source.Label != null && value == row.Source.Label)
                    return row.Label;
                var textName = valueTransform.name;
                if (row.Source.Kind == RowKind.Slider && (textName.Contains("Value") || textName.Contains("Percent") || textName.EndsWith("Text")))
                    return row.Value;
                if (row.Source.Kind == RowKind.Stepper && valueTransform.IsChildOf(row.Source.Control.transform))
                    return row.Caption;
                // another text of the row (community translation, cloud warning, preset description): keep it
                valueTransform.SetParent(row.Root, false);
                return value;
            }
            if (typeof(Image).IsAssignableFrom(type))
            {
                if (row.Slider != null && row.Slider.fillRect != null)
                    return row.Slider.fillRect.GetComponent<Image>();
                return null;
            }
            if (type == typeof(GameObject))
                return row.Root.gameObject;
            if (typeof(Transform).IsAssignableFrom(type))
                return row.Root;
            return null;
        }

        private static void RegisterSteppers(MonoBehaviour page, List<AugaRow> augaRows, AugaSettingsGlue glue)
        {
            foreach (var row in augaRows)
            {
                if (row.Source.Page != page || row.Source.Kind != RowKind.Stepper || row.Dropdown == null)
                    continue;
                switch (page)
                {
                    case GameplaySettings gameplay when gameplay.m_language == row.Caption:
                        glue.LanguageDropdown = row.Dropdown;
                        glue.Gameplay = gameplay;
                        if (Auga.Assets.LanguageTooltip != null)
                        {
                            var tooltip = Object.Instantiate(Auga.Assets.LanguageTooltip, row.Root, false);
                            tooltip.name = "LanguageTooltip";
                            tooltip.SetActive(false);
                            glue.LanguageTooltip = tooltip;
                        }
                        break;
                    case GraphicsSettings graphics when graphics.m_graphicsMode == row.Caption:
                        glue.PresetDropdown = row.Dropdown;
                        glue.Graphics = graphics;
                        break;
                    case GamepadSettings gamepad when gamepad.m_controllerLayoutText == row.Caption:
                        glue.LayoutDropdown = row.Dropdown;
                        glue.Gamepad = gamepad;
                        break;
                }
            }
        }

        // ------------------------------------------------------------------ key bindings

        private static void BuildKeyBindings(KeyboardMouseSettings page, Transform column)
        {
            if (Auga.Assets.LabeledKeybind == null || page.m_keys == null)
                return;
            var keys = new List<KeySetting>();
            GuiButton last = null;
            foreach (var key in page.m_keys)
            {
                var go = Object.Instantiate(Auga.Assets.LabeledKeybind, column, false);
                go.name = key.m_keyName;
                var label = FindText(go.transform, "TMP Label");
                var token = key.m_keyTransform != null ? RawText(FindLabel(key.m_keyTransform, null)) : null;
                if (label != null && !string.IsNullOrEmpty(token)) label.text = token;
                var display = go.GetComponent<AugaBindingDisplay>();
                if (display != null) display.AutomaticKeyName = key.m_keyName;
                var button = go.GetComponentInChildren<Button>(true);
                if (button == null)
                {
                    var hotspot = go.transform.Find("Hotspot") ?? go.transform;
                    button = hotspot.gameObject.AddComponent<GuiButton>();
                    button.targetGraphic = hotspot.GetComponent<Image>();
                }
                last = SwapSelectable<GuiButton>(button);
                if (key.m_keyTransform != null)
                    go.SetActive(IsActiveUpTo(key.m_keyTransform, page.transform));
                keys.Add(new KeySetting { m_keyName = key.m_keyName, m_keyTransform = (RectTransform)go.transform, m_blockedButtons = key.m_blockedButtons ?? new KeyCode[0] });
            }
            page.m_keys = keys;
            page.m_keyRows = Mathf.Max(1, keys.Count);
            page.m_keyCols = 1;
            if (last != null)
            {
                page.m_bottomLeftKeyButton = last;
                page.m_bottomRightKeyButton = last;
            }
        }

        // ------------------------------------------------------------------ graphics templates

        private static void BuildGraphicsTemplates(GraphicsSettings graphics, RectTransform page, Transform sliderColumn, Transform toggleColumn, Transform content)
        {
            var vanillaSlider = graphics.m_qualitySliderPrefab;
            var vanillaToggle = graphics.m_qualityTogglePrefab;

            var sliderTemplate = Object.Instantiate(Auga.Assets.LabeledSliderWithValue, sliderColumn, false);
            sliderTemplate.name = "QualitySlider";
            var label = sliderTemplate.transform.Find("TMP Label");
            if (label != null) label.name = "Label";
            var info = new GameObject("Info", typeof(RectTransform)).transform;
            info.SetParent(sliderTemplate.transform, false);
            var value = sliderTemplate.transform.Find("TMP ValueLabel");
            if (value != null)
            {
                var valueRect = (RectTransform)value;
                var infoRect = (RectTransform)info;
                infoRect.anchorMin = valueRect.anchorMin;
                infoRect.anchorMax = valueRect.anchorMax;
                infoRect.pivot = valueRect.pivot;
                infoRect.anchoredPosition = valueRect.anchoredPosition;
                infoRect.sizeDelta = valueRect.sizeDelta;
                value.SetParent(info, false);
                value.name = "Value";
                Stretch(valueRect);
            }
            var vanillaWarning = vanillaSlider != null ? vanillaSlider.transform.Find("Info/Warning") : null;
            if (vanillaWarning != null)
            {
                var warning = Object.Instantiate(vanillaWarning.gameObject, info, false);
                warning.name = "Warning";
                var warningRect = (RectTransform)warning.transform;
                warningRect.anchorMin = new Vector2(1f, 0.5f);
                warningRect.anchorMax = new Vector2(1f, 0.5f);
                warningRect.pivot = new Vector2(0f, 0.5f);
                warningRect.anchoredPosition = new Vector2(4f, 0f);
            }
            else
            {
                var warning = new GameObject("Warning", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SettingsTooltip));
                warning.transform.SetParent(info, false);
                warning.GetComponent<Image>().enabled = false;
            }
            sliderTemplate.SetActive(false);
            graphics.m_qualitySliderPrefab = sliderTemplate;

            var toggleTemplate = Object.Instantiate(Auga.Assets.LabeledCheckbox, toggleColumn, false);
            toggleTemplate.name = "QualityToggle";
            SwapSelectable<GuiToggle>(toggleTemplate.GetComponent<Toggle>());
            var toggleLabel = toggleTemplate.transform.Find("TMP Label");
            if (toggleLabel != null) toggleLabel.name = "Label";
            toggleTemplate.SetActive(false);
            graphics.m_qualityTogglePrefab = toggleTemplate;
        }

        /// <summary>
        /// GraphicsSettings instantiates its quality slider template and expects the Slider on the clone's root;
        /// Auga's LabeledSliderWithValue keeps it on a child. Same logic as the vanilla method otherwise.
        /// </summary>
        [HarmonyPatch(typeof(GraphicsSettings), "CreateDynamicGraphicsQualitySettings")]
        public static class GraphicsSettings_CreateDynamic_Patch
        {
            public static bool Prefix(GraphicsSettings __instance)
            {
                if (__instance.GetComponentInChildren<AugaSettingsPage>(true) == null)
                    return true;

                GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
                var sliderPrefab = __instance.m_qualitySliderPrefab;
                var index = sliderPrefab.transform.GetSiblingIndex() + 1;
                for (var i = 0; i < 32; i++)
                {
                    var setting = (GraphicsSettingInt)(1 << i);
                    if (!Enum.IsDefined(typeof(GraphicsSettingInt), setting) || !setting.IsShownToUser() || setting.IsPresentSetting()
                        || setting == GraphicsSettingInt.Target3DResolutionVertical || setting == GraphicsSettingInt.UpscalingAlgorithm)
                        continue;
                    var obj = Object.Instantiate(sliderPrefab, sliderPrefab.transform.parent);
                    obj.name = setting.ToString();
                    obj.transform.SetSiblingIndex(index++);
                    var slider = obj.GetComponentInChildren<Slider>(true);
                    var label = obj.transform.Find("Label").GetComponent<TMP_Text>();
                    var info = obj.transform.Find("Info");
                    var valueText = info.Find("Value").GetComponent<TMP_Text>();
                    var warning = info.Find("Warning");
                    var warningImage = warning.GetComponent<Image>();
                    var tooltip = warning.GetComponent<SettingsTooltip>();
                    var range = setting.GetRange();
                    slider.minValue = range.m_minValue;
                    slider.maxValue = range.m_maxValue;
                    label.text = setting.ToDisplayName();
                    obj.SetActive(true);
                    var data = new QualitySliderData(setting, slider, valueText, warningImage, tooltip);
                    __instance.m_qualitySliders.Add(data);
                    __instance.m_dynamicQualitySliders.Add(slider);
                }

                var togglePrefab = __instance.m_qualityTogglePrefab;
                index = togglePrefab.transform.GetSiblingIndex();
                for (var j = 0; j < 32; j++)
                {
                    var setting = (GraphicsSettingBool)(1 << j);
                    if (!Enum.IsDefined(typeof(GraphicsSettingBool), setting) || !setting.IsShownToUser() || setting.IsPresentSetting())
                        continue;
                    var obj = Object.Instantiate(togglePrefab, togglePrefab.transform.parent);
                    obj.name = setting.ToString();
                    obj.transform.SetSiblingIndex(index++);
                    var toggle = obj.GetComponentInChildren<GuiToggle>(true);
                    toggle.transform.Find("Label").GetComponent<TMP_Text>().text = setting.ToDisplayName();
                    obj.SetActive(true);
                    var data = new QualityToggleData(setting, toggle);
                    __instance.m_qualityToggles.Add(data);
                    __instance.m_dynamicQualityToggles.Add(toggle);
                }
                return false;
            }
        }

        // ------------------------------------------------------------------ helpers

        private static AugaRow FindRow(Transform t, Transform pageRoot, Dictionary<Transform, AugaRow> byControl)
        {
            for (var current = t; current != null && current != pageRoot; current = current.parent)
            {
                if (byControl.TryGetValue(current, out var row))
                    return row;
            }
            // a container holding converted rows (the "root" objects the game toggles): use its first row
            AugaRow first = null;
            var bestIndex = int.MaxValue;
            foreach (var pair in byControl)
            {
                if (pair.Key.IsChildOf(t) && pair.Key != t)
                {
                    var index = pair.Value.Root.GetSiblingIndex();
                    if (index < bestIndex) { bestIndex = index; first = pair.Value; }
                }
            }
            return first;
        }

        /// <summary>The highest ancestor of <paramref name="t"/> below the page that is one of the kept vanilla groups.</summary>
        private static Transform KeptAncestor(Transform t, Transform pageRoot)
        {
            Transform kept = null;
            for (var current = t; current != null && current != pageRoot; current = current.parent)
            {
                if (KeepNames.Contains(current.name)) kept = current;
            }
            return kept;
        }

        private static Transform TopLevelUnder(Transform t, Transform root)
        {
            var current = t;
            while (current.parent != null && current.parent != root)
                current = current.parent;
            return current;
        }

        private static Transform ChildTowards(Transform top, Transform t)
        {
            var current = t;
            while (current != null && current.parent != top)
                current = current.parent;
            return current;
        }

        private static Transform FindDescendant(Transform parent, string name, Transform skip)
        {
            foreach (Transform child in parent)
            {
                if (child == skip) continue;
                if (child.name == name) return child;
                var found = FindDescendant(child, name, skip);
                if (found != null) return found;
            }
            return null;
        }

        private static TMP_Text FindLabel(Transform row, TMP_Text exclude)
        {
            if (row == null) return null;
            foreach (Transform child in row)
            {
                if (child.name != "Label" && child.name != "LabelLeft") continue;
                var text = child.GetComponent<TMP_Text>();
                if (text != null && text != exclude) return text;
            }
            return null;
        }

        private static TMP_Text FindText(Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : root.GetComponentInChildren<TMP_Text>(true);
        }

        private static void SetText(Transform root, string path, string text)
        {
            var child = root.Find(path);
            var tmp = child != null ? child.GetComponent<TMP_Text>() : null;
            if (tmp != null) tmp.text = text;
        }

        private static string RawText(TMP_Text text)
        {
            return text != null ? text.text : null;
        }

        private static bool IsActiveUpTo(Transform t, Transform stop)
        {
            for (var current = t; current != null && current != stop; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
            }
            return true;
        }

        private static Transform TransformOf(Object value)
        {
            if (value is GameObject go) return go.transform;
            if (value is Component c) return c.transform;
            return null;
        }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null) Object.DestroyImmediate(component);
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (var i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CopyImage(Image from, Image to)
        {
            if (from == null || to == null) return;
            to.sprite = from.sprite;
            to.color = from.color;
            to.material = from.material;
            to.type = from.type;
            to.raycastTarget = from.raycastTarget;
        }
    }
}
