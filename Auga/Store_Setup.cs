using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    public class StoreMethods
    {
        public StoreGui SetupAugaStoreGui(StoreGui instance)
        {
            if (instance.name.StartsWith("Auga")) return instance;
            
            try
            {
                var originalTransform = instance.transform;
                var parent = originalTransform.parent;
                var siblingIndex = parent.GetSiblingIndex();
                var newStoreGui = GetAugaStoreGui(parent);
                newStoreGui.transform.SetAsLastSibling();

                if (instance.transform.name.Equals("Store_Screen") && instance.m_rootPanel.name.Equals("Store"))
                {
                    originalTransform.gameObject.SetActive(false);
                    instance = newStoreGui;
                    return instance;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error In Store: {e.Message}");
            }

            return instance;
        }

        private StoreGui GetAugaStoreGui(Transform parent)
        {
            var newStore = Object.Instantiate(Auga.Assets.StoreGui, parent, false);
            var newStoreGui = newStore.GetComponent<StoreGui>();
            
            newStoreGui.m_coinPrefab = ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>();
            newStoreGui.transform.Find("Store").gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 140, -180);

            return newStoreGui;
        }
        
    }
    
    [HarmonyPatch]
    public class Store_Setup
    {

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Awake_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var instrs = instructions.ToList();

            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }

            for (int i = 0; i < instrs.Count; ++i)
            {
                if (i == 0)
                {
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;
                    
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;
                    
                    yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(StoreMethods), nameof(StoreMethods.SetupAugaStoreGui))));
                    counter++;
                    
                    //skip next this;
                    i++;

                }
                
                yield return LogMessage(instrs[i]);
                counter++;
            }
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
