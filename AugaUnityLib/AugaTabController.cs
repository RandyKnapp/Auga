using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaTabController : MonoBehaviour
    {
        public List<Text> TabTitles = new List<Text>();
        public List<TabButton> TabButtons = new List<TabButton>();
        public List<GameObject> TabContents = new List<GameObject>();

        private int _selectedIndex = -1;

        public virtual void Start()
        {
            for (var index = 0; index < TabButtons.Count; index++)
            {
                var tabButton = TabButtons[index];
                var i = index;
                tabButton.Button.onClick.AddListener(() => SelectTab(i));
            }
            SelectTab(0);
        }

        public virtual void SelectTab(int index)
        {
            if (_selectedIndex == index)
            {
                return;
            }

            _selectedIndex = index;
            for (var tabIndex = 0; tabIndex < TabTitles.Count; tabIndex++)
            {
                var selected = tabIndex == _selectedIndex;

                var tabTitle = TabTitles[tabIndex];
                var tabButton = TabButtons[tabIndex];
                var tabContent = TabContents[tabIndex];

                tabTitle.gameObject.SetActive(selected);
                tabButton.SetSelected(selected);
                tabContent.SetActive(selected);
            }
        }
    }
}
