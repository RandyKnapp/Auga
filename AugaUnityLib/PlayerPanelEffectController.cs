using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
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
        [CanBeNull] public TMP_Text NameText;
        public TMP_Text InfoText;
        /// <summary>
        /// The time / value line of the two-line layout (NameText above it). When both are set, an effect without a
        /// time or value shows its name on the single InfoText line instead, and the two lines are hidden.
        /// </summary>
        [CanBeNull] public TMP_Text TimeText;

        protected readonly List<StatusEffect> _playerStatusEffects = new List<StatusEffect>();
        protected StatusTooltip _statusTooltip;

        public virtual void Awake()
        {
            _statusTooltip = GetComponent<StatusTooltip>();
            Update();
        }

        public virtual void Update()
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
                UpdateStatusEffect(statusEffect);
            }
            else
            {
                if (_statusTooltip != null)
                {
                    _statusTooltip.StatusEffect = null;
                }
            }
        }

        public virtual void UpdateStatusEffect(StatusEffect statusEffect)
        {
            if (_statusTooltip != null)
            {
                _statusTooltip.StatusEffect = statusEffect;
            }

            Icon.sprite = statusEffect.m_icon;
            var name = Localization.instance.Localize(statusEffect.m_name);
            var info = Localization.instance.Localize(statusEffect.GetIconText());
            if (NameText != null && TimeText != null)
            {
                var hasInfo = !string.IsNullOrEmpty(info);
                NameText.gameObject.SetActive(hasInfo);
                TimeText.gameObject.SetActive(hasInfo);
                InfoText.gameObject.SetActive(!hasInfo);
                NameText.text = name;
                TimeText.text = info;
                InfoText.text = name;
            }
            else
            {
                if (NameText != null)
                {
                    NameText.text = name;
                }
                InfoText.text = info;
            }

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

        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                Update();
            }
        }
    }
}
