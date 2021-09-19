using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class PlayerPanelController : MonoBehaviour
    {
        public Color HighlightColor1 = Color.white;
        public Color HighlightColor2 = Color.white;

        public Text HealthText;
        public Text HealthTextSecondary;
        public Text StaminaText;
        public Text StaminaTextSecondary;

        protected string _highlightColor1;
        protected string _highlightColor2;

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

            var healthDisplay = Mathf.CeilToInt(player.GetHealth());
            var staminaDisplay = Mathf.CeilToInt(player.GetStamina());
            HealthText.text = $"<color={_highlightColor1}>{healthDisplay}</color> / {Mathf.CeilToInt(player.GetMaxHealth())}";
            StaminaText.text = $"<color={_highlightColor1}>{staminaDisplay}</color> / {Mathf.CeilToInt(player.GetMaxStamina())}";

            player.GetTotalFoodValue(out var hp, out var stamina);
            var healthFoodDisplay = Mathf.CeilToInt(hp - player.m_baseHP);
            var staminaFoodDisplay = Mathf.CeilToInt(stamina - player.m_baseStamina);
            HealthTextSecondary.text = $"Base <color={_highlightColor2}>{player.m_baseHP}</color> + Food <color={_highlightColor2}>{healthFoodDisplay}</color>";
            StaminaTextSecondary.text = $"Base <color={_highlightColor2}>{player.m_baseStamina}</color> + Food <color={_highlightColor2}>{staminaFoodDisplay}</color>";
        }
    }
}
