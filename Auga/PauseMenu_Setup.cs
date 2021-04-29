using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class PauseMenu_Setup
    {
        [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
        public static class Menu_Start_Patch
        {
            public static void Postfix(Menu __instance)
            {
                if (__instance.name != "Menu")
                {
                    return;
                }

                var settingsPrefab = __instance.m_settingsPrefab;
                var parent = __instance.transform.parent;

                var newMenu = Object.Instantiate(Auga.Assets.MenuPrefab, parent, false).GetComponent<Menu>();
                newMenu.m_settingsPrefab = settingsPrefab;

                Object.Destroy(__instance.gameObject);
            }
        }
    }
}
