using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class PlayerStatBlock : MonoBehaviour
    {
        public enum Stat { Health, Stamina, Eitr };

        public Color HighlightColor1;
        public Color HighlightColor2;

        public Stat PlayerStat;
        public Text PrimaryText;
        public Text SecondaryText;

        private string _highlightColor1;
        private string _highlightColor2;

        [UsedImplicitly]
        public virtual void Start()
        {
            _highlightColor1 = ColorUtility.ToHtmlStringRGB(HighlightColor1);
            _highlightColor2 = ColorUtility.ToHtmlStringRGB(HighlightColor2);
            Update();
        }

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var currentStat = GetCurrentStat();
            var maxStat = GetMaxStat();
            PrimaryText.text = $"<color={_highlightColor1}>{currentStat}</color> / {maxStat}";

            player.GetTotalFoodValue(out var hp, out var stamina, out var eitr);
            var statFoodDisplay = GetFoodDisplay(hp, stamina, eitr);

            SecondaryText.text = Localization.instance.Localize("$status_desc", _highlightColor2, GetStatBase().ToString("0"), _highlightColor2, statFoodDisplay.ToString());
        }

        protected virtual int GetCurrentStat()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return 0;

            switch (PlayerStat)
            {
                case Stat.Health:   return Mathf.CeilToInt(player.GetHealth());
                case Stat.Stamina:  return Mathf.CeilToInt(player.GetStamina());
                case Stat.Eitr:     return Mathf.CeilToInt(player.GetEitr());
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual int GetMaxStat()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return 0;

            switch (PlayerStat)
            {
                case Stat.Health: return Mathf.CeilToInt(player.GetMaxHealth());
                case Stat.Stamina: return Mathf.CeilToInt(player.GetMaxStamina());
                case Stat.Eitr: return Mathf.CeilToInt(player.GetMaxEitr());
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual int GetFoodDisplay(float hp, float stamina, float eitr)
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return 0;

            var baseValue = GetStatBase();
            switch (PlayerStat)
            {
                case Stat.Health: return Mathf.CeilToInt(hp - baseValue);
                case Stat.Stamina: return Mathf.CeilToInt(stamina - baseValue);
                case Stat.Eitr: return Mathf.CeilToInt(eitr - baseValue);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual float GetStatBase()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return 0;

            switch (PlayerStat)
            {
                case Stat.Health: return player.m_baseHP;
                case Stat.Stamina: return player.m_baseStamina;
                case Stat.Eitr: return 0;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
