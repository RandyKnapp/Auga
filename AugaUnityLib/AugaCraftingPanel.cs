using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaCraftingPanel : MonoBehaviour
    {
        public Text WorkbenchName;
        public Image WorkbenchIcon;
        public Text WorkbenchLevel;
        public RectTransform WorkbenchLevelRoot;
        public Button RepairButton;
        public Image RepairGlow;
        public AugaTabController TabController;
        public CraftingRequirementsPanel CraftingRequirementsPanel;
        public CraftingRequirementsPanel UpgradeRequirementsPanel;
        public UpgradeRequirementsWireFrame UpgradeWireFrame;
        public Text ItemCraftType;
        public RectTransform RecipeList;
        public Scrollbar RecipeListScrollbar;
        public ScrollRectEnsureVisible RecipeListEnsureVisible;
        public GameObject RecipeItemPrefab;
        public ComplexTooltip ItemInfo;
        public GameObject RequirementsContainer;
        public Button CraftButton;
        public Button CraftCancelButton;
        public Button VariantButton;
        public GameObject VariantButtonContainer;
        public GameObject NonVariantButtonContainer;
        public VariantDialog VariantDialog;
        public Transform CraftProgressPanel;
        public Image CraftProgressBar;

        [Header("Dummy Objects")]
        public Image DummyIcon;
        public Text DummyName;
        public Text DummyDescription;
        public Transform DummyRepairPanelSelection;
        public Button DummyCraftTabButton;
        public Button DummyUpgradeTabButton;
        public GuiBar DummyCraftProgressBar;
        public RectTransform DummyQualityPanel;
        public Image DummyMinStationLevelIcon;
        public Toggle DummyPvpToggle;

        private static AugaCraftingPanel _instance;

        public virtual void Initialize(InventoryGui inventoryGui)
        {
            TabController.OnTabChanged += OnTabChanged;
            CraftingRequirementsPanel.Activate(inventoryGui, ItemInfo);
            SetRecipe(inventoryGui.m_selectedRecipe.Key, inventoryGui.m_selectedRecipe.Value, inventoryGui.m_selectedVariant);
        }

        private void OnTabChanged(int previousTabIndex, int currentTabIndex)
        {
            VariantDialog.OnClose();

            var inventoryGui = InventoryGui.instance;
            if (inventoryGui != null)
            {
                if (currentTabIndex == 0)
                {
                    CraftingRequirementsPanel.Activate(InventoryGui.instance, ItemInfo);
                    inventoryGui.OnTabCraftPressed();
                }
                else if (currentTabIndex == 1)
                {
                    UpgradeRequirementsPanel.Activate(InventoryGui.instance, ItemInfo);
                    inventoryGui.OnTabUpgradePressed();
                }
            }
        }

        public virtual void OnEnable()
        {
            if (_instance != null)
            {
                Debug.LogError($"Too many instances of AugaCraftingPanel exist! other={_instance} parent={_instance.transform.parent}");
            }
            _instance = this;

            var inventoryGui = InventoryGui.instance;
            SetRecipe(inventoryGui.m_selectedRecipe.Key, inventoryGui.m_selectedRecipe.Value, inventoryGui.m_selectedVariant);
        }

        public virtual void OnDisable()
        {
            _instance = null;
            VariantDialog.OnClose();
        }

        public void SetRecipe(Recipe recipe, ItemDrop.ItemData item, int variant)
        {
            if (recipe == null)
            {
                ItemInfo.gameObject.SetActive(false);
            }
            else
            {
                var quality = item == null ? 1 : item.m_quality + 1;
                ItemInfo.SetItem(recipe.m_item.m_itemData, quality, variant);
                ItemInfo.gameObject.SetActive(true);
            }

            VariantDialog.OnClose();
            UpdateRequirementsContainerVisibility();
        }

        public void OnUpdateRecipe(InventoryGui instance)
        {
            if (instance.m_craftTimer >= 0)
            {
                CraftProgressBar.gameObject.SetActive(true);
                var percent = instance.m_craftTimer / instance.m_craftDuration;
                CraftProgressBar.fillAmount = percent;
            }

            VariantButtonContainer.SetActive(VariantButton.gameObject.activeSelf);
            NonVariantButtonContainer.SetActive(!VariantButton.gameObject.activeSelf);

            UpdateRequirementsContainerVisibility();
        }

        public virtual void UpdateRequirementsContainerVisibility()
        {
            var hasRecipe = InventoryGui.instance.m_selectedRecipe.Key != null;
            var showingVariants = InventoryGui.instance.m_variantDialog.gameObject.activeSelf;

            RequirementsContainer.SetActive(hasRecipe && !showingVariants);
        }

        public virtual void UpdateUpgradeWireFrame(Recipe recipe, ItemDrop.ItemData item, int quality, Player player, bool allowedQuality)
        {
            if (item == null || UpgradeWireFrame == null)
            {
                return;
            }

            var canCraft = allowedQuality;
            var states = new List<WireState>();
            int index = 0;
            if (allowedQuality)
            {
                foreach (Piece.Requirement resource in recipe.m_resources)
                {
                    if (resource.m_resItem != null)
                    {
                        var playerInventoryAmount = player.GetInventory().CountItems(resource.m_resItem.m_itemData.m_shared.m_name);
                        var amountRequired = resource.GetAmount(quality);
                        var have = playerInventoryAmount >= amountRequired;
                        states.Add(have ? WireState.Have : WireState.DontHave);
                        canCraft = canCraft && have;
                        ++index;
                    }
                }
            }

            for (; index < UpgradeWireFrame.Wires.Length; ++index)
            {
                states.Add(WireState.Absent);
            }

            var currentCraftingStation = player.GetCurrentCraftingStation();
            var requiredStationLevel = recipe.GetRequiredStationLevel(quality);
            canCraft = canCraft && currentCraftingStation != null && currentCraftingStation.GetLevel() >= requiredStationLevel;

            UpgradeWireFrame.Set(states.ToArray(), canCraft);
        }
    }
}
