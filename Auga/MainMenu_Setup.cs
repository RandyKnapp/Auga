using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.SnapTo))]
    public static class SnapTo_Patch
    {
        public static void Postfix(TextsDialog __instance, RectTransform listRoot, ScrollRect scrollRect)
        {
            var augaTextComponent = __instance.GetComponent<AugaTextsDialogFilter>();
            if (augaTextComponent == null)
                return;

            var newVector = new Vector2(0, listRoot.anchoredPosition.y);
            listRoot.anchoredPosition = newVector;
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    public static class FejdStartup_Awake_Patch
    {
        public static void Prefix(FejdStartup __instance)
        {
            ZInput.Initialize();

            //var originalChangeLogAsset = __instance.GetComponentInChildren<ChangeLog>(true).m_changeLog;

            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            /*var originalLogo = __instance.transform.Find("Menu/Logo");
            originalLogo.SetParent(__instance.transform, true);

            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            originalLogo.SetParent(mainMenu, true);

            __instance.m_mainMenu = mainMenu.gameObject;
            __instance.m_menuList = mainMenu.Find("MenuList").gameObject;
            __instance.m_menuSelectedButton = mainMenu.Find("MenuList/StartGame").GetComponent<Button>();
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
            SetButtonListener(connectionFailedDialog, "ButtonYes", __instance.OnConnectionFailedOk);

            var credits = __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            __instance.m_creditsPanel = credits.gameObject;
            __instance.m_creditsList = (RectTransform)credits.Find("ContactInfo");
            SetButtonListener(__instance.m_creditsPanel.transform, "Back-panel/ButtonSettings", __instance.OnCreditsBack);

            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);

            __instance.m_loading = __instance.Replace("Loading", Auga.Assets.MainMenuPrefab).gameObject;
            __instance.m_loading.SetActive(false);

            var charSelect = __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);
            __instance.m_selectCharacterPanel = charSelect.gameObject;
            __instance.m_removeCharacterDialog = charSelect.Find("RemoveCharacterDialog").gameObject;
            __instance.m_removeCharacterName = charSelect.Find("RemoveCharacterDialog/Text").GetComponent<Text>();
            __instance.m_csRemoveButton = charSelect.Find("Panel/Inset/RemoveButton").GetComponent<Button>();
            __instance.m_csStartButton = charSelect.Find("Panel/Start").GetComponent<Button>();
            __instance.m_csNewButton = charSelect.Find("Panel/Inset/NewButton").GetComponent<Button>();
            __instance.m_csNewBigButton = charSelect.Find("Panel/Inset/NewButtonBig").GetComponent<Button>();
            __instance.m_csLeftButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csRightButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csName = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Text>();
            SetButtonListener(charSelect, "Panel/Inset/RemoveButton", __instance.OnCharacterRemove);
            SetButtonListener(charSelect, "Panel/Inset/NewButton", __instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Inset/NewButtonBig", __instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Back", __instance.OnSelelectCharacterBack);
            SetButtonListener(charSelect, "Panel/Start", __instance.OnCharacterStart);
            SetButtonListener(charSelect, "Panel/ManageSaves", () => __instance.OnManageSaves(1));
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", __instance.OnButtonRemoveCharacterYes);
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", __instance.OnButtonRemoveCharacterNo);

            var oldPlayerCustomizaton = __instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>();
            var originalNoHair = oldPlayerCustomizaton.m_noHair;
            var originalNoBeard = oldPlayerCustomizaton.m_noBeard;

            var newCharacter = __instance.Replace("CharacterSelection/NewCharacterPanel", Auga.Assets.MainMenuPrefab);
            var newPlayerCustomization = newCharacter.GetComponent<PlayerCustomizaton>();
            newPlayerCustomization.m_noHair = originalNoHair;
            newPlayerCustomization.m_noBeard = originalNoBeard;
            __instance.m_newCharacterPanel = newCharacter.gameObject;
            __instance.m_csNewCharacterDone = newCharacter.Find("Panel/Done").GetComponent<Button>();
            __instance.m_newCharacterError = newCharacter.Find("Panel/Content/NameExistsWarning").gameObject;
            __instance.m_csNewCharacterName = newCharacter.Find("Panel/Content/CharacterName").GetComponent<InputField>();
            SetButtonListener(newCharacter, "Panel/Done", () => __instance.OnNewCharacterDone(true));
            SetButtonListener(newCharacter, "Panel/Cancel", __instance.OnNewCharacterCancel);

            {
                var toggle = newCharacter.Find("Panel/Content/ToggleGroup/Toggle_Female").GetComponent<Toggle>();
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization.SetPlayerModel(1); });
                toggle.onValueChanged.AddListener((on) => toggle.transform.GetChild(1).gameObject.SetActive(on));
            }
            {
                var toggle = newCharacter.Find("Panel/Content/ToggleGroup/Toggle_Male").GetComponent<Toggle>();
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization.SetPlayerModel(0); });
                toggle.onValueChanged.AddListener((on) => toggle.transform.GetChild(1).gameObject.SetActive(on));
            }

            Object.Destroy(__instance.m_joinIPPanel);
            var startGame = __instance.Replace("StartGame", Auga.Assets.MainMenuPrefab);
            __instance.m_startGamePanel = startGame.gameObject;
            __instance.m_createWorldPanel = startGame.Find("NewWorldDialog").gameObject;
            __instance.m_serverListPanel = startGame.Find("Panel/JoinPanel").gameObject;
            __instance.m_publicServerToggle = startGame.Find("Panel/WorldPanel/CheckboxRow/StartPublicGameToggle").GetComponent<Toggle>();
            __instance.m_openServerToggle = startGame.Find("Panel/WorldPanel/CheckboxRow/StartServerToggle").GetComponent<Toggle>();
            __instance.m_serverPassword = startGame.Find("Panel/WorldPanel/ServerPassword").GetComponent<InputField>();
            __instance.m_serverListRoot = startGame.Find("Panel/JoinPanel/ScrollRect/ItemList").GetComponent<RectTransform>();
            __instance.m_serverListElement = Auga.Assets.ServerListElement;
            __instance.m_serverListEnsureVisible = startGame.Find("Panel/JoinPanel/ScrollRect").GetComponent<ScrollRectEnsureVisible>();
            __instance.m_serverListElementStep = 30;
            __instance.m_serverCount = startGame.Find("Panel/JoinPanel/ServerCount").GetComponent<Text>();
            __instance.m_serverRefreshButton = startGame.Find("Panel/JoinPanel/RefreshButton").GetComponent<Button>();
            __instance.m_filterInputField = startGame.Find("Panel/JoinPanel/Filter").GetComponent<InputField>();
            __instance.m_passwordError = startGame.Find("Panel/WorldPanel/ServerPassword/Tooltip/ErrorText").GetComponent<Text>();
            __instance.m_manualIPButton = startGame.Find("Panel/JoinPanel/JoinIPButton").GetComponent<Button>();
            __instance.m_joinIPPanel = startGame.Find("JoinIP").gameObject;
            __instance.m_joinIPJoinButton = startGame.Find("JoinIP/Connect").GetComponent<Button>();
            __instance.m_joinIPAddress = startGame.Find("JoinIP/Address").GetComponent<InputField>();
            __instance.m_joinGameButton = startGame.Find("Panel/JoinPanel/Connect").GetComponent<Button>();
            __instance.m_worldListPanel = startGame.Find("Panel/WorldPanel").gameObject;
            __instance.m_worldListRoot = startGame.Find("Panel/WorldPanel/ScrollRect/ItemList").GetComponent<RectTransform>();
            __instance.m_worldListElement = Auga.Assets.WorldListElement;
            __instance.m_worldListEnsureVisible = startGame.Find("Panel/WorldPanel/ScrollRect").GetComponent<ScrollRectEnsureVisible>();
            __instance.m_worldListElementStep = 30;
            __instance.m_newWorldName = startGame.Find("NewWorldDialog/WorldName").GetComponent<InputField>();
            __instance.m_newWorldSeed = startGame.Find("NewWorldDialog/WorldSeed").GetComponent<InputField>();
            __instance.m_newWorldDone = startGame.Find("NewWorldDialog/Done").GetComponent<Button>();
            __instance.m_worldStart = startGame.Find("Panel/WorldPanel/Start").GetComponent<Button>();
            __instance.m_worldRemove = startGame.Find("Panel/WorldPanel/RemoveButton").GetComponent<Button>();
            __instance.m_removeWorldDialog = startGame.Find("RemoveWorldDialog").gameObject;
            __instance.m_removeWorldName = startGame.Find("RemoveWorldDialog/Text").GetComponent<Text>();
            __instance.m_friendFilterSwitch = startGame.Find("Panel/JoinPanel/CheckboxRow/FriendsFilter").GetComponent<Toggle>();
            __instance.m_publicFilterSwitch = startGame.Find("Panel/JoinPanel/CheckboxRow/PublicFilter").GetComponent<Toggle>();
            __instance.m_joinIPAddress.text = PlayerPrefs.GetString("LastIPJoined", "");

            SetButtonListener(startGame, "Panel/WorldPanel/RemoveButton", __instance.OnWorldRemove);
            SetButtonListener(startGame, "Panel/WorldPanel/NewButton", __instance.OnWorldNew);
            SetButtonListener(startGame, "Panel/WorldPanel/Back", __instance.OnStartGameBack);
            SetButtonListener(startGame, "Panel/WorldPanel/Start", __instance.OnWorldStart);
            SetButtonListener(startGame, "Panel/JoinPanel/RefreshButton", __instance.RequestServerList);
            SetInputFieldListener(startGame, "Panel/JoinPanel/Filter", (_) => __instance.OnServerFilterChanged());
            SetToggleListener(startGame, "Panel/JoinPanel/CheckboxRow/FriendsFilter", (_) => __instance.OnServerFilterChanged());
            SetToggleListener(startGame, "Panel/JoinPanel/CheckboxRow/PublicFilter", (_) => __instance.OnServerFilterChanged());
            SetButtonListener(startGame, "Panel/JoinPanel/JoinIPButton", __instance.OnJoinIPOpen);
            SetButtonListener(startGame, "Panel/JoinPanel/Back", __instance.OnStartGameBack);
            SetButtonListener(startGame, "Panel/JoinPanel/Connect", __instance.OnJoinStart);
            SetButtonListener(startGame, "RemoveWorldDialog/ButtonYes", __instance.OnButtonRemoveWorldYes);
            SetButtonListener(startGame, "RemoveWorldDialog/ButtonNo", __instance.OnButtonRemoveWorldNo);
            SetButtonListener(startGame, "NewWorldDialog/Cancel", __instance.OnNewWorldBack);
            SetButtonListener(startGame, "NewWorldDialog/Done", () => __instance.OnNewWorldDone(true));
            SetButtonListener(startGame, "JoinIP/Cancel", __instance.OnJoinIPBack);
            SetButtonListener(startGame, "JoinIP/Connect", __instance.OnJoinIPConnect);

            var tabHandler = startGame.GetComponentInChildren<TabHandler>(true);
            tabHandler.m_tabs[0].m_onClick = new Button.ButtonClickedEvent();
            tabHandler.m_tabs[0].m_onClick.AddListener(__instance.OnSelectWorldTab);
            tabHandler.m_tabs[1].m_onClick = new Button.ButtonClickedEvent();
            tabHandler.m_tabs[1].m_onClick.AddListener(__instance.OnServerListTab);

            // Why doesn't this work?
            //var pleaseWait = __instance.Replace("PleaseWait", Auga.Assets.MainMenuPrefab);
            //__instance.m_pleaseWait = pleaseWait.gameObject;

            __instance.m_menuAnimator.runtimeAnimatorController = Auga.Assets.MainMenuPrefab.GetComponent<Animator>().runtimeAnimatorController;

            // New local/cloud save stuff
            __instance.m_csFileSource = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Text>();
            __instance.m_csSourceInfo = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Text>();
            //__instance.m_worldSourceInfoPanel = startGame.Find("Panel/WorldPanel/Server help").gameObject;
            //__instance.m_worldSourceInfo = startGame.Find("Panel/WorldPanel/Server help/Text").GetComponent<Text>();

            __instance.OnSelectWorldTab();

            Object.Instantiate(Auga.Assets.MainMenuPrefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true), __instance.transform);

            Localization.instance.Localize(__instance.transform);*/
        }

        /*private static void SetButtonListener(Transform root, string childName, UnityAction listener)
        {
            var button = root.Find(childName).GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }

        private static void SetInputFieldListener(Transform root, string childName, UnityAction<string> listener)
        {
            var inputField = root.Find(childName).GetComponent<InputField>();
            inputField.onValueChanged = new InputField.OnChangeEvent();
            inputField.onValueChanged.AddListener(listener);
        }

        private static void SetToggleListener(Transform root, string childName, UnityAction<bool> listener)
        {
            var toggle = root.Find(childName).GetComponent<Toggle>();
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(listener);
        }*/
    }

    /*[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
            if (characterSelect != null)
            {
                characterSelect.UpdateCharacterList();
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdatePasswordError))]
    public static class FejdStartup_UpdatePasswordError_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var hasErrorText = !string.IsNullOrEmpty(__instance.m_passwordError.text);
            __instance.m_passwordError.transform.parent.gameObject.SetActive(hasErrorText);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
    public static class FejdStartup_OnNewCharacterDone_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var photoBooth = __instance.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true);
            if (photoBooth != null)
                photoBooth.StartCoroutine(NewCharPhotoCoroutine(__instance, photoBooth));
        }

        private static IEnumerator NewCharPhotoCoroutine(FejdStartup instance, AugaCharacterSelectPhotoBooth photoBooth)
        {
            yield return photoBooth.TakePhoto(instance.m_profileIndex);
            instance.UpdateCharacterList();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ClearCharacterPreview))]
    public static class FejdStartup_ClearCharacterPreview_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(FejdStartup __instance)
        {
            if (AugaCharacterSelectPhotoBooth.TakingPhotos)
            {
                Object.Destroy(__instance.m_playerInstance);
                __instance.m_playerInstance = null;
                return false;
            }

            return true;
        }
    }*/
}
