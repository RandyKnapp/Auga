using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Hud))]
    public static class Hud_Setup
    {
        [HarmonyPatch(nameof(Hud.Awake))]
        [HarmonyPostfix]
        public static void Hud_Awake_Postfix(Hud __instance)
        {
            __instance.Replace("hudroot/HotKeyBar", Auga.Assets.Hud, "hudroot/HotKeyBar");

            __instance.m_statusEffectListRoot = null;
            __instance.m_statusEffectTemplate = new GameObject("DummyStatusEffectTemplate", typeof(RectTransform)).RectTransform();
            __instance.Replace("hudroot/StatusEffects", Auga.Assets.Hud);

            __instance.m_saveIcon = __instance.Replace("hudroot/SaveIcon", Auga.Assets.Hud).gameObject;
            __instance.m_badConnectionIcon = __instance.Replace("hudroot/BadConnectionIcon", Auga.Assets.Hud).gameObject;

            var originalDreamTexts = __instance.m_sleepingProgress.GetComponent<SleepText>().m_dreamTexts;
            var loadingScreen = __instance.Replace("LoadingBlack", Auga.Assets.Hud);
            __instance.m_loadingScreen = loadingScreen.GetComponent<CanvasGroup>();
            __instance.m_loadingProgress = loadingScreen.Find("Loading").gameObject;
            __instance.m_sleepingProgress = loadingScreen.Find("Sleeping").gameObject;
            __instance.m_teleportingProgress = loadingScreen.Find("Teleporting").gameObject;
            __instance.m_loadingImage = loadingScreen.Find("Loading/Image").GetComponent<Image>();
            __instance.m_loadingTip = loadingScreen.Find("Loading/Tip").GetComponent<Text>();
            __instance.m_sleepingProgress.GetComponent<SleepText>().m_dreamTexts = originalDreamTexts;

            var minimap = __instance.GetComponentInChildren<Minimap>();
            var originalMiniMapMaterial = minimap.m_mapImageSmall.material;

            var newMiniMap = __instance.Replace("hudroot/MiniMap/small", Auga.Assets.Hud);
            minimap.m_smallRoot = newMiniMap.gameObject;
            minimap.m_mapImageSmall = newMiniMap.GetComponentInChildren<RawImage>();
            minimap.m_mapImageSmall.material = originalMiniMapMaterial;
            minimap.m_pinRootSmall = (RectTransform)newMiniMap.Find("map/pin_root");
            minimap.m_biomeNameSmall = newMiniMap.Find("biome/Content").GetComponent<Text>();
            minimap.m_smallShipMarker = (RectTransform)newMiniMap.Find("map/ship_marker");
            minimap.m_smallMarker = (RectTransform)newMiniMap.Find("map/player_marker");
            minimap.m_windMarker = (RectTransform)newMiniMap.Find("WindIndicator");

            __instance.m_eventBar = __instance.Replace("hudroot/EventBar", Auga.Assets.Hud).gameObject;
            __instance.m_eventName = __instance.m_eventBar.GetComponentInChildren<Text>();

            __instance.m_damageScreen = __instance.Replace("hudroot/Damaged", Auga.Assets.Hud).GetComponent<Image>();

            var newCrosshair = __instance.Replace("hudroot/crosshair", Auga.Assets.Hud);
            __instance.m_crosshair = newCrosshair.Find("crosshair").GetComponent<Image>();
            __instance.m_crosshairBow = newCrosshair.Find("crosshair_bow").GetComponent<Image>();
            __instance.m_hoverName = newCrosshair.Find("HoverName").GetComponent<Text>();
            __instance.m_pieceHealthRoot = (RectTransform) newCrosshair.Find("PieceHealthRoot");
            __instance.m_pieceHealthBar = newCrosshair.Find("PieceHealthRoot/PieceHealthBar").GetComponent<GuiBar>();
            __instance.m_targetedAlert = newCrosshair.Find("Sneak/Alert").gameObject;
            __instance.m_targeted = newCrosshair.Find("Sneak/Detected").gameObject;
            __instance.m_hidden = newCrosshair.Find("Sneak/Hidden").gameObject;
            __instance.m_stealthBar = newCrosshair.Find("Sneak/StealthBar").GetComponent<GuiBar>();

            var originalGuardianPowerMaterial = __instance.m_gpIcon.material;
            __instance.m_gpRoot = (RectTransform)__instance.Replace("hudroot/GuardianPower", Auga.Assets.Hud);
            __instance.m_gpName = __instance.m_gpRoot.Find("Name").GetComponent<Text>();
            __instance.m_gpIcon = __instance.m_gpRoot.Find("Icon").GetComponent<Image>();
            __instance.m_gpIcon.material = originalGuardianPowerMaterial;
            __instance.m_gpCooldown = __instance.m_gpRoot.Find("TimeText").GetComponent<Text>();

            var newHealthPanel = __instance.Replace("hudroot/healthpanel", Auga.Assets.Hud);
            Object.Destroy(__instance.m_staminaBar2Root.gameObject);
            __instance.m_healthBarRoot = null;
            __instance.m_healthPanel = null;
            __instance.m_healthAnimator = newHealthPanel.Find("HealthBar").GetComponent<Animator>();
            __instance.m_healthBarFast = null;
            __instance.m_healthBarSlow = null;
            __instance.m_healthText = null;
            __instance.m_healthMaxText = null;
            __instance.m_foodBars = new Image[0];
            __instance.m_foodIcons = new Image[0];
            __instance.m_foodBarRoot = null;
            __instance.m_foodBaseBar = null;
            __instance.m_foodText = null;
            __instance.m_staminaAnimator = newHealthPanel.Find("StaminaBar").GetComponent<Animator>();
            __instance.m_staminaBar2Root = null;
            __instance.m_staminaBar2Fast = null;
            __instance.m_staminaBar2Slow = null;

            __instance.m_actionBarRoot = __instance.Replace("hudroot/action_progress", Auga.Assets.Hud).gameObject;
            __instance.m_actionName = __instance.m_actionBarRoot.GetComponentInChildren<Text>();
            __instance.m_actionProgress = __instance.m_actionBarRoot.GetComponent<GuiBar>();

            // Setup the icon material to grayscale the piece icons
            var iconMaterial = __instance.m_pieceIconPrefab.transform.Find("icon").GetComponent<Image>().material;
            Auga.Assets.BuildHudElement.transform.Find("icon").GetComponent<Image>().material = iconMaterial;

            __instance.m_buildHud = __instance.Replace("hudroot/BuildHud/", Auga.Assets.Hud).gameObject;
            var tabContainer = __instance.m_buildHud.transform.Find("BuildHud/DividerLarge/Tabs");
            __instance.m_pieceCategoryTabs = new[] {
                tabContainer.Find("Misc").gameObject,
                tabContainer.Find("Crafting").gameObject,
                tabContainer.Find("Building").gameObject,
                tabContainer.Find("Furniture").gameObject,
            };
            Localization.instance.Localize(tabContainer);

            for (var index = 0; index < __instance.m_pieceCategoryTabs.Length; index++)
            {
                var categoryTab = __instance.m_pieceCategoryTabs[index];
                var i = index;
                categoryTab.GetComponent<Button>().onClick.AddListener(() => SetBuildCategory(i));
            }

            __instance.m_pieceSelectionWindow = __instance.m_buildHud.transform.Find("BuildHud").gameObject;
            __instance.m_pieceCategoryRoot = __instance.m_buildHud.transform.Find("BuildHud/DividerLarge").gameObject;
            __instance.m_pieceListRoot = (RectTransform)__instance.m_buildHud.transform.Find("BuildHud/PieceList/Root");
            __instance.m_pieceListMask = null;
            __instance.m_pieceIconPrefab = Auga.Assets.BuildHudElement;
            __instance.m_closePieceSelectionButton = __instance.m_buildHud.transform.Find("CloseButton").GetComponent<UIInputHandler>();

            var selectedPiece = __instance.m_buildHud.transform.Find("SelectedPiece");
            __instance.m_buildSelection = selectedPiece.Find("Name").GetComponent<Text>();
            __instance.m_pieceDescription = selectedPiece.Find("Info").GetComponent<Text>();
            __instance.m_buildIcon = selectedPiece.Find("IconBG/PieceIcon").GetComponent<Image>();
            var requirements = selectedPiece.Find("Requirements");
            __instance.m_requirementItems = new []{
                requirements.GetChild(0).gameObject,
                requirements.GetChild(1).gameObject,
                requirements.GetChild(2).gameObject,
                requirements.GetChild(3).gameObject,
                requirements.GetChild(4).gameObject,
                requirements.GetChild(5).gameObject,
            };

            __instance.transform.Replace("hudroot/KeyHints", Auga.Assets.Hud);

            var shipHud = __instance.transform.Replace("hudroot/ShipHud", Auga.Assets.Hud);
            __instance.m_shipHudRoot = shipHud.gameObject;
            __instance.m_shipControlsRoot = shipHud.Find("Controls").gameObject;
            __instance.m_rudderLeft = shipHud.Find("Dummy").gameObject;
            __instance.m_rudderRight = shipHud.Find("Dummy").gameObject;
            __instance.m_rudderSlow = shipHud.Find("WindIndicator/Ship/Slow").gameObject;
            __instance.m_rudderForward = shipHud.Find("WindIndicator/Ship/Forward").gameObject;
            __instance.m_rudderFastForward = shipHud.Find("WindIndicator/Ship/FastForward").gameObject;
            __instance.m_rudderBackward = shipHud.Find("WindIndicator/Ship/Backward").gameObject;
            __instance.m_halfSail = shipHud.Find("PowerIcon/SailRotation/HalfSail").gameObject;
            __instance.m_fullSail = shipHud.Find("PowerIcon/SailRotation/FullSail").gameObject;
            __instance.m_rudder = shipHud.Find("PowerIcon/Rudder").gameObject;
            __instance.m_shipWindIndicatorRoot = (RectTransform)shipHud.Find("WindIndicator");
            __instance.m_shipWindIcon = shipHud.Find("WindIndicator/Wind/WindIcon").GetComponent<Image>();
            __instance.m_shipWindIconRoot = (RectTransform)shipHud.Find("WindIndicator/Wind");
            __instance.m_shipRudderIndicator = shipHud.Find("Controls/RudderIndicatorBG/RudderIndicatorMask/RudderIndicator").GetComponent<Image>();
            __instance.m_shipRudderIcon = shipHud.Find("Controls/RudderIcon").GetComponent<Image>();
        }

        [HarmonyPatch(nameof(Hud.UpdateStatusEffects))]
        [HarmonyPrefix]
        public static bool Hud_UpdateStatusEffects_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(Hud.UpdateFood))]
        [HarmonyPrefix]
        public static bool Hud_UpdateFood_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(Hud.SetHealthBarSize))]
        [HarmonyPrefix]
        public static bool Hud_SetHealthBarSize_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(Hud.SetStaminaBarSize))]
        [HarmonyPrefix]
        public static bool Hud_SetStaminaBarSize_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(Hud.UpdateHealth))]
        [HarmonyPrefix]
        public static bool Hud_UpdateHealth_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(Hud.UpdateStamina))]
        [HarmonyPrefix]
        public static bool Hud_UpdateStamina_Prefix()
        {
            return false;
        }

        public static void SetBuildCategory(int index)
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.SetBuildCategory(index);
            }
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    public static class HotkeyBar_UpdateIcons_Patch
    {
        public static void Postfix(HotkeyBar __instance)
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                return;
            }

            for (var index = 0; index < __instance.m_items.Count; ++index)
            {
                var itemData = __instance.m_items[index];
                if (itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= __instance.m_elements.Count)
                {
                    continue;
                }

                var element = __instance.m_elements[itemData.m_gridPos.x];
                var itemTooltip = element.m_go.GetComponent<ItemTooltip>();
                if (itemTooltip != null)
                {
                    itemTooltip.Item = itemData;
                }
            }
        }
    }

    // Hud.UpdateShipHud (0.145 either way)
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateShipHud))]
    public static class Hud_UpdateShipHud_Patch
    {
        public const float RudderMinMax = 0.145f;

        public static void Postfix(Hud __instance, Player player, float dt)
        {
            var ship = player.GetControlledShip();
            if (ship == null || !__instance.m_shipRudderIndicator.gameObject.activeSelf)
            {
                return;
            }

            var rudderValue = ship.GetRudderValue();
            if (rudderValue > 0)
            {
                __instance.m_shipRudderIndicator.fillClockwise = true;
                __instance.m_shipRudderIndicator.fillAmount = rudderValue * RudderMinMax;
            }
            else
            {
                __instance.m_shipRudderIndicator.fillClockwise = false;
                __instance.m_shipRudderIndicator.fillAmount = -rudderValue * RudderMinMax;
            }
        }
    }
}
