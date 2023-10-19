﻿using HarmonyLib;
using UnityEngine;

namespace Auga;

[HarmonyPatch]
public static class Barber_Setup
{
    [HarmonyPatch(typeof(PlayerCustomizaton), nameof(PlayerCustomizaton.OnEnable))]
    public static class PlayerCustomization_Awake_Patch
    {
        public static bool Prefix(PlayerCustomizaton __instance)
        {
            return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.AugaBarber, "BarberGui");
        }
    }
}