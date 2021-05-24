using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    public static class FejdStartup_Awake_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var originalChangeLogAsset = __instance.GetComponentInChildren<ChangeLog>(true).m_changeLog;

            /*var callOriginal = !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.MainMenuPrefab, "TextInput", out var newMainMenu);

            newMainMenu.GetComponentInChildren<ChangeLog>().m_changeLog = originalChangeLogAsset;
            Localization.instance.Localize(newMainMenu.transform);

            return callOriginal;*/

            /*__instance.HideElementByType<ChangeLog>();
            __instance.m_versionLabel.HideElement();
            __instance.m_mainMenu.HideElementByName("showlog");
            __instance.m_mainMenu.HideElementByName("Embers");
            __instance.m_mainMenu.HideElementByName("Embers (1)");
            __instance.m_mainMenu.HideElementByName("Embers (2)");
            __instance.m_mainMenu.HideElementByName("Embers (3)");
            __instance.m_mainMenu.HideElementByName("LOGO");

            Object.Instantiate(Auga.Assets.AugaLogo, __instance.m_mainMenu.transform);*/

            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            __instance.m_mainMenu = mainMenu.gameObject;
            __instance.m_versionLabel = mainMenu.Find("Version").GetComponent<Text>();
            __instance.m_betaText = mainMenu.Find("DummyObjects/Dummy").gameObject;
            __instance.m_ndaPanel = mainMenu.Find("DummyObjects/Dummy").gameObject;
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/StartGame", __instance.OnStartGame);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Settings", __instance.OnButtonSettings);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Credits", __instance.OnCredits);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Exit", __instance.OnAbort);
            __instance.GetComponentInChildren<ChangeLog>(true).m_changeLog = originalChangeLogAsset;

            var connectionFailedDialog = __instance.Replace("ConnectionFailed", Auga.Assets.MainMenuPrefab);
            __instance.m_connectionFailedPanel = connectionFailedDialog.gameObject;
            __instance.m_connectionFailedError = connectionFailedDialog.Find("Text").GetComponent<Text>();

            var credits = __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            __instance.m_creditsPanel = credits.gameObject;
            __instance.m_creditsList = (RectTransform)credits.Find("ContactInfo");
            SetButtonListener(__instance.m_creditsPanel.transform, "Back-panel/ButtonSettings", __instance.OnCreditsBack);

            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);

            __instance.m_loading = __instance.Replace("Loading", Auga.Assets.MainMenuPrefab).gameObject;
            __instance.m_loading.SetActive(false);

            //var newCharacterScreen = __instance.transform.Find("CharacterSelection/NewCharacterPanel");
            //Object.Instantiate(Auga.Assets.MainMenuPrefab.transform.Find("CustomTest").gameObject, newCharacterScreen, false);

            var charSelect = __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);
            __instance.m_selectCharacterPanel = charSelect.gameObject;
            __instance.m_removeCharacterDialog = charSelect.Find("RemoveCharacterDialog").gameObject;
            __instance.m_removeCharacterName = charSelect.Find("RemoveCharacterDialog/Text").GetComponent<Text>();
            __instance.m_csRemoveButton = charSelect.Find("Panel/RemoveButton").GetComponent<Button>();
            __instance.m_csStartButton = charSelect.Find("Panel/Start").GetComponent<Button>();
            __instance.m_csNewButton = charSelect.Find("Panel/NewButton").GetComponent<Button>();
            __instance.m_csNewBigButton = charSelect.Find("Panel/NewButtonBig").GetComponent<Button>();
            __instance.m_csLeftButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csRightButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csName = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Text>();
            SetButtonListener(charSelect, "Panel/RemoveButton", FejdStartup.instance.OnCharacterRemove);
            SetButtonListener(charSelect, "Panel/NewButton", FejdStartup.instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/NewButtonBig", FejdStartup.instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Back", FejdStartup.instance.OnSelelectCharacterBack);
            SetButtonListener(charSelect, "Panel/Start", FejdStartup.instance.OnCharacterStart);
            SetButtonListener(charSelect, "RemoveCharacterDialog/DividerMedium/Content/ButtonYes", FejdStartup.instance.OnButtonRemoveCharacterYes);
            SetButtonListener(charSelect, "RemoveCharacterDialog/DividerMedium/Content/ButtonNo", FejdStartup.instance.OnButtonRemoveCharacterNo);

            Localization.instance.Localize(__instance.transform);
        }

        private static void SetButtonListener(Transform root, string childName, UnityAction listener)
        {
            var button = root.Find(childName).GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }
    }

    //UpdateCharacterList
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
            if (characterSelect != null)
            {
                characterSelect.UpdateCharacterList();
            }
        }
    }
}
