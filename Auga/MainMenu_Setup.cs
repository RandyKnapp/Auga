using System;
using System.Collections;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        // Переданные из Prefix в Postfix данные о волосах/бороде
        private static ItemDrop _originalNoHair;
        private static ItemDrop _originalNoBeard;

        // Null-safe Find + GetComponent. Logs warning if path or component missing.
        private static T FC<T>(Transform root, string path) where T : Component
        {
            if (root == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: root is null (path={path})"); return null; }
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: path not found: {path}"); return null; }
            var c = t.GetComponent<T>();
            if (c == null) Auga.LogWarning($"FC<{typeof(T).Name}>: no component on: {path}");
            return c;
        }

        // Null-safe Find -> GameObject.
        private static GameObject FO(Transform root, string path)
        {
            if (root == null) { Auga.LogWarning($"FO: root is null (path={path})"); return null; }
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"FO: path not found: {path}"); return null; }
            return t.gameObject;
        }

        public static void Prefix(FejdStartup __instance)
        {
            ZInput.Initialize();

            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            // Сохраняем логотип до замены меню
            var originalLogo = __instance.transform.Find("Menu/Logo");
            if (originalLogo != null)
                originalLogo.SetParent(__instance.transform, true);

            // Заменяем все префабы — должно произойти ДО ванильного Awake(),
            // чтобы ванильный код нашёл нужные объекты по именам.
            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            if (mainMenu == null) { Auga.LogError("Failed to replace Menu"); return; }
            if (originalLogo != null)
                originalLogo.SetParent(mainMenu, true);

            __instance.Replace("ConnectionFailed", Auga.Assets.MainMenuPrefab);
            __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);
            __instance.Replace("Loading", Auga.Assets.MainMenuPrefab);
            __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);

            // Сохраняем hair/beard из ванильного NewCharacterPanel ДО его замены
            var oldCustomizaton = __instance.m_newCharacterPanel != null
                ? __instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>()
                : null;
            _originalNoHair = oldCustomizaton?.m_noHair;
            _originalNoBeard = oldCustomizaton?.m_noBeard;

            __instance.Replace("CharacterSelection/NewCharacterPanel", Auga.Assets.MainMenuPrefab);
            __instance.Replace("StartGame", Auga.Assets.MainMenuPrefab);

            // КРИТИЧНО: Replace() уничтожает оригинальные GO, превращая инспекторные
            // ссылки в "dead" объекты. Ванильный Awake() обращается к некоторым из
            // них (m_crossplayServerToggle, m_serverOptions, m_menuList и др.) ДО
            // нашего Postfix. В Unity 6 доступ к .gameObject на destroyed-компоненте
            // бросает NPE. Создаём stubs прямо здесь, чтобы ванильный код не упал.
            FixDeadFields(__instance);
        }

        public static void Postfix(FejdStartup __instance)
        {
            // Все назначения полей выполняются ЗДЕСЬ, ПОСЛЕ ванильного Awake(),
            // который перезаписывал наши значения нулями (т.к. ванильные пути
            // не совпадают со структурой Auga-префабов).

            // ---- Menu ----
            var mainMenu = __instance.transform.Find("Menu");
            if (mainMenu != null)
            {
                __instance.m_mainMenu = mainMenu.gameObject;
                __instance.m_menuList = FO(mainMenu, "MenuList");
                __instance.m_menuSelectedButton = FC<Button>(mainMenu, "MenuList/StartGame");
                // m_versionLabel: Menu/Version использует legacy Text (не TMP_Text) — поле остаётся null
                __instance.m_betaText = FO(mainMenu, "DummyObjects/Dummy");
                __instance.m_ndaPanel = FO(mainMenu, "DummyObjects/Dummy");
                SetButtonListener(mainMenu, "MenuList/StartGame", __instance.OnStartGame);
                SetButtonListener(mainMenu, "MenuList/Settings", __instance.OnButtonSettings);
                SetButtonListener(mainMenu, "MenuList/Credits", __instance.OnCredits);
                SetButtonListener(mainMenu, "MenuList/Exit", __instance.OnAbort);

                // Кнопки MenuList — вложенные prefab-экземпляры AugaMenuButton.
                // Переопределения из MainMenu.prefab не применяются без пересборки asset bundle,
                // поэтому устанавливаем тексты программно через GetComponentInChildren.
                SetMenuButtonText(mainMenu, "MenuList/StartGame", "$menu_start");
                SetMenuButtonText(mainMenu, "MenuList/Settings", "$menu_settings");
                SetMenuButtonText(mainMenu, "MenuList/Credits", "$menu_credits");
                SetMenuButtonText(mainMenu, "MenuList/Exit", "$menu_exit");
            }

            // ---- ConnectionFailed ----
            // В Auga ConnectionFailed находится под CharacterSelection, а не в корне
            var connectionFailed = __instance.transform.Find("CharacterSelection/ConnectionFailed")
                                ?? __instance.transform.Find("ConnectionFailed");
            if (connectionFailed != null)
            {
                __instance.m_connectionFailedPanel = connectionFailed.gameObject;
                // Text компонент — legacy Text, не TMP_Text; поле m_connectionFailedError остаётся null (косметика)
                SetButtonListener(connectionFailed, "ButtonYes", __instance.OnConnectionFailedOk);
            }

            // ---- Credits ----
            var credits = __instance.transform.Find("Credits");
            if (credits != null)
            {
                __instance.m_creditsPanel = credits.gameObject;
                __instance.m_creditsList = credits.Find("ContactInfo") as RectTransform;
                SetButtonListener(credits, "Back-panel/ButtonSettings", __instance.OnCreditsBack);
            }

            // ---- Loading ----
            var loading = __instance.transform.Find("Loading");
            if (loading != null)
            {
                __instance.m_loading = loading.gameObject;
                __instance.m_loading.SetActive(false);
            }

            // ---- CharacterSelectScreen ----
            // m_characterSelectScreen — это весь CharacterSelection-узел. Используется
            // в HideAll(), ShowCharacterSelection(), UpdateCamera() и т.д. Ванильный код
            // не находит его сам (путь не совпадает), поэтому назначаем явно.
            var characterSelectionNode = __instance.transform.Find("CharacterSelection");
            if (characterSelectionNode != null)
                __instance.m_characterSelectScreen = characterSelectionNode.gameObject;

            // ---- SelectCharacter ----
            // Реальная структура Auga SelectCharacter:
            //   SelectCharacter
            //   ├── Panel
            //   │   ├── Inset / RemoveButton, NewButton, NewButtonBig
            //   │   ├── Back
            //   │   ├── Start
            //   │   ├── ManageSaves
            //   │   ├── DummyObjects
            //   │   └── SourceInfo / Text
            //   └── RemoveCharacterDialog / Text, ButtonYes, ButtonNo
            var charSelect = __instance.transform.Find("CharacterSelection/SelectCharacter");
            if (charSelect != null)
            {
                __instance.m_selectCharacterPanel = charSelect.gameObject;
                __instance.m_removeCharacterDialog = FO(charSelect, "RemoveCharacterDialog");
                __instance.m_removeCharacterName = FC<TMP_Text>(charSelect, "RemoveCharacterDialog/Text");
                __instance.m_csRemoveButton = FC<Button>(charSelect, "Panel/Inset/RemoveButton");
                __instance.m_csStartButton = FC<Button>(charSelect, "Panel/Start");
                __instance.m_csNewButton = FC<Button>(charSelect, "Panel/Inset/NewButton");
                __instance.m_csNewBigButton = FC<Button>(charSelect, "Panel/Inset/NewButtonBig");
                __instance.m_csLeftButton = FC<Button>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csRightButton = FC<Button>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csName = FC<TMP_Text>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csFileSource = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
                __instance.m_csSourceInfo = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
                SetButtonListener(charSelect, "Panel/Inset/RemoveButton", __instance.OnCharacterRemove);
                SetButtonListener(charSelect, "Panel/Inset/NewButton", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Panel/Inset/NewButtonBig", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Panel/Back", __instance.OnSelelectCharacterBack);
                SetButtonListener(charSelect, "Panel/Start", __instance.OnCharacterStart);
                SetButtonListener(charSelect, "Panel/ManageSaves", () => __instance.OnManageSaves(1));
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", __instance.OnButtonRemoveCharacterYes);
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", __instance.OnButtonRemoveCharacterNo);

                // Тексты кнопок SelectCharacter — вложенные AugaMenuButton с дефолтом "LABEL"
                SetMenuButtonText(charSelect, "Panel/Inset/RemoveButton", "$menu_remove");
                SetMenuButtonText(charSelect, "Panel/Inset/NewButton", "$menu_new");
                SetMenuButtonText(charSelect, "Panel/Inset/NewButtonBig", "$menu_new");
                SetMenuButtonText(charSelect, "Panel/Back", "$menu_back");
                SetMenuButtonText(charSelect, "Panel/Start", "$menu_start");
                SetMenuButtonText(charSelect, "Panel/ManageSaves", "$menu_managesaves");
            }

            // ---- NewCharacterPanel ----
            // Реальная структура Auga-префаба (из get_hierarchy):
            //   NewCharacterPanel [PlayerCustomizaton]
            //   └── Panel
            //       ├── Content [TabHandler]
            //       │   ├── CharacterName [GuiInputField]
            //       │   ├── NameExistsWarning
            //       │   ├── ToggleGroup
            //       │   │   ├── Toggle_Female [Toggle]
            //       │   │   └── Toggle_Male   [Toggle]
            //       │   ├── SkinTone          ← (не SkinColor!)
            //       │   │   ├── Label [Text]
            //       │   │   └── Slider [Slider]
            //       │   ├── Hair Tone         ← (с пробелом! не HairColor)
            //       │   │   ├── Label [Text]
            //       │   │   └── Slider [Slider]
            //       │   ├── Blondness         ← (не HairTone!)
            //       │   │   ├── Label [Text]
            //       │   │   └── Slider [Slider]
            //       │   ├── TabButtons/Tabs
            //       │   │   ├── Hair   [Button via ColorButtonText]
            //       │   │   └── Beard  [Button via ColorButtonText]
            //       │   └── ScrollRect/Hair [CharacterPortraitsController]
            //       ├── Done   [ColorButtonText(=Button)] / Label [TMP]
            //       └── Cancel [ColorButtonText(=Button)] / Label [TMP]
            var newCharacter = __instance.transform.Find("CharacterSelection/NewCharacterPanel");
            if (newCharacter != null)
            {
                var newPlayerCustomization = newCharacter.GetComponent<PlayerCustomizaton>();
                if (newPlayerCustomization != null)
                {
                    newPlayerCustomization.m_noHair  = _originalNoHair;
                    newPlayerCustomization.m_noBeard = _originalNoBeard;

                    // Слайдеры цвета кожи/волос/оттенка — под Panel/Content/
                    // Имена в Auga-префабе: SkinTone, "Hair Tone" (с пробелом!), Blondness
                    var skinToneT  = newCharacter.Find("Panel/Content/SkinTone");
                    var hairToneT  = newCharacter.Find("Panel/Content/Hair Tone");
                    var blondnessT = newCharacter.Find("Panel/Content/Blondness");
                    if (skinToneT  != null) newPlayerCustomization.m_skinHue   = skinToneT.GetComponentInChildren<Slider>(true);
                    if (hairToneT  != null) newPlayerCustomization.m_hairLevel = hairToneT.GetComponentInChildren<Slider>(true);
                    if (blondnessT != null) newPlayerCustomization.m_hairTone  = blondnessT.GetComponentInChildren<Slider>(true);

                    Auga.Log($"[NewChar] skinHue={newPlayerCustomization.m_skinHue != null} hairLevel={newPlayerCustomization.m_hairLevel != null} hairTone={newPlayerCustomization.m_hairTone != null}");

                    // m_beardPanel — Beard-tab скрывается для женского персонажа через SetActive(isMale)
                    // Устанавливаем на GO кнопки Beard в TabButtons, чтобы прятать вкладку для женщин
                    var beardTabT = newCharacter.Find("Panel/Content/TabButtons/Tabs/Beard");
                    newPlayerCustomization.m_beardPanel = beardTabT != null
                        ? beardTabT.GetComponent<RectTransform>()
                        : null; // PlayerCustomizaton_OnEnable_Patch финализер подавит NPE

                    // m_selectedHair / m_selectedBeard — в Auga нет видимых текстовых полей для имён.
                    // НО: PlayerCustomizaton.Update() на строке 73974 пишет m_selectedHair.text =
                    // ПЕРЕД применением цвета кожи/волос. Если null → NPE → Finalizer глушит →
                    // цвет кожи никогда не применяется. Создаём скрытые заглушки.
                    var hairStubGO = new GameObject("_AugaStub_SelectedHair");
                    hairStubGO.SetActive(false);
                    hairStubGO.transform.SetParent(newCharacter, false);
                    newPlayerCustomization.m_selectedHair = hairStubGO.AddComponent<TMPro.TextMeshProUGUI>();

                    var beardStubGO = new GameObject("_AugaStub_SelectedBeard");
                    beardStubGO.SetActive(false);
                    beardStubGO.transform.SetParent(newCharacter, false);
                    newPlayerCustomization.m_selectedBeard = beardStubGO.AddComponent<TMPro.TextMeshProUGUI>();

                    // m_maleToggle / m_femaleToggle — под Panel/Content/ToggleGroup/
                    var toggleFemaleField = FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Female");
                    var toggleMaleField   = FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Male");
                    if (toggleFemaleField != null) newPlayerCustomization.m_femaleToggle = toggleFemaleField;
                    if (toggleMaleField   != null) newPlayerCustomization.m_maleToggle   = toggleMaleField;

                    // Подключаем tab-кнопки Hair/Beard к CharacterPortraitsController
                    var portraitsCtrl = newCharacter.GetComponentInChildren<AugaUnity.CharacterPortraitsController>(true);
                    var hairTabBtn  = newCharacter.Find("Panel/Content/TabButtons/Tabs/Hair")?.GetComponent<Button>();
                    var beardTabBtn = newCharacter.Find("Panel/Content/TabButtons/Tabs/Beard")?.GetComponent<Button>();
                    if (portraitsCtrl != null)
                    {
                        if (hairTabBtn  != null) hairTabBtn.onClick.AddListener(() => { portraitsCtrl.SwitchToHairMode();  portraitsCtrl.InitializeChraracterPortraits(); });
                        if (beardTabBtn != null) beardTabBtn.onClick.AddListener(() => { portraitsCtrl.SwitchToBeardMode(); portraitsCtrl.InitializeChraracterPortraits(); });
                    }
                }

                __instance.m_newCharacterPanel  = newCharacter.gameObject;
                // Done/Cancel находятся под Panel/, а не Content/
                __instance.m_csNewCharacterDone = FC<Button>(newCharacter, "Panel/Done");
                __instance.m_newCharacterError  = FO(newCharacter, "Panel/Content/NameExistsWarning");
                __instance.m_csNewCharacterName = FC<GUIFramework.GuiInputField>(newCharacter, "Panel/Content/CharacterName");

                SetButtonListener(newCharacter, "Panel/Done",   () => __instance.OnNewCharacterDone(true));
                SetButtonListener(newCharacter, "Panel/Cancel", __instance.OnNewCharacterCancel);

                // Тексты кнопок (Done/Cancel: TextMeshProUGUI на дочернем "Label")
                SetMenuButtonText(newCharacter, "Panel/Done",   "$menu_done");
                SetMenuButtonText(newCharacter, "Panel/Cancel", "$menu_cancel");

                // Лейблы слайдеров — не перезаписываем, в Auga-префабе уже заданы
                // правильные ключи через Unity-редактор (AlwaysUpper Text компонент).
                // Localization.Localize() в конце Postfix'а переведёт их автоматически.

                // Слушатели гендерных тоглов — под Panel/Content/ToggleGroup/
                var newPlayerCustomization2 = newCharacter.GetComponent<PlayerCustomizaton>();
                var toggleFemale = FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Female");
                if (toggleFemale != null)
                {
                    toggleFemale.onValueChanged.RemoveAllListeners();
                    toggleFemale.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization2?.SetPlayerModel(1); });
                }
                var toggleMale = FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Male");
                if (toggleMale != null)
                {
                    toggleMale.onValueChanged.RemoveAllListeners();
                    toggleMale.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization2?.SetPlayerModel(0); });
                }
            }

            // ---- StartGame ----
            var startGame = __instance.transform.Find("StartGame");
            if (startGame != null)
            {
                __instance.m_startGamePanel = startGame.gameObject;
                __instance.m_createWorldPanel = FO(startGame, "NewWorldDialog");
                __instance.m_serverListPanel = FO(startGame, "Panel/JoinPanel");
                __instance.m_publicServerToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartPublicGameToggle");
                __instance.m_openServerToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartServerToggle");
                __instance.m_serverPassword = FC<GUIFramework.GuiInputField>(startGame, "Panel/WorldPanel/ServerPassword");
                __instance.m_passwordError = FC<TMP_Text>(startGame, "Panel/WorldPanel/ServerPassword/Tooltip/ErrorText");
                __instance.m_worldListRoot = FC<RectTransform>(startGame, "Panel/WorldPanel/ScrollRect/ItemList");
                __instance.m_worldListElement = Auga.Assets.WorldListElement;
                __instance.m_worldListEnsureVisible = FC<ScrollRectEnsureVisible>(startGame, "Panel/WorldPanel/ScrollRect");
                __instance.m_worldListElementStep = 30;
                __instance.m_newWorldName = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldName");
                __instance.m_newWorldSeed = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldSeed");
                __instance.m_newWorldDone = FC<Button>(startGame, "NewWorldDialog/Done");
                __instance.m_worldStart = FC<Button>(startGame, "Panel/WorldPanel/Start");
                __instance.m_worldRemove = FC<Button>(startGame, "Panel/WorldPanel/RemoveButton");
                __instance.m_removeWorldDialog = FO(startGame, "RemoveWorldDialog");
                __instance.m_removeWorldName = FC<TMP_Text>(startGame, "RemoveWorldDialog/Text");
                __instance.m_worldListPanel = FO(startGame, "Panel/WorldPanel");

                SetButtonListener(startGame, "Panel/WorldPanel/RemoveButton", __instance.OnWorldRemove);
                SetButtonListener(startGame, "Panel/WorldPanel/NewButton", __instance.OnWorldNew);
                SetButtonListener(startGame, "Panel/WorldPanel/Back", __instance.OnStartGameBack);
                SetButtonListener(startGame, "Panel/WorldPanel/Start", __instance.OnWorldStart);
                SetButtonListener(startGame, "RemoveWorldDialog/ButtonYes", __instance.OnButtonRemoveWorldYes);
                SetButtonListener(startGame, "RemoveWorldDialog/ButtonNo", __instance.OnButtonRemoveWorldNo);
                SetButtonListener(startGame, "NewWorldDialog/Cancel", __instance.OnNewWorldBack);
                SetButtonListener(startGame, "NewWorldDialog/Done", () => __instance.OnNewWorldDone(true));

                var tabHandler = startGame.GetComponentInChildren<TabHandler>(true);
                if (tabHandler != null && tabHandler.m_tabs.Count >= 2)
                {
                    tabHandler.m_tabs[0].m_onClick = new Button.ButtonClickedEvent();
                    tabHandler.m_tabs[0].m_onClick.AddListener(__instance.OnSelectWorldTab);
                    tabHandler.m_tabs[1].m_onClick = new Button.ButtonClickedEvent();
                    tabHandler.m_tabs[1].m_onClick.AddListener(__instance.OnServerListTab);
                }
            }

            // ---- Animator ----
            if (__instance.m_menuAnimator != null)
            {
                __instance.m_menuAnimator.runtimeAnimatorController =
                    Auga.Assets.MainMenuPrefab.GetComponent<Animator>()?.runtimeAnimatorController;
            }

            Localization.instance.Localize(__instance.transform);

            // ---- Stub-компоненты для DEAD-полей ----
            // Поля m_patchLogScroll, m_serverOptionsButton, m_crossplayServerToggle и др. —
            // это новые поля Valheim которых нет в Auga. После Replace() старые Unity-объекты
            // уничтожены, но C#-ссылки остались ("dead"). В Unity 6 обращение .gameObject на
            // destroyed-компонент бросает NPE. Создаём живые stub-компоненты на скрытом GO.
            FixDeadFields(__instance);

            // ---- PhotoBooth ----
            // Запускаем ПОСЛЕ того как FejdStartup.instance и m_mainCamera инициализированы
            // ванильным Awake() — иначе GetCamera() бросает NPE.
            try
            {
                UnityEngine.Object.Instantiate(
                    Auga.Assets.MainMenuPrefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true),
                    __instance.transform);
            }
            catch (Exception e)
            {
                Auga.LogWarning($"PhotoBooth instantiate failed: {e.Message}");
            }

            // OnSelectWorldTab вызывается здесь, когда все поля уже инициализированы.
            try
            {
                __instance.OnSelectWorldTab();
            }
            catch (Exception e)
            {
                Auga.LogWarning($"OnSelectWorldTab failed: {e.Message}");
            }
        }

        // Универсальный авто-фиксер мёртвых и null-полей.
        //
        // Проблема: Auga.Replace() уничтожает оригинальные Unity-объекты, но у FejdStartup
        // есть поля, которые на них ссылались. В Unity 6 доступ к .gameObject на destroyed-
        // компоненте бросает NPE. При обновлении Valheim появляются новые такие поля.
        //
        // Решение: рефлексией перебираем все поля FejdStartup. Если поле:
        //   - имеет тип Component (или его наследник)  → stub GO + AddComponent нужного типа
        //   - имеет тип GameObject                     → пустой stub GO
        //   - содержит dead ИЛИ null ссылку            → обрабатываем; живые пропускаем
        //
        // Вызывается ДВАЖДЫ:
        //   1. В конце Prefix — ДО ванильного Awake, чтобы m_crossplayServerToggle,
        //      m_serverOptions, m_menuList, m_characterSelectScreen и др. были живыми
        //      когда ванильный код к ним обращается.
        //   2. В конце Postfix — после наших назначений, чтобы добить оставшиеся null/dead.
        //
        // Каждое поле получает свой GO → решает "один Selectable на объект" для Button/Toggle.
        private static void FixDeadFields(FejdStartup instance)
        {
            var bindFlags = System.Reflection.BindingFlags.Instance
                          | System.Reflection.BindingFlags.NonPublic
                          | System.Reflection.BindingFlags.Public;

            foreach (var field in typeof(FejdStartup).GetFields(bindFlags))
            {
                var fieldType = field.FieldType;
                bool isComponent = typeof(Component).IsAssignableFrom(fieldType);
                bool isGameObject = fieldType == typeof(GameObject);

                if (!isComponent && !isGameObject) continue;

                var val = field.GetValue(instance) as UnityEngine.Object;

                // Живой объект → пропускаем
                if (val != null && (bool)val) continue;

                // null ИЛИ dead → нужен stub
                var stub = new GameObject($"_AugaStub_{field.Name}");
                stub.SetActive(false);
                stub.transform.SetParent(instance.transform, false);

                if (isGameObject)
                {
                    field.SetValue(instance, stub);
                    Auga.LogWarning($"FixDeadFields: dummy GO for {field.Name}");
                }
                else
                {
                    try
                    {
                        // TMP_Text — абстрактный класс, AddComponent(TMP_Text) падает.
                        // Используем конкретный наследник TextMeshProUGUI.
                        // Аналогично для других абстрактных Component-типов.
                        var addType = fieldType;
                        if (fieldType == typeof(TMPro.TMP_Text) || (fieldType.IsAbstract && typeof(Component).IsAssignableFrom(fieldType)))
                            addType = typeof(TMPro.TextMeshProUGUI);

                        var comp = stub.AddComponent(addType);
                        field.SetValue(instance, comp);
                        Auga.LogWarning($"FixDeadFields: stubbed {field.Name} ({fieldType.Name} → {addType.Name})");
                    }
                    catch (Exception ex)
                    {
                        // AddComponent не поддерживает специальные требования → уничтожаем stub.
                        // Код использующий это поле защищён SafeHide/null-check.
                        UnityEngine.Object.Destroy(stub);
                        Auga.LogWarning($"FixDeadFields: cannot stub {field.Name} ({fieldType.Name}): {ex.Message}");
                    }
                }
            }
        }

        private static void SetButtonListener(Transform root, string childName, UnityAction listener)
        {
            var t = root.Find(childName);
            if (t == null) { Auga.LogWarning($"SetButtonListener: path not found: {childName}"); return; }
            var button = t.GetComponent<Button>();
            if (button == null) { Auga.LogWarning($"SetButtonListener: no Button on: {childName}"); return; }
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }

        // Находит TMP_Text в кнопке (вложенный prefab) и устанавливает локализационный ключ.
        // Вызывается после Localize() поэтому текст сразу переводится движком.
        private static void SetMenuButtonText(Transform root, string path, string locKey)
        {
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"SetMenuButtonText: path not found: {path}"); return; }
            var tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) { Auga.LogWarning($"SetMenuButtonText: no TMP_Text in: {path}"); return; }
            tmp.text = locKey;
        }

        // Устанавливает текст на лейбле (legacy Text или TMP_Text) — для GradientSlider и подобных
        private static void SetLabelText(Transform root, string path, string locKey)
        {
            var t = root.Find(path);
            if (t == null) return;
            // Пробуем TMP_Text сначала (приоритетнее), потом legacy Text
            var tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = locKey; return; }
            var legacyText = t.GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = locKey;
        }

        private static void SetToggleListener(Transform root, string childName, UnityAction<bool> listener)
        {
            var t = root.Find(childName);
            if (t == null) return;
            var toggle = t.GetComponent<Toggle>();
            if (toggle == null) return;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(listener);
        }
    }

    // HideAll() итерирует по списку полей и вызывает SetActive(false) на каждом.
    // Первый же null/dead объект вызывает NPE → все последующие SetActive не выполняются
    // → часть панелей остаётся видимой. Заменяем метод полностью безопасной версией.
    [HarmonyPatch(typeof(FejdStartup), "HideAll")]
    public static class FejdStartup_HideAll_Patch
    {
        public static bool Prefix(FejdStartup __instance)
        {
            // Безопасный HideAll: Unity operator bool возвращает false для null/dead объектов.
            // Это точная копия полей из FejdStartup.HideAll() (decompiled 0.221.12).
            SafeHide(__instance.m_worldVersionPanel);
            SafeHide(__instance.m_playerVersionPanel);
            SafeHide(__instance.m_newGameVersionPanel);
            SafeHide(__instance.m_loading);
            SafeHide(__instance.m_pleaseWait);
            SafeHide(__instance.m_characterSelectScreen);
            SafeHide(__instance.m_creditsPanel);
            SafeHide(__instance.m_startGamePanel);
            SafeHide(__instance.m_createWorldPanel);
            // m_serverOptions — Component, не GameObject; безопасный доступ через оператор bool
            if (__instance.m_serverOptions)
                __instance.m_serverOptions.gameObject.SetActive(false);
            SafeHide(__instance.m_mainMenu);
            SafeHide(__instance.m_ndaPanel);
            SafeHide(__instance.m_betaText);
            return false; // ванильный HideAll пропускаем
        }

        private static void SafeHide(GameObject go)
        {
            // Unity operator bool возвращает false для C#-null И для destroyed GO
            if (go) go.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "SetupGui")]
    public static class FejdStartup_SetupGui_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "Update")]
    public static class FejdStartup_Update_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowCharacterSelection")]
    public static class FejdStartup_ShowCharacterSelection_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // ShowStartGame: показывает StartGame-панель и инициализирует список миров.
    // В Auga-префабе CanvasGroup находится на дочернем "Panel", а не на корне StartGame.
    // UIGroupHandler может оставить его non-interactable → панель видна, но не кликабельна.
    // Postfix + Finalizer оба вызывают EnsureStartGameInteractable для надёжности.
    // (Если оригинал бросил исключение — Postfix не выполнится, но Finalizer всегда выполнится.)
    [HarmonyPatch(typeof(FejdStartup), "ShowStartGame")]
    public static class FejdStartup_ShowStartGame_Patch
    {
        public static void Postfix(FejdStartup __instance)
            => EnsureStartGameInteractable(__instance);

        public static Exception Finalizer(FejdStartup __instance, Exception __exception)
        {
            EnsureStartGameInteractable(__instance);
            return __exception is NullReferenceException ? null : __exception;
        }

        private static void EnsureStartGameInteractable(FejdStartup instance)
        {
            try
            {
                var startGameGO = instance?.m_startGamePanel;
                if (startGameGO == null || !startGameGO) return;

                // Убеждаемся что корень активен
                if (!startGameGO.activeSelf)
                    startGameGO.SetActive(true);

                // CanvasGroup находится на дочернем "Panel", а не на корне StartGame.
                // UIGroupHandler/анимации могут оставить его non-interactable или alpha=0.
                // Принудительно включаем все CanvasGroup внутри StartGame.
                foreach (var cg in startGameGO.GetComponentsInChildren<CanvasGroup>(true))
                {
                    cg.interactable   = true;
                    cg.blocksRaycasts = true;
                    if (cg.alpha < 0.01f) cg.alpha = 1f;
                }
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[ShowStartGame] CanvasGroup fix failed: {ex.Message}");
            }
        }
    }

    // RefreshWorldSelection вызывается внутри ShowStartGame → UpdateWorldList → может NPE.
    [HarmonyPatch(typeof(FejdStartup), "RefreshWorldSelection")]
    public static class FejdStartup_RefreshWorldSelection_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "UpdateWorldList")]
    public static class FejdStartup_UpdateWorldList_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        private static readonly System.Reflection.FieldInfo s_playerInstanceF =
            typeof(FejdStartup).GetField("m_playerInstance",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.FieldInfo s_profilesF =
            typeof(FejdStartup).GetField("m_profiles",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.FieldInfo s_profileIdxF =
            typeof(FejdStartup).GetField("m_profileIndex",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo s_setupPreview =
            typeof(FejdStartup).GetMethod("SetupCharacterPreview",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Финализер вызывается всегда — и при успехе, и при NPE в ванильном коде.
        public static Exception Finalizer(FejdStartup __instance, Exception __exception)
        {
            // 1. Обновляем Auga character-select UI (портреты и выделение)
            try
            {
                var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
                if (characterSelect != null)
                    characterSelect.UpdateCharacterList();
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[AugaCharacterSelect] UpdateCharacterList failed: {ex.GetType().Name}: {ex.Message}");
            }

            // 2. Если vanilla NPE-нул до SetupCharacterPreview → запускаем превью сами.
            // Это происходит когда m_csName (TMP_Text) или m_csLeftButton/Right — null/stub.
            // После фикса FixDeadFields (TextMeshProUGUI вместо TMP_Text) это не нужно,
            // но оставляем как страховку.
            try
            {
                if (__exception is NullReferenceException && s_setupPreview != null)
                {
                    var playerInst = s_playerInstanceF?.GetValue(__instance) as GameObject;
                    if (playerInst == null)
                    {
                        var profiles = s_profilesF?.GetValue(__instance)
                            as System.Collections.Generic.List<PlayerProfile>;
                        var idx = s_profileIdxF != null ? (int)s_profileIdxF.GetValue(__instance) : 0;
                        var profile = (profiles != null && profiles.Count > 0
                                       && idx >= 0 && idx < profiles.Count)
                            ? profiles[idx] : null;
                        s_setupPreview.Invoke(__instance, new object[] { profile });
                    }
                }
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[UpdateCharacterList] SetupCharacterPreview fallback: {ex.GetType().Name}: {ex.Message}");
            }

            // Подавляем NPE от null-полей (m_csName, m_csLeftButton и т.д.)
            return __exception is NullReferenceException ? null : __exception;
        }
    }

    // OnCharacterNew: показывает NewCharacterPanel и скрывает SelectCharacter.
    // Postfix: явно выставляем CanvasGroup.interactable = true на NewCharacterPanel —
    // UIGroupHandler (из assembly_guiutils) мог оставить его false,
    // из-за чего CharacterName input field не реагирует на клики/ввод.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnCharacterNew))]
    public static class FejdStartup_OnCharacterNew_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            try
            {
                var panel = __instance.m_newCharacterPanel;
                if (panel != null)
                {
                    var cg = panel.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.interactable    = true;
                        cg.blocksRaycasts  = true;
                        cg.alpha           = 1f;
                    }
                }
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[OnCharacterNew] CanvasGroup fix failed: {ex.Message}");
            }
        }

        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnCharacterRemove))]
    public static class FejdStartup_OnCharacterRemove_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // CharacterPortraitsController.Awake: инициализирует камеру для рендера причёсок/бород.
    // GetCamera() использует null-safe проверки для PostProcessing/DepthOfField →
    // в Unity 6 без PP-стека работает корректно (компоненты просто не найдены).
    // Финализер подавляет NPE на случай отсутствия m_mainCamera / m_cameraMarkerCharacter.
    [HarmonyPatch(typeof(AugaUnity.CharacterPortraitsController), "Awake")]
    public static class CharacterPortraitsController_Awake_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // CharacterPortraitsController.Update: рендерит портреты причёсок/бород.
    // NPE если m_playerInstance == null (персонаж ещё не заспавнен). Финализер подавляет.
    [HarmonyPatch(typeof(AugaUnity.CharacterPortraitsController), "Update")]
    public static class CharacterPortraitsController_Update_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // AugaCharacterSelectPhotoBooth: фотографирует персонажей для портретов в списке.
    // GetCamera() — null-safe для PostProcessing в Unity 6. Финализеры на Awake/Start
    // подавляют NPE на случай отсутствия m_mainCamera и сопутствующих ошибок.
    [HarmonyPatch(typeof(AugaUnity.AugaCharacterSelectPhotoBooth), "Awake")]
    public static class AugaCharacterSelectPhotoBooth_Awake_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(AugaUnity.AugaCharacterSelectPhotoBooth), "Start")]
    public static class AugaCharacterSelectPhotoBooth_Start_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
    public static class FejdStartup_OnNewCharacterDone_Patch
    {
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

    // ClearCharacterPreview: Destroy(m_playerInstance) + Instantiate(m_changeEffectPrefab).
    // Если m_changeEffectPrefab == null (не найден в Auga-сцене) → NPE при Instantiate.
    // Финализер подавляет — m_playerInstance будет уничтожен через Destroy до NPE.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ClearCharacterPreview))]
    public static class FejdStartup_ClearCharacterPreview_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // OnCharacterStart: проверяет m_profileIndex < m_profiles.Count — если m_profiles null → NPE.
    // Также вызывает ShowStartGame() → RefreshWorldSelection() — защищены Finalizer'ами выше.
    // Добавляем Finalizer для защиты от непредвиденных NPE + логируем результат.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnCharacterStart))]
    public static class FejdStartup_OnCharacterStart_Patch
    {
        public static Exception Finalizer(FejdStartup __instance, Exception __exception)
        {
            if (__exception != null)
                Auga.LogWarning($"[OnCharacterStart] {__exception.GetType().Name}: {__exception.Message}");
            return __exception is NullReferenceException ? null : __exception;
        }
    }

    // PlayerCustomizaton.Update() обращается к m_selectedHair.text, m_selectedBeard.text
    // и вызывает GetPlayer() → если поля null или GetPlayer() == null → NPE каждый кадр.
    // Финализер подавляет NPE — поля будут постепенно установлены через наш Postfix.
    [HarmonyPatch(typeof(PlayerCustomizaton), "Update")]
    public static class PlayerCustomizaton_Update_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // PlayerCustomizaton.OnEnable() обращается к m_beardPanel.gameObject.SetActive() —
    // если m_beardPanel null → NPE. Финализер подавляет, чтобы не крашило панель.
    [HarmonyPatch(typeof(PlayerCustomizaton), nameof(PlayerCustomizaton.OnEnable))]
    public static class PlayerCustomizaton_OnEnable_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }
}
