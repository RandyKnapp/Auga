using System;
using System.Linq;
using AugaUnity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    public static class API
    {
        // Fonts & Assets
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static Font GetBoldFont()
        {
            return Auga.Assets.SourceSansProBold;
        }

        [UsedImplicitly]
        public static Font GetSemiBoldFont()
        {
            return Auga.Assets.SourceSansProSemiBold;
        }

        [UsedImplicitly]
        public static Font GetRegularFont()
        {
            return Auga.Assets.SourceSansProRegular;
        }

        [UsedImplicitly]
        public static Sprite GetItemBackgroundSprite()
        {
            return Auga.Assets.ItemBackgroundSprite;
        }

        // Panels & Buttons
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static GameObject Panel_Create(Transform parent, Vector2 size, string name, bool withCornerDecoration)
        {
            var panel = Object.Instantiate(Auga.Assets.PanelBase, parent);
            panel.name = name;
            if (!withCornerDecoration)
            {
                foreach (Transform child in panel.transform.Find("Background"))
                {
                    Object.Destroy(child.gameObject);
                }
            }

            var rt = (RectTransform)panel.transform;
            rt.pivot = rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            return panel;
        }

        [UsedImplicitly]
        public static Button SmallButton_Create(Transform parent, string name, string labelText)
        {
            var button = Object.Instantiate(Auga.Assets.ButtonSmall, parent);
            button.name = name;
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
        }

        [UsedImplicitly]
        public static Button MediumButton_Create(Transform parent, string name, string labelText)
        {
            var button = Object.Instantiate(Auga.Assets.ButtonMedium, parent);
            button.name = name;
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
        }

        [UsedImplicitly]
        public static Button FancyButton_Create(Transform parent, string name, string labelText)
        {
            var button = Object.Instantiate(Auga.Assets.ButtonFancy, parent);
            button.name = name;
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
        }

        [UsedImplicitly]
        public static Button SettingsButton_Create(Transform parent, string name, string labelText)
        {
            var button = Object.Instantiate(Auga.Assets.ButtonSettings, parent);
            button.name = name;
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
        }

        [UsedImplicitly]
        public static Button DiamondButton_Create(Transform parent, string name, [CanBeNull] Sprite icon)
        {
            var button = Object.Instantiate(Auga.Assets.DiamondButton, parent);
            button.name = name;

            var image = button.GetComponentInChildren<Image>();
            if (icon == null)
            {
                image.enabled = false;
            }
            else
            {
                image.sprite = icon;
            }

            return button.GetComponent<Button>();
        }

        [UsedImplicitly]
        public static GameObject Divider_CreateSmall(Transform parent, string name, float width = -1)
        {
            var divider = Object.Instantiate(Auga.Assets.DividerSmall, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return divider;
        }

        [UsedImplicitly]
        public static Tuple<GameObject, GameObject> Divider_CreateMedium(Transform parent, string name, float width = -1)
        {
            var divider = Object.Instantiate(Auga.Assets.DividerMedium, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return new Tuple<GameObject, GameObject>(divider, divider.transform.Find("Content").gameObject);
        }

        [UsedImplicitly]
        public static Tuple<GameObject, GameObject> Divider_CreateLarge(Transform parent, string name, float width = -1)
        {
            var divider = Object.Instantiate(Auga.Assets.DividerLarge, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return new Tuple<GameObject, GameObject>(divider, divider.transform.Find("Content").gameObject);
        }

        [UsedImplicitly]
        public static void Button_SetTextColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, Color baseTextColor)
        {
            var colorValues = button.GetComponent<ColorButtonTextValues>();
            if (colorValues != null)
            {
                colorValues.TextColors.normalColor = normal;
                colorValues.TextColors.highlightedColor = highlighted;
                colorValues.TextColors.pressedColor = pressed;
                colorValues.TextColors.selectedColor = selected;
                colorValues.TextColors.disabledColor = disabled;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = baseTextColor;
            }
        }

        [UsedImplicitly]
        public static void Button_OverrideTextColor(Button button, Color color)
        {
            var colorValues = button.GetComponent<ColorButtonTextValues>();
            if (colorValues != null)
            {
                Object.Destroy(colorValues);
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = color;
            }
        }

        [UsedImplicitly]
        public static void Tooltip_MakeSimpleTooltip(GameObject obj)
        {
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.SimpleTooltip;
        }

        [UsedImplicitly]
        public static void Tooltip_MakeItemTooltip(GameObject obj, ItemDrop.ItemData item)
        {
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;

            var itemTooltip = obj.GetComponent<ItemTooltip>();
            if (itemTooltip == null)
            {
                itemTooltip = obj.AddComponent<ItemTooltip>();
            }

            itemTooltip.Item = item;
        }

        [UsedImplicitly]
        public static void Tooltip_MakeFoodTooltip(GameObject obj, Player.Food food)
        {
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;

            var foodTooltip = obj.GetComponent<FoodTooltip>();
            if (foodTooltip == null)
            {
                foodTooltip = obj.AddComponent<FoodTooltip>();
            }

            foodTooltip.Food = food;
        }

        [UsedImplicitly]
        public static void Tooltip_MakeStatusEffectTooltip(GameObject obj, StatusEffect statusEffect)
        {
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;

            var statusEffectTooltip = obj.GetComponent<StatusTooltip>();
            if (statusEffectTooltip == null)
            {
                statusEffectTooltip = obj.AddComponent<StatusTooltip>();
            }

            statusEffectTooltip.StatusEffect = statusEffect;
        }

        [UsedImplicitly]
        public static void Tooltip_MakeSkillTooltip(GameObject obj, Skills.Skill skill)
        {
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;

            var skillTooltip = obj.GetComponent<SkillTooltip>();
            if (skillTooltip == null)
            {
                skillTooltip = obj.AddComponent<SkillTooltip>();
            }

            skillTooltip.Skill = skill;
        }

        // Player Panel Tabs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static bool PlayerPanel_HasTab(string tabID)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.HasPlayerPanelTab(tabID);
        }

        [UsedImplicitly]
        public static PlayerPanelTabData PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
        {
            if (WorkbenchPanelController.instance != null)
            {
                var data = new PlayerPanelTabData();

                WorkbenchPanelController.instance.AddPlayerPanelTab(tabID, tabIcon, tabTitleText, onTabSelected, out data.TabTitle, out var tabButton, out data.ContentGO);
                data.Index = WorkbenchPanelController.instance.DefaultTabController.TabButtons.Count - 1;
                data.TabButtonGO = tabButton.gameObject;

                return data;
            }

            return null;
        }

        [UsedImplicitly]
        public static bool PlayerPanel_IsTabActive(GameObject tabButton)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActive(tabButton);
        }

        [UsedImplicitly]
        public static Button PlayerPanel_GetTabButton(int index)
        {
            if (WorkbenchPanelController.instance != null && index >= 0 && index < WorkbenchPanelController.instance.DefaultTabController.TabButtons.Count)
            {
                return WorkbenchPanelController.instance.DefaultTabController.TabButtons[index].Button;
            }
            return null;
        }

        // Workbench Tabs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static bool Workbench_HasWorkbenchTab(string tabID)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.HasWorkbenchTab(tabID);
        }

        [UsedImplicitly]
        public static WorkbenchTabData Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
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
        public static bool Workbench_IsTabActive(GameObject tabButton)
        {
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActive(tabButton);
        }

        [UsedImplicitly]
        public static Button Workbench_GetCraftingTabButton()
        {
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[0].GetComponent<Button>();
        }

        [UsedImplicitly]
        public static Button Workbench_GetUpgradeTabButton()
        {
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[1].GetComponent<Button>();
        }

        [UsedImplicitly]
        public static GameObject Workbench_CreateNewResultsPanel()
        {
            return WorkbenchPanelController.instance?.CraftingPanel.CreateResultsPanel();
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
        public static void ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData> listener)
        {
            ComplexTooltip.OnComplexTooltipGeneratedForItem += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddFoodTooltipCreatedListener(Action<GameObject, Player.Food> listener)
        {
            ComplexTooltip.OnComplexTooltipGeneratedForFood += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddStatusEffectTooltipCreatedListener(Action<GameObject, StatusEffect> listener)
        {
            ComplexTooltip.OnComplexTooltipGeneratedForStatusEffect += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddSkillTooltipCreatedListener(Action<GameObject, Skills.Skill> listener)
        {
            ComplexTooltip.OnComplexTooltipGeneratedForSkill += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddItemStatPreprocessor(Func<ItemDrop.ItemData, string, string, Tuple<string, string>> itemStatPreprocessor)
        {
            ComplexTooltip.AddItemStatPreprocessor(itemStatPreprocessor);
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
        public static GameObject ComplexTooltip_AddCheckBoxTextBox(GameObject complexTooltipGO)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.CheckBoxTextBoxPrefab).gameObject;
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
        public static void ComplexTooltip_SetDescription(GameObject complexTooltipGO, string desc)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetDescription(desc);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetIcon(GameObject complexTooltipGO, Sprite icon)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetIcon(icon);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_EnableDescription(GameObject complexTooltipGO, bool enabled)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.EnableDescription(enabled);
        }

        [UsedImplicitly]
        public static string ComplexTooltip_GenerateItemSubtext(GameObject complexTooltipGO, ItemDrop.ItemData item)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return string.Empty;
            }

            return complexTooltip.GenerateItemSubtext(item);
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

        [UsedImplicitly]
        public static void ComplexTooltip_SetItemNoTextBoxes(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetItemNoTextBoxes(item, quality, variant);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetFood(GameObject complexTooltipGO, Player.Food food)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetFood(food);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetStatusEffect(GameObject complexTooltipGO, StatusEffect statusEffect)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetStatusEffect(statusEffect);
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetSkill(GameObject complexTooltipGO, Skills.Skill skill)
        {
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetSkill(skill);
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
        public static Image RequirementsPanel_GetIcon(GameObject requirementsPanelGO)
        {
            return !RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel) ? null : requirementsPanel.Icon;
        }

        [UsedImplicitly]
        public static GameObject[] RequirementsPanel_RequirementList(GameObject requirementsPanelGO)
        {
            return !RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel) ? null : requirementsPanel.RequirementList;
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


        // Custom Variant Panel
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow)
        {
            if (WorkbenchPanelController.instance == null)
            {
                return null;
            }

            return WorkbenchPanelController.instance.CraftingPanel.EnableCustomVariantDialog(buttonLabel, onShow);
        }

        [UsedImplicitly]
        public static void CustomVariantPanel_SetButtonLabel(string buttonLabel)
        {
            if (WorkbenchPanelController.instance == null)
            {
                return;
            }

            WorkbenchPanelController.instance.CraftingPanel.SetCustomVariantButtonLabel(buttonLabel);
        }

        [UsedImplicitly]
        public static void CustomVariantPanel_Disable()
        {
            WorkbenchPanelController.instance?.CraftingPanel.DisableCustomVariantDialog();
        }
    }
}
