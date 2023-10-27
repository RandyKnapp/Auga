using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AugaUnity
{
    public class SkillsPanelController : MonoBehaviour
    {
        public GameObject SkillsContainer;
        public SkillsPanelSkillController SkillPrefab;

        protected readonly Dictionary<Skills.SkillType, SkillsPanelSkillController> _skills = new Dictionary<Skills.SkillType, SkillsPanelSkillController>();
        protected int _skillsCount;
        protected SkillsDialog _skillsDialog;

        public virtual void Start()
        {
            SkillPrefab.gameObject.SetActive(false);
            _skillsDialog = GetComponent<SkillsDialog>();
            UpdateSkillsDialog();
            InvokeRepeating(nameof(UpdateSkillsDialog), 0f, 1f);
        }

        private void UpdateSkillsDialog()
        {
            if (!isActiveAndEnabled)
                return;

            var player = Player.m_localPlayer;
            if (player != null)
            {
                UpdateSkills(player);
                _skillsDialog.Setup(player);
            }
        }

        public virtual void UpdateSkills(Player player)
        {
            var skills = player.GetSkills();

            foreach (var skillDef in skills.m_skills)
            {
                _skills.TryGetValue(skillDef.m_skill, out var currentSkillElement);

                if (skills.m_skillData.ContainsKey(skillDef.m_skill))
                {
                    if (currentSkillElement == null)
                    {
                        var effect = Instantiate(SkillPrefab, SkillsContainer.transform, false);
                        effect.SkillType = skillDef.m_skill;
                        _skills.Add(skillDef.m_skill, effect);
                    }
                    else
                    {
                        currentSkillElement.SetActive(true);
                    }
                }
                else if (currentSkillElement != null)
                {
                    currentSkillElement.SetActive(true);
                }
            }

            if (_skillsCount != _skills.Count)
            {
                _skillsCount = _skills.Count;
                SortSkillElements();
            }
        }

        public virtual void SortSkillElements()
        {
            var children = SkillsContainer.transform.Cast<Transform>().Select(x => x.GetComponent<SkillsPanelSkillController>()).ToList();
            children.Sort((a, b) => a.SkillType.CompareTo(b.SkillType));
            for (var i = 0; i < children.Count; ++i)
            {
                children[i].transform.SetSiblingIndex(i);
            }
        }
    }
}
