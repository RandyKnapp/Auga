using System;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class PlayerPanelFoodController : MonoBehaviour
    {
        public const string TimeFormat = @"m\:ss";

        public Color HighlightColor = Color.white;

        public int Index;
        public Text NameText;
        public Image Icon;
        public Image CountdownImage;
        public Text TimeRemainingText;
        public Text HealthText;
        public Text StaminaText;
        public Text HealingText;
        public Image HealthIcon;
        public Image StaminaIcon;
        public Image HealingIcon;

        protected UITooltip _tooltip;
        protected FoodTooltip _foodTooltip;
        protected string _hightlightColor;
        protected bool _hasFood;

        public virtual void Start()
        {
            _tooltip = GetComponent<UITooltip>();
            _foodTooltip = GetComponent<FoodTooltip>();
            _hightlightColor = ColorUtility.ToHtmlStringRGB(HighlightColor);
            Show(false);
            Update();
        }

        public virtual void Show(bool hasFood)
        {
            _hasFood = hasFood;
            NameText.enabled = hasFood;
            Icon.enabled = hasFood;
            CountdownImage.enabled = hasFood;
            TimeRemainingText.enabled = hasFood;
            HealthText.enabled = hasFood;
            StaminaText.enabled = hasFood;
            HealingText.enabled = hasFood;
            HealthIcon.enabled = hasFood;
            StaminaIcon.enabled = hasFood;
            HealingIcon.enabled = hasFood;
            _tooltip.enabled = hasFood;
        }

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var foods = player.GetFoods();
            var hasFood = Index < foods.Count;
            if (hasFood != _hasFood)
            {
                Show(hasFood);
            }

            if (_hasFood)
            {
                var food = foods[Index];
                UpdateFood(food);
            }
            else
            {
                _foodTooltip.Food = null;
            }
        }

        public virtual void UpdateFood(Player.Food food)
        {
            _foodTooltip.Food = food;

            var percent = food.m_health / food.m_item.m_shared.m_food;
            var secondsRemaining = Mathf.CeilToInt(percent * food.m_item.m_shared.m_foodBurnTime);

            NameText.text = Localization.instance.Localize(food.m_item.m_shared.m_name);
            var timeDisplay = TimeSpan.FromSeconds(secondsRemaining).ToString(TimeFormat);
            var totalTimeDisplay = TimeSpan.FromSeconds(Mathf.CeilToInt(food.m_item.m_shared.m_foodBurnTime)).ToString(TimeFormat);
            TimeRemainingText.text = $"<color={_hightlightColor}>{timeDisplay}</color> / {totalTimeDisplay}";

            Icon.sprite = food.m_item.GetIcon();
            CountdownImage.fillAmount = percent;

            HealthText.text = Mathf.CeilToInt(food.m_health).ToString();
            StaminaText.text = Mathf.CeilToInt(food.m_stamina).ToString();
            HealingText.text = food.m_item.m_shared.m_foodRegen.ToString("0.#");
        }
    }
}
