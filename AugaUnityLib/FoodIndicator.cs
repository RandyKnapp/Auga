using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var foods = player.GetFoods();
            Image.enabled = foods?.Exists(x => x.m_item.m_shared.m_name == _itemTooltip.Item.m_shared.m_name) ?? false;
        }
    }
}
