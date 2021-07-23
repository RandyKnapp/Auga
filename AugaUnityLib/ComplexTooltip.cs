using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

namespace AugaUnity
{
    public class ItemTooltip : MonoBehaviour
    {
        [NonSerialized]
        public ItemDrop.ItemData Item;
    }

    public class FoodTooltip : MonoBehaviour
    {
        [NonSerialized]
        public Player.Food Food;
    }

    public class StatusTooltip : MonoBehaviour
    {
        [NonSerialized]
        public StatusEffect StatusEffect;
    }

    public class SkillTooltip : MonoBehaviour
    {
        [NonSerialized]
        public Skills.Skill Skill;
    }

    public class TooltipTextBox : MonoBehaviour
    {
        public Text Text;
        public Text RightColumnText;
        public Text ThirdColumnText;

        public virtual void AddLine(Text t, object s, bool localize = true)
        {
            if (t == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(t.text))
            {
                t.text += "\n";
            }

            t.text += (localize ? Localization.instance.Localize(s.ToString()) : s).ToString().Trim();
        }

        public virtual void AddLine(object a, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, "", false);
            AddLine(ThirdColumnText, "", false);
        }

        public virtual void AddLine(object a, object b, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, b, localize);
            AddLine(ThirdColumnText, b, false);
        }

        public virtual void AddLine(object a, object b, object parenthetical, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, $"{b} <color={ComplexTooltip.ParentheticalColor}>({parenthetical})</color>", localize);
            AddLine(ThirdColumnText, b, false);
        }

        public virtual void AddUpgradeLine(object label, object value1, object value2, string color2, bool localize = true)
        {
            AddLine(Text, label, localize);
            AddLine(RightColumnText, value1, localize);
            AddLine(ThirdColumnText, string.IsNullOrEmpty(color2) ? value2 : $"<color={color2}>{value2}</color>", localize);
        }
    }

    public class ComplexTooltip : MonoBehaviour
    {
        public const string ParentheticalColor = "#A39689";
        public const string UpgradeColor = "#1B9B37";

        public enum ObjectBackgroundType { Item, Diamond, Skill }

        [CanBeNull] public Image Icon;
        [CanBeNull] public Image ItemBackground;
        [CanBeNull] public Image DiamondBackground;
        [CanBeNull] public Image SkillBackground;
        [CanBeNull] public GameObject NormalDivider;
        public Text Topic;
        public Text Subtitle;
        [CanBeNull] public Text DescriptionText;
        [CanBeNull] public GameObject BottomDivider;
        [CanBeNull] public ColoredItemBar ColoredItemBar;
        [CanBeNull] public Text ItemQuality;
        public RectTransform TextBoxContainer;
        [CanBeNull] public RectTransform LowerTextBoxContainer;
        public TooltipTextBox TwoColumnTextBoxPrefab;
        public TooltipTextBox CenteredTextBoxPrefab;
        [CanBeNull] public TooltipTextBox UpgradeLabelsPrefab;
        [CanBeNull] public TooltipTextBox UpgradeTwoColumnTextBoxPrefab;

        public static event Action<ComplexTooltip, ItemDrop.ItemData> OnComplexTooltipGeneratedForItem;
        public static event Action<ComplexTooltip, Player.Food> OnComplexTooltipGeneratedForFood;
        public static event Action<ComplexTooltip, StatusEffect> OnComplexTooltipGeneratedForStatusEffect;
        public static event Action<ComplexTooltip, Skills.Skill> OnComplexTooltipGeneratedForSkill;

        protected static readonly StringBuilder _stringBuilder = new StringBuilder();
        protected readonly List<GameObject> _textBoxes = new List<GameObject>();
        protected ItemDrop.ItemData _item;
        protected Player.Food _food;
        protected StatusEffect _statusEffect;
        protected Skills.Skill _skill;

        public virtual void Start()
        {
            TwoColumnTextBoxPrefab.gameObject.SetActive(false);
            CenteredTextBoxPrefab.gameObject.SetActive(false);

            if (UpgradeLabelsPrefab != null)
            {
                UpgradeLabelsPrefab.gameObject.SetActive(false);
            }

            if (UpgradeTwoColumnTextBoxPrefab != null)
            {
                UpgradeTwoColumnTextBoxPrefab.gameObject.SetActive(false);
            }
        }

        public virtual void ClearTextBoxes()
        {
            foreach (var textBox in _textBoxes)
            {
                Destroy(textBox);
            }

            _textBoxes.Clear();
            if (LowerTextBoxContainer != null)
            {
                LowerTextBoxContainer.gameObject.SetActive(false);
            }
        }

        public virtual TooltipTextBox AddTextBox(TooltipTextBox prefab, RectTransform container = null)
        {
            if (container == null)
            {
                container = TextBoxContainer;
            }

            container.gameObject.SetActive(true);
            var textBox = Instantiate(prefab, container, false);
            textBox.gameObject.SetActive(true);
            _textBoxes.Add(textBox.gameObject);
            return textBox;
        }

        public GameObject AddDivider(RectTransform container = null)
        {
            if (container == null)
            {
                container = TextBoxContainer;
            }

            container.gameObject.SetActive(true);
            var divider = Instantiate(NormalDivider, container, false);
            divider.SetActive(true);
            _textBoxes.Add(divider);
            return divider;
        }

        public virtual void ClearData()
        {
            _item = null;
            _food = null;
            _statusEffect = null;
            _skill = null;
        }

        public virtual void SetTopic(string topic)
        {
            Topic.text = topic;
        }

        public virtual void SetSubtitle(string topic)
        {
            Subtitle.text = topic;
        }

        public virtual void SetItem(ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
            ClearData();
            _item = item;
            quality = quality < 0 ? item.m_quality : quality;

            if (ItemQuality != null)
            {
                ItemQuality.transform.parent.gameObject.SetActive(item.m_shared.m_maxQuality > 1);
                ItemQuality.text = quality.ToString();
            }

            SetupColoredItemBar(item);
            EnableObjectBackground(ObjectBackgroundType.Item);

            SetIcon(variant < 0 ? item.GetIcon() : item.m_shared.m_icons[variant]);

            SetTopic(Localization.instance.Localize(item.m_shared.m_name));
            SetSubtitle(GenerateItemSubtext(item));
            GenerateItemTextBoxes(item, quality);
            SetDescription(Localization.instance.Localize(item.m_shared.m_description));

            Localization.instance.Localize(transform);

            OnComplexTooltipGeneratedForItem?.Invoke(this, item);
        }

        public virtual void SetupColoredItemBar(ItemDrop.ItemData item)
        {
            if (ColoredItemBar != null)
            {
                ColoredItemBar.gameObject.SetActive(item != null);
                if (item != null)
                {
                    ColoredItemBar.Setup(item);
                }
            }
        }

        public virtual void EnableObjectBackground(ObjectBackgroundType type)
        {
            if (ItemBackground != null)
            {
                ItemBackground.enabled = type == ObjectBackgroundType.Item;
            }
            if (DiamondBackground != null)
            {
                DiamondBackground.enabled = type == ObjectBackgroundType.Diamond;
            }
            if (SkillBackground != null)
            {
                SkillBackground.enabled = type == ObjectBackgroundType.Skill;
            }
        }

        public virtual void EnableDescription(bool enable)
        {
            if (BottomDivider != null)
            {
                BottomDivider.SetActive(enable);
            }
            if (DescriptionText != null)
            {
                DescriptionText.gameObject.SetActive(enable);
            }
        }

        public virtual void SetIcon(Sprite icon)
        {
            if (Icon != null)
            {
                Icon.sprite = icon;
            }
        }

        public virtual void SetDescription(string description)
        {
            var hasDescription = !string.IsNullOrEmpty(description);
            EnableDescription(hasDescription);
            if (hasDescription && DescriptionText != null)
            {
                DescriptionText.text = description;
            }
        }

        public virtual string GenerateItemSubtext(ItemDrop.ItemData item)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append($"$itemtype_{item.m_shared.m_itemType.ToString().ToLowerInvariant()}");

            if (item.m_shared.m_food > 0)
            {
                _stringBuilder.Append(", $item_food");
            }

            if ((item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon 
                || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon 
                || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
                && item.m_shared.m_skillType != Skills.SkillType.None)
            {
                _stringBuilder.Append(", ");
                _stringBuilder.Append($"$skill_singular_{item.m_shared.m_skillType.ToString().ToLowerInvariant()}");
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
            {
                _stringBuilder.Append(", $item_onehanded");
            }

            return Localization.instance.Localize(_stringBuilder.ToString());
        }

        public virtual void GenerateItemTextBoxes(ItemDrop.ItemData item, int quality)
        {
            ClearTextBoxes();

            var upgrade = item.m_quality != quality;
            if (upgrade)
            {
                var labels = AddTextBox(UpgradeLabelsPrefab);
                labels.AddLine(item.m_quality, quality, false);
            }

            if (!upgrade && item.m_shared.m_dlc.Length > 0)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);
                textBox.AddLine("<color=aqua>$item_dlc</color>");
            }

            if (!upgrade && item.m_crafterID != 0L)
            {
                var textBox = AddTextBox(TwoColumnTextBoxPrefab);
                textBox.AddLine("$item_crafter", item.m_crafterName);
            }

            var statusEffectTooltip = item.GetStatusEffectTooltip();
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Consumable:
                    if (item.m_shared.m_food > 0)
                    {
                        AddFoodTextBox(item);
                    }
                    else if (!string.IsNullOrEmpty(statusEffectTooltip))
                    {
                        var textBox = AddTextBox(CenteredTextBoxPrefab);
                        textBox.AddLine(statusEffectTooltip);
                    }
                    break;

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Ammo:
                    AddDamageTextbox(item, quality, upgrade);
                    AddBlockingTextBox(item, quality, upgrade);
                    AddProjectileTextBox(item, quality);
                    AddAttackStatusTextBox(item);
                    break;

                case ItemDrop.ItemData.ItemType.Shield:
                    AddBlockingTextBox(item, quality, upgrade);
                    break;

                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    AddArmorTextBox(item, quality, upgrade);
                    break;
            }

            // Other info
            {
                var textBox = AddTextBox(upgrade ? UpgradeTwoColumnTextBoxPrefab : TwoColumnTextBoxPrefab);

                if (item.m_shared.m_value > 0)
                {
                    textBox.AddLine("$item_value", item.GetValue(), item.m_shared.m_value);
                }

                if (item.m_shared.m_maxQuality > 1)
                {
                    textBox.AddLine("$item_quality", quality, upgrade);
                }

                if (item.m_shared.m_useDurability)
                {
                    var maxDurability = item.GetMaxDurability(quality);
                    var durability = item.m_durability.ToString("0");
                    var durabilityPercent = Mathf.CeilToInt(item.GetDurabilityPercentage() * 100);

                    if (upgrade)
                    {
                        var prevMaxDurability = item.GetMaxDurability(item.m_quality);
                        var better = prevMaxDurability < maxDurability;
                        textBox.AddUpgradeLine("$item_durability", prevMaxDurability, maxDurability, better ? UpgradeColor : null);
                    }
                    else
                    {
                        textBox.AddLine("$item_durability", $"{durabilityPercent}%", $"{durability}/{maxDurability}");
                        if (item.m_shared.m_canBeReparied)
                        {
                            var recipe = ObjectDB.instance.GetRecipe(item);
                            if (recipe != null)
                            {
                                textBox.AddLine("$item_repairlevel", recipe.m_minStationLevel);
                            }
                        }
                    }
                }

                textBox.AddLine("$item_weight", item.GetWeight().ToString("0.0"));
            }

            if (!item.m_shared.m_teleportable || item.m_shared.m_movementModifier != 0)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);

                if (!item.m_shared.m_teleportable)
                {
                    textBox.AddLine("$item_noteleport");
                }

                if (item.m_shared.m_movementModifier != 0)
                {
                    var movementModifier = (item.m_shared.m_movementModifier * 100).ToString("+0;-0");
                    var totalEquipmentMovementModifier = (Player.m_localPlayer.GetEquipmentMovementModifier() * 100).ToString("+0;-0");
                    textBox.AddLine($"$item_movement_modifier: <color=#D1C9C2>{movementModifier}%</color> ($item_total: <color={ParentheticalColor}>{totalEquipmentMovementModifier}%</color>)");
                }
            }

            var setStatusEffect = item.GetSetStatusEffectTooltip();
            if (!string.IsNullOrEmpty(setStatusEffect))
            {
                var textBox = AddTextBox(TwoColumnTextBoxPrefab);
                textBox.AddLine("$item_seteffect", "", $"{item.m_shared.m_setSize} $item_parts");
                textBox.AddLine("", setStatusEffect);
            }
        }

        public virtual void AddDamageTextbox(ItemDrop.ItemData item, int quality, bool upgrade)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var textBox = AddTextBox(upgrade ? UpgradeTwoColumnTextBoxPrefab : TwoColumnTextBoxPrefab);

            var damage = item.GetDamage(quality);
            var previousDamage = item.GetDamage(item.m_quality);
            var skillType = item.m_shared.m_skillType;

            Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, skillType);

            if (damage.m_damage != 0.0f)
                AddDamageLine(textBox, "$inventory_damage", damage.m_damage, previousDamage.m_damage, min, max, upgrade);
            if (damage.m_blunt != 0.0f)
                AddDamageLine(textBox, "$inventory_blunt", damage.m_blunt, previousDamage.m_blunt, min, max, upgrade);
            if (damage.m_slash != 0.0f)
                AddDamageLine(textBox, "$inventory_slash", damage.m_slash, previousDamage.m_slash, min, max, upgrade);
            if (damage.m_pierce != 0.0f)
                AddDamageLine(textBox, "$inventory_pierce", damage.m_pierce, previousDamage.m_pierce, min, max, upgrade);
            if (damage.m_fire != 0.0f)
                AddDamageLine(textBox, "$inventory_fire", damage.m_fire, previousDamage.m_fire, min, max, upgrade);
            if (damage.m_frost != 0.0f)
                AddDamageLine(textBox, "$inventory_frost", damage.m_frost, previousDamage.m_frost, min, max, upgrade);
            if (damage.m_lightning != 0.0f)
                AddDamageLine(textBox, "$inventory_lightning",damage.m_lightning, previousDamage.m_lightning, min, max, upgrade);
            if (damage.m_poison != 0.0f)
                AddDamageLine(textBox, "$inventory_poison", damage.m_poison, previousDamage.m_poison, min, max, upgrade);
            if (damage.m_spirit != 0.0f)
                AddDamageLine(textBox, "$inventory_spirit", damage.m_spirit, previousDamage.m_spirit, min, max, upgrade);

            if (item.m_shared.m_attackForce > 0)
                textBox.AddLine("$item_knockback", item.m_shared.m_attackForce);
            if (item.m_shared.m_backstabBonus > 0)
                textBox.AddLine("$item_backstab", $"{item.m_shared.m_backstabBonus}x");
        }

        private void AddDamageLine(TooltipTextBox textBox, string label, float damage, float previousDamage, float min, float max, bool upgrade)
        {
            if (upgrade)
            {
                textBox.AddUpgradeLine(label, previousDamage, damage, damage > previousDamage ? UpgradeColor : null);
            }
            else
            {
                textBox.AddLine(label, GetDamageRangeString(damage, min, max));
            }
        }

        public virtual string GetDamageRangeString(float damage, float minFactor, float maxFactor)
        {
            var min = Mathf.RoundToInt(damage * minFactor);
            var max = Mathf.RoundToInt(damage * maxFactor);
            return $"{Mathf.RoundToInt(damage)} <color={ParentheticalColor}>({min}-{max})</color>";
        }

        public virtual void AddBlockingTextBox(ItemDrop.ItemData item, int quality, bool upgrade)
        {
            var blockPower = item.GetBlockPowerTooltip(quality);
            var previousBlockPower = item.GetBlockPowerTooltip(item.m_quality);
            if (blockPower <= 0 || item.m_shared.m_timedBlockBonus <= 0)
            {
                return;
            }

            var textBox = AddTextBox(upgrade ? UpgradeTwoColumnTextBoxPrefab : TwoColumnTextBoxPrefab);

            if (blockPower > 0)
            {
                if (upgrade)
                {
                    textBox.AddUpgradeLine("$item_blockpower", previousBlockPower.ToString("0"), blockPower.ToString("0"), blockPower > previousBlockPower ? UpgradeColor : null);
                }
                else
                {
                    textBox.AddLine("$item_blockpower", blockPower.ToString("0"));
                }
            }

            if (item.m_shared.m_timedBlockBonus > 1)
            {
                var deflectForce = item.GetDeflectionForce(quality);
                var previousDeflectForce = item.GetDeflectionForce(item.m_quality);
                if (upgrade)
                {
                    textBox.AddUpgradeLine("$item_blockpower", previousDeflectForce.ToString("0"), deflectForce.ToString("0"), deflectForce > previousDeflectForce ? UpgradeColor : null);
                }
                else
                {
                    textBox.AddLine("$item_deflection", deflectForce);
                }
                textBox.AddLine("$item_parrybonus", $"{item.m_shared.m_timedBlockBonus}x");
            }
        }

        public virtual void AddProjectileTextBox(ItemDrop.ItemData item, int quality)
        {
            var projectileTooltip = item.GetProjectileTooltip(quality);
            if (projectileTooltip.Length > 0)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);
                textBox.AddLine(projectileTooltip);
            }
        }

        public virtual void AddAttackStatusTextBox(ItemDrop.ItemData item)
        {
            var statusEffectTooltip = item.GetStatusEffectTooltip();
            if (statusEffectTooltip.Length > 0)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);
                textBox.AddLine(statusEffectTooltip);
            }
        }

        public virtual void AddArmorTextBox(ItemDrop.ItemData item, int quality, bool upgrade)
        {
            var textBox = AddTextBox(upgrade ? UpgradeTwoColumnTextBoxPrefab : TwoColumnTextBoxPrefab);
            var armor = item.GetArmor(quality);
            var previousArmor = item.GetArmor(item.m_quality);
            if (upgrade)
            {
                textBox.AddUpgradeLine("$item_armor", previousArmor, armor, armor > previousArmor ? UpgradeColor : null);
            }
            else
            {
                textBox.AddLine("$item_armor", armor);
            }

            var modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers).Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var damageModifier in modifiersTooltipString)
            {
                var fullString = damageModifier.Replace("<color=orange>", "").Replace("$inventory_dmgmod: ", "").Replace("</color>", "");
                textBox.AddLine("$inventory_dmgmod", fullString);
            }
        }

        public virtual void AddFoodTextBox(ItemDrop.ItemData item)
        {
            var textBox = AddTextBox(TwoColumnTextBoxPrefab);

            const string subValueColor = "#706457";
            var healthText = item.m_shared.m_food.ToString("0");
            var staminaText = item.m_shared.m_foodStamina.ToString("0");
            var healingText = item.m_shared.m_foodRegen.ToString("0.#");
            var durationText = TimeSpan.FromSeconds(Mathf.CeilToInt(item.m_shared.m_foodBurnTime)).ToString(PlayerPanelFoodController.TimeFormat);

            textBox.AddLine("$item_food_health", healthText);
            textBox.AddLine("$item_food_stamina", staminaText);
            textBox.AddLine("$item_food_regen", $"+{healingText}<color={subValueColor}> $healing_tick</color>");
            textBox.AddLine("$item_food_duration", durationText);
        }

        public virtual void SetFood(Player.Food food)
        {
            ClearData();
            _food = food;

            SetupColoredItemBar(null);
            EnableObjectBackground(ObjectBackgroundType.Diamond);

            SetIcon(food.m_item.GetIcon());
            SetTopic(food.m_item.m_shared.m_name);
            SetSubtitle("$item_food");
            SetDescription(food.m_item.m_shared.m_description);

            ClearTextBoxes();
            AddFoodTextBox(food.m_item);

            Localization.instance.Localize(transform);
            OnComplexTooltipGeneratedForFood?.Invoke(this, food);
        }

        public virtual void SetStatusEffect(StatusEffect statusEffect)
        {
            ClearData();
            _statusEffect = statusEffect;

            SetupColoredItemBar(null);
            EnableObjectBackground(ObjectBackgroundType.Diamond);

            SetIcon(statusEffect.m_icon);
            SetTopic(statusEffect.m_name);
            SetSubtitle("$effect");
            SetDescription(statusEffect.m_startMessage);

            ClearTextBoxes();
            var tooltipString = statusEffect.GetTooltipString();
            var parts = tooltipString.Split(new []{ "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);
                textBox.Text.text = $"<color=#D1C9C2>{string.Join("\n", parts.Skip(1))}</color>";
            }
            if (parts.Length > 0)
            {
                var textBox = AddTextBox(CenteredTextBoxPrefab);
                textBox.Text.text = parts[0];
            }

            Localization.instance.Localize(transform);
            OnComplexTooltipGeneratedForStatusEffect?.Invoke(this, statusEffect);
        }

        public virtual void SetSkill(Skills.Skill skill)
        {
            ClearData();
            _skill = skill;

            SetupColoredItemBar(null);
            EnableObjectBackground(ObjectBackgroundType.Skill);
            EnableDescription(false);

            SetIcon(skill.m_info.m_icon);
            SetTopic(Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower()));
            SetSubtitle("$skill");

            ClearTextBoxes();
            var textBox = AddTextBox(TwoColumnTextBoxPrefab);
            textBox.AddLine("$level", skill.m_level.ToString("0"));
            textBox.AddLine("$experience", Mathf.CeilToInt(skill.m_accumulator));
            textBox.AddLine("$to_next_level", Mathf.CeilToInt(skill.GetNextLevelRequirement()));

            var textBox2 = AddTextBox(CenteredTextBoxPrefab);
            textBox2.AddLine(skill.m_info.m_description);

            // TODO: Advanced tooltip for skills

            Localization.instance.Localize(transform);
            OnComplexTooltipGeneratedForSkill?.Invoke(this, skill);
        }
    }
}
