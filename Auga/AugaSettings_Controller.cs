using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // ============================================================
    // AugaSettings_Controller.cs  —  "buffer on change, apply on OK"
    //
    // Wire():   читает PlatformPrefs → устанавливает контролы
    // Pend():   onChange → _pending[key] = Action (НЕ в PlatformPrefs)
    // Commit(): Settings.OnOk Prefix → применяет _pending → PlatformPrefs
    //           → GraphicsSettingsManager.ApplyStartupSettings()
    //
    // Исправления v3 (по данным из Unity MCP + prefab YAML):
    //   • WireSlider: ищет TMP ValueLabel по имени (не последний TMP_Text)
    //     → нет порчи лейбла на Controls-слайдерах (LabeledSlider без ValueLabel)
    //   • WireSlider: Func<Slider,string> formatValue для кастомного отображения
    //   • SetupKeyBindings: AutomaticKeyName = GO.name → бинды перестают показывать "W"
    //   • PlatformPrefs keys исправлены по деcompile Valheim 0.221:
    //     LodBias (не LoD), FPSLimit (не TargetFrameRate), SSAO/SSAO_2
    //   • Dropdown: TMP Label caption обновляется вручную (legacy Dropdown
    //     не может обновить TMPro.TextMeshProUGUI через m_CaptionText)
    //   • GuiScale: slider range 50-115, display = slider.value + "%",
    //     store = v/100f в PlatformPrefs
    //   • Autobackups: slider range 1-10, display = count integer
    //   • FramerateLimit: range 0-360, display = fps number / "∞"
    //   • Graphics quality sliders: прямые int-значения (0-2 или 0-3)
    //   • Commit вызывает GraphicsSettingsManager.ApplyStartupSettings()
    // ============================================================

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    public static class Settings_Awake_Init_Patch
    {
        public static void Postfix(Settings __instance)
        {
            try { AugaSettingsWirer.Wire(__instance.gameObject); }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Wire() exception: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.OnOk))]
    public static class Settings_Awake_OnOk_Commit_Patch
    {
        public static void Prefix()
        {
            try { AugaSettingsWirer.Commit(); }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Commit() exception: {ex.Message}"); }
        }
    }

    public static class AugaSettingsWirer
    {
        private static readonly Dictionary<string, Action> _pending = new Dictionary<string, Action>();

        private static readonly MethodInfo s_applyStartupSettings =
            typeof(GraphicsSettingsManager).GetMethod(
                "ApplyStartupSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Pend(string key, Action applyAction)
        {
            _pending[key] = applyAction;
        }

        public static void Commit()
        {
            foreach (var kv in _pending)
            {
                try { kv.Value?.Invoke(); }
                catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Commit '{kv.Key}': {ex.Message}"); }
            }
            _pending.Clear();

            // Re-apply graphics from PlatformPrefs (loads + fires GraphicsSettingsChanged)
            try
            {
                var mgr = GraphicsSettingsManager.Instance;
                if (mgr != null)
                    s_applyStartupSettings?.Invoke(mgr, null);
            }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] GraphicsApply: {ex.Message}"); }

            Auga.Log("[AugaSettings] Commit done");
        }

        public static void Wire(GameObject settingsRoot)
        {
            _pending.Clear();

            var tabHandler = settingsRoot.GetComponentInChildren<TabHandler>(true);
            if (tabHandler == null)
            {
                Auga.LogWarning("[AugaSettings] TabHandler not found");
                return;
            }

            Auga.Log($"[AugaSettings] Wire: {tabHandler.m_tabs.Count} tabs");

            int wired = 0;
            for (int i = 0; i < tabHandler.m_tabs.Count; i++)
            {
                var tab = tabHandler.m_tabs[i];
                if (tab.m_page == null) continue;
                var page = tab.m_page;
                string pageName = page.gameObject.name;

                switch (pageName)
                {
                    case "Audio":    WireAudio(page);    wired++; break;
                    case "Controls": WireControls(page); wired++; break;
                    case "Graphics": WireGraphics(page); wired++; break;
                    case "Misc":     WireMisc(page);     wired++; break;
                    default:
                        Auga.LogWarning($"[AugaSettings] Unknown tab '{pageName}' at [{i}], using index fallback");
                        switch (i)
                        {
                            case 0: WireControls(page); wired++; break;
                            case 1: WireAudio(page);    wired++; break;
                            case 2: WireGraphics(page); wired++; break;
                            case 3: WireMisc(page);     wired++; break;
                        }
                        break;
                }
            }
            Auga.Log($"[AugaSettings] Wired {wired}/{tabHandler.m_tabs.Count} tabs");
        }

        // ===================== Audio =====================
        private static void WireAudio(Transform page)
        {
            WireSlider(page, "MasterVolume",
                read: () => PlatformPrefs.GetFloat("MasterVolume", AudioListener.volume),
                pend: v => Pend("MasterVolume", () => { AudioListener.volume = v; PlatformPrefs.SetFloat("MasterVolume", v); }),
                immediateApply: v => AudioListener.volume = v);

            WireSlider(page, "EffectVolume",
                read: () => PlatformPrefs.GetFloat("SfxVolume", 1f),
                pend: v => Pend("SfxVolume", () => { AudioMan.SetSFXVolume(v); PlatformPrefs.SetFloat("SfxVolume", v); }),
                immediateApply: v => AudioMan.SetSFXVolume(v));

            WireSlider(page, "MusicVolume",
                read: () => PlatformPrefs.GetFloat("MusicVolume", 1f),
                pend: v => Pend("MusicVolume", () => { MusicMan.m_masterMusicVolume = v; PlatformPrefs.SetFloat("MusicVolume", v); }),
                immediateApply: v => MusicMan.m_masterMusicVolume = v);

            WireToggle(page, "ContinuousMusic",
                read: () => PlatformPrefs.GetBool("ContinousMusic", true),
                pend: v => Pend("ContinousMusic", () => { Settings.ContinousMusic = v; PlatformPrefs.SetBool("ContinousMusic", v); }));
        }

        // ===================== Controls =====================
        private static void WireControls(Transform page)
        {
            // LabeledSlider — нет TMP ValueLabel → formatValue не нужен
            WireSlider(page, "MouseSensitivity",
                read: () => PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens),
                pend: v => Pend("MouseSensitivity", () => { PlayerController.m_mouseSens = v; PlatformPrefs.SetFloat("MouseSensitivity", v); }),
                immediateApply: v => PlayerController.m_mouseSens = v);

            WireSlider(page, "GamepadSensitivity",
                read: () => PlatformPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens),
                pend: v => Pend("GamepadSensitivity", () => { PlayerController.m_gamepadSens = v; PlatformPrefs.SetFloat("GamepadSensitivity", v); }),
                immediateApply: v => PlayerController.m_gamepadSens = v);

            WireToggle(page, "InvertMouse",
                read: () => PlatformPrefs.GetBool("InvertMouse"),
                pend: v => Pend("InvertMouse", () => { PlayerController.m_invertMouse = v; PlatformPrefs.SetBool("InvertMouse", v); }));

            WireToggle(page, "ToggleAutoRun",
                read: () => PlatformPrefs.GetInt("ToggleRun", ZInput.IsGamepadActive() ? 1 : 0) == 1,
                pend: v => Pend("ToggleRun", () => { ZInput.ToggleRun = v; PlatformPrefs.SetInt("ToggleRun", v ? 1 : 0); }));

            WireToggle(page, "GamepadEnabled",
                read: () => ZInput.IsGamepadEnabled(),
                pend: v => Pend("GamepadEnabled", () => ZInput.SetGamepadEnabled(v)));

            WireToggle(page, "AlternativeGlyphs",
                read: () => PlatformPrefs.GetInt("AltGlyphs") == 1,
                pend: v => Pend("AltGlyphs", () => PlatformPrefs.SetInt("AltGlyphs", v ? 1 : 0)));

            WireToggle(page, "SwapTriggers",
                read: () => ZInput.SwapTriggers,
                pend: v => Pend("SwapTriggers", () => { ZInput.SwapTriggers = v; PlatformPrefs.SetInt("SwapTriggers", v ? 1 : 0); }));

            // Устанавливаем AutomaticKeyName = GO-имя на каждом AugaBindingDisplay
            // (поле пустое в префабе; GO-имя совпадает с именем кнопки в ZInput)
            SetupKeyBindings(page);
        }

        private static void SetupKeyBindings(Transform page)
        {
            var root = FindDeepChild(page, "KeyBindings");
            if (root == null)
            {
                Auga.LogWarning("[AugaSettings] 'KeyBindings' GO not found in Controls page");
                return;
            }
            var displays = root.GetComponentsInChildren<AugaUnity.AugaBindingDisplay>(true);
            Auga.Log($"[AugaSettings] Setting AutomaticKeyName on {displays.Length} binding displays");
            foreach (var d in displays)
            {
                if (string.IsNullOrEmpty(d.AutomaticKeyName))
                    d.AutomaticKeyName = d.gameObject.name;
            }
        }

        // ===================== Graphics =====================
        private static void WireGraphics(Transform page)
        {
            // --- Toggles (SecondColumnWidgets) ---
            WireToggle(page, "Bloom",               () => PlatformPrefs.GetBool("Bloom", true),          v => Pend("Bloom",             () => PlatformPrefs.SetBool("Bloom", v)));
            WireToggle(page, "SSAO",                () => PlatformPrefs.GetBool("SSAO", true),           v => Pend("SSAO",              () => { PlatformPrefs.SetBool("SSAO", v); PlatformPrefs.SetInt("SSAO_2", -1); }));
            WireToggle(page, "SunShafts",           () => PlatformPrefs.GetBool("SunShafts", true),      v => Pend("SunShafts",         () => PlatformPrefs.SetBool("SunShafts", v)));
            WireToggle(page, "MotionBlur",          () => PlatformPrefs.GetBool("MotionBlur"),           v => Pend("MotionBlur",        () => PlatformPrefs.SetBool("MotionBlur", v)));
            WireToggle(page, "Tessellation",        () => PlatformPrefs.GetBool("Tesselation", true),    v => Pend("Tesselation",       () => PlatformPrefs.SetBool("Tesselation", v)));
            WireToggle(page, "DistantShadows",      () => PlatformPrefs.GetBool("DistantShadows", true), v => Pend("DistantShadows",    () => PlatformPrefs.SetBool("DistantShadows", v)));
            WireToggle(page, "SoftParticles",       () => PlatformPrefs.GetBool("SoftPart", true),       v => Pend("SoftPart",          () => PlatformPrefs.SetBool("SoftPart", v)));
            WireToggle(page, "DepthOfField",        () => PlatformPrefs.GetBool("DOF", true),            v => Pend("DOF",               () => PlatformPrefs.SetBool("DOF", v)));
            WireToggle(page, "AntiAliasing",        () => PlatformPrefs.GetBool("AntiAliasing", true),   v => Pend("AntiAliasing",      () => PlatformPrefs.SetBool("AntiAliasing", v)));
            WireToggle(page, "ChromaticAbberation", () => PlatformPrefs.GetBool("ChromaticAberration"),  v => Pend("ChromaticAberration",() => PlatformPrefs.SetBool("ChromaticAberration", v)));
            WireToggle(page, "VSYNC",               () => PlatformPrefs.GetBool("VSync"),                v => Pend("VSync",             () => { QualitySettings.vSyncCount = v ? 1 : 0; PlatformPrefs.SetBool("VSync", v); }));
            WireToggle(page, "Fullscreen",          () => Screen.fullScreen,                             v => Pend("Fullscreen",        () => Screen.fullScreen = v));

            // --- Quality Sliders (FirstColumnWidgets) — LabeledSliderWithValue, имеют TMP ValueLabel ---
            // Slider value = PlatformPrefs int напрямую (диапазоны взяты из prefab YAML)

            // Vegitation: ClutterQuality, slider 0-3
            WireSlider(page, "Vegitation",
                read: () => PlatformPrefs.GetInt("ClutterQuality", 2),
                pend: v => Pend("ClutterQuality", () => PlatformPrefs.SetInt("ClutterQuality", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 3));

            // ParticleLights: Lights (particle/light count quality), slider 0-2
            WireSlider(page, "ParticleLights",
                read: () => PlatformPrefs.GetInt("Lights", 2),
                pend: v => Pend("Lights", () => PlatformPrefs.SetInt("Lights", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 2));

            // DrawDistance: LodBias (Valheim 0.221 key, NOT "LoD"), slider 0-3
            WireSlider(page, "DrawDistance",
                read: () => PlatformPrefs.GetInt("LodBias", 2),
                pend: v => Pend("LodBias", () => PlatformPrefs.SetInt("LodBias", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 3));

            // ShadowQuality: slider 0-2
            WireSlider(page, "ShadowQuality",
                read: () => PlatformPrefs.GetInt("ShadowQuality", 2),
                pend: v => Pend("ShadowQuality", () => PlatformPrefs.SetInt("ShadowQuality", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 2));

            // PointLights: slider 0-3
            WireSlider(page, "PointLights",
                read: () => PlatformPrefs.GetInt("PointLights", 3),
                pend: v => Pend("PointLights", () => PlatformPrefs.SetInt("PointLights", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 3));

            // PointLightsShadows: slider 0-3
            WireSlider(page, "PointLightsShadows",
                read: () => PlatformPrefs.GetInt("PointLightShadows", 2),
                pend: v => Pend("PointLightShadows", () => PlatformPrefs.SetInt("PointLightShadows", Mathf.RoundToInt(v))),
                formatValue: s => QualityLabel(Mathf.RoundToInt(s.value), 3));

            // FramerateLimit: slider 0-360; 0 = unlimited (FPSLimit = -1)
            WireSlider(page, "FramerateLimit",
                read: () => { int fps = PlatformPrefs.GetInt("FPSLimit", -1); return fps < 0 ? 0f : (float)fps; },
                pend: v => {
                    int fps = Mathf.RoundToInt(v) <= 0 ? -1 : Mathf.RoundToInt(v);
                    Pend("FPSLimit", () => PlatformPrefs.SetInt("FPSLimit", fps));
                },
                formatValue: s => {
                    int v = Mathf.RoundToInt(s.value);
                    return v <= 0 ? "\u221E" : v + " fps"; // ∞
                });

            WireResolutionDropdown(page);
        }

        private static string QualityLabel(int val, int max)
        {
            switch (max)
            {
                case 2: return val == 0 ? "Low" : val == 1 ? "Med" : "High";
                case 3: return val == 0 ? "Low" : val == 1 ? "Med" : val == 2 ? "High" : "Max";
                default: return val.ToString();
            }
        }

        // ===================== Misc =====================
        private static void WireMisc(Transform page)
        {
            // GuiScale: slider range 50-115, PlatformPrefs stores float 0-1
            // read: stored * 100 → slider value (1.0 → 100, fits in 50-115)
            // display: slider.value + "%" (value IS the percentage number)
            // save:  slider.value / 100f → stored
            WireSlider(page, "GuiScale",
                read: () => PlatformPrefs.GetFloat("GuiScale", 1f) * 100f,
                pend: v => Pend("GuiScale", () => {
                    float scale = Mathf.Clamp(v / 100f, 0.5f, 2f);
                    GuiScaler.SetScale(scale);
                    PlatformPrefs.SetFloat("GuiScale", scale);
                }),
                formatValue: s => Mathf.RoundToInt(s.value) + "%");

            // RenderScale → Valheim 0.221: Target3DResolutionVertical (int pixels)
            // slider range 0-1; 1.0 = native (int.MaxValue), <1 = downscaled
            WireSlider(page, "RenderScale",
                read: () => {
                    int tv = PlatformPrefs.GetInt("Target3DResolutionVertical", -1);
                    if (tv < 0) return PlatformPrefs.GetFloat("RenderScale", 1f);
                    return tv == int.MaxValue ? 1f : Mathf.Clamp01((float)tv / Mathf.Max(1, Screen.height));
                },
                pend: v => Pend("Target3DResolutionVertical", () => {
                    int pixels = v >= 1f ? int.MaxValue : Mathf.RoundToInt(Screen.height * Mathf.Clamp01(v));
                    PlatformPrefs.SetInt("Target3DResolutionVertical", pixels);
                    PlatformPrefs.SetFloat("RenderScale", v); // legacy fallback
                }),
                formatValue: s => Mathf.RoundToInt(s.value * 100f) + "%");

            // Autobackups: slider range 1-10, display = count integer
            WireSlider(page, "Autobackups",
                read: () => Mathf.Clamp(PlatformPrefs.GetInt("AutoBackups", 4), 1, 10),
                pend: v => Pend("AutoBackups", () => PlatformPrefs.SetInt("AutoBackups", Mathf.RoundToInt(v))),
                formatValue: s => Mathf.RoundToInt(s.value).ToString());

            WireToggle(page, "ShowKeyHints",
                read: () => PlatformPrefs.GetBool("KeyHints", true),
                pend: v => Pend("KeyHints", () => PlatformPrefs.SetBool("KeyHints", v)));

            WireToggle(page, "ShowTutorials",
                read: () => PlatformPrefs.GetBool("TutorialsEnabled", true),
                pend: v => Pend("TutorialsEnabled", () => { Raven.m_tutorialsEnabled = v; PlatformPrefs.SetBool("TutorialsEnabled", v); }));

            WireToggle(page, "CameraShake",
                read: () => PlatformPrefs.GetBool("CameraShake", true),
                pend: v => Pend("CameraShake", () => PlatformPrefs.SetBool("CameraShake", v)));

            WireToggle(page, "ImmersiveShipCamera",
                read: () => PlatformPrefs.GetBool("ImmersiveShipCamera", true),
                pend: v => Pend("ImmersiveShipCamera", () => PlatformPrefs.SetBool("ImmersiveShipCamera", v)));

            WireToggle(page, "ReduceBackgroundPerformance",
                read: () => PlatformPrefs.GetBool("ReduceBackgroundUsage"),
                pend: v => Pend("ReduceBackgroundUsage", () => { Settings.ReduceBackgroundUsage = v; PlatformPrefs.SetBool("ReduceBackgroundUsage", v); }));

            WireToggle(page, "ReduceFlashingLights",
                read: () => PlatformPrefs.GetBool("ReduceFlashingLights"),
                pend: v => Pend("ReduceFlashingLights", () => { Settings.ReduceFlashingLights = v; PlatformPrefs.SetBool("ReduceFlashingLights", v); }));

            WireToggle(page, "RightClickBuildSelection",
                read: () => PlatformPrefs.GetBool("RightClickBuildSelection"),
                pend: v => Pend("RightClickBuildSelection", () => PlatformPrefs.SetBool("RightClickBuildSelection", v)));

            WireLanguageDropdown(page);
        }

        // ===================== Helpers =====================

        /// <summary>
        /// Находит слайдер по имени GO, устанавливает начальное значение из read().
        /// onChange → pend (в _pending). immediateApply — живой предпросмотр (аудио).
        ///
        /// formatValue (Func&lt;Slider,string&gt;): если задан, ищет GO с именем "TMP ValueLabel"
        /// и обновляет его текст. Намеренно НЕ трогает "TMP Label" (название параметра).
        /// Controls-слайдеры (LabeledSlider) не имеют "TMP ValueLabel" → их Label не портится.
        /// </summary>
        private static void WireSlider(Transform page, string goName,
            Func<float> read, Action<float> pend,
            Action<float> immediateApply = null,
            Func<Slider, string> formatValue = null)
        {
            var go = FindDeepChild(page, goName);
            if (go == null) return;

            var slider = go.GetComponentInChildren<Slider>(true);
            if (slider == null) return;

            float storedValue = 0f;
            try { storedValue = read(); } catch { }
            slider.SetValueWithoutNotify(storedValue);

            // Ищем TMP ValueLabel строго по имени GO (только в LabeledSliderWithValue)
            TMPro.TMP_Text valueText = null;
            if (formatValue != null)
            {
                foreach (var t in go.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (t.gameObject.name == "TMP ValueLabel") { valueText = t; break; }
                }
            }

            void UpdateText()
            {
                if (valueText != null && formatValue != null)
                    valueText.text = formatValue(slider);
            }
            UpdateText();

            slider.onValueChanged.AddListener(v =>
            {
                pend(v);
                try { immediateApply?.Invoke(v); } catch { }
                UpdateText();
            });
        }

        private static void WireToggle(Transform page, string goName,
            Func<bool> read, Action<bool> pend)
        {
            var go = FindDeepChild(page, goName);
            if (go == null) return;
            var toggle = go.GetComponentInChildren<Toggle>(true);
            if (toggle == null) return;
            try { toggle.SetIsOnWithoutNotify(read()); } catch { }
            toggle.onValueChanged.AddListener(v => pend(v));
        }

        private static void WireLanguageDropdown(Transform page)
        {
            var go = FindDeepChild(page, "Language");
            if (go == null) return;
            var dd = go.GetComponentInChildren<Dropdown>(true);
            if (dd == null) return;
            try
            {
                var languages = Localization.instance.GetLanguages();
                if (languages == null || languages.Count == 0) return;

                dd.ClearOptions();
                dd.AddOptions(languages
                    .Select(l => Localization.instance.Localize("$language_" + l.ToLower()))
                    .ToList());

                var currentLang = Localization.instance.GetSelectedLanguage();
                int idx = Mathf.Max(0, languages.IndexOf(currentLang));
                dd.SetValueWithoutNotify(idx);
                RefreshDropdownCaption(dd); // TMP Label caption не обновляется стандартно

                dd.onValueChanged.AddListener(i =>
                {
                    RefreshDropdownCaption(dd);
                    if (i >= 0 && i < languages.Count)
                        Pend("Language", () => Localization.instance.SetLanguage(languages[i]));
                });
            }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Language dropdown: {ex.Message}"); }
        }

        private static void WireResolutionDropdown(Transform page)
        {
            var go = FindDeepChild(page, "Resolution");
            if (go == null) return;
            var dd = go.GetComponentInChildren<Dropdown>(true);
            if (dd == null) return;
            try
            {
                var resolutions = Screen.resolutions;
                if (resolutions == null || resolutions.Length == 0) return;

                // Дедупликация по w×h (Unity 6 возвращает дубликаты при разных refresh rate)
                var seen = new HashSet<string>();
                var unique = new List<Resolution>();
                foreach (var r in resolutions)
                {
                    string key = r.width + "x" + r.height;
                    if (seen.Add(key)) unique.Add(r);
                }

                var options = unique.Select(r => r.width + "x" + r.height).ToList();
                dd.ClearOptions();
                dd.AddOptions(options);

                // Находим текущее разрешение
                int currentIdx = 0;
                for (int i = 0; i < unique.Count; i++)
                    if (unique[i].width == Screen.width && unique[i].height == Screen.height)
                        currentIdx = i;
                dd.SetValueWithoutNotify(currentIdx);
                RefreshDropdownCaption(dd);

                dd.onValueChanged.AddListener(i =>
                {
                    RefreshDropdownCaption(dd);
                    if (i >= 0 && i < unique.Count)
                    {
                        var r = unique[i];
                        Pend("Resolution", () => Screen.SetResolution(r.width, r.height, Screen.fullScreen));
                    }
                });
            }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Resolution dropdown: {ex.Message}"); }
        }

        /// <summary>
        /// Legacy Dropdown с TMP Label (TMPro.TextMeshProUGUI) не может обновить
        /// Caption автоматически (m_CaptionText ожидает UI.Text).
        /// Обновляем TMP Label вручную по текущему dd.value.
        /// </summary>
        private static void RefreshDropdownCaption(Dropdown dd)
        {
            var lbl = dd.transform.Find("TMP Label")?.GetComponent<TMPro.TMP_Text>();
            if (lbl != null && dd.value >= 0 && dd.value < dd.options.Count)
                lbl.text = dd.options[dd.value].text;
        }

        private static GameObject FindDeepChild(Transform root, string name)
        {
            if (root.gameObject.name == name) return root.gameObject;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeepChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
