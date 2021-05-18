using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(ItemTooltip))]
    public class CooldownController : MonoBehaviour
    {
        public Image CooldownImage;

        private ItemTooltip _itemTooltip;

        public void Awake()
        {
            _itemTooltip = GetComponent<ItemTooltip>();
        }

        public void Update()
        {
            var player = Player.m_localPlayer;
            var item = _itemTooltip.Item;
            if (player != null && item != null
                && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
                && item.m_shared.m_consumeStatusEffect != null
                && !string.IsNullOrEmpty(item.m_shared.m_consumeStatusEffect.m_category)
                && GetStatusEffectWithCategory(player, item.m_shared.m_consumeStatusEffect.m_category, out var statusEffect))
            {
                CooldownImage.enabled = true;
                var percentRemaining = 1 - statusEffect.m_time / statusEffect.m_ttl;
                CooldownImage.fillAmount = percentRemaining;
            }
            else
            {
                CooldownImage.enabled = false;
            }
        }

        public bool GetStatusEffectWithCategory(Player player, string category, out StatusEffect statusEffect)
        {
            statusEffect = null;
            var statusEffects = player.GetSEMan().GetStatusEffects();
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            statusEffect = statusEffects.Find(x => x.m_category == category);
            return statusEffect != null;
        }
    }
}
