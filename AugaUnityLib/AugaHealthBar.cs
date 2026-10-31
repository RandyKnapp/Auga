using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [ExecuteInEditMode]
    public class AugaHealthBar : MonoBehaviour
    {
        public enum ModeType { Health, Stamina, Eitr };
        public enum TextPosition { Off = -1, Above, Below, Center, Start, End };
        public enum TextDisplayMode { JustValue, ValueAndMax, ValueMaxPercent, JustPercent }

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
        public bool Hide = false;
        public float FixedLength = 0;
        public TextDisplayMode TextDisplay = TextDisplayMode.JustValue;
        public bool ShowTicks = true;
        [Header("Above, Below, Center, Start, End")]
        public TMP_Text[] CurrentValueText = { null, null, null, null, null };

        [Range(0, 270)]
        public float CurrentValue;
        [Range(1, 270)]
        public float MaxValue;
        [Range(1, 270)]
        public float MaxPotentialValue;

        private readonly List<RectTransform> _ticks = new List<RectTransform>();

        // the label is built again only when what it shows changes
        private string _valueDisplay;
        private TextDisplayMode _shownMode;
        private int _shownValue = -1;
        private int _shownMax = -1;
        private int _shownPercent = -1;

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

            var barIsVisible = !Hide && MaxValue > 0;
            Background.gameObject.SetActive(barIsVisible);
            BackgroundPotential.gameObject.SetActive(barIsVisible);
            TickContainer.gameObject.SetActive(ShowTicks && barIsVisible);

            var currentUsablePercentage = MaxValue / MaxPotentialValue;
            var backgroundWidth = FixedLength > 0 ? (MinBackgroundSize + (FixedLength * currentUsablePercentage)) : (MinBackgroundSize + (MaxValue * PixelsPerUnit));
            Background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, backgroundWidth);
            BackgroundPotential.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, FixedLength > 0 ? (MinBackgroundSize + FixedLength) : (MinBackgroundSize + (MaxPotentialValue * PixelsPerUnit)));

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

            var currentValueDisplay = GetValueDisplay(Mathf.CeilToInt(CurrentValue), Mathf.CeilToInt(MaxValue), Mathf.CeilToInt(actualPercent * 100));

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

        private string GetValueDisplay(int value, int max, int percent)
        {
            if (_valueDisplay != null && TextDisplay == _shownMode && value == _shownValue && max == _shownMax && percent == _shownPercent)
                return _valueDisplay;

            _shownMode = TextDisplay;
            _shownValue = value;
            _shownMax = max;
            _shownPercent = percent;
            switch (TextDisplay)
            {
                case TextDisplayMode.ValueAndMax:
                    _valueDisplay = $"{value}  /  {max}";
                    break;

                case TextDisplayMode.ValueMaxPercent:
                    _valueDisplay = $"{value}  /  {max}     {percent}%";
                    break;

                case TextDisplayMode.JustPercent:
                    _valueDisplay = $"{percent}%";
                    break;

                default:
                    _valueDisplay = value.ToString();
                    break;
            }
            return _valueDisplay;
        }

        private void SetupTicks()
        {
            if (!ShowTicks)
                return;

            var modifiedPixelsPerUnit = FixedLength > 0 ? ((MinBackgroundSize + FixedLength) / MaxPotentialValue ) : PixelsPerUnit;

            var tickCount = Mathf.CeilToInt(MaxPotentialValue / UnitsPerTick) - 1;
            while (_ticks.Count < tickCount)
            {
                var newTick = Instantiate(TickPrefab, TickContainer);
                var image = newTick.GetComponent<Image>();
                var c = image.color;
                c.a = _ticks.Count == 0 ? FirstTickAlpha : OtherTickAlpha;
                image.color = c;
                _ticks.Add(newTick);
            }

            for (var index = 0; index < _ticks.Count; ++index)
            {
                var tick = _ticks[index];
                var shown = index < tickCount;
                tick.gameObject.SetActive(shown);

                // A fixed-length bar spreads the ticks over the maximum the player could reach, which changes with
                // every food eaten or worn off, so every tick is placed again rather than only when it is made.
                if (shown)
                {
                    var position = new Vector2(UnitsPerTick * (index + 1) * modifiedPixelsPerUnit, 0);
                    if (tick.anchoredPosition != position)
                        tick.anchoredPosition = position;
                }
            }
        }

        // the maximum with every food eaten at full strength: what the foods give now is swapped for what they give fresh
        public virtual float GetMaxPotentialHealth(Player player)
        {
            var potential = player.GetMaxHealth();
            foreach (var food in player.m_foods)
                potential += food.m_item.m_shared.m_food - food.m_health;
            return potential;
        }

        public virtual float GetMaxPotentialStamina(Player player)
        {
            var potential = player.GetMaxStamina();
            foreach (var food in player.m_foods)
                potential += food.m_item.m_shared.m_foodStamina - food.m_stamina;
            return potential;
        }

        public virtual float GetMaxPotentialEitr(Player player)
        {
            var potential = player.GetMaxEitr();
            foreach (var food in player.m_foods)
                potential += food.m_item.m_shared.m_foodEitr - food.m_eitr;
            return potential;
        }
    }
}
