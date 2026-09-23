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
