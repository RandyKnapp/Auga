using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Auga.Utilities;

public static class TranspilerHelpers
{
    public static CodeInstruction LogMessage(CodeInstruction instruction)
    {
        //Debug.LogWarning($"VAPOK: IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
        return instruction;
    }
    public static CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
    {
        if (index >= codeInstructions.Count)
            return null;
                
        if (codeInstructions[index].labels.Contains(label))
            return codeInstructions[index];
                
        return FindInstructionWithLabel(codeInstructions, index + 1, label);
    }

}