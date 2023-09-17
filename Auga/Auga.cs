using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using AugaUnity;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using fastJSON;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public class AugaAssets
    {
        public GameObject AugaLogo;
        public GameObject InventoryScreen;
        public GameObject Hud;
        public Texture2D Cursor;
        public GameObject MenuPrefab;
        public GameObject TextViewerPrefab;
        public GameObject MainMenuPrefab;
        public GameObject BuildHudElement;
        public GameObject SettingsPrefab;
        public GameObject MessageHud;
        public GameObject TextInput;
        public GameObject AugaChat;
        public GameObject DamageText;
        public GameObject EnemyHud;
        public GameObject StoreGui;
        public GameObject WorldListElement;
        public GameObject ServerListElement;
        public GameObject PasswordDialog;
        public GameObject ConnectingDialog;
        public GameObject PanelBase;
        public GameObject ButtonSmall;
        public GameObject ButtonMedium;
        public GameObject ButtonFancy;
        public GameObject ButtonSettings;
        public GameObject DiamondButton;
        public Font SourceSansProBold;
        public Font SourceSansProSemiBold;
        public Font SourceSansProRegular;
        public Sprite ItemBackgroundSprite;
        public GameObject InventoryTooltip;
        public GameObject SimpleTooltip;
        public GameObject DividerSmall;
        public GameObject DividerMedium;
        public GameObject DividerLarge;
        public GameObject ConfirmDialog;
        public Sprite RecyclingPanelIcon;

        public GameObject LeftWristMountUI;
    }

    public class AugaColors
    {
        public string BrightestGold = "#FFBF1B";
        public string Topic = "#EAA800";
        public string Emphasis = "#1AACEF";
        public Color Healing = new Color(0.5f, 1.0f, 0.5f, 0.7f);
        public Color PlayerDamage = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        public Color PlayerNoDamage = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color NormalDamage = new Color(1f, 1f, 1f, 1f);
        public Color ResistDamage = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color WeakDamage = new Color(1f, 1f, 0.0f, 1f);
        public Color ImmuneDamage = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color TooHard = new Color(0.8f, 0.7f, 0.7f, 1f);
    }

    [BepInPlugin(PluginID, "Project Auga", Version)]
    [BepInDependency("Menthus.bepinex.plugins.BetterTrader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("maximods.valheim.multicraft", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.github.abearcodes.valheim.simplerecycling", BepInDependency.DependencyFlags.SoftDependency)]
    public class Auga : BaseUnityPlugin
    {
        public const string PluginID = "randyknapp.mods.auga";
        public const string Version = "1.2.15";

        public enum StatBarTextDisplayMode { JustValue, ValueAndMax, ValueMaxPercent, JustPercent }
        public enum StatBarTextPosition { Off = -1, Above, Below, Center, Start, End };

        private static ConfigEntry<bool> _loggingEnabled;
        private static ConfigEntry<LogLevel> _logLevel;
        public static ConfigEntry<bool> UseAugaTrash;

        public static ConfigEntry<bool> HealthBarShow;
        public static ConfigEntry<int> HealthBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> HealthBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> HealthBarTextPosition;
        public static ConfigEntry<bool> HealthBarShowTicks;

        public static ConfigEntry<bool> StaminaBarShow;
        public static ConfigEntry<int> StaminaBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> StaminaBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> StaminaBarTextPosition;
        public static ConfigEntry<bool> StaminaBarShowTicks;

        public static ConfigEntry<bool> EitrBarShow;
        public static ConfigEntry<int> EitrBarFixedSize;
        public static ConfigEntry<StatBarTextDisplayMode> EitrBarTextDisplay;
        public static ConfigEntry<StatBarTextPosition> EitrBarTextPosition;
        public static ConfigEntry<bool> EitrBarShowTicks;
        
        public static ConfigEntry<bool> BuildMenuShow;
        public static ConfigEntry<bool> AugaChatShow;

        public static readonly AugaAssets Assets = new AugaAssets();
        public static readonly AugaColors Colors = new AugaColors();

        public static bool HasBetterTrader;
        public static bool HasMultiCraft;
        public static bool HasSimpleRecycling;
        public static bool HasChatter;
        public static bool HasSearsCatalog;

        private static Auga _instance;
        private Harmony _harmony;
        private static Type _multiCraftUiType;
        private static Type _recyclingContainerButtonHolderType;
        private static Type _recyclingStationButtonHolderType;
        private static WorkbenchTabData _recyclingTabData;

        public static Auga instance => _instance;

        public void Awake()
        {
            _instance = this;
            if (int.TryParse(Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.')[3],out var revision))
            {
                if (revision > 0)
                {
                    Debug.LogWarning($"==============================================================================");
                    Debug.LogWarning($"You are using a PTB version of this mod. It will not work in live.");
                    Debug.LogWarning($"Project Auga - Version {Assembly.GetExecutingAssembly().GetName().Version}");
                    Debug.LogWarning($"Valheim - Version {(global::Version.GetVersionString())}");

                    if ((global::Version.CurrentVersion.m_minor == 217 && global::Version.CurrentVersion.m_patch >= 5 ) || global::Version.CurrentVersion.m_minor > 217)
                    {
                        Debug.LogWarning($"GAME VERSION CHECK - PASSED");
                        Debug.LogWarning($"==============================================================================");
                    }
                    else
                    {
                        Debug.LogError($">>>>>>>>> GAME VERSION MISMATCH - EXITING <<<<<<<<");
                        Debug.LogWarning($"==============================================================================");
                        Thread.Sleep(10000);
                        
                        Destroy(this);
                        return;
                    }
                }
            }
            
            
            LoadDependencies();
            LoadTranslations();
            LoadConfig();
            LoadAssets();

            ApplyCursor();

            HasBetterTrader = Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader");
            HasMultiCraft  = Chainloader.PluginInfos.TryGetValue("maximods.valheim.multicraft", out var multiCraftPlugin);
            HasSimpleRecycling  = Chainloader.PluginInfos.TryGetValue("com.github.abearcodes.valheim.simplerecycling", out var recyclingPlugin);
            HasChatter = Chainloader.PluginInfos.ContainsKey("redseiko.valheim.chatter");
            HasSearsCatalog = Chainloader.PluginInfos.ContainsKey("redseiko.valheim.searscatalog");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginID);

            // Patch MultiCraft_UI.CreateSpaceFromCraftButton
            
            if (HasMultiCraft)
            {
                var multiCraftPluginType = Assembly.LoadFile(multiCraftPlugin.Location);
                _multiCraftUiType = multiCraftPluginType.GetType("MultiCraft.MultiCraft_UI");
                var multicraftLogicType = multiCraftPluginType.GetType("MultiCraft.MultiCraft_Logic");
                var createCraftButtonSpaceMethod = AccessTools.Method(_multiCraftUiType, "CreateSpaceFromCraftButton");
                var isCraftingMethod = AccessTools.Method(multicraftLogicType, "IsCrafting");
                if (createCraftButtonSpaceMethod != null)
                {
                    _harmony.Patch(createCraftButtonSpaceMethod, new HarmonyMethod(typeof(Auga), nameof(MultiCraft_UI_CreateSpaceFromCraftButton_Patch)));
                    _harmony.Patch(isCraftingMethod, new HarmonyMethod(typeof(Auga), nameof(MultiCraft_Logic_IsCrafting_Patch)));
                }
            }

            // Patch Simple Recycling

            if (HasSimpleRecycling)
            {
                var pluginType = Assembly.LoadFile(recyclingPlugin.Location);
                _recyclingContainerButtonHolderType = pluginType.GetType("ABearCodes.Valheim.SimpleRecycling.UI.ContainerRecyclingButtonHolder");
                _recyclingStationButtonHolderType = pluginType.GetType("ABearCodes.Valheim.SimpleRecycling.UI.StationRecyclingTabHolder");
                var containerButtonMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "SetupButton");
                var tabButtonMethod = AccessTools.Method(_recyclingStationButtonHolderType, "SetupTabButton");
                var setActiveMethod = AccessTools.Method(_recyclingStationButtonHolderType, "SetActive");
                var inRecycleTabMethod = AccessTools.Method(_recyclingStationButtonHolderType, "InRecycleTab");
                if (containerButtonMethod != null)
                    _harmony.Patch(containerButtonMethod, new HarmonyMethod(typeof(Auga), nameof(SimpleRecycling_ContainerRecyclingButtonHolder_SetupButton_Patch)));
                if (tabButtonMethod != null)
                    _harmony.Patch(tabButtonMethod, new HarmonyMethod(typeof(Auga), nameof(SimpleRecycling_StationRecyclingTabHolder_SetupTabButton_Patch)));
                if (setActiveMethod != null)
                    _harmony.Patch(setActiveMethod, new HarmonyMethod(typeof(Auga), nameof(SimpleRecycling_StationRecyclingTabHolder_SetActive_Patch)));
                if (inRecycleTabMethod != null)
                    _harmony.Patch(inRecycleTabMethod, new HarmonyMethod(typeof(Auga), nameof(SimpleRecycling_StationRecyclingTabHolder_InRecycleTab_Patch)));
            }
        }

        public static bool MultiCraft_UI_CreateSpaceFromCraftButton_Patch(InventoryGui instance)
        {
            var multiCraftUiInstance = AccessTools.Method(_multiCraftUiType, "get_instance").Invoke(null, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty, null, new object[] { }, CultureInfo.InvariantCulture);
            
            var plusButton = instance.m_craftButton.transform.parent.Find("plus");
            var plusButtonMethod = AccessTools.Method(_multiCraftUiType, "OnPlusButtonPressed");
            plusButton.GetComponent<Button>().onClick.AddListener(() => plusButtonMethod.Invoke(multiCraftUiInstance, new object[]{}));

            var minusButton = instance.m_craftButton.transform.parent.Find("minus");
            var minusButtonMethod = AccessTools.Method(_multiCraftUiType, "OnMinusButtonPressed");
            minusButton.GetComponent<Button>().onClick.AddListener(() => minusButtonMethod.Invoke(multiCraftUiInstance, new object[] { }));

            return false;
        }

        public static bool MultiCraft_Logic_IsCrafting_Patch(ref bool __result)
        {
            __result = InventoryGui.instance.m_craftTimer >= 0;
            return false;
        }

        public static bool SimpleRecycling_ContainerRecyclingButtonHolder_SetupButton_Patch()
        {
            var recycleAllButtonGO = InventoryGui.instance.m_container.Find("RecycleAll").gameObject;

            var onRecycleAllPressedMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "OnRecycleAllPressed");
            var setButtonStateMethod = AccessTools.Method(_recyclingContainerButtonHolderType, "SetButtonState");
            var recycleAllButtonFieldRef = AccessTools.FieldRefAccess<Button>(_recyclingContainerButtonHolderType, "_recycleAllButton");
            var textComponentFieldRef = AccessTools.FieldRefAccess<Text>(_recyclingContainerButtonHolderType, "_textComponent");
            var imageComponentFieldRef = AccessTools.FieldRefAccess<Image>(_recyclingContainerButtonHolderType, "_imageComponent");

            var component = _instance.gameObject.GetComponent("ContainerRecyclingButtonHolder");
            var recycleAllButton = recycleAllButtonGO.GetComponent<Button>();
            recycleAllButtonFieldRef(component) = recycleAllButton;
            recycleAllButton.onClick.RemoveAllListeners();
            recycleAllButton.onClick.AddListener(() => { onRecycleAllPressedMethod.Invoke(component, new object[]{}); });

            textComponentFieldRef(component) = recycleAllButton.GetComponentInChildren<Text>();
            imageComponentFieldRef(component) = recycleAllButton.GetComponentInChildren<Image>();
            setButtonStateMethod.Invoke(component, new object[] { false });

            return false;
        }

        public static bool SimpleRecycling_StationRecyclingTabHolder_SetupTabButton_Patch()
        {
            var recyclingTabButtonFieldRef = AccessTools.FieldRefAccess<Button>(_recyclingStationButtonHolderType, "_recyclingTabButtonComponent");
            var recyclingTabButtonGOFieldRef = AccessTools.FieldRefAccess<GameObject>(_recyclingStationButtonHolderType, "_recyclingTabButtonGameObject");
            var updateCraftingPanelMethod = AccessTools.Method(_recyclingStationButtonHolderType, "UpdateCraftingPanel");
            var component = _instance.gameObject.GetComponent("StationRecyclingTabHolder");

            _recyclingTabData = API.Workbench_AddVanillaWorkbenchTab("RECYCLE", Assets.RecyclingPanelIcon, "Recycle", (_) =>
            {
                updateCraftingPanelMethod.Invoke(component, new object[] { });
            });
            recyclingTabButtonFieldRef(component) = _recyclingTabData.TabButtonGO.GetComponent<Button>();
            recyclingTabButtonGOFieldRef(component) = _recyclingTabData.TabButtonGO;

            return false;
        }

        public static bool SimpleRecycling_StationRecyclingTabHolder_SetActive_Patch()
        {
            return false;
        }

        public static bool SimpleRecycling_StationRecyclingTabHolder_InRecycleTab_Patch(ref bool __result)
        {
            __result = WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActiveById("RECYCLE");
            return false;
        }

        public void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _instance = null;
        }

        private void LoadDependencies()
        {
            var assembly = Assembly.GetCallingAssembly();
            LoadEmbeddedAssembly(assembly, "fastJSON.dll");
            LoadEmbeddedAssembly(assembly, "Unity.Auga.dll");
        }

        private static void LoadEmbeddedAssembly(Assembly assembly, string assemblyName)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{assembly.GetName().Name}.{assemblyName}");
            if (stream == null)
            {
                LogError($"Could not load embedded assembly ({assemblyName})!");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        private static void LoadTranslations()
        {
            var translationsJsonText = LoadJsonText("translations.json");
            if (string.IsNullOrEmpty(translationsJsonText))
            {
                return;
            }

            var translations = (IDictionary<string, object>)JSON.Parse(translationsJsonText);
            foreach (var translation in translations)
            {
                if (!string.IsNullOrEmpty(translation.Key) && !string.IsNullOrEmpty(translation.Value.ToString()))
                {
                    Localization.instance.AddWord(translation.Key, translation.Value.ToString());
                }
            }
        }

        private void LoadConfig()
        {
            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", false, "Enable logging");
            _logLevel = Config.Bind("Logging", "LogLevel", LogLevel.Info, "Only log messages of the selected level or higher");
            UseAugaTrash = Config.Bind("Options", "UseAugaTrash", false, "Enable Auga's built in trash button. Click on the button while holding an item or part of a stack with the mouse.");
            
            HealthBarShow = Config.Bind("StatBars", "HealthBarShow", true, "If false, hides the health bar completely.");
            HealthBarFixedSize = Config.Bind("StatBars", "HealthBarFixedSize", 0, "If greater than 0, forces the health bar to be that many pixels long, regardless of the player's max health.");
            HealthBarTextDisplay = Config.Bind("StatBars", "HealthBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the health bar is displayed.");
            HealthBarTextPosition = Config.Bind("StatBars", "HealthBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the health bar is displayed.");
            HealthBarShowTicks = Config.Bind("StatBars", "HealthBarShowTicks", true, "Show a faint line on the bar every 25 units");

            StaminaBarShow = Config.Bind("StatBars", "StaminaBarShow", true, "If false, hides the stamina bar completely.");
            StaminaBarFixedSize = Config.Bind("StatBars", "StaminaBarFixedSize", 0, "If greater than 0, forces the stamina bar to be that many pixels long, regardless of the player's max stamina.");
            StaminaBarTextDisplay = Config.Bind("StatBars", "StaminaBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the stamina bar is displayed.");
            StaminaBarTextPosition = Config.Bind("StatBars", "StaminaBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the stamina bar is displayed.");
            StaminaBarShowTicks = Config.Bind("StatBars", "StaminaBarShowTicks", true, "Show a faint line on the bar every 25 units");

            EitrBarShow = Config.Bind("StatBars", "EitrBarShow", true, "If false, hides the eitr bar completely.");
            EitrBarFixedSize = Config.Bind("StatBars", "EitrBarFixedSize", 0, "If greater than 0, forces the eitr bar to be that many pixels long, regardless of the player's max eitr. Eitr bar still hides if max eitr is zero.");
            EitrBarTextDisplay = Config.Bind("StatBars", "EitrBarTextDisplay", StatBarTextDisplayMode.JustValue, "Changes how the label of the eitr bar is displayed.");
            EitrBarTextPosition = Config.Bind("StatBars", "EitrBarTextPosition", StatBarTextPosition.Center, "Changes where the label of the eitr bar is displayed.");
            EitrBarShowTicks = Config.Bind("StatBars", "Eitr", true, "Show a faint line on the bar every 25 units");
            
            BuildMenuShow = Config.Bind("BuildMenu", "Use Auga Build Menu (Requires Restart)", true, "If false, disables the Auga Build Menu display");
            AugaChatShow = Config.Bind("AugaChat", "Show Auga Chat. Disable to use other mods. (Requires Restart)", true, "If false, disables the Auga Chat window display");
        }

        private static void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("augaassets");
            Assets.AugaLogo = assetBundle.LoadAsset<GameObject>("AugaLogo");
            Assets.InventoryScreen = assetBundle.LoadAsset<GameObject>("Inventory_screen");
            Assets.Cursor = assetBundle.LoadAsset<Texture2D>("Cursor2");
            Assets.MenuPrefab = assetBundle.LoadAsset<GameObject>("AugaMenu");
            Assets.TextViewerPrefab = assetBundle.LoadAsset<GameObject>("AugaTextViewer");
            Assets.Hud = assetBundle.LoadAsset<GameObject>("HUD");
            Assets.MainMenuPrefab = assetBundle.LoadAsset<GameObject>("MainMenu");
            Assets.BuildHudElement = assetBundle.LoadAsset<GameObject>("BuildHudElement");
            Assets.SettingsPrefab = assetBundle.LoadAsset<GameObject>("AugaSettings");
            Assets.MessageHud = assetBundle.LoadAsset<GameObject>("AugaMessageHud");
            Assets.TextInput = assetBundle.LoadAsset<GameObject>("AugaTextInput");
            Assets.AugaChat = assetBundle.LoadAsset<GameObject>("AugaChat");
            Assets.DamageText = assetBundle.LoadAsset<GameObject>("AugaDamageText");
            Assets.EnemyHud = assetBundle.LoadAsset<GameObject>("AugaEnemyHud");
            Assets.StoreGui = assetBundle.LoadAsset<GameObject>("AugaStoreScreen");
            Assets.WorldListElement = assetBundle.LoadAsset<GameObject>("WorldListElement");
            Assets.ServerListElement = assetBundle.LoadAsset<GameObject>("ServerListElement");
            Assets.PasswordDialog = assetBundle.LoadAsset<GameObject>("AugaPassword");
            Assets.ConnectingDialog = assetBundle.LoadAsset<GameObject>("AugaConnecting");
            Assets.PanelBase = assetBundle.LoadAsset<GameObject>("AugaPanelBase");
            Assets.ButtonSmall = assetBundle.LoadAsset<GameObject>("ButtonSmall");
            Assets.ButtonMedium = assetBundle.LoadAsset<GameObject>("ButtonMedium");
            Assets.ButtonFancy = assetBundle.LoadAsset<GameObject>("ButtonFancy");
            Assets.ButtonSettings = assetBundle.LoadAsset<GameObject>("ButtonSettings");
            Assets.DiamondButton = assetBundle.LoadAsset<GameObject>("DiamondButton");
            Assets.SourceSansProBold = assetBundle.LoadAsset<Font>("SourceSansPro-Bold");
            Assets.SourceSansProSemiBold = assetBundle.LoadAsset<Font>("SourceSansPro-SemiBold");
            Assets.SourceSansProRegular = assetBundle.LoadAsset<Font>("SourceSansPro-Regular");
            Assets.ItemBackgroundSprite = assetBundle.LoadAsset<Sprite>("Container_Square_A");
            Assets.InventoryTooltip = assetBundle.LoadAsset<GameObject>("InventoryTooltip");
            Assets.SimpleTooltip = assetBundle.LoadAsset<GameObject>("SimpleTooltip");
            Assets.DividerSmall = assetBundle.LoadAsset<GameObject>("DividerSmall");
            Assets.DividerMedium = assetBundle.LoadAsset<GameObject>("DividerMedium");
            Assets.DividerLarge = assetBundle.LoadAsset<GameObject>("DividerLarge");
            Assets.ConfirmDialog = assetBundle.LoadAsset<GameObject>("ConfirmDialog");
            Assets.RecyclingPanelIcon = assetBundle.LoadAsset<Sprite>("RecyclingPanel");
            Assets.LeftWristMountUI = assetBundle.LoadAsset<GameObject>("LeftWristMountUI");
        }

        private static void ApplyCursor()
        {
            Cursor.SetCursor(Assets.Cursor, new Vector2(6, 5), CursorMode.Auto);
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            // Optionally load asset bundle from path, if it exists
            var assetBundlePath = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        public static string LoadJsonText(string filename)
        {
            var jsonFileName = GetAssetPath(filename);
            return !string.IsNullOrEmpty(jsonFileName) ? File.ReadAllText(jsonFileName) : null;
        }

        public static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "Auga", assetName);
            if (!File.Exists(assetFileName))
            {
                var assembly = typeof(Auga).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
                if (!File.Exists(assetFileName))
                {
                    LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Info)
            {
                _instance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Warning)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Error)
            {
                _instance.Logger.LogError(message);
            }
        }

        [UsedImplicitly]
        public void Update()
        {
            UpdateStatBars();
        }

        public static void UpdateStatBars()
        {
            if (Hud.instance != null)
            {
                var newHealthPanel = Hud.instance.transform.Find("hudroot/HealthBar");
                var newStaminaPanel = Hud.instance.transform.Find("hudroot/StaminaBar");
                var newEitrPanel = Hud.instance.transform.Find("hudroot/EitrBar");

                if (newHealthPanel != null && newHealthPanel.GetComponent<AugaHealthBar>() is AugaHealthBar healthBar)
                {
                    healthBar.Hide = !HealthBarShow.Value;
                    healthBar.FixedLength = Auga.HealthBarFixedSize.Value;
                    healthBar.TextDisplay = (AugaHealthBar.TextDisplayMode)Auga.HealthBarTextDisplay.Value;
                    healthBar.DisplayTextPosition = (AugaHealthBar.TextPosition)Auga.HealthBarTextPosition.Value;
                    healthBar.ShowTicks = HealthBarShowTicks.Value;
                }

                if (newStaminaPanel != null && newStaminaPanel.GetComponent<AugaHealthBar>() is AugaHealthBar staminaBar)
                {
                    staminaBar.Hide = !StaminaBarShow.Value;
                    staminaBar.FixedLength = Auga.StaminaBarFixedSize.Value;
                    staminaBar.TextDisplay = (AugaHealthBar.TextDisplayMode)Auga.StaminaBarTextDisplay.Value;
                    staminaBar.DisplayTextPosition = (AugaHealthBar.TextPosition)Auga.StaminaBarTextPosition.Value;
                    staminaBar.ShowTicks = StaminaBarShowTicks.Value;
                }

                if (newEitrPanel != null && newEitrPanel.GetComponent<AugaHealthBar>() is AugaHealthBar eitrBar)
                {
                    eitrBar.Hide = !EitrBarShow.Value;
                    eitrBar.FixedLength = Auga.EitrBarFixedSize.Value;
                    eitrBar.TextDisplay = (AugaHealthBar.TextDisplayMode)Auga.EitrBarTextDisplay.Value;
                    eitrBar.DisplayTextPosition = (AugaHealthBar.TextPosition)Auga.EitrBarTextPosition.Value;
                    eitrBar.ShowTicks = EitrBarShowTicks.Value;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_InitTerminal_Patch
    {
        public static void Postfix()
        {
            new Terminal.ConsoleCommand("resetbiomes", "", args =>
            {
                var t = typeof(Player).GetField(nameof(Player.m_knownBiome),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                t.SetValue(Player.m_localPlayer,new HashSet<Heightmap.Biome>());
            });
        }
    }
}

