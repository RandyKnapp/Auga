using System;
using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public static class API
    {
        // Workbench Tabs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static bool HasWorkbenchTab(string tabID)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.HasWorkbenchTab(tabID);
        }

        [UsedImplicitly]
        public static WorkbenchTabData AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
        {
            if (WorkbenchPanelController.instance != null)
            {
                var data = new WorkbenchTabData();

                WorkbenchPanelController.instance.AddWorkbenchTab(tabID, tabIcon, tabTitleText, onTabSelected, out data.TabTitle, out var tabButtonX, out var requirementsPanelX, out var itemInfoX);
                data.Index = WorkbenchPanelController.instance.WorkbenchTabController.TabButtons.Count - 1;
                data.TabButtonGO = tabButtonX.gameObject;
                data.RequirementsPanelGO = requirementsPanelX.gameObject;
                data.ItemInfoGO = itemInfoX.gameObject;

                return data;
            }

            return null;
        }

        [UsedImplicitly]
        public static bool IsTabActive(GameObject tabButton)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActive(tabButton);
        }

        [UsedImplicitly]
        public static Button GetCraftingTabButton()
        {
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[0].GetComponent<Button>();
        }

        [UsedImplicitly]
        public static Button GetUpgradeTabButton()
        {
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[1].GetComponent<Button>();
        }


        // Tooltip Text Boxes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool TextBoxCheck(GameObject tooltipTextBoxGO, out TooltipTextBox tooltipTextBox)
        {
            tooltipTextBox = tooltipTextBoxGO.GetComponent<TooltipTextBox>();
            if (tooltipTextBox == null)
            {
                Auga.LogError("Must call TooltipTextBox interface method with a game object with a TooltipTextBox component on it.");
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize = true)
        {
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(t, s, localize);
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize = true)
        {
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, localize);
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize = true)
        {
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, b, localize);
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize = true)
        {
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, b, parenthetical, localize);
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize = true)
        {
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddUpgradeLine(label, value1, value2, color2, localize);
        }


        // Complex Tooltip
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool ComplexTooltipCheck(GameObject complexTooltipGO, out ComplexTooltip complexTooltip)
        {
            complexTooltip = complexTooltipGO.GetComponent<ComplexTooltip>();
            if (complexTooltip == null)
            {
                Auga.LogError("Must call ComplexTooltip interface method with a game object with a ComplexTooltip component on it.");
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        public static void ComplexTooltip_ClearTextBoxes(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.ClearTextBoxes();
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddTwoColumnTextBox(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.TwoColumnTextBoxPrefab).gameObject;
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddCenteredTextBox(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.CenteredTextBoxPrefab).gameObject;
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddUpgradeLabels(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.UpgradeLabelsPrefab).gameObject;
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddUpgradeTwoColumnTextBox(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.UpgradeTwoColumnTextBoxPrefab).gameObject;
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddDivider(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddDivider();
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetTopic(GameObject complexTooltipGO, string topic)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetTopic(topic);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetSubtitle(GameObject complexTooltipGO, string topic)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetSubtitle(topic);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetItem(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetItem(item, quality, variant);
        }


        // Requirements Panel
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool RequirementsPanelCheck(GameObject requirementsPanelGO, out CraftingRequirementsPanel requirementsPanel)
        {
            requirementsPanel = requirementsPanelGO.GetComponent<CraftingRequirementsPanel>();
            if (requirementsPanel == null)
            {
                Auga.LogError("Must call RequirementsPanel interface method with a game object with a CraftingRequirementsPanel component on it.");
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        public static void RequirementsPanel_SetWires(GameObject requirementsPanelGO, RequirementWireState[] wireStates, bool canCraft)
        {
            if (!RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel))
            {
                return;
            }

            var convertedWireStates = wireStates.Select(wireState => (WireState)wireState).ToList();
            requirementsPanel.WireFrame.Set(convertedWireStates, canCraft);
        }
    }
}
