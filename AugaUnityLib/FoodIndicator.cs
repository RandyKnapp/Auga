using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(ItemTooltip))]
    public class FoodIndicator : MonoBehaviour
    {
        public Image Image;

        private ItemTooltip _itemTooltip;

        public virtual void Awake()
        {
            _itemTooltip = GetComponent<ItemTooltip>();
            Image.enabled = false;
        }

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null || _itemTooltip == null)
            {
                return;
            }

            if (_itemTooltip.Item == null || _itemTooltip.Item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            {
                Image.enabled = false;
                return;
            }

            Image.enabled = IsEaten(player, _itemTooltip.Item.m_shared.m_name);
        }

        // a loop rather than a lambda, which would be allocated anew for every slot every frame
        private static bool IsEaten(Player player, string name)
        {
            var foods = player.GetFoods();
            if (foods == null)
                return false;
            foreach (var food in foods)
            {
                if (food.m_item.m_shared.m_name == name)
                    return true;
            }
            return false;
        }
    }
}
