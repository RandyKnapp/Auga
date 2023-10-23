using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Auga.Compat;

public static class Jewelcrafting
{
    public static Assembly ModAssembly;
    public static Type Synergy;
    public static Type DisplaySynergyView;
    public static Type AddSynergyIcon;

    [HarmonyAfter(new []{"org.bepinex.plugins.jewelcrafting"})]
    public static void IvnentoryGui_Awake_Postfix(InventoryGui __instance)
    {
        var jewelCraftingSynergy = __instance.m_player.transform.Find("Jewelcrafting Synergy")?.gameObject;
        var TrashDividerLine = __instance.m_player.transform.Find("TrashDivider/DividerLeft/Line")?.gameObject;
        if (jewelCraftingSynergy != null)
        {
            var rect = jewelCraftingSynergy.RectTransform();
            Debug.LogError($"jewelCraftingSynergy old position: {rect.anchoredPosition}");
            rect.anchoredPosition += new Vector2(80f, +78f);
            Debug.LogError($"jewelCraftingSynergy new position: {rect.anchoredPosition}");
        }
        else
        {
            Debug.LogError($"jewelCraftingSynergy is null");
            Debug.LogError($"jewelCraftingSynergy is null");
            Debug.LogError($"jewelCraftingSynergy is null");
            Thread.Sleep(15000);
        }
    }
    
    public static IEnumerable<CodeInstruction> DisplaySynergyView_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instrs = instructions.ToList();

        var counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //Debug.LogWarning($"VAPOK: IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }
            
        CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
        {
            if (index >= codeInstructions.Count)
                return null;
                
            if (codeInstructions[index].labels.Contains(label))
                return codeInstructions[index];
                
            return FindInstructionWithLabel(codeInstructions, index + 1, label);
        }


        for (int i = 0; i < instrs.Count; ++i)
        {
            if (i > 0 && instrs[i].opcode == OpCodes.Ldstr && instrs[i].operand.Equals("ac_text"))
            {
                yield return LogMessage(new CodeInstruction(OpCodes.Ldstr,"Text"));
                counter++;
            }
            else
            {
                yield return LogMessage(instrs[i]);
                counter++;
            }
        }
    }

}