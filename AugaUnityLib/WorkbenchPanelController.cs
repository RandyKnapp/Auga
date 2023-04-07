using System;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(AugaTabController))]
    public class WorkbenchPanelController : MonoBehaviour
    {
        public GameObject DefaultContent;
        public AugaTabController DefaultTabController;
        public GameObject WorkbenchContent;
        public AugaTabController WorkbenchTabController;
        public AugaCraftingPanel CraftingPanel;
        public RectTransform PlayerPanelTabTitleContainer;
        public RectTransform PlayerPanelTabButtonContainer;
        public RectTransform WorkbenchTabTitleContainer;
        public RectTransform WorkbenchTabButtonContainer;
        public RectTransform RequirementsContainer;
        public Text PlayerPanelTabTitlePrefab;
        public Text WorkbenchTabTitlePrefab;
        public CraftingRequirementsPanel GenericWorkbenchTabRequirementsPrefab;
        public TabButton TabButtonBasePrefab;

        private int _rememberTabIndex = -1;
        private InventoryGui _inventoryGui;

        // ReSharper disable once InconsistentNaming
        public static WorkbenchPanelController instance;

        public virtual void Awake()
        {
            instance = this;
            _inventoryGui = InventoryGui.instance;
        }

        public virtual void Destroy()
        {
            instance = null;
        }

        public virtual void OnEnable()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                DefaultContent.SetActive(false);
                WorkbenchContent.SetActive(false);
            }
            else
            {
                var atWorkbench = player.GetCurrentCraftingStation() != null;
                DefaultContent.SetActive(!atWorkbench);
                WorkbenchContent.SetActive(atWorkbench); 
                if (atWorkbench)
                {
                    _inventoryGui.m_repairButton = CraftingPanel.RepairButton;
                    _inventoryGui.m_repairButtonGlow = CraftingPanel.RepairGlow;
                    _inventoryGui.m_repairPanel = CraftingPanel.RepairButton.transform;
                    _inventoryGui.m_repairButton.onClick.RemoveAllListeners();
                    _inventoryGui.m_repairButton.onClick.AddListener(_inventoryGui.OnRepairPressed);
                    CraftingPanel.DefaultRepairButton.gameObject.SetActive(false);
                }
                else
                {
                    if (player.m_noPlacementCost)
                    {
                        CraftingPanel.DefaultRepairButton.gameObject.SetActive(true);
                        _inventoryGui.m_repairButton = CraftingPanel.DefaultRepairButton;
                        _inventoryGui.m_repairButtonGlow = CraftingPanel.DefaultRepairGlow;
                        _inventoryGui.m_repairPanel = CraftingPanel.DefaultRepairButton.transform;
                        _inventoryGui.m_repairButton.onClick.RemoveAllListeners();
                        _inventoryGui.m_repairButton.onClick.AddListener(_inventoryGui.OnRepairPressed);
                    }
                    else
                    {
                        CraftingPanel.DefaultRepairButton.gameObject.SetActive(false);
                    }
                }
                
                

                var tabController = GetComponent<AugaTabController>();
                if (atWorkbench)
                {
                    _rememberTabIndex = tabController.SelectedIndex;
                    tabController.SelectTab(1);
                    WorkbenchContent.GetComponent<AugaTabController>().SelectTab(0);
                }
                else if (_rememberTabIndex >= 0)
                {
                    tabController.SelectTab(_rememberTabIndex);
                    WorkbenchContent.GetComponent<AugaTabController>().SelectTab(0);
                }
            }
        }

        public virtual void OnDisable()
        {
            if (DefaultContent.activeSelf)
            {
                var tabController = GetComponent<AugaTabController>();
                _rememberTabIndex = tabController.SelectedIndex;
            }
        }

        public bool HasPlayerPanelTab(string tabID)
        {
            return DefaultTabController.TabButtons.Exists(x => x.name == tabID);
        }

        public bool HasWorkbenchTab(string tabID)
        {
            return WorkbenchTabController.TabButtons.Exists(x => x.name == tabID);
        }

        private void AddTab(AugaTabController controller, Text titlePrefab, Transform titleContainer, Transform tabButtonContainer, string tabID, Sprite tabIcon, string tabLabel, Action<int> onTabSelected, bool mimicVanillaDescription, out Text tabTitle, out TabButton tabButton, GameObject content)
        {
            tabTitle = Instantiate(titlePrefab, titleContainer, true);
            tabTitle.text = Localization.instance.Localize(tabLabel);

            tabButton = Instantiate(TabButtonBasePrefab, tabButtonContainer);
            tabButton.name = tabID;
            if (tabIcon != null)
            {
                tabButton.Icon.sprite = tabIcon;
            }
            tabButton.SetSelected(true);
            tabButton.SetSelected(false);

            controller.TabTitles.Add(tabTitle);
            controller.TabButtons.Add(tabButton);
            controller.TabContents.Add(content);

            var index = controller.TabButtons.Count - 1;
            var mimicVanilla = mimicVanillaDescription;
            controller.OnTabChanged += (previous, current) =>
            {
                if (current == index)
                {
                    var tabContent = controller.TabContents[current];
                    var mimic = tabContent.GetComponent<MimicVanillaCraftingTab>();
                    if (mimic != null)
                        mimic.enabled = mimicVanilla;

                    onTabSelected(index);
                }
            };
        }

        public virtual void AddPlayerPanelTab(string tabID, Sprite tabIcon, string tabLabel, Action<int> onTabSelected, out Text tabTitle, out TabButton tabButton, out GameObject content)
        {
            tabTitle = null;
            tabButton = null;
            content = null;

            if (HasPlayerPanelTab(tabID))
            {
                return;
            }

            var contentContainer = transform.Find("TabContent");
            content = new GameObject($"TabContent_{tabLabel}", typeof(RectTransform));
            content.transform.SetParent(contentContainer);
            content.SetActive(false);
            var rt = (RectTransform)content.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 564);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 780);

            AddTab(DefaultTabController, PlayerPanelTabTitlePrefab, PlayerPanelTabTitleContainer, PlayerPanelTabButtonContainer, tabID, tabIcon, tabLabel, onTabSelected, false, out tabTitle, out tabButton, content);

            var index = DefaultTabController.TabButtons.Count - 1;
            tabButton.Button.onClick.AddListener(() => DefaultTabController.SelectTab(index));
        }

        public virtual void AddWorkbenchTab(string tabID, Sprite tabIcon, string tabLabel, Action<int> onTabSelected, bool mimicVanillaDescription, out Text tabTitle, out TabButton tabButton, out CraftingRequirementsPanel requirementsPanel, out ComplexTooltip itemInfo)
        {
            tabTitle = null;
            tabButton = null;
            requirementsPanel = null;
            itemInfo = null;

            if (HasWorkbenchTab(tabID))
            {
                return;
            }

            itemInfo = CraftingPanel.ItemInfo;
            requirementsPanel = CraftingPanel.GenericCraftingRequirementsPanel;
            AddTab(WorkbenchTabController, WorkbenchTabTitlePrefab, WorkbenchTabTitleContainer, WorkbenchTabButtonContainer, tabID, tabIcon, tabLabel, onTabSelected, mimicVanillaDescription, out tabTitle, out tabButton, requirementsPanel.gameObject);
        }

        public bool IsTabActive(GameObject tabButton)
        {
            var tabButtonComponent = tabButton.GetComponent<TabButton>();
            if (tabButtonComponent != null)
            {
                return tabButtonComponent.Selected;
            }

            var button = tabButton.GetComponent<Button>();
            if (button != null)
            {
                return !button.interactable;
            }

            return false;
        }

        public bool IsTabActiveById(string tabID)
        {
            var tabButton = WorkbenchTabController.TabButtons.Find(x => x.name == tabID);
            if (tabButton == null)
                return false;

            return tabButton.Selected;
        }
    }
}
