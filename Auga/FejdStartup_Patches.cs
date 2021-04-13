using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public static class FejdStartup_Start_Patche
    {
        public static void Postfix(FejdStartup __instance)
        {
            HideElementByType<ChangeLog>(__instance);
            HideElement(__instance.m_versionLabel);
            HideElementByName(__instance.m_mainMenu, "showlog");
            HideElementByName(__instance.m_mainMenu, "Embers");
            HideElementByName(__instance.m_mainMenu, "Embers (1)");
            HideElementByName(__instance.m_mainMenu, "Embers (2)");
            HideElementByName(__instance.m_mainMenu, "Embers (3)");
            HideElementByName(__instance.m_mainMenu, "LOGO");

            Object.Instantiate(Auga.Assets.AugaLogo, __instance.m_mainMenu.transform);
        }

        private static void HideElement(Component c)
        {
            if (c != null)
            {
                c.gameObject.SetActive(false);
            }
        }

        private static void HideElementByType<T>(Component parent) where T : Component
        {
            var c = parent.GetComponentInChildren<T>();
            HideElement(c);
        }

        private static void HideElementByName(GameObject parent, string name)
        {
            HideElementByName(parent.transform, name);
        }

        private static void HideElementByName(Component parent, string name)
        {
            var c = parent.transform.Find(name);
            HideElement(c);
        }
    }
}
