using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Auga
{
    /// <summary>
    /// When a patch from another mod throws inside a UI screen's Awake or Start, Unity disables that component and
    /// the screen never updates again: an inventory that opens without slots, key hints that stop following the
    /// bindings. The exception is logged with the offender's stack and swallowed, so the screen keeps running with
    /// whatever that mod managed to set up before it failed.
    /// </summary>
    [HarmonyPatch]
    public static class UiAwakeGuards
    {
        private static readonly (Type type, string method)[] Guarded =
        {
            (typeof(InventoryGui), "Awake"),
            (typeof(Hud), "Awake"),
            (typeof(KeyHints), "Start"),
            (typeof(Minimap), "Awake"),
            (typeof(Minimap), "Start"),
            (typeof(TextsDialog), "Awake"),
            (typeof(StoreGui), "Awake"),
            (typeof(Menu), "Start"),
        };

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var (type, name) in Guarded)
            {
                var method = AccessTools.Method(type, name);
                if (method != null)
                    yield return method;
            }
        }

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, MethodBase __originalMethod)
        {
            if (__exception == null)
                return null;
            Auga.LogError($"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} failed in a patch from another mod; the screen keeps running without it.\n{__exception}");
            return null;
        }
    }
}
