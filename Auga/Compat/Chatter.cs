using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga.Compat;

public static class Chatter
{
    public static Assembly ChatterType;
    public static Type ToggleCell;


    public static bool CreateChildCell_Patch(Transform parentTransform, ref GameObject __result)
    {
        var buttonToggle = Object.Instantiate(Auga.Assets.ButtonToggle, parentTransform, false);
        __result = buttonToggle;
        return false;
    }

    public static TextMeshProUGUI CreateChildLabel_Patch(GameObject cell)
    {
        return cell.GetComponentInChildren<TextMeshProUGUI>();;
    }

    public static IEnumerable<CodeInstruction> CreateChildLabel_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instrs = instructions.ToList();

        var cell = AccessTools.PropertyGetter(ToggleCell, "Cell");

        var counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }
        
        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,cell));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Chatter), nameof(CreateChildLabel_Patch))));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
        counter++;
    }

    
    public static void OnToggleValueChanged_Patch(bool isOn, GameObject cell, Image background, TextMeshProUGUI label)
    {
        var colors = cell.GetComponent<ColorButtonTextValues>();
        background.color = isOn ? colors.TextColors.normalColor : colors.TextColors.disabledColor;
        label.color = isOn ? colors.TextColors.normalColor : colors.TextColors.disabledColor;
    }
    
    public static IEnumerable<CodeInstruction> OnToggleValueChanged_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instrs = instructions.ToList();

        var cell = AccessTools.PropertyGetter(ToggleCell, "Cell");
        var background = AccessTools.PropertyGetter(ToggleCell, "Background");
        var label = AccessTools.PropertyGetter(ToggleCell, "Label");
        

        var counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }
        
        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,cell));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,background));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,label));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Chatter), nameof(OnToggleValueChanged_Patch))));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
        counter++;
    }
}