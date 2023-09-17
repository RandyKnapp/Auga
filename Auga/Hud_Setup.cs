using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch(typeof(Hud))]
    public static class Hud_Setup
    {
        [HarmonyPatch(nameof(Hud.Awake))]
        [HarmonyPostfix]
        public static void Hud_Awake_Postfix(Hud __instance)
        {

            var hotkeyBar = __instance.Replace("hudroot/HotKeyBar", Auga.Assets.Hud, "hudroot/HotKeyBar");
            hotkeyBar.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 55, -44);
            
            __instance.m_statusEffectListRoot = null;
            __instance.m_statusEffectTemplate = new GameObject("DummyStatusEffectTemplate", typeof(RectTransform)).RectTransform();
            var newStatusEffects = __instance.Replace("hudroot/StatusEffects", Auga.Assets.Hud);
            newStatusEffects.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperRight, -40, -330);

            __instance.m_saveIcon = __instance.Replace("hudroot/SaveIcon", Auga.Assets.Hud).gameObject;
            __instance.m_saveIconImage = __instance.m_saveIcon.GetComponent<Image>();
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

            
            __instance.m_eventBar = __instance.Replace("hudroot/EventBar", Auga.Assets.Hud).gameObject;
            __instance.m_eventName = __instance.m_eventBar.GetComponentInChildren<Text>();
            __instance.m_eventBar.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperCenter, 0, -90);

            __instance.m_damageScreen = __instance.Replace("hudroot/Damaged", Auga.Assets.Hud).GetComponent<Image>();


            var newCrosshair = __instance.Replace("hudroot/crosshair", Auga.Assets.Hud);
            __instance.m_crosshair = newCrosshair.Find("crosshair").GetComponent<Image>();
            __instance.m_crosshairBow = newCrosshair.Find("crosshair_bow").GetComponent<Image>();
            __instance.m_hoverName = newCrosshair.Find("Dummy/HoverName").GetComponent<TextMeshProUGUI>();
            __instance.m_pieceHealthRoot = (RectTransform)newCrosshair.Find("PieceHealthRoot");
            __instance.m_pieceHealthBar = newCrosshair.Find("PieceHealthRoot/PieceHealthBar").GetComponent<GuiBar>();
            __instance.m_targetedAlert = newCrosshair.Find("Sneak/Alert").gameObject;
            __instance.m_targeted = newCrosshair.Find("Sneak/Detected").gameObject;
            __instance.m_hidden = newCrosshair.Find("Sneak/Hidden").gameObject;
            __instance.m_stealthBar = newCrosshair.Find("Sneak/StealthBar").GetComponent<GuiBar>();
            __instance.m_pieceHealthBar.gameObject.AddComponent<MovableHudElement>().Init("BuildPieceHealthBar", TextAnchor.MiddleCenter, 130, 0);
            __instance.m_targetedAlert.transform.parent.gameObject.AddComponent<MovableHudElement>().Init("Stealth", TextAnchor.MiddleCenter, 0, 0);


            var originalGuardianPowerMaterial = __instance.m_gpIcon.material;
            __instance.m_gpRoot = (RectTransform)__instance.Replace("hudroot/GuardianPower", Auga.Assets.Hud);
            __instance.m_gpName = __instance.m_gpRoot.Find("Name").GetComponent<Text>();
            __instance.m_gpIcon = __instance.m_gpRoot.Find("Icon").GetComponent<Image>();
            __instance.m_gpIcon.material = originalGuardianPowerMaterial;
            __instance.m_gpCooldown = __instance.m_gpRoot.Find("TimeText").GetComponent<Text>();
            
            __instance.m_gpRoot.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 60, 70);

            foreach (Transform child in __instance.m_healthPanel)
            {
                Object.Destroy(child.gameObject);
            }
            Object.Destroy(__instance.m_staminaBar2Root.gameObject);
            Object.Destroy(__instance.m_eitrBarRoot.gameObject);

            var hudroot = __instance.transform.Find("hudroot");
            var foodPanel0 = hudroot.gameObject.CopyOver("hudroot/FoodPanel0", Auga.Assets.Hud, 5);
            var foodPanel1 = hudroot.gameObject.CopyOver("hudroot/FoodPanel1", Auga.Assets.Hud, 6);
            var foodPanel2 = hudroot.gameObject.CopyOver("hudroot/FoodPanel2", Auga.Assets.Hud, 7);
            var newHealthPanel = hudroot.gameObject.CopyOver("hudroot/HealthBar", Auga.Assets.Hud, 8);
            var newStaminaPanel = hudroot.gameObject.CopyOver("hudroot/StaminaBar", Auga.Assets.Hud, 9);
            var newEitrPanel = hudroot.gameObject.CopyOver("hudroot/EitrBar", Auga.Assets.Hud, 10);

            foodPanel0.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 138, 66);
            foodPanel1.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 167, 95);
            foodPanel2.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 138, 124);
            newHealthPanel.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 208, 123.5f);
            newStaminaPanel.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 208, 99.5f);
            newEitrPanel.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerLeft, 185, 74.5f);

            __instance.m_healthBarRoot = null;
            __instance.m_healthAnimator = newHealthPanel.GetComponent<Animator>();
            __instance.m_healthBarFast = null;
            __instance.m_healthBarSlow = null;
            __instance.m_healthText = null;
            __instance.m_foodBars = Array.Empty<Image>();
            __instance.m_foodIcons = Array.Empty<Image>();
            __instance.m_foodBarRoot = null;
            __instance.m_foodBaseBar = null;
            __instance.m_foodText = null;
            __instance.m_staminaAnimator = newStaminaPanel.GetComponent<Animator>();
            __instance.m_staminaBar2Root = null;
            __instance.m_staminaBar2Fast = null;
            __instance.m_staminaBar2Slow = null;
            __instance.m_staminaText = null;
            __instance.m_eitrAnimator = newEitrPanel.GetComponent<Animator>();
            __instance.m_eitrBarRoot = null;
            __instance.m_eitrBarFast = null;
            __instance.m_eitrBarSlow = null;
            __instance.m_eitrText = null;

            __instance.m_actionBarRoot = __instance.Replace("hudroot/action_progress", Auga.Assets.Hud).gameObject;
            __instance.m_actionName = __instance.m_actionBarRoot.GetComponentInChildren<Text>();
            __instance.m_actionProgress = __instance.m_actionBarRoot.GetComponent<GuiBar>();
            __instance.m_actionBarRoot.gameObject.AddComponent<MovableHudElement>().Init("ActionProgress", TextAnchor.LowerCenter, 0, 226);

            var newStaggerPanel = __instance.Replace("hudroot/staggerpanel", Auga.Assets.Hud);
            __instance.m_staggerAnimator = newStaggerPanel.GetComponent<Animator>();
            __instance.m_staggerProgress = newStaggerPanel.Find("staggerbar/RightBar/Background/FillMask/FillFast").GetComponent<GuiBar>();
            newStaggerPanel.gameObject.AddComponent<MovableHudElement>().Init("StaggerPanel", TextAnchor.LowerCenter, 0, 151);

            if (Auga.BuildMenuShow.Value && !Auga.HasSearsCatalog)
            {
                // Setup the icon material to grayscale the piece icons
                var iconMaterial = __instance.m_pieceIconPrefab.transform.Find("icon").GetComponent<Image>().material;
                Auga.Assets.BuildHudElement.transform.Find("icon").GetComponent<Image>().material = iconMaterial;

                __instance.m_buildHud = __instance.Replace("hudroot/BuildHud", Auga.Assets.Hud).gameObject;
                var tabController = __instance.m_buildHud.GetComponent<BuildMenuPaginationController>();
                tabController.hud = __instance;
                tabController.buildMenu = __instance.m_buildHud.transform.Find("BuildHud").gameObject;
                
                var tabContainer = __instance.m_buildHud.transform.Find("BuildHud/DividerLarge/TabContainer/Tabs");
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
                __instance.m_pieceCategoryRoot = __instance.m_buildHud.transform.Find("BuildHud/DividerLarge/TabContainer/Tabs").gameObject;
                __instance.m_pieceListRoot = (RectTransform)__instance.m_buildHud.transform.Find("BuildHud/PieceList/Root");
                __instance.m_pieceListMask = null;
                __instance.m_pieceIconPrefab = Auga.Assets.BuildHudElement;
                __instance.m_closePieceSelectionButton = __instance.m_buildHud.transform.Find("CloseButton").GetComponent<UIInputHandler>();
                __instance.m_pieceSelectionWindow.AddComponent<MovableHudElement>().Init(TextAnchor.MiddleCenter, 0, 0);

                var selectedPiece = __instance.m_buildHud.transform.Find("SelectedPiece");
                __instance.m_buildSelection = selectedPiece.Find("Name").GetComponent<Text>();
                __instance.m_pieceDescription = selectedPiece.Find("Info").GetComponent<Text>();
                __instance.m_buildIcon = selectedPiece.Find("Darken/IconBG/PieceIcon").GetComponent<Image>();
                selectedPiece.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerCenter, 0, 15);

                var requirements = selectedPiece.Find("Requirements");
                __instance.m_requirementItems = new [] {
                    requirements.GetChild(0).gameObject,
                    requirements.GetChild(1).gameObject,
                    requirements.GetChild(2).gameObject,
                    requirements.GetChild(3).gameObject,
                    requirements.GetChild(4).gameObject,
                    requirements.GetChild(5).gameObject,
                };
            }
            
            var keyHints = __instance.transform.Replace("hudroot/KeyHints", Auga.Assets.Hud);
            keyHints.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerRight, -34, 62);

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

            __instance.m_shipControlsRoot.AddComponent<MovableHudElement>().Init("ShipControls", TextAnchor.MiddleCenter, 263, -143);
            __instance.m_shipWindIndicatorRoot.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.MiddleCenter, 0, -418);

            Auga.UpdateStatBars();

            Localization.instance.Localize(__instance.transform);
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

        [HarmonyPatch(nameof(Hud.UpdateEitr))]
        [HarmonyPrefix]
        public static bool Hud_UpdateEitr_Prefix()
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

    //public void SetupPieceInfo(Piece piece)
    [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
    public static class Hud_SetupPieceInfo_Patch
    {

        public static void SetupPieceInfo(Hud instance, Piece piece)
        {
                if (piece == null)
                {
                    instance.m_buildSelection.text = Localization.instance.Localize("$hud_nothingtobuild");
                    instance.m_pieceDescription.text = "";
                    instance.m_buildIcon.enabled = false;
                    
                    if (instance.m_snappingIcon != null)
                        instance.m_snappingIcon.enabled = false;
                    
                    for (int index = 0; index < instance.m_requirementItems.Length; ++index)
                        instance.m_requirementItems[index].SetActive(false);
                }
                else
                {
                    Player localPlayer = Player.m_localPlayer;
                    instance.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
                    instance.m_pieceDescription.text = Localization.instance.Localize(piece.m_description);
                    instance.m_buildIcon.enabled = true;
                    instance.m_buildIcon.sprite = piece.m_icon;
                    Sprite snappingIconForPiece = instance.GetSnappingIconForPiece(piece);
                    if (snappingIconForPiece != null)
                    {
                        instance.m_snappingIcon.sprite = snappingIconForPiece;
                        instance.m_snappingIcon.enabled = snappingIconForPiece != null && (piece.m_category == Piece.PieceCategory.Building || piece.m_groundPiece || piece.m_waterPiece);
                    }
                    for (int index = 0; index < instance.m_requirementItems.Length; ++index)
                    {
                        if (index < piece.m_resources.Length)
                        {
                            Piece.Requirement resource = piece.m_resources[index];
                            instance.m_requirementItems[index].SetActive(true);
                            InventoryGui.SetupRequirement(instance.m_requirementItems[index].transform, resource, localPlayer, false, 0);
                        }
                        else
                            instance.m_requirementItems[index].SetActive(false);
                    }
                    if (!(bool) (Object) piece.m_craftingStation)
                        return;
                    CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
                    GameObject requirementItem = instance.m_requirementItems[piece.m_resources.Length];
                    requirementItem.SetActive(true);
                    Image component1 = requirementItem.transform.Find("res_icon").GetComponent<Image>();
                    Text component2 = requirementItem.transform.Find("res_name").GetComponent<Text>();
                    Text component3 = requirementItem.transform.Find("res_amount").GetComponent<Text>();
                    UITooltip component4 = requirementItem.GetComponent<UITooltip>();
                    component1.sprite = piece.m_craftingStation.m_icon;
                    component2.text = Localization.instance.Localize(piece.m_craftingStation.m_name);
                    string name = piece.m_craftingStation.m_name;
                    component4.m_text = name;
                    if (craftingStation != null)
                    {
                        craftingStation.ShowAreaMarker();
                        component1.color = Color.white;
                        component3.text = "";
                        component3.color = Color.white;
                    }
                    else
                    {
                        component1.color = Color.gray;
                        component3.text = "None";
                        component3.color = Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : Color.white;
                    }
                }

        }
        
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instrs = instructions.ToList();

            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }

            for (int i = 0; i < instrs.Count; ++i)
            {
                if (i == 0)
                {
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;

                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
                    counter++;

                    yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Hud_SetupPieceInfo_Patch), nameof(SetupPieceInfo))));
                    counter++;

                    yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                    counter++;

                }
            }
        }

        
        
        public static void Postfix(Hud __instance, Piece piece)
        {
            __instance.m_pieceDescription.gameObject.SetActive(!string.IsNullOrEmpty(__instance.m_pieceDescription.text));

            var requireItemsContainer = __instance.m_requirementItems[0].transform.parent;
            
            if (requireItemsContainer == null) return;
            requireItemsContainer.gameObject.SetActive(piece != null && piece.m_resources is { Length: > 0 });
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
    public static class Hud_UpdateCrosshair_Patch
    {
        public static Transform AugaHoverText;
        public static TextMeshProUGUI HoverTextPrefab;
        public static GameObject HoverTextWithButtonPrefab;
        public static GameObject HoverTextWithButtonRangePrefab;

        private static string _lastHoverText;
        private static readonly Dictionary<string, string> _cachedKeyNames = new Dictionary<string, string>();

        public static void Postfix(Hud __instance)
        {
            if (HoverTextPrefab == null)
            {
                AugaHoverText = __instance.m_crosshair.transform.parent.Find("AugaHoverText");
                HoverTextPrefab = __instance.m_hoverName;
                HoverTextWithButtonPrefab = __instance.m_crosshair.transform.parent.Find("Dummy/HoverNameWithButton").gameObject;
                HoverTextWithButtonRangePrefab = __instance.m_crosshair.transform.parent.Find("Dummy/HoverNameWithButtonRange").gameObject;
            }

            if (!string.IsNullOrEmpty(__instance.m_hoverName.text))
            {
                ColorUtility.TryParseHtmlString(Auga.Colors.BrightestGold, out var brightestGold);
                __instance.m_crosshair.color = brightestGold;
            }
            else
            {
                __instance.m_crosshair.color = new Color(1f, 1f, 1f, 0.5f);
            }
            AdjustText();
            return;

            void AdjustText(bool show = false)
            {
                if (!show)
                    return;
                
                AugaHoverText.gameObject.SetActive(__instance.m_hoverName.gameObject.activeSelf);

                if (_lastHoverText != __instance.m_hoverName.text)
                {
                    _lastHoverText = __instance.m_hoverName.text;
                    foreach (Transform child in AugaHoverText)
                    {
                        Object.Destroy(child.gameObject);
                    }

                    var parts = _lastHoverText.Split('\n');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("["))
                        {
                            var closingBracketIndex = part.IndexOf(']');
                            if (closingBracketIndex > 0)
                            {
                                var textInBracket = part.Substring(1, closingBracketIndex - 1);
                                textInBracket = textInBracket.Replace("<color=yellow>", "").Replace("<b>", "").Replace("</b>", "").Replace("</color>", "").Trim();
                                var otherText = part.Substring(closingBracketIndex + 1).Trim();

                                if (textInBracket == "1-8")
                                {
                                    var lineWithRange = Object.Instantiate(HoverTextWithButtonRangePrefab, AugaHoverText, false);
                                    lineWithRange.SetActive(true);
                                    var bindings = lineWithRange.GetComponentsInChildren<AugaBindingDisplay>();
                                    bindings[0].SetText("1");
                                    bindings[1].SetText("8");
                                    var text = lineWithRange.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                                    text.text = otherText;
                                    continue;
                                }

                                if (_cachedKeyNames.Count == 0)
                                {
                                    foreach (var buttonEntry in ZInput.instance.m_buttons)
                                    {
                                        var bindingDisplay = Localization.instance.GetBoundKeyString(buttonEntry.Key);
                                        if (!_cachedKeyNames.ContainsKey(bindingDisplay))
                                        {
                                            _cachedKeyNames.Add(bindingDisplay, buttonEntry.Key);
                                        }
                                    }
                                }

                                if (_cachedKeyNames.TryGetValue(textInBracket, out var keyName))
                                {
                                    var lineWithBinding = Object.Instantiate(HoverTextWithButtonPrefab, AugaHoverText, false);
                                    lineWithBinding.SetActive(true);
                                    var binding = lineWithBinding.GetComponentInChildren<AugaBindingDisplay>();
                                    binding.SetBinding(keyName);
                                    var text = lineWithBinding.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                                    text.text = otherText;
                                    continue;
                                }
                            }
                        }

                        var line = Object.Instantiate(HoverTextPrefab, AugaHoverText, false);
                        line.gameObject.SetActive(true);
                        line.text = part;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.Save))]
        public static class ZInput_Save_Patch
        {
            public static void Postfix()
            {
                _cachedKeyNames.Clear();
            }
        }
    }

    //StaminaBarEmptyFlash
    [HarmonyPatch(typeof(Hud), nameof(Hud.StaminaBarEmptyFlash))]
    public static class Hud_StaminaBarEmptyFlash_Patch
    {
        private const float FlashDuration = 0.15f;
        private static Coroutine _staminaFlashCoroutine;

        public static bool Prefix(Hud __instance)
        {
            if (_staminaFlashCoroutine != null)
            {
                __instance.StopCoroutine(_staminaFlashCoroutine);
            }

            _staminaFlashCoroutine = __instance.StartCoroutine(FlashStaminaBar(__instance));
            return false;
        }

        private static IEnumerator FlashStaminaBar(Hud instance)
        {
            var staminaBarBorder = instance.m_staminaAnimator.transform.Find("Background/Border").GetComponent<Image>();

            for (var i = 0; i < 3; i++)
            {
                staminaBarBorder.color = Color.white;
                yield return new WaitForSeconds(FlashDuration);
                staminaBarBorder.color = Color.black;
                yield return new WaitForSeconds(FlashDuration);
            }
        }
    }

    //UpdateBuild
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild))]
    public static class Hud_UpdateBuild_Patch
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.OperandIs(" [<color=yellow>"))
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, $" [<color={Auga.Colors.BrightestGold}>");
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    //UpdatePieceList
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdatePieceList))]
    public static class Hud_UpdatePieceList_Patch
    {
        public static bool Prefix(Hud __instance, Player player, Vector2Int selectedNr, Piece.PieceCategory category, bool updateAllBuildStatuses)
        {
            if (!Auga.BuildMenuShow.Value || Auga.HasSearsCatalog)
                return true;
            
            var buildPieces = player.GetBuildPieces();
            var pieceIcons = __instance.m_pieceIcons;
            var selectedIndex = selectedNr.x + selectedNr.y * 13;
            selectedNr.x = selectedIndex % 10;
            selectedNr.y = selectedIndex / 10;

            var i = 0;
            for (; i < buildPieces.Count; ++i)
            {
                if (i >= pieceIcons.Count)
                {
                    // Create icon
                    var icon = Object.Instantiate(__instance.m_pieceIconPrefab, __instance.m_pieceListRoot);
                    var pieceIconData = new Hud.PieceIconData();
                    pieceIconData.m_go = icon;
                    pieceIconData.m_tooltip = icon.GetComponent<UITooltip>();
                    pieceIconData.m_icon = icon.transform.Find("icon").GetComponent<Image>();
                    pieceIconData.m_marker = icon.transform.Find("selected").gameObject;
                    pieceIconData.m_upgrade = icon.transform.Find("upgrade").gameObject;
                    pieceIconData.m_icon.color = new Color(1f, 0.0f, 1f, 0.0f);
                    var component = icon.GetComponent<UIInputHandler>();
                    component.m_onLeftDown += __instance.OnLeftClickPiece;
                    component.m_onRightDown += __instance.OnRightClickPiece;
                    component.m_onPointerEnter += __instance.OnHoverPiece;
                    component.m_onPointerExit += __instance.OnHoverPieceExit;
                    pieceIcons.Add(pieceIconData);
                }

                // Update icon
                var pieceIcon = pieceIcons[i];
                pieceIcon.m_marker.SetActive(i == selectedIndex);

                var piece = buildPieces[i];
                pieceIcon.m_icon.sprite = piece.m_icon;
                pieceIcon.m_icon.enabled = true;
                pieceIcon.m_tooltip.m_text = piece.m_name;
                pieceIcon.m_upgrade.SetActive(piece.m_isUpgrade);
            }

            for (; i < pieceIcons.Count; ++i)
            {
                Object.Destroy(pieceIcons[i].m_go);
                pieceIcons[i] = null;
            }

            pieceIcons.RemoveAll(x => x == null);

            __instance.UpdatePieceBuildStatus(buildPieces, player);
            if (updateAllBuildStatuses)
            {
                __instance.UpdatePieceBuildStatusAll(buildPieces, player);
            }

            if (__instance.m_lastPieceCategory == category)
            {
                return false;
            }

            __instance.m_lastPieceCategory = category;
            __instance.m_pieceBarPosX = __instance.m_pieceBarTargetPosX;
            __instance.UpdatePieceBuildStatusAll(buildPieces, player);

            return false;
        }
    }

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory))]
    public static class PieceTable_PrevCategory_Patch
    {
        public static bool Prefix(ref PieceTable __instance)
        {
            if (!Auga.BuildMenuShow.Value || Auga.HasSearsCatalog)
                return true;

            return Input.GetAxis("Mouse ScrollWheel") == 0;
        }

        /*public static void Postfix(ref PieceTable __instance)
        {
            if (!Auga.BuildMenuShow.Value)
                return;

            var player = Player.m_localPlayer;
            var selectedCategory = player.m_buildPieces.m_selectedCategory;
            if (selectedCategory.Equals(Piece.PieceCategory.Misc) ||
                selectedCategory.Equals(Piece.PieceCategory.Crafting) ||
                selectedCategory.Equals(Piece.PieceCategory.Building) ||
                selectedCategory.Equals(Piece.PieceCategory.Furniture))
            {
                if (!(player.m_buildPieces.GetPiecesInSelectedCategory().Count > 0))
                    player.m_buildPieces.PrevCategory();
            }
        }*/
    }

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory))]
    public static class PieceTable_NextCategory_Patch
    {
        public static bool Prefix(ref PieceTable __instance)
        {
            if (!Auga.BuildMenuShow.Value || Auga.HasSearsCatalog)
                return true;
            
            return Input.GetAxis("Mouse ScrollWheel") == 0;
        }

        /*
        public static void Postfix(ref PieceTable __instance)
        {
            if (!Auga.BuildMenuShow.Value)
                return;

            var player = Player.m_localPlayer;
            if (player != null)
            {
                var selectedCategory = player.m_buildPieces.m_selectedCategory;
                if (selectedCategory.Equals(Piece.PieceCategory.Misc) ||
                    selectedCategory.Equals(Piece.PieceCategory.Crafting) ||
                    selectedCategory.Equals(Piece.PieceCategory.Building) ||
                    selectedCategory.Equals(Piece.PieceCategory.Furniture))
                {
                    if (!(player.m_buildPieces.GetPiecesInSelectedCategory().Count > 0))
                        player.m_buildPieces.NextCategory();
                }
            }
        }
    */
    }
}
