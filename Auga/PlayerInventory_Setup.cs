using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Awake_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            __instance.m_playerGrid.m_onSelected = null;
            __instance.m_playerGrid.m_onRightClick = null;
            __instance.m_containerGrid.m_onSelected = null;
            __instance.m_containerGrid.m_onRightClick = null;

            var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);
            __instance.m_player = playerInventory.RectTransform();
            __instance.m_playerGrid = playerInventory.Find("PlayerGrid").GetComponent<InventoryGrid>();
            __instance.m_playerGrid.m_onSelected += __instance.OnSelectedItem;
            __instance.m_playerGrid.m_onRightClick += __instance.OnRightClickItem;
            __instance.m_weight = playerInventory.Find("Weight/Text").GetComponent<Text>();
            __instance.m_armor = playerInventory.Find("Armor/Text").GetComponent<Text>();

            var containerInventory = __instance.Replace("root/Container", Auga.Assets.InventoryScreen, "root/Container", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);
            __instance.m_container = containerInventory.RectTransform();
            __instance.m_containerName = containerInventory.Find("ContainerHeader/Name").GetComponent<Text>();
            __instance.m_containerGrid = containerInventory.Find("ContainerGrid").GetComponent<InventoryGrid>();
            __instance.m_containerGrid.m_onSelected += __instance.OnSelectedItem;
            __instance.m_containerGrid.m_onRightClick += __instance.OnRightClickItem;
            __instance.m_containerWeight = containerInventory.Find("Weight/Text").GetComponent<Text>();
            containerInventory.Find("TakeAll").GetComponent<Button>().onClick.AddListener(__instance.OnTakeAll);

            Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/RightPanel"), containerInventory.parent, false);

            Localization.instance.Localize(__instance.transform);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class InventoryGrid_UpdateGui_Patch
    {
        public static void Postfix(InventoryGrid __instance)
        {
            if (__instance.name == "PlayerGrid")
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

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    public static class InventoryGui_Show_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                __instance.UpdateContainer(player);
            }
        }
    }
}
