using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public enum TooltipObjectBackgroundType
    {
        Item, Diamond, Skill
    }

    public enum RequirementWireState
    {
        Absent,
        Have,
        DontHave
    }

    public class WorkbenchTabData
    {
        public int Index;
        public Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject RequirementsPanelGO;
        public GameObject ItemInfoGO;
    }
}
