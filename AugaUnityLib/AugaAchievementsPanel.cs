using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    /// <summary>
    /// The compendium's Achievements page. It fills its grid the way vanilla's InventoryGui fills the achievements
    /// screen (grouped by icon tier, secret ones hidden, one tile per achievement, a completion count and the
    /// cheated notice), and leaves the achievement details popup and the gamepad navigation to the vanilla
    /// AchievementsGui component on the same object, whose references Auga points at vanilla's details popup.
    /// Each tile also carries the name and description as a tooltip.
    /// </summary>
    public class AugaAchievementsPanel : MonoBehaviour
    {
        public RectTransform Grid;
        public GameObject ElementPrefab;
        public Scrollbar Scrollbar;
        public TMP_Text CompletionRate;
        public TMP_Text CheatedText;
        public AchievementsGui Gui;

        private readonly List<GameObject> _elements = new List<GameObject>();

        public bool DetailsOpen => Gui != null && Gui.m_achievementDetails != null && Gui.m_achievementDetails.activeSelf;

        public void OnEnable()
        {
            Refresh();
        }

        public void Update()
        {
            // AchievementsGui reads its column count once on Start from whatever grid it finds first; keep it right
            if (Gui != null)
                Gui.m_width = Columns();
        }

        /// <summary>Closes the details popup when it is open; true when it was.</summary>
        public bool CloseDetailsIfOpen()
        {
            if (!DetailsOpen)
                return false;
            Gui.OnCloseAchievementDetails();
            return true;
        }

        public void Refresh()
        {
            foreach (var element in _elements)
            {
                if (element != null) Destroy(element);
            }
            _elements.Clear();
            if (Gui != null) Gui.m_achievementsList.Clear();

            var player = Player.m_localPlayer;
            if (player == null || Achievements.m_instance == null || Grid == null || ElementPrefab == null)
            {
                if (Gui != null) Gui.enabled = false;
                return;
            }

            // vanilla orders the achievements by the tier its locked icon is named after (01_..., 02_..., ...)
            var tiers = new List<List<Achievement>>();
            for (var i = 0; i <= 9; i++)
                tiers.Add(new List<Achievement>());
            var unlocked = 0;
            foreach (var list in Achievements.m_instance.m_achievementLists)
            {
                if (list == null) continue;
                foreach (var achievement in list.m_achievements)
                {
                    if (achievement == null) continue;
                    var lockedName = achievement.m_iconLocked != null ? achievement.m_iconLocked.name : string.Empty;
                    if (lockedName.Length >= 2 && int.TryParse(lockedName.Substring(0, 2), out var tier) && tier >= 0 && tier < tiers.Count)
                        tiers[tier].Add(achievement);
                    else
                        tiers[tiers.Count - 1].Add(achievement);
                    if (achievement.m_unlocked)
                        unlocked++;
                }
            }
            var ordered = new List<Achievement>();
            for (var i = 1; i <= 9; i++)
                ordered.AddRange(tiers[i]);
            if (CompletionRate != null)
                CompletionRate.text = $"{unlocked}/{ordered.Count}";

            foreach (var item in ordered)
            {
                var element = Instantiate(ElementPrefab, Grid);
                element.SetActive(true);
                _elements.Add(element);
                if (Gui != null) Gui.m_achievementsList.Add(element);

                var achievement = item;
                var clickable = (!achievement.m_isSecret || achievement.m_unlocked) && achievement.m_clickable;
                var button = element.GetComponent<Button>();
                if (button != null)
                {
                    if (clickable && Gui != null)
                    {
                        button.onClick.AddListener(() => Gui.OnOpenAchievementDetails(achievement, true));
                    }
                    else
                    {
                        // vanilla destroys the button of a tile without details; a destroyed Selectable resets its
                        // graphic to white, and the tile's frame gets its dark tone from the button's tint, so the
                        // button stays and only stops reacting
                        button.transition = Selectable.Transition.None;
                        button.interactable = false;
                    }
                }

                var shown = achievement.m_unlocked || !achievement.m_isSecret;
                var icon = shown && achievement.m_unlocked ? achievement.m_icon : achievement.m_iconLocked;
                var name = Localization.instance.Localize(shown ? achievement.m_name : "$inventory_achievement_secret");
                var description = Localization.instance.Localize(shown ? achievement.m_description : "$inventory_achievement_secret_description");
                var iconImage = element.transform.Find("icon")?.GetComponent<Image>();
                if (iconImage != null) iconImage.sprite = icon;
                var nameText = element.transform.Find("name")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = name;
                var descriptionText = element.transform.Find("description")?.GetComponent<TMP_Text>();
                if (descriptionText != null) descriptionText.text = description;
                var tooltip = element.GetComponent<UITooltip>();
                if (tooltip != null)
                {
                    tooltip.m_topic = name;
                    tooltip.m_text = description;
                }
                var tile = element.GetComponent<AugaAchievementElement>();
                if (tile != null)
                    tile.SetConcealed(!shown);
            }

            if (Scrollbar != null)
                Scrollbar.value = 1f;
            UpdateCheatedText(player);
            if (Gui != null)
            {
                Gui.m_width = Columns();
                Gui.enabled = _elements.Count > 0;   // its navigation indexes the list every frame
            }
        }

        private void UpdateCheatedText(Player player)
        {
            if (CheatedText == null)
                return;
            var profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            var usedCheats = profile != null && profile.m_usedCheats;
            var cheatedItem = player.GetInventory().AnyCheatedItem();
            string token;
            if (PlayerProfile.s_bypassCheatChecks && (Achievements.IsWorldCheated() || usedCheats || cheatedItem))
                token = "$achievements_permanently_cheated_bypass";
            else if (Achievements.IsWorldCheated())
                token = "$achievements_permanently_cheated_world";
            else if (usedCheats)
                token = "$achievements_permanently_cheated_character";
            else if (cheatedItem)
                token = "$achievements_temporarily_cheated";
            else
                token = string.Empty;
            CheatedText.text = string.IsNullOrEmpty(token) ? string.Empty : Localization.instance.Localize(token);
        }

        /// <summary>How many tiles the grid fits per row, for the gamepad navigation.</summary>
        private int Columns()
        {
            var grid = Grid != null ? Grid.GetComponent<GridLayoutGroup>() : null;
            if (grid == null)
                return 1;
            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                return Mathf.Max(1, grid.constraintCount);
            var width = Grid.rect.width;
            if (width <= 0f && Grid.parent is RectTransform parent)
                width = parent.rect.width;
            var step = grid.cellSize.x + grid.spacing.x;
            if (step <= 0f)
                return 1;
            return Mathf.Max(1, Mathf.FloorToInt((width - grid.padding.horizontal + grid.spacing.x) / step));
        }
    }
}
