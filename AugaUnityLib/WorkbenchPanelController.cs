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
        public RectTransform TabTitleContainer;
        public RectTransform TabButtonContainer;
        public RectTransform RequirementsContainer;
        public Text TabTitlePrefab;
        public CraftingRequirementsPanel GenericWorkbenchTabRequirementsPrefab;
        public TabButton TabButtonBasePrefab;

        private int _rememberTabIndex = -1;

        // ReSharper disable once InconsistentNaming
        public static WorkbenchPanelController instance;

        public virtual void Awake()
        {
            instance = this;
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

                var tabController = GetComponent<AugaTabController>();
                if (atWorkbench)
                {
                    _rememberTabIndex = tabController.SelectedIndex;
                    tabController.SelectTab(1);
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

        public bool HasWorkbenchTab(string tabID)
        {
            return WorkbenchTabController.TabButtons.Exists(x => x.name == tabID);
        }

        public virtual void AddWorkbenchTab(string tabID, Sprite tabIcon, string tabLabel, Action<int> onTabSelected, out Text tabTitle, out TabButton tabButton, out CraftingRequirementsPanel requirementsPanel, out ComplexTooltip itemInfo)
        {
            tabTitle = null;
            tabButton = null;
            requirementsPanel = null;
            itemInfo = null;

            if (HasWorkbenchTab(tabID))
            {
                return;
            }

            tabTitle = Instantiate(TabTitlePrefab, TabTitleContainer, true);
            tabTitle.text = Localization.instance.Localize(tabLabel);

            tabButton = Instantiate(TabButtonBasePrefab, TabButtonContainer);
            tabButton.name = tabID;
            tabButton.Icon.sprite = tabIcon;
            tabButton.SetSelected(true);
            tabButton.SetSelected(false);

            itemInfo = CraftingPanel.ItemInfo;

            requirementsPanel = CraftingPanel.GenericCraftingRequirementsPanel;

            WorkbenchTabController.TabTitles.Add(tabTitle);
            WorkbenchTabController.TabButtons.Add(tabButton);
            WorkbenchTabController.TabContents.Add(requirementsPanel.gameObject);

            var index = WorkbenchTabController.TabButtons.Count - 1;
            WorkbenchTabController.OnTabChanged += (_, current) =>
            {
                if (current == index)
                {
                    onTabSelected(index);
                }
            };
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
    }
}
