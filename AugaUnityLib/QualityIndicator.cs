using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(ItemTooltip))]
    public class QualityIndicator : MonoBehaviour
    {
        public Color PipOnColor = Color.white;
        public Color PipOffColor = Color.white;

        public Image BackgroundImage;
        public List<Image> Pips;

        private ItemTooltip _itemTooltip;

        public virtual void Awake()
        {
            _itemTooltip = GetComponent<ItemTooltip>();
            SetEnabled(false);
        }

        public virtual void LateUpdate()
        {
            var player = Player.m_localPlayer;
            if (player == null || _itemTooltip == null)
            {
                return;
            }

            if (_itemTooltip.Item == null || _itemTooltip.Item.m_shared.m_maxQuality <= 1)
            {
                SetEnabled(false);
                return;
            }

            SetEnabled(true);

            for (var index = 0; index < Pips.Count; index++)
            {
                var pip = Pips[index];
                var qualityIndex = _itemTooltip.Item.m_quality - 1;
                pip.color = index <= qualityIndex ? PipOnColor : PipOffColor;
            }
        }

        public virtual void SetEnabled(bool e)
        {
            BackgroundImage.enabled = e;
            foreach (var pip in Pips)
            {
                pip.enabled = e;
            }
        }
    }
}
