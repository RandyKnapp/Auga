using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [ExecuteInEditMode]
    public class AugaHealthBar : MonoBehaviour
    {
        public enum ModeType { Health, Stamina, Eitr };
        public enum TextPosition { Off = -1, Above, Below, Center, Start, End };

        public ModeType Mode;
        public TextPosition DisplayTextPosition = TextPosition.Above;
        public float PixelsPerUnit = 500.0f / 270.0f;
        public float MinBackgroundSize;
        public float MinBarSize;
        public RectTransform Background;
        public RectTransform Border;
        public RectTransform BackgroundPotential;
        public GuiBar FastBar;
        public GuiBar SlowBar;
        public RectTransform TickContainer;
        public RectTransform TickPrefab;
        public float UnitsPerTick = 25;
        public float FirstTickAlpha = 0.6666f;
        public float OtherTickAlpha = 0.4f;
        [Header("Above, Below, Center, Start, End")]
        public Text[] CurrentValueText = { null, null, null, null, null };

        [Range(0, 270)]
        public float CurrentValue;
        [Range(1, 270)]
        public float MaxValue;
        [Range(1, 270)]
        public float MaxPotentialValue;

        private readonly List<RectTransform> _ticks = new List<RectTransform>();

        public virtual void Start()
        {
            TickPrefab.gameObject.SetActive(false);
            LateUpdate();
        }

        public virtual void LateUpdate()
        {
            if (!Application.isEditor)
            {
                var player = Player.m_localPlayer;
                if (player != null)
                {
                    switch (Mode)
                    {
                        case ModeType.Health:
                            CurrentValue = player.GetHealth();
                            MaxValue = player.GetMaxHealth();
                            MaxPotentialValue = GetMaxPotentialHealth(player);
                            break;

                        case ModeType.Stamina:
                            CurrentValue = player.GetStamina();
                            MaxValue = player.GetMaxStamina();
                            MaxPotentialValue = GetMaxPotentialStamina(player);
                            break;

                        case ModeType.Eitr:
                            CurrentValue = player.GetEitr();
                            MaxValue = player.GetMaxEitr();
                            MaxPotentialValue = GetMaxPotentialEitr(player);
                            break;
                    }
                }
            }

            var barIsVisible = MaxValue > 0;
            Background.gameObject.SetActive(barIsVisible);
            BackgroundPotential.gameObject.SetActive(barIsVisible);
            TickContainer.gameObject.SetActive(barIsVisible);

            var backgroundWidth = MinBackgroundSize + (MaxValue * PixelsPerUnit);
            Background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, backgroundWidth);
            BackgroundPotential.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinBackgroundSize + (MaxPotentialValue * PixelsPerUnit));

            var minPercent = MinBarSize / backgroundWidth;
            var actualPercent = CurrentValue / MaxValue;
            var offsetPercent = Mathf.Lerp(minPercent, 1, actualPercent);
            var fastValue = MaxValue * offsetPercent;
            var slowValue = (MaxValue * offsetPercent) - 0.5f;

            FastBar.SetWidth(backgroundWidth);
            FastBar.SetMaxValue(MaxValue);
            FastBar.SetValue(fastValue);

            SlowBar.SetWidth(backgroundWidth);
            SlowBar.SetMaxValue(MaxValue);
            SlowBar.SetValue(slowValue);

            var currentValueDisplay = Mathf.CeilToInt(CurrentValue).ToString();
            for (var index = 0; index < CurrentValueText.Length; index++)
            {
                var textDisplay = CurrentValueText[index];
                if (!textDisplay)
                    continue;

                textDisplay.gameObject.SetActive(barIsVisible && DisplayTextPosition == (TextPosition)index);
                textDisplay.text = currentValueDisplay;
            }

            if (Application.isEditor)
            {
                FastBar.SetBar(fastValue / MaxValue);
                SlowBar.SetBar(slowValue / MaxValue);
            }

            if (!Application.isEditor)
            {
                SetupTicks();
            }
        }

        private void SetupTicks()
        {
            var tickCount = Mathf.CeilToInt(MaxPotentialValue / UnitsPerTick) - 1;
            for (var index = 0; index < tickCount; ++index)
            {
                if (index >= _ticks.Count)
                {
                    var tick = Instantiate(TickPrefab, TickContainer);
                    tick.anchoredPosition = new Vector2(UnitsPerTick * (index + 1) * PixelsPerUnit, 0);
                    _ticks.Add(tick);
                }
            }

            for (var index = 0; index < _ticks.Count; ++index)
            {
                var tick = _ticks[index];
                var c = tick.GetComponent<Image>().color;
                c.a = index == 0 ? FirstTickAlpha : OtherTickAlpha;
                tick.GetComponent<Image>().color = c;
                tick.gameObject.SetActive(index < tickCount);
            }
        }

        public virtual float GetMaxPotentialHealth(Player player)
        {
            var baseHP = player.GetMaxHealth() - player.m_foods.Sum(x => x.m_health);
            return baseHP + player.m_foods.Sum(x => x.m_item.m_shared.m_food);
        }

        public virtual float GetMaxPotentialStamina(Player player)
        {
            var baseStamina = player.GetMaxStamina() - player.m_foods.Sum(x => x.m_stamina);
            return baseStamina + player.m_foods.Sum(x => x.m_item.m_shared.m_foodStamina);
        }

        public virtual float GetMaxPotentialEitr(Player player)
        {
            var baseEitr = player.GetMaxEitr() - player.m_foods.Sum(x => x.m_eitr);
            return baseEitr + player.m_foods.Sum(x => x.m_item.m_shared.m_foodEitr);
        }
    }
}
