using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public static class FejdStartup_Start_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var originalChangeLogAsset = __instance.GetComponentInChildren<ChangeLog>().m_changeLog;

            /*var callOriginal = !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.MainMenuPrefab, "TextInput", out var newMainMenu);

            newMainMenu.GetComponentInChildren<ChangeLog>().m_changeLog = originalChangeLogAsset;
            Localization.instance.Localize(newMainMenu.transform);

            return callOriginal;*/

            __instance.HideElementByType<ChangeLog>();
            __instance.m_versionLabel.HideElement();
            __instance.m_mainMenu.HideElementByName("showlog");
            __instance.m_mainMenu.HideElementByName("Embers");
            __instance.m_mainMenu.HideElementByName("Embers (1)");
            __instance.m_mainMenu.HideElementByName("Embers (2)");
            __instance.m_mainMenu.HideElementByName("Embers (3)");
            __instance.m_mainMenu.HideElementByName("LOGO");

            Object.Instantiate(Auga.Assets.AugaLogo, __instance.m_mainMenu.transform);

            /*__instance.m_loading = __instance.Replace("Loading", Auga.Assets.MainMenuPrefab, "Loading", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal).gameObject;
            __instance.m_loading.SetActive(false);
            Localization.instance.Localize(__instance.m_loading.transform);*/


            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            /*var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            __instance.m_mainMenu = mainMenu.gameObject;
            __instance.m_versionLabel = mainMenu.Find("Version").GetComponent<Text>();
            __instance.m_betaText = mainMenu.Find("DummyObjects/Dummy").gameObject;
            __instance.m_ndaPanel = mainMenu.Find("DummyObjects/Dummy").gameObject;
            mainMenu.GetComponentInChildren<ChangeLog>().m_changeLog = originalChangeLogAsset;
            Localization.instance.Localize(mainMenu);*/

            //var newCharacterScreen = __instance.transform.Find("CharacterSelection/NewCharacterPanel");
            //Object.Instantiate(Auga.Assets.MainMenuPrefab.transform.Find("CustomTest").gameObject, newCharacterScreen, false);
        }
    }
}
