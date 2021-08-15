using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaTrasher : MonoBehaviour
    {
        public Button Button;
        public ZSFX SFX;

        public void Awake()
        {
            Button.onClick.AddListener(OnClick);
        }

        public void OnDestroy()
        {
            Button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (InventoryGui.instance.m_dragItem != null)
            {
                var dragItem = InventoryGui.instance.m_dragItem;
                var dragAmount = InventoryGui.instance.m_dragAmount;
                var dragInventory = InventoryGui.instance.m_dragInventory;

                if (dragAmount == dragItem.m_stack)
                {
                    Player.m_localPlayer.RemoveFromEquipQueue(dragItem);
                    Player.m_localPlayer.UnequipItem(dragItem, false);
                    dragInventory.RemoveItem(dragItem);
                }
                else
                {
                    dragInventory.RemoveItem(dragItem, dragAmount);
                }

                Instantiate(SFX);

                InventoryGui.instance.SetupDragItem(null, null, 0);
                InventoryGui.instance.UpdateCraftingPanel();
            }
        }
    }
}
