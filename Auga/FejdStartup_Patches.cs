using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public static class FejdStartup_Start_Patche
    {
        public static void Postfix(FejdStartup __instance)
        {
            __instance.HideElementByType<ChangeLog>();
            __instance.m_versionLabel.HideElement();
            __instance.m_mainMenu.HideElementByName("showlog");
            __instance.m_mainMenu.HideElementByName("Embers");
            __instance.m_mainMenu.HideElementByName("Embers (1)");
            __instance.m_mainMenu.HideElementByName("Embers (2)");
            __instance.m_mainMenu.HideElementByName("Embers (3)");
            __instance.m_mainMenu.HideElementByName("LOGO");

            Object.Instantiate(Auga.Assets.AugaLogo, __instance.m_mainMenu.transform);
        }
    }
}
