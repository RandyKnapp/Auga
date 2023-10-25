using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Auga.Utilities;

public class TranspilerHelpers
{
    private static int _counter = 0;
    private static bool _enableLogging = false;

    public TranspilerHelpers(bool loggingEnabled = false)
    {
        _enableLogging = loggingEnabled;
    }
    public CodeInstruction LogMessage(CodeInstruction instruction)
    {
        if (!_enableLogging)
            return instruction;
        
        Debug.Log($"VAPOK: IL_{_counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
        _counter++;
        return instruction;
    }
    public CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
    {
        if (index >= codeInstructions.Count)
            return null;
                
        if (codeInstructions[index].labels.Contains(label))
            return codeInstructions[index];
                
        return FindInstructionWithLabel(codeInstructions, index + 1, label);
    }

}