using Fishlabs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public enum RequirementWireState
    {
        Absent,
        Have,
        DontHave
    }

    public class PlayerPanelTabData
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject ContentGO;
    }

    public class WorkbenchTabData
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject RequirementsPanelGO;
        public GameObject ItemInfoGO;
    }
    public class CraftingControls
    {
        public Button CraftButton;
        public GameObject Multicraft;
        public Button PlusButton;
        public Button MinusButton;
        public TMP_Text CraftAmountText;
        public GameObject CraftAmountBG;
        public GameObject Amount;
        public GuiInputField InputAmount;
        public TMP_Text InputText;
    }
}
