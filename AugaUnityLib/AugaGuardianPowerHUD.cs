using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaGuardianPowerHUD : MonoBehaviour
    {
        public GameObject Root;
        public Image Icon;
        public Text Name;
        public Text Cooldown;

        public void OnEnable()
        {
            if (Hud.instance != null)
            {
                Icon.material = Hud.instance.m_gpIcon.material;
            }
        }

        public void Update()
        {
            var player = Player.m_localPlayer;
            if (!player)
                return;

            player.GetGuardianPowerHUD(out var se, out var cooldown);
            if (se != null)
            {
                Root.gameObject.SetActive(true);
                Icon.sprite = se.m_icon;
                Icon.color = cooldown <= 0.0 ? Color.white : new Color(1f, 0.0f, 1f, 0.0f);
                Name.text = Localization.instance.Localize(se.m_name);
                Cooldown.text = cooldown > 0.0 ? StatusEffect.GetTimeString(cooldown) : Localization.instance.Localize("$hud_ready");
            }
            else
            {
                Root.gameObject.SetActive(false);
            }
        }
    }
}
