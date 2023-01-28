using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public class Store_Setup
    {
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyPrefix]
        public static bool Awake_Prefix(StoreGui __instance)
        {
            if (Auga.HasBetterTrader)
            {
                return true;
            }

            var replaced = SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.StoreGui, "Store_Screen", out var newStoreGui);
            if (replaced)
            {
                newStoreGui.GetComponent<StoreGui>().m_coinPrefab = ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>();
                newStoreGui.transform.Find("Store").gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 140, -180);
            }
            return !replaced;
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList))]
        [HarmonyPostfix]
        public static void FillList_Postfix(StoreGui __instance)
        {
            if (Auga.HasBetterTrader)
            {
                return;
            }

            var items = __instance.m_trader.GetAvailableItems();
            for (var index = 0; index < __instance.m_itemList.Count; index++)
            {
                var item = items[index];
                var itemElement = __instance.m_itemList[index];
                itemElement.GetComponent<ItemTooltip>().Item = item.m_prefab.m_itemData;
            }
        }
    }
}
