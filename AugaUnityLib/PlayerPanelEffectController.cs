using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    // One status effect icon of an AugaStatusEffects list, which hands it its effect every frame.
    public class PlayerPanelEffectController : MonoBehaviour
    {
        // its place in the list
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

        protected StatusTooltip _statusTooltip;

        // the last texts given and what they localized to, so that unchanged texts are not localized every frame
        private string _rawName;
        private string _name;
        private string _rawInfo;
        private string _info;

        public virtual void Awake()
        {
            _statusTooltip = GetComponent<StatusTooltip>();
            Localization.OnLanguageChange += ForgetTexts;
        }

        public virtual void OnDestroy()
        {
            Localization.OnLanguageChange -= ForgetTexts;
        }

        private void ForgetTexts()
        {
            _name = null;
            _info = null;
        }

        /// <summary>Shows the effect, or hides the icon for null.</summary>
        public virtual void Show([CanBeNull] StatusEffect statusEffect)
        {
            var shown = statusEffect != null;
            if (gameObject.activeSelf != shown)
            {
                gameObject.SetActive(shown);
            }

            if (shown)
            {
                UpdateStatusEffect(statusEffect);
            }
            else if (_statusTooltip != null)
            {
                _statusTooltip.StatusEffect = null;
            }
        }

        public virtual void UpdateStatusEffect(StatusEffect statusEffect)
        {
            if (_statusTooltip != null)
            {
                _statusTooltip.StatusEffect = statusEffect;
            }

            Icon.sprite = statusEffect.m_icon;
            var name = Localize(statusEffect.m_name, ref _rawName, ref _name);
            var info = Localize(statusEffect.GetIconText(), ref _rawInfo, ref _info);
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

        private static string Localize(string text, ref string lastText, ref string localized)
        {
            if (localized == null || text != lastText)
            {
                lastText = text;
                localized = Localization.instance.Localize(text);
            }
            return localized;
        }
    }
}
