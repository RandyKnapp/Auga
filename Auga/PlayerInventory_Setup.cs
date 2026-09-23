using System.Globalization;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class InventoryPanel_Patches
    {
        public static AugaCraftingPanel CraftingPanel;
        public static Transform TopRowInventory;
        public static Transform MainRowsInventory;

        /// <summary>Vertical gap between the bottom of the player panel and the top of the container panel, as authored in the prefab.</summary>
        private static float ContainerGap;

        /// <summary>
        /// The player grid grows with the number of inventory rows instead of scrolling. Vanilla already resizes
        /// InventoryGui.m_player in SetInventorySize (base height + (rows - 4) * row height), but it measured both
        /// numbers on the vanilla panel in Awake, before Auga replaced it; the Auga panel is authored so that
        /// "Main" holds exactly (rows - 1) grid rows when the panel is base height + (rows - 4) * row pitch.
        /// </summary>
        private static void SetupExpandingPlayerGrid(InventoryGui gui, Transform playerPanel)
        {
            var grid = gui.m_playerGrid;
            var main = grid.transform.Find("Main");
            var rowPitch = grid.m_elementSpace;
            var layout = main != null ? main.GetComponentInChildren<GridLayoutGroup>(true) : null;
            if (layout != null)
            {
                rowPitch = layout.cellSize.y + layout.spacing.y;
            }

            var scrollRect = main != null ? main.GetComponent<ScrollRect>() : null;
            if (scrollRect != null)
            {
                scrollRect.vertical = false;
                scrollRect.horizontal = false;
                scrollRect.enabled = false;
            }
            var scrollbar = playerPanel.Find("PlayerScroll");
            if (scrollbar != null)
            {
                scrollbar.gameObject.SetActive(false);
            }
            grid.m_scrollbar = null;
            grid.m_ensureVisible = null;

            gui.m_playerHeight = gui.m_player.sizeDelta.y;
            gui.m_invGridHeight = rowPitch;
            ContainerGap = BottomEdge(gui.m_player) - TopEdge(gui.m_container);
            UpdateContainerPosition(gui);
        }

        /// <summary>Keeps the container panel directly below the (possibly resized) player panel.</summary>
        public static void UpdateContainerPosition(InventoryGui gui)
        {
            var player = gui.m_player;
            var container = gui.m_container;
            if (player == null || container == null || player.parent != container.parent)
            {
                return;
            }

            var position = container.anchoredPosition;
            position.y += BottomEdge(player) - ContainerGap - TopEdge(container);
            container.anchoredPosition = position;
        }

        // Both panels are anchored to the same parent edge (top-left) with a fixed height, so their top and bottom
        // edges can be expressed in the parent's anchored space.
        private static float TopEdge(RectTransform rect) => rect.anchoredPosition.y + (1f - rect.pivot.y) * rect.sizeDelta.y;
        private static float BottomEdge(RectTransform rect) => TopEdge(rect) - rect.sizeDelta.y;

        /// <summary>
        /// InventoryGrid now expects every slot prefab to carry an InventoryElement component that references
        /// its icon/amount/quality/... children (the grid used to look them up by name). The Auga slot prefab
        /// predates that component, so build it on the (in-memory) prefab once; every slot instantiated from it
        /// then has it. Child names follow the vanilla slot the Auga prefab was modelled on.
        /// </summary>
        public static void EnsureInventoryElement(GameObject elementPrefab)
        {
            if (elementPrefab == null || elementPrefab.GetComponent<InventoryElement>() != null)
            {
                return;
            }

            var t = elementPrefab.transform;
            var element = elementPrefab.AddComponent<InventoryElement>();

            // The grid subscribes to click and drag handlers on every slot without null checks; drag-and-drop
            // (UIDragHandler) is newer than the Auga slot prefab.
            if (elementPrefab.GetComponentInChildren<UIInputHandler>(true) == null)
            {
                elementPrefab.AddComponent<UIInputHandler>();
            }
            if (elementPrefab.GetComponentInChildren<UIDragHandler>(true) == null)
            {
                elementPrefab.AddComponent<UIDragHandler>();
            }
            element.m_button = elementPrefab.GetComponent<Button>() ?? elementPrefab.GetComponentInChildren<Button>(true);
            element.m_touchRect = t as RectTransform;
            element.m_icon = t.Find("icon")?.GetComponent<Image>();
            element.m_amount = t.Find("amount")?.GetComponent<TMP_Text>();
            element.m_quality = t.Find("quality")?.GetComponent<TMP_Text>();
            element.m_equiped = t.Find("equiped")?.GetComponent<Image>();
            element.m_queued = t.Find("queued")?.GetComponent<Image>();
            element.m_selected = t.Find("selected")?.gameObject;
            element.m_noteleport = t.Find("noteleport")?.GetComponent<Image>();
            element.m_food = t.Find("foodicon")?.GetComponent<Image>();
            var durability = t.Find("durability");
            element.m_durability = durability?.GetComponent<GuiBar>();
            if (durability != null && element.m_durability == null)
            {
                // Auga drives the bar with its own BetterDurabilityBar; the grid still expects a GuiBar to toggle/scale.
                var bar = durability.gameObject.AddComponent<GuiBar>();
                bar.m_bar = (durability.Find("bar") ?? durability.Find("realbar") ?? durability) as RectTransform;
                element.m_durability = bar;
            }
            element.m_tooltip = elementPrefab.GetComponent<UITooltip>() ?? elementPrefab.AddComponent<UITooltip>();
            element.m_touchHighlightColor = Color.white;
            element.m_dropFocus = t.Find("dropFocus")?.GetComponent<Image>() ?? CreateStretchedImage(t, "dropFocus", new Color(1f, 1f, 1f, 0.25f));

            // Hotkey number shown on the top row; the grid looks it up by name.
            if (t.Find("binding") == null)
            {
                var binding = new GameObject("binding", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                binding.transform.SetParent(t, false);
                var rect = (RectTransform)binding.transform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(4, -2);
                rect.sizeDelta = new Vector2(20, 20);
                var text = binding.GetComponent<TextMeshProUGUI>();
                text.fontSize = 14;
                text.raycastTarget = false;
                text.enabled = false;
            }

            if (element.m_button == null)
            {
                // InventoryElement.Initialize reads the button's colour block; give it an inert one.
                var button = elementPrefab.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                element.m_button = button;
            }
        }

        /// <summary>
        /// Fields the grid dereferences without null checks that the Auga grid prefab may leave unset.
        /// </summary>
        private static void EnsureGridReferences(InventoryGrid grid, Transform panel)
        {
            if (grid == null)
            {
                return;
            }

            if (grid.m_uiGroup == null)
            {
                grid.m_uiGroup = panel != null ? panel.GetComponent<UIGroupHandler>() : null;
            }
            if (grid.m_tooltipAnchor == null)
            {
                grid.m_tooltipAnchor = grid.transform as RectTransform;
            }
            if (grid.m_gridRoot == null)
            {
                grid.m_gridRoot = (grid.transform.Find("Root") ?? grid.transform) as RectTransform;
            }

            Auga.Log($"InventoryGrid '{grid.name}': uiGroup={(grid.m_uiGroup != null)} gridRoot={(grid.m_gridRoot != null)} elementPrefab={(grid.m_elementPrefab != null)} ensureVisible={(grid.m_ensureVisible != null)} scrollbar={(grid.m_scrollbar != null)}");
        }

        private static Image CreateStretchedImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_Patch
        {
            [HarmonyPriority(Priority.First)]
            public static void Postfix(InventoryGui __instance)
            {
                Debug.LogWarning($"Starting Auga InventoryGui.Postfix");
                AddItemIconMaterial.IconMaterial = __instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;

                __instance.m_playerGrid.m_onSelected = null;
                __instance.m_playerGrid.m_onRightClick = null;
                __instance.m_containerGrid.m_onSelected = null;
                __instance.m_containerGrid.m_onRightClick = null;

                // The vanilla container panel now lives inside root/Player; Auga replaces Player and Container as
                // siblings under root, so move the vanilla container out first (it is replaced below anyway). It
                // goes right after Player: SetParent appends it as the last child, behind the split dialog, and
                // Replace keeps the sibling index it finds, so the split stack dialog opened underneath the container.
                var vanillaPlayer = __instance.transform.Find("root/Player");
                var vanillaContainer = __instance.transform.Find("root/Player/Container");
                if (vanillaContainer != null)
                {
                    vanillaContainer.SetParent(__instance.transform.Find("root"), false);
                    vanillaContainer.SetSiblingIndex(vanillaPlayer.GetSiblingIndex() + 1);
                }

                var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player");
                __instance.m_player = playerInventory.RectTransform();
                // Touch-only anchor for the split dialog; the vanilla one was inside the replaced Player panel.
                __instance.m_touchSplitAnchor = playerInventory;
                __instance.m_playerGrid = playerInventory.Find("PlayerGrid").GetComponent<InventoryGrid>();
                __instance.m_playerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_playerGrid.m_onRightClick += __instance.OnRightClickItem;
                __instance.m_weight = playerInventory.Find("Weight/Text").GetComponent<TMP_Text>();
                __instance.m_armor = playerInventory.Find("Armor/Text").GetComponent<TMP_Text>();

                var containerInventory = __instance.Replace("root/Container", Auga.Assets.InventoryScreen, "root/Container");
                __instance.m_container = containerInventory.RectTransform();
                __instance.m_containerName = containerInventory.Find("ContainerHeader/Name").GetComponent<TMP_Text>();
                __instance.m_containerGrid = containerInventory.Find("ContainerGrid").GetComponent<InventoryGrid>();
                __instance.m_containerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_containerGrid.m_onRightClick += __instance.OnRightClickItem;

                // InventoryGui.Awake wired these on the vanilla grids before they were replaced; the game invokes
                // CanDropDragOntoItem without a null check on every item, so all of them must be restored.
                __instance.m_playerGrid.m_onReleased += __instance.OnReleasedItem;
                __instance.m_playerGrid.m_onEnter += __instance.OnEnterElement;
                __instance.m_playerGrid.OnMoveToLowerInventoryGrid += __instance.MoveToLowerInventoryGrid;
                __instance.m_playerGrid.OnSetTouchSelection += __instance.SetTouchSelection;
                __instance.m_playerGrid.CanDropDragOntoItem = __instance.CanDropDragOntoItem;
                __instance.m_containerGrid.m_onReleased += __instance.OnReleasedItem;
                __instance.m_containerGrid.m_onEnter += __instance.OnEnterElement;
                __instance.m_containerGrid.OnMoveToUpperInventoryGrid += __instance.MoveToUpperInventoryGrid;
                __instance.m_containerGrid.OnSetTouchSelection += __instance.SetTouchSelection;
                __instance.m_containerGrid.CanDropDragOntoItem = __instance.CanDropDragOntoItem;

                EnsureInventoryElement(__instance.m_playerGrid.m_elementPrefab);
                EnsureInventoryElement(__instance.m_containerGrid.m_elementPrefab);
                EnsureGridReferences(__instance.m_playerGrid, playerInventory);
                EnsureGridReferences(__instance.m_containerGrid, containerInventory);
                SetupExpandingPlayerGrid(__instance, playerInventory);
                __instance.m_containerWeight = containerInventory.Find("Weight/Text").GetComponent<TMP_Text>();
                __instance.m_takeAllButton = containerInventory.Find("TakeAll").GetComponent<ColorButtonText>();
                __instance.m_takeAllButton.onClick.AddListener(__instance.OnTakeAll);
                __instance.m_stackAllButton = containerInventory.Find("StackAll").GetComponent<ColorButtonText>();
                __instance.m_stackAllButton.onClick.AddListener(__instance.OnStackAll);
                
                var oldCraftingPanel = __instance.transform.Find("root/Crafting");
                var craftingPanelSiblingIndex = oldCraftingPanel.GetSiblingIndex();
                oldCraftingPanel.gameObject.SetActive(false);
                Object.Destroy(oldCraftingPanel.gameObject);

                var variantDialog = __instance.Replace("root/VariantDialog", Auga.Assets.InventoryScreen, "root/DummyObjects/DummyVariantDialog");
                __instance.m_variantDialog = variantDialog.GetComponent<VariantDialog>();

                var skillsDialog = __instance.Replace("root/Skills", Auga.Assets.InventoryScreen, "root/RightPanel/TabContent/TabContent_Skills");
                __instance.m_skillsDialog = skillsDialog.GetComponent<SkillsDialog>();
                var dummyContainer = new GameObject("DummyDialogs", typeof(RectTransform));
                dummyContainer.transform.SetParent(skillsDialog.parent);
                variantDialog.SetParent(dummyContainer.transform);
                skillsDialog.SetParent(dummyContainer.transform);
                dummyContainer.SetActive(false);

                var rightPanel = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/RightPanel"), containerInventory.parent, false);
                Debug.LogWarning($"API AAA is null: {API.GetCraftingControls().Amount == null}");
                Debug.LogWarning($"API InputAmount is null: {API.GetCraftingControls().InputAmount == null}");
                Debug.LogWarning($"API InputAmount is null: {API.GetCraftingControls().CraftButton == null}");
                rightPanel.gameObject.name = "RightPanel";
                rightPanel.SetSiblingIndex(craftingPanelSiblingIndex);
                CraftingPanel = rightPanel.GetComponentInChildren<AugaCraftingPanel>(true);
                CraftingPanel.SetMultiCraftEnabled(Auga.HasMultiCraft);
                __instance.m_playerName = rightPanel.Find("DefaultContent/TitleContainer/PlayerPanelTitle").GetComponent<TMP_Text>();
                __instance.m_pvp = rightPanel.Find("TabContent/TabContent_PVP/Dummy/PVPToggle").GetComponent<Toggle>();
                __instance.m_recipeElementPrefab = CraftingPanel.RecipeItemPrefab;
                __instance.m_recipeListRoot = CraftingPanel.RecipeList;
                __instance.m_recipeListScroll = CraftingPanel.RecipeListScrollbar;
                __instance.m_recipeEnsureVisible = CraftingPanel.RecipeListEnsureVisible;
                __instance.m_recipeListSpace = 34;
                __instance.m_craftingStationName = CraftingPanel.WorkbenchName;
                __instance.m_craftingStationIcon = CraftingPanel.WorkbenchIcon;
                __instance.m_craftingStationLevelRoot = CraftingPanel.WorkbenchLevelRoot;
                __instance.m_craftingStationLevel = CraftingPanel.WorkbenchLevel;
                __instance.m_craftButton = CraftingPanel.CraftButton;
                __instance.m_craftButton.onClick.AddListener(__instance.OnCraftPressed);
                __instance.m_craftCancelButton = CraftingPanel.CraftCancelButton;
                __instance.m_craftCancelButton.onClick.AddListener(__instance.OnCraftCancelPressed);
                __instance.m_craftProgressPanel = CraftingPanel.CraftProgressPanel;
                __instance.m_variantButton = CraftingPanel.VariantButton;
                __instance.m_variantButton.onClick.AddListener(__instance.OnShowVariantSelection);
                __instance.m_variantDialog = CraftingPanel.VariantDialog;
                __instance.m_variantDialog.m_selected += __instance.OnVariantSelected;
                __instance.m_repairButton = CraftingPanel.DefaultRepairButton;
                __instance.m_repairButtonGlow = CraftingPanel.DefaultRepairGlow;
                __instance.m_repairPanel = CraftingPanel.DefaultRepairButton.transform;
                __instance.m_repairButton.onClick.AddListener(__instance.OnRepairPressed);

                __instance.m_recipeIcon = CraftingPanel.DummyIcon;
                __instance.m_recipeName = CraftingPanel.DummyName;
                __instance.m_recipeDecription = CraftingPanel.DummyDescription;
                __instance.m_repairPanelSelection = CraftingPanel.DummyRepairPanelSelection;
                __instance.m_tabCraft = CraftingPanel.DummyCraftTabButton;
                __instance.m_tabUpgrade = CraftingPanel.DummyUpgradeTabButton;
                __instance.m_craftProgressBar = CraftingPanel.DummyCraftProgressBar;
                __instance.m_qualityPanel = CraftingPanel.DummyQualityPanel;
                __instance.m_minStationLevelIcon = CraftingPanel.DummyMinStationLevelIcon;
                CraftingPanel.Initialize(__instance);

                Object.Destroy(__instance.transform.Find("root/Info").gameObject);
                /*var info = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/Info"), containerInventory.parent, false);
                info.SetSiblingIndex(3);
                info.gameObject.name = "Info";
                info.Find("Texts").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTexts);
                info.Find("Trophies").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTrophies);*/

                // The split dialog is now its own SplitDialog component (it wires the slider/button listeners
                // itself in OnEnable and raises SplitAccepted/SplitCanceled events for InventoryGui).
                var splitDialog = __instance.Replace("root/SplitDialog", Auga.Assets.InventoryScreen, "root/SplitDialog");
                splitDialog.gameObject.SetActive(false);
                var splitDialogComponent = splitDialog.GetComponent<SplitDialog>() ?? splitDialog.gameObject.AddComponent<SplitDialog>();
                var splitPanel = splitDialog.Find("Dialog").RectTransform();
                splitDialogComponent.m_panel = splitPanel;
                splitDialogComponent.m_panelNormalPosition = splitPanel;
                splitDialogComponent.m_panelTouchPosition = splitPanel;
                splitDialogComponent.m_splitSlider = splitDialog.Find("Dialog/Slider").GetComponent<Slider>();
                splitDialogComponent.m_splitAmount = splitDialog.Find("Dialog/InventoryElement/amount").GetComponent<TMP_Text>();
                splitDialogComponent.m_splitCancelButton = splitDialog.Find("Dialog/ButtonCancel").GetComponent<Button>();
                splitDialogComponent.m_splitOkButton = splitDialog.Find("Dialog/ButtonOk").GetComponent<Button>();
                splitDialogComponent.m_splitIcon = splitDialog.Find("Dialog/InventoryElement/icon").GetComponent<Image>();
                splitDialogComponent.m_splitIconName = splitDialog.Find("Dialog/InventoryElement/DummyText").GetComponent<TMP_Text>();
                __instance.m_splitDialog = splitDialogComponent;

                // The game addresses these by index: [2] when opening texts/trophies/skills, [3] for crafting.
                __instance.m_uiGroups = new [] {
                    containerInventory.GetComponent<UIGroupHandler>(),
                    playerInventory.GetComponent<UIGroupHandler>(),
                    rightPanel.GetComponent<UIGroupHandler>(),
                    rightPanel.GetComponent<UIGroupHandler>()
                };

                var animator = __instance.GetComponent<Animator>();
                var newAnimator = Auga.Assets.InventoryScreen.GetComponent<Animator>();
                animator.runtimeAnimatorController = newAnimator.runtimeAnimatorController;
                animator.Rebind();

                var standardDivider = playerInventory.Find("StandardDivider");
                var trashDivider = playerInventory.Find("TrashDivider");
                standardDivider.gameObject.SetActive(!Auga.UseAugaTrash.Value);
                trashDivider.gameObject.SetActive(Auga.UseAugaTrash.Value);

                Localization.instance.Localize(__instance.transform);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        public static class InventoryGrid_UpdateGui_Patch
        {
            public static void Postfix(InventoryGrid __instance)
            {
                if (__instance.name == "PlayerGrid")
                {
                    if (TopRowInventory == null)
                    {
                        TopRowInventory = __instance.transform.Find("Top");
                        MainRowsInventory = __instance.transform.Find("Main/Grid");
                    }
                }

                //Vector2 startPos = new Vector2(__instance.RectTransform().rect.width / 2f, 0.0f) - new Vector2(__instance.GetWidgetSize().x, 0.0f) * 0.5f;
                foreach (var element in __instance.m_elements)
                {
                    var itemTooltip = element.gameObject.GetComponent<ItemTooltip>();
                    
                    var item = __instance.m_inventory.GetItemAt(element.Position.x, element.Position.y);
                    
                    if (itemTooltip != null && !element.m_used)
                    {
                        itemTooltip.Item = null;
                    }

                    if (element.m_used && itemTooltip != null)
                    {
                        itemTooltip.Item = item;
                    }

                    if (__instance.name == "PlayerGrid")
                    {
                        if (element.Position.y == 0)
                        {
                            element.gameObject.transform.SetParent(TopRowInventory);
                        }
                        else
                        {
                            element.gameObject.transform.SetParent(MainRowsInventory);
                            //Vector2 currentPosition = new Vector3(element.Position.x * (__instance.m_elementSpace), (element.Position.y * -__instance.m_elementSpace) - 26);
                            //element.gameObject.RectTransform().anchoredPosition = startPos + currentPosition;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetInventorySize))]
        public static class InventoryGui_SetInventorySize_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                UpdateContainerPosition(__instance);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                var player = Player.m_localPlayer;
                if (player != null)
                {
                    __instance.UpdateContainer(player);
                }
            }
        }

        //CreateItemTooltip
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
        public static class InventoryGrid_CreateItemTooltip_Patch
        {
            public static bool Prefix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
            {
                var itemTooltip = tooltip.GetComponent<ItemTooltip>();
                if (itemTooltip != null)
                {
                    itemTooltip.Item = item;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]
        public static class InventoryGui_SetRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, __instance.m_selectedVariant);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.OnUpdateRecipe(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnVariantSelected))]
        public static class InventoryGui_OnVariantSelected_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, __instance.m_selectedVariant);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        public static class InventoryGui_SetupRequirementList_Patch
        {
            public static void Postfix(InventoryGui __instance, int quality, Player player, bool allowedQuality, int amount)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.PostSetupRequirementList(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, quality, player, allowedQuality, amount);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats))]
        public static class InventoryGui_UpdateCharacterStats_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                __instance.m_playerName.text = Game.instance.GetPlayerProfile().GetName();
                __instance.m_armor.text = player.GetBodyArmor().ToString(CultureInfo.InvariantCulture);
                return false;
            }
        }

        [HarmonyPatch(typeof(VariantDialog), nameof(VariantDialog.Setup))]
        public static class VariantDialog_Setup_Patch
        {
            public static void Postfix(VariantDialog __instance)
            {
                for (var index = 0; index < __instance.m_elements.Count; index++)
                {
                    var variantElement = __instance.m_elements[index];
                    var selected = index == InventoryGui.instance.m_selectedVariant;

                    var selectedObject = variantElement.transform.Find("selected");
                    if (selectedObject != null)
                    {
                        selectedObject.gameObject.SetActive(selected);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Auga's tab buttons play their own click sound, and the vanilla tab handlers they forward to switch the
    /// inventory UI group, which plays the group-switch effect as well: every crafting tab change sounded twice.
    /// The group switch stays silent while those handlers run; gamepad group cycling keeps its sound.
    /// </summary>
    [HarmonyPatch]
    public static class InventoryGui_TabHandlers_Silent_Patch
    {
        internal static int SilentDepth;

        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed));
            yield return AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed));
        }

        public static void Prefix()
        {
            SilentDepth++;
        }

        public static System.Exception Finalizer(System.Exception __exception)
        {
            SilentDepth = Mathf.Max(0, SilentDepth - 1);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetActiveGroup", typeof(int), typeof(bool))]
    public static class InventoryGui_SetActiveGroup_Silent_Patch
    {
        public static void Prefix(ref bool playSound)
        {
            if (InventoryGui_TabHandlers_Silent_Patch.SilentDepth > 0)
                playSound = false;
        }
    }
}
