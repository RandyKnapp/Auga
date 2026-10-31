using System;
using System.Collections.Generic;
using AugaUnity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Link entries in the row of diamond buttons under the player name, the player panel's tab row. Vanilla had a row
    /// of round buttons there (compendium, trophies, skills, PvP) that mods such as Clans added their own buttons to;
    /// Auga replaces it, so mods register links here instead (API.PlayerPanel_AddLink). A link looks like a tab button
    /// but never becomes the selected tab: a click calls the mod's own action.
    ///
    /// Registrations are kept for the whole session and built again into every new inventory screen (one per world
    /// load), so a mod registers once. Links always come after the tabs.
    /// </summary>
    public static class PlayerPanelLinks
    {
        private class Entry
        {
            public string ID;
            public Sprite Icon;
            public string Tooltip;
            public Action OnClick;
            public bool Visible = true;
            public TabButton Button;
        }

        // Auga's own tab icons are glyphs drawn with padding so they sit inside the diamond; a mod's icon usually fills
        // its sprite, so it is fitted into the diamond's inner square: the middle 60% of the button
        private const float IconInset = 0.2f;

        private static readonly List<Entry> Entries = new List<Entry>();

        /// <summary>Adds a link, or updates the icon, tooltip and action of the one with this ID (keeping its visibility).</summary>
        public static bool Add(string id, Sprite icon, string tooltip, Action onClick)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            var entry = Find(id);
            if (entry == null)
            {
                entry = new Entry { ID = id };
                Entries.Add(entry);
            }

            entry.Icon = icon;
            entry.Tooltip = tooltip;
            entry.OnClick = onClick;

            var controller = WorkbenchPanelController.instance;
            if (controller != null)
            {
                if (entry.Button != null)
                    Apply(entry);
                else
                    Create(controller, entry);
                KeepLast();
            }

            return true;
        }

        public static bool Has(string id) => Find(id) != null;

        public static bool SetVisible(string id, bool visible)
        {
            var entry = Find(id);
            if (entry == null)
                return false;

            entry.Visible = visible;
            if (entry.Button != null)
                entry.Button.gameObject.SetActive(visible);
            return true;
        }

        public static bool Remove(string id)
        {
            var entry = Find(id);
            if (entry == null)
                return false;

            Entries.Remove(entry);
            if (entry.Button != null)
                Object.Destroy(entry.Button.gameObject);
            return true;
        }

        /// <summary>The link's button in the current inventory screen, or null before that screen exists.</summary>
        public static GameObject Get(string id)
        {
            var entry = Find(id);
            return entry != null && entry.Button != null ? entry.Button.gameObject : null;
        }

        /// <summary>Builds every registered link into a new inventory screen's tab row.</summary>
        public static void Build(WorkbenchPanelController controller)
        {
            if (controller == null)
                return;

            foreach (var entry in Entries)
            {
                // the buttons of the previous inventory screen went with it
                if (entry.Button != null && entry.Button.transform.parent == controller.PlayerPanelTabButtonContainer)
                    continue;
                Create(controller, entry);
            }
        }

        /// <summary>Moves the links after the tabs, in the order they were registered.</summary>
        public static void KeepLast()
        {
            foreach (var entry in Entries)
            {
                if (entry.Button != null)
                    entry.Button.transform.SetAsLastSibling();
            }
        }

        private static Entry Find(string id) => Entries.Find(x => x.ID == id);

        private static void Create(WorkbenchPanelController controller, Entry entry)
        {
            var button = Object.Instantiate(controller.TabButtonBasePrefab, controller.PlayerPanelTabButtonContainer);
            button.name = entry.ID;
            // mods' icons are seldom square; the tab icons are
            button.Icon.preserveAspect = true;
            button.Button.onClick.AddListener(() => Click(entry));
            entry.Button = button;
            Apply(entry);
        }

        private static void Apply(Entry entry)
        {
            var button = entry.Button;
            // a null icon keeps the default glyph, which is padded like the tab icons
            var inset = entry.Icon != null ? IconInset : 0f;
            if (entry.Icon != null)
                button.SetIcon(entry.Icon);
            var iconRect = button.Icon.rectTransform;
            iconRect.anchorMin = new Vector2(inset, inset);
            iconRect.anchorMax = new Vector2(1 - inset, 1 - inset);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            button.SetColor();

            var tooltip = button.GetComponent<UITooltip>();
            if (tooltip == null)
                tooltip = button.gameObject.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = Auga.Assets.SimpleTooltip;
            // UITooltip localizes it when shown
            tooltip.m_text = entry.Tooltip ?? "";

            button.gameObject.SetActive(entry.Visible);
        }

        private static void Click(Entry entry)
        {
            try
            {
                entry.OnClick?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Auga] The player panel link '{entry.ID}' failed: {e}");
            }
        }
    }
}
