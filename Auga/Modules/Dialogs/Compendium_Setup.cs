using System;
using AugaUnity;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// The compendium's Achievements page runs on vanilla's achievement code: the page's AchievementsGui (the
    /// vanilla class) gets vanilla's own details popup, which is moved out of the hidden vanilla achievements
    /// screen into the page and restyled, and the page's AugaAchievementsPanel fills the grid.
    /// </summary>
    public static class Compendium_Setup
    {
        /// <summary>
        /// Mods that extend the compendium expect vanilla's layout on every TextsDialog: EpicLoot's pages look for
        /// "Texts_frame" with a "Closebutton" and a "TextArea" (ScrollArea/Content) under it the moment the dialog
        /// wakes up, and fail without them. The page content that holds the dialog's text area takes the frame's
        /// name and gets a hidden close button placeholder; a dialog without a text area gets an inactive frame with
        /// the expected pieces.
        /// </summary>
        public static void SetupTextsDialogs(AugaCompendiumController controller)
        {
            if (controller == null)
                return;
            foreach (var dialog in controller.GetComponentsInChildren<TextsDialog>(true))
            {
                try
                {
                    EnsureVanillaFrame(dialog);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Auga] Compendium text dialog frame setup failed for {dialog.name}: {e}");
                }
            }
        }

        private static void EnsureVanillaFrame(TextsDialog dialog)
        {
            var root = dialog.transform;
            var frame = root.Find("Texts_frame");
            if (frame == null)
            {
                var textArea = dialog.m_textArea != null ? dialog.m_textArea.transform : null;
                foreach (Transform child in root)
                {
                    if (textArea != null && textArea.IsChildOf(child) && child.Find("TextArea/ScrollArea/Content") != null)
                    {
                        frame = child;
                        break;
                    }
                }
                if (frame != null)
                {
                    frame.name = "Texts_frame";
                }
                else
                {
                    frame = NewStretchedRect("Texts_frame", root);
                    var area = NewStretchedRect("TextArea", frame);
                    var scrollArea = NewStretchedRect("ScrollArea", area);
                    scrollArea.gameObject.AddComponent<ScrollRect>();
                    var content = NewStretchedRect("Content", scrollArea);
                    content.gameObject.AddComponent<VerticalLayoutGroup>();
                    frame.gameObject.SetActive(false);
                }
            }
            if (frame.Find("Closebutton") == null)
            {
                var close = NewStretchedRect("Closebutton", frame);
                close.gameObject.AddComponent<Image>().enabled = false;
                close.gameObject.AddComponent<Button>();
                close.gameObject.SetActive(false);
            }
        }

        private static Transform NewStretchedRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void SetupAchievements(AugaCompendiumController controller)
        {
            var page = controller != null ? controller.Achievements : null;
            if (page == null)
                return;
            try
            {
                AdoptDetails(page);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auga] Compendium achievements setup failed: {e}");
            }
        }

        private static void AdoptDetails(AugaAchievementsPanel page)
        {
            var gui = page.Gui != null ? page.Gui : page.GetComponent<AchievementsGui>();
            var vanilla = InventoryGui.instance != null ? InventoryGui.instance.m_achievementsPanel : null;
            var details = vanilla != null ? vanilla.m_achievementDetails : null;
            if (gui == null || details == null)
            {
                Auga.LogWarning("Compendium achievements: no vanilla details popup to adopt; achievement details stay unavailable.");
                return;
            }
            page.Gui = gui;

            // the popup, its row template, list root and scrollbar are vanilla's; they move into the page and the
            // page's gui takes them over (the scrollbar reference is set before the restyle, which swaps the bar)
            details.transform.SetParent(page.transform, false);
            details.transform.SetAsLastSibling();
            details.SetActive(false);
            gui.m_achievementDetails = details;
            gui.m_achievementDetailsElementPrefab = vanilla.m_achievementDetailsElementPrefab;
            gui.m_achievementDetailsListRoot = vanilla.m_achievementDetailsListRoot;
            gui.m_detailsScrollbar = vanilla.m_detailsScrollbar;
            gui.m_scrollbar = page.Scrollbar;

            // the popup's close button is wired to vanilla's gui; it closes through the page's now
            foreach (var button in details.GetComponentsInChildren<Button>(true))
            {
                if (button.name != "Closebutton") continue;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(gui.OnCloseAchievementDetails);
            }

            AugaPanelRestyler.Restyle(details.transform, new RestyleOptions
            {
                Titles = { "topic" },
                DetectHeaders = false,
                Skip = { "coverInteraction" },   // the dark cover that keeps clicks off the grid behind the popup
                BackgroundOverhang = 0f,
            });
            // the list's own frame art; the Auga panel background is enough
            var listFrame = details.transform.Find("DetailsContainer/DetailsList")?.GetComponent<Image>();
            if (listFrame != null) listFrame.enabled = false;
            Localization.instance.Localize(details.transform);
        }
    }
}
