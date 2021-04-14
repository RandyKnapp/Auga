using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Awake_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            Auga.Log($"__instance: ({__instance})");
            var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player", ReplaceFlags.DestroyOriginal);
            __instance.m_player = playerInventory as RectTransform;
        }
    }
}
