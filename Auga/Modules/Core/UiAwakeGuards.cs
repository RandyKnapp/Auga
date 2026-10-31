using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    /// <summary>
    /// When a patch from another mod throws inside a UI screen's Awake or Start, Unity disables that component and
    /// the screen never updates again: an inventory that opens without slots, key hints that stop following the
    /// bindings. The exception is logged with the offender's stack and swallowed, so the screen keeps running with
    /// whatever that mod managed to set up before it failed. The log line bypasses Auga's logging settings: with the
    /// exception swallowed it is the only trace of the failure, and it also explains why every postfix of that
    /// method ordered after the failing one did not run.
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
            (typeof(EnemyHud), "Awake"),
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
        public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, object __instance)
        {
            if (__exception == null)
                return null;
            // A root that Auga swaps in an Awake prefix (EnemyHud) is destroyed before the postfixes run, and a postfix
            // that reads it throws. The replacement runs its own Awake, so there is nothing to report.
            if (__instance is UnityEngine.Object vanilla && vanilla == null)
                return null;
            Debug.LogWarning($"[Auga] {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} failed in a patch from another mod; the screen keeps running without it.\n{__exception}");
            return null;
        }
    }
}
