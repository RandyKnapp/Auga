using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // Every prefab, sprite and font Auga uses, loaded once from the augaassets bundle (Auga.Assets). The names passed
    // to LoadAsset must match the names in the bundle: a renamed prefab silently becomes null.
    public class AugaAssets
    {
        public GameObject AugaLogo;
        public GameObject InventoryScreen;
        public GameObject Hud;
        public Texture2D Cursor;
        public GameObject MenuPrefab;
        public GameObject TextViewerPrefab;
        public GameObject MainMenuPrefab;
        public GameObject SelectCharacterPrefab;
        public GameObject NewCharacterPanelPrefab;
        public GameObject StartGamePrefab;
        public Sprite ServerStatusOnline;
        public Sprite ServerStatusOffline;
        public Sprite ServerStatusRefresh;
        public Sprite ServerStatusUnknown;
        public GameObject BuildHudElement;
        public GameObject SettingsPrefab;
        public GameObject MessageHud;
        public GameObject TextInput;
        public GameObject AugaBarber;
        public GameObject AugaChat;
        public GameObject DamageText;
        public GameObject EnemyHud;
        public GameObject StoreGui;
        public GameObject WorldListElement;
        public GameObject ServerListElement;
        public GameObject PasswordDialog;
        public GameObject ConnectingDialog;
        public GameObject LabeledDropdown;
        public GameObject LabeledCheckbox;
        public GameObject LabeledSliderWithValue;
        public GameObject LabeledKeybind;
        public GameObject LanguageTooltip;
        public GameObject MenuButtonSmall;
        public GameObject ChangeLogPrefab;
        public GameObject AugaLogoSmall;
        public GameObject ScrollBar;
        public GameObject PanelBase;
        public GameObject ButtonSmall;
        public GameObject ButtonMedium;
        public GameObject ButtonFancy;
        public GameObject ButtonToggle;
        public GameObject ButtonSettings;
        public GameObject DiamondButton;
        public Font SourceSansProBold;
        public Font SourceSansProSemiBold;
        public Font SourceSansProRegular;
        public Sprite ItemBackgroundSprite;
        public Sprite ContainerDiamond;
        public GameObject InventoryTooltip;
        public GameObject SimpleTooltip;
        public GameObject DividerSmall;
        public GameObject DividerMedium;
        public GameObject DividerLarge;
        public GameObject ConfirmDialog;
        public Sprite RecyclingPanelIcon;
        public GameObject BuildHud;
        public GameObject LeftWristMountUI;

        internal void Load(AssetBundle assetBundle)
        {
            AugaLogo = assetBundle.LoadAsset<GameObject>("AugaLogo");
            InventoryScreen = assetBundle.LoadAsset<GameObject>("Inventory_screen");
            Cursor = assetBundle.LoadAsset<Texture2D>("Cursor2");
            MenuPrefab = assetBundle.LoadAsset<GameObject>("AugaMenu");
            TextViewerPrefab = assetBundle.LoadAsset<GameObject>("AugaTextViewer");
            Hud = assetBundle.LoadAsset<GameObject>("HUD");
            MainMenuPrefab = assetBundle.LoadAsset<GameObject>("MainMenu");
            SelectCharacterPrefab = assetBundle.LoadAsset<GameObject>("SelectCharacter");
            NewCharacterPanelPrefab = assetBundle.LoadAsset<GameObject>("NewCharacterPanel");
            StartGamePrefab = assetBundle.LoadAsset<GameObject>("StartGame");
            ServerStatusOnline = assetBundle.LoadAsset<Sprite>("status");
            ServerStatusOffline = assetBundle.LoadAsset<Sprite>("status_no");
            ServerStatusRefresh = assetBundle.LoadAsset<Sprite>("status_refresh");
            ServerStatusUnknown = assetBundle.LoadAsset<Sprite>("status_unknown");
            BuildHudElement = assetBundle.LoadAsset<GameObject>("BuildHudElement");
            SettingsPrefab = assetBundle.LoadAsset<GameObject>("AugaSettings");
            MessageHud = assetBundle.LoadAsset<GameObject>("AugaMessageHud");
            TextInput = assetBundle.LoadAsset<GameObject>("AugaTextInput");
            AugaBarber = assetBundle.LoadAsset<GameObject>("AugaBarber");
            AugaChat = assetBundle.LoadAsset<GameObject>("AugaChat");
            DamageText = assetBundle.LoadAsset<GameObject>("AugaDamageText");
            EnemyHud = assetBundle.LoadAsset<GameObject>("AugaEnemyHud");
            StoreGui = assetBundle.LoadAsset<GameObject>("AugaStoreScreen");
            WorldListElement = assetBundle.LoadAsset<GameObject>("WorldListElement");
            ServerListElement = assetBundle.LoadAsset<GameObject>("ServerListElement");
            PasswordDialog = assetBundle.LoadAsset<GameObject>("AugaPassword");
            ConnectingDialog = assetBundle.LoadAsset<GameObject>("AugaConnecting");
            LabeledDropdown = assetBundle.LoadAsset<GameObject>("LabeledDropdown");
            LabeledCheckbox = assetBundle.LoadAsset<GameObject>("LabeledCheckbox");
            LabeledSliderWithValue = assetBundle.LoadAsset<GameObject>("LabeledSliderWithValue");
            LabeledKeybind = assetBundle.LoadAsset<GameObject>("LabeledKeybind");
            LanguageTooltip = assetBundle.LoadAsset<GameObject>("LanguageTooltip");
            MenuButtonSmall = assetBundle.LoadAsset<GameObject>("MenuButtonSmall");
            ChangeLogPrefab = assetBundle.LoadAsset<GameObject>("ChangeLog");
            AugaLogoSmall = assetBundle.LoadAsset<GameObject>("AugaLogoSmall");
            ScrollBar = assetBundle.LoadAsset<GameObject>("ScrollBar");
            PanelBase = assetBundle.LoadAsset<GameObject>("AugaPanelBase");
            ButtonSmall = assetBundle.LoadAsset<GameObject>("ButtonSmall");
            ButtonMedium = assetBundle.LoadAsset<GameObject>("ButtonMedium");
            ButtonFancy = assetBundle.LoadAsset<GameObject>("ButtonFancy");
            ButtonToggle = assetBundle.LoadAsset<GameObject>("ButtonToggle");
            ButtonSettings = assetBundle.LoadAsset<GameObject>("ButtonSettings");
            DiamondButton = assetBundle.LoadAsset<GameObject>("DiamondButton");
            SourceSansProBold = assetBundle.LoadAsset<Font>("SourceSansPro-Bold");
            SourceSansProSemiBold = assetBundle.LoadAsset<Font>("SourceSansPro-SemiBold");
            SourceSansProRegular = assetBundle.LoadAsset<Font>("SourceSansPro-Regular");
            ItemBackgroundSprite = assetBundle.LoadAsset<Sprite>("Container_Square_A");
            ContainerDiamond = assetBundle.LoadAsset<Sprite>("Container_Diamond");
            InventoryTooltip = assetBundle.LoadAsset<GameObject>("InventoryTooltip");
            SimpleTooltip = assetBundle.LoadAsset<GameObject>("SimpleTooltip");
            DividerSmall = assetBundle.LoadAsset<GameObject>("DividerSmall");
            DividerMedium = assetBundle.LoadAsset<GameObject>("DividerMedium");
            DividerLarge = assetBundle.LoadAsset<GameObject>("DividerLarge");
            ConfirmDialog = assetBundle.LoadAsset<GameObject>("ConfirmDialog");
            RecyclingPanelIcon = assetBundle.LoadAsset<Sprite>("RecyclingPanel");
            LeftWristMountUI = assetBundle.LoadAsset<GameObject>("LeftWristMountUI");
            BuildHud = assetBundle.LoadAsset<GameObject>("BuildHud");

            // The Auga buttons carry a click sfx and a "select" sfx. ButtonSfx now plays the select sfx from
            // OnSelect, which a mouse triggers on pointer down, so every click sounded twice (down and up).
            // Strip the select sfx from every prefab asset in the bundle (the tab buttons are stand-alone
            // prefabs instantiated by the tab controllers); every instance made from them inherits that.
            var prefabs = assetBundle.LoadAllAssets<GameObject>();
            RemoveSelectSfx(prefabs);
            WarnAboutClassicTexts(prefabs);
        }

        /// <summary>
        /// Auga is TextMeshPro only: every bundled prefab that still carries a classic UnityEngine.UI.Text is reported
        /// (to the Unity log, so it shows regardless of Auga's own logging setting) so it can be fixed in Unity.
        /// </summary>
        private static void WarnAboutClassicTexts(params GameObject[] prefabs)
        {
            var offenders = 0;
            foreach (var prefab in prefabs)
            {
                if (prefab == null || prefab.transform.parent != null)
                    continue;
                var texts = prefab.GetComponentsInChildren<Text>(true);
                if (texts.Length == 0)
                    continue;
                offenders++;
                var paths = new List<string>();
                foreach (var text in texts)
                {
                    if (paths.Count >= 8) { paths.Add("..."); break; }
                    var path = text.name;
                    for (var t = text.transform.parent; t != null && t != prefab.transform; t = t.parent)
                        path = t.name + "/" + path;
                    paths.Add(path);
                }
                Debug.LogWarning($"[Auga] Prefab '{prefab.name}' uses classic UI Text on {texts.Length} object(s); convert them to TextMeshPro: {string.Join(", ", paths)}");
            }
            if (offenders > 0)
                Debug.LogWarning($"[Auga] {offenders} bundled prefab(s) still use classic UI Text (see the warnings above).");
        }

        private static void RemoveSelectSfx(params GameObject[] prefabs)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;
                foreach (var sfx in prefab.GetComponentsInChildren<ButtonSfx>(true))
                {
                    sfx.m_selectSfxPrefab = null;
                    sfx.m_selectSfxPrefabVibrationOnly = null;
                }
            }
        }
    }
}
