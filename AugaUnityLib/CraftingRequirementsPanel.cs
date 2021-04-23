using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class CraftingRequirementsPanel : MonoBehaviour
    {
        public GameObject[] RequirementList = new GameObject[0];
        public Image Icon;
        [CanBeNull] public Image UpgradedIcon;
        [CanBeNull] public Image WorkbenchIcon;
        public Text WorkbenchLevel;
        [CanBeNull] public Text OriginalQualityLevel;
        [CanBeNull] public Text NewQualityLevel;

        public void Activate(InventoryGui inventoryGui, ComplexTooltip itemInfo)
        {
            inventoryGui.m_recipeRequirementList = RequirementList;
            itemInfo.Icon = Icon;
            ColorUtility.TryParseHtmlString("#EAE1D9", out inventoryGui.m_minStationLevelBasecolor);
            inventoryGui.m_minStationLevelText = WorkbenchLevel;
            Update();
        }

        public void Update()
        {
            var inventoryGui = InventoryGui.instance;

            if (UpgradedIcon != null)
            {
                UpgradedIcon.enabled = Icon.enabled;
                UpgradedIcon.sprite = Icon.sprite;
            }

            if (Player.m_localPlayer != null)
            {
                var workbench = Player.m_localPlayer.GetCurrentCraftingStation();

                if (WorkbenchIcon != null)
                {
                    WorkbenchIcon.enabled = workbench != null;
                    if (workbench != null)
                    {
                        WorkbenchIcon.sprite = workbench.m_icon;
                    }
                }

                var itemData = inventoryGui.m_selectedRecipe.Value;
                if (itemData != null && OriginalQualityLevel != null && NewQualityLevel != null)
                {
                    OriginalQualityLevel.text = itemData.m_quality.ToString();
                    NewQualityLevel.text = (itemData.m_quality + 1).ToString();
                }
            }
        }
    }
}
