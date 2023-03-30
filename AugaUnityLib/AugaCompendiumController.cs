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
        public Text LevelLabel;
        public GameObject LevelStar;
        public Text Health;
        public Text Weakness;
        public Text Resistance;
        public Text Immune;

        public virtual void SetStats(Humanoid humanoid, int level)
        {
            LevelLabel.text = level == 1 ? "$baselevel" : (level - 1).ToString();
            LevelStar.SetActive(level >= 2);
            Health.text = GetMaxHealth(humanoid, level).ToString("0");

            var weak = GetDamageTypes(humanoid, HitData.DamageModifier.Weak);
            var veryWeak = GetDamageTypes(humanoid, HitData.DamageModifier.VeryWeak);
            var allWeak = weak.Concat(veryWeak).ToArray();
            var resist = GetDamageTypes(humanoid, HitData.DamageModifier.Resistant);
            var veryResist = GetDamageTypes(humanoid, HitData.DamageModifier.VeryResistant);
            var allResist = resist.Concat(veryResist).ToArray();
            var immune = GetDamageTypes(humanoid, HitData.DamageModifier.Immune);

            Weakness.text = !allWeak.Any() ? "-" : string.Join("\n", allWeak.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Resistance.text = !allResist.Any() ? "-" : string.Join("\n", allResist.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Immune.text = !immune.Any() ? "-" : string.Join("\n", immune.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
        }

        public static float GetMaxHealth(Humanoid humanoid, int level)
        {
            return humanoid.m_health * level;
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
        public TabHandler TabController;
        public TextsDialog Compendium;
        public TextsDialog LoreCompendium;
        public GameObject BestiaryContent;
        public RectTransform BestiaryList;
        public GameObject BestiaryListElementPrefab;
        public Text BestiaryName;
        public Text BestiaryDescription;
        public CompendiumItem CompendiumItemPrefab;
        public RectTransform DropsContainer;
        public List<BestiaryStatBlock> StatBlocks;

        protected readonly List<KeyValuePair<string, GameObject>> _bestiaryItems = new List<KeyValuePair<string, GameObject>>();
        protected int _selectedBestiaryIndex = -1;
        protected Dictionary<string, Humanoid> _trophyToMonsterCache;

        protected virtual void SetupTrophyToMonsterCache()
        {
            if (ZNetScene.instance == null || ZNetScene.instance.m_namedPrefabs == null)
                return;

            if (_trophyToMonsterCache != null)
                return;

            if (!ZNetScene.instance.m_namedPrefabs.Values.Any())
                return;

            _trophyToMonsterCache = new Dictionary<string, Humanoid>();

            foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
            {
                var humanoid = prefab.GetComponent<Humanoid>();
                var characterDrop = prefab.GetComponent<CharacterDrop>();
                if (characterDrop == null || humanoid == null || characterDrop.m_drops == null || !characterDrop.m_drops.Any())
                {
                    continue;
                }

                foreach (var drop in characterDrop.m_drops)
                {
                    if (drop == null)
                        continue;

                    var itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                    if (itemDrop != null)
                    {
                        if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
                        {
                            if (!_trophyToMonsterCache.ContainsKey(drop.m_prefab.name))
                            {
                                _trophyToMonsterCache.Add(drop.m_prefab.name, humanoid);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public virtual void ShowCompendium()
        {
            gameObject.SetActive(true);
            TabController.SetActiveTab(0);
            Compendium.Setup(Player.m_localPlayer);
            LoreCompendium.Setup(Player.m_localPlayer);
            SetupBestiary();
            Menu.instance.m_settingsInstance = gameObject;
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
                return;
            }

            BestiaryContent.SetActive(_bestiaryItems.Count > 0);

            foreach (var bestiaryItem in _bestiaryItems)
            {
                Destroy(bestiaryItem.Value);
            }
            _bestiaryItems.Clear();
            
            var trophies = player.GetTrophies();
            var tempList = new List<Tuple<int, string, GameObject>>();

            for (var index = 0; index < trophies.Count; ++index)
            {
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
                    t.Find("icon").GetComponent<Image>().sprite = trophyItem.m_itemData.GetIcon();
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
            _selectedBestiaryIndex = index;

            for (var i = 0; i < _bestiaryItems.Count; i++)
            {
                var bestiaryItem = _bestiaryItems[i];
                bestiaryItem.Value.transform.Find("selected").gameObject.SetActive(i == _selectedBestiaryIndex);
            }

            if (_selectedBestiaryIndex < 0 || _selectedBestiaryIndex >= _bestiaryItems.Count)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            _selectedBestiaryIndex = Mathf.Clamp(_selectedBestiaryIndex, 0, _bestiaryItems.Count - 1);
            var selectedEntry = _bestiaryItems[_selectedBestiaryIndex];
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
            BestiaryDescription.text = trophyItem.m_itemData.m_shared.m_name + "_lore";

            foreach (Transform child in DropsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var drop in humanoidPrefab.GetComponent<CharacterDrop>().m_drops)
            {
                var dropElement = Instantiate(CompendiumItemPrefab, DropsContainer);
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

    public class AugaTextsDialogFilter : MonoBehaviour
    {
        public string Filter;
    }
}
