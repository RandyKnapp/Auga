using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class InventoryPanel_Patches
    {
        public static AugaCraftingPanel CraftingPanel;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                AddItemIconMaterial.IconMaterial = __instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;

                __instance.m_playerGrid.m_onSelected = null;
                __instance.m_playerGrid.m_onRightClick = null;
                __instance.m_containerGrid.m_onSelected = null;
                __instance.m_containerGrid.m_onRightClick = null;

                var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);
                __instance.m_player = playerInventory.RectTransform();
                __instance.m_playerGrid = playerInventory.Find("PlayerGrid").GetComponent<InventoryGrid>();
                __instance.m_playerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_playerGrid.m_onRightClick += __instance.OnRightClickItem;
                __instance.m_weight = playerInventory.Find("Weight/Text").GetComponent<Text>();
                __instance.m_armor = playerInventory.Find("Armor/Text").GetComponent<Text>();

                var containerInventory = __instance.Replace("root/Container", Auga.Assets.InventoryScreen, "root/Container", ReplaceFlags.Instantiate | ReplaceFlags.DestroyOriginal);
                __instance.m_container = containerInventory.RectTransform();
                __instance.m_containerName = containerInventory.Find("ContainerHeader/Name").GetComponent<Text>();
                __instance.m_containerGrid = containerInventory.Find("ContainerGrid").GetComponent<InventoryGrid>();
                __instance.m_containerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_containerGrid.m_onRightClick += __instance.OnRightClickItem;
                __instance.m_containerWeight = containerInventory.Find("Weight/Text").GetComponent<Text>();
                containerInventory.Find("TakeAll").GetComponent<Button>().onClick.AddListener(__instance.OnTakeAll);

                var oldCraftingPanel = __instance.transform.Find("root/Crafting");
                var craftingPanelSiblingIndex = oldCraftingPanel.GetSiblingIndex();
                Object.Destroy(oldCraftingPanel.gameObject);
                Object.Destroy(__instance.transform.Find("root/Info").gameObject);

                var rightPanel = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/RightPanel"), containerInventory.parent, false);
                rightPanel.SetSiblingIndex(craftingPanelSiblingIndex);
                CraftingPanel = rightPanel.GetComponentInChildren<AugaCraftingPanel>(true);
                __instance.m_playerName = rightPanel.Find("DefaultContent/TitleContainer/PlayerPanelTitle").GetComponent<Text>();
                __instance.m_pvp = rightPanel.Find("TabContent/TabContent_PVP/PVPToggle").GetComponent<Toggle>();
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

                var info = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/Info"), containerInventory.parent, false);
                info.Find("Texts").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTexts);
                info.Find("Trophies").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTrophies);

                Localization.instance.Localize(__instance.transform);
            }
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        public static class MessageHud_Awake_Patch
        {
            public static void Postfix(MessageHud __instance)
            {
                __instance.gameObject.AddComponent<AugaMessageLog>();
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        public static class InventoryGrid_UpdateGui_Patch
        {
            public static void Postfix(InventoryGrid __instance)
            {
                if (__instance.name == "PlayerGrid")
                {
                    Vector2 startPos = new Vector2(__instance.RectTransform().rect.width / 2f, 0.0f) - new Vector2(__instance.GetWidgetSize().x, 0.0f) * 0.5f;
                    foreach (var element in __instance.m_elements)
                    {
                        if (element.m_pos.y != 0)
                        {
                            Vector2 currentPosition = new Vector3(element.m_pos.x * (__instance.m_elementSpace), (element.m_pos.y * -__instance.m_elementSpace) - 18);
                            element.m_go.RectTransform().anchoredPosition = startPos + currentPosition;
                        }
                    }
                }

                foreach (var element in __instance.m_elements)
                {
                    var itemTooltip = element.m_go.GetComponent<ItemTooltip>();
                    if (itemTooltip != null && !element.m_used)
                    {
                        itemTooltip.Item = null;
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

        [HarmonyPatch(typeof(TextsDialog))]
        public static class TextsDialog_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddActiveEffects))]
            public static bool AddActiveEffects_Prefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddLog))]
            public static bool AddLog_Prefix()
            {
                return false;
            }
        }
    }
}
