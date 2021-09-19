using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaStaggerBar : MonoBehaviour
    {
        public GuiBar LeftBar;
        public Image Icon;

        public void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                var staggerPercentage = player.GetStaggerPercentage();
                LeftBar.SetValue(staggerPercentage);

                Icon.enabled = true;

                var leftItem = player.GetLeftItem();
                var rightItem = player.GetRightItem();
                if (leftItem != null && leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                {
                    Icon.sprite = leftItem.GetIcon();
                }
                else if (rightItem != null && rightItem.IsWeapon())
                {
                    Icon.sprite = rightItem.GetIcon();
                }
                else
                {
                    var unarmedSkill = player.GetSkills().GetSkillDef(Skills.SkillType.Unarmed);
                    Icon.sprite = unarmedSkill.m_icon;
                }
            }
            else
            {
                Icon.enabled = false;
            }
        }
    }
}
