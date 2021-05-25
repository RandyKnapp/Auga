using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class PauseMenu_Setup
    {
        [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
        public static class Menu_Start_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (__instance.name != "Menu")
                {
                    return;
                }

                var parent = __instance.transform.parent;
                Object.Instantiate(Auga.Assets.MenuPrefab, parent, false).GetComponent<Menu>();
                Object.Destroy(__instance.gameObject);
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.OnClose))]
        public static class Menu_OnClose_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                var compendium = __instance.GetComponent<AugaCompendiumController>();
                if (compendium != null)
                {
                    compendium.HideCompendium();
                }
            }
        }
    }
}

