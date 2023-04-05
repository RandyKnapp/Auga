using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public class Store_Setup
    {
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyPrefix]
        public static bool Awake_Pretfix(StoreGui __instance)
        {
            if(__instance.gameObject.name.StartsWith("Auga"))return false;
            if (!Auga.Assets.StoreGui) return true;
            if (Auga.HasBetterTrader)
            {
                return true;
            }

            Auga.Assets.StoreGui = (GameObject)Object.Instantiate(Auga.Assets.StoreGui,
                __instance.gameObject.transform.parent, false);
            return true;
        }
        
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix(StoreGui __instance)
        {
            if (__instance.gameObject.name.StartsWith("Auga")) return;
            StoreGui.m_instance = Auga.Assets.StoreGui.GetComponent<StoreGui>();
            __instance.m_itemlistBaseSize = Auga.Assets.StoreGui.GetComponent<StoreGui>().m_listRoot.rect.height;
            Auga.Assets.StoreGui.GetComponent<StoreGui>().m_coinPrefab = ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>();
            Auga.Assets.StoreGui.transform.Find("Store").gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 140, -180);
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