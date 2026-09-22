using AugaUnity;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Auga's look for the vanilla version label: Source Sans Pro Bold, brown, and the classic outline (1,-1) and
    /// shadow (1,-1) at black 50% as TextMeshPro outline and underlay. Applied in Start, after the text initialized
    /// its material (setting the outline earlier throws inside TextMeshPro).
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
                var shade = new Color(0f, 0f, 0f, 0.5f);
                text.outlineWidth = 0.15f;
                text.outlineColor = shade;
                var material = text.fontMaterial;
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, shade);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                text.UpdateMeshPadding();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Auga] Styling the version label failed: " + e.Message);
            }
            Destroy(this);
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
