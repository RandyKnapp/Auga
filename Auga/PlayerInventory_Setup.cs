using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Awake_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);
            __instance.m_player = playerInventory.RectTransform();
            __instance.m_playerGrid = playerInventory.Find("PlayerGrid").GetComponent<InventoryGrid>();
            __instance.m_playerGrid.m_onSelected += __instance.OnSelectedItem;
            __instance.m_playerGrid.m_onRightClick += __instance.OnRightClickItem;
            __instance.m_weight = playerInventory.Find("Weight/Text").GetComponent<Text>();
            __instance.m_armor = playerInventory.Find("Armor/Text").GetComponent<Text>();
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class InventoryGrid_UpdateGui_Patch
    {
        public static void Postfix(InventoryGrid __instance)
        {
            Vector2 startPos = new Vector2(__instance.RectTransform().rect.width / 2f, 0.0f) - new Vector2(__instance.GetWidgetSize().x, 0.0f) * 0.5f;
            foreach (var element in __instance.m_elements)
            {
                if (element.m_pos.y != 0)
                {
                    Vector2 currentPosition = new Vector3(element.m_pos.x * (__instance.m_elementSpace), (element.m_pos.y * -__instance.m_elementSpace) - 18);
                    element.m_go.RectTransform().anchoredPosition = startPos + currentPosition;
                }
            }
        }
    }
}
