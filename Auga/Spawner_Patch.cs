using System;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.Awake))]
    public static class Spawner_Patch
    {
        public static void Postfix(CreatureSpawner __instance)
        {
            if (__instance.m_creaturePrefab != null && __instance.m_creaturePrefab.name == "Neck")
            {
                Debug.LogError($"CreatureSpawner for Neck: {Environment.StackTrace}");
            }
        }
    }
}
