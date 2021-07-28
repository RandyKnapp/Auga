using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
        public CraftingRequirementsPanel GenericCraftingRequirementsPanel;
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
        public Button CustomVariantButton;
        public GameObject CustomVariantDialog;
        public Text CustomVariantText;
        public Transform CraftProgressPanel;
        public Image CraftProgressBar;
        public GameObject ResultsPanelPrefab;

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
        private Action<bool> _onShowCustomVariantDialog;

        public virtual void Initialize(InventoryGui inventoryGui)
        {
            TabController.OnTabChanged += OnTabChanged;
            ActivatePanel(CraftingRequirementsPanel);
            CustomVariantButton.onClick.AddListener(OnCustomVariantButtonClicked);
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
                else
                {
                    ActivatePanel(GenericCraftingRequirementsPanel);
                    inventoryGui.m_tabCraft.interactable = true;
                    inventoryGui.m_tabUpgrade.interactable = true;
                    inventoryGui.UpdateCraftingPanel();
                }
            }
        }

        public virtual void ActivatePanel(CraftingRequirementsPanel panel)
        {
            CraftingRequirementsPanel.gameObject.SetActive(false);
            UpgradeRequirementsPanel.gameObject.SetActive(false);
            MaxQualityUpgradeRequirementsPanel.gameObject.SetActive(false);
            GenericCraftingRequirementsPanel.gameObject.SetActive(false);

            var inventoryGui = InventoryGui.instance;
            _currentPanel = panel;
            panel.gameObject.SetActive(true);
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

            var player = Player.m_localPlayer;
            if (player != null && player.m_currentStation == null)
            {
                OnTabChanged(-1, 0);
            }

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

            UpdateVariantButtonVisibility();
            UpdateRequirementsContainerVisibility();
        }

        public virtual void UpdateVariantButtonVisibility()
        {
            var showingAnyVariantButton = VariantButton.gameObject.activeInHierarchy || CustomVariantButton.gameObject.activeInHierarchy;
            VariantButtonContainer.SetActive(showingAnyVariantButton);
            NonVariantButtonContainer.SetActive(!showingAnyVariantButton);
        }

        public virtual void UpdateRequirementsContainerVisibility()
        {
            var hasRecipe = TabController.SelectedIndex > 1 || InventoryGui.instance.m_selectedRecipe.Key != null;
            var showingVariants = InventoryGui.instance.m_variantDialog.gameObject.activeInHierarchy || CustomVariantDialog.gameObject.activeInHierarchy;

            RequirementsContainer.SetActive(hasRecipe && !showingVariants);
        }

        public virtual void PostSetupRequirementList(Recipe recipe, ItemDrop.ItemData item, int quality, Player player, bool allowedWorkbenchQuality)
        {
            if (TabController.SelectedIndex == 1 && item != null)
            {
                var maxQuality = item.m_shared.m_maxQuality == item.m_quality;
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

            _currentPanel.WireFrame.Set(states, canCraft);
        }

        public void OnCustomVariantButtonClicked()
        {
            CustomVariantDialog.gameObject.SetActive(!CustomVariantDialog.gameObject.activeSelf);
            UpdateRequirementsContainerVisibility();
            _onShowCustomVariantDialog?.Invoke(CustomVariantDialog.gameObject.activeSelf);
        }

        public virtual Text EnableCustomVariantDialog(string buttonText, Action<bool> onShow)
        {
            _onShowCustomVariantDialog = onShow;

            VariantButton.gameObject.SetActive(false);
            CustomVariantButton.gameObject.SetActive(true);
            CustomVariantButton.GetComponentInChildren<Text>().text = Localization.instance.Localize(buttonText);

            UpdateVariantButtonVisibility();

            return CustomVariantText;
        }

        public virtual void DisableCustomVariantDialog()
        {
            _onShowCustomVariantDialog = null;
            CustomVariantButton.gameObject.SetActive(false);
            UpdateVariantButtonVisibility();
        }

        public virtual GameObject CreateResultsPanel()
        {
            return Instantiate(ResultsPanelPrefab, transform, false);
        }
    }
}
