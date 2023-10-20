using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaTabController : MonoBehaviour
    {
        public List<TMP_Text> TabTitles = new List<TMP_Text>();
        public List<TabButton> TabButtons = new List<TabButton>();
        public List<GameObject> TabContents = new List<GameObject>();

        public event Action<int, int> OnTabChanged;

        public int SelectedIndex { get; private set; } = -1;

        public virtual void Awake()
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
            if (SelectedIndex == index)
            {
                return;
            }

            var previousIndex = SelectedIndex;
            SelectedIndex = index;
            for (var tabIndex = 0; tabIndex < TabButtons.Count; tabIndex++)
            {
                
                var selected = tabIndex == SelectedIndex;

                var tabTitle = tabIndex < TabTitles.Count ? TabTitles[tabIndex] : null;
                var tabButton = tabIndex < TabButtons.Count ? TabButtons[tabIndex] : null;
                var tabContent = tabIndex < TabContents.Count ? TabContents[tabIndex] : null;

                if (tabTitle == null || tabButton == null || tabContent == null)
                {
                    Debug.LogWarning($"tabTitle is nul: {tabTitle == null}");
                    Debug.LogWarning($"tabButton is nul: {tabButton == null}");
                    Debug.LogWarning($"tabContent is nul: {tabContent == null}");
                }
                
                if (tabTitle != null)
                {
                    tabTitle.gameObject.SetActive(selected);
                }

                if (tabButton != null)
                {
                    tabButton.SetSelected(selected);
                }

                if (tabContent != null)
                {
                    tabContent.SetActive(selected);
                }
            }

            if (SelectedIndex >= 0)
            {
                if (SelectedIndex < TabTitles.Count)
                {
                    TabTitles[SelectedIndex].gameObject.SetActive(true);
                }

                if (SelectedIndex < TabButtons.Count)
                {
                    TabButtons[SelectedIndex].SetSelected(true);
                }

                if (SelectedIndex < TabContents.Count)
                {
                    TabContents[SelectedIndex].SetActive(true);
                }
            }

            OnTabChanged?.Invoke(previousIndex, SelectedIndex);
        }
    }
}
