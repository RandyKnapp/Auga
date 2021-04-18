using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class PlayerPanelEffectController : MonoBehaviour
    {
        public int Index = -1;
        public Image Icon;
        public Image CountdownBG;
        public Image CountdownImage;
        public Text NameText;
        public Text InfoText;

        private readonly List<StatusEffect> _playerStatusEffects = new List<StatusEffect>();

        public void Start()
        {
            Update();
        }

        public void Update()
        {
            if (Index < 0)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            _playerStatusEffects.Clear();
            player.GetSEMan().GetHUDStatusEffects(_playerStatusEffects);
            if (Index < _playerStatusEffects.Count)
            {
                var statusEffect = _playerStatusEffects[Index];
                Icon.sprite = statusEffect.m_icon;
                NameText.text = Localization.instance.Localize(statusEffect.m_name);
                InfoText.text = Localization.instance.Localize(statusEffect.GetIconText());

                var hasTimer = statusEffect.m_ttl > 0;
                CountdownBG.enabled = hasTimer;
                CountdownImage.enabled = hasTimer;
                if (hasTimer)
                {
                    var percent = 1 - (statusEffect.m_time / statusEffect.m_ttl);
                    CountdownImage.fillAmount = percent;
                }

                // TODO: "new effect" flash?
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                Update();
            }
        }
    }
}
