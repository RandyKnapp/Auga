using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ! API
using System.Linq;
using System.Threading;
using AugaUnity;
using Object = UnityEngine.Object;
#endif

namespace Auga
{
    [PublicAPI]
    public static class API
    {
        public static string RedText = "#CD2121";
        public static string Red = "#AD1616";
        public static string Brown1 = "#EAE1D9";
        public static string Brown2 = "#D1C9C2";
        public static string Brown3 = "#A39689";
        public static string Brown4 = "#706457";
        public static string Brown5 = "#2E2620";
        public static string Brown6 = "#EAE1D9";
        public static string Brown7 = "#181410";
        public static string Blue = "#216388";
        public static string LightBlue = "#1AACEF";
        public static string Gold = "#B98A12";
        public static string BrightGold = "#EAA800";
        public static string BrightestGold = "#FFBF1B";
        public static string GoldDark = "#755608";
        public static string Green = "#1B9B37";

        [UsedImplicitly]
        public static bool IsLoaded()
        {
#if ! API
            return true;
#else
            return false;
#endif
        }
        // Fonts & Assets
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static Font GetBoldFont()
        {
#if ! API
            return Auga.Assets.SourceSansProBold;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Font GetSemiBoldFont()
        {
#if ! API
            return Auga.Assets.SourceSansProSemiBold;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Font GetRegularFont()
        {
#if ! API
            return Auga.Assets.SourceSansProRegular;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Sprite GetItemBackgroundSprite()
        {
#if ! API
            return Auga.Assets.ItemBackgroundSprite;
#else
            return null;
#endif
        }

        // Panels & Buttons
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static GameObject Panel_Create(Transform parent, Vector2 size, string name, bool withCornerDecoration)
        {
#if ! API
            var panel = Object.Instantiate(Auga.Assets.PanelBase, parent);
            if (Auga.Assets.PanelBase == null)
            {
                Auga.LogError($"Auga.Assets.PanelBase is null");
                Thread.Sleep(25000);
            }
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
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button SmallButton_Create(Transform parent, string name, string labelText)
        {
#if ! API
            var button = Object.Instantiate(Auga.Assets.ButtonSmall, parent);
            button.name = name;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button MediumButton_Create(Transform parent, string name, string labelText)
        {
#if ! API
            var button = Object.Instantiate(Auga.Assets.ButtonMedium, parent);
            button.name = name;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button FancyButton_Create(Transform parent, string name, string labelText)
        {
#if ! API
            var button = Object.Instantiate(Auga.Assets.ButtonFancy, parent);
            button.name = name;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button SettingsButton_Create(Transform parent, string name, string labelText)
        {
#if ! API
            var button = Object.Instantiate(Auga.Assets.ButtonSettings, parent);
            button.name = name;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = Localization.instance.Localize(labelText);
            }

            return button.GetComponent<ColorButtonText>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button DiamondButton_Create(Transform parent, string name, [CanBeNull] Sprite icon)
        {
#if ! API
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
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject Divider_CreateSmall(Transform parent, string name, float width = -1)
        {
#if ! API
            var divider = Object.Instantiate(Auga.Assets.DividerSmall, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return divider;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Tuple<GameObject, GameObject> Divider_CreateMedium(Transform parent, string name, float width = -1)
        {
#if ! API
            var divider = Object.Instantiate(Auga.Assets.DividerMedium, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return new Tuple<GameObject, GameObject>(divider, divider.transform.Find("Content").gameObject);
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Tuple<GameObject, GameObject> Divider_CreateLarge(Transform parent, string name, float width = -1)
        {
#if ! API
            var divider = Object.Instantiate(Auga.Assets.DividerLarge, parent);
            divider.name = name;
            if (width >= 0)
            {
                divider.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            return new Tuple<GameObject, GameObject>(divider, divider.transform.Find("Content").gameObject);
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static void Button_SetTextColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, Color baseTextColor)
        {
#if ! API
            var colorValues = button.GetComponent<ColorButtonTextValues>();
            if (colorValues != null)
            {
                colorValues.TextColors.normalColor = normal;
                colorValues.TextColors.highlightedColor = highlighted;
                colorValues.TextColors.pressedColor = pressed;
                colorValues.TextColors.selectedColor = selected;
                colorValues.TextColors.disabledColor = disabled;
            }

            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = baseTextColor;
            }
#endif
        }

        [UsedImplicitly]
        public static void Button_OverrideTextColor(Button button, Color color)
        {
#if ! API
            var colorValues = button.GetComponent<ColorButtonTextValues>();
            if (colorValues != null)
            {
                Object.Destroy(colorValues);
            }

            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = color;
            }
#endif
        }

        [UsedImplicitly]
        public static void Tooltip_MakeSimpleTooltip(GameObject obj)
        {
#if ! API
            var uiTooltip = obj.GetComponent<UITooltip>();
            if (uiTooltip == null)
            {
                uiTooltip = obj.AddComponent<UITooltip>();
            }

            uiTooltip.m_tooltipPrefab = Auga.Assets.SimpleTooltip;
#endif
        }

        [UsedImplicitly]
        public static void Tooltip_MakeItemTooltip(GameObject obj, ItemDrop.ItemData item)
        {
#if ! API
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
#endif
        }

        [UsedImplicitly]
        public static void Tooltip_MakeFoodTooltip(GameObject obj, Player.Food food)
        {
#if ! API
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
#endif
        }

        [UsedImplicitly]
        public static void Tooltip_MakeStatusEffectTooltip(GameObject obj, StatusEffect statusEffect)
        {
#if ! API
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
#endif
        }

        [UsedImplicitly]
        public static void Tooltip_MakeSkillTooltip(GameObject obj, Skills.Skill skill)
        {
#if ! API
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
#endif
        }

        // Player Panel Tabs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static bool PlayerPanel_HasTab(string tabID)
        {
#if ! API
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.HasPlayerPanelTab(tabID);
#else
            return false;
#endif
        }

        [UsedImplicitly]
        public static PlayerPanelTabData PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
        {
#if ! API
            if (WorkbenchPanelController.instance != null)
            {
                var data = new PlayerPanelTabData();

                WorkbenchPanelController.instance.AddPlayerPanelTab(tabID, tabIcon, tabTitleText, onTabSelected, out data.TabTitle, out var tabButton, out data.ContentGO);
                data.Index = WorkbenchPanelController.instance.DefaultTabController.TabButtons.Count - 1;
                data.TabButtonGO = tabButton.gameObject;

                return data;
            }

            return null;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static bool PlayerPanel_IsTabActive(GameObject tabButton)
        {
#if ! API
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActive(tabButton);
#else
            return false;
#endif
        }

        [UsedImplicitly]
        public static Button PlayerPanel_GetTabButton(int index)
        {
#if ! API
            if (WorkbenchPanelController.instance != null && index >= 0 && index < WorkbenchPanelController.instance.DefaultTabController.TabButtons.Count)
            {
                return WorkbenchPanelController.instance.DefaultTabController.TabButtons[index].Button;
            }
            return null;
#else
            return null;
#endif
        }

        // Workbench Tabs
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static bool Workbench_HasWorkbenchTab(string tabID)
        {
#if ! API
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.HasWorkbenchTab(tabID);
#else
            return false;
#endif
        }

        [UsedImplicitly]
        public static WorkbenchTabData Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
        {
#if ! API
            if (WorkbenchPanelController.instance != null)
            {
                var data = new WorkbenchTabData();

                WorkbenchPanelController.instance.AddWorkbenchTab(tabID, tabIcon, tabTitleText, onTabSelected, false, out data.TabTitle, out var tabButtonX, out var requirementsPanelX, out var itemInfoX);
                data.Index = WorkbenchPanelController.instance.WorkbenchTabController.TabButtons.Count - 1;
                data.TabButtonGO = tabButtonX.gameObject;
                data.RequirementsPanelGO = requirementsPanelX.gameObject;
                data.ItemInfoGO = itemInfoX.gameObject;

                return data;
            }

            return null;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static WorkbenchTabData Workbench_AddVanillaWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)
        {
#if ! API
            if (WorkbenchPanelController.instance != null)
            {
                var data = new WorkbenchTabData();

                WorkbenchPanelController.instance.AddWorkbenchTab(tabID, tabIcon, tabTitleText, onTabSelected, true, out data.TabTitle, out var tabButtonX, out var requirementsPanelX, out var itemInfoX);
                data.Index = WorkbenchPanelController.instance.WorkbenchTabController.TabButtons.Count - 1;
                data.TabButtonGO = tabButtonX.gameObject;
                data.RequirementsPanelGO = requirementsPanelX.gameObject;
                data.ItemInfoGO = itemInfoX.gameObject;

                return data;
            }

            return null;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static bool Workbench_IsTabActive(GameObject tabButton)
        {
#if ! API
            return WorkbenchPanelController.instance != null && WorkbenchPanelController.instance.IsTabActive(tabButton);
#else
            return false;
#endif
        }

        [UsedImplicitly]
        public static Button Workbench_GetCraftingTabButton()
        {
#if ! API
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[0].GetComponent<Button>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static Button Workbench_GetUpgradeTabButton()
        {
#if ! API
            return WorkbenchPanelController.instance?.WorkbenchTabController.TabButtons[1].GetComponent<Button>();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject Workbench_CreateNewResultsPanel()
        {
#if ! API
            return WorkbenchPanelController.instance?.CraftingPanel.CreateResultsPanel();
#else
            return null;
#endif
        }


        // Tooltip Text Boxes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if ! API
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
#endif

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize = true)
        {
#if ! API
            TooltipTextBox_AddLine(tooltipTextBoxGO, t, s, localize, false);
#endif
        }
        
        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize, bool overwrite)
        {
#if ! API
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(t, s, localize, overwrite);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize = true)
        {
#if ! API
            TooltipTextBox_AddLine(tooltipTextBoxGO,a,localize,false);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize, bool overwrite)
        {
#if ! API
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, localize, overwrite);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize = true)
        {
#if ! API
            TooltipTextBox_AddLine(tooltipTextBoxGO,a,b,localize,false);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize, bool overwrite)
        {
#if ! API
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, b, localize, overwrite);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize = true)
        {
#if ! API
            TooltipTextBox_AddLine(tooltipTextBoxGO,a,b,parenthetical,localize,false);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize, bool overwrite)
        {
#if ! API
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddLine(a, b, parenthetical, localize, overwrite);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize = true)
        {
#if ! API
            TooltipTextBox_AddUpgradeLine(tooltipTextBoxGO,label,value1,value2,color2, localize,false);
#endif
        }

        [UsedImplicitly]
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize, bool overwrite)
        {
#if ! API
            if (!TextBoxCheck(tooltipTextBoxGO, out var tooltipTextBox))
            {
                return;
            }

            tooltipTextBox.AddUpgradeLine(label, value1, value2, color2, localize, overwrite);
#endif
        }


        // Complex Tooltip
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if ! API
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
#endif

        [UsedImplicitly]
        public static void ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData> listener)
        {
#if ! API
            ComplexTooltip.OnComplexTooltipGeneratedForItem += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddFoodTooltipCreatedListener(Action<GameObject, Player.Food> listener)
        {
#if ! API
            ComplexTooltip.OnComplexTooltipGeneratedForFood += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddStatusEffectTooltipCreatedListener(Action<GameObject, StatusEffect> listener)
        {
#if ! API
            ComplexTooltip.OnComplexTooltipGeneratedForStatusEffect += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddSkillTooltipCreatedListener(Action<GameObject, Skills.Skill> listener)
        {
#if ! API
            ComplexTooltip.OnComplexTooltipGeneratedForSkill += (complexTooltip, item) => listener(complexTooltip.gameObject, item);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_AddItemStatPreprocessor(Func<ItemDrop.ItemData, string, string, Tuple<string, string>> itemStatPreprocessor)
        {
#if ! API
            ComplexTooltip.AddItemStatPreprocessor(itemStatPreprocessor);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_ClearTextBoxes(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.ClearTextBoxes();
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddTwoColumnTextBox(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.TwoColumnTextBoxPrefab).gameObject;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddCenteredTextBox(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.CenteredTextBoxPrefab).gameObject;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddUpgradeLabels(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.UpgradeLabelsPrefab).gameObject;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddUpgradeTwoColumnTextBox(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.UpgradeTwoColumnTextBoxPrefab).gameObject;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddCheckBoxTextBox(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddTextBox(complexTooltip.CheckBoxTextBoxPrefab).gameObject;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject ComplexTooltip_AddDivider(GameObject complexTooltipGO)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return null;
            }

            return complexTooltip.AddDivider();
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetTopic(GameObject complexTooltipGO, string topic)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetTopic(topic);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetSubtitle(GameObject complexTooltipGO, string topic)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetSubtitle(topic);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetDescription(GameObject complexTooltipGO, string desc)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetDescription(desc);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetIcon(GameObject complexTooltipGO, Sprite icon)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetIcon(icon);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_EnableDescription(GameObject complexTooltipGO, bool enabled)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.EnableDescription(enabled);
#endif
        }

        [UsedImplicitly]
        public static string ComplexTooltip_GenerateItemSubtext(GameObject complexTooltipGO, ItemDrop.ItemData item)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return string.Empty;
            }

            return complexTooltip.GenerateItemSubtext(item);
#else
            return string.Empty;
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetItem(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetItem(item, quality, variant);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetItemNoTextBoxes(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetItemNoTextBoxes(item, quality, variant);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetFood(GameObject complexTooltipGO, Player.Food food)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetFood(food);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetStatusEffect(GameObject complexTooltipGO, StatusEffect statusEffect)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetStatusEffect(statusEffect);
#endif
        }

        [UsedImplicitly]
        public static void ComplexTooltip_SetSkill(GameObject complexTooltipGO, Skills.Skill skill)
        {
#if ! API
            if (!ComplexTooltipCheck(complexTooltipGO, out var complexTooltip))
            {
                return;
            }

            complexTooltip.SetSkill(skill);
#endif
        }


        // Requirements Panel
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if ! API
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
#endif
        [UsedImplicitly]
        public static Image RequirementsPanel_GetIcon(GameObject requirementsPanelGO)
        {
#if ! API
            return !RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel) ? null : requirementsPanel.Icon;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static GameObject[] RequirementsPanel_RequirementList(GameObject requirementsPanelGO)
        {
#if ! API
            return !RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel) ? null : requirementsPanel.RequirementList;
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static void RequirementsPanel_SetWires(GameObject requirementsPanelGO, RequirementWireState[] wireStates, bool canCraft)
        {
#if ! API
            if (!RequirementsPanelCheck(requirementsPanelGO, out var requirementsPanel))
            {
                return;
            }

            var convertedWireStates = wireStates.Select(wireState => (WireState)wireState).ToList();
            requirementsPanel.WireFrame.Set(convertedWireStates, canCraft);
#endif
        }


        // Custom Variant Panel
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [UsedImplicitly]
        public static TMP_Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow)
        {
#if ! API
            if (WorkbenchPanelController.instance == null)
            {
                return null;
            }

            return WorkbenchPanelController.instance.CraftingPanel.EnableCustomVariantDialog(buttonLabel, onShow);
#else
            return null;
#endif
        }

        [UsedImplicitly]
        public static void CustomVariantPanel_SetButtonLabel(string buttonLabel)
        {
#if ! API
            if (WorkbenchPanelController.instance == null)
            {
                return;
            }

            WorkbenchPanelController.instance.CraftingPanel.SetCustomVariantButtonLabel(buttonLabel);
#endif
        }

        [UsedImplicitly]
        public static void CustomVariantPanel_Disable()
        {
#if ! API
            WorkbenchPanelController.instance?.CraftingPanel.DisableCustomVariantDialog();
#endif
        }
    }
}
