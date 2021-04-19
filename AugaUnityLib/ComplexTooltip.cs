using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using JoshH.UI;
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

        public static void AddLine(Text t, object s, bool localize = true)
        {
            if (!string.IsNullOrEmpty(t.text))
            {
                t.text += "\n";
            }

            t.text += (localize ? Localization.instance.Localize(s.ToString()) : s).ToString().Trim();
        }

        public void AddLine(object a, bool localize = true)
        {
            AddLine(Text, a, localize);
        }

        public void AddLine(object a, object b, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, b, localize);
        }

        public void AddLine(object a, object b, object parenthetical, bool localize = true)
        {
            AddLine(Text, a, localize);
            AddLine(RightColumnText, $"{b} <color={ComplexTooltip.ParentheticalColor}>({parenthetical})</color>", localize);
        }
    }

    public class ComplexTooltip : MonoBehaviour
    {
        public const string ParentheticalColor = "#A39689";

        public Image Icon;
        public Image ItemBackground;
        public Image DiamondBackground;
        public Text Topic;
        public Text Subtitle;
        public Text DescriptionText;
        public GameObject BottomDivider;
        public ColoredItemBar ColoredItemBar;
        public RectTransform TextBoxContainer;
        public TooltipTextBox TwoColumnTextBoxPrefab;
        public TooltipTextBox CenteredTextBoxPrefab;

        public static event Action<ComplexTooltip, ItemDrop.ItemData> OnComplexTooltipGeneratedForItem;
        public static event Action<ComplexTooltip, Player.Food> OnComplexTooltipGeneratedForFood;
        public static event Action<ComplexTooltip, StatusEffect> OnComplexTooltipGeneratedForStatusEffect;
        public static event Action<ComplexTooltip, Skills.Skill> OnComplexTooltipGeneratedForSkill;

        private static readonly StringBuilder _sb = new StringBuilder();
        private readonly List<TooltipTextBox> _textBoxes = new List<TooltipTextBox>();

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
            _textBoxes.Add(textBox);
            return textBox;
        }

        public virtual void SetItem(ItemDrop.ItemData item)
        {
            ColoredItemBar.Setup(item);
            ItemBackground.enabled = true;
            DiamondBackground.enabled = false;
            BottomDivider.SetActive(true);
            DescriptionText.gameObject.SetActive(true);

            Icon.sprite = item.GetIcon();

            Topic.text = Localization.instance.Localize(item.m_shared.m_name);
            GenerateItemSubtext(item);
            GenerateItemTooltip(item);
            DescriptionText.text = Localization.instance.Localize(item.m_shared.m_description);

            Localization.instance.Localize(transform);

            OnComplexTooltipGeneratedForItem?.Invoke(this, item);
        }

        public void GenerateItemSubtext(ItemDrop.ItemData item)
        {
            _sb.Clear();
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Torch:
                    _sb.Append($"$skill_{item.m_shared.m_skillType.ToString().ToLowerInvariant()}");
                    _sb.Append(", ");
                    break;
            }

            _sb.Append($"${item.m_shared.m_itemType.ToString().ToLowerInvariant()}");
            Subtitle.text = Localization.instance.Localize(_sb.ToString());
        }

        public void GenerateItemTooltip(ItemDrop.ItemData item)
        {
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

            var quality = item.m_quality;
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
                    textBox.AddLine($"$item_movement_modifier: <color=#D1C9C2>{movementModifier}%</color> ($item_total:<color={ParentheticalColor}>{totalEquipmentMovementModifier}%</color>)");
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
                textBox.AddLine("$inventory_damage", DamageRange(damage.m_damage, min, max));
            if (damage.m_blunt != 0.0f)
                textBox.AddLine("$inventory_blunt", DamageRange(damage.m_blunt, min, max));
            if (damage.m_slash != 0.0f)
                textBox.AddLine("$inventory_slash", DamageRange(damage.m_slash, min, max));
            if (damage.m_pierce != 0.0f)
                textBox.AddLine("$inventory_pierce", DamageRange(damage.m_pierce, min, max));
            if (damage.m_fire != 0.0f)
                textBox.AddLine("$inventory_fire", DamageRange(damage.m_fire, min, max));
            if (damage.m_frost != 0.0f)
                textBox.AddLine("$inventory_frost", DamageRange(damage.m_frost, min, max));
            if (damage.m_lightning != 0.0f)
                textBox.AddLine("$inventory_lightning", DamageRange(damage.m_lightning, min, max));
            if (damage.m_poison != 0.0f)
                textBox.AddLine("$inventory_poison", DamageRange(damage.m_poison, min, max));
            if (damage.m_spirit != 0.0f)
                textBox.AddLine("$inventory_spirit", DamageRange(damage.m_spirit, min, max));

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

        private void AddArmorTextBox(ItemDrop.ItemData item, int quality)
        {
            var textBox = AddTextBox(TwoColumnTextBoxPrefab);
            textBox.AddLine("$item_armor", item.GetArmor(quality));

            var modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers).Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var damageModifier in modifiersTooltipString)
            {
                var fullString = damageModifier.Replace("$inventory_dmgmod: <color=orange>", "").Replace("</color>", "");
                textBox.AddLine("$inventory_dmgmod", fullString);
            }
        }

        public string DamageRange(float damage, float minFactor, float maxFactor)
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
            textBox.AddLine("$item_food_regen", $"+{healingText}<color={subValueColor}> hp/10s</color>");
            textBox.AddLine("$item_food_duration", durationText);
        }

        public virtual void SetFood(Player.Food food)
        {
            ColoredItemBar.gameObject.SetActive(false);
            ItemBackground.enabled = false;
            DiamondBackground.enabled = true;
            BottomDivider.SetActive(true);
            DescriptionText.gameObject.SetActive(true);

            Icon.sprite = food.m_item.GetIcon();
            Topic.text = food.m_item.m_shared.m_name;
            Subtitle.text = "$item_food";
            DescriptionText.text = food.m_item.m_shared.m_description;

            ClearTextBoxes();
            AddFoodTextBox(food.m_item);
            Localization.instance.Localize(transform);

            OnComplexTooltipGeneratedForFood?.Invoke(this, food);
        }

        public virtual void SetStatusEffect(StatusEffect statusEffect)
        {
            ColoredItemBar.gameObject.SetActive(false);
            ItemBackground.enabled = false;
            DiamondBackground.enabled = true;

            Icon.sprite = statusEffect.m_icon;
            Topic.text = statusEffect.m_name;
            Subtitle.text = "$effect";

            DescriptionText.text = statusEffect.m_startMessage;
            if (string.IsNullOrEmpty(DescriptionText.text))
            {
                BottomDivider.SetActive(false);
                DescriptionText.gameObject.SetActive(false);
            }
            else
            {
                BottomDivider.SetActive(true);
                DescriptionText.gameObject.SetActive(true);
            }

            var textBox = AddTextBox(CenteredTextBoxPrefab);
            textBox.Text.text = statusEffect.m_tooltip;

            Localization.instance.Localize(transform);

            OnComplexTooltipGeneratedForStatusEffect?.Invoke(this, statusEffect);
        }

        public virtual void SetSkill(Skills.Skill skill)
        {
            ColoredItemBar.gameObject.SetActive(false);
            ItemBackground.enabled = false;
            DiamondBackground.enabled = true;

            Localization.instance.Localize(transform);

            OnComplexTooltipGeneratedForSkill?.Invoke(this, skill);
        }
    }
}
