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

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_Patch
        {
            [HarmonyPriority(Priority.First)]
            public static void Postfix(InventoryGui __instance)
            {
                AddItemIconMaterial.IconMaterial = __instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;

                __instance.m_playerGrid.m_onSelected = null;
                __instance.m_playerGrid.m_onRightClick = null;
                __instance.m_containerGrid.m_onSelected = null;
                __instance.m_containerGrid.m_onRightClick = null;

                var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player");
                __instance.m_player = playerInventory.RectTransform();
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

                __instance.m_repairButton = CraftingPanel.RepairButton;
                __instance.m_repairButtonGlow = CraftingPanel.RepairGlow;
                __instance.m_repairPanel = CraftingPanel.RepairButton.transform;
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

                var splitDialog = __instance.Replace("root/SplitDialog", Auga.Assets.InventoryScreen, "root/SplitDialog");
                __instance.m_splitPanel = splitDialog;
                __instance.m_splitSlider = splitDialog.Find("Dialog/Slider").GetComponent<Slider>();
                __instance.m_splitAmount = splitDialog.Find("Dialog/InventoryElement/amount").GetComponent<TMP_Text>();
                __instance.m_splitCancelButton = splitDialog.Find("Dialog/ButtonCancel").GetComponent<Button>();
                __instance.m_splitOkButton = splitDialog.Find("Dialog/ButtonOk").GetComponent<Button>();
                __instance.m_splitIcon = splitDialog.Find("Dialog/InventoryElement/icon").GetComponent<Image>();
                __instance.m_splitIconName = splitDialog.Find("Dialog/InventoryElement/DummyText").GetComponent<TMP_Text>();

                __instance.m_splitSlider.onValueChanged.AddListener(__instance.OnSplitSliderChanged);
                __instance.m_splitCancelButton.onClick.AddListener(__instance.OnSplitCancel);
                __instance.m_splitOkButton.onClick.AddListener(__instance.OnSplitOk);

                __instance.m_uiGroups = new [] {
                    containerInventory.GetComponent<UIGroupHandler>(),
                    playerInventory.GetComponent<UIGroupHandler>(),
                    //info.GetComponent<UIGroupHandler>(),
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
                    var itemTooltip = element.m_go.GetComponent<ItemTooltip>();
                    
                    var item = __instance.m_inventory.GetItemAt(element.m_pos.x, element.m_pos.y);
                    
                    if (itemTooltip != null && !element.m_used)
                    {
                        itemTooltip.Item = null;
                    }

                    if (element.m_used)
                    {
                        itemTooltip.Item = item;
                    }

                    if (__instance.name == "PlayerGrid")
                    {
                        if (element.m_pos.y == 0)
                        {
                            element.m_go.transform.SetParent(TopRowInventory);
                        }
                        else
                        {
                            element.m_go.transform.SetParent(MainRowsInventory);
                            //Vector2 currentPosition = new Vector3(element.m_pos.x * (__instance.m_elementSpace), (element.m_pos.y * -__instance.m_elementSpace) - 26);
                            //element.m_go.RectTransform().anchoredPosition = startPos + currentPosition;
                        }
                    }
                }
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
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Key, __instance.m_selectedRecipe.Value, __instance.m_selectedVariant);
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
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Key, __instance.m_selectedRecipe.Value, __instance.m_selectedVariant);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        public static class InventoryGui_SetupRequirementList_Patch
        {
            public static void Postfix(InventoryGui __instance, int quality, Player player, bool allowedQuality)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.PostSetupRequirementList(__instance.m_selectedRecipe.Key, __instance.m_selectedRecipe.Value, quality, player, allowedQuality);
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
}
