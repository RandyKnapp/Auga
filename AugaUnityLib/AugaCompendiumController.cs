using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class CompendiumItem : MonoBehaviour
    {
        public ItemTooltip ItemTooltip;
        public Image Icon;
        public Text Amount;

        public virtual void SetItem(ItemDrop.ItemData item, string amount = null)
        {
            ItemTooltip.Item = item;
            Icon.sprite = item.GetIcon();
            Amount.enabled = !string.IsNullOrEmpty(amount);
            Amount.text = amount;
        }
    }

    public class BestiaryStatBlock : MonoBehaviour
    {
        public GameObject Level0Label;
        public GameObject Level1Star;
        public GameObject Level2Star;
        public Text Health;
        public Text Damage;
        public Text Weakness;
        public Text Resistance;
        public Text Immune;

        public virtual void SetStats(Humanoid humanoid, int level)
        {
            Level0Label.SetActive(level == 1);
            Level1Star.SetActive(level >= 2);
            Level2Star.SetActive(level >= 3);
            Health.text = GetMaxHealth(humanoid, level).ToString("0");
            Damage.text = GetDamage(humanoid, level);

            var weak = GetDamageTypes(humanoid, HitData.DamageModifier.Weak);
            var veryWeak = GetDamageTypes(humanoid, HitData.DamageModifier.VeryWeak);
            var allWeak = weak.Concat(veryWeak).ToArray();
            var resist = GetDamageTypes(humanoid, HitData.DamageModifier.Resistant);
            var veryResist = GetDamageTypes(humanoid, HitData.DamageModifier.VeryResistant);
            var allResist = resist.Concat(veryResist).ToArray();
            var immune = GetDamageTypes(humanoid, HitData.DamageModifier.Immune);

            Weakness.text = !allWeak.Any() ? "-" : string.Join(", ", allWeak.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Resistance.text = !allResist.Any() ? "-" : string.Join(", ", allResist.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Immune.text = !immune.Any() ? "-" : string.Join(", ", immune.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
        }

        public static float GetMaxHealth(Humanoid humanoid, int level)
        {
            return humanoid.m_health * Game.instance.GetDifficultyHealthScale(humanoid.transform.position) * level;
        }

        // See Humanoid.GetLevelDamageFactor
        public static float GetLevelDamageFactor(int level)
        {
            return 1.0f + Mathf.Max(0, level - 1) * 0.5f;
        }

        public static string GetDamage(Humanoid humanoid, int level)
        {
            var levelFactor = GetLevelDamageFactor(level);
            var results = new List<string>();

            var allItems = humanoid.m_defaultItems.Concat(humanoid.m_randomWeapon).Select(x => x.GetComponent<ItemDrop>().m_itemData);
            foreach (var itemData in allItems)
            {
                if (itemData.IsWeapon())
                {
                    var damageString = GetDamageString(itemData, levelFactor);
                    results.Add(damageString);
                }
            }

            return results.Count == 0 ? "-" : string.Join(" | ", results);
        }

        private static string GetDamageString(ItemDrop.ItemData itemData, float levelFactor)
        {
            if (!itemData.HavePrimaryAttack())
            {
                return "<no primary>";
            }

            return itemData.GetDamage().GetTooltipString().Trim().Replace("\n", ", ").Replace(": <color=yellow>", " ").Replace("</color>", "");
        }

        public static List<HitData.DamageType> GetDamageTypes(Humanoid humanoid, HitData.DamageModifier modifier)
        {
            var result = new List<HitData.DamageType>();
            foreach (HitData.DamageType damageType in Enum.GetValues(typeof(HitData.DamageType)))
            {
                var mod = humanoid.m_damageModifiers.GetModifier(damageType);
                if (mod == modifier)
                {
                    result.Add(damageType);
                }
            }

            return result;
        }
    }

    public class AugaCompendiumController : MonoBehaviour
    {
        public AugaTabController TabController;
        public TextsDialog Compendium;
        public GameObject BestiaryContent;
        public RectTransform BestiaryList;
        public GameObject BestiaryListElementPrefab;
        public Image BestiaryIllustration;
        public Sprite BestiaryIllustrationPlaceholder;
        public Text BestiaryName;
        public Text BestiaryBiome;
        public Text BestiaryHostility;
        public Text BestiaryTameable;
        public Text BestiaryDescription;
        public CompendiumItem TrophyIcon;
        public RectTransform DropsContainer;
        public List<BestiaryStatBlock> StatBlocks;

        protected readonly List<GameObject> _bestiaryItems = new List<GameObject>();
        protected int _selectedBestiaryItem = -1;
        protected Dictionary<string, Humanoid> _trophyToMonsterCache;

        protected virtual void SetupTrophyToMonsterCache()
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            if (_trophyToMonsterCache != null)
            {
                return;
            }

            _trophyToMonsterCache = new Dictionary<string, Humanoid>();

            foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
            {
                var humanoid = prefab.GetComponent<Humanoid>();
                var characterDrop = prefab.GetComponent<CharacterDrop>();
                if (characterDrop == null || humanoid == null)
                {
                    continue;
                }

                var added = false;
                foreach (var drop in characterDrop.m_drops)
                {
                    var itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                    if (itemDrop != null)
                    {
                        if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
                        {
                            if (!_trophyToMonsterCache.ContainsKey(drop.m_prefab.name))
                            {
                                added = true;
                                _trophyToMonsterCache.Add(drop.m_prefab.name, humanoid);
                                break;
                            }
                        }
                    }
                }

                if (!added)
                {
                    Debug.LogWarning($"Didn't add any trophy drops for humanoid ({prefab.name})");
                }
            }
        }

        public virtual void Awake()
        {
            TabController.OnTabChanged += OnTabChanged;
        }

        public virtual void ShowCompendium()
        {
            gameObject.SetActive(true);
            TabController.SelectTab(0);
            Compendium.Setup(Player.m_localPlayer);
            SetupBestiary();
            Menu.instance.m_settingsInstance = gameObject;
        }

        private void OnTabChanged(int previousTab, int newTab)
        {
            if (newTab == 1)
            {
                OnBestiaryItemClicked(0);
            }
        }

        public virtual void HideCompendium()
        {
            Menu.instance.m_settingsInstance = null;
            gameObject.SetActive(false);
        }

        public virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu"))
            {
                HideCompendium();
            }
        }

        public virtual void SetupBestiary()
        {
            SetupTrophyToMonsterCache();
            if (_trophyToMonsterCache == null)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                BestiaryContent.SetActive(false);
                BestiaryIllustration.sprite = BestiaryIllustrationPlaceholder;
                return;
            }

            BestiaryContent.SetActive(true);

            foreach (var bestiaryItem in _bestiaryItems)
            {
                Destroy(bestiaryItem);
            }
            _bestiaryItems.Clear();
            
            var trophies = player.GetTrophies();

            for (var index = 0; index < trophies.Count; ++index)
            {
                var trophyName = trophies[index];
                var trophyItemPrefab = ObjectDB.instance.GetItemPrefab(trophyName);
                _trophyToMonsterCache.TryGetValue(trophyName, out var humanoidPrefab);
                if (trophyItemPrefab != null && humanoidPrefab != null)
                {
                    var listItem = Instantiate(BestiaryListElementPrefab, BestiaryList);
                    listItem.SetActive(true);
                    var i = index;
                    listItem.GetComponent<Button>().onClick.AddListener(() => OnBestiaryItemClicked(i));

                    var t = listItem.transform;

                    var creatureName = Localization.instance.Localize(humanoidPrefab.m_name);
                    t.Find("name").GetComponent<Text>().text = creatureName;

                    _bestiaryItems.Add(listItem);
                }
            }

            OnBestiaryItemClicked(_selectedBestiaryItem);
        }

        private void OnBestiaryItemClicked(int index)
        {
            _selectedBestiaryItem = index;

            for (var i = 0; i < _bestiaryItems.Count; i++)
            {
                var bestiaryItem = _bestiaryItems[i];
                bestiaryItem.transform.Find("selected").gameObject.SetActive(i == _selectedBestiaryItem);
            }

            if (_selectedBestiaryItem < 0)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var trophies = player.GetTrophies();
            _selectedBestiaryItem = Mathf.Clamp(_selectedBestiaryItem, 0, trophies.Count - 1);
            var trophy = trophies[_selectedBestiaryItem];
            _trophyToMonsterCache.TryGetValue(trophy, out var humanoidPrefab);
            if (humanoidPrefab == null)
            {
                return;
            }

            var trophyPrefab = ObjectDB.instance.GetItemPrefab(trophy);
            if (trophyPrefab == null)
            {
                return;
            }

            var trophyItem = trophyPrefab.GetComponent<ItemDrop>();

            BestiaryContent.SetActive(true);
            BestiaryName.text = humanoidPrefab.m_name;
            BestiaryHostility.text = humanoidPrefab.GetComponent<MonsterAI>() != null ? "$aggressive" : "$passive";
            BestiaryBiome.text = "???";
            BestiaryTameable.text = humanoidPrefab.GetComponent<Tameable>() != null ? "$tameable" : "$untameable";
            BestiaryDescription.text = trophyItem.m_itemData.m_shared.m_name + "_lore";
            TrophyIcon.SetItem(trophyItem.m_itemData);

            foreach (Transform child in DropsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var drop in humanoidPrefab.GetComponent<CharacterDrop>().m_drops)
            {
                var dropElement = Instantiate(TrophyIcon, DropsContainer);
                var amountText = (drop.m_amountMin == drop.m_amountMax ? $"{drop.m_amountMin}" : $"{drop.m_amountMin}-{drop.m_amountMax}") + $" ({Mathf.CeilToInt(drop.m_chance * 100)})";
                dropElement.SetItem(drop.m_prefab.GetComponent<ItemDrop>().m_itemData, amountText);
            }

            for (var i = 0; i < StatBlocks.Count; i++)
            {
                var statBlock = StatBlocks[i];
                statBlock.SetStats(humanoidPrefab, i + 1);
            }

            Localization.instance.Localize(BestiaryContent.transform);
        }
    }
}
