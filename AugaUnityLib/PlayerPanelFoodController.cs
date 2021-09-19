using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class PlayerPanelFoodController : MonoBehaviour
    {
        public const string TimeFormat = @"m\:ss";

        public Color HighlightColor = Color.white;

        public int Index;
        public bool FlashOnCanEatAgain;
        [CanBeNull] public Text NameText;
        public Image Icon;
        public Image CountdownImage;
        [CanBeNull] public Text TimeRemainingText;
        [CanBeNull] public Text HealthText;
        [CanBeNull] public Text StaminaText;
        [CanBeNull] public Text HealingText;
        [CanBeNull] public Image HealthIcon;
        [CanBeNull] public Image StaminaIcon;
        [CanBeNull] public Image HealingIcon;

        [CanBeNull] protected UITooltip _tooltip;
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
            if (NameText != null)
            {
                NameText.enabled = hasFood;
            }

            Icon.enabled = hasFood;
            CountdownImage.enabled = hasFood;
            if (TimeRemainingText != null)
            {
                TimeRemainingText.enabled = hasFood;
            }

            if (HealthText != null)
            {
                HealthText.enabled = hasFood;
            }

            if (StaminaText != null)
            {
                StaminaText.enabled = hasFood;
            }

            if (HealingText != null)
            {
                HealingText.enabled = hasFood;
            }

            if (HealthIcon != null)
            {
                HealthIcon.enabled = hasFood;
            }

            if (StaminaIcon != null)
            {
                StaminaIcon.enabled = hasFood;
            }

            if (HealingIcon != null)
            {
                HealingIcon.enabled = hasFood;
            }

            if (_tooltip != null)
            {
                _tooltip.enabled = hasFood;
            }
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
            
            var percent = food.m_time / food.m_item.m_shared.m_foodBurnTime;
            var secondsRemaining = Mathf.CeilToInt(food.m_time);

            if (NameText != null)
            {
                NameText.text = Localization.instance.Localize(food.m_item.m_shared.m_name);
            }

            var timeDisplay = TimeSpan.FromSeconds(secondsRemaining).ToString(TimeFormat);
            var totalTimeDisplay = TimeSpan.FromSeconds(Mathf.CeilToInt(food.m_item.m_shared.m_foodBurnTime)).ToString(TimeFormat);
            if (TimeRemainingText != null)
            {
                TimeRemainingText.text = $"<color={_hightlightColor}>{timeDisplay}</color> / {totalTimeDisplay}";
            }

            Icon.sprite = food.m_item.GetIcon();
            CountdownImage.fillAmount = percent;

            if (FlashOnCanEatAgain)
            {
                if (food.CanEatAgain())
                {
                    Icon.color = new Color(1f, 1f, 1f, 0.6f + Mathf.Sin(Time.time * 10.0f) * 0.4f);
                }
                else
                {
                    Icon.color = Color.white;
                }
            }

            if (HealthText != null)
            {
                HealthText.text = Mathf.CeilToInt(food.m_health).ToString();
            }

            if (StaminaText != null)
            {
                StaminaText.text = Mathf.CeilToInt(food.m_stamina).ToString();
            }

            if (HealingText != null)
            {
                HealingText.text = food.m_item.m_shared.m_foodRegen.ToString("0.#");
            }
        }
    }
}