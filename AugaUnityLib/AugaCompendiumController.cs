using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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

        protected readonly List<KeyValuePair<string, GameObject>> _bestiaryItems = new List<KeyValuePair<string, GameObject>>();
        protected int _selectedBestiaryItem = -1;
        protected Dictionary<string, Humanoid> _trophyToMonsterCache;
        protected Dictionary<string, string> _monsterToLocationStringCache;

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

        public virtual void SetupMonsterToLocationStringCache()
        {
            if (ZoneSystem.instance == null || _trophyToMonsterCache == null)
            {
                return;
            }

            _monsterToLocationStringCache = new Dictionary<string, string>();
            var monsters = _trophyToMonsterCache.Values;

            foreach (var monsterPrefab in monsters)
            {
                var monsterPrefabName = monsterPrefab.name;
                var locations = new HashSet<string>();
                foreach (var location in ZoneSystem.instance.m_locations)
                {
                    if (location.m_prefab == null)
                    {
                        continue;
                    }

                    var spawners = location.m_prefab.GetComponentsInChildren<CreatureSpawner>(true);
                    foreach (var spawner in spawners)
                    {
                        if (spawner.m_creaturePrefab != null && spawner.m_creaturePrefab.name == monsterPrefabName)
                        {
                            var teleports = location.m_prefab.GetComponentsInChildren<Teleport>(true).Where(x => !string.IsNullOrEmpty(x.m_enterText));
                            if (teleports.Any())
                            {
                                foreach (var teleport in teleports)
                                {
                                    locations.Add(teleport.m_enterText);
                                }
                            }
                            else
                            {
                                locations.Add($"$biome_{location.m_biome.ToString().ToLowerInvariant()}");
                            }
                        }
                    }

                    var offeringBowls = location.m_prefab.GetComponentsInChildren<OfferingBowl>();
                    foreach (var offeringBowl in offeringBowls)
                    {
                        if (offeringBowl.m_bossPrefab != null && offeringBowl.m_bossPrefab.name == monsterPrefabName)
                        {
                            locations.Add($"$biome_{location.m_biome.ToString().ToLowerInvariant()}");
                        }
                    }
                }

                if (locations.Count > 0)
                {
                    var locationsString = string.Join(", ", locations);
                    Debug.LogWarning($"{monsterPrefabName}: {locationsString}");
                    _monsterToLocationStringCache.Add(monsterPrefabName, locationsString);
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
            SetupMonsterToLocationStringCache();
            if (_trophyToMonsterCache == null || _monsterToLocationStringCache == null)
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
                Destroy(bestiaryItem.Value);
            }
            _bestiaryItems.Clear();
            
            var trophies = player.GetTrophies();
            var tempList = new List<Tuple<int, string, GameObject>>();

            for (var index = 0; index < trophies.Count; ++index)
            {
                // TODO: Fix bug with some kind of ordering issue in the list
                var trophyName = trophies[index];
                var trophyItemPrefab = ObjectDB.instance.GetItemPrefab(trophyName);
                if (trophyItemPrefab == null)
                {
                    continue;
                }

                var trophyItem = trophyItemPrefab.GetComponent<ItemDrop>();
                var position2d = trophyItem.m_itemData.m_shared.m_trophyPos;
                var position = position2d.y * 10 + position2d.x;

                _trophyToMonsterCache.TryGetValue(trophyName, out var humanoidPrefab);
                if (humanoidPrefab != null)
                {
                    var listItem = Instantiate(BestiaryListElementPrefab, BestiaryList);
                    listItem.SetActive(true);
                    var t = listItem.transform;
                    var creatureName = Localization.instance.Localize(humanoidPrefab.m_name);
                    t.Find("name").GetComponent<Text>().text = creatureName;
                    tempList.Add(new Tuple<int, string, GameObject>(position, trophyName, listItem));
                }
            }

            var orderedList = tempList.OrderBy(x => x.Item1).ToList();
            for (var index = 0; index < orderedList.Count; index++)
            {
                var entry = orderedList[index];
                var listItem = entry.Item3;
                listItem.transform.SetSiblingIndex(index);
                var i = index;
                listItem.GetComponent<Button>().onClick.AddListener(() => OnBestiaryItemClicked(i));
                _bestiaryItems.Add(new KeyValuePair<string, GameObject>(entry.Item2, listItem));
            }

            OnBestiaryItemClicked(0);
        }

        private void OnBestiaryItemClicked(int index)
        {
            _selectedBestiaryItem = index;

            for (var i = 0; i < _bestiaryItems.Count; i++)
            {
                var bestiaryItem = _bestiaryItems[i];
                bestiaryItem.Value.transform.Find("selected").gameObject.SetActive(i == _selectedBestiaryItem);
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

            _selectedBestiaryItem = Mathf.Clamp(_selectedBestiaryItem, 0, _bestiaryItems.Count - 1);
            var selectedEntry = _bestiaryItems[_selectedBestiaryItem];
            var trophy = selectedEntry.Key;
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
            _monsterToLocationStringCache.TryGetValue(humanoidPrefab.name, out var locations);
            BestiaryBiome.text = string.IsNullOrEmpty(locations) ? "???" : locations;
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
                var amountText = (drop.m_amountMin == drop.m_amountMax ? $"{drop.m_amountMin}" : $"{drop.m_amountMin}-{drop.m_amountMax}") + $" ({Mathf.CeilToInt(drop.m_chance * 100)}%)";
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
