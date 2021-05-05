using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(ItemTooltip))]
    public class BetterDurabilityBar : MonoBehaviour
    {
        public Image Bar;

        private ItemTooltip _itemTooltip;

        public virtual void Awake()
        {
            _itemTooltip = GetComponent<ItemTooltip>();
        }

        public void LateUpdate()
        {
            var player = Player.m_localPlayer;
            if (player == null || _itemTooltip == null)
            {
                return;
            }

            if (_itemTooltip.Item == null || !_itemTooltip.Item.m_shared.m_useDurability)
            {
                return;
            }

            var percent = _itemTooltip.Item.GetDurabilityPercentage();
            var broken = percent <= 0.001f;
            Bar.fillAmount = broken ? 1 : percent;
            Bar.color = broken ? Mathf.Sin(Time.time * 10f) > 0.0f ? Color.red : new Color(0.0f, 0.0f, 0.0f, 0.0f) : (percent <= 0.2f ? Color.red : Color.white);
        }
    }
}
