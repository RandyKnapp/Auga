using System.Collections.Generic;
using JetBrains.Annotations;
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
        public CraftingRequirementsPanel MaxQualityUpgradeRequirementsPanel;
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

        private static AugaCraftingPanel _instance;
        private CraftingRequirementsPanel _currentPanel;

        public virtual void Initialize(InventoryGui inventoryGui)
        {
            TabController.OnTabChanged += OnTabChanged;
            ActivatePanel(CraftingRequirementsPanel);
        }

        private void OnTabChanged(int previousTabIndex, int currentTabIndex)
        {
            VariantDialog.OnClose();

            var inventoryGui = InventoryGui.instance;
            if (inventoryGui != null)
            {
                if (currentTabIndex == 0)
                {
                    ActivatePanel(CraftingRequirementsPanel);
                    inventoryGui.OnTabCraftPressed();
                }
                else if (currentTabIndex == 1)
                {
                    ActivatePanel(UpgradeRequirementsPanel);
                    inventoryGui.OnTabUpgradePressed();
                }
            }
        }

        public virtual void ActivatePanel(CraftingRequirementsPanel panel)
        {
            var inventoryGui = InventoryGui.instance;
            _currentPanel = panel;
            panel.Activate(inventoryGui, ItemInfo);
            SetRecipe(inventoryGui.m_selectedRecipe.Key, inventoryGui.m_selectedRecipe.Value, inventoryGui.m_selectedVariant);
        }

        [UsedImplicitly]
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

        [UsedImplicitly]
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
                if (item != null)
                {
                    var quality = Mathf.Min(item.m_quality + 1, item.m_shared.m_maxQuality);
                    ItemInfo.SetItem(item, quality, variant);
                }
                else
                {
                    ItemInfo.SetItem(recipe.m_item.m_itemData, 1, variant);
                }
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

        public virtual void PostSetupRequirementList(Recipe recipe, ItemDrop.ItemData item, int quality, Player player, bool allowedWorkbenchQuality)
        {
            if (item != null)
            {
                var maxQuality = item.m_shared.m_maxQuality == item.m_quality;
                UpgradeRequirementsPanel.gameObject.SetActive(!maxQuality);
                MaxQualityUpgradeRequirementsPanel.gameObject.SetActive(maxQuality);
                if (maxQuality)
                {
                    ActivatePanel(MaxQualityUpgradeRequirementsPanel);
                    return;
                }
                else
                {
                    ActivatePanel(UpgradeRequirementsPanel);
                    SetRecipe(recipe, item, item.m_variant);
                }
            }

            if (_currentPanel == null || _currentPanel.WireFrame == null)
            {
                return;
            }

            //Debug.LogWarning($"UpdateWireframe Input: recipe={recipe}, item={item}, quality={quality}, allowedWorkbenchQuality={allowedWorkbenchQuality}");
            var canCraft = allowedWorkbenchQuality;
            var states = new List<WireState>();
            var index = 0;
            if (allowedWorkbenchQuality)
            {
                foreach (var resource in recipe.m_resources)
                {
                    var amountRequired = resource.GetAmount(quality);
                    if (resource.m_resItem != null && amountRequired > 0)
                    {
                        var playerInventoryAmount = player.GetInventory().CountItems(resource.m_resItem.m_itemData.m_shared.m_name);
                        var have = playerInventoryAmount >= amountRequired;
                        //Debug.Log($"  {index}: res={resource.m_resItem.m_itemData.m_shared.m_name}, amountRequired={amountRequired}, playerInventoryAmount={playerInventoryAmount}");
                        states.Add(have ? WireState.Have : WireState.DontHave);
                        canCraft = canCraft && have;
                        ++index;
                    }
                }
            }

            for (; index < _currentPanel.WireFrame.Wires.Length; ++index)
            {
                states.Add(WireState.Absent);
            }

            var currentCraftingStation = player.GetCurrentCraftingStation();
            var requiredCraftingStation = recipe.GetRequiredStation(quality);
            var requiredStationLevel = recipe.GetRequiredStationLevel(quality);
            var hasStation = requiredCraftingStation == null || currentCraftingStation == null || currentCraftingStation.m_name == requiredCraftingStation.m_name;
            var stationLevel = requiredCraftingStation == null || currentCraftingStation == null || currentCraftingStation.GetLevel() >= requiredStationLevel;
            canCraft = canCraft && hasStation && stationLevel;

            //Debug.LogWarning($"UpdateWireframe Values: states={string.Join(",", states)}, canCraft={canCraft}");
            _currentPanel.WireFrame.Set(states, canCraft);
        }
    }
}
