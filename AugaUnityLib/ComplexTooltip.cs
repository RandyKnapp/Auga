using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

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

        public virtual void AddLine(Text t, object s, bool localize = true)
        {
            if (!string.IsNullOrEmpty(t.text))
            {
                t.text += "\n";
            }

            t.text += (localize ? Localization.instance.Localize(s.ToString()) : s).ToString().Trim();
        }

        public virtual void AddLine(object a, bool localize = true)
        {
            AddLine(Text, a, localize);
        }

        public virtual void AddLine(object a, object b, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, b, localize);
        }

        public virtual void AddLine(object a, object b, object parenthetical, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, $"{b} <color={ComplexTooltip.ParentheticalColor}>({parenthetical})</color>", localize);
        }
    }

    public class ComplexTooltip : MonoBehaviour
    {
        public const string ParentheticalColor = "#A39689";

        public enum ObjectBackgroundType { Item, Diamond, Skill }

        [CanBeNull] public Image Icon;
        [CanBeNull] public Image ItemBackground;
        [CanBeNull] public Image DiamondBackground;
        [CanBeNull] public Image SkillBackground;
        [CanBeNull] public GameObject NormalDivider;
        [CanBeNull] public GameObject WideDivider;
        public Text Topic;
        public Text Subtitle;
        [CanBeNull] public Text DescriptionText;
        [CanBeNull] public GameObject BottomDivider;
        [CanBeNull] public ColoredItemBar ColoredItemBar;
        public RectTransform TextBoxContainer;
        public TooltipTextBox TwoColumnTextBoxPrefab;
        public TooltipTextBox CenteredTextBoxPrefab;

        public static event Action<ComplexTooltip, ItemDrop.ItemData> OnComplexTooltipGeneratedForItem;
        public static event Action<ComplexTooltip, Player.Food> OnComplexTooltipGeneratedForFood;
        public static event Action<ComplexTooltip, StatusEffect> OnComplexTooltipGeneratedForStatusEffect;
        public static event Action<ComplexTooltip, Skills.Skill> OnComplexTooltipGeneratedForSkill;

        protected static readonly StringBuilder _stringBuilder = new StringBuilder();
        protected readonly List<TooltipTextBox> _textBoxes = new List<TooltipTextBox>();
        protected ItemDrop.ItemData _item;
        protected Player.Food _food;
        protected StatusEffect _statusEffect;
        protected Skills.Skill _skill;

        public virtual void Start()
        {
            TwoColumnTextBoxPrefab.gameObject.SetActive(false);
            CenteredTextBoxPrefab.gameObject.SetActive(false);
        }

        public virtual void ClearTextBoxes()
        {
            foreach (var textBox in _textBoxes)
            {
                Destroy(textBox.gameObject);
            }

            _textBoxes.Clear();
        }

        public virtual TooltipTextBox AddTextBox(TooltipTextBox prefab)
        {
            var textBox = Instantiate(prefab, TextBoxContainer, false);
            textBox.gameObject.SetActive(true);
            _textBoxes.Add(textBox);
            return textBox;
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

            SetupColoredItemBar(item);
            EnableObjectBackground(ObjectBackgroundType.Item);

            SetIcon(variant <0 ? item.GetIcon() : item.m_shared.m_icons[variant]);

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

            if (NormalDivider != null)
            {
                NormalDivider.SetActive(type != ObjectBackgroundType.Skill);
            }
            if (WideDivider != null)
            {
                WideDivider.SetActive(type == ObjectBackgroundType.Skill);
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

        private void SetIcon(Sprite icon)
        {
            if (Icon != null)
            {
                Icon.sprite = icon;
            }
        }

        private void SetDescription(string description)
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

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon 
                || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon 
                || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
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
            switch (item.m_shared.m_dlc.Length > 0)
            {
                case true:
                {
                    var textBox = AddTextBox(CenteredTextBoxPrefab);
                    textBox.AddLine("<color=aqua>$item_dlc</color>");
                    break;
                }
            }

            if (item.m_crafterID != 0L)
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
                    AddDamageTextbox(item, quality);
                    AddBlockingTextBox(item, quality);
                    AddProjectileTextBox(item, quality);
                    AddAttackStatusTextBox(item);
                    break;

                case ItemDrop.ItemData.ItemType.Shield:
                    AddBlockingTextBox(item, quality);
                    break;

                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    AddArmorTextBox(item, quality);
                    break;
            }

            // Other info
            {
                var textBox = AddTextBox(TwoColumnTextBoxPrefab);

                if (item.m_shared.m_value > 0)
                {
                    textBox.AddLine("$item_value", item.GetValue(), item.m_shared.m_value);
                }

                if (item.m_shared.m_maxQuality > 1)
                {
                    textBox.AddLine("$item_quality", quality);
                }

                if (item.m_shared.m_useDurability)
                {
                    var maxDurability = item.GetMaxDurability(quality).ToString("0");
                    var durability = item.m_durability.ToString("0");
                    var durabilityPercent = Mathf.CeilToInt(item.GetDurabilityPercentage() * 100);

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

        public virtual void AddDamageTextbox(ItemDrop.ItemData item, int quality)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var textBox = AddTextBox(TwoColumnTextBoxPrefab);

            var damage = item.GetDamage(quality);
            var skillType = item.m_shared.m_skillType;

            Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, skillType);

            if (damage.m_damage != 0.0f)
                textBox.AddLine("$inventory_damage", GetDamageRangeString(damage.m_damage, min, max));
            if (damage.m_blunt != 0.0f)
                textBox.AddLine("$inventory_blunt", GetDamageRangeString(damage.m_blunt, min, max));
            if (damage.m_slash != 0.0f)
                textBox.AddLine("$inventory_slash", GetDamageRangeString(damage.m_slash, min, max));
            if (damage.m_pierce != 0.0f)
                textBox.AddLine("$inventory_pierce", GetDamageRangeString(damage.m_pierce, min, max));
            if (damage.m_fire != 0.0f)
                textBox.AddLine("$inventory_fire", GetDamageRangeString(damage.m_fire, min, max));
            if (damage.m_frost != 0.0f)
                textBox.AddLine("$inventory_frost", GetDamageRangeString(damage.m_frost, min, max));
            if (damage.m_lightning != 0.0f)
                textBox.AddLine("$inventory_lightning", GetDamageRangeString(damage.m_lightning, min, max));
            if (damage.m_poison != 0.0f)
                textBox.AddLine("$inventory_poison", GetDamageRangeString(damage.m_poison, min, max));
            if (damage.m_spirit != 0.0f)
                textBox.AddLine("$inventory_spirit", GetDamageRangeString(damage.m_spirit, min, max));

            if (item.m_shared.m_attackForce > 0)
                textBox.AddLine("$item_knockback", item.m_shared.m_attackForce);
            if (item.m_shared.m_backstabBonus > 0)
                textBox.AddLine("$item_backstab", $"{item.m_shared.m_backstabBonus}x");
        }

        public virtual void AddBlockingTextBox(ItemDrop.ItemData item, int quality)
        {
            var blockPower = item.GetBlockPowerTooltip(quality);
            if (blockPower <= 0 || item.m_shared.m_timedBlockBonus <= 0)
            {
                return;
            }

            var textBox = AddTextBox(TwoColumnTextBoxPrefab);

            if (blockPower > 0)
            {
                textBox.AddLine("$item_blockpower", blockPower.ToString("0"));
            }

            if (item.m_shared.m_timedBlockBonus > 1)
            {
                textBox.AddLine("$item_deflection", item.GetDeflectionForce(quality));
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

        public virtual void AddArmorTextBox(ItemDrop.ItemData item, int quality)
        {
            var textBox = AddTextBox(TwoColumnTextBoxPrefab);
            textBox.AddLine("$item_armor", item.GetArmor(quality));

            var modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers).Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var damageModifier in modifiersTooltipString)
            {
                var fullString = damageModifier.Replace("<color=orange>", "").Replace("$inventory_dmgmod: ", "").Replace("</color>", "");
                textBox.AddLine("$inventory_dmgmod", fullString);
            }
        }

        public virtual string GetDamageRangeString(float damage, float minFactor, float maxFactor)
        {
            var min = Mathf.RoundToInt(damage * minFactor);
            var max = Mathf.RoundToInt(damage * maxFactor);
            return $"{Mathf.RoundToInt(damage)} <color={ParentheticalColor}>({min}-{max})</color>";
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
            var textBox = AddTextBox(CenteredTextBoxPrefab);
            textBox.Text.text = statusEffect.m_tooltip;

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
            textBox.AddLine("$level", skill.m_level);
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
