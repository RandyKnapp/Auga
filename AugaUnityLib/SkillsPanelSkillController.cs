using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class SkillsPanelSkillController : MonoBehaviour
    {
        public Skills.SkillType SkillType = Skills.SkillType.None;
        public Image Icon;
        public Image ProgressBarLevel;
        public Image ProgressBarAccumulator;
        public Text NameText;
        public Text LevelText;

        private SkillTooltip _skillTooltip;

        public void Awake()
        {
            _skillTooltip = GetComponent<SkillTooltip>();
        }

        public void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var skills = player.GetSkills();
            if (skills.m_skillData.TryGetValue(SkillType, out var skillData))
            {
                _skillTooltip.Skill = skillData;

                Icon.sprite = skillData.m_info.m_icon;
                NameText.text = Localization.instance.Localize("$skill_" + SkillType.ToString().ToLower());
                LevelText.text = $"$level {skillData.m_level}";
                const float start = 0.2f;
                const float end = 0.5f - start;
                ProgressBarLevel.fillAmount = Mathf.Lerp(start, end, skillData.m_level / 100f);
                ProgressBarAccumulator.fillAmount = Mathf.Lerp(start, end, skillData.GetLevelPercentage());
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                Update();
            }
        }
    }
}
